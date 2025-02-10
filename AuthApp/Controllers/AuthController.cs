using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;

namespace AuthApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly string _dominio = "SEALSCDC01.seal.com.pe";

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (AutenticarUsuario(username, password))
            {
                // Obtener información del usuario desde Active Directory
                var userDetails = ObtenerInformacionUsuario(username);

                // Asegurar que los datos existen en el diccionario, usando GetValueOrDefault()
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, userDetails.GetValueOrDefault("Correo Electrónico", "")),
            new Claim(ClaimTypes.MobilePhone, userDetails.GetValueOrDefault("Teléfono", "")),
            new Claim(ClaimTypes.GivenName, userDetails.GetValueOrDefault("Nombre Completo", "")),
            new Claim("Departamento", userDetails.GetValueOrDefault("Departamento", "")),
            new Claim("Cargo", userDetails.GetValueOrDefault("Título", "")),
            new Claim("Empresa", userDetails.GetValueOrDefault("Empresa", ""))
        };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                return RedirectToAction("Dashboard", "Home");
            }

            ViewBag.Error = "Usuario o contraseña incorrectos.";
            return View();
        }

        // Nueva función para obtener información del usuario
        private Dictionary<string, string> ObtenerInformacionUsuario(string username)
        {
            var userDetails = new Dictionary<string, string>();

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _dominio))
                {
                    UserPrincipal user = UserPrincipal.FindByIdentity(context, username);
                    if (user != null)
                    {
                        userDetails["Nombre Completo"] = user.DisplayName ?? "";
                        userDetails["Correo Electrónico"] = user.EmailAddress ?? "";
                        userDetails["Teléfono"] = user.VoiceTelephoneNumber ?? "";
                        userDetails["Descripción"] = user.Description ?? "";

                        using (DirectorySearcher searcher = new DirectorySearcher(new DirectoryEntry()))
                        {
                            searcher.Filter = $"(samaccountname={username})";
                            string[] attributes = { "department", "title", "company" };

                            foreach (var attr in attributes)
                            {
                                searcher.PropertiesToLoad.Add(attr);
                            }

                            SearchResult result = searcher.FindOne();
                            if (result != null)
                            {
                                userDetails["Departamento"] = result.Properties["department"].Count > 0 ? result.Properties["department"][0].ToString() : "";
                                userDetails["Título"] = result.Properties["title"].Count > 0 ? result.Properties["title"][0].ToString() : "";
                                userDetails["Empresa"] = result.Properties["company"].Count > 0 ? result.Properties["company"][0].ToString() : "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                userDetails["Error"] = ex.Message;
            }

            return userDetails;
        }

        private bool AutenticarUsuario(string usuario, string contraseña)
        {
            try
            {
                using (var contexto = new PrincipalContext(ContextType.Domain, _dominio))
                {
                    return contexto.ValidateCredentials(usuario, contraseña);
                }
            }
            catch (PrincipalServerDownException ex)
            {
                Console.WriteLine("❌ No se pudo conectar al servidor de Active Directory.");
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }
    }
}