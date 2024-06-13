
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

Simply place the contents of the `build/carbon` folder into the `carbon` folder of your project. No additional downloads are required.

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

### Important Note

If you encounter any issues with the configuration, simply delete the `config.yaml` file. The `DatabaseQueryBuilder` will automatically regenerate the configuration file with default settings upon initialization.

### Basic Example

Here is an example of how to use the `Relfost.Database` as a plugin within the Carbon framework:

```csharp
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Relfost.Support.Facades;
using Carbon.Plugins;

namespace Carbon.Plugins
{
    [Info("RelfostDatabasePlugin", "YourName", "1.0.0")]
    [Description("A plugin to demonstrate the Relfost.Database")]
    public class RelfostDatabasePlugin : CarbonPlugin
    {
        private void OnServerInitialized()
        {
            PrintUsers().ConfigureAwait(false);
        }

        private async Task PrintUsers()
        {
            try
            {
                var users = await DB.table("users").limit(20).get();

                foreach (DataRow user in users.Rows)
                {
                    Puts($"{user["id"]}: {user["name"]} - Active: {user["active"]}");
                }
            }
            catch (Exception ex)
            {
                Puts($"Error fetching users: {ex.Message}");
            }
        }
    }
}
```

## Documentation

For detailed information and advanced usage, refer to the [Database Query Builder Documentation](docs/Database.Query.Builder.md).

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License.
