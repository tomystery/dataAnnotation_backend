namespace AnnotationBackend.Models;

// 1. 角色定义
public enum UserRole
{
    ProjectManager,   // 内部 PM：立项、分包调度、交付
    QALead,           // 内部质检主管：规则配置、抽检仲裁
    Reviewer,         // 质检员/初审员：单条审核
    Labeler,          // 标注员：人工标注作业
    ClientAdmin,      // 客户管理员：签署验收、预算控制
    ClientInspector   // 客户抽检员：抽样验收、上报驳回
}

// 2. 单条数据项状态
public enum ItemStatus
{
    Imported,         // 数据已导入
    PreAnnotated,     // AI预标注或跳过
    InAnnotation,     // 人工标注中
    PendingReview,    // 待内部初审
    ReviewRejected,   // 内部初审单条驳回
    ReviewPassed      // 内部初审通过
}

// 3. 批次流转状态
public enum BatchStatus
{
    Draft,            // 打包中 / 待分包
    AssignedToSite,   // 已分派到基地
    InternalSampling, // 内部抽检中
    InternalRework,   // 全批返修（回退到基地人工标注）
    Delivered,        // 达到内部SLA -> 交付客户
    ClientSampling,   // 客户抽检中
    ClientRejected,   // 客户驳回整改（回退到人工标注）
    Accepted,         // 验收通过
    Settled           // 验收与结算
}