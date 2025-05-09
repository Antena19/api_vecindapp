namespace REST_VECINDAPP.Modelos.DTOs
{
    public class SolicitudMembresia
    {
        public int Rut { get; set; }
        public string? DocumentoIdentidad { get; set; }   // <--- CAMBIADO
        public string? DocumentoDomicilio { get; set; }   // <--- CAMBIADO
    }
}
