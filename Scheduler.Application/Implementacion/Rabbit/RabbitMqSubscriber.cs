using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Scheduler.Application.Implementacion.Rabbit
{
    public class RabbitMqSubscriber : IRabbitMqSubscriber, IAsyncDisposable
    {
        private readonly ILogger<RabbitMqSubscriber> _logger;
        private readonly IConfiguration _configuration;

        private IConnection? _connection;
        private IModel? _channel;
        private AsyncEventingBasicConsumer? _consumer;
        private string _queueName = "task_queue";
        private string _consumerTag = string.Empty;

        public RabbitMqSubscriber(ILogger<RabbitMqSubscriber> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _queueName = _configuration["RabbitMQ:Queue"] ?? _queueName;
        }

        public Task StartAsync(Func<string, Task> onMessage, CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                UserName = _configuration["RabbitMQ:User"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: _queueName,
                                  durable: true,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);

            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    await onMessage(message).ConfigureAwait(false);
                    _channel?.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar mensaje de RabbitMQ.");
                    _channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _consumerTag = _channel.BasicConsume(queue: _queueName,
                                                autoAck: false,
                                                consumer: _consumer);

            _logger.LogInformation("RabbitMqSubscriber iniciado. Cola: '{queue}'", _queueName);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!string.IsNullOrEmpty(_consumerTag) && _channel is not null)
                {
                    _channel.BasicCancel(_consumerTag);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cancelando consumer de RabbitMQ.");
            }

            DisposeResources();
            return Task.CompletedTask;
        }

        private void DisposeResources()
        {
            try
            {
                _channel?.Close();
                _channel?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cerrando recursos de RabbitMQ.");
            }
            finally
            {
                _channel = null;
                _connection = null;
                _consumer = null;
                _consumerTag = string.Empty;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Run(() => DisposeResources()).ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }
}
