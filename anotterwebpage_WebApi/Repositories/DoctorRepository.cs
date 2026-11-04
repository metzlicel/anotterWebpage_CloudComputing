using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace anotterwebpage_WebApi.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly ApplicationDbContext _context;

    public DoctorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Doctor?> GetByIdAsync(int id)
    {
        return _context.Doctors
            .SingleOrDefaultAsync(doctor => doctor.Id == id);
    }

    public Task<Doctor?> GetByUserIdAsync(string userId)
    {
        return _context.Doctors
            .SingleOrDefaultAsync(doctor => doctor.UserId == userId);
    }

    public Task<Doctor?> GetActiveAsync()
    {
        return _context.Doctors.SingleOrDefaultAsync();
    }
}
