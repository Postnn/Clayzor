# Handoff: кнопка Add и перетаскивание колонок ClayGrid

## Этап

Изменения и профильные проверки завершены. Commit/push/merge не выполнялись.

## Изменения

- `Controls/Components/Grid/ClayGrid.razor.cs`: добавлены согласованные условия
  `HasServiceColumn` и `ShowAddInServiceHeader`.
- `Controls/Components/Grid/ClayGrid.razor`: Add выводится в сервисной шапке, если
  колонка есть; иначе остаётся в toolbar. Header вызывает общий `HandleRowAddClick`.
- `ClayGrid.razor` и `ClayColumn.razor`: явное `draggable="true"/"false"` и класс
  `clay-grid-column-drag-handle`. Это устраняет прежнее `draggable=""`, при котором
  div не получал нативный drag и вместо него выделялся текст ряда.
- `wwwroot/css/clay.css`: handle запрещает user-select, имеет grab/grabbing.
- `wwwroot/js/clayGridColumnDrag.js`: drag image ограничен самим handle.
- Tests `ClayGridReinitTests.cs`: dynamic Add в сервисной шапке, отсутствие toolbar Add,
  явный draggable и отдельный handle. `ClayGridSectionTests.cs`: fallback Add в toolbar
  при отсутствии сервисной колонки.

## Проверки

- Targeted `ClayGridReinitTests|ClayGridSectionTests`: 24/24.
- `dotnet build Clayzor.sln --no-restore`: 0 ошибок, прежние warnings.
- Реальный `/?id=1001`: toolbarAdd=0, headerAdd=1, данные 6 записей; заголовки имеют
  draggable=true и computed user-select=none.
- Браузерная DragEvent-цепочка SN → grouping tray добавляет chip SN, selection пустой,
  SQL alert отсутствует. PN → filter tray открывает `Фильтр: PN` (ожидаемый dialog,
  а не chip до ввода условия). Страница перезагружена после проверки, dialog=0.
- Нативный CDP mouse-drag в этом окружении не создал dragstart, поэтому нативное
  движение мыши автоматически не подтверждено; подтверждены реальный DOM, атрибуты,
  CSS и полная JS→Blazor drag-цепочка. Полный test suite не повторялся из-за ранее
  зафиксированного несвязанного зависания ClayTreeMutationAbortTests.

## Окружение

Inventory/Users/Styles запущены после сборки. Chrome DevTools: 9222.
Временный helper `C:/Users/devadmin/AppData/Local/Temp/clay-cdp.ps1` расширен
`Mouse.drag` с MouseX/MouseY/TargetX/TargetY.

## Следующее действие

Ожидается ручная проверка владельцем либо команда на commit/push. Новые правки по этому
этапу сначала сверить с данным handoff и текущими незакоммиченными изменениями меню.
