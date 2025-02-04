using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.DirectoryServices.AccountManagement;

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
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.AuthenticationMethod, "LDAP"),
                    new Claim(ClaimTypes.Role, "User") 
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
