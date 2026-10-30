using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StaySmart.Models;

namespace StaySmart.Data;

public class HotelDbContext(DbContextOptions<HotelDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingHistory> BookingHistories => Set<BookingHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // creates the ASP.NET Identity tables

        modelBuilder.Entity<ApplicationUser>().Property(u => u.FullName).HasMaxLength(100);

        modelBuilder.Entity<Room>(e =>
        {
            e.HasIndex(r => r.RoomNumber).IsUnique();
            e.Property(r => r.PricePerNight).HasPrecision(10, 2);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Rooms_Price", "[PricePerNight] > 0");
                t.HasCheckConstraint("CK_Rooms_Capacity", "[Capacity] BETWEEN 1 AND 10");
            });
        });

        modelBuilder.Entity<BookingHistory>(e =>
        {
            e.Property(h => h.ChangedAtUtc).HasColumnType("datetime2");
            e.HasIndex(h => h.BookingId);
            e.HasOne(h => h.Booking).WithMany(b => b.History)
                .HasForeignKey(h => h.BookingId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.Property(b => b.TotalPrice).HasPrecision(10, 2);
            e.HasIndex(b => b.BookingReference).IsUnique();
            e.HasIndex(b => new { b.RoomId, b.CheckIn, b.CheckOut });
            e.Property(b => b.BookingReference).HasMaxLength(8);
            e.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(b => b.CheckIn).HasColumnType("date");
            e.Property(b => b.CheckOut).HasColumnType("date");
            e.Property(b => b.CreatedAtUtc).HasColumnType("datetime2");

            e.HasOne(b => b.Room).WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId).OnDelete(DeleteBehavior.Restrict);
            // Keep booking history if a guest account is ever deleted.
            e.HasOne(b => b.User).WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.SetNull);

            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Bookings_Dates", "[CheckOut] > [CheckIn]");
                t.HasCheckConstraint("CK_Bookings_Guests", "[Guests] BETWEEN 1 AND 10");
                t.HasCheckConstraint("CK_Bookings_Status", "[Status] IN ('Confirmed','CheckedIn','CheckedOut','Cancelled')");
            });
        });
    }
}
