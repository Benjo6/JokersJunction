using Blazored.Modal.Services;
using JokersJunction.Client.Components;
using JokersJunction.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace JokersJunction.Client.Shared
{
    public class NavMenuBase : ComponentBase
    {
        [Inject] public IStateService StateService { get; set; }

        [Inject] public IModalService ModalService { get; set; }

        [Inject] public IAuthService AccountService { get; set; }

        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        public AuthenticationState AuthState { get; set; }

        [Parameter] public EventCallback<string> OnChange { get; set; }

        public int Balance { get; set; }

        protected override async Task OnInitializedAsync()
        {
            AuthState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            Balance = -1;
            if (AuthState.User.Identity.IsAuthenticated)
            {
                Balance = await AccountService.GetBalance();
            }

            StateService.RefreshRequested += RefreshRequest;
        }

        private async void RefreshRequest()
        {
            if (AuthState.User.Identity.IsAuthenticated)
            {
                Balance = await AccountService.GetBalance();
            }

            StateHasChanged();
        }

        protected async Task ShowSignIn()
        {
            var resultModal = ModalService.Show<SignIn>("Sign In");
            var result = await resultModal.Result;

            if (!result.Cancelled)
            {
                await OnChange.InvokeAsync("SignIn");
            }
        }

        protected async void ShowLogin()
        {
            var resultModal = ModalService.Show<LogIn>("Log in");
            var result = await resultModal.Result;

            if (!result.Cancelled)
            {
                Balance = await AccountService.GetBalance();
                await OnChange.InvokeAsync("Login");
            }
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
