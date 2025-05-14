using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PHS.Networking.Enums;
using RemotePythonExecutionTestApp.Services.Interfaces;
using System;
using Tcp.NET.Client;
using Tcp.NET.Client.Events.Args;
using Tcp.NET.Client.Models;

namespace RemotePythonExecutionTestApp.Services
{
    public class RemotePythonRunnerService : IRemotePythonRunnerService
    {
        #region Services

        private ILogger<RemotePythonRunnerService> mLogger;

        #endregion

        #region Var

        private readonly TcpNETClient mTcpClient;

        #endregion

        #region ~

        public RemotePythonRunnerService(IServiceProvider serviceProvider)
        {
            mLogger = serviceProvider.GetService<ILogger<RemotePythonRunnerService>>();

            mTcpClient = new TcpNETClient(new ParamsTcpClient("127.0.0.1", 18000, "\r\n"));
            

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
            throw new NotImplementedException();
        }

        private void ConnectionEvent(object sender, TcpConnectionClientEventArgs args)
        {
            ConnectionEventType connectionEvent = args.ConnectionEventType;

            switch (connectionEvent)
            {
                case ConnectionEventType.Connected:
                    {
                        mLogger.LogInformation("Client connected");
                    }

                    break;

                case ConnectionEventType.Disconnect:
                    {
                        mLogger.LogInformation("Client disconected");
                    }
                    break;
            }
        }

        #endregion

        #region Public fields



        #endregion

        #region Public methods

        public void ConnectAsync()
        {
            mTcpClient.ConnectAsync();
        }

        public void SendCode(string sourceCode)
        {
            mTcpClient.SendAsync(sourceCode);
            //return "Hello from the Message Service";
        }

        #endregion
    }
}
