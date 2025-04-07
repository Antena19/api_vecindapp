namespace REST_VECINDAPP.Modelos
{
    /*
     CREACION DE MODELO USUARIO (REPLICAR TABLAS DE LA BASE DATOS) 
     */
    public class Usuario
    {
        public int rut { get; set; }
        public string dv_rut { get; set; }
        public string nombre { get; set; }
        public string apellido_paterno { get; set; }
        public string apellido_materno { get; set; }
        public string correo_electronico { get; set; }
        public int telefono { get; set; }
        public string direccion { get; set; }
        public string password { get; set; }
        public DateTime fecha_registro { get; set; }
        public bool estado { get; set; }
        public string tipo_usuario { get; set; }
    }
}
