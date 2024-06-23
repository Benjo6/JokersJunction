using Blazored.Modal;
using Blazored.Modal.Services;
using JokersJunction.Client.Services;
using JokersJunction.Shared;
using Microsoft.AspNetCore.Components;

namespace JokersJunction.Client.Components
{
    public class TableDeletionConfirmBase : ComponentBase
    {
        [Parameter] public PokerTable Table { get; set; }
        [Inject] public ITableService TableService { get; set; }
        [CascadingParameter] public BlazoredModalInstance BlazoredModal { get; set; }
        protected async Task DeleteConfirm()
        {

            var result = await TableService.DeletePoker(Table.Id);

            if (result.Successful)
            {
                BlazoredModal.Close(ModalResult.Ok(true));
            }
        }
    }
}