using AnnotationBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AnnotationBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
  private readonly WorkflowService _workflow;

  public WorkflowController(WorkflowService workflow)
  {
    _workflow = workflow;
  }

  // PM 分派批次到某个基地
  [HttpPost("batches/{batchId:guid}/assign-site")]
  public async Task<IActionResult> AssignBatchToSite(Guid batchId, [FromQuery] Guid siteId)
  {
    await _workflow.AssignBatchToSiteAsync(batchId, siteId);
    return Ok(new { message = "批次已成功分发至指定基地" });
  }

  // 标注员提交单条标注
  [HttpPost("items/{itemId:guid}/submit")]
  public async Task<IActionResult> Submit(Guid itemId, [FromQuery] Guid labelerId, [FromBody] string annotationData)
  {
    await _workflow.SubmitAnnotationAsync(itemId, labelerId, annotationData);
    return Ok(new { message = "标注提交成功，进入初审队列" });
  }

  // 质检员初审（单条驳回或通过）[cite: 1]
  [HttpPost("items/{itemId:guid}/review")]
  public async Task<IActionResult> Review(Guid itemId, [FromQuery] Guid reviewerId, [FromQuery] bool isPassed, [FromBody] string? reason)
  {
    await _workflow.ReviewItemAsync(itemId, reviewerId, isPassed, reason);
    return Ok(new { message = isPassed ? "初审通过" : "单条驳回成功" });
  }

  // 内部主管抽检[cite: 1]
  [HttpPost("batches/{batchId:guid}/internal-sample")]
  public async Task<IActionResult> InternalSample(Guid batchId, [FromQuery] bool reachedSla)
  {
    await _workflow.InternalSampleReviewAsync(batchId, reachedSla);
    return Ok(new { message = reachedSla ? "内部SLA达成，已交付客户" : "全批返修已下发" });
  }

  // 客户抽检与验收[cite: 1]
  [HttpPost("batches/{batchId:guid}/client-inspect")]
  public async Task<IActionResult> ClientInspect(Guid batchId, [FromQuery] bool isAccepted)
  {
    await _workflow.ClientInspectAsync(batchId, isAccepted);
    return Ok(new { message = isAccepted ? "客户验收通过，进入结算阶段" : "客户已驳回整改" });
  }


  [HttpPost("items/import")]
  public async Task<IActionResult> ImportItems([FromBody] List<string> urls)
  {
    var items = await _workflow.ImportRawItemsAsync(urls);
    return Ok(new { message = $"成功导入 {items.Count} 条数据", count = items.Count });
  }

  [HttpPost("items/{itemId:guid}/pre-annotate")]
  public async Task<IActionResult> PreAnnotate(Guid itemId, [FromBody] string? aiJson)
  {
    await _workflow.ProcessPreAnnotationAsync(itemId, aiJson);
    return Ok(new { message = "预标注处理完毕，数据已准备就绪" });
  }
}