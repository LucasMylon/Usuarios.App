using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UsuarioApp.Domain.Dtos.Requests;
using UsuarioApp.Domain.Entities;
using UsuarioApp.Domain.Events;
using UsuarioApp.Domain.Interfaces;
using UsuarioApp.Domain.Interfaces.Repositories;
using UsuarioApp.Domain.Settings;
using UsuariosApp.Domain.Services;

namespace UsuariosApp.Tests;

[CollectionDefinition("VariaveisDeAmbiente", DisableParallelization = true)]
public class VariaveisDeAmbienteCollection;

[Collection("VariaveisDeAmbiente")]
public class ConfirmacaoEmailTest
{
    private static readonly CriarContaRequest Request = new("Usuario Teste", "teste@example.com", "Senha@123");
    private static readonly JwtSettings JwtSettings = new()
    {
        SecretKey = Convert.ToBase64String(new byte[32]),
        Issuer = "testes",
        Audience = "testes"
    };

    [Fact]
    public async Task CadastroSemConfirmacaoAtivaContaEPermiteAutenticacao()
    {
        var usuarios = new UsuarioRepositoryFake();
        var publisher = new EventPublisherFake();
        var service = new UsuarioService(usuarios, new PerfilRepositoryFake(), publisher, JwtSettings, true);

        await service.CriarContaAsync(Request);

        Assert.NotNull(usuarios.Usuario);
        Assert.True(usuarios.Usuario.Ativo);
        Assert.Null(usuarios.Usuario.EmailConfirmacaoToken);
        Assert.Empty(publisher.Eventos);
        var autenticacao = await service.AutenticarUsuarioAsync(new(Request.Email, Request.Senha));
        Assert.False(string.IsNullOrWhiteSpace(autenticacao.AccessToken));
    }

    [Fact]
    public async Task CadastroPadraoExigeConfirmacaoEPublicaEvento()
    {
        var usuarios = new UsuarioRepositoryFake();
        var publisher = new EventPublisherFake();
        var service = new UsuarioService(usuarios, new PerfilRepositoryFake(), publisher, JwtSettings);

        await service.CriarContaAsync(Request);

        Assert.NotNull(usuarios.Usuario);
        Assert.False(usuarios.Usuario.Ativo);
        Assert.False(string.IsNullOrWhiteSpace(usuarios.Usuario.EmailConfirmacaoToken));
        var evento = Assert.IsType<UsuarioCriadoEvent>(Assert.Single(publisher.Eventos));
        Assert.Equal(usuarios.Usuario.EmailConfirmacaoToken, evento.Token);
        await Assert.ThrowsAsync<ApplicationException>(() => service.AutenticarUsuarioAsync(new(Request.Email, Request.Senha)));

        await service.ConfirmarEmailAsync(evento.Token);
        Assert.True(usuarios.Usuario.Ativo);
        Assert.Null(usuarios.Usuario.EmailConfirmacaoToken);
        var autenticacao = await service.AutenticarUsuarioAsync(new(Request.Email, Request.Senha));
        Assert.False(string.IsNullOrWhiteSpace(autenticacao.AccessToken));
        await Assert.ThrowsAsync<ApplicationException>(() => service.ConfirmarEmailAsync(evento.Token));
    }

    [Fact]
    public void CadastroPadraoExigePublisher()
    {
        Assert.Throws<ArgumentNullException>(() => new UsuarioService(new UsuarioRepositoryFake(), new PerfilRepositoryFake(), null, JwtSettings));
    }

    [Fact]
    public async Task ApiEmDesenvolvimentoFuncionaSemSmtpOuRabbitMq()
    {
        using var ambiente = new AmbienteTemporario(new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["Desenvolvimento__IgnorarConfirmacaoEmail"] = "true",
            ["EmailSettings__User"] = " ",
            ["EmailSettings__Password"] = " ",
            ["RabbitMQSettings__HostName"] = " ",
            ["ConnectionStrings__UsuariosAppBD"] = "Server=unused;Database=unused;Trusted_Connection=True",
            ["JwtSettings__SecretKey"] = JwtSettings.SecretKey,
            ["Database__ApplyMigrations"] = "false"
        });
        var usuarios = new UsuarioRepositoryFake();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUsuarioRepository>();
                services.RemoveAll<IPerfilRepository>();
                services.AddSingleton<IUsuarioRepository>(usuarios);
                services.AddSingleton<IPerfilRepository>(new PerfilRepositoryFake());
            });
        });
        using var client = factory.CreateClient();

        var cadastro = await client.PostAsJsonAsync("api/usuario/Criar", Request);
        var autenticacao = await client.PostAsJsonAsync("api/usuario/autenticar", new AutenticarUsuarioRequest(Request.Email, Request.Senha));

        Assert.Equal(HttpStatusCode.Created, cadastro.StatusCode);
        Assert.Equal(HttpStatusCode.OK, autenticacao.StatusCode);
        Assert.NotNull(usuarios.Usuario);
        Assert.True(usuarios.Usuario.Ativo);
        Assert.Null(usuarios.Usuario.EmailConfirmacaoToken);
    }

    [Fact]
    public void ApiComConfirmacaoHabilitadaValidaConfiguracaoSmtp()
    {
        using var ambiente = new AmbienteTemporario(new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["Desenvolvimento__IgnorarConfirmacaoEmail"] = "false",
            ["RabbitMQSettings__HostName"] = "localhost",
            ["RabbitMQSettings__Port"] = "5672",
            ["RabbitMQSettings__UserName"] = "teste",
            ["RabbitMQSettings__Password"] = "teste",
            ["RabbitMQSettings__VirtualHost"] = "/",
            ["RabbitMQSettings__QueueName"] = "teste",
            ["EmailSettings__User"] = " ",
            ["EmailSettings__Password"] = " "
        });
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

        var erro = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("EmailSettings", erro.Message);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void ApiRecusaIgnorarConfirmacaoForaDeDesenvolvimento(string ambiente)
    {
        using var variaveis = new AmbienteTemporario(new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = ambiente,
            ["Desenvolvimento__IgnorarConfirmacaoEmail"] = "true"
        });
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment(ambiente));

        var erro = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("apenas em desenvolvimento", erro.Message);
    }

    private sealed class AmbienteTemporario : IDisposable
    {
        private readonly Dictionary<string, string?> _anteriores = new();

        public AmbienteTemporario(Dictionary<string, string?> variaveis)
        {
            foreach (var (nome, valor) in variaveis)
            {
                _anteriores[nome] = Environment.GetEnvironmentVariable(nome);
                Environment.SetEnvironmentVariable(nome, valor);
            }
        }

        public void Dispose()
        {
            foreach (var (nome, valor) in _anteriores)
                Environment.SetEnvironmentVariable(nome, valor);
        }
    }

    private sealed class EventPublisherFake : IEventPublisher
    {
        public List<object> Eventos { get; } = new();

        public Task PublishAsync<TEvent>(TEvent evento, CancellationToken cancellationToken = default)
        {
            Eventos.Add(evento!);
            return Task.CompletedTask;
        }
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        public Usuario? Usuario { get; private set; }

        public Task AddAsync(Usuario entity, CancellationToken cancellationToken = default)
        {
            entity.Perfil = new Perfil { Id = entity.PerfilId, Nome = "USUARIO" };
            Usuario = entity;
            return Task.CompletedTask;
        }

        public Task<bool> AnyAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Usuario?.Email == email);

        public Task<Usuario?> GetAsync(string email, string senha, CancellationToken cancellationToken = default) =>
            Task.FromResult(Usuario?.Email == email && Usuario.Senha == senha ? Usuario : null);

        public Task<Usuario?> GetByEmailConfirmacaoTokenAsync(string token, CancellationToken cancellationToken = default) =>
            Task.FromResult(Usuario?.EmailConfirmacaoToken == token ? Usuario : null);

        public Task UpdateAsync(Usuario entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Usuario entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Usuario>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Usuario?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class PerfilRepositoryFake : IPerfilRepository
    {
        public Task<Perfil?> GetAsync(string nome, CancellationToken cancellationToken = default) =>
            Task.FromResult<Perfil?>(new Perfil { Nome = nome });

        public Task AddAsync(Perfil entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Perfil entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Perfil entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Perfil>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Perfil?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
