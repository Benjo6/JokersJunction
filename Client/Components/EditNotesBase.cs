using Blazored.Modal;
using JokersJunction.Client.Services;
using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace JokersJunction.Client.Components
{
    public class EditNotesBase : ComponentBase
    {
        [Inject] public IPlayerNoteService PlayerNoteService { get; set; }
        [Parameter] public string CurrentNote { get; set; }
        [Parameter] public string NotedPlayerName { get; set; }
        public EditNotesModel EditNotesModel { get; set; } = new EditNotesModel();
        [CascadingParameter] public BlazoredModalInstance BlazoredModal { get; set; }
        public bool ShowErrors { get; set; }
        public IEnumerable<string> Errors { get; set; } = new List<string>();

        protected override void OnInitialized()
        {
            EditNotesModel.CurrentNote = CurrentNote;
            base.OnInitialized();
        }

        protected async Task EditNotes()
        {
            ShowErrors = false;
            Console.WriteLine(EditNotesModel.CurrentNote);
            var result = await PlayerNoteService.Create(new CreateNoteModel
                { Description = EditNotesModel.CurrentNote, NotedPlayerName = NotedPlayerName });
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