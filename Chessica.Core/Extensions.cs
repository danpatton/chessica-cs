namespace Chessica.Core;

public static class Extensions
{
    public static Piece ParsePiece(this string s)
    {
        switch (s)
        {
            case "K":
                return Piece.King;
            case "Q":
                return Piece.Queen;
            case "R":
                return Piece.Rook;
            case "N":
                return Piece.Knight;
            case "B":
                return Piece.Bishop;
            case "":
                return Piece.Pawn;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static string Ascii(this Piece piece)
    {
        switch (piece)
        {
            case Piece.King:
                return "K";
            case Piece.Queen:
                return "Q";
            case Piece.Rook:
                return "R";
            case Piece.Knight:
                return "N";
            case Piece.Bishop:
                return "B";
            case Piece.Pawn:
                return "";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static string Unicode(this Piece piece, Side side)
    {
        switch (piece)
        {
            case Piece.King:
                return side == Side.White ? "\u2654" : "\u265A";
            case Piece.Queen:
                return side == Side.White ? "\u2655" : "\u265B";
            case Piece.Rook:
                return side == Side.White ? "\u2656" : "\u265C";
            case Piece.Knight:
                return side == Side.White ? "\u2658" : "\u265E";
            case Piece.Bishop:
                return side == Side.White ? "\u2657" : "\u265D";
            case Piece.Pawn:
                return side == Side.White ? "\u2659" : "\u265F";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static int Value(this Piece piece)
    {
        switch (piece)
        {
            case Piece.King:
                return 1000;
            case Piece.Queen:
                return 9;
            case Piece.Rook:
                return 5;
            case Piece.Knight:
                return 3;
            case Piece.Bishop:
                return 3;
            case Piece.Pawn:
                return 1;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static string ToFenChar(this Piece piece, Side side)
    {
        switch (piece)
        {
            case Piece.King:
                return side == Side.White ? "K" : "k";
            case Piece.Queen:
                return side == Side.White ? "Q" : "q";
            case Piece.Rook:
                return side == Side.White ? "R" : "r";
            case Piece.Knight:
                return side == Side.White ? "N" : "n";
            case Piece.Bishop:
                return side == Side.White ? "B" : "b";
            case Piece.Pawn:
                return side == Side.White ? "P" : "p";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static (Piece Piece, Side Side) ParseFenChar(this char fenChar)
    {
        switch (fenChar)
        {
            case 'K':
                return (Piece.King, Side.White);
            case 'Q':
                return (Piece.Queen, Side.White);
            case 'R':
                return (Piece.Rook, Side.White);
            case 'N':
                return (Piece.Knight, Side.White);
            case 'B':
                return (Piece.Bishop, Side.White);
            case 'P':
                return (Piece.Pawn, Side.White);
            case 'k':
                return (Piece.King, Side.Black);
            case 'q':
                return (Piece.Queen, Side.Black);
            case 'r':
                return (Piece.Rook, Side.Black);
            case 'n':
                return (Piece.Knight, Side.Black);
            case 'b':
                return (Piece.Bishop, Side.Black);
            case 'p':
                return (Piece.Pawn, Side.Black);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static double WinScore(this Side side)
    {
        return side switch
        {
            Side.White => double.MaxValue,
            Side.Black => double.MinValue,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}