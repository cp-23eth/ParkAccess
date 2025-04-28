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

        private async void CreateActivity(object sender, RoutedEventArgs e)
        {
            try
            {
                string tenantId = "0bd66e42-d830-4cdc-b580-f835a405d038";
                string clientId = "315ca165-3c88-45c1-b62f-45679cb58e62";
                string clientSecret = "6Ge8Q~v-yOZbjJIRNAUbqNLzS3uGcRaQ4X8N_dn-";

                string accessToken = await GetAccessToken(tenantId, clientId, clientSecret);
                if (!string.IsNullOrEmpty(accessToken))
                {
                    await CreateEvent(accessToken);
                }
                else
                {
                    await ShowErrorMessage("Échec de la récupération du token d'accès.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Erreur dans CreateActivity : {ex}");
                await ShowErrorMessage("Erreur lors de la création de l'activité.");
            }
        }

        private static async Task<string> GetAccessToken(string tenantId, string clientId, string clientSecret)
        {
            try
            {
                var app = ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                    .Build();

                string[] scopes = { "https://graph.microsoft.com/.default" };
                var authResult = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                return authResult.AccessToken;
            }
            catch (Exception ex)
            {
                Log.Error($"Erreur lors de l'obtention du token : {ex}");
                return null;
            }
        }

        private async Task CreateEvent(string accessToken)
        {
            if (dateChoice.SelectedDate is null || beginHourChoice.SelectedTime is null || finalHourChoice.SelectedTime is null || string.IsNullOrWhiteSpace(nameText.Text) || SelectedParking == null)
            {
                Log.Warning("Formulaire incomplet.");
                await ShowErrorMessage("Formulaire incomplet.");
                return;
            }

            var planning = new EventData
            {
                Name = nameText.Text,
                ParkingMail = SelectedParking.Mail ?? "Parking inconnu",
                StartDateTime = dateChoice.SelectedDate.Value.Date + beginHourChoice.SelectedTime.Value,
                EndDateTime = dateChoice.SelectedDate.Value.Date + finalHourChoice.SelectedTime.Value
            };

            var eventPayload = new
            {
                subject = planning.Name,
                start = new { dateTime = planning.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss"), timeZone = "Europe/Paris" },
                end = new { dateTime = planning.EndDateTime.ToString("yyyy-MM-ddTHH:mm:ss"), timeZone = "Europe/Paris" },
                location = new { displayName = planning.ParkingMail }
            };

            var jsonPayload = JsonConvert.SerializeObject(eventPayload, Formatting.Indented);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                if (string.IsNullOrWhiteSpace(planning.ParkingMail))
                {
                    Log.Warning("Aucun mail de parking fourni.");
                    await ShowErrorMessage("Aucun parking sélectionné.");
                    return;
                }

                var url = $"https://graph.microsoft.com/v1.0/users/{planning.ParkingMail}/events";
                var response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    Log.Information("Événement créé avec succès !");
                    await ShowSuccessMessage("Événement créé avec succès !");
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Log.Error($"Erreur lors de la création de l'événement : {response.StatusCode} - {errorResponse}");
                    await ShowErrorMessage("Erreur lors de la création de l'événement.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Erreur lors de la création de l'événement : {ex}");
                await ShowErrorMessage("Erreur lors de la création de l'événement.");
            }
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
