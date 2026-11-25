# Clayzor Themes 1 — интеграция

## Состояние

Изменения из изолированного дерева `C:\CLAYZOR-KIMI\Clayzor-themes` перенесены в актуальный `main` после завершения EQ9. Merge и rebase не выполнялись. Общий `wwwroot/css/clay.css` не перезаписывался: тематический блок добавлен после существующих правил, блок `.clay-select-read-error` и повтора сохранён.

Реализованы Classic Light/Dark, Legacy Light, Corporate Light и Modern Light. Общие `ClayThemeHost`, `ClayThemeSelector` и scoped `ClayThemeState` подключены к Inventory и Users. Выбор хранится отдельно в `InvTheme` и `UsersTheme` через существующий `vwНастройки`, с `КодНастройкиКлиента=0`, `КодНастройкиОбщей=0`, штатным INSERT-upsert и контрольным чтением. Доступ к настройке выполняется только после сопоставления Windows principal и текущего сотрудника. Старый `clay-inventory-theme` больше не используется как источник темы.

Добавлен отдельный лабораторный хост `tests/Clayzor.ThemeStudio`; он не включён в production-маршруты и не имеет хранилища пользовательской темы. Запуск:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project tests/Clayzor.ThemeStudio --urls http://localhost:5050
```

## Опубликованные сабмодули

| Сабмодуль | SHA |
|---|---|
| Controls | `e3d600b890ba66f972b1216447832cc609697a96` |
| Entities | `95ef0cfa4532afe338af9434691ec527cae9a071` |
| Inventory | `01b2f41f9566f9146091144771341ed79df176cf` |
| Users | `1001d2c5b2c8ebb8041325393d1a3b883e883c1c` |
| Tests | `b90a635b043d1130d8ab3c47a25cc3e475d57c8b` |

## Проверки объединённого кода

- `dotnet build Clayzor.sln --no-restore` — 0 ошибок; StyleGuard выполнялся в проектах, где уже подключён.
- `dotnet build tests/Clayzor.ThemeStudio/Clayzor.ThemeStudio.csproj` — 0 ошибок. Первый запуск с `--no-restore` до создания `project.assets.json` ожидаемо потребовал обычный restore; после него сборка успешна.
- Профиль `FullyQualifiedName~ClayTheme|FullyQualifiedName~InventoryHomeTests` — 20/20 passed, в том числе повторно после XML-документации.
- Регресс `FullyQualifiedName!~ClayTreeMutationAbortTests` — 1176/1176 passed. Известное несвязанное зависание `ClayTreeMutationAbortTests` не запускалось.
- Theme Studio в Chrome: все пять тем при ширине 1365 и 390 px, один provider, отсутствие горизонтальной прокрутки документа, сохранение экземпляров грида/дерева/ввода, выбора ClaySelect, черновика диалога и состояния дерева. Открыты диалог расположения, фильтр колонки и составной фильтр. JS/Blazor-ошибок в итоговом сценарии нет.
- Inventory: главная, динамический грид 1001, ClaySelect и EquipmentForm `id=8` открыты с Classic Light и одним theme provider. При старом localStorage `dark` применился согласованный Classic Light.
- Users: маршрут поиска открыт с Classic Light и переключателем темы.

## Не закрыто

- THEME-F01/F02 на реальных EquipmentForm и динамическом гриде с несохранённым вводом и настроенным состоянием не выполнялись: переключение в рабочих приложениях вызывает запись пользовательской настройки, а разрешённый SQL-стенд для этой приёмки не назначен. Общие механизмы сохранения состояния проверены в Studio.
- THEME-S02…S06: реальные upsert обоих ключей, новый вход, второй браузер и изоляция двух Windows/SQL-пользователей не выполнялись. Unit-тесты и read-only проверка контракта не заменяют эту интеграционную приёмку.
- Перезапуск IIS, обрыв сети и независимые инфраструктурные сценарии F06 не выполнялись.
- Внешний вид тем должен быть принят пользователем в реальном приложении; публикация интеграции не означает визуальную приёмку.

Inventory (`5020`), Users (`5030`) и Styles (`5040`) перезапущены на объединённых бинарниках. Временный Theme Studio `5050` остановлен. Отдельные процессы исходного стенда тем `5510/5520/5540` не останавливались.

## TH2 — исправления по `Clayzor_Themes_2_rev2.md`

Worktree `C:\CLAYZOR-KIMI\Clayzor-themes-2`, суперпроект `dea7746576e06ce0fcf186b823372dec1fd4de90`. Исходные SHA: Controls `b3616aa`, Entities `95ef0cf`, Inventory `01b2f41`, Users `1001d2c`, MedicalTests `4f87382`, Tests `d0f3347`. Параллельные изменения EQ10 в исходном checkout не переносились. Исходники EquipmentForm, общие draft service/JS, `SQLQueries.cs` и бизнес-SQL остались у их владельцев. Коммитов, push, merge и развёртывания для TH2 нет.

### Контракт и изменения

- Inventory и Users вызывают одну регистрацию `AddInventoryThemePreferences`. Она создаёт scoped состояние и общий store. После смены решения владельца код читает и записывает только `ClayTheme`, с `КодНастройкиКлиента=0` и `КодНастройкиОбщей=0`. `ClayThemeSettingsData` явно квалифицирует `[dbo].[vwНастройки]`, каталог фиксируется проверенной строкой Integrated Security. Запись идёт через штатный INSERT-upsert и подтверждается контрольным чтением. Windows/SQL principal и сотрудник проверяются до доступа, имя приложения в хранилище больше не передаётся.
- Read-only проверка схемы: `vwНастройки` ограничивает строки текущим SID, включённый `tНастройки_InsteadOf_Ins` получает текущего сотрудника по `SUSER_SID()`, требует регистрацию параметра и для типа `0` принудительно ставит CLID `0`. Пользователь уже зарегистрировал `ClayTheme` типа `0`, поэтому регистрационный скрипт и DDL не понадобились. До приёмки в тестовой БД был один старый `InvTheme` и ни одного `UsersTheme`. Старые ключи проверены снимком текущего пользователя до и после теста. Автомиграция отсутствует. Если нужна миграция, отдельно согласовать копирование только при отсутствии канонической строки и правило решения конфликтов.
- `MedicalTests` использует другой каталог. Тестовый Windows login получил отказ при открытии `МедицинскиеАнализы`. Конфигурация общего trusted connection для этого host не подтверждена. Его прежний переключатель сохранён без расширения полномочий.
- `clay.css`: собственные пары текста и фона для `chip-sort-badge` и `chip-group-badge`. Активный `clay-vf-icon--active` получает акцентный fill, светлую подложку и обводку без смены геометрии. Неактивный маркер остаётся в паре шапки. Classic сохраняет исходные маркеры. По `Compare-browser.log` прежние Legacy/Modern давали 1:1 для бейджей и одинаковый fill для обеих иконок. Новые пары с белым: Legacy `#2574BA` — 4.90:1, Corporate `#58656E` — 6.00:1, Modern `#007F69` — 4.95:1. Legacy-шапка `#4078A8` с белым текстом — 4.70:1 против 2.87:1 у первоначального `#629ED0`. Сохранённая пара Classic Dark `#FFAD00` / `#4A7CFF` у старых бейджей имеет 2.00:1 и требует отдельного решения, если Classic допустимо менять.
- Согласованный жёлтый `#FFFF99` сохранён. Для Mud-полей живое состояние пустоты определяется `:placeholder-shown` с пробельным placeholder, который не меняет условие `mud-shrink`. Для простых нативных полей и ClaySelect проверка required ограничена допустимыми типами. Непустой невалидный email остаётся на обычном фоне и сохраняет нативную невалидность. Поля readonly/disabled исключены, числовой 0 и выбранный ключ 0 сохраняются. В Dark на жёлтой подложке видны caret, текст, placeholder и кнопки внутри поля. Плавающие подписи размещаются над заливкой с фоном существующей поверхности. [Условие плавающей подписи в MudBlazor 9.14.0](https://github.com/MudBlazor/MudBlazor/blob/v9.14.0/src/MudBlazor/Components/Input/MudInput.razor.cs#L30-L37)

### Проверки и артефакты

`dotnet build Clayzor.sln --no-restore` и сборка `tests/Clayzor.ThemeStudio/...csproj` прошли. `dotnet test ... --filter 'FullyQualifiedName~ClayTheme|FullyQualifiedName~ClayRequiredFieldStyle|FullyQualifiedName~ClayFormControlsTests|FullyQualifiedName~InventoryHomeTests'` дал 40 passed. Полный `dotnet test Clayzor.sln --no-build --no-restore --blame-hang-timeout 45s --blame-hang-dump-type none` дал 1187 passed до таймаута в `ClayTreeMutationAbortTests.AddChild_SqlException_DoesNotRunSuccessPath`. Прогон `!~ClayTreeMutationAbortTests` дал 1186 passed и один отказ `Eq8ServerGuardsComputerResponsibleTests.WithdrawValidGap_ComposesAtomicSave` («Действующий ответственный изменился»). Этот отказ отдельно воспроизведён в сборке до TH2 из worktree `Clayzor-theme-fixes`. Эти тесты и код оборудования не исправлялись в задаче тем.

Приложения: Studio `5710`, Inventory `5720`, Users `5740`. Изолированная QA-сборка Inventory `5721` использует тот же провайдер и настоящие формы/грид. Её временные элементы измеряют `ServerRoundtrips` прикладного scoped `DbManager` и открывают селектор внутри окна EquipmentForm. Для QA theme state хранится в памяти, чтобы визуальные тесты не меняли пользовательскую настройку после SQL-smoke. Файлы временных layouts/Program восстановлены. Сценарии и результаты в `TestResults/Theme2`.

SQL-smoke: Chrome + Windows Integrated Auth под одним тестовым сотрудником. Все пять значений сохранены через Inventory, затем независимый профиль Chrome получил то же значение в Users. Выбор из Users прочитан в новом EquipmentForm circuit. Подставленный в URL CLID не меняет CLID=0, повтор выбора оставил одну строку. В конце через задокументированную ветку штатного триггера восстановлено исходное отсутствие строки `ClayTheme`. Старые ключи и бизнес-данные сохранены. `Sql-browser.log` содержит результаты.

Studio `?review=true`: браузером для всех пяти вариантов проверены статический ClayGrid, dialog column settings, сортировка по двум колонкам, группировка, обычная/активная иконки и hover, снятие фильтра. Проверены восемь реальных контролов обязательных полей, отсутствие жёлтого у readonly/disabled и непустого невалидного email, кейсы null/0/отсутствующего элемента. `Studio-browser.log`, `grid-*`, `columns-*`, `required-*`.

В QA Inventory измерены F01 и F02. EquipmentForm на существующей тестовой записи сохранила примечание, сырые `01.12.202` и `12:`, validation state, активную кнопку «Применить» и те же DOM-узлы после смены темы. Реальный динамический ClayGrid сохранил две сортировки, группировку, активный фильтр по значению, выделенную строку и горизонтальный scroll. После очистки вернулся обычный индикатор. Счётчик ADO.NET учёл реальные запросы настройки грида и показал 0 дополнительных прикладных round trips в интервале смены темы на обоих экранах. `Equipment-browser.log`, `Dynamic-browser.log`, `equipment-draft-*`, `inventory-dynamic-*`. Ни одна кнопка записи EquipmentForm в этом прогоне не нажималась. `studio-lifecycle.log` подтверждает сохранение состояния дерева, ClaySelect, открытого диалога и возврат в Classic в повторном браузерном прогоне.

`Compare-browser.log`: сравнение с CSS из `dea7746` на том же реальном статическом гриде показало совпадение цветов Classic Light/Dark. Сравнение с более ранними скриншотами до TH1 на тех же маршрутах и viewport 1365×900: грид Classic Light — 239 пикселей, Dark — 242; в основном в области новой иконки и нескольких точках подписи/пагинатора. `select-test` — 34700/37144 пикселя, проверено визуально влияние согласованных жёлтых обязательных полей и их подписей. Снимки до TH1 взяты из `Clayzor-themes/TestResults/ClayTheme`, итоговые снимки `inventory-classic-*`. Для сравнения Dark применены сгенерированные реальные CSS-токены Studio в изолированном браузере без записи предпочтения.

### Статусы приёмки

| ID | Статус | Доказательство / ограничение |
|---|---|---|
| TH2-01 | PASS | Один ключ, реальный SQL-upsert и переход между Inventory/Users/EquipmentForm. Отдельные проверки MedicalTests/IIS и двух SID ниже. |
| TH2-02 | PASS | Реальные статический и динамический гриды с двумя сортировками, группировкой и диалогом настроек. Foreground/background измерены в Chrome. |
| TH2-03 | PASS | Обычная и активная иконки различимы в обоих путях рендера, включая hover и снятие фильтра. Активный фильтр оставался включённым при смене темы в динамическом гриде. |
| TH2-04 | PASS | Реальные контролы Studio во всех пяти темах, computed background/placeholder/caret, Classic Dark визуально проверен. |
| TH2-05 | NOT RUN | Полная матрица включает второй SQL SID и тестовый IIS, которых нет в этом прогоне. Локальные результаты приведены отдельно. |
| THEME-F01 | PASS | Реальная EquipmentForm с временным селектором QA, raw date/time и dirty/validation сохранены, 0 дополнительных прикладных SQL round trips. |
| THEME-F02 | PASS | Настоящий динамический грид сохранил filter/sort/group/selected/scroll, 0 дополнительных прикладных SQL round trips. |
| THEME-F03 | PASS | Повторный Chrome smoke Studio сохранил несохранённый ввод и экземпляр открытого диалога. |
| THEME-F04 | PASS | Повторный Chrome smoke Studio сохранил выбранные узлы и значение ClaySelect. |
| THEME-F05 | NOT RUN | Поздний ответ хранилища темы покрыт профильными unit-тестами, параллельный реальный lookup на форме не проверен. |
| THEME-F06 | NOT RUN | Реальный IIS restart и обрыв сети не выполнялись. Для последующей проверки использовать отдельный стенд, сохранить ввод в EquipmentForm, остановить только тестовый процесс и открыть ту же вкладку после восстановления. Проверить raw, dirty и отсутствие write replay. |
| THEME-F07 | PASS | Возврат в Classic и неизменность цветов индикаторов подтверждены browser-сравнением. |
| THEME-S01 | PASS | Отсутствующая настройка дала Classic Light, до выбора строка в `vwНастройки` отсутствовала. |
| THEME-S02 | PASS | Все пять значений прошли реальный INSERT-upsert. |
| THEME-S03 | PASS | Реальный повтор оставил одну строку, гонка выбора покрыта unit-тестом scoped state. |
| THEME-S04 | PASS | Inventory → Users в другом профиле Chrome → новый EquipmentForm circuit. |
| THEME-S05 | NOT RUN | Второй реальный Windows/SQL login не предоставлен. |
| THEME-S06 | NOT RUN | Подмена CLID проверена в живом SQL-smoke, anonymous/service account отклоняются профильным тестом. Реальное несовпадение Windows/SQL principal через IIS отдельно не воспроизводилось. |
| THEME-S07 | PASS | Профильные тесты отличают отказ загрузки/сохранения и разрешают явный retry. Обрыв рабочего SQL намеренно не создавался. |
| THEME-S08 | PASS | Профильные тесты управляют поздним ответом чтения и очередью сохранений. |
| THEME-S09 | PASS | Старый localStorage проверен browser smoke TH1, активный store TH2 использует только канонический ключ и проверен real SQL. |
| THEME-S10 | PASS | Studio регистрирует scoped state в памяти и не использует store. |

Для следующей актуализации `STYLE_RULES.md` предложено зафиксировать публикацию доверенных CSS-ролей через существующий `ClayThemeTokenProvider`, разделить роли grid badge/active value filter и описать жёлтый как состояние обязательности. Сам `STYLE_RULES.md` и StyleGuard не менялись.

## Merge исправления Legacy и обязательных полей

Изменения из `C:\CLAYZOR-KIMI\Clayzor-theme-fixes` перенесены в `main`. Обычные строки Legacy получают белый фон тела полосатого грида, чередующиеся сохраняют `#EFEFEF`; наведение и выделение применяются поверх них. Общий жёлтый `#FFFF99` используется только для доступного пустого обязательного поля, после ввода восстанавливается цвет текущей темы. Добавлена передача состояния `clay-required-empty` в ClayDropDownList и ClayComboBox. MedicalTests подключён к общему `ClayThemeTokenProvider`.

В каждом затронутом сабмодуле ветка `theme-required-fields` опубликована и влита в `main` через `git merge --no-ff`:

| Сабмодуль | Merge SHA |
|---|---|
| Controls | `b3616aaa4e2eda8852d7b84208d7cff842f23ab1` |
| MedicalTests | `4f87382e7213b28b00769b85699ddabed93cd3d0` |
| Tests | `d0f334776bad2adc7fd7a0fdb0ce0f806fb07b02` |

Проверки после переноса: сборка решения и Studio — 0 ошибок; профильные тесты — 28/28; регресс без известного зависающего `ClayTreeMutationAbortTests` — 1184/1184. Браузерный сценарий на основном коде подтвердил все пять тем, подсветку только пустых обязательных полей, восстановление цвета после ввода, чередование и наведение строк Legacy в гриде 1001, а также состояния полей EquipmentForm `id=8`. Бизнес-данные и настройки темы в БД не записывались.

## Территории и телефонные коды

Реализация подготовлена в отдельном worktree `C:\CLAYZOR-KIMI\Clayzor-territories` на базе суперпроекта `dea7746`. Файлы EQ10, общий `SQLQueries.cs`, Controls, `Program.cs`, меню и layout в этой задаче не редактировались. Коммитов и push нет.

| Маршрут | Сделано |
|---|---|
| `/TerritorialEntity/TerritorialEntity_search` и `.aspx` | Ленивое дерево на `ClayTreeView`, ограничение `rootids` → `root` → `parent`, фильтрация по ID и названию, точный счётчик и лимит 100 совпадений. Правая область использует штатные списки `1030` и `9993`, для города автоматически показывает телефонные коды. |
| `/TerritorialEntity/TerritorialEntity_form` и `.aspx` | Девять полей в порядке legacy, валидация ОКСМ, редактирование существующей территории с проверкой конфликта и сохранением `Parent`, аудит и вложенный список кодов с `CLID=85`. |
| `/TerritorialEntity/PhoneCodeEntity_form` и `.aspx` | Строковый неизменяемый ключ существующей записи, выбор территории типов 2/3/4, допустимый тип связи `0`, доменные операции создания, изменения и удаления. Свежая проверка полномочий, транзакция и контрольное чтение результата. |

**Проверки.** Сборки Entities и Inventory прошли, профильный прогон дал **24/24**. В Chrome на существующих записях тестовой БД проверены три маршрута, ограниченный корень, ленивая загрузка, оба вложенных списка, пустая выдача, запрет callback с wildcard origin, неизменяемый ключ и новая карточка на ширине 390 px. Поиск вернул **5751 совпадение при 100 отображённых узлах**, отдельный `SELECT COUNT(*)` подтвердил итог. Проверки БД проводились только на чтение. Цикл INSERT → Apply → UPDATE и удаление с зависимостями на SQL не выполнялись.

**Остаётся до приёмки.** Создание территории требует штатного возврата ID из `INSTEAD OF INSERT`. Для удаления текущий SQL-контекст не видит `master.dbo.sp_Delete_Территория` и не имеет `EXECUTE`. Для экспорта 1С требуется подтверждённый endpoint и права. Множественный выбор, DnD, расширенный поиск из ClaySelect и хранение состояния дерева зависят от согласования общих контрактов. В `ClayTreeView` воспроизведён пропуск первого URL-фильтра при передаче полного URI. Подготовлен отдельный патч владельцу Controls.

**Передача координатору.** Взять новые feature-файлы и профильные тесты из `Clayzor-territories`, сверить патчи с итоговым EQ10 и выполнить интеграционную сборку. Подробности схемы, полномочий и тестов собраны в отдельном handoff. [Handoff территорий и телефонных кодов](C:/CLAYZOR-KIMI/Clayzor-territories/promts/_handoff/TERRITORIAL_ENTITY_PHONE_CODE.md) · [Патч меню и ссылок из гридов](C:/CLAYZOR-KIMI/Clayzor-territories/TestResults/TerritorialEntity/territorial-inventory-integration.patch) · [Патч первого URL-фильтра ClayTree](C:/CLAYZOR-KIMI/Clayzor-territories/TestResults/TerritorialEntity/territorial-controls-query.patch)

## TH2 — merge в актуальный main

TH2 перенесён после EQ10. Ветка `theme-2` опубликована в каждом затронутом репозитории и влита в `main` через `git merge --no-ff`:

| Репозиторий | Merge SHA |
|---|---|
| Controls | `26b35a266f870bbfaafd5f69e9eefdbfc7de50b1` |
| Entities | `ec7df400c54c9e77252d6cd505a3f565df6ca38a` |
| Inventory | `6a321bb4e29918e6bfd82f9ef0eee2dfd5a83ce0` |
| Users | `c4416fb1fbc7268149d8fd17966d6ce86b865e13` |
| Tests | `aac9630672f5bb0bfdd09811a3f9184ff97aff72` |

Проверки после интеграции: сборка решения и Theme Studio — 0 ошибок; профильные темы — 40/40; общий suite без известного `ClayTreeMutationAbortTests` — **1250/1250 passed**. Browser probe получил ожидание `document.readyState=complete` и проверку CDP-ошибок, устранив гонку начальной навигации. Studio review на основном коде подтвердил все пять тем, badges/active filter, сортировку/группировку и обязательные поля, включая null/0/readonly/disabled и непустой невалидный email. Рабочая БД не изменялась.

## TH-C01 — MedicalTests

Принят отчёт `31_MEDICAL_SHARED_THEME.md`.

- MainLayout использует один `ClayThemeHost` и `ClayThemeSelector`; drawer и business DbManager сохранены.
- `AddInventoryThemePreferences` имеет overload с отдельной фабрикой `ClayAppSettings`; прежняя сигнатура сохранена. MedicalTests читает `ConnectionStrings:InventoryTheme` для общего SQL store.
- Внешний Chrome подтвердил classic:light, пять тем и переключение classic:dark для окна. Без подключения выводится причина невозможности сохранить выбор.
- Общая сборка — 0 ошибок; профиль пяти групп — 85/85; общий suite — 1384/1384.
- Controls `0a298a8`, MedicalTests `784e72e` опубликованы в `origin/main`.
- Для сохранения между приложениями владельцу нужно задать доверенное Integrated Security подключение к Инвентаризации. SQL multiuser/delegation не проверялись.
