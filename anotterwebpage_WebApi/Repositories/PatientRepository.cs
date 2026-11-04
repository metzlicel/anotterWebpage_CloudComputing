using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace anotterwebpage_WebApi.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly ApplicationDbContext _context;

    public PatientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Patient?> GetByIdAsync(int id)
    {
        return _context.Patients
            .SingleOrDefaultAsync(patient => patient.Id == id);
    }

    public Task<Patient?> GetByUserIdAsync(string userId)
    {
        return _context.Patients
            .SingleOrDefaultAsync(patient => patient.UserId == userId);
    }
}
