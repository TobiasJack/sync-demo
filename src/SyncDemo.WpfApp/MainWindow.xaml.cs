using SyncDemo.WpfApp.ViewModels;
using ModernWpf.Controls;

namespace SyncDemo.WpfApp;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel, SyncItemsViewModel syncItemsViewModel)
    {
        InitializeComponent();
        
        var mainVm = viewModel;
        mainVm.SyncItemsViewModel = syncItemsViewModel;
        
        DataContext = mainVm;
    }
}
