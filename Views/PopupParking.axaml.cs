using Avalonia.Controls;
using ParkAccess.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
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
            using (HttpClient client = new HttpClient())
            {
                string url = "http://157.26.121.168:7159/api/calendar/addparking";

                string json = JsonSerializer.Serialize(new
                {
                    nom = newParking.Nom,
                    ceff = newParking.Ceff,
                    mail = newParking.Mail,
                    ip = newParking.Ip
                });

                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Add("X-Api-Key", "123456789");

                try
                {
                    HttpResponseMessage response = await client.PostAsync(url, content);
                }
                catch (Exception ex)
                {
                    
                }
            }
        }

        private async Task<bool> ParkingExistsAsync(ParkingData newParking)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = "http://157.26.121.168:7159/api/calendar/parkings";

                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("X-Api-Key", "123456789");

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

            if (!await ParkingExistsAsync(newParking))
            {
                await AddParkingAsync(newParking);
            }
            else
            {
                Log.Information("Le parking existe déjà, ajout annulé.");
            }
        }

        private void OnClose(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }
    }
}
