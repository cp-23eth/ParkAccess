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
        string url = "http://157.26.121.168:7159/api/calendar/parkings";
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", "123456789");

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

        string url = $"http://157.26.121.168:7159/api/calendar/deleteparking/{SelectedParking.Nom}";

        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add("X-Api-Key", "123456789");

        HttpResponseMessage response = await client.SendAsync(request);
    }
}