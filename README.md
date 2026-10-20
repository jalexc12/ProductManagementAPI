# Product Management API

An ASP.NET Core Web API for managing products and stock. It uses Entity Framework Core with a
SQL Server database and follows a code-first migration workflow.

## Requirements

- .NET 10 SDK
- SQL Server Express Database Engine (or another reachable SQL Server instance)
- Docker Desktop or another Docker-compatible container runtime (for SQL Server integration tests)
- EF Core command-line tool (`dotnet-ef`) version 10.0.12

SQL Server Management Studio (SSMS) is optional; it is a database management application and does
not replace the SQL Server Database Engine.

## Run locally

Run the following commands from the `ProductManagementApi` directory.

### 1. Configure the database connection

Update `appsettings.Development.json` with the connection string for your SQL Server instance:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ProductManagementDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Change `localhost\\SQLEXPRESS` if your SQL Server instance uses a different name. This example
uses Windows Authentication.

### 2. Restore dependencies

```powershell
dotnet restore
```

### 3. Apply database migrations

The migrations create the schema and add eight sample products. The sample-product migration
omits `Id` from its inserts, so SQL Server assigns IDs from `ProductIdSequence`.

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update
```

If the EF Core CLI is not installed:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.12
```

To update an existing global installation:

```powershell
dotnet tool update --global dotnet-ef --version 10.0.12
```

### 4. Start the API

```powershell
dotnet run --launch-profile https
```

The HTTPS profile uses `https://localhost:7237` and also exposes HTTP at
`http://localhost:5147`.

### 5. Open Swagger

In Development, open [https://localhost:7237/swagger](https://localhost:7237/swagger) to explore
and call the API.

## API endpoints

All endpoints use the `/api/products` base route. Product responses include the available `Stock`.

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/products` | Return all products, ordered by ID. |
| `POST` | `/api/products` | Create a product. Returns `201 Created` and a URL for the new product. |
| `GET` | `/api/products/{id}` | Return one product; returns `404` if it does not exist. |
| `PUT` | `/api/products/{id}` | Replace the product fields; returns `404` if it does not exist. |
| `DELETE` | `/api/products/{id}` | Delete a product; returns `204 No Content`, or `404` if it does not exist. |
| `POST` | `/api/products/{id}/decrement-stock/{quantity}` | Decrease stock by a positive quantity. |
| `POST` | `/api/products/{id}/add-to-stock/{quantity}` | Increase stock by a positive quantity. |
| `GET` | `/api/products/search?name={name}` | Return products whose names contain the search text. |
| `GET` | `/api/products/stock-level?min={min}&max={max}` | Return products with stock within the inclusive range. |

### Request examples

Create a product with `POST /api/products`:

```json
{
  "name": "ZEISS Camera Lens",
  "description": "Standard camera lens",
  "price": 499.99,
  "stock": 12
}
```

Use the same fields for `PUT /api/products/{id}`:

```json
{
  "name": "ZEISS Camera Lens",
  "description": "Updated lens description",
  "price": 519.99,
  "stock": 10
}
```

Example query and stock-operation URLs:

```text
GET  /api/products/search?name=lens
GET  /api/products/stock-level?min=5&max=20
POST /api/products/100000/decrement-stock/2
POST /api/products/100000/add-to-stock/5
```

### Validation and response behavior

- Create/update require a non-empty name (maximum 256 characters), a non-empty description
  (maximum 1000 characters), a price greater than zero, and non-negative stock.
- Search requires a non-empty name (maximum 256 characters). Matching is partial; case behavior
  follows the SQL Server database collation.
- Stock-level search requires both `min` and `max`; each must be non-negative and `min` cannot
  exceed `max`. The bounds are inclusive.
- Stock adjustment requires a positive quantity. Decrementing more than available stock is
  rejected; stock cannot become negative. Invalid quantities, insufficient stock, and an
  increment that would exceed the supported integer range are rejected. Invalid quantities
  return `400 Bad Request`; insufficient stock and increments that exceed the supported integer
  range return `409 Conflict`.
- Requests for product IDs that do not exist return `404 Not Found` where applicable.

FluentValidation validates request DTOs automatically for create, update, search, and stock-level
queries. Stock-operation business rules are checked by the product service.

## Run SQL Server integration tests

The integration test project starts a SQL Server container with Testcontainers, applies the
application migrations, and exercises the HTTP API. It verifies generated product IDs, database
check constraints, and concurrent stock decrements.

Run it from the repository root with Docker available:

```powershell
$env:RUN_SQLSERVER_INTEGRATION_TESTS = "1"
dotnet test .\ProductManagementApi.IntegrationTests\ProductManagementApi.IntegrationTests.csproj
```

These tests are opt-in and are reported as skipped unless `RUN_SQLSERVER_INTEGRATION_TESTS=1`.
The regular `ProductManagementApi.Tests` project remains a fast unit-test suite and does not
require Docker.

## Product IDs and database

`Product.Id` is the primary key and the identifier returned by the API. SQL Server generates it
using the `ProductIdSequence` sequence, which starts at `100000`. Inserts from both the API and
the sample-data migration omit `Id`, allowing the database sequence to supply unique values.

Database schema changes and the initial sample products are managed by EF Core migrations. Apply
them with `dotnet ef database update` as shown above.

## Run tests

The test project uses xUnit and EF Core's in-memory provider. These tests don't require a running
SQL Server instance.

From the workspace root (the directory containing both project folders), run:

```powershell
dotnet test .\ProductManagementApi.Tests\ProductManagementApi.Tests.csproj
```

The tests cover service operations, stock rules, search and stock-range queries, and request
validation. They do not replace integration tests against SQL Server or end-to-end HTTP tests.
