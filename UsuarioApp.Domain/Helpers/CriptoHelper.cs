using System;
using System.Security.Cryptography;
using System.Text;

namespace UsuariosApp.Domain.Helpers
{
    /// <summary>
    /// Classe auxiliar para criptografia
    /// </summary>
    public class CryptoHelper
    {
        /// <summary>
        /// Método para retornar um valor criptografado
        /// com algoritmo SHA256
        /// </summary>
        public static string GetSHA256(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            using (SHA256 sha256 = SHA256.Create())
            {
                // Converte o texto em bytes
                byte[] inputBytes = Encoding.UTF8.GetBytes(value);

                // Gera o hash
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                // Converte o hash para string hexadecimal
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }
}