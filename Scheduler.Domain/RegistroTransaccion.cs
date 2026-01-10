namespace Scheduler.Domain
{
    public record RegistroTransaccion
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string? TipoTransaccion { get; set; }
        public decimal Importe { get; set; }
        public string? Observacion { get; set; }
    }
}
