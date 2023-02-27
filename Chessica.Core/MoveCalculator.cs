using System.Collections.Immutable;

namespace Chessica.Core;

public static class MoveCalculator
{
    private record MoveConstraints(
        BitBoard AttackedSquares,
        BitBoard PinnedPieces,
        BitBoard CheckingPieces,
        BitBoard CheckBlockingSquares,
        IImmutableDictionary<Coord, IImmutableList<Coord>> PinnedPiecePaths);

    public static IEnumerable<Move> CalculateMoves(SideState ownSide, SideState enemySide, out bool inCheck)
    {
        var moveConstraints = CalculateConstraints(ownSide, enemySide);
        var enemyPieces = enemySide.AllPieces;
        var ownPieces = ownSide.AllPieces;
        var moves = new List<Move>();

        var ownKing = ownSide.PiecesOfType(Piece.King).Single();
        inCheck = moveConstraints.AttackedSquares.IsOccupied(ownKing);

        var badSquaresForOwnKing = ownPieces | moveConstraints.AttackedSquares;
        foreach (var moveSequence in ownKing.MoveSequences(Piece.King, ownSide.Side))
        {
            var coord = moveSequence.Single();
            if (badSquaresForOwnKing.IsOccupied(coord)) continue;
            moves.Add(new Move(Piece.King, ownKing, coord, enemyPieces.IsOccupied(coord)));
        }

        if (inCheck && moveConstraints.CheckingPieces.Count > 1)
        {
            // multiple check --> only king moves are legal
            return moves;
        }

        if (!inCheck)
        {
            // castling
            var allPieces = ownPieces | enemyPieces;
            var squaresWeCannotCastleThrough = ownPieces | enemyPieces | moveConstraints.AttackedSquares;
            if (ownSide.CanCastleShort)
            {
                var kingToSquare = ownKing with { File = 6 };
                var rookToSquare = ownKing with { File = 5 };
                var castleThroughSquares = new[] { rookToSquare, kingToSquare };
                if (!castleThroughSquares.Any(squaresWeCannotCastleThrough.IsOccupied))
                {
                    moves.Add(new CastlingMove(ownKing, kingToSquare));
                }
            }

            if (ownSide.CanCastleLong)
            {
                var kingToSquare = ownKing with { File = 2 };
                var rookToSquare = ownKing with { File = 3 };
                var inBetweenSquare = ownKing with { File = 1 };
                var castleThroughSquares = new[] { rookToSquare, kingToSquare };
                if (!castleThroughSquares.Any(squaresWeCannotCastleThrough.IsOccupied) && !allPieces.IsOccupied(inBetweenSquare))
                {
                    moves.Add(new CastlingMove(ownKing, kingToSquare));
                }
            }
        }

        foreach (var pieceType in Enum.GetValues(typeof(Piece)).Cast<Piece>())
        {
            if (pieceType == Piece.King)
            {
                // already enumerated king moves above
                continue;
            }
            if (pieceType == Piece.Pawn)
            {
                foreach (var ownPawn in ownSide.PiecesOfType(Piece.Pawn))
                {
                    var pawnMoves = new List<Move>();
                    var allPieces = ownPieces | enemyPieces;
                    foreach (var moveSequence in ownPawn.MoveSequences(Piece.Pawn, ownSide.Side))
                    {
                        var coord = moveSequence.Single();

                        if (coord.File == ownPawn.File)
                        {
                            if (inCheck && !moveConstraints.CheckBlockingSquares.IsOccupied(coord))
                            {
                                continue;
                            }
                            if (allPieces.IsOccupied(coord)) continue;
                            if (ownPawn.Rank == 1 && coord.Rank == 3)
                            {
                                var inBetweenSquare = ownPawn with {Rank = 2};
                                if (allPieces.IsOccupied(inBetweenSquare)) continue;
                            }
                            if (ownPawn.Rank == 6 && coord.Rank == 4)
                            {
                                var inBetweenSquare = ownPawn with {Rank = 5};
                                if (allPieces.IsOccupied(inBetweenSquare)) continue;
                            }

                            if (coord.Rank is 0 or 7)
                            {
                                foreach (var promotion in new[] { Piece.Queen, Piece.Rook, Piece.Knight, Piece.Bishop })
                                {
                                    pawnMoves.Add(new PromotionMove(ownPawn, coord, false, promotion));
                                }
                            }
                            else
                            {
                                pawnMoves.Add(new Move(Piece.Pawn, ownPawn, coord));
                            }
                        }
                        else
                        {
                            if (enemyPieces.IsOccupied(coord) || enemySide.IsEnPassantSquare(coord))
                            {
                                if (inCheck && !(moveConstraints.CheckingPieces.IsOccupied(coord) || enemySide.IsEnPassantSquare(coord)))
                                {
                                    continue;
                                }
                                if (coord.Rank is 0 or 7)
                                {
                                    foreach (var promotion in new[] { Piece.Queen, Piece.Rook, Piece.Knight, Piece.Bishop })
                                    {
                                        pawnMoves.Add(new PromotionMove(ownPawn, coord, true, promotion));
                                    }
                                }
                                pawnMoves.Add(new Move(Piece.Pawn, ownPawn, coord, true));
                            }
                        }
                    }
                    
                    if (moveConstraints.PinnedPieces.IsOccupied(ownPawn))
                    {
                        var pinnedPiecePath = moveConstraints.PinnedPiecePaths[ownPawn];
                        pawnMoves.RemoveAll(m => !pinnedPiecePath.Contains(m.To));
                    }

                    moves.AddRange(pawnMoves);
                }
            }
            else
            {
                foreach (var ownPiece in ownSide.PiecesOfType(pieceType))
                {
                    var pieceMoves = new List<Move>();
                    foreach (var moveSequence in ownPiece.MoveSequences(pieceType, ownSide.Side))
                    {
                        foreach (var coord in moveSequence)
                        {
                            if (ownPieces.IsOccupied(coord)) break;
                            if (inCheck && !moveConstraints.CheckingPieces.IsOccupied(coord) && !moveConstraints.CheckBlockingSquares.IsOccupied(coord))
                            {
                                if (enemyPieces.IsOccupied(coord)) break;
                                continue;
                            }
                            pieceMoves.Add(new Move(pieceType, ownPiece, coord, enemyPieces.IsOccupied(coord)));
                            if (enemyPieces.IsOccupied(coord)) break;
                        }
                    }

                    if (moveConstraints.PinnedPieces.IsOccupied(ownPiece))
                    {
                        var pinnedPiecePath = moveConstraints.PinnedPiecePaths[ownPiece];
                        pieceMoves.RemoveAll(m => !pinnedPiecePath.Contains(m.To));
                    }

                    moves.AddRange(pieceMoves);
                }
            }
        }

        return moves;
    }
    
    private static MoveConstraints CalculateConstraints(SideState ownSide, SideState enemySide)
    {
        BitBoard attackedSquares = 0;
        BitBoard pinnedPieces = 0;
        BitBoard checkingPieces = 0;
        BitBoard checkBlockingSquares = 0;
        var pinnedPiecePaths = ImmutableDictionary.CreateBuilder<Coord, IImmutableList<Coord>>();
        var enemyPieces = enemySide.AllPieces;
        var ownPieces = ownSide.AllPieces;
        var ownPiecesStack = new Stack<Coord>();
        foreach (var pieceType in Enum.GetValues(typeof(Piece)).Cast<Piece>())
        {
            foreach (var enemyPiece in enemySide.PiecesOfType(pieceType))
            {
                foreach (var moveSequence in enemyPiece.MoveSequences(pieceType, enemySide.Side, attacksOnly: true))
                {
                    ownPiecesStack.Clear();
                    foreach (var coord in moveSequence)
                    {
                        if (enemyPieces.IsOccupied(coord))
                        {
                            if (ownPiecesStack.Count == 0)
                            {
                                // defended squares also count as attacked
                                attackedSquares |= coord;
                            }
                            break;
                        }
                        if (!ownPiecesStack.Any())
                        {
                            attackedSquares |= coord;
                        }

                        if (ownSide.King.IsOccupied(coord))
                        {
                            if (ownPiecesStack.Count == 0)
                            {
                                // we are in check!
                                checkingPieces |= enemyPiece;
                                foreach (var c in moveSequence.TakeWhile(c => c != coord))
                                {
                                    checkBlockingSquares |= c;
                                }
                            }
                            else if (ownPiecesStack.Count == 1)
                            {
                                var pinnedPiece = ownPiecesStack.Peek();
                                pinnedPieces |= pinnedPiece;

                                var pinnedPiecePath = new[] { enemyPiece }
                                    .Concat(moveSequence.TakeWhile(m => !(ownPieces.IsOccupied(m) && m != pinnedPiece))).ToImmutableArray();

                                // impossible to be pinned by more than one piece, hence .Add
                                pinnedPiecePaths.Add(pinnedPiece, pinnedPiecePath);
                            }

                            // note we don't break here -- need to keep going "through" the king
                        }
                        else if (ownPieces.IsOccupied(coord))
                        {
                            if (ownPiecesStack.Any())
                            {
                                break;
                            }

                            ownPiecesStack.Push(coord);
                        }
                    }
                }
            }
        }
        return new MoveConstraints(
            attackedSquares, pinnedPieces, checkingPieces, checkBlockingSquares, pinnedPiecePaths.ToImmutable());
    }

    public static IEnumerable<Move> CalculatePseudoLegalMoves(SideState ownSide, SideState enemySide)
    {
        if (ownSide.King == 0)
        {
            // we are kingless => game is already over, there are no pseudo-legal moves
            return Array.Empty<Move>();
        }
        var attackedSquares = GetAttackedSquares(ownSide, enemySide);
        var ownKing = ownSide.King.Single();
        var inCheck = attackedSquares.IsOccupied(ownKing);
        var enemyPieces = enemySide.AllPieces;
        var ownPieces = ownSide.AllPieces;

        var moves = new List<Move>();

        var badSquaresForOwnKing = ownPieces | attackedSquares;
        foreach (var moveSequence in ownKing.MoveSequences(Piece.King, ownSide.Side))
        {
            var coord = moveSequence.Single();
            if (badSquaresForOwnKing.IsOccupied(coord)) continue;
            moves.Add(new Move(Piece.King, ownKing, coord, enemyPieces.IsOccupied(coord)));
        }

        if (!inCheck)
        {
            // castling
            var squaresWeCannotCastleThrough = ownPieces | enemyPieces | attackedSquares;
            if (ownSide.CanCastleShort)
            {
                var kingToSquare = ownKing with { File = 6 };
                var rookToSquare = ownKing with { File = 5 };
                var inBetweenSquares = new[] { rookToSquare, kingToSquare };
                if (!inBetweenSquares.Any(squaresWeCannotCastleThrough.IsOccupied))
                {
                    moves.Add(new CastlingMove(ownKing, kingToSquare));
                }
            }

            if (ownSide.CanCastleLong)
            {
                var kingToSquare = ownKing with { File = 2 };
                var rookToSquare = ownKing with { File = 3 };
                var inBetweenSquares = new[] { rookToSquare, kingToSquare };
                if (!inBetweenSquares.Any(squaresWeCannotCastleThrough.IsOccupied))
                {
                    moves.Add(new CastlingMove(ownKing, kingToSquare));
                }
            }
        }

        foreach (var pieceType in Enum.GetValues(typeof(Piece)).Cast<Piece>())
        {
            if (pieceType == Piece.King)
            {
                // already enumerated king moves above
                continue;
            }
            if (pieceType == Piece.Pawn)
            {
                foreach (var ownPawn in ownSide.PiecesOfType(Piece.Pawn))
                {
                    var pawnMoves = new List<Move>();
                    var allPieces = ownPieces | enemyPieces;
                    foreach (var moveSequence in ownPawn.MoveSequences(Piece.Pawn, ownSide.Side))
                    {
                        var coord = moveSequence.Single();

                        if (coord.File == ownPawn.File)
                        {
                            if (allPieces.IsOccupied(coord)) continue;
                            if (ownPawn.Rank == 1 && coord.Rank == 3)
                            {
                                var inBetweenSquare = ownPawn with {Rank = 2};
                                if (allPieces.IsOccupied(inBetweenSquare)) continue;
                            }
                            if (ownPawn.Rank == 6 && coord.Rank == 4)
                            {
                                var inBetweenSquare = ownPawn with {Rank = 5};
                                if (allPieces.IsOccupied(inBetweenSquare)) continue;
                            }

                            if (coord.Rank is 0 or 7)
                            {
                                foreach (var promotion in new[] { Piece.Queen, Piece.Rook, Piece.Knight, Piece.Bishop })
                                {
                                    pawnMoves.Add(new PromotionMove(ownPawn, coord, false, promotion));
                                }
                            }
                            else
                            {
                                pawnMoves.Add(new Move(Piece.Pawn, ownPawn, coord));
                            }
                        }
                        else
                        {
                            if (enemyPieces.IsOccupied(coord) || enemySide.IsEnPassantSquare(coord))
                            {
                                if (coord.Rank is 0 or 7)
                                {
                                    foreach (var promotion in new[] { Piece.Queen, Piece.Rook, Piece.Knight, Piece.Bishop })
                                    {
                                        pawnMoves.Add(new PromotionMove(ownPawn, coord, true, promotion));
                                    }
                                }
                                pawnMoves.Add(new Move(Piece.Pawn, ownPawn, coord, true));
                            }
                        }
                    }
                    
                    moves.AddRange(pawnMoves);
                }
            }
            else
            {
                foreach (var ownPiece in ownSide.PiecesOfType(pieceType))
                {
                    var pieceMoves = new List<Move>();
                    foreach (var moveSequence in ownPiece.MoveSequences(pieceType, ownSide.Side))
                    {
                        foreach (var coord in moveSequence)
                        {
                            if (ownPieces.IsOccupied(coord)) break;
                            pieceMoves.Add(new Move(pieceType, ownPiece, coord, enemyPieces.IsOccupied(coord)));
                            if (enemyPieces.IsOccupied(coord)) break;
                        }
                    }

                    moves.AddRange(pieceMoves);
                }
            }
        }

        return moves;
    }

    private static BitBoard GetAttackedSquares(SideState ownSide, SideState enemySide)
    {
        BitBoard attackedSquares = 0;
        var ownPiecesExceptKing = ownSide.AllPieces & ~ownSide.King;
        var enemyPieces = enemySide.AllPieces;
        foreach (var pieceType in Enum.GetValues(typeof(Piece)).Cast<Piece>())
        {
            foreach (var enemyPiece in enemySide.PiecesOfType(pieceType))
            {
                foreach (var moveSequence in enemyPiece.MoveSequences(pieceType, enemySide.Side, attacksOnly: true))
                {
                    foreach (var coord in moveSequence)
                    {
                        attackedSquares |= coord;
                        if (enemyPieces.IsOccupied(coord) || ownPiecesExceptKing.IsOccupied(coord))
                        {
                            break;
                        }
                    }
                }
            }
        }

        return attackedSquares;
    }
}