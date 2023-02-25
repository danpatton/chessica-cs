using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Chessica.Core;
using Chessica.Gui.View;
using Chessica.Uci;

namespace Chessica.Gui.ViewModel;

public class BoardViewModel : INotifyPropertyChanged
{
    private BoardState _boardState;

    public Side UserSide { get; private set; } = Side.White;

    private readonly List<Move> _legalMoves = new();

    public ObservableCollection<SquareViewModel> Squares { get; }

    public ObservableCollection<PieceViewModel> Pieces { get; }

    private PieceViewModel? _selectedPiece;

    public PieceViewModel? SelectedPiece
    {
        get => _selectedPiece;
        set
        {
            _selectedPiece = value;
            UpdateSquares();
        }
    }

    public BoardViewModel()
    {
        _boardState = BoardState.StartingPosition;
        Squares = new ObservableCollection<SquareViewModel>();
        Pieces = new ObservableCollection<PieceViewModel>();
        UpdateSquares();
    }

    public void NewGame(Side userSide)
    {
        UserSide = userSide;
        _boardState = BoardState.StartingPosition;
        StatusMessage = "";
        UpdateAll();
        if (userSide == Side.Black)
        {
            MakeEngineMove();
        }
    }

    private void UpdateAll()
    {
        _legalMoves.Clear();
        _legalMoves.AddRange(_boardState.GetLegalMoves().ToList());

        UpdateSquares();
        UpdatePieces();
    }

    private void UpdateSquares()
    {
        var boardInverted = UserSide == Side.Black;
        Squares.Clear();
        for (byte file = 0; file < 8; ++file)
        {
            for (byte rank = 0; rank < 8; ++rank)
            {
                var coord = new Coord(file, rank);
                var square = new SquareViewModel(coord, boardInverted, IsSelected(coord), IsHighlighted(coord));
                Squares.Add(square);
            }
        }
    }

    private void UpdatePieces()
    {
        var boardInverted = UserSide == Side.Black;
        Pieces.Clear();
        foreach (var (side, piece, coord) in _boardState.GetAllPieces())
        {
            var pvm = new PieceViewModel(side, piece, coord, boardInverted);
            Pieces.Add(pvm);
        }
    }

    public bool IsUsersMove => _boardState.SideToMove == UserSide;

    public bool IsSelected(Coord coord)
    {
        return SelectedPiece?.Coord == coord;
    }

    public bool IsHighlighted(Coord coord)
    {
        return SelectedPiece != null &&
               _legalMoves.Any(m => m.From == SelectedPiece.Coord && m.To == coord);
    }

    private bool TryResolveMove(PieceViewModel piece, Coord targetCoord, out Move? move)
    {
        var potentialMoves = _legalMoves.Where(m => m.Piece == piece.Piece && m.From == piece.Coord && m.To == targetCoord).ToList();
        if (potentialMoves.Count == 1)
        {
            move = potentialMoves.Single();
            return true;
        }

        var promotionMoves = potentialMoves.Cast<PromotionMove>().ToList();
        var promotionChoiceWnd = new PromotionChoiceWindow(UserSide);
        promotionChoiceWnd.ShowDialog();
        if (promotionChoiceWnd.SelectedPiece != null)
        {
            move = promotionMoves.Single(m => m.Promotion == promotionChoiceWnd.SelectedPiece);
            return true;
        }

        move = null;
        return false;
    }

    public void MovePiece(PieceViewModel piece, Coord targetCoord)
    {
        if (!TryResolveMove(piece, targetCoord, out var move)) return;
        _boardState.Push(move!);
        _selectedPiece = null;
        UpdateAll();
        var (inCheck, numLegalMoves) = _boardState.GetGameState();
        if (numLegalMoves == 0)
        {
            var messageBoxText = inCheck
                ? "Bloody hell mate, looks like you've got me."
                : "Looks a bit stale to me, mate!";
            var caption = inCheck ? "Checkmate" : "Stalemate";
            MessageBox.Show(messageBoxText, caption);
        }
        else
        {
            MakeEngineMove();
        }
    }

    public void MakeEngineMove()
    {
        StatusMessage = "Thinking...";
        Task.Run(() => new MiniMaxMoveGenerator(4).GetBestMove(_boardState)).ContinueWith(t =>
        {
            var engineMove = t.Result;
            var moveNumber = _boardState.FullMoveNumber;
            var spacer = UserSide == Side.White ? " ..." : " ";
            var pgnMoveSpec = engineMove.ToPgnSpec(_boardState);
            _boardState.Push(engineMove);
            UpdateAll();
            var (inCheck, numLegalMoves) = _boardState.GetGameState();
            var checkIndicator = inCheck
                ? numLegalMoves == 0 ? "#" : "+"
                : string.Empty;
            StatusMessage = $"Last engine move: {moveNumber}{spacer}{pgnMoveSpec}{checkIndicator}";
            if (numLegalMoves == 0)
            {
                var messageBoxText = inCheck
                    ? "Human scum, you are no match for Chessica."
                    : "Managed to snatch a draw off you!";
                var caption = inCheck ? "Checkmate" : "Stalemate";
                MessageBox.Show(messageBoxText, caption);
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public IEnumerable<Coord> GetLegalTargetCoords(PieceViewModel piece)
    {
        return piece.Side == _boardState.SideToMove
            ? _legalMoves
                .Where(m => m.Piece == piece.Piece && m.From == piece.Coord)
                .Select(m => m.To)
            : Array.Empty<Coord>();
    }

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        var handler = PropertyChanged;
        handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}