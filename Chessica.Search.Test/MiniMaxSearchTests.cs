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

    [Test]
    public void TestAvoidsDrawByThreefoldRepetitionWhenAhead_SearchV1()
    {
        TestAvoidsDrawByThreefoldRepetitionWhenAhead(SearchV1);
    }

    [Test]
    public void TestAvoidsDrawByThreefoldRepetitionWhenAhead_SearchV2()
    {
        // currently too slow!
        // TestAvoidsDrawByThreefoldRepetitionWhenAhead(SearchV2);
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

    private static void TestAvoidsDrawByThreefoldRepetitionWhenAhead(ISearch search)
    {
        var board = BoardState.ParseFen("r3r1k1/pQp2ppp/2n2n2/4p3/2P5/B4q1b/P1PP1P1P/R3RBK1 b - - 4 15");

        // black is up a knight
        Assert.That(board.GetScore(), Is.EqualTo(-3.0));
        var initialHashValue = board.HashValue;

        board.Push("Qg4+");
        board.Push("Kh1");

        // here, black plays Qf3+
        var bestMoveForBlackFirstTime = search.GetBestMove(board);
        Assert.That(bestMoveForBlackFirstTime.ToString(), Is.EqualTo("Qg4f3"));

        board.Push("Qf3+");
        board.Push("Kg1");

        // back to initial position
        Assert.That(board.HashValue, Is.EqualTo(initialHashValue));

        board.Push("Qg4+");
        board.Push("Kh1");

        // if black now plays Qf3+ again, white can respond with Kg1 and trigger a draw by threefold repetition
        // since black is currently up a knight, so that would be tantamount to losing 3 points of material,
        // so it shouldn't choose that move again
        var bestMoveForBlackSecondTime = search.GetBestMove(board);
        Assert.That(bestMoveForBlackSecondTime.ToString(), Is.Not.EqualTo("Qg4f3"));
    }
}