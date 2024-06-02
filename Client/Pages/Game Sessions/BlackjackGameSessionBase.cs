using Blazored.LocalStorage;
using Blazored.Modal.Services;
using JokersJunction.Client.Services;
using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Components.Web;
using JokersJunction.Client.Components;
using JokersJunction.Shared;

namespace JokersJunction.Client.Pages.Gamesession;

public class BlackjackGameSessionBase : ComponentBase
{
    [Inject] public IStateService StateService { get; set; }
    [Inject] public IModalService ModalService { get; set; }
    [Inject] public ILocalStorageService LocalStorageService { get; set; }
    [Inject] public NavigationManager NavigationManager { get; set; }

    [Inject] public IPlayerNoteService PlayerNoteService { get; set; }

    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    public AuthenticationState AuthState { get; set; }

    private HubConnection _hubConnection;

    public BlackjackGameInformation GameInformation { get; set; } = new BlackjackGameInformation();

    public string MessageInput { get; set; } = string.Empty;

    public List<GetMessageResult> ChatMessages = new();

    protected override async Task OnInitializedAsync()
    {
        AuthState = await AuthenticationStateProvider.GetAuthenticationStateAsync();

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/gameHub"))
            .Build();

        _hubConnection.On("ReceiveMessage", (object message) =>
        {
            var newMessage = JsonConvert.DeserializeObject<GetMessageResult>(message.ToString());
            ChatMessages.Add(newMessage);
            StateHasChanged();
        });

        _hubConnection.On("ReceiveBlackjackStartingHand", (object hand) =>
        {
            var newHand = JsonConvert.DeserializeObject<List<Card>>(hand.ToString());
            GameInformation.Hand.AddRange(newHand);
            StateHasChanged();
        });

        _hubConnection.On("ReceiveBlackjackDealerHand", (object card) =>
        {
            var dealerCard = JsonConvert.DeserializeObject<Card>(card.ToString());
            GameInformation.DealerHand.Add(dealerCard);
            StateHasChanged();
        });

        _hubConnection.On("ReceiveBlackjackStateRefresh", (object playerState) =>
        {
            var playerStateModel = JsonConvert.DeserializeObject<BlackjackPlayerStateModel>(playerState.ToString());

            GameInformation.Players = playerStateModel.Players;
            GameInformation.Hand = playerStateModel.HandCards ?? new List<Card>();
            GameInformation.GameInProgress = playerStateModel.GameInProgress;
            GameInformation.Winner = null;

            StateHasChanged();
        });

        _hubConnection.On("ReceiveBlackjackBust", (object hand) =>
        {
            var newHand = JsonConvert.DeserializeObject<List<Card>>(hand.ToString());
            GameInformation.Hand = newHand;
            StateHasChanged();
        });

        _hubConnection.On("ReceiveBlackjackHit", (object hand) =>
        {
            var newHand = JsonConvert.DeserializeObject<List<Card>>(hand.ToString());
            GameInformation.Hand = newHand;
            StateHasChanged();
        });

        _hubConnection.On("ReceiveBlackjackStand", StateHasChanged);

        _hubConnection.On("ReceiveBlackjackWin", (object dealerHand) =>
        {
            var newHand = JsonConvert.DeserializeObject<List<Card>>(dealerHand.ToString());
            GameInformation.DealerHand = newHand;
            StateHasChanged();
        });

        _hubConnection.On("ReceiveBlackjackLose", (object dealerHand) =>
        {
            var newHand = JsonConvert.DeserializeObject<List<Card>>(dealerHand.ToString());
            GameInformation.DealerHand = newHand;
            StateHasChanged();
        });

        _hubConnection.On("ReceiveBlackjackDraw", (object dealerHand) =>
        {
            var newHand = JsonConvert.DeserializeObject<List<Card>>(dealerHand.ToString());
            GameInformation.DealerHand = newHand;
            StateHasChanged();
        });

        await _hubConnection.StartAsync();

        await _hubConnection.SendAsync("AddToUsers", await LocalStorageService.GetItemAsync<int>("currentTable"));

        GameInformation.PlayersNotes = (await PlayerNoteService.GetList()).PlayerNotes;

        await base.OnInitializedAsync();
    }

    protected async Task Hit()
    {
        await _hubConnection.SendAsync("BlackjackHit", AuthState.User.Identity.Name);
    }

    protected async Task Stand()
    {
        await _hubConnection.SendAsync("BlackjackStand", AuthState.User.Identity.Name);
    }

    protected async Task Bet()
    {
        if (GameInformation.PlayerBet > 0 &&
            GameInformation.Players.First(e => e.Username == AuthState.User.Identity.Name).GameMoney >= GameInformation.PlayerBet)
        {
            await _hubConnection.SendAsync("BlackjackBet", AuthState.User.Identity.Name, GameInformation.PlayerBet);
        }
        GameInformation.PlayerBet = 0;
    }

    protected async Task LeaveTable()
    {
        await LocalStorageService.RemoveItemAsync("currentTable");
        await _hubConnection.StopAsync();
        NavigationManager.NavigateTo("/");
    }

    protected async Task MarkReady()
    {
        var formModal = ModalService.Show<JoinTable>("Join table");
        var result = await formModal.Result;
        if (result.Cancelled) return;
        await _hubConnection.SendAsync("MarkReady", result.Data);
        StateService.CallRequestRefresh();
        await Task.Delay(500);
        StateService.CallRequestRefresh();
    }

    protected async Task UnmarkReady()
    {
        await _hubConnection.SendAsync("UnmarkReady");
        StateService.CallRequestRefresh();
        await Task.Delay(500);
        StateService.CallRequestRefresh();
    }

    protected async Task SendMessage()
    {
        if (MessageInput.Length > 0)
        {
            await _hubConnection.SendAsync("SendMessage", MessageInput);
            MessageInput = string.Empty;
        }
    }

    protected async Task SendOnEnter(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await SendMessage();
        }
    }
}