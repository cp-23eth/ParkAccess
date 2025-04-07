using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using Avalonia.Controls;
using System.Text.Json;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System.Text;

namespace ParkAccess.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private static readonly HttpClient client = new HttpClient();
        string ip = "";
        public bool status { get; set; }

        

        private async Task SendShellyCommand(string ip,string state)
        {
            try
            {
                string url = $"http://{ip}/relay/0?turn={state}";
                HttpResponseMessage response = await client.GetAsync(url);
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

        private async Task chooseCommand(string ip)
        {
            try
            {
                
                string url = $"http://{ip}/relay/0";
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    bool isOn = doc.RootElement.GetProperty("ison").GetBoolean();

                    status = isOn;
                }
                else
                {
                    Console.WriteLine("Erreur de connexion");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception : {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task ActionOne()
        {
            ip = "157.26.121.184";

            await chooseCommand(ip);

            if (status == true)
            {
                await SendShellyCommand(ip, "off");
            }
            else
            {
                await SendShellyCommand(ip, "on");
            }
        }
    }
}
