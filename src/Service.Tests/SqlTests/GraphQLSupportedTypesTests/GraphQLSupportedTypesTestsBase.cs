// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Azure.DataApiBuilder.Service.GraphQLBuilder.GraphQLTypes.SupportedTypes;

namespace Azure.DataApiBuilder.Service.Tests.SqlTests.GraphQLSupportedTypesTests
{

    [TestClass]
    public abstract class GraphQLSupportedTypesTestBase : SqlTestBase
    {
        protected const string TYPE_TABLE = "TypeTable";

        #region Tests

        [DataTestMethod]
        [DataRow(BYTE_TYPE, 1)]
        [DataRow(BYTE_TYPE, 2)]
        [DataRow(BYTE_TYPE, 3)]
        [DataRow(BYTE_TYPE, 4)]
        [DataRow(SHORT_TYPE, 1)]
        [DataRow(SHORT_TYPE, 2)]
        [DataRow(SHORT_TYPE, 3)]
        [DataRow(SHORT_TYPE, 4)]
        [DataRow(INT_TYPE, 1)]
        [DataRow(INT_TYPE, 2)]
        [DataRow(INT_TYPE, 3)]
        [DataRow(INT_TYPE, 4)]
        [DataRow(LONG_TYPE, 1)]
        [DataRow(LONG_TYPE, 2)]
        [DataRow(LONG_TYPE, 3)]
        [DataRow(LONG_TYPE, 4)]
        [DataRow(SINGLE_TYPE, 1)]
        [DataRow(SINGLE_TYPE, 2)]
        [DataRow(SINGLE_TYPE, 3)]
        [DataRow(SINGLE_TYPE, 4)]
        [DataRow(FLOAT_TYPE, 1)]
        [DataRow(FLOAT_TYPE, 2)]
        [DataRow(FLOAT_TYPE, 3)]
        [DataRow(FLOAT_TYPE, 4)]
        [DataRow(DECIMAL_TYPE, 1)]
        [DataRow(DECIMAL_TYPE, 2)]
        [DataRow(DECIMAL_TYPE, 3)]
        [DataRow(DECIMAL_TYPE, 4)]
        [DataRow(STRING_TYPE, 1)]
        [DataRow(STRING_TYPE, 2)]
        [DataRow(STRING_TYPE, 3)]
        [DataRow(STRING_TYPE, 4)]
        [DataRow(BOOLEAN_TYPE, 1)]
        [DataRow(BOOLEAN_TYPE, 2)]
        [DataRow(BOOLEAN_TYPE, 3)]
        [DataRow(BOOLEAN_TYPE, 4)]
        [DataRow(DATETIME_TYPE, 1)]
        [DataRow(DATETIME_TYPE, 2)]
        [DataRow(DATETIME_TYPE, 3)]
        [DataRow(DATETIME_TYPE, 4)]
        [DataRow(TIME_TYPE, 1)]
        [DataRow(TIME_TYPE, 2)]
        [DataRow(TIME_TYPE, 3)]
        [DataRow(TIME_TYPE, 4)]
        [DataRow(TIME_TYPE, 5)]
        [DataRow(BYTEARRAY_TYPE, 1)]
        [DataRow(BYTEARRAY_TYPE, 2)]
        [DataRow(BYTEARRAY_TYPE, 3)]
        [DataRow(BYTEARRAY_TYPE, 4)]
        [DataRow(GUID_TYPE, 1)]
        [DataRow(GUID_TYPE, 2)]
        [DataRow(GUID_TYPE, 3)]
        [DataRow(GUID_TYPE, 4)]
        public async Task QueryTypeColumn(string type, int id)
        {
            if (!IsSupportedType(type))
            {
                Assert.Inconclusive("Type not supported");
            }

            string field = $"{type.ToLowerInvariant()}_types";
            string graphQLQueryName = "supportedType_by_pk";
            string gqlQuery = "{ supportedType_by_pk(typeid: " + id + ") { " + field + " } }";

            string dbQuery = MakeQueryOnTypeTable(new List<string> { field }, id);

            JsonElement actual = await ExecuteGraphQLRequestAsync(gqlQuery, graphQLQueryName, isAuthenticated: false);
            string expected = await GetDatabaseResultAsync(dbQuery);

            PerformTestEqualsForExtendedTypes(type, expected, actual.ToString());
        }

        [DataTestMethod]
        [DataRow(BYTE_TYPE, "gt", "0", "0", ">")]
        [DataRow(BYTE_TYPE, "gte", "0", "0", ">=")]
        [DataRow(BYTE_TYPE, "lt", "1", "1", "<")]
        [DataRow(BYTE_TYPE, "lte", "1", "1", "<=")]
        [DataRow(BYTE_TYPE, "neq", "0", "0", "!=")]
        [DataRow(BYTE_TYPE, "eq", "1", "1", "=")]
        [DataRow(SHORT_TYPE, "gt", "-1", "-1", ">")]
        [DataRow(SHORT_TYPE, "gte", "-1", "-1", ">=")]
        [DataRow(SHORT_TYPE, "lt", "1", "1", "<")]
        [DataRow(SHORT_TYPE, "lte", "1", "1", "<=")]
        [DataRow(SHORT_TYPE, "neq", "1", "1", "!=")]
        [DataRow(SHORT_TYPE, "eq", "-1", "-1", "=")]
        [DataRow(INT_TYPE, "gt", "-1", "-1", ">")]
        [DataRow(INT_TYPE, "gte", "2147483647", "2147483647", " >= ")]
        [DataRow(INT_TYPE, "lt", "1", "1", "<")]
        [DataRow(INT_TYPE, "lte", "-2147483648", "-2147483648", " <= ")]
        [DataRow(INT_TYPE, "neq", "1", "1", "!=")]
        [DataRow(INT_TYPE, "eq", "-1", "-1", "=")]
        [DataRow(LONG_TYPE, "gt", "-1", "-1", ">")]
        [DataRow(LONG_TYPE, "gte", "9223372036854775807", "9223372036854775807", " >= ")]
        [DataRow(LONG_TYPE, "lt", "1", "1", "<")]
        [DataRow(LONG_TYPE, "lte", "-9223372036854775808", "-9223372036854775808", " <= ")]
        [DataRow(LONG_TYPE, "neq", "1", "1", "!=")]
        [DataRow(LONG_TYPE, "eq", "-1", "-1", "=")]
        [DataRow(STRING_TYPE, "neq", "\'foo\'", "\"foo\"", "!=")]
        [DataRow(STRING_TYPE, "eq", "\'lksa;jdflasdf;alsdflksdfkldj\'", "\"lksa;jdflasdf;alsdflksdfkldj\"", "=")]
        [DataRow(SINGLE_TYPE, "gt", "-9.3", "-9.3", ">")]
        [DataRow(SINGLE_TYPE, "gte", "-9.2", "-9.2", ">=")]
        [DataRow(SINGLE_TYPE, "lt", ".33", "0.33", "<")]
        [DataRow(SINGLE_TYPE, "lte", ".33", "0.33", "<=")]
        [DataRow(SINGLE_TYPE, "neq", "9.2", "9.2", "!=")]
        [DataRow(SINGLE_TYPE, "eq", "\'0.33\'", "0.33", "=")]
        [DataRow(FLOAT_TYPE, "gt", "-9.2", "-9.2", ">")]
        [DataRow(FLOAT_TYPE, "gte", "-9.2", "-9.2", ">=")]
        [DataRow(FLOAT_TYPE, "lt", ".33", "0.33", "<")]
        [DataRow(FLOAT_TYPE, "lte", ".33", "0.33", "<=")]
        [DataRow(FLOAT_TYPE, "neq", "-9.2", "-9.2", "!=")]
        [DataRow(FLOAT_TYPE, "eq", "-9.2", "-9.2", "=")]
        [DataRow(DECIMAL_TYPE, "gt", "-9.292929", "-9.292929", " > ")]
        [DataRow(DECIMAL_TYPE, "gte", "-9.292929", "-9.292929", " >= ")]
        [DataRow(DECIMAL_TYPE, "lt", "0.333333", "0.333333", "<")]
        [DataRow(DECIMAL_TYPE, "lte", "0.333333", "0.333333", " <= ")]
        [DataRow(DECIMAL_TYPE, "neq", "0.0", "0.0", "!=")]
        [DataRow(DECIMAL_TYPE, "eq", "-9.292929", "-9.292929", "=")]
        [DataRow(BOOLEAN_TYPE, "neq", "\'false\'", "false", "!=")]
        [DataRow(BOOLEAN_TYPE, "eq", "\'false\'", "false", "=")]
        public async Task QueryTypeColumnFilterAndOrderBy(string type, string filterOperator, string sqlValue, string gqlValue, string queryOperator)
        {
            if (!IsSupportedType(type))
            {
                Assert.Inconclusive("Type not supported");
            }

            string field = $"{type.ToLowerInvariant()}_types";
            string graphQLQueryName = "supportedTypes";
            string gqlQuery = @"{
                supportedTypes(first: 100 orderBy: { " + field + ": ASC } filter: { " + field + ": {" + filterOperator + ": " + gqlValue + @"} }) {
                    items {
                        " + field + @"
                    }
                }
            }";

            string dbQuery = MakeQueryOnTypeTable(new List<string> { field }, filterValue: sqlValue, filterOperator: queryOperator, filterField: field, orderBy: field, limit: "100");

            JsonElement actual = await ExecuteGraphQLRequestAsync(gqlQuery, graphQLQueryName, isAuthenticated: false);
            string expected = await GetDatabaseResultAsync(dbQuery);

            PerformTestEqualsForExtendedTypes(type, expected, actual.GetProperty("items").ToString());
        }

        /// <summary>
        /// Separate test case for DateTime to allow overwrite for postgreSql.
        /// </summary>
        [DataTestMethod]
        [DataRow(DATETIME_TYPE, "gt", "\'1999-01-08\'", "\"1999-01-08\"", " > ")]
        [DataRow(DATETIME_TYPE, "gte", "\'1999-01-08\'", "\"1999-01-08\"", " >= ")]
        [DataRow(DATETIME_TYPE, "lt", "\'1999-01-08\'", "\"1999-01-08\"", " < ")]
        [DataRow(DATETIME_TYPE, "lte", "\'1999-01-08\'", "\"1999-01-08\"", " <= ")]
        [DataRow(DATETIME_TYPE, "neq", "\'1999-01-08\'", "\"1999-01-08\"", "!=")]
        [DataRow(DATETIME_TYPE, "eq", "\'1999-01-08\'", "\"1999-01-08T01:01:01\"", "=")]
        [DataRow(DATETIME_TYPE, "gt", "\'1999-01-08 10:23:00\'", "\"1999-01-08 10:23:00\"", " > ")]
        [DataRow(DATETIME_TYPE, "gte", "\'1999-01-08 10:23:00\'", "\"1999-01-08 10:23:00\"", " >= ")]
        [DataRow(DATETIME_TYPE, "lt", "\'9999-12-31 23:59:59\'", "\"9999-12-31 23:59:59\"", " < ")]
        [DataRow(DATETIME_TYPE, "lte", "\'9999-12-31 23:59:59\'", "\"9999-12-31 23:59:59\"", " <= ")]
        [DataRow(DATETIME_TYPE, "neq", "\'1999-01-08 10:23:00\'", "\"1999-01-08 10:23:00\"", "!=")]
        [DataRow(DATETIME_TYPE, "eq", "\'1999-01-08 10:23:00\'", "\"1999-01-08 10:23:00\"", "=")]
        [DataRow(DATETIME_TYPE, "gt", "\'1999-01-08 10:23:00.9999999\'", "\"1999-01-08 10:23:00.9999999\"", " > ")]
        [DataRow(DATETIME_TYPE, "gte", "\'1999-01-08 10:23:00.9999999\'", "\"1999-01-08 10:23:00.9999999\"", " >= ")]
        [DataRow(DATETIME_TYPE, "lt", "\'9999-12-31 23:59:59.9999999\'", "\"9999-12-31 23:59:59.9999999\"", " < ")]
        [DataRow(DATETIME_TYPE, "lte", "\'9999-12-31 23:59:59.9999999\'", "\"9999-12-31 23:59:59.9999999\"", " <= ")]
        [DataRow(DATETIME_TYPE, "neq", "\'1999-01-08 10:23:00.9999999\'", "\"1999-01-08 10:23:00.9999999\"", "!=")]
        [DataRow(DATETIME_TYPE, "eq", "\'1999-01-08 10:23:00.9999999\'", "\"1999-01-08 10:23:00.9999999\"", "=")]
        [DataRow(DATETIME_TYPE, "neq", "\'1999-01-08 10:23:54.9999999-14:00\'", "\"1999-01-08 10:23:54.9999999-14:00\"", "!=")]
        [DataRow(DATETIME_TYPE, "eq", "\'1999-01-08 10:23:54.9999999-14:00\'", "\"1999-01-08 10:23:54.9999999-14:00\"", "=")]
        [DataRow(DATETIME_TYPE, "gt", "\'1999-01-08 10:22:00\'", "\"1999-01-08 10:22:00\"", " > ")]
        [DataRow(DATETIME_TYPE, "gte", "\'1999-01-08 10:23:54\'", "\"1999-01-08 10:23:54\"", " >= ")]
        [DataRow(DATETIME_TYPE, "lt", "\'2079-06-06\'", "\"2079-06-06\"", " < ")]
        [DataRow(DATETIME_TYPE, "lte", "\'2079-06-06\'", "\"2079-06-06\"", " <= ")]
        [DataRow(DATETIME_TYPE, "neq", "\'1999-01-08 10:23:54\'", "\"1999-01-08 10:23:54\"", "!=")]
        [DataRow(DATETIME_TYPE, "eq", "\'1999-01-08 10:23:54\'", "\"1999-01-08 10:23:54\"", "=")]
        public async Task QueryTypeColumnFilterAndOrderByDateTime(string type, string filterOperator, string sqlValue, string gqlValue, string queryOperator)
        {
            await QueryTypeColumnFilterAndOrderBy(type, filterOperator, sqlValue, gqlValue, queryOperator);
        }

        /// <summary>
        /// Test case for LocalTime filters.
        /// </summary>
        [DataTestMethod]
        [DataRow(TIME_TYPE, "gt", "\'00:00:00.000\'", "\"00:00:00.000\"", " > ")]
        [DataRow(TIME_TYPE, "gte", "\'10:13:14.123\'", "\"10:13:14.123\"", " >= ")]
        [DataRow(TIME_TYPE, "lt", "\'23:59:59.999\'", "\"23:59:59.999\"", " < ")]
        [DataRow(TIME_TYPE, "lte", "\'23:59:59.999\'", "\"23:59:59.999\"", " <= ")]
        [DataRow(TIME_TYPE, "neq", "\'10:23:54.9999999\'", "\"10:23:54.9999999\"", "!=")]
        [DataRow(TIME_TYPE, "eq", "\'10:23:54.9999999\'", "\"10:23:54.9999999\"", "=")]
        public async Task QueryTypeColumnFilterAndOrderByLocalTime(string type, string filterOperator, string sqlValue, string gqlValue, string queryOperator)
        {
            await QueryTypeColumnFilterAndOrderBy(type, filterOperator, sqlValue, gqlValue, queryOperator);
        }

        [DataTestMethod]
        [DataRow(BYTE_TYPE, "255")]
        [DataRow(BYTE_TYPE, "0")]
        [DataRow(BYTE_TYPE, "null")]
        [DataRow(SHORT_TYPE, "0")]
        [DataRow(SHORT_TYPE, "30000")]
        [DataRow(SHORT_TYPE, "-30000")]
        [DataRow(SHORT_TYPE, "null")]
        [DataRow(INT_TYPE, "9999")]
        [DataRow(INT_TYPE, "0")]
        [DataRow(INT_TYPE, "-9999")]
        [DataRow(INT_TYPE, "null")]
        [DataRow(LONG_TYPE, "0")]
        [DataRow(LONG_TYPE, "9000000000000000000")]
        [DataRow(LONG_TYPE, "-9000000000000000000")]
        [DataRow(LONG_TYPE, "null")]
        [DataRow(STRING_TYPE, "\"aaaaaaaaaa\"")]
        [DataRow(STRING_TYPE, "\"\"")]
        [DataRow(STRING_TYPE, "null")]
        [DataRow(SINGLE_TYPE, "-3.33")]
        [DataRow(SINGLE_TYPE, "2E35")]
        [DataRow(SINGLE_TYPE, "null")]
        [DataRow(FLOAT_TYPE, "-3.33")]
        [DataRow(FLOAT_TYPE, "2E150")]
        [DataRow(FLOAT_TYPE, "null")]
        [DataRow(DECIMAL_TYPE, "-3.333333")]
        [DataRow(DECIMAL_TYPE, "1222222.00000929292")]
        [DataRow(DECIMAL_TYPE, "null")]
        [DataRow(BOOLEAN_TYPE, "true")]
        [DataRow(BOOLEAN_TYPE, "false")]
        [DataRow(BOOLEAN_TYPE, "null")]
        [DataRow(DATETIMEOFFSET_TYPE, "\"1999-01-08 10:23:54+8:00\"")]
        [DataRow(DATETIME_TYPE, "\"1999-01-08 09:20:00\"")]
        [DataRow(DATETIME_TYPE, "\"1999-01-08\"")]
        [DataRow(DATETIME_TYPE, "null")]
        [DataRow(BYTEARRAY_TYPE, "\"U3RyaW5neQ==\"")]
        [DataRow(TIME_TYPE, "\"23:59:59.9999999\"")]
        [DataRow(TIME_TYPE, "\"23:59:59\"")]
        [DataRow(TIME_TYPE, "\"23:59:59.9\"")]
        [DataRow(TIME_TYPE, "\"23:59\"")]
        [DataRow(TIME_TYPE, "null")]
        [DataRow(BYTEARRAY_TYPE, "\"V2hhdGNodSBkb2luZyBkZWNvZGluZyBvdXIgdGVzdCBiYXNlNjQgc3RyaW5ncz8=\"")]
        [DataRow(BYTEARRAY_TYPE, "null")]
        public async Task InsertIntoTypeColumn(string type, string value)
        {
            if (!IsSupportedType(type))
            {
                Assert.Inconclusive("Type not supported");
            }

            string field = $"{type.ToLowerInvariant()}_types";
            string graphQLQueryName = "createSupportedType";
            string gqlQuery = "mutation{ createSupportedType (item: {" + field + ": " + value + " }){ " + field + " } }";

            string dbQuery = MakeQueryOnTypeTable(new List<string> { field }, id: 5001);

            JsonElement actual = await ExecuteGraphQLRequestAsync(gqlQuery, graphQLQueryName, isAuthenticated: true);
            string expected = await GetDatabaseResultAsync(dbQuery);

            PerformTestEqualsForExtendedTypes(type, expected, actual.ToString());

            await ResetDbStateAsync();
        }

        [DataTestMethod]
        [DataRow(TIME_TYPE, "\"32:59:59.9999999\"")]
        [DataRow(TIME_TYPE, "\"22:67:59.9999999\"")]
        public async Task InsertInvalidIntoTypeColumn(string type, string value)
        {
            if (!IsSupportedType(type))
            {
                Assert.Inconclusive("Type not supported");
            }

            string field = $"{type.ToLowerInvariant()}_types";
            string graphQLQueryName = "createSupportedType";
            string gqlQuery = "mutation{ createSupportedType (item: {" + field + ": " + value + " }){ " + field + " } }";

            JsonElement response = await ExecuteGraphQLRequestAsync(gqlQuery, graphQLQueryName, isAuthenticated: true);
            string responseMessage = Regex.Unescape(JsonSerializer.Serialize(response));
            Assert.IsTrue(responseMessage.Contains($"{value} cannot be resolved as column \"{field}\" with type \"TimeSpan\"."));
            await ResetDbStateAsync();
        }

        [DataTestMethod]
        [DataRow(BYTE_TYPE, 255)]
        [DataRow(SHORT_TYPE, 30000)]
        [DataRow(INT_TYPE, 9999)]
        [DataRow(LONG_TYPE, 9000000000000000000)]
        [DataRow(STRING_TYPE, "aaaaaaaaaa")]
        [DataRow(FLOAT_TYPE, -3.33)]
        [DataRow(DECIMAL_TYPE, 1222222.00000929292)]
        [DataRow(BOOLEAN_TYPE, true)]
        [DataRow(DATETIMEOFFSET_TYPE, "1999-01-08 10:23:54+8:00")]
        [DataRow(DATETIME_TYPE, "1999-01-08 10:23:54")]
        [DataRow(TIME_TYPE, "\"23:59:59.9999999\"")]
        [DataRow(TIME_TYPE, "null")]
        [DataRow(BYTEARRAY_TYPE, "V2hhdGNodSBkb2luZyBkZWNvZGluZyBvdXIgdGVzdCBiYXNlNjQgc3RyaW5ncz8=")]
        public async Task InsertIntoTypeColumnWithArgument(string type, object value)
        {
            if (!IsSupportedType(type))
            {
                Assert.Inconclusive("Type not supported");
            }

            string field = $"{type.ToLowerInvariant()}_types";
            string graphQLQueryName = "createSupportedType";
            string gqlQuery = "mutation($param: " + TypeNameToGraphQLType(type) + "){ createSupportedType (item: {" + field + ": $param }){ " + field + " } }";

            string dbQuery = MakeQueryOnTypeTable(new List<string> { field }, id: 5001);

            JsonElement actual = await ExecuteGraphQLRequestAsync(gqlQuery, graphQLQueryName, isAuthenticated: true, new() { { "param", value } });
            string expected = await GetDatabaseResultAsync(dbQuery);

            PerformTestEqualsForExtendedTypes(type, expected, actual.ToString());

            await ResetDbStateAsync();
        }

        [DataTestMethod]
        [DataRow(BYTE_TYPE, "255")]
        [DataRow(BYTE_TYPE, "0")]
        [DataRow(BYTE_TYPE, "null")]
        [DataRow(SHORT_TYPE, "0")]
        [DataRow(SHORT_TYPE, "30000")]
        [DataRow(SHORT_TYPE, "-30000")]
        [DataRow(SHORT_TYPE, "null")]
        [DataRow(INT_TYPE, "9999")]
        [DataRow(INT_TYPE, "0")]
        [DataRow(INT_TYPE, "-9999")]
        [DataRow(INT_TYPE, "null")]
        [DataRow(LONG_TYPE, "0")]
        [DataRow(LONG_TYPE, "9000000000000000000")]
        [DataRow(LONG_TYPE, "-9000000000000000000")]
        [DataRow(LONG_TYPE, "null")]
        [DataRow(STRING_TYPE, "\"aaaaaaaaaa\"")]
        [DataRow(STRING_TYPE, "\"\"")]
        [DataRow(STRING_TYPE, "null")]
        [DataRow(SINGLE_TYPE, "-3.33")]
        [DataRow(SINGLE_TYPE, "2E35")]
        [DataRow(SINGLE_TYPE, "null")]
        [DataRow(FLOAT_TYPE, "-3.33")]
        [DataRow(FLOAT_TYPE, "2E150")]
        [DataRow(FLOAT_TYPE, "null")]
        [DataRow(DECIMAL_TYPE, "-3.333333")]
        [DataRow(DECIMAL_TYPE, "1222222.00000929292")]
        [DataRow(DECIMAL_TYPE, "null")]
        [DataRow(BOOLEAN_TYPE, "true")]
        [DataRow(BOOLEAN_TYPE, "false")]
        [DataRow(BOOLEAN_TYPE, "null")]
        [DataRow(DATETIMEOFFSET_TYPE, "\"1999-01-08 10:23:54+8:00\"")]
        [DataRow(DATETIME_TYPE, "\"1999-01-08 09:20:00\"")]
        [DataRow(DATETIME_TYPE, "\"1999-01-08\"")]
        [DataRow(DATETIME_TYPE, "null")]
        [DataRow(TIME_TYPE, "\"23:59:59.9999999\"")]
        [DataRow(TIME_TYPE, "null")]
        [DataRow(BYTEARRAY_TYPE, "\"U3RyaW5neQ==\"")]
        [DataRow(BYTEARRAY_TYPE, "\"V2hhdGNodSBkb2luZyBkZWNvZGluZyBvdXIgdGVzdCBiYXNlNjQgc3RyaW5ncz8=\"")]
        [DataRow(BYTEARRAY_TYPE, "null")]
        [DataRow(GUID_TYPE, "\"3a1483a5-9ac2-4998-bcf3-78a28078c6ac\"")]
        [DataRow(GUID_TYPE, "null")]
        public async Task UpdateTypeColumn(string type, string value)
        {
            if (!IsSupportedType(type))
            {
                Assert.Inconclusive("Type not supported");
            }

            string field = $"{type.ToLowerInvariant()}_types";
            string graphQLQueryName = "updateSupportedType";
            string gqlQuery = "mutation{ updateSupportedType (typeid: 1, item: {" + field + ": " + value + " }){ " + field + " } }";

            string dbQuery = MakeQueryOnTypeTable(new List<string> { field }, id: 1);

            JsonElement actual = await ExecuteGraphQLRequestAsync(gqlQuery, graphQLQueryName, isAuthenticated: true);
            string expected = await GetDatabaseResultAsync(dbQuery);

            PerformTestEqualsForExtendedTypes(type, expected, actual.ToString());

            await ResetDbStateAsync();
        }

        [DataTestMethod]
        [DataRow(BYTE_TYPE, 255)]
        [DataRow(SHORT_TYPE, 30000)]
        [DataRow(INT_TYPE, 9999)]
        [DataRow(LONG_TYPE, 9000000000000000000)]
        [DataRow(STRING_TYPE, "aaaaaaaaaa")]
        [DataRow(FLOAT_TYPE, -3.33)]
        [DataRow(DECIMAL_TYPE, 1222222.00000929292)]
        [DataRow(BOOLEAN_TYPE, true)]
        [DataRow(DATETIME_TYPE, "1999-01-08 10:23:54")]
        [DataRow(DATETIMEOFFSET_TYPE, "1999-01-08 10:23:54+8:00")]
        [DataRow(BYTEARRAY_TYPE, "V2hhdGNodSBkb2luZyBkZWNvZGluZyBvdXIgdGVzdCBiYXNlNjQgc3RyaW5ncz8=")]
        [DataRow(GUID_TYPE, "3a1483a5-9ac2-4998-bcf3-78a28078c6ac")]
        [DataRow(GUID_TYPE, null)]
        public async Task UpdateTypeColumnWithArgument(string type, object value)
        {
            if (!IsSupportedType(type))
            {
                Assert.Inconclusive("Type not supported");
            }

            string field = $"{type.ToLowerInvariant()}_types";
            string graphQLQueryName = "updateSupportedType";
            string gqlQuery = "mutation($param: " + TypeNameToGraphQLType(type) + "){ updateSupportedType (typeid: 1, item: {" + field + ": $param }){ " + field + " } }";

            string dbQuery = MakeQueryOnTypeTable(new List<string> { field }, id: 1);

            JsonElement actual = await ExecuteGraphQLRequestAsync(gqlQuery, graphQLQueryName, isAuthenticated: true, new() { { "param", value } });
            string expected = await GetDatabaseResultAsync(dbQuery);

            PerformTestEqualsForExtendedTypes(type, expected, actual.ToString());

            await ResetDbStateAsync();
        }

        #endregion

        /// <summary>
        /// Utility function to do special comparisons for some of the extended types
        /// if json compare doesn't suffice
        /// </summary>
        private static void PerformTestEqualsForExtendedTypes(string type, string expected, string actual)
        {
            if (type == SINGLE_TYPE || type == FLOAT_TYPE || type == DECIMAL_TYPE)
            {
                CompareFloatResults(type, actual.ToString(), expected);
            }
            else if (type == DATETIME_TYPE)
            {
                CompareDateTimeResults(actual.ToString(), expected);
            }
            else if (type == DATETIMEOFFSET_TYPE)
            {
                CompareDateTimeOffsetResults(actual.ToString(), expected);
            }
            else if (type == TIME_TYPE)
            {
                CompareTimeResults(actual.ToString(), expected);
            }
            else
            {
                SqlTestHelper.PerformTestEqualJsonStrings(expected, actual.ToString());
            }
        }

        /// <summary>
        /// HotChocolate will parse large floats to exponential notation
        /// while the db will return the number fully printed out. Because
        /// the json deep compare function we are using does not account for such scenario
        /// a special comparison is needed to test floats
        /// </summary>
        private static void CompareFloatResults(string floatType, string actual, string expected)
        {
            string fieldName = $"{floatType.ToLowerInvariant()}_types";

            using JsonDocument actualJsonDoc = JsonDocument.Parse(actual);
            using JsonDocument expectedJsonDoc = JsonDocument.Parse(expected);

            if (actualJsonDoc.RootElement.ValueKind is JsonValueKind.Array)
            {
                ValidateArrayResults(actualJsonDoc, expectedJsonDoc, fieldName);
                return;
            }

            string actualFloat;
            string expectedFloat;
            actualFloat = actualJsonDoc.RootElement.GetProperty(fieldName).ToString();
            expectedFloat = expectedJsonDoc.RootElement.GetProperty(fieldName).ToString();

            // handles cases when one of the values is null
            if (string.IsNullOrEmpty(actualFloat) || string.IsNullOrEmpty(expectedFloat))
            {
                Assert.AreEqual(expectedFloat, actualFloat);
                return;
            }

            switch (floatType)
            {
                case SINGLE_TYPE:
                    Assert.AreEqual(float.Parse(expectedFloat), float.Parse(actualFloat));
                    break;
                case FLOAT_TYPE:
                    Assert.AreEqual(double.Parse(expectedFloat), double.Parse(actualFloat));
                    break;
                case DECIMAL_TYPE:
                    Assert.AreEqual(decimal.Parse(expectedFloat), decimal.Parse(actualFloat));
                    break;
                default:
                    Assert.Fail($"Calling compare on unrecognized float type {floatType}");
                    break;
            }
        }

        /// <summary>
        /// Required due to different format between mysql datetime and HotChocolate datetime
        /// result
        /// </summary>
        private static void CompareDateTimeResults(string actual, string expected)
        {
            string fieldName = "datetime_types";

            using JsonDocument actualJsonDoc = JsonDocument.Parse(actual);
            using JsonDocument expectedJsonDoc = JsonDocument.Parse(expected);

            if (actualJsonDoc.RootElement.ValueKind is JsonValueKind.Array)
            {
                ValidateArrayResults(actualJsonDoc, expectedJsonDoc, fieldName);
                return;
            }

            string actualDateTime = actualJsonDoc.RootElement.GetProperty(fieldName).ToString();
            string expectedDateTime = expectedJsonDoc.RootElement.GetProperty(fieldName).ToString();

            // handles cases when one of the values is null
            if (string.IsNullOrEmpty(actualDateTime) || string.IsNullOrEmpty(expectedDateTime))
            {
                Assert.AreEqual(expectedDateTime, actualDateTime);
            }
            else
            {
                // Adjusting to universal, since DateTime doesn't account for TimeZone
                DateTime expectedDateTimeUniversal = DateTime.Parse(expectedDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                DateTime actualDateTimeUniversal = DateTime.Parse(actualDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                Assert.AreEqual(expectedDateTimeUniversal, actualDateTimeUniversal);
            }
        }

        /// <summary>
        /// Required due to different format between mysql datetimeoffset and HotChocolate datetime
        /// result
        /// </summary>
        private static void CompareDateTimeOffsetResults(string actual, string expected)
        {
            string fieldName = "datetimeoffset_types";

            using JsonDocument actualJsonDoc = JsonDocument.Parse(actual);
            using JsonDocument expectedJsonDoc = JsonDocument.Parse(expected);

            string actualDateTimeOffset = actualJsonDoc.RootElement.GetProperty(fieldName).ToString();
            string expectedDateTimeOffset = expectedJsonDoc.RootElement.GetProperty(fieldName).ToString();

            // handles cases when one of the values is null
            if (string.IsNullOrEmpty(actualDateTimeOffset) || string.IsNullOrEmpty(expectedDateTimeOffset))
            {
                Assert.AreEqual(expectedDateTimeOffset, actualDateTimeOffset);
            }
            else
            {
                Assert.AreEqual(DateTimeOffset.Parse(expectedDateTimeOffset), DateTimeOffset.Parse(actualDateTimeOffset));
            }
        }

        /// <summary>
        /// Required due to different format between SQL time and HotChocolate TimeSpan time(ISO-8601) result.
        /// </summary>
        private static void CompareTimeResults(string actual, string expected)
        {
            string fieldName = "time_types";

            using JsonDocument actualJsonDoc = JsonDocument.Parse(actual);
            using JsonDocument expectedJsonDoc = JsonDocument.Parse(expected);

            if (actualJsonDoc.RootElement.ValueKind is JsonValueKind.Array)
            {
                ValidateArrayResults(actualJsonDoc, expectedJsonDoc, fieldName);
                return;
            }

            string actualTimeString = actualJsonDoc.RootElement.GetProperty(fieldName).ToString();
            string expectedTimeString = expectedJsonDoc.RootElement.GetProperty(fieldName).ToString();

            // handles cases when one of the values is null
            if (string.IsNullOrEmpty(actualTimeString) || string.IsNullOrEmpty(expectedTimeString))
            {
                Assert.AreEqual(expectedTimeString, actualTimeString);
            }
            else
            {
                TimeOnly actualTime = TimeOnly.Parse(actualTimeString, CultureInfo.InvariantCulture);
                TimeOnly expectedTime = TimeOnly.Parse(expectedTimeString, CultureInfo.InvariantCulture);
                Assert.AreEqual(expectedTime.ToLongTimeString(), actualTime.ToLongTimeString());
            }
        }

        private static void ValidateArrayResults(JsonDocument actualJsonDoc, JsonDocument expectedJsonDoc, string fieldName)
        {
            JsonElement.ArrayEnumerator actualEnumerater = actualJsonDoc.RootElement.EnumerateArray();
            foreach (JsonElement expectedElement in expectedJsonDoc.RootElement.EnumerateArray())
            {
                actualEnumerater.MoveNext();
                JsonElement actualElement = actualEnumerater.Current;
                actualElement.TryGetProperty(fieldName, out JsonElement actualValue);
                expectedElement.TryGetProperty(fieldName, out JsonElement expectedValue);

                if (fieldName.StartsWith(DATETIME_TYPE.ToLower()))
                {
                    // MySql returns a format that will not directly parse into DateTime type so we use string here for parsing
                    DateTime actualDateTime = DateTime.Parse(actualValue.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                    DateTime expectedDateTime = DateTime.Parse(expectedValue.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                    Assert.AreEqual(expectedDateTime, actualDateTime);
                }
                else if (fieldName.StartsWith(SINGLE_TYPE.ToLower()))
                {
                    Assert.AreEqual(expectedValue.GetSingle(), actualValue.GetSingle());
                }
                else if (fieldName.StartsWith(TIME_TYPE.ToLower()))
                {
                    TimeOnly actualTime = TimeOnly.Parse(actualValue.ToString(), CultureInfo.InvariantCulture);
                    TimeOnly expectedTime = TimeOnly.Parse(expectedValue.ToString(), CultureInfo.InvariantCulture);
                    Assert.AreEqual(expectedTime.ToLongTimeString(), actualTime.ToLongTimeString());
                }
                else
                {
                    Assert.AreEqual(expectedValue.GetDouble(), actualValue.GetDouble());
                }
            }
        }

        /// <summary>
        /// Needed to map the type name to a graphql type in argument tests
        /// where the argument type need to be specified.
        /// </summary>
        private static string TypeNameToGraphQLType(string typeName)
        {
            if (typeName is GUID_TYPE)
            {
                return STRING_TYPE;
            }
            else if (typeName is DATETIMEOFFSET_TYPE)
            {
                return DATETIME_TYPE;
            }

            return typeName;
        }

        protected abstract string MakeQueryOnTypeTable(
            List<string> queriedColumns,
            string filterValue = "1",
            string filterOperator = "=",
            string filterField = "1",
            string orderBy = "id",
            string limit = "1");

        protected abstract string MakeQueryOnTypeTable(List<string> columnsToQuery, int id);
        protected virtual bool IsSupportedType(string type)
        {
            return true;
        }
    }
}
