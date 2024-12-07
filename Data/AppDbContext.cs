using CustomIdentity.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CustomIdentity.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<ObjectDetection> ObjectDetections { get; set; }
        public DbSet<Models.Object> Objects { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<Camera> Cameras { get; set; }
        public DbSet<Intersection> Intersections { get; set; }
        public DbSet<VideoRequest> VideoRequests { get; set; }
        public DbSet<ObjectDetectionCount> ObjectDetectionCounts { get; set; }
        public DbSet<GetProbabilityDistributionByObjectID> GetProbabilityDistributionByObjectID { get; set; }
        public DbSet<GetDetectionTimeObjectCountsByRequestID> GetDetectionTimeObjectCountsByRequestID { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal properties for the Object entity
            modelBuilder.Entity<Models.Object>(entity =>
            {
                entity.Property(e => e.CO)
                    .HasColumnType("decimal(18, 4)");

                entity.Property(e => e.MinThreshold)
                    .HasColumnType("decimal(18, 4)");

                entity.Property(e => e.NOX)
                    .HasColumnType("decimal(18, 4)");

                entity.Property(e => e.PM)
                    .HasColumnType("decimal(18, 4)");

                entity.Property(e => e.VOC)
                    .HasColumnType("decimal(18, 4)");
            });

            // Configure decimal properties for the ObjectDetection entity
            modelBuilder.Entity<ObjectDetection>(entity =>
            {
                entity.Property(e => e.Probability)
                    .HasColumnType("decimal(18, 4)");
            });

            // You can add other entity configurations as necessary
        }
    }
}
