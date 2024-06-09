# Database Query Builder

This is a versatile and robust database query builder for .NET applications. It supports PostgreSQL, MySQL, MariaDB, and SQLite. The library provides an easy-to-use interface for building and executing complex SQL queries.

## Features

- Supports PostgreSQL, MySQL, MariaDB, and SQLite
- Fluent API for building queries
- JSON querying capabilities
- Supports complex query conditions (AND, OR, NOT)
- Supports JOINs, GROUP BY, HAVING, ORDER BY, LIMIT, and OFFSET
- Supports schema selection for PostgreSQL

## Installation

Simply place the contents of the `build` folder into the `carbon` folder of your project. No additional downloads are required.

## Usage

### Configuration

The configuration file (`config.yaml`) should be placed in the appropriate directory as specified in the `DatabaseQueryBuilder.cs`. The file should have the following structure:

```yaml
# Default database connection to use
default: pgsql

# Database connections configuration
connections:
  pgsql:
    driver: pgsql
    host: localhost
    port: 5432
    database: postgres
    username: postgres
    password: postgres
  mysql:
    driver: mysql
    host: localhost
    port: 3306
    database: database
    username: root
    password: ''
    charset: utf8mb4
    collation: utf8mb4_unicode_ci
    prefix: ''
    prefixIndexes: true
    strict: true
    sslmode: preferred
  sqlite:
    driver: sqlite
    database: database.sqlite
    foreignKeyConstraints: true
  mariadb:
    driver: mariadb
    host: localhost
    port: 3306
    database: database
    username: root
    password: ''
    charset: utf8mb4
    collation: utf8mb4_unicode_ci
    prefix: ''
    prefixIndexes: true
    strict: true
    sslmode: preferred
```

Important Note
If you encounter any issues with the configuration, simply delete the config.yaml file. The DatabaseQueryBuilder will automatically regenerate the configuration file with default settings upon initialization.

Basic Example
Here is an example of how to use the DatabaseQueryBuilder as a plugin within the Carbon framework:

```csharp
namespace Carbon.Plugins
{
    [Info("DatabaseQueryPlugin", "YourName", "1.0.0")]
    [Description("A plugin to demonstrate the DatabaseQueryBuilder")]
    public class DatabaseQueryPlugin : CarbonPlugin
    {
        private DatabaseQueryBuilder db;

        private void OnServerInitialized()
        {
            Puts("Hello world!");

            db = new DatabaseQueryBuilder();

            // Selecting data
            var users = db.table("users")
                .select("id", "name", "email")
                .where("age", ">", 20)
                .orderBy("name")
                .get();

            // Inserting data
            var newUser = new Dictionary<string, object>
            {
                {"name", "John Doe"},
                {"email", "john.doe@example.com"},
                {"age", 30}
            };
            db.table("users").insert(newUser);

            // Updating data
            var updates = new Dictionary<string, object>
            {
                {"email", "john.newemail@example.com"}
            };
            db.table("users")
                .where("name", "=", "John Doe")
                .update(updates);

            // Deleting data
            db.table("users")
                .where("age", "<", 20)
                .delete();
        }
    }
}
```
Advanced Usage

JSON Querying
```csharp
var users = db.table("users")
    .select("id", "name", "data")
    .whereJson("data", "address.city", "New York")
    .get();
```
Transactions
```csharp
using (var transaction = db.beginTransaction())
{
    try
    {
        db.table("users").insert(newUser);
        db.table("profiles").insert(newProfile);
        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```
Contributing
Contributions are welcome! Please feel free to submit a Pull Request.

License
This project is licensed under the MIT License.

```markdown

## Documentation

### DatabaseQueryBuilder.cs

This class provides methods to build and execute SQL queries. It supports different database types and includes a fluent API for creating queries.

#### Methods

- `DatabaseQueryBuilder()`: Initializes the query builder, loading configuration and setting up the database connection.
- `schema(string schema)`: Sets the schema for PostgreSQL databases.
- `scalar<T>(string query, string dbType = null)`: Executes a scalar query and returns the result.
- `table(string tableName)`: Returns a new `QueryBuilder` instance for the specified table.
- `getConnection(string dbType = null)`: Gets a connection object for the specified database type.
- `buildConnectionString(Configuration.DatabaseInfo databaseInfo)`: Builds a connection string for the specified database information.

### QueryBuilder

This class provides methods for building SQL queries.

#### Methods

- `select(params string[] columns)`: Adds columns to the SELECT clause.
- `where(string column, string operation, object value)`: Adds a WHERE condition.
- `orWhere(Action<QueryBuilder> query)`: Adds an OR WHERE condition.
- `whereNot(Action<QueryBuilder> query)`: Adds a WHERE NOT condition.
- `whereAny(string[] columns, string operation, object value)`: Adds a WHERE condition for any of the specified columns.
- `whereAll(string[] columns, string operation, object value)`: Adds a WHERE condition for all of the specified columns.
- `whereJson(string column, string path, object value)`: Adds a JSON query condition.
- `join(string table, string first, string operation, string second)`: Adds a JOIN clause.
- `groupBy(params string[] columns)`: Adds a GROUP BY clause.
- `having(string column, string operation, object value)`: Adds a HAVING condition.
- `orderBy(string column, string direction = "asc")`: Adds an ORDER BY clause.
- `limit(int value)`: Adds a LIMIT clause.
- `offset(int value)`: Adds an OFFSET clause.
- `get()`: Executes the built query and returns the result as a `DataTable`.
- `insert(Dictionary<string, object> values)`: Inserts a new record.
- `update(Dictionary<string, object> values)`: Updates existing records.
- `delete()`: Deletes records matching the query.
- `truncate()`: Truncates the table.

### Configuration

The `Configuration` class is used to load and manage database connection settings.

#### Properties

- `default`: The default database type.
- `connections`: A dictionary of database connection settings.

### Configuration.DatabaseInfo

This nested class contains the connection details for a specific database.

#### Properties

- `driver`: The database driver (e.g., `pgsql`, `mysql`, `sqlite`, `mariadb`).
- `host`: The database host.
- `port`: The database port.
- `database`: The database name.
- `username`: The database username.
- `password`: The database password.
- `charset`: The character set.
- `collation`: The collation.
- `prefix`: The table prefix.
- `prefix_indexes`: Whether to prefix indexes.
- `strict`: Whether to use strict mode.
- `engine`: The storage engine.
- `sslmode`: The SSL mode.
- `foreign_key_constraints`: Whether to enforce foreign key constraints.
- `options`: Additional options for the database connection.

## Conclusion

This documentation provides an overview of the `DatabaseQueryBuilder` library, its features, usage, and configuration. For more detailed information and advanced usage, refer to the code comments and examples provided.

---

This completes the README and documentation for your project. If you have any further questions or need additional details, feel free to ask.
```

