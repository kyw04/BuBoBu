using System.Text;
using Fusion.Sockets;
using UnityEngine;

namespace PangPangShotNetwork
{
    // 로비 러너에서 클라이언트 ↔ 서버가 주고받는 방 생성 요청/응답 규칙
    public static class LobbyProtocol
    {
        public const string DefaultLobbySession = "bubobu-lobby";
        public const int CodeLength = 6;

        // 헷갈리는 문자(0/O, 1/I/L) 제외
        private const string CodeChars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        public static readonly ReliableKey CreateRoomRequest = ReliableKey.FromInts(1, 0, 0, 0);
        public static readonly ReliableKey CreateRoomResponse = ReliableKey.FromInts(2, 0, 0, 0);

        public static string GenerateCode()
        {
            var sb = new StringBuilder(CodeLength);
            for (int i = 0; i < CodeLength; i++)
                sb.Append(CodeChars[Random.Range(0, CodeChars.Length)]);
            return sb.ToString();
        }

        public static string NormalizeCode(string input)
            => string.IsNullOrWhiteSpace(input) ? string.Empty : input.Trim().ToUpperInvariant();

        public static byte[] EncodeCode(string code)
            => string.IsNullOrEmpty(code) ? new byte[] { 0 } : Encoding.UTF8.GetBytes(code);

        // 실패 응답은 0 한 바이트
        public static string DecodeCode(byte[] data)
            => data == null || data.Length == 0 || (data.Length == 1 && data[0] == 0)
                ? null
                : Encoding.UTF8.GetString(data);
    }
}
