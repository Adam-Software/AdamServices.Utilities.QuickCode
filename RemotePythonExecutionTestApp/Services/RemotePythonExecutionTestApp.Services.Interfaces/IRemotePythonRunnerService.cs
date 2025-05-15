using System.Threading.Tasks;

namespace RemotePythonExecutionTestApp.Services.Interfaces
{
    #region Delegate

    public delegate void DataReceivedEventHandler(object sender, string data);

    #endregion

    public interface IRemotePythonRunnerService
    {
        public event DataReceivedEventHandler RaiseDataReceivedEvent;
        //public void ConnectAsync();
        public Task SendСontrolCharacter(string controlCharacter);
        public Task ConnectAndSendCodeAsync(string sourceCode);
        public Task DisconnectAsync();
        //public string ReceiveMessage {  get; }
    }
}
