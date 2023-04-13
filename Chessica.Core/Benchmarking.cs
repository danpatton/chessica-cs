namespace Chessica.Core;

public static class Benchmarking
{
    public static ulong Perft(BoardState board, int depth)
    {
        if (depth == 0) return 1ul;
        if (depth == 1) return (ulong) board.GetLegalMoves().Count();
        var count = 0ul;
        foreach (var move in board.GetLegalMoves())
        {
            using (board.Push(move))
            {
                count += Perft(board, depth - 1);
            }
        }

        return count;
    }

    public static ulong PerftSplit(BoardState board, int depth)
    {
        var moves = board.GetLegalMoves().ToList();
        var count = 0ul;
        foreach (var move in moves)
        {
            using (board.Push(move))
            {
                var p = Perft(board, depth - 1);
                count += p;
                Console.WriteLine($"{move.ToUciString()} moves = {p}");
            }
        }

        return count;
    }
}