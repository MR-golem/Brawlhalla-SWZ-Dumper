<p align="center">
  <img src="src\BrawlhallaDumperGUI\icon.ico" width="96" />
</p>

<h1 align="center">Brawlhalla SWZ Dumper</h1>

<p align="center">
  <b>Decrypt, Encrypt &amp; Read Memory for Brawlhalla</b><br/>
  <sub>.NET 9 · WinForms · Self-contained single-file EXE</sub>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet" />
  <img src="https://img.shields.io/badge/Platform-Win--x64-0078D4?logo=windows" />
  <img src="https://img.shields.io/badge/License-MIT-green" />
</p>

---

## Features

| Tab | Description |
|-----|-------------|
| **Key Finder** | Automatically locates `BrawlhallaAir.swf` in your Steam library and extracts the SWZ encryption key from the ABC bytecode |
| **Decrypt** | Decrypts `.swz` archives into readable XML using the global key |
| **Encrypt** | Re-encrypts modified XML back into a valid `.swz` file the game can load |
| **Memory Reader** | Attaches to a running Brawlhalla process and reads live game data (Camera, Player, Input) via AOB pattern scanning |

## Screenshot

<div align="center">
  <img src="https://placehold.co/800x500/1a1a2e/e0e0e0?text=Memory+Reader+Tab" alt="Memory Reader" />
</div>

## Quick Start

### 1. Download

Grab the latest release from [Releases](../../releases) — a single `BrawlhallaDumperGUI.exe`, no install needed.

### 2. Run as Administrator

The Memory Reader requires admin privileges to read the game process. Right-click → **Run as administrator** (or it will prompt automatically).

### 3. Get your SWZ Key

1. Open the **Key Finder** tab
2. Click **Find Key**
3. The key is copied to your clipboard automatically

### 4. Decrypt SWZ Files

1. Switch to the **Decrypt** tab
2. Paste your key into **Global Key**
3. Select the `.swz` files (e.g., `Dynamic.swz`, `Init.swz`)
4. Choose an output directory
5. Click **Decrypt**

### 5. Read Live Game Data

1. Launch Brawlhalla
2. Switch to the **Memory Reader** tab
3. Click **Refresh** (or enable **Auto-refresh**)
4. The tool scans for AOB patterns and displays live camera, player, and input data

## Building from Source

**Prerequisites:** [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)

```bash
# Clone
git clone https://github.com/YOUR_USERNAME/Brawlhalla-Dumper.git
cd Brawlhalla-Dumper

# Build
dotnet build BrawlhallaDumperGUI/BrawlhallaDumperGUI.csproj -c Release

# Publish (self-contained single-file)
dotnet publish BrawlhallaDumperGUI/BrawlhallaDumperGUI.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -o dist
```

The output will be in `dist/BrawlhallaDumperGUI.exe`.

## Project Structure

```
Brawlhalla-Dumper/
├── BrawlhallaDumperGUI/          # WinForms GUI application
│   ├── Form1.cs                  # Main form logic
│   ├── Form1.Designer.cs         # UI layout
│   ├── BrawlhallaOffsets.cs      # Memory offsets & AOB discovery
│   ├── ProcessMemoryReader.cs    # Win32 P/Invoke memory reader
│   ├── AOBScanner.cs             # Array-of-Bytes pattern scanner
│   ├── PatternSignature.cs       # Pattern definitions & extraction
│   ├── app.manifest              # Admin elevation manifest
│   └── icon.ico                  # Application icon
├── BrawlhallaSWZTool.csproj      # SWZ crypto library
├── BrawlhallaSWZ.cs              # WELL-512 crypto + zlib parser
├── WELL512.cs                    # WELL-512 PRNG implementation
├── SwzExporter.cs                # XML serializer for SWZ entries
├── BrawlKeyFinder/               # SWF key extraction tool
├── Get_Seeds.ps1                 # Seed extraction script
└── README.md
```

## SWZ Crypto Overview

Each `.swz` file uses [WELL-512](https://en.wikipedia.org/wiki/Well_equidistributed_long-period_linear) PRNG encryption:

```
[4 bytes]  header checksum (big-endian uint32)
[4 bytes]  seed            (big-endian uint32)
[entries…]
  [4 bytes]  compressed size   ^ WELL-512 word
  [4 bytes]  decompressed size ^ WELL-512 word
  [4 bytes]  entry checksum    (rolling XOR + rotate)
  [N bytes]  zlib-compressed payload, XORed with WELL-512 stream
```

The global key is embedded in `ANE_RawData.Init()` inside the game's ABC bytecode.

## Credits

- SWZ crypto logic adapted from [@barncastle's gist](https://gist.github.com/barncastle/a21b62df945445b38daf91ede021a3ec)
- ABC/SWF parser vendored from [WallyMapEditor](https://github.com/moffel1020/WallyMapEditor)

## License

This project is provided for research and modding purposes. No game binaries are distributed or modified at runtime.
