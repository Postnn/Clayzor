# EquipmentForm — полная матрица Legacy → Clayzor

Дата: **23 ноября 2026 года**. Матрица охватывает всю карточку и дочерние редакторы, включая код вне diff EQ7. Она дополняет `EquipmentForm_7_Review.md`; конкретный план исправления и приёмки — `EquipmentForm_8.md`.

## Как читать статусы

- **Сохранено** — соответствующий путь найден и сопоставлен по исходникам; это не заявление о собственном запуске SQL/browser.
- **Улучшено** — полезное современное отличие, которое нужно сохранить.
- **Дефект / пропуск** — установленная ошибка или утраченный действующий сценарий; указан блок EQ8.
- **Обоснованное отличие** — другой корректный способ выполнить задачу либо отсутствие уже отключённой Legacy-ветви.
- **Beta / решение / зависимость** — отдельно от реализации: реальное окружение, предметное решение или ещё отсутствующий общий функционал.

Внешние проверки по указанию владельца оставлены на beta. Локальные ошибки не превращаются от этого в необязательные. Тесты в источниках прочитаны; собственный .NET/SQL/browser прогон в этом ревью не выполнялся.

## Зафиксированные источники

| Обозначение | Исходник |
|---|---|
| L, Legacy | [KescoDev/Kesco.App.Web.V4.Inventory][legacy] `12a6efae7eb310adec06a57f6d80b8d9e76985b4`: Inventory/Equipment и встроенные Kesco.Lib.Entities/Controls/DBSelect |
| I, Main, Markup, Service | [Postnn/Kesco.App.Web.Inventory][inventory] `d47e2ff584932ab88a4602f6ed98ee9ed9208ed4`: EquipmentForm.razor[.cs], Details, EquipmentFormService |
| E, SQL | [Entities][entities] `df7a8d08d194ca2b1af691458066a1e17d725664`: Inventory и SQLQueries.cs |
| T | [Tests][tests] `eb1e1230a1df80c73b7ab81c9580fff6342a1738` |
| DBSelect | [DBSelect][dbselect] `9b8551dab6228ebcaff7506110c71fddff5b6f03` |
| C, Controls | [Controls][controls] `ea980be926ae975678314029ba5fc8aa68c675ba` |
| STYLES | [KescoDev/Styles][styles] `fddc5a2157cf906402c50731e84cfa537abd7dba` |

Номера строк и имена методов далее относятся к этим версиям. `Main` — новая EquipmentForm.razor.cs; `Legacy form` — Equipment_form.aspx.cs. На страницах репозиториев доступны закреплённые исходники; основные прямые ссылки приведены в конце файла.

## A. Основная карточка, модель и жизненный цикл

Источники: [Legacy code-behind][legacy-form], [разметка][legacy-markup], [новая форма][form], [новая разметка][markup], [Equipment][equipment], [Service][service].

| ID | Сценарий | Современная реализация | Вывод / доработка |
|---|---|---|---|
| M01 | Открытие существующего ID; отсутствие/недоступность | GetAsync, _notFound, _loadError, RetryLoad | Сохранено; no-row отличается от ошибки основного чтения, guard ID/version/dispose есть |
| M02 | Создание оборудования | Отдельный EquipmentIns; EquipmentForm без ID не создаёт объект | Соответствует отдельному insert-сценарию Legacy; не добавлять скрытый INSERT в редактор |
| M03 | Все основные поля записи | SN, модель, MAC1/2/ILO, network name, camera, writeoff/doc, owner/tenant/shipper, pool, receipt doc, notes | 15 колонок UPDATE сохранены; общий optimistic guard отсутствует — EQ8-06 |
| M04 | Тип → фильтр моделей, сортировка типов | EquipmentTypeSource.SortByName; ModelSource.Filter.EquipmentTypeId | Сохранено; поиск/фильтр не должен очищать бизнес-значение из-за ошибки источника |
| M05 | Модель → соответствующий тип | OnModelChangedAsync, async version guard | UI сохранён/усилен; server не сверяет пару ModelId/TypeId — EQ8-13 |
| M06 | Очистить тип/модель; Escape поиска | OnTypeIdChanged, Cleared, SelectedItemChanged и backup ClaySelect | Подтверждённое изменение отделено от поиска; сохранять корректный общий контракт ClaySelect |
| M07 | Обязательность SN | HasSelectedModel + IsSerialNumberRequired; server validation | Сохранено; server должен получать тип реальной модели — EQ8-13 |
| M08 | Видимость сетевого имени и MAC | HasNetworkName, min(3, Model.NetworkInterfaces) | Соответствует активному Legacy; MAC/имя не сделаны обязательными по закомментированным правилам |
| M09 | Формат MAC | Текущий whitelist стандартных разделителей и hex; UI normalization | Улучшено относительно strip-anything; direct service параметры не нормализованы — EQ8-13. R04 отдельно |
| M10 | Скрытые значения при смене типа | Сохраняются; persisted секции доступны read-only | Полезная защита данных, не обнулять ради вида Legacy |
| M11 | Метаданные модели | PN, multimedia, monitor size, CPU, Net/name, memory type/banks, driver, lifetime, description | Сохранены; модель открывается по настроенному URI |
| M12 | Владелец и арендатор | Owner editable, Tenant read-only; связанные права пересчитываются | UI сохранён; actual owner/server guards и recoverable errors — EQ8-02/04/13 |
| M13 | Документ поступления → поставщик | Единственное лицо кроме owner; version/owner/document guard | Сохранено и защищено от позднего ответа после ручной смены поставщика |
| M14 | Списание: дата/время/документ | Правило doc→date и date→doc кроме SIM; отдельный DS_doc/тип2365 | Сохранено; server type должен быть достоверным; beta проверка document/trigger |
| M15 | Акты списания с оборудованием | Banner со списком названий | Данные есть, кликабельные документы заменены текстом — EQ8-12 при подтверждённом URI |
| M16 | Main UTC/local и секунды | Browser offset по instant, localDateTimeToUtcMilliseconds | Сохранено; не переносить server-as-client ошибки дочерних форм |
| M17 | Сохранение и закрытие / применить | SaveAsync, validation, _saving, refreshOpener, Close | Есть; ошибку write и refresh различать — EQ8-05 |
| M18 | Два редактора одной карточки | Полный UPDATE WHERE Id, без исходной версии | Дефект устойчивости, унаследованный от Legacy — EQ8-06 |
| M19 | Копирование | Отдельное окно EquipmentIns?copyId | Сохранено; родительский draft не сохраняется скрыто и не уничтожается |
| M20 | Отмена / обновление / навигация | NavigationLock, ConfirmAsync, version/equipment/dispose guards | Улучшено; explicit discard отличается от сбоя. DR-01 этим не выполнен |
| M21 | Child refresh и dirty main fields | Частичный refresh по baseline вместо unconditional reload | Полезно, сохранять при внедрении concurrency/version merge |
| M22 | Перезапуск IIS / сеть / app update | Draft и pending преимущественно в circuit | Требуется общий DR-01, EQ8-14; внешние fault tests beta, механизм обязательный |
| M23 | Поле должности из старого markup | Нет активной доказанной привязки/сохранения в Legacy SetBinding/Handlers | Не переносить мёртвое поле без подтверждённого сценария |

## B. Телефоны, аккаунты, команды и права

Источники: [Legacy form][legacy-form], [новый Service][service], [PhoneNumberSelectDataSource][phone-source], [PhoneNumberQuery][phone-query], [phone tests][phone-tests].

| ID | Legacy сценарий / источник | Современный путь | Статус и практическое замечание |
|---|---|---|---|
| T01 | Телефонные поля по характеристике типа и выбранной модели; form DisplayAndRequiredTypeFields | ShowPhones/HasSelectedModel/CanEditPhones (`:170–197`) | Сохранено; существующие persisted phones остаются видны после временной смены типа, редактирование отключается — полезное улучшение |
| T02 | Камера и пул только IP-phone, markup/тип30 | ShowCamera/ShowPhonePool | Сохранено; есть CameraAndPhonePool_RequireIpPhoneType |
| T03 | Поиск телефона по location, employee, type, main phone, equipment; form685–711 / DSOEquipmentPhone | UpdatePhoneSourceFilters + PhoneNumberSelectDataSource + `fn_НомераДляТелАппаратов` | Контекст сохранён, но exclusions ломают запрос — EQ8-07A |
| T04 | Исключить уже выбранные номера, запрет дублей; form1030–1033 | UI duplicate guard, ExcludedPhoneIds, builder проверяет duplicate PhoneId | Двойное добавление защищено; реальная SQL выдача с exclusions дефектна |
| T05 | Аналоговый номер получает допустимое connection equipment; form1035–1045 | GetConnectionEquipmentIdAsync, SQL с phone-type200/equipment-type29/location/employee | Логика перенесена; NULL допустим при отсутствии результата, ошибка не должна трактоваться как NULL; EQ8-04 |
| T06 | Добавление нового номера в draft | `_phones.Add`, без немедленного DML | Сохранено; default/description/connection удерживаются, автор/changed отображаются после reload |
| T07 | Удаление связи, не номера; form1018–1026 | подтверждение → `_phones.Remove` → diff DELETE по EquipmentId+PhoneId | Сохранено; номер справочника не удаляется |
| T08 | Срок возврата и примечание; form2519–2537 | EquipmentPhoneDialog → staged fields | Сохранено; ShowDialogAsync защищает от применения к другой карточке |
| T09 | Новому номеру задать срок/примечание до первого save | EquipmentPhoneSaveBuilder INSERT содержит ReturnDate/Notes | Улучшено: legacy INSERT не передавал эти поля нового item |
| T10 | Основной номер, связь с оборудованием, изменил/изменено | fn_НомераТелАппаратов mapping + formatted label/tooltip/link | Сохранено, тест GetPhones_MapsAllColumns покрывает поля; public UI не даёт произвольно редактировать IsDefault/AutoDial |
| T11 | Условные колонки connection/date; form993–1005 | Razor257–279, PhoneGridKey | Сохранено; readonly ссылки оставлены как полезная навигация, это не право записи |
| T12 | Просроченный срок возврата | grid RowClass + displayed date | Есть; timezone/date semantics дополнительно рассматривает раздел EQ8-08 |
| T13 | Гостиничный аппарат: предупреждение о >1 номере; form606–615/Equipment1604–1609 | GetHotelPhoneCountAsync + ShowHotelPhoneWarning | Сохранён SQL count и теперь видимое предупреждение; счётчик не реагирует на staged list до сохранения, это ограничение обоих подходов, не новая эскалация |
| T14 | Equipment и phone diff сохраняются атомарно | EquipmentPhoneSaveBuilder: один batch, транзакция, XACT_ABORT, DELETE/UPDATE/INSERT | Улучшено число round trips; основная запись и diff совместны; affected main row проверяется; concurrency всей карточки — отдельный общий обзор |
| T15 | Изменение только ReturnDate/Notes | SQL UPDATE metadata; planner не вызывает ATS | Обоснованное улучшение, подтверждено IgnoredMetadataOnly test; не возвращать лишние ATS calls ради буквального legacy |
| T16 | Added/deleted phones, ATS получатели | planner по affected diff + connectionIds/fallback current equipment | Сохранены Xorcom/CCM ветви; выбор только затронутых телефонов намеренно уже legacy all-current |
| T17 | Xorcom имеет приоритет над CCM; form2841–2865 | hasXorcom подавляет CCM | Сохранено, есть mixed-branch test; не менять без бизнес-основания |
| T18 | CCM вызывается по connection equipment, current equipment только fallback | planner592–621 | Сохранено, tests проверяют old/new connection recipients и fallback |
| T19 | Удалённый Xorcom phone | PhoneSyncOperation.Deleting и optional SQL parameter capability | Современный протокол явно сохраняет deletion flag; физические последствия/идемпотентность — beta |
| T20 | Ошибка части ATS операций | pending хранит failed operations; retry без повторного UPDATE | Улучшено; тесты нажимают настоящую Apply кнопку, а не только private handler |
| T21 | Повтор pending после отзыва роли/смены target | Current executor доверяет переданным operations | Дефект авторизации/актуальности из EQ8-03A; исправлять на executor boundary |
| T22 | SQL↔ATS transaction policy | SQL commit перед ATS; pending только circuit | Осознанное текущее отклонение R01; нельзя молча вернуть legacy TransactionScope, который сам не гарантировал внешнего rollback |
| T23 | Настроить сохранённый IP-phone по явному действию | Отсутствует, no-dirty Save no-op | EQ8-03B; независим от metadata-only/no-repeat-UPDATE оптимизаций |
| T24 | Посмотреть config при dirty/MAC/pool/template conditions | OpenPhoneConfigAsync1800–1821 | Проверки сохранены; role102 отдельно от edit43; тест PhoneConfigAccess_UsesRole102InsteadOfEditRole |
| T25 | Прямой HTTP config handler | MapGet → GetPhoneConfigAsync | Известная новая HTTP provider регрессия EQ8-01, не считать новым повторным finding |
| T26 | Скрытый по умолчанию раздел аккаунтов только MAC equipment/global102 | ShowAccounts + toggle + batch IncludeAccounts | Сохранено ленивое раскрытие UI; global scope ослаблен до ANY — EQ8-02 |
| T27 | Общий список аккаунтов без раскрытия секретов | SELECT без Пароль, grid ***** | Улучшено; пароль не попадает в batch, tests проверяют SQL |
| T28 | Отдельное показать/скрыть пароль | Отсутствует | EQ8-12; не компенсировать выдачей edit role |
| T29 | Добавить account/login/password/limited/notes | EquipmentAccountDialog + AddAccountAsync | Сценарий сохранён; server role102+43; username required/length в UI |
| T30 | Обновить account, исходный login неизменяем | Edit dialog OriginalLogin, readonly login, composite key EquipmentId+Login | Сохранено; пароль читается отдельно; SQL error/missing не превращается в blank overwrite |
| T31 | Удалить account с подтверждением | DeleteAccountAsync, composite key | Сохранено; SQL исключения показываются; свежие роли есть, global scope требует поправки |
| T32 | Save child editor failure оставляет данные доступными | Dialog.Close до server save, page только показывает ошибку | Потеря draft child editor при ошибке — передано разделах EQ8-04/05/06; требует общего решения, не копирования legacy popup |
| T33 | Операция сети: computer + global103 | UI/server actual type + ANY103 | Actual type reload лучше UI-only; global mismatch остаётся EQ8-02 |
| T34 | Быстрое UPS подключение: IT/non-SIM + role43 owner/global | EnsureUpsAccessAsync читает actual target/type/owner | В целом сохранено; null owner semantics — контрактный долг, не доказанная эксплуатация |
| T35 | Детальный UPS editor: owner scopes каждого изменённого server | SQL builder проверяет source + changed targets, transaction | Сохранено/усилено; service не проверяет trusted Windows↔SQL identity, дополнительный instance A2 |
| T36 | SIM/accounting/license роли независимы от общего edit43 | Раздельные UI flags/service helpers, бухгалтерия остаётся readonly | В целом сохранено; SIM detail/delete accounting trusted identity gaps остаются |
| T37 | Copy/open target/model/phone/person | Configured links и новый route copyId | Сохранено; открывается сохранённая запись, не скрыто сохраняется dirty draft |
| T38 | Cancel/navigation/refresh с dirty или pending | NavigationLock + ConfirmAsync, refresh сохраняет pending | Улучшены сообщения о SQL committed/pending; не заменяет DR-01 durable draft |
| T39 | Запрет смены страницы во время save и устаревших callbacks | `_saving`, operationVersion/equipmentId guards | Полезная современная защита; реальные command race findings разделы EQ8-04/05/06 не дублируются |

## C. Расположения, ответственные, бухгалтерия, компьютер, ПО, лицензии и UPS

Источники: [Legacy Equipment и дочерние формы][legacy], [новые Details][details], [Main][form], [Entities/SQL][sql]. Метки EQ8 раскрывают точные исправления, а статус beta не означает отсутствующий код.

| ID | Раздел/сценарий | Legacy | Clayzor | Вывод |
|---|---|---|---|---|
| H01 | Расположение: история, путь, примечания, изменивший, время | Main legacy1682–1731 | SQL668+, Markup578–594 | Сохранено чтение и основной состав. Человек/расположение стали plain text, см. EQ8-12. |
| H02 | Новое расположение: location+дата+время обязательны | LocationEquipment507–540 | LocationDialog118–123 | Сохранено. Legacy подставлял now; new пустые поля — допустимое UX отличие, но нужно единое local-now, если восстанавливать default. |
| H03 | Новое расположение не должно совпадать с текущим | Legacy531–539 повторно читал current | Main868–872 сравнивает loaded history | Проверка есть, но только stale UI snapshot. Атомарный no-newer guard SQL691+ проверяет дату, не равенство location. Риск конкурентного состояния; не выдавать за DB обход без trigger evidence. |
| H04 | Возврат location: nullable, не раньше начала, ≤3months | Legacy542–570 | LocationDialog135–138 | Сохранено правило, ошибочен timezone EQ8-08. |
| H05 | Edit location не меняет place/from, только возврат/notes | Legacy288–300 | LocationDialog106–111, Main896–898, SQL725+ | Сохранено ограничение, но точность ключа EQ8-06. |
| H06 | Перенос на location55 требует ответственного только при отсутствии действующего | Legacy619–640,672–688 | Main858,881–906, LocationDialog102/124 | Сохранено; есть повторный required read после dialog. При stale load, появившемся/исчезнувшем ответственном нужен понятный conflict+retained draft. |
| H07 | Перенос с location55 не должен автоматически изымать оборудование | Legacy590–617 явно закомментировал автоматическое изъятие | New не закрывает responsible | Совпадение с активным legacy; НЕ восстанавливать закомментированную логику. |
| H08 | Расположение + новый responsible атомарны | Legacy481–502 TransactionScope | LocationSaveBuilder transaction | Сохранено/улучшена явность SQL. Порядок изменён: legacy responsible→location, new location→responsible. Реальный trigger-contract проверить на beta; не утверждать дефект без evidence. |
| H09 | Перенос телефонной станции переносит связанные SIM locations | Legacy LocationEquipment.Save использует специализированный SQL | SQL691–721 CTE по type28/name и текущим type27 | Код перенесён, параметризован; реальные linkedSIM edge cases — beta. |
| H10 | Responsible history включая закрытые периоды и возвраты | Legacy1770–1822 | SQL737+, Markup629–657 | Сохранено. Latest edit и withdraw активной строки реализованы отдельно. |
| H11 | Выдача: сотрудник, from date/time обязательны | Legacy619–641 | ResponsibleDialog142–146 | Сохранено; Service только role+write, прочая domain validation пока UI/DB. |
| H12 | Изъятие: to date/time, опциональный следующий responsible с from | Legacy642–665 | ResponsibleDialog147–155, Main1022–1040 | Сохранено составное действие; accuracy key EQ8-06 и timezone EQ8-08. |
| H13 | Следующий ответственный не равен действующему | Legacy667–678 explicit current query/check | New dialog не получает CurrentEmployeeId; Main/Service не проверяют | Application check потерян. Реальный trigger outcome неизвестен. Нужна server validation или явно документированный современный контракт; тест same employee не выполняет составной DML. |
| H14 | Изъятие с «На руках» требует другого location≠55 | Legacy753–768,825–847 | Main969, ResponsibleDialog157–160, builder | Сохранено. |
| H15 | Возврат следующему responsible не раньше ЕГО выдачи | Legacy683–690 сравнивает efDateFrom | ResponsibleDialog170 uses `to ?? from` в Withdraw | Пропуск/ошибка prevalidation: withdrawal20го,next-start22го,return21го проходит UI. Исправить periodStart для next; service validation до DML; DB trigger beta. |
| H16 | Edit responsible меняет только возврат/notes | Legacy578–586 | Main1015–1018 clone+twochanges | UI сохраняет исходную identity, но SQL ещё пишет stale ToUtc; версия отсутствует. Конкурентный close может быть затёрт edit. |
| H17 | Изъятие+location+next atomic | Legacy702–730 scope | ResponsibleSaveBuilder transaction | Сохранено атомарное обрамление, но обязательная проверка закрытия отсутствует EQ8-06. |
| H18 | Автоматическая смена договора SIM при responsible/location | Legacy calls PreSaveDataSim закомментированы | New отсутствует | Обоснованно не переносить мёртвую логику. |
| H19 | Бухгалтерская история и edit любой строки | Legacy1922–1980 | Markup687–716, SQL1295+ | Сохранено. Read visibility при отсутствии edit rights улучшено; tests это покрывают. |
| H20 | Accounting owner/dateFrom/инвномер ОС обязательны | BuxMove351–368 | EquipmentAccounting.Validate82–95 | Сохранено; new добавляет порядок дат и digit-инвномер. Не возвращать слабый legacy check. |
| H21 | Материал скрывает/очищает tenant и инвномер | BuxMove type handlers245–280 | AccountingDialog117–121, ApplyMaterialRules | Реализовано; полезный единый rule. Обратимое переключение черновика/защиту введённых значений рассмотреть в draft lifecycle, не терять окончательно до save. |
| H22 | Owner change очищает документы, фильтрует по owner | BuxMove226–246 (legacy содержит copy/paste ошибка Receipt->WriteOff filter) | AccountingDialog104–115; keyed doc DS | Улучшено, НЕ восстанавливать legacy ошибку фильтра. |
| H23 | Accounting INSERT | Legacy Buh.Save + доступность writeoff document до save | EquipmentAccounting100–116, Service470–478 | INSERT есть; role/function предзапрос, без общего atomic document guard как UPDATE. Не доказан DB bypass; отрицательные document/rights-race сценарии — beta/контрактный пробел. |
| H24 | Accounting UPDATE original version+factual ownership | Legacy unconditional update | SQL1335–1388 locked Id/Equipment/ChangedAt/currentOwner/newOwner/docs/rowcount | Подтверждённое улучшение EQ7. Не заменять full refresh или unconditional update. Draft/error lifecycle пока EQ8-04 / EQ8-05. |
| H25 | Accounting DELETE только подтверждённой latest | Legacy main2008–2027 command; грид delete закомментирован | Main1676+, SQL1390–1424 | Современная полезная функциональность с атомарным latest+owner guard; сохранять. Не удалять кнопку ради legacy. |
| H26 | Accounting documents scope | Legacy types и owner lookup, writeoff availability validation | doc sources DS_doc; UPDATE qualified Документы.dbo.vwДокументы | Корректная маршрутизация в документы, прежняя гипотеза неверного DS опровергнута. Runtime per-principal visibility на beta. |
| H27 | Computer: daily snapshot, latest copied, history | Legacy ComputerEquipment + main1483–1680 | Main1177+, EquipmentComputerSnapshot, SQL811–881 | Сохранено дневное upsert и копирование. Обновление same-day пока без optimistic guard (общий lost-update риск). |
| H28 | RAM/HD/SSD variable components, totals, trim trailing zeros | Legacy Computer595–681 и rendering | EquipmentComputerDetail + dialog, Markup492–575 | Сохранено/улучшен parameterization и явные ошибки вместо непроверенных parse. Sticky error EQ8-10. |
| H29 | RAM bank limit | Legacy фиксировал количество controls по модели | New MemoryBanks count-validation | Смысл сохранён; invalid count исправить пока невозможно EQ8-10; прямой service пока не проверяет модельный лимит. |
| H30 | Standard config dependencies/dates | Legacy Computer686–709 | Нет в Dialog/Service/Entity | Пропуск EQ8-10. |
| H31 | Configuration display includes names and dates | Legacy renders name+[date] | Markup563–570 calls FormatConfiguration | Сохранено. |
| H32 | Software history/link to software equipment, uninstalled styling | Legacy1827–1917 | Markup660–684, SELECT1222+ | Сохранено. |
| H33 | Software add/edit, no delete | Legacy InstallSoft and grid | Main1547–1600, Entity+SQL1270+ | Сохранено. Не вводить delete ради CRUD полноты: legacy сам его запрещал. |
| H34 | Software selection fills Installed from model | InstallSoft257–267 | SoftwareDialog70–74 | Сохранено. Текущий фильтр ПО >200 and notwrittenoff; GetById позволяет показать старую выбранную запись. |
| H35 | Software required fields/date order | Legacy InstallSoft225–248 содержит copy/paste и inverted empty check | EquipmentSoftwareInstallation.Validate54–69 | Улучшено: корректно проверяет SoftwareId, Installed, Distribution, date order. Не копировать legacy ошибки. |
| H36 | Inclusive UI uninstall date/exclusive storage | Legacy SoftEquipment period bindings/save | SELECT1227, INSERT1270+, UPDATE1280+ DATEADD | Сохранено, тесты есть. |
| H37 | Uninstall confirmation | Legacy InstallSoft159–180 explicit confirmation | SoftwareDialog Apply напрямую возвращает | Отдельного подтверждения нет. Это UX отличие, не доказанная порча данных; можно заменить явным действием/описанием «Подтвердить деинсталляцию», не обязательно ещё одним modal. |
| H38 | License single record add-ifabsent/edit | Legacy1417–1478 | Markup458+, Main1472+, SQL UPDATE_Soft | Сохранено. |
| H39 | License type/resale/licensee/temp dates/counts | Legacy Soft_form297–341 | EquipmentLicense.Validate74–92, ApplyTypeRules60–70 | Сохранено, скрытые поля очищаются явно. ServerCount null и ClientCount=0 отвергаются; new UI min0/min1. Service Validate не отвергает negative counts, runtime constraints неизвестны. |
| H40 | License exclusive DateTo | Legacy LicenseSoft date binding | Entity107 end+1; SELECT end−1 | Сохранено. |
| H41 | License type explanation/description | Legacy Soft_form217–236 Notification(Type.Description) | LicenseType имеет Id,Name,Resale,Temporary; dialog explanation нет | Информационная возможность пропущена; P2 UX, полезна при выборе типа. Не блокирует корректное save сама по себе. |
| H42 | UPS quick link with same-office suggestions | Legacy main715–719,2602–2619 | SQL1567+, Main1895–1938 | Сохранено path lookup и link insertion. Реальная структура locations/DB constraints beta. |
| H43 | UPS details: add/remove servers, flag, add/edit tests | Legacy LinkUps455–742 | EquipmentUpsDialog, UpsSaveBuilder | Сохранено по действиям; нет удаления тестов в обоих вариантах, правильно. |
| H44 | UPS edit server identity | Legacy serverdialog позволяет заменить equipment | New delete+add в одном pending diff | Эквивалентный современный workflow; не обязательно восстанавливать отдельный modal. |
| H45 | UPS transaction diff | Legacy LinkUps.Save/load-current-delta | UpsSaveBuilder original vs current, onlychangedDML | Улучшено: новые чужие строки не удаляются из-за отсутствия в old snapshot. Но same-row/test changedfields без version/original comparison — см. EQ8-06. |
| H46 | UPS source/affected-owner access | Legacy relied on app/entity | UpsSaveBuilder119–160 locked current source and each affected | Подтверждённое усиление. Не запрещает изменение тестов из-за untouched foreign-owner server; это правильно. |
| H47 | UPS required read before editing | Legacy loading cache | Main1954–1976 strict allreads first | Исправлено EQ7: failure→no dialog/diff. |
| H48 | UPS test server names: computers only, historical unknown preserved | Legacy606–613,1270–1331 | UpsTestServerSelection | Правило перенесено, но SQL source подменяет raw name EQ8-11. |
| H49 | UPS unchecked notifications warning/issues length | Legacy774–851,694–716 | UpsTestDialog131–145 + PatchIssues | Сохранено; new не закрывает тест при overflow, полезное улучшение. |
| H50 | UPS «выключение настроено» применимо к компьютерам | Legacy892–913 hides flag for noncomputer | UpsDialog34 shows checkbox всем; builder validates IT/nonSIM only | Пропуск applicability guard. Минимум disable/hide for noncomputer and server validation preserving legitimate legacy historical values. Не утверждать physical shutdown effect — это только учётный флаг. |
| H51 | UPS server lookup only admissible devices | Legacy IT/notwrittenoff + exclude self/duplicates | SELECT1567 allows every type except53; transaction then rejects≥100/SIM | UI предлагает то, что save гарантированно отвергнет. Выровнять lookup+server contract. Отдельный вопрос UPS-as-load: legacy исключал self, new исключает all53 — требует предметного решения, не автоматического расширения. |
| H52 | UPS audit metadata | Legacy grids/server/test dialogs show ChangedBy/Changed | new Tests loads metadata, dialog grids показывают только date/battery/servers/issues; servers metadata нечитает | Пропуск обозримости/навигации P2, не изменяет сам save. |

## D. SIM и дополнительные услуги

Источники: [Legacy SIM][legacy-sim], [Legacy AdditionalService][legacy-service-form], [новый SIM dialog][sim-dialog], [SIM service][sim-service], [SQL][sql] и [основная форма][form].

| ID | Сценарий legacy | Современный путь | Оценка и смысл | Проверка/источник |
|---|---|---|---|---|
| S01 | Показ SIM по признаку типа, одна запись | ShowSim / CanEditSim / grid | Сохранено, дополнительно сохранён readonly доступ к существующим данным при черновой смене типа | L FillGrid_Sim:1149–1214; I cs:186–199, Razor:428–454 |
| S02 | Add только если нет SIM; edit/delete если есть | `_simGridOptions.ShowAddButton`, row actions | Сохранено, delete удаляет только SIM, оборудование остаётся | I cs:2149,1247–1387; E SQL:991–994; T EquipmentSimPersistenceTests |
| S03 | Номер только цифры, до 50; договор и активация обязательны | EquipmentSimCard.Validate + dialog Apply + server revalidation | Улучшено: сервер повторяет проверку, а не полагается на regex виджета | L SIM_form.cs:SetControlsValues/ValidateInput; E EquipmentSimCard.Validate |
| S04 | PIN4, PIN2 4–8, PUK/PUK2 8 цифр, пустые допустимы | Validate + masked inputs + separate secret read | Сохранено и улучшено секретное чтение; initial-load race EQ8-04 | E EquipmentSimCard.Validate, Service.GetSimSecretsAsync; T secret no-row/error/empty cases |
| S05 | Секреты не должны попасть в общий список | Separate SELECT_SIMКарта_Секреты | Улучшено; не отменять ради legacy | E SQL:887–902,945–952; T GetSimCards_DoesNotReadSecrets |
| S06 | Существующая SIM UTC→local→UTC со временем и секундами | OpenSimDialogAsync / ConvertLocalToUtcAsync / ClayTime HH:mm:ss | Сохранено по коду; browser timezone/DST E2E — beta | I cs:1254–1260,1302; C ClayTimeOptions:TimeFormat |
| S07 | Кнопка активации «Сейчас» | Нет кнопки/аналога в dialog | Пропущено удобное действие; вернуть browser-local now, не server DateTime.Now | L SIM_form.aspx btnNow; Kesco.SimEquipment.js:setDateFromNow; I dialog:25–31 |
| S08 | Незавершённые мобильные договоры в поиске | MobileContractSelectDataSource + Сотовая=1, РаботаЗавершена=0 | Сохранено; существующий завершённый договор может разрешаться по ID для отображения | L dbsContract_BeforeSearch; E SQL:996–1020 |
| S09 | ПДн учитывает договор, оборудование, технический тариф, тип заказчика | PersonalDataPersonSelectDataSource | Дефект EQ8-07B / EQ8-04: потерян CTE/technical tariff predicate; CustomerType заменён эвристикой | L SQL:3038–3063; E SQL:1023–1058 |
| S10 | Предупреждения по несовместимому ПДн | Отсутствуют эквивалентные предупреждения | Пропущено в EQ8-07B / EQ8-04; не копировать устаревший HTML | L SIM_form.cs:dbsPersonalData_OnRenderNtf; I dialog Apply |
| S11 | Заполнить ПДн из ответственного | CurrentEmployeeId + GetEmployeePersonIdAsync | Частично сохранено; null/late response может стереть выбор, EQ8-07B / EQ8-04 | L DisplayFillPersonData/SetPersonData; I dialog:50,160–165 |
| S12 | Смена договора обновляет область ПДн | OnContractChanged очищает selection | Дефект EQ8-07B / EQ8-04: object-resolve неизменного ID ошибочно трактуется как смена договора; не очищать ПДн и секреты при hydration | I dialog:152–158; C ClaySelect:18–19,358–383; T test проверяет только секреты |
| S13 | Найти другой активный SIM того же номера | Required scalar duplicate lookup | Сохранено; ошибка не означает «дубликата нет» | E SQL:955–961; I cs:1280–1292 |
| S14 | Подтверждение списания дубликата на активацию новой SIM | ConfirmAsync + BuildSave transaction | Улучшено атомарностью и проверкой актуальности подтверждённого дубликата | E EquipmentSimSaveBuilder:BEGIN TRANSACTION/UPDATE/ROWCOUNT/ROLLBACK; T SaveSimWithDuplicate |
| S15 | Наследовать ПДн старой SIM, если новое пусто | COALESCE параметра с ПДн дубликата в той же транзакции | Сохранено; не заменять введённое новое ПДн | L WriteOffAnother; E SaveBuilder |
| S16 | Ошибка SIM save оставляет ввод | Dialog.Close до save | Дефект EQ8-04; восстанавливаемый черновик нужен также после reconnect/update/IIS | L SaveData catch; I dialog:190 + parent handlers |
| S17 | SIM grid активация/договор/ПДн/Changed | ClayGrid, FormatDisplayDate | Данные сохранены; ссылки на ПДн и автора потеряны | L grid href:1200–1201; I Razor:441–446 |
| S18 | История и обязательные услуги | Два ClayCheckbox; серверные параметры; version guard | Сохранено на happy path; error recovery EQ8-04 | I cs:1452–1470; SQL:1075–1105; T GetServices_MapsFields |
| S19 | Каталожная услуга копирует название/стоимости | OnTypeChanged | Сохранено; costs readonly при добавлении, editable при редактировании | L SetValuesBySimTypeService/SetInitControls; I ServiceDialog:68–84 |
| S20 | Свободное название без типа | Нет допустимой ветви validation | Дефект EQ8-09, особенно старые custom строки | L ValidateInput допускает OR; E Validate требует type AND name |
| S21 | Фактическая дата from≤to, не будущее | Validate(today) | Сохранено; date-only business clock следует согласовать централизованно | E EquipmentSimService:101–113; T Validate_ChecksServiceAndDates |
| S22 | Окончание услуги включительно в форме, эксклюзивно в БД | DATEADD(-1) при чтении, AddDays(1) при записи | Сохранено | L SimService.FillData/Save; E SQL:1081, Entity:125; T SaveService_UsesExclusiveEnd |
| S23 | Предотвращение перекрытия периодов одинаковой услуги | Симметричный overlap SQL | Улучшено для открытых периодов; не возвращать legacy неполное условие | L SQL:7082; E SQL:1134–1135; T симметричного условия |
| S24 | «Плановые даты» в legacy форме | В новом редакторе не редактируются; read DTO/conditional columns поддержаны | Обоснованное отличие: фактический legacy SELECT возвращает NULL, UPDATE/INSERT планов закомментированы. Нельзя обещать работающий планировщик или создавать фиктивный источник | L SQL:7034–7063,7092–7104; E SQL:1082–1083; I Razor:774–813 |
| S25 | Tariff highlight, mandatory tooltip, MustDisable, overdue plan | RowClass + GetWarning + required icon | Сохранено в действующих данных и при появлении подтверждённого плана | L FillGrid_SimUsl; E methods:82–98; I Razor:794–827; T boundary/DOM cases |
| S26 | BLOCK только действующий период | SimIsBlocked + inclusive date rule | Сохранено: после перевода exclusive→inclusive сравнение >=Today корректно | L SQL/FillGrid_SimUsl; E IsActiveBlock; T Block_UsesInclusiveEnd |
| S27 | Суммы дневной/месячной стоимости | Sum по загруженной коллекции с текущими серверными фильтрами | Сохранено для доступных UI-фильтров; toolbar/column filters скрыты, дополнительный дефект filtered totals не доказан | I cs:254–258,ConfigureSectionGrid; Razor:838–844 |
| S28 | Услуги можно изменять, удаление истории не предлагается | Только row Edit, delete отсутствует | Сохранено | L FillGrid_SimUsl; I Razor:833–836 |
| S29 | Detail только Билайн | UI ShowSimDetail + server detail | Частично: свежий серверный operator guard отсутствует, EQ8-09 | L FillGrid_Sim:1183; I cs:188–190; Service:259–284 |
| S30 | Доступ к detail через 41/42 или подчинённых | Role result + отдельная Tariffication проверка | Доменная ветка сохранена, внешняя БД не требуется роли; identity/scope замечание у разделе EQ8-02 | L SQL:6567–6591; E SQL:905–931 |
| S31 | Один ответственный или единственный текущий из истории | responsibles.Count==1 иначе SingleOrDefault(ToUtc null) | Сохранено, включая одиночного исторического ответственного по правилу legacy | L AccountingShowSimDetail; I Service:279–284; T DetailUri_UsesSingleResponsible |
| S32 | НДС/округление/period params/colors/title | BuildAccountingPhoneUri | Сохранено; escaping улучшен, existing query не теряется; OpenMonth=1,Year=0,Month=0,bln=0 | L AccountingShowSimDetail; E BuildAccountingPhoneUri; T URI VAT matrix |
| S33 | Открытие detail не сохраняет главный черновик | OpenSimDetailAsync только read + clayOpen | Сохранено и безопасно для основного ввода; late ID/dispose guard есть | I cs:1761–1798; EquipmentFormTests dirty detail case |

## E. Сеть и общее клиентское поведение

Источники: [Legacy form/JS][legacy], [STYLES][styles], [новые Controls][controls] и [форма][form]. Адаптивность оценена по коду; живой мобильный render не заявляется.

| ID | Сценарий legacy | Современный путь | Оценка и смысл | Проверка/источник |
|---|---|---|---|---|
| N01 | Поиск сетевых портов по имени, убирается domain prefix | SELECT_NAME_vwЛокальнаяСеть | Сохранено; параметры, fn_SplitWords, сортировка | L GetNetIdByNetName; E SQL:1637–1648 |
| N02 | Число портов больше Model.Net или 2 порта при пропущенном MAC | EquipmentNetworkDiagnostics.HasWarning | Сохранено; red class вместо inline legacy CSS | L DisplayNet:2416–2417; I cs:173–174; T NetworkDiagnostics |
| N03 | Для switch/router <3 подавить warning, >=3 скрыть Net/UPS | HasWarning / HideSection + Razor | Сохранено как подтверждённое правило текущей поставки | L DisplayNet:2421–2431; I Razor:96,321; T boundary theories |
| N04 | Сетевые имена с `*` не показываются, но участвуют в count | `_networkPorts.Where(!StartsWith("*"))`, общий Count | Сохранено | L DisplayNet:2437–2441; I Razor:321–328 |
| N05 | Ссылка порта открывает Net | NetUrl + OpenNetAsync + clayOpen(net-form) | Сохранено; внешняя/локальная цель берётся из конфигурации | I Razor:327; cs:1824–1828 |
| N06 | Attach/detach в Операциях по computer type и support role | Menu + server EnsureNetworkAccessAsync | Логика команды сохранена, серверная проверка усиливает UI; role global scope у разделе EQ8-02 | L RenderHeader role103, PrepareAttachDetachNet; I cs:1843–1889; Service:689–695 |
| N07 | Detach требует подтверждения | ConfirmAsync before command | Сохранено; общий ConfirmAsync проверяет operation version и ID | I cs:1847,2444–2458. Первоначальное подозрение race отозвано, не включать в дефекты |
| N08 | Save текущей карточки перед attach/detach | SaveAsync + проверка IsModified/disposed/model null | Улучшено: команда не продолжается после неуспешного save/reload | I cs:1849–1853; T network reload-null case |
| N09 | После write показать реальное новое состояние | write then required count read | Дефект EQ8-05 при read failure; исход успешной записи нельзя отрицать | I cs:1863–1876 |
| N10 | Поздний сетевой ответ не меняет другую карточку | `_networkVersion`, equipmentId, disposed | Сохранено и усилено | I cs:1855–1869,LoadCurrent increments; test coverage limited to present cases |
| N11 | История сетевых имён, local Changed и автор | Отдельный readonly MudDialog + ClayGrid | Хороший эквивалент hover-tooltip для клавиатуры/touch; ссылка автора стала текстом | L RenderNetNameInfo:2198–2263; I NetworkNameHistoryDialog; parent FormatDate |
| N12 | Ping каждые5с и статусы online/offline/warning | Эквивалентного endpoint/polling нет | Пропущенная/отложенная возможность. Не обозначать «реализовано, не проверено» и не имитировать false status | L Kesco.Equipment.js:542–651, PingHostHandler; current docs matrix:53–55 |
| N13 | WOL после подтверждения → sp_WakeOnlan | Код команды не реализован, issue#3 указан в поставке | Отложено; реальная процедура/пакетная отправка на beta. Доменная реализация сама ещё отсутствует | L Equipment.cs:1571–1575; current docs:55 |
| N14 | Legacy onbeforeunload при изменённой форме | NavigationLock + ConfirmAsync | Эквивалент предупреждения есть у основной формы; это не durable draft | STYLES Kesco.V4.js:1123–1152; I Razor:25,cs:2362–2412 |
| N15 | Повторное открытие дочерних форм сохраняет родителя | MudDialog не перезагружает родителя, clayOpen для новых окон | Сохранено; возврат DTO с ID/version guard; ошибка save требует EQ8-04 | L openFormPostion/idpp/SignalR; I ShowDialogAsync |
| N16 | Сохранение размера/позиции popup | clayFormWindow localStorage по form key | Сохранено назначение через общий компонент; хранится геометрия, НЕ ввод пользователя | C wwwroot/js/clayFormWindow.js:20–67 |
| N17 | Открытие SIM detail через устойчивый popup key | clayOpen("equipment-sim-detail") | Сохранено, parent draft не очищается; реальная блокировка popup/другойorigin — beta | I cs:1778; C clayFormWindow |
| N18 | Readonly checkbox не должен менять модель | ClayCheckbox.HandleClick немедленно возвращает при ReadOnly/Disabled; Space вызывает тот же handler | Общий контрол корректен по коду. SIM filters намеренно интерактивны и в режиме чтения: это чтение, не запись доменных значений | C ClayCheckbox.razor:HandleClick/HandleKeyDown; I Razor:759–760; прочие checkbox модели у parent/auth |
| N19 | Мобильные 320–480px | app.css single-column breakpoints<=600 для main и child rows/date/MAC | По коду предусмотрена адаптация; отсутствие live render не превращать в найденный overflow. Проверить grids, filters, totals, touch-tooltip и dialogs в beta | I app.css:281–312,353–373 |

## F. Прямые серверные входы и фактические guards

Источники: [EquipmentFormService][service], [Employee][employee], [SQL][sql]. Наличие CRUD в Entity без UI principal не само по себе дефект: важна проверка на серверной границе бизнес-операции. Не вводить blanket role43 для чтения.

| ID | Вход EquipmentFormService | Identity/role | Контекст/тип на сервере | Вывод |
|---|---|---|---|---|
| A01 | GetFormDataAsync IncludeAccounts; GetAccountsAsync; GetAccountPasswordAsync | Employee fresh102 (ANY) | equipment/login query, без type reload | Secret split корректен; global102 и HTTP/circuit identity контракт должны быть явными |
| A02 | Add/Update/DeleteAccountAsync | fresh102+43 | данные ключа из account, без actual-type read | Есть role guard; для неподходящего типа/смены контекста полагаться только на UI нельзя, нужен согласованный доменный контракт |
| A03 | UpdateAsync; UpdateWithPhonesAsync | fresh43 | валидация переданного equipment + type lookup; model/type/owner optimistic concurrency отдельно | Известные хорошие guards сохранить; phone selection eligibility и actual context следует проверять в регрессионной матрице |
| A04 | Add/UpdateLocation; Issue/Update/WithdrawResponsible | fresh43 | entity-specific history operations | Guard имеется; история/concurrency рассмотрена в EQ8-06 |
| A05 | SaveSoftwareInstallationAsync; SaveComputerAsync | fresh43 | entity Validate/Save; Service не читает actual computer characteristic | Не заявлять, что UI CanEditComputer является server type guard; direct operation contract должен быть проверен |
| A06 | SaveSimAsync; DeleteSimAsync; SaveSimServiceAsync; GetSimSecretsAsync | fresh42 | entity validation/current SQL в Save; Service без общего actual-type reload | Secret reads защищены; SIM-правила — EQ8-09; ПДн и lifecycle — EQ8-07B/EQ8-04 |
| A07 | GetSimDetailUrlAsync | Нет Employee проверки; SQL roles41/42/subordinates | current responsible/SIM reads | Конкретный gap из EQ8-02 |
| A08 | Attach/DetachNetworkAsync | fresh103 ANY | actual Equipment + Type | Global scope неверен; current type guard есть |
| A09 | Add/DeleteUpsLinkAsync | fresh43 owner-or-global | actual equipment/type/non-SIM/IT | Улучшение относительно UI-only; null scope долг |
| A10 | SaveUpsDetailsAsync | Нет Employee проверки | entity SQL role guards/source+changed owners | Дополнить trusted identity; сохранить атомарные SQL guards |
| A11 | SaveAccountingAsync | fresh66/68 per owner + accounting availability | entity atomic checks | Guard существует; атомарные проверки фактического/нового владельца сохранены; остальные замечания — EQ8-02/04/05 |
| A12 | DeleteAccountingAsync | Нет Employee проверки | atomic SQL roles/current last row | Дополнить trusted identity без удаления SQL защиты |
| A13 | SaveLicenseAsync | fresh44 owner или tenant | actual equipment/type/owner/tenant | Хороший пример fresh target scope, null owner нужно явно трактовать |
| A14 | GetPhoneConfigAsync | fresh102 via provider | MAC/procedure | HTTP provider regression; circuit and HTTP источники identity различать |
| A15 | SynchronizePhoneOperationsAsync/ExecutePhoneSyncOperationsAsync | Нет Employee проверки | caller-provided diff/operation keys, lookup без target auth | Известный P1 auth gap реального executor |
| A16 | Прочие публичные read helpers | Без явной Employee role проверки | parameterized read/views | Не вводить blanket role43: чтение и запись имеют разные права; применить только подтверждённые правила доступности данных |

## G. Многострочность и пределы полей

Источники: [Legacy markup дочерних форм][legacy], [текущие Details][details], [Clayzor Controls][controls]. ClayTextBox однострочный; ClayTextArea передаёт Lines. Факт усечения существующих значений на реальной БД/в браузере не утверждается. EQ8-12/13 требуют сохранить round-trip и сверить пределы со схемой.

| ID | Поле | Активный legacy markup | Clayzor | Что делать |
|---|---|---|---|---|
| U01 | Location notes | TextArea,255 (`LocationEquipment_form.aspx:43`) | TextBox,300 (`LocationDialog:70`) | Вернуть пригодный многострочный ввод; единый лимит по подтверждённому DB контракту. |
| U02 | Responsible notes | TextArea,255 (`ResponsibleEquipment_form.aspx:131`) | TextBox,300 (`ResponsibleDialog:85`) | Аналогично; сохранить значение/переносы строк при edit. |
| U03 | Computer notes | TextArea,300 (`Computer_form.aspx:85`) | TextBox,300 | Лимит тот же, многострочный UX потерян. |
| U04 | Software notes | TextArea,1000 (`InstallSoft.aspx:57`) | TextBox,300 (`SoftwareDialog:58`) | Подтверждённое сужение UI-контракта без обоснования. Проверить фактическую длину БД, затем выбрать безопасный предел; не обрезать сохранённые длинные notes при открытии/редактировании. |
| U05 | License Distribution | TextBox,50 (`Soft_form.aspx:85`) | TextBox,100 (`LicenseDialog:67`) | Расширение не объявлять ошибкой/улучшением до проверки DB length; UI/model/parameter одинаковы. |
| U06 | UPS Issues | TextArea,300 (`LinkUps.aspx:107`) | TextArea,4000 (`UpsTestDialog:73`), SQL parameters4000 | Существенно иной предел. Учитывать auto-appended warnings; подтвердить DB contract на beta, не предполагать truncation/error без схемы. |

## H. Ограничения полноты и обязательные решения

| Область | Точный статус |
|---|---|
| R01 | Выбор архитектуры SQL↔ATS открыт. Нынешний circuit-local pending не становится durable outbox автоматически. Свежую авторизацию/безопасный retry исправить независимо. |
| R04 | Политика MAC требует решения; внутреннюю несогласованность accepted format → raw SQL parameter исправить без возврата удаления любых неhex-символов. |
| Ping indicator / WOL | В исследованном Clayzor отсутствует код соответствующих действий. Нужна реализация/интеграция из общего плана, затем разрешённые внешние испытания; не помечать готовым только по наличию issue. |
| DR-01 | Обязательный общий механизм и интеграция всех редакторов. Предупреждения/геометрия popup/local circuit не достаточны. Проверки реального IIS/update/network на beta. |
| SQL document/role/trigger contracts | Локальная логика и SQL-текст исследованы. Отрицательные реальные сценарии INSERT accounting, same-row races и совместимость целевого SQL требуют beta. Не утверждать ни доказанный обход trigger, ни доказанную безопасность без проверки. |
| Нестандартные типы и реальные справочники | Ветвления по признакам и явным кодам исследованы; все фактические строки справочников/оборудования на производственной БД не перечислялись и не проверялись. Beta должна включать представителей каждой ветви и исторические данные. |
| Источники STYLES | Применимые сценарии формы включены; ранее подготовленная общая задача компонентов остаётся в силе. Это не повторный аудит каждой сторонней библиотеки STYLES. |
| Локальные тесты | Нужны сценарии обнаруженных дефектов, а не только прежние fake responses. Counts автора EQ7 не равны полноте этой матрицы. |

## Что особенно важно не вернуть из Legacy

Сохранить staging и атомарный phone diff, failed-only ATS retry без повторного UPDATE, отдельное чтение секретов, точный accounting UPDATE/latest DELETE, атомарный SIM duplicate writeoff, корректное перекрытие периодов, original/current UPS diff, guards устаревшего UI, адаптивность и типизированные компоненты. Не переносить отключённые SIM-contract popups, фиктивное планирование услуг, copy/paste ошибки валидации/фильтров, неявное округление ключей, raw HTML/JS и неуправляемые ping timers.

## Приёмка по матрице

В handoff EQ8 для каждого ID строки сохранить статус: реализовано и проверено локально; сохранено без изменений; исправлено; обоснованное отличие; beta; зависимость/решение. Если более новый код уже устраняет замечание, указать commit и регрессию. Не удалять строку ради объявления полной готовности. Переход к beta не требует физического теста во время текущей разработки, но требует честного списка оставшихся проверок и функций.


[super]: https://github.com/Postnn/Clayzor/commit/7e40c6c7559b8db3003ea66936ca48cb69d540c3
[inventory]: https://github.com/Postnn/Kesco.App.Web.Inventory/tree/d47e2ff584932ab88a4602f6ed98ee9ed9208ed4
[entities]: https://github.com/Postnn/Clayzor.Lib.Entities/tree/df7a8d08d194ca2b1af691458066a1e17d725664
[tests]: https://github.com/Postnn/Clayzor.Lib.Web.Controls.Tests/tree/eb1e1230a1df80c73b7ab81c9580fff6342a1738
[dbselect]: https://github.com/Postnn/Clayzor.Lib.Web.DBSelect/tree/9b8551dab6228ebcaff7506110c71fddff5b6f03
[controls]: https://github.com/Postnn/Clayzor.Lib.Web.Controls/tree/ea980be926ae975678314029ba5fc8aa68c675ba
[legacy]: https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/tree/12a6efae7eb310adec06a57f6d80b8d9e76985b4
[styles]: https://github.com/KescoDev/Styles/tree/fddc5a2157cf906402c50731e84cfa537abd7dba
[handoff]: https://github.com/Postnn/Clayzor/blob/7e40c6c7559b8db3003ea66936ca48cb69d540c3/promts/_handoff/EQUIPMENTFORM_AUDIT_7.md
[form]: https://github.com/Postnn/Kesco.App.Web.Inventory/blob/d47e2ff584932ab88a4602f6ed98ee9ed9208ed4/Components/Pages/Equipment/EquipmentForm.razor.cs
[markup]: https://github.com/Postnn/Kesco.App.Web.Inventory/blob/d47e2ff584932ab88a4602f6ed98ee9ed9208ed4/Components/Pages/Equipment/EquipmentForm.razor
[service]: https://github.com/Postnn/Kesco.App.Web.Inventory/blob/d47e2ff584932ab88a4602f6ed98ee9ed9208ed4/Services/EquipmentFormService.cs
[sql]: https://github.com/Postnn/Clayzor.Lib.Entities/blob/df7a8d08d194ca2b1af691458066a1e17d725664/SQLQueries.cs
[sql-accounting]: https://github.com/Postnn/Clayzor.Lib.Entities/blob/df7a8d08d194ca2b1af691458066a1e17d725664/SQLQueries.cs#L1335-L1424
[accounting]: https://github.com/Postnn/Clayzor.Lib.Entities/blob/df7a8d08d194ca2b1af691458066a1e17d725664/Inventory/EquipmentAccounting.cs
[equipment]: https://github.com/Postnn/Clayzor.Lib.Entities/blob/df7a8d08d194ca2b1af691458066a1e17d725664/Inventory/Equipment.cs
[employee]: https://github.com/Postnn/Clayzor.Lib.Entities/blob/df7a8d08d194ca2b1af691458066a1e17d725664/Inventory/Employee.cs
[http-handler]: https://github.com/Postnn/Kesco.App.Web.Inventory/blob/d47e2ff584932ab88a4602f6ed98ee9ed9208ed4/Program.cs#L176-L190
[framework-auth]: https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Components/Endpoints/src/DependencyInjection/ServerAuthenticationStateProvider.cs
[phone-service]: https://github.com/Postnn/Kesco.App.Web.Inventory/blob/d47e2ff584932ab88a4602f6ed98ee9ed9208ed4/Services/PhoneFormService.cs
[phone-tests]: https://github.com/Postnn/Clayzor.Lib.Web.Controls.Tests/blob/eb1e1230a1df80c73b7ab81c9580fff6342a1738/EquipmentPhonePersistenceTests.cs
[phone-source]: https://github.com/Postnn/Clayzor.Lib.Web.DBSelect/blob/9b8551dab6228ebcaff7506110c71fddff5b6f03/Equipment/PhoneNumberSelectDataSource.cs
[phone-query]: https://github.com/Postnn/Clayzor.Lib.Entities/blob/df7a8d08d194ca2b1af691458066a1e17d725664/Inventory/PhoneNumberQuery.cs
[sim-dialog]: https://github.com/Postnn/Kesco.App.Web.Inventory/blob/d47e2ff584932ab88a4602f6ed98ee9ed9208ed4/Components/Pages/Equipment/Details/EquipmentSimDialog.razor
[sim-builder]: https://github.com/Postnn/Clayzor.Lib.Entities/blob/df7a8d08d194ca2b1af691458066a1e17d725664/Inventory/EquipmentSimSaveBuilder.cs
[sim-service]: https://github.com/Postnn/Clayzor.Lib.Entities/blob/df7a8d08d194ca2b1af691458066a1e17d725664/Inventory/EquipmentSimService.cs
[accounting-dialog]: https://github.com/Postnn/Kesco.App.Web.Inventory/blob/d47e2ff584932ab88a4602f6ed98ee9ed9208ed4/Components/Pages/Equipment/Details/EquipmentAccountingDialog.razor
[location-dialog]: https://github.com/Postnn/Kesco.App.Web.Inventory/blob/d47e2ff584932ab88a4602f6ed98ee9ed9208ed4/Components/Pages/Equipment/Details/EquipmentLocationDialog.razor
[responsible-dialog]: https://github.com/Postnn/Kesco.App.Web.Inventory/blob/d47e2ff584932ab88a4602f6ed98ee9ed9208ed4/Components/Pages/Equipment/Details/EquipmentResponsibleDialog.razor
[computer-dialog]: https://github.com/Postnn/Kesco.App.Web.Inventory/blob/d47e2ff584932ab88a4602f6ed98ee9ed9208ed4/Components/Pages/Equipment/Details/EquipmentComputerDialog.razor
[computer-entity]: https://github.com/Postnn/Clayzor.Lib.Entities/blob/df7a8d08d194ca2b1af691458066a1e17d725664/Inventory/EquipmentComputerSnapshot.cs
[ups-dialog]: https://github.com/Postnn/Kesco.App.Web.Inventory/blob/d47e2ff584932ab88a4602f6ed98ee9ed9208ed4/Components/Pages/Equipment/Details/EquipmentUpsDialog.razor
[ups-selection]: https://github.com/Postnn/Kesco.App.Web.Inventory/blob/d47e2ff584932ab88a4602f6ed98ee9ed9208ed4/Rules/UpsTestServerSelection.cs
[details]: https://github.com/Postnn/Kesco.App.Web.Inventory/tree/d47e2ff584932ab88a4602f6ed98ee9ed9208ed4/Components/Pages/Equipment/Details
[select-contract]: https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Select/ClaySelect.razor.cs
[legacy-form]: https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Inventory/Equipment/Equipment_form.aspx.cs
[legacy-markup]: https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Inventory/Equipment/Equipment_form.aspx
[legacy-sim]: https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Inventory/Equipment/SIM_form.aspx.cs
[legacy-service-form]: https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Inventory/Equipment/AdditionalService_form.aspx.cs
[legacy-computer]: https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Inventory/Equipment/Computer_form.aspx.cs
