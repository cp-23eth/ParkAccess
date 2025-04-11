using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Serilog;
using System.Collections.ObjectModel;
using static Microsoft.Graph.Constants;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using ParkAccess.ViewModels;
using System.Threading.Tasks;
using System;
using System.Text;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ParkAccess;

public partial class PopupEvent : Window
{
    private static readonly HttpClient client = new HttpClient();

    public ObservableCollection<ParkingData> Parkings { get; } = new();

    public ParkingData SelectedParking { get; set; }

    public PopupEvent()
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

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var parkings = System.Text.Json.JsonSerializer.Deserialize<ObservableCollection<ParkingData>>(json, options);

            if (parkings != null)
            {
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
            Log.Information("Erreur lors de la récupération des parkings.");
        }

    }

    private async void CreateActivity(object sender, Avalonia.Interactivity.RoutedEventArgs e)
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
        if (dateChoice.SelectedDate is null || beginHourChoice.SelectedTime is null || finalHourChoice.SelectedTime is null || nameText.Text == null || SelectedParking == null)
        {
            Log.Information("Formulair incomplet");
            MessageNewEvent.Text = "Formulaire incomplet";
            MessageNewEvent.Foreground = new SolidColorBrush(Colors.Red);
            CreateActivityInfo();
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

            var mail = SelectedParking?.Mail;

            if (string.IsNullOrWhiteSpace(mail))
            {
                Log.Information("Aucun parking sélectionné ou mail invalide.");
                return;
            }

            var url = $"https://graph.microsoft.com/v1.0/users/{mail}/events";
            //var url = $"https://graph.microsoft.com/v1.0/users/{(DataContext as MainWindowViewModel)?.SelectedParking?.Mail}/events";
            var response = await httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Événement créé avec succès !");
                MessageNewEvent.Text = "Événement créé avec succès !";
                MessageNewEvent.Foreground = new SolidColorBrush(Colors.Black);
            }
            else
            {
                string errorResponse = await response.Content.ReadAsStringAsync();
                Log.Information($"Erreur: {response.StatusCode} - {errorResponse}");
                MessageNewEvent.Text = "Erreur lors de la création de l'événement";
                MessageNewEvent.Foreground = new SolidColorBrush(Colors.Red);
            }
            CreateActivityInfo();
        }
    }

    private void CreateActivityInfo()
    {
        MessageNewEvent.IsVisible = true;
    }
}