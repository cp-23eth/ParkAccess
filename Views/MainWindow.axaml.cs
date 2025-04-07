using Avalonia.Controls;
using Microsoft.Graph.Models;
using System.Net.Http;
using System.Text.Json;
using System;
using Microsoft.Identity.Client;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Interactivity;
using ParkAccess.ViewModels;
using System.Threading;
using Avalonia.Threading;

namespace ParkAccess.Views;

public partial class MainWindow : Window
{
    string ip = "157.26.121.184";
    private static readonly HttpClient client = new HttpClient();
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();
        InitializeBtn();

        StartPeriodicRefresh();
    }

    async void InitializeBtn()
    {
        try
        {
            string url = $"http://{ip}/relay/0";
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                bool status = doc.RootElement.GetProperty("ison").GetBoolean();

                parkingBtn1.IsChecked = status;
            }
            else
            {
                Console.WriteLine("Erreur de connexion");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception : {ex.Message}");
        }
    }

    private async void StartPeriodicRefresh()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            while (await timer.WaitForNextTickAsync(token))
            {
                await Dispatcher.UIThread.InvokeAsync(InitializeBtn);
            }
        }
        catch (OperationCanceledException)
        {

        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnClosing(e);
    }

    async void CreateActivity(object sender, RoutedEventArgs e)
    {
        string tenantId = "0bd66e42-d830-4cdc-b580-f835a405d038";
        string clientId = "315ca165-3c88-45c1-b62f-45679cb58e62";
        string clientSecret = "6Ge8Q~v-yOZbjJIRNAUbqNLzS3uGcRaQ4X8N_dn-";


        string accessToken = await GetAccessToken(tenantId, clientId, clientSecret);
        if (!string.IsNullOrEmpty(accessToken))
        {
            await CreateEvent(accessToken);
        }
    }

    static async Task<string> GetAccessToken(string tenantId, string clientId, string clientSecret)
    {
        var app = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
            .Build();

        string[] scopes = { "https://graph.microsoft.com/.default" };
        var authResult = await app.AcquireTokenForClient(scopes).ExecuteAsync();
        return authResult.AccessToken;
    }

    async Task CreateEvent(string accessToken)
    {
        if (dateChoice.SelectedDate is null || beginHourChoice.SelectedTime is null || finalHourChoice.SelectedTime is null)
        {
            Console.WriteLine("Champs de date ou d'heure manquants.");
            return;
        }

        if (nameText.Text != null)
        {
            var planning = new EventData
            {
                Name = nameText.Text,
                ParkingMail = (DataContext as MainWindowViewModel)?.SelectedParking?.Mail ?? "Parking inconnu", // parkingChoice.SelectedItem?.ToString() ?? 
                StartDateTime = dateChoice.SelectedDate.Value.Date + beginHourChoice.SelectedTime.Value,
                EndDateTime = dateChoice.SelectedDate.Value.Date + finalHourChoice.SelectedTime.Value
            };

            var eventPayload = new
            {
                subject = planning.Name,
                start = new { dateTime = planning.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss"), timeZone = "Europe/Paris" },
                end = new { dateTime = planning.EndDateTime.ToString("yyyy-MM-ddTHH:mm:ss"), timeZone = "Europe/Paris" },
                location = new { displayName = planning.ParkingMail },
            };

            var jsonPayload = JsonConvert.SerializeObject(eventPayload, Formatting.Indented);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var url = $"https://graph.microsoft.com/v1.0/users/{(DataContext as MainWindowViewModel)?.SelectedParking?.Mail}/events";
            var response = await httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Événement créé avec succès !");
            }
            else
            {
                string errorResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Erreur: {response.StatusCode} - {errorResponse}");
            }
        }
    }

    private void ceffSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem selectedItem)
        {
            string? selectedText = selectedItem.Content?.ToString();

            if (ceffIndustrieContent != null)
                ceffIndustrieContent.IsVisible = selectedText == "CEFF Industrie";
            if (ceffSanteSocialContent != null)
                ceffSanteSocialContent.IsVisible = selectedText == "CEFF Santé-Social";
            if (ceffCommerceContent != null)
                ceffCommerceContent.IsVisible = selectedText == "CEFF Commerce";
            if (ceffArtisanalContent != null)
                ceffArtisanalContent.IsVisible = selectedText == "CEFF Artisanal";
        }
    }
    private async void OnParkButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var popup = new PopupPark();
        await popup.ShowDialog(this);
    }

    private async void OnPlanButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var popup = new PopupPlani();
        await popup.ShowDialog(this);
    }
}
