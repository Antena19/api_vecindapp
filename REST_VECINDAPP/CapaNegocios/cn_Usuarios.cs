using MySqlConnector;         // Importa la biblioteca para conectar con MySQL
using REST_VECINDAPP.Modelos; // Importa los modelos de la aplicación
using System.Data;            // Importa System.Data para trabajar con bases de datos

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
    }
}