using MySqlConnector;         // Importa la biblioteca para conectar con MySQL
using REST_VECINDAPP.Modelos; // Importa los modelos de la aplicación
using System.Data;            // Importa System.Data para trabajar con bases de datos
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace REST_VECINDAPP.CapaNegocios
{
    // Clase que maneja la lógica de negocio relacionada con los usuarios
    public class cn_Usuarios
    {
        // Variable para almacenar la cadena de conexión a la base de datos
        private readonly string _connectionString;

        // Constructor que recibe la configuración de la aplicación
        // Obtiene la cadena de conexión desde appsettings.json
        public cn_Usuarios(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentException("La cadena de conexión 'DefaultConnection' no está configurada.");
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
    }
}