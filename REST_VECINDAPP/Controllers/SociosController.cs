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
        [HttpGet("estadisticas")]
        public IActionResult ObtenerEstadisticas()
        {
            try
            {
                cn_Socios cnSocios = new cn_Socios(_config);
                var resultado = cnSocios.ObtenerEstadisticas();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                // Capturar más detalles del error
                string errorMessage = ex.Message;
                string stackTrace = ex.StackTrace;
                string innerExceptionMessage = ex.InnerException?.Message;

                // Log el error completo
                Console.WriteLine($"Error: {errorMessage}\nStack: {stackTrace}\nInner: {innerExceptionMessage}");

                return StatusCode(500, new
                {
                    mensaje = "Error al obtener estadísticas",
                    error = errorMessage,
                    detalles = innerExceptionMessage
                });
            }
        }

        [HttpGet("activos")]
        [VerificarRol("directiva")]
        public IActionResult ObtenerSociosActivos()
        {
            try
            {
                cn_Socios cnSocios = new cn_Socios(_config);
                var socios = cnSocios.ObtenerSociosActivos();
                return Ok(socios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener socios activos", error = ex.Message });
            }
        }

        [HttpPut("{id}/estado")]
        public IActionResult ActualizarEstadoSocio(int id, [FromBody] ActualizarEstadoSocioDTO datos)
        {
            try
            {
                // Validar que el motivo sea requerido si se desactiva
                if (datos.Estado == 0 && string.IsNullOrWhiteSpace(datos.Motivo))
                {
                    return BadRequest(new { mensaje = "El motivo es requerido al desactivar un socio" });
                }

                cn_Socios cnSocios = new cn_Socios(_config);
                string resultado = cnSocios.ActualizarEstadoSocio(id, datos.Estado, datos.Motivo);

                return Ok(new { mensaje = resultado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar el estado del socio", error = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint para obtener el historial completo de socios (activos e inactivos)
        /// </summary>
        /// <returns>Lista de todos los socios con su estado y fechas</returns>
        [VerificarRol("directiva")]
        [HttpGet("historial")]
        public IActionResult ObtenerHistorialSocios()
        {
            try
            {
                cn_Socios cnSocios = new cn_Socios(_config);
                var socios = cnSocios.ObtenerHistorialSocios();
                return Ok(socios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener historial de socios", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene todos los socios, tanto activos como inactivos
        /// </summary>
        /// <returns>Lista de todos los socios</returns>
        [VerificarRol("directiva")]
        [HttpGet("todos")]
        public IActionResult ObtenerTodosSocios()
        {
            try
            {
                cn_Socios cnSocios = new cn_Socios(_config);
                var socios = cnSocios.ObtenerTodosSocios();
                return Ok(socios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener la lista de socios", error = ex.Message });
            }
        }



    }
}