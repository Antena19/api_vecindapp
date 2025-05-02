using MySqlConnector;        // Importa la biblioteca para conectar con MySQL
using REST_VECINDAPP.Modelos; // Importa los modelos de la aplicación
using System.Data;           // Importa System.Data para trabajar con bases de datos
using REST_VECINDAPP.Modelos.DTOs;

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

        /// <summary>
        /// Método para solicitar membresía como socio
        /// </summary>
        /// <param name="rut">RUT del vecino que solicita</param>
        /// <param name="documentoIdentidad">Imagen del documento de identidad</param>
        /// <param name="documentoDomicilio">Comprobante de domicilio</param>
        /// <returns>Mensaje de resultado</returns>
        public string SolicitarMembresia(int rut, byte[] documentoIdentidad, byte[] documentoDomicilio)
        {
            string mensaje = string.Empty;

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                using (MySqlCommand cmd = new MySqlCommand("SP_SOLICITAR_MEMBRESIA_SOCIO", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@p_rut", rut);
                    cmd.Parameters.AddWithValue("@p_documento_identidad", documentoIdentidad);
                    cmd.Parameters.AddWithValue("@p_documento_domicilio", documentoDomicilio);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            mensaje = reader["mensaje"].ToString();
                        }
                    }
                }

                conn.Close();
            }

            return mensaje;
        }

        /// <summary>
        /// Método para obtener las solicitudes de socios
        /// </summary>
        /// <param name="estadoSolicitud">Estado de solicitud a filtrar (pendiente, aprobada, rechazada). Si es null, trae todas</param>
        /// <returns>Lista de solicitudes con datos del usuario</returns>
        public List<SolicitudSocioDTO> ConsultarSolicitudes(string estadoSolicitud = null)
        {
            List<SolicitudSocioDTO> solicitudes = new List<SolicitudSocioDTO>();

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                using (MySqlCommand cmd = new MySqlCommand("SP_CONSULTAR_SOLICITUDES_SOCIOS", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@p_estado_solicitud", estadoSolicitud ?? "");

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SolicitudSocioDTO solicitud = new SolicitudSocioDTO
                            {
                                Rut = Convert.ToInt32(reader["rut"]),
                                Nombre = reader["nombre"].ToString(),
                                ApellidoPaterno = reader["apellido_paterno"].ToString(),
                                ApellidoMaterno = reader["apellido_materno"].ToString(),
                                FechaSolicitud = Convert.ToDateTime(reader["fecha_solicitud"]),
                                EstadoSolicitud = reader["estado_solicitud"].ToString(),
                                MotivoRechazo = reader["motivo_rechazo"].ToString()
                            };

                            solicitudes.Add(solicitud);
                        }
                    }
                }

                conn.Close();
            }

            return solicitudes;
        }

        /// <summary>
        /// Método para aprobar una solicitud de socio
        /// </summary>
        /// <param name="rut">RUT del solicitante</param>
        /// <returns>Mensaje de resultado</returns>
        public string AprobarSolicitud(int rut)
        {
            string mensaje = string.Empty;

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                using (MySqlCommand cmd = new MySqlCommand("SP_APROBAR_SOLICITUD_SOCIO", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@p_rut", rut);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            mensaje = reader["mensaje"].ToString();
                        }
                    }
                }

                conn.Close();
            }

            return mensaje;
        }

        /// <summary>
        /// Método para rechazar una solicitud de socio
        /// </summary>
        /// <param name="rut">RUT del solicitante</param>
        /// <param name="motivoRechazo">Motivo del rechazo</param>
        /// <returns>Mensaje de resultado</returns>
        public string RechazarSolicitud(int rut, string motivoRechazo)
        {
            string mensaje = string.Empty;

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                using (MySqlCommand cmd = new MySqlCommand("SP_RECHAZAR_SOLICITUD_SOCIO", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@p_rut", rut);
                    cmd.Parameters.AddWithValue("@p_motivo_rechazo", motivoRechazo);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            mensaje = reader["mensaje"].ToString();
                        }
                    }
                }

                conn.Close();
            }

            return mensaje;
        }

        public EstadisticasResponse ObtenerEstadisticas()
        {
            var response = new EstadisticasResponse();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    try
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand("sp_obtener_estadisticas_socios", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            using (var reader = cmd.ExecuteReader())
                            {
                                // Verificar si hay resultados
                                if (!reader.HasRows)
                                {
                                    // Inicializar con valores por defecto si no hay datos
                                    response.Estadisticas = new EstadisticasSocios
                                    {
                                        TotalSocios = 0,
                                        SolicitudesPendientes = 0,
                                        SociosActivos = 0,
                                        SociosInactivos = 0
                                    };
                                }
                                else if (reader.Read())
                                {
                                    response.Estadisticas = new EstadisticasSocios
                                    {
                                        TotalSocios = reader["total_socios"] != DBNull.Value ? Convert.ToInt32(reader["total_socios"]) : 0,
                                        SolicitudesPendientes = reader["solicitudes_pendientes"] != DBNull.Value ? Convert.ToInt32(reader["solicitudes_pendientes"]) : 0,
                                        SociosActivos = reader["socios_activos"] != DBNull.Value ? Convert.ToInt32(reader["socios_activos"]) : 0,
                                        SociosInactivos = reader["socios_inactivos"] != DBNull.Value ? Convert.ToInt32(reader["socios_inactivos"]) : 0
                                    };
                                }

                                // Verificar si hay un segundo conjunto de resultados
                                if (reader.NextResult())
                                {
                                    response.UltimasActividades = new List<Actividad>();
                                    while (reader.Read())
                                    {
                                        response.UltimasActividades.Add(new Actividad
                                        {
                                            Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                                            Titulo = reader["titulo"] != DBNull.Value ? reader["titulo"].ToString() : string.Empty,
                                            Descripcion = reader["descripcion"] != DBNull.Value ? reader["descripcion"].ToString() : string.Empty,
                                            Fecha = reader["fecha"] != DBNull.Value ? Convert.ToDateTime(reader["fecha"]) : DateTime.Now,
                                            Icono = reader["icono"] != DBNull.Value ? reader["icono"].ToString() : string.Empty,
                                            Color = reader["color"] != DBNull.Value ? reader["color"].ToString() : string.Empty
                                        });
                                    }
                                }
                                else
                                {
                                    // Inicializar con lista vacía si no hay segundo conjunto
                                    response.UltimasActividades = new List<Actividad>();
                                }
                            }
                        }
                    }
                    catch (MySqlException sqlEx)
                    {
                        // Log específico para errores de SQL
                        Console.WriteLine($"Error de MySQL: {sqlEx.Message}, Número: {sqlEx.Number}");
                        throw;
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                            conn.Close();
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                // Log general para cualquier otro error
                Console.WriteLine($"Error general: {ex.Message}");
                throw;
            }
        }
    }
}