using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ParkAccess.ViewModels;
using Serilog;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;

namespace ParkAccess;

public partial class DeleteEvent : Window
{
    private static readonly HttpClient client = new HttpClient();

    public ObservableCollection<EventData> Events { get; } = new();

    public EventData SelectedEvent { get; set; }
    public DeleteEvent()
    {
        InitializeComponent();
        DataContext = this;
        InitializeEvents();
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
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Events.Clear();
                    foreach (var e in events)
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
    private async void OnDelete(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SelectedEvent == null)
        {
            return;
        }

        var request = new HttpRequestMessage(HttpMethod.Delete, $"{Program.Settings.Api.BaseUrl}/deleteevent/{SelectedEvent.Name}");
        request.Headers.Add("X-Api-Key", Program.Settings.Api.Key);

        HttpResponseMessage response = await client.SendAsync(request);
    }

}