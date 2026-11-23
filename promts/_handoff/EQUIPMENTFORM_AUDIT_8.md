# EquipmentForm — протокол EQ8

Дата: 24 ноября 2026 года. Работа выполнена в `main`, merge не выполнялся. Внешние Windows/SQL multiuser, IIS, физические АТС/UPS/ICMP/WOL, billing и SQL2008R2 проверки относятся к beta и не выдаются за локальный PASS.

## 1. База и публикация

Исходные HEAD, все ветки `main`, рабочие деревья чистые:

| Репозиторий | Исходный SHA | Итоговый SHA / публикация |
|---|---|---|
| Clayzor | `5d0aa1b5adce7c48ee6d7f05370f2dc2bdc409a3` | `HEAD` коммита этого отчёта; `main`; pushed |
| Inventory | `b1c5bfcd4317612bf2ff29cc029c354baf72620e` | `0b15e687b083e125907571ee4114aa3cd94f895f`; `main`; pushed |
| Entities | `c5ee73436a668bf10ef2da5691ffa3d1e24143e8` | `97ac84991b725fd638c2a68c14ae31e3e110051f`; `main`; pushed |
| Controls | `d5b7a057dc078cd907dd8b4082808bb8cad6577d` | `6590b78ae8165fed4fd7225e136bd79ed3e8611d`; `main`; pushed |
| DBSelect | `9b8551dab6228ebcaff7506110c71fddff5b6f03` | `e97796ecf5fe05f18b41a52e257abc544f555b80`; `main`; pushed |
| Tests | `46ea7bc01ac35f1ed068e3963118c30bb68c8031` | `f381004dfa6d7fc3bde71135a74f341a277ea33f`; `main`; pushed |

DALC не изменялся (`4491634cb87272b24ac5b3e90e9fc43e1648963d`). R01 и R04 не выбирались за владельца. Новых бизнес-таблиц, процедур и trigger-off обходов нет.

## 2. EQ8-01…EQ8-15

| Блок | Изменения | Локальное доказательство | Оставшееся ограничение |
|---|---|---|---|
| EQ8-01 | `ICurrentUserPrincipalProvider`: HTTP использует authenticated `HttpContext.User`, circuit — инициализированный AuthenticationStateProvider. PhoneConfig endpoint использует production DbManager и HTTP adapter | Настоящий Kestrel pipeline + controlled auth: allow/no role/mismatch/no identity; ноль procedure calls при отказе | Реальные Negotiate/delegation — beta |
| EQ8-02 | Employee владеет trusted identity, UX-снимком effective roles и ANY/global/person scopes. UI использует `CurrentUser.HasRole`; write guards свежие. global102 accounts, global103 network, owner/global UPS/license/accounting. Trusted identity добавлена в accounting delete, UPS details, SIM detail | `Eq8EmployeeRoleScope`, EmployeeAuthorization, account/network/SIM/UPS profiles | Windows/SQL multiuser, замещения и delegation — beta |
| EQ8-03 | Каждый ATS retry проверяет роль, серверный diff и актуальные связи; deleted phone не обязан оставаться привязанным. Добавлена явная команда «Настроить оборудование» без UPDATE | Revoked/foreign/changed target = ноль ATS; allowed failed-only retry; dirty/saved UI | Физические Xorcom/CCM/Meridian и R01 — beta/решение |
| EQ8-04 | Общий `EquipmentEditorDialogBase<T>` удерживает editor и DTO до результата. SIM secret fields блокированы до чтения; same ContractId, Fill и late responses защищены ревизиями. Mandatory reads не превращаются в empty | Настоящие OK-кнопки bUnit, видимый ввод после отказа; Computer retry; SIM/UPS/Location/Responsible/Accounting tests | Полная browser-матрица каждого child — beta |
| EQ8-05 | `Rejected/Unknown/Committed/RefreshFailed` для main/child/delete/network/UPS. После commit retry повторяет только read; unknown хранится отдельно и не разрешает replay | Accounting write=1 + failed refresh + read-only retry; UPS/network outcomes; DR-01 operation API | Потерянный ответ после реального commit — beta |
| EQ8-06 | Main и phone diff сравнивают исходный `ChangedAtUtc`; phone rollback до ATS. История использует точный `От`, affected=1; Responsible edit не восстанавливает stale `До`. UPS same-row original guards. Трёхсторонний merge после child commit сохраняет dirty | V1 A/B, .500, affected 0/2, rollback, phone order, UPS concurrency | Реальные locks/triggers/two sessions — beta |
| EQ8-07 | Phone exclusions до ORDER BY; OR-группа equipment lookup. ПДн: CTE активных технических тарифов, фактический тип заказчика, strict reads | Real data source SQL для 0/1/N/duplicates, параметры, CTE, SQL error vs empty | Целевая схема/тарифы SQL2008R2 — beta |
| EQ8-08 | Location/Responsible/Computer/SIM/UPS используют browser-local now; UTC raw keys сохранены. SIM activation Now и UPS AddTest исправлены | Unit/bUnit + browser UTC+3; date-only не сдвигается | Реальные DST/zones/IIS — beta |
| EQ8-09 | Catalog/custom услуга, оба пустых запрещены; старый custom editable. SIM detail свежо проверяет Билайн, responsible и доступ | Domain custom tests, operator row/URL/VAT matrix | Внешний billing — beta |
| EQ8-10 | Sticky Computer error устранена. Общий `ValidateConfiguration(today,memoryBanks)` вызывается UI и Service; фактическая модель и `БанковПамяти` загружаются сервером | 2 banks→3→delete→Apply, config pairs/future, direct invalid no DML | DB constraints — beta |
| EQ8-11 | UPS raw ServerName отделён от EquipmentName; unnamed fallback не адресат. Computer-only shutdown UI+DB guard, lookup=save filter, historical unknown сохранены, audit добавлен | UPS data/concurrency/dialog tests | Физический UPS и реальная инфраструктура — beta |
| EQ8-12 | Show/hide password global102 без43; activation Now; license Description; overdue return; workplace advisory; UPS audit; multiline notes; явная деинсталляция | UI bUnit/DOM, query-contract advisory/license, secret clear behavior | Rich contact preview — общая зависимость; touch/mobile beta |
| EQ8-13 | ModelId→фактический тип до validation; MAC normalization после whitelist; Computer actual context; Responsible next/current rules; negative license; child actual-type guards | 14 server guard + 16 child guard + domain tests; реальные Dapper params и ноль DML при отказе | SQL races/constraints — beta |
| EQ8-14 | Общий `ClayDraftService` + IndexedDB/AES-GCM `clayDraftStore`: raw last input, typed baseline, user/form/entity/tab scope, Locks/BroadcastChannel, revisions, running/unknown operation, schema1→2 backup. Reconnect canReload guard. Dynamic HD/SSD и nested UPS restore | C# Draft/Reconnect 7 tests; Chrome reload, реальный process restart, deployment schema1→2, Location child restore, Discard count=0 | Полный IIS/network/deployment matrix, все nested/dynamic variants — beta |
| EQ8-15 | Добавлены regression tests, docs и AGENTS; существующие Net/Select/Grid/Date consumers сохранены | Solution build 0 errors; общий suite 1112/1112; browser smoke | Независимая приёмка владельца |

## 3. Полная карта сценарной матрицы

Статусы: **С** — сохранено/регрессионно проверено; **И** — исправлено в EQ8; **О** — обоснованное отличие; **B** — beta; **З** — зависимость/решение.

- **M01–M23:** M01 С; M02 О; M03 И; M04–M17 С/И (M05/M07/M09/M13/M14/M17/M18 усилены server guards, draft и concurrency); M19–M21 С; M22 И (локально, завершение beta); M23 О.
- **T01–T39:** T01–T20 С, включая atomic diff, Xorcom priority и failed-only retry; T21 И; T22 З (R01); T23 И; T24–T25 И; T26–T32 И (global scope, show secret, editor lifecycle); T33 И; T34–T39 С/И (actual owner/type, trusted identity, outcomes, navigation).
- **H01–H52:** H01–H02 С; H03 B; H04–H06 И; H07 О; H08–H09 С/B; H10–H12 С/И; H13 И; H14 С; H15–H17 И; H18 О; H19–H26 С/И (strict identity, draft/outcomes, atomic accounting); H27–H31 И; H32–H37 С/И (multiline и явная деинсталляция); H38–H41 И; H42–H52 С/И (UPS data, applicability, audit, concurrency); реальные DB effects B.
- **S01–S33:** S01–S06 С/И; S07 И; S08–S12 И; S13–S16 С/И; S17 С (подтверждённые links, иначе plain text); S18–S23 С/И; S24 О; S25–S28 С; S29 И; S30–S33 С/B.
- **N01–N19:** N01–N11 С/И; N12 З (Ping не реализован); N13 З (WOL не реализован); N14–N18 С/И; N19 локально 320/480 без horizontal overflow, полная touch/browser matrix B.
- **A01–A16:** A01–A06 И; A07 И; A08 И; A09–A15 И (trusted identity, owner/global/type guards, fresh ATS boundary); A16 С — blanket role43 для чтения не добавлен.
- **U01–U06:** U01–U04 многострочность восстановлена; существующие значения не обрезаются. U05–U06 оставлены по подтверждённым текущим SQL params; фактическая схема/лимиты B, не заявляется произошедшее усечение.
- **R01/R04:** открытые решения владельца. **CL-09/CL-11:** rich contact provider и предметная collaboration не выдаются за реализованные; optimistic concurrency самостоятельно закрыт. **Ping/WOL:** код отсутствует, это отдельные зависимости общей полноты.

## 4. Сохранённые улучшения и consumers

Сохранены atomic Equipment+phone diff, Xorcom/CCM recipients и приоритет, failed-only retry без UPDATE, separate secret reads, SIM duplicate transaction, exact accounting guards, original/current UPS diff, static ClayGrid sections, compact controls, draggable dialogs, Net/ClaySelect/Date/Time behavior. Process-static drag state заменён per-grid owner; default `ClayGridOptions.Id` не изменён. Select сохраняет no-autoselect по кнопке, hover/keyboard separation, ReadOnly/Disabled ViewItem ban, Escape backup и typed ID.

## 5. Команды и фактические результаты

```bash
dotnet build Clayzor.sln --no-restore --verbosity:minimal --consoleLoggerParameters:ErrorsOnly
```

Успешно, **0 ошибок, 32 предупреждения** в финальном прогоне.

```bash
dotnet test tests/Clayzor.Lib.Web.Controls.Tests/Clayzor.Lib.Web.Controls.Tests.csproj --no-build --filter 'FullyQualifiedName!~ClayTreeMutationAbortTests' --verbosity:minimal --consoleLoggerParameters:ErrorsOnly
```

**1112/1112 passed**, 0 skipped. Единственное исключение — ранее документированное несвязанное зависание `ClayTreeMutationAbortTests`; ClayTree не менялся. Новых исключений нет.

Профильные независимые пакеты: SQL/history/ПДн 67/67; concurrency main 22/22; domain validation 26/26; server guards 14/14; child guards 16/16; UPS before-red 10/12 failed → после реализации covered общим suite; DR-01 C# 7/7. Итоговый объединённый Equipment/UPS/Employee/EQ8 профиль — **468/468 passed**.

## 6. Browser и тестовые данные

Chrome CDP, Equipment ID 8, исходный SN `CLAY_TEST_COMPUTER`:

1. Ввод `CLAY_TEST_DR01_LAST` только через `input`, без blur/Save → один encrypted draft.
2. Full reload → значение и «Восстановлен несохранённый черновик».
3. Полный `dotnet` process restart → значение восстановлено.
4. Deployment schema 1→2 → значение восстановлено, `schema1-backup` создан.
5. Новый Location editor + `DR01 child notes без blur` → process restart → editor и текст восстановлены, DML не вызывался.
6. Явные Отмена/Да → draft keys count=0, включая backup; reload показывает исходный SN.
7. 320 px и 480 px: `scrollWidth == viewport`, горизонтального overflow нет.

Новых SQL test fixtures и реальных DML EQ8 не выполнялось. Старые обзорные `CLAY_TEST_*` записи не изменялись. Созданный браузерный draft удалён. Временные artifacts не входят в поставку.

## 7. Beta checklist

- Две реальные Windows/SQL identities, circuits, global/local/ANY, substitutions, revoke/delegation.
- SQL Server 2008 R2, активные triggers, fractional datetime, locks, concurrent writes, document/owner guards.
- Lost response после фактического commit, reconnect и read-only recovery.
- Полный IIS recycle/network loss/deployment matrix: последний символ, invalid/raw, все child/nested/dynamic editors, два пользователя/вкладки, quota/encryption/migration failures.
- Физические АТС, Xorcom/CCM/Meridian retry/configure и решение R01.
- Внешний SIM billing и `URI_accounting_phone_form`.
- UPS/Ping/WOL физические эффекты; Ping/WOL сначала требуют собственной реализации.
- Browser popup/opener, DST/zones, touch/keyboard, 320–480px, rich contacts и eligible collaboration.

## 8. Честный статус

Доказанные локальные дефекты EQ8 закрыты и проверены; внешние эффекты и beta-матрица не выдаются за выполненные. Общая полнота EquipmentForm ограничена отдельными Ping/WOL, rich-contact/collaboration зависимостями и решениями R01/R04. Независимая приёмка владельца не подменяется этим отчётом.
