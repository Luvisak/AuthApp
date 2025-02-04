using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Cargar clave de cifrado desde appsettings.json
var encryptionKey = builder.Configuration["CookieSettings:EncryptionKey"];
if (string.IsNullOrEmpty(encryptionKey))
{
    throw new Exception("La clave de cifrado no está configurada en appsettings.json");
}
var sharedKey = Convert.FromBase64String(encryptionKey);

builder.Services.AddDataProtection()
    .SetApplicationName("SharedAuthApp") // 🔹 Clave compartida entre aplicaciones
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\SharedKeys")) // 🔹 Claves persistentes (solo para pruebas locales)
    .ProtectKeysWithDpapi(); // 🔹 Protege las claves con DPAPI en Windows

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "AuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Domain = "localhost"; // 🔹 Permitir compartir cookies entre AuthApp y CookieReaderApp
    });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}"
);

app.Run();
