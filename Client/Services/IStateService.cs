namespace JokersJunction.Client.Services
{
    public interface IStateService
    {
        event Action RefreshRequested;
        void CallRequestRefresh();
    }
}
