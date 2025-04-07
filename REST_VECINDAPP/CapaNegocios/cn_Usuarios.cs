using MySqlConnector;
using REST_VECINDAPP.Modelos;
using System.Data;

namespace REST_VECINDAPP.CapaNegocios
{
    //
    public class cn_Usuarios
    {
        private readonly string _connectionString;
        public cn_Usuarios(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public List<Usuario> ListarUsuarios(int rut)
        {
            List<Usuario> Usuarios = new List<Usuario>();
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SP_SELECT_USUARIOS", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                if (rut != -1)
                    cmd.Parameters.AddWithValue("@p_rut", rut);
                else
                    cmd.Parameters.AddWithValue("@p_rut", null);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Usuario usuarioTemp = new Usuario();
                        usuarioTemp.rut = Convert.ToInt32(reader["rut"]);
                        usuarioTemp.dv_rut = Convert.ToString(reader["dv_rut"]);
                        usuarioTemp.nombre = Convert.ToString(reader["nombre"]);
                        usuarioTemp.apellido_paterno = Convert.ToString(reader["apellido_paterno"]);
                        usuarioTemp.apellido_materno = Convert.ToString(reader["apellido_materno"]);
                        usuarioTemp.correo_electronico = Convert.ToString(reader["correo_electronico"]);
                        usuarioTemp.telefono = Convert.ToInt32(reader["telefono"]);
                        usuarioTemp.direccion = Convert.ToString(reader["direccion"]);
                        usuarioTemp.password = Convert.ToString(reader["password"]);
                        usuarioTemp.fecha_registro = Convert.ToDateTime(reader["fecha_registro"]);
                        usuarioTemp.estado = Convert.ToBoolean(reader["estado"]);
                        usuarioTemp.tipo_usuario = Convert.ToString(reader["tipo_usuario"]);

                        /* if (int.TryParse(reader["ID_PERFIL"].ToString(), out int output))
                        {
                            usuarioTemp.idPerfil = Convert.ToInt32(reader["ID_PERFIL"]);
                        }
                        usuarioTemp.activo = Convert.ToBoolean(reader["ACTIVO"]);*/
                        Usuarios.Add(usuarioTemp);
                    }
                }
                conn.Close();
            }
            return Usuarios;
        }
    }
}
