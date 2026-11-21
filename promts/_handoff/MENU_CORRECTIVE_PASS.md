# Handoff: исправления меню Inventory по Menu_1.md

## Состояние этапа

Кодовые исправления MENU-V2-01/02/05/06 и их профильные проверки завершены.
Не возвращать AppBar/drawer и корень «Операции». Показ `ex.Message` и строки
подключения с маскировкой пароля сохранён по прямому требованию владельца.
Доступ `devadmin` к настройкам также сохранён. Commit/push/merge не выполнялись.

## Опубликованная база

- Superproject: `2d13f90`
- Inventory: `aa37f25`
- Controls: `03750bc`
- Entities: `46cce48`
- Tests: `0894fc0`

## Незакоммиченные изменения

- Controls `Components/Grid/ClayGrid.Dynamic.cs`: reset повторно применяет URL
  до сохранения/SELECT; default URL-фильтр сравнивается с условием той же колонки
  в реальной сериализации `flt<gridId>` через `GetSavedUrlFilterParams` и
  `FilterTreeHasColumn`.
- Controls `wwwroot/js/clayNavigationMenu.js`: arrow/Home/End выбирают только
  enabled пункты; `ClayNavigationMenuPanel.razor` выводит явное `aria-disabled="true"`.
- Inventory `Components/InventoryMainMenu.razor.cs`: principal получается из
  `AuthenticationStateProvider`, изменения auth state обновляют меню без SQL;
  реализована отписка `Dispose`. `Program.cs` регистрирует
  `AddCascadingAuthenticationState()`. Devadmin и четыре legacy-группы сохранены.
- Inventory `Components/Pages/MenuTest.razor`: пункт 3 disabled между enabled.
- Tests: `ClayGridReinitTests.cs` покрывает forced/default после reset и четыре
  варианта default против сериализованного фильтра; `InventoryMenuTests.cs` —
  devadmin и четыре роли; `InventoryHomeTests.cs` — fake AuthenticationStateProvider;
  `ClayNavigationMenuTests.cs` — ARIA disabled.
- Root `AGENTS.md`: обязательное обновление handoff после каждого этапа.
- Inventory `docs/inventory-menu.md`: ранее добавленное разрешение devadmin.

## Проверки

- `dotnet build Clayzor.sln --no-restore`: успешно, 0 ошибок; существующие
  предупреждения NuGet о уязвимостях и тестовые nullable/obsolete warnings.
- Итоговый объединённый профиль `InventoryMenuTests|ClayNavigationMenuTests|InventoryHomeTests|UrlFilterParserTests|ClayGridReinitTests|ClayGridSectionTests`: 60/60.
- Полный `dotnet test --no-build` остановлен после >11 минут без результата.
  Повтор с `--blame-hang`: 783 passed, затем завис
  `ClayTreeMutationAbortTests.AddChild_SqlException_DoesNotRunSuccessPath`.
  Отдельный запуск этого теста также завис и прерван blame через 20 секунд.
  Это несвязанный существующий тест дерева; его код не менять в этой задаче.
  Созданные TestResults/GUID-каталоги удалены.
- Браузер `/menu-test`: 80 пунктов, disabled 3 имеет `aria-disabled=true` и нет href;
  клавиши 1→2→4, ArrowUp, End→80 со scrollTop=693, Home→1 работают.
- Браузер `/?id=1001`: реальные данные загружены; у devadmin виден disabled пункт
  «Настройки инвентаризации» с отсутствующим `URI_inventory_queries`; тема переключается
  (body `rgb(247,248,250)` → `rgb(14,18,32)`).
- Реальная БД: `/?id=1001&SN=eq~CLAY_URL_FORCED` даёт «Всего: 0 записей» до reset,
  после reset и после reload. Ошибок/500 нет. Меню 1001→1012→1001 загружает данные
  без busy-overlay; отмена reset закрывает диалог без изменения данных.
- Read-only SQL после UI reset: в `vwНастройки` для CLID=0/shared=0 нет строк
  `InvRows_1001`, `InvFields_1001`, `InvFilter_1001`, `InvSort_1001`, `InvGroup_1001`,
  `InvQuickSearch_1001`. Персистенция остаётся блокером схемы/триггера; ничего не менять.
- Tooltip observer, 200 DOM mutations: без tooltip 0,8 мс (0,004 мс/мутация),
  с одним tooltip 1,6 мс (0,008 мс/мутация). Оснований для переписывания нет.

## Оставшиеся ограничения

Девять внешних URI не заданы; Styles `localhost:5040` не проверен с удалённого клиента
и не готов как универсальный remote/HTTPS адрес; 1004 требует отсутствующий
`SUSDB.dbo.vwrpComputerTarget`; реальный embedding host не проверен; параметры
настроек отсутствуют в БД. Не выдумывать значения и не менять схему/триггеры.

## Следующее действие

Ожидается команда владельца на commit/push либо новый corrective pass. Полный test suite
не зелёный из-за указанного несвязанного зависания дерева. Приложения Inventory/Users/Styles
запущены через `dotnet run --no-build`; Chrome DevTools доступен на 9222, helper:
`C:/Users/devadmin/AppData/Local/Temp/clay-cdp.ps1` (добавлен `Key.press`).
