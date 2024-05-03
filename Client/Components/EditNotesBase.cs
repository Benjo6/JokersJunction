using Blazored.Modal;
using JokersJunction.Client.Services;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace JokersJunction.Client.Components
{
    public class EditNotesBase : ComponentBase
    {
        [Inject] public IPlayerNoteService PlayerNoteService { get; set; }
        [Parameter] public string CurrentNote { get; set; }
        [Parameter] public string NotedPlayerName { get; set; }
        public EditNotesModel EditNotesModel { get; set; } = new EditNotesModel();
        [CascadingParameter] public BlazoredModalInstance BlazoredModal { get; set; }
        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        public bool ShowErrors { get; set; }
        public IEnumerable<string> Errors { get; set; } = new List<string>();
        public AuthenticationState AuthState { get; set; }

        protected override async Task OnInitializedAsync()
        {
            AuthState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            EditNotesModel.CurrentNote = CurrentNote;
            await base.OnInitializedAsync();
        }

        protected async Task EditNotes()
        {
            ShowErrors = false;
            Console.WriteLine(EditNotesModel.CurrentNote);
            var result = await PlayerNoteService.Create(new CreatePlayerNote()
                { UserId = AuthState.User.FindFirstValue(ClaimTypes.NameIdentifier), Description = EditNotesModel.CurrentNote, NotedPlayerName = NotedPlayerName });
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