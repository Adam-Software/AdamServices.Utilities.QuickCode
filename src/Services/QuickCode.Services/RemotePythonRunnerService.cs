using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickCode.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
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
            IsConnected = true;
            mLogger.LogInformation("Client connected");
        }

        private void ServerDisconnected(object sender, DisconnectionEventArgs e)
        {
            IsConnected = false;
            mLogger.LogInformation("Client disconected");
        }

        private void ExceptionEncountered(object sender, ExceptionEventArgs e)
        {
            if (e.Exception is IOException)
            {
                mLogger.LogError("ExceptionEncountered thow IOException");
                return;
            }
            
            if (e.Exception is ObjectDisposedException)
            {
                mLogger.LogError("ExceptionEncountered thow ObjectDisposedException");                
                return;
            }

            if (e.Exception is OperationCanceledException)
            {
                mLogger.LogError("ExceptionEncountered thow OperationCanceledException");
                //CheckClientDisconected();
                return;
            }

            mLogger.LogError("ExceptionEncountered {error}", e.Exception);
        }

        /*private void CheckClientDisconected()
        {
            Task.Run(async () =>
            {
                await Task.Delay(100);

                if(mTcpClient.Connected)
                {
                    mLogger.LogError("Client coonected after thow OperationCanceledException");
                    mTcpClient.Disconnect();
                }
                else
                {
                    mLogger.LogError("Client dicoonected after thow OperationCanceledException");
                    IsConnected = false;
                } 
            });
        }*/


        private void MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                var message = System.Text.Encoding.UTF8.GetString(e.Data);
                OnRaiseClientDataReceivedEvent(message);
            }
            catch (Exception ex) 
            {
                mLogger.LogError("MessageReceived {error}", ex);
            }
            
        }

        #endregion

        #region Public fields

        private string mIp = string.Empty;
        public string Ip 
        { 
            get { return mIp; }
            private set
            {
                if (value.Equals(mIp))
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
                if (value == mPort)
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
                if (value == mIsConnected) 
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

            try
            {
                mTcpClient.Connect();
                var code = new Dictionary<string, object>() { { messageType, sourceCode } };
                _ = await mTcpClient.SendAsync("", code);
            }
            catch (TimeoutException ex)
            {
                mLogger.LogError("{error}", ex.Message);
                mLogger.LogError("Remote python service unaviable");
                throw;
            }
            catch (SocketException ex)
            {
                mLogger.LogError("{error}", ex.Message);
                mLogger.LogError("Socket exception because remote python service unaviable");
                throw;
            }
            catch (Exception ex)
            {
                mLogger.LogError("ConnectAndSendCodeAsync {error}", ex.Message);
            }
        }


        public async Task SendСontrolCharacter(string controlCharacter)
        {   
            if(!mTcpClient.Connected)
                return;

            var character = new Dictionary<string, object>() { { "control_characters", controlCharacter } };

            try
            {
                _ = await mTcpClient.SendAsync("", character);
            }
            catch (Exception ex)
            {
                mLogger.LogError("SendСontrolCharacter {ex}", ex);
            } 
        }

        public async Task DisconnectAsync(bool withDebug = false)
        {
            if (!mTcpClient.Connected)
                return;

            if (withDebug)
            {
                await SendСontrolCharacter("q");
                return;
            }

            var exit = new Dictionary<string, object>() { { "exit", "" } };

            try
            {
                await mTcpClient.SendAsync("", exit);
                //mTcpClient.Disconnect(true);
            }
            catch (InvalidOperationException)
            {
                mLogger.LogError("DisconnectAsync thow InvalidOperationException");
                IsConnected = false;
            }
            catch (Exception ex)
            {
                mLogger.LogError("DisconnectAsync {ex}", ex);
            }
            
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

        private void ConnectAsync()
        {
            //return Task.Run(() =>
            //{
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
            //});
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
