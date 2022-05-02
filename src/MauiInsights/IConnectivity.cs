namespace MauiInsights
{
    public interface IConnectivity
    {
        Task<bool> HasInternetConnection();
    }
}
