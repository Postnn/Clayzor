# Промт реализации FinderTel — расширенный выбор телефонного номера

## Задача и существующая основа

Реализуй отдельное окно/страницу расширенного выбора, эквивалентное `Inventory/Default/FinderTel.aspx`, со всеми контекстными фильтрами, колонками, выбором и возвратом результата. Прочитай [общие правила](../01_COMMON_RULES.md) и [промт Finder](PROMPT_Finder.md): обе формы используют общий транспорт выбора, но **разные SQL-функции и разные предметные колонки**.

Исследование выполнено по legacy `KescoDev/Kesco.App.Web.V4.Inventory@12a6efae7eb310adec06a57f6d80b8d9e76985b4`; Inventory `b3da274962c3c0811f71fbfe196f188ed3b5f659`; Entities `bf909e66d89a9a9c2f3fdc5069dd03b9b56368d7`; DBSelect `9b8551dab6228ebcaff7506110c71fddff5b6f03`; Controls `ea980be926ae975678314029ba5fc8aa68c675ba`. Отдельного FinderTel в новом Inventory нет. Однако **значительная часть отбора уже реализована**: `PhoneNumberSelectDataSource`, `PhoneNumberLookup`, `PhoneNumberQuery`, `SQLQueries.SELECT_ТелефонныеНомераДляВыбора` и `EquipmentForm.UpdatePhoneSourceFilters()` вызывают ту же `fn_НомераДляТелАппаратов` с пятью параметрами. Используй и при необходимости расширяй эту общую основу, не создавай конкурирующее бизнес-правило выбора.

### Источники

- [FinderTel.aspx](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Inventory/Default/FinderTel.aspx), [FinderTel.aspx.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Inventory/Default/FinderTel.aspx.cs): `FillFinderTel`, `FinderTelRender`, `ConfigureGrid_Common`, `RenderColumn_Telephone`.
- [DBSEquipmentPhone.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.DBSelect.V4/DBSEquipmentPhone.cs), [DSOEquipmentPhone.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.DBSelect.V4/DSOEquipmentPhone.cs), [Equipment_form.aspx.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Inventory/Equipment/Equipment_form.aspx.cs): `dbsPhone_BeforeSearch` / `dbsPhone_Changed` / `Grid_Phone_AddRecord`.
- [SQLQueries.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Entities/SQLQueries.cs): `SELECT_fn_НомераДляТелАппаратов`, `SELECT_ТипыТелефонныхНомеров`.
- [PhoneType.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Entities/Corporate/Phones/PhoneType.cs), [GridSettings.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Controls.V4/Grid/GridSettings.cs), [GridColumn.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Controls.V4/Grid/GridColumn.cs): тип/иконки и фактическая семантика ссылок.
- [ReturnDialogResult.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Controls.V4/Common/ReturnDialogResult.cs), [Config.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Settings/Config.cs): callback-параметры и `URI_tel_finder`.
- [Текущий PhoneNumberSelectDataSource](https://github.com/Postnn/Clayzor.Lib.Web.DBSelect/blob/9b8551dab6228ebcaff7506110c71fddff5b6f03/Equipment/PhoneNumberSelectDataSource.cs), [PhoneNumberQuery](https://github.com/Postnn/Clayzor.Lib.Entities/blob/bf909e66d89a9a9c2f3fdc5069dd03b9b56368d7/Inventory/PhoneNumberQuery.cs), [новый SQLQueries](https://github.com/Postnn/Clayzor.Lib.Entities/blob/bf909e66d89a9a9c2f3fdc5069dd03b9b56368d7/SQLQueries.cs), [новый EquipmentForm](https://github.com/Postnn/Kesco.App.Web.Inventory/blob/b3da274962c3c0811f71fbfe196f188ed3b5f659/Components/Pages/Equipment/EquipmentForm.razor.cs).
- [ClaySelectAdvancedSearchService](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Services/ClaySelectAdvancedSearchService.cs), [claySelectAdvancedSearch.js](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/wwwroot/js/claySelectAdvancedSearch.js), [clayFormWindow.js](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/wwwroot/js/clayFormWindow.js).

## 1. Входной контракт

Добавь `/Default/FinderTel` и совместимый `/Default/FinderTel.aspx`; заголовок **«Выбор телефонного номера»**. Legacy `DBSEquipmentPhone.URLAdvancedSearch = Config.phone_finder`, а Config читает `URI_tel_finder`. Действующий host/base URL может находиться в `uri.local`, не в репозитории: проверь конфигурацию, не зашивай production-host и не перенаправляй внешние сайты целиком.

| Query parameter | SQL parameter | Смысл |
|---|---|---|
| `fLocation` | `@LocationId` | Расположение оборудования |
| `fEmployee` | `@EmployeeId` | Ответственный/сотрудник оборудования |
| `fTypeEquipment` | `@EquipmentTypeId` | Тип оборудования |
| `fMainNumber` | `@PhoneIdBase` | Уже выбранный основной номер |
| `fEquipment` | `@EquipmentId` | Текущее оборудование |

Отсутствующий параметр и строка `"0"` в legacy дают SQL NULL. Все пять параметров независимы; нельзя потерять последний при сборке URL, перепутать тип оборудования с типом телефонного номера или использовать fMainNumber как часть display номера. Непустое неверное/overflow значение отклонять до SQL с понятным сообщением, не превращать в NULL и расширенную выдачу. Решение о допустимых отрицательных значениях должно следовать реальным функциям/источникам: не выдумывать sentinel.

Современный вызов дополнительно получает `requestId`, `returnOrigin`, `return=1`. Legacy-механизм добавляет `return`, `mvc=4`, `clid`, `control`, `callbackKey`, `callbackUrl`; поля `mvcDialogResult` — `escaped`, `control`, `multiReturn`, `value`. Определи и реализуй поддерживаемый compatibility adapter в общем слое, а не индивидуальную небезопасную отправку на переданный callbackUrl.

## 2. Выборка: сохранить контекст, не писать упрощённый справочник

Основной запрос:

```sql
SELECT *
FROM dbo.fn_НомераДляТелАппаратов(
    @LocationId, @EmployeeId, @EquipmentTypeId, @PhoneIdBase, @EquipmentId
) X
```

Это бизнес-функция допуска телефона к оборудованию. Её DDL и полный выходной schema contract в репозитории формы отсутствуют: на стенде зафиксируй параметры, типы и **полный список колонок в порядке результата**. Нельзя заменить функцию таблицей всех телефонов, восстановить её условия по названию или объявить полный охват без сравнения реальной выдачи. Получение схемы/данных и соответствующий доступ — проверяемый шаг реализации, а не основание считать функцию изученной внутри до чтения DDL.

В новом приложении имеющийся lookup дополнительно соединяет результат с `vwТелефонныеНомераИзменение`, сотрудниками и станциями, строит краткий текст и XorcomName. Для грида требуется полный row projection функции, а не только эти четыре поля lookup. Допустимо вынести общий request/model и чтение в Entities, сохраняя один источник семантики и повторно используя DBSelect для компактного выбора.

Сортировка/поиск/фильтрация/пагинация на стороне SQL Server; `ROW_NUMBER` для 2008 R2, стабильный порядок с ключом, 35 строк по умолчанию. В legacy DataTable сначала загружается целиком: архитектурное улучшение не должно менять множество строк или потерять выбранную запись. Сортируемые/фильтруемые колонки только из подтверждённой схемы, значения параметризованы.

### Исключения уже выбранных номеров

`Equipment_form.dbsPhone_BeforeSearch` перед компактным поиском задаёт те же пять параметров и `IDs.Inverse=true` для уже добавленных номеров; `FinderTel.FillFinderTel` явно читает только пять query-параметров и сам список исключений не разбирает. Не приписывай FinderTel несуществующий legacy query contract. В новом EquipmentForm есть `ExcludedPhoneIds`. При подключении расширенного окна сохрани согласованное поведение caller: не добавлять дубликаты, перепроверять eligibility и исключения при возврате, согласовать передачу списка в общем typed request без неподтверждённых старых параметров.

**Обнаруженный риск текущей основы:** в исследованном срезе `SELECT_ТелефонныеНомераДляВыбора` заканчивается `ORDER BY Телефон`, а `PhoneNumberQuery.AddExcludedIds` дописывает `AND ... NOT IN(...)` в конец строки. Проверить актуальный HEAD: если это ещё не исправлено, условие должно попадать в WHERE до ORDER BY в общем query builder. Не копировать дефект в FinderTel и не чинить второй локальной SQL-строкой. Обязателен тест с непустыми исключениями.

## 3. Колонки и визуальное поведение

Legacy `FinderTelRender` задаёт:

1. `КодТипаТелефонныхНомеров` первым, заголовок **«Тип»**, специальный renderer иконки;
2. `ПолныйНомер` вторым;
3. `ТелефонныйНомер` третьим;
4. остальные столбцы возвращённой таблицы — по исходной схеме/настройкам;
5. `КодТелефонногоНомера` скрыт, но используется для выбора;
6. заголовок `КодОборудованияПодключения` заменён на **«Оборудование подключения»**.

Не сокращай форму до двух колонок «Номер / Абонент» и не объявляй выдуманное фиксированное количество колонок. Приложи к handoff точную карту полного result schema → видимость/заголовок/порядок/формат новой формы.

**Неочевидные ссылки:** code-behind вызывает `SetColumnHrefLocation("КодОборудованияПодключения", "КодРасположения")`: при действующем ключе это ссылка к **расположению**, хотя заголовок говорит об оборудовании. Не меняй её самовольно на EquipmentForm. Для `Абонент` вызван `SetColumnHrefEmployee("Абонент", "")`; стандартная ветка GridColumn требует непустой `HrefIdFieldName`, поэтому сам этот вызов не создаёт рабочую employee-ссылку. Не извлекай employee ID из телефонного ID или текста абонента. Зафиксируй фактический результат на legacy/схеме; изменение назначения ссылки или добавление новой связи — отдельное решение, а не молчаливый «фикс».

Типы телефонов читаются один раз списком через `PhoneType.ListPhoneTypes(Config.DS_user)` → `SELECT_ТипыТелефонныхНомеров`. Для каждой строки сопоставляются код типа, `Name` tooltip и `Icon`; legacy path `/STYLES/{icon}`. Не зашивай лишь известные 100/200 типы: неизвестный код/NULL/нет картинки должны корректно отображаться без поломки таблицы и выбора. Используй общий проверенный путь ресурсов/иконок, текстовый fallback и HTML escaping. На каждую строку отдельный SQL-запрос запрещён.

Настройки грида: полный размер, фиксированная шапка, видимые заголовки даже без строк, пустая заглушка, paginator, 35 строк. Group panel выключен; filter options доступны, filter panel первоначально скрыта. Служебных delete/detail колонок нет, есть **кнопка возврата «Выбрать»** у строки. Кнопки формы Edit/Apply/Refresh/OK убраны: сохранения сущности здесь не существует. Оставшиеся базовые команды вроде Отмена/Справка не заменять новыми CRUD-кнопками.

## 4. Возврат результата и связь с EquipmentForm

Legacy `SetServiceColumnReturn("v4_returnValue", [КодТелефонногоНомера, КодТелефонногоНомера], "Выбрать")` передаёт **ID дважды**, не display text. Сохранить смысл: родитель получает идентификатор, сам восстанавливает данные. Выбор строки **не создаёт** `ТелефонныеНомераОборудование` и не синхронизирует АТС. Такие записи выполняет aggregate родительской формы при её сохранении.

В современном пути:

- `ClaySelectAdvancedSearchService.OpenAsync<TItem,TValue>` ждёт результат, возвращает `ClaySelectExternalResult.FromValue(id)`; caller восстанавливает элемент через источник.
- Дочерняя форма вызывает `claySelectAdvancedSearch.returnValue(requestId, id)`. Value имеет корректный JSON numeric тип для int phone ID, не строку с произвольным HTML/JSON и не полный объект телефона с секретами.
- Message имеет тип `clay-select-return`; проверяются requestId, origin и source. Отмена/закрытие возвращают null и не очищают прежнее значение родителя.
- `EquipmentForm.OnPhoneToAddChangedAsync/AddPhoneAsync` добавляет draft-связь, исключает дубли, определяет оборудование подключения; сохрани эти действия и dirty-state. Родитель не должен reload/закрыться или потерять другие несохранённые поля.
- Сам `GetByIdAsync` имеющегося PhoneNumberSelectDataSource читает общий номер без пяти фильтров. Он восстанавливает display, но **не доказывает допустимость** результата для текущего контекста. При возвращении из окна, когда родитель уже сменил модель/сотрудника/расположение, нужно отвергать устаревший запрос или заново проверять выбор по актуальным параметрам.

### Обязательная зависимость от общего исправления bridge

В pinned `claySelectAdvancedSearch.open` переменная `child` получается из `clayOpen`, затем используется как Window (`event.source`, `.closed`, `.close()`). Но pinned `clayFormWindow.open` возвращает bool. Этот общий дефект должен быть исправлен **один раз на подготовительном этапе Finder/оркестратора**, сохранив все действующие вызовы и централизованное управление окнами. Не объявляй наличие JS-файла доказательством работающего выбора. FinderTel принимается только после реального browser теста return/cancel, двух параллельных запросов и закрытия родительского circuit. Не добавлять локальный `window.open` или отключать origin/source checks для обхода дефекта.

Поддержка legacy `return=2`/multiReturn не возникает автоматически из наличия hidden поля: форма фактически выбирает строку; текущий ClaySelect bridge ориентирован на один ID. Если реальный caller требует multi-return, подтвердить и реализовать общий контракт отдельно; не принимать массив там, где ожидается int. Для `mvc=4` старого caller должен быть либо работающий совместимый adapter, либо явное документированное ограничение; молчаливое закрытие без результата не считается реализацией.

## 5. Безопасность, ошибки и границы

FinderTel — read-only окно. Запись, назначение номеров, PoE, команды оборудования и АТС отсюда отсутствуют. Используй текущую серверную идентичность и права видимости; наличие произвольного QueryString не разрешает обходить авторизацию. В исходном code-behind нет отдельного HasRole: не назначай роль по догадке. Повторно проверяй выбранный ID и контекст на серверной границе родителя.

Не исполнять callback function/string из URL, не отправлять результаты на произвольный callbackUrl/returnOrigin. Общий сервис должен связывать origin с разрешённым вызывающим приложением/созданным запросом. URL/поля SQL/HTML encode, логирование без лишних персональных данных. Ошибка SQL не рисуется как «ничего не найдено», выбор при недостоверной выдаче недоступен. Cancellation/dispose убирают listener/timer/pending task; повторный click выбирает один раз.

## 6. Приёмка

1. Открыть основной и `.aspx` URL с каждым из пяти фильтров по отдельности и вместе; SQL параметры совпадают с legacy mapping.
2. Отсутствие/0 нормализуется в NULL; неверное/overflow значение не расширяет выборку молча.
3. Выдача сверена с реальной `fn_НомераДляТелАппаратов` для разных типов оборудования, расположений, сотрудников и основного номера.
4. Полная карта колонок сопоставлена; первые три, скрытый ключ и подпись «Оборудование подключения» совпадают.
5. Иконки известного/неизвестного/NULL типа, tooltip и fallback не ломают строки.
6. Проверена фактическая ссылка оборудования подключения к расположению и отсутствие ошибочной employee-ссылки по телефонному ID.
7. 35 строк, >35 строк, пустая выдача, filter options, sorting и стабильная серверная пагинация.
8. Выбор возвращает один phone ID в правильный caller и один раз; никаких SQL-записей в FinderTel.
9. Отмена кнопкой/крестиком/закрытием вкладки не меняет родителя, завершается pending request.
10. Fake origin, другой window source, неизвестный requestId и неправильный payload отклоняются.
11. Два открытых выбора не перепутаны; повторный запрос после отмены работает; dispose родителя освобождает resources.
12. Popup blocked обрабатывается, форма не зависает с вечным busy.
13. Родитель EquipmentForm сохраняет draft; selected number добавляется в его коллекцию, но не в БД до его Save.
14. Уже выбранные ID не дублируются; непустые ExcludedPhoneIds дают синтаксически корректный SQL и правильную выдачу.
15. Контекст родителя изменён во время открытого FinderTel: устаревший результат не добавляется без проверки.
16. Номер удалён/стал недоступен после загрузки: нет ложного успешного выбора.
17. Старый caller `mvc=4`/callback и новый requestId-путь проверены по заявленному контракту; unsupported return2 явно обозначен.
18. Самостоятельное открытие без caller не падает и не отправляет результат случайному окну.
19. SQL error, отменённый запрос, потеря соединения и отсутствие прав имеют отличимое состояние, без утечки данных.
20. Мобильный viewport 320–480px, клавиатура/фокус, общий ClayGrid/окно; parent не reload/close.

Handoff: маршруты, caller/config mapping, все 5 параметров, фактический function schema, reuse нового query/lookup, результаты устранения общего bridge/exclusion риска, legacy callback совместимость, реальные SQL/browser checks. Непрочитанное DDL/непроверенный callback явно указать. Не изменять unrelated EquipmentForm/Tel/Net исправления и не коммитить/пушить без актуальной авторизации.

## Дополнение по общим скриптам Styles

Прочитай [PROMPT_SHARED_CLIENT_BEHAVIOR.md](../PROMPT_SHARED_CLIENT_BEHAVIOR.md) и [03_STYLES_CLIENT_AUDIT.md](../03_STYLES_CLIENT_AUDIT.md). Зависимости этой формы: **CL-01** (одноразовый callback/окна), **CL-03** (клавиатура/фокус), **CL-04** (Select), **CL-07** (Grid), **CL-12** (lifecycle и состояние). Клиентские первоисточники — [Kesco.Grid.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.Grid.js) и [Kesco.V4.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.V4.js), применённые только в фактической конфигурации FinderTel.

**Один выбор — один принятый результат.** RowAction передаёт числовой phone ID по согласованному контракту, родитель восстанавливает номер и проверяет актуальную допустимость. Не выводи тип payload из внутреннего batch-selection `HashSet<int>`: это отдельная функция грида. Общая поддержка строковых/составных keys для других форм не должна заставлять FinderTel менять numeric JSON ID на строку или полный объект. После успешного приёма requestId больше не может быть потреблён; повторный message не добавляет вторую draft-связь. На время возврата действие блокируется, отмена оставляет прежние данные родителя.

**Состояние грида.** Настрой уже существующие диалоги column/value filter, sorting, column settings, pager и fixed header. Cancel/Escape/закрытие фильтра сохраняют прежнее условие; Apply применяет новый черновик; в value filter и all, и none снимают пользовательский фильтр значений — так работают legacy Grid.SetFilterByColumnValues и current ClayColumnValueFilterDialog.Apply. Это не действие confirmed empty multiple-return дерева. Clear user filter сохраняет пять входных параметров и ExcludedPhoneIds. Первоначально скрытая filter panel не выключает доступные filter options. Header-sort современного ClayGrid, legacy server/header с ограничением трёх и legacy settings dialog с пятью priorities имеют различие, описанное в CL-07; локальный патч сортировки или безусловное требование пяти критериев здесь не нужны. Group panel и row DnD остаются согласно исходной конфигурации.

**Клавиатура и текст.** Enter/Space на «Выбрать» выполняют ровно один return, клавиша в поле фильтра не запускает чужую строку. При закрытии окна общий focus manager возвращает фокус исходному контролу EquipmentForm; его draft остаётся. Legacy double-click `.grid td` относится к выделению текста, поэтому не использовать его как дополнительный выбор номера/редактирование. Общая text-selection опция не должна перехватывать номер-ссылку, icon action, checkbox или редактор. Типовая иконка сохраняет tooltip из PhoneType.Name и безопасный fallback; имя абонента не становится ссылкой на произвольно выбранного сотрудника.

**Несколько окон.** У Grid уникальный стабильный Id; requestId, listener, DOM bindings и cleanup принадлежат одному экземпляру. Finder и FinderTel не делят current row, dialog state или имя открытого запроса. Дефект process-static `ClayDragState` исправляется в CL-07 одним владельцем, не обходится частной реализацией на странице. Cancel/dispose одного запроса не закрывают другой.

Дополнительная приёмка: Escape и Apply фильтра; clear user filter с непустыми исключениями; return со второй страницы; двойное нажатие Enter/двойной message; Finder и FinderTel одновременно; старый результат после нового запроса/смены модели; keyboard focus после cancel; double-click текст без добавления связи. Browser-проверка должна включать настоящий EquipmentForm и сохранность его несохранённых полей.
