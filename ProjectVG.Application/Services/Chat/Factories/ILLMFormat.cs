namespace ProjectVG.Application.Services.Chat.Factories
{
    public interface ILLMFormat<TInput, TOutput>
    {
        string GetSystemMessage(TInput input);
        string GetInstructions(TInput input);
        string Model { get; }
        float Temperature { get; }
        int MaxTokens { get; }
        TOutput Parse(string llmResponse, TInput input);
        double CalculateCost(int promptTokens, int completionTokens);
    }
}
