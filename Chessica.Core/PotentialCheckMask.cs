namespace Chessica.Core;

public struct PotentialCheckMask
{
    public BitBoard PawnMask;
    public BitBoard BishopMask;
    public BitBoard KnightMask;
    public BitBoard RookMask;
    public BitBoard DiagonalXrayMask;
    public BitBoard OrthogonalXrayMask;
    public BitBoard QueenMask => BishopMask | RookMask;

    public BitBoard From(Piece pieceType)
    {
        switch (pieceType)
        {
            case Piece.Pawn:
                return PawnMask;
            case Piece.Bishop:
                return BishopMask;
            case Piece.Knight:
                return KnightMask;
            case Piece.Rook:
                return RookMask;
            case Piece.Queen:
                return QueenMask;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}