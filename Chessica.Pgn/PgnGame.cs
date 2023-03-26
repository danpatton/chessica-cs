using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Chessica.Core;

namespace Chessica.Pgn;

public record PgnGame(IImmutableDictionary<string, string> Tags, IImmutableList<PgnMove> Moves, PgnGameResult Result)
{
    private static readonly Regex TagRegex = new Regex("\\[(\\S+) \"(.*)\"\\]\\r?\\n", RegexOptions.Multiline);
    private static readonly Regex CommentRegex = new Regex("\\{.*\\}", RegexOptions.Multiline);
    private static readonly Regex MovePairRegex = new Regex("\\d+\\.\\s+(\\S+)(\\s+(\\S+)?)?(\\s+(0-1|1-0|1/2-1/2))?", RegexOptions.Multiline);

    public static PgnGame Parse(string pgn)
    {
        var tagMatches = TagRegex.Matches(pgn).ToList();
        var tags = ImmutableDictionary.CreateBuilder<string, string>();
        foreach (var tagMatch in tagMatches)
        {
            tags.Add(tagMatch.Groups[1].Value, tagMatch.Groups[2].Value);
        }
        var moveTextStartIndex = tagMatches.Any()
            ? tagMatches[^1].Index + tagMatches[^1].Length
            : 0;
        var moveText = pgn[moveTextStartIndex..].Trim();
        if (moveText.Length < 10)
        {
            if (TryParseResult(moveText, out var noMoveResult))
            {
                return new PgnGame(tags.ToImmutable(), ImmutableArray<PgnMove>.Empty, noMoveResult);
            }
        }
        var sanitisedMoveText = CommentRegex.Replace(moveText, "");
        var moves = ParseMoves(sanitisedMoveText, out var result);
        return new PgnGame(tags.ToImmutable(), moves.ToImmutableArray(), result);
    }

    private static IEnumerable<PgnMove> ParseMoves(string pgnMoveText, out PgnGameResult result)
    {
        var moves = new List<PgnMove>();
        var matches = MovePairRegex.Matches(pgnMoveText).ToList();
        foreach (var match in matches)
        {
            moves.Add(new PgnMove(Side.White, match.Groups[1].Value));
            if (match.Groups[3].Success)
            {
                var spec = match.Groups[3].Value;
                if (TryParseResult(spec, out result))
                {
                    return moves;
                }
                moves.Add(new PgnMove(Side.Black, match.Groups[3].Value));
            }

            if (match.Groups[5].Success)
            {
                var spec = match.Groups[5].Value;
                if (TryParseResult(spec, out result))
                {
                    return moves;
                }
            }
        }

        result = PgnGameResult.Other;
        return moves;
    }

    public static bool TryParseResult(string spec, out PgnGameResult result)
    {
        switch (spec)
        {
            case "1-0":
                result = PgnGameResult.WhiteWin;
                return true;
            case "0-1":
                result = PgnGameResult.BlackWin;
                return true;
            case "1/2-1/2":
                result = PgnGameResult.Draw;
                return true;
            default:
                result = PgnGameResult.Other;
                return false;
        }
    }

    public void WriteToFile(string filename)
    {
        using var stream = File.OpenWrite(filename);
        WriteToStream(stream);
    }

    public void WriteToStream(Stream stream)
    {
        using var writer = new StreamWriter(stream);
        for (var i = 0; i < (1 + Moves.Count) / 2; ++i)
        {
            var whiteMove = Moves[2*i];
            writer.Write($"{i+1}. {whiteMove.Spec} ");
            if (2 * i + 1 < Moves.Count)
            {
                var blackMove = Moves[2 * i + 1];
                writer.Write(blackMove.Spec);
            }

            writer.WriteLine();
        }
        writer.Write(" " + Result.ToPgnString());
    }
}
