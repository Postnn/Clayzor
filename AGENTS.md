# Инструкции для агента — репозиторий Clayzor

Это суперпроект с git-сабмодулями (`src/*`, `tests/*`). Сабмодули по умолчанию
находятся в состоянии detached HEAD.

## Правило: detached HEAD в сабмодуле

Перед коммитом внутри сабмодуля всегда проверяй `git status`.

Если первая строка — `HEAD detached ...` и в этом состоянии есть локальный
коммит, который нужно сохранить на ветке:

1. Сначала убедись, что ветка `main` — предок текущего коммита
   (текущий коммит просто "впереди" `main`):
   `git merge-base --is-ancestor main HEAD` (код возврата 0 = да).
2. Если да — прикрепи HEAD к ветке и подвинь её на текущий коммит:
   `git switch -C main`
3. Если нет (ветка `main` разошлась или указывает на посторонний коммит) —
   НЕ выполняй `switch -C` (он перезапишет указатель `main`). Остановись
   и спроси пользователя, что делать.

Если `git status` показывает `On branch main` — HEAD уже на ветке,
ничего прикреплять не нужно.

## Сопутствующее

- После коммитов в сабмодулях отдельно фиксируй обновлённые ссылки на них
  в суперпроекте (`git add src tests` → commit) — иначе возникает рассинхрон.
- Файлы сборки .NET (`bin/`, `obj/`) не коммить: они в `.gitignore` каждого
  сабмодуля.

## Фиксация контекста по этапам

По завершении каждого этапа обновляй актуальный handoff в `promts/_handoff/`.
Для одной задачи используй один файл, не создавай дубли после каждой правки.
Записывай цель этапа, изменённые файлы и поведение, выполненные проверки с их
результатами, SHA коммитов, состояние запущенных приложений, нерешённые проблемы
и конкретное следующее действие. Не выдавай непроверенное за выполненное.
Перед сменой задачи или при риске потери контекста обновляй handoff даже для
незавершённого этапа, явно отмечая незавершённость и последний ошибочный тест.
 
**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

# Clayzor — Медицинские исследования

## Agent instructions
- Перед внесением изменений, если требования неоднозначны, задавай уточняющие вопросы через опросник (question tool)
- Изменения в git (add, commit, push, etc.) — **только по прямому указанию**. Никогда не коммитить автоматически после завершения задачи.
- Актуализация AGENTS.md и документации — **только по прямому указанию**. Не обновлять документацию в рамках других задач без явной просьбы.
- **Прямое добавление тестовых записей в таблицы БД**: сначала отключать триггеры затронутых таблиц, затем добавлять записи, после чего обязательно включать триггеры обратно. Отключение, вставку и повторное включение выполнять одним управляемым SQL-сценарием с `TRY/CATCH`, который включит триггеры даже при ошибке; после завершения проверить, что ни один отключённый для теста триггер не остался выключенным. Правило относится только к тестовому наполнению справочников: штатные CRUD-операции и создание оборудования через `sp_Оборудование_Ins` выполняются с активными триггерами.

## Stack
- .NET 10 Blazor Server (Interactive Server)
- MudBlazor 9.x — `Variant.Outlined` is the standard form input style
- MudBlazor.Extensions 9.x — дополнительные возможности MudBlazor (перетаскивание диалогов и др.)
- Dapper + Microsoft.Data.SqlClient 6.x (SQL Server)
- Windows Integrated Auth (Kerberos/NTLM via Negotiate)
- **SQL Server 2008 R2** — использовать только синтаксис, доступный в этой версии

## Границы Services

- **Blazor-компонент** отвечает только за отображение, черновик, события, проверку пользовательского ввода, сообщения и навигацию. Он не содержит SQL и не реализует собственную проверку ролей. Форме, которой нужны пользователь или права, при первичной асинхронной инициализации доступен типизированный `Employee CurrentUser`.
- **`Clayzor.Lib.Entities`** — единый владелец модели, CRUD, бизнес-правил данных, справочных enum, `Employee` и семантики ролей, языка и замещений. Все SQL-запросы находятся только в `Clayzor.Lib.Entities/SQLQueries.cs`.
- **`Clayzor.Lib.DALC`** — только транспорт SQL Server через `DbManager`, gate соединения и обработку ошибок. **`Clayzor.Lib.Web.DBSelect`** — только источники и фильтры справочников, а не бизнес-авторизация.
- В прикладных Web-проектах **запрещены** Repository/ORM-слой и CRUD-обёртки, которые лишь перенаправляют вызов Entity/DbManager. Service/Coordinator допустим только для реальной координации нескольких сущностей, транзакции, внешней АТС, особых действий до/после записи или минимальной инфраструктуры получения контекста. Он не должен повторять доменную логику.
- Простые чтения и CRUD принадлежат сущностям `Entities`. Пакетное чтение через `DbManager.RunAsync` + `QueryMultipleAsync` сохранять: оно сокращает round trips. SQL и read models размещать вместе с механизмом чтения в `Entities`, не создавая циклических зависимостей и не выполняя inline SQL в Web-формах.
- Чистые правила UI/доступности, например `PhoneFormRules`, размещать рядом с прикладной функциональностью в `Rules`/feature-каталоге, а не в DALC и не в Services БД. Не создавать вторые пути доступа к данным или временные дубли старой и новой архитектуры.
- При рефакторинге Inventory простые CRUD не обёртывать отдельным service на сущность. `EquipmentInsService`, `ModelEquipmentFormService` и `EquipmentFormService` не должны содержать самостоятельные `HasRoleAsync`, `HasRoleForPersonAsync` или CRUD-прокси. `GetForCopyAsync`, создание, uniqueness/usage и простые чтения принадлежат доменным сущностям/read helpers. Допустимы только обоснованные узкие coordinators для составных записей, UPS, сети и АТС.
- `EquipmentFormReadRequest/Result` располагать рядом с пакетным чтением. Порядок result sets, условные секции, права чтения секретных полей, обработку ошибок и отмену сохранять. Пароли, PIN/PUK не включать в общий batch.
- `PhoneFormRules` переносить из `Services` в логически подходящий feature/`Rules` без изменения матрицы полей. Не затирать изменения других агентов и не перезаписывать submodule pointers без проверки HEAD.

## Enum для фиксированных кодов БД

- Любой **фиксированный числовой идентификатор записи таблицы БД**, используемый как бизнес-константа в C#, Razor или SQL-вызове, объявлять именованным элементом enum в `Clayzor.Lib.Entities`. Запрещены `const int AccountantRoleId = 66`, `HasRoleAsync(43)`, `TypeId == 30`, `LocationId == 55`, `DocumentTypeId = 2365` и аналогичные magic IDs.
- **Имя enum строго совпадает с именем таблицы БД**: `Роли`, `ТипыОборудования`, `Расположения`, `ТипыДокументов` — только после подтверждения реальных имён. Enum не называть по форме, DTO или проекту и не создавать отдельный enum на форму.
- Имя элемента соответствует русскому наименованию записи БД с технической нормализацией C# (`_` вместо пробелов/пунктуации), значение — фактический код. Не придумывать наименования и не переименовывать запись по месту использования. XML-doc enum содержит таблицу и колонку-код, XML-doc элементов — исходное наименование/назначение.
- В сигнатурах известных фиксированных кодов принимать enum, а не `int`: `CurrentUser.HasRole(Роли.Бухгалтер)` и `CurrentUser.HasRole(Роли.Бухгалтер, ownerPersonId)`. Приведение к числу допустимо только на границе Dapper/SQL или при сопоставлении значения из БД.
- Переменные PK произвольных записей (`EquipmentId`, `EmployeeId`, `PhoneId`), арифметические константы, размеры, таймауты и `0/1` булевых признаков в enum не включать. Семантические `0`/`-1` («все организации», «не выбрано») оформлять отдельными именованными константами/типами, не добавляя фиктивные записи справочника.
- SQL-литерал фиксированного кода по возможности заменять параметром и передавать enum. Не конкатенировать пользовательский ввод в SQL. DB-side constraint сохранять только с объяснением и тестом.
- Перед созданием enum сверять схему и данные БД или legacy. Неподтверждённое соответствие не выдумывать, а регистрировать как долг. Обязательная карта проверки: `таблица`, `ключевое поле`, `код`, `название БД`, `enum`, `места использования`.
- Для ролей проверять 42/43/44/66/68/102/103, для типов оборудования 27/30, расположения 55 и типа документа 2365. Код 2365 проверять в **базе документов**, не автоматически в инвентаризации. Роль 42 называть только по реальному `Роли`/`vwРоли` (`Администратор_тарифов_связи` в legacy), не `Специалист_SIM` по догадке; смысловое несоответствие фиксировать в отчёте без изменения фактических прав.
- Статический guard/test должен находить очевидные `const int .*RoleId`, `HasRoleAsync(42/43/...)`, `DocumentTypeId = 2365`, `TypeId == 30`, `LocationId == 55`, но не запрещать арифметику и входные данные. Неподтверждённые коды заносить в долговой список, не заменять ложными enum.

## Employee CurrentUser и авторизация

- Использовать и при необходимости бережно расширять/перемещать существующий `Clayzor.Lib.Entities.Inventory.Employee`, не создавая конкурирующую сущность. `EmployeeSelectDataSource`, Users и формы должны продолжать работать.
- `Employee` — **единственный** владелец получения текущего сотрудника, effective roles, `HasRole`/`RequireRoleAsync`, области лица, замещений, языка/культуры и fallback локализации. Не копировать весь legacy Employee без необходимости.
- Получать сотрудника только на сервере из доверенной идентичности. Сверять Windows/Negotiate principal и SQL principal. `SID = SUSER_SID()` применять только при доказанном соответствии пользователю; service-account нельзя выдавать за текущего сотрудника. При отсутствии/ошибке идентичности — deny/fail closed, никакого пользователя по умолчанию.
- Запрещены независимые `HasRoleAsync`, `HasRoleForPersonAsync` и произвольные вызовы `fn_ТекущиеРоли()` в формах, Services и сторонних helpers. Проверки выполнять как `Employee.HasRole(Роли role, ...)`, `HasRoleAsync`/`RequireRoleAsync` через `CurrentUser`.
- Поддерживать роль без лица и по конкретному `personId`, `КодЛица=0` как global-scope и `zeroContains` по legacy. Не подменять глобальную роль наличием роли у одного лица. Замещения и периоды действия проверять через подтверждённую серверную SQL-логику, не упрощённым `RoleId IN (...)`.
- На первом интерактивном рендере загружать `CurrentUser` **до расчёта кнопок и доступности**, используя существующий `OnAfterRenderAsync(firstRender)`/first-load pipeline без двойной загрузки при prerender. В каждой такой форме поле/свойство должно называться `Employee CurrentUser`.
- Scoped loader/provider допустим только для получения и кэширования `Employee`, но не как альтернативная реализация `HasRole`. Не хранить mutable CurrentUser/Roles static, не делиться ими между circuits и не делать N одинаковых запросов при загрузке одной формы.
- Снимок ролей пригоден для UX, но **перед каждой серверной записью и внешним действием обязательна свежая проверка через `Employee`**, включая прямые вызовы без рендера и HTTP endpoints. При отзыве роли после открытия формы запись запрещена. При недоступности SQL/прав — fail closed с сохранением черновика.
- При смене владельца/арендатора/лица пересчитывать разрешения через `CurrentUser`. На write-path загружать актуальные контекст и владельца из БД, не доверять присланным ID. Не передавать CurrentUser/EmployeeId от клиента как доказательство полномочий.
- Не смешивать роль сотрудника, его должность и бизнес-признак лица `fn_БухгалтерскийУчет`. Проверки типа оборудования, владельца, доступности операции и целостности оставлять в доменной логике, отдельно от ролей.
- Сохранить серверные проверки в `GetPhoneConfigAsync`/PhoneConfigHandler, SIM/PIN/PUK, учётных записях/паролях и бухгалтерии. Не расширять доступ при переносе кода.

## Правила компоновки форм и защитные проверки

- Таблицы и табличные секции — **только `ClayGrid`**: никаких `ClayDetailGrid`, `MudTable` и самописных `<table>`.
- Сохранять смысловые пары legacy-полей и порядок Tab. Скрытие зависимого поля выполнять placeholder без смещения остальных полей в чужие позиции. Не переносить HTML-таблицы и фиксированные размеры legacy, сохранять компактную высоту общих контролов и адаптивность 320–480px.
- Для `EquipmentIns`, `EquipmentForm`, `ModelEquipmentForm`, `PhoneTemplateForm` и других форм с правами загружать `CurrentUser` в существующем first-load. Миграцию выполнять последовательно с сохранением сборки: enum/Employee → формы → CRUD/read models → удаление старых `HasRole*`/magic IDs и лишних DI-регистраций.
- `EquipmentInsService.CanUseAccounting` разделять на роль через `Employee` и признак бухучёта лица через домен/use case. `ModelEquipmentForm` и `PhoneTemplateForm` использовать один механизм серверной авторизации.
- Сохранять атомарность `Equipment + diff телефонов`, rollback, отделённую синхронизацию Xorcom/CCM, безопасный повтор без повторного UPDATE, создание оборудования штатной процедурой, ограничения исторических строк, договоры SIM, `На руках`, фильтры ClaySelect/ClayGrid и SQL Server 2008 R2.
- Тесты подтверждают поведение, а не вызовы удалённого `mockService.HasRoleAsync`: коды и имена enum, текущий/не найденный сотрудник, ошибку SQL, несовпадение principals, изоляцию circuits, язык `ru/en` и fallback, global/local роли, действующее и истёкшее замещение, отсутствие роли.
- Отдельно проверять отзыв роли после открытия формы, запрет прямого write без рендера, fail-closed при ошибке получения Employee, сохранение черновика, смену владельца/лица, порядок `QueryMultiple`, отсутствие секретов в batch и отсутствие N+1.
- Регрессионно проверять телефонный diff/АТС, создание оборудования, бухгалтерию, SIM, лицензии, сеть, UPS, даты/locale, ClaySelect и статический ClayGrid. Выполнять `dotnet build Clayzor.sln`, профильные и полный `dotnet test`; если полный набор зависает, честно указывать это и результаты профильных тестов. Browser smoke — на рабочих маршрутах и по возможности под двумя учётными записями, без разрушительных записей в production.
- При рефакторинге оставшиеся services документировать по назначению. Отчёт содержит карту `было → стало`, enum `таблица/поле/код/имя`, пример `CurrentUser` на форме и write-path, обновлённые AGENTS.md, результаты build/test/browser и незакрытые ограничения. Не объявлять работу завершённой при неподтверждённых записях БД или identity.

## Build & Run
```
dotnet build Clayzor.sln
dotnet run --project src\Clayzor.App.Web.MedicalTests
```
Dev URL: `http://localhost:5010`

## Tests
```
dotnet test tests\Clayzor.Lib.Web.Controls.Tests
```
Тестовый проект: xUnit, `net10.0`. Покрывает SQL-билдер, модель фильтра, JSON/URL-сериализацию, описание и индикатор. См. `tests/Clayzor.Lib.Web.Controls.Tests/`.

Watch with hot reload (VS Code task also available):
```
dotnet watch run --project src\Clayzor.App.Web.MedicalTests
```

## Project dependency chain
```
Clayzor.Lib.DALC          ← Dapper, SqlClient
Clayzor.Lib.Entities       ← DALC + Dapper (column mapping)
Clayzor.Lib.Web.Settings   ← config bindings (no DB dep)
Clayzor.Lib.Web.Controls ← Entities + Web.Settings + MudBlazor (Razor Class Library)
Clayzor.App.Web.MedicalTests ← all of the above (web app entry point)
```

## Configuration — connection string priority

`BindClaySettings()` in `Clayzor.Lib.Web.Settings/ClayAppSettings.cs`:
1. Биндит секцию `ClayApp` из конфигурации (`appsettings.json` + `web.config`)
2. Если `ConnectionString` пуста — читает имя из `ClayGrid:Dynamic:ConnectionStringName` (по умолчанию `"DefaultConnection"`)
3. Читает строку подключения с этим именем из `web.config` через `System.Configuration.ConfigurationManager.OpenMappedExeConfiguration().ConnectionStrings`
4. `URI_help_clayGrid` читается из `web.config` через `ConfigurationManager.AppSettings` (ключ `URI_help_clayGrid`)
5. `DictionaryConnectionString` — аналогично п.3 с именем `"DictionaryConnection"`

Оба `appsettings.json` и `web.config` участвуют в конфигурации; `web.config` — основной источник для `connectionStrings` и `appSettings`. Локальные переопределения — в `appsettings.Development.json` (gitignored).

`Program.cs` registers MudBlazor and MudBlazor.Extensions:
```csharp
builder.Services.AddMudServices();
builder.Services.AddMudExtensions(cfg => cfg.WithDefaultDialogOptions(d => d.DragMode = MudDialogDragMode.Simple));
```

`App.razor` must include MudBlazor.Extensions CSS and JS:
```html
<link href="_content/MudBlazor.Extensions/mudBlazorExtensions.min.css" rel="stylesheet" />
<script src="_content/MudBlazor.Extensions/js/mudBlazorExtensions.js"></script>
```

`App.razor` must also include Clayzor RCL JS scripts:
```html
<script src="_content/Clayzor.Lib.Web.Controls/js/clayColumnSettings.js"></script>
<script src="_content/Clayzor.Lib.Web.Controls/js/clayGridColumnDrag.js"></script>
<script src="_content/Clayzor.Lib.Web.Controls/js/clayGridPrint.js"></script>
<script src="_content/Clayzor.Lib.Web.Controls/js/clayGridExcel.js"></script>
<script src="_content/Clayzor.Lib.Web.Controls/js/clayFormWindow.js"></script>
<script src="_content/Clayzor.Lib.Web.Controls/js/clayDraftStore.js"></script>
```

## Map / где что искать

| Что | Где |
|---|---|
| Глобальные правила (Think/Simplicity/Surgical/Goal-Driven), обзор решения, стек, сборка, тесты, цепочка зависимостей, конфигурация, key conventions | **/AGENTS.md** (этот файл) |
| Доступ к БД, SQL-конвенции, пагинация SQL Server 2008 R2 | **[src/Clayzor.Lib.DALC/AGENTS.md](src/Clayzor.Lib.DALC/AGENTS.md)** |
| Entity CRUD & Lookup, добавление новых сущностей | **[src/Clayzor.Lib.Entities/AGENTS.md](src/Clayzor.Lib.Entities/AGENTS.md)** |
| Документация по Entity, CRUD, ILookupEntity | [src/Clayzor.Lib.Entities/docs/entity-crud.md](src/Clayzor.Lib.Entities/docs/entity-crud.md) |
| Воркфлоу добавления новой сущности | [src/Clayzor.Lib.Entities/docs/adding-new-entity.md](src/Clayzor.Lib.Entities/docs/adding-new-entity.md) |
| Роль сборки настроек/конфигурации | **[src/Clayzor.Lib.Web.Settings/AGENTS.md](src/Clayzor.Lib.Web.Settings/AGENTS.md)** |
| Правила форм и Services Inventory | **[src/Kesco.App.Web.Inventory/AGENTS.md](src/Kesco.App.Web.Inventory/AGENTS.md)** |
| Shared-компоненты (ClayGrid, ClayComboBox, ClayEditForm, ...), интерфейсы, сервисы, серверная группировка, фильтрация, фильтр-трей, сериализация, локализация | **[src/Clayzor.Lib.Web.Controls/AGENTS.md](src/Clayzor.Lib.Web.Controls/AGENTS.md)** |
| Документация ClayGrid | [src/Clayzor.Lib.Web.Controls/docs/clay-grid.md](src/Clayzor.Lib.Web.Controls/docs/clay-grid.md) |
| Документация ClayComboBox | [src/Clayzor.Lib.Web.Controls/docs/clay-combo-box.md](src/Clayzor.Lib.Web.Controls/docs/clay-combo-box.md) |
| Документация ClayEditForm | [src/Clayzor.Lib.Web.Controls/docs/clay-edit-form.md](src/Clayzor.Lib.Web.Controls/docs/clay-edit-form.md) |
| Документация ClayErrorBar | [src/Clayzor.Lib.Web.Controls/docs/clay-error-bar.md](src/Clayzor.Lib.Web.Controls/docs/clay-error-bar.md) |
| Документация ClayColumnFilterDialog | [src/Clayzor.Lib.Web.Controls/docs/clay-column-filter-dialog.md](src/Clayzor.Lib.Web.Controls/docs/clay-column-filter-dialog.md) |
| Документация ConfirmDialog | [src/Clayzor.Lib.Web.Controls/docs/confirm-dialog.md](src/Clayzor.Lib.Web.Controls/docs/confirm-dialog.md) |
| Форма телефонного номера Tel и руководство пользователя | [src/Kesco.App.Web.Inventory/docs/phone-form.md](src/Kesco.App.Web.Inventory/docs/phone-form.md) |
| Подтверждённые пропуски EquipmentForm_2 | [promts/EquipmentForm_2_skipped.md](promts/EquipmentForm_2_skipped.md) |
| Руководство пользователя ModelEquipmentForm | [src/Kesco.App.Web.Inventory/docs/model-equipment-form.md](src/Kesco.App.Web.Inventory/docs/model-equipment-form.md) |
| Руководство пользователя EquipmentIns | [src/Kesco.App.Web.Inventory/docs/equipment-ins.md](src/Kesco.App.Web.Inventory/docs/equipment-ins.md) |
| Руководство пользователя EquipmentForm | [src/Kesco.App.Web.Inventory/docs/equipment-form.md](src/Kesco.App.Web.Inventory/docs/equipment-form.md) |
| Контракты EquipmentForm | [src/Kesco.App.Web.Inventory/docs/equipment-form-write-contracts.md](src/Kesco.App.Web.Inventory/docs/equipment-form-write-contracts.md) |
| Руководство пользователя ClaySelect | [src/Clayzor.Lib.Web.Controls/docs/clay-select-user-guide.md](src/Clayzor.Lib.Web.Controls/docs/clay-select-user-guide.md) |
| Динамический режим ClayGrid — план реализации (G0–G14, выполнены) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/_readme_grid_dynamic.md](src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/_readme_grid_dynamic.md) |
| Динамический режим ClayGrid — багфиксы (GF1–GF16) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GF0_README_dynamic_fixes.md](src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GF0_README_dynamic_fixes.md) |
| Динамический режим ClayGrid — группировка (GG1–GG9) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GG0_README_dynamic_grouping.md](src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GG0_README_dynamic_grouping.md) |
| Динамический режим ClayGrid — снятие потолка уровней (GN1–GN4) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GN0_README_grouping_levels.md](src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GN0_README_grouping_levels.md) |
| Динамический режим ClayGrid — печать и Excel (GE1–GE6) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GE0_README_dynamic_export.md](src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GE0_README_dynamic_export.md) |
| ClayGrid — багфиксы UX (GB1–GB11) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/GB0_README_grid_ux_fixes.md](src/Clayzor.Lib.Web.Controls/Components/Grid/promts/GB0_README_grid_ux_fixes.md) |
| DynamicGrid: SQL-схема (dev/test стенд) | [scripts/dynamic-grid/schema.sql](scripts/dynamic-grid/schema.sql) |
| DynamicGrid: быстрый поиск (QS0–QS9) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/QS0_README_quick_search.md](src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/QS0_README_quick_search.md) |
| ClayGrid — аудит безопасности (GA0–GA9, GA1–GA6 выполнены) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/GA0_README_audit_grid.md](src/Clayzor.Lib.Web.Controls/Components/Grid/promts/GA0_README_audit_grid.md) |
| Типографика, шрифты, style enforcement (Variant A, build-time, checklist) | **[src/Clayzor.App.Web.MedicalTests/AGENTS.md](src/Clayzor.App.Web.MedicalTests/AGENTS.md)** |
| Закон стиля (исходник) | [STYLE_RULES.md](STYLE_RULES.md) |
| Промты внедрения стиля (выполнены) | [promts/_done/STYLE_PROMPTS.md](promts/_done/STYLE_PROMPTS.md) |

## Key conventions
- All Razor markup and user-visible text is **Russian**
- No CI/CD, no linter configuration
- Unit tests: `tests/Clayzor.Lib.Web.Controls.Tests/` (xUnit, 39 тестов — SQL-билдер, модель, сериализация)
- **Всегда использовать готовые компоненты из `src\Clayzor.Lib.Web.Controls`** при разработке форм (ClayEditForm, ClayComboBox, ClayTextBox, ClaySelect, ClayTime, ClayChanged, ClayToolbarButton, ConfirmDialog, ClayColumnFilterDialog, ClayErrorBar и другие). Перед использованием MudBlazor или HTML-элементов напрямую проверять наличие Clayzor-аналога. Если у элемента ввода данных есть legacy-аналог, а готового Clayzor-компонента ещё нет, сначала реализовать общий компонент в `Clayzor.Lib.Web.Controls` и затем использовать его на форме; локальные разметка, стили и обходы вместо общего компонента запрещены. Не оборачивать MudBlazor-компоненты, для которых нет legacy-аналога.
- **Таблицы**: для любых таблиц и табличных частей форм использовать только `ClayGrid` — статический режим для уже загруженных данных и динамический режим для конфигурации из БД. Отдельные DetailGrid/Table-компоненты и собственные HTML-таблицы запрещены. В табличных секциях добавление находится в шапке сервисной колонки, а действия над записью — в её ячейках (`OnAdd` + `RowActions`).
- **Legacy-термины и порядок элементов**: если форма существует в legacy, без отдельного явного требования запрещено переименовывать поля, кнопки, пункты меню, варианты radio/dropdown и колонки; менять их порядок, количество колонок, порядок колонок, названия и порядок кнопок и прочие пользовательские элементы. Сохранять исходные подписи дословно, включая регистр и смысл. Новая реализация и современное оформление не являются основанием для «улучшения» терминологии.
- **Развитие общих компонентов**: если существующий компонент Clayzor не поддерживает необходимое поведение, доработать этот компонент минимальным общим API. Не создавать отдельную упрощённую или специализированную версию компонента для конкретной формы.
- **Централизованный внешний вид**: размеры, высота, отступы, цвета и другое оформление Clay-контролов изменяются только централизованно — в общем компоненте, его CSS или общей теме. Запрещены переопределения внешнего вида через CSS отдельной формы/страницы, локальные классы и inline-стили. Высота однотипных контролов должна быть одинаковой на всех формах; если требуется другое значение, оно сначала согласовывается и изменяется сразу для всех форм. **Стандартная высота однострочных полей ввода задаётся только `--clay-control-height: 32px` в `Clayzor.Lib.Web.Controls/wwwroot/css/clay.css` и применяется глобально, включая диалоги. Локальные правила высоты запрещены; textarea и многострочные редакторы не принудительно сжимаются до одной строки. Ширина `ClayDatePicker` и `ClayTime` одинакова и задаётся только `--clay-date-time-width: 180px`: поля не растягиваются вместе с формой, значения даты и времени должны помещаться полностью, локальные правила ширины запрещены.**
- **Открытие форм и карточек**: запрещены прямые `window.open` и `JS.InvokeVoidAsync("open")` в формах, компонентах и прикладном JS. Использовать только общий `clayOpen` из `clayFormWindow.js` с устойчивым ключом окна; размер и позиция каждой формы сохраняются отдельно через `clayFormWindow`/`localStorage`. Открытие карточки не должно закрывать или перезагружать родительскую форму. Нативный `window.open` допустим только внутри централизованной реализации.
- **Кнопки с тултипом**: всегда использовать `<ClayButton>` вместо пары `<MudTooltip><MudIconButton/></MudTooltip>`. `ClayButton` автоматически сбрасывает тултип после клика, предотвращая «залипание» при открытии диалогов. При задании `Href` рендерится как ссылка (`<a>`) — для кнопок, открывающих внешние страницы.
- **Выпадающие меню**: всегда использовать `<ClayMenu>` вместо `<MudMenu>` с `<ActivatorContent>`, `<MudTooltip>` и `<MudIconButton>`. `ClayMenu` автоматически строит кнопку-активатор с опциональным тултипом и сбрасывает тултип после клика, аналогично `ClayButton`.
- **Кастомные чекбоксы**: всегда использовать `<ClayCheckbox>` (controlled: `State` + `OnToggle`, либо двусторонняя привязка `State` + `StateChanged`) вместо inline `<span>`-глифов 16×16. Поддерживает tri-state (`null` = indeterminate), клавиатуру и `ReadOnly`.
- **Контролы форм по legacy-аналогии**: если в legacy есть общий контрол, на формах Clayzor использовать его общий аналог — `ClayTextBox`, `ClayTextArea`, `ClayNumber`, `ClayDatePicker`, `ClayTime`, `ClayDropDownList`, `ClayCheckbox`, `ClaySelect`. Не оборачивать MudBlazor без legacy-аналога и не копировать legacy-реализацию: API и поведение должны быть едиными, оформление — современным визуальным продолжением legacy.
- **Панель редактирования**: формы редактирования используют `ClayToolbar` и `ClayToolbarButton` для команд формы (например, OK, Применить, Обновить, Отмена). Кнопки не дублируют разметку `MudButton` на страницах.
- **Каркас отдельной формы**: использовать `ClayEntityForm` вместо собственного `MudPaper`. Общий CSS задаёт стандартные 5px над кнопками и левый край кнопок ровно по золотой полосе; страницы не добавляют собственные отступы панели. `ClayToolbar` передаётся в `Toolbar` и закреплён сверху, footer с `ClayChanged` — снизу; прокручивается только `ChildContent`. Высота панели автоматически увеличивается при переносе кнопок на узком экране.
- **Сведения об изменении**: каждая форма редактирования содержит `ClayChanged` в `Footer` общего `ClayEntityForm`, под нижней границей формы. Для существующей записи компонент показывает сотрудника и дату последнего изменения; для новой записи область остаётся пустой. `ChangedAt` в БД хранится в UTC и выводится через смещение `clayGridTimeZone.getOffsetMinutes` на дату записи в локальном времени клиента.
- `[Column]` attributes use exact Russian database column names — do not translate
- SQL queries reference tables by Russian names (e.g. `МедицинскиеАнализы`, `МедицинскиеАнализыТипы`)
- Each SQL constant must be documented with `///` XML doc and `--` inline SQL comments for every column
- Every public/protected class, method, property, and field must have a `/// <summary>` XML doc comment
- Database column names are defined exactly once in `ColumnNames.cs` and referenced from `[Column]` attributes (SQLQueries constants are exempt)
- Data loading goes in `OnAfterRenderAsync(bool firstRender)` with `if (firstRender)` guard, **not** in `OnInitializedAsync` — avoids double-load from Blazor prerendering
- **UTC для даты и времени**: любое значение БД `datetime`/`datetime2`, содержащее время, хранится и передаётся как UTC независимо от `DateTimeKind` (`Unspecified` из Dapper также трактуется как UTC). При выводе всегда преобразовывать его в часовой пояс клиента со смещением на дату события, включая DST; это относится к формам, табличным секциям, `ClayChanged`, `ClayLocalTime`, DynamicGrid типам 10/13, печати и Excel. Не показывать исходное UTC как локальный fallback. Календарные date-only значения без времени не сдвигать. Локальный ввод даты/времени перед записью преобразовывать в UTC
- **DR-01 — восстановление ввода**: изменяемые формы подключаются к общему `ClayDraftService`/`clayDraftStore.js`, а не создают собственный autosave. Независимый IndexedDB-draft изолируется по пользователю, форме, сущности и вкладке, шифруется AES-GCM через WebCrypto и содержит typed baseline, зашифрованный raw input (включая секреты), дочерние редакторы, ревизии и отдельный running/unknown operation snapshot. Секреты запрещены в typed JSON, URL и логах. Перед write/external command обязателен подтверждённый `BeginOperation`; при неизвестном исходе replay запрещён. Commit очищает только принятую ревизию, явный Discard — основной draft и migration backup; обычный Dispose сохраняет draft. Reconnect `forceLoad` разрешён только после `clayDraftStore.canReload`. Восстановление не запускает Save/АТС/WOL или иные бизнес-команды. Миграции схемы обязаны сохранять backup до подтверждённой записи новой версии
- Sorting, searching, grouping, and **pagination** for data grids must be performed on SQL Server side (not in-memory)
- Sort column headers call `async Task ToggleSort(string column)` via `@onclick="@(async () => { if (_dataGrid is not null) await _dataGrid.ToggleSort("SqlColumn"); })"` — `_dataGrid` is set via `@ref` on `ClayGrid`. Chips in the grouping tray use `@onclick="async () => await ToggleSort(col)"` inside ClayGrid itself. **Do not call `ToggleSort` as fire-and-forget void** — Blazor will not await the data reload
- `appsettings.Development.json` is gitignored — use it for local connection strings
- All modal/dialog windows must be draggable. Для всех вспомогательных диалогов явно использовать `DialogOptionsEx { DragMode = MudDialogDragMode.Simple }` и `IDialogService.ShowExAsync`; обычный `ShowAsync` без этих опций запрещён, включая ConfirmDialog и вложенные диалоги
- Data grid header row must be fixed (not scroll with data) — `ClayGrid` does this automatically
- При вызове `Db.ExecuteAsync()` с сырым SQL обязательно передавать `commandType: CommandType.Text` — по умолчанию `ExecuteAsync` использует `CommandType.StoredProcedure`
- `DapperColumnMapper` делает fallback на имя свойства, если `[Column]`-атрибут не совпал с колонкой результата — это позволяет использовать SQL-алиасы (`SELECT КодТипа AS Id`) даже при наличии `[Column("КодТипа")]` на свойстве `Id`
- **OnQueryChanged**: обновлять свойства `_query`, а не переприсваивать объект — иначе `TotalCount` сбрасывается в 0 при async-рендере. `ExpandedGroups` управляется страницей и **не перезаписывается** из query. `CompositeFilter` копируется из query целиком (дерево)
- **ClayGridPageBase**: SQL-конфигурация передаётся через параметры `<ClayGrid>` (`SelectSql`, `SearchColumns`, `DefaultOrder`, `EditDialogType`), а не через abstract-свойства. База читает их из `Grid?.SelectSql` и т.д. Страница переопределяет `protected override IClayGrid? Grid => _dataGrid;` и передаёт `DataLoader="this"`
- **Запрещено** подставлять значения параметров в SQL-строку — все параметры передаются через Dapper (`@param`). Параметры пагинации передаются как `@__start`/`@__end` через `DynamicParameters`
- **Обработка ошибок БД**: `DbManager` автоматически перехватывает `SqlException` и вызывает `ISqlErrorHandler.HandleSqlError()`, но пишущие операции повторно выбрасывают исключение. На границе UI форма должна перехватить ожидаемое `SqlException`, сохранить введённые данные и не продолжать success-path; повторно вызывать `ErrorService.Report()` не нужно. Баннер `ClayErrorBar` в `MainLayout` показывает ошибку со строкой подключения, SQL и параметрами
- **Grouping tray**: заголовки колонок генерируются автоматически через `<ClayColumn>` — drag-and-drop (`draggable="true"` с `data-col-sql`,
  `_dragState.DraggedColumn` конкретного экземпляра грида устанавливается через JS→C# `SetDraggedColumn`) и серверная сортировка (`@onclick` → `Grid.ToggleSort(query.SqlName)`) встроены в компонент.
  При перетаскивании на панель группировки колонка добавляется автоматически. Сортировка по сгруппированным колонкам разрешена (клик по чипу в трее).
  Каждый чип имеет переключатель `UnfoldMore`/`UnfoldLess` — разворачивает/сворачивает ВСЕ группы этого уровня (с каскадом вверх: разворачивание уровня N разворачивает и уровни 0..N−1). Кнопка `MoreVert` (⋮) справа — контекстное меню с пунктом «Фильтровать» (аналог меню заголовка колонки, вызывает `OpenFilterDialog`).
  Панель скрыта по умолчанию (`_trayExpanded = false`) и открывается кнопкой `AccountTree` в тулбаре.
  Кнопка группировки появляется автоматически при наличии хотя бы одного `ClayColumnDef` с `Groupable="true"`.
  Кнопки тулбара (настройка колонок, группировка, фильтрация, добавить, выбрать, групповые операции, документация) используют `ClayButton` с CSS-классами `toolbar-columns-btn` /
  `grouping-toggle-btn` / `filter-toggle-btn` / `toolbar-add-btn` / `toolbar-select-btn` / `toolbar-batch-btn` / `toolbar-help-btn` и тултипами — не `MudButton Variant.Filled`
- **Per-level expand/collapse**: `IClayGridDataLoader.IsLevelFullyExpanded(int depth)` / `ToggleLevelExpandedAsync(int depth)` — массовое управление группами. Дерево групп кешируется в `ClayGridPageBase._groupTreeRoots`. `IsLevelFullyExpanded` проверяет, что все FullKey данной глубины присутствуют в `ExpandedGroups`. `ToggleLevelExpandedAsync` при разворачивании каскадно добавляет в `ExpandedGroups` ключи уровней 0..depth, при сворачивании — удаляет ключи только depth. Взаимодействие с индивидуальным `ToggleGroup`: ручное сворачивание одной группы делает `IsLevelFullyExpanded = false`.
- **Column registration**: метаданные колонок регистрируются через `<ClayColumnDef SqlName="..." DisplayName="..." Groupable="true" Filterable="true" />`
  внутри `<ColumnDefs>`, а не через параметры `ShowGroupingTray`/`AvailableGroupColumns`/`AvailableFilterColumns`.
  `ClayGrid` реализует `IClayGrid` и получает регистрацию через каскадный параметр.
- **Column reorder**: перетаскивание колонок в заголовке реализовано кастомным JS (`clayGridColumnDrag.js`) с **insert**-семантикой (вставка перед/после). Заменяет MudBlazor `DragDropColumnReordering`. `ClayColumn` и динамические колонки ClayGrid устанавливают `DragAndDropEnabled="false"`. Заголовки содержат `data-col-sql` атрибут. JS инициализируется через `clayGridColumnDrag.init(gridId, dotnetRef)` в `OnAfterRenderAsync`. `SetDraggedColumn` вызывается из JS на dragstart — устанавливает `_dragState.DraggedColumn` конкретного экземпляра грида для tray-drop. `OnColumnDrop` применяет insert-перемещение в `_columnOrder` с последующим `_dataKey++`. При перетаскивании у горизонтального края таблицы включается автоскролл с видимой подсказкой направления и сохраняется insert-индикатор. `cursor:grab` разрешён только при `draggable=true`; для сортировки без drag — pointer, для неактивного заголовка — default. Обычные ячейки имеют default, pointer — только у интерактивных элементов. Cleanup — `clayGridColumnDrag.dispose(gridId)` в `DisposeAsync`.
- **Grouping tray toggle**: кнопка `AccountTree` включает/выключает трей. При выключении (`_trayExpanded = false`) очищает `_groupColumns` и перезагружает данные в плоском режиме — колонки возвращаются в грид. Кнопка появляется только при наличии хотя бы одного `ClayColumnDef` с `Groupable="true"`
- **Filter tray toggle**: кнопка `FilterAlt` включает/выключает трей фильтрации. При выключении (`_filterTrayExpanded = false`) очищает `_activeFilters` и перезагружает данные без фильтров. Оба трея (группировка + фильтрация) могут быть открыты одновременно — высота грида уменьшается соответственно
- **Tray borders & icons**: панели группировки (`.grouping-tray`) и фильтрации (`.filter-tray`) имеют идентичное оформление. Фон, границы и иконки используют MudBlazor-переменные (`--mud-palette-*`) для авто-адаптации к светлой/тёмной теме. `border-left: 3px solid var(--mud-palette-primary)`, `border-bottom: 2px solid var(--lh-gold)`, фон `var(--mud-palette-background-gray)`. Иконки: по умолчанию `var(--mud-palette-text-secondary)` + opacity 0.45; при наличии чипов в трее (`:has(.xxx-chip)`) — `var(--mud-palette-primary)` + opacity 1. При наведении/перетаскивании `border-left-color` меняется на `var(--lh-gold)`. CSS определён в `wwwroot/css/app.css`
- **Batch operations**: стандартные операции включаются флагами `ShowPrint` / `ShowExcel` без написания кода на странице. Кастомные — через `CustomBatchGroups` (модели `BatchOperationGroup` / `BatchOperation`, обработчики `Func<Task>? OnExecute` реализуются в приложении). Меню открывается кнопкой `PlaylistAddCheck` при `SelectVisible="true"`
- **Selection**: режим выбора включается кнопкой `CheckBox` в тулбаре. Добавляет сервисную колонку с кастомными чекбоксами (16×16px, белый фон, классическая CSS-галочка border-rotate, жирный квадрат для indeterminate). Выделение хранится как `HashSet<int> _selectedIds` (ID сущностей) и **персистентно** между страницами и при смене размера страницы. Сбрасывается только при изменении сути запроса: поиск, группировка, сортировка, фильтры. Галка в заголовке управляет и строками детализации, и группами (через `GroupHeaderRow` → ленивая загрузка дочерних ID → `_groupChildIds`). При выключении режима выбора `_selectAllChecked` сбрасывается. Групповые чекбоксы поддерживают tri-state (checked/unchecked/indeterminate)
- **Print**: все три режима печати (текущая страница / все данные / выбранные) проходят через серверную HTML-генерацию: `BuildPrintHtmlForCurrentPageAsync` / `BuildPrintHtmlAsync` / `BuildPrintHtmlForSelectedAsync` → `ClayGridPrintHtmlGenerator.Build()` → `clayGridPrint.printHtml(html)` (рендерит HTML в скрытый iframe, печатает iframe, удаляет после `afterprint`). `clayGridPrint.printCurrentPage()` удалён — печать текущей страницы больше не использует `window.print()` на DOM грида. **HTML использует те же MudBlazor CSS-классы** (`.mud-table`, `.mud-table-cell`, `.group-header-cell`) и встраивает полный `@media print` CSS — результат визуально идентичен экранному гриду. Групповые строки выводятся без иконок разворота/выбора — только имя группы и количество. Грид **не затрагивается**. На время загрузки — спиннер (`_isExporting`). Печать «Выбранных» — реализована через `BuildAllRowsForSelected` (группированный режим: C# interleaving с `SelectedItemCount` для каждого `GroupHeaderRow`). **Перед каждой операцией** — `ResolveExportColumnsAsync` → prompt `ClayColumnSettingsPromptDialog` (3 исхода): «Выбрать колонки» (диалог `ClayColumnSettingsDialog` с `ShowSorting=false`), «Как на странице» (`GetVisibleColumns()`), «Отмена»
- **Excel export**: экспорт в Excel реализован через `ClayGridExcelGenerator` (генератор .xlsx на сервере, ClosedXML) и `clayGridExcel.js` (скачивание на клиенте через Blob URL). Генератор создаёт стилизованную книгу в цветах Clayzor (navy `#05164D`, gold `#FFAD00`, серый stripe `#F2F4F7`) со шрифтом Verdana. Поддерживает заголовок грида, описания фильтров/группировки, групповые строки с Excel Outline (вложенные группы через стек — все уровни имеют +/- контролы), авто-ширину колонок. Групповые строки выводятся без иконок выбора. На время экспорта рядом с заголовком грида показывается `MudProgressCircular` (Size.Small, Indeterminate) — флаг `_isExporting` управляется в `try/finally`. Экспорт «Всех данных» — полностью реализован через `BuildAllRowsForExcel()`: в плоском режиме один запрос `SELECT * FROM _src`, в режиме группировки — два запроса (GROUP BY для агрегатов + плоский SELECT всех строк, групповая структура строится в C# детектированием смены ключа). Экспорт «Выбранных» — реализован через `BuildAllRowsForSelected`. **Перед каждой операцией** — `ResolveExportColumnsAsync` (тот же prompt, что и для печати)
- **Column settings**: кнопка `ViewColumn` в тулбаре открывает `ClayColumnSettingsDialog` — диалог настройки порядка, видимости и сортировки колонок. Drag-and-drop реализован как jQuery UI Sortable с авто-прокруткой (ghost на `position:fixed`, placeholder в DOM, авто-скролл при приближении к краям контейнера). Сортировка: клик по названию колонки циклически переключает ASC(↑) → DESC(↓) → нет, до 2 колонок одновременно. Бейдж `1↑`/`2↓` (`.chip-sort-badge`) — золотой фон, navy текст, идентичен бейджу трея группировки. Область сортировки (`.sort-toggle-area`) изолирована от drag-and-drop через JS `closest()`. Кнопки сброса внизу диалога: `ClearAll` (сброс сортировки), `RestartAlt` (восстановление исходного порядка, видимости и сортировки). JS в `clayColumnSettings.js` (RCL), результат передаётся в C# через `[JSInvokable] OnJsDrop`. Стили чипов — `.column-settings-chip` (navy, gold accent hover), ghost — `.column-settings-ghost`, placeholder — `.column-settings-placeholder`. Видимость колонок применяется к гриду (через `_hiddenSqlNames` + `ClayColumn.Hidden`). Переключатель заблокирован для сгруппированных колонок. Порядок колонок из диалога применяется к гриду через двухфазный рендеринг (сбор CellTemplate → динамические TemplateColumns по `_columnOrder`). `_columnOrder` всегда синхронизирован с DOM (обновляется через `OnColumnDrop` из кастомного JS), дополнительное чтение DOM не требуется. При применении сортировка синхронизируется обратно в `_sortState` грида (через `SortName`), вызывается `NotifyQueryChanged()` для перезагрузки данных. При отмене диалога порядок, видимость и сортировка восстанавливаются из snapshot. Параметр `Id` задаёт DOM-id корневого элемента грида (используется `clayGridColumnDrag.init`). **Режим без сортировки** (`ShowSorting=false`): sort-toggle-area не кликабельна, бейдж и кнопка сброса сортировки скрыты. Используется при выборе колонок для печати/экспорта. Валидация: кнопка «Применить» блокируется если все колонки скрыты (Snackbar «Должна быть видна хотя бы одна колонка»)
- **Grid height**: вычисляется динамически через `_gridHeight`: `calc(100vh - 280px)` без треев, `calc(100vh - 330px)` с одним треем, `calc(100vh - 380px)` с двумя. Заголовок грида фиксирован (`FixedHeader="true"`)
- **Responsive layout**: тулбар и пагинация обёрнуты в `<div>` с `flex-wrap` — элементы переносятся на узких экранах. Внутренние группы используют `MudStack` для вертикального центрирования
- **Mobile menu**: колонки грида имеют кнопку `⋮` для доступа к группировке и фильтрации без drag-and-drop. Режим управляется параметром `ColumnMenuMode` (по умолчанию `Mobile` — только ≤960px). Пункты меню показываются только когда соответствующая панель (группировка/фильтрация) активирована
- **Navigation drawer**: `DrawerVariant.Responsive` — десктоп (persistent, сдвигает контент), мобильные (temporary overlay). Кнопка-гамбургер меняет иконку `Menu` ↔ `MenuOpen`. AppBar `z-index: 1301` всегда выше overlay. Overlay начинается ниже AppBar + gold underline (`top: 51px`)
- `MudThemeProvider` в `MainLayout` использует `ObserveSystemDarkModeChange="false"` — отключает автоматическое переключение тёмной/светлой темы при изменении системных настроек ОС
- `Program.cs` регистрирует `ClayErrorService` как Scoped и как `ISqlErrorHandler`, передаёт `ISqlErrorHandler` в конструктор `DbManager`
