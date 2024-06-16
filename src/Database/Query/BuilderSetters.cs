// Файл Database/Query/BuilderSetters.cs
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Relfost.Database.Query
{
    public partial class Builder
    {
        public async Task insert(Dictionary<string, object> values)
        {
            var columns = string.Join(", ", values.Keys);
            var parameters = string.Join(", ", values.Keys.Select((_, i) => $"@param{i}"));
            var sql = $"INSERT INTO {_tableName} ({columns}) VALUES ({parameters})";

            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            for (int i = 0; i < values.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@param{i}";
                parameter.Value = values.Values.ElementAt(i);
                command.Parameters.Add(parameter);
            }

            await command.ExecuteNonQueryAsync();
        }

        public async Task insert(IEnumerable<Dictionary<string, object>> values)
        {
            var columns = string.Join(", ", values.First().Keys);
            var parameters = string.Join(", ", Enumerable.Range(0, values.First().Count).Select(i => $"@param{i}"));
            var sql = $"INSERT INTO {_tableName} ({columns}) VALUES {string.Join(", ", values.Select(_ => $"({parameters})"))}";

            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            var index = 0;
            foreach (var valueSet in values)
            {
                for (int i = 0; i < valueSet.Count; i++)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@param{index}";
                    parameter.Value = valueSet.Values.ElementAt(i);
                    command.Parameters.Add(parameter);
                    index++;
                }
            }

            await command.ExecuteNonQueryAsync();
        }

        public async Task insertOrIgnore(IEnumerable<Dictionary<string, object>> values)
        {
            var columns = string.Join(", ", values.First().Keys);
            var parameters = string.Join(", ", Enumerable.Range(0, values.First().Count).Select(i => $"@param{i}"));
            var sql = $"INSERT OR IGNORE INTO {_tableName} ({columns}) VALUES {string.Join(", ", values.Select(_ => $"({parameters})"))}";

            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            var index = 0;
            foreach (var valueSet in values)
            {
                for (int i = 0; i < valueSet.Count; i++)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@param{index}";
                    parameter.Value = valueSet.Values.ElementAt(i);
                    command.Parameters.Add(parameter);
                    index++;
                }
            }

            await command.ExecuteNonQueryAsync();
        }

        public async Task insertUsing(IEnumerable<string> columns, Builder subQuery)
        {
            var columnsList = string.Join(", ", columns);
            var sql = $"INSERT INTO {_tableName} ({columnsList}) {subQuery.buildQuery()}";

            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            for (int i = 0; i < subQuery._whereParameters.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@param{i}";
                parameter.Value = subQuery._whereParameters[i];
                command.Parameters.Add(parameter);
            }

            await command.ExecuteNonQueryAsync();
        }

        public async Task<object> insertGetId(Dictionary<string, object> values, string idColumn = "id")
        {
            await insert(values);
            return await scalar<object>($"SELECT last_insert_rowid()", new List<object>());
        }

        public async Task upsert(IEnumerable<Dictionary<string, object>> values, IEnumerable<string> uniqueColumns, IEnumerable<string> updateColumns)
        {
            var columns = string.Join(", ", values.First().Keys);
            var parameters = string.Join(", ", Enumerable.Range(0, values.First().Count).Select(i => $"@param{i}"));
            var updateClause = string.Join(", ", updateColumns.Select(col => $"{col} = excluded.{col}"));
            var sql = $"INSERT INTO {_tableName} ({columns}) VALUES {string.Join(", ", values.Select(_ => $"({parameters})"))} ON CONFLICT ({string.Join(", ", uniqueColumns)}) DO UPDATE SET {updateClause}";

            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            var index = 0;
            foreach (var valueSet in values)
            {
                for (int i = 0; i < valueSet.Count; i++)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@param{index}";
                    parameter.Value = valueSet.Values.ElementAt(i);
                    command.Parameters.Add(parameter);
                    index++;
                }
            }

            await command.ExecuteNonQueryAsync();
        }

        public async Task<int> update(Dictionary<string, object> values)
        {
            var setClause = string.Join(", ", values.Keys.Select((col, i) => $"{col} = @param{i}"));
            var sql = $"UPDATE {_tableName} SET {setClause} {buildWhere()}";

            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            for (int i = 0; i < values.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@param{i}";
                parameter.Value = values.Values.ElementAt(i);
                command.Parameters.Add(parameter);
            }

            for (int i = 0; i < _whereParameters.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@param{values.Count + i}";
                parameter.Value = _whereParameters[i];
                command.Parameters.Add(parameter);
            }

            return await command.ExecuteNonQueryAsync();
        }

        public async Task updateOrInsert(Dictionary<string, object> conditions, Dictionary<string, object> values)
        {
            var found = await where(conditions.Select(kv => (kv.Key, "=", kv.Value))).exists();
            if (found)
            {
                await where(conditions.Select(kv => (kv.Key, "=", kv.Value))).update(values);
            }
            else
            {
                await insert(conditions.Concat(values).ToDictionary(kv => kv.Key, kv => kv.Value));
            }
        }

        public async Task<int> delete()
        {
            var sql = $"DELETE FROM {_tableName} {buildWhere()}";

            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            for (int i = 0; i < _whereParameters.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@param{i}";
                parameter.Value = _whereParameters[i];
                command.Parameters.Add(parameter);
            }

            return await command.ExecuteNonQueryAsync();
        }

        public async Task truncate()
        {
            var sql = $"TRUNCATE TABLE {_tableName}";

            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            await command.ExecuteNonQueryAsync();
        }
    }
}
