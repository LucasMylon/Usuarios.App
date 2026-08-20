using FluentAssertions;
using UsuarioApp.Domain.Entities;
using Usuarios.App.API.Services;
using UsuariosApp.Domain.Helpers;

namespace UsuariosApp.Tests;

public class SecurityTests
{
    private readonly AspNetPasswordService _passwords = new();

    [Fact]
    public void HashDeSenhaNaoDeveArmazenarTextoPuro()
    {
        var usuario = new Usuario();
        var hash = _passwords.Hash(usuario, "@SenhaSegura2026");

        hash.Should().NotBe("@SenhaSegura2026");
        _passwords.Verify(usuario, hash, "@SenhaSegura2026").Should().BeTrue();
        _passwords.Verify(usuario, hash, "senha-errada").Should().BeFalse();
    }

    [Fact]
    public void MesmaSenhaDeveGerarHashesDiferentesPorCausaDoSalt()
    {
        var usuario = new Usuario();

        var primeiro = _passwords.Hash(usuario, "@SenhaSegura2026");
        var segundo = _passwords.Hash(usuario, "@SenhaSegura2026");

        primeiro.Should().NotBe(segundo);
    }

    [Fact]
    public void TokenDeLinkDeveSerAleatorioESeuHashDeterministico()
    {
        var primeiro = TokenHelper.GenerateLinkToken();
        var segundo = TokenHelper.GenerateLinkToken();

        primeiro.Should().NotBe(segundo);
        TokenHelper.HashLinkToken(primeiro).Should().Be(TokenHelper.HashLinkToken(primeiro));
        TokenHelper.HashLinkToken(primeiro).Should().NotBe(TokenHelper.HashLinkToken(segundo));
    }

    [Theory]
    [InlineData("+55 (11) 99999-9999", "+5511999999999")]
    [InlineData("+351 912 345 678", "+351912345678")]
    public void TelefoneDeveSerNormalizadoParaFormatoInternacional(string entrada, string esperado)
    {
        AccountDataHelper.NormalizePhone(entrada).Should().Be(esperado);
    }
}
