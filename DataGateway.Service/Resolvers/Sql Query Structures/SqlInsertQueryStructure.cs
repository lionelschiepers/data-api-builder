using System;
using System.Collections.Generic;
using System.Net;
using Azure.DataGateway.Service.Exceptions;
using Azure.DataGateway.Service.Models;
using Azure.DataGateway.Services;

namespace Azure.DataGateway.Service.Resolvers
{
    /// <summary>
    /// Wraps all the required data and logic to write a SQL INSERT query
    /// </summary>
    public class SqlInsertStructure : BaseSqlQueryStructure
    {
        /// <summary>
        /// Column names to insert into the given columns
        /// </summary>
        public List<string> InsertColumns { get; }

        /// <summary>
        /// Values to insert into the given columns
        /// </summary>
        public List<string> Values { get; }

        /// <summary>
        /// The inserted columns that the insert will return
        /// </summary>
        public List<string> ReturnColumns { get; }

        public SqlInsertStructure(string tableName, IMetadataStoreProvider metadataStore, IDictionary<string, object> mutationParams)
        : base(metadataStore)
        {
            TableName = tableName;
            InsertColumns = new();
            Values = new();

            TableDefinition tableDefinition = GetTableDefinition();

            // return primary key so the inserted row can be identified
            ReturnColumns = tableDefinition.PrimaryKey;

            foreach (KeyValuePair<string, object> param in mutationParams)
            {
                PopulateColumnsAndParams(param.Key, param.Value);
            }
        }

        /// <summary>
        /// Populates the column name in Columns, creates parameter
        /// and adds its value to Values.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="value">The value of the column.</param>
        private void PopulateColumnsAndParams(string columnName, object value)
        {
            InsertColumns.Add(columnName);
            string paramName;

            try
            {
                paramName = MakeParamWithValue(
                    GetParamAsColumnSystemType(value.ToString(), columnName));

                Values.Add($"@{paramName}");
            }
            catch (ArgumentException ex)
            {
                throw new DatagatewayException(
                    message: ex.Message,
                    statusCode: (int)HttpStatusCode.BadRequest,
                    subStatusCode: DatagatewayException.SubStatusCodes.BadRequest);
            }
        }
    }
}
