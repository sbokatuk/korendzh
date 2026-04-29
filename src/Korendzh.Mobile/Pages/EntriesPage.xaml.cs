using Korendzh.Mobile.Services;

namespace Korendzh.Mobile.Pages;

public partial class EntriesPage : ContentPage
{
    private readonly KorendzhApiClient _api;
    private readonly AuthState _auth;

    public EntriesPage(KorendzhApiClient api, AuthState auth)
    {
        InitializeComponent();
        _api = api;
        _auth = auth;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_auth.IsAuthenticated)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var items = await _api.GetMyEntriesAsync();
            EntriesList.ItemsSource = items;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", "Не удалось загрузить записи: " + ex.Message, "OK");
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e) => await LoadAsync();

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//create");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        _auth.Clear();
        SecureStorage.Default.RemoveAll();
        await Shell.Current.GoToAsync("//login");
    }
}
