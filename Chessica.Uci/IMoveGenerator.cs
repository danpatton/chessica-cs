using Chessica.Core;

namespace Chessica.Uci;

public interface IMoveGenerator
{
    bool TryGetBestMove(BoardState boardState, out Move? bestMove);
    Move GetBestMove(BoardState boardState);
}
