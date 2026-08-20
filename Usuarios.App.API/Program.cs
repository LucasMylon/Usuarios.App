using Microsoft.EntityFrameworkCore;
using UsuarioApp.Domain.Interfaces;
using UsuarioApp.Domain.Interfaces.Repositories;
using UsuariosApp.Domain.Services;
using UsuariosApp.Infra.Data.Contexts;
using UsuariosApp.Infra.Data.Repositories;
using UsuariosApp.Infra.Messages.Consumer;
using UsuariosApp.Infra.Messages.Settings;
using UsuarioApp.Domain.Settings;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using UsuarioApp.Domain.Interfaces.Security;
using Usuarios.App.API.Errors;
using Usuarios.App.API.Services;
using UsuariosApp.Infra.Messages.Sms;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;




var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("account", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

//Configuração para injeção de dependência
builder.Services.AddTransient<IUsuarioService, UsuarioService>();
builder.Services.AddTransient<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddTransient<IPerfilRepository, PerfilRepository>();
builder.Services.AddTransient<IUsuarioTokenRepository, UsuarioTokenRepository>();
builder.Services.AddSingleton<IPasswordService, AspNetPasswordService>();
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

var recoverySettings = builder.Configuration
    .GetRequiredSection("RecoverySettings")
    .Get<RecoverySettings>()!;
ValidateRecoverySettings(recoverySettings);
builder.Services.AddSingleton(recoverySettings);

var smsSettings = builder.Configuration
    .GetRequiredSection("SmsSettings")
    .Get<SmsSettings>()!;
if (!builder.Environment.IsDevelopment()
    || !string.Equals(smsSettings.Provider, "Development", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Nenhum provedor SMS de produção foi configurado. Development só pode ser usado no ambiente Development.");
}
builder.Services.AddSingleton(smsSettings);
builder.Services.AddTransient<ISmsSender, DevelopmentSmsSender>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSettings.SecretKey)),

            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,

            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var idValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var versionValue = context.Principal?.FindFirstValue("security_version");
                if (!Guid.TryParse(idValue, out var id)
                    || !int.TryParse(versionValue, out var version))
                {
                    context.Fail("Token inválido.");
                    return;
                }

                var repository = context.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();
                var usuario = await repository.GetWithProfileByIdAsync(
                    id,
                    context.HttpContext.RequestAborted);
                if (usuario is null || !usuario.Ativo || usuario.VersaoSeguranca != version)
                    context.Fail("Token revogado.");
            }
        };
    });

builder.Services.AddTransient<IEventPublisher, UsuariosApp.Infra.Messages.Publisher.RabbitMQProducer>();

builder.Services.AddHostedService<EmailConsumer>();

var connectionString = builder.Configuration.GetConnectionString("UsuariosAppBD");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("A connection string UsuariosAppBD é obrigatória.");

builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(connectionString));

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();

app.UseRateLimiter();

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

    if (string.IsNullOrWhiteSpace(settings.Issuer))
    {
        throw new InvalidOperationException("JwtSettings:Issuer é obrigatória.");
    }
    if (string.IsNullOrWhiteSpace(settings.Audience))
    {
        throw new InvalidOperationException("JwtSettings:Audience é obrigatória.");
    }
    if (settings.ExpirationMinutes is < 1 or > 1440)
    {
        throw new InvalidOperationException("JwtSettings:ExpirationMinutes deve ser um valor entre 1 e 1440.");
    }
}

static void ValidateRecoverySettings(RecoverySettings settings)
{
    if (settings.LinkExpirationMinutes is < 5 or > 1440
        || settings.SmsCodeExpirationMinutes is < 1 or > 30
        || settings.MaxCodeAttempts is < 1 or > 10
        || settings.RequestCooldownSeconds is < 10 or > 3600)
    {
        throw new InvalidOperationException("RecoverySettings contém valores inválidos.");
    }
}

public partial class Program { }
