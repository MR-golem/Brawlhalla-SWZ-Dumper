using BrawlhallaSWZTool;

// ---------------------------------------------------------------------------
// Usage:
//   BrawlhallaSWZTool decrypt <globalKey_hex> <outputDir> <file1.swz> [file2.swz ...]
//   BrawlhallaSWZTool encrypt <globalKey_hex> <seed_hex>  <input.json> <output.swz>
// ---------------------------------------------------------------------------

if (args.Length < 3)
{
    PrintUsage();
    return 1;
}

var mode = args[0].ToLowerInvariant();

try
{
    switch (mode)
    {
        case "decrypt":
            return RunDecrypt(args);

        case "encrypt":
            return RunEncrypt(args);

        default:
            Console.Error.WriteLine($"Unknown mode: {mode}");
            PrintUsage();
            return 1;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ERROR] {ex.Message}");
    return 2;
}

// ---------------------------------------------------------------------------

static int RunDecrypt(string[] args)
{
    // decrypt <globalKey_hex> <outputDir> <file1.swz> [...]
    if (args.Length < 4)
    {
        Console.Error.WriteLine("decrypt requires: <globalKey_hex> <outputDir> <file.swz> [...]");
        return 1;
    }

    var globalKey = ParseUInt32Arg(args[1]);
    var outputDir = args[2];
    var swzFiles = args[3..];

    foreach (var swzPath in swzFiles)
    {
        if (!File.Exists(swzPath))
        {
            Console.Error.WriteLine($"[SKIP] File not found: {swzPath}");
            continue;
        }

        var swzName = Path.GetFileNameWithoutExtension(swzPath);
        Console.WriteLine($"\n==> Decrypting: {swzPath}");

        using var fs = File.OpenRead(swzPath);
        var entries = BrawlhallaSWZ.Decrypt(fs, globalKey);

        Console.WriteLine($"    {entries.Length} entries found.");

        var fileOutputDir = outputDir;
        SwzExporter.Export(entries, fileOutputDir, swzName);
    }

    return 0;
}

static int RunEncrypt(string[] args)
{
    // encrypt <globalKey_hex> <seed_hex> <input_merged.json> <output.swz>
    if (args.Length < 5)
    {
        Console.Error.WriteLine("encrypt requires: <globalKey_hex> <seed_hex> <input_merged.json> <output.swz>");
        return 1;
    }

    var globalKey = ParseUInt32Arg(args[1]);
    var seed = ParseUInt32Arg(args[2]);
    var inputXml = args[3];
    var outputSwz = args[4];

    if (!File.Exists(inputXml))
    {
        Console.Error.WriteLine($"Input XML not found: {inputXml}");
        return 1;
    }

    var entries = SwzExporter.Import(inputXml);

    var encrypted = BrawlhallaSWZ.Encrypt(seed, globalKey, entries);
    File.WriteAllBytes(outputSwz, encrypted);

    Console.WriteLine($"[*] Written {encrypted.Length} bytes → {outputSwz}");
    return 0;
}

// Accepts: decimal ("827161004"), 0x-prefixed hex ("0x314FA36C"), bare hex ("314FA36C").
static uint ParseUInt32Arg(string s) => CliHelpers.ParseUInt32Arg(s);

static void PrintUsage()
{
    Console.WriteLine("""
    BrawlhallaSWZTool — decrypt/encrypt Brawlhalla .swz files

    DECRYPT:
      BrawlhallaSWZTool decrypt <globalKey> <outputDir> <file1.swz> [file2.swz ...]

      Example:
        BrawlhallaSWZTool decrypt 827161004 ./output Dynamic.swz

    ENCRYPT:
      BrawlhallaSWZTool encrypt <globalKey> <seed> <input.xml> <output.swz>

      Example:
        BrawlhallaSWZTool encrypt 827161004 0xDEADBEEF ./output/Dynamic.xml Dynamic_new.swz

    Output structure (decrypt):
      <outputDir>/
        <swzName>.xml   ← all entries, each as <Entry index="N">
    """);
    Console.WriteLine(" ");
    Console.ReadKey();

}