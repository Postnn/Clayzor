using Kesco.App.Web.BZ.MedicalTests.Components;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities.MedicalTests;
using Kesco.Lib.Web.Settings;
using Microsoft.AspNetCore.Authentication.Negotiate;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Options;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация: appsettings.json + web.config
builder.Configuration.AddWebConfig();
var kescoSettings = builder.Configuration.BindKescoSettings();
builder.Services.AddSingleton(kescoSettings);

// Dapper column mapping
DapperColumnMapper.Initialize();

// DALC: scoped (одно подключение на запрос)
builder.Services.AddScoped<DbManager>(_ => new DbManager(kescoSettings.ConnectionString));

// Аутентификация: Windows (Kerberos/NTLM)
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

// Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// MudBlazor
builder.Services.AddMudServices();
builder.Services.AddMudExtensions(cfg => cfg.WithDefaultDialogOptions(d => d.DragMode = MudDialogDragMode.Simple));

// HttpContext для User.Identity
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
