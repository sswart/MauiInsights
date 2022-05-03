namespace MauiInsights.App
{
    internal class Connectivity : MauiInsights.IConnectivity
    {
        public Task<bool> HasInternetConnection()
        {
            return Task.FromResult(Microsoft.Maui.Networking.Connectivity.NetworkAccess == NetworkAccess.Internet);
        }
    }
}
