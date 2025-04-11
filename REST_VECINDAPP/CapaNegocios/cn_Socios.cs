using MySqlConnector;        // Importa la biblioteca para conectar con MySQL
using REST_VECINDAPP.Modelos; // Importa los modelos de la aplicación
using System.Data;           // Importa System.Data para trabajar con bases de datos

namespace REST_VECINDAPP.CapaNegocios
{
    /// <summary>
    /// Clase que maneja la lógica de negocio relacionada con los socios de la junta de vecinos
    /// </summary>
    public class cn_Socios
    {
        // Variable para almacenar la cadena de conexión a la base de datos
        private readonly string _connectionString;

        /// <summary>
        /// Constructor que recibe la configuración de la aplicación
        /// </summary>
        /// <param name="configuration">Objeto de configuración que contiene la cadena de conexión</param>
        public cn_Socios(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentException("La cadena de conexión 'DefaultConnection' no está configurada.");
        }

        /// <summary>
        /// Método que obtiene la lista de socios desde la base de datos
        /// </summary>
        /// <param name="idsocio">ID del socio a buscar. Si es -1, devuelve todos los socios</param>
        /// <returns>Lista de objetos Socio</returns>
        public List<Socio> ListarSocios(int idsocio)
        {
            // Crea una lista vacía para almacenar los resultados
            List<Socio> Socios = new List<Socio>();

            // Crea y administra la conexión a la base de datos (se cerrará automáticamente al salir del bloque)
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                // Abre la conexión a la base de datos
                conn.Open();

                // Crea un comando para ejecutar el procedimiento almacenado
                MySqlCommand cmd = new MySqlCommand("SP_SELECT_SOCIOS", conn);

                // Especifica que el comando es un procedimiento almacenado
                cmd.CommandType = CommandType.StoredProcedure;

                // Agrega el parámetro para el procedimiento almacenado
                // Si idsocio es -1, se pasa null para obtener todos los socios
                // Si no, se filtra por el idsocio especificado
                if (idsocio != -1)
                    cmd.Parameters.AddWithValue("@p_idsocio", idsocio);
                else
                    cmd.Parameters.AddWithValue("@p_idsocio", null);

                // Ejecuta el comando y obtiene un lector de datos
                using (var reader = cmd.ExecuteReader())
                {
                    // Itera sobre cada fila de resultados
                    while (reader.Read())
                    {
                        // Crea un nuevo objeto Socio para cada registro
                        Socio socioTemp = new Socio();

                        // Lee cada columna del registro y asigna al objeto Socio

                        // Obtiene el ID del socio como entero
                        socioTemp.idsocio = Convert.ToInt32(reader["idsocio"]);

                        // Obtiene el RUT del socio como entero
                        socioTemp.rut = Convert.ToInt32(reader["rut"]);

                        // Obtiene la fecha de solicitud, si es nula usa DateTime.MinValue
                        socioTemp.fecha_solicitud = reader["fecha_solicitud"] != DBNull.Value ?
                            Convert.ToDateTime(reader["fecha_solicitud"]) : DateTime.MinValue;

                        // Obtiene la fecha de aprobación, puede ser nula
                        socioTemp.fecha_aprobacion = reader["fecha_aprobacion"] != DBNull.Value ?
                            Convert.ToDateTime(reader["fecha_aprobacion"]) : (DateTime?)null;

                        // Obtiene el estado de la solicitud (pendiente, aprobada, rechazada)
                        socioTemp.estado_solicitud = Convert.ToString(reader["estado_solicitud"]);

                        // Obtiene el motivo de rechazo (si existe)
                        socioTemp.motivo_rechazo = Convert.ToString(reader["motivo_rechazo"]);

                        // Obtiene el documento de identidad (blob) si existe
                        socioTemp.documento_identidad = reader["documento_identidad"] != DBNull.Value ?
                            (byte[])reader["documento_identidad"] : null;

                        // Obtiene el documento de domicilio (blob) si existe
                        socioTemp.documento_domicilio = reader["documento_domicilio"] != DBNull.Value ?
                            (byte[])reader["documento_domicilio"] : null;

                        // Obtiene el estado como entero (tinyint) en lugar de booleano
                        socioTemp.estado = Convert.ToInt32(reader["estado"]);

                        // Agrega el socio a la lista de resultados
                        Socios.Add(socioTemp);
                    }
                }

                // Cierra la conexión a la base de datos
                conn.Close();
            }

            // Devuelve la lista de socios
            return Socios;
        }
    }
}