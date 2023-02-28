using Chessica.Core;
using Chessica.Search;

namespace Chessica.Uci;

public class MiniMaxMoveGenerator : IMoveGenerator
{
    private readonly int _maxDepth;

    public MiniMaxMoveGenerator(int maxDepth)
    {
        _maxDepth = maxDepth;
    }

    public bool TryGetBestMove(BoardState boardState, out Move? bestMove)
    {
        return MiniMaxSearch.TryGetBestMove(boardState.Clone(), _maxDepth, out bestMove);
    }

    public Move GetBestMove(BoardState boardState)
    {
        return MiniMaxSearch.GetBestMove(boardState.Clone(), _maxDepth);
    }
}