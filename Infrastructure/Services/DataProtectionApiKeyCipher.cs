using LogLens.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace LogLens.Infrastructure.Services
{
    public class DataProtectionApiKeyCipher : IApiKeyCipher
    {
        private const string Purpose = "LogLens.ApiKeyCipher";
        private readonly IDataProtector _protector;

        public DataProtectionApiKeyCipher(IDataProtectionProvider dataProtectionProvider)
        {
            _protector = dataProtectionProvider.CreateProtector(Purpose);
        }

        public string Protect(string rawApiKey) => _protector.Protect(rawApiKey);

        public string? Unprotect(string? protectedValue)
        {
            if (string.IsNullOrWhiteSpace(protectedValue))
            {
                return null;
            }

            return _protector.Unprotect(protectedValue);
        }
    }
}