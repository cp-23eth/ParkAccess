using Avalonia.Controls;
using Avalonia.Media;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ParkAccess
{
    public partial class PopupParking : Window
    {
        public PopupParking()
        {
            InitializeComponent();
        }

        public async Task AddParkingAsync(ParkingData newParking)
        {
            try
            {
                using var client = new HttpClient();
                string json = JsonSerializer.Serialize(new
                {
                    nom = newParking.Nom,
                    ceff = newParking.Ceff,
                    mail = newParking.Mail,
                    ip = newParking.Ip
                });

                StringContent content = new(json, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Add("ApiKey", Program.Settings.Api.Key);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.token);

                HttpResponseMessage response = await client.PostAsync($"{Program.Settings.Api.BaseUrl}/addparking", content);
            }
            catch (Exception ex)
            {
                Log.Error($"Erreur lors de l'ajout du parking : {ex.Message}");
                MessageNewParking.Text = $"Formlaire incomplet";
                MessageNewParking.Foreground = new SolidColorBrush(Colors.Red);
                CreateParkingInfo();
            }
        }

        private async Task<bool> ParkingExistsAsync(ParkingData newParking)
        {
            try
            {
                using var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{Program.Settings.Api.BaseUrl}/parkings");
                request.Headers.Add("ApiKey", Program.Settings.Api.Key);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.token);

                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var existingParkings = JsonSerializer.Deserialize<List<ParkingData>>(json, options);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Log.Error($"Erreur lors de la récupération des parkings : {response.StatusCode}");
                    MessageNewParking.Text = $"Erreur lors de la récupération des parkings : {response.StatusCode}";
                    MessageNewParking.Foreground = new SolidColorBrush(Colors.Red);
                    CreateParkingInfo();
                    return false;
                }

                return existingParkings?.Any(p =>
                    p.Nom == newParking.Nom &&
                    p.Ip == newParking.Ip &&
                    p.Mail == newParking.Mail
                ) ?? false;
            }
            catch (Exception ex)
            {
                Log.Error($"Erreur lors de la récupération des parkings : {ex.Message}");
                return false;
            }
        }

        private async void OnSave(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (nameParking.Text == "" || emailParking.Text == "" || ipParking.Text == "" || ceffComboBox.SelectedItem == null)
            {
                Log.Information("Formulaire incorrect ou incomplet");
                MessageNewParking.Text = $"Formulaire incorrect ou incomplet";
                MessageNewParking.Foreground = new SolidColorBrush(Colors.Red);
                CreateParkingInfo();
                return;
            }

            var selectedItem = ceffComboBox.SelectedItem as ComboBoxItem;
            string? ceff = selectedItem?.Content!.ToString();

            ParkingData newParking = new ParkingData(
                nameParking.Text!,
                emailParking.Text!,
                ceff!,
                ipParking.Text!
            );

            IPAddress ip;

            if (!IPAddress.TryParse(ipParking.Text, out ip!))
            {
                Log.Information("Adresse Ip incorrecte");
                MessageNewParking.Text = $"L'adresse IP est incorrecte";
                MessageNewParking.Foreground = new SolidColorBrush(Colors.Red);
                CreateParkingInfo();
                return;
            }

            if (!await ParkingExistsAsync(newParking))
            {
                await AddParkingAsync(newParking);
                Log.Information("Parking ajouté");
                MessageNewParking.Text = $"Le parking {newParking.Nom} a été ajouté avec succès.";
                MessageNewParking.Foreground = new SolidColorBrush(Colors.Black);
            }
            else
            {
                Log.Information("Le parking existe déjà, ajout annulé.");
                MessageNewParking.Text = $"Le parking {newParking.Nom} existe déjà.";
                MessageNewParking.Foreground = new SolidColorBrush(Colors.Red);
            }
            CreateParkingInfo();
        }

        private void OnClose(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }

        private void CreateParkingInfo()
        {
            MessageNewParking.IsVisible = true;
        }
    }
}
