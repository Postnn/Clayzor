# KescoBZ — Медицинские исследования

## Stack
- .NET 10 Blazor Server (Interactive Server)
- MudBlazor 9.x — `Variant.Outlined` is the standard form input style
- MudBlazor.Extensions 9.x — дополнительные возможности MudBlazor (перетаскивание диалогов и др.)
- Dapper + Microsoft.Data.SqlClient 6.x (SQL Server)
- Windows Integrated Auth (Kerberos/NTLM via Negotiate)

## Build & Run
```
dotnet build KescoBZ.sln
dotnet run --project src\Kesco.App.Web.BZ.MedicalTests
```
Dev URL: `http://localhost:5010`

Watch with hot reload (VS Code task also available):
```
dotnet watch run --project src\Kesco.App.Web.BZ.MedicalTests
```

## Project dependency chain
```
Kesco.Lib.DALC          ← Dapper, SqlClient
Kesco.Lib.Entities       ← DALC + Dapper (column mapping)
Kesco.Lib.Web.Settings   ← config bindings (no DB dep)
Kesco.Lib.Web.BZ.Controls ← Entities + Web.Settings + MudBlazor (Razor Class Library)
Kesco.App.Web.BZ.MedicalTests ← all of the above (web app entry point)
```

## Configuration — connection string priority

`BindKescoSettings()` in `Kesco.Lib.Web.Settings/KescoAppSettings.cs` merges:
1. `web.config` (XML) — loaded via `AddWebConfig()` alongside `appsettings.json`
2. `KescoApp:ConnectionString` (any source) → `ConnectionStrings:DefaultConnection` (fallback)

Both `appsettings.json` and `web.config` participate; the merge order is standard .NET config layering (last source wins for keys). Local overrides go in `appsettings.Development.json` (gitignored).

`Program.cs` registers MudBlazor and MudBlazor.Extensions:
```csharp
builder.Services.AddMudServices();
builder.Services.AddMudExtensions(cfg => cfg.WithDefaultDialogOptions(d => d.DragMode = MudDialogDragMode.Simple));
```

`App.razor` must include MudBlazor.Extensions CSS and JS:
```html
<link href="_content/MudBlazor.Extensions/mudBlazorExtensions.min.css" rel="stylesheet" />
<script src="_content/MudBlazor.Extensions/js/mudBlazorExtensions.js"></script>
```

## Database access pattern

**No repository layer, no ORM, no migrations.** All SQL lives in `Kesco.Lib.Entities/SQLQueries.cs` as named constants.

### SQL constant naming convention
- `SELECT_{DataName}` — запросы на выборку данных
- `INSERT_{TableName}` — добавление записей
- `UPDATE_{TableName}` — обновление записей
- `DELETE_{TableName}` — удаление записей
- `SP_{Name}` — хранимые процедуры
- `FN_{Name}` — пользовательские функции

### Rules
- Queries use `DbManager` (scoped per-request) injected via `@inject DbManager Db`
- Raw SQL for queries: `Db.QueryAsync<T>(SQLQueries.CONST_NAME)` — `QueryAsync` defaults to `CommandType.Text`
- Raw SQL for commands: `Db.ExecuteAsync(SQLQueries.CONST_NAME, entity, commandType: CommandType.Text)` — **must pass `commandType: CommandType.Text`** because `ExecuteAsync` defaults to `CommandType.StoredProcedure`
- Stored procedures: `Db.QueryStoredProcAsync<T>(name, params)`
- All database access must go through `DbManager` methods — no direct `SqlConnection` or Dapper calls in pages or other assemblies
- Column names in SQL are **Russian**: `КодМедицинскогоАнализа`, `НазваниеАнализа`, etc.
- Entity properties map to Russian columns via `[Column(MedA.Имя)]` referencing constants from `ColumnNames.cs` — каждое имя колонки определено ровно один раз
- Every entity class must be registered in `DapperColumnMapper.Initialize()`
- `@using System.Data` required in `_Imports.razor` when using `CommandType.Text`

## Entity CRUD & Lookup pattern
→ [docs/entity-crud.md](docs/entity-crud.md)

## Adding a new entity
→ [docs/adding-new-entity.md](docs/adding-new-entity.md)

## Shared components (Kesco.Lib.Web.BZ.Controls)

| Компонент | Документация |
|---|---|
| **KescoDataList\<T>** — грид с серверной пагинацией, поиском, сортировкой, группировкой | [docs/kesco-data-list.md](docs/kesco-data-list.md) |
| **KescoEditForm\<T>** — MudDialog с валидацией, сохранением, удалением | [docs/kesco-edit-form.md](docs/kesco-edit-form.md) |
| **KescoComboBox\<TItem>** — выпадающий список для `ILookupEntity` | [docs/kesco-combo-box.md](docs/kesco-combo-box.md) |
| **ConfirmDialog** — диалог подтверждения | [docs/confirm-dialog.md](docs/confirm-dialog.md) |
| **ILookupEntity** — интерфейс справочной сущности (`int Id`, `string Name`) | [docs/entity-crud.md](docs/entity-crud.md) |
| **KescoTheme** — corporate theme (DarkNavy + Gold accent). Applied in MainLayout | — |

## Key conventions
- All Razor markup and user-visible text is **Russian**
- No tests exist in this repo
- No CI/CD, no linter configuration
- `[Column]` attributes use exact Russian database column names — do not translate
- SQL queries reference tables by Russian names (e.g. `МедицинскиеАнализы`, `МедицинскиеАнализыТипы`)
- Each SQL constant must be documented with `///` XML doc and `--` inline SQL comments for every column
- Every public/protected class, method, property, and field must have a `/// <summary>` XML doc comment
- Database column names are defined exactly once in `ColumnNames.cs` and referenced from `[Column]` attributes (SQLQueries constants are exempt)
- Grouping column must be hidden when grouping is active — add `Hidden="@_dataGrid?.GroupEnabled ?? false)"` on the `PropertyColumn` used for grouping
- Data loading goes in `OnAfterRenderAsync(bool firstRender)` with `if (firstRender)` guard, **not** in `OnInitializedAsync` — avoids double-load from Blazor prerendering
- Sorting, searching, grouping, and **pagination** for data grids must be performed on SQL Server side (not in-memory) — use `GetPagedAsync` + `GetCountAsync`
- Sort column headers use null-conditional `_dataGrid?.ToggleSort("SqlColumn")` — `_dataGrid` is set via `@ref` on `KescoDataList`
- `appsettings.Development.json` is gitignored — use it for local connection strings
- All modal/dialog windows must be draggable
- Data grid header row must be fixed (not scroll with data) — `KescoDataList` does this automatically
- При вызове `Db.ExecuteAsync()` с сырым SQL обязательно передавать `commandType: CommandType.Text` — по умолчанию `ExecuteAsync` использует `CommandType.StoredProcedure`
- `DapperColumnMapper` делает fallback на имя свойства, если `[Column]`-атрибут не совпал с колонкой результата — это позволяет использовать SQL-алиасы (`SELECT КодТипа AS Id`) даже при наличии `[Column("КодТипа")]` на свойстве `Id`
- **OnQueryChanged**: обновлять свойства `_query`, а не переприсваивать объект — иначе `TotalCount` сбрасывается в 0 при async-рендере
