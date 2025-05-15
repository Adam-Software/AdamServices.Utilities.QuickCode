using System.Threading.Tasks;

namespace QuickCode.Services.Interfaces
{
    #region Delegate

    public delegate void DataReceivedEventHandler(object sender, string data);

    #endregion

    public interface IRemotePythonRunnerService
    {
        public event DataReceivedEventHandler RaiseDataReceivedEvent;
        public Task SendСontrolCharacter(string controlCharacter);
        public Task ConnectAndSendCodeAsync(string sourceCode);
        public Task DisconnectAsync();
    }
}
