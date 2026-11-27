# Оркестратор: ключевые требования редактирования, UI и совместная работа

Задача — реализовать FORM-KEY-01…09 и сохранить DR-01 во всех применимых существующих формах и в основе новых приложений. Сначала прочитай [обязательный контракт](../../docs/agents/editing-forms-key-requirements.md) и actual HEAD; source audit не означает, что UI уже исправлен. Ни новые правила, ни этот промт не разрешают ломать работающие сценарии, CAS/права/R01/R04, запускать production DML/ATS или переносить legacy jQuery/field-sync.

## Порядок и параллельные роли

| Роль | Задание и единоличное владение | Зависимости и передача |
|---|---|---|
| A | Cancel/Reset и DR26; draft API/store, editable revision, unresolved outcome, общий cancel-dialog и TerritorialEntityCardBase lifecycle | Публикует Reset/Close/Continue contract и field-baseline/revision interface; A ведёт оба draft и Cancel задания последовательно в одних файлах |
| U | Общие input/selector field-state, CSS/theme tokens, first/next focus и принятый пользовательский выбор | Согласует baseline с A и completion/ack с B; shell edit-scope передаёт H; не переписывает popup transport |
| B | Уже назначенный владелец popup/result bridge и Tree | Предоставляет acknowledged result/cancel/stale semantics для U, сохраняет свои прежние задачи |
| H | Form metadata, URI_wiki_help resolver/совместимость, общие help/theme commands и shell | Единственный интегратор ClayEntityForm/layout/toolbar: принимает U scope и P панель; общие Settings/Program через приложение-интегратор |
| P | Presence service/typed key/auth/lease, отдельная панель | Независим от бизнес-save/черновиков; получает scope lifecycle A, передаёт панель H и adapters владельцам форм |
| C | Equipment main, parent call-sites, Drafts partial, общая база child dialogs | Включает A/U/H/P, получает child patches; child агенты не правят parent параллельно |
| Владельцы форм | Непересекающиеся adapters конкретных страниц/редакторов, metadata и regression evidence | Используют опубликованные версии API; не создают local CSS/JS, второй draft/presence/theme механизм |
| Интегратор | Program/DI/settings mapping, общий каталог, AGENTS, обновление submodule pointers | Сериализует общие файлы, проверяет контракты и accepted baseline; не выбирает ours/theirs вслепую |

1. **Этап 0:** actual SHA/dirty-state, канонический контракт, список всех route/dialog/editor consumers, поля и реальные entity keys, ownership каждого общего файла. Требования в AGENTS обязательны до новой разработки. Согласовать минимальные API и модель состояний, не создавать монолитный этап «сначала переписать Controls».
2. **Этап 1, параллельно:** A реализует безопасную отмену поверх DR26; U — геометрию/семантический Required, field-state и focus/Enter; H — resolver и shell-команды; P — авторизованную presence модель/lease/панель. При четырёх слотах координатор запускает три независимых потока, четвёртую роль начинает после освобождения слота либо выполняет сам. A/DR26 не запускаются двумя конкурирующими агентами; B/U пересечения Select заранее делятся по файлам/commit.
3. **Этап 2, по готовности:** один сложный карточный consumer, один общий base consumer, один nested dialog и selector проверяют согласованные API; после этого независимые агенты одновременно подключают остальные формы. Подготовку adapters и metadata можно делать раньше. Если общий fix полностью покрывает форму, локальный patch не нужен — предъявить call path и применимые проверки.
4. **Этап 3:** пройти все legacy-соответствия из основного review и современные самостоятельные страницы; Home/Grid/Print, Modifications, ThemeStudio, Users/Medical участвуют в охвате help/theme/контролов, даже когда у них нет отдельного бизнес-fix. Не придумывать presence/Save для read-only/диагностики. На каждые FORM-KEY/DR-01 записать fulfilled/not applicable с причиной/remaining, а не одно «готово» на библиотеку.
5. **Этап 4:** согласованная интеграция, точечная совместная проверка риска, один handoff. Внешние SQL/IIS/deploy/браузеры/сотрудники/ATS остаются beta по решению пользователя; локально доступные проверки resolver/state/event/lifecycle выполняются сейчас и не выдаются за внешний стенд.

## Обязательная общая приёмка

- Cancel: clean close; Close/Reset/Continue и Escape; invalid/unblurred raw; fresh DB V2; ошибка чтения/storage; более новая N+1; Unknown/known outcome; create defaults; child Reset остаётся открытым и не меняет parent. Refresh не делает discard.
- UI: центрированный placeholder; required editable пустое — желтоватое, заполненное — обычный фон, очищенное — снова желтоватое, с placeholder/без; placeholder не задаёт Required; initial focus без кражи; Tab/Shift+Tab; Focus+Dirty+Invalid+Required одновременно; change→revert; autosave/restore/Apply N с N+1; readonly native input интерактивного picker не исключается ошибочно.
- Enter/Select: один переход после принятия, включая unchanged value; IME/textarea/key repeat; поиск без принятого выбора; Escape/blur/restore не продвигают; callback reject/stale не закрывает/не крадёт фокус; после await пользовательский фокус имеет приоритет.
- Help/theme: команды каждой формы/редактора при direct URL и modal, без дублирования confirm-toolbar; URI_wiki_help и каталог/legacy prefix; переключение сохраняет draft, baseline и операции; существующие темы не удаляются.
- Presence: два сотрудника/одна сущность, aliases/несколько приложений, composite key, разные сущности, несколько вкладок, readonly, identity/access/reconnect, TTL и поздний leave, новая запись без общей группы0, отсутствие поля/draft-sync; реальная topology заявлена честно.

## Handoff и правило завершения

Каждый агент сдаёт: owner; base commit; собственные файлы; shared API version; FORM-KEY→call path→evidence; список подключённых consumers; фактически выполненные команды/результаты; beta остаток; несовместимости/следующее действие. Требования считаются реализованными только после подключения применимых форм, а не по существованию общего компонента. Приёмку одной EquipmentForm не выдавать за готовность всех текущих и будущих приложений. Поддержка будущих приложений означает общий API, документацию/AGENTS и воспроизводимый способ подключения, а не неизвестные ещё приложения, якобы прошедшие тесты.

## Промты

- [PROMPT_FIX_Cancel_Reload.md](PROMPT_FIX_Cancel_Reload.md)
- [PROMPT_FIX_Shared_Editing_UX.md](PROMPT_FIX_Shared_Editing_UX.md)
- [PROMPT_FIX_Form_Help_Theme.md](PROMPT_FIX_Form_Help_Theme.md)
- [PROMPT_IMPLEMENT_Entity_Presence.md](PROMPT_IMPLEMENT_Entity_Presence.md)
