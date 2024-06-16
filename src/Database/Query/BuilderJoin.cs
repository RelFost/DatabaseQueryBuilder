// Файл Database/Query/BuilderJoin.cs
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
        public Builder join(string table, Action<JoinClause> joinAction)
        {
            var joinClause = new JoinClause(_dbExtension, table);
            joinAction(joinClause);
            _joinStatements.Add(joinClause.Build());
            return this;
        }

        public Builder leftJoin(string table, Action<JoinClause> joinAction)
        {
            var joinClause = new JoinClause(_dbExtension, table, "LEFT");
            joinAction(joinClause);
            _joinStatements.Add(joinClause.Build());
            return this;
        }

        public Builder rightJoin(string table, Action<JoinClause> joinAction)
        {
            var joinClause = new JoinClause(_dbExtension, table, "RIGHT");
            joinAction(joinClause);
            _joinStatements.Add(joinClause.Build());
            return this;
        }

        public Builder crossJoin(string table)
        {
            _joinStatements.Add($"CROSS JOIN {table}");
            return this;
        }
    }

    public class JoinClause
    {
        private readonly DatabaseManager _dbExtension;
        private readonly string _table;
        private readonly string _type;
        private readonly List<string> _conditions;

        public JoinClause(DatabaseManager dbExtension, string table, string type = "INNER")
        {
            _dbExtension = dbExtension;
            _table = table;
            _type = type;
            _conditions = new List<string>();
        }

        public JoinClause on(string first, string operation, string second)
        {
            _conditions.Add($"{first} {operation} {second}");
            return this;
        }

        public JoinClause orOn(string first, string operation, string second)
        {
            if (_conditions.Any())
            {
                _conditions[^1] = $"({_conditions[^1]} OR {first} {operation} {second})";
            }
            else
            {
                _conditions.Add($"{first} {operation} {second}");
            }
            return this;
        }

        public JoinClause where(string column, string operation, object value)
        {
            _conditions.Add($"{column} {operation} @param{_conditions.Count}");
            return this;
        }

        public string Build()
        {
            return $"{_type} JOIN {_table} ON {string.Join(" AND ", _conditions)}";
        }
    }
}
