# Аудит JavaScript Styles и дополнение к переносу форм Inventory

**Результат:** общие скрипты содержат существенную клиентскую логику, которая не видна из одной `.aspx`/code-behind формы. Подтверждены новые общие задачи для окон, фокуса и клавиатуры, Tree multiple, контактов и collaboration; найдены конкретные несовпадения current Grid/time/lifecycle. Значительная часть Grid, Select, календаря, меню и UTC helper уже реализована. Подготовлен отдельный [PROMPT_SHARED_CLIENT_BEHAVIOR.md](PROMPT_SHARED_CLIENT_BEHAVIOR.md); конкретные зависимости/приёмка добавлены во все 16 частных промтов, а последовательность/владение — в [00_ORCHESTRATOR.md](00_ORCHESTRATOR.md).

## 1. Метод, срез и предел доказательства

Дата: **23 ноября 2026 года**. Исследованы дерево репозитория Styles и все 32 JavaScript-файла как инвентарь; first-party функциональные скрипты прочитаны и сопоставлены с legacy renderer/callers и современными C#/JS компонентами. Для больших/minified vendor-файлов установлены назначение, загрузка и влияющие на реальные сценарии API/defaults; это **не построчный аудит безопасности всех сторонних библиотек**. У корневого V3 script.js построен индекс и разобраны relevant return/window/cookie/bootstrap/bridge/time границы, без заявления полного переноса всех неиспользуемых V3-контролов.

| Репозиторий | Закреплённый commit | Роль |
|---|---|---|
| KescoDev/Styles | `fddc5a2157cf906402c50731e84cfa537abd7dba` | 245 записей дерева; 32 JS, суммарно 1 595 315 байт |
| KescoDev/Kesco.App.Web.V4.Inventory | `12a6efae7eb310adec06a57f6d80b8d9e76985b4` | Реальные Page/EntityPage, renderer, handlers и 16 форм |
| Postnn/Clayzor | `05f2fc7561bf2a5648cca7fce991be6e4c12cdb2` | Main повторно сверён при дополнении; субмодули по этому срезу |
| Postnn/Clayzor.Lib.Web.Controls | `ea980be926ae975678314029ba5fc8aa68c675ba` | Современные Select/Form/Grid/Tree/window/menu/services/JS |
| Postnn/Kesco.App.Web.Inventory | `b3da274962c3c0811f71fbfe196f188ed3b5f659` | Текущие consumers/layout/startup/app.js |
| Postnn/Clayzor.Lib.Entities | `bf909e66d89a9a9c2f3fdc5069dd03b9b56368d7` | Metadata query, типы и Dapper mapping |
| Postnn/Clayzor.Lib.DALC | `4491634cb87272b24ac5b3e90e9fc43e1648963d` | Ошибки, strict reads, reconnect evidence |

DBSelect/Tests и полный исходный baseline — в [01_COMMON_RULES.md](01_COMMON_RULES.md). Код приложения/удалённые репозитории/БД не изменялись; JavaScript не исполнялся, браузер и интеграционный стенд не запускались. Все описанные проверки — требования следующей реализации, а не уже полученные runtime-результаты. Факт «не найден общий эквивалент» ограничен исследованным срезом; перед разработкой нужно сверить новые исправления.

Статусы ниже разделяют: **реализовано** (сохранить/проверить подключение), **частично** (есть основа, нужен конкретный контракт), **пробел/дефект по исходнику**, **намеренное отличие**, **внешний/неподтверждённый caller**. Это предотвращает и пропуск поведения, и повторную реализацию уже готовых компонентов.

## 2. Как скрипты действительно попадают на форму

Источник — [Page.RegisterScript](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Controls.V4/Common/Page.cs). Скрипты берутся из Config.styles_js, дополняются styles_cache и при необходимости domain; динамическая карта зависит от типа страницы/контролов. Windows/IIS ссылки могут отличаться регистром от Git tree (kesco.js/Kesco.js), что нельзя автоматически перенести в Linux `_content` URL.

| Ветка loader | Что загружается | Следствие для анализа |
|---|---|---|
| Базовая, до print return | jQuery 1.12.4, jQuery UI, dialogextend, cookie, Kesco.js | Сам Kesco.js влияет и на общую страницу; print не равен полной форме |
| Обычная V4, non-print | qtip, validate, mask, Kesco.V4, SignalR 2.4.3 + app hub + Kesco.SignalR, Kesco.qtip, Confirm, Dialog | Загруженный plugin ещё не означает, что конкретный `.mask`/phone/document API используется |
| Контролы даты/Grid/Tree/DocPage | Kesco.Datepicker | Общий календарь действительно участвует в формах с датой; server PeriodTimePicker остаётся отдельной логикой |
| Grid | floatThead + Kesco.Grid | Table поведение определяется и renderer settings; не все возможности включены во всех формах |
| Menu | Kesco.Menu | Сейчас есть современный эквивалент; не новый menu framework |
| TreeView | jstree + Kesco.TreeView и CSS | Keyboard/checkbox defaults jstree влияют на реальный UX |
| DocPage | Kesco.DocPage | Ни одна из 16 форм не DocPage; документный workflow вне этого переноса |
| !IsKescoRun && IsSilverLight | Silverlight + Kesco.Silver4js | Внешний desktop bridge; нельзя подменить clientName UserAgent |

Корневой script.js, отдельные Wait/LocalTime/ContactRedirector/sticker/json2/sizzle не найдены в этом loader. Прямой внешний caller остаётся возможным и должен быть доказан отдельно. 2.4.1 и 2.4.3 лежат в Styles одновременно, но исследованный Page выбирает 2.4.3. Нельзя объявить весь32-файловый набор обязательной runtime-зависимостью Clayzor.

## 3. Полный инвентарь 32 JS-файлов

Ссылки закреплены на commit; статус означает роль в миграции, а не итог прохождения браузерной проверки.

| Файл | Подключение / caller | Изученная роль | Решение для Clayzor |
|---|---|---|---|
| [js/Kesco.Confirm.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.Confirm.js) | V4 Page, non-print | Общие подтверждения; отдельные документные/телефонные workflow | CL-06; внешние функции — только по подтверждённому caller |
| [js/Kesco.ContactRedirector.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.ContactRedirector.js) | Не в показанном V4 loader; внешний contact caller | DoMakeCall → CustomConfirmPhone, явный диалог звонка | Граница CL-09/12; не обязательный встроенный dialer |
| [js/Kesco.Datepicker.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.Datepicker.js) | DatePicker/PeriodTimePicker/Grid/Tree/DocPage | Календарь, локали, Today/commit, month-only | CL-05: используемые date-only сценарии; month-only условно |
| [js/Kesco.Dialog.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.Dialog.js) | V4 Page, non-print | Window registry/context, GET/POST, array callback, close/result | CL-01/06; closeAll фактически выключен |
| [js/Kesco.DocPage.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.DocPage.js) | Только DocPage | Подписи, описание документа, AfterSave, DocView | Вне 16 Page/EntityPage форм; внешний существующий процесс |
| [js/Kesco.Grid.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.Grid.js) | При Grid | Фильтры/колонки/sort/group, действия строк, double-click text, print/export | CL-07; большая часть уже есть, точечные gap/регрессия |
| [js/Kesco.LocalTime.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.LocalTime.js) | Не зарегистрирован показанным V4 Page | Отдельный дублирующий UTC→local helper | CL-10 по семантике; не добавлять ещё один converter |
| [js/Kesco.Menu.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.Menu.js) | При Menu | Nested hover menu, задержки, уровни/иконки | Современный ClayNavigationMenu уже есть; CL-12 регрессия |
| [js/Kesco.SignalR.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.SignalR.js) | V4 Page + hub + SignalR 2.4.3 | Регистрация/присутствие, XML signal, reconnect, status; chat stub | CL-11 предметный контракт, CL-02 reconnect; чат не требуется |
| [js/Kesco.Silver4js.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.Silver4js.js) | Условно !IsKescoRun && IsSilverLight | IE Silverlight bridge, clientName, DocView, WAIT polling | Внешний desktop runtime; CL-12, не портировать Silverlight |
| [js/Kesco.TreeView.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.TreeView.js) | При TreeView + jstree | Lazy tree, active/checked, roots/search/count, state, move/copy, return | CL-08 и CL-01; перенос по фактическим действиям Territory |
| [js/Kesco.V4.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.V4.js) | V4 Page, non-print | XHR/patch, контролы, select, focus, dirty, hotkeys, modal, return, даты/tooltips | CL-01–06/09/10/12: предметное поведение без старого транспорта |
| [js/Kesco.Wait.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.Wait.js) | Отдельно не зарегистрирован V4 Page | Global wait/stop/focus; сходная логика есть в Kesco.js | CL-02/03/12: scoped operation и cleanup |
| [js/Kesco.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.js) | Базовый Page; также print до раннего return | Window open, query/cookie, focus/tab, wait, time, bootstrap | CL-01/02/03/10/12; localStorage.clear и global traps не переносить |
| [js/Kesco.qtip.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.qtip.js) | V4 Page, non-print | AJAX rich contact cards, delayed hover, nested/interactive/photo | CL-09: реальный общий gap, нужен подтверждённый contacts provider |
| [js/Silverlight.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Silverlight.js) | Условный desktop branch | Vendor embedding/detection Silverlight | Не переносить; внешние desktop-сценарии отдельно |
| [js/jquery-1.12.4.min.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/jquery-1.12.4.min.js) | Базовый Page | Vendor DOM/event/AJAX runtime | Роль зависимости установлена; новый runtime не требуется |
| [js/jquery-ui.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/jquery-ui.js) | Базовый Page | Vendor dialog/date/drag/widgets | Пользовательские контракты у Clay/Mud; не переносить весь vendor API |
| [js/jquery.cookie.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/jquery.cookie.js) | Базовый Page | Cookie helper для старых settings/return | Только узкий реальный legacy adapter CL-01; не внутренняя state bus |
| [js/jquery.dialogextend.min.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/jquery.dialogextend.min.js) | Базовый Page | Дополнительные состояния/размеры dialog | Нужные dialog сценарии CL-06; не обязательный пакет |
| [js/jquery.editable.select.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/jquery.editable.select.js) | Caller .editableSelect не найден в исследованных16 | Свободный ввод+список, фильтр, клавиатура, add/remove/destroy | Не доказанный общий blocker; строгий FK не становится free text |
| [js/jquery.floatThead.min.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/jquery.floatThead.min.js) | Grid branch | Sticky table header, resize/scroll | ClayGrid уже имеет основу; затронутая регрессия CL-07 |
| [js/jquery.qtip.min.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/jquery.qtip.min.js) | V4 Page, non-print | Vendor tooltip framework под Kesco.qtip | Роль зависимости; реализуется общий preview CL-09 без старого vendor |
| [js/jquery.signalR-2.4.1.min.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/jquery.signalR-2.4.1.min.js) | Файл есть; показанный Page выбирает 2.4.3 | Альтернативная версия legacy SignalR client | Не объявлять оба клиента активными; не ставить в Clayzor |
| [js/jquery.signalR-2.4.3.min.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/jquery.signalR-2.4.3.min.js) | V4 Page, non-print | Активный legacy SignalR client | Отделить транспорт от отсутствующего предметного CL-11 |
| [js/jquery.ui.mask.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/jquery.ui.mask.js) | Глобально non-print; .mask caller этих 16 не найден | Маски, raw/formatted, caret, paste, Esc/blur | Файл изучен; задача только после доказательства caller; SIM использует regex |
| [js/jquery.validate.min.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/jquery.validate.min.js) | Глобально non-print | Vendor validation helper | Серверные constraints+общие поля CL-03/05; загрузка не доказывает rules каждой формы |
| [js/jstree.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/jstree.js) | Tree branch | Vendor tree с keyboard/focus/checkbox/lazy/double-click | Проверены используемые участки/defaults; CL-08, не перенос библиотеки |
| [js/sticker.jquery.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/sticker.jquery.js) | Прямая регистрация в изученном V4 loader не найдена | Вспомогательное закрепление элемента при scroll | Не подтверждённый самостоятельный сценарий этих 16; не blocker |
| [json2.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/json2.js) | Отдельный старый helper; не в показанном V4 loader | JSON polyfill | Современный runtime; дополнительный polyfill без требования не нужен |
| [script.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/script.js) | V3/старые внешние callers; не показанный V4 bootstrap | Индекс функций: IE controls, modal/cookies, SOAP/XML, MD5, Silverlight, time/print | Разобраны boundary/return/bridge; не заявлен перенос всех старых V3 inputs |
| [sizzle.min.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/sizzle.min.js) | Отдельный старый helper; не в показанном V4 loader | Selector engine | Роль зависимости, не отдельная задача компонентов |


Инвентарь: 15 файлов `Kesco.*.js`, один корневой прикладной `script.js`, ещё 16 vendor/helper-файлов. Каждый исходный JS представлен ровно одной строкой в этой таблице; отсутствие обязательной задачи у helper — явное решение, а не пропуск файла.

## 4. Проверенные находки source → current → задача

### 4.1. Окна и результат — CL-01

[V4 return](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.V4.js) + [ReturnDialogResult](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Controls.V4/Common/ReturnDialogResult.cs) + [handler](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Controls.V4/Handlers/DialogResultHandler.cs) доказывают массив value/label для mvc1/4, correlation control/callbackKey и отдельный cookie fallback. [Window manager](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.Dialog.js) хранит Window/context и вызывает selection callback только для массива. В current [advanced search](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/wwwroot/js/claySelectAdvancedSearch.js) ожидает Window, а [opener](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/wwwroot/js/clayFormWindow.js) возвращает boolean. Исправление требуется в shared bridge с сохранением старых boolean consumers.

**Новый доказанный результат:** multiple Save дерева отправляет даже пустой `[]`; MVC handler не считает его cancel; `v4s_setSelectedValue(...,True)` сначала очищает прежний набор. Пустой принятый результат отличается от закрытия/отмены. Cookie empty path не объявлен доказанным для всех внешних callers. Глобальные DlgRez/RetVal не подходят двум одновременным modern selectors.

Границы: closeDialogEx не выполняет закомментированный child.close, closeAll отключён; parent-close не должен автоматически закрывать все children. Геометрия уже есть, доработки касаются request/window identity, корректного reuse и cleanup. Новый payload/route без старого caller acceptance не подтверждает legacy совместимость.

### 4.2. Input/dirty/focus — CL-02/03/04/05

[V4 handlers](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.V4.js) и [focusNextCtrl](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.js) дополняют server `NextControl`: Enter в обычном поле переводит фокус, textarea оставляет newline; lookup передаёт committed ID отдельно от текста. Простые Clay wrappers не экспонируют единый управляемый focus/next/field-error контракт; это конкретный gap, не отсутствие базового Tab. [clayFocusElement](https://github.com/Postnn/Kesco.App.Web.Inventory/blob/b3da274962c3c0811f71fbfe196f188ed3b5f659/wwwroot/js/app.js) имеет бесконечное ожидание видимости через RAF без отмены/dispose.

[ClaySelect Interaction](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Select/ClaySelect.Interaction.cs) уже содержит поиск, typed selection, resolver, version/cancel, Escape backup, auto-single/exact, view/create/advanced. Но `_activeIndex` только по results, buttons за ними, Tab/blur закрывают popup: клавиатурный путь к actions не определён устойчиво. Требуются focus-within/action navigation и активная строка в viewport, с сохранением [намеренных современных правил](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Select/AGENTS.md): no-autoselect по кнопке списка, hover отдельно от keyboard index, readonly запрещает ViewItem.

Legacy dirty `v4t_CheckChange` сканирует только часть видимых DOM-полей, beforeunload условный и зависит от validate. Современный snapshot включает radio/скрытый draft/children; invalid input не теряется. В current уже есть page NavigationLock; новый общий guard должен сочетаться с [forceLoad reconnect](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Services/ClayDbReconnectService.cs), а не дублировать авторизацию или отключать reconnect. Неопределённый write не повторяется автоматически.

[Datepicker](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.Datepicker.js) и V4 parser дают сокращённый ввод, Today/commit, локали и month-only. ClayDatePicker уже существует, но явный contract compact/partial/today/focus нуждается в проверке/расширении. Legacy year limit 2078, pivot 50 и очистка ошибки — особенности, требующие явного решения, а не глобальные бизнес-ограничения. Duty и период отчёта остаются date-only. Integer выводится из домена/SQL, не Precision=0; NumPool.Ctrl требует подтверждения схемы. `.mask`/`.editableSelect` callers этих 16 не подтверждены.

**Действующий DR-01.** Этап 1 защищает raw input живого экземпляра, включая ввод до server callback, хранит ключ AES только в RAM и адресно удаляет черновик при штатном закрытии. Долговечный серверный журнал сохраняет исход команды независимо от редактора. После полной загрузки ввод начинается с БД. См. [действующий контракт](../../docs/agents/forms.md#загрузка-даты-и-черновики), раздел 13 [COMMON](01_COMMON_RULES.md), CL-02 [общего промта](PROMPT_SHARED_CLIENT_BEHAVIOR.md) и G0.6 [оркестратора](00_ORCHESTRATOR.md). Карта применимости сохраняется для всех 16 форм, включая read-only критерии и вложенные редакторы.

### 4.3. Dialog — CL-06

`v4_showConfirm` различает Yes/No/Cancel; `v4_confirmMsgBox` имеет иной default close→No; callbacks и focus задаёт caller. [Current ConfirmDialog](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/ConfirmDialog.razor) — bool. Для LocationFact нужен общий typed трёхисходный вариант с совместимостью bool consumers. Escape/title close/backdrop не приравниваются к одной бизнес-команде без контракта формы. Документные/телефонные классы [Confirm.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.Confirm.js) не делают эти функции обязательными для всех 16.

### 4.4. Grid — CL-07

[Grid JS](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.Grid.js) сопоставлен с ClayGrid partials, dialogs, JS, state, print/export. Фильтры с draft/Cancel, column settings, grouping, sticky header, paging, state и child refresh имеют современную основу. Подтверждённое несовпадение — [process-static DraggedColumn](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Grid/ClayDragState.cs), видимый всем circuits; нужен owner grid/session. Default Id `clay-grid` также требует уникальных экземплярных IDs, особенно для нескольких static tables.

| Тонкость | Подтверждённая семантика | Решение |
|---|---|---|
| Value filter all/none | Оба снимают этот value filter: legacy Grid.SetFilterByColumnValues очищает коллекцию, фильтрация применяет её только Count>0; current dialog использует ClearedSentinel | Сохранить; **не путать с принятым empty multiple tree result** |
| Sort limits | Legacy settings UI 5; server/header 3; current header 2, но dialog/state path отдельный | Согласовать header/dialog/state, не требовать безусловно5 для каждого грида |
| Batch selected keys | Current HashSet<int>/int.TryParse; обычные row actions уже поддерживают строковый ключ | Finder composite/string return делать typed RowAction; generic batch refactor только нужному consumer |
| Double-click cell | Styles выделяет весь текст ячейки | Общий ограниченный hook/опция при сохранении этого UX; не превращать в edit/return по умолчанию |
| Несколько таблиц | [Grid AGENTS](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Grid/AGENTS.md): максимум один DynamicGrid на страницу | NumPool/Modifications — static коллекции, Type master static + detail один Dynamic |

### 4.5. Tree — CL-08

[Tree JS](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.TreeView.js) + jstree/defaults + server renderer: active узел правой области и checked набор независимы; three_state/whole_node/tie_selection false; ancestor/descendant исключаются на одной ветви, siblings допустимы. Текущий ClayTree имеет только None/Single; keyboard DOM focus отсутствует в тексте-node span, RestoreFocus меняет selection. Нужны общий multiple, focus и контроль начальной selection без повторного навязывания на каждом lazy-load.

[Current filter](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Tree/ClayTreeView.Filter.cs) при zero nodes загружает обычные roots; [load result](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Tree/Models/ClayTreeLoadResult.cs) не имеет full total. Территории требуют отдельного no-match state и exact count до TOP100; max+1/capped не равно full count. Match rows/ancestor context/normal roots различаются. Добавляются доступный quick-search через общую filter model, keyboard, double-click expand/typeahead и ID tooltip в применимом режиме. Tree move/copy/guards уже имеют основу: не переписывать их и не включать недоступные форме действия.

Сохранить текущую принятую single-path persistence; legacy collapse закрывает descendants, current in-memory state иной — фиксируется решение, не безусловный откат. Root scope/CLID/keys/HTTP TerritoryData остаются обязательными требованиями частного промта. **Save return=2 подтверждает выбор, не пишет территории в БД.**

Уточнение по сохранению существующего Clayzor: возврат корней при пустом filter описан в Tree AGENTS/TF_E; это установленная текущая семантика, а не универсально доказанная ошибка всех consumers. Требование Territory к no-match реализуется совместимым настраиваемым режимом с явным включением этой формой и сохранением прежнего default остальных деревьев. Статический аудит различий не разрешает глобально отменять современные контракты ради совпадения с legacy; регрессионные условия — раздел 12 COMMON.

### 4.6. Карточки контактов — CL-09

[RenderLinkPerson/RenderLink](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Controls.V4/Common/Page.cs) → caller-type/data-id → [AJAX qtip](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.qtip.js); подтверждённые consumers: NumPool, Maintenance, Modifications, Duty, LocationFact owner, audit author через [Changed](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Controls.V4/DBControls/Changed.cs). Отложенное открытие, отмена таймера показа при уходе, интерактивные ссылки, viewport и nested/photo поведение не заменяются обычным title. В legacy AJAX success нет собственного request/version guard; отмена загрузки при смене контекста/dispose и изоляция позднего ответа — дополнительные требования современного preview, а не приписанная старому коду гарантия. Current [tooltip helper](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/wwwroot/js/clayTooltipViewport.js) решает только позиционирование обычной подсказки.

Нужен общий preview/provider contract. Config.contacts endpoint/поля/права/фото/звонки должны быть установлены по внешнему приложению; полного DTO в Styles нет. Не выдумывать SQL/schema, не выполнять переданный HTML/JS и не запускать звонок на hover. ReadOnly Select policy сохраняется отдельной разрешённой ссылкой. [Current Changed](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Form/ClayChanged.razor) пока выводит простую авторскую ссылку target_blank; его подключение к общему preview/window — точечная доработка.

### 4.7. Время — CL-10

[ClayLocalTime](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Form/ClayLocalTime.razor) и ClayChanged уже вычисляют offset на UTC-дату записи. [DynamicGrid](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Grid/ClayGrid.Dynamic.cs) получает текущий offset единожды; [converter](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Grid/Dynamic/ClayDateTimeConverter.cs) сдвигает только Kind.Utc, DateTimeOffset переводит через `.DateTime`. [Dapper row loading](https://github.com/Postnn/Clayzor.Lib.Entities/blob/bf909e66d89a9a9c2f3fdc5069dd03b9b56368d7/DynamicGrid/DynamicSql.cs), [startup column mapper](https://github.com/Postnn/Clayzor.Lib.Entities/blob/bf909e66d89a9a9c2f3fdc5069dd03b9b56368d7/MedicalTests/MedicalTest.cs) и [DAL](https://github.com/Postnn/Clayzor.Lib.DALC/blob/4491634cb87272b24ac5b3e90e9fc43e1648963d/DbManager.cs) не вводят UTC normalization.

По исходнику доказано: поданный Unspecified для metadata 10/13 не получает обещанный UTC→local shift; current offset не покрывает другую сезонную дату. Наблюдение ошибки конкретной live строки не заявляется. Нужен единый instant contract для этих колонок/аудита и screen/print/export, без изменения date-only и бизнес TimeZone. [Print/export reader](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Grid/Dynamic/ClayDynamicCellReader.cs) должен использовать тот же смысл преобразования.

### 4.8. Совместная работа — CL-11

[Client](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.SignalR.js) + [EntityPage](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Controls.V4/Common/EntityPage.cs) + [Page](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Controls.V4/Common/Page.cs) + [Hub](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.SignalR/KescoHub.cs) показывают предметные presence/edit flags, регистрацию/перерегистрацию, отправку изменений контролов и RefreshForce, иногда shared Entity reference. Это больше, чем транспорт Blazor. В current аналогичный общий бизнес-контракт не найден.

Scope не универсален: PhonePoolAtribute явно `IsCollaborateWork=false`; SignalUI имеет menu/positive-int-key условия; BeelineParams содержит пустой `ActionAfterSignalRConnected` override. Поэтому нужна per-form eligibility карта, не blanket subscription всех карточек/отчётов. Подтверждённую live field collaboration нельзя объявить готовой одной панелью присутствия. Требования shared описывают typed адресные события, доверенную identity, защиту dirty/conflict, lifecycle и отсутствие replay/force reload. Нового чата не требуется: legacy chat stub/закомментирован.

### 4.9. Runtime и внешние границы — CL-12

[Bootstrap](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.js) вызывает `localStorage.clear()` для origin; global Tab loop может не завершиться без focusable элемента. V4 использует общий XHR/XML/eval; старые Grid/Tree/dialog callbacks зависят от globals. Эти технические особенности не переносятся. Современные state keys, timers/listeners/RAF и subscriptions должны иметь владельца, версию, cleanup; несколько форм/пользователей не делят mutable state.

[Menu](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Navigation/ClayNavigationMenu.razor.cs) + [JS](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/wwwroot/js/clayNavigationMenu.js) уже имеют hover 300, keyboard/touch, viewport, disabled, dispose. Регрессия существующего меню достаточна. DocPage/DocView/ActiveX/Silverlight/V3 SOAP — внешние/старые поколения, не 17-я форма. Перед новой интеграцией desktop clientName, cookie return и contacts следует подтвердить реального caller и согласовать текущий EquipmentForm/Net контур.

## 5. Как уточнены все 16 частных промтов

В каждом файле добавлен раздел «Дополнение по общим скриптам Styles» с CL-ID, конкретной семантикой, источниками и наблюдаемой приёмкой. Прежняя бизнес-логика/SQL сохранены; в TerritorySearch дополнительно исправлена неопределённость empty Save на доказанное MVC4 поведение.

| Форма / файл | Существенное дополнение |
|---|---|
| [Finder](_DONE/PROMPT_Finder.md) | Typed составной ключ оборудования, один consumed callback, keyboard advanced/return, Grid state, bool/Window gate |
| [FinderTel](_DONE/PROMPT_FinderTel.md) | Numeric phone key, актуальный контекст/late response, keyboard caller, cancel сохраняет выбор; без ненужного batch refactor |
| [TypeEquipmentMenu](PROMPT_TypeEquipmentMenu.md) | Master static/detail один Dynamic, общий opener, независимые state IDs, существующее меню |
| [SIM_FMTN](PROMPT_SIM_FMTN.md) | Точная цепочка Prefix→Short→Description→Save; regex/paste/leading zeros; caller draft/Changed |
| [BeelineParams](PROMPT_BeelineParams.md) | Два writable поля, Windows TimeZone отдельно от display timezone, readonly Time; eligibility collaboration отдельно |
| [PhonePoolAtribute_form](PROMPT_PhonePoolAtribute_form.md) | Fixed pool/type readonly, value commit/maxlength150, разрешённый просмотр отдельно; explicit collaboration opt-out |
| [NumPool](PROMPT_NumPool.md) | Шапка focus, исправление missing NextControl, подтверждённое добавление/Private, четыре static grids, contacts/time/unique keys |
| [PhoneCodeEntity_form](PROMPT_PhoneCodeEntity_form.md) | Цепочка полей, integer AreaLength, lookup action keyboard, targeted refresh и audit |
| [TerritorialEntity_form](PROMPT_TerritorialEntity_form.md) | Девять полей focus, OKCM NULL/0/integer, отдельный readonly drilldown, child cards без потери draft |
| [TerritorialEntity_search](PROMPT_TerritorialEntity_search.md) | Active≠checked, branch exclusion, empty Save≠Cancel, exact total/empty result, keyboard/focus, single-path state |
| [Maintenance](PROMPT_Maintenance.md) | Employee card, явное выполнение/подтверждение, нет повторного INSERT, scoped busy и временная семантика |
| [Modifications](_DONE/PROMPT_Modifications.md) | Date-only period commit, UTC columns, contacts, несколько static tables с отдельными state/snapshots, print/export |
| [PingHost](PROMPT_PingHost.md) | Start/Stop operation scope, disposal/cancel/late callback; не глобальный wait и не фиктивная editable entity |
| [duty_form](PROMPT_duty_form.md) | Date-only/динамический Required, keyboard radio и focus, employee contacts, audit отдельно |
| [LocationFact](_DONE/PROMPT_LocationFact.md) | Scanner Equipment→Equipment, три результата dialog, owner card внутри modal, отсутствие позднего focus/повторного write |
| [SlBl_form](PROMPT_SlBl_form.md) | Textarea newline/paste/commit, последовательные подтверждения, сохранность address/kind/text и неизвестного результата, явная mail operation |

Ни один частный промт не разрешает переносить arbitrary JS callback/HTML или изобретать серверные права/данные ради UI-паритета. Hotkeys F2/Insert только явно назначенные, не автоматическое новое действие16 форм.

## 6. Что изменилось в оркестраторе

Добавлен G0.5 с отдельными CL-статусами и владельцами, без глобального барьера «сначала реализовать весь Styles». Контракт single window может быть принят до Tree multiple; contacts не блокируют TypeEquipmentMenu/Ping; metadata date fix и grid isolation развиваются независимо от card CRUD. Shared реализация принимается перед зависимыми consumers, но новые файлы форм можно готовить параллельно по согласованному API.

Назначаются отдельные роли окон/dialog, inputs/lifecycle, Grid/Tree/time, contacts/collaboration по доступным слотам. Один физический файл — один текущий писатель; DI/resources/SQL и суперпроект принимает интегратор. Для каждой поставки фиксируются база, CL версия, consumers, результаты проверок и незакрытая интеграция. Существующий агент EquipmentForm/Net остаётся в том же реестре владения.

G0.6 закрепляет общий механизм за одним владельцем, владельцы форм подключают согласованный контракт. Приёмка охватывает живой reconnect, RAM-ключ, адресное удаление и независимую сверку команды после IIS restart. Сценарии ещё предстоит проверить при реализации. Этот source audit фиксирует только наблюдения по legacy и current.

## 7. Что ещё необходимо подтвердить при реализации

1. Живой contacts provider/DTO/права и внешние phone/photo/desktop действия; из JS их полную реализацию восстановить нельзя.
2. Конкретные старые внешние callers cookie/POST протоколов, их origin и пустой-result semantics; доказанный MVC4 путь не равен всем внешним путям.
3. Действующие AGENTS/новые commits: это snapshot, не утверждение, что агент исправлений не изменил компонент после него.
4. Browser order blur/paste/keyboard/window/focus, popup blocking, cross-origin/reused windows и cancellation; всё это не подтверждается одним source review.
5. SQL timestamp semantics metadata 10/13 и реальные CLR значения/локаль; дефект converter доказан для соответствующих входов, но live данные не просматривались.
6. Eligibility и ожидаемый режим collaboration каждой EntityPage, одновременно работающие пользователи и actual rights/concurrency; empty override/opt-out не игнорировать.
7. Предметные типы вроде NumPool.Ctrl, допустимые date shorthand/pivot/minmax, sort header/dialog/state и Tree collapse/typeahead решения; они должны быть записаны явно и проверены на consumers.

Этот список задаёт точные условия приёмки и не останавливает независимые работы. Итоговый пакет содержит **21 Markdown-файл: 16 отдельных промтов форм, один shared-промт, оркестратор, COMMON и два реестра/аудита**. Архив содержит весь согласованный набор; shared-доработки не увеличивают число оставшихся бизнес-форм.
