-- ============================================================================
-- Dynamic Grid — dev/test schema
-- SQL Server 2008 R2 compatible
-- Applies to: LocalDB (dev/test workstation only, NOT production)
-- ============================================================================

-- 1. Grid definitions (п.2 спецификации)
IF OBJECT_ID('dbo.ClayGridSettings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClayGridSettings (
        КодЗапроса          int           NOT NULL PRIMARY KEY,
        Запрос              varchar(50)   NULL,
        Пиктограмма         varchar(50)   NULL,
        [SQL]               varchar(4000) NULL,
        ID                  varchar(50)   NULL,
        IDName              varchar(50)   NULL,
        ФормаРедактирования varchar(100)  NULL,
        ФормаНового         varchar(100)  NULL,
        SQLDelete           varchar(300)  NULL
    );
END
GO

-- 2. Column definitions (п.3 спецификации)
IF OBJECT_ID('dbo.ClayGridColumns', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClayGridColumns (
        КодКолонки                int           NOT NULL PRIMARY KEY,
        КодЗапроса                 int           NOT NULL,
        Колонка                    varchar(50)   NULL,
        ЗаголовокКолонки           varchar(50)   NULL,
        КлючURL                    varchar(50)   NULL,
        Порядок                    int           NULL,
        Формат                     varchar(2000) NULL,
        Тип                        int           NULL,
        УчаствуетВБыстромПоиске    tinyint       NULL
    );
END
GO

-- Add quick-search column on existing DBs (idempotent)
IF COL_LENGTH('dbo.ClayGridColumns', 'УчаствуетВБыстромПоиске') IS NULL
    ALTER TABLE dbo.ClayGridColumns ADD УчаствуетВБыстромПоиске tinyint NULL;
GO

-- 3. User params with upsert trigger (п.4 спецификации)
IF OBJECT_ID('dbo.ClayGridUserParams', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClayGridUserParams (
        КодНастройкиКлиента int           NOT NULL,
        Параметр            varchar(20)   NOT NULL,
        Значение            varchar(1000) NULL,
        CONSTRAINT UQ_ClayGridUserParams UNIQUE (КодНастройкиКлиента, Параметр)
    );
END
GO

-- Instead-of-insert trigger: application sends ONLY INSERT;
-- trigger performs upsert (update if exists, insert otherwise).
-- Set-based — handles multi-row inserts correctly.
IF OBJECT_ID('dbo.TR_ClayGridUserParams_Upsert', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_ClayGridUserParams_Upsert;
GO

CREATE TRIGGER dbo.TR_ClayGridUserParams_Upsert
ON dbo.ClayGridUserParams
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Update rows where the key already exists
    UPDATE tgt
    SET tgt.Значение = src.Значение
    FROM dbo.ClayGridUserParams AS tgt
    INNER JOIN inserted AS src
        ON tgt.КодНастройкиКлиента = src.КодНастройкиКлиента
       AND tgt.Параметр            = src.Параметр;

    -- Insert rows that don't exist yet
    INSERT INTO dbo.ClayGridUserParams (КодНастройкиКлиента, Параметр, Значение)
    SELECT src.КодНастройкиКлиента, src.Параметр, src.Значение
    FROM inserted AS src
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.ClayGridUserParams AS tgt
        WHERE tgt.КодНастройкиКлиента = src.КодНастройкиКлиента
          AND tgt.Параметр            = src.Параметр
    );
END
GO

-- ============================================================================
-- Seed data: grid #140 — Медицинские исследования
-- ============================================================================

-- Grid definition
IF NOT EXISTS (SELECT 1 FROM dbo.ClayGridSettings WHERE КодЗапроса = 140)
BEGIN
    INSERT INTO dbo.ClayGridSettings (КодЗапроса, Запрос, Пиктограмма, [SQL], ID, IDName, ФормаРедактирования, ФормаНового, SQLDelete)
    VALUES (
        140,
        N'Медицинские исследования',
        NULL,
        N'SELECT КодИсследования, Название, ДатаСоздания, КодТипа, Активно FROM Исследования',
        N'КодИсследования',
        N'Название',
        N'/medical/edit',
        N'/medical/new',
        N'DELETE FROM Исследования WHERE КодИсследования=@id'
    );
END
GO

-- Columns (5 rows)
IF NOT EXISTS (SELECT 1 FROM dbo.ClayGridColumns WHERE КодЗапроса = 140)
BEGIN
    INSERT INTO dbo.ClayGridColumns (КодКолонки, КодЗапроса, Колонка, ЗаголовокКолонки, КлючURL, Порядок, Формат, Тип, УчаствуетВБыстромПоиске)
    VALUES
        (1001, 140, N'КодИсследования', N'№',              N'id',      1, NULL,                                                       1, 1),
        (1002, 140, N'Название',        N'Название',        N'name',    2, NULL,                                                       2, 1),
        (1003, 140, N'ДатаСоздания',    N'Создано',         N'created', 3, N'dd.MM.yyyy',                                               3, 1),
        (1004, 140, N'КодТипа',         N'Тип исследования', N'type',    4, N'SELECT КодТипа, Наименование FROM Типы ORDER BY Наименование', 5, 1),
        (1005, 140, N'Активно',         N'Активно',         N'active',  0, N'Активно=1',                                                7, 0);
END
ELSE
    -- Update quick-search flag for existing seed rows (idempotent)
    UPDATE dbo.ClayGridColumns SET УчаствуетВБыстромПоиске = c.Val
    FROM (VALUES
        (1001, 1), (1002, 1), (1003, 1), (1004, 1), (1005, 0)
    ) AS c(КодКолонки, Val)
    WHERE dbo.ClayGridColumns.КодКолонки = c.КодКолонки AND КодЗапроса = 140;
GO

-- ============================================================================
-- Shared params — «Поделиться» настройками грида (серия SH)
-- ============================================================================

-- 1. Shared params table
IF OBJECT_ID('dbo.ClayGridUserSharedParams', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClayGridUserSharedParams (
        КодНастройкиОбщей int IDENTITY(1,1) NOT NULL PRIMARY KEY, -- идентификатор общей настройки
        Название nvarchar(100) NOT NULL                            -- название ссылки, показывается пользователю
    );
END
GO

-- 1a. Sentinel record: КодНастройкиОбщей = 0 means personal (non-shared) params.
-- Required for FK — without it, all existing personal rows would violate the constraint.
IF NOT EXISTS (SELECT 1 FROM dbo.ClayGridUserSharedParams WHERE КодНастройкиОбщей = 0)
BEGIN
    SET IDENTITY_INSERT dbo.ClayGridUserSharedParams ON;
    INSERT INTO dbo.ClayGridUserSharedParams (КодНастройкиОбщей, Название)
        VALUES (0, N'(личные настройки)');
    SET IDENTITY_INSERT dbo.ClayGridUserSharedParams OFF;
END
GO

-- 2. New column in ClayGridUserParams: shared param reference.
-- 0 = personal setting (default for all existing rows).
IF COL_LENGTH('dbo.ClayGridUserParams', 'КодНастройкиОбщей') IS NULL
    ALTER TABLE dbo.ClayGridUserParams
        ADD КодНастройкиОбщей int NOT NULL
            CONSTRAINT DF_ClayGridUserParams_КодНастройкиОбщей DEFAULT 0;
GO

-- 3. Foreign key — NO CASCADE.
-- Sentinel record 0 makes CASCADE dangerous: a single DELETE WHERE КодНастройкиОбщей = 0
-- would silently wipe ALL personal params for ALL users across ALL grids.
IF OBJECT_ID('dbo.FK_ClayGridUserParams_SharedParams', 'F') IS NULL
    ALTER TABLE dbo.ClayGridUserParams
        ADD CONSTRAINT FK_ClayGridUserParams_SharedParams
        FOREIGN KEY (КодНастройкиОбщей)
        REFERENCES dbo.ClayGridUserSharedParams (КодНастройкиОбщей);
GO

-- 4. Rebuild UNIQUE constraint: add КодНастройкиОбщей to the key.
-- Without it, a user can only have ONE row per param name — second «share» would
-- violate uniqueness.
IF OBJECT_ID('dbo.UQ_ClayGridUserParams', 'UQ') IS NOT NULL
    ALTER TABLE dbo.ClayGridUserParams DROP CONSTRAINT UQ_ClayGridUserParams;
GO

IF OBJECT_ID('dbo.UQ_ClayGridUserParams', 'UQ') IS NULL
    ALTER TABLE dbo.ClayGridUserParams
        ADD CONSTRAINT UQ_ClayGridUserParams
        UNIQUE (КодНастройкиКлиента, Параметр, КодНастройкиОбщей);
GO

-- 5. Rewrite upsert trigger: match on all three key columns.
-- Old trigger matched only on (КодНастройкиКлиента, Параметр) — saving a personal
-- param would overwrite the shared param value, silently corrupting sent links.
IF OBJECT_ID('dbo.TR_ClayGridUserParams_Upsert', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_ClayGridUserParams_Upsert;
GO

CREATE TRIGGER dbo.TR_ClayGridUserParams_Upsert
ON dbo.ClayGridUserParams
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Update existing rows. КодНастройкиОбщей is part of the key:
    -- 0 — personal setting, non-zero — shared setting.
    UPDATE tgt
       SET tgt.Значение = src.Значение
      FROM dbo.ClayGridUserParams AS tgt
     INNER JOIN inserted AS src
        ON tgt.КодНастройкиКлиента = src.КодНастройкиКлиента
       AND tgt.Параметр            = src.Параметр
       AND tgt.КодНастройкиОбщей   = src.КодНастройкиОбщей;

    -- Insert new rows.
    INSERT INTO dbo.ClayGridUserParams (КодНастройкиКлиента, Параметр, Значение, КодНастройкиОбщей)
    SELECT src.КодНастройкиКлиента, src.Параметр, src.Значение, src.КодНастройкиОбщей
      FROM inserted AS src
     WHERE NOT EXISTS (
               SELECT 1
                 FROM dbo.ClayGridUserParams AS tgt
                WHERE tgt.КодНастройкиКлиента = src.КодНастройкиКлиента
                  AND tgt.Параметр            = src.Параметр
                  AND tgt.КодНастройкиОбщей   = src.КодНастройкиОбщей
           );
END
GO

-- 5a. Table-valued function: read shared params by sharedId.
-- Contract: @КодНастройкиОбщей int → Параметр varchar(50), Значение nvarchar(MAX).
-- Filters out 0 intentionally: 0 is the sentinel for personal params.
-- No filter on КодНастройкиКлиента — a shared link is opened by a DIFFERENT user.
IF OBJECT_ID('dbo.ClayGridUserParamsShared', 'IF') IS NOT NULL
    DROP FUNCTION dbo.ClayGridUserParamsShared;
GO

CREATE FUNCTION dbo.ClayGridUserParamsShared (@КодНастройкиОбщей int)
RETURNS TABLE
AS
RETURN
(
    SELECT CAST(Параметр AS varchar(50))    AS Параметр,  -- param name
           CAST(Значение AS nvarchar(MAX))  AS Значение   -- saved param value
      FROM dbo.ClayGridUserParams
     WHERE КодНастройкиОбщей = @КодНастройкиОбщей
       AND @КодНастройкиОбщей <> 0
);
GO

-- 6. Seed: one shared setting with a couple of params for grid #140,
-- so SH7 (shared list) and SH8 (sharedId mode) can be tested without
-- going through the full UI flow.
IF NOT EXISTS (SELECT 1 FROM dbo.ClayGridUserSharedParams WHERE КодНастройкиОбщей = 1)
BEGIN
    SET IDENTITY_INSERT dbo.ClayGridUserSharedParams ON;

    INSERT INTO dbo.ClayGridUserSharedParams (КодНастройкиОбщей, Название)
        VALUES (1, N'Тестовая общая настройка — грид 140');

    SET IDENTITY_INSERT dbo.ClayGridUserSharedParams OFF;

    -- Seed params for shared setting #1: columns and sort state for grid 140
    INSERT INTO dbo.ClayGridUserParams (КодНастройкиКлиента, Параметр, Значение, КодНастройкиОбщей)
    VALUES
        (0, N'cols140', N'КодИсследования=1,Название=1,ДатаСоздания=1,КодТипа=1,Активно=1', 1),
        (0, N'srt140',  N'КодИсследования:ASC', 1);
END
GO
