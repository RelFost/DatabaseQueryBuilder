using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Relfost.Database;
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

        public Builder where(string column, string operation, object value)
        {
            _whereConditions.Add($"{column} {operation} '{value}'");
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
            var dataTable = await get();
            return dataTable.Rows.Cast<DataRow>().Select(row => row.ItemArray).ToArray();
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

        public async Task chunk(int count, Func<DataTable, Task<bool>> callback)
        {
            int offset = 0;
            while (true)
            {
                var chunkData = await limit(count).offset(offset).get();
                if (chunkData.Rows.Count == 0) break;

                var continueProcessing = await callback(chunkData);
                if (!continueProcessing) break;

                offset += count;
            }
        }

        public async Task chunk(int count, Func<DataTable, Task> callback)
        {
            await chunk(count, async data =>
            {
                await callback(data);
                return true;
            });
        }

        public async Task chunkById(int count, Func<DataTable, Task<bool>> callback)
        {
            object lastId = null;
            while (true)
            {
                var query = limit(count);
                if (lastId != null)
                {
                    query = query.where("id", ">", lastId);
                }
                var chunkData = await query.orderBy("id").get();
                if (chunkData.Rows.Count == 0) break;

                var continueProcessing = await callback(chunkData);
                if (!continueProcessing) break;

                lastId = chunkData.Rows[chunkData.Rows.Count - 1]["id"];
            }
        }

        public async Task chunkById(int count, Func<DataTable, Task> callback)
        {
            await chunkById(count, async data =>
            {
                await callback(data);
                return true;
            });
        }

        public async Task<int> count()
        {
            var result = await scalar<int>("SELECT COUNT(*) FROM " + _tableName);
            return result;
        }

        public async Task<T> max<T>(string column)
        {
            var result = await scalar<T>($"SELECT MAX({column}) FROM " + _tableName);
            return result;
        }

        public async Task<T> min<T>(string column)
        {
            var result = await scalar<T>($"SELECT MIN({column}) FROM " + _tableName);
            return result;
        }

        public async Task<T> avg<T>(string column)
        {
            var result = await scalar<T>($"SELECT AVG({column}) FROM " + _tableName);
            return result;
        }

        public async Task<T> sum<T>(string column)
        {
            var result = await scalar<T>($"SELECT SUM({column}) FROM " + _tableName);
            return result;
        }

        public async Task<bool> exists()
        {
            var result = await count();
            return result > 0;
        }

        public async Task<bool> doesntExist()
        {
            var result = await count();
            return result == 0;
        }

        public Builder addSelect(params string[] columns)
        {
            _selectColumns.AddRange(columns);
            return this;
        }

        public Builder distinct()
        {
            if (!_selectColumns.Contains("DISTINCT"))
            {
                _selectColumns.Insert(0, "DISTINCT");
            }
            return this;
        }

        public Builder raw(string sql, params object[] bindings)
        {
            var sqlWithBindings = BindingsToSql(sql, bindings);
            _selectColumns.Add(sqlWithBindings);
            return this;
        }

        public Builder selectRaw(string sql, params object[] bindings)
        {
            var sqlWithBindings = BindingsToSql(sql, bindings);
            _selectColumns.Add(sqlWithBindings);
            return this;
        }

        public Builder whereRaw(string sql, params object[] bindings)
        {
            var sqlWithBindings = BindingsToSql(sql, bindings);
            _whereConditions.Add(sqlWithBindings);
            return this;
        }

        public Builder orWhereRaw(string sql, params object[] bindings)
        {
            var sqlWithBindings = BindingsToSql(sql, bindings);
            _whereConditions.Add($"OR {sqlWithBindings}");
            return this;
        }

        public Builder havingRaw(string sql, params object[] bindings)
        {
            var sqlWithBindings = BindingsToSql(sql, bindings);
            _havingConditions.Add(sqlWithBindings);
            return this;
        }

        public Builder orHavingRaw(string sql, params object[] bindings)
        {
            var sqlWithBindings = BindingsToSql(sql, bindings);
            _havingConditions.Add($"OR {sqlWithBindings}");
            return this;
        }

        public Builder orderByRaw(string sql, params object[] bindings)
        {
            var sqlWithBindings = BindingsToSql(sql, bindings);
            _orderByStatements.Add(sqlWithBindings);
            return this;
        }

        public Builder groupByRaw(string sql, params object[] bindings)
        {
            var sqlWithBindings = BindingsToSql(sql, bindings);
            _groupByColumns.Add(sqlWithBindings);
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

        public async Task lazy(Func<DataRow, Task> callback)
        {
            await chunk(int.MaxValue, async data =>
            {
                foreach (DataRow row in data.Rows)
                {
                    await callback(row);
                }
                return true;
            });
        }

        public async Task lazyById(Func<DataRow, Task> callback)
        {
            await chunkById(int.MaxValue, async data =>
            {
                foreach (DataRow row in data.Rows)
                {
                    await callback(row);
                }
                return true;
            });
        }

        public async Task lazyByIdDesc(Func<DataRow, Task> callback)
        {
            await chunkByIdDesc(int.MaxValue, async data =>
            {
                foreach (DataRow row in data.Rows)
                {
                    await callback(row);
                }
                return true;
            });
        }

        public async Task chunkByIdDesc(int count, Func<DataTable, Task<bool>> callback)
        {
            object lastId = null;
            while (true)
            {
                var query = limit(count);
                if (lastId != null)
                {
                    query = query.where("id", "<", lastId);
                }
                var chunkData = await query.orderBy("id DESC").get();
                if (chunkData.Rows.Count == 0) break;

                var continueProcessing = await callback(chunkData);
                if (!continueProcessing) break;

                lastId = chunkData.Rows[chunkData.Rows.Count - 1]["id"];
            }
        }

        public async Task chunkByIdDesc(int count, Func<DataTable, Task> callback)
        {
            await chunkByIdDesc(count, async data =>
            {
                await callback(data);
                return true;
            });
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

        private async Task<T> scalar<T>(string query)
        {
            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = query;

            var result = await command.ExecuteScalarAsync();
            return (T)Convert.ChangeType(result, typeof(T));
        }
    }
}
