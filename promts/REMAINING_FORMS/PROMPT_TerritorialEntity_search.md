# Промт: реализовать TerritorialEntity_search — дерево территорий и телефонные коды

## Задача и scope

Реализуй `Inventory/TerritorialEntity/TerritorialEntity_search.aspx` вместе с необходимой функциональностью `TerritoryData.ashx`: дерево территорий, поиск/фильтр, восстановление состояния, выбор значения, контекстные действия и правую область территорий/телефонных кодов. Отдельный технический .ashx-файл не является новой формой, но его поведение входит в этот промт.

Не ограничивайся примером ClayTreeView или плоским списком. На срезе нового Inventory `b3da274962c3c0811f71fbfe196f188ed3b5f659` есть `TreeTest` и общий ClayTreeView, но нет этого прикладного экрана. Тестовая страница не является переносом бизнес-сценария.

Обязательны [01_COMMON_RULES.md](01_COMMON_RULES.md) и инструкции `Controls/Components/Tree/AGENTS.md`. Карточки территории и телефонного кода реализуются отдельными промтами: [TerritorialEntity_form](PROMPT_TerritorialEntity_form.md), [PhoneCodeEntity_form](PROMPT_PhoneCodeEntity_form.md).

## 1. Исходники, которые определяют поведение

- [TerritorialEntity_search.aspx.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Inventory/TerritorialEntity/TerritorialEntity_search.aspx.cs): `Page_PreInit`, `LoadTreeViewData`, `ChangeViewData`, контекстные команды.
- [TerritorialEntity_search.aspx](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Inventory/TerritorialEntity/TerritorialEntity_search.aspx), [Kesco.Territories.js](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Inventory/TerritorialEntity/Kesco.Territories.js).
- [TerritoryData.ashx.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Inventory/TerritorialEntity/TerritoryData.ashx.cs): выбор SQL-ветки и возврат ID/text.
- [TreeView.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Controls.V4/TreeView/TreeView.cs), [TreeViewDataHandler.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Controls.V4/TreeView/TreeViewDataHandler.cs), [TreeViewSettings.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Controls.V4/TreeView/TreeViewSettings.cs), [TreeViewContent.htm](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Controls.V4/TreeView/TreeViewContent.htm): унаследованные multi-return, фильтр, права таблицы, DnD, state.
- [Legacy SQLQueries.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Entities/SQLQueries.cs): `SELECT_ТерриторииДанныеДляДерева`, `…_State`, `…_Фильтр`; общие SQL перемещения дерева и `SELECT_ПраваНаТаблицу`.
- [Territory.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Entities/Territories/Territory.cs), [Config.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Settings/Config.cs), [ресурсы Areas ru](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Localization/Corporate/Areas/ResContainer.ru.resx).
- [Общие ресурсы ru](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Localization/ResContainer.ru.resx): `Cfi_lblSave="Сохранить"`, `Cfi_lblCancel="Отмена"` — подписи унаследованных команд множественного выбора.
- [Текущие ClayTreeOptions.cs](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Tree/ClayTreeOptions.cs), [Tree/AGENTS.md](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Tree/AGENTS.md), [ClayTreeDynamicSettings.cs](https://github.com/Postnn/Clayzor.Lib.Web.Controls/blob/ea980be926ae975678314029ba5fc8aa68c675ba/Components/Tree/ClayTreeDynamicSettings.cs), [TreeTest.razor](https://github.com/Postnn/Kesco.App.Web.Inventory/blob/b3da274962c3c0811f71fbfe196f188ed3b5f659/Components/Pages/TreeTest.razor).
- Общий callback — источники из [PROMPT_Finder.md](_DONE/PROMPT_Finder.md). Не считай текущий bridge исправным без этапа G0 оркестратора.

## 2. Маршруты, заголовок и режимы открытия

Предоставь `/TerritorialEntity/TerritorialEntity_search` и `/TerritorialEntity/TerritorialEntity_search.aspx`. Заголовок страницы **«Территории и телефонные коды»**, синтетический корень дерева **«Территории»**. Legacy показывает InventoryMainMenu и двухчастную рабочую область.

| Вход/режим | Установленное назначение |
|---|---|
| Без `return` | Просмотр/навигация по дереву и вложенным спискам |
| `return=1` | У узлов появляется «Выбрать значение», возвращает числовой ID территории и текст |
| `return=2` | Множественные checkbox, команда возврата массива выбранных IDs |
| `selectedids` | Первоначально выбранные узлы, разделитель legacy — запятая |
| `rootids` → `root` → `parent` | Приоритет входных ограничений корней именно в этом порядке; работоспособность специализированных partial-веток не следует из наличия этих параметров, см. §4 |
| `clid` | Контекст пользовательских параметров |
| `requestId/returnOrigin` | Новый защищённый callback |
| `callbackKey/control/callbackUrl/mvc` | Legacy callback compatibility, если реальные callers сохраняются |

Параметры самого handler (`nodeid`, `stateLoad`, `loadId`, `searchText`, `searchParam`, `returntype`, `returncondition`, `orderByField`, `orderByDirection`, `isDefaultFilter`, `idpage`, `ctrlid`, `searchShowTop`, `reloadParentNode`) — транспорт старого TreeView. Переноси их пользовательский смысл, а не весь протокол WebForms. Если старый endpoint остаётся нужен внешним callers, реализуй тонкий проверенный адаптер к общей логике; не создавай альтернативный CRUD/SQL-слой.

Синтетический root=0 не является строкой `Территории`. Его checkbox в legacy скрыт. Не возвращай его как настоящую территорию и не отправляй на edit/delete.

В режиме `return=2` нижняя панель унаследованного TreeView содержит именно **«Сохранить»** и **«Отмена»**. Первая получает отмеченные IDs через `v4_treeViewGetCheckedNodesIds` и вызывает `v4_returnValueArray(ids)`: это подтверждение выбора массива, а не запись территорий в БД. Вторая посылает `DeselectAllTreeView`; обработчик вызывает `v4_treeViewDeselectAllNodes` и затем `V4DropWindow`. В новой форме «Отмена» очищает локальное выделение и закрывает выбор без применения результата родителю. Не заменяй её действием «очистить и остаться» и не возвращай пустой массив как отмену: подтверждённый пустой выбор и отмена — разные исходы. Реальные кнопки формируются только при включённом checkbox-режиме; не добавляй их в обычный просмотр.

**Пустое подтверждение установлено для MVC4-пути, а не оставлено на усмотрение реализации.** `TreeView.cs` безусловно вызывает `v4_returnValueArray(ids)` и при `ids=[]`; [Kesco.V4.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.V4.js) (`v4_returnValueArray`, `v4s_setSelectedValue`) передаёт JSON `[]`; [DialogResultHandler.cs](https://github.com/KescoDev/Kesco.App.Web.V4.Inventory/blob/12a6efae7eb310adec06a57f6d80b8d9e76985b4/Kesco.Lib.Web.Controls.V4/Handlers/DialogResultHandler.cs) не считает непустую строку `[]` отменой и вызывает multi-result setter. Setter сначала отправляет `clearSelectedItems`, затем выполняет ноль добавлений. Поэтому **«Сохранить» с пустым набором очищает подтверждённый выбор родителя**, а **«Отмена» его не изменяет**. Современный typed protocol должен явно различать эти результаты. Старый cookie-path с пустой `RetVal` и неизвестными callers требует собственной проверки совместимости; он не меняет доказанный MVC4-контракт.

`URI_area_search` используется в Config и как `area_search` для правой области, и как `territory_search` у DBSTerritory. Фактический URL задаётся отсутствующим в репозитории `uri.local`. Это может быть metadata-список, поэтому не предполагай, что он всегда равен текущему дереву; иначе можно получить рекурсивное вложение страницы в себя.

## 3. Левая область и schema

Legacy source:

| Настройка | Значение |
|---|---|
| Table/View | `Территории` / `Территории` |
| PK | `КодТерритории` |
| Текст | `Территория` |
| Иерархия | `Parent` и nested-set `L/R` |
| Connection | `Config.DS_user` |
| RootName | «Территории» |
| State | `IsSaveState=true`, `ParamName="InvTerTreeState"`, `ClId=ClId` |
| Поиск/расширенный фильтр | Включены |
| Пункт сортировки | Выключен |
| Перетаскивание | Включено |
| Возврат | Разрешён |
| Resize | Левая область изменяет ширину |
| Стандартные add/rename/delete | Выключены; вместо add/edit — собственные пункты |
| Сброс default filter | `IsDeleteDefaultFilter=false` |

Основные поля для фильтра имеют alias `id → «Код территории»`, `text → «Территория»`, типы Int32/String. Список `ParentId, L, R, Изменил, Изменено, BitMask, ЕстьДети` передаётся в `SetColumnAdvSearchName`. Несмотря на имя метода, проверка в `RenderAdvancedSearchSettings` показывает: эти колонки **исключаются** из доступных пользовательских фильтров. Не покажи их как набор «расширенного поиска».

Источник дополнительных сохранённых условий — `ConditionName=Config.filterClause`; в Web.config `filterClause="Территории"`. Не теряй подключение существующих условий, если они используются. Их реальные metadata/права проверяются на стенде.

## 4. SQL-сценарии TerritoryData и совместимость handler

### Обычная ленивая загрузка

`SELECT_ТерриторииДанныеДляДерева` возвращает:

- `Id=КодТерритории`;
- `Text=Территория`;
- `ParentId=ISNULL(Parent,0)`;
- `ЕстьДети`: при существовании прямого ребёнка — `R-L`, иначе 1;
- пустой служебный `Фильтр`.

`@Потомки=1`: для `@Код=0` — корни `Parent IS NULL`, для ненулевого — прямые дети `Parent=@Код`. `@Потомки=0`: сам узел. Поиск детей нельзя заменить выбором всех потомков: интерфейс должен оставаться деревом прямых уровней.

`TerritoryData.GetTreeData_Sql` принудительно устанавливает orderByField в поле имени территории. Следовательно, этот экран отображает уровни по имени, даже если общий handler по умолчанию знает L. Направление сортировки также должно проходить whitelist, а не попасть в SQL строкой из URL.

### Ограниченные корни: подтверждённые несовпадения контрактов

Разделяй унаследованный входной контракт `rootids/root/parent` и фактическую пригодность специального SQL территории для него:

1. `TreeViewDataHandler.GetTreeDataPartial` передаёт `@Код=""` и **`@Потомки=2`**, затем ожидает выбранные корни и их прямых детей. Но `TerritoryData.GetTreeData_Sql` при пустых `where/openList` возвращает `SELECT_ТерриторииДанныеДляДерева`, где предикат содержит только `@Потомки=0` и `=1`. Для значения 2 в этом SQL нет разрешающей ветки. Это статически установленная несовместимость, а не готовая реализация ограниченного дерева.
2. `GetSearchTreeDataPartial` вызывает `GetTreeData_Sql` только с первыми четырьмя аргументами; в отличие от `GetSearchTreeData`, не вызывает `GetAddQuery` и не передаёт `where/addField/addTable`. Пустой `where` снова выбирает обычный SQL территории, а `DBReader` получает `null` вместо входных `@Код/@Потомки` и только output `@КоличествоНайденных`. Нужные специальному SQL аргументы отсутствуют; по одному `searchParam="filter"` override фильтр не выбирает.
3. В `ProcessRequest` при начальном `nodeid=0` пути `loadId` и `selectedids` проверяются раньше `partialLoad`; при наличии rootids загрузка state выключается. Поэтому не делай недоказанный вывод, что любой URL с rootids обязательно проявляет один и тот же сбой: вызванную ветку и фактический ответ БД надо воспроизвести отдельно.

В Clayzor реализуй ограничение набора корней явным typed-контрактом общей загрузки и фильтра. Оно должно действовать на сервере для initial/lazy/filter/state/selectedids и сохранять допустимые пути внутри scope. Не копируй недостающую SQL-ветку, не изображай пустой результат как успешное ограничение и не устраняй проблему снятием root scope. Исправление описывай как устранение указанных несовпадений исходников; DB/browser-воспроизведение legacy при подготовке промта не проводилось.

### Восстановление сохранённого состояния

При nonempty openList используется `SELECT_ТерриторииДанныеДляДерева_State`:

1. Берутся предки раскрытых/выбранных IDs по L/R.
2. Добавляются дети открытых узлов и недостающие корни.
3. Сохраняются служебные BitMask/поля audit.
4. **Только эта ветка** в конце исключает `КодТТерритории=4` и имена, содержащие `"связь"`; при расчёте ЕстьДети тоже исключаются дети типа 4.

Это не правило всех деревьев и не фильтр обычного запроса: обычная и фильтрованная ветки таких исключений не содержат. Проверь живой сценарий, классифицируй как legacy-несогласованность и зафиксируй решение. Не превращай случайное ограничение state SQL в глобальное исключение городов — форма отдельно имеет пользовательский сценарий города.

Отдельное статическое расхождение ключа state: страница присваивает `tvAreas.ClId=ClId`, TreeView формирует `type=save_state&Clid=<ClId>&ParamName=InvTerTreeState`, а `ProcessRequest` сохраняет параметр с этим Clid и `AppParamType.SavedWithClid`. Однако `InitOpenListState` создаёт `AppParamsManager` с локальным **`clid="0"`**. Это несовпадение аргументов чтения и записи установлено по коду. Без проверки `AppParamsManager`, его fallback и данных параметров нельзя утверждать, что состояние всегда теряется или смешивается для всех ненулевых CLID. В новом persistence чтение и запись должны использовать один согласованный ключ пользователя/контекста; проверить CLID=0, два разных ненулевых CLID и разные пользователи. Не исправляй это превращением всех контекстов в общий CLID=0 и не переноси старую модель множества веток вместо принятого single-path-контракта ClayTree.

### Фильтр

`SELECT_ТерриторииДанныеДляДерева_Фильтр`:

- вычисляет все совпадения и `@КоличествоНайденных`;
- сохраняет первые 100 совпадений по L;
- добавляет предков и корни для контекста;
- использует BitMask: 1 — совпадение, 2 — содержит совпадения/контекст предка, 4 — корень;
- возвращает ID/text/ParentId/L/R/ЕстьДети/BitMask, сортирует по имени.

Сохрани различие найденных узлов и контекста, реальный total против лимита 100, раскрытие полного уровня из фильтрованной ветки, очистку фильтра. Не считай добавленных предков дополнительными совпадениями. При отсутствии совпадений не выдавай старую выборку за результат поиска.

**Дефект совпавшего настоящего корня.** Первичная вставка совпадений в `SELECT_ТерриторииДанныеДляДерева_Фильтр` записывает `ISNULL(r.Parent,0)` в временное поле `Parent`. Вставка предков/дополнительных корней использует настоящий `Parent`, то есть NULL для корня, но не вставляет уже имеющийся Id. Если реальный корень сам попал в первые 100 совпадений, он остаётся с Parent=0; финальный SELECT отдаёт это как ParentId. Между тем `GetSearchTreeData` начинает сборку дерева только со строк **`[ParentId] IS NULL`**. Такой совпавший корень не прикрепляется к синтетическому root, и его ветка с совпавшими детьми не попадает в сериализуемое дерево, хотя total учитывает совпадения. Если совпал только ребёнок, его настоящий корень добавляется как предок с NULL, и этот путь собирается иначе. Это статически прослеживаемый дефект связки SQL/handler, не доказательство того, какие данные сейчас есть на рабочем сервере.

В новой read model/SQL согласуй одно представление настоящего корня для всех веток и сборщика; синтетический ID0 и NULL в исходной иерархии должны оставаться различимыми. Не меняй реальные `Территории.Parent` ради исправления отображения. Обязательны отдельные случаи: совпал только настоящий корень; совпали корень и ребёнок; совпал только ребёнок. В первых двух корень и соответствующие совпадения не исчезают, а контекстные предки не прибавляются к total.

**Счётчик найденных.** SQL записывает в `@КоличествоНайденных` полный `@@ROWCOUNT` до отсечения до 100. Но обе унаследованные функции `GetSearchTreeData` и `GetSearchTreeDataPartial` читают его через `Convert.ToInt16`. При значении 32768 и выше возникает переполнение преобразования, даже если возвращено только 100 совпадений. Современный контракт счётчика — SQL int / C# `int` без промежуточного short; покрыть 32767/32768 и отдельно проверить, что limit отображения не подменяет total. Не уменьшай результат до 32767 и не проглатывай ошибку в ноль.

В новом SQL все значения — параметризованные. Нельзя переносить конкатенацию `openList`, произвольного `where` и order direction. Идентификаторы колонок/таблиц фиксируются server-side schema, условия создаются общим безопасным builder. SQL Server 2008 R2 обязателен.

## 5. Правая область и переходы состояния

По умолчанию `ShowData="territories"`. Сохраняется `TerritoryId` — выбранный настоящий узел. Кнопка выбора вида имеет пункты **«Территории»** (1) и **«Телефонные коды»** (2).

`LoadTreeViewData`:

| Событие | Результат |
|---|---|
| `id="0"`, в частности после применения фильтра | Сброс ShowData в territories, TerritoryId очищен, правый iframe скрыт/пуст, тексты меню сброшены |
| Выбрана обычная территория | Тексты «Территории в [название]», «Телефонные коды в [название]»; загружается выбранный текущий вид |
| Выбран тип территории 4 (комментарий legacy: город) | Автоматически tel_code; пункт территорий исчезает, вместо dropdown показана подпись телефонных кодов |
| ChangeViewData=2 | tel_code; иначе territories; повторная загрузка только при выбранном TerritoryId |

При переходе из города в обычную территорию legacy не принудительно возвращает вид territories: сохраняет текущее ShowData. Проверь и сохрани эту последовательность; не делай каждый выбор узла сбросом вида.

Загрузки:

- Territories: `Config.area_search` + `parent=<id>`.
- Phone codes: `Config.telcode` + `areaid=<id>`.
- Оба добавляют `hidemenu=true&hidetitle=true`.
- Закомментированный `CLID=86` **не действует**; не вводи его как обязательный legacy default.

Правая область должна использовать уже имеющийся динамический ClayGrid и реальные metadata-списки. Полные query-id, набор/порядок колонок, операции добавления и адреса карточек определяются конфигурацией/`Запросы`, их нет в source этой страницы. Прочитай их на стенде. Не подменяй «территории в выбранной» всеми потомками, если действующий SQL задаёт только прямых детей.

Смена вида/узла/фильтра отменяет прежнюю загрузку. Медленный ответ A не должен показаться под заголовком B. Ошибка одной области не очищает другую и не выдаётся за пустой результат.

## 6. Контекстное меню и CRUD-связи

Собственные пункты в точном порядке:

1. **«Добавить дочернюю»** → `AreaChildAdd`.
2. **«Редактировать»** → `AreaEdit`.

JS `openAreaForm` передаёт:

- edit: `id=<selected>`;
- add child: `id=0&parent=<selected>`;
- `fromTree=true`;
- `parentidp`, `parentId`, `parentName`.

В Clayzor открывай полноценную `TerritorialEntity_form` через clayOpen. Не используй встроенный generic rename/add-child диалог ClayTree: он меняет одно название, а карточка требует тип, код страны, направление и остальные реквизиты. Не включай стандартное удаление дерева: в legacy этого пункта нет, а штатное удаление территории — отдельная master-процедура из карточки.

Legacy custom menu не фильтруется теми же проверками HasInsert/HasUpdate, что стандартное меню. Это не разрешение делать операции без прав. Сохрани доступные read-сценарии, но server-side write всегда требует актуальных полномочий.

После сохранения дочерней территории обнови соответствующего родителя, восстанови видимые уровни/выделение в рамках принятого ClayTree-контракта, не перезагружай всё приложение. После изменения имени обнови имя/алфавитную позицию; после удаления — корректно обработай исчезнувший selection и правую область. Черновики соседних карточек не сбрасываются.

## 7. DnD и реальные записи в БД

`IsDraggable=true` означает, что эта страница **не полностью read-only**. Общий `TreeView.ProcessClientCommand("MoveTreeViewItem")`:

- при смене parent: `UPDATE Территории SET Parent=@Parent WHERE КодТерритории=@Id`;
- при перестановке внутри родителя: используется `UPDATE_ДеревоПеремещениеУзлаБезСменыРодителя`, поиск новой позиции по L и обновление L;
- при ошибке показывается сообщение, затем перечитывается узел/ветка.

Локального override ChangeTreeViewItemParent нет. Реальную поддержку nested-set обеспечивает схема/триггеры, которых в этих source-файлах нет. До DnD-переноса проверь её на стенде. Не переписывай L/R самодельным алгоритмом поверх действующих triggers.

В Clayzor применяй существующие механизмы IClayTreeMutations/Entities, но со специальным domain adapter, если generic DML не учитывает current-user и правила Территорий. Нельзя просто включить `TableName="Территории"` и считать доступность UI достаточной авторизацией.

Проверки: сам в себя, в собственного потомка, удалённый source/target, reparent в корень (SQL NULL, не фиктивный ID0), отсутствие прав, отказ/ошибка триггера, повторное действие, конкурентное изменение и hierarchy consistency.

**Сортировка по имени vs reorder:** отображение этого legacy handler по имени может скрывать эффект изменения L. Не меняй порядок экранных узлов на L лишь ради удобства DnD. Раздели reparent и sibling reorder, изучи реальный сценарий и запиши принятое поведение.

## 8. Современный ClayTree: что уже есть и чего не хватает

Переиспользуй ленивую загрузку, schema, фильтр, контекстные меню, refresh и mutation lifecycle текущего компонента.

На исследованном срезе:

- `ClayTreeOptions` упоминает `SelectionMode.Multiple` как задел; в текущем enum `ClayTreeSelectionMode` реально объявлены только None/Single, checkbox multiple не реализован.
- `ClaySelectAdvancedSearchService`/JS автоматически устанавливает только `return=1`.
- ClayTree persistence сохраняет **один путь**: LastExpandedId плюс SelectedIds, а не все раскрытые ветки legacy `InvTerTreeState`.
- NestedSet level paging использует курсор L; текущая прикладная сортировка по имени требует осторожности. Не включай LevelPageSize с алфавитной сортировкой без подтверждённой совместимости cursor/order; на умеренных уровнях допустима полная ленивая загрузка уровня.
- Выбор общего запроса формы не должен записывать mutable состояние в static. В legacy handler есть static request-поля, их не переносить: current-user/query/state должны быть изолированы между circuits.

Рабочий план:

1. Использовать современный single-path persistence как установленный общий контракт, документировав отличие от legacy и проверив восстановление выбранной территории/вида. Не возвращать в общий компонент прежнюю модель множества веток исключительно ради этой страницы.
2. Для обязательного `return=2` согласованно расширить **общий** ClayTree/callback либо предоставить отдельный совместимый выбор на общей инфраструктуре. Не писать частный DOM-чекбокс поверх дерева. Нельзя завершить перенос, заявив «multi не нужен», без подтверждённого исключения этого сценария.
3. При нескольких rootids обеспечить ограничения на всех lazy/filter/state запросах, а не только визуальное скрытие корней. Один `RootId` текущих options не равен массиву rootids; нужен подтверждённый adapter.
4. Сохранённое состояние другого root/filter/caller не должно расширять разрешённую область данных.

Общий дефект boolean-vs-Window при открытии advanced search устраняется этапом G0 оркестратора под владельцем Finder. Новые обработчики возврата должны использовать подтверждённый протокол `{type:"clay-select-return",requestId,value}`, проверять origin/source/requestId и завершать отмену без изменения поля. Для multi payload заранее зафиксировать тип массива/элемента; не сериализовать числа в двусмысленный текст.

## 9. Права, ошибки и локализация

Старое дерево получает `PermOnSelect/Insert/Update/Delete` через `HAS_PERMS_BY_NAME(@TableName,'OBJECT',operation)`. Это effective SQL permissions, не конкретная роль «Администратор». В code-behind специального HasRole нет. Подтверди модель прав под реальным пользователем; не назначай номера ролей по догадке.

CurrentUser нужен до вычисления доступности действий. Перед reparent/reorder/create/edit/delete — свежая проверка в Entities под доверенной identity. Нет прав на изменение — доступны разрешённые чтение/выбор, нет записи через forged command. Недоступность identity/SQL не становится разрешением. Сохранение пользовательского state тоже не должно затрагивать чужой профиль.

Тексты из БД, включая имена с кавычками и HTML, должны быть закодированы; legacy `GetPrefixIcon` вставляет text в onclick-строку, такую конструкцию не переносить.

Не показывай SQL-ошибку как отсутствие детей. После отменённой/упавшей загрузки должен быть явный retry. При mutation failure UI возвращается к фактической структуре, без «успешного» перемещения. Dispose освобождает subscriptions, request tasks и JS handles.

## 10. Приёмка

| Группа | Минимальные сценарии |
|---|---|
| Маршруты/меню | Новый и .aspx URL, заголовок, отсутствие рекурсивного iframe, корректное скрытие меню во вложенных списках |
| Lazy tree | Корни, 3+ уровня, листья, широкие уровни, отсутствие N+1 на каждом render |
| Root scope | rootids/root/parent приоритет, один/несколько roots, initial partial, partial filter, сочетания с loadId/selectedids, invalid IDs, сохранённый путь вне scope; ни пустой SQL-ветки `@Потомки=2`, ни отсутствующих входных аргументов |
| Фильтр | ID/text, содержит/начинается, нет совпадений, ровно 100/101+, предки не считаются совпадениями |
| Совпадение корня | Настоящий корень без совпавших детей; корень+ребёнок; только ребёнок. Сохранены корни, соответствующие ветки/подсветка и правильный total |
| Счётчик | 32767/32768 и больше совпадений: int не переполняется, UI-limit 100 не меняет total |
| State | Выбранный ID, один путь ClayTree, CLID=0/два ненулевых CLID/два пользователя, одинаковый ключ save/load, удалённый узел, неизвестное/старое state |
| Legacy state quirks | Города type4 и имя «связь» в normal/filter/state ветках — решение явно записано |
| Правая область | Обычная территория, город, город→обычная, смена вида, id0 после фильтра, поздний SQL-ответ |
| Карточки | Add child Parent, edit и delete через карточку, точечный refresh без сброса родителя |
| DnD | Reparent/root/reorder, отмена, сам/потомок, stale node, отказ прав, ошибка trigger, L/R integrity |
| Single return | ID+text, cancel/крестик, opener исчез, чужой origin/source/requestId |
| Multi return | selectedids, добавление/снятие, несколько веток, синтетический root не выбирается; «Сохранить» возвращает массив, подтверждённый пустой массив очищает выбор родителя по установленному MVC4/новому typed-контракту; «Отмена» снимает только локальное выделение и закрывает без изменения поля родителя |
| Права | Read-only пользователь, запись прямым вызовом, отзыв прав после открытия, fail closed |
| Безопасность данных | Кавычки/HTML в имени, SQL-like значения query/filter, отсутствие static cross-user state |
| UI | 320–480px, resize split, keyboard, scroll, пустые/ошибочные области |

Проведи meaningful тесты дерева/фильтра/callback/mutations и browser-интеграцию с обеими карточками. Проверка статической Razor-разметки не заменяет проверку DnD на тестовой БД и реального return.

## 11. Завершение и порядок

Эта форма завершает территориальный блок: базовые Territory/TelCode модели и lookup → PhoneCodeEntity_form и TerritorialEntity_form → готовый search + связанная приемка. Чтение и каркас можно делать параллельно карточкам, общие Controls/multi-return/SQLQueries правит назначенный единственный владелец.

Handoff включает карту legacy→ClayTree; решения по state-фильтру городов, alphabetical reorder и legacy multi; устранение несовместимости partial SQL/аргументов, root NULL/0 и short-счётчика; подтверждённый ключ state save/load; фактические metadata правых списков, общий callback и доказанные права/DDL/триггеры. Для каждого legacy-дефекта отдельно укажи статическое доказательство и проведённое либо недоступное DB/browser-воспроизведение. Все оставшиеся ограничения указывай отдельно; наличие обычного single-select дерева не является выполнением этого промта.

## Дополнение по общим скриптам Styles

Обязательны [PROMPT_SHARED_CLIENT_BEHAVIOR.md](PROMPT_SHARED_CLIENT_BEHAVIOR.md) и [03_STYLES_CLIENT_AUDIT.md](03_STYLES_CLIENT_AUDIT.md). Этот экран зависит от **CL-01** (single/multiple/cancel protocol и child refresh), **CL-03** (клавиатура/фокус), **CL-07** (правая область Grid), **CL-08** (Tree) и **CL-12** (state/lifecycle). Изучи [Kesco.TreeView.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/Kesco.TreeView.js) вместе с реально подключёнными defaults [jstree.js](https://github.com/KescoDev/Styles/blob/fddc5a2157cf906402c50731e84cfa537abd7dba/js/jstree.js). Vendor-плагины переносить в Clayzor не требуется.

### Active node, checkbox и возвращаемый набор — разные состояния

Legacy TreeView задаёт `three_state=false`, `whole_node=false`, `tie_selection=false`. Клик текста/выделение узла вызывает `LoadTreeViewData` для правой области, а `v4_treeViewGetCheckedNodesIds` возвращает именно отмеченные checkbox. Поэтому одного `SelectedIds` и CSS-подсветки ClayTree недостаточно. Общий CL-08 должен предоставлять раздельные активный узел и checked keys; страница их использует, не рисуя частные DOM-checkbox поверх дерева.

При отметке узла `v4_treeViewUncheckNodesAtOtherLevelsOfBranch` снимает отметки у его предков и потомков. Соседи и разные ветки могут оставаться отмеченными; автоматической отметки всех детей/tri-state каскада нет. Реализуй это как согласованную политику общего дерева. Выбор хранится по ключам независимо от DOM, не теряется от фильтра, сворачивания и lazy load; связь предок/потомок учитывается и для первоначально выбранных ещё не отображённых ключей. Не загружай весь справочник ради сравнения: используй доступные ancestor relationships/domain adapter. Разрешённый root scope и права проверяются до возврата, синтетический ID0 исключён.

Исходные `selectedids` инициализируют checked state по caller identity. Legacy повторно вызывает `check_node` для них на каждом `load_node`, что может вернуть снятую пользователем отметку; не копируй это. При отмене не следует копировать и `deselect_all` как гарантированное снятие галочек: при `tie_selection=false` это другой набор. Закрытие/Cancel отправляет отсутствие результата; подтверждение `[]` явно очищает родителя, как доказано в §2. Один requestId принимается один раз, duplicate/stale message не повторяет очистку или применение.

### Поиск, total и пустая выдача

ClayTree уже имеет общий filter tree/dialog, признаки `IsMatch/HasMatchChildren`, ленивую загрузку полного уровня из фильтрованной ветки и state. Используй эту основу. Поля ID/text и режимы «содержит/начинается» должны быть доступны через общий API фильтра/согласованный toolbar slot; не добавляй конкурирующий SQL или JS-фильтр только на эту страницу.

На pinned срезе `ClayTreeLoadResult` не содержит полного total: `ApplyFilterAsync` считает `IsMatch` из ограниченного набора, срезает max+1 и показывает «более Max». Для территории это не равно установленному SQL `@КоличествоНайденных` до отсечения. CL-08 должен дать совместимый optional total/result contract, территория передаёт точный `int`; UI различает найдено всего, показано совпадений и добавленных предков. Не заставляй каждое другое дерево делать отдельный дорогой COUNT, если оно не нуждается в точном числе; там допускается честный capped-result. Здесь число 100/101+ не заменяет полный total.

Ещё одно точное расхождение: текущий `ApplyFilterAsync` при нуле строк вызывает `LoadRootsAsync`, а AGENTS описывает возврат к обычным корням. Для этого экрана нужен явный результат «не найдено» и действие очистки фильтра, а не обычные корни под действующим поиском как будто это совпадения. Владелец CL-08 добавляет совместимый настраиваемый режим пустого результата и явно включает его для TerritorySearch; прежний default и сценарии существующих деревьев сохраняются. Само отличие от legacy не разрешает менять все Tree consumers. Приёмка включает Territory no-match, старый режим другого потребителя и различие zero/error/default-only filter. Legacy `v4_SetSearchResult` содержит присваивание `extSearch = 1` в условии; такой дефект не переносится.

### Клавиатура и реальный фокус

Подключённый jstree даёт навигацию стрелками, Home/End, активацию Enter и double-click expand. В current ClayTree текст узла — `span` без tree keyboard handling; существующий `RestoreFocus` в drag code меняет selected state, а не DOM focus. CL-08 совместно с CL-03 должен обеспечить keyboard owner/roving focus, правильные tree/treeitem semantics и возврат фокуса после собственного reload/диалога. Страница не подменяет это поздним `setTimeout(...focus...)`, способным отобрать фокус у поля поиска или другой карточки.

Up/Down проходят видимые узлы, Left сворачивает/переходит к родителю, Right раскрывает/переходит к ребёнку; Enter активирует, Space в multiple переключает отметку по явному контракту. Клик checkbox не должен неожиданно сбросить активный узел правой области. Double-click текста раскрывает дерево, не выполняет return/save/edit. Vendor F2 inline edit не включается: Kesco `check_callback` запрещает `edit`, а эта форма открывает полноценную TerritorialEntity_form. Безлимитный vendor open_all не переносится в lazy SQL дерево. Подсказка ID `#id` из legacy и полный усечённый текст поддерживаются общей настройкой tooltip, без глобального поиска DOM по raw ID.

### Несколько контекстов и существующие мутации

Сохрани принятый single-path persistence ClayTree. Отдельно зафиксируй отличие collapse: legacy закрывает дочерние открытые ветки, current компонент хранит их раскрытость в памяти; выбери и опиши согласованное поведение без возвращения legacy all-branches storage. Transient checked choice одного caller не должен становиться выбором другого caller или другого пользователя.

TreeId, Grid Id, state keys, requestId и lifecycle identity изолированы по экземпляру/контексту. Справа используется не более одного DynamicGrid одновременно согласно Grid/AGENTS; переключение territories/phone codes не означает два скрытых Dynamic экземпляра. Общий defect `ClayDragState` исправляется владельцем CL-07, не локальным helper страницы. Источники открываемых карточек/refresh принадлежат вызвавшей области, а не первому grid на странице.

DnD-подтверждения, guards self/descendant/no-op, reload двух затронутых родителей и connectivity failure handling уже существуют в ClayTree: используй их с domain adapter из §7. Не дублируй nested-set алгоритм в JS. Generic inline rename/add/delete остаются выключенными; полноценные карточки, права и согласованный alphabetical reorder — обязательная прикладная часть.

Дополнительная приёмка: checked A + active B; parent→child и child→parent; siblings; initial checked после снятия и новой lazy load; offscreen выбранный ключ; filter/clear без потери checked state; confirmed `[]` против Cancel; повторное callback сообщение; zero против SQL error; total 100/101/32767/32768; keyboard после lazy load и mutation; focus не украден после ухода в другое поле; два callers/CLID/circuits не смешиваются. Исследование исходников не заменяет эти runtime-проверки.

