namespace anotterwebpage_WebApi.Services;

public interface IIdentityProfileInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task InitializeDoctorAsync(
        string userId,
        string email,
        CancellationToken cancellationToken = default);
}
