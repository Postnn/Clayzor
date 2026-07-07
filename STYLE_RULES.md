# Kesco BZ — правила стилизации UI (единый закон)

> Положить в корень репозитория и подключить как правило агента:
> `CLAUDE.md` / `.cursor/rules/style.md` / `.github/copilot-instructions.md`.
> Этот файл — **источник истины по стилю**. Агент обязан следовать ему в каждой UI-задаче.

---

## 0. Главный закон

**Визуальная стилизация UI (цвет, шрифт, фон, границы, тень, скругление) живёт ТОЛЬКО в `Kesco.App.Web.BZ.MedicalTests/wwwroot/css/app.css` и в теме `Kesco.Lib.Web.BZ.Controls/Themes/KescoTheme.cs`.**

Нигде больше — ни в `.razor`, ни в `.cs`, ни в связанных сборках — собственная визуальная стилизация не допускается. Нарушение = ошибка сборки (см. §6).

Приложение построено на **MudBlazor**. Стиль — корпоративный «люфтганзовский»: тёмно-синий, золотой акцент, чистый белый, острые углы (2px), Verdana 12px.

---

## 1. Единый источник токенов (Вариант A)

> ВАЖНО про MudBlazor: поля цвета в палитре имеют тип `MudColor` и парсятся через
> `MudColor.Parse`, который принимает только настоящие цвета (`#hex`, `rgb()`), **но НЕ
> `var(...)`**. Присвоить палитре `Primary = "var(--kesco-navy)"` = краш `Not a valid color`
> на `KescoTheme.Create()`. Поэтому направление зависимости обратное:

**Источник значений цвета — тема (C#). `app.css` их потребляет.**

1. Фирменные цвета определяются **один раз** как C#-константы в
   `Kesco.Lib.Web.BZ.Controls/Themes/KescoColors.cs` (значения в hex).
2. `KescoTheme.cs` собирает палитру из `KescoColors.*` (литералы, не `var()`).
3. `MudThemeProvider` в рантайме эмитит из палитры CSS-переменные `--mud-palette-*` в DOM.
4. В `app.css` блок `:root` **не хранит литералы цвета**, а делает `--kesco-*` алиасами
   на `--mud-palette-*`. Так цвет живёт в одном месте (тема) и **сам адаптируется к dark mode**.

Литералами в `app.css :root` остаются только **не-цветовые** бренд-константы (шрифт,
размер, радиус, толщина акцента) и фиксированный `--kesco-white`.

```css
:root {
    /* Не-цветовые константы — единственные литералы */
    --kesco-font-family: Verdana, Arial, sans-serif;
    --kesco-font-size:   0.75rem;   /* 12pt */
    --kesco-radius:      2px;
    --kesco-accent:      3px;
    --kesco-white:       #FFFFFF;

    /* Цвета — алиасы на палитру MudBlazor (источник — KescoTheme/KescoColors) */
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
}
```

Старые `--lh-*` — тонкие алиасы на `--kesco-*` (`--lh-navy: var(--kesco-navy);`), чтобы не
переписывать 891 строку. Новый код `--lh-*` не вводит — только `--kesco-*`.

Следствие dark mode (принято при выборе Варианта A): поверхности на «морском» фоне
(шапка таблицы, чипы, заголовок диалога, appbar) следуют за `primary` и в тёмной теме
становятся её dark-primary. Золото (`secondary`) в обеих темах остаётся золотым.

### Какую переменную брать

- **Всё, что должно адаптироваться к тёмной теме** (текст, фон, поверхность, primary, error, разделители) → бери переменные MudBlazor: `var(--mud-palette-primary)`, `--mud-palette-text-primary`, `--mud-palette-surface`, `--mud-palette-error`, `--mud-palette-lines-default` и т.д. Их MudBlazor сам пересчитывает в dark mode.
- **Постоянные бренд-акценты**, которые не меняются между темами (золотая линия) → `var(--kesco-gold)`.
- **Хардкод hex (`#05164D`) в `.razor`/`.cs`/любых override — запрещён.** Только `var(--kesco-*)` или `var(--mud-palette-*)`, и только в `app.css`/теме.

---

## 2. Что ЗАПРЕЩЕНО (ловится сборкой)

В `.razor` и `.cs` **запрещены визуальные инлайн-стили**. Конкретно — атрибут `style="…"` или параметр `Style="…"`, содержащий любое из:

`color` · `background` / `background-color` · `border` (любые) · `box-shadow` · `font-*` (family/size/weight/style) · `fill` / `stroke` · `letter-spacing` · `text-transform` · сырой hex-цвет `#RGB`/`#RRGGBB` · `rgb(...)`/`rgba(...)` с цветом.

Также запрещено в `.cs`:
- строковые литералы CSS с цветом/шрифтом (`"background:#d32f2f"` и т.п.);
- генерация `<style>…</style>` или инлайн-`style` с визуальными свойствами (кроме белого списка §5).

**Пример нарушения (как сейчас в `KescoErrorBar.razor`):**
```razor
<div style="background:#d32f2f;color:#fff;padding:12px 16px;border-radius:4px"> ❌
```

---

## 3. Что РАЗРЕШЕНО

- **Layout-утилиты MudBlazor как CSS-классы**: `Class="d-flex flex-wrap gap-2 pa-4 mb-4 align-center justify-space-between"`. Это основной способ раскладки.
- **Пропсы компонентов MudBlazor**, задающие вид семантически (не хардкодом):
  `Color="Color.Primary"`, `Color="Color.Secondary"`, `Variant="Variant.Filled"`, `Typo="Typo.h4"`, `Elevation="0"`, `Size="Size.Small"`, `Dense="true"`.
- **Именованные классы из `app.css`**: `Class="page-title-accent"`, `Class="kesco-card"` и т.п. Если нужного класса нет — **добавить его в `app.css`**, а не писать инлайн.
- **Чисто структурный инлайн без визуала** (`style="min-width:0;overflow:hidden;text-overflow:ellipsis"`) допустим, но предпочтительнее вынести в utility-класс. Визуала в нём быть не должно.

**Правило замены:** увидел визуальный инлайн → заведи/используй класс в `app.css` → в разметке оставь только `Class="…"`.

---

## 4. Канонические рецепты по элементам

Агент использует **только** эти способы. Никаких новых вариантов оформления одного и того же элемента.

| Элемент | Как делать | Как НЕ делать |
|---|---|---|
| Заголовок страницы | `<MudText Typo="Typo.h4" Class="page-title-accent">Текст</MudText>` | свой `font-size`/`border-bottom` инлайном |
| Подзаголовки | `Typo="Typo.h5"` / `h6` (шкала задана в теме) | произвольный `font-weight` инлайном |
| Вторичный текст | `Typo="Typo.body2"` + класс, если нужен цвет | `Style="color:var(--lh-grey-dark)"` |
| Кнопка действия | `<MudButton Color="Color.Primary" Variant="Variant.Filled">` | `Style="background:#05164D"` |
| Акцентная кнопка | `Color="Color.Secondary"` (золото задано в app.css) | свой золотой фон инлайном |
| Иконочная кнопка | компонент `KescoButton` (тултип + `MudIconButton`) | голый `MudIconButton` со `Style` |
| Карточка | `<MudPaper Elevation="0" Class="pa-5 kesco-card">` | `Style="border-left:3px solid …"` |
| Диалог | `MudDialog` (заголовок стилизован глобально в app.css) | свой фон/паддинг заголовка |
| Таблица/грид | `KescoGrid` (стилизован глобально) | ручная разметка `<table style>` |
| Ошибки/алерты | класс `.kesco-error-bar` (цвета из `--mud-palette-error`) | хардкод `#d32f2f`/`#b71c1c` |
| Золотой акцент-линия | `Class="page-title-accent"` или правило в app.css | инлайн `border-bottom` в разметке |

Нужен новый визуальный паттерн? → добавляется **класс в `app.css`** и строка в эту таблицу. В разметку попадает только имя класса.

---

## 5. Документированные исключения (белый список)

Только эти места вправе содержать CSS/цвет вне `app.css` — потому что генерируют **автономный HTML-документ** (печать/экспорт), не входящий в DOM приложения:

- `Kesco.Lib.Web.BZ.Controls/Services/KescoGridPrintStyles.cs` — **единственное** место для CSS печати. Весь CSS печати живёт здесь.
- `Kesco.Lib.Web.BZ.Controls/Services/KescoGridPrintHtmlGenerator.cs` — только *подключает* стили из `KescoGridPrintStyles.cs`, не пишет свой CSS.
- `Kesco.Lib.Web.BZ.Controls/Services/KescoGridExcelGenerator.cs` — форматирование ячеек Excel (это не UI-CSS).

Эти файлы обязаны брать значения цветов из тех же токенов (общие C#-константы, синхронизированные с `--kesco-*`), а не заводить свои hex. Всё остальное вне белого списка — под запретом и ловится сборкой.

`app.css` и `KescoTheme.cs` — не «исключения», а **единственные штатные места** визуального стиля.

---

## 6. Контроль (сборка падает при нарушении)

Соблюдение правил обеспечивается автоматически (детали и промт на создание — в `promts/STYLE_PROMPTS.md`, Промт 2):

1. **MSBuild-таргет** `EnforceStyleRules` (`BeforeTargets="CoreCompile"`) сканирует `**/*.razor` и `**/*.cs` (кроме белого списка §5 и `app.css`) на запрещённые паттерны §2 и выдаёт `<Error>` → **сборка падает**.
2. **Roslyn-анализатор** `KescoStyleAnalyzer` ловит в `.cs` строковые литералы с цветом/шрифтом и генерацию `<style>` вне белого списка → **ошибка компиляции**.

Обе проверки включены в оба проекта (`Kesco.App.*` и `Kesco.Lib.*`). Диагностики сразу уровня Error, `TreatWarningsAsErrors` не требуется.

---

## 7. Чек-лист агента перед завершением UI-задачи

- [ ] Ни одного визуального `style=`/`Style=` в изменённых `.razor`.
- [ ] Ни одного hex-цвета/шрифта в `.cs` вне белого списка §5.
- [ ] Цвета берутся из `var(--mud-palette-*)` (адаптивные) или `var(--kesco-*)` (бренд), только в `app.css`/теме.
- [ ] Новые визуальные паттерны оформлены классом в `app.css` и внесены в таблицу §4.
- [ ] Раскладка — через utility-классы Mud (`d-flex`, `gap-*`, `pa-*`), не инлайном.
- [ ] Локальная сборка проходит (проверки §6 не сработали).
