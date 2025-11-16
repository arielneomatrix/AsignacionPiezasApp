using System;
namespace AsignacionPiezasApp.Models
{
    public class EstatusPieza
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public override string ToString() => Nombre;
    }
}