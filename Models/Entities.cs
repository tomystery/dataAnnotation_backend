namespace AnnotationBackend.Models;

// 1. 基地 / 供应商物理职场（如：河南基地、山东职场）
public class Site
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // 导航属性：基地下的所有人员
    public List<User> Members { get; set; } = new();
    // 导航属性：分配给该基地的批次
    public List<Batch> AssignedBatches { get; set; } = new();
}

// 2. 系统用户
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    // 基地归属（内部人员如 Labeler/Reviewer 必填）
    public Guid? SiteId { get; set; }
    public Site? Site { get; set; }

    // 客户归属（仅 ClientAdmin / ClientInspector 使用，实现多租户隔离）
    public Guid? ClientId { get; set; }
}

// 3. 项目实体（由 PM 负责立项）
public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid ClientId { get; set; } // 属于哪个客户

    public Guid ProjectManagerId { get; set; } // 负责该项目的总部 PM
    public Guid? QALeadId { get; set; }        // 负责该项目的质检主管

    public List<Batch> Batches { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// 4. 任务批次（分包调度的核心载体）
public class Batch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BatchNumber { get; set; } = string.Empty; // 例：BATCH-202610-001
    public BatchStatus Status { get; set; } = BatchStatus.Draft;

    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    // 关键字段：当前批次分派给哪一个基地负责打标
    public Guid? AssignedSiteId { get; set; }
    public Site? AssignedSite { get; set; }

    public List<TaskItem> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// 5. 单条标注任务项
public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid? BatchId { get; set; }
    public Batch? Batch { get; set; }

    public string RawDataUrl { get; set; } = string.Empty;  // 待标注文件/图片存储地址
    public string? AnnotationData { get; set; }             // 标注结果（JSON 字符串）
    public ItemStatus Status { get; set; } = ItemStatus.Imported;

    public Guid? LabelerId { get; set; }                   // 当前作业的标注员
    public Guid? ReviewerId { get; set; }                  // 初审质检员
    public string? RejectReason { get; set; }              // 驳回原因说明

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}