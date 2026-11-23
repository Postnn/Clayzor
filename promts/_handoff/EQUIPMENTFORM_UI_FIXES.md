# EquipmentForm и ClayGrid — UI-исправления после EQ7

Дата: 22.11.2026. База EQ7 опубликована отдельно: суперпроект `7e40c6c`, Inventory `d47e2ff`, Entities `df7a8d0`, Tests `eb1e123`. Этот этап находится в рабочем дереве и не включён в коммиты EQ7.

## Реализовано

- Все вспомогательные диалоги EquipmentForm, ConfirmDialog и вложенный UPS-тест открываются через `ShowExAsync` с `DialogOptionsEx.DragMode=Simple`. Правило добавлено в корневой `AGENTS.md`.
- В изменении расположения неизменяемые дата/время получают одновременно `ReadOnly` и `Disabled`; срок возврата и примечания остаются редактируемыми.
- Диалог компьютера: RAM строго по `MemoryBanks`, без добавления/удаления; HD/SSD добавляются одной горизонтальной строкой с `nowrap` и внутренней прокруткой; числовые блоки 120 px; стандартные конфигурации и дата находятся в одной строке, названию отведена основная ширина.
- В `ClayFilterValueEditor` и disabled-поле составного фильтра «Значение» используется как `Placeholder`, а не внутренний `Label`. Высота остаётся глобальной `--clay-control-height:32px`.
- `clayGridColumnDrag.js`: горизонтальный автоскролл у краёв `.mud-table-container`, видимая подсказка направления, продолжение insert-индикатора, cleanup при drop/dragend/dispose.
- Курсоры: обычные строки/ячейки — default; ссылки и кнопки — pointer; группа — pointer; drag — grab только при `draggable=true`; сортировка без drag — pointer; неактивный заголовок detail-grid — default.
- В секциях EquipmentForm ФИО «Изменил» и текущий ответственный открывают карточку сотрудника через `clayOpen`. В SQL/read-модели License, Software, Accounting и SIM Service добавлен `ChangedById`.
- В `clayNavigationMenu.js` pointerdown очищает pending hover timer пункта и кратковременно подавляет hover после клика; итог Toggle не перекрывается pointerenter/leave.
- Правило UTC добавлено в корневой `AGENTS.md`: SQL datetime со временем всегда UTC, включая `Kind=Unspecified`; вывод локализуется на дату события, date-only не сдвигается.
- `ClayDateTimeConverter` переводит UTC при любом Kind, поддерживает DateTimeOffset и SQL time/TimeSpan. DynamicGrid повторяет чтение client offset до успешной инициализации JS. `ClayChanged` не показывает UTC как fallback.
- EquipmentForm обновляет локальные даты после refresh компьютера, SIM-услуг, лицензий, ПО и бухучёта; диалоги расположения/ответственного получают локальные дату/время.

## Проверки

- Профиль Equipment/UPS/Filter/Navigation — 341/341; расширенный профиль после ссылок — 343 теста; целевой UTC/HD/SSD набор — 29/29; DynamicGrid/converter — 38/38.
- Новые тесты: `ClayFilterValueEditorTests`, `EquipmentComputerDialogTests`, Location disabled/local time, ComputerRefresh, EmployeeLink/PassiveHeaders, SQL Unspecified/DateTimeOffset/TimeSpan и ClayLocalTime Unspecified.
- `dotnet build Clayzor.sln --no-restore` — успешно, 0 ошибок, 35 предупреждений.
- Общий набор без ранее документированного `ClayTreeMutationAbortTests` — **999/999**.

## Browser

- Location: дата и время имеют `disabled=true` и визуально серые; диалог перемещён мышью на `(250,130)` px.
- Computer: диалог 960 px, HD/SSD имеют `nowrap`, одинаковую Y-координату, блоки 120 px и внутренний `overflow-x:auto`; поля конфигураций 576 px + дата 180 px. На Equipment 8 реальное `MemoryBanks=0`, поэтому RAM-поля не отображаются; тест с `MemoryBanks=2` подтверждает два поля.
- UTC+3 (`Europe/Moscow`): Equipment 8 `18.11 23:20 UTC → 19.11 02:20`; DynamicGrid 1011 тип 10 `07:43 UTC → 10:43`. Видимая «Выдано» грида 1001 — обычный тип 3 и не использовалась как доказательство типа 10.
- Filter: все три поля 32 px; у значения `placeholder=Значение`, внутреннего label нет.
- Detail-grid Equipment: header/cell `cursor=default`; «Изменил» — ссылка `http://localhost:5030/user?id=4`, pointer. Dynamic grid: cell default, активный header grab.
- Synthetic HTML5 drag у правого края grid 1001: `scrollLeft` увеличился, `data-auto-scroll=right`, подсказка «Прокрутка →», drop indicator виден; dragend очищает состояние.
- Меню: hover открыл, клик закрыл без повторного открытия через 700 ms; следующий клик один раз открыл одну панель.

Данные в БД этим этапом не изменялись. Временные скриншоты находятся вне репозитория.
