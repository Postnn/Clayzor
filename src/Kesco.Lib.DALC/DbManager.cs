using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;

namespace Kesco.Lib.DALC;

/// <summary>
/// Менеджер подключения к SQL Server через Dapper.
/// Управляет ленивым открытием <see cref="SqlConnection"/> и предоставляет методы для выполнения запросов.
/// Регистрируется как Scoped (одно подключение на HTTP-запрос).
/// </summary>
public class DbManager : IDisposable
{
    private readonly string _connectionString;
    private SqlConnection? _connection;

    /// <summary>
    /// Создаёт экземпляр <see cref="DbManager"/> с указанной строкой подключения.
    /// </summary>
    /// <param name="connectionString">Строка подключения к SQL Server.</param>
    public DbManager(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Ленивое подключение к SQL Server. Открывается при первом обращении, повторно используется в рамках скоупа.
    /// </summary>
    public SqlConnection Connection
    {
        get
        {
            if (_connection is null)
                _connection = new SqlConnection(_connectionString);
            if (_connection.State != ConnectionState.Open)
                _connection.Open();
            return _connection;
        }
    }

    /// <summary>
    /// Выполняет хранимую процедуру и возвращает коллекцию сущностей.
    /// </summary>
    /// <typeparam name="T">Тип сущности.</typeparam>
    /// <param name="storedProcName">Имя хранимой процедуры.</param>
    /// <param name="parameters">Параметры запроса (анонимный объект или <see cref="Dapper.DynamicParameters"/>).</param>
    /// <param name="commandTimeout">Таймаут команды (сек).</param>
    /// <returns>Коллекция сущностей типа <typeparamref name="T"/>.</returns>
    public async Task<IEnumerable<T>> QueryStoredProcAsync<T>(string storedProcName, object? parameters = null, int? commandTimeout = null)
    {
        return await Connection.QueryAsync<T>(storedProcName, parameters, commandType: CommandType.StoredProcedure, commandTimeout: commandTimeout);
    }

    /// <summary>
    /// Выполняет raw SQL-запрос на выборку и возвращает коллекцию сущностей.
    /// </summary>
    /// <typeparam name="T">Тип сущности.</typeparam>
    /// <param name="sql">SQL-запрос (SELECT).</param>
    /// <param name="parameters">Параметры запроса (анонимный объект или <see cref="Dapper.DynamicParameters"/>).</param>
    /// <param name="commandTimeout">Таймаут команды (сек).</param>
    /// <returns>Коллекция сущностей типа <typeparamref name="T"/>.</returns>
    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null, int? commandTimeout = null)
    {
        return await Connection.QueryAsync<T>(sql, parameters, commandTimeout: commandTimeout);
    }

    /// <summary>
    /// Выполняет хранимую процедуру и возвращает скалярное значение.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
    /// <param name="storedProcName">Имя хранимой процедуры.</param>
    /// <param name="parameters">Параметры запроса.</param>
    /// <param name="commandType">Тип команды (по умолчанию StoredProcedure).</param>
    /// <returns>Значение типа <typeparamref name="T"/> или default.</returns>
    public async Task<T?> ExecuteScalarAsync<T>(string storedProcName, object? parameters = null, CommandType commandType = CommandType.StoredProcedure)
    {
        return await Connection.ExecuteScalarAsync<T>(storedProcName, parameters, commandType: commandType);
    }

    /// <summary>
    /// Выполняет команду (INSERT, UPDATE, DELETE) или хранимую процедуру.
    /// </summary>
    /// <param name="storedProcName">SQL-запрос или имя хранимой процедуры.</param>
    /// <param name="parameters">Параметры запроса.</param>
    /// <param name="commandType">Тип команды (по умолчанию StoredProcedure).</param>
    /// <returns>Количество затронутых строк.</returns>
    public async Task<int> ExecuteAsync(string storedProcName, object? parameters = null, CommandType commandType = CommandType.StoredProcedure)
    {
        return await Connection.ExecuteAsync(storedProcName, parameters, commandType: commandType);
    }

    /// <summary>
    /// Закрывает и освобождает подключение.
    /// </summary>
    public void Dispose()
    {
        if (_connection is not null)
        {
            if (_connection.State != ConnectionState.Closed)
                _connection.Close();
            _connection.Dispose();
            _connection = null;
        }
    }
}
