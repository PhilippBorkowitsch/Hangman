using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

public interface IGameClient
{
    Task SendGameId(string id);
    Task RenderWord(string word);
    Task RenderGameLobby(string[] players);
    Task DetermineWordSetter(string player);
    Task Turn(string player);
    Task Victory(string player);
    Task SendPlayerName(string name);
    Task SendRemainingTries(int tries);
    Task SendGuessedLetters(string[] letters);
}

public class GameHub : Hub<IGameClient>
{
    public const string NoGameGroup = "NO GAME";

    public List<Game> Games;
    public List<Player> Players;

    public GameHub(List<Game> games, List<Player> players)
    {
        Games = games;
        Players = players;
    }

    public override async Task OnConnectedAsync()
    {   
        var p = new Player{ConnectionId = Context.ConnectionId, Name = Context.ConnectionId};
        Players.Add(p);

        await Groups.AddToGroupAsync(Context.ConnectionId, NoGameGroup);
        await Clients.Caller.SendPlayerName(p.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var player = Players.Find(p => p.ConnectionId == Context.ConnectionId);
        var game = Games.Find(g => g.ConnectionInGame(player.ConnectionId));

        if (!(game is null))
        {
            game.RemovePlayer(player);

            await Groups.RemoveFromGroupAsync(player.ConnectionId, game.Id);
            
            if (game.PlayerCount() == 0) Games.Remove(game); 
            else await Clients.Group(game.Id).RenderGameLobby(game.GetPlayerList());

        } else {
            await Groups.RemoveFromGroupAsync(player.ConnectionId, NoGameGroup);
        }

        Players.Remove(player);

        await base.OnDisconnectedAsync(exception);
    }


    public async Task CreateGame()
    {
        var game = new Game(){Id = Guid.NewGuid().ToString()};
        Console.WriteLine("ID REQUESTING NEW GAME: " + Context.ConnectionId);

        var requestingPlayer = Players.Find(p => p.ConnectionId == Context.ConnectionId);

        game.AddPlayer(requestingPlayer);
        Games.Add(game);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, NoGameGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, game.Id);

        await Clients.Caller.SendGameId(game.Id);
        await Clients.Group(game.Id).RenderGameLobby(game.GetPlayerList());
    }

    public async Task JoinGame(string id)
    {
        var gameToJoin = Games.Find(g => g.Id == id);

        // TODO: Make it possible to join during game
        if(gameToJoin.GameStarted) return;
        try
        {
            gameToJoin.AddPlayer(Players.Find(p => p.ConnectionId == Context.ConnectionId));    
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, NoGameGroup);
            await Groups.AddToGroupAsync(Context.ConnectionId, gameToJoin.Id);

            await Clients.Caller.SendGameId(gameToJoin.Id);
            await Clients.Group(gameToJoin.Id).RenderGameLobby(gameToJoin.GetPlayerList());

        } catch (Exception)
        {
            return;
        }

    }

    public async Task StartGame(string id)
    {
        var gameToStart = Games.Find(g => g.Id == id);
        try
        {
            gameToStart.StartGame();

            await Clients.Group(gameToStart.Id).DetermineWordSetter(gameToStart.WordSetter.Name);

        } catch (Exception) 
        {
            return;
        }
    }

    public async Task WordSet(string word, string id)
    {
        var game = Games.Find(g => g.Id == id);
        
        if (game.WordSetter.ConnectionId != Context.ConnectionId) return;
        if (game.GameStarted) return;

        try{
            game.SetWord(word);
            game.GameStarted = true;

            game.NextPlayer();

            await Clients.Group(game.Id).RenderWord(game.GuessedWord);
            await Clients.Group(game.Id).Turn(game.CurrentPlayer.Name);

        } catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return;
        }
    }

    public async Task GuessLetter(string letter, string id)
    {
        var game = Games.Find(g => g.Id == id);

        if (game.CurrentPlayer.ConnectionId != Context.ConnectionId) return;
        if (!game.GameStarted) return;

        try 
        {
            var success = game.GuessLetter(letter);
            await Clients.Group(game.Id).RenderWord(game.GuessedWord);

            if (game.GetWinner() != null)
            {
                await Clients.Group(game.Id).Victory(game.GetWinner().Name);
                return;
            }

            if (!success) game.NextPlayer();

            await Clients.Group(game.Id).SendRemainingTries(game.GetRemainingTries());
            await Clients.Group(game.Id).Turn(game.CurrentPlayer.Name);
            await Clients.Group(game.Id).SendGuessedLetters(game.GetGuessedLetters());


        } catch (Exception)
        {
            return;
        }



    }


}
