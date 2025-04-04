using Avalonia.Controls;
using Microsoft.Graph.Models;
using System.Net.Http;
using System.Text.Json;
using System;

namespace ParkAccess.Views;

public partial class MainWindow : Window
{
    string ip = "157.26.121.111";
    private static readonly HttpClient client = new HttpClient();

    public MainWindow()
    {
        InitializeComponent();
        InitializeBtn();
    }

    async void InitializeBtn()
    {
        try
        {
            string url = $"http://{ip}/relay/0";
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                bool status = doc.RootElement.GetProperty("ison").GetBoolean();

                parkingBtn1.IsChecked = status;
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
}
