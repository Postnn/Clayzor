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
| `GroupColumn` | `string?` | `null` | SQL-имя колонки группировки |
| `ShowGroupToggle` | `bool` | `false` | Показать переключатель группировки |
| `GroupToggleLabel` | `string` | `"Группировать"` | Подпись переключателя |
| `ChildContent` | `RenderFragment?` | — | Колонки MudDataGrid |
| `OnAdd` | `EventCallback` | — | Обработчик кнопки «Добавить» |
| `OnRowClick` | `EventCallback<DataGridRowClickEventArgs<TEntity>>` | — | Клик по строке |
| `OnQueryChanged` | `EventCallback<KescoDataQuery>` | — | Изменение состояния запроса |

## KescoDataQuery

Класс состояния запроса, передаваемый в `OnQueryChanged`:

- `SearchText` — текст поиска
- `GroupEnabled` — включена ли группировка
- `GroupColumn` — SQL-колонка группировки
- `SortColumns` — список `SortColumn(Column, Desc)`
- `PageNumber` — номер текущей страницы (1-based)
- `PageSize` — размер страницы
- `TotalCount` — общее число записей (заполняется страницей после `GetCountAsync`)
- `BuildOrderBy(defaultOrder)` — строит `ORDER BY`
- `BuildWhereClause(searchColumns)` — строит `WHERE ... LIKE @search`

## Пример использования

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
- **Группировка** — колонка группировки скрывается (`Hidden`), сортировка по ней блокируется. Сбрасывает страницу на 1
- **Смена размера страницы** — числовое поле (1–999). Сбрасывает страницу на 1
- **Кнопка «Обновить»** — сбрасывает страницу на 1, перезагружает данные
- **Переход по страницам** — кнопки `|<`, `<`, `>`, `>|`. Не сбрасывают фильтры
- **Защита выхода за границы** — при уменьшении `TotalCount` номер страницы автоматически обрезается до максимального
