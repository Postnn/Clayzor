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
