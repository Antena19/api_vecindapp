using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using REST_VECINDAPP.CapaNegocios;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace REST_VECINDAPP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly IConfiguration _config;
        public UsuariosController(IConfiguration configuration)
        {
            _config = configuration;
        }

        [Authorize]//Esta linea indica que se requiere token valido para funcionar.
        // GET api/<UsuariosController>/5
        [HttpGet]
        public ActionResult Get(int rut)
        {
            cn_Usuarios cnUsuarios=new cn_Usuarios(_config);
            return Ok(cnUsuarios.ListarUsuarios(rut));
        }        
    }
}
