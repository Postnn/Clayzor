# Kesco BZ — промты для внедрения единого стиля

Готовые промты для агента. Выполнять **по порядку**. Перед каждым агент обязан прочитать `STYLE_RULES.md`.
Порядок неслучаен: сначала фундамент (токены), потом «забор» (контроль сборки), потом уборка (миграция нарушений).

---

## Промт 0 — контекст (добавить в начало каждого промта или в правила агента)

```
Проект: Blazor Server + MudBlazor. Две сборки:
- Kesco.App.Web.BZ.MedicalTests (приложение)
- Kesco.Lib.Web.BZ.Controls (библиотека контролов: KescoGrid, KescoButton, диалоги)

Единственные штатные места визуального стиля:
- Kesco.App.Web.BZ.MedicalTests/wwwroot/css/app.css
- Kesco.Lib.Web.BZ.Controls/Themes/KescoTheme.cs (тема MudBlazor)

Закон стиля описан в STYLE_RULES.md — прочитай его перед работой и следуй ему буквально.
Не меняй бизнес-логику. Не трогай значения фирменных цветов. Не вводи новых вариантов
оформления одного элемента — используй канонические рецепты из §4 STYLE_RULES.md.
```

---

## Промт 1 — единый источник цвета (Вариант A: источник — тема)

```
ВАЖНО: поля цвета в палитре MudBlazor имеют тип MudColor и парсятся через MudColor.Parse,
который НЕ понимает var(...). Присвоение Primary = "var(--kesco-navy)" роняет приложение
(ArgumentException: Not a valid color на KescoTheme.Create()). Поэтому цвета — литералы в
C#, а app.css их потребляет через эмитируемые MudBlazor переменные --mud-palette-*.

Задача: убрать дублирование цвета между app.css и KescoTheme.cs, сделав единственным
источником тему. Значения цветов не менять.

Шаги:
1. Создай Kesco.Lib.Web.BZ.Controls/Themes/KescoColors.cs — static-класс с public const
   string на каждый фирменный цвет (hex), напр.:
       public const string Navy = "#05164D";
       public const string Gold = "#FFAD00";
       ... (Navy, NavyDark #030F35, NavyLight #1A2D6B, Navy2 #0A1D3D, BlueMid #00235F,
            Gold, GoldDark #E69C00, White #FFFFFF, OffWhite #F7F8FA, GreyLight #EBEDF0,
            GreyMid #9B9B9B, GreyDark #4A4A4A, Text #1A1A2E, Error #C62828, Success #2E7D32)
2. В KescoTheme.cs собери PaletteLight из KescoColors.* (ЛИТЕРАЛЫ, не var()):
       Primary = KescoColors.Navy, Secondary = KescoColors.Gold,
       Error = KescoColors.Error, Info = KescoColors.BlueMid, ... — все поля.
   Приватные const-строки цветов, дублирующие KescoColors, удали.
   Typography.FontFamily = ["var(--kesco-font-family)"] ОСТАВЬ — там var() валиден.
   PaletteDark оставь с её собственными значениями, не трогай.
3. В app.css блок :root замени литералы цвета на алиасы к палитре MudBlazor (её
   переменные --mud-palette-* уже эмитятся MudThemeProvider и используются в файле):
       --kesco-navy:       var(--mud-palette-primary);
       --kesco-navy-dark:  var(--mud-palette-primary-darken);
       --kesco-navy-light: var(--mud-palette-primary-lighten);
       --kesco-navy-2:     var(--mud-palette-drawer-background);
       --kesco-blue-mid:   var(--mud-palette-tertiary);
       --kesco-gold:       var(--mud-palette-secondary);
       --kesco-gold-dark:  var(--mud-palette-secondary-darken);
       --kesco-offwhite:   var(--mud-palette-background);
       --kesco-grey-light: var(--mud-palette-lines-default);
       --kesco-grey-mid:   var(--mud-palette-text-disabled);
       --kesco-grey-dark:  var(--mud-palette-text-secondary);
       --kesco-text:       var(--mud-palette-text-primary);
       --kesco-error:      var(--mud-palette-error);
       --kesco-warning:    var(--mud-palette-warning);
       --kesco-info:       var(--mud-palette-info);
       --kesco-success:    var(--mud-palette-success);
   Не-цветовые константы оставь литералами в :root:
       --kesco-font-family, --kesco-font-size (0.75rem), --kesco-radius (2px),
       --kesco-accent (3px), --kesco-white (#FFFFFF).
4. Существующие --lh-* НЕ переписывай по файлу — сделай алиасами на --kesco-*:
       --lh-navy: var(--kesco-navy); --lh-gold: var(--kesco-gold); ... (все девять).
       --lh-navy-light в файле не используется — можно алиас на --kesco-navy-2.
5. Проверь: приложение стартует (нет Not a valid color), светлая тема выглядит как раньше.
   В тёмной теме морские поверхности станут dark-primary — это ожидаемо для Варианта A.

Не меняй app.js, разметку компонентов и бизнес-логику. Только KescoColors.cs, палитра в
KescoTheme.cs и блок :root в app.css.
```

---

## Промт 2 — включить принудительный контроль (сборка падает при нарушении)

```
Задача: сделать так, чтобы визуальная стилизация вне app.css/KescoTheme.cs приводила
к ОШИБКЕ СБОРКИ. Реализовать в два слоя.

СЛОЙ A — MSBuild-таргет для .razor (основной барьер, даёт compile-time error):
1. Создай файл build/StyleGuard.targets в корне решения со свойством-таргетом
   EnforceStyleRules, BeforeTargets="CoreCompile".
2. Таргет сканирует **/*.razor и **/*.cs проекта (Include через ItemGroup), ИСКЛЮЧАЯ:
   - wwwroot/css/app.css
   - Themes/KescoTheme.cs
   - Services/KescoGridPrintStyles.cs
   - Services/KescoGridPrintHtmlGenerator.cs
   - Services/KescoGridExcelGenerator.cs
   - каталоги obj/ и bin/
3. Запрещённые паттерны (регулярки, без учёта регистра) внутри style="…" или Style="…":
   color\s*: | background(-color)?\s*: | border[a-z-]*\s*: | box-shadow\s*: |
   font-(family|size|weight|style)\s*: | fill\s*: | stroke\s*: | letter-spacing\s*: |
   text-transform\s*: | #[0-9a-fA-F]{3,8}\b | rgba?\(
   Плюс отдельно: наличие подстроки "<style" в .cs вне белого списка.
   ВАЖНО: чисто структурный инлайн (display/flex/gap/width/min-width/overflow/
   text-overflow/position/z-index/top/padding/margin) НЕ считается нарушением.
4. При совпадении — вывести <Error Text="..."/> с именем файла, номером строки и
   найденным фрагментом, чтобы сборка упала с понятным сообщением.
5. Подключи build/StyleGuard.targets в оба .csproj через <Import Project="..."/>.

СЛОЙ B — Roslyn-анализатор для .cs (ловит хардкод в коде, напр. в генераторах):
6. Создай проект-анализатор Kesco.Lib.Web.BZ.Controls.Analyzers (netstandard2.0,
   Microsoft.CodeAnalysis.CSharp) с диагностикой KESCO001 (severity Error):
   "Визуальная стилизация вне app.css/KescoTheme.cs запрещена".
7. Анализатор проверяет строковые литералы и интерполяции: если строка содержит
   визуальный CSS (те же паттерны, что в п.3) ИЛИ подстроку "<style" — репортит KESCO001
   на позиции литерала. Файлы белого списка (§5 STYLE_RULES.md) исключить по пути.
8. Подключи анализатор к обоим проектам как <ProjectReference ... OutputItemType="Analyzer"
   ReferenceOutputAssembly="false"/> (или как NuGet-analyzer, если так удобнее в CI).
9. Добавь юнит-тесты анализатора: (а) хардкод "background:#d32f2f" → KESCO001;
   (б) "display:flex;gap:4px" → без диагностики; (в) файл из белого списка → без диагностики.

Проверка приёмки: временно добавь в любой .razor style="color:red" — сборка ДОЛЖНА
упасть с указанием файла/строки. Убери — сборка проходит. Затем то же для .cs-литерала.
```

---

## Промт 3 — устранить существующие нарушения

```
Задача: привести текущий код к STYLE_RULES.md. После этого сборка (с включённым
контролем из Промта 2) должна проходить без ошибок стиля. Логику и разметку по смыслу
не менять — только способ стилизации.

Нарушители (найдены сканом):

1. Kesco.Lib.Web.BZ.Controls/Components/KescoErrorBar.razor — ХУДШИЙ случай.
   Полностью самописная разметка с хардкод-цветами (#d32f2f, #b71c1c, #8b0000, #ffcdd2…).
   - Перепиши на MudBlazor (MudAlert/MudPaper) ИЛИ на семантические классы .kesco-error-*.
   - Все красные цвета замени на var(--mud-palette-error) / var(--mud-palette-error-darken)
     и производные — определи эти классы (.kesco-error-bar, .kesco-error-details,
     .kesco-error-sql, .kesco-error-param-table) в app.css.
   - В .razor должны остаться только Class="…", без единого визуального style=.

2. Kesco.App.Web.BZ.MedicalTests/Components/Pages/Home.razor
   - Убрать Style="color:var(--lh-grey-dark)", Style="border-left:3px solid var(--lh-gold)",
     Style="color:var(--lh-navy)", Style="border-left:3px solid var(--lh-grey-light)".
   - Ввести классы .kesco-card (нейтральная карточка) и .kesco-card--accent
     (золотая левая граница) в app.css; в разметке — Class="pa-5 kesco-card--accent".
   - Цвет вторичного текста — классом (.kesco-text-secondary { color: var(--mud-palette-text-secondary) }).
   - Иконки-акценты — через Color="Color.Primary"/"Color.Secondary", не Style.

3. Kesco.App.Web.BZ.MedicalTests/Components/Layout/MainLayout.razor
   - Style="font-weight:700; letter-spacing:0.04em;" на заголовке → класс .kesco-appbar-title.
   - Style="opacity:0.85;" → класс (opacity — не визуальный цвет, но вынеси для чистоты).
   - Style="border-color:rgba(255,255,255,0.2);" на MudDivider → класс .kesco-appbar-divider
     с цветом из токена.
   - Style="color:#8A93A8; font-size:0.65rem;" в футере drawer → класс .kesco-drawer-version
     (цвет из --mud-palette-text-secondary или --kesco-grey-mid).

4. Kesco.Lib.Web.BZ.Controls/Services/KescoGridPrintHtmlGenerator.cs
   - Если генерирует CSS сам — перенеси весь CSS в KescoGridPrintStyles.cs (белый список),
     оставив в генераторе только вызов/вставку готовой строки стилей.
   - Значения цветов в KescoGridPrintStyles.cs синхронизировать с --kesco-* (общие C#-константы).

5. Прочий инлайн в гриде и диалогах (KescoColumnSettingsDialog.razor, KescoGrid.razor,
   KescoCheckbox.razor, KescoColumnValueFilterDialog.razor, фильтры):
   - Чисто структурный инлайн (display/flex/gap/min-width/overflow/text-overflow) можно
     оставить, но по возможности заменить на utility-классы Mud (d-flex, gap-1, flex-1).
   - Любой ВИЗУАЛЬНЫЙ инлайн (цвет/фон/граница/шрифт), если найдётся, — вынести в app.css.

По каждому файлу: сначала покажи diff, дождись, что сборка со StyleGuard зелёная,
затем переходи к следующему. Ничего не удаляй из функциональности.
```

---

## Промт 4 — постоянная приписка к любой UI-задаче

Добавляй это в конец любого запроса на UI, чтобы агент не «расползался» по стилю:

```
Стиль строго по STYLE_RULES.md: никакого визуального style=/Style= в .razor,
никакого hex/шрифта в .cs. Цвета — только var(--mud-palette-*) (адаптивные) или
var(--kesco-*) (бренд), и только в app.css/KescoTheme.cs. Нужен новый вид элемента —
заведи класс в app.css и используй Class="…". Раскладку делай utility-классами Mud
(d-flex, gap-*, pa-*). Если для нужного вида есть несколько вариантов оформления —
покажи варианты и спроси, а не выбирай молча. Перед завершением прогони чек-лист §7.
```

---

## Промт 5 — размер лейбла/легенды input-полей = --kesco-font-size

```
Контекст: в app.css правило legend { font-size: var(--kesco-font-size) } УЖЕ есть, но
визуально не работает, потому что MudBlazor у всплывающего (shrink) лейбла применяет
transform: ... scale(0.75). Пока scale жив, размер шрифта на вид не меняется. Менять
только font-size бесполезно — это и есть причина, по которой правка «не применяется».

Задача: сделать так, чтобы лейбл и легенда outlined-полей отображались размером
var(--kesco-font-size) в обоих состояниях (в покое и всплывшими). Правки ТОЛЬКО в
Kesco.App.Web.BZ.MedicalTests/wwwroot/css/app.css. Инлайн-стили не добавлять.

Шаги:
   ВАЖНО про DOM (проверено по факту): класс mud-shrink стоит на div .mud-input, а НЕ
   на самом лейбле. Лейбл <label class="mud-input-label mud-input-label-outlined …">
   лежит СОСЕДНИМ элементом ПОСЛЕ .mud-input. Поэтому селектор лейбла — через ~, иначе
   правило не матчится и «не срабатывает».

1. В app.css замени существующий блок правил лейбла/легенды (около строк 63–67) на:

   /* Базовый размер лейбла */
   .mud-input-control .mud-input-label-outlined {
       font-size: var(--kesco-font-size) !important;
   }
   /* Лейбл в покое (пустое поле без фокуса) — тот же размер, без уменьшения */
   .mud-input:not(.mud-shrink) ~ .mud-input-label-outlined {
       font-size: var(--kesco-font-size) !important;
   }
   /* Лейбл ВСПЛЫЛ по любой причине:
      (a) в поле есть значение → mud-shrink на .mud-input (лейбл — сосед ПОСЛЕ, через ~)
      (b) поле в фокусе → mud-shrink НЕТ, лейбл поднят фокусом → ловим через :focus-within
      (в focused-состоянии mud-shrink отсутствует — проверено по DOM). */
   .mud-input.mud-shrink ~ .mud-input-label-outlined,
   .mud-input-control:focus-within .mud-input-label-outlined {
       transform: translate(14px, -9px) scale(1) !important;   /* -9px подстроить ±2px */
       font-size: var(--kesco-font-size) !important;
       line-height: 1 !important;
   }
   /* Вырез рамки (notch) под полный размер — в обоих случаях всплытия */
   .mud-input.mud-shrink .mud-input-outlined-border legend,
   .mud-input-control:focus-within .mud-input-outlined-border legend {
       font-size: var(--kesco-font-size) !important;
   }

2. Запусти приложение, открой форму MedicalTestEditDialog и проверь ЛЮБОЕ outlined-поле
   (MudTextField, MudNumericField, KescoComboBox). Всплывший лейбл должен быть размером
   как текст поля и сидеть по центру верхней линии рамки, без «дырки» и без линии,
   проходящей сквозь текст.
3. Если лейбл стоит выше/ниже линии — подстрой ТОЛЬКО значение translateY (-9px) в шаге 1
   в диапазоне примерно от -7px до -11px. Ничего больше не меняй.
4. ОБЯЗАТЕЛЬНО: на .mud-input в форме висит класс mud-typography-subtitle1 — у обычного
   MudTextField его быть не должно, он раздувает и текст значения, и высоту рамки. Именно
   из-за него в ПУСТОМ поле подпись кажется «очень мелкой»: сама подпись уже правильного
   размера --kesco-font-size, но сидит одна в рамке, рассчитанной на крупный текст.
   Признак проблемы: текст значения в поле крупнее подписи (а должны быть равны).
   - Найди источник mud-typography-subtitle1 (Typo=/Class= на поле в .razor или в обёртке
     KescoEditForm/KescoComboBox — вероятно, остаток прошлых правок) и убери его.
   - Убедись, что в загруженном app.css есть и работает:
     .mud-input-control input, .mud-input-control .mud-input-slot { font-size:
     var(--kesco-font-size) !important }
   После этого рамка и значение сядут на --kesco-font-size, и подпись в пустом поле
   перестанет теряться — размер станет одинаковым во всех состояниях.
5. Заодно устрани нарушение конституции: в Kesco.Lib.Web.BZ.Controls/Components/
   KescoComboBox.razor есть инлайн <style> с .kesco-combo-popover. Перенеси эти правила
   в app.css как есть (селекторы не меняй) и удали блок <style> из компонента.
6. Верификация в DevTools:
   - Выбери <label class="… mud-input-label-outlined">, в Computed проверь font-size =
     значение --kesco-font-size (напр. 12.8px при 0.8rem) и что в transform НЕТ scale < 1.
   - Сравни Computed font-size подписи в ПУСТОМ поле и во ВСПЛЫВШЕМ — должны быть равны.
     Если равны, а «мелко» всё равно — причина в п.4 (раздутая рамка), не в подписи.
   - Проверь ЧЕТЫРЕ состояния: пустое без фокуса, пустое в фокусе, заполненное в фокусе
     (при вводе — тут раньше лейбл мельчал), заполненное без фокуса. Размер лейбла во
     всех одинаковый.
   Если app.css не подхватился — hard-refresh и чистая пересборка (кэш статики Blazor).

Не трогай разметку формы, логику и другие правила app.css.
```

---

## Промт 6 — уплотнение формы MedicalTestEditDialog (по образцу полей фильтра)

```
Задача: уплотнить форму src/Kesco.App.Web.BZ.MedicalTests/Components/Pages/
MedicalTestEditDialog.razor — уменьшить ВЫСОТУ самих полей и промежутки между ними, как
сделано в полях настраиваемого фильтра. Стиль строго по STYLE_RULES.md: плотность задаём
пропсами MudBlazor, БЕЗ инлайн-визуала.

ЭТАЛОН — Kesco.Lib.Web.BZ.Controls/Components/Grid/Filter/KescoFilterValueEditor.razor:
там каждое поле имеет Variant="Variant.Outlined" + Margin="Margin.Dense", а у MudSelect
ещё Dense="true". Именно Margin.Dense уменьшает высоту поля (переключает типографику с
subtitle1 на компактную и убирает лишние вертикальные паддинги). CSS min-height НЕ нужен.

Ключевое (даёт уменьшение высоты полей):
1. Каждому полю в форме добавь Margin="Margin.Dense" (Variant="Variant.Outlined" уже есть):
   - MudTextField "Название" и "Норма (строка)"
   - MudNumericField "Порядок" и все 4 нормы ("От"/"По" в обоих MudGrid)
2. Комбобокс приведи к тому же виду: в Kesco.Lib.Web.BZ.Controls/Components/
   KescoComboBox.razor у внутреннего MudSelect добавь Margin="Margin.Dense" Dense="true"
   (как у селектов фильтра). Так «Тип исследования» станет такой же высоты, что и поля
   фильтра, и это будет единообразно во всём приложении.

Промежутки и заголовки (уплотняют по вертикали):
3. У всех полей замени Class="mb-3" на Class="mb-2".
4. Оба <MudGrid> с нормами: добавь Spacing="2".
5. Секционные заголовки: <MudText Typo="Typo.subtitle1" Class="mt-2 mb-2"> →
   Typo="Typo.subtitle2" Class="mt-1 mb-1"> (обе секции норм).
6. Два MudSwitch («Группа», «Заключение») — в одну строку:
   оберни в <div class="d-flex flex-wrap gap-4">…</div>, у самих MudSwitch убери mb-*.

Тонкая доводка (только если после шагов 1–6 всё ещё просторно) — в app.css, scope под
классом-обёрткой формы (например обернуть поля в <div class="kesco-form-dense">):
7. .kesco-form-dense .mud-input-control-helper-container { min-height: 0; }
   .kesco-form-dense .mud-input-control-helper-text { margin-top: 2px; line-height: 1.2; }
   Высоту поля правилами min-height НЕ трогай — её уже задаёт Margin.Dense (шаги 1–2).

Проверка: поля стали такой же высоты, как в диалоге фильтра; лейблы (после Промта 5)
читаемы во всех состояниях; на других экранах поля не изменились, кроме единообразно
уплотнившихся комбобоксов (шаг 2 — ожидаемо). Инлайн-визуала в .razor нет. Логику формы
(bind, сохранение, удаление, валидацию) не менять.
```
