using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using REST_VECINDAPP.CapaNegocios;
using REST_VECINDAPP.Modelos;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using REST_VECINDAPP.Servicios;
using System.Security.Claims;

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
        private readonly cn_Usuarios _cnUsuarios;
        private readonly IEmailService _emailService;

        /// <summary>
        /// Constructor del controlador de usuarios
        /// </summary>
        /// <param name="configuration">Configuración de la aplicación</param>
        public UsuariosController(IConfiguration configuration, cn_Usuarios cnUsuarios, IEmailService emailService)
        {
            _config = configuration;
            _emailService = emailService;
            _cnUsuarios = cnUsuarios;
        }

        /// <summary>
        /// Obtiene un usuario específico o todos los usuarios
        /// Requiere autenticación (token JWT válido)
        /// </summary>
        /// <param name="rut">RUT del usuario a buscar. Si es -1 o no se proporciona, devuelve todos los usuarios</param>
        /// <returns>Lista de usuarios o un usuario específico</returns>
        [Authorize]
        [HttpGet]
        public ActionResult<List<Usuario>> Get([FromQuery] int rut = -1)
        {
            var cnUsuarios = _cnUsuarios;

            return Ok(cnUsuarios.ListarUsuarios(rut));
        }

        /// <summary>
        /// Registra un nuevo usuario en el sistema
        /// </summary>
        /// <param name="usuario">Datos del usuario a registrar</param>
        /// <returns>Resultado del registro</returns>
        [HttpPost("registrar")]
        [AllowAnonymous]
        public IActionResult Registrar([FromBody] Usuario usuario)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    mensaje = "Datos de registro inválidos",
                    errores = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            var cnUsuarios = _cnUsuarios;

            var (exito, mensaje) = cnUsuarios.RegistrarUsuario(usuario);

            if (exito)
            {
                return Ok(new
                {
                    mensaje = "Usuario registrado exitosamente",
                    rut = usuario.rut
                });
            }
            else
            {
                if (mensaje.Contains("correo electrónico") || mensaje.Contains("RUT"))
                {
                    return Conflict(new { mensaje });
                }

                return BadRequest(new { mensaje });
            }
        }

        /// <summary>
        /// Enviar solicitud para convertirse en socio
        /// </summary>
        [HttpPost("socios/solicitud")]
        [Authorize]
        public IActionResult EnviarSolicitudSocio([FromBody] SolicitudSocioDto solicitudSocio)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    mensaje = "Datos de solicitud de socio inválidos",
                    errores = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            var cnUsuarios = _cnUsuarios;


            var (exito, mensaje) = cnUsuarios.EnviarSolicitudSocio(
                solicitudSocio.Rut,
                solicitudSocio.RutArchivo
            );

            if (exito)
            {
                return Ok(new
                {
                    mensaje = "Solicitud de socio enviada exitosamente"
                });
            }
            else
            {
                if (mensaje.Contains("ya enviada"))
                {
                    return Conflict(new { mensaje });
                }

                if (mensaje.Contains("no cumple"))
                {
                    return BadRequest(new { mensaje });
                }

                return BadRequest(new { mensaje });
            }
        }

        /// <summary>
        /// Modelo de datos para solicitud de socio
        /// </summary>
        public class SolicitudSocioDto
        {
            [Required(ErrorMessage = "El RUT del solicitante es obligatorio")]
            public int Rut { get; set; }

            [Required(ErrorMessage = "El archivo de identificación es obligatorio")]
            public string RutArchivo { get; set; }

            [MaxLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
            public string Descripcion { get; set; }
        }

        /// <summary>
        /// Actualizar datos personales del usuario
        /// </summary>
        [HttpPut("{rut}")]
        [Authorize]
        public IActionResult ActualizarUsuario(int rut, [FromBody] ActualizacionUsuarioDto datosActualizacion)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    mensaje = "Datos de actualización inválidos",
                    errores = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            if (rut <= 0)
            {
                return BadRequest(new { mensaje = "RUT inválido" });
            }

            var cnUsuarios = _cnUsuarios;
            var (exito, mensaje) = cnUsuarios.ActualizarDatosUsuario(
                rut,
                datosActualizacion.Nombres,
                datosActualizacion.Apellidos,  // Esto se asigna a apellidoPaterno
                datosActualizacion.CorreoElectronico,
                datosActualizacion.Telefono,
                datosActualizacion.Direccion,
                datosActualizacion.ApellidoMaterno  // Agregar este parámetro
            );
            Console.WriteLine($"Resultado actualización: exito={exito}, mensaje='{mensaje}'");

            if (exito)
            {
                return Ok(new
                {
                    mensaje = "Datos de usuario actualizados exitosamente"
                });
            }
            else
            {
                if (mensaje.Contains("no encontrado"))
                {
                    return NotFound(new { mensaje });
                }

                if (mensaje.Contains("correo ya registrado"))
                {
                    return Conflict(new { mensaje });
                }

                return BadRequest(new { mensaje });
            }
        }

        /// <summary>
        /// Modelo de datos para actualización de usuario
        /// </summary>
        public class ActualizacionUsuarioDto
        {
            [StringLength(100, ErrorMessage = "Los nombres no pueden exceder 100 caracteres")]
            public string Nombres { get; set; }

            [StringLength(100, ErrorMessage = "Los apellidos no pueden exceder 100 caracteres")]
            public string Apellidos { get; set; }

            [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
            [StringLength(100, ErrorMessage = "El correo electrónico no puede exceder 100 caracteres")]
            public string CorreoElectronico { get; set; }

            [Phone(ErrorMessage = "El formato del teléfono no es válido")]
            [StringLength(100, ErrorMessage = "El teléfono no puede exceder 100 caracteres")]
            public string Telefono { get; set; }

            [StringLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres")]
            public string Direccion { get; set; }

            public string ApellidoMaterno { get; set; }
        }

        /// <summary>
        /// Asignar rol a un usuario
        /// </summary>
        [HttpPut("roles/{rut}")]
        [Authorize]
        public IActionResult AsignarRol(int rut, [FromBody] RolDto rolDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    mensaje = "Datos de asignación de rol inválidos",
                    errores = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            if (rut <= 0)
            {
                return BadRequest(new { mensaje = "RUT inválido" });
            }

            var cnUsuarios = _cnUsuarios;
            var (exito, mensaje) = cnUsuarios.AsignarRolUsuario(
                rut,
                rolDto.Rol
            );

            if (exito)
            {
                return Ok(new
                {
                    mensaje = "Rol asignado exitosamente"
                });
            }
            else
            {
                if (mensaje.Contains("no encontrado"))
                {
                    return NotFound(new { mensaje });
                }

                if (mensaje.Contains("no autorizado"))
                {
                    return Unauthorized(new { mensaje });
                }

                return BadRequest(new { mensaje });
            }
        }

        /// <summary>
        /// Modelo de datos para asignación de rol
        /// </summary>
        public class RolDto
        {
            [Required(ErrorMessage = "El rol es obligatorio")]
            [RegularExpression("^(Vecino|Socio|Directiva)$", ErrorMessage = "Rol inválido. Debe ser Vecino, Socio o Directiva")]
            public string Rol { get; set; }
        }

        /// <summary>
        /// Eliminar un usuario del sistema
        /// </summary>
        [HttpDelete("{rut}")]
        [Authorize]
        public IActionResult EliminarUsuario(int rut)
        {
            if (rut <= 0)
            {
                return BadRequest(new { mensaje = "RUT inválido" });
            }

            // Obtener el RUT del usuario autenticado desde el token JWT
            int rutSolicitante;

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("rut") ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out rutSolicitante))
            {
                // Como alternativa, podríamos usar el mismo RUT que se está intentando eliminar
                // Esto permitiría al usuario eliminar su propia cuenta
                rutSolicitante = rut;
            }

            var cnUsuarios = _cnUsuarios;
            var (exito, mensaje) = cnUsuarios.EliminarUsuario(rut, rutSolicitante);

            if (exito)
            {
                return Ok(new
                {
                    mensaje = "Usuario eliminado exitosamente"
                });
            }
            else
            {
                if (mensaje.Contains("no encontrado"))
                {
                    return NotFound(new { mensaje });
                }
                if (mensaje.Contains("No autorizado"))
                {
                    return Unauthorized(new { mensaje });
                }
                if (mensaje.Contains("No se puede eliminar"))
                {
                    return Conflict(new { mensaje });
                }
                return BadRequest(new { mensaje });
            }
        }
        /// <summary>
        /// Obtener información del usuario autenticado
        /// </summary>
        [HttpGet("autenticado")]
        [Authorize]
        public IActionResult ObtenerUsuarioAutenticado()
        {
            var cnUsuarios = _cnUsuarios;
            int rutUsuario = ObtenerRutUsuarioAutenticado();

            var (exito, usuario, mensaje) = cnUsuarios.ObtenerDatosUsuario(rutUsuario);

            if (exito)
            {
                return Ok(new
                {
                    mensaje = "Información de usuario obtenida exitosamente",
                    usuario = new
                    {
                        rut = usuario.rut,
                        nombre = usuario.nombre,
                        apellido_paterno = usuario.apellido_paterno,
                        apellido_materno = usuario.apellido_materno,
                        correo_electronico = usuario.correo_electronico,
                        telefono = usuario.telefono,
                        direccion = usuario.direccion,
                        tipo_usuario = usuario.tipo_usuario
                    }
                });
            }
            else
            {
                if (mensaje.Contains("no encontrado"))
                {
                    return NotFound(new { mensaje });
                }

                return BadRequest(new { mensaje });
            }
        }

        /// <summary>
        /// Método para obtener el RUT del usuario autenticado
        /// </summary>
        private int ObtenerRutUsuarioAutenticado()
        {
            var rut = User.Claims.FirstOrDefault(c => c.Type == "Rut")?.Value;

            if (string.IsNullOrEmpty(rut))
            {
                throw new UnauthorizedAccessException("No se pudo obtener el RUT del usuario autenticado");
            }

            return int.Parse(rut);
        }
    }
}