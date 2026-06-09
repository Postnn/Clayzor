using System.Data;
using Kesco.Lib.DALC;

namespace Kesco.Lib.Entities;

/// <summary>
/// Базовый класс сущности со стандартными CRUD-операциями.
/// Производные классы переопределяют SQL-константы и свойство <see cref="Id"/>.
/// </summary>
public abstract class Entity
{
    /// <summary>Первичный ключ сущности.</summary>
    public abstract int Id { get; set; }

    /// <summary>SQL-запрос SELECT для выборки сущностей.</summary>
    protected abstract string SelectSql { get; }

    /// <summary>SQL-запрос INSERT для добавления сущности.</summary>
    protected abstract string InsertSql { get; }

    /// <summary>SQL-запрос UPDATE для обновления сущности.</summary>
    protected abstract string UpdateSql { get; }

    /// <summary>SQL-запрос DELETE для удаления сущности по <see cref="Id"/>.</summary>
    protected abstract string DeleteSql { get; }

    /// <summary>
    /// Выполняет SELECT с динамическими WHERE и ORDER BY.
    /// Вызывается из статического метода GetAllAsync производного класса.
    /// </summary>
    protected static async Task<IEnumerable<T>> GetAllAsync<T>(
        DbManager db, string selectSql,
        string? whereClause, string? orderByClause, object? param)
        where T : Entity
    {
        var sql = selectSql;
        if (whereClause is not null)
            sql += $" WHERE {whereClause}";
        if (orderByClause is not null)
            sql += $" ORDER BY {orderByClause}";
        return await db.QueryAsync<T>(sql, param);
    }

    /// <summary>
    /// Выполняет простой SELECT без динамических WHERE/ORDER BY.
    /// Вызывается из статического метода GetAllAsync производного класса.
    /// </summary>
    protected static async Task<IEnumerable<T>> GetAllSimpleAsync<T>(DbManager db, string selectSql)
        where T : Entity
    {
        return await db.QueryAsync<T>(selectSql);
    }

    /// <summary>
    /// Выполняет SELECT с постраничной выборкой через OFFSET/FETCH.
    /// </summary>
    protected static async Task<IEnumerable<T>> GetPagedAsync<T>(
        DbManager db, string selectSql,
        string? whereClause, string? orderByClause, object? param,
        int pageNumber, int pageSize)
        where T : Entity
    {
        var sql = selectSql;
        if (whereClause is not null)
            sql += $" WHERE {whereClause}";
        if (orderByClause is not null)
            sql += $" ORDER BY {orderByClause}";
        sql += $" OFFSET {(pageNumber - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
        return await db.QueryAsync<T>(sql, param);
    }

    /// <summary>
    /// Возвращает общее количество записей, соответствующих фильтру.
    /// Оборачивает SELECT в подзапрос COUNT(*).
    /// </summary>
    protected static async Task<int> GetCountAsync<T>(
        DbManager db, string selectSql,
        string? whereClause, object? param)
        where T : Entity
    {
        var sql = $"SELECT COUNT(*) FROM ({selectSql}";
        if (whereClause is not null)
            sql += $" WHERE {whereClause}";
        sql += ") AS _cnt";
        return await db.ExecuteScalarAsync<int>(sql, param, commandType: System.Data.CommandType.Text);
    }

    /// <summary>
    /// Добавляет сущность в БД.
    /// </summary>
    /// <param name="db">Менеджер подключения к БД.</param>
    public async Task InsertAsync(DbManager db)
    {
        await db.ExecuteAsync(InsertSql, this, commandType: CommandType.Text);
    }

    /// <summary>
    /// Обновляет сущность в БД.
    /// </summary>
    /// <param name="db">Менеджер подключения к БД.</param>
    public async Task UpdateAsync(DbManager db)
    {
        await db.ExecuteAsync(UpdateSql, this, commandType: CommandType.Text);
    }

    /// <summary>
    /// Удаляет сущность из БД по <see cref="Id"/>.
    /// </summary>
    /// <param name="db">Менеджер подключения к БД.</param>
    public async Task DeleteAsync(DbManager db)
    {
        await db.ExecuteAsync(DeleteSql, new { Id }, commandType: CommandType.Text);
    }
}
