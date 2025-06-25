using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using QuickCode.Core.Mvvm;
using QuickCode.Services.Interfaces;
using QuickCode.Services.Interfaces.RemotePythonRunnerServiceDependency.JsonModel;
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
            UpdateExecutionResultBackground(data);
        }

        private void UpdateExecutionResultBackground(string data, bool isExitData = false)
        {
            Application.Current?.Dispatcher?.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (!IsExecutionStop && !isExitData)
                {
                    ExecutionResult.AppendLine(data);
                    return;
                }
                    
                if (isExitData)
                {
                    ExecutionResult.AppendLine(data);
                    IsExecutionStop = true;
                }
            }));
        }

      

        private void RaiseIsConnectedChangeEvent(object sender)
        {
            IsConnected = mRemotePythonRunner.IsConnected;
            
            if (!IsConnected)
            {
                ExitData exitData = mRemotePythonRunner.ExitData;
                var executionResult = $"Debug sessoin ended.";


                if (exitData.IsExitDataUpdated) 
                    executionResult = $"Debug sessoin ended.\n" +
                        $"Exit code {exitData.ExitCode}\n" +
                        $"Process Id {exitData.ProcessId}\n" +
                        $"Start Time {exitData.StartTime}\n" +
                        $"Exit Time {exitData.ExitTime}\n" +
                        $"Total Processor Time {exitData.TotalProcessorTime}\n" +
                        $"User Processor Time {exitData.UserProcessorTime}";

                UpdateExecutionResultBackground(executionResult, true);
            }
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

        public BindableStringBuilder ExecutionResult {  get; } = new BindableStringBuilder();

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

                if (!isNewValue)
                    return;

                CodeExecuteCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
                SendDebuggerCommand.RaiseCanExecuteChanged();

            }
        }

        private bool mIsExecutionStop = true;
        public bool IsExecutionStop
        {
            get { return mIsExecutionStop; }
            set
            {
                bool isNewValue = SetProperty(ref mIsExecutionStop, value);

                if (!isNewValue)
                    return;

                CodeExecuteCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
                SendDebuggerCommand.RaiseCanExecuteChanged();
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

            ExecutionResult.Clear();
            string sourceCode = SourceCode;
            IsExecutionStop = false;

            try
            {
                await mRemotePythonRunner.ConnectAndSendCodeAsync(sourceCode, IsDebugEnable);
            }
            catch (TimeoutException)
            {
                UpdateExecutionResultBackground($"Не могу подключиться к серверу по указаному адресу {mRemotePythonRunner.Ip}:{mRemotePythonRunner.Port}");
            }
            catch (SocketException)
            {
                UpdateExecutionResultBackground($"Не могу подключиться к серверу по указаному адресу {mRemotePythonRunner.Ip}:{mRemotePythonRunner.Port}");
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

            return !string.IsNullOrEmpty(SourceCode) && IsExecutionStop;
        }

        private void Stop()
        {
            mRemotePythonRunner.DisconnectAsync(IsDebugEnable);
            IsExecutionStop = true;
        }

        private bool StopCanExecute()
        {
            return !IsExecutionStop;
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
