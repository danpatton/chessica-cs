namespace Chessica.Core.Test;

[TestFixture]
public class BitBoardTests
{
    [Test]
    public void TestEnumeration()
    {
        BitBoard bb = 0;
        Coord a4 = "a4";
        Coord b5 = "b5";
        bb |= a4;
        bb |= b5;
        Assert.That(bb, Has.Count.EqualTo(2));
        var coords = bb.ToArray();
        Assert.That(coords, Has.Length.EqualTo(2));
        Assert.That(coords, Does.Contain(a4));
        Assert.That(coords, Does.Contain(b5));
    }
}