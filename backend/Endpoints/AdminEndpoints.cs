using System.Data;
using System.Text.RegularExpressions;
using backend.Data;
using backend.Dtos;
using Microsoft.EntityFrameworkCore;

namespace backend.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin");

        admin.MapGet("/schema/schemas", GetSchemas);
        admin.MapGet("/schema/{schemaName}/objects", GetSchemaObjects);
        admin.MapGet("/schema/{schemaName}/objects/{objectName}/columns", GetColumns);
        admin.MapGet("/schema/{schemaName}/objects/{objectName}/data", GetObjectData);
        admin.MapPost("/sql", ExecuteSql);

        return app;
    }

    private static async Task<IResult> GetSchemas(AppDbContext db, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT schema_name AS "Name"
            FROM information_schema.schemata
            WHERE schema_name NOT IN ('pg_catalog', 'information_schema')
              AND schema_name NOT LIKE 'pg_toast%'
              AND schema_name NOT LIKE 'pg_temp_%'
            ORDER BY schema_name;
            """;

        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var schemas = new List<SchemaInfoDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            schemas.Add(new SchemaInfoDto(reader.GetString(0)));
        }

        return Results.Ok(schemas);
    }

    private static async Task<IResult> GetSchemaObjects(
        string schemaName,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT table_name AS "Name", table_type AS "Type"
            FROM information_schema.tables
            WHERE table_schema = @schemaName
            UNION ALL
            SELECT sequence_name, 'SEQUENCE'
            FROM information_schema.sequences
            WHERE sequence_schema = @schemaName
            UNION ALL
            SELECT routine_name, routine_type
            FROM information_schema.routines
            WHERE routine_schema = @schemaName
            ORDER BY 2, 1;
            """;

        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "schemaName";
        parameter.Value = schemaName;
        command.Parameters.Add(parameter);

        var objects = new List<SchemaObjectDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var type = reader.GetString(1) switch
            {
                "BASE TABLE" => "TABLE",
                _ => reader.GetString(1),
            };
            objects.Add(new SchemaObjectDto(reader.GetString(0), type));
        }

        return Results.Ok(objects);
    }

    private static async Task<IResult> GetColumns(
        string schemaName,
        string objectName,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT column_name AS "Name",
                   data_type AS "DataType",
                   is_nullable = 'YES' AS "IsNullable",
                   column_default AS "DefaultValue"
            FROM information_schema.columns
            WHERE table_schema = @schemaName AND table_name = @objectName
            ORDER BY ordinal_position;
            """;

        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var schemaParameter = command.CreateParameter();
        schemaParameter.ParameterName = "schemaName";
        schemaParameter.Value = schemaName;
        command.Parameters.Add(schemaParameter);

        var objectParameter = command.CreateParameter();
        objectParameter.ParameterName = "objectName";
        objectParameter.Value = objectName;
        command.Parameters.Add(objectParameter);

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

    private static async Task<IResult> GetObjectData(
        string schemaName,
        string objectName,
        int? limit,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!IsValidIdentifier(schemaName) || !IsValidIdentifier(objectName))
        {
            return Results.BadRequest(new { message = "Invalid schema or object name." });
        }

        const string verifySql = """
            SELECT table_type
            FROM information_schema.tables
            WHERE table_schema = @schemaName AND table_name = @objectName
            LIMIT 1;
            """;

        await using var connection = db.Database.GetDbConnection();
        await OpenConnectionIfNeededAsync(connection, cancellationToken);

        await using var verifyCommand = connection.CreateCommand();
        verifyCommand.CommandText = verifySql;

        var schemaParameter = verifyCommand.CreateParameter();
        schemaParameter.ParameterName = "schemaName";
        schemaParameter.Value = schemaName;
        verifyCommand.Parameters.Add(schemaParameter);

        var objectParameter = verifyCommand.CreateParameter();
        objectParameter.ParameterName = "objectName";
        objectParameter.Value = objectName;
        verifyCommand.Parameters.Add(objectParameter);

        if (await verifyCommand.ExecuteScalarAsync(cancellationToken) is not string objectType)
        {
            return Results.NotFound(new { message = $"Object '{schemaName}.{objectName}' was not found." });
        }

        if (objectType is not ("BASE TABLE" or "VIEW"))
        {
            return Results.BadRequest(new { message = $"Object '{schemaName}.{objectName}' does not contain row data." });
        }

        var rowLimit = Math.Clamp(limit ?? 100, 1, 500);

        await using var dataCommand = connection.CreateCommand();
        dataCommand.CommandText = $"""SELECT * FROM "{schemaName}"."{objectName}" LIMIT {rowLimit}""";

        try
        {
            await using var reader = await dataCommand.ExecuteReaderAsync(cancellationToken);
            return Results.Ok(await ReadQueryResultAsync(reader, cancellationToken));
        }
        catch (Exception exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static async Task<IResult> ExecuteSql(
        ExecuteSqlRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Sql))
        {
            return Results.BadRequest(new { message = "SQL is required." });
        }

        await using var connection = db.Database.GetDbConnection();
        await OpenConnectionIfNeededAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = request.Sql.Trim();

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (reader.FieldCount > 0)
            {
                return Results.Ok(await ReadQueryResultAsync(reader, cancellationToken));
            }

            var rowsAffected = reader.RecordsAffected;
            while (await reader.NextResultAsync(cancellationToken))
            {
                if (reader.RecordsAffected > 0)
                {
                    rowsAffected = reader.RecordsAffected;
                }
            }

            var message = rowsAffected switch
            {
                >= 0 => $"Command completed. Rows affected: {rowsAffected}.",
                _ => "Command completed successfully.",
            };

            int? reportedRowsAffected = rowsAffected switch
            {
                >= 0 => rowsAffected,
                _ => null,
            };

            return Results.Ok(new SqlQueryResultDto([], [], reportedRowsAffected, message));
        }
        catch (Exception exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static readonly Regex IdentifierRegex = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    private static bool IsValidIdentifier(string value) => IdentifierRegex.IsMatch(value);

    private static async Task OpenConnectionIfNeededAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        if (connection.State is not ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }

    private static async Task<SqlQueryResultDto> ReadQueryResultAsync(
        System.Data.Common.DbDataReader reader,
        CancellationToken cancellationToken)
    {
        var columns = Enumerable.Range(0, reader.FieldCount)
            .Select(reader.GetName)
            .ToList();
        var rows = new List<IReadOnlyList<object?>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new object?[reader.FieldCount];
            for (var index = 0; index < reader.FieldCount; index++)
            {
                row[index] = await reader.IsDBNullAsync(index, cancellationToken)
                    ? null
                    : reader.GetValue(index);
            }

            rows.Add(row);
        }

        return new SqlQueryResultDto(columns, rows, null, null);
    }
}
