# 08. Общий редактор значения фильтра (KescoFilterValueEditor)

Единый редактор значения по типу колонки — чтобы не дублировать логику между
`KescoColumnFilterDialog` и новым диалогом настраиваемого фильтра (задача 09).
Сырой Mud допустим (как в существующих диалогах библиотеки).

## Новый файл `Components/Grid/Filter/KescoFilterValueEditor.razor` (+ `.razor.cs`)
- Параметры: `ColumnTypeDescriptor Type` (задача 03), `@bind-Value (object?)`,
  необязательный список вариантов `IReadOnlyList<KescoFilterOption>? Options`
  (для value-picker по Text/Number), текущий `ColumnFilterOperator Operator`.
- Рендер — **один** `switch` по `Type.Kind`:
  - Text → `MudTextField`;
  - Number → `MudNumericField<int?>`;
  - Decimal → `MudNumericField<decimal?>`;
  - Date → `MudDatePicker`;
  - Boolean → `MudSelect<bool?>`;
  - если задан `Options` → `MudSelect`/`KescoComboBox`-стиль (SQL/тип не меняются).
- Если `!Type.OperatorTakesValue(Operator)` (IsEmpty/IsNotEmpty/IsNull/IsNotNull) —
  редактор **скрывается**, `Value = null`.
- Биндинг `object? ↔ T` — адаптер на `Type.Parse`/`Type.Format` (инвариантная культура);
  на корневом контроле `@key` из `SqlName`+`Kind` — пересоздание при смене типа.

## Тип вариантов (рядом)
```csharp
public sealed class KescoFilterOption { public object? Value { get; init; } public string Label { get; init; } = ""; }
```

## (Желательно) refactor `KescoColumnFilterDialog`
Перевести его редакторы значения на `KescoFilterValueEditor` — единое поведение для
обоих диалогов. Не блокирующее требование; если рискованно — сделать в отдельный заход.

## Критерии
- [ ] Редактор корректен для Text/Number/Decimal/Date/Boolean и для value-picker.
- [ ] Операторы без значения скрывают редактор и обнуляют `Value`.
- [ ] `object? Value` биндится без ошибок (адаптер + `@key`, инвариантная культура).
- [ ] `dotnet build` без ошибок.
