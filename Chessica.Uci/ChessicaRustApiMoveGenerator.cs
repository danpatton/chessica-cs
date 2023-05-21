using Chessica.Core;
using Chessica.Rust;

namespace Chessica.Uci;

public class ChessicaRustApiMoveGenerator : IMoveGenerator
{
    private readonly uint _maxDepth;
    private readonly uint _ttKeyBits;
    private readonly ulong _rngSeed;

    public ChessicaRustApiMoveGenerator(uint maxDepth, uint ttKeyBits, ulong? rngSeed = null)
    {
        _maxDepth = maxDepth;
        _ttKeyBits = ttKeyBits;
        _rngSeed = rngSeed ?? 0ul;
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
        if (ChessicaRustApi.TryGetBestMove(initialFen, moveHistory, _maxDepth, _ttKeyBits, _rngSeed, out var uciMove) && uciMove != null)
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