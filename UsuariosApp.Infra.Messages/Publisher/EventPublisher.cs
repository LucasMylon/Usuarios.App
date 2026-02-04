
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UsuarioApp.Domain.Interfaces;
using UsuariosApp.Infra.Messages.Settings;

namespace UsuariosApp.Infra.Messages.Publisher
{
    public class RabbitMQProducer(RabbitMQSettings settings) : IEventPublisher
    {
        public void Publish<TEvent>(TEvent evento)
        {
            Task.Run(async () => {

                //Conexão com o RabbitMQ
                var factory = new ConnectionFactory
                {
                    HostName = settings.HostName,
                    Port = settings.Port,
                    UserName = settings.UserName,
                    Password = settings.Password,
                    VirtualHost = settings.VirtualHost
                };

                //Fazendo a conexão
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                //Declarando a fila
                await channel.QueueDeclareAsync(
                    queue: settings.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                //Convertendo a mensagem para bytes
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evento));

                //Publicando a mensagem na fila
                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: settings.QueueName,
                    body: body
                );
            });
        }

       
        
    }
}
