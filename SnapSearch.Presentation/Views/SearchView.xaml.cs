using SnapSearch.Presentation.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace SnapSearch.Presentation.Views
{
    public partial class SearchView : System.Windows.Controls.UserControl
    {
        #region Fields

        private FilePreviewWindow? _previewWindow;

        #endregion Fields

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
            if (e.OldValue is SearchViewModel oldVm)
                oldVm.OpenPreviewRequested -= OnOpenPreviewRequested;

            if (e.NewValue is SearchViewModel vm)
                vm.OpenPreviewRequested += OnOpenPreviewRequested;
        }

        private void OnOpenPreviewRequested(Application.DTOs.FileResultDto file, string keyword)
        {
            if (DataContext is not SearchViewModel vm)
                return;

            var files = vm.SearchResults.ToList();
            int index = files.IndexOf(file);

            if (index < 0)
                index = 0;

            _previewWindow?.Close();

            _previewWindow = new FilePreviewWindow(files, index, keyword);
            _previewWindow.Closed += (s, e) => _previewWindow = null;
            _previewWindow.Show();
        }

        private void DataGrid_MouseDoubleClick(object sender,
            MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg &&
                dg.SelectedItem is Application.DTOs.FileResultDto file &&
                DataContext is SearchViewModel vm &&
                vm.OpenFilePreviewCommand.CanExecute(file))
            {
                vm.OpenFilePreviewCommand.Execute(file);
            }
        }

        private void KeywordBox_GotFocus(object sender,
            System.Windows.RoutedEventArgs e)
        {
            if (DataContext is SearchViewModel vm)
                vm.ShowSuggestions = vm.FilteredSuggestions.Count > 0
                                  && !string.IsNullOrWhiteSpace(vm.Keyword);
        }

        private void KeywordBox_LostFocus(object sender,
            System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (DataContext is SearchViewModel vm)
                    vm.ShowSuggestions = false;
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void SuggestionsList_PreviewMouseDown(object sender,
            MouseButtonEventArgs e)
        {
            e.Handled = false;
        }

        private void TextBox_PreviewKeyDown(object sender,
            System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is not SearchViewModel vm)
                return;

            if (e.Key == Key.Enter)
            {
                vm.ShowSuggestions = false;

                if (vm.SearchCommand.CanExecute(null))
                {
                    vm.SearchCommand.Execute(null);
                    e.Handled = true;
                }

                return;
            }

            if (e.Key == Key.Escape)
            {
                vm.ShowSuggestions = false;
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Down && vm.ShowSuggestions && SuggestionsListBox.Items.Count > 0)
            {
                SuggestionsListBox.Focus();
                SuggestionsListBox.SelectedIndex = 0;
                e.Handled = true;
            }
        }

        #endregion Private Methods
    }
}