// Файл Database/Query/BuilderRaw.cs
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
