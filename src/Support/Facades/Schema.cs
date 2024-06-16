// Файл Database/Support/Facades/Schema.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Relfost.Database;
using Relfost.Database.Schema;

namespace Relfost.Support.Facades
{
    public static class Schema
    {
        private static readonly Builder schemaBuilder = new Builder(new DatabaseManager());

        public static Task create(string tableName, Action<Blueprint> table)
        {
            return schemaBuilder.create(tableName, table);
        }

        public static Task<bool> hasTable(string tableName)
        {
            return schemaBuilder.hasTable(tableName);
        }

        public static Task<bool> hasColumn(string tableName, string columnName)
        {
            return schemaBuilder.hasColumn(tableName, columnName);
        }

        public static Task<bool> hasIndex(string tableName, IEnumerable<string> columns, string indexType = "index")
        {
            return schemaBuilder.hasIndex(tableName, columns, indexType);
        }

        public static Builder connection(string connectionName)
        {
            return schemaBuilder.connection(connectionName);
        }

        public static Task drop(string tableName)
        {
            return schemaBuilder.drop(tableName);
        }

        public static Task dropIfExists(string tableName)
        {
            return schemaBuilder.dropIfExists(tableName);
        }

        public static Task rename(string from, string to)
        {
            return schemaBuilder.rename(from, to);
        }

        public static Task table(string tableName, Action<Blueprint> table)
        {
            return schemaBuilder.table(tableName, table);
        }
    }
}
