using SnapSearch.Presentation.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace SnapSearch.Presentation.Views
{
    public partial class SearchView : System.Windows.Controls.UserControl
    {
        #region Constructor

        public SearchView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        #endregion Constructor

        #region Private Methods — DataContext

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

        #endregion Private Methods — DataContext

        #region Private Methods — DataGrid

        private void DataGrid_MouseDoubleClick(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
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

        #endregion Private Methods — DataGrid

        #region Private Methods — Autocomplete

        private void KeywordBox_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is SearchViewModel vm)
                vm.ShowSuggestions = vm.FilteredSuggestions.Count > 0
                                  && !string.IsNullOrWhiteSpace(vm.Keyword);
        }

        private void KeywordBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            // Delay hiding so a click on a suggestion item is processed first
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (DataContext is SearchViewModel vm)
                    vm.ShowSuggestions = false;
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void SuggestionsList_PreviewMouseDown(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            // Allow the ListBox selection to process before LostFocus hides it
            e.Handled = false;
        }

        #endregion Private Methods — Autocomplete

        #region Private Methods — Keyboard

        private void TextBox_PreviewKeyDown(object sender,
            System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is not SearchViewModel vm)
                return;

            // Enter → run search
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

            // Escape → dismiss suggestions
            if (e.Key == Key.Escape)
            {
                vm.ShowSuggestions = false;
                e.Handled = true;
                return;
            }

            // Down arrow → move focus into suggestion list
            if (e.Key == Key.Down && vm.ShowSuggestions && SuggestionsListBox.Items.Count > 0)
            {
                SuggestionsListBox.Focus();
                SuggestionsListBox.SelectedIndex = 0;
                e.Handled = true;
            }
        }

        #endregion Private Methods — Keyboard
    }
}