using System.Text.RegularExpressions;
using LogLens.Application.Interfaces;

namespace LogLens.Application.Services
{
    public partial class LogSanitizer : ILogSanitizer
    {
        public string Sanitize(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Empty;
            }

            var result = message;
            result = GuidRegex().Replace(result, "<*>");
            result = IpRegex().Replace(result, "<*>");
            result = QuotedStringRegex().Replace(result, "<*>");
            result = NumberRegex().Replace(result, "<*>");
            return MultiPlaceholderRegex().Replace(result, "<*>").Trim();
        }

        [GeneratedRegex(@"\b[0-9a-fA-F]{8}\-(?:[0-9a-fA-F]{4}\-){3}[0-9a-fA-F]{12}\b", RegexOptions.Compiled)]
        private static partial Regex GuidRegex();

        [GeneratedRegex(@"\b(?:25[0-5]|2[0-4]\d|1?\d?\d)(?:\.(?:25[0-5]|2[0-4]\d|1?\d?\d)){3}\b", RegexOptions.Compiled)]
        private static partial Regex IpRegex();

        [GeneratedRegex("\"[^\"]*\"|'[^']*'", RegexOptions.Compiled)]
        private static partial Regex QuotedStringRegex();

        [GeneratedRegex(@"\b\d+(?:\.\d+)?\b", RegexOptions.Compiled)]
        private static partial Regex NumberRegex();

        [GeneratedRegex(@"(?:<\*>\s*){2,}", RegexOptions.Compiled)]
        private static partial Regex MultiPlaceholderRegex();
    }
}