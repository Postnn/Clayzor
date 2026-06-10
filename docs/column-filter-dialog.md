# ColumnFilterDialog

Диалог настройки фильтра по колонке с типо-зависимыми операторами и полями ввода.
Открывается при перетаскивании заголовка колонки на панель фильтрации (filter tray)
или при клике на существующий чип фильтра.

## Параметры

| Параметр | Тип | По умолчанию | Описание |
|---|---|---|---|
| `ColumnDisplayName` | `string` | `""` | Отображаемое имя колонки (в заголовке диалога и чипе) |
| `ColumnSqlName` | `string` | `""` | SQL-имя колонки — включается в возвращаемый `ColumnFilter` |
| `ColumnType` | `ColumnType` | `Text` | Тип данных колонки — определяет доступные операторы и поле ввода |
| `ExistingFilter` | `ColumnFilter?` | `null` | Существующий фильтр для режима редактирования. `null` — новый фильтр |

## ColumnType и доступные операторы

| `ColumnType` | Поле ввода | Операторы |
|---|---|---|
| `Text` | `MudTextField T="string"` | Contains, Equals, StartsWith, EndsWith, NotEquals |
| `Number` | `MudNumericField T="int?"` | Equals, NotEquals, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual |
| `Boolean` | `MudSelect T="bool?"` (Да/Нет) | Equals |

## ColumnFilterOperator

| Оператор | SQL | Метка в диалоге |
|---|---|---|
| `Contains` | `col LIKE @p` (`%value%`) | «содержит» |
| `Equals` | `col = @p` | «равно» |
| `StartsWith` | `col LIKE @p` (`value%`) | «начинается с» |
| `EndsWith` | `col LIKE @p` (`%value`) | «заканчивается на» |
| `GreaterThan` | `col > @p` | «больше (>)» |
| `GreaterThanOrEqual` | `col >= @p` | «больше или равно (≥)» |
| `LessThan` | `col < @p` | «меньше (<)» |
| `LessThanOrEqual` | `col <= @p` | «меньше или равно (≤)» |
| `NotEquals` | `col <> @p` | «не равно» |

## Статический метод

### `GetFilterDescription(ColumnFilter filter, string displayName)`

Возвращает читаемое описание фильтра для отображения в чипе панели фильтров.

Примеры:
- `«Название содержит «грипп»»`
- `«Код = 42»`
- `«Порядок > 10»`
- `«Группа = Да»`

## Возвращаемое значение

При подтверждении диалог возвращает `DialogResult.Ok(ColumnFilter)` со свойствами:

| Свойство | Тип | Описание |
|---|---|---|
| `Column` | `string` | SQL-имя колонки |
| `ParamName` | `string` | Имя Dapper-параметра (без @, транслитерированное, уникальное) |
| `Operator` | `ColumnFilterOperator` | Оператор сравнения |
| `Value` | `object?` | Значение фильтра |

При отмене — `DialogResult.Cancel()`.

## Транслитерация ParamName

Имя параметра формируется из SQL-имени колонки путём транслитерации кириллицы в латиницу
и замены недопустимых символов. Пример: `cf_KodMeditsinskogoAnaliza` из `КодМедицинскогоАнализа`.
