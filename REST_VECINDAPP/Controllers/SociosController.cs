using Microsoft.AspNetCore.Mvc;
using REST_VECINDAPP.CapaNegocios;
using REST_VECINDAPP.Modelos;
using REST_VECINDAPP.Modelos.DTOs;
using REST_VECINDAPP.Seguridad;
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

        /// <summary>
        /// Endpoint para solicitar ser socio
        /// </summary>
        /// <param name="solicitud">Datos de la solicitud</param>
        /// <returns>Resultado de la operación</returns>
        [VerificarRol("vecino", "socio", "directiva")]
        [HttpPost("solicitar")]
        public IActionResult SolicitarMembresia([FromForm] SolicitudMembresia solicitud)
        {
            try
            {
                // Convertir el archivo de identidad a bytes
                byte[] documentoIdentidad;
                using (var ms = new MemoryStream())
                {
                    solicitud.DocumentoIdentidad.CopyTo(ms);
                    documentoIdentidad = ms.ToArray();
                }

                // Convertir el archivo de domicilio a bytes
                byte[] documentoDomicilio;
                using (var ms = new MemoryStream())
                {
                    solicitud.DocumentoDomicilio.CopyTo(ms);
                    documentoDomicilio = ms.ToArray();
                }

                cn_Socios cnSocios = new cn_Socios(_config);
                string resultado = cnSocios.SolicitarMembresia(solicitud.Rut, documentoIdentidad, documentoDomicilio);

                return Ok(new { mensaje = resultado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al procesar la solicitud", error = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint para consultar solicitudes de socio
        /// </summary>
        /// <param name="estado">Estado de las solicitudes a consultar</param>
        /// <returns>Lista de solicitudes</returns>
        [VerificarRol("directiva")]
        [HttpGet("solicitudes")]
        public IActionResult ConsultarSolicitudes([FromQuery] string estado = null)
        {
            try
            {
                cn_Socios cnSocios = new cn_Socios(_config);
                var solicitudes = cnSocios.ConsultarSolicitudes(estado);

                return Ok(solicitudes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al consultar solicitudes", error = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint para aprobar una solicitud de socio
        /// </summary>
        /// <param name="rut">RUT del solicitante</param>
        /// <returns>Resultado de la operación</returns>
        [VerificarRol("directiva")]
        [HttpPut("aprobar/{rut}")]
        public IActionResult AprobarSolicitud(int rut)
        {
            try
            {
                cn_Socios cnSocios = new cn_Socios(_config);
                string resultado = cnSocios.AprobarSolicitud(rut);

                return Ok(new { mensaje = resultado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al aprobar la solicitud", error = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint para rechazar una solicitud de socio
        /// </summary>
        /// <param name="rut">RUT del solicitante</param>
        /// <param name="datos">Datos del rechazo</param>
        /// <returns>Resultado de la operación</returns>
        [VerificarRol("directiva")]
        [HttpPut("rechazar/{rut}")]
        public IActionResult RechazarSolicitud(int rut, [FromBody] RechazoDTO datos)
        {
            try
            {
                cn_Socios cnSocios = new cn_Socios(_config);
                string resultado = cnSocios.RechazarSolicitud(rut, datos.MotivoRechazo);

                return Ok(new { mensaje = resultado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al rechazar la solicitud", error = ex.Message });
            }
        }
    }
}