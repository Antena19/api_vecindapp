using System;

namespace REST_VECINDAPP.Modelos
{
    /*
     CREACION DE MODELO USUARIO (REPLICAR TABLAS DE LA BASE DATOS) 
     */
    public class Usuario
    {
        public int rut { get; set; }
        public string? dv_rut { get; set; }
        public string? nombre { get; set; }
        public string? apellido_paterno { get; set; }
        public string? apellido_materno { get; set; }
        public string? correo_electronico { get; set; }
        public string? telefono { get; set; }
        public string? direccion { get; set; }
        public string? password { get; set; }
        public DateTime fecha_registro { get; set; } = DateTime.Now;
        public int estado { get; set; } = 1;
        public string? tipo_usuario { get; set; }
        public string? token_recuperacion { get; set; }
        public DateTime? fecha_token_recuperacion { get; set; }
    }
}