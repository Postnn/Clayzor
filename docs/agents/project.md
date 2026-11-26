# Сборка, конфигурация и карта проекта

Команды и пути в примерах приведены от корня суперпроекта.

## Build & Run
```
dotnet build Clayzor.sln
dotnet run --project src\Clayzor.App.Web.MedicalTests
```
Dev URL: `http://localhost:5010`

## Tests
```
dotnet test tests\Clayzor.Lib.Web.Controls.Tests
```
Тестовый проект: xUnit, `net10.0`. Покрывает SQL-билдер, модель фильтра, JSON/URL-сериализацию, описание и индикатор. См. `tests/Clayzor.Lib.Web.Controls.Tests/`.

Watch with hot reload (VS Code task also available):
```
dotnet watch run --project src\Clayzor.App.Web.MedicalTests
```

## Project dependency chain
```
Clayzor.Lib.DALC          ← Dapper, SqlClient
Clayzor.Lib.Entities       ← DALC + Dapper (column mapping)
Clayzor.Lib.Web.Settings   ← config bindings (no DB dep)
Clayzor.Lib.Web.Controls ← Entities + Web.Settings + MudBlazor (Razor Class Library)
Clayzor.App.Web.MedicalTests ← all of the above (web app entry point)
```

## Configuration — connection string priority

`BindClaySettings()` in `Clayzor.Lib.Web.Settings/ClayAppSettings.cs`:
1. Биндит секцию `ClayApp` из конфигурации (`appsettings.json` + `web.config`)
2. Если `ConnectionString` пуста — читает имя из `ClayGrid:Dynamic:ConnectionStringName` (по умолчанию `"DefaultConnection"`)
3. Читает строку подключения с этим именем из `web.config` через `System.Configuration.ConfigurationManager.OpenMappedExeConfiguration().ConnectionStrings`
4. `URI_help_clayGrid` читается из `web.config` через `ConfigurationManager.AppSettings` (ключ `URI_help_clayGrid`)
5. `DictionaryConnectionString` — аналогично п.3 с именем `"DictionaryConnection"`

Оба `appsettings.json` и `web.config` участвуют в конфигурации; `web.config` — основной источник для `connectionStrings` и `appSettings`. Локальные переопределения — в `appsettings.Development.json` (gitignored).

`Program.cs` registers MudBlazor and MudBlazor.Extensions:
```csharp
builder.Services.AddMudServices();
builder.Services.AddMudExtensions(cfg => cfg.WithDefaultDialogOptions(d => d.DragMode = MudDialogDragMode.Simple));
```

`App.razor` must include MudBlazor.Extensions CSS and JS:
```html
<link href="_content/MudBlazor.Extensions/mudBlazorExtensions.min.css" rel="stylesheet" />
<script src="_content/MudBlazor.Extensions/js/mudBlazorExtensions.js"></script>
```

`App.razor` must also include Clayzor RCL JS scripts:
```html
<script src="_content/Clayzor.Lib.Web.Controls/js/clayColumnSettings.js"></script>
<script src="_content/Clayzor.Lib.Web.Controls/js/clayGridColumnDrag.js"></script>
<script src="_content/Clayzor.Lib.Web.Controls/js/clayGridPrint.js"></script>
<script src="_content/Clayzor.Lib.Web.Controls/js/clayGridExcel.js"></script>
<script src="_content/Clayzor.Lib.Web.Controls/js/clayFormWindow.js"></script>
<script src="_content/Clayzor.Lib.Web.Controls/js/clayDraftStore.js"></script>
```

- `appsettings.Development.json` is gitignored — use it for local connection strings
- `Program.cs` регистрирует `ClayErrorService` как Scoped и как `ISqlErrorHandler`, передаёт `ISqlErrorHandler` в конструктор `DbManager`

## Map / где что искать

| Что | Где |
|---|---|
| Доступ к БД, SQL-конвенции, пагинация SQL Server 2008 R2 | **[src/Clayzor.Lib.DALC/AGENTS.md](../../src/Clayzor.Lib.DALC/AGENTS.md)** |
| Entity CRUD & Lookup, добавление новых сущностей | **[src/Clayzor.Lib.Entities/AGENTS.md](../../src/Clayzor.Lib.Entities/AGENTS.md)** |
| Документация по Entity, CRUD, ILookupEntity | [src/Clayzor.Lib.Entities/docs/entity-crud.md](../../src/Clayzor.Lib.Entities/docs/entity-crud.md) |
| Воркфлоу добавления новой сущности | [src/Clayzor.Lib.Entities/docs/adding-new-entity.md](../../src/Clayzor.Lib.Entities/docs/adding-new-entity.md) |
| Роль сборки настроек/конфигурации | **[src/Clayzor.Lib.Web.Settings/AGENTS.md](../../src/Clayzor.Lib.Web.Settings/AGENTS.md)** |
| Правила форм и Services Inventory | **[src/Kesco.App.Web.Inventory/AGENTS.md](../../src/Kesco.App.Web.Inventory/AGENTS.md)** |
| Shared-компоненты (ClayGrid, ClayComboBox, ClayEditForm, ...), интерфейсы, сервисы, серверная группировка, фильтрация, фильтр-трей, сериализация, локализация | **[src/Clayzor.Lib.Web.Controls/AGENTS.md](../../src/Clayzor.Lib.Web.Controls/AGENTS.md)** |
| Документация ClayGrid | [src/Clayzor.Lib.Web.Controls/docs/clay-grid.md](../../src/Clayzor.Lib.Web.Controls/docs/clay-grid.md) |
| Документация ClayComboBox | [src/Clayzor.Lib.Web.Controls/docs/clay-combo-box.md](../../src/Clayzor.Lib.Web.Controls/docs/clay-combo-box.md) |
| Документация ClayEditForm | [src/Clayzor.Lib.Web.Controls/docs/clay-edit-form.md](../../src/Clayzor.Lib.Web.Controls/docs/clay-edit-form.md) |
| Документация ClayErrorBar | [src/Clayzor.Lib.Web.Controls/docs/clay-error-bar.md](../../src/Clayzor.Lib.Web.Controls/docs/clay-error-bar.md) |
| Документация ClayColumnFilterDialog | [src/Clayzor.Lib.Web.Controls/docs/clay-column-filter-dialog.md](../../src/Clayzor.Lib.Web.Controls/docs/clay-column-filter-dialog.md) |
| Документация ConfirmDialog | [src/Clayzor.Lib.Web.Controls/docs/confirm-dialog.md](../../src/Clayzor.Lib.Web.Controls/docs/confirm-dialog.md) |
| Форма телефонного номера Tel и руководство пользователя | [src/Kesco.App.Web.Inventory/docs/phone-form.md](../../src/Kesco.App.Web.Inventory/docs/phone-form.md) |
| Подтверждённые пропуски EquipmentForm_2 | [promts/EquipmentForm_2_skipped.md](../../promts/EquipmentForm_2_skipped.md) |
| Руководство пользователя ModelEquipmentForm | [src/Kesco.App.Web.Inventory/docs/model-equipment-form.md](../../src/Kesco.App.Web.Inventory/docs/model-equipment-form.md) |
| Руководство пользователя EquipmentIns | [src/Kesco.App.Web.Inventory/docs/equipment-ins.md](../../src/Kesco.App.Web.Inventory/docs/equipment-ins.md) |
| Руководство пользователя EquipmentForm | [src/Kesco.App.Web.Inventory/docs/equipment-form.md](../../src/Kesco.App.Web.Inventory/docs/equipment-form.md) |
| Контракты EquipmentForm | [src/Kesco.App.Web.Inventory/docs/equipment-form-write-contracts.md](../../src/Kesco.App.Web.Inventory/docs/equipment-form-write-contracts.md) |
| Руководство пользователя ClaySelect | [src/Clayzor.Lib.Web.Controls/docs/clay-select-user-guide.md](../../src/Clayzor.Lib.Web.Controls/docs/clay-select-user-guide.md) |
| Динамический режим ClayGrid — план реализации (G0–G14, выполнены) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/DynamicFilter/_readme_grid_dynamic.md](../../src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/DynamicFilter/_readme_grid_dynamic.md) |
| Динамический режим ClayGrid — багфиксы (GF1–GF16) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/DynamicFilter/GF0_README_dynamic_fixes.md](../../src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/DynamicFilter/GF0_README_dynamic_fixes.md) |
| Динамический режим ClayGrid — группировка (GG1–GG9) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GG/GG0_README_dynamic_grouping.md](../../src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GG/GG0_README_dynamic_grouping.md) |
| Динамический режим ClayGrid — снятие потолка уровней (GN1–GN4) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GN/GN0_README_grouping_levels.md](../../src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GN/GN0_README_grouping_levels.md) |
| Динамический режим ClayGrid — печать и Excel (GE1–GE6) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GE/GE0_README_dynamic_export.md](../../src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GE/GE0_README_dynamic_export.md) |
| ClayGrid — багфиксы UX (GB1–GB11) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/DynamicFilter/GB0_README_grid_ux_fixes.md](../../src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/DynamicFilter/GB0_README_grid_ux_fixes.md) |
| DynamicGrid: SQL-схема (dev/test стенд) | [scripts/dynamic-grid/schema.sql](../../scripts/dynamic-grid/schema.sql) |
| DynamicGrid: быстрый поиск (QS0–QS9) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/QS/QS0_README_quick_search.md](../../src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/QS/QS0_README_quick_search.md) |
| ClayGrid — аудит безопасности (GA0–GA9, GA1–GA6 выполнены) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/GA0_README_audit_grid.md](../../src/Clayzor.Lib.Web.Controls/Components/Grid/promts/GA0_README_audit_grid.md) |
| Типографика, шрифты, style enforcement (Variant A, build-time, checklist) | **[src/Clayzor.App.Web.MedicalTests/AGENTS.md](../../src/Clayzor.App.Web.MedicalTests/AGENTS.md)** |
| Закон стиля (исходник) | [STYLE_RULES.md](../../STYLE_RULES.md) |
| Промты внедрения стиля (выполнены) | [promts/_done/STYLE_PROMPTS.md](../../promts/_done/STYLE_PROMPTS.md) |

## Новая форма или новое приложение на Clayzor

1. Прочитай корневой `AGENTS.md`, [правила форм](forms.md) и обязательные [ключевые требования FORM-KEY-01…09 / DR-01](editing-forms-key-requirements.md). Сошлись на каноническом контракте в `AGENTS.md` нового приложения/модуля; самостоятельный репозиторий должен иметь разрешимую закреплённую ссылку.
2. Подключи существующие общие контролы, draft/Cancel/field-state/focus/navigation, shell справки и темы; для редактора существующей сущности — общий presence. При недостающей возможности сначала доработай общий компонент, не копируй Inventory-реализацию в новое приложение.
3. Зарегистрируй форму: ключ/route aliases, путь справки, `URI_wiki_help` (существующее имя сохраняется по решению пользователя), контекст редактирования/черновика и каноническую идентичность сущности. `URI_help_clayGrid` остаётся отдельной справкой компонента. Режимы поиска/отчёта/диагностики не получают фиктивную бизнес-сущность.
4. Сохрани общий ClayTheme, default Classic Light, настройки/обработку ошибок и все работающие consumers. Перед интеграцией назначь владельцев пересекающихся Settings/Program/shell/CSS/API; подключение новой формы не даёт права переписать их несовместимо.
5. В handoff укажи матрицу ключевых требований, реальную поддержку topology presence, локальные проверки и отдельные beta-сценарии. Наличие scaffold/метода/класса не означает выполнение пользовательского сценария.
