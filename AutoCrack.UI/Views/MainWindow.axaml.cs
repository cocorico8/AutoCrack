using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AutoCrack.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Handles dragging the frameless window
        private void OnDragWindow(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        // Handles the close button
        private void OnCloseBtnClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}