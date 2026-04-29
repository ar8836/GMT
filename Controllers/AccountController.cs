using Microsoft.AspNetCore.Mvc;
using GMT.Models;
using System.Linq;

namespace GMT.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Portal de acceso (login + registro)
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // Muestra la página de Login (redirigir al portal)
        [HttpGet]
        public IActionResult Login()
        {
            return RedirectToAction("Index");
        }

        // Procesa los datos del formulario
        [HttpPost]
        public IActionResult Login(string correo, string password)
        {
            // 1. Buscar al alumno en la tabla de login
            var cuenta = _context.Logins.FirstOrDefault(u => u.CorreoInstitucional.ToLower() == correo.ToLower());

            // 2. Validar (Ahorita en texto plano, luego le ponemos Hash)
            if (cuenta != null && cuenta.PasswordHash == password)
            {
                // ¡Éxito! Lo mandamos al Home
                return RedirectToAction("Index", "Home");
            }

            // 3. Si falla, lo regresamos al Login con un mensaje de error
            ViewBag.Error = "Credenciales incorrectas. Solo alumnos autorizados.";
            return View();
        }
    }
}
