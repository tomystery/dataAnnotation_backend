using AnnotationBackend.Data;
using AnnotationBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AnnotationBackend.Services;

public class WorkflowService
{
  private readonly AppDbContext _context;

  public WorkflowService(AppDbContext context)
  {
    _context = context;
  }

  // 1. PM 分包：将批次下发给指定基地
  public async Task AssignBatchToSiteAsync(Guid batchId, Guid siteId)
  {
    var batch = await _context.Batches.FindAsync(batchId)
        ?? throw new InvalidOperationException("批次不存在");

    var site = await _context.Sites.FindAsync(siteId)
        ?? throw new InvalidOperationException("目标基地不存在");

    batch.AssignedSiteId = site.Id;
    batch.Status = BatchStatus.AssignedToSite;
    await _context.SaveChangesAsync();
  }

  // 2. 基地标注员领取/提交单条标注
  public async Task SubmitAnnotationAsync(Guid itemId, Guid labelerId, string annotationData)
  {
    var item = await _context.TaskItems.FindAsync(itemId)
        ?? throw new InvalidOperationException("任务项不存在");

    var user = await _context.Users.FindAsync(labelerId)
        ?? throw new InvalidOperationException("用户不存在");

    if (user.Role != UserRole.Labeler)
      throw new InvalidOperationException("非标注员无法提交标注");

    item.AnnotationData = annotationData;
    item.LabelerId = labelerId;
    item.Status = ItemStatus.PendingReview; // 进入待内部初审[cite: 1]
    item.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();
  }

  // 3. 内部初审（Reviewer）：单条通过或单条驳回[cite: 1]
  public async Task ReviewItemAsync(Guid itemId, Guid reviewerId, bool isPassed, string? rejectReason)
  {
    var item = await _context.TaskItems.FindAsync(itemId)
        ?? throw new InvalidOperationException("任务项不存在");

    if (item.Status != ItemStatus.PendingReview)
      throw new InvalidOperationException("任务项未在初审等待队列中");

    item.ReviewerId = reviewerId;
    item.UpdatedAt = DateTime.UtcNow;

    if (isPassed)
    {
      item.Status = ItemStatus.ReviewPassed; // 初审通过[cite: 1]
      item.RejectReason = null;
    }
    else
    {
      item.Status = ItemStatus.ReviewRejected; // 单条驳回至人工标注[cite: 1]
      item.RejectReason = rejectReason;
    }

    await _context.SaveChangesAsync();
  }

  // 4. 内部主管抽检（QALead）：全批返修 或 达到SLA交付客户[cite: 1]
  public async Task InternalSampleReviewAsync(Guid batchId, bool reachedSla)
  {
    var batch = await _context.Batches.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == batchId)
        ?? throw new InvalidOperationException("批次不存在");

    if (reachedSla)
    {
      batch.Status = BatchStatus.Delivered; // 达到内部SLA -> 交付客户[cite: 1]
    }
    else
    {
      batch.Status = BatchStatus.InternalRework; // 全批返修[cite: 1]
                                                 // 将该批次所有数据退回到人工标注流程[cite: 1]
      foreach (var item in batch.Items)
      {
        item.Status = ItemStatus.InAnnotation;
      }
    }

    await _context.SaveChangesAsync();
  }

  // 5. 客户抽检（ClientInspector）：驳回整改 或 验收通过[cite: 1]
  public async Task ClientInspectAsync(Guid batchId, bool isAccepted)
  {
    var batch = await _context.Batches.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == batchId)
        ?? throw new InvalidOperationException("批次不存在");

    if (isAccepted)
    {
      batch.Status = BatchStatus.Accepted; // 验收通过 -> 进入结算[cite: 1]
    }
    else
    {
      batch.Status = BatchStatus.ClientRejected; // 驳回整改至人工标注[cite: 1]
      foreach (var item in batch.Items)
      {
        item.Status = ItemStatus.InAnnotation;
      }
    }

    await _context.SaveChangesAsync();
  }

  // 1. 数据导入：由 PM 批量导入原始数据列表
  public async Task<List<TaskItem>> ImportRawItemsAsync(List<string> rawUrls)
  {
    var items = rawUrls.Select(url => new TaskItem
    {
      RawDataUrl = url,
      Status = ItemStatus.Imported,
      UpdatedAt = DateTime.UtcNow
    }).ToList();

    _context.TaskItems.AddRange(items);
    await _context.SaveChangesAsync();
    return items;
  }

  // 2. AI 预标注或跳过：调用模型结果写入，或直接置为人工标注状态
  public async Task ProcessPreAnnotationAsync(Guid itemId, string? aiPredictionJson = null)
  {
    var item = await _context.TaskItems.FindAsync(itemId)
        ?? throw new InvalidOperationException("任务不存在");

    if (item.Status != ItemStatus.Imported)
      throw new InvalidOperationException("仅已导入的数据可以执行预标注");

    if (!string.IsNullOrEmpty(aiPredictionJson))
    {
      item.AnnotationData = aiPredictionJson; // 存入模型预推断结果
      item.Status = ItemStatus.PreAnnotated;
    }
    else
    {
      // 跳过 AI 预标注，直接流转到待人工标注
      item.Status = ItemStatus.InAnnotation;
    }

    item.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
  }
}