namespace RemotePythonExecutionTestApp.Services.Interfaces
{
    public interface IRemotePythonRunnerService
    {
        public void ConnectAsync();
        public void SendCode(string sourceCode);
    }
}
