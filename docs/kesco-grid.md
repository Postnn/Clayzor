# KescoGrid\<T>

Универсальный компонент-грид с серверной постраничной выборкой, поиском, сортировкой, группировкой и фильтрацией по колонкам.
Используется совместно с базовым классом страницы [`KescoGridPageBase<T>`](#kescogridpagebaset).

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
| `Columns` | `RenderFragment?` | — | Колонки MudDataGrid (`TemplateColumn` / `PropertyColumn`) |
| `ColumnDefs` | `RenderFragment?` | — | Метаданные колонок — `<KescoColumnDef>` компоненты для регистрации группируемых/фильтруемых колонок |
| `FilterColumnTypes` | `IReadOnlyDictionary<string, ColumnType>` | `[]` | Тип данных фильтруемых колонок: SQL-имя → `ColumnType`. Авто-вычисляется в `KescoGridPageBase` через рефлексию |
| `OnAdd` | `EventCallback` | — | Обработчик кнопки «Добавить» |
| `OnRowClick` | `EventCallback<DataGridRowClickEventArgs<TEntity>>` | — | Клик по строке |
| `OnQueryChanged` | `EventCallback<KescoDataQuery>` | — | Изменение состояния запроса |
| `AllowColumnReorder` | `bool` | `true` | Разрешить перетаскивание колонок грида мышью |

Удалённые параметры (больше не используются):
- ~~`Groupable`~~ — группировка выполняется сервером, не MudBlazor
- ~~`GroupExpanded`~~ — состояние развёрнутости per-group, не глобальное
- ~~`GroupColumn`~~ — заменён на `GroupColumns` (множественная группировка)
- ~~`ShowGroupToggle`~~ / ~~`GroupToggleLabel`~~ — старый toggle-режим удалён
- ~~`ShowGroupingTray`~~ / ~~`AvailableGroupColumns`~~ — заменены на `KescoColumnDef` в `<ColumnDefs>`
- ~~`ShowFilterTray`~~ / ~~`AvailableFilterColumns`~~ — заменены на `KescoColumnDef` в `<ColumnDefs>`
- ~~`ChildContent`~~ — переименован в `Columns`, заодно добавлен `ColumnDefs`

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
- `ColumnFilters` — `Dictionary<string, ColumnFilter>` — фильтры по колонкам, ключ = SQL-имя колонки
- `BuildColumnFilterClause(DynamicParameters, columnNameMap?)` — строит `WHERE` из `ColumnFilters`, добавляет параметры в `DynamicParameters`. В плоском режиме имена колонок маппятся через `columnNameMap`
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

## Серверная фильтрация по колонкам

Фильтрация по колонкам выполняется **на стороне SQL Server** через `BuildColumnFilterClause`.
UI — панель фильтров (filter tray) с drag-and-drop заголовков и диалогом `ColumnFilterDialog` для настройки условий.

### Модель данных
- `ColumnType` — тип данных колонки: `Text` (Contains/Equals/StartsWith/EndsWith/NotEquals), `Number` (равенство + сравнения >/</>=/<=), `Boolean` (Equals)
- `ColumnFilterOperator` — оператор сравнения: `Contains`, `Equals`, `NotEquals`, `StartsWith`, `EndsWith`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`
- `ColumnFilter` — условие фильтра: `Column` (SQL-имя), `ParamName` (имя Dapper-параметра), `Operator`, `Value`
- `KescoDataQuery.ColumnFilters` — `Dictionary<string, ColumnFilter>` — ключ = SQL-имя колонки

### SQL-генерация
- `KescoDataQuery.BuildColumnFilterClause(DynamicParameters parameters, Dictionary<string, string>? columnNameMap)` — генерирует WHERE-фрагмент (`col LIKE @p` / `col = @p` / `col > @p` и т.д.) и добавляет параметры в `DynamicParameters`
- `columnNameMap` — опциональный маппинг имён (например, `"TestTypeName"` → `"t.ТипМедицинскогоАнализа"`) для плоского режима, где имена колонок в SELECT отличаются от подзапросного режима

### Filter tray
- Панель включается кнопкой `FilterAlt` (`ShowFilterTray="true"`), скрыта по умолчанию (`_filterTrayExpanded = false`). Кнопка появляется автоматически при наличии хотя бы одного `KescoColumnDef` с `Filterable="true"`
- Добавление фильтра: перетаскивание заголовка колонки на панель → открывается `ColumnFilterDialog`
- Редактирование: клик по чипу фильтра → повторно открывается диалог с текущими значениями
- Удаление: клик по × на чипе
- При выключении панели все фильтры сбрасываются, данные перезагружаются
- Чип показывает читаемое описание: `«Название содержит «грипп»»` (через `ColumnFilterDialog.GetFilterDescription`)
- Filter tray не конфликтует с grouping tray — оба могут быть открыты одновременно

### Интеграция на странице
```csharp
// Определения фильтруемых колонок и их типов
private static readonly Dictionary<string, string> _filterColumnsDef = new()
{
    ["Код"] = "КодМедицинскогоАнализа", ["Название"] = "НазваниеАнализа", ...
};
private static readonly Dictionary<string, ColumnType> _filterColumnTypes = new()
{
    ["КодМедицинскогоАнализа"] = ColumnType.Number,
    ["НазваниеАнализа"] = ColumnType.Text, ...
};
// Для плоского режима — маппинг имён (подзапросные → алиасы таблиц)
private static readonly Dictionary<string, string> _filterFlatColumnMap = new()
{
    ["TestTypeName"] = "t.ТипМедицинскогоАнализа", ...
};

// В LoadFlatData:
var dp = new DynamicParameters();
dp.Add("search", $"%{_query.SearchText}%");
var colFilterWhere = _query.BuildColumnFilterClause(dp, _filterFlatColumnMap);
// Объединить searchWhere + colFilterWhere через AND

// В LoadGroupedData:
var dp = new DynamicParameters();
var colFilterWhere = _query.BuildColumnFilterClause(dp); // без маппинга
// colFilterWhere вставляется внутрь подзапроса _g
```

### `sqlName` для фильтрации в grouped-режиме
- В grouped-режиме фильтры применяются внутри подзапроса `FROM (SELECT ...) _g`, поэтому `ColumnFilter.Column` должен содержать выходное имя колонки (например, `"КодМедицинскогоАнализа"`, а не `"a.КодМедицинскогоАнализа"`)
- В плоском режиме — через `columnNameMap` имена преобразуются в алиасные

## KescoGridPageBase\<T>

Базовый класс Blazor-страницы. Инкапсулирует весь инфраструктурный код загрузки данных:
плоский режим (`LoadFlatData`) и режим группировки (`LoadGroupedData`).

Страница-наследник реализует только **6 свойств** (специфика сущности) и методы диалогов.

### Abstract/virtual свойства

| Свойство | Тип | Обязательно | Описание |
|---|---|---|---|
| `SelectSql` | `string` | ✓ | Базовый SELECT SQL (без WHERE/ORDER BY) |
| `FlatSearchColumns` | `string[]` | ✓ | Колонки поиска для плоского режима (с алиасами `a.`/`t.`) |
| `GroupedSearchColumns` | `string[]` | ✓ | Колонки поиска для группированного режима (выходные имена) |
| `DefaultOrder` | `string` | ✓ | ORDER BY по умолчанию |
| `GroupExprMap` | `Dictionary<string, string>` | ✓ | Маппинг GroupColumns → выходные имена подзапроса |
| `FilterFlatColumnMap` | `Dictionary<string, string>?` | — | Маппинг фильтров для плоского WHERE (`null` — не нужен) |
| `FilterColumnTypes` | `IReadOnlyDictionary<string, ColumnType>` | — | Типы колонок (авто-вычисляется через рефлексию по `[Column]` и C#-типам) |

### Инжектируемые сервисы

`DbManager Db` и `ISnackbar Snackbar` — инжектируются автоматически, объявлять на странице не нужно.

### Шаблон страницы

```razor
@page "/my-entity"
@using Kesco.Lib.Web.BZ.Controls
@using MudBlazor.Extensions
@using MudBlazor.Extensions.Options
@inherits KescoGridPageBase<MyEntity>

@inject IDialogService DialogService
@inject KescoAppSettings AppSettings

<KescoGrid TEntity="IGridRow"
           @ref="_dataGrid"
           Title="Мои записи"
           Items="_rows"
           Loading="_loading"
           PageSize="@AppSettings.DefaultPageSize"
           FilterColumnTypes="@FilterColumnTypes"
           TotalCount="@_query.TotalCount"
           ShowPagination="true"
           OnAdd="OpenAddDialog"
           OnRowClick="OnRowClicked"
           OnQueryChanged="OnQueryChanged">

    <ColumnDefs>
        <KescoColumnDef SqlName="SqlCol"           DisplayName="Код"      Groupable="true" Filterable="true" />
        <KescoColumnDef SqlName="НазваниеКолонки"  DisplayName="Название" Groupable="true" Filterable="true" />
    </ColumnDefs>

    <Columns>

    <TemplateColumn T="IGridRow" Title="Код" Sortable="false"
                    Hidden="@(_dataGrid?.GroupColumns.Contains("SqlCol") ?? false)">
        <HeaderTemplate>
            <div style="display:flex;align-items:center;width:100%;cursor:pointer"
                 draggable="true"
                 @ondragstart="@(e => { e.DataTransfer.EffectAllowed = "move"; KescoDragState.DraggedColumn = "SqlCol"; })"
                 @onclick="@(async () => { if (_dataGrid is not null) await _dataGrid.ToggleSort("SqlCol"); })">
                <MudText Style="flex:1;text-align:center">Код</MudText>
                @if (_dataGrid is not null) { @_dataGrid.GetSortBadge("SqlCol") }
            </div>
        </HeaderTemplate>
        <CellTemplate>
            @if (context.Item is GroupHeaderRow header)
            {
                <MudStack Row="true" AlignItems="AlignItems.Center" Style="@($"padding-left:{header.Depth * 16}px")">
                    <MudIconButton Icon="@(header.IsExpanded ? Icons.Material.Filled.ExpandMore : Icons.Material.Filled.ChevronRight)"
                                   Size="Size.Small"
                                   OnClick="() => ToggleGroup(header)"
                                   Style="padding:0;width:22px;height:22px;min-width:22px" />
                    <MudText Style="font-weight:600;font-size:0.875rem">@header.DisplayValue (@header.ItemCount шт.)</MudText>
                </MudStack>
            }
            else if (context.Item is DetailRow<MyEntity> detail)
            {
                <MudText Style="@($"padding-left:{(detail.Depth + 1) * 16}px")">@detail.Item.Id</MudText>
            }
        </CellTemplate>
    </TemplateColumn>

    <TemplateColumn T="IGridRow" Title="Название" Sortable="false"
                    Hidden="@(_dataGrid?.GroupColumns.Contains("НазваниеКолонки") ?? false)">
        <HeaderTemplate>
            <div style="display:flex;align-items:center;width:100%;cursor:pointer"
                 draggable="true"
                 @ondragstart="@(e => { e.DataTransfer.EffectAllowed = "move"; KescoDragState.DraggedColumn = "НазваниеКолонки"; })"
                 @onclick="@(async () => { if (_dataGrid is not null) await _dataGrid.ToggleSort("НазваниеКолонки"); })">
                <MudText Style="flex:1;text-align:center">Название</MudText>
                @if (_dataGrid is not null) { @_dataGrid.GetSortBadge("НазваниеКолонки") }
            </div>
        </HeaderTemplate>
        <CellTemplate>
            @if (context.Item is DetailRow<MyEntity> detail)
            {
                <MudText>@detail.Item.Name</MudText>
            }
        </CellTemplate>
    </TemplateColumn>

    </Columns>

</KescoGrid>

@code {
    private KescoGrid<IGridRow> _dataGrid = null!;

    // ── Настройки источника данных ──────────────────────────────────────────────
    protected override string SelectSql => SQLQueries.SELECT_МоиЗаписи;
    protected override string[] FlatSearchColumns    => ["a.НазваниеКолонки", "t.ДругаяКолонка"];
    protected override string[] GroupedSearchColumns => ["НазваниеКолонки", "ДругаяКолонка"];
    protected override string DefaultOrder           => "НазваниеКолонки";
    protected override Dictionary<string, string> GroupExprMap => _groupExprMap;
    protected override Dictionary<string, string>? FilterFlatColumnMap => _filterFlatColumnMap;

    // ── Конфигурация колонок ────────────────────────────────────────────────────
    private static readonly Dictionary<string, string> _groupExprMap = new()
    {
        ["НазваниеКолонки"] = "НазваниеКолонки",
    };
    private static readonly Dictionary<string, string> _filterFlatColumnMap = new()
    {
        ["НазваниеКолонки"] = "a.НазваниеКолонки",
    };

    // ── События грида ───────────────────────────────────────────────────────────
    private Task OnQueryChanged(KescoDataQuery q) => OnQueryChangedBase(q);

    // ── Диалоги ─────────────────────────────────────────────────────────────────
    private async Task OpenAddDialog() { /* ... */ }

    private async Task OnRowClicked(DataGridRowClickEventArgs<IGridRow> args)
    {
        if (args.Item is GroupHeaderRow header) { await ToggleGroup(header); return; }
        if (args.Item is DetailRow<MyEntity> detail) { /* открыть диалог редактирования */ }
    }
}
```

## Состояния

- **Поиск** — сбрасывает страницу на 1, вызывает `OnQueryChanged` с debounce 300 мс
- **Сортировка** — до 2 колонок, циклически: ASC → DESC → убрать. Сбрасывает страницу на 1. Сортировка по чипу в трее также работает (направление учитывается в `GROUP BY ... ORDER BY`). **`ToggleSort` возвращает `Task` — вызывать только через `await`**, иначе Blazor не дождётся перезагрузки данных
- **Группировка (tray)** — панель над гридом, скрытая по умолчанию. Открывается кнопкой `AccountTree` в тулбаре (класс `grouping-toggle-btn`). Добавление колонок — перетаскивание заголовка на панель. Удаление — клик по × на чипе. Изменение порядка — перетаскивание чипов. Сортировка по чипу — клик по его названию (бейдж `chip-sort-badge`). При любом изменении сбрасывается страница на 1
- **Фильтрация (tray)** — панель над гридом, скрытая по умолчанию. Открывается кнопкой `FilterAlt` в тулбаре (класс `filter-toggle-btn`). Добавление фильтра — перетаскивание заголовка на панель → открывается `ColumnFilterDialog`. Редактирование — клик по чипу. Удаление — клик по × на чипе. При выключении трея все фильтры сбрасываются. При любом изменении сбрасывается страница на 1
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
| Фильтрация | `FilterAlt` | `filter-toggle-btn` / `filter-toggle-btn--active` | Показывает/скрывает панель фильтрации |
| Добавить | `Add` | `toolbar-add-btn` | Вызывает `OnAdd` |

## Стилизация панелей

Панели группировки и фильтрации имеют идентичное визуальное оформление. CSS определён в `wwwroot/css/app.css`.

| Элемент | CSS-класс | Свойства |
|---|---|---|
| Панель группировки | `.grouping-tray` | `border-left: 3px solid var(--lh-navy)`, `border-bottom: 2px solid var(--lh-gold)`, фон `var(--lh-offwhite)` |
| Панель фильтрации | `.filter-tray` | **Идентично** `.grouping-tray` — левая и нижняя границы совпадают |
| Иконка группировки | `.grouping-tray-icon` | `color: var(--lh-navy)`, `opacity: 0.45` |
| Иконка фильтрации | `.filter-tray-icon` | **Идентично** `.grouping-tray-icon` |
| Hover / drag-over | `:has(...:hover)`, `.drag-over` | `border-left-color: var(--lh-gold)` для обеих панелей |
| Чип группировки | `.grouping-chip` | `background: var(--lh-navy)`, белый текст |
| Чип фильтра | `.filter-chip` | `background: rgba(5, 22, 77, 0.72)`, белый текст, `border-bottom: 2px solid var(--lh-gold)` |
