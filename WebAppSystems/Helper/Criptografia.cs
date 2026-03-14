using System.Security.Cryptography;
using System.Text;

namespace WebAppSystems.Helper
{
    public static class Criptografia
    {
        // Novo hash seguro com BCrypt (work factor 12)
        public static string GerarHash(this string valor)
        {
            return BCrypt.Net.BCrypt.HashPassword(valor, workFactor: 12);
        }

        // Verifica senha suportando tanto BCrypt (novo) quanto SHA1 (legado)
        public static bool VerificarSenha(string senhaDigitada, string hashArmazenado)
        {
            if (string.IsNullOrEmpty(hashArmazenado)) return false;

            // BCrypt hashes começam com $2a$, $2b$ ou $2y$
            if (hashArmazenado.StartsWith("$2"))
            {
                return BCrypt.Net.BCrypt.Verify(senhaDigitada, hashArmazenado);
            }

            // Fallback: SHA1 legado (40 chars hex)
            return senhaDigitada.GerarHashSHA1() == hashArmazenado;
        }

        // SHA1 mantido apenas para migração de senhas legadas
        public static string GerarHashSHA1(this string valor)
        {
            var hash = SHA1.Create();
            var encoding = new ASCIIEncoding();
            var array = encoding.GetBytes(valor);
            array = hash.ComputeHash(array);
            var strHexa = new StringBuilder();
            foreach (var item in array)
            {
                strHexa.Append(item.ToString("x2"));
            }
            return strHexa.ToString();
        }
    }
}
