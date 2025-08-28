namespace ProjectVG.Common.Models.Session
{
	public interface IClientConnection
	{
		string UserId { get; }
		DateTime ConnectedAt { get; }
		/// <summary>
/// 클라이언트에 텍스트 메시지를 비동기적으로 전송합니다.
/// </summary>
/// <param name="message">전송할 텍스트 메시지(널이 아님).</param>
/// <returns>전송 작업이 완료될 때까지 대기할 수 있는 Task.</returns>
Task SendTextAsync(string message);
		/// <summary>
/// 연결된 클라이언트로 바이너리 데이터를 비동기적으로 전송합니다.
/// </summary>
/// <param name="data">전송할 바이트 배열 페이로드.</param>
/// <returns>전송 작업의 완료를 나타내는 Task.</returns>
Task SendBinaryAsync(byte[] data);
	}
}


