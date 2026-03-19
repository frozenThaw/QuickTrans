using System.Windows.Input;
using System.Windows.Threading;
using QuickTrans.App.Infrastructure;
using QuickTrans.App.Services;

namespace QuickTrans.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(8);

    private readonly ITranslationService _translationService;
    private readonly DispatcherTimer _idleTimer;
    private readonly AsyncRelayCommand _translateCommand;
    private readonly RelayCommand _expandCommand;
    private readonly RelayCommand _collapseCommand;

    private string _inputText = string.Empty;
    private string _translatedText = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private bool _isExpanded = true;
    private bool _isWindowActive;
    private bool _isResultVisible;
    private DateTimeOffset _lastInteractionAt = DateTimeOffset.UtcNow;
    private CancellationTokenSource? _activeRequestCts;

    public MainWindowViewModel(ITranslationService translationService)
    {
        _translationService = translationService;

        _translateCommand = new AsyncRelayCommand(TranslateAsync, () => !string.IsNullOrWhiteSpace(InputText));
        _expandCommand = new RelayCommand(Expand);
        _collapseCommand = new RelayCommand(Collapse, () => !IsBusy && IsExpanded);

        _idleTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _idleTimer.Tick += OnIdleTimerTick;
        _idleTimer.Start();
    }

    public ICommand TranslateCommand => _translateCommand;

    public ICommand ExpandCommand => _expandCommand;

    public ICommand CollapseCommand => _collapseCommand;

    public string InputText
    {
        get => _inputText;
        set
        {
            if (!SetProperty(ref _inputText, value))
            {
                return;
            }

            RegisterInteraction();

            if (string.IsNullOrWhiteSpace(value))
            {
                _activeRequestCts?.Cancel();
                ResetResult();
            }

            _translateCommand.RaiseCanExecuteChanged();
        }
    }

    public string TranslatedText
    {
        get => _translatedText;
        private set
        {
            if (SetProperty(ref _translatedText, value))
            {
                OnPropertyChanged(nameof(HasTranslation));
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsResultPanelVisible));
                _collapseCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        private set
        {
            if (SetProperty(ref _isExpanded, value))
            {
                _collapseCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsResultVisible
    {
        get => _isResultVisible;
        private set
        {
            if (SetProperty(ref _isResultVisible, value))
            {
                OnPropertyChanged(nameof(IsResultPanelVisible));
            }
        }
    }

    public bool IsResultPanelVisible => IsBusy || IsResultVisible;

    public bool HasTranslation => !string.IsNullOrWhiteSpace(TranslatedText);

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public void RegisterInteraction()
    {
        _lastInteractionAt = DateTimeOffset.UtcNow;
    }

    public void SetWindowActive(bool isActive)
    {
        _isWindowActive = isActive;

        if (isActive)
        {
            RegisterInteraction();
        }
    }

    public void Dispose()
    {
        _idleTimer.Stop();
        _idleTimer.Tick -= OnIdleTimerTick;

        if (_activeRequestCts is not null)
        {
            _activeRequestCts.Cancel();
            _activeRequestCts.Dispose();
            _activeRequestCts = null;
        }
    }

    private async Task TranslateAsync()
    {
        var normalizedInput = InputText.Trim();

        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            return;
        }

        RegisterInteraction();
        Expand();

        _activeRequestCts?.Cancel();

        var requestCts = new CancellationTokenSource();
        _activeRequestCts = requestCts;

        IsBusy = true;
        ErrorMessage = string.Empty;
        TranslatedText = string.Empty;

        try
        {
            var result = await _translationService.TranslateToChineseAsync(normalizedInput, requestCts.Token);

            if (!ReferenceEquals(_activeRequestCts, requestCts))
            {
                return;
            }

            if (result.IsSuccess)
            {
                TranslatedText = result.TranslatedText;
                ErrorMessage = string.Empty;
            }
            else
            {
                TranslatedText = string.Empty;
                ErrorMessage = result.ErrorMessage;
            }

            IsResultVisible = true;
        }
        catch (OperationCanceledException) when (requestCts.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (!ReferenceEquals(_activeRequestCts, requestCts))
            {
                return;
            }

            TranslatedText = string.Empty;
            ErrorMessage = "Translation failed unexpectedly.";
            IsResultVisible = true;
        }
        finally
        {
            if (ReferenceEquals(_activeRequestCts, requestCts))
            {
                _activeRequestCts = null;
                IsBusy = false;
            }

            requestCts.Dispose();
        }
    }

    private void Expand()
    {
        IsExpanded = true;
        RegisterInteraction();
    }

    private void Collapse()
    {
        if (IsBusy)
        {
            return;
        }

        IsExpanded = false;
    }

    private void ResetResult()
    {
        IsResultVisible = false;
        TranslatedText = string.Empty;
        ErrorMessage = string.Empty;
    }

    private void OnIdleTimerTick(object? sender, EventArgs e)
    {
        if (!IsExpanded || IsBusy || _isWindowActive)
        {
            return;
        }

        if (DateTimeOffset.UtcNow - _lastInteractionAt >= IdleTimeout)
        {
            Collapse();
        }
    }
}
