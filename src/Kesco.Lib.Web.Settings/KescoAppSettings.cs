using Microsoft.Extensions.Configuration;

namespace Kesco.Lib.Web.Settings;

/// <summary>
/// Настройки приложения Kesco BZ.
/// Связываются из секции "KescoApp" конфигурации (appsettings.json + web.config).
/// </summary>
public class KescoAppSettings
{
    /// <summary>Строка подключения к основной БД.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Строка подключения к БД справочников (опционально).</summary>
    public string? DictionaryConnectionString { get; set; }

    /// <summary>Наименование приложения.</summary>
    public string ApplicationName { get; set; } = "Kesco BZ";

    /// <summary>Путь к LDAP для аутентификации (опционально).</summary>
    public string? LdapPath { get; set; }

    /// <summary>Размер страницы по умолчанию для таблиц данных.</summary>
    public int DefaultPageSize { get; set; } = 50;

    /// <summary>Таймаут команд БД в секундах.</summary>
    public int CommandTimeout { get; set; } = 30;
}

/// <summary>
/// Методы расширения для загрузки конфигурации из web.config и связывания в <see cref="KescoAppSettings"/>.
/// </summary>
public static class WebConfigExtensions
{
    /// <summary>
    /// Добавляет web.config как дополнительный источник XML-конфигурации.
    /// Вызывается перед BindKescoSettings().
    /// </summary>
    /// <param name="builder">Построитель конфигурации.</param>
    /// <param name="path">Путь к web.config (по умолчанию — текущая директория).</param>
    /// <returns>Построитель конфигурации для цепочки вызовов.</returns>
    public static IConfigurationBuilder AddWebConfig(this IConfigurationBuilder builder, string? path = null)
    {
        var webConfigPath = path ?? Path.Combine(Directory.GetCurrentDirectory(), "web.config");
        if (File.Exists(webConfigPath))
            builder.AddXmlFile(webConfigPath, optional: true, reloadOnChange: true);
        return builder;
    }

    /// <summary>
    /// Связывает секцию "KescoApp" в <see cref="KescoAppSettings"/>.
    /// Если ConnectionString не задана в секции, используется ConnectionStrings:DefaultConnection.
    /// Если DictionaryConnectionString не задана, используется ConnectionStrings:DictionaryConnection.
    /// </summary>
    /// <param name="configuration">Корневая конфигурация приложения.</param>
    /// <returns>Заполненный объект <see cref="KescoAppSettings"/>.</returns>
    public static KescoAppSettings BindKescoSettings(this IConfiguration configuration)
    {
        var settings = new KescoAppSettings();
        configuration.GetSection("KescoApp").Bind(settings);

        if (string.IsNullOrEmpty(settings.ConnectionString))
            settings.ConnectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        if (string.IsNullOrEmpty(settings.DictionaryConnectionString))
            settings.DictionaryConnectionString = configuration.GetConnectionString("DictionaryConnection");

        return settings;
    }
}
