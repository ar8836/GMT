using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GMT.Controllers
{
    public class DashboardController : Controller
    {
            [Authorize]
            public IActionResult Alumno()
            {
                return View("Alumno/Index");
            }

        [Authorize]
        public IActionResult Empresa()
        {
            return View("Empresa/Index");
        }

        // AGREGAR ESTO: Acción para Peticiones
        [Authorize]
        public IActionResult Peticiones()
        {
            // Especifica la ruta relativa si está dentro de la carpeta Alumno
            return View("Alumno/Peticiones");
        }

        // AGREGAR ESTO: Acción para Perfil Académico
        [Authorize]
        public IActionResult PerfilAcademico()
        {
            return View("Alumno/PerfilAcademico");
        }
    }
}