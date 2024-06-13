using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Relfost.Database.Query
{
    public class Builder
    {
        private readonly DatabaseManager _dbExtension;
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

        public Builder(DatabaseManager dbExtension, string tableName)
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

        public Builder select(params string[] columns)
        {
            _selectColumns.AddRange(columns);
            return this;
        }

        public Builder addSelect(params string[] columns)
        {
            _selectColumns.AddRange(columns);
            return this;
        }

        public Builder distinct()
        {
            _selectColumns.Insert(0, "DISTINCT");
            return this;
        }

        public Builder where(string column, string operation, object value)
        {
            _whereConditions.Add($"{column} {operation} '{value}'");
            return this;
        }

        public Builder where(string column, object value)
        {
            return where(column, "=", value);
        }

        public Builder orWhere(string column, string operation, object value)
        {
            _whereConditions.Add($"OR {column} {operation} '{value}'");
            return this;
        }

        public Builder orWhere(string column, object value)
        {
            return orWhere(column, "=", value);
        }

        public Builder whereRaw(string sql, params object[] bindings)
        {
            _whereConditions.Add(BindingsToSql(sql, bindings));
            return this;
        }

        public Builder orWhereRaw(string sql, params object[] bindings)
        {
            _whereConditions.Add($"OR {BindingsToSql(sql, bindings)}");
            return this;
        }

        public Builder havingRaw(string sql, params object[] bindings)
        {
            _havingConditions.Add(BindingsToSql(sql, bindings));
            return this;
        }

        public Builder orHavingRaw(string sql, params object[] bindings)
        {
            _havingConditions.Add($"OR {BindingsToSql(sql, bindings)}");
            return this;
        }

        public Builder orderByRaw(string sql, params object[] bindings)
        {
            _orderByStatements.Add(BindingsToSql(sql, bindings));
            return this;
        }

        public Builder groupByRaw(string sql, params object[] bindings)
        {
            _groupByColumns.Add(BindingsToSql(sql, bindings));
            return this;
        }

        public Builder orWhere(Action<Builder> query)
        {
            var subQuery = new Builder(_dbExtension, _tableName);
            query(subQuery);
            _whereConditions.Add($"({string.Join(" AND ", subQuery._whereConditions)})");
            return this;
        }

        public Builder whereNot(Action<Builder> query)
        {
            var subQuery = new Builder(_dbExtension, _tableName);
            query(subQuery);
            _whereConditions.Add($"NOT ({string.Join(" OR ", subQuery._whereConditions)})");
            return this;
        }

        public Builder whereAny(string[] columns, string operation, object value)
        {
            var conditions = columns.Select(col => $"{col} {operation} '{value}'");
            _whereConditions.Add($"({string.Join(" OR ", conditions)})");
            return this;
        }

        public Builder whereAll(string[] columns, string operation, object value)
        {
            var conditions = columns.Select(col => $"{col} {operation} '{value}'");
            _whereConditions.Add($"({string.Join(" AND ", conditions)})");
            return this;
        }

        public Builder whereJson(string column, string path, object value)
        {
            _whereConditions.Add($"{column}->'{path}' = '{value}'");
            return this;
        }

        public Builder whereJsonContains(string column, object value)
        {
            var formattedValue = value is string ? $"'{value}'" : $"'{JsonConvert.SerializeObject(value)}'";
            _whereConditions.Add($"{column} @> {formattedValue}");
            return this;
        }

        public Builder whereJsonLength(string column, string operation, int value)
        {
            _whereConditions.Add($"json_array_length({column}) {operation} {value}");
            return this;
        }

        public Builder join(string table, string first, string operation, string second)
        {
            _joinStatements.Add($"JOIN {table} ON {first} {operation} {second}");
            return this;
        }

        public Builder leftJoin(string table, string first, string operation, string second)
        {
            _joinStatements.Add($"LEFT JOIN {table} ON {first} {operation} {second}");
            return this;
        }

        public Builder rightJoin(string table, string first, string operation, string second)
        {
            _joinStatements.Add($"RIGHT JOIN {table} ON {first} {operation} {second}");
            return this;
        }

        public Builder crossJoin(string table)
        {
            _joinStatements.Add($"CROSS JOIN {table}");
            return this;
        }

        public Builder whereBetween(string column, object start, object end)
        {
            _whereConditions.Add($"{column} BETWEEN '{start}' AND '{end}'");
            return this;
        }

        public Builder whereNotBetween(string column, object start, object end)
        {
            _whereConditions.Add($"{column} NOT BETWEEN '{start}' AND '{end}'");
            return this;
        }

        public Builder whereIn(string column, IEnumerable<object> values)
        {
            var formattedValues = string.Join(", ", values.Select(v => $"'{v}'"));
            _whereConditions.Add($"{column} IN ({formattedValues})");
            return this;
        }

        public Builder whereNotIn(string column, IEnumerable<object> values)
        {
            var formattedValues = string.Join(", ", values.Select(v => $"'{v}'"));
            _whereConditions.Add($"{column} NOT IN ({formattedValues})");
            return this;
        }

        public Builder whereNull(string column)
        {
            _whereConditions.Add($"{column} IS NULL");
            return this;
        }

        public Builder whereNotNull(string column)
        {
            _whereConditions.Add($"{column} IS NOT NULL");
            return this;
        }

        public Builder whereDate(string column, string operation, string date)
        {
            _whereConditions.Add($"DATE({column}) {operation} '{date}'");
            return this;
        }

        public Builder whereMonth(string column, string operation, int month)
        {
            _whereConditions.Add($"EXTRACT(MONTH FROM {column}) {operation} {month}");
            return this;
        }

        public Builder whereDay(string column, string operation, int day)
        {
            _whereConditions.Add($"EXTRACT(DAY FROM {column}) {operation} {day}");
            return this;
        }

        public Builder whereYear(string column, string operation, int year)
        {
            _whereConditions.Add($"EXTRACT(YEAR FROM {column}) {operation} {year}");
            return this;
        }

        public Builder whereTime(string column, string operation, string time)
        {
            _whereConditions.Add($"EXTRACT(TIME FROM {column}) {operation} '{time}'");
            return this;
        }

        public Builder whereColumn(string firstColumn, string operation, string secondColumn)
        {
            _whereConditions.Add($"{firstColumn} {operation} {secondColumn}");
            return this;
        }

        public Builder whereExists(Action<Builder> query)
        {
            var subQuery = new Builder(_dbExtension, _tableName);
            query(subQuery);
            _whereConditions.Add($"EXISTS ({subQuery.buildQuery()})");
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

        public Builder inRandomOrder()
        {
            _orderByStatements.Add("RANDOM()");
            return this;
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

        public Builder groupBy(params string[] columns)
        {
            _groupByColumns.AddRange(columns);
            return this;
        }

        public Builder having(string column, string operation, object value)
        {
            _havingConditions.Add($"{column} {operation} '{value}'");
            return this;
        }

        public Builder havingBetween(string column, object start, object end)
        {
            _havingConditions.Add($"{column} BETWEEN '{start}' AND '{end}'");
            return this;
        }

        public Builder limit(int value)
        {
            _limit = value.ToString();
            return this;
        }

        public Builder offset(int value)
        {
            _offset = value.ToString();
            return this;
        }

        public Builder take(int value)
        {
            return limit(value);
        }

        public Builder skip(int value)
        {
            return offset(value);
        }

        public Builder when<T>(T condition, Action<Builder, T> trueCallback, Action<Builder> falseCallback = null)
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

        public Builder sharedLock()
        {
            _lockMode = "FOR SHARE";
            return this;
        }

        public Builder lockForUpdate()
        {
            _lockMode = "FOR UPDATE";
            return this;
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

            var adapter = _dbExtension.getAdapter(command);
            var dataSet = new DataSet();
            await Task.Run(() => adapter.Fill(dataSet));

            return dataSet.Tables[0];
        }

        public async Task<object[]> toArray()
        {
            var table = await get();
            return table.Rows.Cast<DataRow>().Select(row => row.ItemArray).ToArray();
        }

        public async Task<DataRow> first()
        {
            var table = await get();
            return table.Rows.Cast<DataRow>().FirstOrDefault();
        }

        public async Task<object> value(string column)
        {
            var row = await first();
            return row?[column];
        }

        public async Task<DataRow> find(object id)
        {
            return await where("id", id).first();
        }

        public async Task<object[]> pluck(string column)
        {
            var table = await get();
            return table.Rows.Cast<DataRow>().Select(row => row[column]).ToArray();
        }

        public async Task<List<Dictionary<string, object>>> pluck(params string[] columns)
        {
            var table = await get();
            return table.Rows.Cast<DataRow>()
                .Select(row => columns.ToDictionary(column => column, column => row[column]))
                .ToList();
        }

        public async Task chunk(int count, Func<DataTable, Task<bool>> callback)
        {
            var offset = 0;
            while (true)
            {
                var results = await skip(offset).take(count).get();
                if (results.Rows.Count == 0)
                {
                    break;
                }

                var continueProcessing = await callback(results);
                if (!continueProcessing)
                {
                    break;
                }

                offset += count;
            }
        }

        public async Task chunk(int count, Func<DataTable, Task> callback)
        {
            await chunk(count, async chunk =>
            {
                await callback(chunk);
                return true;
            });
        }

        public async Task chunkById(int count, Func<DataTable, Task<bool>> callback)
        {
            var lastId = 0;
            while (true)
            {
                var results = await where("id", ">", lastId).orderBy("id").take(count).get();
                if (results.Rows.Count == 0)
                {
                    break;
                }

                var continueProcessing = await callback(results);
                if (!continueProcessing)
                {
                    break;
                }

                lastId = Convert.ToInt32(results.Rows[results.Rows.Count - 1]["id"]);
            }
        }

        public async Task chunkById(int count, Func<DataTable, Task> callback)
        {
            await chunkById(count, async chunk =>
            {
                await callback(chunk);
                return true;
            });
        }

        public async Task chunkByIdDesc(int count, Func<DataTable, Task<bool>> callback)
        {
            var lastId = int.MaxValue;
            while (true)
            {
                var results = await where("id", "<", lastId).orderBy("id", "desc").take(count).get();
                if (results.Rows.Count == 0)
                {
                    break;
                }

                var continueProcessing = await callback(results);
                if (!continueProcessing)
                {
                    break;
                }

                lastId = Convert.ToInt32(results.Rows[results.Rows.Count - 1]["id"]);
            }
        }

        public async Task chunkByIdDesc(int count, Func<DataTable, Task> callback)
        {
            await chunkByIdDesc(count, async chunk =>
            {
                await callback(chunk);
                return true;
            });
        }

        public async Task<List<Dictionary<string, object>>> lazy()
        {
            var result = new List<Dictionary<string, object>>();
            await chunk(100, async chunk =>
            {
                result.AddRange(chunk.Rows.Cast<DataRow>().Select(row => row.ItemArray.ToDictionary(item => item.ToString(), item => (object)item)));
                return true;
            });
            return result;
        }

        public async Task<List<Dictionary<string, object>>> lazyById()
        {
            var result = new List<Dictionary<string, object>>();
            await chunkById(100, async chunk =>
            {
                result.AddRange(chunk.Rows.Cast<DataRow>().Select(row => row.ItemArray.ToDictionary(item => item.ToString(), item => (object)item)));
                return true;
            });
            return result;
        }

        public async Task<List<Dictionary<string, object>>> lazyByIdDesc()
        {
            var result = new List<Dictionary<string, object>>();
            await chunkByIdDesc(100, async chunk =>
            {
                result.AddRange(chunk.Rows.Cast<DataRow>().Select(row => row.ItemArray.ToDictionary(item => item.ToString(), item => (object)item)));
                return true;
            });
            return result;
        }

        public async Task<int> count()
        {
            var query = $"SELECT COUNT(*) FROM {_tableName} {buildWhere()}";
            return await _dbExtension.scalar<int>(query);
        }

        public async Task<decimal> max(string column)
        {
            var query = $"SELECT MAX({column}) FROM {_tableName} {buildWhere()}";
            return await _dbExtension.scalar<decimal>(query);
        }

        public async Task<decimal> min(string column)
        {
            var query = $"SELECT MIN({column}) FROM {_tableName} {buildWhere()}";
            return await _dbExtension.scalar<decimal>(query);
        }

        public async Task<decimal> avg(string column)
        {
            var query = $"SELECT AVG({column}) FROM {_tableName} {buildWhere()}";
            return await _dbExtension.scalar<decimal>(query);
        }

        public async Task<decimal> sum(string column)
        {
            var query = $"SELECT SUM({column}) FROM {_tableName} {buildWhere()}";
            return await _dbExtension.scalar<decimal>(query);
        }

        public async Task<bool> exists()
        {
            var query = $"SELECT EXISTS(SELECT 1 FROM {_tableName} {buildWhere()} LIMIT 1)";
            return await _dbExtension.scalar<bool>(query);
        }

        public async Task<bool> doesntExist()
        {
            return !(await exists());
        }

        public async Task<int> update(Dictionary<string, object> values)
        {
            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();

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

            return await command.ExecuteNonQueryAsync();
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

            queryBuilder.Append(buildWhere());

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

        private string buildWhere()
        {
            if (_whereConditions.Any())
            {
                return " WHERE " + string.Join(" AND ", _whereConditions);
            }
            return string.Empty;
        }

        private string BindingsToSql(string sql, params object[] bindings)
        {
            if (bindings == null || bindings.Length == 0)
            {
                return sql;
            }

            for (int i = 0; i < bindings.Length; i++)
            {
                sql = sql.Replace("?", $"'{bindings[i]}'");
            }

            return sql;
        }
    }
}
