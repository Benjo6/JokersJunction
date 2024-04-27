using Blazored.LocalStorage;
using Blazored.Modal.Services;
using JokersJunction.Client.Shared;
using Microsoft.AspNetCore.Components;

namespace JokersJunction.Client.Pages
{
    public class IndexBase : ComponentBase
    {
        [Inject] public ILocalStorageService LocalStorageService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Parameter] public EventCallback<string> OnChange { get; set; }


        public int CurrentTable { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            CurrentTable = await LocalStorageService.GetItemAsync<int>("currentTable");
        }

        protected async Task LogOut()
        {
            var resultModal = ModalService.Show<LogOut>("Log Out");
            var result = await resultModal.Result;

            if (!result.Cancelled)
            {
                await OnChange.InvokeAsync("Logout");
            }
        }
    }
}
