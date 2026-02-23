using Avalonia.Controls;
using Avalonia.Interactivity;

namespace EasySave.UI.Views
{
    public partial class ConfirmDialog : Window
    {
        public ConfirmDialog()
        {
            InitializeComponent();
        }
        public ConfirmDialog(string message) : this()
        {
            MessageText.Text = message;

            YesButton.Click += (sender, e) => Close(true);

            NoButton.Click += (sender, e) => Close(false);
        }
    }
}