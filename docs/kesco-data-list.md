# KescoDataList\<T>

Универсальный компонент-грид с серверной постраничной выборкой, поиском, сортировкой и группировкой.

## Параметры

| Параметр | Тип | По умолчанию | Описание |
|---|---|---|---|
| `Title` | `string` | `"Список"` | Заголовок |
| `Items` | `IEnumerable<TEntity>` | `[]` | Данные текущей страницы |
| `Loading` | `bool` | `false` | Индикатор загрузки |
| `ShowAddButton` | `bool` | `true` | Показать кнопку «Добавить» |
| `PageSize` | `int` | `50` | Размер страницы по умолчанию |
| `ShowPagination` | `bool` | `true` | Показать панель постраничной навигации |
| `TotalCount` | `int` | `0` | Общее количество записей (передаётся из страницы) |
| `ShowGroupingTray` | `bool` | `false` | Включить drag‑and‑drop tray для выбора колонок группировки |
| `AvailableGroupColumns` | `Dictionary<string, string>` | `[]` | Доступные для группировки колонки: ключ — отображаемое имя, значение — SQL-имя колонки |
| `ChildContent` | `RenderFragment?` | — | Колонки MudDataGrid |
| `OnAdd` | `EventCallback` | — | Обработчик кнопки «Добавить» |
| `OnRowClick` | `EventCallback<DataGridRowClickEventArgs<TEntity>>` | — | Клик по строке |
| `OnQueryChanged` | `EventCallback<KescoDataQuery>` | — | Изменение состояния запроса |
| `AllowColumnReorder` | `bool` | `true` | Разрешить перетаскивание колонок грида мышью |

Удалённые параметры (больше не используются):
- ~~`Groupable`~~ — группировка выполняется сервером, не MudBlazor
- ~~`GroupExpanded`~~ — состояние развёрнутости per-group, не глобальное
- ~~`GroupColumn`~~ — заменён на `GroupColumns` (множественная группировка)
- ~~`ShowGroupToggle`~~ / ~~`GroupToggleLabel`~~ — старый toggle-режим удалён

## Публичные методы

| Метод | Описание |
|---|---|
| `async Task ToggleSort(string sqlCol)` | Циклически переключает сортировку: ASC → DESC → убрать. Возвращает `Task` — **обязательно awaitable**. Вызывается из `<HeaderTemplate>` и из чипа трея |
| `GetSortBadge(string sqlCol)` | Возвращает `RenderFragment` с бейджем сортировки (номер + стрелка) |
| `RefreshAsync()` | Сбрасывает номер страницы на 1 и вызывает `OnQueryChanged` |
| `GroupColumns` | `IReadOnlyList<string>` — текущий список SQL-имён колонок в трее группировки |

## KescoDataQuery

Класс состояния запроса, передаваемый в `OnQueryChanged`:

- `SearchText` — текст поиска
- `GroupEnabled` — включена ли группировка
- `GroupColumns` — список SQL-имён колонок группировки в порядке приоритета
- `ExpandedGroups` — `HashSet<string>` полных ключей развёрнутых групп (разделитель `\u001F`)
- `SortColumns` — список `SortColumn(Column, Desc)`
- `PageNumber` — номер текущей страницы (1-based)
- `PageSize` — размер страницы
- `TotalCount` — общее число записей (заполняется страницей после загрузки)
- `BuildOrderBy(defaultOrder)` — строит `ORDER BY`; при включённой группировке все `GroupColumns` идут первыми
- `BuildWhereClause(searchColumns)` — строит `WHERE ... LIKE @search`

## Серверная группировка

Группировка выполняется **на стороне SQL Server** (не MudBlazor). Два отдельных запроса:

1. **Групповые агрегаты**: `GROUP BY` + `COUNT(*)`, возвращает уникальные значения и количество записей
2. **Детальные строки**: выборка с `ROW_NUMBER()` и фильтром по значениям группы

### Модель данных

- `IGridRow` — маркерный интерфейс строки в плоском списке
- `GroupHeaderRow` — заголовок группы с `FullKey`, `DisplayValue`, `ItemCount`, `Depth`, `IsExpanded`
- `DetailRow<T>` — обёртка сущности с `Item` и `GroupKey`
- `GroupedPage<T>` — результат: `Rows` (плоский список `IGridRow`) + `TotalEffectiveRows`

### Рендеринг групп

Колонки используют `TemplateColumn T="IGridRow"` с проверкой типа в `CellTemplate`:

```razor
<TemplateColumn T="IGridRow" Title="Код" Sortable="false"
                Hidden="@(_dataGrid?.GroupColumns.Contains("SqlCol") ?? false)">
    <CellTemplate>
        @if (context.Item is GroupHeaderRow header)
        {
            <MudIconButton Icon="..." OnClick="() => ToggleGroup(header)" />
            <MudText>@header.DisplayValue (@header.ItemCount шт.)</MudText>
        }
        else if (context.Item is DetailRow<MyEntity> detail)
        {
            <MudText>@detail.Item.Id</MudText>
        }
    </CellTemplate>
</TemplateColumn>
```

**Запрещено** использовать `PropertyColumn` с `Groupable`/`Grouping`/`GroupBy`/`GroupTemplate` — эти атрибуты удалены.

### `ExpandedGroups` и пагинация

- Состояние развёрнутости хранится в `KescoDataQuery.ExpandedGroups` (`HashSet<string>` ключей)
- Каждый заголовок группы = 1 эффективная строка, каждая строка детализации = 1
- `TotalCount` = общее эффективное количество строк
- При сворачивании/разворачивании группы количество видимых строк меняется
- При разворачивании последней группы на странице — авто-переход на следующую страницу
- При сворачивании группы, если `PageNumber > ceil(TotalCount / PageSize)` — авто-возврат на `maxPage`

### Сохранение порядка агрегатов из БД

При построении дерева групп **запрещено** пересортировывать список агрегатов после получения из БД:

```csharp
// НЕПРАВИЛЬНО — уничтожает порядок ORDER BY из SQL:
foreach (var a in aggregates.OrderBy(a => a.FullKey)) { ... }

// ПРАВИЛЬНО — порядок из БД сохраняется:
foreach (var a in aggregates) { ... }
```

Синтетические родительские узлы строятся внутри того же цикла `foreach (var gr in groupRows)` **перед** листовым узлом, поэтому при прямом обходе `aggregates` инвариант «родитель встречается раньше дочернего» всегда соблюдается.

### `GroupExprMap`

В странице определяется словарь соответствия SQL-имён из `GroupColumns` → выходные имена в подзапросе:

```csharp
private static readonly Dictionary<string, string> GroupExprMap = new()
{
    ["TestTypeName"] = "TestTypeName",
    ["КодМедицинскогоАнализа"] = "КодМедицинскогоАнализа",
    ["НазваниеАнализа"] = "НазваниеАнализа",
    ["Порядок"] = "Порядок"
};
```

Важно: в подзапросах `FROM (SELECT ...) _g` видны только выходные имена колонок (не табличные алиасы `a.`/`t.`).

### WHERE для grouped-режима

В grouped-режиме `BuildWhereClause` должен использовать выходные имена:
```csharp
var where = _query.BuildWhereClause("НазваниеАнализа", "TestTypeName");
```
В плоском режиме — табличные алиасы:
```csharp
var where = _query.BuildWhereClause("a.НазваниеАнализа", "t.ТипМедицинскогоАнализа");
```

## Пример использования (с группировкой)

```razor
@page "/items"
@using Kesco.Lib.Web.BZ.Controls

<KescoDataList TEntity="IGridRow"
               @ref="_dataGrid"
               Title="Заголовок"
               Items="_rows"
               Loading="_loading"
               PageSize="@AppSettings.DefaultPageSize"
               ShowGroupingTray="true"
               AvailableGroupColumns="@_groupColumnsDef"
               TotalCount="@_query.TotalCount"
               ShowPagination="true"
               OnAdd="OpenAddDialog"
               OnRowClick="OnRowClicked"
               OnQueryChanged="OnQueryChanged">
    <TemplateColumn T="IGridRow" Title="Код" Sortable="false"
                    Hidden="@(_dataGrid?.GroupColumns.Contains("SqlCol") ?? false)">
        <HeaderTemplate>
            <div draggable="true"
                 @ondragstart="@(e => { e.DataTransfer.EffectAllowed = "move"; KescoDragState.DraggedColumn = "SqlCol"; })"
                 @onclick="@(async () => { if (_dataGrid is not null) await _dataGrid.ToggleSort("SqlCol"); })">
                <MudText>Код</MudText>
                @_dataGrid?.GetSortBadge("SqlCol")
            </div>
        </HeaderTemplate>
        <CellTemplate>
            @if (context.Item is GroupHeaderRow h)
            {
                <MudIconButton Icon="..." OnClick="() => ToggleGroup(h)" />
                <MudText>@h.DisplayValue (@h.ItemCount шт.)</MudText>
            }
            else if (context.Item is DetailRow<MyEntity> d)
            {
                <MudText>@d.Item.Id</MudText>
            }
        </CellTemplate>
    </TemplateColumn>
    <TemplateColumn T="IGridRow" Title="Название" Sortable="false"
                    Hidden="@(_dataGrid?.GroupColumns.Contains("NameSqlCol") ?? false)">
        <HeaderTemplate>
            <div draggable="true"
                 @ondragstart="@(e => { e.DataTransfer.EffectAllowed = "move"; KescoDragState.DraggedColumn = "NameSqlCol"; })"
                 @onclick="@(async () => { if (_dataGrid is not null) await _dataGrid.ToggleSort("NameSqlCol"); })">
                <MudText>Название</MudText>
                @_dataGrid?.GetSortBadge("NameSqlCol")
            </div>
        </HeaderTemplate>
        <CellTemplate>
            @if (context.Item is DetailRow<MyEntity> d)
            {
                <MudText>@d.Item.Name</MudText>
            }
        </CellTemplate>
    </TemplateColumn>
</KescoDataList>

@code {
    private KescoDataList<IGridRow> _dataGrid = null!;
    private KescoDataQuery _query = new();

    private Dictionary<string, string> _groupColumnsDef = new()
    {
        ["Группа"] = "GroupSqlCol",
        ["Код"] = "SqlCol"
    };

    private static readonly Dictionary<string, string> GroupExprMap = new()
    {
        ["GroupSqlCol"] = "GroupSqlCol",
        ["SqlCol"] = "SqlCol"
    };

    private List<IGridRow> _rows = [];
    private bool _loading = true;

    private async Task OnQueryChanged(KescoDataQuery query)
    {
        _query.SearchText = query.SearchText;
        _query.GroupEnabled = query.GroupEnabled;
        _query.GroupColumns = query.GroupColumns;
        _query.SortColumns = query.SortColumns;
        _query.PageNumber = query.PageNumber;
        _query.PageSize = query.PageSize;
        // ExpandedGroups управляется страницей, не перезаписывается
        await LoadData();
    }

    private async Task LoadData()
    {
        _loading = true;
        try
        {
            if (_query.GroupEnabled && _query.GroupColumns.Count > 0)
                await LoadGroupedData();
            else
                await LoadFlatData();
        }
        finally
        {
            _loading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadFlatData()
    {
        var where = _query.BuildWhereClause("a.Col1", "a.Col2");
        var orderBy = _query.BuildOrderBy("DefaultCol");
        var param = new { search = $"%{_query.SearchText}%" };

        _query.TotalCount = await MyEntity.GetCountAsync(Db, where, param);
        var items = await MyEntity.GetPagedAsync(Db, where, orderBy, param,
            _query.PageNumber, _query.PageSize);
        _rows = items.Select(i => (IGridRow)new DetailRow<MyEntity> { Item = i }).ToList();
    }

    private async Task LoadGroupedData()
    {
        // Использовать выходные имена колонок для WHERE в подзапросе
        var where = _query.BuildWhereClause("Col1", "Col2");
        var orderBy = _query.BuildOrderBy("DefaultCol");

        // 1. Групповые агрегаты
        var groupSql = BuildGroupSql(...);
        var groupRows = await Db.QueryAsync<GroupRow>(groupSql, param);

        // 2. Построение дерева групп (с синтет. родителями для многоуровневой)
        // 3. Вычисление эффективных строк (ComputeEffectiveRows + ComputeParentCounts)
        // 4. WalkTree — определение видимых на странице групп и диапазонов деталей
        // 5. Загрузка детальных строк для развёрнутых групп (LoadDetailRows)
        // 6. _rows = плоский список (GroupHeaderRow + DetailRow<T>)
        // 7. _query.TotalCount = totalEffectiveRows
    }

    private async Task ToggleGroup(GroupHeaderRow header)
    {
        if (_query.ExpandedGroups.Contains(header.FullKey))
            _query.ExpandedGroups.Remove(header.FullKey);
        else
            _query.ExpandedGroups.Add(header.FullKey);
        await LoadData();
        // Авто-переход на след. страницу, если детали не влезли
    }
}
```

## Состояния

- **Поиск** — сбрасывает страницу на 1, вызывает `OnQueryChanged` с debounce 300 мс
- **Сортировка** — до 2 колонок, циклически: ASC → DESC → убрать. Сбрасывает страницу на 1. Сортировка по чипу в трее также работает (направление учитывается в `GROUP BY ... ORDER BY`). **`ToggleSort` возвращает `Task` — вызывать только через `await`**, иначе Blazor не дождётся перезагрузки данных
- **Группировка (tray)** — панель над гридом, скрытая по умолчанию. Открывается кнопкой `AccountTree` в тулбаре (класс `grouping-toggle-btn`). Добавление колонок — перетаскивание заголовка на панель. Удаление — клик по × на чипе. Изменение порядка — перетаскивание чипов. Сортировка по чипу — клик по его названию (бейдж `chip-sort-badge`). При любом изменении сбрасывается страница на 1
- **Сворачивание/разворачивание группы** — НЕ сбрасывает страницу на 1. Если детали не влезают — авто-переход вперёд
- **Смена размера страницы** — числовое поле (1–999). Сбрасывает страницу на 1
- **Кнопка «Обновить»** — сбрасывает страницу на 1, перезагружает данные
- **Переход по страницам** — кнопки `|<`, `<`, `>`, `>|`. Не сбрасывают фильтры
- **Защита выхода за границы** — при уменьшении `TotalCount` номер страницы автоматически обрезается до максимального

## Кнопки тулбара

Все кнопки в строке заголовка — `MudIconButton` с тултипом. Не использовать `MudButton Variant.Filled`.

| Кнопка | Иконка | CSS-класс | Поведение |
|---|---|---|---|
| Группировка | `AccountTree` | `grouping-toggle-btn` / `grouping-toggle-btn--active` | Показывает/скрывает панель tray |
| Добавить | `Add` | `toolbar-add-btn` | Вызывает `OnAdd` |
