using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Regions;
using QuickCode.Core.Mvvm;
using QuickCode.Services.Interfaces;
using System;
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

        public ContentRegionViewModel(IRegionManager regionManager, IRemotePythonRunnerService remotePythonRunner, ILogger<ContentRegionViewModel> logger) : base(regionManager)
        {
            mLogger = logger;
            mRemotePythonRunner = remotePythonRunner;

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

        public override void OnNavigatedTo(NavigationContext navigationContext) {}

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
            set { SetProperty(ref mIsConnected, value); }
        }

        #endregion

        #region DelegateCommand methods

        private void Start()
        {
            mExecutionResult = "";
            var sourceCode = SourceCode;
            mRemotePythonRunner.ConnectAndSendCodeAsync(sourceCode, EnableDebug);
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
            return true;
        }

        private void SendDebugger(object commandParam)
        {
            var command = commandParam as string;
            mRemotePythonRunner.SendСontrolCharacter(command);
        }

        private bool SendDebuggerCanExecute(object arg)
        {
            return true;
        }

        #endregion

    }
}
