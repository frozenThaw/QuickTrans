using System.Windows.Input;
using QuickTrans.App.Infrastructure;
using QuickTrans.App.Services;

namespace QuickTrans.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly ITranslationService _translationService;
    private readonly AsyncRelayCommand _translateCommand;

    private string _inputText = string.Empty;
    private string _translatedText = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private bool _isResultVisible;
    private CancellationTokenSource? _activeRequestCts;

    public MainWindowViewModel(ITranslationService translationService)
    {
        _translationService = translationService;

        _translateCommand = new AsyncRelayCommand(TranslateAsync, () => !string.IsNullOrWhiteSpace(InputText));
    }

    public ICommand TranslateCommand => _translateCommand;

    public string InputText
    {
        get => _inputText;
        set
        {
            if (!SetProperty(ref _inputText, value))
            {
                return;
            }

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

    public void Dispose()
    {
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

        _activeRequestCts?.Cancel();

        var requestCts = new CancellationTokenSource();
        _activeRequestCts = requestCts;

        IsBusy = true;
        ErrorMessage = string.Empty;
        TranslatedText = string.Empty;

        try
        {
            var result = await _translationService.TranslateAsync(normalizedInput, requestCts.Token);

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

    private void ResetResult()
    {
        IsResultVisible = false;
        TranslatedText = string.Empty;
        ErrorMessage = string.Empty;
    }
}
