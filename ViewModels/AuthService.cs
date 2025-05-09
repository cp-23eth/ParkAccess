using Microsoft.Identity.Client;
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
        _msalApp = PublicClientApplicationBuilder.Create("315ca165-3c88-45c1-b62f-45679cb58e62")
            .WithTenantId("0bd66e42-d830-4cdc-b580-f835a405d038")
            .WithRedirectUri("http://localhost")
            .Build();
    }

    public async Task Login()
    {
        try
        {
            var result = await _msalApp.AcquireTokenInteractive(["api://315ca165-3c88-45c1-b62f-45679cb58e62/api_access"])
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync();

            token = result.AccessToken;
        }
        catch (MsalException ex)
        {
            
        }
    }
}