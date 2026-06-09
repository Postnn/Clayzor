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
| `Groupable` | `bool` | `false` | Включить группировку в MudDataGrid |
| `GroupExpanded` | `bool` | `true` | Группы развёрнуты по умолчанию |
| `GroupColumn` | `string?` | `null` | SQL-имя колонки группировки (только для `ShowGroupToggle`). При tray-режиме используйте `GroupColumns` |
| `GroupColumns` | `IReadOnlyList<string>` | `[]` | Текущий список SQL-имён колонок группировки. Только для чтения на странице, управляется tray |
| `ShowGroupToggle` | `bool` | `false` | Показать переключатель одиночной группировки (старый режим) |
| `ShowGroupingTray` | `bool` | `false` | Включить drag‑and‑drop tray для множественной группировки (новый режим) |
| `AvailableGroupColumns` | `Dictionary<string, string>` | `[]` | Доступные для группировки колонки: ключ — отображаемое имя, значение — SQL-имя колонки. Только для tray-режима |
| `GroupToggleLabel` | `string` | `"Группировать"` | Подпись переключателя |
| `ChildContent` | `RenderFragment?` | — | Колонки MudDataGrid |
| `OnAdd` | `EventCallback` | — | Обработчик кнопки «Добавить» |
| `OnRowClick` | `EventCallback<DataGridRowClickEventArgs<TEntity>>` | — | Клик по строке |
| `OnQueryChanged` | `EventCallback<KescoDataQuery>` | — | Изменение состояния запроса |

## Публичные методы

| Метод | Описание |
|---|---|
| `ToggleSort(string sqlCol)` | Циклически переключает сортировку: ASC → DESC → убрать. Вызывается из `<HeaderTemplate>` |
| `GetSortBadge(string sqlCol)` | Возвращает `RenderFragment` с бейджем сортировки (номер + стрелка). Вызывается из `<HeaderTemplate>` |
| `GetGroupByOrder(string sqlCol)` | Возвращает приоритет группировки для колонки (0 = внешний уровень). Используется в биндинге `GroupByOrder` на `PropertyColumn`. Если колонка не участвует в группировке — возвращает 0 |

## KescoDataQuery

Класс состояния запроса, передаваемый в `OnQueryChanged`:

- `SearchText` — текст поиска
- `GroupEnabled` — включена ли группировка
- `GroupColumns` — список SQL-имён колонок группировки в порядке приоритета (новый, заменяет `GroupColumn`)
- `GroupColumn` — SQL-имя первой колонки группировки (удобство для обратной совместимости, установка записывает в `GroupColumns[0]`)
- `SortColumns` — список `SortColumn(Column, Desc)`
- `PageNumber` — номер текущей страницы (1-based)
- `PageSize` — размер страницы
- `TotalCount` — общее число записей (заполняется страницей после `GetCountAsync`)
- `BuildOrderBy(defaultOrder)` — строит `ORDER BY`; при включённой группировке все `GroupColumns` идут первыми
- `BuildWhereClause(searchColumns)` — строит `WHERE ... LIKE @search`

## Пример использования (tray-режим)

```razor
@page "/items"
@using Kesco.Lib.Web.BZ.Controls

<KescoDataList TEntity="MyEntity"
               @ref="_dataGrid"
               Title="Заголовок"
               Items="_items"
               Loading="_loading"
               PageSize="@AppSettings.DefaultPageSize"
               ShowGroupingTray="true"
               AvailableGroupColumns="@_groupColumnsDef"
               Groupable="@(_dataGrid?.GroupColumns.Count > 0)"
               TotalCount="@_query.TotalCount"
               ShowPagination="true"
               OnAdd="OpenAddDialog"
               OnRowClick="OnRowClicked"
               OnQueryChanged="OnQueryChanged">
    <PropertyColumn T="MyEntity" TProperty="int" Property="x => x.Id" Sortable="false"
                    Groupable="true"
                    Grouping="@(_dataGrid?.GroupColumns.Contains("SqlCol") ?? false)"
                    Hidden="@(_dataGrid?.GroupColumns.Contains("SqlCol") ?? false)"
                    GroupByOrder="@(_dataGrid?.GetGroupByOrder("SqlCol") ?? 0)"
                    GroupBy="@(x => x.Id)">
        <HeaderTemplate>
            <div style="display:flex;align-items:center;width:100%;cursor:pointer"
                 draggable="true"
                 @ondragstart="@(e => { e.DataTransfer.EffectAllowed = "move"; KescoDragState.DraggedColumn = "SqlCol"; })"
                 @onclick="@(() => _dataGrid?.ToggleSort("SqlCol"))">
                <MudText Style="flex:1;text-align:center">Код</MudText>
                @if (_dataGrid is not null) { @_dataGrid.GetSortBadge("SqlCol") }
            </div>
        </HeaderTemplate>
        <GroupTemplate>
            <MudText Typo="Typo.body1" Style="font-weight:600">
                @context.Grouping!.Key (@context.Grouping!.Count() шт.)
            </MudText>
        </GroupTemplate>
    </PropertyColumn>
    <PropertyColumn T="MyEntity" TProperty="string" Property="x => x.GroupProp"
                    Groupable="true"
                    Grouping="@(_dataGrid?.GroupColumns.Contains("GroupSqlCol") ?? false)"
                    Hidden="@(_dataGrid?.GroupColumns.Contains("GroupSqlCol") ?? false)"
                    GroupByOrder="@(_dataGrid?.GetGroupByOrder("GroupSqlCol") ?? 0)"
                    GroupBy="@(x => x.GroupProp ?? "")">
        <HeaderTemplate>
            <div style="display:flex;align-items:center;width:100%;cursor:pointer"
                 draggable="true"
                 @ondragstart="@(e => { e.DataTransfer.EffectAllowed = "move"; KescoDragState.DraggedColumn = "GroupSqlCol"; })"
                 @onclick="@(() => _dataGrid?.ToggleSort("GroupSqlCol"))">
                <MudText Style="flex:1;text-align:center">Группа</MudText>
                @if (_dataGrid is not null) { @_dataGrid.GetSortBadge("GroupSqlCol") }
            </div>
        </HeaderTemplate>
        <GroupTemplate>
            <MudText Typo="Typo.body1" Style="font-weight:600">
                @context.Grouping!.Key (@context.Grouping!.Count() шт.)
            </MudText>
        </GroupTemplate>
    </PropertyColumn>
</KescoDataList>

@code {
    private KescoDataList<MyEntity> _dataGrid = null!;
    private KescoDataQuery _query = new();

    private Dictionary<string, string> _groupColumnsDef = new()
    {
        ["Группа"] = "GroupSqlCol",
        ["Код"] = "SqlCol"
    };

    private async Task OnQueryChanged(KescoDataQuery query)
    {
        _query.SearchText = query.SearchText;
        _query.GroupEnabled = query.GroupEnabled;
        _query.GroupColumns = query.GroupColumns;
        _query.SortColumns = query.SortColumns;
        _query.PageNumber = query.PageNumber;
        _query.PageSize = query.PageSize;
        await LoadData();
    }
}
```

## Пример использования (toggle-режим, старый)

```razor
@page "/items"
@using Kesco.Lib.Web.BZ.Controls

<KescoDataList TEntity="MyEntity"
               @ref="_dataGrid"
               Title="Заголовок"
               Items="_items"
               Loading="_loading"
               PageSize="@AppSettings.DefaultPageSize"
               TotalCount="@_query.TotalCount"
               ShowPagination="true"
               GroupColumn="GroupColumnSqlName"
               ShowGroupToggle="true"
               GroupToggleLabel="Группировать по X"
               Groupable="_dataGrid?.GroupEnabled ?? false"
               OnAdd="OpenAddDialog"
               OnRowClick="OnRowClicked"
               OnQueryChanged="OnQueryChanged">
    <PropertyColumn T="MyEntity" TProperty="int" Property="x => x.Id" Sortable="false">
        <HeaderTemplate>
            <div style="display:flex;align-items:center;width:100%;cursor:pointer"
                 @onclick="@(() => _dataGrid?.ToggleSort("SqlColumnName"))">
                <MudText Style="flex:1;text-align:center">Код</MudText>
                @if (_dataGrid is not null) { @_dataGrid.GetSortBadge("SqlColumnName") }
            </div>
        </HeaderTemplate>
    </PropertyColumn>
    <PropertyColumn T="MyEntity" TProperty="string" Property="x => x.GroupProp"
                    Hidden="@(_dataGrid?.GroupEnabled ?? false)"
                    Groupable="true"
                    Grouping="@(_dataGrid?.GroupEnabled ?? false)"
                    GroupBy="@(x => x.GroupProp ?? "")">
        <HeaderTemplate>
            <div style="display:flex;align-items:center;width:100%;cursor:pointer"
                 @onclick="@(() => _dataGrid?.ToggleSort("GroupColumnSqlName"))">
                <MudText Style="flex:1;text-align:center">Группа</MudText>
                @if (_dataGrid is not null) { @_dataGrid.GetSortBadge("GroupColumnSqlName") }
            </div>
        </HeaderTemplate>
        <GroupTemplate>
            <MudText Typo="Typo.body1" Style="font-weight:600">
                @context.Grouping!.Key (@context.Grouping!.Count() шт.)
            </MudText>
        </GroupTemplate>
    </PropertyColumn>
</KescoDataList>

@code {
    private KescoDataList<MyEntity> _dataGrid = null!;
    private KescoDataQuery _query = new();

    private List<MyEntity> _items = [];
    private bool _loading = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadData();
            StateHasChanged();
        }
    }

    private async Task OnQueryChanged(KescoDataQuery query)
    {
        _query.SearchText = query.SearchText;
        _query.GroupEnabled = query.GroupEnabled;
        _query.GroupColumn = query.GroupColumn;
        _query.SortColumns = query.SortColumns;
        _query.PageNumber = query.PageNumber;
        _query.PageSize = query.PageSize;
        await LoadData();
    }

    private async Task LoadData()
    {
        _loading = true;
        try
        {
            var orderBy = _query.BuildOrderBy("DefaultCol1, DefaultCol2");
            var where = _query.BuildWhereClause("a.SearchCol1", "a.SearchCol2");
            var param = new { search = $"%{_query.SearchText}%" };

            _query.TotalCount = await MyEntity.GetCountAsync(Db, where, param);
            _items = (await MyEntity.GetPagedAsync(Db, where, orderBy, param,
                _query.PageNumber, _query.PageSize)).ToList();
        }
        finally
        {
            _loading = false;
        }
    }
}
```

## Состояния

- **Поиск** — сбрасывает страницу на 1, вызывает `OnQueryChanged` с debounce 300 мс
- **Сортировка** — до 2 колонок, циклически: ASC → DESC → убрать. Группировочная колонка не сортируется. Сбрасывает страницу на 1
- **Группировка (toggle)** — колонка группировки скрывается (`Hidden`), сортировка по ней блокируется. Сбрасывает страницу на 1
- **Группировка (tray)** — перетаскивание заголовков колонок на панель над гридом. Добавление колонок
  также через кнопку «+ Колонка». Удаление — клик по × на чипе. Изменение порядка — перетаскивание чипов
  внутри панели. При перестроении сбрасывается страница на 1. Каждый заголовок колонки должен иметь
  `draggable="true"` и `@ondragstart`, устанавливающий `KescoDragState.DraggedColumn`.
  Порядок уровней группировки управляется биндингом `GroupByOrder="@(_dataGrid?.GetGroupByOrder("SqlCol") ?? 0)"` —
  MudBlazor вычисляет его при рендере, не требует пересоздания грида и не использует reflection
- **Смена размера страницы** — числовое поле (1–999). Сбрасывает страницу на 1
- **Кнопка «Обновить»** — сбрасывает страницу на 1, перезагружает данные
- **Переход по страницам** — кнопки `|<`, `<`, `>`, `>|`. Не сбрасывают фильтры
- **Защита выхода за границы** — при уменьшении `TotalCount` номер страницы автоматически обрезается до максимального

## Многоуровневая группировка — механика

MudBlazor 9.x определяет порядок уровней группировки исключительно по параметру `GroupByOrder` на каждой `Column<T>`.
В `GroupItems()` колонки с `Grouping=true` сортируются по `GroupByOrder` по возрастанию: **меньше = внешний уровень**.

### Правила для каждой группируемой колонки

```razor
<PropertyColumn ...
    Groupable="true"
    Grouping="@(_dataGrid?.GroupColumns.Contains("SqlCol") ?? false)"
    Hidden="@(_dataGrid?.GroupColumns.Contains("SqlCol") ?? false)"
    GroupByOrder="@(_dataGrid?.GetGroupByOrder("SqlCol") ?? 0)"
    GroupBy="@(x => x.Prop)">
```

| Атрибут | Зачем |
|---|---|
| `Grouping` | Включает/выключает группировку по этой колонке |
| `Hidden` | Скрывает колонку в гриде, пока она участвует в группировке |
| `GroupByOrder` | Задаёт приоритет уровня (0 = внешний). Биндинг вычисляется при каждом рендере |
| `GroupBy` | Функция-ключ группировки |

### Почему нельзя использовать `@key` или reflection

- `@key="_gridKey"` с инкрементом пересоздаёт `MudDataGrid`. При инициализации нового экземпляра
  MudBlazor вызывает `GroupItems()` **до** любых `OnAfterRender*`-хуков — все колонки в этот момент
  имеют `GroupByOrder=0`, порядок определяется DOM-позицией (порядком объявления в Razor).
- Reflection + `SetValueAsync` в `OnAfterRenderAsync` всегда опаздывает по той же причине.
- Декларативный биндинг `GroupByOrder="@(...)"` вычисляется Blazor **при рендере**, до вызова
  `GroupItems()`, и поэтому всегда корректен.
