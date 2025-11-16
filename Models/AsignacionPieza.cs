using System;
namespace AsignacionPiezasApp.Models
{
    public class AsignacionPieza
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public Guid? UsuarioId { get; set; }
        public Guid? EstatusId { get; set; }
        public string? FotoPath { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}