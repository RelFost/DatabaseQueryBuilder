// Файл Database/Query/BuilderAggregate.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Relfost.Database.Query
{
    public partial class Builder
    {
        public async Task<int> count()
        {
            var result = await scalar<int>("SELECT COUNT(*) FROM " + _tableName, new List<object>());
            return result;
        }

        public async Task<T> max<T>(params string[] columns)
        {
            var columnList = string.Join(", ", columns.Select(col => $"MAX({col})"));
            var result = await scalar<T>($"SELECT {columnList} FROM " + _tableName, new List<object>());
            return result;
        }

        public async Task<T> min<T>(params string[] columns)
        {
            var columnList = string.Join(", ", columns.Select(col => $"MIN({col})"));
            var result = await scalar<T>($"SELECT {columnList} FROM " + _tableName, new List<object>());
            return result;
        }

        public async Task<T> avg<T>(params string[] columns)
        {
            var columnList = string.Join(", ", columns.Select(col => $"AVG({col})"));
            var result = await scalar<T>($"SELECT {columnList} FROM " + _tableName, new List<object>());
            return result;
        }

        public async Task<T> sum<T>(params string[] columns)
        {
            var columnList = string.Join(", ", columns.Select(col => $"SUM({col})"));
            var result = await scalar<T>($"SELECT {columnList} FROM " + _tableName, new List<object>());
            return result;
        }

        public async Task<bool> exists()
        {
            var result = await scalar<int>($"SELECT 1 FROM {_tableName} WHERE {_whereConditions.First()} LIMIT 1", _whereParameters);
            return result > 0;
        }

        public async Task<bool> doesntExist()
        {
            var result = await scalar<int>($"SELECT 1 FROM {_tableName} WHERE {_whereConditions.First()} LIMIT 1", _whereParameters);
            return result == 0;
        }
    }
}
