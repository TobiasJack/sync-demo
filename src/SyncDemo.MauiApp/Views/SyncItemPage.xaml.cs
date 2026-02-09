using SyncDemo.MauiApp.ViewModels;

namespace SyncDemo.MauiApp.Views;

public partial class SyncItemPage : ContentPage
{
    public SyncItemPage(SyncItemViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
