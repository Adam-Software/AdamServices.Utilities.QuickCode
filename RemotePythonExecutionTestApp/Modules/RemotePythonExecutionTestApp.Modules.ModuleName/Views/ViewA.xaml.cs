using System.Windows.Controls;

namespace RemotePythonExecutionTestApp.Modules.ModuleName.Views
{
    public partial class ViewA : UserControl
    {
        public ViewA()
        {
            InitializeComponent();

            ExecutionResultEditor.TextChanged += ExecutionResultEditor_TextChanged;
        }

        private void ExecutionResultEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            ExecutionResultEditor.ScrollToEnd();
        }
    }
}
