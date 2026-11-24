# EquipmentForm — 188 сценариев после EQ10

Документ обновлён по `EquipmentForm_Legacy_Clayzor_Matrix_EQ9.md` и `EQUIPMENTFORM_AUDIT_10.md`. Сохранены все 188 ID, смысл строк и отдельный **статус автора EQ9**; последний не является новым независимым runtime PASS.

## Фактические проверки EQ10

- `dotnet build Clayzor.sln` и `dotnet build tests/Clayzor.ThemeStudio/Clayzor.ThemeStudio.csproj` — 0 errors.
- Общий suite без известного зависающего `ClayTreeMutationAbortTests` — **1247/1247 passed**; новых Skip/исключений не добавлено.
- Coordinator — **13/13**; `Eq10Draft` — **8/8**; `Eq10Domain` — **33/33**.
- `Eq10EditorLifecycleTests` — **2/2**, `Eq10EditorBaselineCalendarTests` — **6/6**, `Eq10EditorReadinessTests` — **5/5**.
- EquipmentForm + phone/computer/SIM persistence — **124/124**.
- Два SIM fixtures и пять production browser draft scenarios — **7/7**.
- Browser EquipmentForm id8 + Computer edit/cancel без OK — **PASS**; corporate theme восстановлена, errors 0.

Обозначения evidence: `actual-executed` — указанный набор действительно выполнен; `source-reviewed` — подтверждение по production source; `inherited/not-rerun` — новый runtime PASS не заявляется; `beta`, `owner-decision`, `dependency`, `justified difference` сохраняют самостоятельный смысл и не маскируются общим зелёным suite.

## Локальное закрытие F01–F14

| Finding | EQ10 результат |
|---|---|
| F01 | Локально закрыт: immutable opening snapshot и post-await dispatch guards; coordinator 13/13, persistence 124/124. |
| F02 | Локально закрыт: editable main merge, derived IDs, durable restore и conflict resolution; coordinator 13/13. |
| F03 | Локально закрыт: production typed autosave/restore и структура dynamic rows; editor lifecycle 2/2, readiness 5/5. |
| F04 | Локально закрыт: immutable period original при fresh V2/missing/closed; baseline/calendar 6/6. |
| F05 | Локально закрыт: Unknown после Discard, read-only reconcile и mixed ledger; Eq10Draft 8/8, Eq10Domain 33/33, coordinator 13/13. |
| F06 | Локально закрыт: trusted ATS before/targets и deletion provenance; Eq10Domain 33/33. |
| F07 | Локально закрыт: trusted Configure origin/scope на retry; Eq10Domain 33/33. |
| F08 | Локально закрыт: atomic location/responsible и server date guards; Eq10Domain 33/33. |
| F09 | Локально закрыт: scoped secret readiness без раскрытия при pending/denied/failed; readiness 5/5, Eq10Draft 8/8. |
| F10 | Локально закрыт: SIM contract/source/readiness и восстановление другого ContractId; readiness 5/5, SIM fixtures 2/2. |
| F11 | Локально закрыт: business Today для validation/read filters/indicators; baseline/calendar 6/6. |
| F12 | Локально закрыт: phase-aware NotSent/Unknown/Committed/RefreshFailed; Eq10Domain 33/33, coordinator 13/13. |
| F13 | Локально закрыт: deferred advisory render и stale success/error guards; coordinator 13/13, SIM fixtures 2/2. |
| F14 | Локально закрыт: DOM registration isolation и cleanup после C# rejection; Eq10Draft 8/8. |

Локальное закрытие не является universal recovery PASS. Реальные Windows/SQL multiuser, IIS recycle/network/deploy faults, SQL Server 2008 R2 locks/triggers, физические ATS/UPS/ICMP/WOL, billing и полная DST/touch/mobile матрица остаются **beta**. **R01/R04 — owner decisions**; outbox не добавлен. После recreate service/server context retry без подтверждённого trusted intent остаётся **fail-closed**.

## M. Основная карточка и жизненный цикл

| ID | Сценарий полного сравнения | Статус автора EQ9 | EQ10 evidence |
|---|---|---|---|
| M01 | Открытие существующего ID; отсутствие/недоступность | local-executed | actual-executed (Eq10Draft 8/8): rejected registration cleanup и повторная загрузка; F14 закрыт локально. |
| M02 | Создание оборудования | justified | justified difference: обоснованное modern отличие сохранено; новый runtime PASS не требуется. |
| M03 | Все основные поля записи | local-executed | actual-executed (coordinator 13/13; EquipmentForm/persistence 124/124): editable main merge, derived IDs и restore; F02 закрыт локально. |
| M04 | Тип → фильтр моделей, сортировка типов | local-executed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| M05 | Модель → соответствующий тип | local-executed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| M06 | Очистить тип/модель; Escape поиска | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| M07 | Обязательность SN | local-executed | actual-executed (EquipmentForm/persistence 124/124): обязательные поля и серверные guards сохранены. |
| M08 | Видимость сетевого имени и MAC | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| M09 | Формат MAC | local-executed | source-reviewed: MAC whitelist/normalization сохранены; owner-decision: R04 не выбрана. |
| M10 | Скрытые значения при смене типа | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| M11 | Метаданные модели | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| M12 | Владелец и арендатор | local-executed | actual-executed (coordinator 13/13): opening snapshot и stale guards; F01 закрыт локально. |
| M13 | Документ поступления → поставщик | local-executed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| M14 | Списание: дата/время/документ | local-executed | actual-executed (coordinator 13/13; EquipmentForm/persistence 124/124): writeoff входит в editable merge с exact UTC semantics. |
| M15 | Акты списания с оборудованием | justified | justified difference: обоснованное modern отличие сохранено; новый runtime PASS не требуется. |
| M16 | Main UTC/local и секунды | local-executed | actual-executed (coordinator 13/13; Eq10EditorBaselineCalendar 6/6): UTC/local и business Today; F11 локально закрыт, real DST — beta. |
| M17 | Сохранение и закрытие / применить | local-executed | actual-executed (coordinator 13/13; Eq10Domain 33/33; persistence 124/124): opening, await и phase-aware outcomes; F01/F05/F12 локально закрыты. |
| M18 | Два редактора одной карточки | local-executed | actual-executed (coordinator 13/13): независимые fresh/local изменения и real conflicts объединяются без derived false conflict. |
| M19 | Копирование | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| M20 | Отмена / обновление / навигация | local-executed | actual-executed (Eq10Draft 8/8): Unknown переживает Discard/navigation/re-register; повторный Begin блокируется; F05 локально закрыт. |
| M21 | Child refresh и dirty main fields | local-executed | actual-executed (coordinator 13/13; persistence 124/124): LocationId/EmployeeId derived, собственный child write не создаёт ложный конфликт. |
| M22 | Перезапуск IIS / сеть / app update | local-executed | actual-executed (Eq10Draft 8/8; editor lifecycle 2/2, baseline/calendar 6/6, readiness 5/5): локальный DR-01 закрыт; IIS/network/deploy — beta; recreate без trusted intent fail-closed. |
| M23 | Поле должности из старого markup | justified | justified difference: обоснованное modern отличие сохранено; новый runtime PASS не требуется. |

## T. Телефоны, аккаунты, команды и права

| ID | Сценарий полного сравнения | Статус автора EQ9 | EQ10 evidence |
|---|---|---|---|
| T01 | Телефонные поля по характеристике типа и выбранной модели; form DisplayAndRequiredTypeFields | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| T02 | Камера и пул только IP-phone, markup/тип30 | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| T03 | Поиск телефона по location, employee, type, main phone, equipment; form685–711 / DSOEquipmentPhone | local-executed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| T04 | Исключить уже выбранные номера, запрет дублей; form1030–1033 | local-executed | source-reviewed + inherited/not-rerun: фильтры и запрет дублей сохранены; отдельный новый сценарий не заявляется. |
| T05 | Аналоговый номер получает допустимое connection equipment; form1035–1045 | local-executed | source-reviewed + inherited/not-rerun: фильтры и запрет дублей сохранены; отдельный новый сценарий не заявляется. |
| T06 | Добавление нового номера в draft | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| T07 | Удаление связи, не номера; form1018–1026 | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| T08 | Срок возврата и примечание; form2519–2537 | local-executed | actual-executed (Eq10EditorLifecycle 2/2): production typed autosave/restore подключён; F03 локально закрыт. |
| T09 | Новому номеру задать срок/примечание до первого save | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| T10 | Основной номер, связь с оборудованием, изменил/изменено | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| T11 | Условные колонки connection/date; form993–1005 | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| T12 | Просроченный срок возврата | local-executed | actual-executed (Eq10EditorBaselineCalendar 6/6): business Today обновляется; F11 локально закрыт, внешняя calendar/DST матрица — beta. |
| T13 | Гостиничный аппарат: предупреждение о >1 номере; form606–615/Equipment1604–1609 | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| T14 | Equipment и phone diff сохраняются атомарно | local-executed | actual-executed (Eq10Domain 33/33; persistence 124/124): atomic Equipment+phone diff и trusted actual-before; F06 локально закрыт. |
| T15 | Изменение только ReturnDate/Notes | local-executed | actual-executed (Eq10Domain 33/33; persistence 124/124): metadata-only изменения не запускают ATS. |
| T16 | Added/deleted phones, ATS получатели | local-executed | actual-executed (Eq10Domain 33/33): trusted before/targets, old/new CCM, Xorcom priority и deletion guards; F06 локально закрыт, физическая ATS — beta. |
| T17 | Xorcom имеет приоритет над CCM; form2841–2865 | local-executed | actual-executed (Eq10Domain 33/33): trusted before/targets, old/new CCM, Xorcom priority и deletion guards; F06 локально закрыт, физическая ATS — beta. |
| T18 | CCM вызывается по connection equipment, current equipment только fallback | local-executed | actual-executed (Eq10Domain 33/33): trusted before/targets, old/new CCM, Xorcom priority и deletion guards; F06 локально закрыт, физическая ATS — beta. |
| T19 | Удалённый Xorcom phone | source-reviewed | actual-executed (Eq10Domain 33/33): trusted before/targets, old/new CCM, Xorcom priority и deletion guards; F06 локально закрыт, физическая ATS — beta. |
| T20 | Ошибка части ATS операций | local-executed | actual-executed (Eq10Domain 33/33; coordinator 13/13; Eq10Draft 8/8): mixed durable ledger, Unknown lifetime и preflight phases; F05/F12 локально закрыты, внешние сбои — beta. |
| T21 | Повтор pending после отзыва роли/смены target | local-executed | actual-executed (Eq10Domain 33/33): trusted origin/owner/type на Configure retry; F07 локально закрыт. |
| T22 | SQL↔ATS transaction policy | owner-decision | owner-decision: R01 не выбрана; SQL↔ATS outbox и новая БД-схема не добавлялись. |
| T23 | Настроить сохранённый IP-phone по явному действию | local-executed | actual-executed (Eq10Domain 33/33): initial Configure и retry сохраняют owner/global43, origin и IP-phone scope. |
| T24 | Посмотреть config при dirty/MAC/pool/template conditions | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| T25 | Прямой HTTP config handler | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| T26 | Скрытый по умолчанию раздел аккаунтов только MAC equipment/global102 | local-executed | actual-executed (Eq10EditorReadiness 5/5; Eq10Draft 8/8): recovered secret не раскрывается до fresh allowed readiness; F09 локально закрыт. |
| T27 | Общий список аккаунтов без раскрытия секретов | source-reviewed | source-reviewed: separate secret reads и отсутствие секретов в списках/typed JSON сохранены. |
| T28 | Отдельное показать/скрыть пароль | source-reviewed | actual-executed (Eq10EditorReadiness 5/5): denied/pending readiness сохраняет protected draft без DOM/model secret. |
| T29 | Добавить account/login/password/limited/notes | source-reviewed | actual-executed (Eq10EditorReadiness 5/5; persistence 124/124): write guards сохранены, secret recovery не расширяет права. |
| T30 | Обновить account, исходный login неизменяем | local-executed | actual-executed (Eq10EditorLifecycle 2/2; Eq10EditorReadiness 5/5): typed lifecycle и secret readiness; F03/F09 локально закрыты. |
| T31 | Удалить account с подтверждением | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| T32 | Save child editor failure оставляет данные доступными | local-executed | actual-executed (Eq10EditorLifecycle 2/2; Eq10Draft 8/8; persistence 124/124): ввод и unresolved outcome сохраняются; F03/F05/F09 локально закрыты. |
| T33 | Операция сети: computer + global103 | local-executed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| T34 | Быстрое UPS подключение: IT/non-SIM + role43 owner/global | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| T35 | Детальный UPS editor: owner scopes каждого изменённого server | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| T36 | SIM/accounting/license роли независимы от общего edit43 | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| T37 | Copy/open target/model/phone/person | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| T38 | Cancel/navigation/refresh с dirty или pending | local-executed | actual-executed (Eq10Draft 8/8): editable discard отделён от running/unknown attempt; F05 локально закрыт. |
| T39 | Запрет смены страницы во время save и устаревших callbacks | local-executed | actual-executed (coordinator 13/13): opening A→B и post-await dispatch блокируются; F01 локально закрыт. |

## H. Истории и дочерние редакторы

| ID | Сценарий полного сравнения | Статус автора EQ9 | EQ10 evidence |
|---|---|---|---|
| H01 | Расположение: история, путь, примечания, изменивший, время | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| H02 | Новое расположение: location+дата+время обязательны | local-executed | actual-executed (coordinator 13/13; Eq10Domain 33/33): immutable opening context и серверные date guards; F01/F08 локально закрыты. |
| H03 | Новое расположение не должно совпадать с текущим | beta | actual-executed (Eq10Domain 33/33): локальные atomic guards усилены; beta: реальные concurrent same-location, SQL locks/triggers не проверялись. |
| H04 | Возврат location: nullable, не раньше начала, ≤3months | local-executed | actual-executed (Eq10Domain 33/33): moving now и обязательные server date boundaries; F08 локально закрыт. |
| H05 | Edit location не меняет place/from, только возврат/notes | local-executed | actual-executed (Eq10EditorBaselineCalendar 6/6; Eq10Domain 33/33): immutable original V1 при fresh V2/missing/closed; F04 локально закрыт. |
| H06 | Перенос на location55 требует ответственного только при отсутствии действующего | local-executed | actual-executed (Eq10Domain 33/33): expected active state и location+responsible атомарны; F08 локально закрыт, реальные locks/triggers — beta. |
| H07 | Перенос с location55 не должен автоматически изымать оборудование | justified | justified difference: обоснованное modern отличие сохранено; новый runtime PASS не требуется. |
| H08 | Расположение + новый responsible атомарны | local-executed | actual-executed (Eq10Domain 33/33): expected active state и location+responsible атомарны; F08 локально закрыт, реальные locks/triggers — beta. |
| H09 | Перенос телефонной станции переносит связанные SIM locations | beta | beta: реальные linked SIM effects не проверялись; локальный SQL contract сохранён. |
| H10 | Responsible history включая закрытые периоды и возвраты | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| H11 | Выдача: сотрудник, from date/time обязательны | local-executed | actual-executed (Eq10Domain 33/33): обязательные поля и актуальный server now проверяются на write boundary. |
| H12 | Изъятие: to date/time, опциональный следующий responsible с from | local-executed | actual-executed (Eq10EditorBaselineCalendar 6/6; Eq10Domain 33/33): original baseline и обязательная дата To; F04/F08 локально закрыты. |
| H13 | Следующий ответственный не равен действующему | local-executed | actual-executed (Eq10Domain 33/33): fresh next-employee и composite guards сохранены. |
| H14 | Изъятие с «На руках» требует другого location≠55 | local-executed | actual-executed (Eq10Domain 33/33): atomic actual55 требует новое location≠55. |
| H15 | Возврат следующему responsible не раньше ЕГО выдачи | local-executed | actual-executed (Eq10EditorBaselineCalendar 6/6): local start day и offsets сохранены; F11 локально закрыт, real DST — beta. |
| H16 | Edit responsible меняет только возврат/notes | local-executed | actual-executed (Eq10EditorBaselineCalendar 6/6; Eq10Domain 33/33): immutable original, stale ToUtc и atomic close/next guards; F04/F08 локально закрыты. |
| H17 | Изъятие+location+next atomic | local-executed | actual-executed (Eq10EditorBaselineCalendar 6/6; Eq10Domain 33/33): immutable original, stale ToUtc и atomic close/next guards; F04/F08 локально закрыты. |
| H18 | Автоматическая смена договора SIM при responsible/location | justified | justified difference: обоснованное modern отличие сохранено; новый runtime PASS не требуется. |
| H19 | Бухгалтерская история и edit любой строки | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| H20 | Accounting owner/dateFrom/инвномер ОС обязательны | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| H21 | Материал скрывает/очищает tenant и инвномер | local-executed | actual-executed (Eq10EditorLifecycle 2/2): Value baseline и production autosave/restore; F03 локально закрыт. |
| H22 | Owner change очищает документы, фильтрует по owner | justified | justified difference: обоснованное modern отличие сохранено; новый runtime PASS не требуется. |
| H23 | Accounting INSERT | beta | beta: реальные negative Accounting INSERT и triggers не проверялись; код не выдаётся за внешний PASS. |
| H24 | Accounting UPDATE original version+factual ownership | source-reviewed | source-reviewed: Accounting original/version/owner/latest guards сохранены; Location-specific F04 сюда не переносится. |
| H25 | Accounting DELETE только подтверждённой latest | source-reviewed | source-reviewed: Accounting original/version/owner/latest guards сохранены; Location-specific F04 сюда не переносится. |
| H26 | Accounting documents scope | beta | beta: runtime visibility документов не проверялась; DS routing сохранён. |
| H27 | Computer: daily snapshot, latest copied, history | local-executed | actual-executed (coordinator 13/13; persistence 124/124; Eq10EditorLifecycle 2/2): opening snapshot, expected existence и typed lifecycle; F01/F03 локально закрыты. |
| H28 | RAM/HD/SSD variable components, totals, trim trailing zeros | local-executed | actual-executed (Eq10EditorLifecycle 2/2): arrays, removals и stable typed/raw restore; F03 локально закрыт. |
| H29 | RAM bank limit | local-executed | actual-executed (coordinator 13/13; persistence 124/124): actual-model RAM guard и запрет смешения A/B; F01 локально закрыт. |
| H30 | Standard config dependencies/dates | source-reviewed | actual-executed (Eq10EditorBaselineCalendar 6/6): Computer.Today актуализируется перед validation/write; F11 локально закрыт. |
| H31 | Configuration display includes names and dates | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| H32 | Software history/link to software equipment, uninstalled styling | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| H33 | Software add/edit, no delete | local-executed | actual-executed (Eq10EditorLifecycle 2/2; persistence 124/124): EquipmentId/version guard и typed lifecycle сохранены. |
| H34 | Software selection fills Installed from model | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| H35 | Software required fields/date order | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| H36 | Inclusive UI uninstall date/exclusive storage | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| H37 | Uninstall confirmation | justified | justified difference: обоснованное modern отличие сохранено; новый runtime PASS не требуется. |
| H38 | License single record add-ifabsent/edit | local-executed | actual-executed (Eq10EditorLifecycle 2/2; persistence 124/124): expected existence/version и durable edit lifecycle; F03 локально закрыт. |
| H39 | License type/resale/licensee/temp dates/counts | local-executed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| H40 | License exclusive DateTo | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| H41 | License type explanation/description | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| H42 | UPS quick link with same-office suggestions | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| H43 | UPS details: add/remove servers, flag, add/edit tests | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| H44 | UPS edit server identity | justified | justified difference: обоснованное modern отличие сохранено; новый runtime PASS не требуется. |
| H45 | UPS transaction diff | local-executed | actual-executed (EquipmentForm/persistence 124/124): no-op UPS raw instant и .500 сохранены. |
| H46 | UPS source/affected-owner access | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| H47 | UPS required read before editing | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| H48 | UPS test server names: computers only, historical unknown preserved | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| H49 | UPS unchecked notifications warning/issues length | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| H50 | UPS «выключение настроено» применимо к компьютерам | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| H51 | UPS server lookup only admissible devices | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| H52 | UPS audit metadata | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |

## S. SIM и услуги

| ID | Сценарий полного сравнения | Статус автора EQ9 | EQ10 evidence |
|---|---|---|---|
| S01 | Показ SIM по признаку типа, одна запись | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| S02 | Add только если нет SIM; edit/delete если есть | source-reviewed | actual-executed (coordinator 13/13): SIM opening snapshot не смешивает A/B; server add/edit/delete guards сохранены. |
| S03 | Номер только цифры, до 50; договор и активация обязательны | source-reviewed | actual-executed (EquipmentForm/persistence 124/124): обязательные SIM fields и server validation сохранены. |
| S04 | PIN4, PIN2 4–8, PUK/PUK2 8 цифр, пустые допустимы | local-executed | actual-executed (Eq10EditorReadiness 5/5): typed/pending secret restore и readiness; F03/F10 локально закрыты. |
| S05 | Секреты не должны попасть в общий список | local-executed | actual-executed (Eq10EditorReadiness 5/5; Eq10Draft 8/8): секреты отсутствуют в общем списке/readiness и раскрываются только scoped после allowed read. |
| S06 | Существующая SIM UTC→local→UTC со временем и секундами | local-executed | actual-executed (EquipmentForm/persistence 124/124): UTC/local и секунды сохранены; beta: полная DST/browser-zone матрица. |
| S07 | Кнопка активации «Сейчас» | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| S08 | Незавершённые мобильные договоры в поиске | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| S09 | ПДн учитывает договор, оборудование, технический тариф, тип заказчика | source-reviewed | actual-executed (coordinator 13/13; SIM fixtures 2/2): production Select/source, DB errors и advisory semantics проверены локально. |
| S10 | Предупреждения по несовместимому ПДн | local-executed | actual-executed (SIM fixtures 2/2): deferred initial advisory отрисовывается, stale errors не применяются; F13 локально закрыт. |
| S11 | Заполнить ПДн из ответственного | local-executed | actual-executed (coordinator 13/13): selection revision действует и для success, и для error; F13 локально закрыт. |
| S12 | Смена договора обновляет область ПДн | local-executed | actual-executed (Eq10EditorReadiness 5/5; SIM fixtures 2/2): restored different ContractId не очищает ПДн; F10 локально закрыт. |
| S13 | Найти другой активный SIM того же номера | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| S14 | Подтверждение списания дубликата на активацию новой SIM | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| S15 | Наследовать ПДн старой SIM, если новое пусто | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| S16 | Ошибка SIM save оставляет ввод | local-executed | actual-executed (Eq10EditorLifecycle 2/2; Eq10Draft 8/8; persistence 124/124): ввод и Unknown сохраняются; F03/F05 локально закрыты. |
| S17 | SIM grid активация/договор/ПДн/Changed | justified | justified difference: обоснованное modern отличие сохранено; новый runtime PASS не требуется. |
| S18 | История и обязательные услуги | local-executed | actual-executed (coordinator 13/13; SIM fixtures 2/2; Eq10EditorBaselineCalendar 6/6): stale filters и единый business day; F11/F13 локально закрыты. |
| S19 | Каталожная услуга копирует название/стоимости | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| S20 | Свободное название без типа | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| S21 | Фактическая дата from≤to, не будущее | local-executed | actual-executed (Eq10EditorBaselineCalendar 6/6; persistence 124/124): актуальный business Today перед validation/write; F11 локально закрыт. |
| S22 | Окончание услуги включительно в форме, эксклюзивно в БД | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| S23 | Предотвращение перекрытия периодов одинаковой услуги | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| S24 | «Плановые даты» в legacy форме | justified | justified difference: обоснованное modern отличие сохранено; новый runtime PASS не требуется. |
| S25 | Tariff highlight, mandatory tooltip, MustDisable, overdue plan | source-reviewed | actual-executed (Eq10EditorBaselineCalendar 6/6): indicators, BLOCK и read filters используют согласованный business day; F11 локально закрыт. |
| S26 | BLOCK только действующий период | local-executed | actual-executed (Eq10EditorBaselineCalendar 6/6): indicators, BLOCK и read filters используют согласованный business day; F11 локально закрыт. |
| S27 | Суммы дневной/месячной стоимости | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| S28 | Услуги можно изменять, удаление истории не предлагается | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| S29 | Detail только Билайн | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| S30 | Доступ к detail через 41/42 или подчинённых | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| S31 | Один ответственный или единственный текущий из истории | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| S32 | НДС/округление/period params/colors/title | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| S33 | Открытие detail не сохраняет главный черновик | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |

## N. Сеть и общие UI сценарии

| ID | Сценарий полного сравнения | Статус автора EQ9 | EQ10 evidence |
|---|---|---|---|
| N01 | Поиск сетевых портов по имени, убирается domain prefix | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| N02 | Число портов больше Model.Net или 2 порта при пропущенном MAC | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| N03 | Для switch/router <3 подавить warning, >=3 скрыть Net/UPS | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| N04 | Сетевые имена с `*` не показываются, но участвуют в count | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| N05 | Ссылка порта открывает Net | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| N06 | Attach/detach в Операциях по computer type и support role | local-executed | actual-executed (EquipmentForm/persistence 124/124): computer/support guards и принятая необходимость main save сохранены. |
| N07 | Detach требует подтверждения | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| N08 | Save текущей карточки перед attach/detach | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| N09 | После write показать реальное новое состояние | local-executed | actual-executed (Eq10Domain 33/33; coordinator 13/13; persistence 124/124): Committed/RefreshFailed и preflight NotSent различаются; F12 локально закрыт. |
| N10 | Поздний сетевой ответ не меняет другую карточку | source-reviewed | source-reviewed: current-context guards сохранены; отдельный новый network runtime-сценарий не заявляется. |
| N11 | История сетевых имён, local Changed и автор | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| N12 | Ping каждые5с и статусы online/offline/warning | dependency | dependency: Ping/ICMP не реализован в этой задаче; физический эффект и polling не имитируются. |
| N13 | WOL после подтверждения → sp_WakeOnlan | dependency | dependency: WOL/sp_WakeOnlan не реализован в этой задаче; физический эффект не имитируется. |
| N14 | Legacy onbeforeunload при изменённой форме | local-executed | actual-executed (Eq10Draft 8/8): navigation/discard не забывает Unknown; F05 локально закрыт. |
| N15 | Повторное открытие дочерних форм сохраняет родителя | local-executed | actual-executed (Eq10Draft 8/8; editor lifecycle 2/2, baseline/calendar 6/6, readiness 5/5): parent/child roots, baseline и readiness; F03/F04/F09/F10/F14 локально закрыты. |
| N16 | Сохранение размера/позиции popup | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| N17 | Открытие SIM detail через устойчивый popup key | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| N18 | Readonly checkbox не должен менять модель | source-reviewed | inherited/not-rerun: отдельный новый runtime-сценарий EQ10 не выполнялся; новый PASS не заявляется. |
| N19 | Мобильные 320–480px | beta | beta: полная 320–480px/touch/mobile матрица не выполнялась; адаптивная CSS сохранена. |

## A. Authoritative entrypoints

| ID | Сценарий полного сравнения | Статус автора EQ9 | EQ10 evidence |
|---|---|---|---|
| A01 | GetFormDataAsync IncludeAccounts; GetAccountsAsync; GetAccountPasswordAsync | local-executed | actual-executed (Eq10EditorReadiness 5/5; Eq10Draft 8/8): fresh global102 gate предшествует recovered secret; F09 локально закрыт. |
| A02 | Add/Update/DeleteAccountAsync | local-executed | actual-executed (Eq10EditorReadiness 5/5; persistence 124/124): global102+ANY43 write guards сохранены, recovery не расширяет права. |
| A03 | UpdateAsync; UpdateWithPhonesAsync | local-executed | actual-executed (Eq10Domain 33/33; persistence 124/124): fresh identity/model/optimistic guard и phase-aware preflight; F12 локально закрыт. |
| A04 | Add/UpdateLocation; Issue/Update/WithdrawResponsible | local-executed | actual-executed (Eq10Domain 33/33; Eq10EditorBaselineCalendar 6/6): immutable baseline, atomic composite guards и обязательные даты; F04/F08 локально закрыты. |
| A05 | SaveSoftwareInstallationAsync; SaveComputerAsync | local-executed | actual-executed (coordinator 13/13; Eq10Domain 33/33; persistence 124/124): stale opening/dispatch blocked, phases различаются; F01/F12 локально закрыты. |
| A06 | SaveSimAsync; DeleteSimAsync; SaveSimServiceAsync; GetSimSecretsAsync | local-executed | actual-executed (Eq10EditorReadiness 5/5; Eq10EditorBaselineCalendar 6/6; persistence 124/124): secret readiness, SIM restore и business Today; F10/F11 локально закрыты. |
| A07 | GetSimDetailUrlAsync | source-reviewed | source-reviewed: контракт сохранён; отдельный новый runtime-сценарий EQ10 не заявляется. |
| A08 | Attach/DetachNetworkAsync | local-executed | source-reviewed: global103/actual computer guard сохранён; owner-scoped substitution не добавлялась. |
| A09 | Add/DeleteUpsLinkAsync | source-reviewed | source-reviewed: UPS owner/affected-owner atomic guards и null-owner semantics сохранены. |
| A10 | SaveUpsDetailsAsync | source-reviewed | source-reviewed: UPS owner/affected-owner atomic guards и null-owner semantics сохранены. |
| A11 | SaveAccountingAsync | source-reviewed | source-reviewed: Accounting trusted identity, owner scopes и latest guards сохранены. |
| A12 | DeleteAccountingAsync | source-reviewed | source-reviewed: Accounting trusted identity, owner scopes и latest guards сохранены. |
| A13 | SaveLicenseAsync | local-executed | actual-executed (EquipmentForm/persistence 124/124): owner/tenant44 и expected existence/version сохранены. |
| A14 | GetPhoneConfigAsync | source-reviewed | source-reviewed: HTTP/circuit provider различены; beta: реальные Windows/delegation runtime. |
| A15 | SynchronizePhoneOperationsAsync/ExecutePhoneSyncOperationsAsync | local-executed | actual-executed (Eq10Domain 33/33; coordinator 13/13): trusted intent/before/origin, mixed ledger и phase outcomes; F05/F06/F07/F12 локально закрыты. |
| A16 | Прочие публичные read helpers | justified | justified difference + source-reviewed: blanket43 для чтения не добавлен; secret readiness и registration имеют отдельные контракты. |

## U. Текстовые поля и ограничения

| ID | Сценарий полного сравнения | Статус автора EQ9 | EQ10 evidence |
|---|---|---|---|
| U01 | Location notes | source-reviewed | actual-executed (Eq10EditorLifecycle 2/2; Eq10EditorBaselineCalendar 6/6): многострочный ввод, immutable baseline и lifecycle сохранены. |
| U02 | Responsible notes | source-reviewed | actual-executed (Eq10EditorLifecycle 2/2; Eq10EditorBaselineCalendar 6/6): многострочный ввод, immutable baseline и lifecycle сохранены. |
| U03 | Computer notes | source-reviewed | actual-executed (Eq10EditorLifecycle 2/2; browser EquipmentForm id8 Computer edit/cancel без OK): Computer notes/draft сохранены, форма восстановлена, errors 0. |
| U04 | Software notes | beta | beta: фактическая DB length и negative truncation cases не подтверждены; существующие значения не обрезаются. |
| U05 | License Distribution | beta | beta: фактическая DB length не подтверждена; UI/model/params согласованы, truncation не утверждается. |
| U06 | UPS Issues | beta | beta: DB contract не подтверждён; truncation не утверждается. |


## Граница итогового результата

Все 188 строк сохранены индивидуально; `actual-executed` относится только к указанному набору и не распространяется автоматически на inherited/source-reviewed строки. Общий suite не заменяет внешнюю beta и не превращает зависимости или owner decisions в реализованные функции.
