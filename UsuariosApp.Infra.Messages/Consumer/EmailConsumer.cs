using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using UsuarioApp.Domain.Events;
using UsuariosApp.Infra.Messages.Settings;

namespace UsuariosApp.Infra.Messages.Consumer
{
    public class EmailConsumer : BackgroundService
    {
        private readonly RabbitMQSettings _settings;

        public EmailConsumer(RabbitMQSettings settings)
        {
            _settings = settings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost
            };

            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            var consumer = new AsyncEventingBasicConsumer(channel);

            var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("SEU_EMAIL@gmail.com", "SENHA_APP"),
                EnableSsl = true
            };

            stoppingToken.Register(() =>
            {
                channel.CloseAsync();
                connection.CloseAsync();
                smtp.Dispose();
            });

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    return;
                }

                var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                var evento = JsonSerializer.Deserialize<UsuarioCriadoEvent>(message);

                if (evento == null)
                {
                    Console.WriteLine("❌ Evento inválido");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    return;
                }

                var link = $"https://localhost:5236/api/usuario/confirmar-email?token={evento.Token}";

                try
                {
                    using var mail = new MailMessage
                    {
                        From = new MailAddress("SEU_EMAIL@gmail.com"),
                        Subject = "Confirmação de Email",
                        Body = $"Clique no link:\n\n{link}",
                        IsBodyHtml = false
                    };

                    mail.To.Add(evento.Email);

                    await smtp.SendMailAsync(mail);

                    Console.WriteLine($"📧 Email enviado para {evento.Email}");

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erro ao enviar email: {ex.Message}");

                    await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
            };

            await channel.BasicConsumeAsync(
                queue: _settings.QueueName,
                autoAck: false,
                consumer: consumer
            );


            
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}