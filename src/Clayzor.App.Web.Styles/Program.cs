using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var stylesPath = builder.Configuration["Styles:PhysicalPath"] ?? @"C:\CLAYZOR-KIMI\Styles";
if (!Directory.Exists(stylesPath))
    throw new DirectoryNotFoundException($"Каталог стилей не найден: {stylesPath}");

var app = builder.Build();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(stylesPath),
    RequestPath = "/styles",
});
app.MapGet("/", () => Results.Text("Clayzor Styles"));
app.Run("http://localhost:5040");
