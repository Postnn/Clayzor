# EquipmentForm — протокол EQ9

Дата: 24 ноября 2026 года. Работа выполнена в `main`, merge не выполнялся. Независимое ревью `EquipmentForm_8_Review.md` использовано как исходный список дефектов. R01/R04 не выбирались за владельца. Реальные Windows/SQL multiuser, IIS, физические АТС/UPS/ICMP/WOL, billing, SQL Server 2008 R2, полная DST/touch beta-матрица не выдаются за локальный PASS.

## 1. База и публикация

| Репозиторий | Исходный SHA EQ8 | Итоговый SHA / публикация |
|---|---|---|
| Clayzor | `bfc3ea06c423c7e9a1f0bb0acf1ef62e61dc3e6f` | `HEAD` коммита этого отчёта; `main`; pushed |
| Inventory | `0b15e687b083e125907571ee4114aa3cd94f895f` | `3e035399f3b594f8b23e5d25e45b503e65c54ef4`; `main`; pushed |
| Entities | `97ac84991b725fd638c2a68c14ae31e3e110051f` | `f181014111524a0a48a0e63d59663066e72ac124`; `main`; pushed |
| Controls | `6590b78ae8165fed4fd7225e136bd79ed3e8611d` | `d4186764d4f60a2813f32a968c97fd5c6585c03a`; `main`; pushed |
| DBSelect | `e97796ecf5fe05f18b41a52e257abc544f555b80` | не изменялся |
| Tests | `f381004dfa6d7fc3bde71135a74f341a277ea33f` | `9161d4561fb4b94cd559f5505645ceea0a921d47`; `main`; pushed |
| DALC | `4491634cb87272b24ac5b3e90e9fc43e1648963d` | не изменялся |

Новых бизнес-таблиц, процедур и trigger-off обходов нет. Тестовый бизнес-SQL не выполнялся; старые `CLAY_TEST_*` записи не изменялись. Второй агент выполнил календарный пакет и новые `Eq9Calendar*` тесты; первый агент Controls оставил production-наработки, интеграция и независимая проверка выполнены координатором.

## 2. EQ9-01…EQ9-13

| Блок | Production-изменение и локальное доказательство | Статус/ограничение |
|---|---|---|
| EQ9-01 | `EquipmentForm.SaveComputerDraftAsync` ожидает actual `SaveComputerAsync`. `EquipmentEditorContext` привязывает equipmentId/generation/disposal; pre-write guard в `SaveChildDraftAsync`. Deferred/faulted/success Computer callback и настоящий OK старого AccountDialog | local-executed |
| EQ9-02 | `RefreshMainAfterChildWriteAsync` делает base/draft/fresh merge по полям и отдельно для writeoff. Конфликт сохраняет исходный ChangedAtUtc и блокирует main Save | local-executed, `ChildRefresh_*` |
| EQ9-03 | `SQLQueries.cs`, DTO и builders: original ChangedAt/expected existence, atomic cardinality, Software EquipmentId guard, SIM current target, Computer/License expected absent | local-executed, `Eq9ChildConcurrencyTests`; реальные locks/triggers — beta |
| EQ9-04 | `EquipmentLocationSaveBuilder` и `EquipmentResponsibleSaveBuilder` атомарно проверяют location55, active responsible, open period и следующее расположение | local-executed SQL contract; целевые races — beta |
| EQ9-05 | Planner/retry проходят fresh identity/target/diff. Deleted sync требует diff, подтверждённый `UpdateWithPhonesAsync`; reassigned/forged original запрещён. Configure — owner/global43 | `Eq9PhoneSyncTrustTests`; после потери серверного deletion context replay остаётся заблокированным, R01 открыт |
| EQ9-06 | `PhoneSyncOutcome` различает Succeeded/Failed/Unknown. Versioned durable operation с attempt/scopes блокирует replay; Refresh сохраняет marker. Post-commit storage/read failure возвращает RefreshFailed | local-executed; если SQL/АТС эффект нельзя доказать read-only сверкой, Unknown сохраняется без угадывания |
| EQ9-07 | `ClayDraftService`/`clayDraftStore`: typed JSON, atomic user key, handles, ожидаемое release Web Lock, fail-closed Begin/health, migration recovery | `Eq9DraftProtocolTests`, browser probe 6/6 |
| EQ9-08 | Commit принимает только AcceptedScopes и revisions; legacy parent сохраняется, late N+1 и другие scopes остаются. Cancel очищает только свою область | Production Chrome: parent, accepted/late scopes, raw/typed, Unknown |
| EQ9-09 | `EquipmentEditorStateStore` сохраняет selected IDs, baseline/version и dynamic RAM/HD/SSD. Phone/UpsLink включены. Account/SIM raw secrets применяются после readiness | `Eq9EditorStateTests`, dialog tests; полная browser-матрица всех child — beta |
| EQ9-10 | Owner/document/phone/type/SIM filters получили expected error/stale handling. Account password и SIM secrets имеют readiness. `ClaySelect` получил совместимый read error/retry channel | `Eq9ClaySelectReadErrorTests`, owner late fault, dialog read failure tests |
| EQ9-11 | Responsible/SIM используют согласованный business calendar; Location/Responsible `now` при Apply. UPS unchanged tests сохраняют raw UTC с миллисекундами | `Eq9Calendar*` 10/10, offsets 0/±180, UPS .500 |
| EQ9-12 | `SELECT_ЛицоПерсональныхДанныхСоответствие` и `GetPersonalDataAdvisoryAsync` проверяют existing/change/Fill/restore без блокировки и очистки | `EquipmentSimDialogTests`: invalid ID и read failure; целевые SQL-данные — beta |
| EQ9-13 | Solution build, общий и профильный suite, production browser probes, docs, AGENTS и эта матрица | 0 errors; 1160/1160 общий; 527/527 профиль; browser 6/6 |

## 3. F01…F14 независимого ревью

| Finding | Закрытие | Доказательство |
|---|---|---|
| F01 | Actual Computer Save awaited | deferred/faulted/success callback tests |
| F02 | Настоящий base/draft/fresh merge без выдачи fresh token конфликту | `ChildRefresh_ConflictingMainChange_*`, independent merge |
| F03 | Original version/target и atomic cardinality дочерних записей | `Eq9ChildConcurrencyTests`, `EquipmentResponsibleSaveBuilder` |
| F04 | Единая trusted ATS boundary и owner-scope configure | `Eq9PhoneSyncTrustTests` |
| F05 | Durable Unknown, failed-only retry, post-commit outcomes | Trust tests, unknown lifecycle, browser Unknown round-trip |
| F06 | Typed wire, atomic key, handle lifecycle, fail-closed registration | `Eq9DraftProtocolTests`, production browser key/lifecycle probes |
| F07 | Scoped commit вместо удаления всей записи | Browser parent/accepted scopes/late revisions |
| F08 | Typed child state + raw + readiness + dynamic structure | `Eq9EditorStateTests`, Account/SIM readiness, `applyRaw` browser test |
| F09 | Expected read errors, late guards, secret readiness | Owner late fault, dialog and `Eq9ClaySelectReadErrorTests` |
| F10 | Согласованный business calendar и moving now | `Eq9Calendar*`, `GetNow` в Location/Responsible |
| F11 | Raw UTC нетронутых UPS tests | `UpsSave_UnchangedTimestamp_PreservesFractionalRawInstant` |
| F12 | ПДн advisory existing/change/Fill/restore | `EquipmentSimDialogTests` и SQL advisory query |
| F13 | Editor guard до Save, после await и на disposal | настоящий Apply старого AccountDialog, pre-write guard |
| F14 | Atomic server location55/responsible | SQL builders и `Eq9ChildConcurrencyTests` |

## 4. Обозначения доказательств

- **local-executed** — реализация и доступная локальная проверка выполнены.
- **source-reviewed** — поведение подтверждено по коду и существующим regression tests, отдельный новый runtime-сценарий не требовался.
- **beta** — внешняя/реальная среда либо предметная схема остаётся непроверенной.
- **owner-decision / dependency** — решение владельца или отдельный нереализованный общий функционал.
- **justified** — обоснованное отличие от Legacy, не дефект.

Сокращения evidence: `EF` — EquipmentFormTests; `CC` — Eq9ChildConcurrencyTests; `AT` — Eq9PhoneSyncTrustTests + Eq8PhoneSyncRetryTests; `DR` — Eq9DraftProtocolTests + production browser probe; `ST` — Eq9EditorStateTests; `CAL` — Eq9CalendarServiceTests/Eq9CalendarDialogTests; `SIM` — EquipmentSimDialogTests; `PH` — EquipmentPhonePersistenceTests; `SU` — общий suite 1160/1160.

## 5. Все 188 сценариев по одному

| ID | Поведение | Статус | Evidence / оставшаяся зависимость |
|---|---|---|---|
| M01 | Открытие существующего ID, not-found/read error/retry | local-executed | EF |
| M02 | Создание через отдельный EquipmentIns, без скрытого INSERT редактора | justified | SU |
| M03 | Основные 15 полей UPDATE и optimistic guard | local-executed | EF, `Equipment.cs` |
| M04 | Тип фильтрует модели, сортировка и recoverable source errors | local-executed | EF, ClaySelect read errors |
| M05 | Модель устанавливает тип; сервер сверяет пару | local-executed | EF, server model/type guard |
| M06 | Очистка типа/модели и Escape backup | source-reviewed | SU, ClaySelect contracts |
| M07 | SN обязателен по фактической модели/типу | local-executed | EF, server guards |
| M08 | Видимость сетевого имени и MAC по модели | source-reviewed | EF, NetworkDiagnostics |
| M09 | MAC whitelist и normalization, R04 не выбрана | local-executed | EF; политика R04 — owner-decision |
| M10 | Скрытые значения сохраняются, несовместимые секции read-only | source-reviewed | EF |
| M11 | Метаданные модели и открытие по подтверждённому URI | source-reviewed | EF |
| M12 | Owner/tenant, права и late/error guards | local-executed | EF `OwnerChanged_LateFaultFromA_*` |
| M13 | Документ поступления → поставщик без late overwrite | local-executed | EF, receipt error handling |
| M14 | Списание: дата/документ и фактический тип SIM | local-executed | EF; реальные triggers — beta |
| M15 | Акты списания текстом без выдуманного URI | justified | EF, 0/1/N acts |
| M16 | UTC/local со смещением на instant | local-executed | EF, CAL; реальные DST — beta |
| M17 | OK/Применить: actual write, refresh и close разделяются | local-executed | EF, deferred Computer callback |
| M18 | Два редактора не затирают друг друга | local-executed | EF base/draft/fresh conflict |
| M19 | Копирование через EquipmentIns?copyId без скрытого Save | source-reviewed | EF |
| M20 | Отмена/Refresh/NavigationLock и explicit Discard | local-executed | EF, DR |
| M21 | Child refresh сохраняет dirty и объединяет независимые поля | local-executed | EF `ChildRefresh_*` |
| M22 | Перезапуск сети/app update: durable draft без replay | local-executed | DR, Chrome reload/restart; реальные IIS faults — beta |
| M23 | Недоказанное поле должности не переносится | justified | Legacy contract review |
| T01 | Телефонные поля по типу/модели, persisted read-only | source-reviewed | EF |
| T02 | Камера и пул только IP-phone | source-reviewed | EF |
| T03 | Поиск телефона по контексту и исправленные exclusions | local-executed | PH, SQL exclusions |
| T04 | Исключение выбранных номеров и запрет дублей | local-executed | PH, builder duplicate guard |
| T05 | Аналоговое connection equipment и read errors | local-executed | EF AddPhone guards |
| T06 | Добавление номера в draft без немедленного DML | source-reviewed | EF |
| T07 | Удаление связи, не справочного номера | source-reviewed | PH, atomic diff |
| T08 | Срок/примечание телефона и restore по PhoneId | local-executed | EF, ST |
| T09 | Новый номер сохраняет ReturnDate/Notes в INSERT | source-reviewed | PH |
| T10 | Основной номер, связь, changed metadata | source-reviewed | PH `GetPhones_MapsAllColumns` |
| T11 | Условные колонки и readonly ссылки | source-reviewed | EF |
| T12 | Просроченный срок по клиентскому календарю | local-executed | EF, CAL |
| T13 | Предупреждение о нескольких номерах гостиницы | source-reviewed | EF, hotel count |
| T14 | Equipment+phone diff атомарен, deleted row обязан существовать | local-executed | PH, `DELETE_ОборудованиеТелефона` rowcount |
| T15 | ReturnDate/Notes без АТС | local-executed | PH metadata-only |
| T16 | ATS получатели только affected diff | local-executed | PH, AT |
| T17 | Xorcom приоритет над CCM | local-executed | PH mixed branch |
| T18 | CCM old/new connection recipients и fallback | local-executed | PH |
| T19 | Xorcom deletion flag | source-reviewed | PH; физический эффект — beta |
| T20 | Частичный ATS failure: failed-only retry, Unknown не повторяется | local-executed | AT, DR |
| T21 | Retry после revoke/changed target запрещён | local-executed | AT |
| T22 | SQL↔ATS архитектура | owner-decision | R01 не выбрана; durable outbox не выдуман |
| T23 | Явная настройка IP-phone без UPDATE | local-executed | AT owner/global scope |
| T24 | Config dirty/MAC/pool/template и role102 | source-reviewed | EF |
| T25 | HTTP config handler с authenticated provider | source-reviewed | SU, Kestrel pipeline EQ8 |
| T26 | Ленивый accounts UI и global102 | local-executed | EF, server role scopes |
| T27 | Список аккаунтов без секретов | source-reviewed | SU, separate password SELECT |
| T28 | Show/hide password отдельно от edit43 | source-reviewed | EF |
| T29 | Add account с 102+43 и required login | source-reviewed | SU |
| T30 | Edit account с исходным login и secret readiness | local-executed | `EquipmentAccountDialogTests`, ST |
| T31 | Delete account по составному ключу и свежим ролям | source-reviewed | SU |
| T32 | Child failure сохраняет данные редактора | local-executed | EF Unknown lifecycle, DR |
| T33 | Network actual type + global103 | local-executed | EF, server guard |
| T34 | Quick UPS link с actual owner/type | source-reviewed | SU |
| T35 | UPS details проверяет владельцев изменённых серверов | source-reviewed | SU |
| T36 | SIM/accounting/license scopes независимы от edit43 | source-reviewed | SU |
| T37 | Copy/open targets используют подтверждённые routes | source-reviewed | EF |
| T38 | Refresh с dirty/pending сохраняет Unknown marker | local-executed | EF Unknown Cancel/Refresh |
| T39 | Старые callbacks не пишут после смены ID/generation | local-executed | EF Account old editor |
| H01 | История расположений, путь, примечания, Changed | source-reviewed | EF; ссылки без подтверждённого URI — plain text |
| H02 | Новое расположение и local-now при Apply | local-executed | CAL, Location `GetNow` |
| H03 | Совпадение с текущим расположением при concurrent move | beta | UI guard есть; атомарная предметная проверка/триггер не подтверждены |
| H04 | Return location: nullable, от начала, ≤3 месяцев | local-executed | CAL, moving now |
| H05 | Edit location меняет только Return/Notes по исходной версии | local-executed | CC, exact .500 |
| H06 | Location55 требует responsible только при отсутствии активного | local-executed | CC, atomic SQL guard |
| H07 | Перенос с location55 не изымает responsible автоматически | justified | Legacy закомментированная ветвь не восстановлена |
| H08 | Location+responsible атомарны | local-executed | CC, transaction/rollback |
| H09 | Перенос телефонной станции переносит связанные SIM | beta | SQL сохранён; реальные linked SIM edge cases |
| H10 | Responsible history и closed periods | source-reviewed | EF |
| H11 | Выдача: employee/from обязательны, moving now | local-executed | CAL |
| H12 | Изъятие: to/next/from и исходный open period | local-executed | CC, CAL |
| H13 | Следующий responsible отличается от действующего | local-executed | EF, fresh current guard |
| H14 | Изъятие с location55 требует новое location≠55 | local-executed | CC, atomic SQL guard |
| H15 | Return следующего не раньше его local start | local-executed | CAL, offsets |
| H16 | Edit responsible не восстанавливает stale ToUtc и проверяет версию | local-executed | CC |
| H17 | Withdraw+location+next атомарны с ожидаемым close | local-executed | CC |
| H18 | Автоматическая смена договора SIM не переносится из закомментированного Legacy | justified | Legacy contract review |
| H19 | Accounting history/edit с read visibility | source-reviewed | EF |
| H20 | Accounting owner/dateFrom/inventory validation | source-reviewed | SU |
| H21 | Material rules и сохранение typed draft | local-executed | EF, ST |
| H22 | Owner change фильтрует документы без legacy copy/paste ошибки | justified | SU |
| H23 | Accounting INSERT и document/rights races | beta | Код есть; отрицательные реальные сценарии и triggers |
| H24 | Accounting UPDATE original version/factual ownership | source-reviewed | SU, exact atomic guards |
| H25 | Accounting DELETE только latest с guard | source-reviewed | SU |
| H26 | Accounting document scope per principal | beta | DS routing проверен; runtime visibility |
| H27 | Computer daily snapshot с expected absent/existing | local-executed | CC, `ExpectedExisting` |
| H28 | RAM/HD/SSD components, totals и dynamic restore | local-executed | ST, EF |
| H29 | RAM bank limit по фактической модели | local-executed | EF, server model context |
| H30 | Standard configuration dependencies/dates | source-reviewed | SU, EQ8 domain tests |
| H31 | Configuration display с именами и датами | source-reviewed | EF |
| H32 | Software history/link/uninstalled styling | source-reviewed | EF |
| H33 | Software add/edit без delete, с target/version guard | local-executed | CC |
| H34 | Software selection заполняет Installed из модели | source-reviewed | SU |
| H35 | Software required fields/date order | source-reviewed | SU |
| H36 | Inclusive UI uninstall/exclusive DB | source-reviewed | SU |
| H37 | Явное действие деинсталляции вместо лишнего modal | justified | EF |
| H38 | License single record с expected baseline | local-executed | CC |
| H39 | License type/resale/temp/counts, отрицательные значения | local-executed | SU, domain validation |
| H40 | License exclusive DateTo | source-reviewed | SU |
| H41 | License Description | source-reviewed | EF |
| H42 | UPS quick link и same-office suggestions | source-reviewed | SU; реальная схема locations — beta |
| H43 | UPS details add/remove servers, flags, tests | source-reviewed | SU |
| H44 | Замена сервера через delete+add в одном diff | justified | SU |
| H45 | UPS original/current diff, same-row guards и raw precision | local-executed | CC, EF UPS .500 |
| H46 | UPS source/affected-owner guards | source-reviewed | SU |
| H47 | UPS mandatory reads до редактора | source-reviewed | EF |
| H48 | UPS names: computers only, historical unknown сохранены | source-reviewed | SU |
| H49 | UPS warning/issues и удержание редактора при overflow | source-reviewed | SU |
| H50 | Shutdown flag применим только к компьютерам | source-reviewed | SU; физический эффект не заявляется |
| H51 | UPS lookup соответствует server contract | source-reviewed | SU |
| H52 | UPS ChangedBy/Changed audit | source-reviewed | EF |
| S01 | Показ SIM по типу, одна запись и readonly historical data | source-reviewed | EF |
| S02 | Add только без SIM; edit/delete существующей | source-reviewed | SU |
| S03 | Номер цифры, договор/активация и server validation | source-reviewed | SU |
| S04 | PIN/PUK отдельно, формат и readiness | local-executed | SIM, ST; deferred secret read |
| S05 | Секреты не попадают в общий список/typed JSON | local-executed | ST, SU separate SELECT |
| S06 | Существующая SIM UTC→local→UTC с секундами | local-executed | SIM; реальные browser DST — beta |
| S07 | «Сейчас» использует browser-local now | source-reviewed | SIM |
| S08 | Незавершённые договоры в поиске, historical resolve | source-reviewed | SU |
| S09 | ПДн picker: договор/оборудование/технический тариф/тип | source-reviewed | SU, CTE candidates |
| S10 | Advisory несовместимого ПДн без HTML и блокировки | local-executed | SIM invalid ID |
| S11 | Fill ПДн из ответственного с version guard и advisory | local-executed | SIM, late response guards |
| S12 | Смена договора без ложного сброса same-ID, readiness | local-executed | SIM, ContractEdit secrets |
| S13 | Поиск другого активного SIM того же номера | source-reviewed | SU |
| S14 | Подтверждение списания дубликата атомарно | source-reviewed | SU |
| S15 | ПДн дубликата наследуется только при пустом новом | source-reviewed | SU |
| S16 | Ошибка SIM save оставляет durable ввод | local-executed | EF, DR |
| S17 | SIM grid: активация/договор/ПДн/Changed | justified | EF; без подтверждённых URI — plain text |
| S18 | История/обязательные услуги и recoverable filters | local-executed | EF SIM filter error path |
| S19 | Каталожная услуга копирует название/стоимости | source-reviewed | SU |
| S20 | Свободное название без типа и старый custom editable | source-reviewed | SU |
| S21 | Фактические даты from≤to, не будущее по business Today | local-executed | CAL |
| S22 | Окончание услуги inclusive UI/exclusive DB | source-reviewed | SU |
| S23 | Симметричный overlap периодов | source-reviewed | SU |
| S24 | Плановые даты не редактируются, источник NULL не выдумывается | justified | Legacy/SQL review |
| S25 | Tariff highlight, mandatory/MustDisable/overdue warnings | source-reviewed | EF |
| S26 | BLOCK только действующий период по business Today | local-executed | CAL |
| S27 | Суммы дневной/месячной стоимости | source-reviewed | EF |
| S28 | Редактирование услуг без удаления истории | source-reviewed | EF |
| S29 | SIM detail только Билайн со свежим guard | source-reviewed | SU |
| S30 | Detail role 41/42/subordinates и Tariffication check | source-reviewed | SU; внешняя БД — beta |
| S31 | Один ответственный или единственный текущий | source-reviewed | SU |
| S32 | НДС/округление/period params/colors/title | source-reviewed | SU URI matrix |
| S33 | Открытие detail не сохраняет main draft | source-reviewed | EF |
| N01 | Поиск сетевых портов и domain prefix | source-reviewed | SU |
| N02 | Warning по числу портов/MAC | source-reviewed | EF NetworkDiagnostics |
| N03 | Switch/router suppression/hide Net+UPS | source-reviewed | EF |
| N04 | Имена `*` скрыты, но участвуют в count | source-reviewed | EF |
| N05 | Ссылка порта открывает Net через clayOpen | source-reviewed | EF |
| N06 | Attach/detach по computer type и global103 | local-executed | EF, server guard |
| N07 | Detach требует подтверждения | source-reviewed | EF |
| N08 | Save основной карточки перед attach/detach | source-reviewed | EF reload-null case |
| N09 | Write отделяется от последующего count read | local-executed | EF, RefreshFailed outcomes |
| N10 | Поздний сетевой ответ не меняет другую карточку | source-reviewed | EF version guards |
| N11 | История сетевых имён с local Changed | source-reviewed | EF; автор без URI — plain text |
| N12 | Ping indicator и polling | dependency | Не реализовано; отдельная общая задача |
| N13 | WakeOnLan/sp_WakeOnlan | dependency | Не реализовано; физический эффект не имитируется |
| N14 | Защита формы от закрытия + durable draft | local-executed | EF, DR |
| N15 | Повторное открытие child сохраняет родителя и typed state | local-executed | EF, ST, DR |
| N16 | Размер/позиция popup в общем localStorage, не ввод | source-reviewed | SU |
| N17 | SIM detail popup key и сохранение main draft | source-reviewed | EF; popup/other origin — beta |
| N18 | Readonly checkbox не меняет модель; фильтры read-only доступны | source-reviewed | SU |
| N19 | Mobile 320–480px, grids/dialogs/touch | beta | Адаптивная CSS есть; прежний 320/480 smoke, полная матрица не выполнена |
| A01 | Accounts/secret reads: global102 и trusted provider | local-executed | SU, role scopes |
| A02 | Add/Update/DeleteAccount: 102+43 и actual type | local-executed | SU, child guards |
| A03 | Update/UpdateWithPhones: fresh43, model/type, optimistic guard | local-executed | EF, PH |
| A04 | Location/Responsible fresh43 и original history guards | local-executed | CC |
| A05 | Software/Computer fresh43 и actual computer characteristic | local-executed | CC, SU child guards |
| A06 | SIM/SIM service/secrets: fresh42, actual type, business Today | local-executed | CAL, SU child guards |
| A07 | SIM detail fresh trusted identity и current responsible | source-reviewed | SU |
| A08 | Attach/DetachNetwork: global103 и actual type | local-executed | EF |
| A09 | Add/DeleteUpsLink: owner-or-global43, actual type | source-reviewed | SU; null owner не расширен в ANY |
| A10 | SaveUpsDetails: trusted identity + atomic owner guards | source-reviewed | SU |
| A11 | SaveAccounting: fresh owner roles + atomic guards | source-reviewed | SU |
| A12 | DeleteAccounting: trusted identity + latest guard | source-reviewed | SU |
| A13 | SaveLicense: owner/tenant44, actual type и original baseline | local-executed | CC, SU |
| A14 | GetPhoneConfig: fresh102 via correct HTTP/circuit provider | source-reviewed | SU Kestrel pipeline |
| A15 | Planner/Executor ATS: fresh identity/diff/target/operation origin | local-executed | AT |
| A16 | Прочие read helpers без blanket role43 | justified | SU, раздельные права чтения/записи |
| U01 | Location notes многострочные | source-reviewed | EF |
| U02 | Responsible notes многострочные | source-reviewed | EF |
| U03 | Computer notes многострочные | source-reviewed | EF |
| U04 | Software notes и фактический предел схемы | beta | Не обрезать существующие; подтвердить DB length |
| U05 | License Distribution и фактический предел схемы | beta | UI/model/params согласованы; подтвердить DB length |
| U06 | UPS Issues, auto-warnings и фактический предел схемы | beta | Подтвердить DB contract; truncation не утверждается |

## 6. Сохранённые улучшения и consumers

Сохранены atomic Equipment+phone diff, Xorcom/CCM priority/recipients, failed-only retry без UPDATE, separate secret reads, SIM duplicate writeoff, exact accounting/UPS guards, static ClayGrid sections, compact/draggable UI, typed ClaySelect backup/no-auto-select, Net/Grid/Date/Time behavior и per-grid drag isolation. R01/R04 не изменялись. Новых ограничений для Net и остальных потребителей Controls нет: соответствующие тесты включены в общий suite.

## 7. Команды и фактические результаты

```bash
dotnet build Clayzor.sln --no-restore --verbosity:minimal --consoleLoggerParameters:ErrorsOnly
```

Успешно: **0 ошибок, 33 предупреждения**.

```bash
dotnet test tests/Clayzor.Lib.Web.Controls.Tests/Clayzor.Lib.Web.Controls.Tests.csproj --no-build --filter 'FullyQualifiedName!~ClayTreeMutationAbortTests' --verbosity:minimal --consoleLoggerParameters:ErrorsOnly
```

**1160/1160 passed**, 0 skipped. Исключение только ранее документированное несвязанное зависание `ClayTreeMutationAbortTests`; новых исключений нет.

```bash
dotnet test tests/Clayzor.Lib.Web.Controls.Tests/Clayzor.Lib.Web.Controls.Tests.csproj --no-build --filter 'FullyQualifiedName~Eq9|FullyQualifiedName~Equipment|FullyQualifiedName~Ups|FullyQualifiedName~Employee|FullyQualifiedName~ClaySelect|FullyQualifiedName~NetForm|FullyQualifiedName~Draft|FullyQualifiedName~Reconnect' --verbosity:minimal --consoleLoggerParameters:ErrorsOnly
```

**527/527 passed**, 0 skipped.

Дополнительные проверки: календарь **10/10**; ранее падавшие старые fixtures после актуализации pre-read/DI **31/31**; EquipmentForm **94/94**; объединённый restore/calendar/SIM/UPS профиль **118/118**.

## 8. Production browser proof

Chrome CDP, настоящие IndexedDB/WebCrypto и production `clayDraftStore.js`, без бизнес-DML:

1. `Eq9DraftStoreProbe.html`: **6/6 PASS** — atomic first user key; accepted scopes/late revisions; durable Unknown/replay block; corrupt/unsupported setup с сохранением bytes; storage health recovery; awaited applyRaw/readiness.
2. Дополнительный lifecycle probe: `saveTyped` parent сохраняется после child commit; accepted scope очищается, late `N+1` остаётся; Unknown возвращается как versioned JSON; повторный Begin отклоняется. Обнаруженная ранняя гонка release Web Lock исправлена ожиданием фактического release, повторный `draftId` совпадает.
3. Equipment ID 8: ввод `CLAY_TEST_EQ9_LAST` только через `input` → full reload → значение восстановлено, Save не вызывался. Тестовый draft затем удалён через store/Отмена, временные CDP-файлы удалены.

Реальные IIS recycle/network loss/deployment, два Windows/SQL пользователя, все child/nested variants, DST/touch и независимая приёмка остаются вне локального proof.

## 9. Beta и открытые решения

- Две реальные Windows/SQL identities, circuits, global/local/ANY, substitutions/revoke/delegation.
- SQL Server 2008 R2, активные triggers, constraints, locks, concurrent writes, H03/H09/H23/H26 и отрицательные document/owner сценарии.
- Lost response после фактического SQL/ATS commit и предметная read-only сверка там, где существующие ключи не доказывают эффект. Unknown не превращается в предполагаемый success.
- Физические Xorcom/CCM/Meridian, UPS/ICMP/WOL, внешний billing и `URI_accounting_phone_form`.
- Ping/WOL пока нереализованы; rich contacts/collaboration — отдельные зависимости.
- R01 (SQL↔ATS) и R04 (MAC) — решения владельца.
- U04–U06: подтвердить фактические DB lengths; truncation не заявляется.
- Полная browser/DST/touch/320–480px, popup/opener и независимая приёмка.

## 10. Честный статус

Подтверждённые кодовые дефекты F01–F14 исправлены и покрыты локальными production-path проверками. Непринятый ввод сохраняется с контекстом, Unknown не разрешает replay, конфликт не скрывается новым token. Внешняя beta-матрица, отдельные зависимости и R01/R04 остаются открытыми и не маскируются общим PASS.
