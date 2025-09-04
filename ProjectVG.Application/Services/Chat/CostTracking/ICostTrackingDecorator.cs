using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat.CostTracking
{
    public interface ICostTrackingDecorator<T> where T : class
    {
        T Service { get; }
        Task ProcessAsync(ChatProcessContext context);
        Task ProcessAsync(ChatRequestCommand request);
    }
}
