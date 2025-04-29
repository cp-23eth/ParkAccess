using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using ParkAccess.ViewModels;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ParkAccess
{
    public partial class PopupEvent : Window
    {
        private static readonly HttpClient client = new();

        public ObservableCollection<ParkingData> Parkings { get; } = new();

        private ParkingData _selectedParking;
        public ParkingData SelectedParking
        {
            get => _selectedParking;
            set => _selectedParking = value;
        }

        public PopupEvent()
        {
            InitializeComponent();
            DataContext = this;
            _ = InitializeParkings();
        }

        private async Task InitializeParkings()
        {
            try
            {
                if (Program.Settings?.Api == null || string.IsNullOrEmpty(Program.Settings.Api.BaseUrl) || string.IsNullOrEmpty(Program.Settings.Api.Key))
                {
                    Log.Error("API settings are not correctement configurés.");
                    return;
                }

                var request = new HttpRequestMessage(HttpMethod.Get, $"{Program.Settings.Api.BaseUrl}/parkings");
                request.Headers.Add("ApiKey", Program.Settings.Api.Key);

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
                else
                {
                    Log.Warning("La désérialisation des parkings a retourné null.");
                }
            }
            catch (HttpRequestException ex)
            {
                Log.Error($"Erreur HTTP lors de la récupération des parkings : {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"Erreur inattendue dans InitializeParkings : {ex}");
            }
        }

        private async void CreateActivity(object sender, RoutedEventArgs e)
        {
            if (dateChoice.SelectedDate is null || beginHourChoice.SelectedTime is null || finalHourChoice.SelectedTime is null || nameText.Text == null || SelectedParking == null)
            {
                Log.Information("Formulaire incomplet");
                MessageNewEvent.Text = "Formulaire incomplet";
                MessageNewEvent.Foreground = new SolidColorBrush(Colors.Red);
                CreateActivityInfo();
                return;
            }

            var eventData = new EventData
            {
                Name = nameText.Text,
                ParkingMail = SelectedParking?.Mail ?? "Parking inconnu",
                Start = new DateTimeOffset(dateChoice.SelectedDate.Value.Date + beginHourChoice.SelectedTime.Value),
                End = new DateTimeOffset(dateChoice.SelectedDate.Value.Date + finalHourChoice.SelectedTime.Value)
            };

            var jsonPayload = JsonConvert.SerializeObject(eventData, Formatting.Indented);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("ApiKey", Program.Settings.Api.Key);

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
        private void CreateActivityInfo()
        {
            MessageNewEvent.IsVisible = true;
        }
    }
}
