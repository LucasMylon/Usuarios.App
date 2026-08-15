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
        private readonly RabbitMQSettings _rabbitMQSettings;
        private readonly EmailSettings _emailSettings;
        private readonly AppSettings _appSettings;

        public EmailConsumer(
            RabbitMQSettings rabbitMQSettings,
            EmailSettings emailSettings,
            AppSettings appSettings)
        {
            _rabbitMQSettings = rabbitMQSettings;
            _emailSettings = emailSettings;
            _appSettings = appSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMQSettings.HostName,
                Port = _rabbitMQSettings.Port,
                UserName = _rabbitMQSettings.UserName,
                Password = _rabbitMQSettings.Password,
                VirtualHost = _rabbitMQSettings.VirtualHost
            };

            await using var connection = await factory.CreateConnectionAsync(stoppingToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(
                queue: _rabbitMQSettings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken
            );

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    return;
                }

                UsuarioCriadoEvent? evento;

                try
                {
                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    evento = JsonSerializer.Deserialize<UsuarioCriadoEvent>(message);
                }
                catch (JsonException)
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    return;
                }

                if (evento == null)
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    return;
                }

                var baseUrl = _appSettings.BaseUrl.TrimEnd('/');
                var token = Uri.EscapeDataString(evento.Token);
                var link = $"{baseUrl}/api/usuario/confirmar-email?token={token}";

                try
                {
                    using var smtp = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port)
                    {
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(_emailSettings.User, _emailSettings.Password),
                        EnableSsl = true
                    };

                    using var mail = new MailMessage
                    {
                        From = new MailAddress(_emailSettings.User),
                        Subject = "Confirmação de Email",
                        Body = $"Clique no link:\n\n{link}",
                        IsBodyHtml = false
                    };

                    mail.To.Add(evento.Email);

                    await smtp.SendMailAsync(mail, stoppingToken);

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erro ao enviar email: {ex.Message}");

                    await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
            };

            await channel.BasicConsumeAsync(
                queue: _rabbitMQSettings.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken
            );


            
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
