using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QuickTrans.App.ViewModels;

namespace QuickTrans.App.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += OnLoaded;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private void WindowRoot_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _viewModel.RegisterInteraction();
    }

    private void DragHandle_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _viewModel.RegisterInteraction();
        TryDragMove();
    }

    private void CollapsedShell_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _viewModel.RegisterInteraction();

        if (e.OriginalSource is DependencyObject source &&
            FindAncestor<Button>(source) is not null)
        {
            return;
        }

        TryDragMove();
    }

    private void ExpandButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.RegisterInteraction();
    }

    private void InputTextBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        _viewModel.RegisterInteraction();
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
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsExpanded) && _viewModel.IsExpanded)
        {
            Dispatcher.BeginInvoke(FocusInputTextBox, DispatcherPriority.Input);
        }
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

    private static T? FindAncestor<T>(DependencyObject dependencyObject) where T : DependencyObject
    {
        var current = dependencyObject;

        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
