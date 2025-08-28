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
        /// <summary>
        /// 길이 16의 대문자(A–Z)와 숫자(0–9)로 구성된 암호학적으로 안전한 무작위 UID를 생성합니다.
        /// </summary>
        /// <remarks>
        /// 내부적으로 System.Security.Cryptography.RandomNumberGenerator로 난수를 생성하여 각 바이트를 문자 집합의 인덱스로 매핑합니다.
        /// </remarks>
        /// <returns>생성된 16자 UID 문자열.</returns>
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