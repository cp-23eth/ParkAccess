using Microsoft.Identity.Client;
using ParkAccess;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class AuthService
{
    public static string? token { get; set; } = null;
    private readonly IPublicClientApplication _msalApp;

    public AuthService()
    {
        _msalApp = PublicClientApplicationBuilder.Create(Program.Settings.Api.ClientId)
            .WithTenantId(Program.Settings.Api.TenantId)
            .WithRedirectUri(Program.Settings.Api.RedirectUrl)
            .Build();
    }

    public async Task Login()
    {
        try
        {
            var result = await _msalApp.AcquireTokenInteractive([Program.Settings.Api.Audience])
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync();

            token = result.AccessToken;
        }
        catch (MsalException ex)
        {
            
        }
    }
}