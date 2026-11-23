# Inventory / ClayNavigationMenu — независимый повторный аудит и актуализированный промт агенту (V2)

**Дата:** 20.11.2026  
**Статус проверки:** GitHub read-only; никаких изменений, commit/push/merge и запусков локального SQL Server/браузера не делалось. Указанные ниже исходники действительно опубликованы в `main`. Цифры тестов и браузерные измерения приведены из документации агента, а не подтверждены независимым запуском.

## 0. Зафиксированные решения владельца — НЕ ОБСУЖДАТЬ ПОВТОРНО

1. **Глобальное удаление AppBar и drawer из MainLayout — согласованное архитектурное решение владельца.** Не ставить задачу вернуть AppBar, drawer, гамбургер, их старые ссылки или делать отдельный старый layout для Home. Не отмечать это как дефект или несоответствие legacy. Сохранить работоспособность остальных страниц в новой оболочке.
2. **Отдельный корень «Операции» из legacy намеренно не переносится.** «Очистить параметры формы» и соответствующие операции должны оставаться в существующем меню групповых операций **ClayGrid**. Не создавать второй reset и не возвращать пункт в навигационное меню.
3. Основная архитектура **принимается**: независимая `ClayNavigationMenu` (Controls), типизированное чтение `Запросы` (Entities), Inventory-specific composer и URL-builder (Inventory), один существующий динамический ClayGrid на главной странице. **Не переписывать** с нуля и не создавать Repository/ORM, ещё один грид или дублированный слой бизнес-правил.
4. Для URL внешних форм/настроек **не выдумывать** конфигурационные значения. При отсутствии подтверждённого URL сохранять disabled с понятной причиной. Реальные адреса требуют проверки действующего `uri.local`/IIS или ответа владельца.
5. SQL Server **2008 R2**: совместимость синтаксиса обязательна. Триггеры и боевую схему без отдельного согласования не менять. Не создавать тестовые записи и не отключать триггеры без прямой санкции.

Этот раздел отменяет старые замечания **MENU-04** (снятие общего AppBar/drawer) и **MENU-07** (отдельный корень «Операции») из `INVENTORY_MENU_INDEPENDENT_REVIEW_2026-11-20.md`. Агенту запрещено трактовать их как открытые задачи.

## 1. Реально проверенные SHA / точная база для работы

| Репозиторий | SHA текущей опубликованной версии |
|---|---|
| `Postnn/clayzor` | `2d13f904cd4c5b65508243e5d234468e509e80c2` |
| `Postnn/Kesco.App.Web.Inventory` | `aa37f25366271d0d200bfa2eb68c4fd98e15540a` |
| `Postnn/Clayzor.Lib.Web.Controls` | `03750bc9c6d25065541335febb19181e16fd86fa` |
| `Postnn/Clayzor.Lib.Entities` | `46cce480349e55a82335dac5b7943fe60ac2f161` |
| `Postnn/Clayzor.Lib.Web.Controls.Tests` | `0894fc0018bc3bfc2a57a7f07a125e2d9c2c44eb` |
| `Postnn/Clayzor.Lib.Web.DBSelect` | `8ae98fb8c2a5fea20515a21895c1df9791c46327` |

Gitlink суперпроекта сверены: в указанном суперпроекте submodule pointers соответствуют перечисленным версиям. Новое приложение `src/Clayzor.App.Web.Styles` входит в **сам суперпроект**, а не в сабмодуль.

Изучены новые коммиты 20.11.2026: `Inventory: компактная Home, темы и локальные иконки`, `Menu: финальные UI-исправления и recoverable SQL ошибки`, `Tests: финальные регрессии меню, тем и SQL ошибок`, `Inventory: завершить меню и добавить Styles`.

Эталон legacy: `KescoDev/Kesco.App.Web.V4.Inventory/Inventory/default.aspx(.cs)`, `Inventory/Default/Grid.aspx(.cs)`, `Kesco.Lib.Web.Menu.Inventory.V4/MainMenu.cs`, `Kesco.Lib.Web.Controls.V4/MenuSimple.cs`, `Kesco.Lib.Entities/Corporate/Queries/Query.cs`, `Kesco.Lib.Entities/SQLQueries.cs`, `Kesco.Lib.Web.Controls.V4/Grid/Grid.cs` (`ClearFormParams`); `KescoDev/Styles/js/Kesco.Menu.js`, `Kesco.V4.js`, `css/Kesco.css`.

## 2. Что из предыдущего аудита исправлено / принято

**MENU-02 — технически исправлено в коде:** `MainLayout.ToggleDarkModeAsync()` теперь меняет `_isDarkMode`, вызывает `InvokeAsync(StateHasChanged)` родительского layout, сохраняет выбор в `localStorage`. `EntityFormLayout` читает тот же ключ при первом интерактивном рендере. `Home` передаёт кнопку через `AdditionalContent` корневой полосы `ClayNavigationMenu`, а не рисует её вне меню. В `InventoryHomeTests`/`ClayNavigationMenuTests` нет полной проверки вычисленных CSS-переменных темы: сохранить короткую регрессионную проверку, но не возвращаться к старой реализации.

**UI/компоновка:** `ClayGridOptions.CompactLayout` и `FillAvailableHeight`, уменьшенная геометрия меню/пагинации, PageTitle через `TitleChanged`, тема справа в общей полосе, раздельный `EntityFormLayout`, фиксированный popup с scroll и flip — подтверждено чтением исходников. Агент заявляет браузерные замеры; эти замеры не воспроизводились независимым аудитором.

**Новый Styles-host** реализован в `src/Clayzor.App.Web.Styles`, обслуживает каталог из `Styles:PhysicalPath` по `/styles`. По отчёту агента локальные значки `Notebook.gif` и другие реально загружались (`naturalWidth=16`). Сам принцип единого ресурса Styles между приложениями разумен. **Но текущие значения окружения пригодны только для локального браузера на той же машине**, см. MENU-V2-04.

**SQL error/retry:** `ClayGrid.Dynamic.cs` перехватывает `SqlException`, показывает recoverable alert с «Повторить», `RetryDynamicAsync` повторно инициирует тот же компонент, busy-overlay снимается. Дополнительные тесты на recovery/маскировку пароля действительно добавлены. **В интерфейсе появилась новая потенциальная утечка подключения**, см. MENU-V2-03.

**Прежние корректные контракты оставляем:** типы 1/2/3, сортировка `ТипЗапроса, КодЗапроса`, `/?id=N` для default dynamic grid, `idQuery=N` для внешней формы, контекстный отчёт без `idQuery`, `URI_inventory_queries` с `_idQuery=NE`, aria-current по ID, отсутствие отдельного корня «Операции», вложенность/hover/scroll и независимость instances. Новые тесты этих контрактов не заменять декоративными.

## 3. Открытые замечания по исходникам и недоказанные интеграционные условия

### MENU-V2-01 — P1: сброс грида удаляет forced URL-фильтр, не изменяя URL

**Файл:** `Postnn/Clayzor.Lib.Web.Controls/Components/Grid/ClayGrid.Dynamic.cs`, методы `ClearDynamicStateAsync`, `ApplyUrlParams`, `SaveDynamicState`, `LoadDynamicData`.

`ApplyUrlParams` добавляет фильтр без подчёркивания (`ColumnUrlKey=op~value`) в `_filterRoot`, регистрирует `InvFilter_<GridId>` в `_dynamicForcedParamNames`, и `SaveDynamicState()` не должен сохранять принудительно навязанное состояние как обычное пользовательское. Однако при очистке исполняется:

```csharp
_filterRoot = new ClayFilterGroupNode();
_dynamicSavedParams.Clear();
_dynamicForcedParamNames.Clear();
await SaveDynamicState();
await RefreshQuickSearchEffective(opt);
await NotifyQueryChanged();
```

После reset `ApplyUrlParams` **не вызывается**, новый SELECT выполняется без forced-условия на **том же URL**. Повторная загрузка страницы снова может применить forced filter, но это не оправдывает неправильный текущий результат.

**Обязательное исправление:** разделить пользовательские настройки и инварианты URL. После очистки и до первого SELECT повторно применить актуальные forced URL-фильтры и forced columns к определению данного грида; не сохранять их в личные `vwНастройки`. Не возвращать отменённые пользовательские фильтры и прежний page size. Учитывать специфику `sharedId` (для shared reset недоступен), смену GridId/CLID, отмену диалога. Не выполнять повторную загрузку определения/создавать второй экземпляр грида только ради исправления сброса.

**Тест:** через реальный `ClayGrid` и scripted SQL (не тестом одного parser) открыть URL с forced-фильтром, выполнить toolbar reset и подтверждение, убедиться, что SQL фактической загрузки по-прежнему содержит условие и параметр; пользовательские фильтры и размер страницы сброшены, `_dynamicForcedParamNames` соответствует URL, персональный `InvFilter_<GridId>` не загрязнён forced-значениями; после refresh URL по-прежнему применяет forced. Покрыть простой grid, другой GridId и другой CLID.

### MENU-V2-02 — P2: default URL-фильтр проверяется не против того формата сохранённых ключей

**Файлы:** `ClayGridUrlFilterParser.Apply`, `ClayGrid.Dynamic.cs::ApplyUrlParams`, `ClayGridUserParamsData.LoadAsync`.

`ClayGridUrlFilterParser.Apply()` для default URL-параметра `_ColumnUrlKey=...` проверяет:

```csharp
if (pf.IsDefault && savedUserParams.ContainsKey(pf.UrlKey)) continue;
```

Но реальный вызывающий код передаёт `_dynamicSavedParams`, где имена — `InvFilter_<GridId>`, `InvFields_<GridId>` и т. п., **а не** URL-ключ конкретной колонки. Тест `UrlFilterParserTests.Apply_DefaultKey_HasSaved_SkipsCondition` подсовывает искусственный словарь `{"col":"saved_value"}`; он не доказывает интеграционную семантику. В результате default-условие обычно накладывается даже при сохранённых настройках пользователя.

**Действие:** установить и документировать, что именно означает «сохранённый фильтр для данного URL-ключа» в существующем контракте Grid: наличие общей сохранённой filter-сериализации либо конкретного условия внутри неё. Выбрать вариант, **следующий уже реализованной семантике legacy / G8**, а не произвольную новую. Исправить слой согласования данных/ключей; не ломать forced-фильтры и user settings. Тестировать совместно `_ColumnUrlKey` с реальным serialized `InvFilter_1001` в вариантах: отсутствует, пустой, содержит условие на ту же колонку, содержит условие на другую колонку, после reset. Если неясно, какая семантика legacy, остановиться и спросить владельца до изменения поведения.

### MENU-V2-03 — P1 (безопасность и граница слоёв): строка подключения и сырой SQL error выводятся в UI

**Файл:** `ClayGrid.Dynamic.cs`, `ShowDynamicLoadError`, `MaskConnectionStringPassword`; разметка `ClayGrid.razor`.

Новый код добавляет пользователю в `MudAlert`:

```csharp
$"Ошибка: {exception.Message}\n" +
$"Строка подключения: {MaskConnectionStringPassword(Db.ConnectionString)}"
```

Маскируются только значения `Password`/`Pwd`; сервер/имя БД/пользователь/параметры подключения и диагностические сведения остаются **доступны всем пользователям формы**. Это ненужное раскрытие внутренних параметров SQL Server. Пароль как минимум маскируется, но это не делает весь connection string безопасным для UI. Не делать вид, что этот риск закрыт тестом `MaskOnlyPasswordValue` — тест как раз закрепляет демонстрацию сервера и БД.

**Действие:** сохранить recoverable SQL alert и кнопку «Повторить», но разделить **общедоступное сообщение** и **серверную диагностику**. По умолчанию показывать запрос/код и понятную ошибку «Не удалось загрузить данные; повторите попытку», без строки подключения и необработанных деталей SQL. Детали `SqlException`/контекст подключения — только в защищённом server-side ILogger/диагностике с учётом политики логирования секретов, без отдачи в DOM, JavaScript, HTML source, console и network response. Если владелец желает видеть подробности прямо в UI разработчика, предусмотреть **явно авторизованный диагностический режим** после согласования, не включать его для всех пользователей. Все прежние recovery и reuse same component regression tests сохранить, добавить тест на отсутствие `Server=`, `Database=`, `User Id=`, `Password=` и raw exception text в обычном DOM.

### MENU-V2-04 — P1 для удалённых пользователей / P2 для локального dev: localhost Styles недоступен клиентам

**Файлы:** `Postnn/clayzor/src/Clayzor.App.Web.Styles/Program.cs`, `appsettings.json`; `Inventory/web.config` (`URI_styles=http://localhost:5040/styles`), `Inventory/Program.cs`, `InventoryMenuUrlBuilder.ResolveIcon`.

Styles app запускается `app.Run("http://localhost:5040")` и обрабатывает `RequestPath = "/styles"` из `C:\CLAYZOR-KIMI\Styles` (путь конфигурируется). Это подтверждает локальный HTTP 200 **на машине агента**. Но `<img src="http://localhost:5040/styles/...">` в браузере удалённого сотрудника обращается к **его** localhost. Для удалённого браузера рабочие иконки не гарантированы; HTTPS Inventory + HTTP Styles дополнительно даёт mixed-content/cross-origin особенности.

**Действие:** оставить решение об отдельном Styles-сервисе, если оно действительно нужно нескольким приложениям, но сделать URL доступным из **реального клиентского браузера**. Допустимо корректное опубликованное общесетевое HTTPS-имя или same-origin маршрут/reverse-proxy; **не выдумывать** имя сервера, порт или расположение. `Styles:PhysicalPath`, bind address, внешний URI и окружение должны быть конфигурируемыми и согласованными между dev/test/work. Для локального dev разрешён `localhost`, но он не должен быть заявлен универсальным способом развёртывания. Проверить загрузку пиктограмм *с отдельной клиентской машины*, отсутствие blocked mixed content, верный MIME, 200, `naturalWidth>0`; один адрес должен корректно работать в Inventory и хотя бы в другом приложении, если оно подключает этот общий Styles. Если окружение недоступно — честно зарегистрировать deploy blocker, без поддельного pass.

### MENU-V2-05 — P2: контекст Windows-групп берётся из `HttpContextAccessor` внутри Interactive Server circuit

**Файл:** `Inventory/Components/InventoryMainMenu.razor.cs::LoadAsync()`.

Пункт настроек вычисляется `SettingsRoles.Any(role => HttpContextAccessor.HttpContext?.User.IsInRole(role) == true)` **после `OnAfterRenderAsync`**, в интерактивном компоненте. Согласно Microsoft, `IHttpContextAccessor` не гарантирует актуальный `HttpContext` для Interactive Server. Это может убрать пункт у авторизованного пользователя или использовать устаревший контекст. Бизнес-правила отображения остаются legacy: ровно `BUILTIN\Administrators`, `TEST\Programists`, `EURO\Programists`, `TEST\SupportTeam`; ни одной новой группы/роли.

**Действие:** читать principal из `AuthenticationStateProvider` / `CascadingAuthenticationState`, корректно зарегистрированного для текущего server circuit (или согласованного общего `Employee CurrentUser`, если к моменту работы он уже внедрён). Не создавать независимую систему прав на основе SQL, не дублировать встроенную авторизацию целевого приложения; для UI-видимости соблюдать указанные группы. Учесть `authStateChanged`, reconnect/circuit, анонимного пользователя и `IsInRole` на реальном WindowsPrincipal; отсутствие/неизвестность идентичности — не показывать пункт. Обязательно browser/integration auth tests для каждой legacy-группы + без роли, включая навигацию `/` → `/?id=1001`.

**Источник ASP.NET Core .NET 10:** https://learn.microsoft.com/en-us/aspnet/core/blazor/components/httpcontext?view=aspnetcore-10.0.

### MENU-V2-06 — P2: обработка клавиатуры останавливается на disabled-ссылке

**Файлы:** `ClayNavigationMenuPanel.razor`, `ClayNavigationMenu.razor`, `wwwroot/js/clayNavigationMenu.js::focusItem`.

Disabled leaf отрисован как `<a role="menuitem" href=null aria-disabled="true" disabled>`. Для `<a>` без `href` нативный `disabled` не работает. `focusItem` выбирает подряд все элементы по `itemSelector`, даже недоступные, и пытается `.focus()` на не-фокусируемой ссылке. Доказанного браузерного воспроизведения в этих ревизиях нет — **это обоснованный дефект по DOM/JS, подлежащий воспроизведению**, а не заявленный browser failure.

**Действие:** корректно исключать недоступные пункты из arrow/Home/End navigation либо поддерживать ARIA keyboard design с явно фокусируемым `aria-disabled` и без активации, в зависимости от принятого контракта `role=menuitem`. Сохранить mouse hover, Escape, `Tab` и 80 пунктов с прокруткой. Добавить bUnit-тест разметки + **реальный браузерный** сценарий enabled / disabled / enabled и многократные Down/Up/Home/End/Enter/Space. Убедиться, что фокус не зависает и disabled не активируется.

### MENU-V2-07 — P2: реальные ссылки, SQL-персистенция и старые исключения — внешние зависимости

**Подтверждено текущей документацией агента**, не независимым SQL-доступом:

- **9 URI-ключей не имеют значения:** `URI_area_search_tree`, `URI_equipment_type`, `URI_inventory_fact`, `URI_inventory_rpt_modifications`, `URI_inventory_rpt_sockets`, `URI_location_search`, `URI_maintenance`, `URI_sim_noservices`, `URI_inventory_queries`. Disabled с явным `title` — правильное безопасное поведение при отсутствии URI, **не дефект кода**. Нужна фактическая конфигурация legacy `uri.local`/IIS и разрешение владельца на проверку живых ссылок; значения не придумывать.
- **Запрос 1004** ссылается на отсутствующий `SUSDB.dbo.vwrpComputerTarget`. Это проблема данных/окружения, а не причина переписывать menu builder. Не менять SQL определения запроса 1004 без отдельного задания.
- **Параметры** `InvRows_1001`, `InvFields_1001`, `InvFilter_1001`, `InvSort_1001`, `InvGroup_1001`, `InvQuickSearch_1001` отсутствуют в `НастройкиПараметры` по SQL-отчёту агента. `ClayGridUserParamsData.SaveManyAsync` действительно выполняет INSERT в `vwНастройки`, полагаясь на существующий INSTEAD OF trigger. Если trigger игнорирует неизвестные ключи, `25→50` после reload недостаточно для подтверждения persistence. Требуется read-only сравнение эффективных значений *до* и *после* штатных пользовательских операций через UI, на согласованном тестовом CLID и нескольких GridId; проверить точные строки БД, а не только визуальное значение. Добавление ключей или изменение триггеров — **отдельно согласовать с владельцем**, не выполнять скрытно.
- `return=2`, `hidemenu`, `hidetitle` должны быть проверены в **реальном embedding host**; отсутствие прежнего фиксированного margin является сознательным следствием flex-компоновки и не должно автоматически считаться дефектом. Блокер только если реально перекрываются кнопки/пагинация или нарушен обмен с внешним хостом.

### MENU-V2-08 — P3: производительность нового глобального tooltip viewport script (проверить, не переписывать вслепую)

**Файл:** `Postnn/Clayzor.Lib.Web.Controls/wwwroot/js/clayTooltipViewport.js`, подключён глобально в `Inventory/Components/App.razor`.

Новый `MutationObserver` слушает `document.body` с `{childList:true, subtree:true, attributes:true, attributeFilter:["style","class"]}` и при **каждом** изменении пересканирует `.mud-tooltip.mud-popover-open`. На страницах большого динамического грида атрибуты/DOM меняются часто, даже когда открытого tooltip нет. Это потенциальная нагрузка, **не подтверждённая измерениями**. Сначала проверить browser performance (navigation/scroll/filter/1000+ строк, без открытого tooltip, с одним tooltip); если нагрузка существенная — ограничить наблюдаемую область или объединять операции в один animation frame и выполнять при наличии popup. Не убирать необходимое позиционирование вслепую.

## 4. Целевое поведение: тест-матрица для приёмки

| Контракт | Ожидаемое поведение / доказательство |
|---|---|
| `/?id=1001&Department=eq~X` | Forced-фильтр в SQL до reset, сразу после reset и после refresh, не пишется как пользовательская настройка |
| `/?id=1001&_Department=eq~X` + persisted state | Default-фильтр применяется строго по G8, доказано на реальном формате `InvFilter_1001`, не словарём `{"Department":"..."}` |
| Отмена подтверждения Reset | Ни один SQL write и данные/состояние не изменяются |
| Reset 1001/CLID=1 | Не затрагивает 1012/CLID=1 и 1001/CLID=2; shared не меняется |
| SQL 208/connection error | Recoverable сообщение, retry на том же компоненте и без утечки connection string в DOM |
| Тема | Изменяется реальный `MudThemeProvider` и палитра; сохранение при перезагрузке; `EntityFormLayout` следует выбору |
| Windows principal | Пункт «Настройки» точно для четырёх legacy-групп, отсутствует у остальных; работает в interactive circuit |
| Styles из другого клиента | Запрос картинки идёт к опубликованному адресу, не localhost клиента; HTTP 200/HTTPS без mixed content |
| Выпадающее меню | 320/360/600/900/1155/1440 px и zoom 125/200%; scroll до последнего пункта, flip и ESc/Tab/mouse/touch, два независимых экземпляра |
| Disabled между enabled | Стрелки не застревают и недоступный пункт не активируется |
| Общее оформление | Нет возвращённых AppBar/drawer/корня «Операции»; тема в строке меню; один dynamic grid |
| Интеграция / Embed | `hidemenu`, `return`, `return=2`, `hidetitle`, header и pagination без перекрытий в реальном host |
| Настройки БД | Доказана реальная персистенция через read-only проверку данных после UI Save/Reset либо явно указан блокер схемы |

## 5. Готовый подробный промт агенту

### Роль и рамки задачи

Ты — Codex-агент, исправляющий **только подтверждённые проблемы повторного независимого аудита меню Inventory**. Исходная реализация уже архитектурно правильно разделена. **Не создавай новую компоненту вместо существующей**, не переписывай универсальную `ClayNavigationMenu`, не возвращай старые AppBar/drawer и самостоятельный корень «Операции».

Рабочая версия — `Postnn/clayzor@2d13f904` и перечисленные в §1 gitlink SHA. Перед правками проверь актуальные `git status`, `git branch --show-current`, `git submodule status` и новые HEAD; не перетирай параллельные изменения. Изучи `AGENTS.md` суперпроекта, `STYLE_RULES.md`, `Entities/AGENTS.md`, `Controls/AGENTS.md`, `Controls/Grid/AGENTS.md`, `Inventory/AGENTS.md`. Не нарушай правило: SQL только в Entities/SQLQueries; shared UI в Controls; Inventory-only composition в Inventory. **Любые commit/push/merge — только по отдельной прямой команде владельца.** Не менять schema, справочники и тестовые данные без разрешения.

### Этап I — восстановить контракты forced/default URL-фильтров и reset (обязательный)

1. До редактирования проведи трассировку `OnParametersSetAsync → InitDynamicMode → RestoreDynamicState → ApplyUrlParams → LoadDynamicData → SaveDynamicState`, включая URL, GridId/CLID/sharedId, serialized фильтр и состояние `_dynamicForcedParamNames`. Запиши короткую матрицу с фактическими ключами параметров.
2. Напиши **падающие регрессионные тесты на реальном компоненте**: forced-фильтр теряется при Reset; default URL-фильтр неправильно конкурирует с сохранённым `InvFilter_1001`. Тесты обязаны проверять фактический SELECT/SQL parameters и пользовательское состояние, а не только `ClayGridUrlFilterParser.Parse()`.
3. Исправь reset без поломки shared/save. Принудительный фильтр после Reset остаётся в первом же SELECT; сохранённое user-state очищается по текущему GridId/CLID; forced не сохраняется как персональная настройка. Для default URL-фильтра реализуй именно подтверждённую договорённость G8. Если существует неоднозначность между «сохранённый фильтр всей формы» и «сохранённое условие отдельной колонки», **не выбирай самостоятельно** — задай точный вопрос и приложи две проверенные альтернативы.
4. Повтори сценарии switching 1001→1012→1001, CLID и shared ссылки; проверь отсутствие дублей definition/columns и вечного busy-overlay.

### Этап II — привести recoverable SQL errors к безопасной UI-диагностике (обязательный)

1. Сохрани возможность Retry без уничтожения компоненты и без потери контекста запроса.
2. Не показывай обычному пользователю ни `Db.ConnectionString`, ни серверные имена/DB/UserId, ни необработанный SQL stack/внутренние детали. Диагностику вносить в **серверный** лог с correlation identifier и исходным `SqlException`; не логировать секретные значения без действующей политики sanitization.
3. Если детальная UI-диагностика уже была явно потребована владельцем, согласуй отдельный авторизованный developer-only режим. Пока нет такого требования — fail closed для показа деталей.
4. Добавь тесты на отсутствие connection string в HTML/DOM, на ошибку 208, connectivity, retry и последующий успешный переход 1025→1001.

### Этап III — Windows principal и пункт настроек (обязательный)

1. Убери получение principal через `IHttpContextAccessor.HttpContext` из `InventoryMainMenu.LoadAsync` интерактивной фазы. Прочитай доступный `AuthenticationStateProvider` и согласованную модель `Employee CurrentUser`, если она уже реализована другими агентами; повторный механизм ролей не создавать.
2. Сохрани точные четыре legacy Windows-группы, условие `IsInRole`, порядок и наличие пункта в меню. Не добавляй C#-проверок прав на SQL Server — это не задача этой доработки; защищённость целевого приложения/процедур сохраняется своим существующим механизмом.
3. При auth state change / revalidation обновляй доступность пункта без перезаписи чужого меню. Проверь anonymous, user with groups, user without groups, reconnect.
4. Подтверди интеграционной проверкой Windows principal на тестовом стенде. Если `AuthenticationStateProvider` ещё не настроен, предоставь минимальный проверенный план DI и тест на него; не угадывай, что пользователь всегда аутентифицирован.

### Этап IV — Styles и браузер из другой машины (обязательный для deploy readiness)

1. Раздели «локально работает на машине разработчика» и «доступно браузеру сотрудника». Объясни, почему `localhost:5040` в HTML пользователя непригоден для удалённых клиентов.
2. Исследуй фактический целевой host для Styles, существующие IIS bindings/reverse proxy и действующий legacy `uri.local`, не придумывая URL. При наличии конфигурации настрой `URI_styles`, bind address и публикацию по согласованной среде. Не открывай Windows-каталог произвольным внешним клиентам без оценки доступа: публикуй только необходимые статические файлы.
3. Проверь из отдельного браузера/хоста фактическую загрузку иконок, HTML `src`, response/MIME, `naturalWidth`, CSP/mixed content. Локальный HTTP 200 **не считается достаточным** доказательством.
4. Если реальные URI ещё не предоставлены, оставь пункт «Не подтверждено до получения конфигурации» и список конкретных ключей, без фальшивого pass.

### Этап V — accessibility, интеграция и живые настройки (обязательный для полного приёма)

1. Воспроизведи disabled между enabled в меню клавиатурой и исправь navigation; не трогай уже работающий hover/scroll/flip без необходимости.
2. Зафиксируй данные по SQL-персистенции `InvRows_1001`/`InvFields_1001`/`InvFilter_1001`/`InvSort_1001`/`InvGroup_1001`/`InvQuickSearch_1001`. После штатного UI Save/Reset сделай **read-only** проверку фактических строк. Не изменяй `НастройкиПараметры`, `INSTEAD OF` triggers и схемы автоматически; если ключей нет — отдельный вопрос владельцу с SQL-результатами и минимальным DDL/DML проектом **без выполнения**.
3. Отдельно проверь embedding `return=2` на реальном host; `hidemenu`, `hidetitle` и pagination. Не требуй возврата фиксированного legacy margin только ради буквального совпадения CSS.
4. Изучив новый глобальный `clayTooltipViewport.js`, измерь, не влияет ли его `MutationObserver` на работу большого грида. Меняй только при воспроизводимом дефекте производительности.
5. Проверь регрессии `MainLayout` и `EntityFormLayout` — без требования вернуть старый AppBar/drawer. Проверяй темы и меню на Home, формы продолжают открываться и сохраняться.

### Этап VI — итоговая приёмка и отчёт

1. `dotnet build Clayzor.sln --no-restore` — 0 ошибок.
2. Профильные тесты: `InventoryMenuTests`, `ClayNavigationMenuTests`, `InventoryHomeTests`, `UrlFilterParserTests`, `ClayGridReinitTests`, `ClayGridSectionTests` + новые интеграционные, затем расширенный профиль. Не повторять утверждение о полном `dotnet test`, если testhost не запускался.
3. Браузер: минимум 320×568, 360×640, 600×700, 900×400, 900×700, 1155×750, 1440×900; zoom 125%/200%; длинные названия ellipsis/title, wheel, клавиатура, touch, второй инстанс, последняя строка, темы и текущая страница. Не коммитить screenshot/artifacts в основную ветку.
4. SQL: read-only отчёт по 78 пунктам из `Запросы` (24/42/12), реальному состоянию `НастройкиПараметры`, триггерам, 1004 и внешним URI. Не производить неконтролируемых DML.
5. Сопоставить матрицу legacy → Clayzor: `MainMenu` assembly, MenuSimple, CSS/JS, routing, SQL, текущие intentional design deviations (глобальный no-AppBar/drawer; no separate «Операции»).
6. Обновить `Inventory/docs/inventory-menu.md` **только если владелец явно разрешает документационные изменения**; если нет — подготовить исправленный текст отдельным отчётом. Не копировать в документацию фразу «не коммитили», поскольку реализация уже опубликована в `main`.
7. Приложить проверяемый отчёт: issue ID, подтверждённые причины, diff/files, тест/результат, реальные UI и SQL evidence, оставшиеся неопределённости. Не объявлять deployment и полное legacy parity без подтверждения девяти URL, Styles из удалённого браузера и персистенции настроек.

**Границы:** не возвращать AppBar/drawer/«Операции»; не менять бизнес-сущности, таблицы и триггеры без согласования; не создавать параллельную систему авторизации; не превращать ошибку SQL в 500/падение circuit; не делать код-рефакторинг несвязанных форм. Если обнаружишь противоречие данных/спецификаций, задай конкретный вопрос с точными исходниками вместо домысливания.

## 6. Что считается закрытым, что нет

| Область | Статус после новых коммитов |
|---|---|
| Архитектура общей компоненты / DB queries / Inventory composer | **Принимается** |
| Удаление AppBar/drawer | **Согласовано владельцем; не дефект** |
| Корень «Операции» отсутствует; reset в групповых операциях | **Согласовано владельцем; не дефект** |
| Тема: parent rerender/localStorage/форма | **Исправлено по исходникам**, независимый браузерный прогон не выполнен |
| Компактная Home/PageTitle, scroll, popup, nested menus | **Изменения присутствуют**, геометрия в браузере — по отчёту агента |
| Recoverable SQL error + Retry | **Функция есть**, но UI показывает внутреннюю строку подключения → **открыто** |
| Forced filter после reset | **Открытый P1**, подтверждено кодом |
| Default URL-filter vs persisted filter | **Открытый P2**, подтверждено несоответствие ключей в интеграции |
| Windows principal интерактивного меню | **Открытый P2**, подтверждён риск реализации |
| Styles-host для браузера другого компьютера | **Открытый deploy blocker**, localhost dev-only |
| Disabled keyboard navigation | **Требует браузерной регрессии / исправления** |
| Девять URI-ключей, запись параметров, 1004, embedded host | **Внешние/интеграционные ограничения**, не выдумывать решения |

**Итог:** меню не требует новой архитектуры; нужен ограниченный corrective pass. До него **не заявлять 100% соответствие legacy и готовность remote deployment**, но не инициировать очередное переделывание согласованного UI.
