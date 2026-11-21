# Handoff: уточнение стиля по Kimi Design / Lufthansa

## Этап

Файл `promts/clayzor-lufthansa-style.kui.fig` разобран, центральные стили уточнены.
Commit/push/merge не выполнялись. В рабочих деревьях остаются прежние изменения меню,
ClayGrid и фильтр-трея.

## Источник

- Контейнер ZIP: `canvas.fig` (`fig-kiwi`), `meta.json`, `thumbnail.png`.
- Страница «Токены стиля — 02» содержит явные значения, поэтому бинарный canvas не
  потребовался для подтверждения правил.
- Шрифты Lufthansa в пакет не входят и в Windows не установлены. Сохранён Verdana +
  Arial/sans-serif; CDN и новые зависимости не добавлялись.
- Описана только светлая тема; существующая dark palette не изменялась.

## Изменения

- `ClayColors.cs`: Navy Deep `#000245`, Navy Mid `#52627C`, Surface `#F5F5F5`,
  Hairline `#EBEBEB`; основной текст использует Navy `#05164D`.
- `ClayTheme.cs`: светлый secondary text = Navy Mid; H4 = 36px/300, H5 = 24px/700;
  Button = базовый размер/700/normal letter spacing/без uppercase; Caption/Overline =
  secondary size; radius ссылается на общий CSS token.
- `clay.css`: base 14px (`0.875rem`), secondary 13px (`0.8125rem`), control radius
  4px, card/dialog radius 8px; toolbar/icon buttons используют control radius;
  grouping/filter chips — pill; таблицы — светлая шапка, navy текст и нижняя линия,
  без uppercase. Два прежних неопределённых `--clay-font-size-small` заменены на
  `--clay-font-size-secondary`.
- `STYLE_RULES.md` и `MedicalTests/AGENTS.md` синхронизированы с новыми токенами.
- Добавлены `ClayThemeTests.cs` (2 теста палитры, типографики и radius contract).

## Проверки

- Targeted `ClayThemeTests|ClayGridOptionsTests|ClayGridSectionTests|ClayGridReinitTests`:
  30/30.
- Финальный `dotnet build Clayzor.sln --no-restore`: 0 ошибок, 31 прежний warning.
- Реальный Inventory, вычисленные значения: 14px/13px, 4px/8px, primary
  `rgba(5,22,77,1)`, background `rgba(245,245,245,1)`; шапка таблицы light gray/navy,
  `text-transform: none`; icon radius 4px.
- Dark toggle: сохраняется `clay-inventory-theme=dark`, primary `rgba(74,124,255,1)`,
  background `rgba(14,18,32,1)`; шапка использует dark background-gray и dark primary.
  Грид содержит 6 записей. Затем восстановлена светлая тема.
- EquipmentForm №5: загружена без loading/notFound; input radius=4px, paper radius=8px.
  После центрального правила `.mud-button` отдельная браузерная проверка подтвердила
  button font-size=14px, transform=none, radius=4px. Временная вкладка закрыта.
- Временные extraction/screenshots `promts/.style-analysis` удалены; исходный `.fig`
  сохранён без изменений.

## Окружение

Inventory: `bash-lm9zl7rt`; Users: `bash-hm6b50kb`; Styles продолжает работать:
`bash-ypa8ywn8`.

## Следующее действие

Ожидается визуальная проверка владельцем либо команда на commit/push. Если нужен именно
Lufthansa Head/Text, отдельно потребуются лицензированные font assets и разрешение на
их подключение.
