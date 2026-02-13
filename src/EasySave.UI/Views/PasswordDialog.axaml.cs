using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace EasySave.UI.Views
{
    public partial class PasswordDialog : Window
    {
        public string? Password { get; private set; }

        public PasswordDialog()
        {
            AvaloniaXamlLoader.Load(this);

            var confirmBtn = this.FindControl<Button>("ConfirmButton")!;
            var cancelBtn = this.FindControl<Button>("CancelButton")!;

            confirmBtn.Click += OnConfirmClick;
            cancelBtn.Click += OnCancelClick;
        }

        public PasswordDialog(string prompt) : this()
        {
            var promptText = this.FindControl<TextBlock>("PromptText")!;
            promptText.Text = prompt;
        }

        private void OnConfirmClick(object? sender, RoutedEventArgs e)
        {
            var passwordBox = this.FindControl<TextBox>("PasswordBox")!;
            Password = passwordBox.Text;
            Close(Password);
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Password = null;
            Close(null);
        }
    }
}
