using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
                Log.Information($"Nombre de parkings d�s�rialis�s : {parkings.Count}");
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

        var request = new HttpRequestMessage(HttpMethod.Delete, $"{Program.Settings.Api.BaseUrl}/deleteparking/{SelectedParking.Nom}");
        request.Headers.Add("X-Api-Key", Program.Settings.Api.Key);

        HttpResponseMessage response = await client.SendAsync(request);
        Log.Information($"Parking supprim�");
        DeleteParkingInfo();

    }

    private void DeleteParkingInfo()
    {
        MessageNewEvent.IsVisible = true;
    }
}