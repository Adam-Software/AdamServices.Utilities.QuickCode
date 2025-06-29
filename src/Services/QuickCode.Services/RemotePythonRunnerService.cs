using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickCode.Services.Interfaces;
using QuickCode.Services.Interfaces.RemotePythonRunnerServiceDependency.CustomCollection;
using QuickCode.Services.Interfaces.RemotePythonRunnerServiceDependency.JsonModel;
using SimpleUdp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using WatsonTcp;

namespace QuickCode.Services
{
    public class RemotePythonRunnerService : IRemotePythonRunnerService
    {
        //public event DataReceivedEventHandler RaiseDataReceivedEvent;
        public event IsConnectedChangeEventHandler RaiseIsConnectedChangeEvent;
       
        #region Services

        private readonly ILogger<RemotePythonRunnerService> mLogger;

        #endregion

        #region Var

        private WatsonTcpClient mTcpClient;
        private UdpEndpoint mUdpEndpoint;
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
            mUdpEndpoint = new UdpEndpoint(Ip, Port);

            appSettingsMonitor.OnChange(OnChangeClientSettings);
            Subscribe();

            EventQueue = new EventQueue<string>();

            mLogger.LogInformation("Service run on {ip}:{port}", Ip, Port);
            
        }

        #endregion

        #region Subscribe/Unsubscribe

        private void Subscribe()
        {
            mTcpClient.Events.ServerConnected += ServerConnected;
            mTcpClient.Events.MessageReceived += MessageReceived;
            mTcpClient.Events.ExceptionEncountered += ExceptionEncountered;
            mTcpClient.Events.ServerDisconnected += ServerDisconnected;
            //EventQueue.Enqueued += Enqueued;
            //mTcpClient.Callbacks.SyncRequestReceivedAsync += SyncRequestReceivedAsync;

            mUdpEndpoint.DatagramReceived += DatagramReceived;
        }

        /*private async Task<SyncResponse> SyncRequestReceivedAsync(SyncRequest request)
        {
            await Task.Run(() =>
            {
                return new SyncResponse(request, string.Empty);
            });
            var message = System.Text.Encoding.UTF8.GetString(request.Data);
            OnRaiseClientDataReceivedEvent(message);

            return await Task.FromResult(new SyncResponse(request, string.Empty));
            
        }*/

        private void Unsubscribe()
        {
            mTcpClient.Events.ServerConnected -= ServerConnected;
            mTcpClient.Events.MessageReceived -= MessageReceived;
            mTcpClient.Events.ExceptionEncountered -= ExceptionEncountered;
            mTcpClient.Events.ServerDisconnected -= ServerDisconnected;
            //mTcpClient.Callbacks.SyncRequestReceivedAsync -= SyncRequestReceivedAsync;
            //EventQueue.Enqueued += Enqueued;
        }

        #endregion

        #region Events

        private void ServerConnected(object sender, ConnectionEventArgs e)
        {   
            IsConnected = true;
            ExitData = new();

            mLogger.LogInformation("Client connected");
        }

        private void ServerDisconnected(object sender, DisconnectionEventArgs e)
        {
            EventQueue.Clear();
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
                return;
            }

            mLogger.LogError("ExceptionEncountered {error}", e.Exception);
        }

        private void MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Metadata != null)
                MetadataReceived(e.Metadata); 
        }

       

        private void DatagramReceived(object sender, Datagram e)
        {
            var message = System.Text.Encoding.UTF8.GetString(e.Data);
            EventQueue.Enqueue(message);
            //OnRaiseClientDataReceivedEvent(message);
        }

        /*private void Enqueued(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }*/

        private ExitData mExitdata = new();
        public ExitData ExitData
        {
            get { return mExitdata; }
            set 
            { 
                if(value == null)
                    return; 

                mExitdata = value; 
            }
        }

        private void MetadataReceived(Dictionary<string, object> metadata)
        {
            foreach (var key in metadata.Keys)
            {
                switch (key)
                {
                    case "exitData":
                        {
                            ExitData = mTcpClient.SerializationHelper.DeserializeJson<ExitData>(metadata[key].ToString());
                            break;
                        }
                }
            }
        }

        private void OnChangeClientSettings(AppSettings settings, string arg2)
        {
            Ip = settings.ClientSettings.Ip;
            Port = settings.ClientSettings.Port;
        }

        #endregion

        #region Public fields

        public EventQueue<string> EventQueue { get; } 

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

            var exitMetadata = new Dictionary<string, object>() { { "exit", "" } };

            try
            {
                await mTcpClient.SendAsync("", exitMetadata);
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

        /*protected virtual void OnRaiseClientDataReceivedEvent(string data)
        {
            var raiseEvent = RaiseDataReceivedEvent;
            raiseEvent?.Invoke(this, data);

        }*/

        protected virtual void OnIsConnectedChangeEvent()
        {
            var raiseEven = RaiseIsConnectedChangeEvent;
            raiseEven?.Invoke(this);
        }

        #endregion
    }
}
