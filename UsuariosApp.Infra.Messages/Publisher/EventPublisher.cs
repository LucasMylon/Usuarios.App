using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using UsuarioApp.Domain.Interfaces;
using UsuariosApp.Infra.Messages.Settings;

namespace UsuariosApp.Infra.Messages.Publisher;

public class RabbitMQProducer(RabbitMQSettings settings) : IEventPublisher
{
    public async Task Publish<TEvent>(TEvent evento)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            VirtualHost = settings.VirtualHost
        };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evento));

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: settings.QueueName,
            body: body
        );

        Console.WriteLine("🔥 Mensagem enviada pro RabbitMQ");
    }
}