using Chessica.Core;
using Chessica.Search;

namespace Chessica.Uci;

public class MiniMaxMoveGenerator : IMoveGenerator
{
    public MiniMaxSearch Search { get; }

    public MiniMaxMoveGenerator(int maxDepth)
    {
        Search = new MiniMaxSearch(maxDepth);
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