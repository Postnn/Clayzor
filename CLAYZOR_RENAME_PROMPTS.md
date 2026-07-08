# Переименование Kesco → Clayzor / Clay — пошаговый runbook для агента

Задача: полностью убрать слово **kesco** (в любом регистре) из решения — код, имена проектов
и решения, namespaces, стили, JS, документация, скрипты, md-файлы агента. Ничего с `kesco`
не должно остаться, КРОМЕ внешней инфраструктуры (см. «Не трогать»).

Фреймворк — кастомизация MudBlazor, называется **Clayzor**, короткий префикс — **Clay**
(по аналогии: MudBlazor → `Mud`). Дополнительно из всех имён убираем аббревиатуру **BZ**
(она встречается только в dotted-именах и в прозе «Kesco BZ» — в именах типов её нет).

---

## Конвенция именования (закон переименования)

| Категория | Сейчас | Станет | Токен |
|---|---|---|---|
| Решение | `KescoBZ.sln`, папка `KescoBZ/` | `Clayzor.sln`, `Clayzor/` | **Clayzor**, BZ убрать |
| Проекты и папки | `Kesco.Lib.Web.BZ.Controls`, `Kesco.App.Web.BZ.MedicalTests`, `Kesco.Lib.Web.BZ.Controls.Tests`, `Kesco.Lib.Web.BZ.Controls.Analyzers`, `Kesco.Lib.DALC`, `Kesco.Lib.Entities`, `Kesco.Lib.Web.Settings` | `Clayzor.Lib.Web.Controls`, `Clayzor.App.Web.MedicalTests`, `Clayzor.Lib.Web.Controls.Tests`, `Clayzor.Lib.Web.Controls.Analyzers`, `Clayzor.Lib.DALC`, `Clayzor.Lib.Entities`, `Clayzor.Lib.Web.Settings` | **Clayzor** + убрать `.BZ` |
| namespace / using / @using | `Kesco.Lib.Web.BZ.Controls…` | `Clayzor.Lib.Web.Controls…` (сегмент `.BZ` удалён) | **Clayzor** + убрать `.BZ` |
| RootNamespace / AssemblyName (.csproj) | `Kesco.*.BZ.*` | `Clayzor.*` (без `.BZ`) | **Clayzor** + убрать `.BZ` |
| Путь статики RCL | `_content/Kesco.Lib.Web.BZ.Controls/…` | `_content/Clayzor.Lib.Web.Controls/…` | **Clayzor** + убрать `.BZ` |
| Типы / компоненты | `KescoButton`, `KescoGrid`, `KescoTheme`, `KescoColors`, `KescoErrorBar`, `KescoComboBox`, `KescoEditForm`, `KescoFilter*`, `KescoGridPrint*`, `KescoAppSettings`, … | `ClayButton`, `ClayGrid`, `ClayTheme`, `ClayColors`, … `ClayAppSettings` | **Clay** |
| Файлы компонентов | `KescoButton.razor`, `KescoGrid.*.cs`, `KescoFilterValueEditor.razor.cs`, … | `ClayButton.razor`, `ClayGrid.*.cs`, … | **Clay** |
| Const в ColorClass | `KescoNavy`, `KescoGold`, `KescoFontFamily` | `ClayNavy`, `ClayGold`, `ClayFontFamily` | **Clay** |
| CSS-переменные | `--kesco-navy`, `--kesco-font-size`, … | `--clay-navy`, `--clay-font-size`, … | **clay** |
| CSS-классы | `.kesco-card`, `.kesco-error-bar`, `.kesco-filter-*`, `.kesco-checkbox-*`, … | `.clay-card`, `.clay-error-bar`, … | **clay** |
| JS-файлы | `kescoColumnSettings.js`, `kescoGridColumnDrag.js`, `kescoGridPrint.js`, `kescoGridExcel.js` | `clayColumnSettings.js`, `clayGridColumnDrag.js`, `clayGridPrint.js`, `clayGridExcel.js` | **clay** |
| JS-объекты + интероп-строки (МЕНЯТЬ ПАРОЙ) | `window.kescoGridPrint` ↔ `InvokeAsync("kescoGridPrint.printHtml")` | `window.clayGridPrint` ↔ `"clayGridPrint.printHtml"` | **clay** |
| Config-секция | `"KescoApp"` в appsettings + `GetSection("KescoApp")` | `"ClayApp"` (в паре с `ClayAppSettings`) | **Clay** |
| Doc-файлы | `docs/kesco-grid.md`, `docs/kesco-combo-box.md`, `docs/kesco-edit-form.md`, `docs/kesco-error-bar.md`, `docs/kesco-column-filter-dialog.md` | `docs/clay-*.md` | **clay** |
| Тексты, называющие фреймворк | «Kesco BZ», «Kesco» (как продукт) | «Clayzor» | **Clayzor** |

### НЕ трогать (внешняя инфраструктура, это не бренд)
- `LDAP://dc.kesco.local` и любые реальные домены/хосты/URL, строки подключения к БД,
  секреты, пути AD — в `appsettings*.json`, `launchSettings.json`, `.env`, CI-секретах.
- Переименование таких значений сломает вход/подключения. Если их тоже надо ребрендить —
  отдельная осознанная задача, не в этом прогоне.

### Порядок важен
Сначала структурный слой (Clayzor + удаление `.BZ`: dotted-пути, имена проектов и решения),
потом типы (Clay), потом css/js/config (clay), потом доки/скрипты, потом финальная зачистка.
Это снимает конфликт `KescoBZ` (→ `Clayzor`, а НЕ `ClayBZ`/`ClayzorBZ`) и `Kesco.*.BZ.*`
(→ `Clayzor.*` без BZ, а не `Clay.*`).

---

## Промт R0 — подготовка

```
Мы переименовываем всё «kesco» → Clayzor/Clay по конвенции из CLAYZOR_RENAME_PROMPTS.md
(таблица выше). Перед началом:
1. Убедись, что решение собирается и тесты проходят на текущем коде (базовая точка).
2. Сделай коммит/ветку rename/clayzor — все шаги делаем в ней, чтобы легко откатить.
3. Прочитай раздел «Не трогать»: инфраструктурные значения (LDAP dc.kesco.local, строки
   подключения, реальные хосты/URL/секреты) НЕ переименовываем.
4. Для переименования ТИПОВ и NAMESPACE используй семантический rename IDE/Roslyn
   (dotnet/Rider/VS «Rename symbol»), а не слепой текстовый replace — он сам поправит
   все ссылки. Текстовый regex — только для css/js/строк/доков/имён файлов и папок.
Выведи список того, что собираешься менять, ДО изменений.
```

## Промт R1 — структурный слой: решение, проекты, namespaces (→ Clayzor, без BZ)

```
Переименуй структурный слой Kesco → Clayzor и УБЕРИ сегмент .BZ. Прочие хвосты
(Lib/App/Web/DALC/Entities/Settings/Controls/MedicalTests/Tests/Analyzers) сохрани.

1. Решение: KescoBZ.sln → Clayzor.sln; корневая папка KescoBZ/ → Clayzor/.
2. Проекты (папка + .csproj + запись в .sln) — целевые имена БЕЗ BZ:
     Kesco.Lib.Web.BZ.Controls          → Clayzor.Lib.Web.Controls
     Kesco.App.Web.BZ.MedicalTests       → Clayzor.App.Web.MedicalTests
     Kesco.Lib.Web.BZ.Controls.Tests     → Clayzor.Lib.Web.Controls.Tests
     Kesco.Lib.Web.BZ.Controls.Analyzers → Clayzor.Lib.Web.Controls.Analyzers  (если проект есть)
     Kesco.Lib.DALC                      → Clayzor.Lib.DALC
     Kesco.Lib.Entities                  → Clayzor.Lib.Entities
     Kesco.Lib.Web.Settings              → Clayzor.Lib.Web.Settings
   Обнови в .sln пути и имена проектов (GUID не меняй), ProjectReference во всех .csproj.
3. В каждом .csproj обнови <RootNamespace> и <AssemblyName> на целевые Clayzor.* (без .BZ).
   Если не заданы явно — добавь явные, чтобы имя сборки стало Clayzor.* (важно для п.5).
4. Во ВСЕХ .cs/.razor: namespace, using, @using, @namespace — замени `Kesco.` → `Clayzor.`
   И удали сегмент `.BZ` из путей: `Clayzor.Lib.Web.BZ.Controls…` → `Clayzor.Lib.Web.Controls…`.
   Т.е. `Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter` → `Clayzor.Lib.Web.Controls.Components.Grid.Filter`.
   Делай через rename namespace в IDE, где возможно.
5. Путь статики RCL зависит от AssemblyName. В src/…/Components/App.razor замени префикс
   _content/Kesco.Lib.Web.BZ.Controls/js/… → _content/Clayzor.Lib.Web.Controls/js/…
   (имена js-файлов пока НЕ трогай — они в R3). Проверь другие ссылки на _content/…BZ… .
6. Собери решение (dotnet build Clayzor.sln). Ошибки компиляции = недоправленные ссылки —
   исправь до зелёной сборки. Тесты запускать в конце (R5).
После шага 5 в проекте НЕ должно остаться ни `Kesco.`, ни сегмента `.BZ.` в namespace/путях.
```

## Промт R2 — типы и компоненты (→ Clay)

```
Переименуй все ТИПЫ с префиксом Kesco → Clay через семантический rename IDE/Roslyn
(это переименует и объявления, и все использования, и теги компонентов <KescoX> → <ClayX>).

1. Все классы/записи/интерфейсы/enum/компоненты вида Kesco<Заглавная> → Clay<...>:
   KescoButton→ClayButton, KescoGrid→ClayGrid, KescoTheme→ClayTheme, KescoColors→ClayColors,
   KescoErrorBar→ClayErrorBar, KescoErrorService→ClayErrorService, KescoComboBox→ClayComboBox,
   KescoEditForm→ClayEditForm, KescoCheckbox→ClayCheckbox, KescoMenu→ClayMenu,
   KescoColumn*→ClayColumn*, KescoFilter*→ClayFilter*, KescoGrid*→ClayGrid*,
   KescoGridPageBase→ClayGridPageBase, KescoGridPrintStyles/HtmlGenerator/ExcelGenerator→Clay…,
   KescoCompositeSqlBuilder→ClayCompositeSqlBuilder, KescoDataQuery→ClayDataQuery,
   KescoGroupingEngine→ClayGroupingEngine, KescoDragState→ClayDragState,
   KescoAppSettings→ClayAppSettings, KescoSettings→ClaySettings, KescoGridRow→ClayGridRow,
   KescoFilterNode/GroupNode/Option/Source/Strings/UrlHelper/JsonConverter→Clay…, и т.д.
   (полный перечень типов есть в исходниках — переименуй КАЖДЫЙ Kesco<Заглавная>).
2. Члены с префиксом Kesco внутри типов (например const в цветовой класс):
   KescoNavy→ClayNavy, KescoGold→ClayGold, KescoFontFamily→ClayFontFamily,
   KescoFontSize→ClayFontSize и любые другие Kesco<Заглавная> члены.
3. Переименуй файлы под новые имена типов: Kesco*.razor / Kesco*.cs / Kesco*.razor.cs /
   partial-файлы KescoGrid.*.cs → Clay*.* (сохрани суффиксы .razor/.razor.cs/.DragDrop.cs).
   Тестовые файлы Kesco*Tests.cs → Clay*Tests.cs.
4. Проверь code-behind пары (.razor + .razor.cs) — имена классов и @code совпадают.
5. Собери решение — зелёная сборка. НЕ трогай пока css/js/строки интеропа (R3).
После R2 не должно остаться идентификаторов вида Kesco<Заглавная>.
```

## Промт R3 — стили, JS, config-строки (→ clay)

```
Переименуй нижний регистр kesco → clay в CSS/JS/строках. Правь текстово, но аккуратно.

1. CSS (wwwroot/css/app.css и любые .razor с Class="…"): замени `--kesco-` → `--clay-`
   и `.kesco-`/`kesco-` (в классах) → `clay-`. Затрагиваются все переменные (--kesco-navy,
   --kesco-font-size, …) и классы (.kesco-card, .kesco-error-bar, .kesco-filter-*,
   .kesco-checkbox-*, .kesco-column-*, .kesco-drawer-*, .kesco-grid-chip-*, …).
   В .razor обнови все Class="… kesco-… …" → clay-.
2. JS-файлы: переименуй файлы kescoColumnSettings.js→clayColumnSettings.js,
   kescoGridColumnDrag.js→clayGridColumnDrag.js, kescoGridPrint.js→clayGridPrint.js,
   kescoGridExcel.js→clayGridExcel.js. Внутри — глобальные объекты
   window.kescoColumnSettings→window.clayColumnSettings и т.д., и все внутренние kesco-имена.
3. Ссылки на js в App.razor: <script src="_content/Clayzor.Lib.Web.Controls/js/clay*.js">
   (префикс _content уже Clayzor без BZ из R1, имена файлов теперь clay*).
4. JS-интероп — МЕНЯТЬ ПАРОЙ (иначе рантайм-ошибка): строки в C#
   InvokeAsync/InvokeVoidAsync("kescoGridPrint.printHtml"|"kescoColumnSettings.init"|
   "kescoGridColumnDrag.init"|".dispose"|"kescoGridExcel.downloadFile"|
   "kescoGridPrint.showSpinner"|".hideSpinner") → замени kesco→clay ТОЧНО так же, как
   переименовал объекты в п.2. Проверь, что каждое имя из C# существует в js.
5. Config: в appsettings*.json секцию "KescoApp" → "ClayApp"; в коде GetSection("KescoApp")
   и/или Configure<ClayAppSettings>("KescoApp") → "ClayApp". НЕ трогай значение
   "LdapPath":"LDAP://dc.kesco.local" и прочие инфра-значения.
6. Собери и запусти приложение, проверь в браузере: стили применяются (классы clay-*),
   печать/экспорт/drag работают (интероп совпал), консоль без ошибок про undefined kesco*.
```

## Промт R4 — документация, md агента, скрипты (→ Clayzor/Clay/clay по контексту)

```
Обнови тексты. Внутри каждого файла применяй ту же конвенцию: имя фреймворка → Clayzor,
идентификаторы кода → как в коде (типы Clay*, namespaces Clayzor.*, css/js clay*).

1. docs/: переименуй файлы kesco-grid.md→clay-grid.md, kesco-combo-box.md→clay-combo-box.md,
   kesco-edit-form.md→clay-edit-form.md, kesco-error-bar.md→clay-error-bar.md,
   kesco-column-filter-dialog.md→clay-column-filter-dialog.md. Внутри и в остальных доках
   (adding-new-entity.md, entity-crud.md) обнови все упоминания: типы <KescoX>→<ClayX>,
   пути Kesco.*→Clayzor.*, css --kesco-/.kesco-→clay, «Kesco»/«Kesco BZ» как продукт→«Clayzor».
   Поправь внутренние ссылки на переименованные файлы.
2. AGENTS.md и STYLE_RULES.md: замени все Kesco/kesco по конвенции (пути проектов →Clayzor.*,
   KescoTheme→ClayTheme, KescoColors→ClayColors, --kesco-*→--clay-*, .kesco-*→.clay-*,
   «Kesco BZ»→«Clayzor»). Заголовки/описания фреймворка → Clayzor.
3. promts/ (в т.ч. promts/_done/STYLE_PROMPTS.md и src/.../Grid/promts/**): обнови упоминания
   тем же правилом. Это архив выполненных промтов — можно ограничиться заменой токенов,
   не переписывая смысл.
4. scripts/setup.ps1: KescoBZ.sln→Clayzor.sln, путь src/Kesco.App.Web.BZ.MedicalTests→
   src/Clayzor.App.Web.MedicalTests, тексты «Kesco BZ Setup»/«Kesco BZ»/«Kesco»→«Clayzor».
5. Прочие .json/.props/.targets/.yml/.editorconfig — при наличии kesco замени по конвенции
   (кроме инфра-значений).
```

## Промт R5 — финальная зачистка и проверка

```
Финал: убедиться, что kesco не осталось нигде, и всё работает.

1. Полный поиск без учёта регистра по всему репозиторию (исключая bin/obj/.git):
     grep -rin "kesco" . --exclude-dir=bin --exclude-dir=obj --exclude-dir=.git
   Ожидаемо пусто. Единственное допустимое исключение — инфра-значения из «Не трогать»
   (напр. LDAP dc.kesco.local), если решено их сохранить. Всё остальное — доправить.
   Затем проверь удаление BZ из имён:
     grep -rn "\.BZ\." . --exclude-dir=bin --exclude-dir=obj --exclude-dir=.git
     grep -rniE "kescoBZ|clayzorBZ|Kesco BZ" . --exclude-dir=bin --exclude-dir=obj --exclude-dir=.git
   Тоже должно быть пусто (если где-то `BZ` — это часть неродственного слова, оцени вручную).
2. Проверь имена: нет файлов/папок Kesco*/kesco* и нет `.BZ` в именах проектов/namespace;
   решение Clayzor.sln; проекты Clayzor.Lib.Web.Controls / Clayzor.App.Web.MedicalTests / …;
   js-файлы clay*.js.
3. dotnet build Clayzor.sln — зелёно. dotnet test — тесты проходят.
4. Запусти приложение: открой грид (сортировка/группировка/фильтр/печать/экспорт/drag
   колонок — всё через интероп clay*), открой форму редактирования (стили clay-*, легенды,
   плотность). Консоль браузера без ошибок undefined.
5. Если используется Roslyn-анализатор/пути из STYLE_PROMPTS — проверь, что его белый список
   (KescoGridPrintStyles.cs и т.п.) обновлён на Clay*/Clayzor.* пути.
Готово, когда grep по "kesco" пуст (кроме согласованной инфраструктуры), сборка и тесты
зелёные, приложение работает.
```

---

## Примечание про `--lh-*`
В app.css есть ещё алиасы `--lh-*` (наследие «Lufthansa»-палитры) — это НЕ `kesco`, под текущую
задачу они не попадают и трогать их не нужно. Если хочешь и их ребрендить под Clayzor
(например `--clay-legacy-*` или просто убрать алиасы) — скажи, добавлю отдельный промт.
