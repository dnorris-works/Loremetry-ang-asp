using System.Globalization;
using System.Text.RegularExpressions;
using backend.Data;
using backend.Dtos;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace backend.Services;

public static class WinningCatImportService
{
    private const int ReadyMinPerStore = 50;

    private static readonly Regex NodeIdRegex = new(@"^\d+$", RegexOptions.Compiled);

    public static async Task<WinningCatImportResultDto> ImportCsvAsync(
        AppDbContext db,
        string csvText,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(csvText))
        {
            return Fail("CSV text is empty.");
        }

        var importStartedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        var imported = 0;
        var skippedDepartment = 0;
        var skippedUnparseable = 0;

        foreach (var line in csvText.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cells = ParseCsvLine(line);
            if (cells.Count == 0)
            {
                continue;
            }

            var parsed = new List<(string Name, string NodeId)>();
            var ok = true;

            foreach (var cell in cells)
            {
                if (string.IsNullOrWhiteSpace(cell))
                {
                    continue;
                }

                if (TryParseNodeCell(cell, out var pair))
                {
                    parsed.Add(pair);
                }
                else
                {
                    ok = false;
                    break;
                }
            }

            if (!ok || parsed.Count == 0)
            {
                skippedUnparseable++;
                continue;
            }

            var department = parsed[0].Name.ToLowerInvariant();
            string store;

            if (department == "kindle store")
            {
                store = "Kindle";
            }
            else if (department == "books")
            {
                store = "Books";
            }
            else
            {
                skippedDepartment++;
                continue;
            }

            if (parsed.Count < 2)
            {
                skippedUnparseable++;
                continue;
            }

            var pathParts = parsed.Skip(1).Select(part => part.Name);
            var path = string.Join(" > ", pathParts);
            var nodeId = parsed[^1].NodeId;

            try
            {
                await ImportCategoryAsync(db, path, store, nodeId, importStartedAt, cancellationToken);
                imported++;
            }
            catch
            {
                skippedUnparseable++;
            }
        }

        var staleCount = await CountStalePathsAsync(db, importStartedAt, cancellationToken);

        await PlatformServiceStatusService.RefreshWinningCatStatusAsync(db, cancellationToken);

        return new WinningCatImportResultDto(
            Success: true,
            Imported: imported,
            SkippedOtherDepartment: skippedDepartment,
            SkippedUnparseable: skippedUnparseable,
            StaleCount: staleCount,
            ImportedAt: importStartedAt,
            Error: null);
    }

    public static async Task<WinningCatCatalogStatusDto> GetCatalogStatusAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var kindleCount = await ScalarLongAsync(
            db,
            """
            SELECT COUNT(*)::bigint
            FROM lore.kdp_categories
            WHERE source = 'winningcat' AND store = 'Kindle'
            """,
            cancellationToken);

        var booksCount = await ScalarLongAsync(
            db,
            """
            SELECT COUNT(*)::bigint
            FROM lore.kdp_categories
            WHERE source = 'winningcat' AND store = 'Books'
            """,
            cancellationToken);

        var lastImportAt = await ScalarStringAsync(
            db,
            """
            SELECT MAX(last_seen_at)
            FROM lore.kdp_categories
            WHERE source = 'winningcat'
            """,
            cancellationToken);

        var totalCount = kindleCount + booksCount;
        var hasData = totalCount > 0;
        var ready = kindleCount >= ReadyMinPerStore && booksCount >= ReadyMinPerStore;

        var message = FormatCatalogMessage(kindleCount, booksCount, lastImportAt, ready, hasData);

        return new WinningCatCatalogStatusDto(
            Success: true,
            HasData: hasData,
            Ready: ready,
            KindleCount: kindleCount,
            BooksCount: booksCount,
            TotalCount: totalCount,
            LastImportAt: lastImportAt,
            Message: message,
            Error: null);
    }

    public static async Task<WinningCatStaleCleanupResultDto> RemoveStaleAsync(
        AppDbContext db,
        string since,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(since))
        {
            return new WinningCatStaleCleanupResultDto(false, 0, "Import timestamp is required.");
        }

        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            DELETE FROM lore.kdp_categories
            WHERE source = 'winningcat' AND (last_seen_at IS NULL OR last_seen_at < @since)
            """;

        command.Parameters.Add(new NpgsqlParameter("since", since.Trim()));

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            var removed = await command.ExecuteNonQueryAsync(cancellationToken);
            await PlatformServiceStatusService.RefreshWinningCatStatusAsync(db, cancellationToken);

            return new WinningCatStaleCleanupResultDto(true, removed, null);
        }
        catch (Exception exception)
        {
            return new WinningCatStaleCleanupResultDto(false, 0, exception.Message);
        }
    }

    private static async Task ImportCategoryAsync(
        AppDbContext db,
        string path,
        string store,
        string nodeId,
        string importStartedAt,
        CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            INSERT INTO lore.kdp_categories (path, store, amazon_node_id, source, created_at, last_seen_at)
            VALUES (@path, @store, @nodeId, 'winningcat', @importStartedAt, @importStartedAt)
            ON CONFLICT (path, store) DO UPDATE SET
                amazon_node_id = EXCLUDED.amazon_node_id,
                last_seen_at = @importStartedAt,
                source = CASE
                    WHEN lore.kdp_categories.source IN ('category_finder', 'category_analyzer')
                    THEN lore.kdp_categories.source
                    ELSE 'winningcat'
                END
            """;

        command.Parameters.Add(new NpgsqlParameter("path", path));
        command.Parameters.Add(new NpgsqlParameter("store", store));
        command.Parameters.Add(new NpgsqlParameter("nodeId", nodeId));
        command.Parameters.Add(new NpgsqlParameter("importStartedAt", importStartedAt));

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> CountStalePathsAsync(
        AppDbContext db,
        string since,
        CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)::bigint
            FROM lore.kdp_categories
            WHERE source = 'winningcat' AND (last_seen_at IS NULL OR last_seen_at < @since)
            """;

        command.Parameters.Add(new NpgsqlParameter("since", since));

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }

    private static async Task<long> ScalarLongAsync(
        AppDbContext db,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? 0 : Convert.ToInt64(result);
    }

    private static async Task<string?> ScalarStringAsync(
        AppDbContext db,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            return null;
        }

        var value = Convert.ToString(result);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool TryParseNodeCell(string cell, out (string Name, string NodeId) pair)
    {
        pair = default;
        cell = cell.Trim();

        var open = cell.LastIndexOf('(');
        var close = cell.LastIndexOf(')');
        if (open < 0 || close < open)
        {
            return false;
        }

        var id = cell[(open + 1)..close];
        if (string.IsNullOrEmpty(id) || !NodeIdRegex.IsMatch(id))
        {
            return false;
        }

        var name = cell[..open].Trim();
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        pair = (name, id);
        return true;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            switch (character)
            {
                case '"':
                    if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                    {
                        current.Append('"');
                        index++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    break;
                case ',' when !inQuotes:
                    fields.Add(current.ToString().Trim());
                    current.Clear();
                    break;
                default:
                    current.Append(character);
                    break;
            }
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }

    private static string FormatCatalogMessage(
        long kindleCount,
        long booksCount,
        string? lastImportAt,
        bool ready,
        bool hasData)
    {
        if (ready)
        {
            var when = string.IsNullOrWhiteSpace(lastImportAt)
                ? string.Empty
                : $" Last import: {lastImportAt}.";

            return
                $"WinningCat catalog is in the database — {kindleCount:N0} Kindle and {booksCount:N0} Books categories.{when}";
        }

        if (hasData)
        {
            return
                $"Partial WinningCat catalog — {kindleCount:N0} Kindle and {booksCount:N0} Books categories. "
                + "Import a full CSV for reliable category matching (need at least 50 per store).";
        }

        return "No WinningCat data in the database. Import the CSV to enable KDP category matching.";
    }

    private static WinningCatImportResultDto Fail(string message) =>
        new(false, 0, 0, 0, 0, string.Empty, message);
}
