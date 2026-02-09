using System.Windows;
using SyncDemo.WpfApp.ViewModels;
  
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
