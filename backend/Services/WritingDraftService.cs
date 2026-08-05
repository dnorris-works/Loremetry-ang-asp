using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public static class WritingDraftService
{
    public static async Task<WritingDraftDto> UpsertAsync(
        long userId,
        UpsertWritingDraftRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var draftKey = request.DraftKey.Trim();
        if (string.IsNullOrWhiteSpace(draftKey))
        {
            throw new InvalidOperationException("DraftKey is required.");
        }

        if (draftKey.Length > 500)
        {
            throw new InvalidOperationException("DraftKey is too long.");
        }

        var mode = request.Mode.Trim().ToLowerInvariant();
        if (mode is not ("draft" or "document"))
        {
            throw new InvalidOperationException("Mode must be 'draft' or 'document'.");
        }

        var existing = await db.WritingDrafts
            .FirstOrDefaultAsync(
                draft => draft.UserId == userId && draft.DraftKey == draftKey,
                cancellationToken);

        var updatedAt = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            existing = new WritingDraft
            {
                UserId = userId,
                DraftKey = draftKey,
            };
            db.WritingDrafts.Add(existing);
        }

        existing.Mode = mode;
        existing.Source = string.IsNullOrWhiteSpace(request.Source) ? null : request.Source.Trim();
        existing.ParentId = request.ParentId;
        existing.DocumentId = request.DocumentId;
        existing.Category = request.Category?.Trim() ?? string.Empty;
        existing.Title = request.Title?.Trim() ?? string.Empty;
        existing.FileName = request.FileName?.Trim() ?? string.Empty;
        existing.TextContent = request.TextContent ?? string.Empty;
        existing.DestinationKey = request.DestinationKey?.Trim() ?? string.Empty;
        existing.MimeType = request.MimeType?.Trim() ?? string.Empty;
        existing.UpdatedAt = updatedAt;

        await db.SaveChangesAsync(cancellationToken);

        return ToDto(existing);
    }

    public static async Task<WritingDraftDto?> GetAsync(
        long userId,
        string draftKey,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var normalizedKey = draftKey.Trim();
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            return null;
        }

        var draft = await db.WritingDrafts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.UserId == userId && item.DraftKey == normalizedKey,
                cancellationToken);

        return draft is null ? null : ToDto(draft);
    }

    public static async Task<bool> DeleteAsync(
        long userId,
        string draftKey,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var normalizedKey = draftKey.Trim();
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            return false;
        }

        var draft = await db.WritingDrafts
            .FirstOrDefaultAsync(
                item => item.UserId == userId && item.DraftKey == normalizedKey,
                cancellationToken);

        if (draft is null)
        {
            return false;
        }

        db.WritingDrafts.Remove(draft);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static WritingDraftDto ToDto(WritingDraft draft) =>
        new(
            draft.DraftKey,
            draft.Mode,
            draft.Source,
            draft.ParentId,
            draft.DocumentId,
            draft.Category,
            draft.Title,
            draft.FileName,
            draft.TextContent,
            draft.DestinationKey,
            draft.MimeType,
            draft.UpdatedAt);
}
