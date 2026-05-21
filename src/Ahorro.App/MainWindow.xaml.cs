using System.Windows;
using Ahorro.ViewModels.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Ahorro.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.HostApp.Services.GetRequiredService<MainShellViewModel>();
    }
}
