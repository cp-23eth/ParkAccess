using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace ParkAccess
{
    public partial class DeleteParking : Window
    {
        private static readonly HttpClient client = new();

        public ObservableCollection<ParkingData> Parkings { get; } = new();

        private ParkingData _selectedParking;
        public ParkingData SelectedParking
        {
            get => _selectedParking;
            set => _selectedParking = value;
        }

        public DeleteParking()
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
                    Log.Error("API settings are not properly configured.");
                    return;
                }

                var request = new HttpRequestMessage(HttpMethod.Get, $"{Program.Settings.Api.BaseUrl}/parkings");
                request.Headers.Add("ApiKey", Program.Settings.Api.Key);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.token);

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
                else
                {
                    Log.Warning("La désérialisation des parkings a retourné null.");
                }
            }
            catch (HttpRequestException ex)
            {
                Log.Error($"Erreur lors de la récupération des parkings : {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"Erreur inattendue dans InitializeParkings : {ex}");
            }
        }

        private async void OnDelete(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (SelectedParking == null)
            {
                Log.Warning("Aucun parking sélectionné pour suppression.");
                return;
            }

            try
            {
                if (Program.Settings?.Api == null || string.IsNullOrEmpty(Program.Settings.Api.BaseUrl) || string.IsNullOrEmpty(Program.Settings.Api.Key))
                {
                    Log.Error("API settings are not properly configured.");
                    return;
                }

                var request = new HttpRequestMessage(HttpMethod.Delete, $"{Program.Settings.Api.BaseUrl}/deleteparking/{SelectedParking.Nom}");
                request.Headers.Add("ApiKey", Program.Settings.Api.Key);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.token);

                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                Log.Information($"Parking {SelectedParking.Nom} supprimé.");

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MessageDeleteParking.Text = $"Le parking {SelectedParking.Nom} a été supprimé avec succès.";
                    MessageDeleteParking.Foreground = new SolidColorBrush(Colors.Black);
                    MessageDeleteParking.IsVisible = true;
                });
            }
            catch (HttpRequestException ex)
            {
                Log.Error($"Erreur HTTP lors de la suppression du parking : {ex.Message}");
                await ShowErrorMessage($"Erreur lors de la suppression du parking {SelectedParking.Nom}");
            }
            catch (Exception ex)
            {
                Log.Error($"Erreur inattendue lors de la suppression du parking : {ex}");
                await ShowErrorMessage($"Erreur inattendue pour le parking {SelectedParking.Nom}");
            }
        }

        private async Task ShowErrorMessage(string message)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MessageDeleteParking.Text = message;
                MessageDeleteParking.Foreground = new SolidColorBrush(Colors.Red);
                MessageDeleteParking.IsVisible = true;
            });
        }
    }
}
