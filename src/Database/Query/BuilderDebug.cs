// Файл Database/Query/BuilderDebug.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Relfost.Database.Query
{
    public partial class Builder
    {
        public Builder dd()
        {
            dump();
            Environment.Exit(0);
            return this;
        }

        public Builder dump()
        {
            Console.WriteLine(buildQuery());
            Console.WriteLine(JsonConvert.SerializeObject(_whereParameters));
            return this;
        }

        public Builder ddRawSql()
        {
            dumpRawSql();
            Environment.Exit(0);
            return this;
        }

        public Builder dumpRawSql()
        {
            var sql = buildQuery();
            for (int i = 0; i < _whereParameters.Count; i++)
            {
                sql = sql.Replace($"@param{i}", _whereParameters[i]?.ToString() ?? "NULL");
            }
            Console.WriteLine(sql);
            return this;
        }
    }
}
