# F1. Исправления диалога настраиваемого фильтра

Файлы: `Components/Grid/Filter/KescoFilterDialog.razor(.cs)`,
`Components/Grid/Filter/KescoFilterGroup.razor(.cs)`.
Закрывает баги 1, 2, 3, 5. Модель дерева (`KescoFilterGroupNode`) не меняется.

## Баг 1 — на верхнем уровне только одна группа
Причина: диалог имеет две ветки рендера — при пустом черновике показывает
**только** «добавить условие» (`AddFirstExpression`), при непустом — оборачивает
корень в группу `IsRoot=true`. Из-за этого нельзя начать с группы, а корень
визуально выглядит как единственная обёртка.

Исправить:
1. Убрать в `KescoFilterDialog.razor` ветку пустого состояния и метод
   `AddFirstExpression`. **Всегда** рендерить корневую группу:
   ```razor
   <KescoFilterGroup Node="@_draft" Columns="@Columns" LookupOptions="@LookupOptions"
                     IsRoot="true" OnChanged="@OnDraftChanged" />
   ```
   `KescoFilterGroup` и так всегда показывает кнопки «добавить условие/группу»
   (в т.ч. при пустых `Nodes`), поэтому добавить группу первой станет возможно,
   а «добавить группу» на корне добавляет сиблинг-группу в `_draft.Nodes`.
2. В `KescoFilterGroup.razor` для `IsRoot` рендерить корень **плоско**: без левой
   рамки/отступа (`border-left`) и без визуального «вложения», чтобы дочерние
   группы читались как сиблинги верхнего уровня. Переключатель И/ИЛИ и кнопки
   добавления у корня оставить.

Критерий: в диалоге можно добавить на верхнем уровне несколько групп-сиблингов
и отдельные условия; можно начать сразу с группы.

## Баг 2 — нет текстового представления фильтра в диалоге
Добавить живое описание черновика, обновляемое при каждом изменении.

В `KescoFilterDialog.razor.cs`:
```csharp
private string _draftDescription = "";
private Func<string,string> DisplayNameOf => sql =>
    Columns.FirstOrDefault(c => c.SqlName == sql)?.DisplayName ?? sql;

private void RecalcDescription() =>
    _draftDescription = KescoFilterDescriptionBuilder.BuildText(_draft, DisplayNameOf) ?? "";
```
Вызывать `RecalcDescription()` в `OnParametersSet` (после создания `_draft`) и в
`OnDraftChanged`. В разметке над деревом показать блок только если описание непустое:
```razor
@if (!string.IsNullOrEmpty(_draftDescription))
{
    <MudPaper Elevation="0" Class="pa-2 mb-2" Style="background:var(--mud-palette-background-grey)">
        <MudText Typo="Typo.body2">@_draftDescription</MudText>
    </MudPaper>
}
```

Критерий: при изменении условий текст фильтра в диалоге обновляется.

## Баг 3 — SqlName вместо DisplayName в блоке «редактируется в диалоге колонки»
Причина: `KescoFilterGroup.GetLeafDescription` использует `leaf.Column` (SqlName).
Исправить — резолвить отображаемое имя через уже имеющийся параметр `Columns`:
```csharp
private string GetLeafDescription(ColumnFilter leaf)
{
    var dn = Columns.FirstOrDefault(c => c.SqlName == leaf.Column)?.DisplayName
             ?? leaf.Column;
    var colPart = string.IsNullOrEmpty(leaf.Column) ? "" : $"{dn}: ";
    var opLabel = KescoFilterOperatorLabels.Get(leaf.Operator);
    return leaf.Value is not null ? $"{colPart}{opLabel} «{leaf.Value}»" : $"{colPart}{opLabel}";
}
```
(Заодно проверить, что нигде в диалоге/группе больше не показывается `leaf.Column` напрямую.)

## Баг 5 — нет скролла + утечка глобального стиля
Причина: `<style>.mud-dialog-content{ overflow: visible; }</style>` в
`KescoFilterDialog.razor` — глобальное правило, применяется ко **всем** `MudDialog`
и запрещает прокрутку.

Исправить:
1. **Удалить** этот `<style>` полностью.
2. Обернуть дерево (и блок описания из бага 2) в контейнер с ограниченной высотой
   и вертикальным скроллом, например:
   ```razor
   <div style="min-width:480px;max-width:700px;max-height:60vh;overflow-y:auto">
       ... описание + <KescoFilterGroup ... /> ...
   </div>
   ```
   Никаких глобальных `.mud-dialog-content` переопределений.

Критерий: при большом числе условий диалог прокручивается; прочие диалоги
приложения не затронуты.

## Замечания
- После правок в `KescoFilterDialog` останется один путь рендера (без ветки
  пустого состояния) — проверить, что `_draft` инициализируется до первого рендера.
- `dotnet build` без ошибок; проверить открытие/применение/сброс фильтра.
