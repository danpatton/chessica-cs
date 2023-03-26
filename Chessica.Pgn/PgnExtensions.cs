namespace Chessica.Pgn;

public static class PgnExtensions
{
    public static string ToPgnString(this PgnGameResult result)
    {
        switch (result)
        {
            case PgnGameResult.WhiteWin:
                return "1-0";
            case PgnGameResult.BlackWin:
                return "0-1";
            case PgnGameResult.Draw:
                return "1/2-1/2";
            case PgnGameResult.Other:
                return "";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}