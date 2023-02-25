using System.Reflection;
using Chessica.Pgn;

namespace Chessica.Core.Test;

public class TestUtils
{
    public static void AssertValidMoveSequence(BoardState boardState, IEnumerable<Move> moves)
    {
        foreach (var move in moves)
        {
            var legalMoves = boardState.GetLegalMoves();
            Assert.That(legalMoves, Does.Contain(move));
            boardState.Push(move);
        }
    }

    public static string LoadEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new Exception("No such resource");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static void SimulatePgnGame(PgnGame pgn, string[] expectedFenSequence, PgnGameResult expectedResult)
    {
        var board = BoardState.StartingPosition;

        var undoStack = new Stack<IDisposable>();
        foreach (var (pgnMove, expectedFen) in pgn.Moves.Zip(expectedFenSequence))
        {
            var fen = board.ToFenString();
            Assert.That(fen, Is.EqualTo(expectedFen));
            var move = board.ToMove(pgnMove.Spec);
            undoStack.Push(board.Push(move));
        }

        Assert.That(pgn.Result, Is.EqualTo(expectedResult));
        switch (pgn.Result)
        {
            case PgnGameResult.BlackWin:
            case PgnGameResult.WhiteWin:
                if (pgn.Moves.Any())
                {
                    var isCheckmate = pgn.Moves[^1].Spec.EndsWith("#");
                    Assert.That(board.IsCheckmate(), Is.EqualTo(isCheckmate));
                }
                break;
            default:
                Assert.That(board.IsCheckmate(), Is.False);
                break;
        }

        // now unwind the stack, ensuring we can undo every move
        while (undoStack.TryPop(out var undoer))
        {
            undoer.Dispose();
        }
    }
}