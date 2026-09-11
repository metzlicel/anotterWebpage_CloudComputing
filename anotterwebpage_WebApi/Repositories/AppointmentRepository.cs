using Microsoft.EntityFrameworkCore;
using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Repositories;

public class AppointmentRepository : IAppointmentRepository 
{
    private readonly ApplicationDbContext _context;

    public AppointmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Appointment>> GetAllAsync()
    {
        return await _context.Appointments
            .ToListAsync();
    }
    // public Task<Appointment?> GetByIdAsync(int id);


    public async Task<List<Appointment>> GetBusyAsync(
        DateTime start,
        DateTime end)
    {
        return await _context.Appointments
            .Where(a => a.Start < end && a.End > start)
            .ToListAsync();
    }

    public async Task<Appointment?> GetByIdAsync(int id)
    {
        return await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<bool> HasOverlapAsync(
        DateTime start,
        DateTime end)
    {
        return await _context.Appointments
            .AnyAsync(a => a.Start < end && a.End > start);
    }
    
    public async Task<bool> HasOverlapAsync(
        DateTime start,
        DateTime end,
        int excludeAppointmentId)
    {
        return await _context.Appointments
            .AnyAsync(a =>
                a.Id != excludeAppointmentId &&
                a.Start < end &&
                a.End > start);
    }

    public async Task<Appointment> CreateAsync(
        Appointment appointment)
    {
        _context.Appointments.Add(appointment);

        await _context.SaveChangesAsync();

        return appointment;
    }
    public async Task<Appointment> UpdateAsync(
        Appointment appointment)
    {
        _context.Appointments.Update(appointment);

        await _context.SaveChangesAsync();

        return appointment;
    }
    
    public async Task DeleteAsync(Appointment appointment)
    {
        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();
    }
}