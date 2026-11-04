using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Repositories;

public interface IDoctorRepository
{
    Task<Doctor?> GetByIdAsync(int id);

    Task<Doctor?> GetByUserIdAsync(string userId);

    Task<Doctor?> GetActiveAsync();
}
