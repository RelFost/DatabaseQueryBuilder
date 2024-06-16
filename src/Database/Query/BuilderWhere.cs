// Файл Database/Query/BuilderWhere.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Relfost.Database.Query
{
    public partial class Builder
    {
        // Перегрузка метода where с двумя аргументами
        public Builder where(string column, object value)
        {
            return where(column, "=", value);
        }

        // Перегрузка метода where для массива условий
        public Builder where(IEnumerable<(string column, string operation, object value)> conditions)
        {
            foreach (var condition in conditions)
            {
                where(condition.column, condition.operation, condition.value);
            }
            return this;
        }

        // Оригинальный метод where
        public Builder where(string column, string operation, object value)
        {
            _whereConditions.Add($"{column} {operation} @param{_whereParameters.Count}");
            _whereParameters.Add(value);
            return this;
        }

        // Перегрузка метода where для многократных условий в массиве
        public Builder where(List<List<object>> conditions)
        {
            foreach (var condition in conditions)
            {
                if (condition.Count == 2)
                {
                    where((string)condition[0], "=", condition[1]);
                }
                else if (condition.Count == 3)
                {
                    where((string)condition[0], (string)condition[1], condition[2]);
                }
            }
            return this;
        }

        // Метод whereNot
        public Builder whereNot(Action<Builder> query)
        {
            var subQuery = new Builder(_dbExtension, _tableName);
            query(subQuery);
            _whereConditions.Add($"NOT ({string.Join(" OR ", subQuery._whereConditions)})");
            _whereParameters.AddRange(subQuery._whereParameters);
            return this;
        }

        // Метод whereAny
        public Builder whereAny(string[] columns, string operation, object value)
        {
            var conditions = columns.Select(col => $"{col} {operation} @param{_whereParameters.Count}");
            _whereConditions.Add($"({string.Join(" OR ", conditions)})");
            _whereParameters.Add(value);
            return this;
        }

        // Метод whereAll
        public Builder whereAll(string[] columns, string operation, object value)
        {
            var conditions = columns.Select(col => $"{col} {operation} @param{_whereParameters.Count}");
            _whereConditions.Add($"({string.Join(" AND ", conditions)})");
            _whereParameters.Add(value);
            return this;
        }

        // Метод whereJson
        public Builder whereJson(string column, string path, object value)
        {
            _whereConditions.Add($"{column}->'{path}' = @param{_whereParameters.Count}");
            _whereParameters.Add(value);
            return this;
        }

        // Метод whereJsonContains
        public Builder whereJsonContains(string column, object value)
        {
            var formattedValue = value is string ? value.ToString() : JsonConvert.SerializeObject(value);
            _whereConditions.Add($"{column} @> @param{_whereParameters.Count}");
            _whereParameters.Add(formattedValue);
            return this;
        }

        // Метод whereJsonLength
        public Builder whereJsonLength(string column, string operation, int value)
        {
            _whereConditions.Add($"json_array_length({column}) {operation} @param{_whereParameters.Count}");
            _whereParameters.Add(value);
            return this;
        }

        // Метод whereBetween
        public Builder whereBetween(string column, object start, object end)
        {
            _whereConditions.Add($"{column} BETWEEN @param{_whereParameters.Count} AND @param{_whereParameters.Count + 1}");
            _whereParameters.Add(start);
            _whereParameters.Add(end);
            return this;
        }

        // Метод whereNotBetween
        public Builder whereNotBetween(string column, object start, object end)
        {
            _whereConditions.Add($"{column} NOT BETWEEN @param{_whereParameters.Count} AND @param{_whereParameters.Count + 1}");
            _whereParameters.Add(start);
            _whereParameters.Add(end);
            return this;
        }

        // Метод whereIn
        public Builder whereIn(string column, IEnumerable<object> values)
        {
            var formattedValues = string.Join(", ", values.Select((v, i) => $"@param{_whereParameters.Count + i}"));
            _whereConditions.Add($"{column} IN ({formattedValues})");
            _whereParameters.AddRange(values);
            return this;
        }

        // Метод whereIn с подзапросом
        public Builder whereIn(string column, Action<Builder> subQuery)
        {
            var subBuilder = new Builder(_dbExtension, _tableName);
            subQuery(subBuilder);
            var subQuerySql = subBuilder.buildQuery();
            _whereConditions.Add($"{column} IN ({subQuerySql})");
            _whereParameters.AddRange(subBuilder._whereParameters);
            return this;
        }

        // Метод whereNotIn
        public Builder whereNotIn(string column, IEnumerable<object> values)
        {
            var formattedValues = string.Join(", ", values.Select((v, i) => $"@param{_whereParameters.Count + i}"));
            _whereConditions.Add($"{column} NOT IN ({formattedValues})");
            _whereParameters.AddRange(values);
            return this;
        }

        // Метод whereNull
        public Builder whereNull(string column)
        {
            _whereConditions.Add($"{column} IS NULL");
            return this;
        }

        // Метод whereNotNull
        public Builder whereNotNull(string column)
        {
            _whereConditions.Add($"{column} IS NOT NULL");
            return this;
        }

        // Метод whereDate
        public Builder whereDate(string column, string operation, string date)
        {
            _whereConditions.Add($"DATE({column}) {operation} @param{_whereParameters.Count}");
            _whereParameters.Add(date);
            return this;
        }

        // Метод whereMonth
        public Builder whereMonth(string column, string operation, int month)
        {
            _whereConditions.Add($"EXTRACT(MONTH FROM {column}) {operation} @param{_whereParameters.Count}");
            _whereParameters.Add(month);
            return this;
        }

        // Метод whereDay
        public Builder whereDay(string column, string operation, int day)
        {
            _whereConditions.Add($"EXTRACT(DAY FROM {column}) {operation} @param{_whereParameters.Count}");
            _whereParameters.Add(day);
            return this;
        }

        // Метод whereYear
        public Builder whereYear(string column, string operation, int year)
        {
            _whereConditions.Add($"EXTRACT(YEAR FROM {column}) {operation} @param{_whereParameters.Count}");
            _whereParameters.Add(year);
            return this;
        }

        // Метод whereTime
        public Builder whereTime(string column, string operation, string time)
        {
            _whereConditions.Add($"EXTRACT(TIME FROM {column}) {operation} @param{_whereParameters.Count}");
            _whereParameters.Add(time);
            return this;
        }

        // Метод whereColumn
        public Builder whereColumn(string firstColumn, string operation, string secondColumn)
        {
            _whereConditions.Add($"{firstColumn} {operation} {secondColumn}");
            return this;
        }

        // Перегрузка метода whereColumn для массива условий
        public Builder whereColumn(IEnumerable<(string firstColumn, string operation, string secondColumn)> conditions)
        {
            foreach (var condition in conditions)
            {
                whereColumn(condition.firstColumn, condition.operation, condition.secondColumn);
            }
            return this;
        }

        // Метод orWhere
        public Builder orWhere(Action<Builder> query)
        {
            var subQuery = new Builder(_dbExtension, _tableName);
            query(subQuery);
            _whereConditions.Add($"({_whereConditions.Last()} OR ({string.Join(" AND ", subQuery._whereConditions)}))");
            _whereParameters.AddRange(subQuery._whereParameters);
            return this;
        }

        // Метод whereBetweenColumns
        public Builder whereBetweenColumns(string column, (string startColumn, string endColumn) columns)
        {
            _whereConditions.Add($"{column} BETWEEN {columns.startColumn} AND {columns.endColumn}");
            return this;
        }

        // Метод whereNotBetweenColumns
        public Builder whereNotBetweenColumns(string column, (string startColumn, string endColumn) columns)
        {
            _whereConditions.Add($"{column} NOT BETWEEN {columns.startColumn} AND {columns.endColumn}");
            return this;
        }

        // Метод whereExists
        public Builder whereExists(Action<Builder> subQuery)
        {
            var subBuilder = new Builder(_dbExtension, _tableName);
            subQuery(subBuilder);
            var subQuerySql = subBuilder.buildQuery();
            _whereConditions.Add($"EXISTS ({subQuerySql})");
            _whereParameters.AddRange(subBuilder._whereParameters);
            return this;
        }

        // Перегрузка метода whereExists для подзапроса
        public Builder whereExists(Builder subQuery)
        {
            var subQuerySql = subQuery.buildQuery();
            _whereConditions.Add($"EXISTS ({subQuerySql})");
            _whereParameters.AddRange(subQuery._whereParameters);
            return this;
        }

        // Метод where с подзапросом
        public Builder where(string column, string operation, Action<Builder> subQuery)
        {
            var subBuilder = new Builder(_dbExtension, _tableName);
            subQuery(subBuilder);
            var subQuerySql = subBuilder.buildQuery();
            _whereConditions.Add($"{column} {operation} ({subQuerySql})");
            _whereParameters.AddRange(subBuilder._whereParameters);
            return this;
        }

        // Метод whereFullText
        public Builder whereFullText(string column, string value)
        {
            _whereConditions.Add($"MATCH ({column}) AGAINST (@param{_whereParameters.Count})");
            _whereParameters.Add(value);
            return this;
        }

        // Метод orWhereFullText
        public Builder orWhereFullText(string column, string value)
        {
            _whereConditions.Add($"OR MATCH ({column}) AGAINST (@param{_whereParameters.Count})");
            _whereParameters.Add(value);
            return this;
        }

        private string buildWhere()
        {
            if (_whereConditions.Any())
            {
                return " WHERE " + string.Join(" AND ", _whereConditions);
            }
            return string.Empty;
        }
    }
}
