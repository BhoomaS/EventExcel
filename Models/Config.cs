using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System;

namespace MemberSummary.Models
{
    public static class Config
    {
        private static IConfiguration _configuration;

        public static void Init(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static string GetJwtTokenKey()
        {
            var key = _configuration["JwtToken:Key"];

            // Ensure the key is at least 256 bits (32 bytes)
            if (key.Length < 32)
            {
                // Pad the key if it is too short (you can replace with your own padding logic)
                key = key.PadRight(32, '0');
            }

            return key;
        }

        //public static string GenerateJwtKey()
        //{
        //    using (var rng = new RNGCryptoServiceProvider())
        //    {
        //        byte[] key = new byte[32]; // 256 bits
        //        rng.GetBytes(key);
        //        return Convert.ToBase64String(key);
        //    }
        //}



    }
}
