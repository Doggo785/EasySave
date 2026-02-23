using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace EasySave.UI.Views
{
    /// <summary>
    /// Dialog result: "local" = switch to local, "continue" = proceed anyway, null = cancel.
    /// </summary>
    public partial class ServerOfflineDialog : Window
    {
        public ServerOfflineDialog()
        {
            AvaloniaXamlLoader.Load(this);

            var switchBtn = this.FindControl<Button>("SwitchLocalButton")!;
            var continueBtn = this.FindControl<Button>("ContinueButton")!;
            var cancelBtn = this.FindControl<Button>("CancelButton")!;

            switchBtn.Click += OnSwitchLocal;
            continueBtn.Click += OnContinue;
            cancelBtn.Click += OnCancel;
        }

        public ServerOfflineDialog(string title, string message,
            string switchLabel, string continueLabel, string cancelLabel) : this()
        {
            this.FindControl<TextBlock>("TitleText")!.Text = title;
            this.FindControl<TextBlock>("MessageText")!.Text = message;
            this.FindControl<Button>("SwitchLocalButton")!.Content = switchLabel;
            this.FindControl<Button>("ContinueButton")!.Content = continueLabel;
            this.FindControl<Button>("CancelButton")!.Content = cancelLabel;
        }

        private void OnSwitchLocal(object? sender, RoutedEventArgs e) => Close("local");
        private void OnContinue(object? sender, RoutedEventArgs e) => Close("continue");
        private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);
    }
}
