using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EasySave.UI.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}