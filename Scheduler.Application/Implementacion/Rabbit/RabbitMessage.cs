namespace Scheduler.Application.Implementacion.Rabbit
{
    public record RabbitMessage
    {
        public int Banco { get; init; } = 0;
        public  string Path { get; init; }
    }
}
