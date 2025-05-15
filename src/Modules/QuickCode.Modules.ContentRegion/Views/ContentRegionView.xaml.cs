using System.Windows.Controls;

namespace QuickCode.Modules.ContentRegion.Views
{
    public partial class ContentRegionView : UserControl
    {
        public ContentRegionView()
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
