using CommunityToolkit.Mvvm.Input;
using System;
using System.Net.Http;
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

        private readonly HttpClient client;

        public ParkingData(string nom, string mail, string ceff, string ip)
        {
            Nom = nom;
            Mail = mail;
            Ceff = ceff;
            Ip = ip;

            client = new HttpClient();
            ToggleParkingCommand = new RelayCommand(async () => await ToggleParkingFonction());
        }

        private async Task ToggleParkingFonction()
        {
            bool status = await ChooseCommand(Ip);

            if (status)
                await SendShellyCommand(Ip, "off");
            else
                await SendShellyCommand(Ip, "on");
        }

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
                    return doc.RootElement.GetProperty("ison").GetBoolean();
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
