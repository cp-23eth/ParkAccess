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
using ParkAccess.ViewModels;
using System.Threading;
using Avalonia.Threading;

namespace ParkAccess.Views;

public partial class MainWindow : Window
{
    private static readonly HttpClient client = new HttpClient();
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();
        //InitializeBtn();

        StartPeriodicRefresh();
    }

    //async void InitializeBtn()
    //{
        //try
        //{
        //    string url = $"http://{ip}/relay/0";
        //    HttpResponseMessage response = await client.GetAsync(url);
        //    if (response.IsSuccessStatusCode)
        //    {
        //        string jsonResponse = await response.Content.ReadAsStringAsync();
        //        using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        //        bool status = doc.RootElement.GetProperty("ison").GetBoolean();

        //        parkingBtn1.IsChecked = status;
        //    }
        //    else
        //    {
        //        Console.WriteLine("Erreur de connexion");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine($"Exception : {ex.Message}");
        //}
    //}

    private async void StartPeriodicRefresh()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await timer.WaitForNextTickAsync(token))
            {
                //await Dispatcher.UIThread.InvokeAsync(InitializeBtn);
            }
        }
        catch (OperationCanceledException)
        {

        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnClosing(e);
    }

    private async void OnParkButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var popup = new PopupParking();
        await popup.ShowDialog(this);
    }

    private async void OnPlanButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var popup = new PopupEvent();
        await popup.ShowDialog(this);
    }

    private async void OnDelParkButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var popup = new DeleteParking();
        await popup.ShowDialog(this);
    }

    private async void OnDelPlanButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var popup = new DeleteEvent();
        await popup.ShowDialog(this);
    }
}
