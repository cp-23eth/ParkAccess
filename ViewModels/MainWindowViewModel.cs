using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Collections.ObjectModel;
using Microsoft.Graph.Models;
using System.Collections.Generic;

namespace ParkAccess.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private static readonly HttpClient client = new HttpClient();
        string ip = "";
        public bool status { get; set; }

        public ObservableCollection<Parking> Parkings { get;}

        public MainWindowViewModel ()
        {
            var parking = new List<Parking>
            {
                new Parking("Pierre-Jolissaint", "Pierre-Jolissaint.parking@iceff.ch", "Industrie", "157.26.121.184"),
                new Parking("Pierre-Jolissaint", "Pierre-Jolissaint.parking@iceff.ch", "Industrie", "157.26.121.184"),
                new Parking("Pierre-Jolissaint", "Pierre-Jolissaint.parking@iceff.ch", "Industrie", "157.26.121.184")
            };
            Parkings = new ObservableCollection<Parking>(parking);
        }

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
