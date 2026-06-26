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
| `PageNumber` | `int` | `1` | Текущий номер страницы (передаётся из страницы). **Обязателен** для синхронизации пагинатора при авто-переходах в `ToggleGroup` |
| `Columns` | `RenderFragment?` | — | Колонки грида — `<KescoColumn>` компоненты |
| `ColumnDefs` | `RenderFragment?` | — | Метаданные колонок — `<KescoColumnDef>` компоненты для регистрации группируемых/фильтруемых колонок |
| `FilterColumnTypes` | `IReadOnlyDictionary<string, ColumnType>` | `[]` | Тип данных фильтруемых колонок: SQL-имя → `ColumnType`. Авто-вычисляется в `KescoGridPageBase` через рефлексию |
| `SelectSql` | `string` | `""` | Базовый SELECT SQL (без WHERE/ORDER BY) |
| `SearchColumns` | `string[]` | `[]` | Выходные имена колонок для полнотекстового поиска |
| `DefaultOrder` | `string` | `""` | Порядок сортировки по умолчанию (например, `"Порядок, НазваниеАнализа"`) |
| `EditDialogType` | `Type?` | `null` | Тип диалога редактирования/добавления. Должен принимать параметр `Model` типа сущности |
| `DataLoader` | `IKescoGridDataLoader?` | `null` | Загрузчик данных. Страница передаёт `DataLoader="this"` |
| `ColumnMenuMode` | `ColumnMenuMode` | `Mobile` | Режим кнопки меню (⋮) в заголовках: `Hidden` — скрыта, `Always` — всегда видна, `Mobile` — только на мобильных (≤960px) |
| `SelectVisible` | `bool` | `false` | Показать кнопку выбора записей (чекбоксы + меню групповых операций) |
| `ShowPrint` | `bool` | `false` | Показать группу «Печать» в меню групповых операций (текущая страница и все данные реализованы) |
| `ShowExcel` | `bool` | `false` | Показать группу «Выгрузка в Excel» в меню групповых операций. При экспорте рядом с заголовком показывается спиннер |
| `CustomBatchGroups` | `IReadOnlyList<BatchOperationGroup>?` | `null` | Кастомные группы операций (рендерятся после стандартных) |
| `OnAdd` | `EventCallback` | — | Обработчик кнопки «Добавить» |
| `OnRowClick` | `EventCallback<DataGridRowClickEventArgs<TEntity>>` | — | Клик по строке |
| `AllowColumnReorder` | `bool` | `true` | Разрешить перетаскивание колонок грида мышью |

Удалённые параметры (больше не используются):
- ~~`OnPrintCurrentPage`, `OnPrintAll`, `OnPrintSelected`~~ — заменены на `ShowPrint`
- ~~`OnExcelCurrentPage`, `OnExcelAll`, `OnExcelSelected`~~ — заменены на `ShowExcel`
- ~~`Groupable`~~ — группировка выполняется сервером, не MudBlazor
- ~~`GroupExpanded`~~ — состояние развёрнутости per-group, не глобальное
- ~~`GroupColumn`~~ — заменён на `GroupColumns` (множественная группировка)
- ~~`ShowGroupToggle`~~ / ~~`GroupToggleLabel`~~ — старый toggle-режим удалён
- ~~`ShowGroupingTray`~~ / ~~`AvailableGroupColumns`~~ — заменены на `KescoColumnDef` в `<ColumnDefs>`
- ~~`ShowFilterTray`~~ / ~~`AvailableFilterColumns`~~ — заменены на `KescoColumnDef` в `<ColumnDefs>`
- ~~`ChildContent`~~ — переименован в `Columns`, заодно добавлен `ColumnDefs`
- ~~`OnQueryChanged`~~ — заменён на `DataLoader` (IKescoGridDataLoader)

## Публичные методы

| Метод | Описание |
|---|---|
| `async Task ToggleSort(string sqlCol)` | Циклически переключает сортировку: ASC → DESC → убрать. Возвращает `Task` — **обязательно awaitable** |
| `GetSortBadge(string sqlCol)` | Возвращает `RenderFragment` с бейджем сортировки (номер + стрелка) |
| `RefreshAsync()` | Сбрасывает номер страницы на 1 и вызывает `OnQueryChanged` |
| `GroupColumns` | `IReadOnlyList<string>` — текущий список SQL-имён колонок в трее группировки |
| `IsGrouped(string sqlName)` | Возвращает `true`, если колонка участвует в группировке |
| `GetColumnMeta(string sqlName)` | Метаданные колонки по SQL-имени |
| `GetColumnMetaById(int columnId)` | Метаданные колонки по числовому `ColumnId` |
| `GetGroupByOrder(string sqlColumn)` | Порядок группировки для SQL-колонки (позиция в трее) |
| `AddGroupAsync(string sqlName)` | Добавляет колонку в трей группировки (альтернатива drag-and-drop) |
| `AddFilterAsync(string sqlName)` | Открывает диалог фильтрации для колонки (альтернатива drag-and-drop) |

## KescoColumnDef

Невидимый компонент-регистратор метаданных колонки. Размещается внутри `<ColumnDefs>`. При инициализации регистрирует метаданные в `IKescoGrid` через каскадный параметр.

| Параметр | Тип | По умолчанию | Обязательный | Описание |
|---|---|---|---|---|
| `ColumnId` | `int` | — | ✓ `EditorRequired` | Числовой идентификатор — связь с `KescoColumn` |
| `SqlName` | `string` | `""` | — | SQL-имя колонки — выходное имя из SELECT |
| `DisplayName` | `string` | `""` | — | Отображаемое имя (заголовок колонки, чипы треев) |
| `SortName` | `string?` | `null` | — | Имя для ORDER BY. Если `null` — используется `SqlName` |
| `Groupable` | `bool` | `false` | — | Разрешить группировку по колонке |
| `Filterable` | `bool` | `false` | — | Разрешить фильтрацию по колонке |

## KescoColumn\<TEntity>

Колонка грида с автоматически построенным заголовком. Получает метаданные (`DisplayName`, `SqlName`, `SortName`, `Groupable`) из зарегистрированного `KescoColumnDef` по числовому `ColumnId`.

**Авто-возможности:**
- Title из `KescoColumnDef.DisplayName`
- HeaderTemplate с серверной сортировкой (`Grid.ToggleSort`)
- Заголовок содержит `data-col-sql` для кастомного insert-drag (через `kescoGridColumnDrag.js`)
- `KescoDragState.DraggedColumn` устанавливается через JS→C# `SetDraggedColumn` при dragstart — tray-drop продолжает работать
- Автоматическое скрытие колонки при группировке (`Hidden = IsGrouped(SqlName)`)
- Кнопка меню `⋮` (мобильные / `ColumnMenuMode`) — альтернативный вход для группировки и фильтрации без drag-and-drop
- **`DragAndDropEnabled="false"`** — MudBlazor `DragDropColumnReordering` **не используется**. Перемещение колонок реализовано кастомным JS с insert-семантикой (вставка перед/после)

| Параметр | Тип | Обязательный | Описание |
|---|---|---|---|
| `ColumnId` | `int` | ✓ `EditorRequired` | Числовой идентификатор — связь с `KescoColumnDef` |
| `CellTemplate` | `RenderFragment<CellContext<TEntity>>?` | — | Шаблон содержимого ячейки |

## KescoGroupHeader

Стандартный заголовок строки группировки. Отображает иконку раскрытия/сворачивания, название группы и количество элементов.

| Параметр | Тип | Описание |
|---|---|---|
| `Header` | `GroupHeaderRow` | Данные строки заголовка группы |
| `OnToggle` | `EventCallback<GroupHeaderRow>` | Вызывается при клике на иконку раскрытия/сворачивания |

## KescoDragState

Статическое хранилище имени перетаскиваемой SQL-колонки между dragstart и drop. Используется вместо `DataTransfer.GetData`, недоступного в Blazor .NET.

```csharp
public static class KescoDragState
{
    public static string? DraggedColumn { get; set; }
}
```

## KescoDataQuery

Класс состояния запроса, передаваемый в `IKescoGridDataLoader.OnQueryChangedAsync()`:

- `SearchText` — текст поиска
- `GroupEnabled` — включена ли группировка
- `GroupColumns` — список SQL-имён колонок группировки в порядке приоритета
- `ExpandedGroups` — `HashSet<string>` полных ключей развёрнутых групп (разделитель `\u001F`)
- `SortColumns` — список `SortColumn(Column, Desc)`
- `PageNumber` — номер текущей страницы (1-based)
- `PageSize` — размер страницы
- `TotalCount` — общее число записей (заполняется страницей после загрузки)
- `ColumnFilters` — `Dictionary<string, ColumnFilter>` — фильтры по колонкам, ключ = SQL-имя колонки
- `BuildColumnFilterClause(DynamicParameters, columnNameMap?)` — строит `WHERE` из `ColumnFilters`, добавляет параметры в `DynamicParameters`
- `BuildOrderBy(defaultOrder)` — строит `ORDER BY`; при включённой группировке все `GroupColumns` идут первыми
- `BuildWhereClause(searchColumns)` — строит `WHERE ... LIKE @search`
- `CombineWhere(string?, string?)` — объединяет два WHERE-фрагмента через `AND`

## IKescoGrid

Интерфейс, реализуемый `KescoGrid<TEntity>`. Используется тремя потребителями:
- `KescoColumnDef` — регистрация метаданных через каскадный параметр
- `KescoColumn<TEntity>` — поиск метаданных по `ColumnId` для авто-заголовка
- `KescoGridPageBase<T>` — чтение SQL-настроек грида

| Член | Тип | Описание |
|---|---|---|
| `SelectSql` | `string` | Базовый SELECT |
| `SearchColumns` | `string[]` | Колонки поиска (выходные имена) |
| `DefaultOrder` | `string` | ORDER BY по умолчанию |
| `EditDialogType` | `Type?` | Тип диалога |
| `IsGrouped(sqlName)` | `bool` | Участвует ли в группировке |
| `ToggleSort(sqlName)` | `Task` | Переключение сортировки |
| `GetSortBadge(sqlName)` | `RenderFragment` | Бейдж сортировки |
| `GetColumnMeta(sqlName)` | `KescoColumnMeta?` | Метаданные по SQL-имени |
| `GetColumnMetaById(columnId)` | `KescoColumnMeta?` | Метаданные по ID |
| `RegisterColumn(columnId, sqlName, displayName, groupable, filterable, sortName?)` | `void` | Регистрация колонки |
| `UnregisterColumn(columnId, sqlName)` | `void` | Отмена регистрации |
| `ColumnsChanged` | `event Action?` | Событие изменения реестра |
| `TrayStateChanged` | `event Action?` | Событие открытия/закрытия панелей |
| `ColumnMenuMode` | `ColumnMenuMode` | Режим кнопки ⋮ (Hidden/Always/Mobile) |
| `IsGroupingTrayExpanded` | `bool` | Открыта ли панель группировки |
| `IsFilterTrayExpanded` | `bool` | Открыта ли панель фильтрации |
| `AddGroupAsync(sqlName)` | `Task` | Добавить колонку в трей группировки |
| `AddFilterAsync(sqlName)` | `Task` | Открыть диалог фильтра для колонки |

## KescoColumnMeta

Метаданные зарегистрированной колонки (readonly, init-only свойства):

| Свойство | Тип | Описание |
|---|---|---|
| `ColumnId` | `int` | Числовой идентификатор |
| `SqlName` | `string` | SQL-имя (выходное имя SELECT) |
| `DisplayName` | `string` | Отображаемое имя |
| `SortName` | `string` | Имя для ORDER BY (по умолчанию = SqlName) |
| `Groupable` | `bool` | Разрешена группировка |
| `Filterable` | `bool` | Разрешена фильтрация |

## Серверная группировка

Группировка выполняется **на стороне SQL Server** (не MudBlazor). Реализация — `KescoGroupingEngine` (статический класс). Два отдельных запроса:

1. **Групповые агрегаты**: `GROUP BY` + `COUNT(*)`, возвращает уникальные значения и количество записей
2. **Детальные строки**: выборка с `ROW_NUMBER()` и фильтром по значениям группы

### Модель данных

- `IGridRow` — маркерный интерфейс строки в плоском списке
- `GroupHeaderRow` — заголовок группы с `FullKey`, `DisplayValue`, `ItemCount`, `Depth`, `IsExpanded`
- `DetailRow<T>` — обёртка сущности с `Item`, `GroupKey`, `Depth`
- `GroupedPage<T>` — результат: `Rows` (плоский список `IGridRow`) + `TotalEffectiveRows`

### Рендеринг групп

Колонки используют `<KescoColumn>` с проверкой типа в `CellTemplate`:

```razor
<KescoColumn TEntity="IGridRow" ColumnId="2">
    <CellTemplate>
        @if (context.Item is GroupHeaderRow header)
        {
            <KescoGroupHeader Header="header" OnToggle="ToggleGroup" />
        }
        else if (context.Item is DetailRow<MedicalTest> detail)
        {
            <MudText Style="@($"padding-left:{(detail.Depth + 1) * 16}px")">@detail.Item.Id</MudText>
        }
    </CellTemplate>
</KescoColumn>
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

### Имена колонок в WHERE и GROUP BY

- `SearchColumns` передаются как выходные имена (например, `"НазваниеАнализа"`, `"TestTypeName"`)
- SQL-пагинация через `ROW_NUMBER()` оборачивает SELECT в подзапрос `FROM (SELECT ...) _src`, где выходные имена колонок видны напрямую — алиасы таблиц не нужны
- `GroupColumns` содержат те же выходные имена — они напрямую используются в `GROUP BY`

## Серверная фильтрация по колонкам

Фильтрация по колонкам выполняется **на стороне SQL Server** через `BuildColumnFilterClause`.
UI — панель фильтров (filter tray) с drag-and-drop заголовков и диалогом `KescoColumnFilterDialog` для настройки условий.

### Модель данных
- `ColumnType` — тип данных колонки: `Text` (10 операторов: Contains/NotContains/Equals/NotEquals/StartsWith/NotStartsWith/EndsWith/NotEndsWith/IsEmpty/IsNotEmpty), `Number` (равенство + сравнения >/</>=/<=), `Boolean` (Equals)
- `ColumnFilterOperator` — оператор сравнения: `Contains`, `NotContains`, `Equals`, `NotEquals`, `StartsWith`, `NotStartsWith`, `EndsWith`, `NotEndsWith`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `IsEmpty`, `IsNotEmpty`
- `LogicalOperator` — `And` / `Or` для объединения двух условий на одной колонке
- `ColumnFilter` — условие фильтра: `Column` (SQL-имя), `ParamName` (имя Dapper-параметра), `Operator`, `Value` + опциональные `LogicalOperator`, `SecondOperator`, `SecondValue`, `SecondParamName` (до двух условий на колонку)
- `KescoDataQuery.ColumnFilters` — `Dictionary<string, ColumnFilter>` — ключ = SQL-имя колонки

### SQL-генерация
- `KescoDataQuery.BuildColumnFilterClause(DynamicParameters parameters, Dictionary<string, string>? columnNameMap)` — генерирует WHERE-фрагмент (`col LIKE @p` / `col = @p` / `col > @p` и т.д.) и добавляет параметры в `DynamicParameters`
- `columnNameMap` — опциональный маппинг имён для плоского режима, где имена колонок в SELECT отличаются от подзапросного режима

### Filter tray
- Панель включается кнопкой `FilterAlt`, скрыта по умолчанию. Кнопка появляется автоматически при наличии хотя бы одного `KescoColumnDef` с `Filterable="true"`
- Добавление фильтра: перетаскивание заголовка колонки (KescoColumn автоматически поддерживает drag) на панель → открывается `KescoColumnFilterDialog`
- Редактирование: клик по чипу фильтра → повторно открывается диалог с текущими значениями
- Удаление: клик по × на чипе
- При выключении панели все фильтры сбрасываются, данные перезагружаются
- Чип показывает читаемое описание: `«Название содержит «грипп»»` или для двух условий `«Название: содержит «грипп» И не содержит «ковид»»` (через `KescoColumnFilterDialog.GetFilterDescription`)
- Filter tray не конфликтует с grouping tray — оба могут быть открыты одновременно

### Интеграция на странице

Конфигурация SQL передаётся через параметры `<KescoGrid>`. `KescoGridPageBase` автоматически читает их из `IKescoGrid`:

```razor
<KescoGrid TEntity="IGridRow"
           DataLoader="this"
           SelectSql="@SQLQueries.SELECT_МоиЗаписи"
           SearchColumns="@(new[]{"НазваниеАнализа","TestTypeName"})"
           DefaultOrder="Порядок, НазваниеАнализа"
           EditDialogType="@typeof(MyEditDialog)"
           FilterColumnTypes="@FilterColumnTypes"
           ... >
```

В плоском и группированном режимах используются одни и те же `SearchColumns` — выходные имена колонок видны в подзапросе `ROW_NUMBER()`. `FilterColumnTypes` вычисляется автоматически через рефлексию.

## KescoGridPageBase\<T>

Базовый класс Blazor-страницы. Инкапсулирует весь инфраструктурный код загрузки данных:
плоский режим и режим группировки.

### Паттерн использования

Страница-наследник:
1. Наследуется: `@inherits KescoGridPageBase<MyEntity>`
2. Передаёт SQL-конфигурацию в параметры `<KescoGrid>`: `SelectSql`, `SearchColumns`, `DefaultOrder`, `EditDialogType`
3. Передаёт `DataLoader="this"` — подключает `IKescoGridDataLoader`
4. Переопределяет свойство `Grid`: `protected override IKescoGrid? Grid => _dataGrid;`
5. Объявляет поле `private KescoGrid<IGridRow> _dataGrid = null!;` для `@ref`

### Virtual-свойства (могут быть переопределены)

| Свойство | Тип | По умолчанию | Описание |
|---|---|---|---|
| `Grid` | `IKescoGrid?` | `null` | Ссылка на грид — **обязательно переопределить**: `protected override IKescoGrid? Grid => _dataGrid;` |
| `AddSuccessMessage` | `string` | `"Запись добавлена"` | Текст уведомления после добавления |
| `SaveSuccessMessage` | `string` | `"Запись обновлена"` | Текст уведомления после сохранения |
| `FilterColumnTypes` | `IReadOnlyDictionary<string, ColumnType>` | Авто-вычисляется | Типы колонок (рефлексия по `[Column]` и C#-типам) |

### Инжектируемые сервисы

`DbManager Db`, `ISnackbar Snackbar` и `IDialogService DialogService` — инжектируются автоматически, объявлять на странице не нужно.

### Методы (не переопределяются)

| Метод | Описание |
|---|---|
| `LoadData()` | Диспетчер: вызывает LoadGroupedData или LoadFlatData в зависимости от состояния группировки |
| `ToggleGroup(GroupHeaderRow)` | Раскрытие/сворачивание группы с авто-пагинацией |
| `OpenAddDialog()` | Открывает диалог добавления (тип из `EditDialogType`) |
| `OnRowClicked(DataGridRowClickEventArgs<IGridRow>)` | Обработчик клика по строке: группа → ToggleGroup, деталь → диалог редактирования |

### Поля (protected, доступны в разметке)

| Поле | Тип | Описание |
|---|---|---|
| `_query` | `KescoDataQuery` | Текущее состояние запроса |
| `_rows` | `List<IGridRow>` | Строки текущей страницы |
| `_loading` | `bool` | Признак загрузки |

### Шаблон страницы

```razor
@page "/my-entity"
@using Kesco.Lib.Web.Settings
@using Kesco.Lib.Web.BZ.Controls
@inherits KescoGridPageBase<MyEntity>
@inject KescoAppSettings AppSettings

<PageTitle>Мои записи</PageTitle>

<KescoGrid TEntity="IGridRow"
           @ref="_dataGrid"
           DataLoader="this"
           Title="Мои записи"
           SelectSql="@SQLQueries.SELECT_МоиЗаписи"
           SearchColumns="@(new[]{"НазваниеАнализа","TestTypeName"})"
           DefaultOrder="Порядок, НазваниеАнализа"
           EditDialogType="@typeof(MyEditDialog)"
           Items="_rows"
           Loading="_loading"
           PageSize="@AppSettings.DefaultPageSize"
           FilterColumnTypes="@FilterColumnTypes"
            TotalCount="@_query.TotalCount"
            PageNumber="@_query.PageNumber"
            ShowPagination="true"
            OnAdd="OpenAddDialog"
            OnRowClick="OnRowClicked">

    <ColumnDefs>
        <KescoColumnDef ColumnId="1" SqlName="TestTypeName"            DisplayName="Тип"      Groupable="true" Filterable="true" />
        <KescoColumnDef ColumnId="2" SqlName="КодЗаписи"              DisplayName="Код"      Groupable="true" Filterable="true" />
        <KescoColumnDef ColumnId="3" SqlName="НазваниеЗаписи"         DisplayName="Название" Groupable="true" Filterable="true" />
    </ColumnDefs>

    <Columns>

        <KescoColumn TEntity="IGridRow" ColumnId="2">
            <CellTemplate>
                @if (context.Item is GroupHeaderRow header)
                {
                    <KescoGroupHeader Header="header" OnToggle="ToggleGroup" />
                }
                else if (context.Item is DetailRow<MyEntity> detail)
                {
                    <MudText Style="@($"padding-left:{(detail.Depth + 1) * 16}px")">@detail.Item.Id</MudText>
                }
            </CellTemplate>
        </KescoColumn>

        <KescoColumn TEntity="IGridRow" ColumnId="3">
            <CellTemplate>
                @if (context.Item is DetailRow<MyEntity> detail)
                {
                    <MudText>@detail.Item.Name</MudText>
                }
            </CellTemplate>
        </KescoColumn>

        <KescoColumn TEntity="IGridRow" ColumnId="1">
            <CellTemplate>
                @if (context.Item is DetailRow<MyEntity> detail)
                {
                    <MudText>@detail.Item.TestTypeName</MudText>
                }
            </CellTemplate>
        </KescoColumn>

    </Columns>

</KescoGrid>

@code {
    private KescoGrid<IGridRow> _dataGrid = null!;
    protected override IKescoGrid? Grid => _dataGrid;
}
```

## Состояния

- **Поиск** — сбрасывает страницу на 1, вызывает `OnQueryChangedAsync` с debounce 300 мс
- **Сортировка** — до 2 колонок, циклически: ASC → DESC → убрать. Сбрасывает страницу на 1. Сортировка по чипу в трее также работает (направление учитывается в `GROUP BY ... ORDER BY`). **`ToggleSort` возвращает `Task` — вызывать только через `await`**, иначе Blazor не дождётся перезагрузки данных
- **Группировка (tray)** — панель над гридом, скрытая по умолчанию. Открывается кнопкой `AccountTree` в тулбаре (класс `grouping-toggle-btn`). Добавление колонок — перетаскивание заголовка на панель (drag встроен в `KescoColumn`). Удаление — клик по × на чипе. Изменение порядка — перетаскивание чипов. Сортировка по чипу — клик по его названию (бейдж `chip-sort-badge`). При любом изменении сбрасывается страница на 1
- **Фильтрация (tray)** — панель над гридом, скрытая по умолчанию. Открывается кнопкой `FilterAlt` в тулбаре (класс `filter-toggle-btn`). Добавление фильтра — перетаскивание заголовка (drag встроен в `KescoColumn`) на панель → открывается `KescoColumnFilterDialog`. Редактирование — клик по чипу. Удаление — клик по × на чипе. При выключении трея все фильтры сбрасываются. При любом изменении сбрасывается страница на 1
- **Сворачивание/разворачивание группы** — НЕ сбрасывает страницу на 1. Если детали не влезают — авто-переход вперёд
- **Смена размера страницы** — числовое поле (1–999). Сбрасывает страницу на 1
- **Кнопка «Обновить»** — сбрасывает страницу на 1, перезагружает данные
- **Переход по страницам** — кнопки `|<`, `<`, `>`, `>|`. Не сбрасывают фильтры
- **Защита выхода за границы** — при уменьшении `TotalCount` номер страницы автоматически обрезается до максимального
- **Экспорт в Excel** — на время выгрузки рядом с заголовком грида показывается `MudProgressCircular` (Size.Small, Indeterminate). Флаг `_isExporting` устанавливается в `true` перед вызовом `DataLoader.ExcelExportAsync()` и сбрасывается в `false` в `finally`-блоке. После завершения появляется снекбар с именем файла или ошибкой
- **Печать всех данных** — `_isExporting` = true → `DataLoader.BuildPrintHtmlAsync(columns, title, ...)` (загружает все строки в отдельный список, не трогая `_rows`) → `KescoGridPrintHtmlGenerator.Build()` (генерирует HTML с инлайн-стилями) → `kescoGridPrint.printHtml(html)` (рендерит в скрытый iframe, печатает, удаляет). Грид НЕ модифицируется. Свёрнутые группы печатаются только заголовком

## Кнопки тулбара

Все кнопки в строке заголовка — `MudIconButton` с тултипом. Не использовать `MudButton Variant.Filled`.

| Кнопка | Иконка | CSS-класс | Поведение |
|---|---|---|---|
| Группировка | `AccountTree` | `grouping-toggle-btn` / `grouping-toggle-btn--active` | Показывает/скрывает панель tray |
| Фильтрация | `FilterAlt` | `filter-toggle-btn` / `filter-toggle-btn--active` | Показывает/скрывает панель фильтрации |
| Добавить | `Add` | `toolbar-add-btn` | Вызывает `OnAdd` |
| Выбрать записи | `CheckBox` | `toolbar-select-btn` / `toolbar-select-btn--active` | Включает/выключает чекбоксы в строках |
| Групповые операции | `PlaylistAddCheck` | `toolbar-batch-btn` | Меню: Печать / Excel (текущая страница, выбранные, все данные) |

## Стилизация панелей

Панели группировки и фильтрации имеют идентичное визуальное оформление. Все цвета используют MudBlazor-переменные палитры (`--mud-palette-*`) — автоматически адаптируются к светлой/тёмной теме. CSS определён в `wwwroot/css/app.css`.

| Элемент | CSS-класс | Свойства |
|---|---|---|
| Панель группировки | `.grouping-tray` | `border-left: 3px solid var(--mud-palette-primary)`, `border-bottom: 2px solid var(--lh-gold)`, фон `var(--mud-palette-background-gray)`, обводка `var(--mud-palette-lines-default)` |
| Панель фильтрации | `.filter-tray` | **Идентично** `.grouping-tray` |
| Иконка (неактивна) | `.grouping-tray-icon`, `.filter-tray-icon` | `color: var(--mud-palette-text-secondary)`, `opacity: 0.45` |
| Иконка (активна) | `.grouping-tray:has(.grouping-chip) .grouping-tray-icon`, `.filter-tray:has(.filter-chip) .filter-tray-icon` | `color: var(--mud-palette-primary)`, `opacity: 1` — когда в трее есть хотя бы один чип |
| Hover / drag-over | `:has(...:hover)`, `.drag-over` | `background: var(--mud-palette-surface)`, `border-left-color: var(--lh-gold)` |
| Чип группировки | `.grouping-chip` | `background: var(--lh-navy)`, белый текст, `border-bottom: 2px solid transparent`; hover: фон `#0A1D6B` + золотой border-bottom |
| Чип фильтрации | `.filter-chip` | **Идентично** `.grouping-chip` (сплошной navy фон, hover с золотым подчёркиванием), но `cursor: default` |

## Групповые операции

Меню групповых операций открывается кнопкой `PlaylistAddCheck` при `SelectVisible="true"`.

### Стандартные операции

Включаются флагами без написания кода на странице:

| Параметр | Тип | По умолчанию | Описание |
|---|---|---|---|
| `ShowPrint` | `bool` | `false` | Группа «Печать»: текущая страница (реализована), все данные (реализована), выбранные (заглушка) |
| `ShowExcel` | `bool` | `false` | Группа «Выгрузка в Excel»: текущая страница, выбранные, все данные. На время экспорта рядом с заголовком грида показывается `MudProgressCircular` |

```razor
<KescoGrid ... SelectVisible="true" ShowPrint="true" ShowExcel="true" />
```

#### Экспорт в Excel

Реализован через два компонента:
- **`KescoGridExcelGenerator`** (сервер) — генерирует .xlsx через ClosedXML в цветах Kesco (navy `#05164D`, gold `#FFAD00`), шрифт Verdana. Поддерживает: заголовок, описание фильтров/группировки, групповые строки с Excel Outline (сворачиваемые группы), авто-ширину колонок
- **`kescoGridExcel.js`** (клиент) — скачивает файл через Blob URL из base64-контента

Поток: `ExcelCurrentPageInternal()` / `ExcelAllInternal()` / `ExcelSelectedInternal()` → `DataLoader.ExcelExportAsync(ExcelExportRequest)` → `KescoGridExcelGenerator.ExportToExcel(...)` → base64 → `kescoGridExcel.downloadFile()` → снекбар.

**Индикатор загрузки**: флаг `_isExporting` в `KescoGrid.razor` устанавливается в `true` перед вызовом `DataLoader.ExcelExportAsync()` и сбрасывается в `false` в `finally`-блоке. Пока `_isExporting = true`, рядом с заголовком грида (`MudText Typo="Typo.h5"`) показывается `MudProgressCircular Color="Color.Primary" Indeterminate="true" Size="Size.Small"`.

Режимы `Selected` и `All` — TODO (пока выгружают текущую страницу).

#### Печать всех данных

Реализована через три компонента:
- **`KescoGridPageBase.BuildPrintHtmlAsync()`** (сервер) — загружает все строки в отдельный список (НЕ модифицирует `_rows`) и генерирует HTML
- **`KescoGridPrintHtmlGenerator.Build()`** (сервер) — строит самодостаточный HTML с инлайн-стилями (Ч/Б-таблица, тёмный header, `@page{landscape;15mm}`, `thead{display:table-header-group}`)
- **`kescoGridPrint.printHtml(html)`** (клиент) — создаёт скрытый iframe, пишет HTML, вызывает `iframe.contentWindow.print()`, удаляет iframe после `afterprint`

Поток: `PrintAllInternal()` → `DataLoader.BuildPrintHtmlAsync(columns, title, ...)` → `KescoGridPrintHtmlGenerator.Build()` → `kescoGridPrint.printHtml(html)`.

**Ключевое отличие от v1**: грид (`_rows`, `_dataKey`, `_query`, `ExpandedGroups`) полностью не затрагивается. Печать изолирована в iframe — никакого восстановления страницы не требуется.

**Индикатор загрузки**: `MudProgressCircular` у заголовка (`_isExporting`).

**Плоский режим**: SQL `SELECT * FROM (selectSql) _src WHERE ... ORDER BY ...` — без `ROW_NUMBER()`.

**Режим группировки**: `WalkTree` с `pageStart=1, pageEnd=int.MaxValue` — всё дерево. Для развёрнутых листовых групп загружаются ВСЕ detail-строки. Свёрнутые группы — только заголовок.

### Кастомные операции

Параметр `CustomBatchGroups` (`IReadOnlyList<BatchOperationGroup>?`) — список кастомных групп, рендерятся после стандартных.

**`BatchOperationGroup`**:

| Свойство | Тип | Описание |
|---|---|---|
| `Label` | `string` | Заголовок группы |
| `Icon` | `string?` | Иконка Material Icons, опционально |
| `Operations` | `IReadOnlyList<BatchOperation>` | Список операций |

**`BatchOperation`**:

| Свойство | Тип | Описание |
|---|---|---|
| `Label` | `string` | Название |
| `Icon` | `string?` | Иконка, опционально |
| `RequiresSelection` | `bool` | Показывать только при выбранных строках |
| `RequiresAll` | `bool` | Показывать когда ничего не выбрано ИЛИ выбраны все |
| `OnExecute` | `Func<Task>?` | Обработчик (выполняется в приложении) |

```razor
@code {
    private static readonly IReadOnlyList<BatchOperationGroup> MyGroups = new[]
    {
        new BatchOperationGroup
        {
            Label = "Мои операции",
            Icon = Icons.Material.Filled.Settings,
            Operations = new[]
            {
                new BatchOperation
                {
                    Label = "Отправить выбранные",
                    Icon = Icons.Material.Filled.Send,
                    RequiresSelection = true,
                    OnExecute = async () => { /* ... */ }
                },
                new BatchOperation
                {
                    Label = "Архивировать всё",
                    RequiresAll = true,
                    OnExecute = async () => { /* ... */ }
                }
            }
        }
    };
}
```

## Диалог настройки колонок

Кнопка `ViewColumn` в тулбаре открывает `KescoColumnSettingsDialog` — диалог управления порядком, видимостью и сортировкой колонок.

### Drag-and-drop

Реализован как jQuery UI Sortable (нативные события `mousedown`/`mousemove`/`mouseup`, **не** HTML5 drag):
- **Ghost** (`.column-settings-ghost`) — клон чипа на `position:fixed`, следует за курсором
- **Placeholder** (`.column-settings-placeholder`) — dashed gold border на месте вставки, динамически вставляется в DOM
- **Авто-прокрутка** — при переполнении контейнера и приближении курсора к верхнему/нижнему краю (зона 40px) контейнер автоматически прокручивается. Скорость растёт пропорционально близости к краю (до 15px/фрейм). Ищется ближайший скроллируемый предок (`getScrollParent`), поскольку прокрутка находится на `DialogContent`, а не на контейнере чипов
- JS-логика в `kescoColumnSettings.js` (RCL), результат передаётся в C# через `[JSInvokable] OnJsDrop(sourceIdx, targetIdx)`

### Сортировка

Каждый чип позволяет настроить сортировку по колонке. Клик по области названия колонки или бейджа сортировки циклически переключает состояние: **нет сортировки → ASC (↑) → DESC (↓) → нет сортировки**.

- **До 2 колонок** в сортировке одновременно (приоритеты 1 и 2)
- **Бейдж сортировки** (`.chip-sort-badge`) — золотой фон, navy текст: `1↑` / `2↓`. Отображается справа от названия колонки. Идентичен бейджу в трее группировки
- **Курсор** `pointer` на всей зоне «название + бейдж» (`.sort-toggle-area`)
- **Область сортировки изолирована от drag-and-drop** — JS игнорирует `mousedown` внутри `.sort-toggle-area` и `.chip-label-clickable`
- **Применяется вместе с другими изменениями** при нажатии «Применить»: поля `SortPriority` и `IsSortDesc` на `ColumnSettingsItem` передаются обратно в грид

### Кнопки сброса

Две кнопки в левой части `DialogActions`:

| Кнопка | Иконка | Действие |
|---|---|---|
| «Сбросить сортировку» | `ClearAll` | Очищает `_dialogSortState` — все бейджи исчезают, сортировка возвращается к умолчанию |
| «Восстановить порядок и видимость по-умолчанию» | `RestartAlt` | Возвращает `_items` к снапшоту, сделанному при открытии диалога. Сбрасывает и сортировку, и порядок, и видимость |

### CSS-классы

| Класс | Описание |
|---|---|
| `.column-settings-chip` | Чип: navy фон, белый текст, `border-left: 3px solid transparent`; hover → золотой border-left |
| `.column-settings-ghost` | Клон, следующий за курсором: `opacity:0.88`, `box-shadow`, золотой border-left |
| `.column-settings-placeholder` | Маркер позиции вставки: `color-mix(in srgb, var(--lh-gold) 12%, transparent)`, dashed gold border |

### Применение к гриду

| Возможность | Статус |
|---|---|
| **Видимость** | ✅ MudSwitch → `_hiddenSqlNames` → `KescoColumn.Hidden`. Сгруппированные — переключатель заблокирован |
| **Порядок (диалог→грид)** | ✅ Двухфазный рендеринг: сбор CellTemplate → динамические `TemplateColumn` по `_columnOrder`. Apply → `_dataKey++` → перерендер |
| **Порядок (грид→диалог)** | ✅ `_columnOrder` всегда синхронизирован с DOM (обновляется через `OnColumnDrop`), дополнительное чтение DOM не требуется |
| **Отмена диалога** | ✅ Порядок, видимость и сортировка восстанавливаются из `_columnOrderSnapshot` / `_originalItems` |
| **Сортировка** | ✅ Клик по названию/бейджу → цикл ASC/DESC/нет. До 2 колонок. Бейдж `1↑`/`2↓`. При «Применить» синхронизируется в `_sortState` грида (через `SortName`), вызывается `NotifyQueryChanged()` для перезагрузки данных. Сброс — кнопка `ClearAll` |
| **Header drag** | ✅ Кастомный JS (`kescoGridColumnDrag.js`) с **insert**-семантикой (вставка перед/после целевой колонки), заменяет MudBlazor `DragDropColumnReordering` |

### Примечания

- `KescoGrid` требует параметр `Id` — DOM-id корневого элемента (используется `kescoGridColumnDrag.init`)
- На странице может быть несколько `KescoGrid` с разными `Id`
- `KescoColumn` регистрирует `CellTemplate` через `IKescoGrid.RegisterCellTemplate` (для динамического рендеринга)
- `@onclick` на динамических колонках использует `void HandleSortClick` (избегает async-лямбда-проблем Razor)

## Кастомный drag-and-drop колонок (kescoGridColumnDrag.js)

Перемещение колонок в заголовке грида реализовано через кастомный JS (`kescoGridColumnDrag.js`) с **insert**-семантикой — перетаскиваемая колонка вставляется перед/после целевой, в отличие от MudBlazor `DragDropColumnReordering`, который делал swap.

### Принцип работы

1. **`kescoGridColumnDrag.init(gridId, dotnetRef)`** — вызывается из `OnAfterRenderAsync` KescoGrid при каждом перерендере динамических колонок. Безопасен для многократного вызова (dispose предыдущего).
2. **`dragstart`** (capture:true) — определяет `srcSqlName` по `data-col-sql`, устанавливает `effectAllowed='move'`, вызывает C# `SetDraggedColumn(sql)` → устанавливает `KescoDragState.DraggedColumn` для tray-drop.
3. **`dragover`** (capture:true) — показывает индикатор вставки (`.kesco-grid-drop-indicator`) на целевой колонке: слева от центра = вставить перед, справа = после.
4. **`drop`** — вызывает C# `OnColumnDrop(srcSql, targetSql, insertBefore)` → обновляет `_columnOrder` через insert (удаление источника + вставка на целевую позицию) → `_dataKey++` → перерендер.
5. **`dragend`** — очистка, вызов `SetDraggedColumn(null)`.
6. **`kescoGridColumnDrag.dispose(gridId)`** — в `DisposeAsync` KescoGrid для очистки обработчиков.

### Требования

- Заголовки колонок должны содержать `data-col-sql` с SQL-именем
- `DragAndDropEnabled="false"` на всех `TemplateColumn` — MudBlazor не участвует в drag-and-drop колонок
- `KescoGrid` должен иметь уникальный `Id` (используется для поиска корневого элемента)
- `App.razor` должен подключать JS: `<script src="_content/Kesco.Lib.Web.BZ.Controls/js/kescoGridColumnDrag.js"></script>`
- CSS-индикатор (`.kesco-grid-drop-indicator`) определён в `app.css`

### KescoGrid → JS-методы

| JSInvokable | Направление | Описание |
|---|---|---|
| `SetDraggedColumn(string?)` | JS → C# | Устанавливает/сбрасывает `KescoDragState.DraggedColumn` |
| `OnColumnDrop(src, target, insertBefore)` | JS → C# | Применяет insert-перемещение в `_columnOrder` |
