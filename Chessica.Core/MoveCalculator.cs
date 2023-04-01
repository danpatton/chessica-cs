namespace Chessica.Core;

public static class MoveCalculator
{
    private record MoveConstraints(
        BitBoard AttackedSquares,
        BitBoard CheckingPieces,
        BitBoard CheckBlockingSquares,
        BitBoard DiagonalPins,
        BitBoard OrthogonalPins);

    public static IEnumerable<Move> CalculateMoves(SideState ownSide, SideState enemySide, out bool inCheck)
    {
        var moveConstraints = CalculateConstraints(ownSide, enemySide);
        var potentialChecks = CalculatePotentialChecks(enemySide, ownSide);
        var enemyPieces = enemySide.AllPieces;
        var ownPieces = ownSide.AllPieces;
        var pins = moveConstraints.DiagonalPins | moveConstraints.OrthogonalPins;

        var ownKing = ownSide.PiecesOfType(Piece.King).Single();
        inCheck = moveConstraints.CheckingPieces.Any;

        var kingMoves = BitBoard.KingsMoveMask(ownKing) & ~(ownPieces | moveConstraints.AttackedSquares);
        var moves = kingMoves
            .Select(coord => new Move(Piece.King, ownKing, coord, enemyPieces.IsOccupied(coord)))
            .ToList();

        if (moveConstraints.CheckingPieces.Count > 1)
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

        foreach (var sliderPiece in new[] { Piece.Queen, Piece.Rook, Piece.Bishop })
        {
            var checks = potentialChecks.From(sliderPiece);
            foreach (var ownPiece in ownSide.PiecesOfType(sliderPiece))
            {
                var pieceMoves = new List<Move>();
                foreach (var moveSequence in ownPiece.MoveSequences(sliderPiece, ownSide.Side))
                {
                    foreach (var coord in moveSequence)
                    {
                        if (ownPieces.IsOccupied(coord)) break;
                        if (inCheck && !moveConstraints.CheckingPieces.IsOccupied(coord) &&
                            !moveConstraints.CheckBlockingSquares.IsOccupied(coord))
                        {
                            if (enemyPieces.IsOccupied(coord)) break;
                            continue;
                        }

                        pieceMoves.Add(new Move(sliderPiece, ownPiece, coord, enemyPieces.IsOccupied(coord),
                            checks.IsOccupied(coord)));
                        if (enemyPieces.IsOccupied(coord)) break;
                    }
                }

                if (moveConstraints.DiagonalPins.IsOccupied(ownPiece))
                {
                    var allowedMoves = BitBoard.BishopsMoveMask(ownPiece) & moveConstraints.DiagonalPins;
                    pieceMoves.RemoveAll(m => !allowedMoves.IsOccupied(m.To));
                }
                else if (moveConstraints.OrthogonalPins.IsOccupied(ownPiece))
                {
                    var allowedMoves = BitBoard.RooksMoveMask(ownPiece) & moveConstraints.OrthogonalPins;
                    pieceMoves.RemoveAll(m => !allowedMoves.IsOccupied(m.To));
                }

                moves.AddRange(pieceMoves);
            }
        }

        foreach (var ownKnight in ownSide.Knights)
        {
            if (pins.IsOccupied(ownKnight))
            {
                // pinned knights can't move at all
                continue;
            }

            var checks = potentialChecks.From(Piece.Knight);
            var knightMoves = ownKnight.KnightMovesMask();
            foreach (var coord in knightMoves)
            {
                if (ownPieces.IsOccupied(coord)) continue;
                if (inCheck && !moveConstraints.CheckingPieces.IsOccupied(coord) &&
                    !moveConstraints.CheckBlockingSquares.IsOccupied(coord)) continue;
                moves.Add(new Move(Piece.Knight, ownKnight, coord, enemyPieces.IsOccupied(coord),
                    checks.IsOccupied(coord)));
            }
        }

        foreach (var ownPawn in ownSide.PiecesOfType(Piece.Pawn))
        {
            var pawnMoves = new List<Move>();
            var allPieces = ownPieces | enemyPieces;
            foreach (var moveSequence in ownPawn.MoveSequences(Piece.Pawn, ownSide.Side))
            {
                var coord = moveSequence.Single();

                var isCapture = coord.File != ownPawn.File;
                if (isCapture)
                {
                    if (enemySide.IsEnPassantSquare(coord) && ownPawn.Rank == ownKing.Rank)
                    {
                        // weird edge case; "partially" pinned pawn (ep capture reveals rook/queen check on same rank)
                        var enemyRooksAndQueens = enemySide.Rooks | enemySide.Queens;
                        var dir = ownKing.File > ownPawn.File ? -1 : 1;
                        var canCaptureEp = true;
                        for (var file = ownKing.File + dir; file is >= 0 and < 8; file += dir)
                        {
                            var c = ownKing with { File = file };
                            if (enemyRooksAndQueens.IsOccupied(c))
                            {
                                canCaptureEp = false;
                                break;
                            }

                            if (c.File != coord.File && c.File != ownPawn.File && allPieces.IsOccupied(c))
                            {
                                break;
                            }
                        }

                        if (!canCaptureEp)
                        {
                            continue;
                        }
                    }
                    if (enemyPieces.IsOccupied(coord) || enemySide.IsEnPassantSquare(coord))
                    {
                        if (inCheck && enemySide.IsEnPassantSquare(coord))
                        {
                            // ep capture needs to get us out of check
                            var pawnWeAreCapturing = coord with { Rank = ownPawn.Rank };
                            if (!moveConstraints.CheckingPieces.IsOccupied(pawnWeAreCapturing) &&
                                !moveConstraints.CheckBlockingSquares.IsOccupied(pawnWeAreCapturing))
                            {
                                continue;
                            }
                        }
                        if (inCheck && !(moveConstraints.CheckingPieces.IsOccupied(coord) || enemySide.IsEnPassantSquare(coord)))
                        {
                            continue;
                        }
                        if (coord.Rank is 0 or 7)
                        {
                            pawnMoves.AddRange(new[]
                            {
                                new PromotionMove(ownPawn, coord, Piece.Queen, true, potentialChecks.QueenMask.IsOccupied(coord)),
                                new PromotionMove(ownPawn, coord, Piece.Rook, true, potentialChecks.RookMask.IsOccupied(coord)),
                                new PromotionMove(ownPawn, coord, Piece.Knight, true, potentialChecks.KnightMask.IsOccupied(coord)),
                                new PromotionMove(ownPawn, coord, Piece.Bishop, true, potentialChecks.BishopMask.IsOccupied(coord))
                            });
                        }
                        else
                        {
                            pawnMoves.Add(new Move(Piece.Pawn, ownPawn, coord, true, potentialChecks.PawnMask.IsOccupied(coord)));
                        }
                    }
                }
                else
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
                        pawnMoves.AddRange(new[]
                        {
                            new PromotionMove(ownPawn, coord, Piece.Queen, potentialChecks.QueenMask.IsOccupied(coord)),
                            new PromotionMove(ownPawn, coord, Piece.Rook, potentialChecks.RookMask.IsOccupied(coord)),
                            new PromotionMove(ownPawn, coord, Piece.Knight, potentialChecks.KnightMask.IsOccupied(coord)),
                            new PromotionMove(ownPawn, coord, Piece.Bishop, potentialChecks.BishopMask.IsOccupied(coord))
                        });
                    }
                    else
                    {
                        pawnMoves.Add(new Move(Piece.Pawn, ownPawn, coord, isCheck: potentialChecks.PawnMask.IsOccupied(coord)));
                    }
                }
            }

            if (moveConstraints.DiagonalPins.IsOccupied(ownPawn))
            {
                pawnMoves.RemoveAll(m => !moveConstraints.DiagonalPins.IsOccupied(m.To));
            }
            else if (moveConstraints.OrthogonalPins.IsOccupied(ownPawn))
            {
                pawnMoves.RemoveAll(m => !moveConstraints.OrthogonalPins.IsOccupied(m.To));
            }

            moves.AddRange(pawnMoves);
        }

        return moves;
    }

    private static MoveConstraints CalculateConstraints(SideState ownSide, SideState enemySide)
    {
        BitBoard attackedSquares = 0ul;
        BitBoard checkingPieces = 0ul;
        BitBoard checkBlockingSquares = 0ul;
        var ownKing = ownSide.King.Single;
        var anyPiecesExceptOwnKing = (ownSide.AllPieces | enemySide.AllPieces) & ~ownSide.King;
        foreach (var pieceType in Enum.GetValues(typeof(Piece)).Cast<Piece>())
        {
            foreach (var enemyPiece in enemySide.PiecesOfType(pieceType))
            {
                foreach (var moveSequence in enemyPiece.MoveSequences(pieceType, enemySide.Side, attacksOnly: true))
                {
                    BitBoard attackPath = 0ul;
                    foreach (var coord in moveSequence)
                    {
                        if (ownKing == coord)
                        {
                            // we are in check!
                            checkingPieces |= enemyPiece;
                            checkBlockingSquares |= attackPath;
                            // note we don't break here -- need to keep going "through" the king
                        }
                        attackPath |= coord;
                        if (anyPiecesExceptOwnKing.IsOccupied(coord))
                        {
                            break;
                        }
                    }

                    attackedSquares |= attackPath;
                }
            }
        }

        var diagonalPins = GetDiagonalPins(ownSide, enemySide);
        var orthogonalPins = GetOrthogonalPins(ownSide, enemySide);
        return new MoveConstraints(
            attackedSquares, checkingPieces, checkBlockingSquares, diagonalPins, orthogonalPins);
    }

    public static IEnumerable<Move> CalculatePseudoLegalMoves(SideState ownSide, SideState enemySide)
    {
        if (ownSide.King == 0)
        {
            // we are kingless => game is already over, there are no pseudo-legal moves
            return Array.Empty<Move>();
        }
        var attackedSquares = GetAttackedSquares(ownSide, enemySide);
        var ownKing = ownSide.King.Single;
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
                                    pawnMoves.Add(new PromotionMove(ownPawn, coord, promotion, false));
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
                                        pawnMoves.Add(new PromotionMove(ownPawn, coord, promotion, true));
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

    private static BitBoard GetDiagonalPins(SideState ownSide, SideState enemySide)
    {
        var ownKing = ownSide.King.Single;
        var mask = BitBoard.BishopsMoveMask(ownKing);
        var pinners = (enemySide.Bishops | enemySide.Queens) & mask;
        BitBoard pinMask = 0ul;
        foreach (var pinner in pinners)
        {
            var pinPath = mask & BitBoard.BoundingBoxMask(pinner, ownKing) & ~ownSide.King;
            var ownPiecesOnPath = ownSide.AllPieces & pinPath;
            var enemyPiecesOnPath = enemySide.AllPieces & pinPath;
            if (ownPiecesOnPath.Count == 1 && enemyPiecesOnPath.Count == 1)
            {
                pinMask |= pinPath;
            }
        }
        return pinMask;
    }

    private static BitBoard GetOrthogonalPins(SideState ownSide, SideState enemySide)
    {
        var ownKing = ownSide.King.Single;
        var mask = BitBoard.RooksMoveMask(ownKing);
        var pinners = (enemySide.Rooks | enemySide.Queens) & mask;
        BitBoard pinMask = 0ul;
        foreach (var pinner in pinners)
        {
            var pinPath = mask & BitBoard.BoundingBoxMask(pinner, ownKing) & ~ownSide.King;
            var ownPiecesOnPath = ownSide.AllPieces & pinPath;
            var enemyPiecesOnPath = enemySide.AllPieces & pinPath;
            if (ownPiecesOnPath.Count == 1 && enemyPiecesOnPath.Count == 1)
            {
                pinMask |= pinPath;
            }
        }
        return pinMask;
    }

    public static PotentialCheckMask CalculatePotentialChecks(SideState kingSide, SideState attackingSide)
    {
        var king = kingSide.King.Single;
        var kingSidePieces = kingSide.AllPieces;
        var attackingPieces = attackingSide.AllPieces;

        BitBoard pawnMask = 0;
        BitBoard bishopMask = 0;
        BitBoard rookMask = 0;
        var knightMask = king.KnightMovesMask();

        var pawnAttackingRank = kingSide.Side == Side.White ? king.Rank + 1 : king.Rank - 1;
        if (king.File > 0) pawnMask |= new Coord(king.File - 1, pawnAttackingRank);
        if (king.File < 7) pawnMask |= new Coord(king.File + 1, pawnAttackingRank);

        foreach (var moveSequence in king.MoveSequences(Piece.Bishop, kingSide.Side))
        {
            foreach (var coord in moveSequence)
            {
                if (attackingPieces.IsOccupied(coord)) break;
                bishopMask |= coord;
                if (kingSidePieces.IsOccupied(coord)) break;
            }
        }

        foreach (var moveSequence in king.MoveSequences(Piece.Rook, kingSide.Side))
        {
            foreach (var coord in moveSequence)
            {
                if (attackingPieces.IsOccupied(coord)) break;
                rookMask |= coord;
                if (kingSidePieces.IsOccupied(coord)) break;
            }
        }

        return new PotentialCheckMask
        {
            PawnMask = pawnMask,
            BishopMask = bishopMask,
            KnightMask = knightMask,
            RookMask = rookMask
        };
    }
}