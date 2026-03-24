using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SnapSearch.Presentation.Common
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        #region Fields

        private bool _isBusy;

        private string _statusMessage = string.Empty;

        #endregion Fields

        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion Events

        #region Properties

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        #endregion Properties

        #region Protected Methods

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
                            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        #endregion Protected Methods
    }
}