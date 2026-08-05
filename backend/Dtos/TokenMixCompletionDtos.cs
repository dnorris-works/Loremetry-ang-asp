namespace backend.Dtos;

public record TokenMixCompletionResult(
    string Text,
    string Model,
    int InputTokens,
    int OutputTokens);
