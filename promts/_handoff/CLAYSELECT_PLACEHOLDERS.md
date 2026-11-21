# Handoff: placeholder ClaySelect «Выберите»

## Этап

Выполнено без commit/push/merge.

## Изменения

Во всех явных placeholder `ClaySelect` слово «Введите» заменено на «Выберите»:
41 вхождение в Inventory (Tel, SelectTest, EquipmentIns, EquipmentForm,
ModelEquipmentForm и detail-dialogs). Обновлены 2 связанных литерала в тестах.
Плейсхолдеры других контролов не изменялись. `ClaySelectOptions.Placeholder` по умолчанию
остаётся пустой строкой — существующего текста по умолчанию не было.

## Проверки

- Search по `src` и тестам: старых placeholder «Введите» не осталось.
- Targeted `ClaySelectTests|EquipmentInsTests`: 48/48.
- `dotnet build Clayzor.sln --no-restore`: 0 ошибок, 35 прежних warnings.
- Реальная EquipmentForm №15: placeholder `Выберите документ списания`, `Выберите тип
  оборудования`, `Выберите модель`, `Выберите владельца`, `Выберите документ поступления`,
  `Выберите поставщика`; старого текста в DOM нет.
- Вкладка проверки закрыта. Inventory/Users запущены: `bash-3nkd5v5j`, `bash-6a2vhd3t`.
