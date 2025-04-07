namespace REST_VECINDAPP.Modelos
{
    public class Socio
    {
        public int idsocio { get; set; }
        public int rut { get; set; }
        public DateTime fecha_solicitud { get; set; }
        public DateTime? fecha_aprobacion { get; set; }
        public string estado_solicitud { get; set; }
        public string motivo_rechazo { get; set; }
        public byte[] documento_identidad { get; set; }
        public byte[] documento_domicilio { get; set; }
        public bool estado { get; set; }
    }
}
