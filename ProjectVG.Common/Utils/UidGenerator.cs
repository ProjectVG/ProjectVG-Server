using ProjectVG.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ProjectVG.Common.Utils
{
    public class UidGenerator
    {
        private const int UID_LENGTH = 16;
        private const string UID_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        /// <summary>
        /// 랜덤 UID 생성 (길이 보장)
        /// </summary>
        public static string GenerateRandomUID()
        {
            var randomBytes = new byte[UID_LENGTH];
            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(randomBytes);
            }

            var uid = new char[UID_LENGTH];
            for (int i = 0; i < UID_LENGTH; i++) {
                uid[i] = UID_CHARS[randomBytes[i] % UID_CHARS.Length];
            }

            return new string(uid);
        }
    }
}