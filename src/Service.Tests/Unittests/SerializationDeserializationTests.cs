// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text.Json;
using Azure.DataApiBuilder.Config.DatabasePrimitives;
using Azure.DataApiBuilder.Config.ObjectModel;
using Azure.DataApiBuilder.Core.Services.MetadataProviders.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.DataApiBuilder.Service.Tests.Unittests
{
    [TestClass]
    [TestCategory("Serialization and Deserialization using SqlMetadataProvider converters")]
    public class SerializationDeserializationTests
    {
        private DatabaseTable _databaseTable;
        private DatabaseView _databaseView;
        private DatabaseStoredProcedure _databaseStoredProcedure;
        private ColumnDefinition _columnDefinition;
        private ParameterDefinition _parameterDefinition;
        private SourceDefinition _sourceDefinition;
        private JsonSerializerOptions _options;

        /// <summary>
        /// Validates serialization and deserilization of DatabaseTable object
        /// This test first creates a DatabaseTable object, serializes and deserializes it using our converters
        /// and verifies that the deserialized object is same as original.
        /// For DatabaseTable we are checking properties : SchemaName, Name, FullName, SourceType, SourceDefinition and TableDefinition
        /// This test also tests number of properties currently on DatabaseTable object, this is for future scenario, if there is a new
        /// property added this test will catch it and also will expose developer to the serialization and deserialization logic
        /// </summary>
        [TestMethod]
        public void TestDatabaseTableSerializationDeserialization()
        {
            InitializeObjects();

            // Test to catch if there is change in number of properties/fields
            // Note: On Addition of property make sure it is added in following object creation _databaseTable and include in serialization
            // and deserialization test.
            int fields = typeof(DatabaseTable).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length;
            Assert.AreEqual(fields, 6);

            string serializedDatabaseTable = JsonSerializer.Serialize(_databaseTable, _options);
            DatabaseTable deserializedDatabaseTable = JsonSerializer.Deserialize<DatabaseTable>(serializedDatabaseTable, _options)!;

            Assert.AreEqual(deserializedDatabaseTable.SourceType, _databaseTable.SourceType);
            Assert.AreEqual(deserializedDatabaseTable.FullName, _databaseTable.FullName);
            deserializedDatabaseTable.Equals(_databaseTable);
            VerifySourceDefinitionSerializationDeserialization(deserializedDatabaseTable.SourceDefinition, _databaseTable.SourceDefinition, "FirstName");
            VerifySourceDefinitionSerializationDeserialization(deserializedDatabaseTable.TableDefinition, _databaseTable.TableDefinition, "FirstName");
        }

        /// <summary>
        /// Validates serialization and deserilization of DatabaseView object
        /// This test first creates a DatabaseTable object, serializes and deserializes it using our converters
        /// and verifies that the deserialized object is same as original.
        /// For DatabaseView we are checking properties : SchemaName, Name, FullName, SourceType, SourceDefinition and ViewDefinition
        /// This test also tests number of properties currently on DatabaseTable object, this is for future scenario, if there is a new
        /// property added this test will catch it and also will expose developer to the serialization and deserialization logic
        /// </summary>
        [TestMethod]
        public void TestDatabaseViewSerializationDeserialization()
        {
            InitializeObjects();

            // Test to catch if there is change in number of properties/fields
            // Note: On Addition of property make sure it is added in following object creation _databaseView and include in serialization
            // and deserialization test.
            int fields = typeof(DatabaseView).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length;
            Assert.AreEqual(fields, 6);

            string serializedDatabaseView = JsonSerializer.Serialize(_databaseView, _options);
            DatabaseView deserializedDatabaseView = JsonSerializer.Deserialize<DatabaseView>(serializedDatabaseView, _options)!;

            Assert.AreEqual(deserializedDatabaseView.SourceType, _databaseView.SourceType);
            deserializedDatabaseView.Equals(_databaseView);
            VerifySourceDefinitionSerializationDeserialization(deserializedDatabaseView.SourceDefinition, _databaseView.SourceDefinition, "FirstName");
            VerifySourceDefinitionSerializationDeserialization(deserializedDatabaseView.ViewDefinition, _databaseView.ViewDefinition, "FirstName");
        }

        /// <summary>
        /// Validates serialization and deserilization of DatabaseStoredProcedure object
        /// This test first creates a DatabaseTable object, serializes and deserializes it using our converters
        /// and verifies that the deserialized object is same as original.
        /// For DatabaseStoredProcedure we are checking properties : SchemaName, Name, FullName, SourceType, SourceDefinition and StoredProcedureDefinition
        /// This test also tests number of properties currently on DatabaseTable object, this is for future scenario, if there is a new
        /// property added this test will catch it and also will expose developer to the serialization and deserialization logic
        /// </summary>
        [TestMethod]
        public void TestDatabaseStoredProcedureSerializationDeserialization()
        {
            InitializeObjects();

            // Test to catch if there is change in number of properties/fields
            // Note: On Addition of property make sure it is added in following object creation _databaseStoredProcedure and include in serialization
            // and deserialization test.
            int fields = typeof(DatabaseStoredProcedure).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length;
            Assert.AreEqual(fields, 6);

            string serializedDatabaseSP = JsonSerializer.Serialize(_databaseStoredProcedure, _options);
            DatabaseStoredProcedure deserializedDatabaseSP = JsonSerializer.Deserialize<DatabaseStoredProcedure>(serializedDatabaseSP, _options)!;

            Assert.AreEqual(deserializedDatabaseSP.SourceType, _databaseStoredProcedure.SourceType);
            deserializedDatabaseSP.Equals(_databaseStoredProcedure);
            VerifySourceDefinitionSerializationDeserialization(deserializedDatabaseSP.SourceDefinition, _databaseStoredProcedure.SourceDefinition, "FirstName", true);
            VerifySourceDefinitionSerializationDeserialization(deserializedDatabaseSP.StoredProcedureDefinition, _databaseStoredProcedure.StoredProcedureDefinition, "FirstName", true);
        }

        /// <summary>
        /// Validates serialization and deserilization of RelationShipPair object
        /// This test first creates a RelationshipPair object, serializes and deserializes it using our converters
        /// and verifies that the deserialized object is same as original.
        /// </summary>
        [TestMethod]
        public void TestRelationShipPairSerializationDeserialization()
        {
            InitializeObjects();

            RelationShipPair pair = GetRelationShipPair();
            string serializedRelationShipPair = JsonSerializer.Serialize(pair, _options);
            RelationShipPair deserializedRelationShipPair = JsonSerializer.Deserialize<RelationShipPair>(serializedRelationShipPair, _options);

            VerifyRelationShipPair(pair, deserializedRelationShipPair);
        }

        /// <summary>
        /// Validates serialization and deserilization of ForeignKeyDefinition object
        /// </summary>
        [TestMethod]
        public void TestForeginKeyDefinitionSerializationDeserialization()
        {
            InitializeObjects();

            RelationShipPair pair = GetRelationShipPair();

            ForeignKeyDefinition foreignKeyDefinition = new()
            {
                Pair = pair,
                ReferencedColumns = new List<string> { "Index" },
                ReferencingColumns = new List<string> { "FirstName" }

            };

            string serializedForeignKeyDefinition = JsonSerializer.Serialize(foreignKeyDefinition, _options);
            ForeignKeyDefinition deserializedForeignKeyDefinition = JsonSerializer.Deserialize<ForeignKeyDefinition>(serializedForeignKeyDefinition, _options);

            int fields = typeof(ForeignKeyDefinition).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length;
            Assert.AreEqual(fields, 3);

            Assert.IsTrue(foreignKeyDefinition.Equals(deserializedForeignKeyDefinition));
            VerifyRelationShipPair(pair, deserializedForeignKeyDefinition.Pair);
        }

        [TestMethod]
        public void TestColumnDefinitionNegativeCases()
        {
            InitializeObjects();

            ColumnDefinition col2 = GetColumnDefinition(typeof(string), null, true, false, false, new string("John"), false);
            // as DbType of one is null and other is not it will fail - testing DbType?.Equals(other.DbType) == true to return false
            Assert.IsFalse(col2.Equals(_columnDefinition));

            ColumnDefinition col3 = GetColumnDefinition(typeof(string), null, false, false, false, null, false);
            // as DefaultValue of one is null and other is not it will fail - testing DefaultValue?.Equals(other.DefaultValue) == true to return false
            Assert.IsFalse(col3.Equals(_columnDefinition));

            _options = new JsonSerializerOptions()
            {
                Converters = { new ObjectConverter() }
            };

            // test to check if TypeConverter is not passed then we cannot serialize System.Type
            Assert.ThrowsException<NotSupportedException>(() =>
            {
                JsonSerializer.Serialize(_columnDefinition, _options);
            });

            // test to check the need for ObjectConverter to handle object DefaultValue of type object
            _options = new JsonSerializerOptions()
            {
                Converters = { new TypeConverter() }
            };
            string serializeColumnDefinition = JsonSerializer.Serialize(_columnDefinition, _options);
            ColumnDefinition col = JsonSerializer.Deserialize<ColumnDefinition>(serializeColumnDefinition, _options);
            Assert.IsFalse(_columnDefinition.Equals(col));
        }

        private void InitializeObjects()
        {
            _options = new()
            {
                Converters = {
                    new DatabaseObjectConverter(),
                    new TypeConverter(),
                    new ObjectConverter(),
                }
            };

            _columnDefinition = GetColumnDefinition(typeof(string), DbType.String, true, false, false, new string("John"), false);
            _sourceDefinition = GetSourceDefinition(false, false, new List<string>() { "FirstName" }, _columnDefinition);

            _databaseTable = new DatabaseTable()
            {
                Name = "customers",
                SourceType = EntitySourceType.Table,
                SchemaName = "model",
                TableDefinition = _sourceDefinition,
            };

            _databaseView = new DatabaseView()
            {
                Name = "customers",
                SourceType = EntitySourceType.View,
                SchemaName = "model",
                ViewDefinition = new()
                {
                    IsInsertDMLTriggerEnabled = false,
                    IsUpdateDMLTriggerEnabled = false,
                    PrimaryKey = new List<string>() { "FirstName" },
                },
            };
            _databaseView.ViewDefinition.Columns.Add("FirstName", _columnDefinition);

            _parameterDefinition = new()
            {
                SystemType = typeof(int),
                DbType = DbType.Int32,
                HasConfigDefault = true,
                ConfigDefaultValue = 1,
            };

            _databaseStoredProcedure = new DatabaseStoredProcedure()
            {
                SchemaName = "dbo",
                Name = "GetPersonById",
                SourceType = EntitySourceType.StoredProcedure,
                StoredProcedureDefinition = new()
                {
                    PrimaryKey = new List<string>() { "FirstName" },
                }
            };
            _databaseStoredProcedure.StoredProcedureDefinition.Columns.Add("FirstName", _columnDefinition);
            _databaseStoredProcedure.StoredProcedureDefinition.Parameters.Add("Id", _parameterDefinition);
        }

        private static void VerifySourceDefinitionSerializationDeserialization(SourceDefinition expectedSourceDefinition, SourceDefinition deserializedSourceDefinition, string columnValue, bool isStoredProcedure = false)
        {
            // test number of properties/fields defined in Source Definition
            int fields = typeof(SourceDefinition).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length;
            Assert.AreEqual(fields, 5);

            // test values
            Assert.AreEqual(expectedSourceDefinition.IsInsertDMLTriggerEnabled, deserializedSourceDefinition.IsInsertDMLTriggerEnabled);
            Assert.AreEqual(expectedSourceDefinition.IsUpdateDMLTriggerEnabled, deserializedSourceDefinition.IsUpdateDMLTriggerEnabled);
            Assert.AreEqual(expectedSourceDefinition.PrimaryKey.Count, deserializedSourceDefinition.PrimaryKey.Count);
            Assert.AreEqual(expectedSourceDefinition.Columns.Count, deserializedSourceDefinition.Columns.Count);
            VerifyColumnDefinitionSerializationDeserialization(expectedSourceDefinition.Columns.GetValueOrDefault(columnValue), deserializedSourceDefinition.Columns.GetValueOrDefault(columnValue));

            if (isStoredProcedure)
            {
                VerifyParameterDefinitionSerializationDeserialization(((StoredProcedureDefinition)expectedSourceDefinition).Parameters.GetValueOrDefault("Id"),
                    ((StoredProcedureDefinition)deserializedSourceDefinition).Parameters.GetValueOrDefault("Id"));
            }
        }

        private static void VerifyColumnDefinitionSerializationDeserialization(ColumnDefinition expectedColumnDefinition, ColumnDefinition deserializedColumnDefinition)
        {
            // test number of properties/fields defined in Column Definition
            int fields = typeof(ColumnDefinition).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length;
            Assert.AreEqual(fields, 7);
            // test values
            expectedColumnDefinition.Equals(deserializedColumnDefinition);
        }

        private static void VerifyParameterDefinitionSerializationDeserialization(ParameterDefinition expectedParameterDefinition, ParameterDefinition deserializedParameterDefinition)
        {
            // test number of properties/fields defined in Column Definition
            int fields = typeof(ParameterDefinition).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length;
            Assert.AreEqual(fields, 4);
            // test values
            expectedParameterDefinition.Equals(deserializedParameterDefinition);
        }

        private static void VerifyRelationShipPair(RelationShipPair expectedRelationShipPair, RelationShipPair deserializedRelationShipPair)
        {
            int fields = typeof(RelationShipPair).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length;
            Assert.AreEqual(fields, 2);

            Assert.IsTrue(expectedRelationShipPair.Equals(deserializedRelationShipPair));

            // test referencingtable sourcedefinition and tabledefinition
            VerifySourceDefinitionSerializationDeserialization(deserializedRelationShipPair.ReferencingDbTable.SourceDefinition, expectedRelationShipPair.ReferencingDbTable.SourceDefinition, "FirstName");
            VerifySourceDefinitionSerializationDeserialization(deserializedRelationShipPair.ReferencingDbTable.TableDefinition, expectedRelationShipPair.ReferencingDbTable.TableDefinition, "FirstName");

            // test referenced table sourcedefinition and tabledefinition
            VerifySourceDefinitionSerializationDeserialization(deserializedRelationShipPair.ReferencedDbTable.SourceDefinition, expectedRelationShipPair.ReferencedDbTable.SourceDefinition, "Index");
            VerifySourceDefinitionSerializationDeserialization(deserializedRelationShipPair.ReferencedDbTable.TableDefinition, expectedRelationShipPair.ReferencedDbTable.TableDefinition, "Index");
        }

        private static ColumnDefinition GetColumnDefinition(Type SystemType, DbType? DbType, bool HasDefault, bool IsAutoGenerated, bool IsReadOnly, object DefaultVault, bool IsNullable)
        {
            return new()
            {
                SystemType = SystemType,
                DbType = DbType,
                HasDefault = HasDefault,
                IsAutoGenerated = IsAutoGenerated,
                IsReadOnly = IsReadOnly,
                DefaultValue = DefaultVault,
                IsNullable = IsNullable,
            };
        }

        private static SourceDefinition GetSourceDefinition(bool IsInsertDMLTriggerEnabled, bool IsUpdateDMLTriggerEnabled, List<string> PrimaryKeys, ColumnDefinition columnDefinition)
        {
            SourceDefinition sourceDefinition = new()
            {
                IsInsertDMLTriggerEnabled = IsInsertDMLTriggerEnabled,
                IsUpdateDMLTriggerEnabled = IsUpdateDMLTriggerEnabled,
                PrimaryKey = PrimaryKeys,
            };
            sourceDefinition.Columns.Add(PrimaryKeys[0], columnDefinition);

            return sourceDefinition;
        }

        private RelationShipPair GetRelationShipPair()
        {
            ColumnDefinition col2 = GetColumnDefinition(typeof(int), DbType.Int32, true, false, false, 10, false);
            SourceDefinition source2 = GetSourceDefinition(false, false, new List<string>() { "Index" }, col2);
            DatabaseTable table2 = new()
            {
                Name = "customers",
                SourceType = EntitySourceType.Table,
                SchemaName = "model",
                TableDefinition = source2,
            };
            return new(_databaseTable, table2);
        }
    }
}
