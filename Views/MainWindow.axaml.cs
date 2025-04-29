using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ParkAccess.Views;

public partial class MainWindow : Window
{
    private static readonly HttpClient client = new HttpClient();
    private CancellationTokenSource? _cts;
    private DispatcherTimer? _dispatcherTimer;

    public MainWindow()
    {
        InitializeComponent();

        this.Opened += (_, _) =>
        {
            StartParkingsUpdate();
        };
    }

    private void StartParkingsUpdate()
    {
        _cts = new CancellationTokenSource();
        _dispatcherTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        _dispatcherTimer.Tick += async (_, _) =>
        {
            if (_cts.Token.IsCancellationRequested) return;

            try
            {
                await UpdateParkings();
            }
            catch (Exception ex)
            {
                Log.Error("Erreur dans UpdateParkings : " + ex.Message);
            }
        };

        _dispatcherTimer.Start();
    }

    private async Task UpdateParkings()
    {
        var parkings = await FetchParkingsAsync();

        foreach (var parking in parkings)
        {
            var isOpen = await CheckIfParkingIsOpen(parking.Ip);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var toggleButton = FindToggleButtonByParkingName(parking.Nom);

                if (toggleButton != null)
                {
                    toggleButton.IsChecked = isOpen;
                }
                else
                {
                    Log.Warning($"Aucun bouton trouvé pour {parking.Nom}");
                }
            });
        }
    }

    private async Task<List<ParkingData>> FetchParkingsAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Program.Settings.Api.BaseUrl}/parkings");
        request.Headers.Add("X-Api-Key", Program.Settings.Api.Key);

        HttpResponseMessage response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var parkings = System.Text.Json.JsonSerializer.Deserialize<List<ParkingData>>(json, options);

        return parkings ?? new List<ParkingData>();
    }

    private async Task<bool> CheckIfParkingIsOpen(string parkingIp)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"http://{parkingIp}/relay/0");
            request.Headers.Add("X-Api-Key", Program.Settings.Api.Key);

            HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            var status = System.Text.Json.JsonSerializer.Deserialize<ParkingStatus>(json);

            return status?.IsOpen ?? false;
        }catch
        {
            Log.Error("Erreur dans CheckIfParkingIsOpen");
            return false;
        }
    }

    private ToggleButton? FindToggleButtonByParkingName(string parkingName)
    {
        foreach (var tb in ParkingContent.GetVisualDescendants().OfType<ToggleButton>())
        {
            var parameter = tb.CommandParameter as string;

            if (parameter == parkingName)
                return tb;
        }

        return null;
    }

    private void ToggleButton_Loaded(object sender, RoutedEventArgs e)
    {
        var toggleButton = sender as ToggleButton;
        if (toggleButton != null)
        {
            var parkingNom = toggleButton.CommandParameter as string;
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        Log.CloseAndFlush();
        base.OnClosing(e);
    }

    private async void OnParkButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var popup = new PopupParking();
        await popup.ShowDialog(this);
    }

    private async void OnPlanButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var popup = new PopupEvent();
        await popup.ShowDialog(this);
    }

    private async void OnDelParkButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var popup = new DeleteParking();
        await popup.ShowDialog(this);
    }

    private async void OnDelPlanButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var popup = new DeleteEvent();
        await popup.ShowDialog(this);
    }

    public class ParkingStatus
    {
        [JsonPropertyName("ison")]
        public bool IsOpen { get; set; }
    }
}
