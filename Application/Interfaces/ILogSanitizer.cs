namespace LogLens.Application.Interfaces
{
    public interface ILogSanitizer
    {
        string Sanitize(string message);
    }
}