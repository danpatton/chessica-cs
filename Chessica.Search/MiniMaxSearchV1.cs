using Chessica.Core;

namespace Chessica.Search;

public class MiniMaxSearchV1 : ISearch
{
    private readonly int _maxDepth;
    private readonly Dictionary<long, double>[] _transpositionTables;

    public int CacheHits { get; private set; }
    public int CacheMisses { get; private set; }

    public MiniMaxSearchV1(int maxDepth)
    {
        _maxDepth = maxDepth;
        _transpositionTables = Enumerable.Range(0, maxDepth).Select(_ => new Dictionary<long, double>()).ToArray();
    }

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
            ? orderedLegalMoves.MaxBy(move => GetScoreToDepth(board, _maxDepth, move, double.MinValue, double.MaxValue) + move.PositionalNudge(board))
            : orderedLegalMoves.MinBy(move => GetScoreToDepth(board, _maxDepth, move, double.MinValue, double.MaxValue) - move.PositionalNudge(board));

        return true;
    }

    private double GetScoreToDepth(BoardState board, int depth, Move initialMove)
    {
        var initialSideToMove = board.SideToMove;
        using (board.Push(initialMove))
        {
            if (depth == 0)
            {
                return board.GetScore();
            }

            var tt = _transpositionTables[depth - 1];
            if (tt.TryGetValue(board.HashValue, out var cachedScore))
            {
                ++CacheHits;
                return cachedScore;
            }

            ++CacheMisses;

            var legalMoves = board.GetLegalMoves().ToList();
            if (legalMoves.Count == 0)
            {
                return board.IsCheckmate()
                    ? initialSideToMove.WinScore()
                    : 0.0;
            }

            var score = board.SideToMove == Side.White
                ? legalMoves.Max(move => GetScoreToDepth(board, depth - 1, move) + move.PositionalNudge(board))
                : legalMoves.Min(move => GetScoreToDepth(board, depth - 1, move) - move.PositionalNudge(board));

            tt.Add(board.HashValue, score);

            return score;
        }
    }

    private double GetScoreToDepth(BoardState board, int depth, Move initialMove, double alpha, double beta)
    {
        var initialSideToMove = board.SideToMove;
        using (board.Push(initialMove))
        {
            if (depth == 0)
            {
                return board.GetScore();
            }

            var tt = _transpositionTables[depth - 1];
            if (tt.TryGetValue(board.HashValue, out var cachedScore))
            {
                ++CacheHits;
                return cachedScore;
            }

            ++CacheMisses;

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

                tt.Add(board.HashValue, bestScore);
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

                tt.Add(board.HashValue, bestScore);
                return bestScore;
            }
        }
    }
}