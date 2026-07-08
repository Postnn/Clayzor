# Разбиение AGENTS.md и /docs по сборкам — промты для агента

Задача: разбить монолитный корневой `AGENTS.md` на per-project `AGENTS.md` и разложить
файлы из `/docs` по папкам соответствующих сборок (с созданием новых файлов). Корневой
`AGENTS.md` остаётся точкой входа: глобальные правила + обзор решения + карта-навигация.

Принципы (соблюдать во всех промтах):
- НИЧЕГО НЕ ТЕРЯЕМ и НЕ ДУБЛИРУЕМ: каждый раздел монолита уходит ровно в одно место.
- Per-project AGENTS.md НЕ повторяют глобальные правила из корня — агент читает ближайший
  AGENTS.md вверх по дереву, корневой применяется всегда. Дочерние — только своё.
- После перемещения ЧИНИМ все относительные ссылки (nav-карта, ссылки на docs, кросс-ссылки
  между доками).
- Тексты уже в нейминге Clayzor/Clay — новые упоминания тоже; слова `kesco`/`BZ` не вводить.

---

## Целевая раскладка

```
AGENTS.md                                            (слим: глоб. правила + обзор + карта)
STYLE_RULES.md                                       (как есть, корень)
promts/_done/STYLE_PROMPTS.md                        (как есть)

src/Clayzor.Lib.DALC/
  AGENTS.md                                          (НОВЫЙ: доступ к БД, SQL-конвенции)

src/Clayzor.Lib.Entities/
  AGENTS.md                                          (НОВЫЙ: Entity CRUD/Lookup)
  docs/entity-crud.md                                (перенос)
  docs/adding-new-entity.md                          (перенос)

src/Clayzor.Lib.Web.Settings/
  AGENTS.md                                          (НОВЫЙ: роль сборки настроек + указатель)

src/Clayzor.Lib.Web.Controls/
  AGENTS.md                                          (НОВЫЙ: shared-компоненты, грид, группировка, фильтры)
  docs/clay-grid.md                                  (перенос)
  docs/clay-combo-box.md                             (перенос)
  docs/clay-edit-form.md                             (перенос)
  docs/clay-error-bar.md                             (перенос)
  docs/clay-column-filter-dialog.md                  (перенос)
  docs/confirm-dialog.md                             (перенос)

src/Clayzor.App.Web.MedicalTests/
  AGENTS.md                                          (НОВЫЙ: типографика, style enforcement, сборка UI)

/docs/                                               (опустошить и удалить, если пусто)
```

## Карта разделов монолитного AGENTS.md → куда уходит

| Раздел(ы) исходного AGENTS.md | Назначение |
|---|---|
| Tradeoff-интро; 1. Think Before Coding; 2. Simplicity First; 3. Surgical Changes; 4. Goal-Driven Execution | **корень AGENTS.md** (глобальные правила) |
| Clayzor — Медицинские исследования; Agent instructions; Stack; Build & Run; Tests; Project dependency chain; Configuration — connection string priority; Key conventions | **корень AGENTS.md** (обзор/эксплуатация) |
| Database access pattern; SQL constant naming convention; Rules; SQL Server 2008 R2 pagination | **Clayzor.Lib.DALC/AGENTS.md** |
| Entity CRUD & Lookup pattern; Adding a new entity | **Clayzor.Lib.Entities/AGENTS.md** (+ ссылки на docs) |
| Shared components (Clayzor.Lib.Web.Controls); Интерфейсы; Services; Codebehind-структура ClayGrid; Codebehind-структура ClayGridPageBase; Server-side grouping architecture (+ подпункты); Server-side column filtering (+ подпункты); Filter tray; Сериализация и URL-персистенция фильтра; Локализация фильтра; Интеграция на странице | **Clayzor.Lib.Web.Controls/AGENTS.md** |
| Typography & Fonts; Style enforcement (Architecture Variant A, Build-time enforcement, checklist) | **Clayzor.App.Web.MedicalTests/AGENTS.md** |

## Карта /docs → куда уходит

| Файл | Назначение | Почему |
|---|---|---|
| clay-grid.md | Clayzor.Lib.Web.Controls/docs/ | справка компонента ClayGrid |
| clay-combo-box.md | Clayzor.Lib.Web.Controls/docs/ | компонент ClayComboBox |
| clay-edit-form.md | Clayzor.Lib.Web.Controls/docs/ | компонент ClayEditForm |
| clay-error-bar.md | Clayzor.Lib.Web.Controls/docs/ | компонент ClayErrorBar |
| clay-column-filter-dialog.md | Clayzor.Lib.Web.Controls/docs/ | компонент фильтра колонок |
| confirm-dialog.md | Clayzor.Lib.Web.Controls/docs/ | компонент ConfirmDialog |
| entity-crud.md | Clayzor.Lib.Entities/docs/ | про базовый класс Entity (Entities) |
| adding-new-entity.md | Clayzor.Lib.Entities/docs/ | воркфлоу добавления сущности (старт — Entities) |

---

## Промт D0 — план и подготовка

```
Мы разбиваем корневой AGENTS.md на per-project AGENTS.md и раскладываем /docs по папкам
сборок согласно DOCS_SPLIT_PROMPTS.md (две карты выше). Перед изменениями:
1. Работаем в ветке docs/split. Убедись, что сборка/тесты зелёные (точка отката).
2. Прочитай обе карты. Правило: ничего не терять и не дублировать; каждый раздел — в одно
   место; per-project AGENTS.md не повторяют глобальные правила корня.
3. Выведи предлагаемое дерево файлов ДО изменений и сверь его со мной, если что-то из
   разделов монолита не мапится однозначно.
```

## Промт D1 — разбить AGENTS.md

```
Разбей корневой AGENTS.md по карте разделов. Переноси разделы ДОСЛОВНО (не переписывай
содержание), только перенос + правка относительных ссылок.

1. Создай per-project AGENTS.md (новые файлы) и перенеси в них соответствующие разделы:
   - src/Clayzor.Lib.DALC/AGENTS.md ← Database access pattern, SQL constant naming
     convention, Rules, SQL Server 2008 R2 pagination.
   - src/Clayzor.Lib.Entities/AGENTS.md ← Entity CRUD & Lookup pattern, Adding a new entity.
   - src/Clayzor.Lib.Web.Controls/AGENTS.md ← весь блок Shared components и ниже до конца
     фильтрации (Интерфейсы, Services, Codebehind ClayGrid/ClayGridPageBase, Server-side
     grouping, Server-side column filtering, Filter tray, сериализация, локализация,
     интеграция на странице).
   - src/Clayzor.App.Web.MedicalTests/AGENTS.md ← Typography & Fonts, Style enforcement
     (Architecture Variant A, Build-time enforcement, checklist).
   - src/Clayzor.Lib.Web.Settings/AGENTS.md ← НОВЫЙ короткий стаб: 3–5 строк о роли сборки
     (классы настроек/конфигурации Clayzor) и ссылка на корневой раздел Configuration.
     (В монолите отдельного раздела под неё нет — не выдумывай технику, только роль+указатель.)
   В начале КАЖДОГО дочернего AGENTS.md добавь строку:
   «> Глобальные правила и обзор решения — в корневом /AGENTS.md. Здесь — только специфика
   проекта Clayzor.Lib.… .»

2. Из корневого AGENTS.md УДАЛИ перенесённые разделы. Оставь в нём только:
   Tradeoff-интро; 1–4 (Think/Simplicity/Surgical/Goal-Driven); Clayzor — Медицинские
   исследования; Agent instructions; Stack; Build & Run; Tests; Project dependency chain;
   Configuration — connection string priority; Key conventions.

3. В конец корневого AGENTS.md добавь секцию «## Map / где что искать» с относительными
   ссылками на каждый per-project AGENTS.md и на ключевые доки (после D2 пути будут
   src/<Project>/AGENTS.md и src/<Project>/docs/<file>.md).

4. Собери решение — на код это не влияет; проверь, что md валидны (заголовки, таблицы).
```

## Промт D2 — разложить /docs по сборкам и починить ссылки

```
Перемести файлы из /docs в docs-папки соответствующих сборок по карте /docs.

1. Создай папки и перемести (git mv, чтобы сохранить историю):
   → src/Clayzor.Lib.Web.Controls/docs/: clay-grid.md, clay-combo-box.md, clay-edit-form.md,
     clay-error-bar.md, clay-column-filter-dialog.md, confirm-dialog.md
   → src/Clayzor.Lib.Entities/docs/: entity-crud.md, adding-new-entity.md
2. Почини ВСЕ относительные ссылки:
   - в перенесённых доках (кросс-ссылки друг на друга и на код) — путь мог измениться;
   - в per-project и корневом AGENTS.md (nav-карта, любые ссылки вида docs/…);
   - если доки ссылались на исходники относительными путями — пересчитай от нового места.
3. Если после переноса папка /docs пуста — удали её. Если остались файлы, не попавшие в
   карту, — покажи их мне, не удаляй.
4. Прогони проверку битых ссылок (см. D3, шаг 2) — не должно быть ни одной.
```

## Промт D3 — проверка целостности

```
Убедись, что при разбиении ничего не потеряно, не задвоено и все ссылки живые.

1. Полнота и отсутствие дублей: сверь, что каждый заголовок исходного AGENTS.md
   присутствует РОВНО в одном из файлов (корневой + пять per-project). Ни одного раздела
   не потеряно и ни один не скопирован дважды.
2. Битые ссылки: пройди по всем .md (корень, src/**/AGENTS.md, src/**/docs/**) и проверь,
   что каждая относительная ссылка [..](..) указывает на существующий файл/якорь.
   Например: grep -rnoE "\]\([^)]+\.md[^)]*\)" . --include="*.md" | (проверить каждую цель).
3. Нейминг: в перемещённых/новых md нет слов kesco/BZ; упоминания фреймворка — Clayzor,
   компонентов — Clay*, css — clay*, namespaces/проекты — Clayzor.* .
4. Дерево совпадает с целевой раскладкой из DOCS_SPLIT_PROMPTS.md; /docs удалён (или показан
   остаток). Покажи финальное дерево md-файлов.
Готово, когда: все разделы размещены 1:1, битых ссылок нет, дерево соответствует целевому.
```
