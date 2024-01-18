// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using Azure.DataApiBuilder.Config;
using Azure.DataApiBuilder.Config.Converters;
using Azure.DataApiBuilder.Config.NamingPolicies;
using Azure.DataApiBuilder.Config.ObjectModel;
using Azure.DataApiBuilder.Service.Exceptions;
using static Azure.DataApiBuilder.Core.Configurations.IRuntimeConfigProvider;

namespace Azure.DataApiBuilder.Core.Configurations;

/// <summary>
/// This class is responsible for exposing the runtime config to the rest of the service when in a hosted scenario.
/// The <c>HostedRuntimeConfigProvider</c> won't directly load the config, but will instead rely on the <see cref="FileSystemRuntimeConfigLoader"/> to do so.
/// </summary>
/// <remarks>
/// The <c>HostedRuntimeConfigProvider</c> will maintain internal state of the config, and will only load it once.
///
/// This class should be treated as the owner of the config that is available within the service, and other classes
/// should not load the config directly, or maintain a reference to it, so that we can do hot-reloading by replacing
/// the config that is available from this type.
/// </remarks>
public class HostedRuntimeConfigProvider : IRuntimeConfigProvider
{
    public List<RuntimeConfigLoadedHandler> RuntimeConfigLoadedHandlers { get; } = new List<RuntimeConfigLoadedHandler>();

    public bool IsLateConfigured { get; set; }

    public Dictionary<string, string?> ManagedIdentityAccessToken { get; set; } = new Dictionary<string, string?>();

    public RuntimeConfigLoader ConfigLoader { get; set; }

    public RuntimeConfig? _runtimeConfig;

    public HostedRuntimeConfigProvider(RuntimeConfigLoader runtimeConfigLoader)
    {
        ConfigLoader = runtimeConfigLoader;
    }

    /// <summary>
    /// Return the previous loaded config, or it will attempt to load the config that
    /// is known by the loader.
    /// </summary>
    /// <returns>The RuntimeConfig instance.</returns>
    /// <remark>Dont use this method if environment variable references need to be retained.</remark>
    /// <exception cref="DataApiBuilderException">Thrown when the loader is unable to load an instance of the config from its known location.</exception>
    public RuntimeConfig GetConfig()
    {
        if (_runtimeConfig is not null)
        {
            return _runtimeConfig;
        }
        else
        {
            throw new DataApiBuilderException(
                message: "Runtime config isn't setup.",
                statusCode: HttpStatusCode.ServiceUnavailable,
                subStatusCode: DataApiBuilderException.SubStatusCodes.ErrorInInitialization);
        }
    }

    public bool TryGetConfig([NotNullWhen(true)] out RuntimeConfig? runtimeConfig)
    {
        throw new NotImplementedException();
    }

    public bool TryGetLoadedConfig([NotNullWhen(true)] out RuntimeConfig? runtimeConfig)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Initialize the runtime configuration provider with the specified configurations.
    /// This initialization method is used when the configuration is sent to the ConfigurationController
    /// in the form of a string instead of reading the configuration from a configuration file.
    /// This method assumes the connection string is provided as part of the configuration.
    /// Initialize the first database within the datasource list.
    /// </summary>
    /// <param name="configuration">The engine configuration.</param>
    /// <param name="schema">The GraphQL Schema. Can be left null for SQL configurations.</param>
    /// <param name="accessToken">The string representation of a managed identity access token</param>
    /// <returns>true if the initialization succeeded, false otherwise.</returns>
    public async Task<bool> Initialize(
        string configuration,
        string? schema,
        string? accessToken)
    {
        if (string.IsNullOrEmpty(configuration))
        {
            throw new ArgumentException($"'{nameof(configuration)}' cannot be null or empty.", nameof(configuration));
        }

        if (RuntimeConfigLoader.TryParseConfig(
                configuration,
                out RuntimeConfig? runtimeConfig,
                replaceEnvVar: false,
                replacementFailureMode: EnvironmentVariableReplacementFailureMode.Ignore))
        {
            _runtimeConfig = runtimeConfig;

            if (string.IsNullOrEmpty(runtimeConfig.DataSource.ConnectionString))
            {
                throw new ArgumentException($"'{nameof(runtimeConfig.DataSource.ConnectionString)}' cannot be null or empty.", nameof(runtimeConfig.DataSource.ConnectionString));
            }

            if (_runtimeConfig.DataSource.DatabaseType == DatabaseType.CosmosDB_NoSQL)
            {
                _runtimeConfig = HandleCosmosNoSqlConfiguration(schema, _runtimeConfig, _runtimeConfig.DataSource.ConnectionString);
            }

            ManagedIdentityAccessToken[_runtimeConfig.GetDefaultDataSourceName()] = accessToken;
        }

        bool configLoadSucceeded = await InvokeConfigLoadedHandlersAsync();

        IsLateConfigured = true;

        return configLoadSucceeded;
    }

    /// <summary>
    /// Set the runtime configuration provider with the specified accessToken for the specified datasource.
    /// This initialization method is used to set the access token for the current runtimeConfig.
    /// As opposed to using a json input and regenerating the runtimconfig, it sets the access token for the current runtimeConfig on the provider.
    /// </summary>
    /// <param name="accessToken">The string representation of a managed identity access token</param>
    /// <param name="dataSourceName">Name of the datasource for which to assign the token.</param>
    /// <returns>true if the initialization succeeded, false otherwise.</returns>
    public bool TrySetAccesstoken(
        string? accessToken,
        string dataSourceName)
    {
        if (_runtimeConfig is null)
        {
            // if runtimeConfig is not set up, throw as cannot initialize.
            throw new DataApiBuilderException($"{nameof(RuntimeConfig)} has not been loaded.", HttpStatusCode.BadRequest, DataApiBuilderException.SubStatusCodes.ErrorInInitialization);
        }

        // Validate that the datasource exists in the runtimeConfig and then add or update access token.
        if (_runtimeConfig.CheckDataSourceExists(dataSourceName))
        {
            ManagedIdentityAccessToken[dataSourceName] = accessToken;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Initialize the runtime configuration provider with the specified configurations.
    /// This initialization method is used when the configuration is sent to the ConfigurationController
    /// in the form of a string instead of reading the configuration from a configuration file.
    /// </summary>
    /// <param name="configuration">The engine configuration.</param>
    /// <param name="schema">The GraphQL Schema. Can be left null for SQL configurations.</param>
    /// <param name="connectionString">The connection string to the database.</param>
    /// <param name="accessToken">The string representation of a managed identity access token</param>
    /// <returns>true if the initialization succeeded, false otherwise.</returns>
    public async Task<bool> Initialize(
        string jsonConfig,
        string? graphQLSchema,
        string connectionString,
        string? accessToken,
        bool replaceEnvVar = true,
        EnvironmentVariableReplacementFailureMode replacementFailureMode = EnvironmentVariableReplacementFailureMode.Throw)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or empty.", nameof(connectionString));
        }

        if (string.IsNullOrEmpty(jsonConfig))
        {
            throw new ArgumentException($"'{nameof(jsonConfig)}' cannot be null or empty.", nameof(jsonConfig));
        }

        IsLateConfigured = true;

        if (RuntimeConfigLoader.TryParseConfig(jsonConfig, out RuntimeConfig? runtimeConfig, replaceEnvVar: replaceEnvVar, replacementFailureMode: replacementFailureMode))
        {
            _runtimeConfig = runtimeConfig.DataSource.DatabaseType switch
            {
                DatabaseType.CosmosDB_NoSQL => HandleCosmosNoSqlConfiguration(graphQLSchema, runtimeConfig, connectionString),
                _ => runtimeConfig with { DataSource = runtimeConfig.DataSource with { ConnectionString = connectionString } }
            };
            ManagedIdentityAccessToken[_runtimeConfig.GetDefaultDataSourceName()] = accessToken;
            _runtimeConfig.UpdateDataSourceNameToDataSource(_runtimeConfig.GetDefaultDataSourceName(), _runtimeConfig.DataSource);

            return await InvokeConfigLoadedHandlersAsync();
        }

        return false;
    }

    public async Task<bool> InvokeConfigLoadedHandlersAsync()
    {
        List<Task<bool>> configLoadedTasks = new();
        if (_runtimeConfig is not null)
        {
            foreach (RuntimeConfigLoadedHandler configLoadedHandler in RuntimeConfigLoadedHandlers)
            {
                configLoadedTasks.Add(configLoadedHandler(this, _runtimeConfig));
            }
        }

        bool[] results = await Task.WhenAll(configLoadedTasks);

        // Verify that all tasks succeeded.
        return results.All(x => x);
    }

    private static RuntimeConfig HandleCosmosNoSqlConfiguration(string? schema, RuntimeConfig runtimeConfig, string connectionString, string dataSourceName = "")
    {
        if (string.IsNullOrEmpty(dataSourceName))
        {
            dataSourceName = runtimeConfig.GetDefaultDataSourceName();
        }

        DbConnectionStringBuilder dbConnectionStringBuilder = new()
        {
            ConnectionString = connectionString
        };

        if (string.IsNullOrEmpty(schema))
        {
            throw new ArgumentException($"'{nameof(schema)}' cannot be null or empty.", nameof(schema));
        }

        HyphenatedNamingPolicy namingPolicy = new();

        DataSource dataSource = runtimeConfig.GetDataSourceFromDataSourceName(dataSourceName);

        Dictionary<string, JsonElement> options;
        if (dataSource.Options is not null)
        {
            options = new(dataSource.Options)
            {
                // push the "raw" GraphQL schema into the options to pull out later when requested
                { namingPolicy.ConvertName(nameof(CosmosDbNoSQLDataSourceOptions.GraphQLSchema)), JsonSerializer.SerializeToElement(schema) }
            };
        }
        else
        {
            throw new ArgumentException($"'{nameof(CosmosDbNoSQLDataSourceOptions)}' cannot be null or empty.", nameof(CosmosDbNoSQLDataSourceOptions));
        }

        // SWA may provide CosmosDB database name in connectionString
        string? database = dbConnectionStringBuilder.ContainsKey("Database") ? (string)dbConnectionStringBuilder["Database"] : null;

        if (database is not null)
        {
            // Add or update the options to contain the parsed database
            options[namingPolicy.ConvertName(nameof(CosmosDbNoSQLDataSourceOptions.Database))] = JsonSerializer.SerializeToElement(database);
        }

        // Update the connection string in the datasource with the one that was provided to the controller
        dataSource = dataSource with { Options = options, ConnectionString = connectionString };

        if (dataSourceName == runtimeConfig.GetDefaultDataSourceName())
        {
            // update default db.
            runtimeConfig = runtimeConfig with { DataSource = dataSource };
        }

        // update dictionary
        runtimeConfig.UpdateDataSourceNameToDataSource(dataSourceName, dataSource);

        return runtimeConfig;
    }
}
