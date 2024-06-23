using Blazored.LocalStorage;
using Blazored.Modal.Services;
using JokersJunction.Client.Services;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;

namespace JokersJunction.Client.Pages.Game_Sessions;

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

        var savedToken = await LocalStorageService.GetItemAsync<string>("authToken");

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/GameHub"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(savedToken);
            })
            .Build();

        _hubConnection.On("ReceiveMessage", (object message) =>
        {
            var newMessage = JsonConvert.DeserializeObject<GetMessageResult>(message.ToString());
            ChatMessages.Add(newMessage);
            StateHasChanged();
        });

        _hubConnection.On("ReceiveBlackjackTurnPlayer", async () =>
        {
            GameInformation.CurrentPlayer = AuthState.User.Identity.Name;
            await InvokeAsync(StateHasChanged);
        });


        _hubConnection.On("ReceiveBlackjackDealerHand", (object card) =>
        {
            var dealerCard = JsonConvert.DeserializeObject<Card>(card.ToString());
            //GameInformation.DealerHand.Add(dealerCard);
            StateHasChanged();
        });

        _hubConnection.On("ReceiveBlackjackStateRefresh", async (object playerState) =>
        {
            var playerStateModel = JsonConvert.DeserializeObject<BlackjackPlayerStateModel>(playerState.ToString());
            StateService.CallRequestRefresh();
            await Task.Delay(200);
            StateService.CallRequestRefresh();
            GameInformation.Players = playerStateModel.Players;
            GameInformation.Hand = playerStateModel.HandCards ?? new List<Card>();
            GameInformation.DealerHand = playerStateModel.DealerCards ?? new List<Card>();
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

        _hubConnection.On("ReceiveBlackjackStand", (object dealerHand) =>
        {
            var newDealerHand = JsonConvert.DeserializeObject<List<Card>>(dealerHand.ToString());
            GameInformation.DealerHand = newDealerHand;
            StateHasChanged();
        });


        _hubConnection.On("ReceiveBlackjackWin", async () =>
        {
            GameInformation.Winner = "Player";
            StateService.CallRequestRefresh();
            await Task.Delay(200);
            StateService.CallRequestRefresh();
            StateHasChanged();
        });

        _hubConnection.On("ReceiveBlackjackLose", () =>
        {
            GameInformation.Winner = "Dealer";
            StateHasChanged();
        });

        _hubConnection.On("ReceiveBlackjackDraw", async () =>
        {
            GameInformation.Winner = "Draw";
            StateService.CallRequestRefresh();
            await Task.Delay(200);
            StateService.CallRequestRefresh();
            StateHasChanged();
        });

        await _hubConnection.StartAsync();

        await _hubConnection.SendAsync("AddToUsersToBlackjack", await LocalStorageService.GetItemAsync<int>("currentTable"));

        //GameInformation.PlayersNotes = (await PlayerNoteService.GetList()).PlayerNotes;

        await base.OnInitializedAsync();
    }

    protected async Task Start()
    {
        // Deal the initial cards
        await _hubConnection.SendAsync("StartBlackjackGame", await LocalStorageService.GetItemAsync<int>("currentTable"), 100);
    }

    protected async Task Hit()
    {
        // Ask for one more card
        await _hubConnection.SendAsync("BlackjackHit", AuthState.User.Identity.Name);
    }

    protected async Task Stand()
    {
        // Stop with the current cards
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