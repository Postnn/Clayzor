# EquipmentForm_5 — исправления независимого ревью

## Статус и границы

Семь подтверждённых дефектов EQ5 исправлены. Выполнены before-red/after-green для
EQ5-P1-01, EQ5-P1-02, EQ5-P2-05 и EQ5-P2-06, профильные и регрессионные тесты,
реальные browser/SQL-сценарии на тестовых записях. Изменения оставлены в рабочем дереве:
commit/push/merge не выполнялись.

Это не объявление полной приёмки всей EquipmentForm: R01, R04 и реальные внешние
интеграции остаются отдельными ограничениями, перечисленными ниже.

## Исходные ревизии

| Репозиторий | Исходный SHA | Конечное состояние |
|---|---|---|
| Суперпроект | `792acd5f0ac81c02efbf469697abbcb8fe616996` | без нового commit; добавлен этот handoff |
| Inventory | `62669bb776e9b288ac34548b80083c7c57e324be` | рабочее дерево на `main` |
| Entities | `6e9fd7c5c4ec41da18fd041995ed17f2c161e85b` | рабочее дерево на `main` |
| Tests | `a1167ac9df53d8b4157a620cbe3a54c119baacdc` | рабочее дерево на `main` |

Detached HEAD не было. Посторонние untracked-файлы `promts/` не изменялись.

## Матрица дефектов

| Issue | Точная причина и before-red | Исправление | After-green и реальная проверка | Статус |
|---|---|---|---|---|
| EQ5-P1-01 | `CloneUpsServers` не копировал `EquipmentTypeId`. `SqlRowThroughFormClone_PreservesComputerSelection` не находил `srv-a` после настоящих SQL map + clone. | В клон добавлен тип; остальные существенные поля сохраняются. | SQL row типов 1/2 проходит через реальный приватный метод формы в отрисованный test-dialog: `srv-a` активен, не-компьютер скрыт, `legacy` read-only. | Исправлено |
| EQ5-P1-02 | `GroupBy(Id)` считал два новых `Id=0` дубликатом. Before-red: `InvalidOperationException` до SQL. | Дубликаты запрещены только для положительных ключей; исходные тесты обязаны иметь сохранённые положительные ID, неизвестный положительный current ID отклоняется. Diff вычисляется один раз. | Два INSERT с независимыми параметрами; дубликат `Id>0` отклоняется; UPDATE + 2 INSERT без DELETE. В БД созданы тесты 3/4 со значениями 10/20 и именем `clay-test-net`. | Исправлено |
| EQ5-P1-03 | Доступ проверялся только по первому ID из переданных списков. | `SaveUpsDetailsAsync` получает исходный `sourceServerEquipmentId`; SQL transaction проверяет актуальную связь UPS↔source и роль 43 по владельцу source и каждого фактически изменяемого сервера. Неизменные серверы в affected set не входят. Отказ возвращает rollback-status до DML. | Отрицательные тесты status 1/2/3, add/delete/flag/tests-only affected matrix. Реальная запись add server + 2 tests прошла с включёнными триггерами. | Исправлено по консервативному правилу ниже |
| EQ5-P1-04 | Delete по `TOP 1 От DESC` мог удалить более новую строку; owner брался из устаревшего UI. | Сервис принимает `accountingId`. Один `SERIALIZABLE` SQL batch с `UPDLOCK,HOLDLOCK` выбирает latest по `От DESC, Id DESC`, сверяет requested ID, актуального владельца, функцию бухучёта и роли 66/68, затем удаляет. | Browser/SQL: после открытия ID 4 добавлен ID 6; подтверждение удаления 4 отклонено, обе строки сохранены. После Refresh удалён именно ID 6, ID 4 остался. | Исправлено |
| EQ5-P2-05 | `AddServer` создавал `#id`, тип 0. Before-red: фактическое имя `#9`. | Lookup и оба SQL SELECT возвращают `EquipmentTypeId` и сетевое `ServerName`; `AddServer` асинхронно читает выбранную строку, не добавляет фиктивное имя, защищён от повторного async-add. | bUnit: новый `srv-new` сразу доступен в test-dialog и попадает в `Servers`. Browser: добавленный компьютер сразу отображён и доступен как `clay-test-net`; телефон в список уведомлений не входит. | Исправлено |
| EQ5-P2-06 | Общий `_error` после пустого логина навсегда блокировал OK. Before-red: timeout ожидания DialogResult. | `_readFailed` отделён от исправимой validation error; редактирование логина очищает только validation error. Ошибка чтения секрета остаётся блокирующей. | bUnit: пустой логин → ввод `svc-admin` → успешный результат. SQL 53, no-row и unauthorized остаются fail-closed после событий ввода; Cancel доступна. Browser: пустой логин → OK активна → `svc-admin` сохранён. | Исправлено |
| EQ5-P2-07 | Два SELECT UPS были до `try`; поздний результат мог открыть неактуальный диалог. | Загрузка servers/tests и timezone перенесена в защищённый блок; SQL error показывает сообщение и не открывает редактор; version/disposed/id guards повторяются после awaits; cancellation/disconnect завершают операцию. | bUnit: ошибка первого и второго SELECT, сохранение SN-черновика, отсутствие dialog; отложенный ответ после перехода 7→8 не открывает dialog. | Исправлено |

## Правило прав UPS

Выбрано ограничивающее правило без молчаливого расширения доступа:

| Операция | Нужная актуальная авторизация |
|---|---|
| Добавление/изменение только теста UPS | Роль 43 глобально или по владельцу исходного оборудования + существующая связь source с указанным UPS |
| Добавление связи с сервером | Как выше + роль 43 по владельцу добавляемого сервера |
| Удаление связи | Как выше + роль 43 по владельцу удаляемого сервера |
| Изменение флага выключения | Как выше + роль 43 по владельцу изменяемого сервера |
| Неизменный сервер другого владельца в том же UPS | Дополнительное право не требуется, его строка не затрагивается |
| Другой UPS ID, удалённая исходная связь | Rollback со status 2, diff не выполняется |
| Отозванная роль, смена владельца, неподходящий тип/SIM | Rollback со status 1/3, diff не выполняется |

Проверка выполняется в той же транзакции и на актуальных строках БД, а не по переданному
`ownerId`. `fn_ТекущиеРоли()` различает глобальную область `КодЛица=0` и локальную роль
по владельцу. SQL trigger не отключался. Legacy `LinkUps.Save` содержит diff без такой
полной проверки каждого владельца; новое правило намеренно безопаснее.

В текущем baseline общего `Employee CurrentUser` нет; второй параллельный домен прав не
создавался. Существующие `HasRole*` вне этого узкого пути не мигрировались. Это сохранённое
архитектурное несоответствие для отдельного согласованного этапа, а не объявленное соответствие.

## Accounting delete и две сессии

1. UI передаёт только `equipmentId` и подтверждённый `accountingId`, не доверяя старому ownerId.
2. `SERIALIZABLE` transaction с `UPDLOCK,HOLDLOCK` фиксирует актуальный latest и диапазон истории.
3. Tie-break при равных `От`: `КодОборудованияБухУчета DESC`.
4. Status 1 — история пуста; status 2 — появилась другая latest; status 3 — актуальный
   владелец не даёт права 66/68 или не ведёт бухучёт. Во всех случаях rollback без DELETE.
5. Только совпадение ID и успешная авторизация разрешают DELETE конкретной строки.

Проверка выполнена двумя независимыми клиентскими сессиями: Blazor circuit держал снимок ID 4,
внешняя SQL-сессия закрыла его период и добавила ID 6. Подтверждение в Blazor не удалило ни 4,
ни 6. После обновления UI нормальный Delete удалил только 6. Отдельный одновременный stress-test
нескольких параллельных потоков не проводился; доказана реальная гонка «открыл → другая сессия
добавила → подтвердил», которая являлась причиной дефекта.

## Контролируемый rollback UPS

В реальной БД выполнен batch с первой валидной тестовой вставкой и второй вставкой
`КодОборудованияUPS=NULL`. Вторая вызвала ошибку NOT NULL, `XACT_ABORT` + CATCH откатили
transaction. `COUNT(*)` для `CLAY_TEST_EQ5_ROLLBACK%` после ошибки — **0**. Trigger не
отключался, production-строки не использовались.

## Очистка тестовых данных

Создавались только записи на тестовом оборудовании 7/8/15:

- UPS tests ID 3/4 и связи UPS 15 с серверами 7/8 — удалены;
- логин `svc-admin` оборудования 7 — удалён;
- accounting ID 4/6 (`CLAY_TEST_EQ5_OLD/NEW`) — удалены;
- rollback-маркеры отсутствуют.

Финальный SQL-контроль: `ups_tests=0`, `ups_links=0`, `account=0`, `accounting=0`,
`disabled_triggers=0`. Внешние АТС, UPS-оборудование, сеть и Wake-on-LAN не вызывались.

## Команды и totals

- `dotnet build Clayzor.sln --no-restore --verbosity:minimal --consoleLoggerParameters:ErrorsOnly`
  — успешно, **0 ошибок**. Повторный обычный build показал 31 прежнее предупреждение
  (NuGet vulnerabilities, существующий MUD analyzer и CS0162), новых compiler warnings нет.
- Equipment-профиль из задания — **240/240 passed**.
- Полный набор с исключением несвязанного `ClayTreeMutationAbortTests` — **842/842 passed**.
- Попытка полного набора с исключением только ранее известного
  `ClayTreeMutationAbortTests.AddChild_SqlException_DoesNotRunSuccessPath` зависла после
  840 passed. `--blame-hang --blame-hang-timeout 60s --blame-hang-dump-type none`
  идентифицировал `ClayTreeMutationAbortTests.DragDrop_SqlException_DoesNotRunSuccessPath`.
  Код ClayTree не изменялся; временные blame/TestResults удалены.

## Изменённые файлы

Entities:

- `Inventory/EquipmentAccounting.cs`
- `Inventory/UpsEquipmentLookup.cs`
- `Inventory/UpsSaveBuilder.cs`
- `Inventory/UpsServerLink.cs`
- `SQLQueries.cs`

Inventory:

- `Components/Pages/Equipment/EquipmentForm.razor.cs`
- `Components/Pages/Equipment/Details/EquipmentUpsDialog.razor`
- `Components/Pages/Equipment/Details/EquipmentAccountDialog.razor`
- `Services/EquipmentFormService.cs`

Tests:

- `EquipmentUpsDialogTests.cs` (новый)
- `EquipmentUpsTestDialogTests.cs`
- `EquipmentUpsPersistenceTests.cs`
- `EquipmentAccountDialogTests.cs`
- `EquipmentAccountingPersistenceTests.cs`
- `EquipmentFormTests.cs`

## Незакрытые решения и внешние проверки

### R01 — SQL ↔ АТС

Текущее поведение остаётся circuit-local: SQL commit выполнен, неуспешные Xorcom/CCM
операции можно повторить в том же circuit без повторного SQL UPDATE. После потери circuit
pending не восстанавливается. Нужен выбор владельца:

- **A:** принять как документированное исключение и установить ручную сверку;
- **B:** отдельная задача durable outbox с идемпотентностью и возобновлением;
- **C:** отдельно доказать и восстановить legacy TransactionScope вокруг SQL и внешних вызовов.

Автоматический outbox, фоновые задания и изменение процедур АТС не внедрялись.

### R04 — MAC

Текущая UI/backend-нормализация общая: удаляются whitespace, `:`, `-`, `.`, результат
приводится к uppercase. 12 HEX и lowercase принимаются; стандартные и смешанные разрешённые
разделители принимаются; остальные символы отклоняются; пустое значение допустимо на уровне
`IsValidMacAddress`, а обязательность зависит от типа/модели. Расширение набора символов без
решения владельца не выполнялось. Несовместимость с иной legacy-нормализацией не скрывается.

### Внешние интеграции

WakeOnLan, SIM detail, реальные Xorcom/CCM, физические UPS-оповещения, внешние URI и реальная
multi-owner смена ролей в БД в этом этапе не проверялись. Browser smoke и SQL diff не
выдаются за доказательство этих внешних эффектов.
