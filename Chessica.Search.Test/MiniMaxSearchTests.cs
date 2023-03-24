using Chessica.Core;

namespace Chessica.Search.Test;

public class MiniMaxSearchTests
{
    private static readonly MiniMaxSearchV1 SearchV1 = new MiniMaxSearchV1(3);
    private static readonly MiniMaxSearchV2 SearchV2 = new MiniMaxSearchV2(4);

    [Test]
    public void TestFindsMateInTwo_SearchV1()
    {
        TestFindsMateInTwo(SearchV1);
    }

    [Test]
    public void TestFindsMateInTwo_SearchV2()
    {
        TestFindsMateInTwo(SearchV2);
    }

    [Test]
    public void TestFindsMateInThree_SearchV1()
    {
        TestFindsMateInThree(SearchV1);
    }

    [Test]
    public void TestFindsMateInThree_SearchV2()
    {
        TestFindsMateInThree(SearchV2);
    }

    private static void TestFindsMateInTwo(ISearch search)
    {
        var boardState = BoardState.ParseFen("kr5r/p7/8/8/8/1R2Q3/6q1/KR6 w - - 0 1");
        var bestMove = search.GetBestMove(boardState);
        Assert.That(bestMove.ToString(), Is.EqualTo("Qe3xa7"));
        boardState.Push(bestMove);
        boardState.Push(boardState.ToMove("Kxa7"));
        bestMove = search.GetBestMove(boardState);
        boardState.Push(bestMove);
        Assert.That(boardState.IsCheckmate(), Is.True);        
    }

    private static void TestFindsMateInThree(ISearch search)
    {
        var boardState = BoardState.ParseFen("4rrk1/pppb4/7p/3P2pq/3Q4/P5P1/1PP2nKP/R3RNN1 b - - 0 1");
        var bestMove = search.GetBestMove(boardState);
        Assert.That(bestMove.ToString(), Is.EqualTo("Bd7h3"));
        boardState.Push(bestMove);
        boardState.Push(boardState.ToMove("Nxh3"));
        bestMove = search.GetBestMove(boardState);
        Assert.That(bestMove.ToString(), Is.EqualTo("Qh5f3"));
        boardState.Push(bestMove);
        boardState.Push(boardState.ToMove("Kg1"));
        bestMove = search.GetBestMove(boardState);
        Assert.That(bestMove.ToString(), Is.EqualTo("Qf3h1"));
        boardState.Push(bestMove);
        Assert.That(boardState.IsCheckmate(), Is.True);
    }
}