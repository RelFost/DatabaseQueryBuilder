using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Relfost.Database;
using Relfost.Database.Query;

namespace Relfost.Support.Facades
{
    public static class DB
    {
        private static readonly DatabaseManager _dbManager = new DatabaseManager();

        public static Builder Table(string tableName)
        {
            return _dbManager.table(tableName);
        }

        public static async Task<T> Scalar<T>(string query, string dbType = null)
        {
            return await _dbManager.scalar<T>(query, dbType);
        }

        public static async Task CreateTable(string tableName, Dictionary<string, string> columns)
        {
            await _dbManager.createTable(tableName, columns);
        }

        public static async Task<bool> TableExists(string tableName)
        {
            return await _dbManager.tableExists(tableName);
        }

        public static async Task AddIndex(string tableName, string indexName, params string[] columns)
        {
            await _dbManager.addIndex(tableName, indexName, columns);
        }

        public static async Task DropIndex(string tableName, string indexName)
        {
            await _dbManager.dropIndex(tableName, indexName);
        }

        public static void UpdateConfig(DatabaseManager.Configuration newConfig)
        {
            _dbManager.UpdateConfig(newConfig);
        }

        public static void UpdateSettings(DatabaseManager.Settings newSettings)
        {
            _dbManager.UpdateSettings(newSettings);
        }

        public static void ClearLogs()
        {
            _dbManager.ClearLogs();
        }
    }
}
