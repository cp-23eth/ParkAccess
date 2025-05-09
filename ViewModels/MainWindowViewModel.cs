using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

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
            InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await Authenticate();

            RefreshData();
        }

        public void RefreshData()
        {
            InitializeParkings();
            InitializeEvents();
            InitializeHistory();
        }

        public async Task Authenticate()
        {
            var authService = new AuthService();
            await authService.Login();

            if (string.IsNullOrEmpty(AuthService.token))
            {
                Console.WriteLine("L'utilisateur n'est pas connecté.");
            }
        }

        public async Task InitializeParkings()
        {
            if (Program.Settings?.Api == null || string.IsNullOrEmpty(Program.Settings.Api.BaseUrl) || string.IsNullOrEmpty(Program.Settings.Api.Key))
            {
                Log.Error("API settings are not properly configured.");
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(AuthService.token))
                {
                    Log.Error("Token d'accès manquant.");
                    return;
                }

                var request = new HttpRequestMessage(HttpMethod.Get, $"{Program.Settings.Api.BaseUrl}/parkings");
                request.Headers.Add("ApiKey", Program.Settings.Api.Key);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.token);

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
                Log.Error($"Parking HTTP request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"An unexpected error occurred: {ex}");
            }
        }

        public async Task InitializeEvents()
        {
            if (Program.Settings?.Api == null || string.IsNullOrEmpty(Program.Settings.Api.BaseUrl) || string.IsNullOrEmpty(Program.Settings.Api.Key))
            {
                Log.Error("API settings are not configured properly.");
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(AuthService.token))
                {
                    Log.Error("Token d'accès manquant.");
                    return;
                }

                var request = new HttpRequestMessage(HttpMethod.Get, $"{Program.Settings.Api.BaseUrl}/events");
                request.Headers.Add("ApiKey", Program.Settings.Api.Key);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.token);

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
                Log.Error($"Event HTTP request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"Unhandled exception in InitializeEvents: {ex}");
            }
        }

        public async Task InitializeHistory()
        {
            if (Program.Settings?.Api == null || string.IsNullOrEmpty(Program.Settings.Api.BaseUrl) || string.IsNullOrEmpty(Program.Settings.Api.Key))
            {
                Log.Error("API settings are not properly configured.");
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(AuthService.token))
                {
                    Log.Error("Token d'accès manquant.");
                    return;
                }

                var request = new HttpRequestMessage(HttpMethod.Get, $"{Program.Settings.Api.BaseUrl}/history");
                request.Headers.Add("ApiKey", Program.Settings.Api.Key);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.token);

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
                Log.Error($"History HTTP request failed: {ex.Message}");
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

        public void DeleteHistory()
        {
            try
            {
                if (string.IsNullOrEmpty(AuthService.token))
                {
                    Log.Error("Token d'accès manquant.");
                    return;
                }

                var request = new HttpRequestMessage(HttpMethod.Delete, $"{Program.Settings.Api.BaseUrl}/deletehistory");
                request.Headers.Add("ApiKey", Program.Settings.Api.Key);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.token);

                HttpResponseMessage response = client.SendAsync(request).Result;
                response.EnsureSuccessStatusCode();

                RefreshData();
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
    }
}
