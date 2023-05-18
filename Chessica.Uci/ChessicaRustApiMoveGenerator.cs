using Chessica.Core;
using Chessica.Rust;

namespace Chessica.Uci;

public class ChessicaRustApiMoveGenerator : IMoveGenerator
{
    private readonly uint _maxDepth;
    private readonly uint _ttKeyBits;

    public ChessicaRustApiMoveGenerator(uint maxDepth, uint ttKeyBits)
    {
        _maxDepth = maxDepth;
        _ttKeyBits = ttKeyBits;
    }

    public bool TryGetBestMove(BoardState boardState, out Move? bestMove)
    {
        var uciMoves = boardState.GetLegalMoves()
            .ToDictionary(m => m.ToUciString(), m => m);

        if (!uciMoves.Any())
        {
            bestMove = null;
            return false;
        }

        var initialFen = BoardState.StartingPosition.ToFenString();
        var moveHistory = boardState.UciMoveHistory.ToList();
        if (ChessicaRustApi.TryGetBestMove(initialFen, moveHistory, _maxDepth, _ttKeyBits, out var uciMove) && uciMove != null)
        {
            return uciMoves.TryGetValue(uciMove, out bestMove);
        }

        bestMove = null;
        return false;
    }

    public Move GetBestMove(BoardState boardState)
    {
        if (TryGetBestMove(boardState, out var bestMove) && bestMove != null)
        {
            return bestMove;
        }

        throw new Exception("No best move");
    }
}