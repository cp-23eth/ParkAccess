using Avalonia.Controls;
using Avalonia.Media;
using ParkAccess.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
                using (HttpClient client = new HttpClient())
                {
                    string json = JsonSerializer.Serialize(new
                    {
                        nom = newParking.Nom,
                        ceff = newParking.Ceff,
                        mail = newParking.Mail,
                        ip = newParking.Ip
                    });

                    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    client.DefaultRequestHeaders.Add("X-Api-Key", Program.Settings.Api.Key);

                    HttpResponseMessage response = await client.PostAsync($"{Program.Settings.Api.BaseUrl}/addparking", content);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Erreur lors de l'ajout du parking : {ex.Message}");
                MessageNewParking.Text = $"Formlaire incorrect ou incomplet";
                MessageNewParking.Foreground = new SolidColorBrush(Colors.Red);
                CreateParkingInfo();
            }
        }

        private async Task<bool> ParkingExistsAsync(ParkingData newParking)
        {
            // TODO : il ne détecte pas les doubles parkings (avec le meme email)
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{Program.Settings.Api.BaseUrl}/parkings");
                    request.Headers.Add("X-Api-Key", Program.Settings.Api.Key);

                    HttpResponseMessage response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    string json = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var existingParkings = JsonSerializer.Deserialize<List<ParkingData>>(json, options);

                    return existingParkings?.Any(p =>
                        p.Nom == newParking.Nom &&
                        p.Ip == newParking.Ip &&
                        p.Mail == newParking.Mail
                    ) ?? false;


                }
                catch (Exception ex)
                {
                    Log.Error($"Erreur lors de la vérification de l'existence du parking : {ex.Message}");
                    return false;
                }
            }
        }

        private async void OnSave(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (nameParking.Text == null || emailParking.Text == null || ipParking.Text == null || ceffComboBox.SelectedItem == null)
            {
                return;
            }

            var selectedItem = ceffComboBox.SelectedItem as ComboBoxItem;
            string ceff = selectedItem?.Content.ToString();

            ParkingData newParking = new ParkingData(
                nameParking.Text,
                emailParking.Text,
                ceff,
                ipParking.Text
            );

            IPAddress ip;

            if (!IPAddress.TryParse(ipParking.Text, out ip))
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
