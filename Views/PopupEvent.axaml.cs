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
            var eventData = new EventData
            {
                Name = nameText.Text,
                ParkingMail = SelectedParking?.Mail ?? "Parking inconnu",
                ParkingIp = SelectedParking?.Ip ?? "IP inconnue",
                Start = new DateTimeOffset(dateChoice.SelectedDate.Value.Date + beginHourChoice.SelectedTime.Value),
                End = new DateTimeOffset(dateChoice.SelectedDate.Value.Date + finalHourChoice.SelectedTime.Value)
            };

            var jsonPayload = JsonConvert.SerializeObject(eventData, Formatting.Indented);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", Program.Settings.Api.Key);

                var response = await client.PostAsync($"{Program.Settings.Api.BaseUrl}/addevent", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Log.Information($"Événement créé avec succès : {responseBody}");
                    MessageNewEvent.Text = "Événement ajouté avec succès.";
                    MessageNewEvent.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    Log.Error($"Erreur API : {response.StatusCode} - {responseBody}");
                    MessageNewEvent.Text = $"Erreur API : {response.StatusCode}";
                    MessageNewEvent.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Exception lors de l'ajout de l'événement : {ex}");
                MessageNewEvent.Text = "Erreur lors de la communication avec l'API.";
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