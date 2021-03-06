import React from 'react';
import { useState, useEffect } from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import Container from 'react-bootstrap/Container';
import Button from 'react-bootstrap/Button';


function Hangman() {
    const [hubConnection, setHubConnection] = useState(null);

    const [word, setWord] = useState('');
    const [wordInput, setWordInput] = useState('');
    const [wordGuessInput, setWordGuessInput] = useState('');

    const [player, setPlayer] = useState('');
    const [wordSetter, setWordSetter] = useState('');
    const [turnPlayer, setTurnPlayer] = useState('');
    const [winner, setWinner] = useState('');

    const [gameId, setGameId] = useState('');
    const [gamePlayers, setGamePlayers] = useState([]);
    const [gameStarted, setGameStarted] = useState(false);

    const [guessesRemaining, setGuessesRemaining] = useState(7);
    const [guessedLetters, setGuessedLetters] = useState([]);


    const [codeInput, setCodeInput] = useState('');

    useEffect(() => {
        let connection = new HubConnectionBuilder().withUrl(process.env.REACT_APP_API_ENDPOINT).build();
        setHubConnection(connection);
    }, [])

    useEffect(() => {
        if (hubConnection) {
            hubConnection.start()
                .then(result => {                  
                    setPlayer(hubConnection.connectionId);
        
                    hubConnection.on('sendPlayerName', name => {
                        setPlayer(name);
                    });

                    hubConnection.on('sendGameId', id => {
                        setGameId(id);
                    })

                    hubConnection.on('renderGameLobby', players => {
                        setGamePlayers(players);
                    })

                    hubConnection.on('determineWordSetter', playerName => {
                        setWordSetter(playerName);
                        setGameStarted(true);
                    })

                    hubConnection.on('renderWord', worderino => {
                        console.log("WORD is " + worderino);
                        setWord(worderino);
                    })

                    hubConnection.on('turn', player => {
                        setTurnPlayer(player);
                    })

                    hubConnection.on('victory', player => {
                        setWinner(player);
                    })

                    hubConnection.on('sendRemainingTries', tries => {
                        setGuessesRemaining(tries);
                    })

                    hubConnection.on('sendGuessedLetters', letters => {
                        setGuessedLetters(letters);
                    })

                }).catch(e => console.log('Connection failed: ', e));
        }
    }, [hubConnection])

    // useEffect(() => {
    //     if (player !== '' && player === wordSetter) {
    //         alert("U Are WordSetter!");
    //     }
    // }, [wordSetter])

    const newGame = () => hubConnection.invoke('createGame').catch(err => console.error(err.toString()));
    const joinGame = () => hubConnection.invoke('joinGame', codeInput).catch(err => console.error(err.toString()));
    const startGame = () => hubConnection.invoke('startGame', gameId).catch(err => console.error(err.toString()));
    const sendWord = () => hubConnection.invoke('wordSet', wordInput, gameId).catch(err => console.error(err.toString()));
    const sendGuess = () => hubConnection.invoke('guessLetter', wordGuessInput, gameId).catch(err => console.error(err.toString()));

    const sendGuessButton = () => {
        sendGuess();
        setWordGuessInput('');
    }

    return(
        <>
            <Container> 
                <h2>Player: {player}</h2>
                <h2>GameId: {gameId}</h2>
            </Container>

            {gameId === '' && <Container>
                <input value={codeInput} onInput={e => setCodeInput(e.target.value)}></input>
                <Button variant="primary" disabled={codeInput === ''} onClick={joinGame}>Join Game</Button>
                <Button variant="primary" onClick={newGame}>New Game</Button>
            </Container>}

            {gameId !== '' && 
            <Container>
                <h4>Current Players:</h4>
                <ul>{gamePlayers.map(name => <li key={name}>{name}</li>)}</ul>
                {!gameStarted? 
                    (<Button disabled={gamePlayers.length < 2} onClick={startGame}>Start Game</Button>) : 
                    winner === '' ?
                        player === wordSetter ? (
                            word === '' ?
                                <div>
                                    <h3>Please choose a word:</h3>
                                    <input value={wordInput} onInput={e => setWordInput(e.target.value)}></input>
                                    <Button variant="primary" onClick={sendWord}>Submit</Button>
                                </div>
                            :
                                <div>
                                    <h1>The others are guessing: </h1>
                                    <h2>{word}</h2>
                                    <h2>Tries remaining: {guessesRemaining}</h2>
                                    <h5>Guessed Letters: {guessedLetters.map(x => x + ", ")}</h5>
                                </div>
                        
                        ) : (
                            word === '' ? 
                                <div>Word Setter is setting the Word...</div> 
                            :
                                player !== '' && player === turnPlayer ?
                                    <>
                                        <div>Guess the Word: {word}</div>
                                        <input maxLength={1} value={wordGuessInput} onInput={e => setWordGuessInput(e.target.value)}></input>
                                        <Button variant="primary" onClick={sendGuessButton} disabled={wordGuessInput.length < 1}>Submit</Button>
                                        <h2>Tries remaining: {guessesRemaining}</h2>
                                        <h5>Guessed Letters: {guessedLetters.map(x => x + ", ")}</h5>
                                    </>
                                :
                                    <>
                                        <div>Other Player is guessing the Word: {word}</div>
                                        <h2>Tries remaining: {guessesRemaining}</h2>
                                        <h5>Guessed Letters: {guessedLetters.map(x => x + ", ")}</h5>
                                    </>
                            )
                    :
                            player !== '' && player === winner ?
                                <h1>A WINNER IS YOU</h1>
                            :
                                <h2>THE OTHERS BESTED YOU :(</h2>
                }
            </Container>}

        </>
    )

}

export default Hangman;