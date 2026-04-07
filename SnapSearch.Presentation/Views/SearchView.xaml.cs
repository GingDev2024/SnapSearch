using SnapSearch.Presentation.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace SnapSearch.Presentation.Views
{
    public partial class SearchView : System.Windows.Controls.UserControl
    {
        public SearchView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender,
            System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is SearchViewModel vm)
                vm.OpenPreviewRequested += OnOpenPreviewRequested;
        }

        private void OnOpenPreviewRequested(Application.DTOs.FileResultDto file, string keyword)
        {
            var win = new FilePreviewWindow(file, keyword);
            win.Show();
        }

        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg &&
                dg.SelectedItem is Application.DTOs.FileResultDto file)
            {
                if (DataContext is SearchViewModel vm &&
                    vm.OpenFilePreviewCommand.CanExecute(file))
                {
                    vm.OpenFilePreviewCommand.Execute(file);
                }
            }
        }

        private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is SearchViewModel vm)
            {
                if (vm.SearchCommand.CanExecute(null))
                {
                    vm.SearchCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}