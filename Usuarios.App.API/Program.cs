using Microsoft.EntityFrameworkCore;
using UsuarioApp.Domain.Interfaces;
using UsuarioApp.Domain.Interfaces.Repositories;
using UsuarioApp.Domain.Interfaces.Services;
using UsuariosApp.Domain.Services;
using UsuariosApp.Infra.Data.Contexts;
using UsuariosApp.Infra.Data.Repositories;
using UsuariosApp.Infra.Messages.Settings;
using UsuariosApp.Infra.Messages.Publisher;

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
builder.Services.AddSingleton(builder.Configuration.GetSection("RabbitMQSettings").Get<RabbitMQSettings>());

builder.Services.AddTransient<IEventPublisher, UsuariosApp.Infra.Messages.Publisher.RabbitMQProducer>();

builder.Services.AddDbContext<DataContext>(options=>options.UseSqlServer(builder.Configuration.GetConnectionString("UsuariosAppBD")));

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

public partial class Program { }