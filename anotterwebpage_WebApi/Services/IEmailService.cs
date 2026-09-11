namespace anotterwebpage_WebApi.Services;

public interface IEmailService
{
    Task SendEmailAsync(
        string to,
        string subject,
        string body);
}