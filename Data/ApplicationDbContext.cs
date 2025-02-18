using Microsoft.EntityFrameworkCore;
using Models;
using Models.Enums;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = "Server=localhost;Port=3306;Database=fitness_app;User=root;Password=sa12345";
            optionsBuilder.UseMySQL(connectionString);
        }
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<UserChallenge> UserChallenges { get; set; }
    public DbSet<Nutrition> Nutritions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).HasDefaultValue(UserRole.User);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        });

        // Exercise configuration
        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.ToTable("Exercises");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Sets).IsRequired();
            entity.Property(e => e.Reps).IsRequired();
            entity.Property(e => e.RestTime).IsRequired();
            entity.Property(e => e.CreatedById).IsRequired();

            entity.HasOne(e => e.CreatedBy)
                .WithMany(u => u.CreatedExercises)
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WorkoutPlan configuration
        modelBuilder.Entity<WorkoutPlan>(entity =>
        {
            entity.ToTable("WorkoutPlans");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.UserId).IsRequired();

            entity.HasOne(w => w.User)
                .WithMany(u => u.WorkoutPlans)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Many-to-Many relationship
            entity.HasMany(w => w.Exercises)
                .WithMany(e => e.WorkoutPlans)
                .UsingEntity(j => j.ToTable("WorkoutPlanExercises"));
        });

        // Challenge configuration
        modelBuilder.Entity<Challenge>(entity =>
        {
            entity.ToTable("Challenges");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // UserChallenge configuration
        modelBuilder.Entity<UserChallenge>(entity =>
        {
            entity.ToTable("UserChallenges");
            entity.HasKey(e => new { e.UserId, e.ChallengeId });

            entity.HasOne(uc => uc.User)
                .WithMany(u => u.UserChallenges)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(uc => uc.Challenge)
                .WithMany(c => c.UserChallenges)
                .HasForeignKey(uc => uc.ChallengeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Nutrition configuration
        modelBuilder.Entity<Nutrition>(entity =>
        {
            entity.ToTable("Nutritions");
            entity.HasKey(e => e.Id);

            entity.HasOne(n => n.User)
                .WithMany(u => u.Nutritions)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
} 