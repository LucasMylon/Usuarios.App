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
        public void DeveCriarUsuarioComSucesso()
        {
            var request = new CriarContaRequest(
                Nome: _faker.Person.FullName,
                Email: _faker.Internet.Email(),
                Senha: "@Teste2025"
                );
            var response = _client.PostAsJsonAsync("api/usuario/Criar", request).Result;

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact(
            DisplayName = "Não deve permitir criar usuários com o mesmo email."
            
        )]
        public void NaoDevePermitirCriarUsuariosComMesmoEmail()
        {
            var request = new CriarContaRequest(
                Nome: _faker.Person.FullName,
                Email: _faker.Internet.Email(),
                Senha: "@Teste2025"
                );
            var response1 = _client.PostAsJsonAsync("api/usuario/Criar", request).Result;
            response1.StatusCode.Should().Be(HttpStatusCode.Created);

            var response2 = _client.PostAsJsonAsync("api/usuario/Criar", request).Result;
            response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = response2.Content.ReadAsStringAsync().Result;
            content.Should().Contain("O email informado já está cadastrado.");
        }

        [Fact(
            DisplayName = "Deve obrigar o preenchimento de senha forte."          
        )]
        public void DeveObrigarPreenchimentoDeSenhaForte()
        {
            var request = new CriarContaRequest(
               Nome: _faker.Person.FullName,
               Email: _faker.Internet.Email(),
               Senha: "123"
               );
            var response = _client.PostAsJsonAsync("api/usuario/Criar", request).Result;
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = response.Content.ReadAsStringAsync().Result;
            
            content.Should().Contain("A senha do usuário deve conter pelo menos uma letra maiúscula.");
            content.Should().Contain("A senha do usuário deve conter pelo menos uma letra minúscula.");
            
            content.Should().Contain("A senha do usuário deve conter pelo menos um caractere especial.");

        }

        [Fact(
            DisplayName = "Deve autenticar um usuário com sucesso."
            
        )]
        public void DeveAutenticarUsuarioComSucesso()
        {
            var requestCriarConta = new CriarContaRequest(
               Nome: _faker.Person.FullName,
               Email: _faker.Internet.Email(),
               Senha: "@Teste2025"
               );
            var responseCriar = _client.PostAsJsonAsync("api/usuario/Criar", requestCriarConta).Result;
            responseCriar.StatusCode.Should().Be(HttpStatusCode.Created);

            var requestAutenticar = new AutenticarUsuarioRequest(
                Email : requestCriarConta.Email,
                Senha : requestCriarConta.Senha
                );

            var responseAutenticar = _client.PostAsJsonAsync
                ("api/usuario/Autenticar", requestAutenticar).Result;
            responseAutenticar.StatusCode.Should().Be(HttpStatusCode.OK);
            
            
        }

        [Fact(
            DisplayName = "Deve retornar acesso negado para usuário inválido."
        )]
        public void DeveRetornarAcessoNegadoParaUsuarioInvalido()
        {
            var requestAutenticar = new AutenticarUsuarioRequest(
                Email: _faker.Internet.Email(),
                Senha: _faker.Internet.Password()
                );

            var responseAutenticar = _client.PostAsJsonAsync
                ("api/usuario/Autenticar", requestAutenticar).Result;
            responseAutenticar.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
          
        }
    }
}