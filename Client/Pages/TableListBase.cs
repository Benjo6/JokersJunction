﻿using JokersJunction.Client.Services;
using JokersJunction.Shared;
using Microsoft.AspNetCore.Components;

namespace JokersJunction.Client.Pages
{
    public class TableListBase : ComponentBase
    {
        [Inject]
        public ITableService TableService { get; set; }

        [Parameter]
        public EventCallback<string> OnChange { get; set; }

        public IEnumerable<PokerTable> PokerTables { get; set; }

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

            var result = await TableService.GetList();

            if (result.Successful)
            {
                PokerTables = result.PokerTables;
            }
            else
            {
                ShowError = true;
                ErrorMessage = result.Error;
            }
        }
    }
}