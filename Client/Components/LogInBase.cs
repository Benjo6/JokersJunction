using Blazored.Modal;
using Blazored.Modal.Services;
using JokersJunction.Client.Services;
using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace JokersJunction.Client.Components
{
    public class LogInBase : ComponentBase
    {
        public LoginModel User { get; set; } = new();
        [Inject]
        public IAuthService AccountService { get; set; }

        [CascadingParameter]
        public BlazoredModalInstance BlazoredModal { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        protected async Task LogIn()
        {
            var result = await AccountService.Login(User);

            if (result.Successful)
            {

                BlazoredModal.Close(ModalResult.Ok(true));
            }
            else
            {
                ErrorMessage = result.Error;
            }
        }
    }
}