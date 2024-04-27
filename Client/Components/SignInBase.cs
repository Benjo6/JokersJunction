using Blazored.Modal;
using JokersJunction.Client.Services;
using JokersJunction.Shared.Models;
using JokersJunction.Shared.Requests;
using Microsoft.AspNetCore.Components;

namespace JokersJunction.Client.Components
{
    public class SignInBase : ComponentBase
    {
        [Inject]
        public IAuthService AccountService { get; set; }

        [CascadingParameter]
        public BlazoredModalInstance BlazoredModal { get; set; }

        public RegisterRequest User { get; set; } = new RegisterRequest();

        public bool ShowErrors { get; set; }
        public IEnumerable<string> Errors { get; set; }

        protected async Task CreateAccount()
        {
            ShowErrors = false;
            var result = await AccountService.Register(User);
            if (result.Successful)
            {
                BlazoredModal.Close();
            }
            else
            {
                Errors = result.Errors;
                ShowErrors = true;
            }
        }
    }

}