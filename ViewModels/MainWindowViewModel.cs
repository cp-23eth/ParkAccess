using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace ParkAccess.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private static readonly HttpClient client = new();
        public bool Status { get; set; }

        public ObservableCollection<ParkingData> Parkings { get; } = new();
        public ObservableCollection<EventData> Events { get; } = new();
        public ObservableCollection<History> History { get; } = new();

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
            InitializeHistory();
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
                request.Headers.Add("ApiKey", Program.Settings.Api.Key);

                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var parkings = JsonSerializer.Deserialize<List<ParkingData>>(json, JsonOptions);

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
                request.Headers.Add("ApiKey", Program.Settings.Api.Key);

                HttpResponseMessage response = await client.SendAsync(request);

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();

                Log.Information(json);

                var events = JsonSerializer.Deserialize<List<EventData>>(json, JsonOptions);

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

        public async void InitializeHistory()
        {
            if (Program.Settings?.Api == null || string.IsNullOrEmpty(Program.Settings.Api.BaseUrl) || string.IsNullOrEmpty(Program.Settings.Api.Key))
            {
                Log.Error("API settings are not properly configured.");
                return;
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{Program.Settings.Api.BaseUrl}/gethistory");
                request.Headers.Add("ApiKey", Program.Settings.Api.Key);

                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var history = JsonSerializer.Deserialize<List<History>>(json, JsonOptions);

                if (history != null)
                {
                    var sortedHistory = history.OrderByDescending(h => h.Date).ToList();

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        History.Clear();
                        foreach (var h in sortedHistory)
                        {
                            History.Add(h);
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

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
