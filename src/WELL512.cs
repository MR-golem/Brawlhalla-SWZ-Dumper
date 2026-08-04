namespace BrawlhallaSWZTool;

/// <summary>
/// WELL512 pseudo-random number generator.
/// Matches the Brawlhalla SWZ crypto stream exactly.
/// </summary>
public sealed class WELL512
{
    private readonly uint[] _state = new uint[16];
    private uint _index;

    public WELL512(uint seed)
    {
        _state[0] = seed;
        for (var i = 1u; i < 16u; i++)
            _state[i] = 0x6C078965u * (_state[i - 1] ^ (_state[i - 1] >> 30)) + i;
        _index = 0;
    }

    public uint NextUInt()
    {
        var a = _state[_index];
        var c = _state[(_index + 13) & 15];

        var b = a ^ c ^ (a << 16) ^ (c << 15);
        c = _state[(_index + 9) & 15];
        c ^= c >> 11;
        a = _state[_index] = b ^ c;

        var d = a ^ ((a << 5) & 0xDA442D24u);
        _index = (_index + 15) & 15;
        a = _state[_index];
        _state[_index] = a ^ b ^ d ^ (a << 2) ^ (b << 18) ^ (c << 28);

        return _state[_index];
    }
}
