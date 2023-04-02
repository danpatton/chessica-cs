namespace Chessica.Core;

public static class IntMove
{
    public static uint Move(Piece piece, Coord from, Coord to, bool isCapture, bool isCheck)
    {
        return (uint)piece
               | (uint)from.Ordinal << 3
               | (uint)to.Ordinal << 9
               | (isCapture ? 1u : 0u) << 15
               | (isCheck ? 1u : 0u) << 16;
    }

    public static bool IsCastlingMove(this uint move)
    {
        return false;
    }

    public static bool IsPromotionMove(this uint move)
    {
        return false;        
    }
}