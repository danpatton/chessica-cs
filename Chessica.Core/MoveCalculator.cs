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
        var allPieces = ownPieces | enemyPieces;
        var pins = moveConstraints.DiagonalPins | moveConstraints.OrthogonalPins;

        var ownKing = ownSide.PiecesOfType(Piece.King).Single;
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
            var squaresWeCannotCastleThrough = ownPieces | enemyPieces | moveConstraints.AttackedSquares;
            if (ownSide.CanCastleShort)
            {
                var kingToSquare = ownKing with { File = 6 };
                var castlingPath = BitBoard.BoundingBoxMask(ownKing, kingToSquare) & ~ownSide.King;
                if (!(castlingPath & squaresWeCannotCastleThrough).Any)
                {
                    moves.Add(new CastlingMove(ownKing, kingToSquare));
                }
            }

            if (ownSide.CanCastleLong)
            {
                var kingToSquare = ownKing with { File = 2 };
                var inBetweenSquare = ownKing with { File = 1 };
                var castlingPath = BitBoard.BoundingBoxMask(ownKing, kingToSquare) & ~ownSide.King;
                if (!(castlingPath & squaresWeCannotCastleThrough).Any &&
                    !allPieces.IsOccupied(inBetweenSquare))
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
                BitBoard allowedMoves = ulong.MaxValue;
                if (moveConstraints.DiagonalPins.IsOccupied(ownPiece))
                {
                    allowedMoves = BitBoard.BishopsMoveMask(ownPiece) & moveConstraints.DiagonalPins;
                }
                else if (moveConstraints.OrthogonalPins.IsOccupied(ownPiece))
                {
                    allowedMoves = BitBoard.RooksMoveMask(ownPiece) & moveConstraints.OrthogonalPins;
                }

                foreach (var moveSequence in ownPiece.MoveSequences(sliderPiece, ownSide.Side))
                {
                    foreach (var coord in moveSequence)
                    {
                        if (!allowedMoves.IsOccupied(coord)) continue;
                        if (ownPieces.IsOccupied(coord)) break;
                        if (inCheck && !moveConstraints.CheckingPieces.IsOccupied(coord) &&
                            !moveConstraints.CheckBlockingSquares.IsOccupied(coord))
                        {
                            if (enemyPieces.IsOccupied(coord)) break;
                            continue;
                        }

                        moves.Add(new Move(sliderPiece, ownPiece, coord, enemyPieces.IsOccupied(coord),
                            checks.IsOccupied(coord)));
                        if (enemyPieces.IsOccupied(coord)) break;
                    }
                }
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

        // ownSide.Side == White => d == 1
        // ownSide.Side == Black => d == -1
        var d = 1 - 2 * (int)ownSide.Side;
        var pawnDoublePushRank = 3 + (int)ownSide.Side;

        var pawnMoves = new List<Move>();

        var diagonallyPinnedPawns = ownSide.Pawns & moveConstraints.DiagonalPins;
        if (diagonallyPinnedPawns.Any)
        {
            foreach (var ownPawn in diagonallyPinnedPawns)
            {
                BitBoard ownPawnBb = ownPawn;
                foreach (var capture in ownPawnBb.PawnCaptureMask(ownSide.Side) & moveConstraints.DiagonalPins &
                                        enemyPieces)
                {
                    pawnMoves.Add(new Move(Piece.Pawn, ownPawn, capture, true,
                        potentialChecks.PawnMask.IsOccupied(capture)));
                }
            }
        }

        var orthogonallyPinnedPawns = ownSide.Pawns & moveConstraints.OrthogonalPins;
        if (orthogonallyPinnedPawns.Any)
        {
            foreach (var ownPawn in orthogonallyPinnedPawns)
            {
                BitBoard ownPawnBb = ownPawn;
                var singlePush = ownPawnBb.PawnPushMask(ownSide.Side) & moveConstraints.OrthogonalPins &
                                 ~enemyPieces;
                if (singlePush.Any)
                {
                    var target = singlePush.Single;
                    pawnMoves.Add(new Move(Piece.Pawn, ownPawn, target, false,
                        potentialChecks.PawnMask.IsOccupied(target)));
                }

                var doublePush = singlePush.PawnPushMask(ownSide.Side) & moveConstraints.OrthogonalPins &
                                 ~enemyPieces & BitBoard.RankMask(pawnDoublePushRank);
                if (doublePush.Any)
                {
                    var target = doublePush.Single;
                    pawnMoves.Add(new Move(Piece.Pawn, ownPawn, target, false,
                        potentialChecks.PawnMask.IsOccupied(target)));
                }
            }
        }

        var unpinnedPawns = ownSide.Pawns & ~pins;
        var pawnPushes = unpinnedPawns.PawnPushMask(ownSide.Side) & ~allPieces;
        foreach (var pawnPush in pawnPushes)
        {
            var pawn = pawnPush with { Rank = pawnPush.Rank - d };
            if (pawnPush.Rank is 0 or 7)
            {
                pawnMoves.Add(new PromotionMove(pawn, pawnPush, Piece.Queen, false,
                    potentialChecks.QueenMask.IsOccupied(pawnPush)));
                pawnMoves.Add(new PromotionMove(pawn, pawnPush, Piece.Rook, false,
                    potentialChecks.RookMask.IsOccupied(pawnPush)));
                pawnMoves.Add(new PromotionMove(pawn, pawnPush, Piece.Knight, false,
                    potentialChecks.QueenMask.IsOccupied(pawnPush)));
                pawnMoves.Add(new PromotionMove(pawn, pawnPush, Piece.Bishop, false,
                    potentialChecks.RookMask.IsOccupied(pawnPush)));
            }
            else
            {
                pawnMoves.Add(new Move(Piece.Pawn, pawn, pawnPush));
            }
        }

        var pawnDoublePushes =
            pawnPushes.PawnPushMask(ownSide.Side) & BitBoard.RankMask(pawnDoublePushRank) & ~allPieces;
        foreach (var doublePawnPush in pawnDoublePushes)
        {
            var pawn = doublePawnPush with { Rank = doublePawnPush.Rank - d - d };
            pawnMoves.Add(new Move(Piece.Pawn, pawn, doublePawnPush));
        }

        var pawnLeftCapturesExcludingEp = unpinnedPawns.PawnLeftCaptureMask(ownSide.Side) & enemySide.AllPieces;
        foreach (var pawnCapture in pawnLeftCapturesExcludingEp)
        {
            var pawn = new Coord(Rank: pawnCapture.Rank - d, File: pawnCapture.File + d);
            if (pawnCapture.Rank is 0 or 7)
            {
                pawnMoves.Add(new PromotionMove(pawn, pawnCapture, Piece.Queen, true,
                    potentialChecks.QueenMask.IsOccupied(pawnCapture)));
                pawnMoves.Add(new PromotionMove(pawn, pawnCapture, Piece.Rook, true,
                    potentialChecks.RookMask.IsOccupied(pawnCapture)));
                pawnMoves.Add(new PromotionMove(pawn, pawnCapture, Piece.Knight, true,
                    potentialChecks.QueenMask.IsOccupied(pawnCapture)));
                pawnMoves.Add(new PromotionMove(pawn, pawnCapture, Piece.Bishop, true,
                    potentialChecks.RookMask.IsOccupied(pawnCapture)));
            }
            else
            {
                pawnMoves.Add(new Move(Piece.Pawn, pawn, pawnCapture, true,
                    potentialChecks.PawnMask.IsOccupied(pawnCapture)));
            }
        }

        var pawnRightCapturesExcludingEp = unpinnedPawns.PawnRightCaptureMask(ownSide.Side) & enemySide.AllPieces;
        foreach (var pawnCapture in pawnRightCapturesExcludingEp)
        {
            var pawn = new Coord(Rank: pawnCapture.Rank - d, File: pawnCapture.File - d);
            if (pawnCapture.Rank is 0 or 7)
            {
                pawnMoves.Add(new PromotionMove(pawn, pawnCapture, Piece.Queen, true,
                    potentialChecks.QueenMask.IsOccupied(pawnCapture)));
                pawnMoves.Add(new PromotionMove(pawn, pawnCapture, Piece.Rook, true,
                    potentialChecks.RookMask.IsOccupied(pawnCapture)));
                pawnMoves.Add(new PromotionMove(pawn, pawnCapture, Piece.Knight, true,
                    potentialChecks.QueenMask.IsOccupied(pawnCapture)));
                pawnMoves.Add(new PromotionMove(pawn, pawnCapture, Piece.Bishop, true,
                    potentialChecks.RookMask.IsOccupied(pawnCapture)));
            }
            else
            {
                pawnMoves.Add(new Move(Piece.Pawn, pawn, pawnCapture, true,
                    potentialChecks.PawnMask.IsOccupied(pawnCapture)));
            }
        }

        enemySide.EnPassantSquare.MatchSome(ep =>
        {
            BitBoard bbEp = ep;
            var potentialCapturers = unpinnedPawns & bbEp.PawnCaptureMask(enemySide.Side);
            var capturersOnKingsRank = potentialCapturers & BitBoard.RankMask(ownKing.Rank);
            var canCaptureEp = true;
            if (capturersOnKingsRank.Count == 1)
            {
                // weird edge case; "partially" pinned pawn (ep capture reveals rook/queen check on same rank)
                var enemyRooksAndQueensOnSameRank =
                    (enemySide.Rooks | enemySide.Queens) & BitBoard.RankMask(ownKing.Rank);
                foreach (var enemyRookOrQueen in enemyRooksAndQueensOnSameRank)
                {
                    var bb = BitBoard.BoundingBoxMask(enemyRookOrQueen, ownKing) & allPieces;
                    if (bb.Count < 5)
                    {
                        canCaptureEp = false;
                        break;
                    }
                }
            }

            if (canCaptureEp)
            {
                var isCheck = potentialChecks.PawnMask.IsOccupied(ep);
                foreach (var ownPawn in potentialCapturers)
                {
                    pawnMoves.Add(new Move(Piece.Pawn, ownPawn, ep, true, isCheck));
                }
            }
        });

        if (inCheck)
        {
            var checkDodgers = moveConstraints.CheckingPieces | moveConstraints.CheckBlockingSquares;
            enemySide.EnPassantSquare.MatchSome(ep =>
            {
                BitBoard epBb = ep;
                if (epBb.PawnPushMask(enemySide.Side) == moveConstraints.CheckingPieces)
                {
                    // ep capture gets us out of check
                    checkDodgers = epBb;
                }
            });
            pawnMoves.RemoveAll(m => !checkDodgers.IsOccupied(m.To));
        }

        moves.AddRange(pawnMoves);

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