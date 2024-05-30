using System.Security.Cryptography;
using System.Text;

namespace DownModel
{
    public static class HashHelper
    {
        public static string Hash256(this string text)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] hashBytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(text));

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        public static string Hash256(this Stream stream)
        {
            stream.Position = 0;
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] hashBytes = sha256Hash.ComputeHash(stream);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        public static string Hash256(this byte[] bytes)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] hashBytes = sha256Hash.ComputeHash(bytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}
