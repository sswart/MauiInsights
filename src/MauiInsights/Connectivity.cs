namespace MauiInsights
{
    public class EssentialsConnectivity : IConnectivity
    {
        public Task<bool> HasInternetConnection()
        {
            return Task.FromResult(Connectivity.NetworkAccess == NetworkAccess.Internet);
        }
    }
}
