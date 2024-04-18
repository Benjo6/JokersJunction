using Blazored.Modal;
using Blazored.Modal.Services;
using JokersJunction.Client.Services;
using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace JokersJunction.Client.Components
{
    public class JoinTableBase : ComponentBase
    {
        [Inject] public IAuthService AccountService { get; set; }
        [CascadingParameter] public BlazoredModalInstance BlazoredModal { get; set; }
        public JoinTableModal JoinTableModal { get; set; } = new();
        public bool ShowErrors { get; set; }
        public List<string> Errors { get; set; } = new();

        protected void JoinTable()
        {
            BlazoredModal.Close(ModalResult.Ok(JoinTableModal.Amount));
        }

        protected async Task HandleSubmit()
        {
            ShowErrors = false;
            Errors = new List<string>();
            var isValid = await AccountService.GetBalance() >= JoinTableModal.Amount && JoinTableModal.Amount > 0;

            if (isValid)
            {
                JoinTable();
            }
            else
            {
                Errors.Add("Invalid amount");
                ShowErrors = true;
            }
        }
    }
}
