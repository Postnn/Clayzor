# Tel_5 — независимая проверка соответствия legacy

**Дата:** 19 ноября 2026 г.  
**Объект:** ветка `tel-2-fixes` суперпроекта `Postnn/clayzor`, коммит `b21ed4f3cae618757d568e74e6210806e9e3c3d8`.  
**Inventory:** `Postnn/Kesco.App.Web.Inventory`, `ac8b8cb91391cd860f93b4e979616c294fde1685`.  
**Тесты:** `Postnn/Clayzor.Lib.Web.Controls.Tests`, `48329a1c18ae4e7a677dc591858447f611c7a3b6`.  
**Зависимости:** Entities `bd56dd5798d5cb583343623a5dc9f632197f99f9`, Controls `91a27c46f5305ae9dbd8405527ed44551232bfc5`, DBSelect `8ae98fb8c2a5fea20515a21895c1df9791c46327`. Эти SHA фактически закреплены в суперпроекте.

## Метод

Проверены реальный diff Tel_4→Tel_5, исходные файлы на указанном commit, новые тесты, реализации `PhoneFormService`, `PhoneSyncPlanner`, `PhoneSaveBuilder` и соответствующая legacy-логика `Inventory/Telefon/Tel.aspx.cs` и `Kesco.Lib.Entities/Corporate/Phones/Phone.cs` из `KescoDev/Kesco.App.Web.V4.Inventory`. Проверка **статическая**, без самостоятельного запуска SQL Server, браузера или АТС. Отчёт агента о `315/315` — не независимое подтверждение результатов запуска.

## Проверка задач Tel_5

| Задача | Вывод | Источник |
|---|---|---|
| TEL4-F01: план АТС после серверного FullNumber | **Подтверждено**: `previewOperations` применяется для Xorcom pre-clean, после `Service.SaveAsync(request)` `PhoneSyncPlanner.Build` вызывается заново, уже выполненные предварительные операции исключаются через `Except(preSaveOperations)`. Успешный SQL Save выполняется один раз. | `Tel.razor.cs`, `SaveAsync`, строки около 697–746; `TelAsyncStateTests`: `Save_ServerFullNumberChangeTriggersPostSync`, `Save_UnchangedServerFullNumberDoesNotSync`, Xorcom A→B. |
| TEL4-F02: свежий snapshot Delete | **Подтверждено с оговоркой**: `GetDeleteSnapshotAsync` перечитывает `GetAsync(id)` (основную запись и связи) и станцию, а `BuildDelete` получает persisted snapshot, игнорируя черновик. Если телефон уже отсутствует при чтении, метод возвращает `null`, `DeleteConfirmedAsync` прекращает удаление и не вызывает АТС. | `PhoneFormService.cs`, `GetDeleteSnapshotAsync`; `Tel.razor.cs`, `DeleteConfirmedAsync` около 870–901; `TelAsyncStateTests`: `Delete_UsesFreshPersistedEquipment`, `Delete_MissingPersistedPhoneDoesNotSync`. |
| TEL4-F03: предупреждение pending | **Реализовано, но текст требует уточнения**: четыре обработчика используют `CloseConfirmation`; при `pending` отображается сообщение о сохранённых SQL-данных и потере повтора АТС. Однако сообщение заслоняет сведения о **новом несохранённом черновике** и не содержит явного вопроса «Удалить номер?» в операции Delete. | `Tel.razor.cs`, `CloseConfirmation` около 127, `DeleteAsync`, `RefreshAsync`, `CancelAsync`, `BeforeInternalNavigationAsync`; тесты `PendingSync_CloseConfirmationExplainsSavedDataAndManualSync` и `Draft_CloseConfirmationKeepsOriginalMessage` тестируют только строковый helper. |
| TEL4-F04: интеграционный риск | **Корректно признан**: нет фикстур для реальных розеток, не запускались Meridian/Xorcom/CCM, отсутствует durable outbox. Гонка между snapshot и физическим SQL DELETE сохраняется. | `docs/tel-legacy-audit.md`, секция Tel_5; `docs/phone-form.md`. |

## Сверка с legacy

1. **Save.** В legacy сначала выполняется бизнес-валидация, затем `Phone.Save` и синхронизация внутри `TransactionScope`. В Clayzor — атомарный SQL Save и внешняя синхронизация после commit, кроме необходимой предварительной очистки старой Xorcom. Это сознательная техническая разница, уже задокументированная. Исправление Tel_5 устраняет несоответствие между авторитетным `FullNumber` и постпланом синхронизации.
2. **Delete.** Legacy использует `Phone.GetPhoneEquipments(true)` для принудительного обновления состава оборудования перед удалением. Clayzor теперь делает актуальное чтение телефона и оборудования через `GetDeleteSnapshotAsync`; соответствует цели этой части legacy. Два SQL-чтения нового снимка и последующий DELETE не представляют собой единую транзакцию: изменения конкурентного пользователя всё ещё могут произойти между чтением и удалением. Агент не заявляет, что эта гонка устранена.
3. **Xorcom.** Предварительная очистка старой станции до Save/Delete и компенсационное восстановление при SQL-ошибке сохраняются из Tel_3/Tel_4. Это учитывает подтверждённую особенность процедуры, которая определяет станцию по текущей записи номера в SQL.
4. **Роли.** Дополнительные C#-проверки на `SynchronizeAsync` не требуются по согласованному архитектурному решению: исполнение и авторизация процедур контролируются SQL Server.

## Новое замечание TEL5-UI-01 (P2): неоднозначное предупреждение при pending

**Доказательство:** `CloseConfirmation(draftMessage)` при `_pendingOperations.Count > 0` всегда возвращает только сообщение про уже сохранённые SQL-данные. Метод не учитывает, могли ли пользовательские изменения появиться **после** последнего SQL Save. `IsModified` объединяет наличие `pending` и разницу черновика, поэтому текущий код умеет отличить их, но не использует это различие в тексте. Тест `PendingSync_CloseConfirmationExplainsSavedDataAndManualSync` специально проверяет, что исходное сообщение о черновике из результата исключено.

**Воспроизведение:** сохранить номер, получить ошибку АТС, изменить `Примечания` и нажать `Отмена`. Показан текст «Данные уже сохранены в БД…», не предупреждающий об утрате последнего изменения примечания. При выборе `Удалить` с pending также исчезает обычный вопрос об удалении номера и его связей; остаётся только сообщение про потерю повтора АТС. Пользователь может принять подтверждение, не понимая, что удаляет телефон.

**Предлагаемое решение:** раздельно вычислять `HasUnsavedDraft` (сравнение исходной и текущей модели + коллекций, **без** pending) и `HasPendingSync`. Формировать конкретное предупреждение для каждого действия:

- **Cancel / навигация:** если есть оба состояния, сообщить и про потерю новых несохранённых правок, и про необходимость ручной синхронизации АТС; если только pending — только про АТС.
- **Delete:** подтверждение всегда явно сообщает, что **удаляется телефон и все связи**; при pending добавить предупреждение про нерешённую синхронизацию.
- **Refresh:** текущий метод вызывает `LoadCurrentAsync(..., preservePending: true)`; pending **остаётся в текущем circuit**, поэтому не следует утверждать, что Refresh безусловно лишает возможности повтора АТС. Если же пользователь редактировал черновик, предупредить об его сбросе.

Добавить bUnit-сценарии не только для текстового helper, но и для реального вызова `Cancel`, `Delete` и `Refresh` с комбинациями `pending=true/false`, `draft=true/false`; особенно подтвердить, что отказ в диалоге ничего не изменяет. Никакой новой роли, outbox или SQL-логики для этого не требуется.

## Качество тестов / оставшиеся ограничения

- Новые `TelAsyncStateTests` действительно содержат: новую серверную версию полного номера (Meridian, Xorcom), пустую запись при Delete, свежее оборудование и отдельный текст pending. Но проверка свежего snapshot подменяет `GetDeleteSnapshotAsync` mock-сервисом. **Само чтение SQL** с правильной сборкой основного номера, станцией и оборудованием не покрыто отдельным интеграционным тестом. По коду реализация выглядит верной, но это пробел покрытия.
- `315/315` профильных тестов, успешный build, real SQL Save 1002 и GUI — **результаты из сообщения агента**, не воспроизведённые независимо.
- Полный `dotnet test` без фильтра, по сообщению агента, завис без запуска. Нельзя считать полный набор зелёным.
- Реальные команды АТС и бизнес-случаи с розетками/расположениями не тестировались; это отдельное ограничение опытной эксплуатации.

## Итог

**TEL4-F01 и TEL4-F02 подтверждены. TEL4-F03 функционально внедрено, но остаётся поправить одно пользовательское предупреждение (TEL5-UI-01). Препятствий класса P1 в новом diff Tel_5 не обнаружено.**

Для merge рекомендую закрыть TEL5-UI-01 и зафиксировать известные ограничения интеграционных испытаний в описании PR. До этого ветку оставить без merge, как и было согласовано. Не следует создавать новые механизмы авторизации и не следует выполнять реальные команды АТС ради проверки без отдельного безопасного тестового регламента.

## Код для проверки

- [Tel.razor.cs](https://github.com/Postnn/Kesco.App.Web.Inventory/blob/ac8b8cb91391cd860f93b4e979616c294fde1685/Components/Pages/Telefon/Tel.razor.cs)
- [PhoneFormService.cs](https://github.com/Postnn/Kesco.App.Web.Inventory/blob/ac8b8cb91391cd860f93b4e979616c294fde1685/Services/PhoneFormService.cs)
- [PhoneSyncPlanner.cs](https://github.com/Postnn/Kesco.App.Web.Inventory/blob/ac8b8cb91391cd860f93b4e979616c294fde1685/Services/PhoneSyncPlanner.cs)
- [TelAsyncStateTests.cs](https://github.com/Postnn/Clayzor.Lib.Web.Controls.Tests/blob/48329a1c18ae4e7a677dc591858447f611c7a3b6/TelAsyncStateTests.cs)
- [Legacy Tel.aspx.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb3/Inventory/Telefon/Tel.aspx.cs)
