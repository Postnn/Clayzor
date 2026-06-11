# KescoColumnFilterDialog

Диалог настройки фильтра по колонке с типо-зависимыми операторами и полями ввода.
Открывается при перетаскивании заголовка колонки на панель фильтрации (filter tray)
или при клике на существующий чип фильтра.

Поддерживает до двух условий на одну колонку, объединяемых через логический оператор **И** / **ИЛИ**.

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
| `Text` | `MudTextField T="string"` | Contains, NotContains, Equals, NotEquals, StartsWith, NotStartsWith, EndsWith, NotEndsWith, IsEmpty, IsNotEmpty |
| `Number` | `MudNumericField T="int?"` | Equals, NotEquals, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual |
| `Boolean` | `MudSelect T="bool?"` (Да/Нет) | Equals |

- **IsEmpty** / **IsNotEmpty** — поле значения скрывается, кнопка «Применить» активна без ввода значения
- Остальные операторы требуют заполненного значения

## Два условия и логический оператор

Диалог поддерживает до двух условий на колонку:

1. **Первое условие** — всегда отображается
2. **Второе условие** — добавляется кнопкой «+ добавить условие», удаляется кнопкой `×`

Логический оператор (**И** / **ИЛИ**) — две кнопки переключения между условиями. SQL-генерация:

```sql
-- Одно условие:
col LIKE @p1

-- Два условия (И):
(col LIKE @p1) AND (col LIKE @p2)

-- Два условия (ИЛИ):
(col LIKE @p1) OR (col LIKE @p2)
```

Каждое условие оборачивается в скобки, чтобы избежать неоднозначности.

## ColumnFilterOperator

| Оператор | SQL | Метка в диалоге |
|---|---|---|
| `Contains` | `col LIKE @p` (`%value%`) | «содержит» |
| `NotContains` | `col NOT LIKE @p` (`%value%`) | «не содержит» |
| `Equals` | `col = @p` | «равно» |
| `NotEquals` | `col <> @p` | «не равно» |
| `StartsWith` | `col LIKE @p` (`value%`) | «начинается с» |
| `NotStartsWith` | `col NOT LIKE @p` (`value%`) | «не начинается с» |
| `EndsWith` | `col LIKE @p` (`%value`) | «заканчивается на» |
| `NotEndsWith` | `col NOT LIKE @p` (`%value`) | «не заканчивается на» |
| `GreaterThan` | `col > @p` | «больше (>)» |
| `GreaterThanOrEqual` | `col >= @p` | «больше или равно (≥)» |
| `LessThan` | `col < @p` | «меньше (<)» |
| `LessThanOrEqual` | `col <= @p` | «меньше или равно (≤)» |
| `IsEmpty` | `(col IS NULL OR col = '')` | «пустая строка» |
| `IsNotEmpty` | `(col IS NOT NULL AND col <> '')` | «не пустая строка» |

## ColumnFilter — модель данных

```csharp
class ColumnFilter
{
    // ── Первое условие ──
    string Column;                    // SQL-имя колонки
    string ParamName;                 // Имя Dapper-параметра (без @)
    object? Value;                    // Значение
    ColumnFilterOperator Operator;    // Оператор
    bool HasValue;

    // ── Второе условие (опционально) ──
    LogicalOperator LogicalOperator;  // And / Or
    string SecondParamName;
    object? SecondValue;
    ColumnFilterOperator SecondOperator;
    bool HasSecondClause;
}
```

## Статический метод

### `GetFilterDescription(ColumnFilter filter, string displayName)`

Возвращает читаемое описание фильтра для отображения в чипе панели фильтров.

Примеры:
- **Одно условие:** `Название: содержит «грипп»`
- **Два условия:** `Название: содержит «грипп» И не содержит «ковид»`
- **Числовое:** `Код: > 42`
- **Булево:** `Группа: = Да`
- **IsEmpty:** `Название: пустая строка`

## Возвращаемое значение

При подтверждении диалог возвращает `DialogResult.Ok(ColumnFilter)`.

При отмене — `DialogResult.Cancel()`.

## Транслитерация ParamName

Имя параметра формируется из SQL-имени колонки путём транслитерации кириллицы в латиницу и замены недопустимых символов:
- `cf_KodMeditsinskogoAnaliza` — для первого условия
- `cf2_KodMeditsinskogoAnaliza` — для второго условия
