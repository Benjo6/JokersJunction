using Blazored.Modal;
using JokersJunction.Client.Services;
using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace JokersJunction.Client.Components
{
    public class NewTableBase : ComponentBase
    {
        [Inject] public ITableService TableService { get; set; }

        [CascadingParameter]
        public BlazoredModalInstance BlazoredModal { get; set; }

        public CreateTableModel PokerTable { get; set; } = new CreateTableModel();
        public bool ShowErrors { get; set; }
        public IEnumerable<string> Errors { get; set; }


        protected async Task CreateTable()
        {
            ShowErrors = false;
            var result = await TableService.CreatePoker(PokerTable);
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