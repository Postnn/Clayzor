# Handoff: подпись основного номера сотрудника в Tel

## Этап

Исправление завершено. Commit/push/merge не выполнялись.

## Причина и изменение

После перехода общей темы на Verdana 14px подпись «Основной номер сотрудника:» требовала
218px при фиксированной label-колонке 200px и заходила на чекбокс. Другие подписи формы
не превышали 200px.

В `Inventory/wwwroot/css/app.css` обе label-колонки `.phone-form-pair`, а также
`.phone-form-single` / `.phone-form-section` и tablet-вариант увеличены до 230px.
Мобильная одноколоночная раскладка не изменена. Локальных визуальных override в Razor нет.

## Проверки

- Реальная форма `/Telefon/Tel?id=1002`: label column=230px, подпись заканчивается до
  чекбокса, визуально читается полностью.
- Viewport 480×900: одна колонка 428px, `white-space: normal`, горизонтального
  переполнения страницы нет.
- `dotnet build src/Kesco.App.Web.Inventory/Kesco.App.Web.Inventory.csproj --no-restore`:
  0 ошибок, 14 прежних warnings.
- Временный screenshot удалён, вкладка проверки закрыта. Inventory/Users остаются
  запущенными (`bash-lm9zl7rt`, `bash-hm6b50kb`).
