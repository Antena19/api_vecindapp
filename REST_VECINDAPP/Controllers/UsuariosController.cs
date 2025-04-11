using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using REST_VECINDAPP.CapaNegocios;
using REST_VECINDAPP.Modelos;
using System.Collections.Generic;
using System.Text.RegularExpressions;


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

        /// <summary>
        /// Registra un nuevo usuario en el sistema
        /// </summary>
        /// <param name="usuario">Datos del usuario a registrar</param>
        /// <returns>Resultado del registro</returns>
        [HttpPost("registrar")]
        [AllowAnonymous]
        public IActionResult Registrar([FromBody] Usuario usuario)
        {
            // Validar modelo de entrada
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

            // Crear instancia de la capa de negocios
            cn_Usuarios cnUsuarios = new cn_Usuarios(_config);

            // Intentar registrar usuario
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
                // Dependiendo del mensaje, podemos devolver diferentes códigos de error
                if (mensaje.Contains("correo electrónico") || mensaje.Contains("RUT"))
                {
                    return Conflict(new { mensaje });
                }

                return BadRequest(new { mensaje });
            }
        }

        /// <summary>
        /// Valida el formato del RUT chileno
        /// </summary>
        private bool ValidarRut(int rut, string? dv)
        {
            // Validar que el RUT no sea negativo
            if (rut < 1)
                return false;

            // Validar dígito verificador
            if (string.IsNullOrWhiteSpace(dv))
                return false;

            // Convertir a string para cálculo del dígito verificador
            string rutString = rut.ToString();

            int suma = 0;
            int multiplicador = 2;

            // Calcular suma de multiplicaciones
            for (int i = rutString.Length - 1; i >= 0; i--)
            {
                suma += int.Parse(rutString[i].ToString()) * multiplicador;
                multiplicador = multiplicador == 7 ? 2 : multiplicador + 1;
            }

            // Calcular dígito verificador
            int dvCalculado = 11 - (suma % 11);
            string dvEsperado = dvCalculado == 11 ? "0" :
                                dvCalculado == 10 ? "K" :
                                dvCalculado.ToString();

            // Comparar con dígito verificador proporcionado
            return dv.ToUpper() == dvEsperado;
        }

        /// <summary>
        /// Valida el formato de correo electrónico
        /// </summary>
        private bool ValidarCorreoElectronico(string? correo)
        {
            // Validar que el correo no esté vacío
            if (string.IsNullOrWhiteSpace(correo))
                return false;

            // Expresión regular para validación de correo electrónico
            string patron = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(correo, patron);
        }
    }
}