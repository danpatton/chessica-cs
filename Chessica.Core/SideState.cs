using System.Diagnostics;
using Optional;

namespace Chessica.Core;

public class SideState
{
    public Side Side { get; }

    public BitBoard King;
    public BitBoard Queens;
    public BitBoard Rooks;
    public BitBoard Knights;
    public BitBoard Bishops;
    public BitBoard Pawns;

    public bool CanCastleLong { get; private set; }
    public bool CanCastleShort { get; private set; }

    private Option<Coord> _enPassantSquare = Option.None<Coord>();

    public SideState(Side side, bool canCastleLong = true, bool canCastleShort = true)
    {
        Side = side;
        CanCastleLong = canCastleLong;
        CanCastleShort = canCastleShort;
    }

    public SideState Clone()
    {
        var clone = new SideState(Side, CanCastleLong, CanCastleShort)
        {
            King = King,
            Queens = Queens,
            Rooks = Rooks,
            Knights = Knights,
            Bishops = Bishops,
            Pawns = Pawns
        };
        _enPassantSquare.MatchSome(clone.SetEnPassantSquare);
        return clone;
    }

    public bool CanCastle => CanCastleLong || CanCastleShort;

    public bool IsEnPassantSquare(Coord coord) => _enPassantSquare.Match(c => c.Equals(coord), () => false);

    public void SetEnPassantSquare(Coord coord)
    {
        _enPassantSquare = Option.Some(coord);
    }

    public Option<Coord> EnPassantSquare => _enPassantSquare;

    public void ClearEnPassantSquare()
    {
        if (_enPassantSquare.HasValue)
        {
            _enPassantSquare = Option.None<Coord>();
        }
    }

    public void SetCastlingRights(bool canCastleLong, bool canCastleShort)
    {
        CanCastleLong = canCastleLong;
        CanCastleShort = canCastleShort;
    }

    public double GetSimpleMaterialScore()
    {
        return King.Count * Piece.King.Value() +
               Queens.Count * Piece.Queen.Value()
               + Rooks.Count * Piece.Rook.Value()
               + Knights.Count * Piece.Knight.Value()
               + Bishops.Count * Piece.Bishop.Value()
               + Pawns.Count * Piece.Pawn.Value();
    }

    public void ApplyMove(Piece pieceType, Coord from, Coord to)
    {
        switch (pieceType)
        {
            case Piece.King:
                King.Move(from, to);
                CanCastleShort = false;
                CanCastleLong = false;
                break;
            case Piece.Queen:
                Queens.Move(from, to);
                break;
            case Piece.Rook:
                Rooks.Move(from, to);
                var startingRank = Side == Side.White ? 0 : 7;
                if (from.Rank == startingRank)
                {
                    switch (from.File)
                    {
                        case 7:
                            CanCastleShort = false;
                            break;
                        case 0:
                            CanCastleLong = false;
                            break;
                    }
                }
                break;
            case Piece.Knight:
                Knights.Move(from, to);
                break;
            case Piece.Bishop:
                Bishops.Move(from, to);
                break;
            case Piece.Pawn:
                Pawns.Move(from, to);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void UndoMove(Piece pieceType, Coord from, Coord to)
    {
        switch (pieceType)
        {
            case Piece.King:
                King.Move(to, from);
                break;
            case Piece.Queen:
                Queens.Move(to, from);
                break;
            case Piece.Rook:
                Rooks.Move(to, from);
                break;
            case Piece.Knight:
                Knights.Move(to, from);
                break;
            case Piece.Bishop:
                Bishops.Move(to, from);
                break;
            case Piece.Pawn:
                Pawns.Move(to, from);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void ApplyPromotion(Coord from, Coord to, Piece promotion)
    {
        Pawns &= ~(BitBoard)from;
        switch (promotion)
        {
            case Piece.Queen:
                Queens |= to;
                break;
            case Piece.Rook:
                Rooks |= to;
                break;
            case Piece.Knight:
                Knights |= to;
                break;
            case Piece.Bishop:
                Bishops |= to;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void UndoPromotion(Coord from, Coord to, Piece promotion)
    {
        Pawns |= from;
        switch (promotion)
        {
            case Piece.Queen:
                Queens &= ~(BitBoard)to;
                break;
            case Piece.Rook:
                Rooks &= ~(BitBoard)to;
                break;
            case Piece.Knight:
                Knights &= ~(BitBoard)to;
                break;
            case Piece.Bishop:
                Bishops &= ~(BitBoard)to;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void ApplyCapture(Coord coord)
    {
        var startingRank = Side == Side.White ? 0 : 7;
        if (Rooks.IsOccupied(coord) && coord.Rank == startingRank)
        {
            switch (coord.File)
            {
                case 7:
                    CanCastleShort = false;
                    break;
                case 0:
                    CanCastleLong = false;
                    break;
            }
        }
        // allow king to be "captured" during search (this should never happen as part of game play)
        King &= ~(BitBoard)coord;
        // TODO: better accounting
        Queens &= ~(BitBoard)coord;
        Rooks &= ~(BitBoard)coord;
        Knights &= ~(BitBoard)coord;
        Bishops &= ~(BitBoard)coord;
        Pawns &= ~(BitBoard)coord;
    }

    public void UndoCapture(Piece piece, Coord coord)
    {
        switch (piece)
        {
            case Piece.Pawn:
                Pawns |= coord;
                break;
            case Piece.Bishop:
                Bishops |= coord;
                break;
            case Piece.Knight:
                Knights |= coord;
                break;
            case Piece.Rook:
                Rooks |= coord;
                break;
            case Piece.Queen:
                Queens |= coord;
                break;
            case Piece.King:
                // king can be "captured" during search
                King |= coord;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void ApplyEnPassantCapture(Coord coord)
    {
        var targetPawnRank = Side == Side.White ? 3 : 4;
        var targetPawn = coord with { Rank = (byte)targetPawnRank };
        Pawns &= ~(BitBoard)targetPawn;
    }

    public BitBoard PiecesOfType(Piece pieceType)
    {
        switch (pieceType)
        {
            case Piece.King: return King;
            case Piece.Queen: return Queens;
            case Piece.Rook: return Rooks;
            case Piece.Knight: return Knights;
            case Piece.Bishop: return Bishops;
            case Piece.Pawn: return Pawns;
            default: throw new Exception("WTF");
        }
    }

    public IEnumerable<(Piece Piece, Coord Coord)> GetPieces()
    {
        foreach (var kingCoord in King)
        {
            yield return (Piece.King, kingCoord);
        }
        foreach (var queenCoord in Queens)
        {
            yield return (Piece.Queen, queenCoord);
        }
        foreach (var rookCoord in Rooks)
        {
            yield return (Piece.Rook, rookCoord);
        }
        foreach (var knightCoord in Knights)
        {
            yield return (Piece.Knight, knightCoord);
        }
        foreach (var bishopCoord in Bishops)
        {
            yield return (Piece.Bishop, bishopCoord);
        }
        foreach (var pawnCoord in Pawns)
        {
            yield return (Piece.Pawn, pawnCoord);
        }
    }

    public Option<Piece> GetPiece(Coord coord)
    {
        if (Pawns.IsOccupied(coord))
        {
            var others = Bishops | Knights | Rooks | Queens | King;
            Debug.Assert(!others.IsOccupied(coord), $"Multiple {Side} pieces occupying {coord} !?");
            return Option.Some(Piece.Pawn);
        }
        if (Bishops.IsOccupied(coord))
        {
            var others = Pawns | Knights | Rooks | Queens | King;
            Debug.Assert(!others.IsOccupied(coord), $"Multiple {Side} pieces occupying {coord} !?");
            return Option.Some(Piece.Bishop);
        }
        if (Knights.IsOccupied(coord))
        {
            var others = Pawns | Bishops | Rooks | Queens | King;
            Debug.Assert(!others.IsOccupied(coord), $"Multiple {Side} pieces occupying {coord} !?");
            return Option.Some(Piece.Knight);
        }
        if (Rooks.IsOccupied(coord))
        {
            var others = Pawns | Bishops | Knights | Queens | King;
            Debug.Assert(!others.IsOccupied(coord), $"Multiple {Side} pieces occupying {coord} !?");
            return Option.Some(Piece.Rook);
        }
        if (Queens.IsOccupied(coord))
        {
            var others = Pawns | Bishops | Knights | Rooks | King;
            Debug.Assert(!others.IsOccupied(coord), $"Multiple {Side} pieces occupying {coord} !?");
            return Option.Some(Piece.Queen);
        }
        if (King.IsOccupied(coord))
        {
            var others = Pawns | Bishops | Knights | Rooks | Queens;
            Debug.Assert(!others.IsOccupied(coord), $"Multiple {Side} pieces occupying {coord} !?");
            return Option.Some(Piece.King);
        }
        return Option.None<Piece>();
    }

    public BitBoard AllPieces => King | Queens | Rooks | Knights | Bishops | Pawns;

    public static SideState StartingPosition(Side side)
    {
        switch (side)
        {
            case Side.White:
                return new SideState(side)
                {
                    King = 0x10,
                    Queens = 0x08,
                    Rooks = 0x81,
                    Knights = 0x42,
                    Bishops = 0x24,
                    Pawns = 0xff00
                };
            case Side.Black:
                return new SideState(side)
                {
                    King = 0x1000000000000000,
                    Queens = 0x0800000000000000,
                    Rooks = 0x8100000000000000,
                    Knights = 0x4200000000000000,
                    Bishops = 0x2400000000000000,
                    Pawns = 0xff000000000000
                };
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void AddPiece(Piece piece, Coord coord)
    {
        switch (piece)
        {
            case Piece.Pawn:
                Pawns |= coord;
                break;
            case Piece.Bishop:
                Bishops |= coord;
                break;
            case Piece.Knight:
                Knights |= coord;
                break;
            case Piece.Rook:
                Rooks |= coord;
                break;
            case Piece.Queen:
                Queens |= coord;
                break;
            case Piece.King:
                King |= coord;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SerialiseTo(BinaryWriter writer)
    {
        writer.Write(King);
        writer.Write(Queens);
        writer.Write(Rooks);
        writer.Write(Knights);
        writer.Write(Bishops);
        writer.Write(Pawns);
        _enPassantSquare.Match(
            coord => writer.Write(coord),
            () => writer.Write(0));
        writer.Write((int)Side);
        writer.Write(CanCastleLong);
        writer.Write(CanCastleShort);
    }
}