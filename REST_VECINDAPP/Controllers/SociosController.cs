using Microsoft.AspNetCore.Mvc;
using REST_VECINDAPP.CapaNegocios;
using REST_VECINDAPP.Modelos;
using System.Collections.Generic;

namespace REST_VECINDAPP.Controllers
{
    /// <summary>
    /// Controlador para gestionar las operaciones relacionadas con los socios de la junta de vecinos
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SociosController : ControllerBase
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// Constructor del controlador de socios
        /// </summary>
        /// <param name="configuration">Configuración de la aplicación</param>
        public SociosController(IConfiguration configuration)
        {
            _config = configuration;
        }

        /// <summary>
        /// Obtiene un socio específico o todos los socios
        /// </summary>
        /// <param name="idsocio">ID del socio a buscar. Si es -1 o no se proporciona, devuelve todos los socios</param>
        /// <returns>Lista de socios o un socio específico</returns>
        [HttpGet]
        public ActionResult<List<Socio>> Get([FromQuery] int idsocio = -1)
        {
            cn_Socios cnSocios = new cn_Socios(_config);
            return Ok(cnSocios.ListarSocios(idsocio));
        }
    }
}
