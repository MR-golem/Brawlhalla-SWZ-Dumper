// https://gist.github.com/barncastle/a21b62df945445b38daf91ede021a3ec

using System.IO.Compression;
using System.Text;

namespace BrawlhallaSWZTool;

public static class BrawlhallaSWZ
{
    public static string[] Decrypt(Stream input, uint globalKey)
    {
        var checksum = ReadUInt32BE(input);
        var seed     = ReadUInt32BE(input);

        var rand = new WELL512(seed ^ globalKey);

        var hash        = 0x2DF4A1CDu;
        var hash_rounds = (int)(globalKey % 0x1F + 5);
        for (var i = 0; i < hash_rounds; i++)
            hash ^= rand.NextUInt();

        if (hash != checksum)
            throw new InvalidDataException($"Header checksum mismatch: expected {checksum:X8}, got {hash:X8}");

        var results = new List<string>();
        while (input.Position != input.Length)
        {
            if (ReadStringEntry(input, rand, out var entry))
                results.Add(entry!);
        }

        return results.ToArray();
    }

    public static byte[] Encrypt(uint seed, uint globalKey, params string[] stringEntries)
    {
        var rand = new WELL512(seed ^ globalKey);

        var hash        = 0x2DF4A1CDu;
        var hash_rounds = (int)(globalKey % 0x1F + 5);
        for (var i = 0; i < hash_rounds; i++)
            hash ^= rand.NextUInt();

        using var ms = new MemoryStream(0x1000);
        WriteUInt32BE(ms, hash);
        WriteUInt32BE(ms, seed);

        foreach (var entry in stringEntries)
            WriteStringEntry(Encoding.UTF8.GetBytes(entry), rand, ms);

        return ms.ToArray();
    }

    // -------------------------------------------------------------------------

    private static byte[] ZlibDecompress(byte[] data)
    {
        using var input  = new MemoryStream(data, 2, data.Length - 2); // skip zlib header
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] ZlibCompress(byte[] data)
    {
        using var output = new MemoryStream();
        output.WriteByte(0x78); // zlib header CMF
        output.WriteByte(0x9C); // zlib header FLG (default compression)
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
    {
        deflate.Write(data, 0, data.Length);
    }
        var adler = Adler32(data);
        Span<byte> checksum = stackalloc byte[4]
    {
        (byte)(adler >> 24), (byte)(adler >> 16),
        (byte)(adler >>  8), (byte)(adler)
    };
        output.Write(checksum);
        return output.ToArray();
    }

    private static uint Adler32(byte[] data)
    {
        uint s1 = 1, s2 = 0;
        foreach (var b in data)
        {
            s1 = (s1 + b)  % 65521;
            s2 = (s2 + s1) % 65521;
        }
        return (s2 << 16) | s1;
    }
    
    private static bool ReadStringEntry(Stream input, WELL512 rand, out string? result)
    {
        var compressedSize   = ReadUInt32BE(input) ^ rand.NextUInt();
        var decompressedSize = ReadUInt32BE(input) ^ rand.NextUInt();  // consumed for PRNG advance
        var checksum         = ReadUInt32BE(input);

        _ = decompressedSize; // intentionally unused after PRNG advance

        if (compressedSize + input.Position > input.Length)
        {
            result = null;
            return false;
        }

        var buffer = new byte[compressedSize];
        input.Read(buffer);

        var hash = rand.NextUInt();

        for (var i = 0; i < (int)compressedSize; i++)
        {
            var shift = i & 0xF;
            buffer[i] ^= (byte)(((0xFFu << shift) & rand.NextUInt()) >> shift);
            hash = buffer[i] ^ RotateRight(hash, i % 7 + 1);
        }

        if (hash != checksum)
            throw new InvalidDataException($"Entry checksum mismatch: expected {checksum:X8}, got {hash:X8}");

        var decompressed = ZlibDecompress(buffer);
        result = Encoding.UTF8.GetString(decompressed);
        return true;
    }

    private static void WriteStringEntry(byte[] input, WELL512 rand, Stream output)
    {
        var compressed = ZlibCompress(input);
        var compressedSize   = (uint)compressed.Length ^ rand.NextUInt();
        var decompressedSize = (uint)input.Length     ^ rand.NextUInt();

        var checksum = rand.NextUInt();

        for (var i = 0; i < compressed.Length; i++)
        {
            checksum = compressed[i] ^ RotateRight(checksum, i % 7 + 1);

            var shift = i & 0xF;
            compressed[i] ^= (byte)(((0xFFu << shift) & rand.NextUInt()) >> shift);
        }

        WriteUInt32BE(output, compressedSize);
        WriteUInt32BE(output, decompressedSize);
        WriteUInt32BE(output, checksum);
        output.Write(compressed);
    }

    // -------------------------------------------------------------------------

    private static uint RotateRight(uint v, int bits) =>
        (v >> bits) | (v << (32 - bits));

    private static uint ReadUInt32BE(Stream s)
    {
        Span<byte> buf = stackalloc byte[4];
        s.Read(buf);
        return (uint)(buf[3] | (buf[2] << 8) | (buf[1] << 16) | (buf[0] << 24));
    }

    private static void WriteUInt32BE(Stream s, uint v)
    {
        Span<byte> buf = stackalloc byte[4]
        {
            (byte)((v >> 24) & 0xFF),
            (byte)((v >> 16) & 0xFF),
            (byte)((v >>  8) & 0xFF),
            (byte)((v >>  0) & 0xFF)
        };
        s.Write(buf);
    }
}
