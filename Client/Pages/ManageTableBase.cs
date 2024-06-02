using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using JokersJunction.Client.Components;

namespace JokersJunction.Client.Pages
{
    public class ManageTableBase : ComponentBase
    {
        [Inject] public IModalService ModalService { get; set; }

        protected PokerTableList PokerChildComponent;
        protected BlackjackTableList BlackjackChildComponent;


        protected async Task ShowNewTable()
        {
            var resultModal = ModalService.Show<NewTable>("Create New Table");
            var result = await resultModal.Result;

            if (!result.Cancelled)
            {
                PokerChildComponent.Refresh();
            }

        }
    }


}