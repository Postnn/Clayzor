# Handoff: разделение гридов EquipmentForm

## Этап

Выполнено без commit/push/merge.

## Изменение

В `Inventory/wwwroot/css/app.css` для `.equipment-detail-grid` сохранён существующий
`margin-top: 5px` и добавлен hairline `outline: 1px solid var(--mud-palette-lines-default)`.
Outline не увеличивает размер грида и не изменяет запрошенные 5px. До изменения границы
уже имели фактические 5px, но визуально сливались из-за отсутствия внешнего контура.

## Проверки

- Реальная `/Equipment/EquipmentForm?id=15`: расположения → ответственные = 5px,
  ответственные → бухучёт = 5px; у всех outline=1px solid.
- Скриншот проверен: видимые гриды разделены и имеют тонкий контур.
- `dotnet build src/Kesco.App.Web.Inventory/Kesco.App.Web.Inventory.csproj --no-restore`:
  0 ошибок, 14 прежних warnings.
- Временный screenshot удалён, вкладка проверки закрыта. Inventory/Users остаются
  запущенными (`bash-lm9zl7rt`, `bash-hm6b50kb`).
