using Korendzh.Mobile.Services;

namespace Korendzh.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly KorendzhApiClient _api;
    private readonly AuthState _auth;

    public LoginPage(KorendzhApiClient api, AuthState auth)
    {
        InitializeComponent();
        _api = api;
        _auth = auth;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _auth.LoadAsync();
        if (_auth.IsAuthenticated)
        {
            await Shell.Current.GoToAsync("//entries");
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var pwd = PasswordEntry.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(pwd))
        {
            ErrorLabel.Text = "Введите email и пароль.";
            ErrorLabel.IsVisible = true;
            return;
        }

        ErrorLabel.IsVisible = false;
        LoginBtn.IsEnabled = false;
        try
        {
            var resp = await _api.LoginAsync(email, pwd);
            if (resp is null)
            {
                ErrorLabel.Text = "Неверный email или пароль.";
                ErrorLabel.IsVisible = true;
                return;
            }
            await Shell.Current.GoToAsync("//entries");
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = "Ошибка соединения: " + ex.Message;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoginBtn.IsEnabled = true;
        }
    }
}
