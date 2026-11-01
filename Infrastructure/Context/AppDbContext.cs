using Domian.Entities.AppUsers;
using Domian.Entities.Gamification;
using Domian.Entities.Learning;
using Domian.Entities.Practice;
using Domian.Entities.Progress;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Users
        public DbSet<AppUser> AppUsers { get; set; } = null!;
        public DbSet<UserOnboarding> UserOnboardings { get; set; } = null!;

        // Learning content (authored by admins)
        public DbSet<Course> Courses { get; set; } = null!;
        public DbSet<CourseSection> CourseSections { get; set; } = null!;
        public DbSet<Chapter> Chapters { get; set; } = null!;
        public DbSet<JourneyNode> JourneyNodes { get; set; } = null!;
        public DbSet<Lesson> Lessons { get; set; } = null!;
        public DbSet<LessonTest> LessonTests { get; set; } = null!;
        public DbSet<MiniCourse> MiniCourses { get; set; } = null!;
        public DbSet<LearningPath> LearningPaths { get; set; } = null!;
        public DbSet<LearningPathCourse> LearningPathCourses { get; set; } = null!;

        // Learner progress
        public DbSet<Enrollment> Enrollments { get; set; } = null!;
        public DbSet<NodeCompletion> NodeCompletions { get; set; } = null!;
        public DbSet<LessonSubmission> LessonSubmissions { get; set; } = null!;

        // Practice
        public DbSet<Problem> Problems { get; set; } = null!;
        public DbSet<ProblemTest> ProblemTests { get; set; } = null!;
        public DbSet<ProblemSubmission> ProblemSubmissions { get; set; } = null!;
        public DbSet<SolvedProblem> SolvedProblems { get; set; } = null!;

        // Gamification
        public DbSet<UserStats> UserStats { get; set; } = null!;
        public DbSet<ActivityDay> ActivityDays { get; set; } = null!;
        public DbSet<WeeklyXp> WeeklyXps { get; set; } = null!;
        public DbSet<Follow> Follows { get; set; } = null!;
        public DbSet<CosmeticItem> CosmeticItems { get; set; } = null!;
        public DbSet<DailyQuote> DailyQuotes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Users: emails and usernames are unique among accounts that are not deleted.
            modelBuilder.Entity<AppUser>(user =>
            {
                user.HasIndex(x => x.Email).IsUnique().HasFilter("[IsDeleted] = 0");
                user.HasIndex(x => x.UserName).IsUnique().HasFilter("[IsDeleted] = 0");
            });

            // Content: public keys are unique; deleting a parent removes its children (admins archive
            // instead of deleting anything learners have progress on).
            modelBuilder.Entity<Course>().HasIndex(x => x.Slug).IsUnique();
            modelBuilder.Entity<CourseSection>(section =>
            {
                section.HasOne(x => x.Course).WithMany(x => x.Sections).HasForeignKey(x => x.CourseId);
                section.HasIndex(x => new { x.CourseId, x.Number }).IsUnique();
            });
            modelBuilder.Entity<Chapter>(chapter =>
            {
                chapter.HasOne(x => x.Section).WithMany(x => x.Chapters).HasForeignKey(x => x.SectionId);
                chapter.HasIndex(x => new { x.SectionId, x.Key }).IsUnique();
            });
            modelBuilder.Entity<JourneyNode>(node =>
            {
                node.HasOne(x => x.Chapter).WithMany(x => x.Nodes).HasForeignKey(x => x.ChapterId);
                node.HasIndex(x => x.Key).IsUnique();
            });
            modelBuilder.Entity<Lesson>(lesson =>
            {
                lesson.HasOne(x => x.Node).WithOne(x => x.Lesson).HasForeignKey<Lesson>(x => x.NodeId);
                lesson.HasIndex(x => x.NodeId).IsUnique();
            });
            modelBuilder.Entity<LessonTest>()
                .HasOne(x => x.Lesson).WithMany(x => x.Tests).HasForeignKey(x => x.LessonId);
            modelBuilder.Entity<MiniCourse>(mini =>
            {
                mini.HasOne(x => x.Course).WithMany(x => x.MiniCourses).HasForeignKey(x => x.CourseId);
                mini.HasIndex(x => x.Key).IsUnique();
            });
            modelBuilder.Entity<LearningPath>().HasIndex(x => x.Slug).IsUnique();
            modelBuilder.Entity<LearningPathCourse>(link =>
            {
                link.HasOne(x => x.Path).WithMany(x => x.Courses).HasForeignKey(x => x.PathId);
                link.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
            });

            // Progress: one row per (user, thing).
            modelBuilder.Entity<Enrollment>().HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();
            modelBuilder.Entity<NodeCompletion>().HasIndex(x => new { x.UserId, x.NodeId }).IsUnique();
            modelBuilder.Entity<LessonSubmission>().HasIndex(x => new { x.UserId, x.NodeId, x.SubmittedAt });

            // Practice
            modelBuilder.Entity<Problem>(problem =>
            {
                problem.HasIndex(x => x.Key).IsUnique();
                problem.HasIndex(x => new { x.Kind, x.Language, x.Number });
            });
            modelBuilder.Entity<ProblemTest>()
                .HasOne(x => x.Problem).WithMany(x => x.Tests).HasForeignKey(x => x.ProblemId);
            modelBuilder.Entity<ProblemSubmission>().HasIndex(x => new { x.UserId, x.ProblemId, x.SubmittedAt });
            modelBuilder.Entity<SolvedProblem>().HasIndex(x => new { x.UserId, x.ProblemId }).IsUnique();

            // Gamification
            modelBuilder.Entity<ActivityDay>().HasIndex(x => new { x.UserId, x.Day }).IsUnique();
            modelBuilder.Entity<WeeklyXp>(weekly =>
            {
                weekly.HasIndex(x => new { x.UserId, x.WeekStart }).IsUnique();
                weekly.HasIndex(x => new { x.WeekStart, x.Xp });
            });
            modelBuilder.Entity<Follow>().HasIndex(x => new { x.FollowerId, x.FolloweeId }).IsUnique();
            modelBuilder.Entity<CosmeticItem>().HasIndex(x => x.Key).IsUnique();
        }
    }
}
