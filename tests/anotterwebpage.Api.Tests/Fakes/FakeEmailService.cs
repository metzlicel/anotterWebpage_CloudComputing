using anotterwebpage_WebApi.Services;

namespace anotterwebpage.Api.Tests.Fakes;

public class FakeEmailService : IEmailService
{
    public Task SendEmailAsync(
        string to,
        string subject,
        string body)
    {
        return Task.CompletedTask;
    }
}