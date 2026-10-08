using AnnotationBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AnnotationBackend.Data;

public class AppDbContext : DbContext
{
  // ：base(options) 是调用基类（DbContext）的构造函数，并传递 DbContextOptions 实例。
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Site> Sites => Set<Site>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 枚举在 PostgreSQL 中以清晰的字符串存储
        // 在 OnModelCreating 方法中，指定要配置的实体是 User。
        modelBuilder.Entity<User>()
        // 选中 User 实体中的 Role 属性。通常在 C# 中，这个属性是一个枚举（例如 UserRole.Admin、UserRole.User）
            .Property(u => u.Role)
            // 告诉 EF Core 使用值转换器，将其持久化为数据库中的字符串类型（如 PostgreSQL/MySQL 中的 varchar 或 text）
            .HasConversion<string>();

        modelBuilder.Entity<TaskItem>()
            .Property(t => t.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Batch>()
            .Property(b => b.Status)
            .HasConversion<string>();

        // 配置外键关联与级联
        modelBuilder.Entity<Batch>()
            .HasOne(b => b.AssignedSite)
            .WithMany(s => s.AssignedBatches)
            .HasForeignKey(b => b.AssignedSiteId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}