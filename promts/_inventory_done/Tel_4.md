# Независимый аудит Tel_3 — 19.11.2026

## Исходные версии

Проверена ветка `tel-2-fixes` (без merge):

- суперпроект `Postnn/clayzor`: `fe89c653693a0469b43982d0bc7134930e0d7296`;
- Inventory: `52ba1d58c7b15fecf79ae7c6648c1ce721b83002`;
- Entities: `bd56dd5798d5cb583343623a5dc9f632197f99f9`;
- Controls: `91a27c46f5305ae9dbd8405527ed44551232bfc5`;
- DBSelect: `8ae98fb8c2a5fea20515a21895c1df9791c46327`;
- Tests: `a19fe094351f0b571cda7aa9ac8f1a760a5d4744`.

Сравнение с предыдущим `Tel_2`: Inventory `5b84cc6`, Entities `fa3b395`, Controls `51b7fd4`, DBSelect `5c98a19`, Tests `d978795`.

Legacy: `KescoDev/Kesco.App.Web.V4.Inventory`, `Inventory/Telefon/Tel.aspx.cs` (методы `DbsSocket_BeforeSearch`, `DbsEquipment_Changed`, `GetRequiredSynchronizations`, `Save`, `Delete`), `Kesco.Lib.Entities/Corporate/Phones/Phone.cs` (`Save`, `SavePhoneGroup`, `SavePhoneEquipment`).

## Проверенные исправления

| ID | Вердикт по исходникам | Примечание |
|---|---|---|
| P1-01 | Исправлено в запросе | `SELECT_ТелефонРозеткаДопустимаВОфисе`: L/R вложенного офиса, `Работает = 1`, собственная розетка исключает только проверку занятости; `SELECT_ТелефонРозеткаДопустимаОборудования` аналогично. Это устраняет ранее подтверждённое несовпадение с поиском. |
| P1-02 | Исправлено | `PhoneSyncPlanner.Build`: `if (mainChanged || equipmentChanged)` запускает Meridian/Xorcom для структурных изменений оборудования; изменение только метаданных не считается структурным. |
| P1-03 | Реализован явный pre/post порядок и компенсация | `Tel.SaveAsync`: Xorcom `Deleting` старой станции до SQL при смене станции; новая станция после SQL. `DeleteConfirmedAsync`: Xorcom перед SQL DELETE, прочие операции после. При SQL-ошибке предусмотрена компенсация. Реальный эффект `sp_Xorcom_PhoneNumberSync` независимо не проверялся: доступен только отчёт агента о SQL-определении. |
| P2-04 | Исправлено | `PhoneSocketSelectDataSource.SearchAsync` сразу возвращает пустой результат при `StationId == null` без SQL, даже если есть расположения оборудования. |
| P2-05 | Исправлено | `SaveAsync` повторно читает `GetCapabilitiesAsync` до нормализации, проверяет version и пару StationId/TypeId. |
| P2-06 | Сознательно не реализовано | Durable pending/outbox для синхронизаций отсутствует, инструкция по восстановлению есть в `docs/phone-form.md`. |
| P3-07 | Осознанное UX-решение | Пустой текст `ClayGrid` оставлен, высота уменьшена до 32 px. Это отличие от legacy сохранено по более позднему указанию. |

### Подтверждение тестового покрытия по коду

Добавлены тесты `Sync_StructuralEquipmentDiffIncludesStation`, `Socket_MissingStationFailsClosedWithEquipmentLocations`, `SocketValidation_UsesOfficeSubtreeAndChecksContextForCurrentSocket`, `Save_RefreshesCapabilitiesBeforeNormalization`, `Xorcom_StationChangeRunsOldDeleteBeforeSaveAndNewSyncAfterSave`, `Xorcom_SaveFailureRestoresOldStation`, `Xorcom_PreSaveFailureBlocksSqlWrite`, `Xorcom_DeleteRunsBeforeSqlDelete`, `Xorcom_DeleteFailureRestoresOldStation`. Часть тестов проверяет структуру SQL строками, часть — mock-сервис в bUnit. Число 305/305, реальный браузер и чтение SQL подтверждены отчётом агента; независимо не запускались.

## Остаточные замечания / проверки

### R1 — P2: Delete после несохранённой смены станции

`Tel.razor.cs:DeleteConfirmedAsync` собирает `originalStation = _originalStation` и `currentStation = _selectedStation`, затем запускает `PhoneSyncPlanner.Build(..., deleting: true)`. При несохранённой смене Xorcom A→B план может включить **две** операции Xorcom с разными `Key`, но обе процедуры получают один `PhoneId` и, по объяснению агента, читают **сохранённую** станцию A. Это может вызвать дублирующую очистку старого узла или ложный отказ удалить номер из-за операции для B. Нужно для DELETE получать состояние сохранённой записи БД (станция, оборудование) и не учитывать черновые значения, либо согласованно отвергать Delete до отката черновика. Тест: `open A; draft B; without Apply -> Delete`, убедиться, что вызывается только SQL-релевантная станция A, и нет вызовов/CCM из черновика B. Legacy тоже использует часть текущих значений формы, но физический контракт SQL-процедуры делает риск значимым.

### R2 — P2: Валидация до потенциально необратимого pre-sync Xorcom

`Tel.SaveAsync` запускает Xorcom pre-sync до `PhoneFormService.SaveAsync`, где выполняются основная серверная проверка ролей/данных и SQL. При отклонении формы сервером выполняется компенсация; она полезна, но не гарантирует атомарность внешней АТС. Для уменьшения лишних side effects предварительно выполнять read-only серверную валидацию непосредственно перед pre-sync, сохранив обязательную финальную проверку при SQL Save. Не считать эту предварительную валидацию заменой финальной: данные могут измениться между вызовами. Тест: бизнес-валидация (например, занят порт) блокирует pre-sync без очистки старой АТС. Предварительные проверки прав в C# **не требуются**: по уточнению заказчика права выполняются внутри SQL-процедур синхронизации.

### R3 — P2: Fail-closed для расчёта полного номера

`PhoneFormService.ValidateServerAsync` при `ExecuteScalarAsync<string>(SELECT_ТелефонАвторитетныйПолныйНомер)` использует fallback `?? phone.Number`. `DbManager.ExecuteScalarAsync` в этом проекте возвращает `default` при connectivity SQL error. Поэтому при кратковременном сбое запрос может не вернуть авторитетное значение, а дальнейшие SQL-проверки теоретически успешно выполниться, оставив некорректный номер. Не заменять отказ чтения на пользовательский короткий номер: ошибка должна отменять Save; отладить разницу между «данных нет» и потерей соединения. Тест: временная ошибка авторитетного расчёта -> нет SQL Update / INSERT.

### R4 — P2: Регрессии общих стилей

Изменения в `ClayEntityForm.razor` и `wwwroot/css/clay.css` убрали общий `pa-3` и перенесли padding на toolbar/content. Это влияет не только на Tel, но и на другие формы Clayzor. Протестировать в браузере `EquipmentForm`, `EquipmentIns`, `ModelEquipmentForm` и Tel на 600/900/1155 px: положение золотой линии, toolbar, footer, полей, вертикальная прокрутка, кликабельность.

### R5 — ограничение приёмки: нет интеграционных данных

Сравнение L/R и SQL test read-only на контрольных ID доказывают отсутствие SQL syntax errors, **но не корректную выборку** для альтернативных кабинетов одного офиса. Подготовить тестовые фикстуры на разрешённом стенде с гарантированной очисткой, либо отложить этот класс проверок с явной фиксацией приёмочного риска. Xorcom mock проверяет порядок, но не реальный внешний эффект и восстановление после сбоя circuit. Durable outbox по соглашению не включать в текущий scope, но не трактовать этот риск как устранённый.

## Что не считать замечанием

По прямому уточнению заказчика **не добавлять вторую проверку ролей** в `PhoneFormService.SynchronizeAsync` и Tel: авторизация уже выполняется SQL-процедурами синхронизации. Оценивать ответ SQL Server, логирование и компенсацию ошибки. Отдельный параллельный рефакторинг `Employee CurrentUser` не включать в Tel_3 без явного решения.

## Рекомендация

Пункты Tel_3 по коду реализованы и соответствуют заявленному замыслу. Для безусловного merge рекомендуются R1, R2, R3 + регрессионный smoke test общих форм и фикстуры SQL. При решении принять риски без исправлений зафиксировать их как open issues с владельцем и ограничением на rollout к реальным АТС. Не запускать реальные внешние команды АТС без согласованного окна.

## Ссылки

- [Tel.razor.cs](https://github.com/Postnn/Kesco.App.Web.Inventory/blob/52ba1d58c7b15fecf79ae7c6648c1ce721b83002/Components/Pages/Telefon/Tel.razor.cs)
- [PhoneFormService.cs](https://github.com/Postnn/Kesco.App.Web.Inventory/blob/52ba1d58c7b15fecf79ae7c6648c1ce721b83002/Services/PhoneFormService.cs)
- [PhoneSyncPlanner.cs](https://github.com/Postnn/Kesco.App.Web.Inventory/blob/52ba1d58c7b15fecf79ae7c6648c1ce721b83002/Services/PhoneSyncPlanner.cs)
- [SQLQueries.cs](https://github.com/Postnn/Clayzor.Lib.Entities/blob/bd56dd5798d5cb583343623a5dc9f632197f99f9/SQLQueries.cs)
- [PhoneSocketSelectDataSource.cs](https://github.com/Postnn/Clayzor.Lib.Web.DBSelect/blob/8ae98fb8c2a5fea20515a21895c1df9791c46327/Phones/PhoneSocketSelectDataSource.cs)
- [TelAsyncStateTests.cs](https://github.com/Postnn/Clayzor.Lib.Web.Controls.Tests/blob/a19fe094351f0b571cda7aa9ac8f1a760a5d4744/TelAsyncStateTests.cs)
- [Legacy Tel.aspx.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/main/Inventory/Telefon/Tel.aspx.cs)
