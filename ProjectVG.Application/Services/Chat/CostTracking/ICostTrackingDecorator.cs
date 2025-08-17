using ProjectVG.Application.Models.Chat;
using ProjectVG.Domain.Entities.ConversationHistorys;

namespace ProjectVG.Application.Services.Chat.CostTracking
{
    public interface ICostTrackingDecorator<T> where T : class
    {
        T Service { get; }
        Task ProcessAsync(ChatProcessContext context);
        Task<UserInputAnalysis> ProcessAsync(string userInput, IEnumerable<ConversationHistory> conversationHistory);
    }
}
