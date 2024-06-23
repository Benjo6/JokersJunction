using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.Modal.Services;
using JokersJunction.Client.Components;
using JokersJunction.Client.Services;
using JokersJunction.Shared;
using Microsoft.AspNetCore.Components;

namespace JokersJunction.Client.Shared
{
    public class DisplayPokerTableBase : ComponentBase
    {
        [Parameter]
        public EventCallback<string> OnChange { get; set; }

        [Parameter]
        public PokerTable CurrentTable { get; set; }

        [Inject] public ITableService TableService { get; set; }

        [Inject] public ILocalStorageService LocalStorageService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        protected async Task DeleteConfirm()
        {
            var parameters = new ModalParameters();
            parameters.Add(nameof(CurrentTable), CurrentTable);

            var resultModal = ModalService.Show<TableDeletionConfirm>("Confirm", parameters);
            var result = await resultModal.Result;
            if (!result.Cancelled)
            {
                await OnChange.InvokeAsync("List was changed");
            }
        }

        protected async Task JoinTable()
        {
            await LocalStorageService.SetItemAsync("currentTable", CurrentTable.Id);
            NavigationManager.NavigateTo("/Poker-Game");
        }
    }
}