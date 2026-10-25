using System;
using System.Globalization;

namespace CursedCrown
{
    // Shared input rules for desktop, headset menus and Editor checks.
    public static class SessionInput
    {
        public const int MaxRelayCodeLength = 12;
        public static bool TryAddress(string value, out string address)
        {
            address = (value ?? "").Trim();
            if (string.Equals(address, "localhost", StringComparison.OrdinalIgnoreCase)) address = "127.0.0.1";
            string[] parts = address.Split('.');
            if (parts.Length != 4) return false;
            var normalized = new string[4];
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length < 1 || parts[i].Length > 3) return false;
                foreach (char c in parts[i]) if (c < '0' || c > '9') return false;
                if (!byte.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out byte part)) return false;
                normalized[i] = part.ToString(CultureInfo.InvariantCulture);
            }
            address = string.Join(".", normalized);
            byte first = byte.Parse(normalized[0], CultureInfo.InvariantCulture);
            return first > 0 && first < 224; // Reject unspecified and multicast/broadcast destinations.
        }
        public static bool TryRelayCode(string value, out string code)
        {
            code = (value ?? "").Trim().ToUpperInvariant();
            if (code.Length == 0 || code.Length > MaxRelayCodeLength) return false;
            foreach (char c in code)
                if (!(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9')) return false;
            return true;
        }
    }
}
