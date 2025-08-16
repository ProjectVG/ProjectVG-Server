namespace ProjectVG.Common.Models.Session
{
	public interface IClientConnection
	{
		string SessionId { get; }
		string? UserId { get; }
		DateTime ConnectedAt { get; }
		/// <summary>
/// 지정한 텍스트 메시지를 비동기적으로 전송합니다.
/// </summary>
/// <param name="message">전송할 텍스트 메시지(널이 아닌 문자열).</param>
/// <returns>전송 작업을 나타내는 비동기 작업(Task).</returns>
Task SendTextAsync(string message);
		/// <summary>
/// 현재 연결된 클라이언트로 바이너리 데이터를 비동기적으로 전송합니다.
/// </summary>
/// <param name="data">전송할 바이트 배열(비어 있을 수 있음). null이 아닌 값이어야 합니다.</param>
/// <returns>전송이 완료될 때까지 완료되는 Task.</returns>
Task SendBinaryAsync(byte[] data);
	}
}


