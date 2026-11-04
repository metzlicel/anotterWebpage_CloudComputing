namespace anotterwebpage_WebApi.Services;

public interface IDoctorBootstrapService
{
    Task<DoctorBootstrapResult> BootstrapAsync(
        CancellationToken cancellationToken = default);
}
