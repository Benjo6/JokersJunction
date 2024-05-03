using Blazored.Modal;
using JokersJunction.Client.Services;
using JokersJunction.Shared.Requests;
using Microsoft.AspNetCore.Components;

namespace JokersJunction.Client.Components
{
    public class NewTableBase : ComponentBase
    {
        [Inject] public ITableService TableService { get; set; }

        [CascadingParameter]
        public BlazoredModalInstance BlazoredModal { get; set; }

        public CreateTableRequest PokerTable { get; set; } = new CreateTableRequest();
        public bool ShowErrors { get; set; }
        public IEnumerable<string> Errors { get; set; }


        protected async Task CreateTable()
        {
            ShowErrors = false;
            var result = await TableService.Create(PokerTable);
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