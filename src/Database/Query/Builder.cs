// Файл Database/Query/Builder.cs
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
    public partial class Builder
    {
        private readonly DatabaseManager _dbExtension;
        private readonly string _tableName;
        private readonly List<string> _selectColumns;
        private readonly List<string> _whereConditions;
        private readonly List<object> _whereParameters;
        private readonly List<string> _joinStatements;
        private readonly List<string> _groupByColumns;
        private readonly List<string> _havingConditions;
        private readonly List<object> _havingParameters;
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
            _whereParameters = new List<object>();
            _joinStatements = new List<string>();
            _groupByColumns = new List<string>();
            _havingConditions = new List<string>();
            _havingParameters = new List<object>();
            _orderByStatements = new List<string>();
            _limit = null;
            _offset = null;
            _lockMode = null;
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
                queryBuilder.Append(string.Join(", ", _orderByStatements));
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
    }
}
