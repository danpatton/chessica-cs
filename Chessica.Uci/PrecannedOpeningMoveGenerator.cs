using Chessica.Core;

namespace Chessica.Uci;

public class PrecannedOpeningMoveGenerator : IMoveGenerator
{
    private readonly int _maxOpeningMoveCount;
    private readonly IMoveGenerator _fallbackMoveGenerator;

    private static readonly Dictionary<string, string> PrecannedOpeningMoves = new()
    {
        /* ************* BLACK ************* */

        // Italian game
        ["rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3"] = "e5",
        ["rnbqkbnr/pppp1ppp/8/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq -"] = "Nc6",
        ["r1bqkbnr/pppp1ppp/2n5/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R b KQkq -"] = "Bc5",
        ["r1bqk1nr/pppp1ppp/2n5/2b1p3/2B1P3/2P2N2/PP1P1PPP/RNBQK2R b KQkq -"] = "Nf6",
        ["r1bqk1nr/pppp1ppp/2n5/2b1p3/2B1P3/2N2N2/PPPP1PPP/R1BQK2R b KQkq -"] = "Nf6",
        ["r1bqk1nr/pppp1ppp/2n5/2b1p3/2B1P3/3P1N2/PPP2PPP/RNBQK2R b KQkq -"] = "Nf6",
        ["r1bqk2r/pppp1ppp/2n2n2/2b1p3/2B1P3/2NP1N2/PPP2PPP/R1BQK2R b KQkq -"] = "O-O",
        ["r1bq1rk1/pppp1ppp/2n2n2/2b1p1B1/2B1P3/2NP1N2/PPP2PPP/R2QK2R b KQ -"] = "h6",

        // Kings Indian defence
        ["rnbqkbnr/pppppppp/8/8/3P4/8/PPP1PPPP/RNBQKBNR b KQkq d3"] = "Nf6",
        ["rnbqkb1r/pppppppp/5n2/8/2PP4/8/PP2PPPP/RNBQKBNR b KQkq c3"] = "g6",
        ["rnbqkb1r/pppppp1p/5np1/8/2PP4/2N5/PP2PPPP/R1BQKBNR b KQkq -"] = "Bg7",
        ["rnbqk2r/ppppppbp/5np1/8/2PPP3/2N5/PP3PPP/R1BQKBNR b KQkq e3"] = "O-O",
        ["rnbq1rk1/ppppppbp/5np1/8/2PPP3/2N2N2/PP3PPP/R1BQKB1R b KQ -"] = "Na6",

        /* ************* WHITE ************* */
        
        // Italian game
        ["rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6"] = "Nf3",
        ["r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq -"] = "Bc4",
        ["r1bqk1nr/pppp1ppp/2n5/2b1p3/2B1P3/5N2/PPPP1PPP/RNBQK2R w KQkq -"] = "Nc3",
    };

    public PrecannedOpeningMoveGenerator(int maxOpeningMoveCount, IMoveGenerator fallbackMoveGenerator)
    {
        _maxOpeningMoveCount = maxOpeningMoveCount;
        _fallbackMoveGenerator = fallbackMoveGenerator;
    }

    public bool TryGetBestMove(BoardState boardState, out Move? bestMove)
    {
        if (boardState.FullMoveNumber < _maxOpeningMoveCount)
        {
            var fen = boardState.ToFenString(includeMoveClocks: false);
            if (PrecannedOpeningMoves.TryGetValue(fen, out var moveStr))
            {
                bestMove = boardState.ToMove(moveStr);
                return true;
            }
        }

        return _fallbackMoveGenerator.TryGetBestMove(boardState, out bestMove);
    }

    public Move GetBestMove(BoardState boardState)
    {
        if (!TryGetBestMove(boardState, out var bestMove))
        {
            throw new Exception("No legal moves!");
        }

        return bestMove!;
    }
}