# Database: Query Builder

#### \# [Introduction](#introduction)
#### \# [Running Database Queries](#running-database-queries)
##### > \# [Chunking Results](#chunking-results)
##### > \# [Streaming Results Lazily](#streaming-results-lazily)
##### > \# [Aggregates](#aggregates)
#### \# [Select Statements](#select-statements)
#### \# [Raw Expressions](#raw-expressions)
#### \# [Joins](#joins)

## Introduction

The query builder provides a convenient, fluent interface for creating and running database queries. It can be used to perform most database operations in your application and works perfectly with all supported database systems.

The query builder uses parameter binding to protect your application against SQL injection attacks. There is no need to clean or sanitize strings passed to the query builder as query bindings.

## Running Database Queries

### Retrieving All Rows From a Table

You may use the `table` method provided by the `DB` facade to begin a query. The `table` method returns a fluent query builder instance for the given table, allowing you to chain more constraints onto the query and then finally retrieve the results of the query using the `get` method:

```csharp
using System;
using System.Data;
using System.Threading.Tasks;
using Relfost.Support.Facades;
using Carbon.Plugins;

namespace Carbon.Plugins
{
    [Info("Template", "AuthorName", "1.0.0")]
    [Description("Plugin to test database functionality.")]
    public class Template : CarbonPlugin
    {
        private void OnServerInitialized()
        {
            Puts("Template Initializated");
            PrintUsers().ConfigureAwait(false);
        }

        private async Task PrintUsers()
        {
            DataTable users = await DB.table("users").limit(10).get();

            foreach (DataRow user in users.Rows)
            {
                Puts($"{user["id"]}: {user["name"]} - Active: {user["active"]}");
            }
        }
    }
}
```

### Retrieving a Single Row / Column From a Table

If you just need to retrieve a single row from the database, you may use the `first` method. This method will return a single `DataRow` instance:

```csharp
DataRow user = await DB.table('users').where('name', 'John').first();
```

If you don't even need an entire row, you may extract a single value from a record using the `value` method. This method will return the value of the column directly:

```csharp
var email = await DB.table('users').where('name', 'John').value('email');
```

To retrieve a list of column values, you may use the `pluck` method. For example, this method will retrieve a list of user names:

```csharp
var names = await DB.table('users').pluck('name');
```

You may also specify more than one column that you wish to retrieve:

```csharp
var users = await DB.table('users').pluck('name', 'email');
```

### Chunking Results

If you need to work with thousands of database records, consider using the `chunk` method. This method retrieves a small chunk of the results at a time and feeds each chunk into a `Closure` for processing. Here's an example of processing records in chunks of 100:

```csharp
await DB.table('users').orderBy('id').chunk(100, async (users) => {
    foreach (var user in users.Rows)
    {
        // Process the records...
    }
});
```

If you are updating database records while chunking results, your chunk results could change unexpectedly. So, when updating records while chunking, it is always best to use the `chunkById` method. This method will automatically paginate the results based on the primary key:

```csharp
await DB.table('users').where('active', false).chunkById(100, async (users) => {
    foreach (var user in users.Rows)
    {
        await DB.table('users').where('id', user['id']).update(new Dictionary<string, object> {
            { 'active', true }
        });
    }
});
```

To stop processing the additional chunks, you may return `false` from the `Closure`:

```csharp
await DB.table('users').orderBy('id').chunk(100, async (users) => {
    // Process the records...
    return false; // Stop further chunk processing
});
```

### Aggregates

The query builder also provides a variety of aggregate methods, such as `count`, `max`, `min`, `avg`, and `sum`:

```csharp
var users = await DB.table('users').count();
var price = await DB.table('orders').max('price');
var price = await DB.table('orders').where('finalized', 1).avg('price');
```

### Determining If Records Exist

Instead of using the `count` method to determine if any records exist that match your query's constraints, you may use the `exists` and `doesntExist` methods:

```csharp
if (await DB.table('users').where('finalized', 1).exists())
{
    // ...
}

if (await DB.table('users').where('finalized', 1).doesntExist())
{
    // ...
}
```

### Selects

The `select` method allows you to specify the `select` clause for the query:

```csharp
var users = await DB.table('users').select('name', 'email as user_email').get();
```

The `distinct` method allows you to force the query to return distinct results:

```csharp
var users = await DB.table('users').distinct().get();
```

If you already have a query builder instance and you wish to add a column to its existing select clause, you may use the `addSelect` method:

```csharp
var query = DB.table('users').select('name');
var users = await query.addSelect('age').get();
```

### Raw Expressions

Sometimes you may need to use a raw expression in a query. To create a raw expression, you may use the `DB.raw` method:

```csharp
var users = await DB.table('users').select(DB.raw('count(*) as user_count, status')).where('status', '<>', 1).groupBy('status').get();
```

You can also use the following methods to insert raw expressions into various parts of your query:

#### `selectRaw`

```csharp
var orders = await DB.table('orders').selectRaw('price * ? as price_with_tax', new object[] { 1.0825 }).get();
```

#### `whereRaw` / `orWhereRaw`

```csharp
var orders = await DB.table('orders').whereRaw('price > IF(state = "TX", ?, 100)', new object[] { 200 }).get();
```

#### `havingRaw` / `orHavingRaw`

```csharp
var orders = await DB.table('orders').select('department', DB.raw('SUM(price) as total_sales')).groupBy('department').havingRaw('SUM(price) > ?', new object[] { 2500 }).get();
```

#### `orderByRaw`

```csharp
var orders = await DB.table('orders').orderByRaw('updated_at - created_at DESC').get();
```

#### `groupByRaw`

```csharp
var orders = await DB.table('orders').select('city', 'state').groupByRaw('city, state').get();
```

### Advanced Join Clauses

Joining multiple tables is a breeze. Just use the `join` method. The basic usage is:

```csharp
var users = await DB.table('users').join('contacts', 'users.id', '=', 'contacts.user_id').select('users.*', 'contacts.phone').get();
```

For a left join, you can use the `leftJoin` method, and for a right join, the `rightJoin` method:

```csharp
var users = await DB.table('users').leftJoin('posts', 'users.id', '=', 'posts.user_id').get();
var users = await DB.table('users').rightJoin('posts', 'users.id', '=', 'posts.user_id').get();
```

For a cross join, you can use the `crossJoin` method:

```csharp
var users = await DB.table('users').crossJoin('colors').get();
```

