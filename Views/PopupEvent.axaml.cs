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

        private async Task ShowErrorMessage(string message)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MessageNewEvent.Text = message;
                MessageNewEvent.Foreground = new SolidColorBrush(Colors.Red);
                MessageNewEvent.IsVisible = true;
            });
        }

        private async Task ShowSuccessMessage(string message)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MessageNewEvent.Text = message;
                MessageNewEvent.Foreground = new SolidColorBrush(Colors.Black);
                MessageNewEvent.IsVisible = true;
            });
        }
    }
}
