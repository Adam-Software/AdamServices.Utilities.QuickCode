using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickCode.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WatsonTcp;

namespace QuickCode.Services
{
    public class RemotePythonRunnerService : IRemotePythonRunnerService
    {
        public event DataReceivedEventHandler RaiseDataReceivedEvent;
        public event IsConnectedChangeEventHandler RaiseIsConnectedChangeEvent;

        #region Services

        private readonly ILogger<RemotePythonRunnerService> mLogger;

        #endregion

        #region Var

        private readonly WatsonTcpClient mTcpClient;
        private bool mIsDisposing;

        #endregion

        #region ~

        public RemotePythonRunnerService(IServiceProvider serviceProvider)
        {
            mLogger = serviceProvider.GetService<ILogger<RemotePythonRunnerService>>();

            mTcpClient = new WatsonTcpClient("127.0.0.1", 19000);
            
            Subscribe();
            mLogger.LogInformation("Load RemotePythonRunnerService ~");
        }

        #endregion

        #region Subscribe/Unsubscribe

        private void Subscribe()
        {
            mTcpClient.Events.ServerConnected += ServerConnected;
            mTcpClient.Events.MessageReceived += MessageReceived;
            mTcpClient.Events.ExceptionEncountered += ExceptionEncountered;
            mTcpClient.Events.ServerDisconnected += ServerDisconnected;
        }

        private void Unsubscribe()
        {
            mTcpClient.Events.ServerConnected -= ServerConnected;
            mTcpClient.Events.MessageReceived -= MessageReceived;
            mTcpClient.Events.ExceptionEncountered -= ExceptionEncountered;
            mTcpClient.Events.ServerDisconnected -= ServerDisconnected;
            
        }

        #endregion

        #region Events

        private void ServerConnected(object sender, ConnectionEventArgs e)
        {
            mLogger.LogInformation("Client connected");
            IsConnected = true;
        }

        private void ServerDisconnected(object sender, DisconnectionEventArgs e)
        {
            mLogger.LogInformation("Client disconected");
            IsConnected = false;
        }

        private void ExceptionEncountered(object sender, ExceptionEventArgs e)
        {
            mLogger.LogInformation("{error}", e.Exception.Message);
        }

        private void MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var message = System.Text.Encoding.Default.GetString(e.Data);
            OnRaiseClientDataReceivedEvent(message);
        }

        #endregion

        #region Public fields

        private bool mIsConnected = false;
        public bool IsConnected 
        { 
            get { return mIsConnected; }
            set 
            {
                if (mIsConnected == value) 
                    return;

                mIsConnected = value;

                OnIsConnectedChangeEvent();
            }
        }

        #endregion

        #region Public methods

        public async Task ConnectAndSendCodeAsync(string sourceCode, bool withDebug = false)
        {
            var messageType = "source_code";

            if (withDebug)
                messageType = "debug_source_code";

            var code = new Dictionary<string, object>() { { messageType, sourceCode } };
            mTcpClient.Connect();
          
            if (mTcpClient.Connected)
            {
                var sendResult = await mTcpClient.SendAsync("", code);

                mLogger.LogInformation("Send message result: {result}", sendResult);
            }
        }

        public async Task SendСontrolCharacter(string controlCharacter)
        {
            
            var character = new Dictionary<string, object>() { { "control_characters", controlCharacter } };

            if (mTcpClient.Connected)
            {
                _ = await mTcpClient.SendAsync("", character);
            }
        }

        public async Task DisconnectAsync(bool withDebug = false)
        {
            if (withDebug)
            {
                await SendСontrolCharacter("q");
                return;
            }

            mTcpClient.Disconnect();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private methods

        protected virtual void Dispose(bool disposing)
        {
            if (!mIsDisposing)
            {
                if (disposing)
                {
                    Unsubscribe();

                    if (mTcpClient?.Connected ?? false)
                        mTcpClient.Disconnect(true);

                    mTcpClient?.Dispose();
                }

                mIsDisposing = true;
            }
        }

        #endregion

        #region RaiseEvent

        protected virtual void OnRaiseClientDataReceivedEvent(string data)
        {
            var raiseEvent = RaiseDataReceivedEvent;
            raiseEvent?.Invoke(this, data);

        }

        protected virtual void OnIsConnectedChangeEvent()
        {
            var raiseEven = RaiseIsConnectedChangeEvent;
            raiseEven?.Invoke(this);
        }

        #endregion
    }
}
