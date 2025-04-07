using Avalonia.Controls;
using Microsoft.Graph.Models;
using System.Net.Http;
using System.Text.Json;
using System;
using Microsoft.Identity.Client;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Interactivity;

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

    private void ceffSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem selectedItem)
        {
            string? selectedText = selectedItem.Content?.ToString();

            if (ceffIndustrieContent != null)
                ceffIndustrieContent.IsVisible = selectedText == "CEFF Industrie";
            if (ceffSanteSocialContent != null)
                ceffSanteSocialContent.IsVisible = selectedText == "CEFF Santé-Social";
            if (ceffCommerceContent != null)
                ceffCommerceContent.IsVisible = selectedText == "CEFF Commerce";
            if (ceffArtisanalContent != null)
                ceffArtisanalContent.IsVisible = selectedText == "CEFF Artisanal";
        }
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

    async void CreateActivity(object sender, RoutedEventArgs e)
    {
        string tenantId = "0bd66e42-d830-4cdc-b580-f835a405d038";
        string clientId = "315ca165-3c88-45c1-b62f-45679cb58e62";
        string clientSecret = "6Ge8Q~v-yOZbjJIRNAUbqNLzS3uGcRaQ4X8N_dn-";
        string resourceEmail = "parking.test@iceff.ch";


        string accessToken = await GetAccessToken(tenantId, clientId, clientSecret);
        if (!string.IsNullOrEmpty(accessToken))
        {
            await CreateEvent(resourceEmail, accessToken);
        }
    }

    static async Task<string> GetAccessToken(string tenantId, string clientId, string clientSecret)
    {
        var app = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
            .Build();

        string[] scopes = { "https://graph.microsoft.com/.default" };
        var authResult = await app.AcquireTokenForClient(scopes).ExecuteAsync();
        return authResult.AccessToken;
    }

    async Task CreateEvent(string resourceEmail, string accessToken)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var eventPayload = new
        {
            subject = nameText.Text,
            start = new { dateTime = dateChoice + "T" + beginHourChoice, timeZone = "Europe/Paris" },
            end = new { dateTime = dateChoice + "T" + finalHourChoice, timeZone = "Europe/Paris" },
            location = new { displayName = parkingChoice },
            //attendees = new[]
            //{
            //    new
            //    {
            //        emailAddress = new { address = "cp-23eth@iceff.ch", name = "Ethan Hofstetter" },
            //        type = "required"
            //    }
            //}
        };

        var jsonPayload = JsonConvert.SerializeObject(eventPayload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var url = $"https://graph.microsoft.com/v1.0/users/{resourceEmail}/events";
        var response = await httpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Événement créé avec succès !");
        }
        else
        {
            string errorResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Erreur: {response.StatusCode} - {errorResponse}");
        }
    }
}
