// Reference: Microsoft.Extensions.Logging.Abstractions
// Reference: Npgsql
// Reference: MySql.Data
// Reference: YamlDotNet
// Reference: System.Data.SQLite

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using Npgsql;
using MySql.Data.MySqlClient;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Newtonsoft.Json;

namespace Extension.DatabaseQueryBuilder
{
    public class DatabaseQueryBuilder
    {
        private readonly Configuration config;
        private readonly Configuration.DatabaseInfo currentDatabase;
        private string currentSchema;

        public DatabaseQueryBuilder()
        {
            // var configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs", $"{nameof(DatabaseQueryBuilder)}.yaml");
            var configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "carbon", "extensions", "data", $"{nameof(DatabaseQueryBuilder)}", "config.yaml");
            if (!File.Exists(configFilePath))
            {
                CreateDefaultConfigFile(configFilePath);
            }
            config = LoadConfig(configFilePath);
            currentDatabase = config.connections[config.@default];


            if (currentDatabase.driver == "sqlite")
            {
                var sqliteDatabasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "carbon", "extensions", "data", $"{nameof(DatabaseQueryBuilder)}", "database.sqlite");
                Directory.CreateDirectory(Path.GetDirectoryName(sqliteDatabasePath)); // Убедиться, что директория существует
                currentDatabase.database = sqliteDatabasePath;
            }
        }

        private void CreateDefaultConfigFile(string path)
        {
            var defaultConfig = new Configuration();
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(defaultConfig);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, yaml);
        }

        private Configuration LoadConfig(string path)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = File.ReadAllText(path);
            return deserializer.Deserialize<Configuration>(yaml);
        }

        public DatabaseQueryBuilder schema(string schema)
        {
            if (currentDatabase.driver == "pgsql")
            {
                currentSchema = schema;
            }
            return this;
        }

        private IDbConnection getConnection(string dbType = null)
        {
            var databaseInfo = dbType == null ? currentDatabase : config.connections[dbType];

            return databaseInfo.driver switch
            {
                "pgsql" => new NpgsqlConnection(buildConnectionString(databaseInfo)),
                "mysql" => new MySqlConnection(buildConnectionString(databaseInfo)),
                "sqlite" => new SQLiteConnection(buildConnectionString(databaseInfo)),
                "mariadb" => new MySqlConnection(buildConnectionString(databaseInfo)),
                _ => throw new Exception("Unsupported database driver.")
            };
        }

        private string buildConnectionString(Configuration.DatabaseInfo databaseInfo)
        {
            return databaseInfo.driver switch
            {
                "pgsql" => $"Host={databaseInfo.host};Port={databaseInfo.port};Database={databaseInfo.database};Username={databaseInfo.username};Password={databaseInfo.password}",
                "mysql" or "mariadb" => $"Server={databaseInfo.host};Port={databaseInfo.port};Database={databaseInfo.database};User={databaseInfo.username};Password={databaseInfo.password};Charset={databaseInfo.charset};SslMode={databaseInfo.sslmode}",
                "sqlite" => $"Data Source={databaseInfo.database};Version=3;",
                _ => throw new Exception("Unsupported database driver.")
            };
        }

        public T scalar<T>(string query, string dbType = null)
        {
            using var connection = getConnection(dbType);
            connection.Open();

            if (!string.IsNullOrEmpty(currentSchema) && currentDatabase.driver == "pgsql")
            {
                using var schemaCommand = connection.CreateCommand();
                schemaCommand.CommandText = $"SET search_path TO {currentSchema}";
                schemaCommand.ExecuteNonQuery();
            }

            using var command = connection.CreateCommand();
            command.CommandText = query;

            var result = command.ExecuteScalar();
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public QueryBuilder table(string tableName)
        {
            return new QueryBuilder(this, tableName);
        }

        public class QueryBuilder
        {
            private readonly DatabaseQueryBuilder _dbExtension;
            private readonly string _tableName;
            private readonly List<string> _selectColumns;
            private readonly List<string> _whereConditions;
            private readonly List<string> _joinStatements;
            private readonly List<string> _groupByColumns;
            private readonly List<string> _havingConditions;
            private readonly List<string> _orderByStatements;
            private string _limit;
            private string _offset;
            private string _lockMode;

            public QueryBuilder(DatabaseQueryBuilder dbExtension, string tableName)
            {
                _dbExtension = dbExtension;
                _tableName = tableName;
                _selectColumns = new List<string>();
                _whereConditions = new List<string>();
                _joinStatements = new List<string>();
                _groupByColumns = new List<string>();
                _havingConditions = new List<string>();
                _orderByStatements = new List<string>();
                _limit = null;
                _offset = null;
                _lockMode = null;
            }

            public QueryBuilder select(params string[] columns)
            {
                _selectColumns.AddRange(columns);
                return this;
            }

            public QueryBuilder where(string column, string operation, object value)
            {
                _whereConditions.Add($"{column} {operation} '{value}'");
                return this;
            }

            public QueryBuilder orWhere(Action<QueryBuilder> query)
            {
                var subQuery = new QueryBuilder(_dbExtension, _tableName);
                query(subQuery);
                _whereConditions.Add($"({string.Join(" AND ", subQuery._whereConditions)})");
                return this;
            }

            public QueryBuilder whereNot(Action<QueryBuilder> query)
            {
                var subQuery = new QueryBuilder(_dbExtension, _tableName);
                query(subQuery);
                _whereConditions.Add($"NOT ({string.Join(" OR ", subQuery._whereConditions)})");
                return this;
            }

            public QueryBuilder whereAny(string[] columns, string operation, object value)
            {
                var conditions = columns.Select(col => $"{col} {operation} '{value}'");
                _whereConditions.Add($"({string.Join(" OR ", conditions)})");
                return this;
            }

            public QueryBuilder whereAll(string[] columns, string operation, object value)
            {
                var conditions = columns.Select(col => $"{col} {operation} '{value}'");
                _whereConditions.Add($"({string.Join(" AND ", conditions)})");
                return this;
            }

            public QueryBuilder whereJson(string column, string path, object value)
            {
                _whereConditions.Add($"{column}->'{path}' = '{value}'");
                return this;
            }

            public QueryBuilder whereJsonContains(string column, object value)
            {
                var formattedValue = value is string ? $"'{value}'" : $"'{JsonConvert.SerializeObject(value)}'";
                _whereConditions.Add($"{column} @> {formattedValue}");
                return this;
            }

            public QueryBuilder whereJsonLength(string column, string operation, int value)
            {
                _whereConditions.Add($"json_array_length({column}) {operation} {value}");
                return this;
            }

            public QueryBuilder join(string table, string first, string operation, string second)
            {
                _joinStatements.Add($"JOIN {table} ON {first} {operation} {second}");
                return this;
            }

            public QueryBuilder whereBetween(string column, object start, object end)
            {
                _whereConditions.Add($"{column} BETWEEN '{start}' AND '{end}'");
                return this;
            }

            public QueryBuilder whereNotBetween(string column, object start, object end)
            {
                _whereConditions.Add($"{column} NOT BETWEEN '{start}' AND '{end}'");
                return this;
            }

            public QueryBuilder whereIn(string column, IEnumerable<object> values)
            {
                var formattedValues = string.Join(", ", values.Select(v => $"'{v}'"));
                _whereConditions.Add($"{column} IN ({formattedValues})");
                return this;
            }

            public QueryBuilder whereNotIn(string column, IEnumerable<object> values)
            {
                var formattedValues = string.Join(", ", values.Select(v => $"'{v}'"));
                _whereConditions.Add($"{column} NOT IN ({formattedValues})");
                return this;
            }

            public QueryBuilder whereNull(string column)
            {
                _whereConditions.Add($"{column} IS NULL");
                return this;
            }

            public QueryBuilder whereNotNull(string column)
            {
                _whereConditions.Add($"{column} IS NOT NULL");
                return this;
            }

            public QueryBuilder whereDate(string column, string operation, string date)
            {
                _whereConditions.Add($"DATE({column}) {operation} '{date}'");
                return this;
            }

            public QueryBuilder whereMonth(string column, string operation, int month)
            {
                _whereConditions.Add($"EXTRACT(MONTH FROM {column}) {operation} {month}");
                return this;
            }

            public QueryBuilder whereDay(string column, string operation, int day)
            {
                _whereConditions.Add($"EXTRACT(DAY FROM {column}) {operation} {day}");
                return this;
            }

            public QueryBuilder whereYear(string column, string operation, int year)
            {
                _whereConditions.Add($"EXTRACT(YEAR FROM {column}) {operation} {year}");
                return this;
            }

            public QueryBuilder whereTime(string column, string operation, string time)
            {
                _whereConditions.Add($"EXTRACT(TIME FROM {column}) {operation} '{time}'");
                return this;
            }

            public QueryBuilder whereColumn(string firstColumn, string operation, string secondColumn)
            {
                _whereConditions.Add($"{firstColumn} {operation} {secondColumn}");
                return this;
            }

            public QueryBuilder whereExists(Action<QueryBuilder> query)
            {
                var subQuery = new QueryBuilder(_dbExtension, _tableName);
                query(subQuery);
                _whereConditions.Add($"EXISTS ({subQuery.buildQuery()})");
                return this;
            }

            public QueryBuilder orderBy(string column, string direction = "asc")
            {
                _orderByStatements.Add($"{column} {direction.ToUpper()}");
                return this;
            }

            public QueryBuilder latest(string column = "created_at")
            {
                return orderBy(column, "desc");
            }

            public QueryBuilder oldest(string column = "created_at")
            {
                return orderBy(column, "asc");
            }

            public QueryBuilder inRandomOrder()
            {
                _orderByStatements.Add("RANDOM()");
                return this;
            }

            public QueryBuilder reorder(string column = null, string direction = null)
            {
                _orderByStatements.Clear();
                if (column != null)
                {
                    _orderByStatements.Add($"{column} {direction.ToUpper()}");
                }
                return this;
            }

            public QueryBuilder groupBy(params string[] columns)
            {
                _groupByColumns.AddRange(columns);
                return this;
            }

            public QueryBuilder having(string column, string operation, object value)
            {
                _havingConditions.Add($"{column} {operation} '{value}'");
                return this;
            }

            public QueryBuilder havingBetween(string column, object start, object end)
            {
                _havingConditions.Add($"{column} BETWEEN '{start}' AND '{end}'");
                return this;
            }

            public QueryBuilder limit(int value)
            {
                _limit = value.ToString();
                return this;
            }

            public QueryBuilder offset(int value)
            {
                _offset = value.ToString();
                return this;
            }

            public QueryBuilder take(int value)
            {
                return limit(value);
            }

            public QueryBuilder skip(int value)
            {
                return offset(value);
            }

            public QueryBuilder when<T>(T condition, Action<QueryBuilder, T> trueCallback, Action<QueryBuilder> falseCallback = null)
            {
                if (Convert.ToBoolean(condition))
                {
                    trueCallback(this, condition);
                }
                else if (falseCallback != null)
                {
                    falseCallback(this);
                }
                return this;
            }

            public QueryBuilder sharedLock()
            {
                _lockMode = "FOR SHARE";
                return this;
            }

            public QueryBuilder lockForUpdate()
            {
                _lockMode = "FOR UPDATE";
                return this;
            }

            public DataTable get()
            {
                using var connection = _dbExtension.getConnection();
                connection.Open();

                if (!string.IsNullOrEmpty(_dbExtension.currentSchema) && _dbExtension.currentDatabase.driver == "pgsql")
                {
                    using var schemaCommand = connection.CreateCommand();
                    schemaCommand.CommandText = $"SET search_path TO {_dbExtension.currentSchema}";
                    schemaCommand.ExecuteNonQuery();
                }

                var query = buildQuery();

                using var command = connection.CreateCommand();
                command.CommandText = query;

                var adapter = _dbExtension.getAdapter(command);
                var dataSet = new DataSet();
                adapter.Fill(dataSet);

                return dataSet.Tables[0];
            }

            public void dd()
            {
                Console.WriteLine(buildQuery());
                Environment.Exit(0);
            }

            public void dump()
            {
                Console.WriteLine(buildQuery());
            }

            public void dumpRawSql()
            {
                var query = buildQuery();
                foreach (var condition in _whereConditions)
                {
                    query = query.Replace($"'{condition}'", condition);
                }
                Console.WriteLine(query);
            }

            public void ddRawSql()
            {
                dumpRawSql();
                Environment.Exit(0);
            }

            private string buildQuery()
            {
                var queryBuilder = new StringBuilder();
                queryBuilder.Append("SELECT ");
                queryBuilder.Append(_selectColumns.Any() ? string.Join(", ", _selectColumns) : "*");
                queryBuilder.Append($" FROM {_tableName} ");

                if (_joinStatements.Any())
                {
                    queryBuilder.Append(string.Join(" ", _joinStatements));
                }

                if (_whereConditions.Any())
                {
                    queryBuilder.Append(" WHERE ");
                    queryBuilder.Append(string.Join(" AND ", _whereConditions));
                }

                if (_groupByColumns.Any())
                {
                    queryBuilder.Append(" GROUP BY ");
                    queryBuilder.Append(string.Join(", ", _groupByColumns));
                }

                if (_havingConditions.Any())
                {
                    queryBuilder.Append(" HAVING ");
                    queryBuilder.Append(string.Join(" AND ", _havingConditions));
                }

                if (_orderByStatements.Any())
                {
                    queryBuilder.Append(" ORDER BY ");
                    queryBuilder.Append(string.Join(" ", _orderByStatements));
                }

                if (_limit != null)
                {
                    queryBuilder.Append(" LIMIT ");
                    queryBuilder.Append(_limit);
                }

                if (_offset != null)
                {
                    queryBuilder.Append(" OFFSET ");
                    queryBuilder.Append(_offset);
                }

                if (!string.IsNullOrEmpty(_lockMode))
                {
                    queryBuilder.Append(" " + _lockMode);
                }

                return queryBuilder.ToString();
            }

            public int insert(Dictionary<string, object> values)
            {
                using var connection = _dbExtension.getConnection();
                connection.Open();

                var columns = string.Join(", ", values.Keys);
                var parameters = string.Join(", ", values.Keys.Select(k => $"@{k}"));
                var query = $"INSERT INTO {_tableName} ({columns}) VALUES ({parameters})";

                using var command = connection.CreateCommand();
                command.CommandText = query;

                foreach (var pair in values)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@{pair.Key}";
                    parameter.Value = pair.Value;
                    command.Parameters.Add(parameter);
                }

                return command.ExecuteNonQuery();
            }

            public int insertOrIgnore(Dictionary<string, object> values)
            {
                if (_dbExtension.currentDatabase.driver == "mysql" || _dbExtension.currentDatabase.driver == "mariadb")
                {
                    using var connection = _dbExtension.getConnection();
                    connection.Open();

                    var columns = string.Join(", ", values.Keys);
                    var parameters = string.Join(", ", values.Keys.Select(k => $"@{k}"));
                    var query = $"INSERT IGNORE INTO {_tableName} ({columns}) VALUES ({parameters})";

                    using var command = connection.CreateCommand();
                    command.CommandText = query;

                    foreach (var pair in values)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = $"@{pair.Key}";
                        parameter.Value = pair.Value;
                        command.Parameters.Add(parameter);
                    }

                    return command.ExecuteNonQuery();
                }
                else if (_dbExtension.currentDatabase.driver == "pgsql")
                {
                    using var connection = _dbExtension.getConnection();
                    connection.Open();

                    var columns = string.Join(", ", values.Keys);
                    var parameters = string.Join(", ", values.Keys.Select(k => $"@{k}"));
                    var query = $"INSERT INTO {_tableName} ({columns}) VALUES ({parameters}) ON CONFLICT DO NOTHING";

                    using var command = connection.CreateCommand();
                    command.CommandText = query;

                    foreach (var pair in values)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = $"@{pair.Key}";
                        parameter.Value = pair.Value;
                        command.Parameters.Add(parameter);
                    }

                    return command.ExecuteNonQuery();
                }
                else
                {
                    throw new Exception("Unsupported database driver for insertOrIgnore.");
                }
            }

            public long insertGetId(Dictionary<string, object> values, string idColumn = "id")
            {
                using var connection = _dbExtension.getConnection();
                connection.Open();

                var columns = string.Join(", ", values.Keys);
                var parameters = string.Join(", ", values.Keys.Select(k => $"@{k}"));
                var query = $"INSERT INTO {_tableName} ({columns}) VALUES ({parameters}) RETURNING {idColumn}";

                using var command = connection.CreateCommand();
                command.CommandText = query;

                foreach (var pair in values)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@{pair.Key}";
                    parameter.Value = pair.Value;
                    command.Parameters.Add(parameter);
                }

                var result = command.ExecuteScalar();
                return Convert.ToInt64(result);
            }

            public int upsert(List<Dictionary<string, object>> values, List<string> uniqueBy, List<string> updateColumns)
            {
                if (_dbExtension.currentDatabase.driver == "mysql" || _dbExtension.currentDatabase.driver == "mariadb" || _dbExtension.currentDatabase.driver == "pgsql")
                {
                    using var connection = _dbExtension.getConnection();
                    connection.Open();

                    var firstRow = values.First();
                    var columns = string.Join(", ", firstRow.Keys);
                    var parameters = string.Join(", ", firstRow.Keys.Select(k => $"@{k}"));
                    var updates = string.Join(", ", updateColumns.Select(c => $"{c} = EXCLUDED.{c}"));
                    var uniqueColumns = string.Join(", ", uniqueBy);
                    var query = $"INSERT INTO {_tableName} ({columns}) VALUES ({parameters}) ON CONFLICT ({uniqueColumns}) DO UPDATE SET {updates}";

                    using var command = connection.CreateCommand();
                    command.CommandText = query;

                    foreach (var row in values)
                    {
                        command.Parameters.Clear();
                        foreach (var pair in row)
                        {
                            var parameter = command.CreateParameter();
                            parameter.ParameterName = $"@{pair.Key}";
                            parameter.Value = pair.Value;
                            command.Parameters.Add(parameter);
                        }

                        command.ExecuteNonQuery();
                    }

                    return values.Count;
                }
                else
                {
                    throw new Exception("Unsupported database driver for upsert.");
                }
            }

            public int update(Dictionary<string, object> values)
            {
                using var connection = _dbExtension.getConnection();
                connection.Open();

                var updates = string.Join(", ", values.Keys.Select(k => $"{k} = @{k}"));
                var query = $"UPDATE {_tableName} SET {updates}";

                if (_whereConditions.Any())
                {
                    query += " WHERE " + string.Join(" AND ", _whereConditions);
                }

                using var command = connection.CreateCommand();
                command.CommandText = query;

                foreach (var pair in values)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@{pair.Key}";
                    parameter.Value = pair.Value;
                    command.Parameters.Add(parameter);
                }

                return command.ExecuteNonQuery();
            }

            public int updateOrInsert(Dictionary<string, object> conditions, Dictionary<string, object> values)
            {
                using var connection = _dbExtension.getConnection();
                connection.Open();

                var conditionString = string.Join(" AND ", conditions.Keys.Select(k => $"{k} = @{k}"));
                var query = $"SELECT COUNT(*) FROM {_tableName} WHERE {conditionString}";

                using var selectCommand = connection.CreateCommand();
                selectCommand.CommandText = query;

                foreach (var pair in conditions)
                {
                    var parameter = selectCommand.CreateParameter();
                    parameter.ParameterName = $"@{pair.Key}";
                    parameter.Value = pair.Value;
                    selectCommand.Parameters.Add(parameter);
                }

                var exists = Convert.ToInt32(selectCommand.ExecuteScalar()) > 0;

                if (exists)
                {
                    var updates = string.Join(", ", values.Keys.Select(k => $"{k} = @{k}"));
                    query = $"UPDATE {_tableName} SET {updates} WHERE {conditionString}";

                    using var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText = query;

                    foreach (var pair in conditions)
                    {
                        var parameter = updateCommand.CreateParameter();
                        parameter.ParameterName = $"@{pair.Key}";
                        parameter.Value = pair.Value;
                        updateCommand.Parameters.Add(parameter);
                    }

                    foreach (var pair in values)
                    {
                        var parameter = updateCommand.CreateParameter();
                        parameter.ParameterName = $"@{pair.Key}";
                        parameter.Value = pair.Value;
                        updateCommand.Parameters.Add(parameter);
                    }

                    return updateCommand.ExecuteNonQuery();
                }
                else
                {
                    var allValues = conditions.Concat(values).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    return insert(allValues);
                }
            }

            public int updateJson(string column, string path, object value)
            {
                using var connection = _dbExtension.getConnection();
                connection.Open();

                var update = $"{column} = jsonb_set({column}, '{{{path}}}', '{value}', true)";
                var query = $"UPDATE {_tableName} SET {update}";

                if (_whereConditions.Any())
                {
                    query += " WHERE " + string.Join(" AND ", _whereConditions);
                }

                using var command = connection.CreateCommand();
                command.CommandText = query;

                return command.ExecuteNonQuery();
            }

            public int increment(string column, int amount = 1, Dictionary<string, object> additionalValues = null)
            {
                using var connection = _dbExtension.getConnection();
                connection.Open();

                var update = $"{column} = {column} + {amount}";

                if (additionalValues != null && additionalValues.Any())
                {
                    var additionalUpdates = string.Join(", ", additionalValues.Keys.Select(k => $"{k} = @{k}"));
                    update = $"{update}, {additionalUpdates}";
                }

                var query = $"UPDATE {_tableName} SET {update}";

                if (_whereConditions.Any())
                {
                    query += " WHERE " + string.Join(" AND ", _whereConditions);
                }

                using var command = connection.CreateCommand();
                command.CommandText = query;

                if (additionalValues != null)
                {
                    foreach (var pair in additionalValues)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = $"@{pair.Key}";
                        parameter.Value = pair.Value;
                        command.Parameters.Add(parameter);
                    }
                }

                return command.ExecuteNonQuery();
            }

            public int decrement(string column, int amount = 1, Dictionary<string, object> additionalValues = null)
            {
                return increment(column, -amount, additionalValues);
            }

            public int delete()
            {
                using var connection = _dbExtension.getConnection();
                connection.Open();

                var query = $"DELETE FROM {_tableName}";

                if (_whereConditions.Any())
                {
                    query += " WHERE " + string.Join(" AND ", _whereConditions);
                }

                using var command = connection.CreateCommand();
                command.CommandText = query;

                return command.ExecuteNonQuery();
            }

            public void truncate()
            {
                using var connection = _dbExtension.getConnection();
                connection.Open();

                var query = $"TRUNCATE TABLE {_tableName}";

                if (_dbExtension.currentDatabase.driver == "pgsql")
                {
                    query += " CASCADE";
                }

                using var command = connection.CreateCommand();
                command.CommandText = query;
                command.ExecuteNonQuery();
            }
        }

        private IDbDataAdapter getAdapter(IDbCommand command, string dbType = null)
        {
            if (dbType == null)
            {
                dbType = currentDatabase.driver;
            }

            return dbType switch
            {
                "pgsql" => new NpgsqlDataAdapter((NpgsqlCommand)command),
                "mysql" or "mariadb" => new MySqlDataAdapter((MySqlCommand)command),
                "sqlite" => new SQLiteDataAdapter((SQLiteCommand)command),
                _ => throw new Exception("Unsupported database driver.")
            };
        }

        internal class Configuration
        {
            public string @default { get; set; } = "pgsql";
            public Dictionary<string, DatabaseInfo> connections { get; set; } = new()
            {
                {
                    "pgsql", new DatabaseInfo
                    {
                        driver = "pgsql",
                        host = "localhost",
                        port = 5432,
                        database = "postgres",
                        username = "postgres",
                        password = "postgres"
                    }
                },
                {
                    "mysql", new DatabaseInfo
                    {
                        driver = "mysql",
                        host = "localhost",
                        port = 3306,
                        database = "database",
                        username = "root",
                        password = "",
                        charset = "utf8mb4",
                        collation = "utf8mb4_unicode_ci",
                        prefix = "",
                        prefix_indexes = true,
                        strict = true,
                        engine = null,
                        sslmode = "preferred",
                        options = new Dictionary<string, string>()
                    }
                },
                {
                    "sqlite", new DatabaseInfo
                    {
                        driver = "sqlite",
                        database = "database.sqlite",
                        prefix = "",
                        foreign_key_constraints = true
                    }
                },
                {
                    "mariadb", new DatabaseInfo
                    {
                        driver = "mariadb",
                        host = "localhost",
                        port = 3306,
                        database = "database",
                        username = "root",
                        password = "",
                        charset = "utf8mb4",
                        collation = "utf8mb4_unicode_ci",
                        prefix = "",
                        prefix_indexes = true,
                        strict = true,
                        engine = null,
                        sslmode = "preferred",
                        options = new Dictionary<string, string>()
                    }
                }
            };

            public class DatabaseInfo
            {
                public string driver { get; set; }
                public string host { get; set; }
                public uint port { get; set; }
                public string database { get; set; }
                public string username { get; set; }
                public string password { get; set; }
                public string charset { get; set; }
                public string collation { get; set; }
                public string prefix { get; set; }
                public bool prefix_indexes { get; set; }
                public bool strict { get; set; }
                public string engine { get; set; }
                public string sslmode { get; set; }
                public bool foreign_key_constraints { get; set; }
                public Dictionary<string, string> options { get; set; }
            }
        }
    }
}
