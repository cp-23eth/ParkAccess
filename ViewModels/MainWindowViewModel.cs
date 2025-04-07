using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Collections.ObjectModel;
using Microsoft.Graph.Models;
using System.Collections.Generic;
using static Microsoft.Graph.Constants;
using Serilog;
using Avalonia.Threading;

namespace ParkAccess.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private static readonly HttpClient client = new HttpClient();
        string ip = "";
        string url = "http://157.26.121.168:7159/api/calendar/parkings";
        public bool status { get; set; }

        public ObservableCollection<Parking> Parkings { get; } = new ObservableCollection<Parking>();

        public MainWindowViewModel ()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("log.txt")
                .CreateLogger();

            InitializeParkings();
        }

        public async void InitializeParkings()
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                Log.Information(responseBody);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var parkings = JsonSerializer.Deserialize<ObservableCollection<Parking>>(responseBody, options);

                Log.Information($"Count parkings: {parkings?.Count}");
                if (parkings != null)
                {
                    Log.Information($"Nombre de parkings désérialisés : {parkings.Count}");
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Parkings.Clear();
                        foreach (var parking in parkings)
                        {
                            Log.Information($"Ajout du parking : Nom: {parking.Nom}, Mail: {parking.Mail}, Ceff: {parking.Ceff}, Ip: {parking.Ip}");
                            Parkings.Add(parking);
                        }
                    });
                }
            }
            catch (HttpRequestException)
            {
                
            }
            
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
