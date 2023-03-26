namespace Chessica.Core;

public class HashHistory
{
    private readonly Dictionary<long, int> _hashCounter;

    public HashHistory(long initialHashValue)
    {
        _hashCounter = new Dictionary<long, int>
        {
            [initialHashValue] = 1
        };
    }

    public HashHistory(HashHistory other)
    {
        _hashCounter = new Dictionary<long, int>(other._hashCounter);
    }

    public int Push(long hashValue)
    {
        var newCount = _hashCounter[hashValue] = _hashCounter.TryGetValue(hashValue, out var count) ? count + 1 : 1;
        return newCount;
    }

    public void Pop(long hashValue)
    {
        --_hashCounter[hashValue];
        if (_hashCounter[hashValue] == 0)
        {
            _hashCounter.Remove(hashValue);
        }
    }

    public HashHistory Clone()
    {
        return new HashHistory(this);
    }
}