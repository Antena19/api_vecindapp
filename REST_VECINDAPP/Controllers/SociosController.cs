using Microsoft.AspNetCore.Mvc;
using REST_VECINDAPP.CapaNegocios;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace REST_VECINDAPP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SociosController : ControllerBase
    {
        private readonly IConfiguration _config;
        public SociosController(IConfiguration configuration)
        {
            _config = configuration;
        }

        // GET api/<SociosController>/5
        [HttpGet]
        public ActionResult Get(int idsocio)
        {
            cn_Socios cnSocios = new cn_Socios(_config);
            return Ok(cnSocios.ListarSocios(idsocio));
        }
    }
}
