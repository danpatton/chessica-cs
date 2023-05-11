using System.Diagnostics;
using Chessica.Core;

namespace Chessica.Uci;

public class UciMoveGenerator : IMoveGenerator
{
    private readonly string _uciEnginePath;

    public UciMoveGenerator(string uciEnginePath)
    {
        _uciEnginePath = uciEnginePath;
    }

    public bool TryGetBestMove(BoardState boardState, out Move? bestMove)
    {
        var uciMoves = boardState.GetLegalMoves()
            .ToDictionary(m => m.ToUciString(), m => m);

        if (!uciMoves.Any())
        {
            bestMove = null;
            return false;
        }

        var psi = new ProcessStartInfo(_uciEnginePath)
        {
            RedirectStandardOutput = true,
            RedirectStandardInput = true
        };
        var process = Process.Start(psi);
        if (process == null)
        {
            bestMove = null;
            return false;
        }

        process.StandardInput.WriteLine("uci");
        var uciReply = "";
        while (uciReply != "uciok")
        {
            uciReply = process.StandardOutput.ReadLine();
        }

        process.StandardInput.WriteLine("ucinewgame");
        process.StandardInput.WriteLine($"position fen {boardState.ToFenString()}");
        process.StandardInput.WriteLine("go");
        var goReply = process.StandardOutput.ReadLine();
        if (goReply == null)
        {
            bestMove = null;
            return false;
        }

        process.StandardInput.WriteLine("quit");
        if (!process.WaitForExit(100))
        {
            process.Kill();
        }

        var uciMove = goReply.Split(" ")[1];
        return uciMoves.TryGetValue(uciMove, out bestMove);
    }

    public Move GetBestMove(BoardState boardState)
    {
        if (TryGetBestMove(boardState, out var bestMove) && bestMove != null)
        {
            return bestMove;
        }

        throw new Exception("No best move");
    }
}