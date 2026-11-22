# NET-1 — форма порта локальной сети

## Статус

Реализована страница `/Net/Net?id=<КодЛокальнойСети>` с основной записью, двумя
независимыми списками ссылок, справочниками, тремя атомарными режимами записи,
Changed, OK/Применить/Отмена и PoE. Изменения закоммичены и запушены в `main`; merge
не выполнялся.

Реализация готова к независимому ревью. Полная приёмка не объявляется: реальный PoE
не вызывался, `sp_ResetPoEport` на стенде отсутствует; реальные MAC ILO и коллизия
сетевых имён заблокированы триггерами модели/уникальности; стенд SQL Server 2022,
а не 2008 R2.

## Ревизии публикации

| Репозиторий | HEAD | Состояние |
|---|---|---|
| Суперпроект | текущий коммит публикации | ссылки на сабмодули, `Net_1.md` и этот handoff |
| Inventory | `1f7a12c07d0f789be9cf81cfba32c2cc7448dfb0` | `main`, push выполнен |
| Entities | `87a9e860758f1c2df0b37e7730f597e18e44da44` | `main`, push выполнен |
| DALC | `4491634cb87272b24ac5b3e90e9fc43e1648963d` | `main`, push выполнен |
| DBSelect | `9b8551dab6228ebcaff7506110c71fddff5b6f03` | `main`, push выполнен |
| Controls | `ea980be926ae975678314029ba5fc8aa68c675ba` | `main`, push выполнен |
| Tests | `0814c1f94b23b361df720746215dff94713fef65` | `main`, push выполнен |

Detached HEAD не было. В EquipmentForm изменён только обратный переход к Net; посторонние
untracked-файлы в коммиты не включены.

## Матрица legacy → новая реализация

| Область | Legacy | Clayzor |
|---|---|---|
| Маршрут и существующий порт | `Inventory/Net/Net.aspx`; `Net.aspx.cs:40-79` | `Components/Pages/Net/Net.razor:1`; `Net.razor.cs:155`; `LocalNetworkPort.cs:64` |
| Заголовок и имя `NetworkName_Port` | `Net.aspx.cs:124-179` | `Net.razor:38`; `LocalNetworkPort.cs:77` |
| Розетка и Notes | `Net.aspx:52-59`; `Net.aspx.cs:82-93,192-222` | `Net.razor:42`; `Net.razor.cs:238`; `NetSocketSelectDataSource.cs`; `SQLQueries.cs:2329` |
| Подключено, 4 строки + ссылки | `Net.aspx:63-74` | `Net.razor:58-72` |
| Примечание, 4 строки + ссылки | `Net.aspx:78-89` | `Net.razor:74-88` |
| Сотрудник, работающие, организация | `Net.aspx.cs:116,224-232` | `Net.razor:96`; `Net.razor.cs:253`; `Employee.cs` |
| Заказчик, обязательный | `Net.aspx.cs:295-325,650-675` | `Net.razor:110`; `LocalNetworkPort.cs:114`; `PhoneCustomerSelectDataSource.cs` |
| Свободные соседние порты | `Net.aspx.cs:259-275` | `Net.razor:122`; `LocalNetworkPort.cs:89`; `SQLQueries.cs:2305` |
| Все соседние порты | `Net.aspx.cs:277-293` | `Net.razor:134`; `SQLQueries.cs:2313` |
| Взаимоисключающие режимы | `Net.aspx.cs:238-248` | `Net.razor.cs:276-288` |
| Checkbox и календарный срок | `Net.aspx.cs:250-257,327-341` | `Net.razor:144`; `Net.razor.cs:290` |
| OK / Применить / Отмена | `Net.aspx.cs:352-369,625-712` | `Net.razor:13-21`; `Net.razor.cs:297-370,414-448` |
| Changed | `Net.aspx.cs:171-179` | `Net.razor:158-164`; `ClayChanged` |
| PoE, подтверждение, timeout 120 | `Net.aspx.cs:371-390,402-454` | `Net.razor.cs:372-406`; `LocalNetworkPort.cs:168`; `SQLQueries.cs:2479` |
| Родительское обновление | `Net.aspx.cs:692-717` | `Net.razor.cs:337` через существующий `clayModelEquipmentForm.refreshOpener` |
| Конфигурация | `Config.net_form`, `Config.equipment_form` | `Program.cs`: `URI_net_form`, `URI_equipment_form`; `web.config` |

## Поиск оборудования

Цепочка: `Net.Connected`/`Net.Description` → `NetEquipmentLinkExtractor.Extract`
(`NetEquipmentLinkExtractor.cs:19`) → первый regex → точечный `StartsWith("ilo-")` и
`Replace` → case-sensitive `List.Contains` → второй MAC regex →
`NetEquipmentLinkResolver.ResolveAsync` (`NetEquipmentLinkResolver.cs:39`) →
параметризованные пачки ≤500 токенов → `SQLQueries.SELECT_Оборудование_СсылкиПоТокенам`
(`SQLQueries.cs:2266`) → пять предикатов: точное имя, доменный суффикс, MAC1, MAC2, ILO →
положительный EquipmentId → `Net.razor.cs:408` → `ClayFormWindowService`/`clayOpen`.

`TOP 1` намеренно оставлен без `ORDER BY`. Исходные тексты не модифицируются и не
заменяются ссылками. Resolver вызывается при первичной загрузке и после успешного Apply,
не на каждый input/render. Ошибки SQL и regex timeout не удаляют текст. SQL-чтение через
`QueryRequiredAsync` поддерживает cancellation и не подменяет connectivity пустым ответом.

### R01–R20

| Проверка | Результат |
|---|---|
| R01 пустые/null/whitespace | PASS, extractor theory |
| R02 точное имя | PASS, extractor + bUnit + реальный ID 8 |
| R03 дефис/подчёркивание/FQDN | PASS, golden extractor |
| R04 `ilo-` | PASS, golden + resolver; подпись и поиск используют преобразованный токен |
| R05 `ILO-` и повторный `ilo-` | PASS, golden |
| R06 `DOMAIN\HOST` | PASS, golden + реальный доменный суффикс ID 8 |
| R07 MAC-варианты | PASS, включая фрагменты `:` и цельный дефисный MAC; реальный разделённый MAC без нормализации не совпадает |
| R08 MAC2/ILO | MAC2 PASS реальный ID 7; ILO SQL PASS в тесте, реальная запись BLOCKED триггером модели |
| R09 IP | PASS, ровно кандидат `20` для `192.168.1.20` |
| R10 HTML/кириллица/спецсимволы | PASS, golden + безопасный Blazor text rendering, без MarkupString |
| R11 регистр/порядок | PASS, case-sensitive extractor; SQL collation не используется для изменения порядка |
| R12 один токен в двух полях | PASS, bUnit + browser: ссылки под обоими полями |
| R13 порядок двух проходов/строки | PASS, golden |
| R14 несуществующий рядом с существующим | PASS, bUnit + browser |
| R15 `_` в LIKE | PASS как документированный legacy-эффект: `name_x` реально нашёл `DOMAIN\nameAx`; не исправлялось |
| R16 коллизии | TOP 1/no ORDER BY покрыто тестом; реальная коллизия BLOCKED триггером уникального сетевого имени |
| R17 длинный текст/N+1 | PASS: 2000 токенов; resolver ограничен пачками 500, сценарий 501 = 2 SQL-запроса |
| R18 draft/Apply/ошибка | PASS: bUnit Apply + ошибка сохраняет draft; реальный browser Apply обновил ссылку и БД |
| R19 cancellation/dispose/race | PASS: resolver cancellation + поздний ответ после dispose не применяется |
| R20 SQL injection/разделители/длина | PASS: параметры, реальный `x' OR 1=1 --` без результата и без записи |

## Схема, права и транзакции

- `vwЛокальнаяСеть` — базовая `ЛокальнаяСеть` с фильтром: роль 102 ИЛИ наличие
  оборудования во `vwКодыОборудования`.
- `tЛокальнаяСеть_InsUpd` повторно проверяет видимость записи, уникальность розетки,
  совпадение офиса и заполняет `Изменил` по `SUSER_SID()`/`Изменено=GETUTCDATE()`.
- Новая форма не выдумывает роль редактора. `Employee.GetCurrentAsync` требует совпадение
  Windows principal с `SUSER_SNAME()` и сотрудника по `SUSER_SID()`. При записи/PoE
  идентичность запрашивается заново. Актуальная запись и права повторно проходят через view
  и реальный триггер.
- Обычная запись, switch и exchange — отдельные batch с `BEGIN TRANSACTION`,
  `UPDLOCK,HOLDLOCK`, проверкой `Изменено` источника, заказчика и цели, CATCH/ROLLBACK.
  Session-wide SERIALIZABLE/XACT_ABORT не устанавливаются.
- Switch очищает Connected/Description/Employee/Socket источника и переносит draft в цель.
- Exchange использует три UPDATE, чтобы обойти уникальность розетки без отключения триггера.
- `СрокВозврата` при switch/exchange намеренно не меняется, как в legacy.
- Наследованная ветка `else if (switch != -1 && exchange != -1)` для просроченной даты
  недостижима при штатном UI. Новый UI показывает «просрочено», но не добавляет жёсткий
  запрет, которого нет в штатном legacy.
- Legacy `Location.Value=net.Id` не копируется. Новый источник при наличии офиса использует
  его поддерево; при отсутствии офиса не подставляет ID порта в ID расположения.
- PoE: кнопка только при фактически загруженном `PoE=1`, свежая идентичность, серверная
  проверка записи, timeout 120, overlay в finally, повторная команда заблокирована.
  `sp_ResetPoEport` на стенде отсутствует; реальный reset не вызывался.

## Реальная приёмка

На тестовом оборудовании 8 созданы порты ID 1/2/3 (250/251/252), затем удалены.

Browser:

- `/Net/Net?id=1`: заголовок «Локальная сеть», подпись `clay-test-net_250`, ошибок нет;
- Connected `clay-test-net` → Equipment 8, Description `a0b1c2d3e4f5` → Equipment 7;
- ссылка Description открыла `/Equipment/EquipmentForm?id=7` через `clayOpen`, Net осталась;
- Apply изменил Connected на `001122334455`, обновил ссылку, БД содержит тот же текст,
  `Изменил=4`;
- viewport 360×800: `innerWidth=360`, `scrollWidth=360`, горизонтального переполнения нет.

SQL Server 2022 (`16.0.1220.3`):

```text
resolver|exact=8|mac1=7|mac2=7|mac_separator_no_match=0|injection=0
resolver|domain_suffix=8
resolver|mac_ilo=BLOCKED_BY_EQUIPMENT_MODEL_TRIGGER
resolver|underscore_wildcard_matches_nameAx=8
resolver|collision=BLOCKED_BY_UNIQUE_NETWORK_NAME_TRIGGER
concurrency|stale_status=1|external_text_preserved=1
exchange|status=0|field_map=pass|return_dates=unchanged
rollback|before_update=2|unchanged=true
rollback|before_update=3|unchanged=true
switch|status=0|field_map=pass|return_dates=unchanged
switch|occupied_status=3|unchanged=true
```

Rollback внесён контролируемым `RAISERROR` перед вторым и третьим UPDATE копии реальной
константы; состояние обеих записей JSON-совпало до/после. Отдельный stress-test потоков
не проводился. Все временные порты удалены, исходные поля оборудования 7/8 восстановлены,
`disabled_triggers=0`, `remaining_test_ports=0`. Временные SQL-файлы удалены.

### Повторная приёмка с данными грида 1011

Для полноценной проверки созданы четыре порта оборудования 8 (ID 4–7, порты 10–13),
две розетки `CTN_A`/`CTN_B` и изолированная ветка «офис → комната». Отдельная ветка
потребовалась потому, что исходные тестовые расположения не имеют офиса и реальный
`tЛокальнаяСеть_InsUpd` корректно отклонял розетку сообщением «Розетка расположена
в другом офисе». Тестовое наполнение справочников выполнено управляемыми SQL-сценариями
по AGENTS.md с гарантированным повторным включением триггеров; штатные INSERT/UPDATE/DELETE
портов выполнялись с активными триггерами.

У грида 1011 `ФормаРедактирования=@URI_net_form`, а `ФормаНового` и `SQLDelete` пустые.
Поэтому создание/удаление проверено SQL через `vwЛокальнаяСеть`, а не выдуманными кнопками
в UI. Редактирование открыто карандашом из грида в отдельном окне.

Browser PASS:

- грид 1011 показал четыре созданные записи; popup `/Net/Net?id=4` открылся из строки;
- независимые ссылки Connected/Description, выбор `CTN_A` и его Notes;
- Apply сохранил розетку/тексты и адресно обновил родительский грид без его перезагрузки;
- switch перенёс Connected/Description/Employee/Socket с порта 10 на свободный порт 11,
  очистил источник и обновил грид;
- exchange между портами 10 и 12 обменял Connected/Description/Employee, при этом сроки
  возврата остались у своих записей (31.12.2026 и 15.01.2027 соответственно);
- создание порта 14 через активный триггер: грид показал 5 строк; удаление портов 13 и 14
  через `vwЛокальнаяСеть`: грид показал 3 строки;
- 360×800: `innerWidth=360`, `scrollWidth=360`; `pageerror` и HTTP-ошибок в основном
  сценарии не было.

После первого цикла проверок тестовые записи были удалены, но затем по требованию
пользователя полный набор восстановлен для независимого ревью и **оставлен в БД**.
Текущие данные: порты ID 9–13 и 15 (номера 10–15), розетки ID 6/7 (`CTN_A`/`CTN_B`),
офис 9010, комната 9011; у оборудования 8 добавлена текущая запись расположения 9011.
Грид 1011 показывает 6 строк. Исходная запись расположения 1005 и сетевые поля
оборудования 7/8 сохранены. Все триггеры `ЛокальнаяСеть`, `Розетки`, `Расположения`,
`ОборудованиеРасположение` включены. Эти данные не удалять до отдельной команды пользователя.
ID 15 отсутствовал после восстановления (`IDENT_CURRENT=13`) и был создан заново
с маркером `CLAY_TEST_NET review 15`; это не восстановление неизвестных исходных полей.
Временные browser-сценарии удалены.

### Исправления Changed, формата даты и ширины

- `Net.razor`: строковый параметр `ChangedByName` получает `@_model?.ChangedByName`;
  раньше без `@` в подвал попадал литерал Razor-привязки вместо фамилии.
- `ClayDateTimeConverter.Format`: legacy-формат `dd.mm.yy hh:mi`, используемый 12 гридами,
  преобразуется в .NET `dd.MM.yy HH:mm`. Буква `i` была литералом формата, а не частью datetime.
- `Net.razor.cs`: `ClayEntityFormOptions.MaxWidth="100%"`, как у EquipmentForm и Tel;
  раньше общий дефолт ограничивал форму 760 px.
- `ClayFormWindowOptions.Width=1100`: стандартный popup больше не открывается шириной
  760 px; значение соответствует уже существующему дефолту JS `clayOpen`. Пользовательские
  размеры по-прежнему сохраняются и имеют приоритет.
- Регрессии: `NetFormTests.ChangedFooter_UsesLoadedEmployeeName`,
  `NetFormTests.EntityForm_UsesFullAvailableWidth`,
  `ClayGridDynamicFormNavigationTests.WindowService_DefaultWidthMatchesClayOpenDefault` и
  `SpecialColumnTypesTests.Format_LegacyDateTimeFormat_UsesMonthAndMinutes`.
- Build успешен, 0 ошибок; общий набор 915/915
  (прежний `ClayTreeMutationAbortTests` исключён).
- Browser PASS: грид 1011 показывает `22.11.26 07:43` без `i`; подвал формы ID 9 —
  `Изменил: Тестов Администратор 22.11.2026 10:43:34`. ID 15 открывается из грида:
  popup 1100 px, форма внутри 1084 px; при viewport 1400 px форма и подвал — 1384 px.
  Шесть тестовых записей сохранены.

### Обратный переход EquipmentForm → Net и компоновка

- Legacy `Equipment_form.aspx.cs:2410-2471` (`DisplayNet`) ищет сетевое имя в
  `vwЛокальнаяСеть.Подключено` через `SELECT_NAME_vwЛокальнаяСеть`, удаляет доменный
  префикс и выводит `СетевоеИмя_Порт`. Это отдельный read-only сценарий, не attach/detach.
- В `EquipmentForm` под «Сетевое имя» добавлены такие же ссылки на `URI_net_form` через
  `clayOpen` с ключом `net-form`. Read-модель `EquipmentNetworkPortLink` включена в пакетное
  `GetFormDataAsync`, поэтому отдельного SQL round trip нет. Dapper-регистрация покрыта
  `EquipmentNetworkPersistenceTests.NetworkPortLink_MapsLegacyColumns`.
- На реальных данных Equipment 8 отображается `clay-test-net_12` → `/Net/Net?id=11`;
  SQL возвращает ровно эту запись. Browser подтвердил открытие Net в popup без закрытия
  EquipmentForm и корректную подпись всех полей.
- Правая колонка Net использует существующий `.equipment-ins-fields` (gap 6 px):
  «Сотрудник» и «Заказчик» больше не склеены. Новых локальных CSS нет.
- `NetFormTests.EntityForm_UsesFullAvailableWidth` дополнительно закрепляет общий gap;
  `EquipmentFormTests.NetworkPortLink_OpensLegacyNetForm` — URL и `clayOpen`.
- Build успешен, 0 ошибок; профильные 19/19, общий набор 917/917
  (прежний `ClayTreeMutationAbortTests` исключён). Browser PASS: gap 6 px, грид 1011
  содержит прежние 6 тестовых записей, ошибок страницы нет.
- После проверок выполнен `dotnet build-server shutdown`; в Windows остались ровно три
  `dotnet.exe`: Styles, Inventory и Users. Данные ревью не удалялись.

## Build и тесты

- `dotnet build Clayzor.sln --no-restore ...` — успешно, 0 ошибок, 35 прежних предупреждений.
- `dotnet test ... --filter 'FullyQualifiedName~Net|FullyQualifiedName~Network|FullyQualifiedName~Socket'`
  — **98/98 passed**.
- `dotnet test ... --no-build --filter 'FullyQualifiedName!~ClayTreeMutationAbortTests'`
  — **911/911 passed**.
- Известный несвязанный `ClayTreeMutationAbortTests` исключён из-за ранее подтверждённого
  зависания; его код не менялся.

## Изменённые файлы

Inventory:

- `Components/Pages/Net/Net.razor` (новый)
- `Components/Pages/Net/Net.razor.cs` (новый)
- `Program.cs`
- `web.config`

Entities:

- `Inventory/LocalNetworkPort.cs` (новый)
- `Inventory/NetEquipmentLinkExtractor.cs` (новый)
- `Inventory/NetEquipmentLinkResolver.cs` (новый)
- `Inventory/NetSocketLookup.cs` (новый)
- `Inventory/ColumnNames.cs`
- `Inventory/Employee.cs`
- `MedicalTests/MedicalTest.cs`
- `SQLQueries.cs`

DALC:

- `DbManager.cs`: `QueryRequiredAsync` с cancellation/fail-closed read и optional timeout
  для существующего `ExecuteAsync`.

DBSelect:

- `Locations/NetSocketSelectDataSource.cs` (новый)

Tests:

- `NetEquipmentLinkExtractorTests.cs` (новый)
- `NetEquipmentLinkResolverTests.cs` (новый)
- `LocalNetworkPortTests.cs` (новый)
- `NetSocketSelectDataSourceTests.cs` (новый)
- `NetFormTests.cs` (новый)
- `ClayGridTestDbFakes.cs`

Суперпроект: `promts/_handoff/NET_1.md`.

`git diff --check` успешен во всех затронутых репозиториях. Inventory, Users и Styles
перезапущены после проверок.

## Открытые ограничения / решения

- Реальный SQL Server 2008 R2 недоступен; использован совместимый синтаксис на 2022.
- Реальный PoE не проверен, процедура на стенде отсутствует; внешние вызовы не выполнялись.
- Реальный ILO не создан из-за правил модели; реальная коллизия запрещена триггером.
- Политика TOP 1, wildcard `_`, фрагменты MAC/IP и недостижимая legacy-валидация даты
  сохранены/документированы, молчаливых «улучшений» нет.
