using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickCode.Services.Interfaces;
using QuickCode.Services.Interfaces.JsonModel;
using System;
using System.Text.Json;
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

        #endregion

        #region ~

        public RemotePythonRunnerService(IServiceProvider serviceProvider)
        {
            mLogger = serviceProvider.GetService<ILogger<RemotePythonRunnerService>>();

            //new ParamsTcpClient("10.254.254.230", 19000, "\r\n", isSSL: false)
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

            //mTcpClient.ConnectionEvent += ConnectionEvent;
            //mTcpClient.MessageEvent += MessageEvent;
            //mTcpClient.ErrorEvent += ErrorEvent;
        }





        private void Unsubscribe()
        {
            mTcpClient.Events.ServerConnected -= ServerConnected;
            mTcpClient.Events.MessageReceived -= MessageReceived;
            mTcpClient.Events.ExceptionEncountered -= ExceptionEncountered;
            mTcpClient.Events.ServerDisconnected -= ServerDisconnected;
            //mTcpClient.ConnectionEvent -= ConnectionEvent;
            //mTcpClient.MessageEvent -= MessageEvent;
            //mTcpClient.ErrorEvent -= ErrorEvent;
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
            //var messageEvent = args.MessageEventType;

            //if (messageEvent != MessageEventType.Receive)
            //    return;

            var message = System.Text.Encoding.Default.GetString(e.Data);
            OnRaiseClientDataReceivedEvent(message);
        }

        /*private void MessageEvent(object sender, TcpMessageClientEventArgs args)
        {
            var messageEvent = args.MessageEventType;

            if (messageEvent != MessageEventType.Receive)
                return;

            OnRaiseClientDataReceivedEvent(args.Message);            
        }*/

        /*private void ConnectionEvent(object sender, TcpConnectionClientEventArgs args)
        {
            ConnectionEventType connectionEvent = args.ConnectionEventType;

            switch (connectionEvent)
            {
                case ConnectionEventType.Connected:
                    {
                        mLogger.LogInformation("Client connected");
                        IsConnected = true;

                        //CheckRunning();
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

        public Task CheckRunning()
        {
            return Task.Run(async () =>
            {
                while (mTcpClient.IsRunning) 
                {
                    var test = await mTcpClient.SendAsync(new byte[0]);
                    IsConnected = test;
                }
                IsConnected = false;
            });
        }

        private void ErrorEvent(object sender, TcpErrorClientEventArgs args)
        {
            IsConnected = false;
            mLogger.LogInformation("{error}", args.Exception);
        }*/

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

            //var result = 
            mTcpClient.Connect();
          
            if (mTcpClient.Connected)
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
            _ = await mTcpClient.SendAsync(message);

            //mLogger.LogInformation("Send control_characters result: {result}", result);
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
