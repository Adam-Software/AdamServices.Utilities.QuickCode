using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickCode.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
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

        private WatsonTcpClient mTcpClient;
        private bool mIsDisposing;

        #endregion

        #region ~

        public RemotePythonRunnerService(IServiceProvider serviceProvider)
        {
            mLogger = serviceProvider.GetService<ILogger<RemotePythonRunnerService>>();
            IOptionsMonitor<AppSettings> appSettingsMonitor = serviceProvider.GetService<IOptionsMonitor<AppSettings>>();
            
            Ip = appSettingsMonitor.CurrentValue.ClientSettings.Ip;
            Port = appSettingsMonitor.CurrentValue.ClientSettings.Port;
            
            mTcpClient = new WatsonTcpClient(Ip, Port);
            appSettingsMonitor.OnChange(OnChangeClientSettings);
            Subscribe();

            mLogger.LogInformation("Service run on {ip}:{port}", Ip, Port);
        }

        private void OnChangeClientSettings(AppSettings settings, string arg2)
        {
            Ip = settings.ClientSettings.Ip;
            Port = settings.ClientSettings.Port;
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

        private string mIp = string.Empty;
        public string Ip 
        { 
            get { return mIp; }
            private set
            {
                if (mIp.Equals(value))
                    return;

                mIp = value;
                RecreateTcpClient();
            } 
        }

        private int mPort;
        public int Port 
        { 
            get { return mPort; }
            private set
            {
                if (mPort == value)
                    return;

                mPort = value;
                RecreateTcpClient();
            } 
        }


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

            await ConnectAsync();

            if (!mTcpClient.Connected)
                throw new TimeoutException();

            var code = new Dictionary<string, object>() { { messageType, sourceCode } };
            _ = await mTcpClient.SendAsync("", code);
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

        private async Task ConnectAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    mTcpClient.Connect();
                }
                catch (TimeoutException ex)
                {
                    mLogger.LogError("{error}", ex.Message);
                    mLogger.LogError("Remote python service unaviable");
                    return;
                }
                catch (SocketException ex)
                {
                    mLogger.LogError("{error}", ex.Message);
                    mLogger.LogError("Socket exception because remote python service unaviable");
                    return;
                }
                catch (Exception ex)
                {
                    mLogger.LogError("{error}", ex.Message);
                    return;
                }
            });
        }

        private void RecreateTcpClient()
        {
            if (mTcpClient == null)
                return;

            if (!mTcpClient.Connected)
            {
                mLogger.LogInformation("The client's address has been changed");
            }

            if (mTcpClient.Connected)
            {
                mTcpClient.Disconnect(true);
                mLogger.LogInformation("The connection was interrupted to change the client's address");
            }

            Unsubscribe();
            mTcpClient.Dispose();
            mTcpClient = null;
            mTcpClient = new WatsonTcpClient(Ip, Port);
            Subscribe();
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
