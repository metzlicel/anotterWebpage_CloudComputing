using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Repositories;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(int id);

    Task<Patient?> GetByUserIdAsync(string userId);
}
