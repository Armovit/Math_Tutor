using MathTutor.Application.Abstractions;
using MathTutor.Application.Common;
using MathTutor.Application.DTOs;
using MathTutor.Domain.Entities;
using MathTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MathTutor.Infrastructure.Services;

public sealed class MaterialService(MathTutorDbContext dbContext) : IMaterialService
{
    public async Task<IReadOnlyList<MaterialDto>> GetMaterialsAsync(MaterialQuery query, CancellationToken cancellationToken = default)
    {
        var materials = dbContext.EducationalMaterials.Include(x => x.Tasks).AsNoTracking().AsQueryable();

        if (query.PublishedOnly) materials = materials.Where(x => x.IsPublished);
        if (!string.IsNullOrWhiteSpace(query.Topic))
        {
            var topic = query.Topic.Trim().ToLower();
            materials = materials.Where(x => x.Topic.ToLower().Contains(topic));
        }

        if (!string.IsNullOrWhiteSpace(query.Section))
        {
            var section = query.Section.Trim().ToLower();
            materials = materials.Where(x => x.Section.ToLower().Contains(section));
        }
        if (query.Type is not null) materials = materials.Where(x => x.Type == query.Type);
        if (query.Difficulty is not null) materials = materials.Where(x => x.Difficulty == query.Difficulty);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            materials = materials.Where(x => x.Title.ToLower().Contains(search) || x.Topic.ToLower().Contains(search) || x.Section.ToLower().Contains(search) || x.Description.ToLower().Contains(search));
        }

        materials = query.SortBy switch
        {
            "Difficulty" => materials.OrderBy(x => x.Difficulty).ThenBy(x => x.Title),
            "Date" => materials.OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc),
            "Topic" => materials.OrderBy(x => x.Topic).ThenBy(x => x.Section).ThenBy(x => x.Title),
            "Section" => materials.OrderBy(x => x.Section).ThenBy(x => x.Title),
            "Type" => materials.OrderBy(x => x.Type).ThenBy(x => x.Title),
            _ => materials.OrderBy(x => x.Title)
        };

        return await materials.Select(x => ToDto(x)).ToListAsync(cancellationToken);
    }

    public async Task<MaterialDto?> GetMaterialAsync(int id, CancellationToken cancellationToken = default)
    {
        var material = await dbContext.EducationalMaterials.Include(x => x.Tasks).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return material is null ? null : ToDto(material);
    }

    public async Task<OperationResult<MaterialDto>> SaveMaterialAsync(MaterialEditDto material, int adminUserId, CancellationToken cancellationToken = default)
    {
        if (adminUserId <= 0) return OperationResult<MaterialDto>.Failure("Не удалось определить администратора. Перезайдите в систему.");
        if (string.IsNullOrWhiteSpace(material.Title)) return OperationResult<MaterialDto>.Failure("Введите название материала.");
        if (string.IsNullOrWhiteSpace(material.Topic)) return OperationResult<MaterialDto>.Failure("Введите тему материала.");
        if (string.IsNullOrWhiteSpace(material.Section)) return OperationResult<MaterialDto>.Failure("Введите раздел материала.");
        if (string.IsNullOrWhiteSpace(material.Description)) return OperationResult<MaterialDto>.Failure("Введите описание материала.");

        EducationalMaterial entity;
        if (material.Id == 0)
        {
            entity = new EducationalMaterial { CreatedByUserId = adminUserId, CreatedAtUtc = DateTime.UtcNow };
            dbContext.EducationalMaterials.Add(entity);
        }
        else
        {
            entity = await dbContext.EducationalMaterials.Include(x => x.Tasks).FirstOrDefaultAsync(x => x.Id == material.Id, cancellationToken)
                ?? throw new InvalidOperationException("Материал не найден.");
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        entity.Title = material.Title.Trim();
        entity.Topic = material.Topic.Trim();
        entity.Section = material.Section.Trim();
        entity.Type = material.Type;
        entity.Description = material.Description.Trim();
        entity.Difficulty = material.Difficulty;
        entity.TheoryContent = material.TheoryContent.Trim();
        entity.IsPublished = material.IsPublished;

        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<MaterialDto>.Success(ToDto(entity), "Материал сохранён.");
    }

    public async Task<OperationResult> DeleteMaterialAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EducationalMaterials.FindAsync([id], cancellationToken);
        if (entity is null) return OperationResult.Failure("Материал не найден.");
        dbContext.EducationalMaterials.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Success("Материал удалён.");
    }

    private static MaterialDto ToDto(EducationalMaterial x) => new(x.Id, x.Title, x.Topic, x.Section, x.Type, x.Description, x.Difficulty, x.TheoryContent, x.IsPublished, x.Tasks.Count, x.CreatedAtUtc, x.UpdatedAtUtc);
}
