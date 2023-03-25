using Chessica.Core;

namespace Chessica.Search;

public class MiniMaxSearchV2 : ISearch
{
    private readonly int _maxDepth;

    public MiniMaxSearchV2(int maxDepth)
    {
        _maxDepth = maxDepth;
    }

    public int CacheHits => 0;

    public int CacheMisses => 0;

    public Move GetBestMove(BoardState board)
    {
        if (!TryGetBestMove(board, out var bestMove))
        {
            throw new Exception("No legal moves!");
        }

        return bestMove!;
    }

    public bool TryGetBestMove(BoardState board, out Move? bestMove)
    {
        var moves = board.GetLegalMoves()
            .OrderBy(move => SearchOrder(move, board))
            .ToArray();

        if (moves.Length == 0)
        {
            bestMove = null;
            return false;
        }

        var maximising = board.SideToMove == Side.White;
        var alpha = double.NegativeInfinity;
        var beta = double.PositiveInfinity;
        Move? bestMoveSoFar = null;

        foreach (var move in moves)
        {
            if (alpha >= beta)
            {
                break;
            }

            var nudge = move.PositionalNudge(board) * (maximising ? 1 : -1);
            using (board.Push(move))
            {
                var score = DoSearch(board, _maxDepth - 1, move, alpha, beta, nudge);
                if (maximising)
                {
                    if (score > alpha)
                    {
                        alpha = score;
                        bestMoveSoFar = move;
                    }
                }
                else
                {
                    if (score < beta)
                    {
                        beta = score;
                        bestMoveSoFar = move;
                    }
                }
            }
        }

        bestMove = bestMoveSoFar;
        return bestMoveSoFar != null;
    }

    private static double SearchOrder(Move move, BoardState board)
    {
        // checks first
        if (move.IsCheck) return double.NegativeInfinity;
        // ..then captures in order of "lowest risk"
        if (move.IsCapture) return 1e-3 * move.Piece.Value();
        // ..then positional nudge
        return 10 - move.PositionalNudge(board);
    }

    private static double SearchOrder(Move move, Move lastMove, BoardState board)
    {
        // checks first
        if (move.IsCheck) return double.NegativeInfinity;
        // ..then take-backs
        if (move.IsCapture && move.To == lastMove.To) return 0;
        // ..then other captures in order of "lowest risk"
        if (move.IsCapture) return 1e-3 * move.Piece.Value();
        // ..then positional nudge
        return 10 - move.PositionalNudge(board);
    }

    private double DoSearch(BoardState board, int depth, Move lastMove, double alpha, double beta, double cumulativeNudge)
    {
        var moves = board.GetLegalMoves()
            .OrderBy(move => SearchOrder(move, lastMove, board))
            .ToArray();

        if (moves.Length == 0)
        {
            var score = cumulativeNudge +
                        (lastMove.IsCheck ? board.SideToMove.LossScore() : board.GetScore());
            return score;
        }

        var maximising = board.SideToMove == Side.White;
        var bestScore = maximising ? double.NegativeInfinity : double.PositiveInfinity;

        foreach (var move in moves)
        {
            if (alpha >= beta)
            {
                break;
            }

            var nudge = cumulativeNudge + move.PositionalNudge(board) * (maximising ? 1 : -1);
            using (board.Push(move))
            {
                var score = depth == 0
                    ? DoQuiescenceSearch(board, move, alpha, beta, nudge)
                    : DoSearch(board, depth - 1, move, alpha, beta, nudge);

                if (maximising)
                {
                    bestScore = alpha = Math.Max(bestScore, score);
                }
                else
                {
                    bestScore = beta = Math.Min(bestScore, score);
                }
            }
        }

        return bestScore;
    }

    private double DoQuiescenceSearch(BoardState board, Move lastMove, double alpha, double beta, double cumulativeNudge)
    {
        if (!lastMove.IsCapture && !lastMove.IsCheck)
        {
            var score = cumulativeNudge + board.GetScore();
            return score;
        }

        var moves = board.GetLegalMoves()
            .Where(m => m.IsCapture || m.IsCheck)
            .OrderBy(move => SearchOrder(move, lastMove, board))
            .ToArray();

        if (moves.Length == 0)
        {
            if (!lastMove.IsCheck)
            {
                var score = cumulativeNudge + board.GetScore();
                return score;
            }
            var (_, legalMoveCount) = board.GetGameState();
            if (legalMoveCount == 0)
            {
                var score = board.SideToMove.LossScore();
                return score;
            }
            else
            {
                return board.GetScore();
            }
        }

        var maximising = board.SideToMove == Side.White;
        var bestScore = maximising ? double.NegativeInfinity : double.PositiveInfinity;

        foreach (var move in moves)
        {
            if (alpha >= beta)
            {
                break;
            }

            var nudge = cumulativeNudge + move.PositionalNudge(board);
            using (board.Push(move))
            {
                var score = DoQuiescenceSearch(board, move, alpha, beta, nudge);

                if (maximising)
                {
                    bestScore = alpha = Math.Max(bestScore, score);
                }
                else
                {
                    bestScore = beta = Math.Min(bestScore, score);
                }
            }
        }

        return bestScore;
    }
}