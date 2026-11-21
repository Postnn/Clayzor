# EquipmentForm_4 — рабочий протокол приёмки

## Этап А — исходное состояние

Legacy `C:/CLAYZOR-KIMI/Kesco.App.Web.V4.Inventory` находится на требуемом
`12a6efae7eb310adec06a57f6d80b8d9e76985b4`, рабочее дерево чистое.
HEAD новых модулей совпадает со снимком аудита: Inventory `aa37f25`, Entities `46cce48`,
DALC `99e85b5`, Controls `03750bc`, Tests `0894fc0`. Все незакоммиченные изменения
известны по текущей сессии: меню/grid/theme/placeholders; посторонних изменений не
обнаружено. Откат и перезапись не выполнялись.

Матрица ниже — исходный статус, не итоговая приёмка. «Исходник» означает только найденный
путь, а не доказанный runtime.

| Раздел | Legacy UI/метод | Новый UI/метод | PK/ключ SQL | Add | Edit | Delete/Withdraw | Когда фиксируется SQL | Когда обновляется родитель | Права | Транзакция | Ошибки/rollback | Тест | Вердикт |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Основная запись | `Equipment_form` / `Equipment.Save` | `EquipmentForm.SaveAsync` / `UpdateAsync` | `КодОборудования` | EquipmentIns | 15 полей | Списание полями | Применить/OK | Save/reload | 43 | UPDATE | P2-06: connectivity пока выглядит not-found | `EquipmentFormTests` | Подтверждён P2-06 |
| Телефонные связи | `gridPhone` / phone diff | `EquipmentPhoneDialog` / `EquipmentPhoneSaveBuilder` | EquipmentId+PhoneId+connection | staged | staged/return | staged | До ATS, затем sync | callback/reload | SQL sync | SQL commit отделён от ATS | P1-02: pending нельзя повторить кнопкой | `SyncFailure_RetriesWithoutRepeatingDatabaseSave` обходит кнопку | Подтверждён P1-02; R01 открыт |
| Учётные записи | `EquipmentLogin_form` / `EquipmentLogin.Save` | `EquipmentAccountDialog` / `UpdateAccountAsync` | EquipmentId+Login | Да | Да | Да | OK диалога | RefreshAccounts | 102 read, 102+43 write | Один UPDATE | P1-01: connectivity → пустой пароль | `EquipmentAccountPersistenceTests` | Подтверждён P1-01 |
| SIM | `SIM_form` / `Sim.Save` | `EquipmentSimDialog` / `EquipmentSimSaveBuilder` | EquipmentId | Да | Да | Да | OK/подтверждение duplicate | RefreshSim | 42 | UPSERT+writeoff duplicate | P1-01: no-row/connectivity → пустые коды | `EquipmentSimPersistenceTests`, `EquipmentSimDialogTests` | Подтверждён P1-01 |
| SIM услуги | `AdditionalService_form` | `EquipmentSimServiceDialog` | Период/услуга по SQL | Да | По правилам | История | OK | RefreshServices | 42 | Builder | Полный runtime не проверен | `EquipmentSimServicePersistenceTests` | Не подтверждено полностью |
| Лицензия | `Soft_form` | `EquipmentLicenseDialog` | EquipmentId/Soft по SQL | Одна | Да | По UI | OK | RefreshLicense | 44 по лицу | Builder | Полный runtime не проверен | Equipment*PersistenceTests | Не подтверждено полностью |
| Компьютер | `Computer_form` | `EquipmentComputerDialog` | EquipmentId+date | Текущий snapshot | По правилам | Нет | OK | RefreshComputer | 43 | Snapshot builder | R04/R06 открыты | `EquipmentComputerPersistenceTests` | Не подтверждено полностью |
| Расположения | `LocationEquipment_form` | `EquipmentLocationDialog` | EquipmentId+От | Да | Последняя | История | OK | RefreshLocations | 43 | С responsible при «На руках» | Отрицательные runtime ещё нужны | `EquipmentLocationPersistenceTests` | Подтверждено по исходникам |
| Ответственные | `ResponsibleEquipment_form` | `EquipmentResponsibleDialog` | EquipmentId+От | Issue | Последний | Withdraw | OK | RefreshResponsibles | 43 | С location при withdraw | Отрицательные runtime ещё нужны | EquipmentFormTests | Подтверждено по исходникам |
| ПО | `InstallSoft` | `EquipmentSoftwareDialog` | EquipmentId+Software+period | Да | Да | Uninstall | OK | RefreshSoftware | 43 | Builder | R06 открыт | EquipmentSoftwarePersistenceTests | Не подтверждено полностью |
| Бухгалтерия | `EquipmentBuxMove_form` | `EquipmentAccountingDialog` | Id/От, latest delete | Да | Последняя | Последняя | OK | RefreshAccounting | 66/68 по лицу | Дочерний save отдельно | R02 baseline и R03 latest delete открыты | EquipmentAccountingPersistenceTests | Риски R02/R03 |
| UPS серверы/тесты | `LinkUps` / Detect*Delta | `EquipmentUpsDialog` / `UpsSaveBuilder` | UPS IDs | Да | Да | Server only; tests не удалять | Save UPS | RefreshUps | P2-05 граница не доказана | Diff transaction | P2-03 no-op UPDATE; P2-04 нет checklist | `EquipmentUpsPersistenceTests` | Подтверждены P2-03/P2-04; P2-05 риск |
| Router | `gridRouter` | Router readonly ClayGrid | EquipmentId | Нет | Нет | Нет | Нет | Load form | Общее чтение | Нет записи | Ошибочный CRUD не найден | EquipmentForm router tests | Подтверждено по исходникам |

## Before-red P1-01

Добавлены connectivity/no-row тесты для account password и SIM secrets. До исправления:
6 failed / 2 passed (10054 уже выбрасывался как не классифицированный connectivity).
Это воспроизводит fail-soft вместо ожидаемого исключения. Секреты в failure messages не
выводятся.

Следующий файл изменений и after-green будут добавлены после завершения P1-01.

## Этап Б — EQ-AUD-P1-01 after-green

Исправлены `EquipmentFormService.GetAccountPasswordAsync` и `GetSimSecretsAsync`:
точечный `RunDbAsync` сохраняет connectivity exception, no-row вызывает отдельный
`InvalidOperationException`, легитимные пустые значения остаются допустимыми.
`EquipmentAccountDialog` и `EquipmentSimDialog` показывают безопасную ошибку, блокируют
OK и имеют guard перед результатом; SQL write при неуспешном read отсутствует.
В DALC добавлен только `InternalsVisibleTo(Kesco.App.Web.Inventory)` для существующего
internal test seam; публичный API/семантика ошибок не изменены.

Изменены: `Services/EquipmentFormService.cs`, оба dialog Razor, `DALC.csproj`, тесты
`EquipmentAccountPersistenceTests`, `EquipmentSimPersistenceTests`; добавлены
`EquipmentAccountDialogTests`, расширен `EquipmentSimDialogTests`.

Результат: 27/27, включая 53/-2/10054, no-row, retry с легитимными пустыми значениями,
Notes без замены пароля, contract без замены SIM-кодов, отключённый OK/Cancel и
CommandLog без UPDATE. Секреты в отчёте отсутствуют.

## Этап Б — EQ-AUD-P1-02 after-green

`EquipmentForm` теперь разделяет `HasUnsavedDraft` и `HasPendingPhoneSync`; IsModified и
CanSave учитывают pending. Cancel/Navigation предупреждают, что SQL уже сохранён и повтор
из формы будет потерян. Refresh использует отдельный текст и сохраняет pending в circuit.
Pending хранит operation-level `PhoneSyncOperation`: первый diff планируется в операции,
после частичного сбоя сохраняются только failed Xorcom/CCM, повтор идёт через
`ExecutePhoneSyncOperationsAsync`; успешные команды не дублируются. При ошибке планирования
сохраняется исходный diff для повторного планирования.

Изменены `EquipmentForm.razor.cs`, `Services/EquipmentFormService.cs`,
`EquipmentFormTests.cs`. Использован существующий `PhoneFormService.SynchronizeAsync`,
новая внешняя АТС не вызывалась.

Before-red: реальный pending Apply был disabled. After-green: 6/6 — SQL save 1 + sync 1,
затем Apply без нового UPDATE + sync 1, partial failure повторяет только failed operation,
Cancel reject/confirm, Refresh и смена id. Отдельно зафиксированы и устранены тестовые
зависания от ожидания `ClickAsync` при открытии confirmation и ограничения fake
NavigationManager. Полный suite на этом этапе не запускался.

## Этап В — UPS after-green

P2-03: `UpsSaveBuilder` теперь обновляет сервер только при смене TurnOffConfigured, тест —
при смене TestedAt/BatteryTime/Servers/Issues. Дубликаты текущих server/test IDs
отклоняются. Удаления тестов нет. Before-red 3/3 failed, после 8/8 builder/persistence.

P2-04: добавлен `Rules/UpsTestServerSelection` и typed ClayGrid в
`EquipmentUpsTestDialog`. Формат БД — comma-separated. Доступны только компьютеры
(подтверждённый `ТипыОборудования.Компьютер=1`) с непустым именем; исторические
нераспознанные имена сохраняются read-only. Неотмеченные серверы видны в warning и
дописываются в Issues без повторов, длина >4000 блокирует OK. `EquipmentUpsDialog`
передаёт текущие Servers в dialog. В SELECT серверов добавлен КодТипаОборудования и
соответствующее свойство read-model. Итоговый UPS profile: 13/13, включая реальные
чекбоксы/OK/Cancel и отсутствие SQL при отмене.

P2-05 остаётся риском, а не доказанным bypass. Read-only SQL показал enabled
`tПодключенияUPS_InsUpdDel`: требует глобальную роль 43 через `fn_ТекущиеРоли()`, без
проверки владельца каждой строки. C# `SaveUpsDetailsAsync` проверяет владельца первого
server ID — это дополнительное ограничение, но не доказывает права всех владельцев.
Legacy menu проверяет владельца основного Equipment. Для multi-owner нужен выбор
владельца; C# authorization не расширялась. Version-based concurrency в контракте
не найдена, новой проверки версий не добавлено.

Временные SQL-выгрузки удаляются после фиксации; схема и triggers не изменялись.

## Этап Г — остальные риски и секции

P2-06: `GetAsync` переведён на strict `RunDbAsync`; `EquipmentForm` отличает SQL/connectivity
от no-row, не ломает circuit и показывает реальный Retry в alert. Ошибка batch секций
обрабатывается тем же путём. Before-red 2 connectivity failed, after 5/5 no-row,
53/-2 и primary/batch retry.

R02: подтверждён stale `_originalSnapshot` после дочернего accounting save без основного
черновика. `RefreshAccountingAsync` теперь пересчитывает snapshot только если черновика
не было; пользовательские конфликтующие поля сохраняются. Before-red dirty=true,
after 2/2 clean/modified cases.

R03: legacy `BuhEquipment.DeleteLastInfo` имеет тот же TOP 1 latest DELETE без version key.
Это унаследованный concurrent-risk, а не новое отклонение; код не менялся.

R04: оба backend write-path (`UpdateAsync` и `UpdateWithPhonesAsync`) перед SQL проверяют
тип/модель, владельца, SN по типу, дату/документ списания и MAC по текущему правилу.
SIM-исключение сохранено через enum 27. Before-red 3 случая, after profile 7/7.
Расширение legacy MAC normalization на произвольные неhex символы не выполнялось —
нужно решение владельца по отклонению от текущего whitelist.

R05: исходники Save проверяют соответствующие роли при каждом вызове; новых C#-правил
для Tel/АТС не добавлено. R06: external SIM detail/WakeOnLan и реальные АТС/сеть/UPS
остаются ограничениями; не вызывались. Компьютерный snapshot использует DateTime.Today,
правила истории не расширены. Остальные дочерние формы проверены по исходникам и
существующим negative/persistence tests, но полной runtime-эквивалентности это не
заявляет.

Объединённый Equipment profile после изменений: 221/221.

## Этап Е — итоговая матрица и протокол

Документ `docs/equipment-form-acceptance-matrix.md` не создавался: текущие правила не
авторизовали отдельное документирование. Итог сохранён в этом handoff.

| Раздел | Legacy UI/метод | Новый UI/метод | PK/ключ SQL | Add | Edit | Delete/Withdraw | Когда фиксируется SQL | Когда обновляется родитель | Права | Транзакция | Ошибки/rollback | Тест | Вердикт |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Основная запись | Equipment.Save | EquipmentForm.SaveAsync | EquipmentId | EquipmentIns | 15 полей | Полями | Apply/OK | Reload/opener | 43 + backend validation | UPDATE/batch phones | Strict read + Retry | FormPersistence/FormTests | Подтверждено; R01 отдельно |
| Телефонные связи | Equipment phone diff | PhoneDialog + operation pending | Equipment+Phone+connection | Staged | Staged | Staged | SQL до ATS | Reload/callback | SQL sync procedures | SQL commit затем ATS | Failed operations only | Pending 6/6 + PhonePersistence | Подтверждено с отклонением R01 |
| Учётные записи | EquipmentLogin_form | AccountDialog | EquipmentId+Login | Да | Да | Да | OK | RefreshAccounts | 102/43 | Single write | Read fail closed, no write on error | Account 12/12 в общем профиле | Подтверждено |
| SIM | SIM_form | SimDialog | EquipmentId | Да | Да | Да | OK/duplicate confirm | RefreshSim | 42 | Atomic duplicate+save | Secrets fail closed | Sim tests | Подтверждено по коду/fake; реальные коды не использовались |
| SIM услуги | AdditionalService_form | SimServiceDialog | Период/услуга | Да | По правилам | История | OK | RefreshServices | 42 | Builder | Existing negative paths | SimServicePersistence | Подтверждено с ограничением runtime |
| Лицензия | Soft_form | LicenseDialog | EquipmentId/Soft | Одна | Да | По UI | OK | RefreshLicense | 44 owner/tenant | Builder | Existing negative paths | License persistence | Подтверждено с ограничением runtime |
| Компьютер | Computer_form | ComputerDialog | EquipmentId+Today | Текущий snapshot | Текущий | Нет | OK | RefreshComputer | 43 | Snapshot | Validation/errors | Computer tests | Подтверждено с ограничением runtime |
| Расположения | LocationEquipment_form | LocationDialog | EquipmentId+От | Да | Последняя | История | OK | RefreshLocations | 43 | С responsible | SQL errors не меняют row | Location tests | Подтверждено |
| Ответственные | ResponsibleEquipment_form | ResponsibleDialog | EquipmentId+От | Issue | Последний | Withdraw | OK | Refresh | 43 | С location | SQL exception сохраняет displayed row | Form tests | Подтверждено |
| ПО | InstallSoft | SoftwareDialog | Equipment+Software+period | Да | Да | Uninstall | OK | RefreshSoftware | 43 | Builder | Existing validation | Software tests | Подтверждено с ограничением runtime |
| Бухгалтерия | EquipmentBuxMove_form | AccountingDialog | Id/latest От | Да | Последняя | Legacy latest | OK | RefreshAccounting | 66/68 по лицу | Дочерний отдельно | Snapshot fixed; R03 legacy risk | Accounting 2/2 + persistence | Подтверждено с R03 риском |
| UPS серверы/тесты | LinkUps Detect*Delta | UpsDialog/TestDialog | UPS+server/test IDs | Да | Да | Server; tests нет | OK | RefreshUps | P2-05 открыт | Minimal diff transaction | No-op UPDATE устранён | UPS 13/13 | Подтверждено; P2-05 владельцу |
| Router | gridRouter | Readonly ClayGrid | EquipmentId | Нет | Нет | Нет | Нет | Load | Read | Нет | Ошибочный CRUD не найден | Router tests | Подтверждено |

### Изменённые файлы аудита

- Inventory: `Services/EquipmentFormService.cs`; `EquipmentForm.razor(.cs)`;
  `Details/EquipmentAccountDialog.razor`, `EquipmentSimDialog.razor`,
  `EquipmentUpsDialog.razor`, `EquipmentUpsTestDialog.razor`; новый
  `Rules/UpsTestServerSelection.cs`.
- Entities: `Inventory/UpsSaveBuilder.cs`, `Inventory/UpsServerLink.cs`,
  `SQLQueries.cs`, `DatabaseEnums.cs`.
- DALC: только `Clayzor.Lib.DALC.csproj` (InternalsVisibleTo Inventory для существующего seam).
- Tests: изменены EquipmentFormTests, Form/Account/Sim/Ups persistence и SimDialogTests;
  добавлены EquipmentAccountDialogTests, EquipmentFormReadPersistenceTests,
  EquipmentUpsTestDialogTests, UpsTestServerSelectionTests.
- Остальные modified files в git относятся к предыдущим этапам menu/grid/theme/placeholders,
  не к этому аудиту.

### Проверки и протокол

- `dotnet build Clayzor.sln --no-restore`: 0 ошибок, 35 прежних warnings.
- Equipment + selection profile: 221/221. Pending focused rerun после concurrency fix: 6/6.
- Full run: 822 passed, один Cancel test упал на изменяемой JS collection и затем был
  исправлен/повторно пройден; после 822 известный `ClayTreeMutationAbortTests.AddChild_`
  `SqlException_DoesNotRunSuccessPath` завис на 60s hang-timeout. Полный suite не зелёный;
  Tree код не менялся. Созданный этим запуском TestResults GUID удалён; более старый
  pre-existing TestResults оставлен.
- Browser EquipmentForm №15: загружена, loadError/notFound=false, 3 видимых grid;
  320/600/900/1155/1440 без document overflow, toolbar/footer видимы. UPS quick dialog
  открыт и отменён без Save. Вкладка закрыта. SQL write/external devices не использовались.
- Read-only SQL: UPS trigger enabled и требует глобальную роль 43; DisabledTriggers=0.
  Схема/триггеры/справочники не изменялись, восстановление данных не потребовалось.
- Inventory/Users запущены: `bash-anwsalm3`, `bash-p3l49gdc`.

### Оставшиеся решения владельца / ограничения

1. R01: legacy выполняет ATS внутри TransactionScope и при ошибке не вызывает Complete;
   Clayzor осознанно использует SQL commit → ATS → circuit-local retry. Это eventual
   consistency, не эквивалентная атомарность. Подтвердить допустимость; durable outbox не
   добавлять без согласования.
2. P2-05: выбрать правильную область прав для multi-owner UPS. SQL trigger проверяет только
   глобальную 43; текущий C# проверяет владельца первого server, legacy UI — основного
   Equipment. Не выбирать silently.
3. R03: latest accounting DELETE совпадает с legacy, но при параллельной новой записи может
   удалить её. Решить, нужен ли ключ версии/Id, отдельным изменением.
4. R04: текущий MAC whitelist строже legacy удаления всех неhex. Подтвердить целевое
   поведение для нестандартных разделителей.
5. WakeOnLan/SIM detail/внешние URI и реальные АТС/сеть/UPS остаются непроверенными или
   отложенными. Полная parity и итоговая приёмка не заявляются.

Commit/push/merge не выполнялись.

## Дополнительная UI-проверка — EquipmentAccountingDialog

Дата и документ больше не являются независимыми grid-элементами, попадающими в неявную
узкую строку. В Razor добавлены контейнеры `.equipment-accounting-date-document` для обеих
пар; CSS задаёт date 160–220px + document minmax(0,1fr), до 600px — одну колонку.
Общий `ShowDialogAsync` получил необязательный DialogOptions; только accounting dialog
использует `MaxWidth.Medium + FullWidth`.

Проверено на реальной EquipmentForm №8: dialog=960px, каждый document select=536px,
placeholder читается полностью. Viewport 480px: dialog=416px, обе пары по одной колонке,
document overflow=false. Dialog отменён без SQL, screenshot удалён, вкладка закрыта.
`dotnet build Inventory --no-restore`: 0 ошибок, 15 прежних warnings. Inventory/Users:
`bash-xs85vi9x`, `bash-p3l49gdc`.
