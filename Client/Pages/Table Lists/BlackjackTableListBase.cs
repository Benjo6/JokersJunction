using JokersJunction.Client.Services;
using JokersJunction.Shared;
using Microsoft.AspNetCore.Components;

namespace JokersJunction.Client.Pages;

public class BlackjackTableListBase : ComponentBase
{
    [Inject]
    public ITableService TableService { get; set; }

    [Parameter]
    public EventCallback<string> OnChange { get; set; }

    public IEnumerable<BlackjackTable> BlackjackTables { get; set; }

    public bool ShowError { get; set; }
    public string ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await GetTablesList();
    }

    protected async void UpdateHandler(string _)
    {
        await GetTablesList();
        await OnChange.InvokeAsync("Table List changed");
        StateHasChanged();
    }

    public async void Refresh()
    {
        await GetTablesList();
        StateHasChanged();
    }

    private async Task GetTablesList()
    {
        ShowError = false;

        var result = await TableService.GetBlackjackList();

        if (result.Successful)
        {
            BlackjackTables = result.Tables;
        }
        else
        {
            ShowError = true;
            ErrorMessage = result.Error;
        }
    }
}