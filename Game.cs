using System;
using System.Collections.Generic;
using System.Linq;

public class Game
{
    private const int MaxPlayers = 5;
    private const int MaxFailGuesses = 7; 
    private const int MaxWordLength = 15; 

    public string Id {get; set;}
    public string Word {get; private set;}
    public string GuessedWord;
    private int FailGuesses;


    private List<Player> Players;
    public Player CurrentPlayer;
    public Player WordSetter;

    public bool GameStarted;
    internal int PlayerCount() { return Players.Count; }

    public Game()
    {
        FailGuesses = 0;
        GameStarted = false;
        Players = new List<Player>();
    }

    public void AddPlayer(Player player)
    {
        Console.WriteLine("ADDING PLAYER " + player.Name + " TO GAME");
        if (Players.Count < MaxPlayers) 
            Players.Add(player);
        else 
            throw new Exception("Cannot add Player: Game is full!");
    }

    public void RemovePlayer(Player player)
    {
        //TODO: Fix
        if (Players.Exists(p => p.ConnectionId == player.ConnectionId)) Players.Remove(player);
    }

    public Player GetPlayer(string connectionId)
    {
        return Players.Find(p => p.ConnectionId == connectionId);
    }

    internal string[] GetPlayerList()
    {
        // string[] output = new string[Players.Count];

        // foreach (Player player in Players) output.Append(player.Name);

        // return output;

        return Players.Select(player => player.Name).ToArray();

    }

    internal bool ConnectionInGame(string connectionId)
    {
        return Players.Exists(p => p.ConnectionId == connectionId);
    }

    /// <summary>
    /// Determines WordSetter if enough players are in Game
    /// </summary>
    public void StartGame()
    {
        if (GameStarted) throw new Exception("Cannot start game: Game already started!");
        if (Players.Count < 2) throw new Exception("Cannot start game: Not enough players!");

        // Determine word setter:

        var random = new Random();
        var result = random.Next(Players.Count);

        WordSetter = Players[result];
    }

    public void SetWord(string word)
    {
        if (word.Length > MaxWordLength) throw new Exception("Cannot set word: Word too long!");

        Word = word.ToUpper();

        var blankWord = "";

        for (int i = word.Length; i > 0; i--) blankWord += "_";

        GuessedWord = blankWord;
    }

    public bool GuessLetter(string letter)
    {
        if (letter.Length > 1) throw new Exception("Cannot guess letter: Multiple letters detected");

        char[] charsOfGuessedWord = GuessedWord.ToCharArray();
        bool letterInWord = false;

        for (int i = 0; i < Word.Length; i++)
        {
            if (Word[i] == letter.ToUpper()[0]) 
            {
                charsOfGuessedWord[i] = letter.ToUpper()[0];
                letterInWord = true;
            }
        }

        GuessedWord = new string(charsOfGuessedWord);
        if (!letterInWord) FailGuesses++;
        
        return letterInWord;
    }

    public Player GetWinner()
    {
        if (FailGuesses >= MaxFailGuesses) return Players.Find(p => p.ConnectionId == WordSetter.ConnectionId);
        if (GuessedWord.ToUpper() == Word.ToUpper()) return Players.Find(p => p.ConnectionId == CurrentPlayer.ConnectionId);
        
        return null;
    }

    public void NextPlayer()
    {
        var eligiblePlayers = Players.FindAll(p => p.ConnectionId != WordSetter.ConnectionId);
        if (CurrentPlayer is null){
           CurrentPlayer = eligiblePlayers[new Random().Next(eligiblePlayers.Count)];
        } else {
            int currentIndex = eligiblePlayers.FindIndex(p => p.ConnectionId == CurrentPlayer.ConnectionId);
            CurrentPlayer = eligiblePlayers[(currentIndex + 1) % eligiblePlayers.Count];
        }
    }

}

public class Player
{
    public string Name {get; set;}
    public string ConnectionId {get; set;}

}