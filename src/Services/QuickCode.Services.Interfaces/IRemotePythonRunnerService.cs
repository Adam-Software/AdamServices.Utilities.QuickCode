using System;
using System.Threading.Tasks;

namespace QuickCode.Services.Interfaces
{
    #region Delegate

    public delegate void DataReceivedEventHandler(object sender, string data);
    public delegate void IsConnectedChangeEventHandler(object sender);
    #endregion

    public interface IRemotePythonRunnerService : IDisposable
    {
        public event DataReceivedEventHandler RaiseDataReceivedEvent;
        public event IsConnectedChangeEventHandler RaiseIsConnectedChangeEvent;
        public Task SendСontrolCharacter(string controlCharacter);
        public Task ConnectAndSendCodeAsync(string sourceCode, bool withDebug = false);
        public Task DisconnectAsync(bool withDebug = false);

        public bool IsConnected { get; }
    }
}
