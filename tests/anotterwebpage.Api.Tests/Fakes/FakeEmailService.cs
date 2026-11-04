using anotterwebpage_WebApi.Services;

namespace anotterwebpage.Api.Tests.Fakes;

public class FakeEmailService : IEmailService
{
    public List<(string To, string Subject, string Body)> SentEmails { get; } = [];
    public int FailuresRemaining { get; set; }

    public void Reset()
    {
        SentEmails.Clear();
        FailuresRemaining = 0;
    }

    public Task SendEmailAsync(
        string to,
        string subject,
        string body)
    {
        if (FailuresRemaining > 0)
        {
            FailuresRemaining--;
            throw new InvalidOperationException("Temporary SMTP failure.");
        }

        SentEmails.Add((to, subject, body));
        return Task.CompletedTask;
    }
}
