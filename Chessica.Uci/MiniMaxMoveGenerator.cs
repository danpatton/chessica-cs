using Chessica.Core;
using Chessica.Search;

namespace Chessica.Uci;

public class MiniMaxMoveGenerator : IMoveGenerator
{
    private readonly ISearch _search;

    public MiniMaxMoveGenerator(int maxDepth)
    {
        _search = new MiniMaxSearchV2(maxDepth);
    }

    public bool TryGetBestMove(BoardState boardState, out Move? bestMove)
    {
        return _search.TryGetBestMove(boardState.Clone(), out bestMove);
    }

    public Move GetBestMove(BoardState boardState)
    {
        return _search.GetBestMove(boardState.Clone());
    }
}