using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using UsuarioApp.Domain.Dtos.Requests;

namespace UsuariosApp.Tests
{
    public class UsuariosTest
    {
        private readonly HttpClient _client;
        private readonly Faker _faker;

        public UsuariosTest()
        {
            _client = new WebApplicationFactory<Program>().CreateClient();
            _faker = new Faker("pt_BR");
        }

        [Fact(DisplayName = "Deve criar um novo usuário com sucesso.")]
        public async Task DeveCriarUsuarioComSucesso()
        {
            var request = new CriarContaRequest(
                Nome: _faker.Person.FullName,
                Email: _faker.Internet.Email(),
                Senha: "@Teste2025"
                );
            var response = await _client.PostAsJsonAsync("api/usuario/Criar", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact(
            DisplayName = "Não deve permitir criar usuários com o mesmo email."
            
        )]
        public async Task NaoDevePermitirCriarUsuariosComMesmoEmail()
        {
            var request = new CriarContaRequest(
                Nome: _faker.Person.FullName,
                Email: _faker.Internet.Email(),
                Senha: "@Teste2025"
                );
            var response1 = await _client.PostAsJsonAsync("api/usuario/Criar", request);
            response1.StatusCode.Should().Be(HttpStatusCode.Created);

            var response2 = await _client.PostAsJsonAsync("api/usuario/Criar", request);
            response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = await response2.Content.ReadAsStringAsync();
            content.Should().Contain("O email informado já está cadastrado.");
        }

        [Fact(
            DisplayName = "Deve obrigar o preenchimento de senha forte."          
        )]
        public async Task DeveObrigarPreenchimentoDeSenhaForte()
        {
            var request = new CriarContaRequest(
               Nome: _faker.Person.FullName,
               Email: _faker.Internet.Email(),
               Senha: "123"
               );
            var response = await _client.PostAsJsonAsync("api/usuario/Criar", request);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            
            content.Should().Contain("A senha do usuário deve conter pelo menos uma letra maiúscula.");
            content.Should().Contain("A senha do usuário deve conter pelo menos uma letra minúscula.");
            
            content.Should().Contain("A senha do usuário deve conter pelo menos um caractere especial.");

        }



        
        [Fact(DisplayName = "Não deve autenticar usuário com email não confirmado.")]
        public async Task NaoDeveAutenticarUsuarioComEmailInativo()
        {
            var requestCriarConta = new CriarContaRequest(
               Nome: _faker.Person.FullName,
               Email: _faker.Internet.Email(),
               Senha: "@Teste2025"
               );
            var responseCriar = await _client.PostAsJsonAsync("api/usuario/Criar", requestCriarConta);
            responseCriar.StatusCode.Should().Be(HttpStatusCode.Created);

            var requestAutenticar = new AutenticarUsuarioRequest(
                Email : requestCriarConta.Email,
                Senha : requestCriarConta.Senha
                );

            var responseAutenticar = await _client.PostAsJsonAsync
                ("api/usuario/Autenticar", requestAutenticar);
            responseAutenticar.StatusCode.Should().Be(HttpStatusCode.Unauthorized);


        }

        [Fact(
            DisplayName = "Deve retornar acesso negado para usuário inválido."
        )]
        public async Task DeveRetornarAcessoNegadoParaUsuarioInvalido()
        {
            var requestAutenticar = new AutenticarUsuarioRequest(
                Email: _faker.Internet.Email(),
                Senha: _faker.Internet.Password()
                );

            var responseAutenticar = await _client.PostAsJsonAsync
                ("api/usuario/Autenticar", requestAutenticar);
            responseAutenticar.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
          
        }
    }
}
