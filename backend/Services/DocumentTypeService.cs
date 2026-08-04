using backend.Data;
using backend.Dtos;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public static class DocumentTypeService
{
    public static async Task<IReadOnlyList<DocumentTypeDto>> ListActiveAsync(
        AppDbContext db,
        string? parent,
        CancellationToken cancellationToken)
    {
        var query = db.DocumentTypes
            .AsNoTracking()
            .Where(type => type.Active);

        if (parent is "story")
        {
            query = query.Where(type => type.AppliesToStory);
        }
        else if (parent is "series")
        {
            query = query.Where(type => type.AppliesToSeries);
        }

        return await query
            .OrderBy(type => type.SortOrder)
            .ThenBy(type => type.DisplayName)
            .Select(type => new DocumentTypeDto(
                type.Code,
                type.DisplayName,
                type.AppliesToStory,
                type.AppliesToSeries))
            .ToListAsync(cancellationToken);
    }
}
