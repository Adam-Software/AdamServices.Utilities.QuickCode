using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PHS.Networking.Enums;
using QuickCode.Services.Interfaces;
using QuickCode.Services.Interfaces.JsonModel;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Tcp.NET.Client;
using Tcp.NET.Client.Events.Args;
using Tcp.NET.Client.Models;

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

        private readonly TcpNETClient mTcpClient;

        #endregion

        #region ~

        public RemotePythonRunnerService(IServiceProvider serviceProvider)
        {
            mLogger = serviceProvider.GetService<ILogger<RemotePythonRunnerService>>();

            mTcpClient = new TcpNETClient(new ParamsTcpClient("localhost", 19000, "\r\n", isSSL:false));
            
            Subscribe();
            mLogger.LogInformation("Load RemotePythonRunnerService ~");
        }

        #endregion

        #region Subscribe/Unsubscribe

        private void Subscribe()
        {
            mTcpClient.ConnectionEvent += ConnectionEvent;
            mTcpClient.MessageEvent += MessageEvent;
        }

        private void Unsubscribe()
        {
            mTcpClient.ConnectionEvent -= ConnectionEvent;
            mTcpClient.MessageEvent -= MessageEvent;
        }

        #endregion

        #region Events

        private void MessageEvent(object sender, TcpMessageClientEventArgs args)
        {
            var messageEvent = args.MessageEventType;
            
            if(messageEvent != MessageEventType.Receive)
                return;

            OnRaiseClientDataReceivedEvent(args.Message);
            
        }

        private void ConnectionEvent(object sender, TcpConnectionClientEventArgs args)
        {
            ConnectionEventType connectionEvent = args.ConnectionEventType;

            switch (connectionEvent)
            {
                case ConnectionEventType.Connected:
                    {
                        mLogger.LogInformation("Client connected");
                        IsConnected = true;
                    }

                    break;

                case ConnectionEventType.Disconnect:
                    {
                        mLogger.LogInformation("Client disconected");
                        IsConnected= false;
                    }
                    break;
            }
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

            var result = await mTcpClient.ConnectAsync();
          
            if (result)
            {
                ReceivedMessage receivedMessage = new()
                {
                    MessageType = messageType,
                    Code = sourceCode
                };

                var message = JsonSerializer.Serialize(receivedMessage);
                var sendResult = await mTcpClient.SendAsync(message);

                mLogger.LogInformation("Send message result: {result}", sendResult);
            }
        }

        public async Task SendСontrolCharacter(string controlCharacter)
        {
            ReceivedMessage receivedMessage = new()
            {
                MessageType = "control_characters",
                ControlCharacters = controlCharacter
            };

            var message = JsonSerializer.Serialize(receivedMessage);
            var result = await mTcpClient.SendAsync(message);

            mLogger.LogInformation("Send control_characters result: {result}", result);
        }

        public async Task DisconnectAsync(bool withDebug = false)
        {
            if (withDebug)
            {
                await SendСontrolCharacter("q");
                return;
            }

            await mTcpClient.DisconnectAsync();
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
