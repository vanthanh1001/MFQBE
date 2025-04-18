using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
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
            var connectionString = "Server=localhost;Database=fitness_app;User ID=sa;Password=sa12345;TrustServerCertificate=True";
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<UserChallenge> UserChallenges { get; set; }
    public DbSet<Nutrition> Nutritions { get; set; }
    public DbSet<WorkoutSession> WorkoutSessions { get; set; }
    public DbSet<ExercisePerformance> ExercisePerformances { get; set; }
    public DbSet<FitnessGoal> FitnessGoals { get; set; }
    public DbSet<Meal> Meals { get; set; }

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
            entity.HasIndex(e => e.FirebaseUid).IsUnique();
            
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

        // Meal configuration
        modelBuilder.Entity<Meal>(entity =>
        {
            entity.ToTable("Meals");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Ingredients).HasMaxLength(1000);
            
            entity.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WorkoutSession configuration
        modelBuilder.Entity<WorkoutSession>(entity =>
        {
            entity.ToTable("WorkoutSessions");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.StartTime).IsRequired();
            entity.Property(e => e.DurationInMinutes).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
            
            entity.HasOne(ws => ws.User)
                .WithMany(u => u.WorkoutSessions)
                .HasForeignKey(ws => ws.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(ws => ws.WorkoutPlan)
                .WithMany()
                .HasForeignKey(ws => ws.WorkoutPlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // ExercisePerformance configuration
        modelBuilder.Entity<ExercisePerformance>(entity =>
        {
            entity.ToTable("ExercisePerformances");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ActualSets).IsRequired();
            entity.Property(e => e.ActualReps).IsRequired();
            entity.Property(e => e.WeightUsed).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
            
            entity.HasOne(ep => ep.WorkoutSession)
                .WithMany(ws => ws.Performances)
                .HasForeignKey(ep => ep.WorkoutSessionId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(ep => ep.Exercise)
                .WithMany()
                .HasForeignKey(ep => ep.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Exercise>()
        .Property(e => e.DetailImageUrls)
        .HasConversion(
            v => string.Join(',', v ?? new List<string>()),
            v => string.IsNullOrEmpty(v) 
                ? new List<string>() 
                : v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
        .HasDefaultValue(new List<string>())
        .IsRequired();
    
        modelBuilder.Entity<Exercise>()
            .Property(e => e.ThumbnailImageUrl)
            .HasDefaultValue(string.Empty)
            .IsRequired();
    }
}