﻿using Blazored.LocalStorage;
using Blazored.Modal.Services;
using JokersJunction.Client.Components;
using JokersJunction.Client.Services;
using JokersJunction.Shared.Models;
using JokersJunction.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;

namespace JokersJunction.Client.Pages
{
    public class GameSessionBase : ComponentBase
    {
        [Inject] public IStateService StateService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ILocalStorageService LocalStorageService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        [Inject] public IPlayerNoteService PlayerNoteService { get; set; }

        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        public AuthenticationState AuthState { get; set; }

        private HubConnection _hubConnection;

        public GameInformation GameInformation { get; set; } = new GameInformation { Players = new List<GamePlayer>() };

        public string MessageInput { get; set; } = string.Empty;

        public List<GetMessageResult> ChatMessages = new List<GetMessageResult>();

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

            _hubConnection.On("ReceiveStartingHand", (object hand) =>
            {
                var newHand = JsonConvert.DeserializeObject<List<Card>>(hand.ToString());
                GameInformation.Hand.AddRange(newHand);
                StateHasChanged();
            });

            _hubConnection.On("ReceiveTurnPlayer", (string currentPlayer) =>
            {
                GameInformation.CurrentPlayer = currentPlayer;
                StateHasChanged();
            });

            _hubConnection.On("ReceiveFlop", (object flopjson) =>
            {
                var flop = JsonConvert.DeserializeObject<List<Card>>(flopjson.ToString());
                GameInformation.TableCards.AddRange(flop);
                StateHasChanged();
            });

            _hubConnection.On("ReceiveTurnOrRiver", (object card) =>
            {
                var turnOrRiverCard = JsonConvert.DeserializeObject<List<Card>>(card.ToString());
                GameInformation.TableCards.AddRange(turnOrRiverCard);
                StateHasChanged();
            });


            _hubConnection.On("ReceiveWinner", (string winner) =>
            {
                GameInformation.Winner = winner;
                StateHasChanged();
            });


            _hubConnection.On("ReceiveStateRefresh", (object playerState) =>
            {
                var playerStateModel = JsonConvert.DeserializeObject<PlayerStateModel>(playerState.ToString());

                GameInformation.Players = playerStateModel.Players;
                GameInformation.TableCards = playerStateModel.CommunityCards ?? new List<Card>();
                GameInformation.Hand = playerStateModel.HandCards ?? new List<Card>();
                GameInformation.GameInProgress = playerStateModel.GameInProgress;
                GameInformation.RaiseAmount = playerStateModel.RaiseAmount;
                GameInformation.Pots = playerStateModel.Pots ?? new List<Pot>();
                if (GameInformation.GameInProgress == false)
                {
                    foreach (var gameInformationPlayer in GameInformation.Players)
                    {
                        gameInformationPlayer.IsPlaying = false;
                    }
                }

                GameInformation.Winner = null;

                StateHasChanged();
            });

            _hubConnection.On("ReceiveKick", async () =>
            {
                await LocalStorageService.RemoveItemAsync("currentTable");
                NavigationManager.NavigateTo("/");
            });

            await _hubConnection.StartAsync();

            await _hubConnection.SendAsync("AddToUsers", await LocalStorageService.GetItemAsync<int>("currentTable"));

            GameInformation.PlayersNotes = (await PlayerNoteService.GetList()).PlayerNotes;

            await base.OnInitializedAsync();

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

        protected async Task Check()
        {
            await _hubConnection.SendAsync("ActionCheck");
        }

        protected async Task Fold()
        {
            await _hubConnection.SendAsync("ActionFold");
        }

        protected async Task Raise()
        {
            if (GameInformation.PlayerRaise > 0 &&
                GameInformation.Players.First(e => e.Username == AuthState.User.Identity.Name).GameMoney >
                GameInformation.PlayerRaise + GameInformation.RaiseAmount)
            {
                await _hubConnection.SendAsync("ActionRaise", GameInformation.PlayerRaise);
            }
            GameInformation.PlayerRaise = 0;
        }

        protected async Task Call()
        {
            await _hubConnection.SendAsync("ActionCall");
        }
        protected async Task AllIn()
        {
            await _hubConnection.SendAsync("ActionAllIn");
        }

        protected async Task SendMessage()
        {
            if (MessageInput.Length > 0)
            {
                await _hubConnection.SendAsync("SendMessage", MessageInput);
                MessageInput = string.Empty;
            }

        }
        protected async Task SendSignal(string user, string signal)
        {
            await _hubConnection.SendAsync("SendSignal", user, signal);
        }

        protected async Task StartConnection(string user)
        {
            await _hubConnection.SendAsync("StartConnection", user);
        }
    }
}