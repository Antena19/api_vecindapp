using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using REST_VECINDAPP.CapaNegocios;
using REST_VECINDAPP.Modelos;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;
using REST_VECINDAPP.Servicios;

namespace REST_VECINDAPP.Controllers
{
    /// Controlador para gestionar la autenticación de usuarios en la aplicación
    [Route("api/[controller]")]
    [ApiController]
    public class AutenticacionController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly cn_Usuarios _cnUsuarios;

        /// Constructor del controlador de autenticación
        /// <param name="configuration">Configuración de la aplicación</param>
        public AutenticacionController(IConfiguration configuration, IEmailService emailService, cn_Usuarios cnUsuarios)
        {
            _config = configuration;
            _emailService = emailService;
            _cnUsuarios = cnUsuarios;
            
        }

        /// Modelo para la solicitud de inicio de sesión
        public class LoginRequest
        {
            [Required(ErrorMessage = "El RUT es obligatorio")]
            public string? Username { get; set; }

            [Required(ErrorMessage = "La contraseña es obligatoria")]
            public string? Password { get; set; }
        }

        /// Modelo para la solicitud de recuperación de contraseña
        public class RecuperacionClaveRequest
        {
            [Required(ErrorMessage = "El correo electrónico es obligatorio")]
            [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
            public string? CorreoElectronico { get; set; }
        }

        /// Modelo para la confirmación de recuperación de contraseña
        public class ConfirmacionRecuperacionRequest
        {
            [Required(ErrorMessage = "El RUT es obligatorio")]
            public int Rut { get; set; }

            [Required(ErrorMessage = "El token de recuperación es obligatorio")]
            public string? Token { get; set; }

            [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
            public string? NuevaContrasena { get; set; }

            [Required(ErrorMessage = "La confirmación de contraseña es obligatoria")]
            public string? ConfirmarContrasena { get; set; }
        }

        /// Autentica a un usuario y devuelve un token JWT si las credenciales son válidas
        /// <param name="request">Objeto con credenciales de usuario</param>
        /// <returns>Token JWT si las credenciales son válidas</returns>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Validar modelo de entrada
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    mensaje = "Datos de inicio de sesión inválidos",
                    errores = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            // Intentar parsear el username como RUT
            if (!int.TryParse(request.Username, out int rut))
            {
                return BadRequest(new { message = "El nombre de usuario debe ser un RUT válido" });
            }

            // Usar la capa de negocios para validar las credenciales
            var cnUsuarios = _cnUsuarios;

            var (exito, mensaje) = cnUsuarios.IniciarSesion(rut, request.Password);

            if (!exito)
            {
                return Unauthorized(new { message = mensaje });
            }

            // Crear token JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT:Key no está configurado.");
            var key = Encoding.UTF8.GetBytes(jwtKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, request.Username),
                    new Claim("Rut", rut.ToString()), // Agregar RUT como claim
                    new Claim(ClaimTypes.Role, "Usuario") // Puedes ajustar esto según el rol del usuario
                }),
                Expires = DateTime.UtcNow.AddHours(1), // Expira en 1 hora
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Después de generar el token, obtener datos del usuario
            var (datosExito, datosUsuario, _) = cnUsuarios.ObtenerDatosUsuario(rut);

            if (!datosExito || datosUsuario == null)
            {
                return Ok(new { token = tokenString }); // Solo devolver token si no se pueden obtener datos
            }

            // Devolver token y datos del usuario
            return Ok(new
            {
                token = tokenString,
                usuario = datosUsuario
            });
        }

        /// <summary>
        /// Solicitar recuperación de contraseña
        /// </summary>
        /// <param name="request">Datos para solicitar recuperación de contraseña</param>
        /// <returns>Resultado de la solicitud de recuperación</returns>
        [HttpPost("recuperar-clave")]
        public async Task<IActionResult> SolicitarRecuperacionClave([FromBody] RecuperacionClaveRequest request)
        {
            // Validar modelo de entrada
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    mensaje = "Datos de recuperación inválidos",
                    errores = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            // Usar la capa de negocios para solicitar recuperación de clave
            cn_Usuarios cnUsuarios = new cn_Usuarios(_config, _emailService);
            var (exito, mensaje, rut) = await cnUsuarios.SolicitarRecuperacionClave(request.CorreoElectronico);

            if (exito)
            {
                return Ok(new { mensaje = "Se ha enviado un correo de recuperación", rut = rut });
            }
            else
            {
                // Manejar diferentes tipos de errores
                if (mensaje.Contains("no encontrado"))
                {
                    return NotFound(new { mensaje });
                }

                return BadRequest(new { mensaje });
            }
        }

        /// <summary>
        /// Confirmar recuperación de contraseña
        /// </summary>
        /// <param name="request">Datos para confirmar recuperación de contraseña</param>
        /// <returns>Resultado de la confirmación de recuperación</returns>
        [HttpPost("confirmar-recuperacion")]
        public IActionResult ConfirmarRecuperacionClave([FromBody] ConfirmacionRecuperacionRequest request)
        {
            // Validar modelo de entrada
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    mensaje = "Datos de confirmación inválidos",
                    errores = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            // Validar que las contraseñas coincidan
            if (request.NuevaContrasena != request.ConfirmarContrasena)
            {
                return BadRequest(new { mensaje = "Las contraseñas no coinciden" });
            }

            // Usar la capa de negocios para confirmar recuperación de clave
            var cnUsuarios = _cnUsuarios;

            var (exito, mensaje) = cnUsuarios.ConfirmarRecuperacionClave(
                request.Rut,
                request.Token,
                request.NuevaContrasena
            );

            if (exito)
            {
                return Ok(new { mensaje = "Contraseña restablecida exitosamente" });
            }
            else
            {
                // Manejar diferentes tipos de errores
                if (mensaje.Contains("token inválido") || mensaje.Contains("expirado"))
                {
                    return Unauthorized(new { mensaje });
                }

                return BadRequest(new { mensaje });
            }
        }

        // Agregar este nuevo endpoint de prueba
        [HttpGet("test/{rut}")]
        public IActionResult TestUsuario(int rut)
        {
            try
            {
                var (exito, usuario, mensaje) = _cnUsuarios.ObtenerDatosUsuario(rut);

                return Ok(new
                {
                    exito = exito,
                    usuario = usuario,
                    mensaje = mensaje
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message,
                    stack = ex.StackTrace
                });
            }
        }
    }
}