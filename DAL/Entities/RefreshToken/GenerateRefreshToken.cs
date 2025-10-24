using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities.RefreshToken
{
    public class GenerateRefreshToken
    {
         public string GeneraterefreshToken()
        {
            var randomByte = new byte[64];
            using (var RG = RandomNumberGenerator.Create())
            {
                RG.GetBytes(randomByte);
                return Convert.ToBase64String(randomByte);
            }
        }
    }
}
