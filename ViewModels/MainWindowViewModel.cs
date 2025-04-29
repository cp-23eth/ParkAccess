using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Collections.ObjectModel;
using Microsoft.Graph.Models;
using Serilog;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;
using System.Collections.Generic;

namespace ParkAccess.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
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

        public MainWindowViewModel()
        {
            RefreshData();
        }

        public void RefreshData()
        {
            InitializeParkings();
            InitializeEvents();
        }

        public async void InitializeParkings()
        {
            if (Program.Settings?.Api == null || string.IsNullOrEmpty(Program.Settings.Api.BaseUrl) || string.IsNullOrEmpty(Program.Settings.Api.Key))
            {
                Log.Error("API settings are not properly configured.");
                return;
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{Program.Settings.Api.BaseUrl}/parkings");
                request.Headers.Add("X-Api-Key", Program.Settings.Api.Key);

                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var parkings = JsonSerializer.Deserialize<List<ParkingData>>(json, options);

                if (parkings != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Parkings.Clear();
                        foreach (var p in parkings)
                        {
                            Parkings.Add(p);
                        }
                    });
                }
                else
                {
                    Log.Warning("No parkings found in the API response.");
                }
            }
            catch (HttpRequestException ex)
            {
                Log.Error($"HTTP request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"An unexpected error occurred: {ex}");
            }
        }

        public async void InitializeEvents()
        {
            if (Program.Settings?.Api == null || string.IsNullOrEmpty(Program.Settings.Api.BaseUrl) || string.IsNullOrEmpty(Program.Settings.Api.Key))
            {
                Log.Error("API settings are not configured properly.");
                return;
            }

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

                var events = JsonSerializer.Deserialize<List<EventData>>(json, options);

                if (events != null)
                {
                    var sortedEvents = events.OrderBy(e => e.Start).ToList();

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Events.Clear();
                        foreach (var e in sortedEvents)
                        {
                            Events.Add(e);
                        }
                    });
                }
                else
                {
                    Log.Warning("No events found in the API response.");
                }
            }
            catch (HttpRequestException ex)
            {
                Log.Error($"HTTP request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"Unhandled exception in InitializeEvents: {ex}");
            }
        }
    }
}
