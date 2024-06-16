// Файл Database/Query/BuilderIterator.cs
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
        public async Task chunk(int count, Func<DataTable, Task<bool>> callback)
        {
            int offset = 0;
            while (true)
            {
                var chunkData = await limit(count).offset(offset).get();
                if (chunkData.Rows.Count == 0) break;

                var continueProcessing = await callback(chunkData);
                if (!continueProcessing) break;

                offset += count;
            }
        }

        public async Task chunk(int count, Func<DataTable, Task> callback)
        {
            await chunk(count, async data =>
            {
                await callback(data);
                return true;
            });
        }

        public async Task chunkById(int count, Func<DataTable, Task<bool>> callback, string orderBy = "ASC")
        {
            object lastId = null;
            while (true)
            {
                var query = limit(count);
                if (lastId != null)
                {
                    query = query.where("id", orderBy == "ASC" ? ">" : "<", lastId);
                }
                var chunkData = await query.orderBy("id " + orderBy).get();
                if (chunkData.Rows.Count == 0) break;

                var continueProcessing = await callback(chunkData);
                if (!continueProcessing) break;

                lastId = chunkData.Rows[chunkData.Rows.Count - 1]["id"];
            }
        }

        public async Task chunkById(int count, Func<DataTable, Task> callback, string orderBy = "ASC")
        {
            await chunkById(count, async data =>
            {
                await callback(data);
                return true;
            }, orderBy);
        }

        public async Task chunkByIdDesc(int count, Func<DataTable, Task<bool>> callback)
        {
            await chunkById(count, callback, "DESC");
        }

        public async Task chunkByIdDesc(int count, Func<DataTable, Task> callback)
        {
            await chunkById(count, async data =>
            {
                await callback(data);
                return true;
            }, "DESC");
        }

        public async Task lazy(Func<DataRow, Task> callback)
        {
            await chunk(int.MaxValue, async data =>
            {
                foreach (DataRow row in data.Rows)
                {
                    await callback(row);
                }
                return true;
            });
        }

        public async Task lazyById(Func<DataRow, Task> callback)
        {
            await chunkById(int.MaxValue, async data =>
            {
                foreach (DataRow row in data.Rows)
                {
                    await callback(row);
                }
                return true;
            });
        }

        public async Task lazyByIdDesc(Func<DataRow, Task> callback)
        {
            await chunkByIdDesc(int.MaxValue, async data =>
            {
                foreach (DataRow row in data.Rows)
                {
                    await callback(row);
                }
                return true;
            });
        }
    }
}
