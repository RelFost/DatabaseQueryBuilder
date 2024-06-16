// Файл Database/Schema/Builder.cs
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Relfost.Database;

namespace Relfost.Database.Schema
{
    public class Builder
    {
        private readonly DatabaseManager _dbExtension;

        public Builder(DatabaseManager dbExtension)
        {
            _dbExtension = dbExtension;
        }

        public async Task create(string tableName, Action<Blueprint> table)
        {
            var blueprint = new Blueprint(tableName);
            table(blueprint);

            var sql = blueprint.buildCreateTableSql();
            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> hasTable(string tableName)
        {
            var sql = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{tableName}'";
            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        public async Task<bool> hasColumn(string tableName, string columnName)
        {
            var sql = $"SELECT COUNT(*) FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = '{columnName}'";
            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        public async Task<bool> hasIndex(string tableName, IEnumerable<string> columns, string indexType = "index")
        {
            var columnsList = string.Join(", ", columns.Select(col => $"'{col}'"));
            var sql = $"SELECT COUNT(*) FROM information_schema.statistics WHERE table_name = '{tableName}' AND index_name = '{indexType}' AND column_name IN ({columnsList})";
            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        public Builder connection(string connectionName)
        {
            var connection = _dbExtension.config.connections[connectionName];
            _dbExtension.currentDatabase = connection;
            return this;
        }

        public async Task drop(string tableName)
        {
            var sql = $"DROP TABLE {tableName}";
            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        public async Task dropIfExists(string tableName)
        {
            var sql = $"DROP TABLE IF EXISTS {tableName}";
            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        public async Task rename(string from, string to)
        {
            var sql = $"ALTER TABLE {from} RENAME TO {to}";
            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        public async Task table(string tableName, Action<Blueprint> table)
        {
            var blueprint = new Blueprint(tableName);
            table(blueprint);

            var sql = blueprint.buildAlterTableSql();
            using var connection = _dbExtension.getConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
    }

    public class Blueprint
    {
        private readonly string _tableName;
        private readonly List<string> _columns;
        private readonly List<string> _indexes;
        private readonly List<string> _primaryKeys;
        private readonly List<string> _foreignKeys;
        private readonly List<string> _uniques;

        public Blueprint(string tableName)
        {
            _tableName = tableName;
            _columns = new List<string>();
            _indexes = new List<string>();
            _primaryKeys = new List<string>();
            _foreignKeys = new List<string>();
            _uniques = new List<string>();
        }

        public void id()
        {
            _columns.Add("id BIGSERIAL PRIMARY KEY");
        }

        public void varchar(string columnName, int length = 255)
        {
            _columns.Add($"{columnName} VARCHAR({length})");
        }

        public void timestamps()
        {
            _columns.Add("created_at TIMESTAMP");
            _columns.Add("updated_at TIMESTAMP");
        }

        public void engine(string engine)
        {
            // Handle engine specification
        }

        public void charset(string charset)
        {
            // Handle charset specification
        }

        public void collation(string collation)
        {
            // Handle collation specification
        }

        public void temporary()
        {
            // Handle temporary table
        }

        public void comment(string comment)
        {
            // Handle table comment
        }

        public string buildCreateTableSql()
        {
            var columnsSql = string.Join(", ", _columns);
            return $"CREATE TABLE {_tableName} ({columnsSql})";
        }

        public string buildAlterTableSql()
        {
            var columnsSql = string.Join(", ", _columns);
            return $"ALTER TABLE {_tableName} ADD ({columnsSql})";
        }
    }
}
