using MySqlConnector;
using REST_VECINDAPP.Modelos;
using System.Data;

namespace REST_VECINDAPP.CapaNegocios
{
    public class cn_Socios
    {
        private readonly string _connectionString;
        public cn_Socios(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public List<Socio> ListarSocios(int idsocio)
        {
            List<Socio> Socios = new List<Socio>();
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SP_SELECT_SOCIOS", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                if (idsocio != -1)
                    cmd.Parameters.AddWithValue("@p_idsocio", idsocio);
                else
                    cmd.Parameters.AddWithValue("@p_idsocio", null);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Socio socioTemp = new Socio();
                        socioTemp.idsocio = Convert.ToInt32(reader["idsocio"]);
                        socioTemp.rut = Convert.ToInt32(reader["rut"]);
                        socioTemp.fecha_solicitud = reader["fecha_solicitud"] != DBNull.Value ? Convert.ToDateTime(reader["fecha_solicitud"]) : DateTime.MinValue;
                        socioTemp.fecha_aprobacion = reader["fecha_aprobacion"] != DBNull.Value ? Convert.ToDateTime(reader["fecha_aprobacion"]) : (DateTime?)null;
                        socioTemp.estado_solicitud = Convert.ToString(reader["estado_solicitud"]);
                        socioTemp.motivo_rechazo = Convert.ToString(reader["motivo_rechazo"]);
                        socioTemp.documento_identidad = reader["documento_identidad"] != DBNull.Value    ? (byte[])reader["documento_identidad"]    : null;
                        socioTemp.documento_domicilio = reader["documento_domicilio"] != DBNull.Value ? (byte[])reader["documento_domicilio"] : null;
                        socioTemp.estado = Convert.ToBoolean(reader["estado"]);
                        
                        /* if (int.TryParse(reader["ID_PERFIL"].ToString(), out int output))
                        {
                            usuarioTemp.idPerfil = Convert.ToInt32(reader["ID_PERFIL"]);
                        }
                        usuarioTemp.activo = Convert.ToBoolean(reader["ACTIVO"]);*/
                        Socios.Add(socioTemp);
                    }
                }
                conn.Close();
            }
            return Socios;
        }
    }
}

    

