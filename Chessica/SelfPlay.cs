using System.Collections.Immutable;
using Chessica.Core;
using Chessica.Pgn;
using Chessica.Uci;

namespace Chessica;

public static class SelfPlay
{
    public static PgnGame Run(IMoveGenerator moveGenerator)
    {
        var moves = new List<PgnMove>();
        var board = BoardState.StartingPosition;
        while (true)
        {
            var side = board.SideToMove;
            var move = moveGenerator.GetBestMove(board);
            var pgnSpec = move!.ToPgnSpec(board);
            board.Push(move);

            var (inCheck, numLegalMoves) = board.GetGameState();
            if (inCheck && numLegalMoves == 0)
            {
                pgnSpec += "#";
            }
            else if (inCheck)
            {
                pgnSpec += "+";
            }

            moves.Add(new PgnMove(side, pgnSpec));

            if (side == Side.White)
            {
                Console.Write($"{board.FullMoveNumber}. {pgnSpec} ");
            }
            else
            {
                Console.WriteLine(pgnSpec);
            }

            if (board.IsDrawByThreefoldRepetition())
            {
                Console.WriteLine(PgnGameResult.Draw.ToPgnString());
                return new PgnGame(
                    ImmutableDictionary<string, string>.Empty,
                    moves.ToImmutableArray(),
                    PgnGameResult.Draw);
            }
            if (numLegalMoves == 0)
            {
                var result = inCheck
                    ? board.SideToMove == Side.White ? PgnGameResult.BlackWin : PgnGameResult.WhiteWin
                    : PgnGameResult.Draw;
                Console.WriteLine(result.ToPgnString());
                return new PgnGame( 
                    ImmutableDictionary<string, string>.Empty,
                    moves.ToImmutableArray(),
                    result);
            }
        }
    }
}