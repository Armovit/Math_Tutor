using MathTutor.Application.Common;
using MathTutor.Application.DTOs;

namespace MathTutor.Application.Abstractions;

public interface ITaskService
{
    Task<IReadOnlyList<TaskDto>> GetTasksAsync(int? materialId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskDto>> GetTasksAsync(TaskQuery query, CancellationToken cancellationToken = default);
    Task<OperationResult<TaskDto>> SaveTaskAsync(TaskEditDto task, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteTaskAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<SubmissionDto>> SubmitAnswerAsync(SubmitAnswerRequest request, CancellationToken cancellationToken = default);
}
