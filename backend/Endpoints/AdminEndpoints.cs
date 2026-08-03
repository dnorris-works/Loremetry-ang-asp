using System.Text.Json;
using System.Text.RegularExpressions;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Endpoints;

public static class AdminEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin");

        admin.MapGet("/collections", GetCollections);
        admin.MapGet("/collections/{id:guid}", GetCollection);
        admin.MapPost("/collections", CreateCollection);
        admin.MapDelete("/collections/{id:guid}", DeleteCollection);

        admin.MapPost("/collections/{collectionId:guid}/fields", CreateField);
        admin.MapDelete("/fields/{id:guid}", DeleteField);

        admin.MapGet("/collections/{collectionId:guid}/entries", GetEntries);
        admin.MapPost("/collections/{collectionId:guid}/entries", CreateEntry);
        admin.MapPut("/entries/{id:guid}", UpdateEntry);
        admin.MapDelete("/entries/{id:guid}", DeleteEntry);

        admin.MapGet("/schema/tables", GetTables);
        admin.MapGet("/schema/tables/{tableName}/columns", GetColumns);

        return app;
    }

    private static async Task<IResult> GetCollections(AppDbContext db, CancellationToken cancellationToken)
    {
        var collections = await db.Collections
            .AsNoTracking()
            .OrderBy(collection => collection.Name)
            .Select(collection => new CollectionSummaryDto(
                collection.Id,
                collection.Name,
                collection.Slug,
                collection.Description,
                collection.Fields.Count,
                collection.Entries.Count,
                collection.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(collections);
    }

    private static async Task<IResult> GetCollection(Guid id, AppDbContext db, CancellationToken cancellationToken)
    {
        var collection = await db.Collections
            .AsNoTracking()
            .Include(item => item.Fields.OrderBy(field => field.DisplayOrder))
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return collection is null ? Results.NotFound() : Results.Ok(ToDetailDto(collection));
    }

    private static async Task<IResult> CreateCollection(
        CreateCollectionRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug))
        {
            return Results.BadRequest(new { message = "Name and slug are required." });
        }

        var slug = NormalizeSlug(request.Slug);
        if (await db.Collections.AnyAsync(collection => collection.Slug == slug, cancellationToken))
        {
            return Results.Conflict(new { message = "A collection with this slug already exists." });
        }

        var now = DateTimeOffset.UtcNow;
        var collection = new Collection
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Collections.Add(collection);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/admin/collections/{collection.Id}", ToDetailDto(collection));
    }

    private static async Task<IResult> DeleteCollection(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var collection = await db.Collections.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (collection is null)
        {
            return Results.NotFound();
        }

        db.Collections.Remove(collection);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> CreateField(
        Guid collectionId,
        CreateCollectionFieldRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var collection = await db.Collections.FirstOrDefaultAsync(item => item.Id == collectionId, cancellationToken);
        if (collection is null)
        {
            return Results.NotFound();
        }

        var fieldKey = NormalizeFieldKey(request.FieldKey);
        if (await db.CollectionFields.AnyAsync(
                field => field.CollectionId == collectionId && field.FieldKey == fieldKey,
                cancellationToken))
        {
            return Results.Conflict(new { message = "A field with this key already exists." });
        }

        var field = new CollectionField
        {
            Id = Guid.NewGuid(),
            CollectionId = collectionId,
            Name = request.Name.Trim(),
            FieldKey = fieldKey,
            FieldType = request.FieldType,
            IsRequired = request.IsRequired,
            DisplayOrder = request.DisplayOrder,
        };

        db.CollectionFields.Add(field);
        collection.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/admin/fields/{field.Id}", ToFieldDto(field));
    }

    private static async Task<IResult> DeleteField(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var field = await db.CollectionFields
            .Include(item => item.Collection)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (field is null)
        {
            return Results.NotFound();
        }

        field.Collection.UpdatedAt = DateTimeOffset.UtcNow;
        db.CollectionFields.Remove(field);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> GetEntries(
        Guid collectionId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!await db.Collections.AnyAsync(collection => collection.Id == collectionId, cancellationToken))
        {
            return Results.NotFound();
        }

        var entries = await db.CollectionEntries
            .AsNoTracking()
            .Where(entry => entry.CollectionId == collectionId)
            .OrderByDescending(entry => entry.UpdatedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(entries.Select(ToEntryDto));
    }

    private static async Task<IResult> CreateEntry(
        Guid collectionId,
        CreateCollectionEntryRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var collection = await db.Collections
            .Include(item => item.Fields)
            .FirstOrDefaultAsync(item => item.Id == collectionId, cancellationToken);

        if (collection is null)
        {
            return Results.NotFound();
        }

        var validationError = ValidateEntryValues(collection.Fields, request.Values);
        if (validationError is not null)
        {
            return Results.BadRequest(new { message = validationError });
        }

        var now = DateTimeOffset.UtcNow;
        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            CollectionId = collectionId,
            ValuesJson = JsonSerializer.Serialize(request.Values, JsonOptions),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.CollectionEntries.Add(entry);
        collection.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/admin/entries/{entry.Id}", ToEntryDto(entry));
    }

    private static async Task<IResult> UpdateEntry(
        Guid id,
        UpdateCollectionEntryRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var entry = await db.CollectionEntries
            .Include(item => item.Collection)
            .ThenInclude(collection => collection.Fields)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (entry is null)
        {
            return Results.NotFound();
        }

        var validationError = ValidateEntryValues(entry.Collection.Fields, request.Values);
        if (validationError is not null)
        {
            return Results.BadRequest(new { message = validationError });
        }

        entry.ValuesJson = JsonSerializer.Serialize(request.Values, JsonOptions);
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        entry.Collection.UpdatedAt = entry.UpdatedAt;

        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(ToEntryDto(entry));
    }

    private static async Task<IResult> DeleteEntry(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var entry = await db.CollectionEntries
            .Include(item => item.Collection)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (entry is null)
        {
            return Results.NotFound();
        }

        entry.Collection.UpdatedAt = DateTimeOffset.UtcNow;
        db.CollectionEntries.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> GetTables(AppDbContext db, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT table_schema AS "Schema", table_name AS "Name"
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
            ORDER BY table_name;
            """;

        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var tables = new List<TableInfoDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(new TableInfoDto(reader.GetString(1), reader.GetString(0)));
        }

        return Results.Ok(tables);
    }

    private static async Task<IResult> GetColumns(
        string tableName,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT column_name AS "Name",
                   data_type AS "DataType",
                   is_nullable = 'YES' AS "IsNullable",
                   column_default AS "DefaultValue"
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = @tableName
            ORDER BY ordinal_position;
            """;

        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var columns = new List<ColumnInfoDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnInfoDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetBoolean(2),
                reader.IsDBNull(3) ? null : reader.GetString(3)));
        }

        return Results.Ok(columns);
    }

    private static CollectionDetailDto ToDetailDto(Collection collection) =>
        new(
            collection.Id,
            collection.Name,
            collection.Slug,
            collection.Description,
            collection.CreatedAt,
            collection.UpdatedAt,
            collection.Fields.OrderBy(field => field.DisplayOrder).Select(ToFieldDto).ToList());

    private static CollectionFieldDto ToFieldDto(CollectionField field) =>
        new(field.Id, field.Name, field.FieldKey, field.FieldType, field.IsRequired, field.DisplayOrder);

    private static CollectionEntryDto ToEntryDto(CollectionEntry entry) =>
        new(
            entry.Id,
            entry.CollectionId,
            JsonSerializer.Deserialize<Dictionary<string, object?>>(entry.ValuesJson, JsonOptions) ?? [],
            entry.CreatedAt,
            entry.UpdatedAt);

    private static string? ValidateEntryValues(
        IEnumerable<CollectionField> fields,
        Dictionary<string, object?> values)
    {
        foreach (var field in fields)
        {
            values.TryGetValue(field.FieldKey, out var value);

            if (field.IsRequired && IsEmptyValue(value))
            {
                return $"Field '{field.Name}' is required.";
            }
        }

        return null;
    }

    private static bool IsEmptyValue(object? value) =>
        value is null ||
        (value is string text && string.IsNullOrWhiteSpace(text));

    private static readonly Regex SlugRegex = new(@"[^a-z0-9]+", RegexOptions.Compiled);
    private static readonly Regex FieldKeyRegex = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    private static string NormalizeSlug(string slug) =>
        SlugRegex.Replace(slug.Trim().ToLowerInvariant(), "-").Trim('-');

    private static string NormalizeFieldKey(string fieldKey) =>
        FieldKeyRegex.Replace(fieldKey.Trim().ToLowerInvariant(), "_").Trim('_');
}
