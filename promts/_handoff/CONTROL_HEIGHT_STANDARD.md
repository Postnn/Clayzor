# Handoff: глобальная высота однострочных полей

## Этап

Выполнено. Сабмодули закоммичены и запушены в `main`: Controls `3f70786`, Inventory `62669bb`; суперпроект фиксирует обновлённые ссылки и документацию этапа.

## Причина и изменения

Локальное правило 32px действовало только на ModelEquipmentForm/EquipmentForm, а диалоги
MudBlazor находятся вне этих контейнеров и получали другую высоту.

- `Controls/wwwroot/css/clay.css`: добавлен `--clay-control-height: 32px`; однострочные
  Mud input controls получают height/min-height через глобальный selector, включая диалоги.
  Checkbox/radio исключены; textarea не сжимается. `ClaySelect` использует тот же token,
  дублирующие height/min-height у ClayTime удалены.
- `Inventory/wwwroot/css/app.css`: удалены локальные height/padding/helper-height правила
  Model/Equipment. Раскладка и ширина форм не менялись.
- Корневой `AGENTS.md`: явно зафиксированы глобальный token, запрет локальных высот и
  исключение многострочных редакторов. `STYLE_RULES.md` содержит token в примере :root.

## Проверки

- `dotnet build Clayzor.sln --no-restore`: 0 ошибок, 31 прежний warning.
- EquipmentForm №8 после hard reload: Mud=[32], ClaySelect=[32]; notes textarea=63px.
- Accounting dialog: Mud=[32], ClaySelect=[32], textarea=63px. Полная раскладка документов
  сохранена, screenshot проверен и удалён, Save не вызывался.
- Tel №1002: Mud=[32], ClaySelect=[32].
- ModelEquipmentForm №945: Mud=[32], ClaySelect=[32].
- Временная вкладка закрыта. Inventory/Users/Styles запущены: `bash-bjs13q3q`,
  `bash-uesr5no8`, `bash-bmwmcnip`.
