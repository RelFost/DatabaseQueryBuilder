using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Relfost.Database;
using Relfost.Database.Query;

namespace Relfost.Support.Facades
{
    public static class DB
    {
        private static readonly DatabaseManager dbManager = new DatabaseManager();

        public static Builder table(string tableName)
        {
            return dbManager.table(tableName);
        }

        public static void updateConfig(DatabaseManager.Configuration newConfig)
        {
            dbManager.UpdateConfig(newConfig);
        }

        public static void updateSettings(DatabaseManager.Settings newSettings)
        {
            dbManager.UpdateSettings(newSettings);
        }

        public static void clearLogs()
        {
            dbManager.ClearLogs();
        }
    }
}
