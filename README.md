# sqlbulkcopy-cli

`sqlbulkcopy-cli` is a small, cross-platform .NET command-line utility for streaming data from a SQL Server or Azure SQL query/view/table into a destination SQL Server or Azure SQL table with `Microsoft.Data.SqlClient.SqlBulkCopy`.

The tool streams rows directly from a `SqlDataReader` into `SqlBulkCopy`. It does **not** create CSV, BCP, temporary, or intermediate files.

## Features

- .NET 8 LTS CLI
- Cross-platform: Linux, macOS, and Windows
- Streams data directly from source to destination
- Supports SQL Server and Azure SQL on different logical servers
- Supports command-line arguments, environment variables, and optional JSON config
- Safe `schema.object` handling for `--source-object` and `--destination-table`
- Case-insensitive column mapping by name
- Explicit `--map Source=Destination` overrides
- Destination modes: `append`, `truncate`, `delete`
- Optional transaction support
- Progress reporting with `SqlRowsCopied`
- Microsoft Entra authentication support through `Microsoft.Data.SqlClient` and `Microsoft.Data.SqlClient.Extensions.Azure`

## Requirements

- .NET 8 SDK to build
- Network access to the source and destination SQL Server or Azure SQL databases
- Permissions to read from the source object/query and write to the destination table

## Install and build

### Build locally

```bash
dotnet build /home/runner/work/sqlbulkcopy-cli/sqlbulkcopy-cli/sqlbulkcopy-cli.slnx
```

### Run from source

```bash
dotnet run --project /home/runner/work/sqlbulkcopy-cli/sqlbulkcopy-cli/sqlbulkcopy-cli -- copy --help
```

### Pack as a .NET tool

```bash
dotnet pack /home/runner/work/sqlbulkcopy-cli/sqlbulkcopy-cli/sqlbulkcopy-cli/sqlbulkcopy-cli.csproj -c Release
```

After packaging, install the generated tool package and run it as:

```bash
sqlbulkcopy copy --help
```

## Usage

### Query to table

```bash
sqlbulkcopy copy \
  --source-connection "$SOURCE_SQL" \
  --source-query "SELECT Id, Name, CreatedDate FROM dbo.vwCustomers" \
  --destination-connection "$DEST_SQL" \
  --destination-table "dbo.Customers"
```

### View or table to table

```bash
sqlbulkcopy copy \
  --source-connection "$SOURCE_SQL" \
  --source-object "dbo.vwCustomers" \
  --destination-connection "$DEST_SQL" \
  --destination-table "dbo.Customers"
```

When `--source-object` is used, the tool safely generates:

```sql
SELECT * FROM [schema].[object]
```

`--source-query` and `--source-object` are mutually exclusive.

## Configuration precedence

Configuration values are applied in this order:

1. JSON configuration file
2. environment variables
3. command-line arguments

Later sources override earlier ones.

### Environment variables

Common variables:

```bash
SQLBULKCOPY_SOURCE_CONNECTION
SQLBULKCOPY_SOURCE_QUERY
SQLBULKCOPY_SOURCE_OBJECT
SQLBULKCOPY_DESTINATION_CONNECTION
SQLBULKCOPY_DESTINATION_TABLE
SQLBULKCOPY_MODE
SQLBULKCOPY_BATCH_SIZE
SQLBULKCOPY_NOTIFY_AFTER
SQLBULKCOPY_MAP
```

See `/home/runner/work/sqlbulkcopy-cli/sqlbulkcopy-cli/.env.example` for a safe sample.

### JSON configuration

Use the default `sqlbulkcopy.json` in the working directory or pass an explicit file:

```bash
sqlbulkcopy copy --config ./sqlbulkcopy.json
```

A sample file is included at `/home/runner/work/sqlbulkcopy-cli/sqlbulkcopy-cli/sqlbulkcopy.json.example`.

## Column mapping

The default mode is:

```text
--mapping auto
```

Automatic mapping:

- reads source column names from the `SqlDataReader`
- reads destination table metadata from SQL Server
- matches columns by name, case-insensitively
- skips incompatible columns
- reports unmatched or incompatible columns before copying

Explicit mappings are also supported:

```bash
sqlbulkcopy copy \
  --source-connection "$SOURCE_SQL" \
  --source-object "dbo.vwCustomers" \
  --destination-connection "$DEST_SQL" \
  --destination-table "dbo.Customers" \
  --map SourceCustomerId=CustomerId \
  --map CustomerName=Name
```

## Destination modes

Default mode:

```text
--mode append
```

Supported modes:

- `append`
- `truncate`
- `delete`

Destructive modes require explicit confirmation:

```bash
sqlbulkcopy copy \
  --source-connection "$SOURCE_SQL" \
  --source-object "dbo.vwCustomers" \
  --destination-connection "$DEST_SQL" \
  --destination-table "dbo.Customers" \
  --mode truncate \
  --confirm-destructive
```

Only the explicitly supplied destination table is affected.

## Transactions

Use `--transaction` to wrap the destination-side work in a single SQL transaction:

1. begin destination transaction
2. perform `truncate` or `delete` if requested
3. execute bulk copy
4. commit only after success
5. rollback on failure

Example:

```bash
sqlbulkcopy copy \
  --source-connection "$SOURCE_SQL" \
  --source-query "SELECT Id, Name FROM dbo.vwCustomers" \
  --destination-connection "$DEST_SQL" \
  --destination-table "dbo.Customers" \
  --mode delete \
  --confirm-destructive \
  --transaction
```

For very large copies, a single transaction can increase log usage and lock duration.

## Bulk copy options

Supported flags:

- `--batch-size`
- `--timeout`
- `--bulk-timeout`
- `--notify-after`
- `--keep-identity`
- `--keep-nulls`
- `--check-constraints`
- `--table-lock`
- `--fire-triggers`
- `--use-internal-transaction`

`--transaction` and `--use-internal-transaction` cannot be used together.

## Authentication

The tool supports standard SQL connection strings and Microsoft Entra authentication modes supported by `Microsoft.Data.SqlClient`.

### Developer workstation

Use a connection string such as:

```text
Authentication=Active Directory Default
```

This works well with Azure CLI sign-in and other credential sources in `DefaultAzureCredential`.

### Interactive Linux sign-in

For cases where a browser/device flow is needed, use a supported interactive Entra mode in the connection string, for example:

```text
Authentication=Active Directory Interactive
```

or:

```text
Authentication=Active Directory Device Code Flow
```

Device-code messages are written to standard error so they remain visible in terminals and logs.

### Azure-hosted workloads

For managed identity:

```text
Authentication=Active Directory Managed Identity
```

### Automation and CI/CD

Prefer Entra approaches such as:

- workload identity federation
- managed identity
- service principals configured outside source control

Do **not** store passwords, tokens, or client secrets in the repository.

## Progress output

Example output:

```text
Source:      server-a / DatabaseA / [dbo].[vwCustomerExport]
Destination: server-b / DatabaseB / [dbo].[Customer]
Mode:        truncate
Batch size:  10,000

Copying...

100,000 rows
200,000 rows
300,000 rows
Mapped columns: 3

Completed
Rows copied: 327,418
Elapsed:     00:00:18.42
Rate:        17,775 rows/sec
```

Connection display intentionally omits credentials and sensitive connection-string content.

## Local development validation

Run tests:

```bash
dotnet test /home/runner/work/sqlbulkcopy-cli/sqlbulkcopy-cli/sqlbulkcopy-cli.slnx
```

Verify CLI help:

```bash
dotnet run --project /home/runner/work/sqlbulkcopy-cli/sqlbulkcopy-cli/sqlbulkcopy-cli -- copy --help
```

## Security notes

- Never commit real `.env` or `sqlbulkcopy.json` files with secrets
- Use `.env.example` and `sqlbulkcopy.json.example` as templates only
- The CLI does not log passwords, full connection strings, or tokens
