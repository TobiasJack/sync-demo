using SyncDemo.WpfApp.ViewModels;

namespace SyncDemo.WpfApp;

public partial class MainWindow : ModernWpf.Controls.Window
{
    public MainWindow(MainViewModel viewModel, SyncItemsViewModel syncItemsViewModel)
    {
        InitializeComponent();
        
        var mainVm = viewModel;
        mainVm.SyncItemsViewModel = syncItemsViewModel;
        
        DataContext = mainVm;
    }
}
