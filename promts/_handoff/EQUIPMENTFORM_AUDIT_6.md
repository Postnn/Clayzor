# EquipmentForm_6 — исправления независимого ревью

## Статус и границы

Исправлены EQ6-P1-01 и EQ6-P1-02; семь исправлений EQ5 сохранены. Добавлены
before-red/after-green тесты, bUnit-проверки UI и реальные SQL/browser-проверки.
Изменения и ссылки сабмодулей опубликованы в `main`; merge не выполнялся.

Это готовность к повторной независимой проверке, а не объявление полной приёмки
EquipmentForm. R01, R04, внешние интеграции и проверка именно на SQL Server 2008 R2
остаются открытыми ограничениями.

## Исходные и итоговые ревизии

| Репозиторий | Исходный SHA | Итоговый SHA / состояние |
|---|---|---|
| Суперпроект | `af49979a7361322509371d9a3066974223952746` | коммит публикации этого handoff и ссылок в `main` |
| Inventory | `7709310e240ac7c9653645c585a844cfbccf9486` | `cae01d812e080ff684635467c3dde79f4f6f2f29` (`origin/main`) |
| Entities | `ce2be459a2531a544e72900f2f5104dd8765f9b2` | `f392a67b59afd19397eec75fa31db10d55dcf34f` (`origin/main`) |
| Tests | `0b91b95e9d7b0cd22443c2e9430a24c085381a31` | `e195f1b9b854542c12c9272f1a6d65349a84d99e` (`origin/main`) |
| DALC | `a02383b4ceb7fbba93223f95f9a66895ed28ec63` | не изменялся |

Detached HEAD не было. Посторонние untracked-файлы `promts/` не изменялись.

## Матрица дефектов

| Issue | Точная причина и before-red | Исправление | After-green и реальная проверка | Статус |
|---|---|---|---|---|
| EQ6-P1-01 | Оба write-path вызывали read `ExecuteScalarAsync<int>`: connectivity могла вернуть `default(int)=0`; `NULL` и неизвестный статус также достигали конца метода. Before-red: 6 тестов no-row/unknown status и accounting SQL 53 завершились без ожидаемого исключения. UPS SQL 10054 выбрасывался уже на старом пути; mutation с read-методом для UPS SQL 53 дал red (`InvalidOperationException` вместо `SqlException`). | `EquipmentAccounting.DeleteLatestAsync` и `UpsServerLink.SaveDetailsAsync` переведены на `ExecuteWriteScalarAsync<int?>`. Явно принимаются только `0,1,2,3`; `0` — успех, прежние `1/2/3` сохраняют исключения, `NULL` и прочие значения дают `InvalidOperationException`. | Все 9 connectivity/no-row/unknown-status кейсов зелёные; отдельные bUnit-тесты подтверждают отсутствие success-refresh и сохранение строки/черновика. SQL `SqlException` 53/10054 достигает UI catch. | Исправлено |
| EQ6-P1-02 | Batch бухучёта содержал session-wide `SET TRANSACTION ISOLATION LEVEL SERIALIZABLE` и `SET XACT_ABORT ON`. Before-red: тест обнаружил оба `SET`. | Оба session-wide `SET` удалены. Транзакция и `SELECT ... WITH (UPDLOCK, HOLDLOCK)` сохранены; `HOLDLOCK` даёт необходимую serializable-семантику выборки только на время транзакции. TRY/CATCH и rollback сохранены. | На одном открытом соединении после статусов `0/1/2/3` и внесённого `RAISERROR`: isolation остаётся `read committed`, `XACT_ABORT=0`, `@@TRANCOUNT=0`. Повторно открытое pooled connection имеет те же значения. | Исправлено |
| EQ6-P2 display | Новый сервер без `СетевоеИмя` отображался пустой строкой до reload; существующий SQL имел fallback на тип и модель. | Табличная колонка показывает `EquipmentName`, только если `ServerName` пуст. Само сетевое имя и правила выбора уведомлений не изменены. | bUnit: после реального `AddServer` видно «Компьютер без имени», `ServerName` остаётся пустым, `EquipmentName` заполнен. | Исправлено |

## Fail-closed write-path и UX

- `DbManager.ExecuteWriteScalarAsync` уже имел необходимый контракт и используется без
  изменений DALC: любой `SqlException`, включая connectivity, выбрасывается наружу.
- Тип статуса именно `int?`, поскольку `ExecuteScalarAsync<int>` преобразует отсутствие
  результата в `0` ещё до проверки вызывающим кодом.
- При SQL-ошибке родительская бухгалтерская коллекция не обновляется как после успешного
  удаления; список UPS также не обновляется по success-path.
- Текст SQL catch в обоих сценариях: результат операции неизвестен, нужно обновить запись
  и проверить данные. Не утверждается ни успех, ни гарантированный rollback при потере
  ответа после возможного COMMIT.
- Ошибка на стадии записи и ошибка после записи при потере ответа не различаются по одному
  `SqlException`; поэтому безопасный UX одинаковый. Доказательство rollback при обрыве
  после COMMIT намеренно не заявляется.

## Реальный SQL-протокол

Стенд: `localhost`, база `Инвентаризация`, `SERVERPROPERTY('ProductVersion')` =
**`16.0.1220.3`**, `ProductLevel=RTM` (SQL Server 2022). Это не SQL Server 2008 R2.
Использованы только конструкции, доступные в 2008 R2, но фактический прогон на 2008 R2
не выполнен из-за отсутствия такого сервера.

Проверка выполнялась через `System.Data.SqlClient`, одно открытое соединение и отдельную
вторую SQL-сессию. Batch соответствовал обновлённой константе удаления. Триггеры не
отключались.

```text
initial|isolation=read committed|xact_abort=0|trancount=0
after-status-1|isolation=read committed|xact_abort=0|trancount=0
after-status-3|isolation=read committed|xact_abort=0|trancount=0
after-status-2-two-session|isolation=read committed|xact_abort=0|trancount=0
two-session-stale|status=2|rows=2
after-status-0|isolation=read committed|xact_abort=0|trancount=0
latest-delete|status=0|old_rows=1
sql-error|raised=true
after-sql-error|isolation=read committed|xact_abort=0|trancount=0
cleanup|remaining=0
pooled-reuse|isolation=read committed|xact_abort=0|trancount=0
disabled-triggers=0
test-rows=0
```

Условия статусов:

- `1` — отсутствие истории у изолированного тестового ID;
- `3` — тестовая запись на оборудовании 7 и параметры несуществующих ролей `-66/-68`,
  что проверяет отказ авторизации без изменения реальных ролей;
- `2` — на оборудовании 8 первая сессия держала подтверждённый старый ID, вторая закрыла
  его период и добавила более новую запись; удаление старого ID отклонено, обе строки
  остались;
- `0` — после этого удалена именно актуальная последняя строка, старая сохранилась;
- SQL-ошибка — контролируемый `RAISERROR(...,16,1)` внутри TRY копии того же batch;
  выполнен CATCH/ROLLBACK, открытых транзакций нет.

Отдельный одновременный stress-test потоков не проводился. Доказана требуемая реальная
гонка двух сессий «открыл старое → другая сессия добавила → подтвердил старое».

## Browser и очистка

На обновлённой сборке Inventory открыта `/Equipment/EquipmentForm?id=8`:
заголовок `[8] Компьютер HP Vectra VL5`, SN `CLAY_TEST_COMPUTER`, ошибок в alert нет.
Через реальную форму подтверждено удаление тестовой бухгалтерской записи ID 20
`CLAY_TEST_EQ6_UI`; после success-refresh секция показывает «Нет записей бухгалтерского
учёта», SQL count равен 0. Это проверяет настоящий `DbManager`/write-path, а не только
скриптованный тест.

Все записи `CLAY_TEST_EQ6%` и `CLAY_TEST_EQ6_UI` удалены. Финальный контроль:
`test-rows=0`, `disabled_triggers=0`. Временный SQL verification script удалён.
Внешние АТС, UPS, сеть и Wake-on-LAN не вызывались.

## Сборка и тесты

- До исправления: целевой запуск persistence — **8 failed / 26 passed / 34 total**.
  Падения соответствуют accounting SQL 53, шести no-row/unknown status и session-wide SET.
  UPS SQL 10054 не был red на старом пути; отдельный mutation с read-методом и SQL 53 —
  **1 failed / 1 passed**, как и ожидалось для двух разных кодов.
- После исправления: исходные persistence-классы — **34/34 passed**; после добавления
  второго connectivity-кейса весь Equipment-профиль повторён ниже.
- Новые bUnit connectivity UI — **2/2 passed**.
- `dotnet test ... --filter 'FullyQualifiedName~Equipment|FullyQualifiedName~UpsTestServerSelection'`
  — **252/252 passed**.
- `dotnet build Clayzor.sln --no-restore --verbosity:minimal --consoleLoggerParameters:ErrorsOnly`
  — успешно, **0 ошибок**, 32 прежних предупреждения.
- `dotnet test ... --no-build --filter 'FullyQualifiedName!~ClayTreeMutationAbortTests'`
  — **854/854 passed**.
- `ClayTreeMutationAbortTests` исключён целиком из-за ранее подтверждённого несвязанного
  зависания `DragDrop_SqlException_DoesNotRunSuccessPath`; код ClayTree не менялся.

Запущенные приложения временно останавливались для освобождения заблокированных DLL и
apphost, затем Inventory, Users и Styles запущены заново на обновлённых сборках.

## Изменённые файлы

Entities:

- `Inventory/EquipmentAccounting.cs`
- `Inventory/UpsServerLink.cs`
- `SQLQueries.cs`

Inventory:

- `Components/Pages/Equipment/EquipmentForm.razor.cs`
- `Components/Pages/Equipment/Details/EquipmentUpsDialog.razor`

Tests:

- `EquipmentAccountingPersistenceTests.cs`
- `EquipmentUpsPersistenceTests.cs`
- `EquipmentFormTests.cs`
- `EquipmentUpsDialogTests.cs`

Суперпроект:

- `promts/_handoff/EQUIPMENTFORM_AUDIT_6.md`

`git diff --check` для всех изменённых репозиториев успешен; ошибок whitespace нет.

## Незакрытые ограничения

- **R01:** SQL→АТС остаётся circuit-local retry; после потери circuit pending не
  восстанавливается. Выбор A/B/C владельцем не сделан, архитектура не менялась.
- **R04:** набор MAC-разделителей не расширялся, нормализация не менялась.
- WakeOnLan, SIM detail, реальные Xorcom/CCM, физические UPS и внешние URI не проверялись.
- Реальный SQL Server 2008 R2 недоступен; проведён прогон на SQL Server 2022 с
  совместимым синтаксисом, что не заменяет отдельную проверку на 2008 R2.
