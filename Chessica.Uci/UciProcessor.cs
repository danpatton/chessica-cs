using Chessica.Core;

namespace Chessica.Uci;

public delegate void UciEventHandler(object sender, string uciReply);

public class UciProcessor
{
    private readonly IMoveGenerator _moveGenerator;
    private BoardState _boardState;

    public UciProcessor(IMoveGenerator moveGenerator)
    {
        _moveGenerator = moveGenerator;
        _boardState = BoardState.StartingPosition;
        IsRunning = true;
    }

    public bool IsRunning { get; private set; }

    public void Process(string line)
    {
        var commandTokens = line.Split(" ");
        var commandString = commandTokens[0];
        var commandArgs = commandTokens.Skip(1).ToArray();
        HandleCommand(commandString, commandArgs);
    }

    public event UciEventHandler? OnEvent;

    private void WriteLine(string line)
    {
        OnEvent?.Invoke(this, line);
    }

    private void HandleCommand(string commandString, string[] commandArgs)
    {
        switch (commandString)
        {
            case "uci":
                WriteLine("id name Chessica 0.1");
                WriteLine("id author Dan P");
                WriteLine("uciok");
                break;
            case "isready":
                WriteLine("readyok");
                break;
            case "setoption":
                HandleSetOptionCommand(commandArgs);
                break;
            case "ucinewgame":
                HandleUciNewGame();
                break;
            case "position":
                HandlePositionCommand(commandArgs);
                break;
            case "go":
                HandleGoCommand(commandArgs);
                break;
            case "stop":
                break;
            case "ponderhit":
                break;
            case "quit":
                IsRunning = false;
                break;
            default:
                HandleUnknownCommand(commandString, commandArgs);
                break;
        }
    }

    private void HandleUciNewGame()
    {
        _boardState = BoardState.StartingPosition;
    }

    private void HandleSetOptionCommand(string[] commandArgs)
    {
        var optionName = commandArgs.Length >= 2 ? commandArgs[1] : "";
        WriteLine($"No such option: {optionName}");
    }

    private void HandleUnknownCommand(string commandString, string[] commandArgs)
    {
        WriteLine($"Unknown command: {commandString} " + string.Join(" ", commandArgs));
    }

    public void HandlePositionCommand(string[] commandArgs)
    {
        if (commandArgs[0] == "startpos")
        {
            _boardState = BoardState.StartingPosition;
            if (commandArgs[1] == "moves")
            {
                foreach (var moveStr in commandArgs[2..])
                {
                    var from = Coord.FromString(moveStr[..2]);
                    var to = Coord.FromString(moveStr[2..4]);
                    if (moveStr.Length > 4)
                    {
                        var (promotion, _) = moveStr[4].ParseFenChar();
                        _boardState.Push(new PromotionMove(from, to, promotion, from.File != to.File));
                    }
                    else
                    {
                        var maybePiece = _boardState.GetPiece(_boardState.SideToMove, from);
                        maybePiece.Match(
                            piece =>
                            {
                                if (piece == Piece.King && from.File == 4 && to.File is 6 or 2)
                                {
                                    _boardState.Push(new CastlingMove(from, to));
                                }
                                else
                                {
                                    _boardState.Push(new Move(piece, from, to, _boardState.IsOccupied(to)));
                                }
                            },
                            () => throw new Exception($"Illegal move: {moveStr}"));
                    }
                }
            }
        }
        else if (commandArgs[0] == "fen")
        {
            var fen = string.Join(" ", commandArgs[1..]);
            _boardState = BoardState.ParseFen(fen);
        }
    }

    public void HandleGoCommand(string[] commandArgs)
    {
        var bestMove = _moveGenerator.GetBestMove(_boardState);
        WriteLine($"bestmove {bestMove.From}{bestMove.To}");
    }
}