# Добавление новой сущности

## Порядок действий

### 1. Константы колонок

Добавить имена колонок в `ColumnNames.cs` (каждое имя ровно один раз):

```csharp
public static class MedA
{
    // существующие...
    public const string КодНовойЗаписи = "КодНовойЗаписи";
    public const string НазваниеНовойЗаписи = "НазваниеНовойЗаписи";
}
```

### 2. SQL-константы

Добавить в `Kesco.Lib.Entities/SQLQueries.cs`:

```csharp
/// <summary>Выборка новых записей.</summary>
public const string SELECT_НоваяТаблица = @"
-- Основные поля
SELECT КодНовойЗаписи,    -- идентификатор
       НазваниеНовойЗаписи -- название
FROM НоваяТаблица";

/// <summary>Добавление новой записи.</summary>
public const string INSERT_НоваяТаблица = @"
INSERT INTO НоваяТаблица (НазваниеНовойЗаписи)
VALUES (@Name)";

/// <summary>Обновление новой записи.</summary>
public const string UPDATE_НоваяТаблица = @"
UPDATE НоваяТаблица SET НазваниеНовойЗаписи = @Name
WHERE КодНовойЗаписи = @Id";

/// <summary>Удаление новой записи.</summary>
public const string DELETE_НоваяТаблица = @"
DELETE FROM НоваяТаблица WHERE КодНовойЗаписи = @Id";
```

### 3. Класс сущности

Создать в `Kesco.Lib.Entities/`:

```csharp
[Table("НоваяТаблица")]
public class NewEntity : Entity
{
    [Key]
    [Column(MedA.КодНовойЗаписи)]
    public override int Id { get; set; }

    [Column(MedA.НазваниеНовойЗаписи)]
    public string Name { get; set; } = string.Empty;

    protected override string SelectSql => SQLQueries.SELECT_НоваяТаблица;
    protected override string InsertSql => SQLQueries.INSERT_НоваяТаблица;
    protected override string UpdateSql => SQLQueries.UPDATE_НоваяТаблица;
    protected override string DeleteSql => SQLQueries.DELETE_НоваяТаблица;

    public static async Task<IEnumerable<NewEntity>> GetAllAsync(
        DbManager db, string? where, string? orderBy, object? param)
        => await Entity.GetAllAsync<NewEntity>(db, SQLQueries.SELECT_НоваяТаблица, where, orderBy, param);

    public static async Task<IEnumerable<NewEntity>> GetPagedAsync(
        DbManager db, string? where, string? orderBy, object? param,
        int pageNumber, int pageSize)
        => await Entity.GetPagedAsync<NewEntity>(db, SQLQueries.SELECT_НоваяТаблица, where, orderBy, param, pageNumber, pageSize);

    public static async Task<int> GetCountAsync(
        DbManager db, string? where = null, object? param = null)
        => await Entity.GetCountAsync<NewEntity>(db, SQLQueries.SELECT_НоваяТаблица, where, param);
}
```

### 4. Регистрация в DapperColumnMapper

В `Kesco.Lib.Entities/MedicalTests/MedicalTest.cs`:

```csharp
public static void Initialize()
{
    if (_initialized) return;
    RegisterColumnMap<MedicalTest>();
    RegisterColumnMap<MedicalTestType>();
    RegisterColumnMap<NewEntity>();  // ← добавить
    _initialized = true;
}
```

### 5. Страница

Создать `Components/Pages/NewEntityPage.razor`:

```razor
@page "/new-entities"
@inject DbManager Db
@inject IDialogService DialogService
@inject ISnackbar Snackbar
@inject KescoAppSettings AppSettings

<PageTitle>Новые записи</PageTitle>

<KescoGrid TEntity="NewEntity"
           @ref="_dataGrid"
           Title="Новые записи"
           Items="_items"
           Loading="_loading"
           PageSize="@AppSettings.DefaultPageSize"
           TotalCount="@_query.TotalCount"
           OnAdd="OpenAddDialog"
           OnRowClick="OnRowClicked"
           OnQueryChanged="OnQueryChanged">
    <PropertyColumn T="NewEntity" TProperty="int" Property="x => x.Id" Sortable="false">
        <HeaderTemplate>
            <div @onclick="@(() => _dataGrid?.ToggleSort("КодНовойЗаписи"))" ...>
                <MudText>Код</MudText>
                @if (_dataGrid is not null) { @_dataGrid.GetSortBadge("КодНовойЗаписи") }
            </div>
        </HeaderTemplate>
    </PropertyColumn>
    <PropertyColumn T="NewEntity" TProperty="string" Property="x => x.Name" Sortable="false">
        <HeaderTemplate>
            <div @onclick="@(() => _dataGrid?.ToggleSort("НазваниеНовойЗаписи"))" ...>
                <MudText>Название</MudText>
                @if (_dataGrid is not null) { @_dataGrid.GetSortBadge("НазваниеНовойЗаписи") }
            </div>
        </HeaderTemplate>
    </PropertyColumn>
</KescoGrid>
```

### 6. CRUD в диалоге

```razor
@* NewEntityEditDialog.razor *@
<KescoEditForm TEntity="NewEntity" Model="Model" OnSave="SaveAsync" OnDelete="DeleteAsync">
    <MudTextField @bind-Value="Model.Name" Label="Название" Variant="Variant.Outlined" Required="true" />
</KescoEditForm>

@code {
    [Parameter] public NewEntity Model { get; set; } = null!;

    private async Task SaveAsync(NewEntity model)
    {
        if (model.Id == 0)
            await model.InsertAsync(Db);
        else
            await model.UpdateAsync(Db);
    }
}
```
