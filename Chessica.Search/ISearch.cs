using Chessica.Core;

namespace Chessica.Search;

public interface ISearch
{
    Move GetBestMove(BoardState board);
    bool TryGetBestMove(BoardState board, out Move? bestMove);
    int CacheHits { get; }
    int CacheMisses { get; }
}
