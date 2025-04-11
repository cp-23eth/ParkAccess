using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Serilog;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;

namespace ParkAccess;

public partial class DeleteParking : Window
{
    private static readonly HttpClient client = new HttpClient();

    public ObservableCollection<ParkingData> Parkings { get; } = new();

    public ParkingData SelectedParking { get; set; }
    public DeleteParking()
    {
        InitializeComponent();
        DataContext = this;
        InitializeParkings();
    }

    public async void InitializeParkings()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{Program.Settings.Api.BaseUrl}/parkings");
            request.Headers.Add("X-Api-Key", Program.Settings.Api.Key);

            HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();

            Log.Information(json);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var parkings = JsonSerializer.Deserialize<ObservableCollection<ParkingData>>(json, options);

            if (parkings != null)
            {
                Log.Information($"Nombre de parkings désérialisés : {parkings.Count}");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Parkings.Clear();
                    foreach (var parking in parkings)
                    {
                        Parkings.Add(parking);
                    }
                });
            }
        }
        catch (HttpRequestException)
        {

        }
    }

    private async void OnDelete(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SelectedParking == null)
        {
            return;
        }

        try
        {


            var request = new HttpRequestMessage(HttpMethod.Delete, $"{Program.Settings.Api.BaseUrl}/deleteparking/{SelectedParking.Nom}");
            request.Headers.Add("X-Api-Key", Program.Settings.Api.Key);

            HttpResponseMessage response = await client.SendAsync(request);
            Log.Information($"Parking supprimé");
            MessageDeleteParking.Text = $"Le parking {SelectedParking.Nom} a été supprimé avec succès.";
            MessageDeleteParking.Foreground = new SolidColorBrush(Colors.Black);
        }
        catch (HttpRequestException ex)
        {
            Log.Error($"Erreur lors de la suppression du parking : {ex.Message}");
            MessageDeleteParking.Text = $"Le parking {SelectedParking.Nom} ne peut pas être supprimé";
            MessageDeleteParking.Foreground = new SolidColorBrush(Colors.Red);
        }
        DeleteParkingInfo();

    }

    private void DeleteParkingInfo()
    {
        MessageDeleteParking.IsVisible = true;
    }
}