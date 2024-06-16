// Файл Database/Query/BuilderGetters.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Data;
using System.Threading.Tasks;

namespace Relfost.Database.Query
{
    public partial class Builder
    {
        private async Task<T> scalar<T>(string query, List<object> parameters)
        {
            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = query;

            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@param{i}";
                parameter.Value = parameters[i];
                command.Parameters.Add(parameter);
            }

            var result = await command.ExecuteScalarAsync();
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public async Task<DataTable> get()
        {
            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();

            if (!string.IsNullOrEmpty(_dbExtension.currentSchema) && _dbExtension.currentDatabase.driver == "pgsql")
            {
                using var schemaCommand = connection.CreateCommand();
                schemaCommand.CommandText = $"SET search_path TO {_dbExtension.currentSchema}";
                await schemaCommand.ExecuteNonQueryAsync();
            }

            var query = buildQuery();

            using var command = connection.CreateCommand();
            command.CommandText = query;

            for (int i = 0; i < _whereParameters.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@param{i}";
                parameter.Value = _whereParameters[i];
                command.Parameters.Add(parameter);
            }

            var adapter = _dbExtension.getAdapter(command);
            var dataSet = new DataSet();
            await Task.Run(() => adapter.Fill(dataSet));

            return dataSet.Tables[0];
        }

        public async Task<DataRow> first()
        {
            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();

            if (!string.IsNullOrEmpty(_dbExtension.currentSchema) && _dbExtension.currentDatabase.driver == "pgsql")
            {
                using var schemaCommand = connection.CreateCommand();
                schemaCommand.CommandText = $"SET search_path TO {_dbExtension.currentSchema}";
                await schemaCommand.ExecuteNonQueryAsync();
            }

            var query = buildQuery();
            query += " LIMIT 1";

            using var command = connection.CreateCommand();
            command.CommandText = query;

            for (int i = 0; i < _whereParameters.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@param{i}";
                parameter.Value = _whereParameters[i];
                command.Parameters.Add(parameter);
            }

            var adapter = _dbExtension.getAdapter(command);
            var dataSet = new DataSet();
            await Task.Run(() => adapter.Fill(dataSet));

            return dataSet.Tables[0].Rows.Count > 0 ? dataSet.Tables[0].Rows[0] : null;
        }

        public async Task<object> value(string column)
        {
            var firstRow = await first();
            return firstRow?[column];
        }

        public async Task<DataRow> find(object id)
        {
            where("id", "=", id);
            return await first();
        }

        public async Task<List<object>> pluck(string column)
        {
            var dataTable = await get();
            return dataTable.Rows.Cast<DataRow>().Select(row => row[column]).ToList();
        }

        public async Task<List<Dictionary<string, object>>> pluck(params string[] columns)
        {
            var dataTable = await get();
            return dataTable.Rows.Cast<DataRow>().Select(row =>
            {
                var dict = new Dictionary<string, object>();
                foreach (var column in columns)
                {
                    dict[column] = row[column];
                }
                return dict;
            }).ToList();
        }
    }
}
