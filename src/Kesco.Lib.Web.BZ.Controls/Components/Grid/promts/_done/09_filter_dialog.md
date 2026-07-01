# 09. Рекурсивный UI настраиваемого фильтра + диалог

Конструктор дерева условий в стиле Telerik Filter. Открытие/возврат — через
`IDialogService` + `IMudDialogInstance` (как `KescoColumnFilterDialog`), не `@bind-Visible`.
Использует редактор из задачи 08 и типы из задачи 02.

## Источник полей
Список фильтруемых колонок передаётся гридом (задача 10):
`(string SqlName, string DisplayName, ColumnTypeDescriptor Type)` + необязательные
варианты (`Options` на колонку). Операторы — `Type.Operators`. Подписи операторов —
переиспользовать `KescoColumnFilterDialog.GetOperatorLabel` (вынести в public/хелпер).

## `KescoFilterExpression.razor` (+ `.razor.cs`)
Одно условие (одноклаузный `ColumnFilter`, `Source=CompositeDialog`):
- `MudSelect<string>` — колонка (значение=SqlName, текст=DisplayName); при смене —
  сброс невалидных оператора/значения;
- `MudSelect<ColumnFilterOperator>` — операторы по `Type`;
- `KescoFilterValueEditor` (задача 08);
- кнопка удаления; изменения → `EventCallback OnChanged`.

## `KescoFilterGroup.razor` (+ `.razor.cs`) — рекурсивный
Привязан к `KescoFilterGroupNode`:
- переключатель И/ИЛИ (активен при 2+ узлах);
- перебор `Nodes`: `ColumnFilter` → `KescoFilterExpression`; `KescoFilterGroupNode` → `KescoFilterGroup` (рекурсия);
- кнопки «добавить условие» (новый одноклаузный `ColumnFilter`, `Source=CompositeDialog`),
  «добавить группу», «удалить» (у корня удаления нет);
- изменения всплывают через `OnChanged`.

## `KescoFilterDialog.razor` (+ `.razor.cs`)
- `MudDialog` + `[CascadingParameter] IMudDialogInstance MudDialog`.
- Параметры: корневая `KescoFilterGroupNode`, список колонок, необязательные `Options`.
- Работает с **глубокой копией** входа (`(KescoFilterGroupNode)root.Clone()`).
- Подвал (`MudButton`): «Применить» → `MudDialog.Close(DialogResult.Ok(draftClone))`;
  «Сбросить» → `Close(DialogResult.Ok(new KescoFilterGroupNode()))`; «Отмена» → `Cancel()`.
- Пустой корень → приглашение добавить условие.
- Узлы-листы с `Source=ColumnDialog` показывать единым блоком «редактируется в диалоге
  колонки»; их внутренние клаузы в дереве не редактируются (маршрут — задача 11).

## Критерии
- [ ] Открытие/возврат через `IDialogService`.
- [ ] Поля только из переданного списка; операторы/редактор по `Type`.
- [ ] Рекурсивная вложенность; add/remove условий и групп; И/ИЛИ на каждой группе.
- [ ] Правки по копии; «Применить» отдаёт копию; «Сбросить» — пустую группу.
- [ ] `dotnet build` без ошибок.
