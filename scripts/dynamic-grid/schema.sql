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
