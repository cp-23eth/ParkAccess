using CommunityToolkit.Mvvm.Input;
using Microsoft.Graph.Models;
using System;
using System.Net.Http; // Ajout pour HttpClient
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ParkAccess
{
    public class ParkingData
    {
        [JsonPropertyName("nom")]
        public string Nom { get; set; }

        [JsonPropertyName("mail")]
        public string Mail { get; set; }

        [JsonPropertyName("ceff")]
        public string Ceff { get; set; }

        [JsonPropertyName("ip")]
        public string Ip { get; set; }

        public ICommand ToggleParkingCommand { get; }

        private readonly HttpClient client; // Déclaration du HttpClient

        // Constructeur
        public ParkingData(string nom, string mail, string ceff, string ip)
        {
            Nom = nom;
            Mail = mail;
            Ceff = ceff;
            Ip = ip;

            client = new HttpClient(); // Initialisation du client HTTP
            ToggleParkingCommand = new RelayCommand(async () => await ToggleParkingFonction());
        }

        // Méthode pour basculer l'état du parking
        private async Task ToggleParkingFonction()
        {
            bool status = await ChooseCommand(Ip);  // Doit attendre le résultat

            if (status)
                await SendShellyCommand(Ip, "off");
            else
                await SendShellyCommand(Ip, "on");
        }

        // Commande publique (RelayCommand) pour lier avec la vue
        private async Task ToggleParking(ParkingData parking)
        {
            if (parking == null)
                return;

            // Ajoutez votre logique ici si nécessaire
        }

        // Vérifie l'état actuel du relais sur le périphérique Shelly
        private async Task<bool> ChooseCommand(string ip)
        {
            try
            {
                var url = $"http://{ip}/relay/0";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    return doc.RootElement.GetProperty("ison").GetBoolean();  // Retourne le statut
                }
                else
                {
                    Console.WriteLine("Erreur de connexion");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception : {ex.Message}");
                return false;
            }
        }

        // Envoie la commande à Shelly pour basculer l'état du relais
        private async Task SendShellyCommand(string ip, string state)
        {
            try
            {
                var url = $"http://{ip}/relay/0?turn={state}";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Commande {state} envoyée avec succès !");
                }
                else
                {
                    Console.WriteLine($"Erreur : {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception : {ex.Message}");
            }
        }

        public override string ToString()
        {
            return $"Nom: {Nom}, Mail: {Mail}, Ceff: {Ceff}, Ip: {Ip}";
        }
    }
}
