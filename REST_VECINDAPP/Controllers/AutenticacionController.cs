using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using REST_VECINDAPP.CapaNegocios;
using REST_VECINDAPP.Modelos;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

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
            public string? Username { get; set; }
            public string? Password { get; set; }
        }

        /// <summary>
        /// Autentica a un usuario y devuelve un token JWT si las credenciales son válidas
        /// </summary>
        /// <param name="request">Objeto con credenciales de usuario</param>
        /// <returns>Token JWT si las credenciales son válidas</returns>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Verificar que las credenciales no sean nulas
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "El nombre de usuario y la contraseña son obligatorios" });
            }

            // Usar temporalmente credenciales hardcodeadas para simplificar
            // En un entorno de producción, deberías verificar contra la base de datos
            if (request.Username != "admin" || request.Password != "1234")
            {
                return Unauthorized(new { message = "Credenciales inválidas" });
            }

            /* Para implementar la verificación contra la base de datos, descomenta este código
            // Obtener usuarios de la base de datos
            cn_Usuarios cnUsuarios = new cn_Usuarios(_config);
            var usuarios = cnUsuarios.ListarUsuarios(-1);
            
            // Buscar el usuario por nombre de usuario
            var usuario = usuarios.FirstOrDefault(u => 
                u.nombre?.ToLower() == request.Username.ToLower() || 
                u.correo_electronico?.ToLower() == request.Username.ToLower());
            
            if (usuario == null)
            {
                return Unauthorized(new { message = "Credenciales inválidas" });
            }
            
            // Para validar la contraseña con BCrypt necesitarías instalar el paquete BCrypt.Net-Next
            // bool passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, usuario.password);
            
            // Por ahora, comparamos directamente (solo para desarrollo)
            bool passwordValid = usuario.password == request.Password;
            
            if (!passwordValid)
            {
                return Unauthorized(new { message = "Credenciales inválidas" });
            }
            */

            // Crear token JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT:Key no está configurado.");
            var key = Encoding.UTF8.GetBytes(jwtKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, request.Username),
                    new Claim(ClaimTypes.Role, "Admin")
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
    }
}