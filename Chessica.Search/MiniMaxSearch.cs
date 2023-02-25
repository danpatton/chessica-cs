using Chessica.Core;

namespace Chessica.Search;

public static class MiniMaxSearch
{
    public static Move GetBestMove(BoardState board, int depth)
    {
        if (!TryGetBestMove(board, depth, out var bestMove))
        {
            throw new Exception("No legal moves!");
        }

        return bestMove!;
    }

    public static bool TryGetBestMove(BoardState board, int depth, out Move? bestMove)
    {
        var legalMoves = board.GetLegalMoves().ToList();
        if (!legalMoves.Any())
        {
            bestMove = null;
            return false;
        }

        var orderedLegalMoves = board.SideToMove == Side.White
            ? legalMoves.OrderByDescending(move => GetScoreToDepth(board, 2, move) + move.PositionalNudge(board))
            : legalMoves.OrderBy(move => GetScoreToDepth(board, 2, move) - move.PositionalNudge(board));

        bestMove = board.SideToMove == Side.White
            ? orderedLegalMoves.MaxBy(move => GetScoreToDepth(board, depth, move, double.MinValue, double.MaxValue) + move.PositionalNudge(board))
            : orderedLegalMoves.MinBy(move => GetScoreToDepth(board, depth, move, double.MinValue, double.MaxValue) - move.PositionalNudge(board));

        return true;
    }

    private static double GetScoreToDepth(BoardState board, int depth, Move initialMove)
    {
        var initialSideToMove = board.SideToMove;
        using (board.Push(initialMove))
        {
            if (depth == 0)
            {
                return board.GetScore();
            }

            var legalMoves = board.GetLegalMoves().ToList();
            if (legalMoves.Count == 0)
            {
                return board.IsCheckmate()
                    ? initialSideToMove.WinScore()
                    : 0.0;
            }

            return board.SideToMove == Side.White
                ? legalMoves.Max(move => GetScoreToDepth(board, depth - 1, move) + move.PositionalNudge(board))
                : legalMoves.Min(move => GetScoreToDepth(board, depth - 1, move) - move.PositionalNudge(board));
        }
    }

    private static double GetScoreToDepth(BoardState board, int depth, Move initialMove, double alpha, double beta)
    {
        var initialSideToMove = board.SideToMove;
        using (board.Push(initialMove))
        {
            if (depth == 0)
            {
                return board.GetScore();
            }

            var legalMoves = board.GetLegalMoves().ToList();
            if (legalMoves.Count == 0)
            {
                return board.IsCheckmate()
                    ? initialSideToMove.WinScore()
                    : 0.0;
            }

            if (board.SideToMove == Side.White)
            {
                var bestScore = alpha;
                foreach (var move in legalMoves)
                {
                    var score = GetScoreToDepth(board, depth - 1, move, bestScore, beta);
                    bestScore = Math.Max(bestScore, score);
                    if (beta <= bestScore)
                    {
                        break;
                    }
                }

                return bestScore;
            }
            else
            {
                var bestScore = beta;
                foreach (var move in legalMoves)
                {
                    var score = GetScoreToDepth(board, depth - 1, move, alpha, bestScore);
                    bestScore = Math.Min(bestScore, score);
                    if (bestScore <= alpha)
                    {
                        break;
                    }
                }

                return bestScore;
            }
        }
    }
}