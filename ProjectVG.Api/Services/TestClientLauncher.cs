namespace ProjectVG.Api.Services;

public class TestClientLauncher
{
    public void Launch()
    {
        Task.Delay(1000).ContinueWith(_ => {
            try
            {
                var htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "test-clients", "test-client.html");
                if (File.Exists(htmlPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = htmlPath,
                        UseShellExecute = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal
                    });
                }
            }
            catch { }
        });
    }
} 