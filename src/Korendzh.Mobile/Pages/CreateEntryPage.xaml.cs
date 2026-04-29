using System.Globalization;
using Korendzh.Mobile.Services;

namespace Korendzh.Mobile.Pages;

public partial class CreateEntryPage : ContentPage
{
    private readonly KorendzhApiClient _api;

    public CreateEntryPage(KorendzhApiClient api)
    {
        InitializeComponent();
        _api = api;
        DatePicker.Date = DateTime.Today;
        DatePicker.MaximumDate = DateTime.Today;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        if (!decimal.TryParse(HoursEntry.Text?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var hours)
            || hours <= 0 || hours > 24)
        {
            ShowError("Введите часы — число от 0.25 до 24.");
            return;
        }
        if (string.IsNullOrWhiteSpace(TaskEntry.Text))
        {
            ShowError("Заполните задачу.");
            return;
        }

        var carName = string.IsNullOrWhiteSpace(CarEntry.Text) ? null : CarEntry.Text.Trim();
        var plate = string.IsNullOrWhiteSpace(PlateEntry.Text) ? null : PlateEntry.Text.Trim();
        if ((carName is null) ^ (plate is null))
        {
            ShowError("Заполните оба поля автомобиля или оставьте оба пустыми.");
            return;
        }

        SaveBtn.IsEnabled = false;
        try
        {
            var ok = await _api.CreateEntryAsync(new KorendzhApiClient.CreateEntryRequest(
                WorkerId: null,
                WorkDate: DateOnly.FromDateTime(DatePicker.Date),
                Hours: hours,
                TaskName: TaskEntry.Text!.Trim(),
                CarName: carName,
                LicensePlate: plate,
                Description: string.IsNullOrWhiteSpace(DescEntry.Text) ? null : DescEntry.Text.Trim()));

            if (!ok)
            {
                ShowError("Сервер отклонил запись. Проверьте поля.");
                return;
            }

            await Shell.Current.GoToAsync("//entries");
        }
        catch (Exception ex)
        {
            ShowError("Ошибка соединения: " + ex.Message);
        }
        finally
        {
            SaveBtn.IsEnabled = true;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//entries");
    }

    private void ShowError(string text)
    {
        ErrorLabel.Text = text;
        ErrorLabel.IsVisible = true;
    }
}
