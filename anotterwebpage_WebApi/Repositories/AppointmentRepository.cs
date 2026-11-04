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
        return await WithProfiles()
            .ToListAsync();
    }

    public async Task<List<Appointment>> GetByPatientIdAsync(int patientId)
    {
        return await WithProfiles()
            .Where(appointment => appointment.PatientId == patientId)
            .ToListAsync();
    }

    public async Task<List<Appointment>> GetByDoctorIdAsync(int doctorId)
    {
        return await WithProfiles()
            .Where(appointment => appointment.DoctorId == doctorId)
            .ToListAsync();
    }

    public async Task<List<Appointment>> GetBusyAsync(
        DateTime start,
        DateTime end)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Where(a => a.Status != AppointmentStatus.Cancelada)
            .Where(a => a.Start < end && a.End > start)
            .ToListAsync();
    }

    public async Task<Appointment?> GetByIdAsync(int id)
    {
        return await WithProfiles()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<bool> HasOverlapAsync(
        DateTime start,
        DateTime end)
    {
        return await _context.Appointments
            .AnyAsync(a =>
                a.Status != AppointmentStatus.Cancelada &&
                a.Start < end &&
                a.End > start);
    }
    
    public async Task<bool> HasOverlapAsync(
        DateTime start,
        DateTime end,
        int excludeAppointmentId)
    {
        return await _context.Appointments
            .AnyAsync(a =>
                a.Id != excludeAppointmentId &&
                a.Status != AppointmentStatus.Cancelada &&
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
    
    private IQueryable<Appointment> WithProfiles()
    {
        return _context.Appointments
            .Include(appointment => appointment.Patient)
            .Include(appointment => appointment.Doctor)
            .Include(appointment => appointment.ConsultationDetail);
    }
}
