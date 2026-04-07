using SnapSearch.Presentation.ViewModels;

namespace SnapSearch.Presentation.Views
{
    public partial class HealthView : System.Windows.Controls.UserControl
    {
        #region Public Constructors

        public HealthView()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Public Methods

        // Stop the auto-refresh timer when the view is unloaded
        // (navigating away creates a fresh VM via the factory anyway)
        protected override void OnVisualParentChanged(
            System.Windows.DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            if (Parent is null && DataContext is HealthViewModel vm)
                vm.StopTimer();
        }

        #endregion Public Methods
    }
}