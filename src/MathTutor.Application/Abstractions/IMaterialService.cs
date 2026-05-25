using MathTutor.Application.Common;
using MathTutor.Application.DTOs;

namespace MathTutor.Application.Abstractions;

public interface IMaterialService
{
    Task<IReadOnlyList<MaterialDto>> GetMaterialsAsync(MaterialQuery query, CancellationToken cancellationToken = default);
    Task<MaterialDto?> GetMaterialAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<MaterialDto>> SaveMaterialAsync(MaterialEditDto material, int adminUserId, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteMaterialAsync(int id, CancellationToken cancellationToken = default);
}
