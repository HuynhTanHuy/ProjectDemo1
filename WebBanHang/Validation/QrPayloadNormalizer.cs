using System.Globalization;
using System.Text.RegularExpressions;

namespace WebBanHang.Validation
{
    /// <summary>Chuẩn hóa &amp; validate payload QR phía server — không tin tưởng client.</summary>
    public static partial class QrPayloadNormalizer
    {
        private const int MaxPayloadLength = 64;

        [GeneratedRegex(@"^COPY-\d{1,20}$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex CopyCodePattern();

        /// <summary>Trả về true nếu là BookCopyId thuần số hoặc mã COPY-&lt;số&gt;.</summary>
        public static bool TryParseBookCopyPayload(string? raw, out int? bookCopyId, out string? normalizedCopyCode)
        {
            bookCopyId = null;
            normalizedCopyCode = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var s = raw.Trim();
            if (s.Length == 0 || s.Length > MaxPayloadLength)
            {
                return false;
            }

            if (s.All(char.IsAsciiDigit))
            {
                if (!int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
                {
                    return false;
                }

                bookCopyId = id;
                return true;
            }

            if (!CopyCodePattern().IsMatch(s))
            {
                return false;
            }

            normalizedCopyCode = s.ToUpperInvariant();
            return true;
        }

        /// <summary>Token thẻ thành viên: chỉ chữ/số, độ dài cố định hợp lệ.</summary>
        public static bool TryParseMemberToken(string? raw, out string? token)
        {
            token = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var s = raw.Trim();
            if (s.Length is < 16 or > 64)
            {
                return false;
            }

            if (!s.All(char.IsAsciiLetterOrDigit))
            {
                return false;
            }

            token = s;
            return true;
        }
    }
}
