# Независимая проверка Tel_6 против legacy и задания TEL5-UI-01

Дата: 19 ноября 2026 г.

## Точно проверенный baseline

- Суперпроект `Postnn/clayzor` — `cd49ed677fd815250f1f72606827ef97feb1e1e3` (`tel-2-fixes`), на 5 коммитов впереди `main`, без merge.
- `Postnn/Kesco.App.Web.Inventory` — `75cb36c5b45d6a3dc919bb1b69cdce18d1a14189`.
- `Postnn/Clayzor.Lib.Web.Controls.Tests` — `354d612df42e365660ea30e45b5d6b55b9aa4572`.
- Diff к Tel_5: Inventory `ac8b8cb9...75cb36c5`, Tests `48329a1c...354d612d`; в Inventory изменены только Tel.razor.cs и 2 документа, в Tests только TelAsyncStateTests.cs.
- Контрольные legacy-источники: `KescoDev/Kesco.App.Web.V4.Inventory/Inventory/Telefon/Tel.aspx`, `Tel.aspx.cs`; в legacy удаление явно подтверждается и не содержит понятия pending-синхронизации Blazor.

## Результаты

**Принято в исходниках:** `HasUnsavedDraft` и `HasPendingSync` разделены; `CloseConfirmation` объединяет два предупреждения; `DeleteConfirmation` всегда содержит явный вопрос об удалении; `RefreshAsync` подтверждает только потерю черновика и вызывает `LoadCurrentAsync(..., preservePending: true)`; `CancelAsync` и `BeforeInternalNavigationAsync` используют сообщения об обоих рисках. Изменений SQL, синхронизации, слоя доступа к данным в Tel_6 не внесено.

**Тесты:** в `TelAsyncStateTests` добавлены реальные bUnit `MudDialogProvider`/`ConfirmDialog` для Cancel (4 комбинации), Delete (4), Refresh (4), подтверждения Delete и Refresh. Отказ действительно проверяется по модели, pending и отсутствию вызова сервиса. Однако Delete-тесты вызывают private `DeleteAsync()` через reflection, а не нажимают видимую кнопку. Отдельного интеграционного теста навигации через `NavigationManager` в новом diff нет. Заявленные сборка `0 ошибок`, `31/31`, `136/136`, `329/329` и браузерные проверки — по отчёту агента: самостоятельно сборка, браузер и SQL Server не запускались.

## Найденное несоответствие: TEL6-UI-01 — Delete при pending недоступен через UI

Приоритет **P2, подтверждённый дефект доступности UI**, не дефект бизнес-логики Delete.

`Postnn/Kesco.App.Web.Inventory/Components/Pages/Telefon/Tel.razor`:

```razor
<ClayToolbarButton Options="_deleteOptions"
    Disabled="@(_loading || _saving || !_canEdit || _pendingOperations.Count > 0)"
    OnClick="DeleteAsync" />
```

`Postnn/Clayzor.Lib.Web.Controls/Components/Form/ClayToolbarButton.razor` реализован через `<MudButton Disabled="@(Disabled || _opt.Disabled)">`. Следовательно, при `_pendingOperations.Count > 0` Delete реально отключён. В то же время в Tel_6 добавлены `DeleteConfirmation(HasPendingSync)` и сценарий `Delete_ConfirmedDraftAndPendingDeletesPhone`, явно предусматривающие удаление при pending. Тест обходит кнопку, вызывая `DeleteAsync()` reflection. Заявленное «Delete с pending предупреждает» **не реализовано для пользователя через toolbar**.

### Точное задание агенту

1. Определить и зафиксировать ожидаемое поведение: согласно `TEL5-UI-01` и новому диалогу — разрешить Delete при pending после явного предупреждения (если нет отдельного обоснованного требования блокировки). Убрать только запрещающее условие `_pendingOperations.Count > 0` из `Disabled` кнопки Delete; оставить защиту `_loading`, `_saving`, `!_canEdit` и подтверждение действия. Не менять SQL или проверки ролей.
2. Добавить bUnit-тест, проверяющий **rendered toolbar MudButton**, а не прямой вызов `DeleteAsync`: при `pending=true` кнопка доступна (если права и форма загружена); клик открывает `MudDialog` с явным вопросом удаления и предупреждением об АТС; «Нет» сохраняет черновик/pending, `GetDeleteSnapshotAsync`, `DeleteAsync` и ATS не вызываются; «Да» запускает корректную последовательность с persisted snapshot. Проверить `draft=false/true` и `pending=false/true`.
3. Добавить проверку фактической внутренней навигации через `NavigationManager`/`NavigationLock` для комбинаций draft/pending, включая отказ от перехода. Нынешний код выглядит корректным, но diff Tel_6 тестирует Cancel/Delete/Refresh, а не NavigationLock.
4. Отдельно документировать браузерное ограничение: `NavigationLock.ConfirmExternalNavigation` вызывает системный beforeunload prompt, его текст браузер не позволяет произвольно заменить. Это не дефект C# и не основание дублировать SQL-авторизацию.
5. Прогнать профильные и связанные UI-тесты, проверить реально кликабельный toolbar в браузере, сообщить SHA. Не выполнять merge до проверки.

## Риски и вердикт

По текущему diff остальные исправления TEL5-UI-01 соответствуют цели и не привносят новых SQL/ATS-расхождений с legacy. Один точечный P2 надо закрыть для заявленного сценария Delete с pending. По прежнему остаются не снятые **интеграционные ограничения**, заявленные агентом: нет реальных фикстур расположений/розеток, нет вызовов внешних АТС и автоматического восстановления pending после потери circuit; полный нефильтрованный набор тестов не объявлен пройденным. Эти ограничения не являются новыми дефектами Tel_6.

**Итог:** принять реализацию разделения черновика/pending, но **не считать весь UI-пункт TEL5-UI-01 окончательно закрытым** до исправления отключённого Delete и регрессионного UI-теста. Рекомендуется короткий локальный follow-up, а не новый полный цикл переписывания формы.
