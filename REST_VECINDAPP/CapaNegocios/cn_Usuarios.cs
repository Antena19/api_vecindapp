using MySqlConnector;         // Importa la biblioteca para conectar con MySQL
using REST_VECINDAPP.Modelos; // Importa los modelos de la aplicación
using System.Data;            // Importa System.Data para trabajar con bases de datos
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using REST_VECINDAPP.Servicios;


namespace REST_VECINDAPP.CapaNegocios
{
    // Clase que maneja la lógica de negocio relacionada con los usuarios
    public class cn_Usuarios
    {
        // Variable para almacenar la cadena de conexión a la base de datos
        private readonly string _connectionString;
        private readonly IEmailService _emailService;


        // Constructor que recibe la configuración de la aplicación
        // Obtiene la cadena de conexión desde appsettings.json
        public cn_Usuarios(IConfiguration configuration, IEmailService emailService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentException("La cadena de conexión 'DefaultConnection' no está configurada.");
            _emailService = emailService;
        }

        /// <summary>
        /// Método que obtiene la lista de usuarios desde la base de datos
        /// </summary>
        /// <param name="rut">RUT del usuario a buscar. Si es -1, devuelve todos los usuarios</param>
        /// <returns>Lista de objetos Usuario</returns>
        public List<Usuario> ListarUsuarios(int rut)
        {
            // Crea una lista vacía para almacenar los resultados
            List<Usuario> Usuarios = new List<Usuario>();

            // Crea y administra la conexión a la base de datos (se cerrará automáticamente al salir del bloque)
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                // Abre la conexión a la base de datosS
                conn.Open();

                // Crea un comando para ejecutar el procedimiento almacenado
                MySqlCommand cmd = new MySqlCommand("SP_SELECT_USUARIOS", conn);

                // Especifica que el comando es un procedimiento almacenado
                cmd.CommandType = CommandType.StoredProcedure;

                // Agrega el parámetro para el procedimiento almacenado
                // Si rut es -1, se pasa null para obtener todos los usuarios
                // Si no, se filtra por el rut especificado
                if (rut != -1)
                    cmd.Parameters.AddWithValue("@p_rut", rut);
                else
                    cmd.Parameters.AddWithValue("@p_rut", null);

                // Ejecuta el comando y obtiene un lector de datos
                using (var reader = cmd.ExecuteReader())
                {
                    // Itera sobre cada fila de resultados
                    while (reader.Read())
                    {
                        // Crea un nuevo objeto Usuario para cada registro
                        Usuario usuarioTemp = new Usuario();

                        // Lee cada columna del registro y asigna al objeto Usuario

                        // Obtiene el RUT como entero
                        usuarioTemp.rut = Convert.ToInt32(reader["rut"]);

                        // Obtiene el dígito verificador como string
                        usuarioTemp.dv_rut = Convert.ToString(reader["dv_rut"]);

                        // Obtiene el nombre del usuario
                        usuarioTemp.nombre = Convert.ToString(reader["nombre"]);

                        // Obtiene los apellidos
                        usuarioTemp.apellido_paterno = Convert.ToString(reader["apellido_paterno"]);
                        usuarioTemp.apellido_materno = Convert.ToString(reader["apellido_materno"]);

                        // Obtiene el correo electrónico
                        usuarioTemp.correo_electronico = Convert.ToString(reader["correo_electronico"]);

                        // Obtiene el teléfono como string (para aceptar formatos como +56912345678)
                        usuarioTemp.telefono = Convert.ToString(reader["telefono"]);

                        // Obtiene la dirección del usuario
                        usuarioTemp.direccion = Convert.ToString(reader["direccion"]);

                        // Obtiene la contraseña (hash) del usuario
                        usuarioTemp.password = Convert.ToString(reader["password"]);

                        // Obtiene la fecha de registro
                        usuarioTemp.fecha_registro = Convert.ToDateTime(reader["fecha_registro"]);

                        // Obtiene el estado como entero (0: inactivo, 1: activo)
                        usuarioTemp.estado = Convert.ToInt32(reader["estado"]);

                        // Obtiene el tipo de usuario (vecino, socio, directiva)
                        usuarioTemp.tipo_usuario = Convert.ToString(reader["tipo_usuario"]);

                        // Para los campos nuevos, verificamos si son NULL en la base de datos
                        // Token de recuperación de contraseña (puede ser NULL)
                        if (reader["token_recuperacion"] != DBNull.Value)
                            usuarioTemp.token_recuperacion = Convert.ToString(reader["token_recuperacion"]);

                        // Fecha de generación del token (puede ser NULL)
                        if (reader["fecha_token_recuperacion"] != DBNull.Value)
                            usuarioTemp.fecha_token_recuperacion = Convert.ToDateTime(reader["fecha_token_recuperacion"]);

                        // Agrega el usuario a la lista de resultados
                        Usuarios.Add(usuarioTemp);
                    }
                }

                // Cierra la conexión a la base de datos
                conn.Close();
            }

            // Devuelve la lista de usuarios
            return Usuarios;
        }

        /// <summary>
        /// Valida el formato del RUT chileno
        /// </summary>
        /// <param name="rut">RUT sin dígito verificador</param>
        /// <param name="dv">Dígito verificador</param>
        /// <returns>True si el RUT es válido, false en caso contrario</returns>
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
        /// <param name="correo">Correo electrónico a validar</param>
        /// <returns>True si el correo tiene un formato válido, false en caso contrario</returns>
        private bool ValidarCorreoElectronico(string correo)
        {
            // Expresión regular para validación de correo electrónico
            string patron = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(correo, patron);
        }

        /// <summary>
        /// Valida la complejidad de la contraseña
        /// </summary>
        /// <param name="contraseña">Contraseña a validar</param>
        /// <returns>True si la contraseña cumple con los requisitos, false en caso contrario</returns>
        private bool ValidarComplejidadContraseña(string contraseña)
        {
            // Requisitos:
            // - Mínimo 8 caracteres
            // - Al menos una letra mayúscula
            // - Al menos una letra minúscula
            // - Al menos un número
            // - Al menos un carácter especial
            var patron = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$");
            return patron.IsMatch(contraseña);
        }

        /// <summary>
        /// Valida el formato de teléfono chileno
        /// </summary>
        /// <param name="telefono">Número de teléfono a validar</param>
        /// <returns>True si el teléfono tiene un formato válido, false en caso contrario</returns>
        private bool ValidarTelefono(string telefono)
        {
            // Formatos válidos: 
            // +56912345678
            // 912345678
            // (56)912345678
            if (string.IsNullOrWhiteSpace(telefono))
                return true; // El teléfono es opcional

            var patron = new Regex(@"^(\+?56|0)?[9]\d{8}$");
            return patron.IsMatch(telefono.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", ""));
        }

        /// <summary>
        /// Hashea una contraseña usando SHA256
        /// </summary>
        /// <param name="contraseña">Contraseña en texto plano</param>
        /// <returns>Hash de la contraseña</returns>
        private string HashearContraseña(string contraseña)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Convertir la contraseña a bytes y calcular hash
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(contraseña));

                // Convertir bytes a string hexadecimal
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Registra un nuevo usuario en el sistema
        /// </summary>
        /// <param name="usuario">Objeto Usuario a registrar</param>
        /// <returns>Resultado del registro</returns>
        public (bool Exito, string Mensaje) RegistrarUsuario(Usuario usuario)
        {
            // 1. Validaciones previas
            if (!ValidarRut(usuario.rut, usuario.dv_rut))
            {
                return (false, "El RUT ingresado no es válido.");
            }

            if (string.IsNullOrWhiteSpace(usuario.correo_electronico) ||
                !ValidarCorreoElectronico(usuario.correo_electronico))
            {
                return (false, "El correo electrónico no tiene un formato válido.");
            }

            if (string.IsNullOrWhiteSpace(usuario.nombre) ||
                string.IsNullOrWhiteSpace(usuario.apellido_paterno))
            {
                return (false, "El nombre y apellido son obligatorios.");
            }

            // Validación de contraseña
            if (string.IsNullOrWhiteSpace(usuario.password))
            {
                return (false, "La contraseña no puede estar vacía.");
            }

            if (!ValidarComplejidadContraseña(usuario.password))
            {
                return (false, "La contraseña debe tener al menos 8 caracteres, " +
                               "incluyendo mayúsculas, minúsculas, números y un carácter especial.");
            }

            // Validación de teléfono (si se proporciona)
            if (!string.IsNullOrWhiteSpace(usuario.telefono) &&
                !ValidarTelefono(usuario.telefono))
            {
                return (false, "El número de teléfono no tiene un formato válido.");
            }

            // 2. Hashear la contraseña
            string passwordHash = HashearContraseña(usuario.password);

            // 3. Registrar en base de datos
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SP_REGISTRAR_USUARIOS", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;

                        // Parámetros del procedimiento almacenado
                        cmd.Parameters.AddWithValue("@p_rut", usuario.rut);
                        cmd.Parameters.AddWithValue("@p_dv_rut", usuario.dv_rut);
                        cmd.Parameters.AddWithValue("@p_nombre", usuario.nombre);
                        cmd.Parameters.AddWithValue("@p_apellido_paterno", usuario.apellido_paterno);
                        cmd.Parameters.AddWithValue("@p_apellido_materno", usuario.apellido_materno ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@p_correo_electronico", usuario.correo_electronico);
                        cmd.Parameters.AddWithValue("@p_telefono", usuario.telefono ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@p_direccion", usuario.direccion ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@p_password", passwordHash);

                        // Ejecutar el comando
                        cmd.ExecuteNonQuery();
                    }

                    return (true, "Usuario registrado exitosamente");
                }
                catch (MySqlException ex)
                {
                    // Manejo de errores de MySQL (validaciones del SP)
                    return (false, ex.Message);
                }
                catch (Exception ex)
                {
                    // Manejo de errores generales
                    return (false, $"Error inesperado: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Iniciar sesión de usuario
        /// </summary>
        /// <param name="rut">RUT del usuario</param>
        /// <param name="contrasena">Contraseña del usuario</param>
        /// <returns>Resultado del inicio de sesión</returns>
        public (bool Exito, string Mensaje) IniciarSesion(int rut, string contrasena)
        {
            // Hashear la contraseña para comparación
            string passwordHash = HashearContraseña(contrasena);

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SP_INICIAR_SESION", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros del procedimiento almacenado
                        cmd.Parameters.AddWithValue("@p_rut", rut);
                        cmd.Parameters.AddWithValue("@p_password", passwordHash);

                        // Parámetro de salida para el mensaje
                        MySqlParameter msgParam = new MySqlParameter("@p_mensaje", MySqlDbType.VarChar, 255);
                        msgParam.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(msgParam);

                        // Ejecutar el comando
                        cmd.ExecuteNonQuery();

                        // Obtener el mensaje de salida
                        string mensaje = msgParam.Value?.ToString() ?? "";

                        return (mensaje == "OK", mensaje);
                    }
                }
                catch (Exception ex)
                {
                    return (false, $"Error al iniciar sesión: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Solicitar recuperación de contraseña
        /// </summary>
        /// <param name="correoElectronico">Correo electrónico del usuario</param>
        /// <returns>Resultado de la solicitud de recuperación</returns>
        public async Task<(bool Exito, string Mensaje, int Rut)> SolicitarRecuperacionClave(string correoElectronico)
        {
            // Validar formato de correo electrónico
            if (!ValidarCorreoElectronico(correoElectronico))
            {
                return (false, "El correo electrónico no tiene un formato válido.",0);
            }

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();

                    // Primero, obtener el RUT asociado al correo
                    int rut = 0;
                    using (MySqlCommand cmdBuscarRut = new MySqlCommand("SELECT rut FROM usuarios WHERE correo_electronico = @p_correo_electronico", conn))
                    {
                        cmdBuscarRut.Parameters.AddWithValue("@p_correo_electronico", correoElectronico);
                        object resultado = cmdBuscarRut.ExecuteScalar();

                        if (resultado == null || resultado == DBNull.Value)
                        {
                            return (false, "Correo electrónico no encontrado.", 0);
                        }

                        rut = Convert.ToInt32(resultado);
                    }
                    // Ahora, llamar al procedimiento SP_GENERAR_TOKEN_RECUPERACION
                    using (MySqlCommand cmd = new MySqlCommand("SP_GENERAR_TOKEN_RECUPERACION", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros del procedimiento almacenado
                        cmd.Parameters.AddWithValue("@p_rut", rut);
                        cmd.Parameters.AddWithValue("@p_correo_electronico", correoElectronico);

                        // Ejecutar y obtener resultado
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string token = reader["token"].ToString();

                                // Preparar mensaje de correo
                                string subject = "Recuperación de contraseña - VecindApp";
                                string htmlBody = $@"
                                <html>
                                <body>
                                    <h2>Recuperación de contraseña</h2>
                                    <p>Hola,</p>
                                    <p>Has solicitado un código para restablecer tu contraseña en VecindApp.</p>
                                    <p>Tu código de recuperación es: <strong>{token}</strong></p>
                                    <p>Este código expirará en 1 hora.</p>
                                    <p>Si no has solicitado este cambio, puedes ignorar este correo.</p>
                                    <p>Saludos,<br>El equipo de VecindApp</p>
                                </body>
                                </html>";

                                // Enviar correo
                                bool emailSent = await _emailService.SendEmailAsync(correoElectronico, subject, htmlBody);

                                if (emailSent)
                                    return (true, "Se ha enviado un correo con el código de recuperación", rut);
                                else
                                    return (false, "Error al enviar correo de recuperación", 0);

                             
                            }
                            else
                            {
                                return (false, "Error al generar token de recuperación.", 0);
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    if (ex.Message.Contains("Correo electrónico no coincide"))
                    {
                        return (false, "El correo electrónico no coincide con el RUT registrado.", 0);
                    }
                    return (false, $"Error al solicitar recuperación de clave: {ex.Message}", 0);
                }
                catch (Exception ex)
                {
                    return (false, $"Error al solicitar recuperación de clave: {ex.Message}", 0);
                }
            }
        }

        /// <summary>
        /// Confirmar recuperación de contraseña
        /// </summary>
        /// <param name="rut">RUT del usuario</param>
        /// <param name="token">Token de recuperación</param>
        /// <param name="nuevaContrasena">Nueva contraseña</param>
        /// <returns>Resultado de la confirmación de recuperación</returns>
        public (bool Exito, string Mensaje) ConfirmarRecuperacionClave(int rut, string token, string nuevaContrasena)
        {
            // Validar complejidad de la nueva contraseña
            if (!ValidarComplejidadContraseña(nuevaContrasena))
            {
                return (false, "La nueva contraseña no cumple con los requisitos de complejidad.");
            }

            // Hashear la nueva contraseña
            string nuevaContrasenaHash = HashearContraseña(nuevaContrasena);

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SP_RESTABLECER_CONTRASENA", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros del procedimiento almacenado
                        cmd.Parameters.AddWithValue("@p_rut", rut);
                        cmd.Parameters.AddWithValue("@p_token", token);
                        cmd.Parameters.AddWithValue("@p_nueva_password", nuevaContrasenaHash);

                        // Ejecutar el comando
                        cmd.ExecuteNonQuery();

                        return (true, "Contraseña restablecida exitosamente");
                    }
                }
                catch (MySqlException ex)
                {
                    if (ex.Message.Contains("Token inválido") || ex.Message.Contains("expirado"))
                    {
                        return (false, "El token de recuperación es inválido o ha expirado.");
                    }
                    return (false, $"Error al confirmar recuperación de clave: {ex.Message}");
                }
                catch (Exception ex)
                {
                    return (false, $"Error al confirmar recuperación de clave: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Cambiar contraseña del usuario
        /// </summary>
        /// <param name="rut">RUT del usuario</param>
        /// <param name="contrasenaActual">Contraseña actual</param>
        /// <param name="nuevaContrasena">Nueva contraseña</param>
        /// <returns>Resultado del cambio de contraseña</returns>
        public (bool Exito, string Mensaje) CambiarContrasena(int rut, string contrasenaActual, string nuevaContrasena)
        {
            // Validar complejidad de la nueva contraseña
            if (!ValidarComplejidadContraseña(nuevaContrasena))
            {
                return (false, "La nueva contraseña no cumple con los requisitos de complejidad.");
            }

            // Hashear las contraseñas
            string contrasenaActualHash = HashearContraseña(contrasenaActual);
            string nuevaContrasenaHash = HashearContraseña(nuevaContrasena);

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SP_CAMBIAR_CONTRASENA", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros del procedimiento almacenado
                        cmd.Parameters.AddWithValue("@p_rut", rut);
                        cmd.Parameters.AddWithValue("@p_contrasena_actual", contrasenaActualHash);
                        cmd.Parameters.AddWithValue("@p_nueva_contrasena", nuevaContrasenaHash);

                        // Parámetro de salida para el mensaje
                        MySqlParameter msgParam = new MySqlParameter("@p_mensaje", MySqlDbType.VarChar, 255);
                        msgParam.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(msgParam);

                        // Ejecutar el comando
                        cmd.ExecuteNonQuery();

                        // Obtener el mensaje de salida
                        string mensaje = msgParam.Value?.ToString() ?? "";

                        return (mensaje == "OK", mensaje);
                    }
                }
                catch (Exception ex)
                {
                    return (false, $"Error al cambiar contraseña: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Actualizar datos del usuario
        /// </summary>
        /// <param name="rut">RUT del usuario</param>
        /// <param name="nombres">Nombres del usuario</param>
        /// <param name="apellidoPaterno">Apellido paterno</param>
        /// <param name="apellidoMaterno">Apellido materno (opcional)</param>
        /// <param name="correoElectronico">Correo electrónico</param>
        /// <param name="telefono">Teléfono (opcional)</param>
        /// <param name="direccion">Dirección (opcional)</param>
        /// <param name="apellidoMaterno">apellidoMaterno (opcional)</param>
        /// <returns>Resultado de la actualización</returns>
        public (bool Exito, string Mensaje) ActualizarDatosUsuario(
            int rut,
            string nombres,
            string apellidoPaterno,
            string? correoElectronico = null,
            string? telefono = null,
            string? direccion = null,
            string? apellidoMaterno = null)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(nombres))
            {
                return (false, "Los nombres son obligatorios.");
            }

            if (string.IsNullOrWhiteSpace(apellidoPaterno))
            {
                return (false, "El apellido paterno es obligatorio.");
            }

            // Validar correo electrónico si se proporciona
            if (!string.IsNullOrWhiteSpace(correoElectronico) &&
                !ValidarCorreoElectronico(correoElectronico))
            {
                return (false, "El correo electrónico no tiene un formato válido.");
            }

            // Validar teléfono si se proporciona
            if (!string.IsNullOrWhiteSpace(telefono) &&
                !ValidarTelefono(telefono))
            {
                return (false, "El número de teléfono no tiene un formato válido.");
            }

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SP_ACTUALIZAR_DATOS_USUARIO", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros del procedimiento almacenado
                        cmd.Parameters.AddWithValue("@p_rut", rut);
                        cmd.Parameters.AddWithValue("@p_nombres", nombres);
                        cmd.Parameters.AddWithValue("@p_apellido_paterno", apellidoPaterno);
                        cmd.Parameters.AddWithValue("@p_apellido_materno", apellidoMaterno ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@p_correo_electronico", correoElectronico ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@p_telefono", telefono ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@p_direccion", direccion ?? (object)DBNull.Value);

                        // Parámetro de salida para el mensaje
                        MySqlParameter msgParam = new MySqlParameter("@p_mensaje", MySqlDbType.VarChar, 255);
                        msgParam.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(msgParam);

                        // Ejecutar el comando
                        cmd.ExecuteNonQuery();

                        // Obtener el mensaje de salida
                        string mensaje = msgParam.Value?.ToString() ?? "";

                        return (mensaje == "OK", mensaje);
                    }
                }
                catch (Exception ex)
                {
                    return (false, $"Error al actualizar datos de usuario: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Obtener datos de un usuario
        /// </summary>
        /// <param name="rut">RUT del usuario</param>
        /// <returns>Datos del usuario</returns>
        public (bool Exito, Usuario? Usuario, string Mensaje) ObtenerDatosUsuario(int rut)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SP_SELECT_USUARIOS", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros del procedimiento almacenado
                        cmd.Parameters.AddWithValue("@p_rut", rut);

                        // Ejecutar el comando
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Crear objeto Usuario con los datos obtenidos
                                Usuario usuario = new Usuario
                                {
                                    rut = Convert.ToInt32(reader["rut"]),
                                    dv_rut = Convert.ToString(reader["dv_rut"]),
                                    nombre = Convert.ToString(reader["nombre"]),
                                    apellido_paterno = Convert.ToString(reader["apellido_paterno"]),
                                    apellido_materno = reader["apellido_materno"] != DBNull.Value
                                        ? Convert.ToString(reader["apellido_materno"])
                                        : null,
                                    correo_electronico = Convert.ToString(reader["correo_electronico"]),
                                    telefono = reader["telefono"] != DBNull.Value
                                        ? Convert.ToString(reader["telefono"])
                                        : null,
                                    direccion = reader["direccion"] != DBNull.Value
                                        ? Convert.ToString(reader["direccion"])
                                        : null,
                                    tipo_usuario = Convert.ToString(reader["tipo_usuario"])
                                };

                                return (true, usuario, "Usuario encontrado");
                            }
                            else
                            {
                                return (false, null, "Usuario no encontrado");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return (false, null, $"Error al obtener datos de usuario: {ex.Message}");
                }
            }
        }
        /// <summary>
        /// Asignar rol a un usuario
        /// </summary>
        /// <param name="rut">RUT del usuario</param>
        /// <param name="rol">Rol a asignar</param>
        /// <returns>Resultado de la asignación de rol</returns>
        public (bool Exito, string Mensaje) AsignarRolUsuario(int rut, string rol)
        {
            // Validar que el rol sea uno de los permitidos
            string[] rolesValidos = { "Vecino", "Socio", "Directiva" };
            if (!rolesValidos.Contains(rol))
            {
                return (false, "Rol inválido. Debe ser Vecino, Socio o Directiva.");
            }

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SP_ASIGNAR_ROL_USUARIO", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros del procedimiento almacenado
                        cmd.Parameters.AddWithValue("@p_rut", rut);
                        cmd.Parameters.AddWithValue("@p_rol", rol);

                        // Parámetro de salida para el mensaje
                        MySqlParameter msgParam = new MySqlParameter("@p_mensaje", MySqlDbType.VarChar, 255);
                        msgParam.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(msgParam);

                        // Ejecutar el comando
                        cmd.ExecuteNonQuery();

                        // Obtener el mensaje de salida
                        string mensaje = msgParam.Value?.ToString() ?? "";

                        return (mensaje == "OK", mensaje);
                    }
                }
                catch (Exception ex)
                {
                    return (false, $"Error al asignar rol: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Eliminar un usuario del sistema
        /// </summary>
        /// <param name="rut">RUT del usuario a eliminar</param>
        /// <param name="rutSolicitante">RUT del usuario que solicita la eliminación</param>
        /// <returns>Resultado de la eliminación</returns>
        public (bool Exito, string Mensaje) EliminarUsuario(int rut, int rutSolicitante)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SP_ELIMINAR_USUARIO", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        // Parámetros del procedimiento almacenado
                        cmd.Parameters.AddWithValue("@p_rut", rut);
                        cmd.Parameters.AddWithValue("@p_rut_solicitante", rutSolicitante);

                        // Parámetro de salida para el mensaje
                        MySqlParameter msgParam = new MySqlParameter("@p_mensaje", MySqlDbType.VarChar, 255);
                        msgParam.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(msgParam);

                        // Ejecutar el comando
                        cmd.ExecuteNonQuery();

                        // Obtener el mensaje de salida
                        string mensaje = msgParam.Value?.ToString() ?? "";
                        return (mensaje == "OK", mensaje);
                    }
                }
                catch (Exception ex)
                {
                    return (false, $"Error al eliminar usuario: {ex.Message}");
                }
            }
        }

        public (bool Exito, string Mensaje) EnviarSolicitudSocio(int rut, string documentoIdentidad, string documentoDomicilio)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SP_SOLICITAR_MEMBRESIA_SOCIO", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros del procedimiento almacenado
                        cmd.Parameters.AddWithValue("@p_rut", rut);
                        cmd.Parameters.AddWithValue("@p_documento_identidad", documentoIdentidad);
                        cmd.Parameters.AddWithValue("@p_documento_domicilio", documentoDomicilio);

                        // Parámetro de salida para el mensaje
                        MySqlParameter msgParam = new MySqlParameter("@p_mensaje", MySqlDbType.VarChar, 255);
                        msgParam.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(msgParam);

                        // Ejecutar el comando
                        cmd.ExecuteNonQuery();

                        // Obtener el mensaje de salida
                        string mensaje = msgParam.Value?.ToString() ?? "";

                        return (mensaje == "OK", mensaje);
                    }
                }
                catch (Exception ex)
                {
                    return (false, $"Error al solicitar membresía de socio: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Recuperar contraseña de forma simple usando RUT y nombre completo
        /// </summary>
        /// <param name="rut">RUT del usuario</param>
        /// <param name="nombreCompleto">Nombre completo del usuario (nombre y apellidos)</param>
        /// <param name="nuevaContrasena">Nueva contraseña</param>
        /// <returns>Resultado de la recuperación</returns>
        public (bool Exito, string Mensaje) RecuperarClaveSimple(int rut, string nombreCompleto, string nuevaContrasena)
        {
            // Validar complejidad de la nueva contraseña
            if (!ValidarComplejidadContraseña(nuevaContrasena))
            {
                return (false, "La nueva contraseña no cumple con los requisitos de complejidad.");
            }

            // Hashear la nueva contraseña
            string nuevaContrasenaHash = HashearContraseña(nuevaContrasena);

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();

                    using (MySqlCommand cmd = new MySqlCommand("SP_RECUPERAR_CONTRASENA_SIMPLE", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros del procedimiento almacenado
                        cmd.Parameters.AddWithValue("@p_rut", rut);
                        cmd.Parameters.AddWithValue("@p_nombre_completo", nombreCompleto);
                        cmd.Parameters.AddWithValue("@p_nueva_password", nuevaContrasenaHash);

                        // Ejecutar el procedimiento
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string mensaje = reader["mensaje"].ToString();
                                return (true, mensaje);
                            }
                            else
                            {
                                return (false, "Error al procesar la solicitud de recuperación");
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    // Manejar errores específicos del procedimiento almacenado
                    if (ex.Message.Contains("Usuario no encontrado"))
                    {
                        return (false, "Usuario no encontrado");
                    }
                    if (ex.Message.Contains("nombre completo no coincide"))
                    {
                        return (false, "El nombre completo no coincide con el registrado para este RUT");
                    }
                    return (false, $"Error al recuperar contraseña: {ex.Message}");
                }
                catch (Exception ex)
                {
                    return (false, $"Error al recuperar contraseña: {ex.Message}");
                }
            }
        }

    }
}