using System;

namespace AsignacionPiezasApp.Models
{
    public class Usuario
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty; // <-- username
        public override string ToString() => string.IsNullOrWhiteSpace(NombreUsuario)
            ? Nombre
            : $"{Nombre} ({NombreUsuario})";
    }
}
