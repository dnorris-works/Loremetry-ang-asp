using backend.Auth;
using backend.Data;
using backend.Dtos;
using backend.Services;

namespace backend.Endpoints;

public static class WritingAssistantEndpoints
{
    private const string SystemPrompt = """
        You are a writing assistant for fiction authors using Loremetry.
        The user message includes lore context and the current open document when available.
        Help revise, expand, or answer questions about the draft.
        When producing revised prose, return markdown suitable for the document editor.
        Be concise unless the user asks for a full rewrite.
        """;

    public static IEndpointRouteBuilder MapWritingAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var assistant = app.MapGroup("/api/writing/assistant");

        assistant.MapPost("/chat", Chat);

        return app;
    }

    private static async Task<IResult> Chat(
        WritingAssistantChatRequest request,
        HttpRequest httpRequest,
        AuthService authService,
        AppDbContext db,
        TokenMixCompletionService completionService,
        CancellationToken cancellationToken)
    {
        var authResult = await AuthEndpointHelpers.TryResolveUserAsync(httpRequest, authService, cancellationToken);
        if (authResult.Error is { } error)
        {
            return error;
        }

        var userPrompt = request.UserPrompt?.Trim() ?? string.Empty;
        if (userPrompt.Length == 0)
        {
            return Results.BadRequest(new { message = "UserPrompt is required." });
        }

        var augmentedPrompt = request.AugmentedPrompt?.Trim();
        var completionUserPrompt = string.IsNullOrWhiteSpace(augmentedPrompt) ? userPrompt : augmentedPrompt;

        try
        {
            var text = await completionService.CompleteChatAsync(
                db,
                SystemPrompt,
                completionUserPrompt,
                modelOverride: null,
                cancellationToken);

            return Results.Ok(new WritingAssistantChatResponse(text));
        }
        catch (InvalidOperationException ex) when (IsConfigurationError(ex.Message))
        {
            return Results.Problem(
                title: "AI not configured",
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "AI request failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static bool IsConfigurationError(string message) =>
        message.Contains("not configured", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("requires default_provider", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("not supported yet", StringComparison.OrdinalIgnoreCase);
}
