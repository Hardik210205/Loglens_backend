namespace LogLens.Application.Interfaces
{
    public interface IApiKeyCipher
    {
        string Protect(string rawApiKey);
        string? Unprotect(string? protectedValue);
    }
}