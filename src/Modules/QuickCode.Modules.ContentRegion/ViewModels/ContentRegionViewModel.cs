using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using QuickCode.Core.Mvvm;
using QuickCode.Services.Interfaces;
using System;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Threading;

namespace QuickCode.Modules.ContentRegion.ViewModels
{
    public class ContentRegionViewModel : RegionViewModelBase
    {
        #region DelegateCommand

        public DelegateCommand<object> CodeExecuteCommand { get; }
        public DelegateCommand StopCommand { get; }
        public DelegateCommand<object> SendDebuggerCommand { get; }

        #endregion

        #region Services

        private readonly ILogger<ContentRegionViewModel> mLogger;
        private readonly IRemotePythonRunnerService mRemotePythonRunner;

        #endregion

        #region Var

        private bool mIsExecutionStop = false;

        #endregion

        #region ~

        public ContentRegionViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            mLogger = serviceProvider.GetService<ILogger<ContentRegionViewModel>>();
            mRemotePythonRunner = serviceProvider.GetService<IRemotePythonRunnerService>();

            CodeExecuteCommand = new DelegateCommand<object>(CodeExecute, CodeExecuteCanExecute);
            StopCommand = new DelegateCommand(Stop, StopCanExecute);
            SendDebuggerCommand = new DelegateCommand<object>(SendDebugger, SendDebuggerCanExecute);

            Subscribe();
        }

        #endregion

        #region Subscribe/Unsubscribe

        private void Subscribe()
        {
            mRemotePythonRunner.RaiseDataReceivedEvent += RaiseDataReceivedEvent;
            mRemotePythonRunner.RaiseIsConnectedChangeEvent += RaiseIsConnectedChangeEvent;
        }

        private void Unsubscribe()
        {
            mRemotePythonRunner.RaiseDataReceivedEvent -= RaiseDataReceivedEvent;
            mRemotePythonRunner.RaiseIsConnectedChangeEvent -= RaiseIsConnectedChangeEvent;
        }

        #endregion

        #region Navigation

        #endregion

        #region Events

        private void RaiseDataReceivedEvent(object sender, string data)
        {
            Application.Current?.Dispatcher?.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
               if(!mIsExecutionStop && IsConnected)
                {
                    ExecutionResult = ExecutionResult + $"{data}\n";
                } 
            }));
        }

        private void RaiseIsConnectedChangeEvent(object sender)
        {
            IsConnected = mRemotePythonRunner.IsConnected;

            if(!IsConnected)
                ExecutionResult = ExecutionResult + "Debug sessoin ended";
        }

        #endregion

        #region Public fields

        private string mSourceCode;
        public string SourceCode
        {
            get { return mSourceCode; }
            set 
            { 
                var isNewValue= SetProperty(ref mSourceCode, value);

                if (isNewValue)
                    CodeExecuteCommand.RaiseCanExecuteChanged();
            }
        }

        private string mExecutionResult = string.Empty;
        public string ExecutionResult
        {
            get { return mExecutionResult; }
            set
            {
                SetProperty(ref mExecutionResult, value);
            }
        }

        public bool mIsDebugEnable = false;
        public bool IsDebugEnable
        {
            get { return mIsDebugEnable; }
            set
            {
                SetProperty(ref mIsDebugEnable, value);
            }
        }

        public bool mIsConnected;
        public bool IsConnected
        {
            get { return mIsConnected; }
            set 
            { 
                var isNewValue = SetProperty(ref mIsConnected, value);

                if (isNewValue)
                {
                    CodeExecuteCommand.RaiseCanExecuteChanged();
                    StopCommand.RaiseCanExecuteChanged();
                    SendDebuggerCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region Public methods

        public override void Destroy()
        {
            Unsubscribe();
        }

        #endregion

        #region DelegateCommand methods

        private async void CodeExecute(object withDebug)
        {
            if (IsConnected && IsDebugEnable) 
            {
                await mRemotePythonRunner.SendСontrolCharacter("s");
                return;
            }

            bool parseResult = bool.TryParse(withDebug.ToString(), out bool isDebugEnable);
            if (parseResult) 
                IsDebugEnable = isDebugEnable;

            ExecutionResult = string.Empty;
            string sourceCode = SourceCode;
            mIsExecutionStop = false;

            try
            {
                await mRemotePythonRunner.ConnectAndSendCodeAsync(sourceCode, IsDebugEnable);
            }
            catch (TimeoutException)
            {
                ExecutionResult = $"Не могу подключиться к серверу по указаному адресу {mRemotePythonRunner.Ip}:{mRemotePythonRunner.Port}";
            }
            catch (SocketException)
            {
                ExecutionResult = $"Не могу подключиться к серверу по указаному адресу {mRemotePythonRunner.Ip}:{mRemotePythonRunner.Port}";
            }
            catch(Exception ex) 
            {
                mLogger.LogError("{error}", ex);
            }
        }

        private bool CodeExecuteCanExecute(object withDebug)
        {
            _ = bool.TryParse(withDebug.ToString(), out bool isDebugEnable);

            if (IsConnected && IsDebugEnable && isDebugEnable)
                return true;

            return !string.IsNullOrEmpty(SourceCode) && !IsConnected;
        }

        private void Stop()
        {
            mRemotePythonRunner.DisconnectAsync(IsDebugEnable);
            mIsExecutionStop = true;
        }

        private bool StopCanExecute()
        {
            return IsConnected;
        }

        private void SendDebugger(object commandParam)
        {
            var command = commandParam as string;
            mRemotePythonRunner.SendСontrolCharacter(command);
        }

        private bool SendDebuggerCanExecute(object arg)
        {
            return IsConnected && IsDebugEnable;
        }

        #endregion

    }
}
