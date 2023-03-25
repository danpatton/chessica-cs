using Chessica.Core;
using Chessica.Search;

namespace Chessica.Uci;

public class MiniMaxMoveGenerator : IMoveGenerator
{
    public ISearch Search { get; }

    public MiniMaxMoveGenerator(int maxDepth)
    {
        Search = new MiniMaxSearchV1(maxDepth);
    }

    public bool TryGetBestMove(BoardState boardState, out Move? bestMove)
    {
        return Search.TryGetBestMove(boardState.Clone(), out bestMove);
    }

    public Move GetBestMove(BoardState boardState)
    {
        return Search.GetBestMove(boardState.Clone());
    }
}