using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Data;

namespace Relfost.Database.Query
{
    public partial class Builder
    {
        public Builder select(params string[] columns)
        {
            _selectColumns.AddRange(columns);
            return this;
        }

        public Builder addSelect(params string[] columns)
        {
            _selectColumns.AddRange(columns);
            return this;
        }

        public Builder distinct()
        {
            if (!_selectColumns.Contains("DISTINCT"))
            {
                _selectColumns.Insert(0, "DISTINCT");
            }
            return this;
        }
    }
}
