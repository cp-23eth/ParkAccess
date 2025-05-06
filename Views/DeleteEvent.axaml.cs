using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace ParkAccess
{
    public partial class DeleteEvent : Window
    {
        private static readonly HttpClient client = new();

        public ObservableCollection<EventData> Events { get; } = new();

        private EventData _selectedEvent;
        public EventData SelectedEvent
        {
            get => _selectedEvent;
            set => _selectedEvent = value;
        }

        public DeleteEvent()
        {
            InitializeComponent();
            DataContext = this;
            _ = InitializeEvents();
        }

        private async Task InitializeEvents()
        {
            try
            {
                if (Program.Settings?.Api == null || string.IsNullOrEmpty(Program.Settings.Api.BaseUrl) || string.IsNullOrEmpty(Program.Settings.Api.Key))
                {
                    Log.Error("API settings are not properly configured.");
                    return;
                }

                var request = new HttpRequestMessage(HttpMethod.Get, $"{Program.Settings.Api.BaseUrl}/events");
                request.Headers.Add("ApiKey", Program.Settings.Api.Key);
                var _token = SecureTokenStore.GetToken();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

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
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Events.Clear();
                        foreach (var e in events)
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

        private async void OnDelete(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (SelectedEvent == null)
            {
                Log.Warning("No event selected for deletion.");
                return;
            }

            try
            {
                if (Program.Settings?.Api == null || string.IsNullOrEmpty(Program.Settings.Api.BaseUrl) || string.IsNullOrEmpty(Program.Settings.Api.Key))
                {
                    Log.Error("API settings are not properly configured.");
                    return;
                }

                var request = new HttpRequestMessage(HttpMethod.Delete, $"{Program.Settings.Api.BaseUrl}/deleteevent/{SelectedEvent.Name}");
                request.Headers.Add("ApiKey", Program.Settings.Api.Key);
                var _token = SecureTokenStore.GetToken();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                Log.Information("Event successfully deleted.");

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MessageDeleteEvent.Text = "Événement supprimé avec succès";
                    MessageDeleteEvent.Foreground = new SolidColorBrush(Colors.Black);
                    MessageDeleteEvent.IsVisible = true;
                });
            }
            catch (HttpRequestException ex)
            {
                Log.Error($"HTTP request failed during event deletion: {ex.Message}");
                await ShowErrorMessage();
            }
            catch (Exception ex)
            {
                Log.Error($"Unhandled exception during event deletion: {ex}");
                await ShowErrorMessage();
            }
        }

        private async Task ShowErrorMessage()
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MessageDeleteEvent.Text = "Erreur lors de la suppression de l'événement";
                MessageDeleteEvent.Foreground = new SolidColorBrush(Colors.Red);
                MessageDeleteEvent.IsVisible = true;
            });
        }
    }
}
