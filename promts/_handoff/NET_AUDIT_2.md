# NET-2 — исправления независимого ревью

## Статус и границы

Исправлены NET2-P1-01…03 и NET2-P2-01…02. Проверены C1–C5; C3/C4/C5 исправлены,
C1/C2 подтверждены без изменения рабочего SQL. Это готовность к повторному ревью,
не объявление окончательной приёмки. Физический PoE и реальный SQL Server 2008 R2
не проверялись.

После отдельного разрешения пользователя изменения и ссылки сабмодулей закоммичены
и запушены в `main`; merge не выполнялся. Рабочие деревья содержат только перечисленные
ниже изменения и этот протокол.

## Ревизии

| Репозиторий | Исходный SHA | Итоговый SHA / состояние |
|---|---|---|
| Суперпроект | `80082ba94c0daf6e8cfdc012845a61f0a912912e` | текущий коммит публикации в `main` |
| Entities | `87a9e860758f1c2df0b37e7730f597e18e44da44` | `bf909e66d89a9a9c2f3fdc5069dd03b9b56368d7` (`origin/main`) |
| Inventory | `1f7a12c07d0f789be9cf81cfba32c2cc7448dfb0` | `b3da274962c3c0811f71fbfe196f188ed3b5f659` (`origin/main`) |
| Tests | `0814c1f94b23b361df720746215dff94713fef65` | `48fea7751b0797a2fe0d0c6a01a86045a8bd66ca` (`origin/main`) |
| DALC / DBSelect / Controls | опубликованные NET-1 SHA | не изменялись |

Detached HEAD не было. Посторонние untracked-файлы не изменялись.

## Матрица дефектов

| Issue | Legacy / причина | Before-red | Исправление | After-green / реальная проверка | Статус |
|---|---|---|---|---|---|
| NET2-P1-01 | Legacy передаёт полный токен; `SqlParameter.Size=50` усекал его. | `Resolve_LongToken_DoesNotTruncateParameter`: ожидалось 51, фактически 50. | `DbType.String` сохранён, размер равен фактической длине токена. SQL остаётся параметризованным. | Extractor 50/51/100/200 возвращает один полный токен; fake-DAL подтверждает значение и Size. SQL: имя из 50 `a` найдено для 50, не найдено для 51. Browser порта 16: длинные токены не дали ссылки на Equipment 18, короткие `clay-test-net`/`clay-test-phone` сохранились. | FIXED |
| NET2-P1-02 | ID/модель/версия раньше захватывались после `await ConfirmAsync`. | Подтверждено по старому коду. Первая попытка before-red упёрлась в неполный script queue; это не выдаётся за доказанный вызов старого PoE. | Контекст захватывается до confirm. После confirm и после свежей identity повторно проверяются model reference, ID, load version, disposed и `PoE=1`. | bUnit A(PoE)→B(PoE)→«Да»: `sp_ResetPoEport` не вызван, TimeoutLog пуст. Неизменный A, отмена и ошибка покрыты прежними тестами. Физический PoE не вызывался. | FIXED |
| NET2-P1-03 | SQL-ошибка записывалась в сообщение внутри `_model is not null`; модель A не очищалась при ошибке B. | Первичный SQL 53: alert не появлялся, wait-for-alert red. | Отдельный `_loadError` вне модели, Retry, очистка модели/identity/lookup state при новой загрузке. | Первичный 53 → alert + Retry → успех на том же экземпляре; A→B со 10054 не показывает A; no-row B показывает «не найден». Ошибка resolver по-прежнему не удаляет основные поля. | FIXED |
| NET2-P2-01 | Старый SQL мог выбрать офис предыдущего расположения, если последнее не имело офиса. | Тест не нашёл `@LatestLocationId`; старый SQL подтверждён статически. | Сначала `TOP 1` последнее расположение по `От DESC`, затем ближайший офисный предок только его. | Реально: 9010 → вложенный Office B 9016 → расположение 86 без офиса. Новый запрос вернул NULL, старый — ошибочный 9016. | FIXED |
| NET2-P2-02 | Отложенный employee/socket ответ проверял только версию собственного контрола; ручной заказчик не инвалидировал employee lookup. | Подтверждено по коду. Первый deferred harness создавал deadlock на единственном DbManager gate; после исправления harness загрузка B начинается до освобождения A. | Load инвалидирует обе версии. После await проверяются версия, model, route ID и фактический выбор. Ручной заказчик инвалидирует employee lookup. | Ручной заказчик остаётся после позднего ответа; A→B не меняет сотрудника/заказчика/Notes B; обычное подставление организации и очистка сотрудника сохранены. | FIXED |

## Проверки C1–C5

| Проверка | Результат |
|---|---|
| C1, varchar/Unicode | `Подключено varchar(100)`, `Примечания varchar(300)`, оба `Cyrillic_General_CI_AS`; legacy и новый код используют varchar. Физическая схема не нуждается в nvarchar для текущего контракта. Реальный exchange портов 17/18 точно обменял строки с `і/ї/є/ЇЄ`, сотрудников и примечания; сроки остались NULL. VERIFIED, кодировка SQL не менялась. |
| C2, TargetFree | Собственный CASE `КодРозетки IS NULL AND КодСотрудника IS NULL AND Подключено=''` совпадает с `fn_ЛокальнаяСеть_СоседниеПорты`. Порт ID 10 только с примечанием реально имеет `Занят=0`. Соседняя функция остаётся в серверной проверке. VERIFIED, SQL не менялся. |
| C3, JS после SQL | После успешного SQL Apply сначала перезагружает сохранённую запись; OK очищает dirty-state до закрытия. `JSException/JSDisconnectedException` от refresh обрабатываются отдельным warning. bUnit подтверждает один UPDATE и `IsModified=false`. FIXED |
| C4, NULL | Колонки БД NOT NULL, прямой entity-вызов с NULL раньше падал на `Trim()`. Теперь NULL → `Trim() ?? ""`; before-red NullReference, after-green оба параметра пустые. Исторический NULL не выдумывается. FIXED |
| C5, организация | `SELECT_СотрудникПоИдентификатору` возвращает `Организация` через `ЛицаЗаказчики` тем же запросом. Mismatch показывает название; ID 0 не скрывает предупреждение. bUnit: `Организация 1` и `Организация 0`. В текущей БД все сотрудники имеют организацию 1, поэтому реальная multi-owner проверка не заявляется. FIXED |

C6: физический PoE и настоящий SQL Server 2008 R2 остаются ограничениями, не PASS.

## Токены и производительность

- 50/51/100/200 букв — один полный токен, искусственного общего лимита regex нет.
- SQL Size равен длине значения; короткие и длинные токены двух полей не смешиваются.
- 501 токен по-прежнему даёт 2 batch по 500+1; N+1 не добавлено.
- Пять предикатов, `TOP 1` без ORDER BY, `_` wildcard, `ilo-`, MAC и injection-семантика не менялись.
- Положительный поиск имени длиннее 50 нельзя проверить на текущей физической схеме
  `Оборудование.СетевоеИмя varchar(50)`: такое значение нельзя сохранить. Отрицательный
  51-vs-50 проверен реально и в браузере; положительный >50 не выдаётся за возможный PASS.

## Реальные записи и очистка

Исходные записи ревью не удалялись и не перестраивались:

- порты ID 9–13 и 15;
- розетки ID 6/7;
- расположения 9010/9011.

Для NET2 созданы и после проверок удалены только отдельные записи:

- Equipment 18, `SN=CLAY_TEST_NET2_50`, создано через `sp_Оборудование_Ins`;
- порты ID 16/17/18 (номера 90/91/92);
- временные расположения Office C 9015, Office B 9016, Room B 9017;
- связанные записи `ОборудованиеРасположение` оборудования 18.

Финальный SQL: NET2 ports/equipment/history/locations = 0, исходных review ports = 6.
Все триггеры `ЛокальнаяСеть`, `Оборудование`, `ОборудованиеРасположение`, `Расположения`
включены. Тестовое наполнение справочников использовало управляемый TRY/CATCH с
гарантированным повторным включением; штатные CRUD выполнялись с активными триггерами.

## Build, тесты и browser

- `dotnet build Clayzor.sln --no-restore --verbosity:minimal --consoleLoggerParameters:ErrorsOnly`
  — успешно, 0 ошибок, 32 прежних предупреждения.
- Профиль `NetFormTests|NetEquipmentLink|LocalNetworkPortTests` — **74/74 passed**.
- Полный нефильтрованный `dotnet test --no-build --blame-hang --blame-hang-timeout 30s ...`:
  **935 passed**, затем hang на известном
  `ClayTreeMutationAbortTests.AddChild_SqlException_DoesNotRunSuccessPath`.
  Это не полный PASS. Blame XML удалён.
- Регрессионный `--filter 'FullyQualifiedName!~ClayTreeMutationAbortTests'` —
  **935/935 passed**.
- Browser NET2: длинные токены и две независимые короткие ссылки, exchange с Unicode,
  ошибок страницы нет. Нагрузочный stress-test не проводился.
- После проверок `dotnet build-server shutdown`; в Windows остались ровно три
  `dotnet.exe`: Inventory, Users, Styles.

## Изменённые файлы и diff-summary

Entities (26 insertions, 14 deletions):

- `Inventory/ColumnNames.cs`
- `Inventory/Employee.cs`
- `Inventory/LocalNetworkPort.cs`
- `Inventory/NetEquipmentLinkResolver.cs`
- `SQLQueries.cs`

Inventory (85 insertions, 19 deletions):

- `Components/Pages/Net/Net.razor`
- `Components/Pages/Net/Net.razor.cs`

Tests (307 insertions, 6 deletions):

- `ClayGridTestDbFakes.cs`
- `LocalNetworkPortTests.cs`
- `NetEquipmentLinkExtractorTests.cs`
- `NetEquipmentLinkResolverTests.cs`
- `NetFormTests.cs`

Суперпроект: `promts/_handoff/NET_AUDIT_2.md` (этот файл). `promts/Net_2.md` не изменялся.

## Общая ширина ClayDatePicker и ClayTime

- `ClayDatePicker` получил класс `clay-date`; `clay-date` и `clay-time` используют
  `--clay-date-time-width: 180px` из общего `wwwroot/css/clay.css`. Поля не растягиваются
  вместе с формой. Правило добавлено в корневой `AGENTS.md`.
- Browser EquipmentForm: date/time по 180 px; Net: date 180 px. При viewport 1400 и 800
  размеры не меняются, `15.01.2027` отображается полностью.
- `ClayFormControlsTests` — 8/8, общий набор — 936/936. Build успешен, 0 ошибок.
- В регрессии исправлена только тестовая нестабильность `LoadErrorAfterIdChange`:
  фоновый ClaySelect мог забрать единственную script-ошибку, поэтому очередь содержит
  две SQL-ошибки для фонового и основного запросов. Продуктовое поведение не менялось.
- Дополнительные файлы: `ClayDatePicker.razor`, `wwwroot/css/clay.css`, корневой
  `AGENTS.md`, `ClayFormControlsTests.cs`, `NetFormTests.cs`.

## Незакрытые ограничения

- Реальный PoE не вызывался, `sp_ResetPoEport` на стенде отсутствует.
- SQL Server 2008 R2 недоступен; реальный прогон выполнен на SQL Server 2022
  (`16.0.1220.3`) с совместимым синтаксисом.
- Реальная multi-organizational mismatch не проверена: в стенде одна организация.
- Физические внешние устройства и нагрузочные сценарии не проверялись.
