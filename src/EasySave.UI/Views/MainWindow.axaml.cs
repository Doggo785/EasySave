using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace EasySave.UI.Views
{
    public partial class MainWindow : Window
    {
        // Tracks whether the window is currently in fullscreen (maximized) mode
        private bool _isFullscreen = true;

        public MainWindow()
        {
            InitializeComponent();

            // Maximize the window on startup
            this.Loaded += (s, e) =>
            {
                this.WindowState = WindowState.Maximized;
            };

            // Allow the window to be dragged by clicking anywhere when in normal (windowed) mode
            this.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
                    && this.WindowState == WindowState.Normal)
                {
                    BeginMoveDrag(e);
                }
            };

            // Handle Windows key shortcuts manually since SystemDecorations="None" disables native snap
            this.KeyDown += (s, e) =>
            {
                // Win + Up : maximize the window
                if (e.Key == Key.Up && e.KeyModifiers == KeyModifiers.Meta)
                {
                    this.WindowState = WindowState.Maximized;
                    _isFullscreen = true;
                    e.Handled = true;
                }
                // Win + Down : minimize the window to the taskbar
                else if (e.Key == Key.Down && e.KeyModifiers == KeyModifiers.Meta)
                {
                    this.WindowState = WindowState.Minimized;
                    e.Handled = true;
                }
            };
        }

        // Closes the application entirely
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Minimizes the window to the taskbar
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Toggles between maximized (fullscreen) and normal (windowed) mode
        // When switching to windowed mode, the window is centered on the screen
        private void ToggleFullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isFullscreen)
            {
                // Switch to windowed mode and center the window on screen
                var bounds = Screens.Primary?.Bounds ?? new Avalonia.PixelRect(0, 0, 1920, 1080);
                this.WindowState = WindowState.Normal;
                this.Width = 1100;
                this.Height = 750;
                this.Position = new Avalonia.PixelPoint(
                    bounds.Width / 2 - 550,
                    bounds.Height / 2 - 375
                );
                _isFullscreen = false;
            }
            else
            {
                // Switch back to fullscreen (maximized)
                this.WindowState = WindowState.Maximized;
                _isFullscreen = true;
            }
        }
    }
}