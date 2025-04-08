using Avalonia.Controls;
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

        public async Task AddParkingAsync(Parking newParking)
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
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    Log.Information("Parking ajouté avec succès !");
                }
                else
                {
                    Log.Information($"Erreur : {response.StatusCode}");
                }
            }
        }

        private async Task<bool> ParkingExistsAsync(Parking newParking)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = "http://157.26.121.168:7159/api/calendar/parkings";

                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string json = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var existingParkings = JsonSerializer.Deserialize<List<Parking>>(json, options);

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
            Log.Information("OnSave called");
            if (nameParking.Text == null || emailParking.Text == null || ipParking.Text == null || ceffComboBox.SelectedItem == null)
            {
                return;
            }

            var selectedItem = ceffComboBox.SelectedItem as ComboBoxItem;
            string ceff = selectedItem?.Content.ToString();

            Parking newParking = new Parking(
                nameParking.Text,
                emailParking.Text,
                ceff,
                ipParking.Text
            );

            Log.Information("newParking " + "Nom : " + newParking.Nom.ToString() + " Mail : " + newParking.Mail.ToString() + " Ceff : " + newParking.Ceff.ToString() + " Ip : " + newParking.Ip.ToString());

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
