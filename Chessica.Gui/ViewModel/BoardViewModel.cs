using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Chessica.Core;
using Chessica.Gui.View;
using Chessica.Pgn;
using Chessica.Uci;

namespace Chessica.Gui.ViewModel;

public class BoardViewModel : INotifyPropertyChanged
{
    private BoardState _boardState;
    private readonly List<PgnMove> _pgnMoves;
    private PgnGameResult _pgnResult;

    public Side UserSide { get; private set; } = Side.White;

    public Side EngineSide => UserSide == Side.White ? Side.Black : Side.White;

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
        _pgnMoves = new List<PgnMove>();
        _pgnResult = PgnGameResult.Other;
        Squares = new ObservableCollection<SquareViewModel>();
        Pieces = new ObservableCollection<PieceViewModel>();
        UpdateSquares();
    }

    public void NewGame(Side userSide)
    {
        UserSide = userSide;
        _boardState = BoardState.StartingPosition;
        _pgnMoves.Clear();
        _pgnResult = PgnGameResult.Other;
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
        for (var file = 0; file < 8; ++file)
        {
            for (var rank = 0; rank < 8; ++rank)
            {
                var coord = new Coord(file, rank);
                var square = new SquareViewModel(
                    coord,
                    boardInverted,
                    IsSelected(coord),
                    IsPotentialMove(coord),
                    IsPotentialCapture(coord));
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

    public bool IsPotentialMove(Coord coord)
    {
        return SelectedPiece != null &&
               _legalMoves.Any(m => m.From == SelectedPiece.Coord && m.To == coord && !m.IsCapture);
    }

    public bool IsPotentialCapture(Coord coord)
    {
        return SelectedPiece != null &&
               _legalMoves.Any(m => m.From == SelectedPiece.Coord && m.To == coord && m.IsCapture);
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
        var moveSpec = move!.ToPgnSpec(_boardState);
        _boardState.Push(move);
        _selectedPiece = null;
        var (inCheck, numLegalMoves) = _boardState.GetGameState();
        if (inCheck)
        {
            moveSpec += numLegalMoves == 0 ? "#" : "+";
        }

        _pgnMoves.Add(new PgnMove(UserSide, moveSpec));
        UpdateAll();
        if (numLegalMoves == 0)
        {
            var messageBoxText = inCheck
                ? "Bloody hell mate, looks like you've got me."
                : "Looks a bit stale to me, mate!";
            var caption = inCheck ? "Checkmate" : "Stalemate";
            MessageBox.Show(messageBoxText, caption);
            _pgnResult = inCheck
                ? UserSide == Side.White ? PgnGameResult.WhiteWin : PgnGameResult.BlackWin
                : PgnGameResult.Draw;
            OnPropertyChanged(nameof(PgnMoveHistory));
        }
        else
        {
            OnPropertyChanged(nameof(PgnMoveHistory));
            MakeEngineMove();
        }
    }

    public void MakeEngineMove()
    {
        StatusMessage = "Thinking...";
        Task.Run(() => new MiniMaxMoveGenerator(4).GetBestMove(_boardState)).ContinueWith(t =>
        {
            var engineMove = t.Result;
            var moveSpec = engineMove.ToPgnSpec(_boardState);
            _boardState.Push(engineMove);
            UpdateAll();
            var (inCheck, numLegalMoves) = _boardState.GetGameState();
            if (inCheck)
            {
                moveSpec += numLegalMoves == 0 ? "#" : "+";
            }

            _pgnMoves.Add(new PgnMove(EngineSide, moveSpec));
            StatusMessage = $"Last engine move: {moveSpec}";
            if (numLegalMoves == 0)
            {
                var messageBoxText = inCheck
                    ? "Human scum, you are no match for Chessica."
                    : "Managed to snatch a draw off you!";
                var caption = inCheck ? "Checkmate" : "Stalemate";
                MessageBox.Show(messageBoxText, caption);
                _pgnResult = inCheck
                    ? EngineSide == Side.White ? PgnGameResult.WhiteWin : PgnGameResult.BlackWin
                    : PgnGameResult.Draw;
            }
            
            OnPropertyChanged(nameof(PgnMoveHistory));
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public bool CanUndo => IsUsersMove && _pgnMoves.Count >= 2;

    public void UndoLastFullMove()
    {
        if (!IsUsersMove) return;
        if (_pgnMoves.Count < 2) return;
        _pgnMoves.RemoveRange(_pgnMoves.Count - 2, 2);
        _boardState.TryPop();
        _boardState.TryPop();
        UpdateAll();
        OnPropertyChanged(nameof(PgnMoveHistory));
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

    public string PgnMoveHistory
    {
        get
        {
            using var memoryStream = new MemoryStream();
            new PgnGame(ImmutableDictionary<string, string>.Empty, _pgnMoves.ToImmutableArray(), _pgnResult).WriteToStream(memoryStream);
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
    }

    public string FenBoardState => _boardState.ToFenString();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        var handler = PropertyChanged;
        handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}