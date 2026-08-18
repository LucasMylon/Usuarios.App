using Microsoft.EntityFrameworkCore;
using UsuarioApp.Domain.Interfaces;
using UsuarioApp.Domain.Interfaces.Repositories;
using UsuariosApp.Domain.Services;
using UsuariosApp.Infra.Data.Contexts;
using UsuariosApp.Infra.Data.Repositories;
using UsuariosApp.Infra.Messages.Consumer;
using UsuariosApp.Infra.Messages.Settings;
using UsuarioApp.Domain.Settings;




var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Configuração para injeção de dependência
builder.Services.AddTransient<IUsuarioService, UsuarioService>();
builder.Services.AddTransient<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddTransient<IPerfilRepository, PerfilRepository>();
var rabbitMQSettings = builder.Configuration
    .GetRequiredSection("RabbitMQSettings")
    .Get<RabbitMQSettings>()!;
ValidateRabbitMQSettings(rabbitMQSettings);
builder.Services.AddSingleton(rabbitMQSettings);

var emailSettings = builder.Configuration
    .GetRequiredSection("EmailSettings")
    .Get<EmailSettings>()!;
ValidateEmailSettings(emailSettings);
builder.Services.AddSingleton(emailSettings);

var appSettings = builder.Configuration
    .GetRequiredSection("AppSettings")
    .Get<AppSettings>()!;
ValidateAppSettings(appSettings);
builder.Services.AddSingleton(appSettings);

var jwtSettings = builder.Configuration
    .GetRequiredSection("JwtSettings")
    .Get<JwtSettings>()!;
ValidateJwtSettings(jwtSettings);
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddTransient<IEventPublisher, UsuariosApp.Infra.Messages.Publisher.RabbitMQProducer>();

builder.Services.AddHostedService<EmailConsumer>();

var connectionString = builder.Configuration.GetConnectionString("UsuariosAppBD");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("A connection string UsuariosAppBD é obrigatória.");

builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

static void ValidateRabbitMQSettings(RabbitMQSettings settings)
{
    if (string.IsNullOrWhiteSpace(settings.HostName)
        || string.IsNullOrWhiteSpace(settings.UserName)
        || string.IsNullOrWhiteSpace(settings.Password)
        || string.IsNullOrWhiteSpace(settings.VirtualHost)
        || string.IsNullOrWhiteSpace(settings.QueueName)
        || settings.Port is < 1 or > 65535)
    {
        throw new InvalidOperationException("RabbitMQSettings contém valores obrigatórios ausentes ou inválidos.");
    }
}

static void ValidateEmailSettings(EmailSettings settings)
{
    if (string.IsNullOrWhiteSpace(settings.SmtpServer)
        || string.IsNullOrWhiteSpace(settings.User)
        || string.IsNullOrWhiteSpace(settings.Password)
        || settings.Port is < 1 or > 65535)
    {
        throw new InvalidOperationException("EmailSettings contém valores obrigatórios ausentes ou inválidos.");
    }
}

static void ValidateAppSettings(AppSettings settings)
{
    if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri)
        || baseUri.Scheme is not ("http" or "https"))
    {
        throw new InvalidOperationException("AppSettings:BaseUrl deve ser uma URL HTTP ou HTTPS absoluta.");
    }
}

static void ValidateJwtSettings(JwtSettings settings)
{
    if (string.IsNullOrWhiteSpace(settings.SecretKey))
    {
        throw new InvalidOperationException("JwtSettings:SecretKey é obrigatória.");
    }
    try
    {
        var keyBytes = Convert.FromBase64String(settings.SecretKey);
        if (keyBytes.Length < 32) // 256 bits
        {
            throw new InvalidOperationException("JwtSettings:SecretKey deve ter pelo menos 256 bits de comprimento.");
        }
    }
    catch (FormatException)
    {
        throw new InvalidOperationException("JwtSettings:SecretKey deve ser uma string Base64 válida.");
    }
}

public partial class Program { }
