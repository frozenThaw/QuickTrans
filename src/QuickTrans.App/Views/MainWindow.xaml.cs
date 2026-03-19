using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using QuickTrans.App.ViewModels;

namespace QuickTrans.App.Views;

public partial class MainWindow : Window
{
    private const double EdgeDockThreshold = 48;
    private const double EdgeRevealWidth = 20;
    private static readonly TimeSpan AutoHideDelay = TimeSpan.FromSeconds(5);

    private readonly MainWindowViewModel _viewModel;
    private readonly DispatcherTimer _autoHideTimer;

    private DockSide _dockSide;
    private bool _isHiddenAtEdge;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _autoHideTimer = new DispatcherTimer
        {
            Interval = AutoHideDelay
        };
        _autoHideTimer.Tick += OnAutoHideTimerTick;

        DataContext = viewModel;
        Loaded += OnLoaded;
        Activated += OnActivated;
        Deactivated += OnDeactivated;
        MouseEnter += OnWindowMouseEnter;
        MouseLeave += OnWindowMouseLeave;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnClosed(EventArgs e)
    {
        _autoHideTimer.Stop();
        _autoHideTimer.Tick -= OnAutoHideTimerTick;
        MouseEnter -= OnWindowMouseEnter;
        MouseLeave -= OnWindowMouseLeave;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private void WindowRoot_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        StopAutoHideTimer();
    }

    private void DragHandle_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        StopAutoHideTimer();
        RevealFromEdge();
        TryDragMove();
        DockToEdgeIfNeeded();
    }

    private void InputTextBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        StopAutoHideTimer();
    }

    private void InputTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None)
        {
            return;
        }

        if (_viewModel.TranslateCommand.CanExecute(null))
        {
            _viewModel.TranslateCommand.Execute(null);
        }

        e.Handled = true;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionWindow();
        FocusInputTextBox();
        ScheduleAutoHideIfNeeded();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.InputText))
        {
            OnInputStateChanged();
            return;
        }

        if (e.PropertyName == nameof(MainWindowViewModel.IsBusy))
        {
            OnBusyStateChanged();
        }
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        StopAutoHideTimer();
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        ScheduleAutoHideIfNeeded();
    }

    private void PositionWindow()
    {
        UpdateLayout();

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + (workArea.Width - ActualWidth) / 2;
        Top = workArea.Top + 72;
    }

    private void FocusInputTextBox()
    {
        Activate();
        InputTextBox.Focus();
        InputTextBox.CaretIndex = InputTextBox.Text.Length;
    }

    private void TryDragMove()
    {
        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void OnWindowMouseEnter(object sender, MouseEventArgs e)
    {
        StopAutoHideTimer();

        if (_isHiddenAtEdge)
        {
            RevealFromEdge();
        }
    }

    private void OnWindowMouseLeave(object sender, MouseEventArgs e)
    {
        ScheduleAutoHideIfNeeded();
    }

    private void OnAutoHideTimerTick(object? sender, EventArgs e)
    {
        StopAutoHideTimer();
        HideIntoEdgeIfNeeded();
    }

    private void OnInputStateChanged()
    {
        if (HasInput())
        {
            StopAutoHideTimer();
            RevealFromEdge();
            return;
        }

        ScheduleAutoHideIfNeeded();
    }

    private void OnBusyStateChanged()
    {
        if (_viewModel.IsBusy)
        {
            StopAutoHideTimer();
            RevealFromEdge();
            return;
        }

        ScheduleAutoHideIfNeeded();
    }

    private void DockToEdgeIfNeeded()
    {
        UpdateLayout();

        var workArea = SystemParameters.WorkArea;
        ClampTop(workArea);

        var distanceToLeft = Math.Abs(Left - workArea.Left);
        var distanceToRight = Math.Abs(workArea.Right - (Left + ActualWidth));
        var shouldDock = distanceToLeft <= EdgeDockThreshold || distanceToRight <= EdgeDockThreshold;

        if (!shouldDock)
        {
            _dockSide = DockSide.None;
            _isHiddenAtEdge = false;
            Left = Math.Max(workArea.Left, Math.Min(Left, workArea.Right - ActualWidth));
            return;
        }

        _dockSide = distanceToLeft <= distanceToRight ? DockSide.Left : DockSide.Right;
        HideIntoEdgeIfNeeded();
    }

    private void HideIntoEdgeIfNeeded()
    {
        if (_dockSide == DockSide.None || _viewModel.IsBusy)
        {
            return;
        }

        UpdateLayout();

        var workArea = SystemParameters.WorkArea;
        var revealWidth = GetRevealWidth();

        Left = _dockSide switch
        {
            DockSide.Left => workArea.Left - (ActualWidth - revealWidth),
            DockSide.Right => workArea.Right - revealWidth,
            _ => Left
        };

        ClampTop(workArea);
        _isHiddenAtEdge = true;
    }

    private void RevealFromEdge()
    {
        if (_dockSide == DockSide.None || !_isHiddenAtEdge)
        {
            return;
        }

        UpdateLayout();

        var workArea = SystemParameters.WorkArea;
        Left = _dockSide switch
        {
            DockSide.Left => workArea.Left,
            DockSide.Right => workArea.Right - ActualWidth,
            _ => Left
        };

        ClampTop(workArea);
        _isHiddenAtEdge = false;
    }

    private void ScheduleAutoHideIfNeeded()
    {
        StopAutoHideTimer();

        if (_dockSide == DockSide.None || _isHiddenAtEdge || _viewModel.IsBusy || IsMouseOver || IsActive)
        {
            return;
        }

        _autoHideTimer.Start();
    }

    private void StopAutoHideTimer()
    {
        _autoHideTimer.Stop();
    }

    private void ClampTop(Rect workArea)
    {
        var maxTop = Math.Max(workArea.Top, workArea.Bottom - ActualHeight);
        Top = Math.Max(workArea.Top, Math.Min(Top, maxTop));
    }

    private double GetRevealWidth()
    {
        return EdgeRevealWidth + Math.Max(MainShell.Margin.Left, MainShell.Margin.Right);
    }

    private bool HasInput()
    {
        return !string.IsNullOrWhiteSpace(_viewModel.InputText);
    }

    private enum DockSide
    {
        None,
        Left,
        Right
    }
}
