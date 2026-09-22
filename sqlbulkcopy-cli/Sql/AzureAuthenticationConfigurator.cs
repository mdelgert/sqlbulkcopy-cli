using Microsoft.Data.SqlClient;

namespace sqlbulkcopy_cli.Sql;

internal static class AzureAuthenticationConfigurator
{
    private static int _configured;

    public static void Configure()
    {
        if (Interlocked.Exchange(ref _configured, 1) == 1)
        {
            return;
        }

        var provider = new ActiveDirectoryAuthenticationProvider(new ActiveDirectoryAuthenticationProviderOptions
        {
            DeviceCodeFlowCallback = result =>
            {
                System.Console.Error.WriteLine(result.Message);
                return Task.CompletedTask;
            }
        });

        foreach (var method in new[]
                 {
                     SqlAuthenticationMethod.ActiveDirectoryIntegrated,
                     SqlAuthenticationMethod.ActiveDirectoryInteractive,
                     SqlAuthenticationMethod.ActiveDirectoryServicePrincipal,
                     SqlAuthenticationMethod.ActiveDirectoryDeviceCodeFlow,
                     SqlAuthenticationMethod.ActiveDirectoryManagedIdentity,
                     SqlAuthenticationMethod.ActiveDirectoryMSI,
                     SqlAuthenticationMethod.ActiveDirectoryDefault
                 })
        {
            if (provider.IsSupported(method))
            {
                SqlAuthenticationProvider.SetProvider(method, provider);
            }
        }
    }
}
