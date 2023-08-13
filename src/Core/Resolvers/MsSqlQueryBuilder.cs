// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Data.Common;
using System.Text;
using Azure.DataApiBuilder.Config.DatabasePrimitives;
using Azure.DataApiBuilder.Config.ObjectModel;
using Azure.DataApiBuilder.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

namespace Azure.DataApiBuilder.Core.Resolvers
{
    /// <summary>
    /// Class for building MsSql queries.
    /// </summary>
    public class MsSqlQueryBuilder : BaseSqlQueryBuilder, IQueryBuilder
    {
        private const string FOR_JSON_SUFFIX = " FOR JSON PATH, INCLUDE_NULL_VALUES";
        private const string WITHOUT_ARRAY_WRAPPER_SUFFIX = "WITHOUT_ARRAY_WRAPPER";

        // Name of the column which stores the number of records with given PK. Used in Upsert queries.
        public const string COUNT_ROWS_WITH_GIVEN_PK = "cnt_rows_to_update";

        private static DbCommandBuilder _builder = new SqlCommandBuilder();

        /// <inheritdoc />
        public override string QuoteIdentifier(string ident)
        {
            return _builder.QuoteIdentifier(ident);
        }

        /// <inheritdoc />
        public string Build(SqlQueryStructure structure)
        {
            string dataIdent = QuoteIdentifier(SqlQueryStructure.DATA_IDENT);
            string fromSql = $"{QuoteIdentifier(structure.DatabaseObject.SchemaName)}.{QuoteIdentifier(structure.DatabaseObject.Name)} " +
                             $"AS {QuoteIdentifier($"{structure.SourceAlias}")}{Build(structure.Joins)}";

            fromSql += string.Join(
                    "",
                    structure.JoinQueries.Select(
                        x => $" OUTER APPLY ({Build(x.Value)}) AS {QuoteIdentifier(x.Key)}({dataIdent})"));

            string predicates = JoinPredicateStrings(
                                    structure.GetDbPolicyForOperation(EntityActionOperation.Read),
                                    structure.FilterPredicates,
                                    Build(structure.Predicates),
                                    Build(structure.PaginationMetadata.PaginationPredicate));

            string query = $"SELECT TOP {structure.Limit()} {WrappedColumns(structure)}"
                + $" FROM {fromSql}"
                + $" WHERE {predicates}"
                + $" ORDER BY {Build(structure.OrderByColumns)}";

            query += FOR_JSON_SUFFIX;
            if (!structure.IsListQuery)
            {
                query += "," + WITHOUT_ARRAY_WRAPPER_SUFFIX;
            }

            return query;
        }

        /// <inheritdoc />
        public string Build(SqlInsertStructure structure)
        {
            SourceDefinition sourceDefinition = structure.GetUnderlyingSourceDefinition();
            bool isInsertDMLTriggerEnabled = sourceDefinition.IsInsertDMLTriggerEnabled;
            string tableName = $"{QuoteIdentifier(structure.DatabaseObject.SchemaName)}.{QuoteIdentifier(structure.DatabaseObject.Name)}";

            // Predicates by virtue of database policy for Create action.
            string dbPolicypredicates = JoinPredicateStrings(structure.GetDbPolicyForOperation(EntityActionOperation.Create));

            // Columns whose values are provided in the request body - to be inserted into the record.
            string insertColumns = Build(structure.InsertColumns);

            // Values to be inserted into the entity.
            string values = dbPolicypredicates.Equals(BASE_PREDICATE) ?
                $"VALUES ({string.Join(", ", structure.Values)});" : $"SELECT {insertColumns} FROM (VALUES({string.Join(", ", structure.Values)})) T({insertColumns}) WHERE {dbPolicypredicates};";

            // Final insert query to be executed against the database.
            StringBuilder insertQuery = new();
            if (!isInsertDMLTriggerEnabled)
            {
                // When there is no DML trigger enabled on the table for insert operation, we can use OUTPUT clause to return the data.
                insertQuery.Append($"INSERT INTO {tableName} ({insertColumns}) OUTPUT " +
                    $"{MakeOutputColumns(structure.OutputColumns, OutputQualifier.Inserted.ToString())} ");
                insertQuery.Append(values);
            }
            else
            {
                // When a DML trigger for INSERT operation is enabled on the table, its a bit tricky to get the inserted data.
                // We need to insert the values for all the non-autogenerated columns in the PK into a temporary table.
                // Finally this temporary table will be used to do a subsequent select on the actual table where we would join the
                // actual table and the temporary table based on the values of the non-autogenerated PK columns.
                // If there is a column in the PK which is autogenerated, we cannot and will not insert it into the temporary table.
                // Hence in the select query, we will add an additional WHERE predicate (alongwith the join).
                // for the autogenerated column in the PK.
                // It is to be noted that MsSql supports only one IDENTITY/autogenerated column per table.

                string tempTableName = $"{QuoteIdentifier(structure.DatabaseObject.SchemaName)}.{QuoteIdentifier($"#{structure.DatabaseObject.Name}_T")}";
                (string autoGenPKColumn, List<string> nonAutoGenPKColumns) = GetSegregatedPKColumns(sourceDefinition);
                if (nonAutoGenPKColumns.Count > 0)
                {
                    // Create temporary table containing zero rows and all the non-autogenerated columns present in the PK.
                    // We need to create it only when there are non-autogenerated columns present in the PK.
                    string queryToCreateTempTable = $"SELECT {string.Join(", ", nonAutoGenPKColumns.Select(pk => $"{QuoteIdentifier(pk)}"))}" +
                        $" INTO {tempTableName} FROM {tableName} WHERE 0 = 1;";

                    // We need to output values of all the non-autogenerated columns in the PK into the temporary table.
                    string nonAutoGenPKsOutput = string.Join(", ", nonAutoGenPKColumns.Select(pk => $"{OutputQualifier.Inserted}.{QuoteIdentifier(pk)}"));

                    // Creation of temporary table followed by inserting data into actual table.
                    insertQuery.Append(queryToCreateTempTable);
                    insertQuery.Append($"INSERT INTO {tableName} ({insertColumns}) ");
                    insertQuery.Append($"OUTPUT {nonAutoGenPKsOutput} INTO {tempTableName} ");
                }
                else
                {
                    insertQuery.Append($"INSERT INTO {tableName} ({insertColumns}) ");
                }

                insertQuery.Append(values);

                // Build the subsequent select query to return the inserted data.
                StringBuilder subsequentSelect = new($"SELECT {MakeOutputColumns(structure.OutputColumns, tableName)} FROM {tableName} ");

                if (nonAutoGenPKColumns.Count > 0)
                {
                    // We will perform inner join on the basis of all the non-autogenerated columns in the PK.
                    string joinPredicates = string.Join(
                        "AND ",
                        nonAutoGenPKColumns.Select(pk => $"{tableName}.{QuoteIdentifier(pk)} = {tempTableName}.{QuoteIdentifier(pk)}"));
                    subsequentSelect.Append($"INNER JOIN {tempTableName} ON {joinPredicates} ");
                }

                if (!string.IsNullOrEmpty(autoGenPKColumn))
                {
                    // If there is an autogenerated column in the PK, we will add an additional WHERE condition for it.
                    // Using IDENT_CURRENT('tableName') method provided by sql server,
                    // we can get the last generated value of the autogenerated column.
                    subsequentSelect.Append($"WHERE {tableName}.{QuoteIdentifier(autoGenPKColumn)} = IDENT_CURRENT('{tableName}')");
                }

                insertQuery.Append(subsequentSelect.ToString());

                // Since we created a temporary table, it will be dropped automatically as the session terminates.
                // So, we don't need to explicitly drop the temporary table.
                insertQuery.Append(";");
            }

            return insertQuery.ToString();
        }

        /// <summary>
        /// Helper method to get the autogenerated column in the PK and the non-autogenerated ones seperately.
        /// </summary>
        /// <param name="sourceDefinition">Table definition.</param>
        private static (string, List<string>) GetSegregatedPKColumns(SourceDefinition sourceDefinition)
        {
            string autoGenPKColumn = string.Empty;
            List<string> nonAutoGenPKColumns = new();
            foreach (string primaryKey in sourceDefinition.PrimaryKey)
            {
                if (sourceDefinition.Columns[primaryKey].IsAutoGenerated)
                {
                    autoGenPKColumn = primaryKey;
                }
                else
                {
                    nonAutoGenPKColumns.Add(primaryKey);
                }
            }

            return (autoGenPKColumn, nonAutoGenPKColumns);
        }

        /// <inheritdoc />
        public string Build(SqlUpdateStructure structure)
        {
            SourceDefinition sourceDefinition = structure.GetUnderlyingSourceDefinition();
            bool isUpdateTriggerEnabled = sourceDefinition.IsUpdateDMLTriggerEnabled;
            string tableName = $"{QuoteIdentifier(structure.DatabaseObject.SchemaName)}.{QuoteIdentifier(structure.DatabaseObject.Name)}";
            string predicates = JoinPredicateStrings(
                                   structure.GetDbPolicyForOperation(EntityActionOperation.Update),
                                   Build(structure.Predicates));
            string columnsToBeReturned =
                MakeOutputColumns(structure.OutputColumns, isUpdateTriggerEnabled ? "" : OutputQualifier.Inserted.ToString());

            StringBuilder updateQuery = new($"UPDATE {tableName} SET {Build(structure.UpdateOperations, ", ")} ");

            // If a trigger is enabled on the entity, we cannot use OUTPUT clause to return the record.
            // In such a case, we will use a subsequent select query to get the record.
            if (isUpdateTriggerEnabled)
            {
                updateQuery.Append($"SELECT {columnsToBeReturned} FROM {tableName} WHERE {predicates};");
            }
            else
            {
                updateQuery.Append($"OUTPUT {columnsToBeReturned} WHERE {predicates};");
            }

            return updateQuery.ToString();
        }

        /// <inheritdoc />
        public string Build(SqlDeleteStructure structure)
        {
            string predicates = JoinPredicateStrings(
                       structure.GetDbPolicyForOperation(EntityActionOperation.Delete),
                       Build(structure.Predicates));

            return $"DELETE FROM {QuoteIdentifier(structure.DatabaseObject.SchemaName)}.{QuoteIdentifier(structure.DatabaseObject.Name)} " +
                    $"WHERE {predicates} ";
        }

        /// <inheritdoc />
        public string Build(SqlExecuteStructure structure)
        {
            return $"EXECUTE {QuoteIdentifier(structure.DatabaseObject.SchemaName)}.{QuoteIdentifier(structure.DatabaseObject.Name)} " +
                $"{BuildProcedureParameterList(structure.ProcedureParameters)}";
        }

        /// <inheritdoc />
        public string Build(SqlUpsertQueryStructure structure)
        {
            SourceDefinition sourceDefinition = structure.GetUnderlyingSourceDefinition();
            bool isUpdateTriggerEnabled = sourceDefinition.IsUpdateDMLTriggerEnabled;
            string tableName = $"{QuoteIdentifier(structure.DatabaseObject.SchemaName)}.{QuoteIdentifier(structure.DatabaseObject.Name)}";

            // Predicates by virtue of PK.
            string pkPredicates = JoinPredicateStrings(Build(structure.Predicates));

            // Predicates by virtue of PK + database policy.
            string updatePredicates = JoinPredicateStrings(pkPredicates, structure.GetDbPolicyForOperation(EntityActionOperation.Update));

            string updateOperations = Build(structure.UpdateOperations, ", ");
            string columnsToBeReturned =
                MakeOutputColumns(structure.OutputColumns, isUpdateTriggerEnabled ? "" : OutputQualifier.Inserted.ToString());
            string queryToGetCountOfRecordWithPK = $"SELECT COUNT(*) as {COUNT_ROWS_WITH_GIVEN_PK} FROM {tableName} WHERE {pkPredicates}";

            // Query to get the number of records with a given PK.
            string prefixQuery = $"DECLARE @ROWS_TO_UPDATE int;" +
                $"SET @ROWS_TO_UPDATE = ({queryToGetCountOfRecordWithPK}); " +
                $"{queryToGetCountOfRecordWithPK};";

            // Final query to be executed for the given PUT/PATCH operation.
            StringBuilder upsertQuery = new(prefixQuery);

            // Query to update record (if there exists one for given PK).
            StringBuilder updateQuery = new(
                $"IF @ROWS_TO_UPDATE = 1 " +
                $"BEGIN " +
                $"UPDATE {tableName} " +
                $"SET {updateOperations} ");

            if (isUpdateTriggerEnabled)
            {
                // If a trigger is enabled on the entity, we cannot use OUTPUT clause to return the record.
                // In such a case, we will use a subsequent select query to get the record.
                updateQuery.Append($"WHERE {updatePredicates};");
                string subsequentSelect = $"SELECT {columnsToBeReturned} FROM {tableName} WHERE {updatePredicates};";
                updateQuery.Append(subsequentSelect);
            }
            else
            {
                updateQuery.Append($"OUTPUT {columnsToBeReturned} WHERE {updatePredicates};");
            }

            // End the IF block.
            updateQuery.Append("END ");

            // Append the update query to upsert query.
            upsertQuery.Append(updateQuery);
            if (!structure.IsFallbackToUpdate)
            {
                // Append the conditional to check if the insert query is to be executed or not.
                // Insert is only attempted when no record exists corresponding to given PK.
                upsertQuery.Append("ELSE BEGIN ");

                // Columns which are assigned some value in the PUT/PATCH request.
                string insertColumns = Build(structure.InsertColumns);

                // Predicates added by virtue of database policy for create operation.
                string createPredicates = JoinPredicateStrings(structure.GetDbPolicyForOperation(EntityActionOperation.Create));

                // Query to insert record (if there exists none for given PK).
                StringBuilder insertQuery = new($"INSERT INTO {tableName} ({insertColumns}) ");

                bool isInsertTriggerEnabled = sourceDefinition.IsInsertDMLTriggerEnabled;
                if (!isInsertTriggerEnabled)
                {
                    // We can only use OUTPUT clause to return inserted data when there is no trigger enabled on the entity.
                    columnsToBeReturned = MakeOutputColumns(structure.OutputColumns, OutputQualifier.Inserted.ToString());
                    insertQuery.Append($"OUTPUT {columnsToBeReturned}");
                }
                else if (!isUpdateTriggerEnabled)
                {
                    // If an insert trigger is enabled but there was no update trigger enabled,
                    // we need to generated columns to be returned without 'Inserted' prefix.
                    columnsToBeReturned = MakeOutputColumns(structure.OutputColumns, string.Empty);
                }

                // Query to fetch the column values to be inserted into the entity.
                string fetchColumnValuesQuery = BASE_PREDICATE.Equals(createPredicates) ?
                    $"VALUES({string.Join(", ", structure.Values)});" :
                    $"SELECT {insertColumns} FROM (VALUES({string.Join(", ", structure.Values)})) T({insertColumns}) WHERE {createPredicates};";

                // Append the values to be inserted to the insertQuery.
                insertQuery.Append(fetchColumnValuesQuery);

                if (isInsertTriggerEnabled)
                {
                    // Since a trigger is enabled, a subsequent select query is to be executed to get the inserted record.
                    string subsequentSelect = $"SELECT {columnsToBeReturned} from {tableName} WHERE {pkPredicates};";
                    insertQuery.Append(subsequentSelect);
                }

                // Append the insert query to the upsert query.
                upsertQuery.Append(insertQuery.ToString());

                // End the ELSE block.
                upsertQuery.Append("END");
            }

            return upsertQuery.ToString();
        }

        /// <summary>
        /// Labels with which columns can be marked in the OUTPUT clause
        /// </summary>
        private enum OutputQualifier { Inserted, Deleted, None };

        /// <summary>
        /// Adds qualifiers (inserted or deleted) to output columns in OUTPUT clause
        /// and joins them with commas. e.g. for outputcolumns [C1, C2, C3] and output
        /// qualifier Inserted return
        /// Inserted.ColumnName1 AS {Label1}, Inserted.ColumnName2 AS {Label2},
        /// Inserted.ColumnName3 AS {Label3}
        /// </summary>
        private string MakeOutputColumns(List<LabelledColumn> columns, string columnPrefix)
        {
            return string.Join(", ", columns.Select(c => Build(c, columnPrefix)));
        }

        /// <summary>
        /// Build a labelled column as a column and attach
        /// ... AS {Label} to it
        /// </summary>
        private string Build(LabelledColumn column, string columnPrefix)
        {
            if (columnPrefix.IsNullOrEmpty())
            {
                return $"{QuoteIdentifier(column.ColumnName)} AS {QuoteIdentifier(column.Label)}";
            }

            return $"{columnPrefix}.{QuoteIdentifier(column.ColumnName)} AS {QuoteIdentifier(column.Label)}";
        }

        /// <summary>
        /// Add a JSON_QUERY wrapper on the column
        /// </summary>
        private string WrapSubqueryColumn(LabelledColumn column, SqlQueryStructure subquery)
        {
            string builtColumn = Build(column as Column);
            if (subquery.IsListQuery)
            {
                return $"JSON_QUERY (COALESCE({builtColumn}, '[]'))";
            }

            return $"JSON_QUERY ({builtColumn})";
        }

        /// <summary>
        /// Build columns and wrap columns which represent join subqueries
        /// </summary>
        private string WrappedColumns(SqlQueryStructure structure)
        {
            return string.Join(", ",
                structure.Columns.Select(
                    c => structure.IsSubqueryColumn(c) ?
                        WrapSubqueryColumn(c, structure.JoinQueries[c.TableAlias!]) + $" AS {QuoteIdentifier(c.Label)}" :
                        Build(c)
            ));
        }

        /// <summary>
        /// Builds the parameter list for the stored procedure execute call
        /// paramKeys are the user-generated procedure parameter names
        /// paramValues are the auto-generated, parameterized values (@param0, @param1..)
        /// </summary>
        private static string BuildProcedureParameterList(Dictionary<string, object> procedureParameters)
        {
            StringBuilder sb = new();
            foreach ((string paramKey, object paramValue) in procedureParameters)
            {
                sb.Append($"@{paramKey} = {paramValue}, ");
            }

            string parameterList = sb.ToString();
            // If at least one parameter added, remove trailing comma and space, else return empty string
            return parameterList.Length > 0 ? parameterList[..^2] : parameterList;
        }

        /// <summary>
        /// Builds the query to fetch result set details of stored-procedure.
        /// result_field_name is the name of the result column.
        /// result_type contains the sql type, i.e char,int,varchar. Using TYPE_NAME method
        /// allows us to get the type without size constraints. example: TYPE_NAME for both
        /// varchar(100) and varchar(max) would be varchar.
        /// is_nullable is a boolean value to know if the result column is nullable or not.
        /// </summary>
        public string BuildStoredProcedureResultDetailsQuery(string databaseObjectName)
        {
            string query = "SELECT " +
                            "name as result_field_name, TYPE_NAME(system_type_id) as result_type, is_nullable " +
                            "FROM " +
                            "sys.dm_exec_describe_first_result_set_for_object (" +
                            $"OBJECT_ID('{databaseObjectName}'), 0) " +
                            "WHERE is_hidden is not NULL AND is_hidden = 0";
            return query;
        }

        /// <inheritdoc/>
        public string GetQueryToGetEnabledTriggers()
        {
            string query = "SELECT STE.type_desc FROM sys.triggers ST inner join sys.trigger_events STE " +
                "On ST.object_id = STE.object_id AND ST.parent_id = object_id(@param0 + '.' + @param1) WHERE ST.is_disabled = 0;";

            return query;
        }
    }
}
