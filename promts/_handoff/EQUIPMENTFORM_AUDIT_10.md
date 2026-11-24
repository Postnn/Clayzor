# EquipmentForm EQ10 — реализация и локальная приёмка

Статус: исправления и локальные проверки завершены. Публикация в `main` выполняется отдельными коммитами сабмодулей и суперпроекта. Внешняя beta и независимое пользовательское ревью не заявляются.

## Исходная версия

- Superproject: `dea7746576e06ce0fcf186b823372dec1fd4de90`
- Inventory: `01b2f41f9566f9146091144771341ed79df176cf`
- Entities: `95ef0cfa4532afe338af9434691ec527cae9a071`
- Controls: `b3616aaa4e2eda8852d7b84208d7cff842f23ab1`
- Tests: `d0f334776bad2adc7fd7a0fdb0ce0f806fb07b02`
- DBSelect: `e97796ecf5fe05f18b41a52e257abc544f555b80`
- DALC: `4491634cb87272b24ac5b3e90e9fc43e1648963d`

Вход: `promts/EquipmentForm_9/EquipmentForm_10.md`, независимые findings F01–F14. Работа разделена между Controls draft-store, domain/service, child editors и координатором. Merge/rebase/reset force-push не выполнялись. Новые темы, required styling, Net и чужие входные пакеты не откатывались.

## Production-контракты

- **Editor identity:** `EquipmentEditorOpening` фиксирует model reference, EquipmentId, operation version и generation до первого await. Computer/SIM/Location/Responsible/SIM service проверяют контекст перед ShowDialog и dispatch. DTO не перепривязывается к текущему `_model.Id`.
- **Main merge:** явный набор editable fields; LocationId/EmployeeId и их описания — derived. Общий base/local/fresh merge применяется child refresh и durable restore, включая writeoff local/UTC. Разрешение конфликта сохраняет либо local, либо только fresh-conflicted fields, не уничтожая остальной draft.
- **Draft ownership/readiness:** уникальный root-selector формы и класс её dialog, scoped raw restore/readiness, C# rejection cleanup. Discard удаляет editable values, но сохраняет running/unknown operation-only snapshot. Parent autosave coalescing не теряет последнюю ревизию.
- **Child editors:** typed revision autosave через `EquipmentEditorContext`; Account/SIM secrets восстанавливаются только после fresh successful readiness. `EquipmentPeriodOriginal` хранит original key/version/existence/active. Fresh V2/missing/closed original не становится baseline и не вызывает INSERT.
- **Attempts/outcomes:** main intent содержит expected/after snapshots и read-only сверку. `PhoneSyncAttempt` хранит per-operation Succeeded/Failed/NotSent/Rejected/Unknown. Configure и retry используют trusted intent, origin и scope; mixed retry выполняет только CanRetry и не повторяет main UPDATE.
- **Phases:** Computer использует phased write. Новые пути используют `EquipmentOperationException.Phase`, а не номер SqlException. Committed и RefreshFailed различаются; повтор подтверждённой записи запрещён.
- **Calendar:** Computer и SIM service получают business Today перед validation и write. Form data/SIM/advisory используют `@BusinessToday`. UPS raw instant и `.500` не изменены.

## F01–F14

| Finding | Production-изменение и фактическая проверка | Статус |
|---|---|---|
| F01 | Immutable opening snapshot и post-await guards. `ComputerOpen_DeferredTimeAfterNavigation_DoesNotOpenOrSave`, `ComputerSave_DeferredTimeAfterNavigation_DoesNotDispatch`, `ComputerSave_ActualCallbackAwaitsWriteAndObservesFault`, old Account editor test. | PASS local |
| F02 | Editable/derived merge и общий restore. `ChildRefresh_DerivedLocationAndResponsible_MergeWithoutConflict` (A→B и A→null), independent/conflicting restore и selective resolution. | PASS local |
| F03 | Подключённый typed autosave с revisions/coalescing и tombstones Computer collections. `Eq10EditorLifecycleTests`, draft tests и production browser store. | PASS local |
| F04 | Immutable `EquipmentPeriodOriginal` для Location/Responsible, V1/V2/missing/closed и actual Responsible save. `Eq10EditorBaselineCalendarTests`, Responsible SQL failure test. | PASS local |
| F05 | Unknown переживает Discard/navigation/re-register; operation-only draft и read-only reconciliation. `Eq10Draft*`, `ReconcileDraftOperation_*`, mixed PhoneSyncAttempt tests. | PASS local |
| F06 | Atomic Save подтверждает before/after и old/new targets; forged caller original отклоняется. `Eq10DomainPhoneTests`, `EquipmentPhonePersistenceTests`. | PASS local |
| F07 | Explicit Configure origin + owner/global43/IP-phone на каждом retry; ANY43 для обычного main сохранён. Domain Configure/revoke/owner/type tests. | PASS local |
| F08 | Atomic expected active, direct date/equipment validation, exact CAS и required ToUtc. `Eq10DomainCompositeTests` и form location tests. | PASS local |
| F09 | Account raw secret только после fresh successful access; denied/pending/failed сохраняет protected draft и блокирует OK. `AccountDeniedThenRetry_KeepsProtectedRawUntilAllowed`. | PASS local |
| F10 | SIM typed restore different ContractId/Pdn/source, оба completion orders; destructive reset только user-change. `SimRestore_BothCompletionOrdersPreserveContractAndPerson`. | PASS local |
| F11 | Business Today до validation/write и в read filters/indicators; restored old Today не используется. `Eq10DomainPhaseCalendarTests`, `Eq10EditorBaselineCalendarTests`, UPS tests. | PASS local |
| F12 | NotSent/Rejected до dispatch, Unknown после dispatch, Committed/RefreshFailed. `Eq10DomainPhaseCalendarTests`, `PhoneFormService` и Computer phased tests. | PASS local |
| F13 | Guarded initial advisory render и одинаковые stale success/catch predicates. `SimInitialAdvisory_CompletionRendersWarning`, `SimFilters_LateFaultFromA_DoesNotOverwriteSuccessfulB`, `SimFillFault_AfterManualSelectionIsIgnored`. | PASS local |
| F14 | Root/portal registration isolation и cleanup после C# rejection с сохранением durable bytes. `Eq10DraftServiceTests` и пять real-browser production scenarios. | PASS local |

Локальные PASS не заменяют beta: реальные Windows/SQL multiuser, IIS recycle/network/deploy, SQL Server 2008 R2 locks/triggers, физические ATS/UPS/ICMP/WOL, billing и полная DST/touch/mobile матрица не исполнялись. R01/R04 не выбирались. После recreate service/server context retry без подтверждённого trusted intent остаётся fail-closed; универсальное recovery не заявляется.

## Команды и результаты

- `dotnet build Clayzor.sln --no-restore` — **0 ошибок**.
- `dotnet build tests/Clayzor.ThemeStudio/Clayzor.ThemeStudio.csproj --no-restore` — **0 ошибок**.
- Полный доступный suite `FullyQualifiedName!~ClayTreeMutationAbortTests` — **1247/1247 passed, 0 failed, 0 skipped**. Прежнее несвязанное зависание `ClayTreeMutationAbortTests` не запускалось; новых исключений не добавлено.
- Coordinator F01/F02/reconcile/SIM filters — **13/13**.
- `Eq10Draft` — **8/8**; `Eq10Domain` — **33/33**; editor classes — **2/2 + 6/6 + 5/5**.
- EquipmentForm + phone/computer/SIM persistence — **124/124**.
- `EquipmentPhonePersistenceTests` — **10/10**.
- Два SIM advisory fixtures и все пять production browser draft scenarios — **7/7**.
- Browser EquipmentForm `id=8`: восстановлена пользовательская `corporate:light`; Computer открыт, textarea изменён, Cancel без OK — **PASS**, ошибок 0, уникальные draft owner/dialog class подтверждены. Бизнес-записи не выполнялись.

Миграция/backup, corrupt/unsupported data, storage health, atomic key, accepted scopes+revision, release/retry и shared consumer regression входят в общий 1247-тестовый набор. `AGENTS.md` уточняет: explicit Discard удаляет editable values/backup, но не running/unknown business attempt.

Матрица всех 188 сценариев: `promts/_handoff/EquipmentForm_Legacy_Clayzor_Matrix_EQ10.md`. Она отделяет actual-executed, source-reviewed, inherited/not-rerun, beta, owner-decision, dependency и justified differences.

## Публикация

Опубликованы в `main`:

| Репозиторий | SHA |
|---|---|
| Controls | `05f4550bba2cebb586d53ef5c2fe04405acc78cb` |
| Entities | `520ad732a3b0b83faa92783d38ae6bc1ae19e57e` |
| Inventory | `3e336ee8df8d99d405c024f324df8f9b46b49fb8` |
| Tests | `ee7f6a8f101b09ab28722ba18782e92f07b15a27` |

Суперпроект фиксируется после матрицы 188 сценариев. Inventory `5020`, Users `5030` и Styles `5040` запущены на проверенных бинарниках. Временные test/browser artifacts вне репозитория не публикуются.
