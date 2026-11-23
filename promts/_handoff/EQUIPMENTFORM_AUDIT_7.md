# EquipmentForm — протокол EQ7

Дата: 22.11.2026. Работа выполнена в `main`, merge не выполнялся. На момент протокола изменения находятся в рабочих деревьях и ещё не опубликованы.

## База и ветки

- Суперпроект `05f2fc7561bf2a5648cca7fce991be6e4c12cdb2`.
- Inventory `b3da274962c3c0811f71fbfe196f188ed3b5f659`.
- Entities `bf909e66d89a9a9c2f3fdc5069dd03b9b56368d7`.
- Tests `48fea7751b0797a2fe0d0c6a01a86045a8bd66ca`.
- Отдельно сохранены предшествующие изменения ширины Date/Time и документации Net; они не откатывались.

## P1-01 — обязательные чтения

`EquipmentFormService` перевёл чтения, формирующие исходные снимки и влияющие на DML, на `QueryRequiredAsync` и fail-closed scalar-helper. SQL connectivity больше не превращает UPS, SIM duplicate, accounting, расположения, ответственных, аккаунты и сетевой счётчик в пустой результат.

Доказательства:

- `EquipmentRequiredReadTests` — первый/второй SELECT UPS, SIM duplicate, accounting, locations, responsibles, network count и легитимный no-row.
- bUnit: ошибка первого/второго чтения UPS не открывает пустой диалог и сохраняет SN draft; после восстановления повторное открытие успешно; поздний ответ прежнего EquipmentId не открывает диалог.
- SIM duplicate connectivity обрабатывается без DML. В `EquipmentSimSaveBuilder` списание дубликата дополнительно ограничено актуальным номером, несписанным состоянием и `@@ROWCOUNT=1` в той же транзакции.

## P1-02 — CurrentUser и роли

- `EquipmentForm` загружает `Employee CurrentUser` при первичной загрузке.
- `Employee.GetCurrentAsync` сверяет серверный Windows principal с `SUSER_SNAME()` и SID. Неизвестная идентичность запрещает действие.
- Роль проверяется свежо через `fn_ТекущиеРоли()` с глобальной областью `КодЛица=0` либо конкретным лицом. Запись не принимает EmployeeId от клиента.
- Подтверждены роли 42/43/44/66/68/102/103 по таблице `Роли`; добавлены соответствующие enum.
- `EmployeeAuthorizationTests` покрывают principal mismatch, отзыв роли, SQL identity error, отсутствие provider, global/local scope.
- Старые тестовые сценарии получили эмуляцию доверенной identity в `ScriptedConnection`; негативные тесты используют явные ответы и не ослабляют production fail-closed.

На реальном стенде `SUSER_SNAME()` соответствует сотруднику 4. Реальные роли: 42/43/44/102/103 глобально, 66 для лица 1. Полная много пользовательская матрица с замещением выполнена только в automated tests, не в браузере под разными Windows-учётными записями.

## P1-03 — конкурентный accounting UPDATE

`EquipmentAccounting.SaveAsync` для существующей строки использует один SQL batch с `UPDLOCK,HOLDLOCK`, проверкой ID, EquipmentId, `Изменено`, фактического и нового владельца, документов и `@@ROWCOUNT`. Статусы 1–5 не считаются успехом. INSERT и существующий exact-id DELETE не переписаны.

Дополнительно исправлены два дефекта, найденные только реальными проверками:

1. Проверка документов использует существующее `[Документы].dbo.vwДокументы`; первоначальный `vwДокументы` в базе Inventory отсутствовал.
2. При открытии существующей записи теперь копируется `ChangedAt`. До исправления `DateTime.MinValue` вызывал `SqlDateTime overflow` до SQL и разрывал circuit.
3. После success-refresh новая UTC-дата переводится в локальное время; это проверено отдельно.

Реальный SQL Server 2022 `16.0.1220.3`, сотрудник 4, активный триггер `tОборудованиеБухУчета_InsUpdDel`:

| Сценарий | Результат |
|---|---|
| Создание оборудования через `sp_Оборудование_Ins` | Equipment ID 19, accounting ID 21 |
| Обычный UPDATE | статус 0, `Изменил=4`, новая версия |
| Старая версия после первого UPDATE | статус 1, новое примечание не применено |
| Другое оборудование | статус 1 |
| Недоступный документ | статус 4 |
| Новый владелец без accounting scope/роли | статус 3 |
| Повторный UPDATE с актуальной версией | статус 0 |
| После batch | `@@TRANCOUNT=0`, trigger enabled |

Прямая попытка тестового изменения фактического владельца была отклонена штатным триггером; триггер не отключался и не обходился. Реальную потерю сети SQL 53 на работающем стенде не вызывали; она покрыта mock/bUnit, а не заявляется как реальный connectivity test.

Browser на Inventory: редактирование accounting ID 21 через диалог изменило примечание на `EQ7 UI saved`, строка обновилась без перезагрузки, ошибок/alert нет. UI DELETE удалил именно последнюю запись, секция показала «Нет записей бухгалтерского учёта», SQL count=0.

## P2-01 — услуги SIM

Восстановлены `ОтПлан`, `ДоПлан`, `УслугуНеобходимоОтключить`, `ОбязательнаяУслуга`, сортировка `_BLOCK`, warning-state, итоги стоимости и текст «заблокировано». Фактический `До` остаётся эксклюзивным в БД и включительным в UI.

Важно: действующий legacy SQL возвращает оба плановых поля как NULL. Новый источник не придуман; колонки появляются только при фактических плановых данных.

- `EquipmentSimServicePersistenceTests` — маппинг, границы плана, active/expired `_BLOCK`, exclusive/inclusive dates.
- bUnit — warning, плановая колонка, `_BLOCK` и итоги.
- Реальная SIM 9: после «Показать историю» видна услуга `_CLAY`, период 17.11.2026, стоимость 2/3 и итоги 2/3; SQL-ошибок нет.

## P2-02 — детализация SIM

По `Equipment_form.aspx.cs:2622-2731` реализованы:

- кнопка только для SIM с исполнителем Билайн (код 908) и заданным `URI_accounting_phone_form`;
- свежие чтения ответственных и SIM; одна доступная SIM, legacy-выбор ответственного;
- параметры `UserT`, `Dogovor`, `DogovorT`, `Phone`, `NDSAllSumm`, `NDSIn`, `NDSStavka`, `Scale`, `OpenMonth`, `Year`, `Month`, `bln`, `User`, `Color`, `Title`, `standalone`;
- три категории НДС и стандартное query encoding;
- открытие через `clayOpen` с ключом `equipment-sim-detail`, без навигации родителя.

Внешняя проверка подчинённости отделена от ролей 41/42. При отсутствии БД `Тарификация` роль 42 продолжает работать; внешний сценарий fail closed.

`EquipmentSimDetailTests` — НДС, escaping, несколько SIM, ответственный, отсутствие доступа и внешняя подчинённость. bUnit подтверждает `clayOpen` и сохранение родительского URL. Реальный внешний биллинг не вызывался.

**Ограничение:** в текущем `web.config` URI пуст, поэтому на стенде кнопка скрыта. Фактическая конечная интеграция не проверена.

## P2-03 — сеть

По legacy `DisplayNet` восстановлены:

- warning при `count > Model.Net` и при двух портах без MAC/MAC2;
- исключение типов 33/35: warning снимается при count<3, при count>=3 секция скрывается;
- фильтр `*` и красный класс ссылок;
- fail-closed network count;
- attach/detach не выполняется после неуспешного Save/reload или `_model=null`; поздние ответы не обращаются к прежнему ID.

`EquipmentNetworkDiagnosticsTests` и bUnit покрывают Net=1/2/3, два MAC, switch/router, звёздные имена и not-found после UPDATE.

| Внешняя функция | Legacy | Clayzor / причина |
|---|---|---|
| Ping indicator | `eqmnt.ping.existInLocalNet/state()` | Не реализован; физический ping не имитирован |
| WakeOnLan | `sp_WakeOnlan` | Не реализован; процедуры на стенде нет, физический WOL запрещён |
| Attach/detach | SQL-операция | Реализовано с ролью 103; реальные Net-review данные не изменялись |

## P2-04 — остальной охват и XACT_ABORT

Существующая матрица `src/Kesco.App.Web.Inventory/docs/equipment-form-legacy-matrix.md` обновлена по SIM, сети и детализации. Повторно выполнен весь общий набор, а не только новые тесты.

Pooled SQL test на одном `SPID=59`:

- success → текущая сессия `XACT_ABORT=1`;
- повторное открытие из пула → `XACT_ABORT=0`;
- SQL divide-by-zero → текущая сессия `1`, повторное открытие → `0`.

Следовательно, утечка `SET XACT_ABORT ON` через `sp_reset_connection` не подтверждена. Данные этим тестом не изменялись; session-wide SERIALIZABLE/XACT_ABORT в новые accounting builders не добавлялись.

## R01

Создан `EQUIPMENTFORM_R01_DECISION.md`: A (текущее исключение), B (durable outbox), C (legacy-like), матрица SQL×ATS×circuit×replay. Текущая circuit-local реализация не изменена. **R01 остаётся открытым до выбора владельца.**

## Проверки

- Начальный профиль после identity-изменений: 37 падений из-за неполных test doubles; после исправления контракта заглушек — 308/308.
- Новые detail/network тесты сначала были before-red на отсутствующих методах, затем after-green.
- Финальный профиль Equipment/UPS/EmployeeAuthorization — 336/336.
- `dotnet build Clayzor.sln --no-restore --verbosity:minimal --consoleLoggerParameters:ErrorsOnly` — успешно, 0 ошибок, 35 существующих предупреждений.
- Общий набор без `ClayTreeMutationAbortTests` — **989/989**. Первый запуск выявил один тест principal с implicit identity; после явной настройки fake повтор полностью зелёный.
- `ClayTreeMutationAbortTests` исключён целиком из-за ранее документированного несвязанного зависания; ClayTree не изменялся.
- Browser: EquipmentForm 19 в 1418×902 и 320×900; компоновка без горизонтального переполнения, неподвижные toolbar/footer, реальные CRUD и SIM 9. Временные скриншоты не входят в репозиторий.

## Очистка и процессы

- Equipment 19, accounting 21 и связанные location/responsible удалены; контрольные counts равны 0.
- Net-review порты 9–13 и 15 не изменялись; розетки и расположения Net не использовались для новых записей.
- Отключённых триггеров нет.
- Временные SQL/PS1-файлы удалены.
- Запущены ровно три приложения: Styles 5040, Inventory 5020, Users 5030.

## Изменённые файлы

- Entities: `DatabaseEnums.cs`, `SQLQueries.cs`, `Inventory/Employee.cs`, `EquipmentAccounting.cs`, `EquipmentNetworkPortLink.cs`, `EquipmentSimCard.cs`, `EquipmentSimSaveBuilder.cs`, `EquipmentSimService.cs`, `ColumnNames.cs`.
- Inventory: `EquipmentForm.razor[.cs]`, `Services/EquipmentFormService.cs`, `web.config`, `docs/equipment-form.md`, `docs/equipment-form-legacy-matrix.md`.
- Tests: новые `EquipmentRequiredReadTests.cs`, `EmployeeAuthorizationTests.cs`, `EquipmentSimDetailTests.cs`, `EquipmentNetworkDiagnosticsTests.cs`; профильные Equipment tests и общий `ClayGridTestDbFakes.cs`.
- Суперпроект: `EQUIPMENTFORM_R01_DECISION.md`, данный протокол; предшествующие AGENTS/Net/Controls изменения сохранены.

## Честный статус

**Готово к независимой повторной проверке фактического diff, но не объявляется полной приёмкой legacy.** Открыты: выбор R01, решение R04 по MAC-нормализации, реальная внешняя детализация/URI, физические ATS/UPS/WOL, много пользовательский browser-тест ролей и проверка на SQL Server 2008 R2. Стенд подтверждён только на SQL Server 2022.
