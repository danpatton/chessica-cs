using Chessica.Core;

namespace Chessica.Search.Test;

public class MiniMaxSearchTests
{
    private const int MaxDepth = 3;

    [Test]
    public void TestFindsMateInTwo()
    {
        var boardState = BoardState.ParseFen("kr5r/p7/8/8/8/1R2Q3/6q1/KR6 w - - 0 1");
        var bestMove = MiniMaxSearch.GetBestMove(boardState, MaxDepth);
        Assert.That(bestMove.ToString(), Is.EqualTo("Qe3xa7"));
        boardState.Push(bestMove);
        boardState.Push(boardState.ToMove("Kxa7"));
        bestMove = MiniMaxSearch.GetBestMove(boardState, MaxDepth);
        boardState.Push(bestMove);
        Assert.That(boardState.IsCheckmate(), Is.True);
    }

    [Test]
    public void TestFindsMateInThree()
    {
        var boardState = BoardState.ParseFen("4rrk1/pppb4/7p/3P2pq/3Q4/P5P1/1PP2nKP/R3RNN1 b - - 0 1");
        var bestMove = MiniMaxSearch.GetBestMove(boardState, MaxDepth);
        Assert.That(bestMove.ToString(), Is.EqualTo("Bd7h3"));
        boardState.Push(bestMove);
        boardState.Push(boardState.ToMove("Nxh3"));
        bestMove = MiniMaxSearch.GetBestMove(boardState, MaxDepth);
        Assert.That(bestMove.ToString(), Is.EqualTo("Qh5f3"));
        boardState.Push(bestMove);
        boardState.Push(boardState.ToMove("Kg1"));
        bestMove = MiniMaxSearch.GetBestMove(boardState, MaxDepth);
        Assert.That(bestMove.ToString(), Is.EqualTo("Qf3h1"));
        boardState.Push(bestMove);
        Assert.That(boardState.IsCheckmate(), Is.True);
    }
}