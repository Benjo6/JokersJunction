using Blazored.Modal.Services;
using JokersJunction.Client.Shared;
using Microsoft.AspNetCore.Components;

namespace JokersJunction.Client.Pages
{
    public class HomePageBase : ComponentBase
    {
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        protected async Task LogOut()
        {
            var resultModal = ModalService.Show<LogOut>("Log Out");
            var result = await resultModal.Result;

            if (!result.Cancelled)
            {
                StateHasChanged();
            }
        }

        protected void NavigateToPokerTables()
        {
            NavigationManager.NavigateTo("/poker-tables");
        }
        protected void NavigateToBlackjackTables()
        {
            NavigationManager.NavigateTo("/blackjack-tables");
        }

    }
}