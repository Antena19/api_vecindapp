using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using REST_VECINDAPP.CapaNegocios;
using REST_VECINDAPP.Modelos;
using System.Collections.Generic;

namespace REST_VECINDAPP.Controllers
{
    /// <summary>
    /// Controlador para gestionar las operaciones relacionadas con los usuarios de la aplicación
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// Constructor del controlador de usuarios
        /// </summary>
        /// <param name="configuration">Configuración de la aplicación</param>
        public UsuariosController(IConfiguration configuration)
        {
            _config = configuration;
        }

        /// <summary>
        /// Obtiene un usuario específico o todos los usuarios
        /// Requiere autenticación (token JWT válido)
        /// </summary>
        /// <param name="rut">RUT del usuario a buscar. Si es -1 o no se proporciona, devuelve todos los usuarios</param>
        /// <returns>Lista de usuarios o un usuario específico</returns>
        [Authorize] // Esta línea indica que se requiere token válido para funcionar
        [HttpGet]
        public ActionResult<List<Usuario>> Get([FromQuery] int rut = -1)
        {
            // Crea una instancia de la capa de negocio para usuarios
            cn_Usuarios cnUsuarios = new cn_Usuarios(_config);

            // Obtiene los usuarios según el parámetro de RUT y retorna el resultado
            return Ok(cnUsuarios.ListarUsuarios(rut));
        }
    }
}