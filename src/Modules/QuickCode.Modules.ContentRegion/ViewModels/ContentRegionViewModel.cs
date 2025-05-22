using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using QuickCode.Core.Mvvm;
using QuickCode.Services.Interfaces;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace QuickCode.Modules.ContentRegion.ViewModels
{
    public class ContentRegionViewModel : RegionViewModelBase
    {
        #region DelegateCommand

        public DelegateCommand StartCommand { get; }
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

            StartCommand = new DelegateCommand(Start, StartCanExecute);
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
                if(ExecutionResult != null && !mIsExecutionStop)
                    ExecutionResult = ExecutionResult + $"{data}\n";
            }));
        }

        private void RaiseIsConnectedChangeEvent(object sender)
        {
            IsConnected = mRemotePythonRunner.IsConnected;
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
                    StartCommand.RaiseCanExecuteChanged();
            }
        }

        private string mExecutionResult = "";
        public string ExecutionResult
        {
            get { return mExecutionResult; }
            set
            {
                SetProperty(ref mExecutionResult, value);
            }
        }

        public bool mEnableDebug = false;
        public bool EnableDebug
        {
            get { return mEnableDebug; }
            set
            {
                SetProperty(ref mEnableDebug, value);
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

        private async void Start()
        {
            ExecutionResult = "";
            string sourceCode = SourceCode;

            try
            {
                await mRemotePythonRunner.ConnectAndSendCodeAsync(sourceCode, EnableDebug);
            }
            catch (TimeoutException)
            {
                ExecutionResult = $"Не могу подключиться к серверу по указаному адресу {mRemotePythonRunner.Ip}:{mRemotePythonRunner.Port}";
            }
            catch(Exception ex) 
            {
                mLogger.LogError("{error}", ex);
            }
            
            mIsExecutionStop = false;
        }

        private bool StartCanExecute()
        {
            return !string.IsNullOrEmpty(SourceCode);
        }

        private void Stop()
        {
            mRemotePythonRunner.DisconnectAsync(EnableDebug);
            mIsExecutionStop= true; 
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
            return IsConnected && EnableDebug;
        }

        #endregion

    }
}
