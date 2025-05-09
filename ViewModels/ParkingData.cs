using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
                await SendShellyCommand(Ip, Nom, "off");
            else
                await SendShellyCommand(Ip, Nom, "on");
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
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task SendShellyCommand(string ip, string nom, string state)
        {
            try
            {
                var url = $"http://{ip}/relay/0?turn={state}";
                var response = await client.GetAsync(url);

                using var client2 = new HttpClient();

                string rawMessage = $"Le parking \"{nom}\" a été mis en \"{state}\" manuellement";

                string jsonPayload = JsonSerializer.Serialize(rawMessage);

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client2.DefaultRequestHeaders.Add("ApiKey", Program.Settings.Api.Key);
                client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.token);
                HttpResponseMessage response2 = await client2.PostAsync($"{Program.Settings.Api.BaseUrl}/addhistory", content);

            }
            catch (Exception ex)
            {
                
            }
        }

        public override string ToString()
        {
            return $"Nom: {Nom}, Mail: {Mail}, Ceff: {Ceff}, Ip: {Ip}";
        }
    }
}
