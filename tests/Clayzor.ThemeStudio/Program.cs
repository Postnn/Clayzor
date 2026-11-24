using Clayzor.Lib.DALC;
using Clayzor.Lib.Web.Controls;
using Clayzor.Lib.Web.Controls.Services;
using Clayzor.Lib.Web.Controls.Themes;
using Clayzor.Lib.Web.Settings;
using Clayzor.ThemeStudio.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddScoped<ClayThemeState>();
builder.Services.AddClayGridDynamic(builder.Configuration);
builder.Services.AddClayTree(builder.Configuration);
builder.Services.AddSingleton(new ClayAppSettings());
builder.Services.AddScoped<ClayErrorService>();
builder.Services.AddScoped<ISqlErrorHandler>(services => services.GetRequiredService<ClayErrorService>());
builder.Services.AddScoped(_ => new DbManager(string.Empty));

var app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
