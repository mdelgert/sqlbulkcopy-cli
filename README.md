# sqlbulkcopy-cli

`sqlbulkcopy` copies rows from a SQL Server or Azure SQL `SELECT` query directly into an existing table on another server by streaming from `SqlDataReader` into `SqlBulkCopy`.

## What it does

- streams query results directly into an existing destination table
- appends by default
- optionally truncates the destination table first
- accepts any connection string supported by `Microsoft.Data.SqlClient`

Version 1 intentionally stays small:

```text
SELECT query
    ↓
SqlBulkCopy
    ↓
existing destination table
```

## Installation

Build and pack the tool:

```bash
dotnet add package Microsoft.Data.SqlClient.Extensions.Azure
dotnet pack /absolute/path/to/sqlbulkcopy-cli/src/sqlbulkcopy/sqlbulkcopy.csproj -c Release
dotnet tool install --global --add-source /absolute/path/to/sqlbulkcopy-cli/src/sqlbulkcopy/bin/Release sqlbulkcopy
```

## Building

```bash
dotnet build
```

## Usage

```bash
sqlbulkcopy \
  --source "$SOURCE_CONNECTION" \
  --query "SELECT * FROM dbo.MyView" \
  --destination "$DESTINATION_CONNECTION" \
  --table "dbo.MyTable"
```

### Append behavior

Without `--truncate`, rows are appended to the destination table.

### Truncate first

```bash
sqlbulkcopy \
  --source "$SOURCE_CONNECTION" \
  --query "SELECT Id, Name, Email FROM dbo.vwCustomerExport" \
  --destination "$DESTINATION_CONNECTION" \
  --table "dbo.CustomerImport" \
  --truncate
```

### Batch size

`--batch-size` defaults to `10000`.

```bash
sqlbulkcopy \
  --source "$SOURCE_CONNECTION" \
  --query "SELECT * FROM dbo.MyView" \
  --destination "$DESTINATION_CONNECTION" \
  --table "dbo.MyTable" \
  --batch-size 10000
```

## Environment variables

If connection strings are not supplied on the command line, `sqlbulkcopy` uses:

- `SQLCOPY_SOURCE`
- `SQLCOPY_DESTINATION`

Example:

```bash
export SQLCOPY_SOURCE="Server=tcp:source.database.windows.net,1433;Database=SourceDb;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;"
export SQLCOPY_DESTINATION="Server=tcp:destination.database.windows.net,1433;Database=DestinationDb;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;"

sqlbulkcopy \
  --query "SELECT Id, Name, Email FROM dbo.vwCustomerExport" \
  --table "dbo.CustomerImport" \
  --truncate
```

## Azure SQL example

```bash
sqlbulkcopy \
  --source "Server=tcp:source.database.windows.net,1433;Database=SourceDb;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;" \
  --query "SELECT * FROM dbo.vwCustomers" \
  --destination "Server=tcp:destination.database.windows.net,1433;Database=DestinationDb;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;" \
  --table "dbo.Customers"
```

## View-to-table example

```bash
sqlbulkcopy \
  --source "$SOURCE_CONNECTION" \
  --query "SELECT Id, Name, Email FROM dbo.vwCustomerExport" \
  --destination "$DESTINATION_CONNECTION" \
  --table "dbo.CustomerImport"
```

## Authentication examples

SQL authentication:

```text
Server=tcp:server.database.windows.net,1433;
Database=MyDb;
User ID=user;
Password = <password>;
Encrypt=True;
TrustServerCertificate=False;
```

Microsoft Entra authentication:

```text
Server=tcp:server.database.windows.net,1433;
Database=MyDb;
Authentication=Active Directory Default;
Encrypt=True;
TrustServerCertificate=False;
```

Do not store credentials in the repository.
