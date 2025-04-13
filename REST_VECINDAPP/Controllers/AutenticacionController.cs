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

namespace REST_VECINDAPP.Controllers
{
    /// <summary>
    /// Controlador para gestionar la autenticación de usuarios en la aplicación
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AutenticacionController : ControllerBase
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// Constructor del controlador de autenticación
        /// </summary>
        /// <param name="configuration">Configuración de la aplicación</param>
        public AutenticacionController(IConfiguration configuration)
        {
            _config = configuration;
        }

        /// <summary>
        /// Modelo para la solicitud de inicio de sesión
        /// </summary>
        public class LoginRequest
        {
            [Required(ErrorMessage = "El RUT es obligatorio")]
            public string? Username { get; set; }

            [Required(ErrorMessage = "La contraseña es obligatoria")]
            public string? Password { get; set; }
        }

        /// <summary>
        /// Modelo para la solicitud de recuperación de contraseña
        /// </summary>
        public class RecuperacionClaveRequest
        {
            [Required(ErrorMessage = "El correo electrónico es obligatorio")]
            [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
            public string? CorreoElectronico { get; set; }
        }

        /// <summary>
        /// Modelo para la confirmación de recuperación de contraseña
        /// </summary>
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

        /// <summary>
        /// Autentica a un usuario y devuelve un token JWT si las credenciales son válidas
        /// </summary>
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
            cn_Usuarios cnUsuarios = new cn_Usuarios(_config);
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

            return Ok(new { token = tokenString });
        }

        /// <summary>
        /// Solicitar recuperación de contraseña
        /// </summary>
        /// <param name="request">Datos para solicitar recuperación de contraseña</param>
        /// <returns>Resultado de la solicitud de recuperación</returns>
        [HttpPost("recuperar-clave")]
        public IActionResult SolicitarRecuperacionClave([FromBody] RecuperacionClaveRequest request)
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
            cn_Usuarios cnUsuarios = new cn_Usuarios(_config);
            var (exito, mensaje) = cnUsuarios.SolicitarRecuperacionClave(request.CorreoElectronico);

            if (exito)
            {
                return Ok(new { mensaje = "Se ha enviado un correo de recuperación" });
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
            cn_Usuarios cnUsuarios = new cn_Usuarios(_config);
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
    }
}