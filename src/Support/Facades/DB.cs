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

        public static Builder table(string tableName)
        {
            return _dbManager.table(tableName);
        }

        public static async Task<T> scalar<T>(string query, string dbType = null)
        {
            return await _dbManager.scalar<T>(query, dbType);
        }

        public static async Task createTable(string tableName, Dictionary<string, string> columns)
        {
            await _dbManager.createTable(tableName, columns);
        }

        public static async Task<bool> tableExists(string tableName)
        {
            return await _dbManager.tableExists(tableName);
        }

        public static async Task addIndex(string tableName, string indexName, params string[] columns)
        {
            await _dbManager.addIndex(tableName, indexName, columns);
        }

        public static async Task dropIndex(string tableName, string indexName)
        {
            await _dbManager.dropIndex(tableName, indexName);
        }

        public static void updateConfig(DatabaseManager.Configuration newConfig)
        {
            _dbManager.UpdateConfig(newConfig);
        }

        public static void updateSettings(DatabaseManager.Settings newSettings)
        {
            _dbManager.UpdateSettings(newSettings);
        }

        public static void clearLogs()
        {
            _dbManager.ClearLogs();
        }
    }
}
