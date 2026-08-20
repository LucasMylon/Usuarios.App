using System.Net.Mail;
using System.Text.RegularExpressions;

namespace UsuariosApp.Domain.Helpers;

public static partial class AccountDataHelper
{
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static string NormalizePhone(string telefone)
    {
        var normalized = telefone.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (!PhoneRegex().IsMatch(normalized))
            throw new ApplicationException("O telefone deve estar no formato internacional, por exemplo +5511999999999.");
        return normalized;
    }

    public static void ValidateEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            if (!string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase))
                throw new FormatException();
        }
        catch (FormatException)
        {
            throw new ApplicationException("O e-mail informado é inválido.");
        }
    }

    public static void ValidatePassword(string senha)
    {
        if (senha.Length is < 8 or > 128
            || !senha.Any(char.IsUpper)
            || !senha.Any(char.IsLower)
            || !senha.Any(char.IsDigit)
            || senha.All(char.IsLetterOrDigit))
        {
            throw new ApplicationException("A nova senha deve ter entre 8 e 128 caracteres, com maiúscula, minúscula, número e caractere especial.");
        }
    }

    [GeneratedRegex("^\\+[1-9][0-9]{7,14}$", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();
}
