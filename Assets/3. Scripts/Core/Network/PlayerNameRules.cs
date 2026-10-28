using System.Text;
using UnityEngine;

namespace PangPangShotNetwork
{
    // 플레이어 이름 규칙: 메뉴에서 입력 → 연결 토큰으로 서버 전달 → 서버가 정리해서 Entity에 동기화
    public static class PlayerNameRules
    {
        public const int MaxLength = 12;
        private const string PrefsKey = "PlayerName";

        // 빈 이름이면 Player000 ~ Player999
        public static string RandomName() => $"Player{Random.Range(0, 1000):D3}";

        // 앞뒤 공백 제거, 리치 텍스트 태그 문자(<, >) 제거, 길이 제한
        public static string Sanitize(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            var sb = new StringBuilder(MaxLength);
            foreach (char c in name.Trim())
            {
                if (c == '<' || c == '>' || char.IsControl(c)) continue;
                sb.Append(c);
                if (sb.Length >= MaxLength) break;
            }
            return sb.ToString().Trim();
        }

        public static string OrRandom(string name)
        {
            string clean = Sanitize(name);
            return clean.Length > 0 ? clean : RandomName();
        }

        public static byte[] ToToken(string name) => Encoding.UTF8.GetBytes(Sanitize(name));

        public static string FromToken(byte[] token)
            => token == null || token.Length == 0 ? string.Empty : Sanitize(Encoding.UTF8.GetString(token));

        public static string LoadSaved() => PlayerPrefs.GetString(PrefsKey, string.Empty);

        public static void Save(string name)
        {
            PlayerPrefs.SetString(PrefsKey, name);
            PlayerPrefs.Save();
        }
    }
}
