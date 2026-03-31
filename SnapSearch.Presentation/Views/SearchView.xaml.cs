using SnapSearch.Presentation.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace SnapSearch.Presentation.Views
{
    public partial class SearchView : System.Windows.Controls.UserControl
    {
        #region Public Constructors
        public SearchView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }
        #endregion Public Constructors

        #region Private Methods
        private void OnDataContextChanged(object sender,
            System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is SearchViewModel vm)
                vm.OpenPreviewRequested += OnOpenPreviewRequested;
        }

        private void OnOpenPreviewRequested(
            Application.DTOs.FileResultDto file, string keyword)
        {
            var win = new FilePreviewWindow(file, keyword);
            win.Show();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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
        #endregion Private Methods
    }
}