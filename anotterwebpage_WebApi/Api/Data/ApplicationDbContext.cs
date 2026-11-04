using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<AppointmentNotification> AppointmentNotifications { get; set; }
        public DbSet<AppointmentConsultationDetail> AppointmentConsultationDetails { get; set; }
        public DbSet<Collab> Collabs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Appointment>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Start).IsRequired();
                entity.Property(a => a.End).IsRequired();
                entity.Property(a => a.Motivo).IsRequired();
                entity.Property(a => a.Status).IsRequired();

                entity.HasOne(a => a.Patient)
                    .WithMany(p => p.Appointments)
                    .HasForeignKey(a => a.PatientId)
                    .IsRequired();

                entity.HasOne(a => a.Doctor)
                    .WithMany(d => d.Appointments)
                    .HasForeignKey(a => a.DoctorId)
                    .IsRequired();
            });

            builder.Entity<AppointmentNotification>(entity =>
            {
                entity.HasKey(notification => notification.Id);
                entity.Property(notification => notification.EventKey).IsRequired();
                entity.Property(notification => notification.RecipientEmail).IsRequired();
                entity.Property(notification => notification.Subject).IsRequired();
                entity.Property(notification => notification.Body).IsRequired();
                entity.Property(notification => notification.Type).IsRequired();
                entity.Property(notification => notification.CreatedAt).IsRequired();
                entity.Property(notification => notification.NextAttemptAt).IsRequired();
                entity.HasIndex(notification => new
                    {
                        notification.EventKey,
                        notification.RecipientEmail
                    })
                    .IsUnique();

                entity.HasOne(notification => notification.Appointment)
                    .WithMany(appointment => appointment.Notifications)
                    .HasForeignKey(notification => notification.AppointmentId)
                    .IsRequired();
            });

            builder.Entity<AppointmentConsultationDetail>(entity =>
            {
                entity.HasKey(detail => detail.Id);
                entity.Property(detail => detail.Diagnostico).IsRequired();
                entity.Property(detail => detail.Recomendaciones).IsRequired();
                entity.Property(detail => detail.CreatedAt).IsRequired();
                entity.Property(detail => detail.UpdatedAt).IsRequired();
                entity.HasIndex(detail => detail.AppointmentId).IsUnique();

                entity.HasOne(detail => detail.Appointment)
                    .WithOne(appointment => appointment.ConsultationDetail)
                    .HasForeignKey<AppointmentConsultationDetail>(detail => detail.AppointmentId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Patient>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.UserId).IsRequired();
                entity.HasIndex(p => p.UserId).IsUnique();
                entity.Property(p => p.Nombre).IsRequired();
                entity.Property(p => p.Apellido).IsRequired();
                entity.Property(p => p.Numero).IsRequired();

                entity.HasOne<IdentityUser>()
                    .WithOne()
                    .HasForeignKey<Patient>(p => p.UserId)
                    .IsRequired();
            });

            builder.Entity<Doctor>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.UserId).IsRequired();
                entity.HasIndex(d => d.UserId).IsUnique();
                entity.Property(d => d.Email).IsRequired();

                entity.HasOne<IdentityUser>()
                    .WithOne()
                    .HasForeignKey<Doctor>(d => d.UserId)
                    .IsRequired();
            });

            builder.Entity<Collab>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Nombre).IsRequired();
                entity.Property(a => a.Apellido).IsRequired();
                entity.Property(a => a.Email).IsRequired();
                entity.Property(a => a.Numero).IsRequired();
                entity.Property(a => a.Motivo).IsRequired();
            });
        }
    }
}
