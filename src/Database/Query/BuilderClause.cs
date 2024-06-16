// Файл Database/Query/BuilderClause.cs
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
        public Builder groupBy(params string[] columns)
        {
            _groupByColumns.AddRange(columns);
            return this;
        }

        public Builder having(string column, string operation, object value)
        {
            _havingConditions.Add($"{column} {operation} '@param{_havingParameters.Count}'");
            _havingParameters.Add(value);
            return this;
        }

        public Builder orderBy(string column, string direction = "asc")
        {
            _orderByStatements.Add($"{column} {direction.ToUpper()}");
            return this;
        }

        public Builder latest(string column = "created_at")
        {
            return orderBy(column, "desc");
        }

        public Builder oldest(string column = "created_at")
        {
            return orderBy(column, "asc");
        }

        public Builder reorder(string column = null, string direction = null)
        {
            _orderByStatements.Clear();
            if (column != null)
            {
                _orderByStatements.Add($"{column} {direction.ToUpper()}");
            }
            return this;
        }

        public Builder limit(int value)
        {
            _limit = value.ToString();
            return this;
        }

        public Builder take(int value)
        {
            return limit(value);
        }

        public Builder offset(int value)
        {
            _offset = value.ToString();
            return this;
        }

        public Builder skip(int value)
        {
            return offset(value);
        }

        public Builder inRandomOrder()
        {
            _orderByStatements.Add("RANDOM()");
            return this;
        }

        public async Task<int> increment(string column, int amount = 1, Dictionary<string, object> additionalColumns = null)
        {
            var setClause = $"{column} = {column} + {amount}";
            if (additionalColumns != null)
            {
                setClause += ", " + string.Join(", ", additionalColumns.Select((col, i) => $"{col.Key} = @param{i}"));
            }
            var sql = $"UPDATE {_tableName} SET {setClause} {buildWhere()}";

            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            if (additionalColumns != null)
            {
                for (int i = 0; i < additionalColumns.Count; i++)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@param{i}";
                    parameter.Value = additionalColumns.Values.ElementAt(i);
                    command.Parameters.Add(parameter);
                }
            }

            for (int i = 0; i < _whereParameters.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@param{(additionalColumns?.Count ?? 0) + i}";
                parameter.Value = _whereParameters[i];
                command.Parameters.Add(parameter);
            }

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<int> decrement(string column, int amount = 1, Dictionary<string, object> additionalColumns = null)
        {
            return await increment(column, -amount, additionalColumns);
        }

        public async Task<int> incrementEach(Dictionary<string, int> columns)
        {
            var setClause = string.Join(", ", columns.Select((col, i) => $"{col.Key} = {col.Key} + @param{i}"));
            var sql = $"UPDATE {_tableName} SET {setClause} {buildWhere()}";

            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            for (int i = 0; i < columns.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@param{i}";
                parameter.Value = columns.Values.ElementAt(i);
                command.Parameters.Add(parameter);
            }

            for (int i = 0; i < _whereParameters.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@param{columns.Count + i}";
                parameter.Value = _whereParameters[i];
                command.Parameters.Add(parameter);
            }

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<int> decrementEach(Dictionary<string, int> columns)
        {
            var negativeColumns = columns.ToDictionary(kv => kv.Key, kv => -kv.Value);
            return await incrementEach(negativeColumns);
        }
    }
}
