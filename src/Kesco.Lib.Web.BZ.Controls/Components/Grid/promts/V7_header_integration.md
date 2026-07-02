# V7. Интеграция в заголовок: значок слева, подсветка, маршрутизация

Собрать фичу воедино: значок фильтра по значению в **левом углу** заголовка
колонки (треб. 11), подсветка при активном фильтре (треб. 13), открытие диалога
V6 с ленивой загрузкой V4, применение результата в дерево `_filterRoot` со
взаимоисключением фильтра по условию (треб. 8, 9) и маршрутизация «открыть форму
условия» (треб. 7). Инверсию уже посчитал V6, SQL строит V2.

## Файлы
- `Components/Grid/KescoColumn.razor` — значок слева в `HeaderTemplate`.
- `Components/Grid/IKescoGrid.cs` — методы доступа (иконка/состояние/открытие).
- `Components/Grid/KescoGrid.Filtering.cs` — открытие диалога V6, применение
  результата, маршрутизация в `KescoColumnFilterDialog` (с `InitialOperator`).
- `Components/Grid/KescoGrid.razor.cs` — при необходимости реализация методов
  `IKescoGrid` и проброс `LoadDistinctValuesAsync` в замыкание для V6.

## Значок в заголовке (`KescoColumn.razor`)
В `HeaderTemplate` сейчас: слева гибкий блок с текстом+drag, справа — `KescoMenu`
(⋮). Добавить **слева** (перед гибким блоком) значок фильтра по значению:
- Видимость: только когда
  `Grid.IsValueFilterAvailable(_meta.SqlName)` == true
  (грид проверяет `EnableValueFilter && meta.Filterable && meta.AllowValueFilter`).
- Иконка: `Icons.Material.Filled.FilterList` (или существующая иконка «воронка»
  из темы). Цвет по состоянию (треб. 13):
  `Grid.IsValueFilterActive(_meta.SqlName)` → `Color.Primary`/выделенный,
  иначе `Color.Default`/приглушённый.
- Клик: `await Grid.OpenValueFilterDialog(_meta.SqlName)`.
- Разместить как `MudIconButton` малого размера (padding:0; ~22px, как активатор
  `KescoMenu`), чтобы не ломать высоту строки заголовка.

## `IKescoGrid` — добавить
- `bool IsValueFilterAvailable(string sqlName);`
- `bool IsValueFilterActive(string sqlName);`  // есть `ValueFilter`-лист по колонке
- `Task OpenValueFilterDialog(string sqlName);`
Реализация в `KescoGrid.razor.cs`/`KescoGrid.Filtering.cs`.

## Открытие диалога (`KescoGrid.Filtering.cs`)
Метод `OpenValueFilterDialog(sqlName)`:
1. Найти `meta = _columnBySqlName[sqlName]`; тип —
   `FilterColumnTypes.TryGetValue(sqlName, out var t) ? t : ColumnType.Text`.
2. Найти в `_filterRoot.Nodes`:
   - `existingValue` = первый `ValueFilter` с `Column==sqlName`;
   - `existingCond`  = первый `ColumnFilter` с `Column==sqlName` и
     `Source==ColumnDialog` (как в `OpenFilterDialog`).
3. Замыкание загрузки для V6:
   `Func<Task<DistinctValuesResult>> load = () => DataLoader.LoadDistinctValuesAsync(sqlName, BuildCurrentQuery(), 100);`
   Использовать текущее состояние запроса (тот же снимок, что уходит в
   `NotifyQueryChanged`; см. как формируется `KescoDataQuery` в гриде — переиспользовать
   имеющийся сборщик снимка запроса, не изобретать новый).
4. `DialogParameters<KescoColumnValueFilterDialog>` с
   `ColumnSqlName`, `ColumnDisplayName`, `ColumnType`, `BoolTrueLabel`,
   `BoolFalseLabel` (из `meta`), `ExistingValueFilter=existingValue`,
   `ExistingConditionFilter=existingCond`, `LoadValues=load`.
   Опции — как у `OpenFilterDialog` (`DialogOptionsEx`, `DragMode=Simple`,
   `MaxWidth.ExtraSmall`).
5. По результату:
   - `ValueFilter vf` → **применить** (см. ниже);
   - `Cleared` → удалить `existingValue` из дерева (если был), перезагрузить;
   - `OpenConditionRequest(op)` → закрыть и вызвать существующий
     `OpenFilterDialog(sqlName, meta.DisplayName)`, но с пресетом оператора:
     прокинуть `InitialOperator=op` (V5) — расширить `OpenFilterDialog` необязательным
     параметром `ColumnFilterOperator? initialOperator = null` и передать его в
     `DialogParameters<KescoColumnFilterDialog>`;
   - `RemoveConditionRequest` → удалить `existingCond` (переиспользовать
     `RemoveFilter(sqlName)`), затем при желании снова открыть диалог значений.

## Применение `ValueFilter` в дерево (взаимоисключение, треб. 8, 9)
Отдельный метод `ApplyValueFilter(ValueFilter vf)`:
- В колонке одновременно **не должно** быть и условия, и значения. Перед вставкой
  `ValueFilter` удалить из `_filterRoot.Nodes` существующий `ColumnFilter`
  (`Source=ColumnDialog`, `Column==sqlName`) и старый `ValueFilter` этой колонки.
- Вставить `vf` в `_filterRoot.Nodes` (лист верхнего уровня, как делает
  `OpenFilterDialog` для `ColumnFilter`). Логика корня — как есть (`_filterRoot`
  остаётся источником истины; SQL соберёт `KescoCompositeSqlBuilder`).
- Симметрично: при применении фильтра **по условию** (`OpenFilterDialog`) — если
  для колонки уже есть `ValueFilter`, удалить его перед вставкой условия
  (добавить эту зачистку в существующий `OpenFilterDialog`).
- `_pageNumber = 1; await NotifyQueryChanged();` (как в текущих методах).

## Подсветка (треб. 13)
`IsValueFilterActive(sqlName)` = в `_filterRoot.Nodes` есть `ValueFilter` с
`Column==sqlName` и `HasValue`. Значок в заголовке подписывается на перерисовку
через уже существующее `TrayStateChanged`/`ColumnsChanged` (либо вызвать
`StateHasChanged` грида после `NotifyQueryChanged` — заголовки перерисуются).

## Критерии
- [ ] Значок в **левом** углу заголовка, виден только для колонок с включённым
      режимом (11), меняет цвет при активном фильтре по значению (13).
- [ ] Диалог V6 открывается с ленивым `LoadValues`, использующим текущий контекст
      запроса; значения не грузятся до открытия (2).
- [ ] Результат корректно применяется/снимается; страница сбрасывается на 1;
      данные перезагружаются.
- [ ] Взаимоисключение: применение значения удаляет условие колонки и наоборот
      (8, 9).
- [ ] Клик по условию из диалога значений открывает `KescoColumnFilterDialog` с
      пресетом оператора (7, через V5).
- [ ] `dotnet build` без ошибок.
