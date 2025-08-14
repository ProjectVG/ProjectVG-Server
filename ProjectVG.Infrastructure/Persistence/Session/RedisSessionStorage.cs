using Microsoft.Extensions.Logging;
using ProjectVG.Common.Models.Session;

namespace ProjectVG.Infrastructure.Persistence.Session
{
	public class RedisSessionStorage : ISessionStorage
	{
		private readonly ILogger<RedisSessionStorage> _logger;

		public RedisSessionStorage(ILogger<RedisSessionStorage> logger)
		{
			_logger = logger;
		}

		public Task<SessionInfo?> GetAsync(string sessionId)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<SessionInfo>> GetAllAsync()
		{
			throw new NotImplementedException();
		}

		public Task<SessionInfo> CreateAsync(SessionInfo session)
		{
			throw new NotImplementedException();
		}

		public Task<SessionInfo> UpdateAsync(SessionInfo session)
		{
			throw new NotImplementedException();
		}

		public Task DeleteAsync(string sessionId)
		{
			throw new NotImplementedException();
		}

		public Task<bool> ExistsAsync(string sessionId)
		{
			throw new NotImplementedException();
		}

		public Task<int> GetActiveSessionCountAsync()
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<SessionInfo>> GetSessionsByUserIdAsync(string? userId)
		{
			throw new NotImplementedException();
		}
	}
}


