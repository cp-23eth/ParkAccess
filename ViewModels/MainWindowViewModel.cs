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
using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;

namespace ParkAccess.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private static readonly HttpClient client = new HttpClient();
        public bool status { get; set; }

        public ObservableCollection<ParkingData> Parkings { get; } = new();
        public ObservableCollection<EventData> Events { get; } = new();

        private ParkingData _selectedParking;
        public ParkingData SelectedParking
        {
            get => _selectedParking;
            set => SetProperty(ref _selectedParking, value);
        }

        public MainWindowViewModel ()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("log.txt")
                .CreateLogger();

            RefreshData();
        }

        public void RefreshData()
        {
            InitializeParkings();
            InitializeEvents();
        }


        public async void InitializeParkings()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{Program.Settings.Api.BaseUrl}/parkings");
                request.Headers.Add("X-Api-Key", Program.Settings.Api.Key);

                HttpResponseMessage response = await client.SendAsync(request);

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();

                //log.information(json);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var parkings = JsonSerializer.Deserialize<ObservableCollection<ParkingData>>(json, options);

                //Log.Information($"Count parkings: {parkings?.Count}");
                if (parkings != null)
                {
                    //Log.Information($"Nombre de parkings désérialisés : {parkings.Count}");
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Parkings.Clear();
                        foreach (var p in parkings)
                        {
                            //Log.Information($"Ajout du parking : Nom: {parking.Nom}, Mail: {parking.Mail}, Ceff: {parking.Ceff}, Ip: {parking.Ip}");
                            Parkings.Add(p);
                        }
                    });
                }
            }
            catch (HttpRequestException)
            {
                
            }
        }

        public async void InitializeEvents()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{Program.Settings.Api.BaseUrl}/events");
                request.Headers.Add("X-Api-Key", Program.Settings.Api.Key);

                HttpResponseMessage response = await client.SendAsync(request);

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();

                Log.Information(json);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var events = JsonSerializer.Deserialize<ObservableCollection<EventData>>(json, options);

                if (events != null)
                {
                    var sortedEvents = events.OrderBy(e => e.StartDateTime).ToList();

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Events.Clear();
                        foreach (var e in sortedEvents)
                        {
                            Events.Add(e);
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
            string ip = "157.26.121.184";

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
