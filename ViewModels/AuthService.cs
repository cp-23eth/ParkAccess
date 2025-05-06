using Microsoft.Identity.Client;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class AuthService
{
    private readonly IPublicClientApplication _msalApp;
    private readonly string[] _scopes = new[] { "api://1ff69fc9-74de-4fba-9c13-34bf73df95a4/access_as_user" };

    public AuthService()
    {
        _msalApp = PublicClientApplicationBuilder.Create("315ca165-3c88-45c1-b62f-45679cb58e62")
            .WithTenantId("0bd66e42-d830-4cdc-b580-f835a405d038")
            .WithRedirectUri("http://localhost")
            .Build();
    }

    public async Task<string?> LoginAndGetTokenAsync()
    {
        var existingToken = SecureTokenStore.GetToken();
        if (!string.IsNullOrWhiteSpace(existingToken))
        {
            Console.WriteLine("Token trouvé localement.");
            return existingToken;
        }

        try
        {
            var result = await _msalApp.AcquireTokenInteractive(_scopes)
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync();

            SecureTokenStore.SetToken(result.AccessToken);

            return result.AccessToken;
        }
        catch (MsalException ex)
        {
            Console.WriteLine($"Login failed: {ex.Message}");
            return null;
        }
    }
}

public class SecureTokenStore
{
    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ParkAccess",
        "token.dat");

    public static void SetToken(string token)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory!);

        byte[] encrypted = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(token),
            null,
            DataProtectionScope.CurrentUser);

        File.WriteAllBytes(FilePath, encrypted);
    }

    public static string? GetToken()
    {
        if (!File.Exists(FilePath))
            return null;

        byte[] encrypted = File.ReadAllBytes(FilePath);
        byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);

        return Encoding.UTF8.GetString(decrypted);
    }
}