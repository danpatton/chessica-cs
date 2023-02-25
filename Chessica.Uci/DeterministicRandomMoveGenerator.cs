using Chessica.Core;

namespace Chessica.Uci;

public class DeterministicRandomMoveGenerator : IMoveGenerator
{
    public bool TryGetBestMove(BoardState boardState, out Move? bestMove)
    {
        var rng = new Random(boardState.ToFenString(includeMoveClocks: false).GetHashCode());
        var legalMoves = boardState.GetLegalMoves().ToList();
        if (!legalMoves.Any())
        {
            bestMove = null;
            return false;
        }

        bestMove = legalMoves[rng.Next(legalMoves.Count)];
        return true;
    }

    public Move GetBestMove(BoardState boardState)
    {
        if (!TryGetBestMove(boardState, out var bestMove))
        {
            throw new Exception("No legal moves!");
        }

        return bestMove!;
    }
}