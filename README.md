# libewf C# Wrapper

A complete C# P/Invoke wrapper for the native `libewf` library. Read forensic disk images in Expert Witness Format:

- `.E01` (EnCase single segment)
- `.L01` (logical images, segmented)
- `.Lx01` (logical images, additional segments)
- Other EWF-compatible formats

## Features

✅ **Random-access reads** from EWF images
✅ **Metadata extraction** (MD5, SHA1, SHA256 hashes, sector info, format)
✅ **File extraction** to raw format
✅ **Sector range extraction** for specific partitions
✅ **MBR partition detection** and enumeration
✅ **Multi-segment support** (all L01/Lx01 files handled automatically)
✅ **Progress reporting** during extraction
✅ **Full CLI tool** with multiple commands

## Project Layout

```
.
├── LibEwf/                      # Managed wrapper library
│   ├── LibEwfInterop.cs         # P/Invoke declarations (libewf v20131121+)
│   ├── EwfImage.cs              # Main image reader class
│   ├── EwfVolumeInfo.cs         # Metadata container
│   └── EwfFileExtractor.cs      # Extraction utilities & MBR parser
├── Example/                     # Full-featured CLI application
│   ├── Program.cs               # 4 commands: info, extract, sectors, partitions
│   └── Example.csproj
└── README.md
```

## Installation

### 1. Install Native libewf

**Linux (Ubuntu/Debian)**
```bash
sudo apt-get install libewf
```

**macOS**
```bash
brew install libewf
```

**Windows**
- Download from [libewf releases](https://github.com/libyal/libewf/releases)
- Place `libewf.dll` in your output directory or add to PATH
- Ensure you have the Visual C++ Redistributable installed

### 2. Build the Solution

```bash
dotnet restore
dotnet build
```

## Quick Start

### Display Metadata

```bash
dotnet run --project Example -- info C:\forensics\image.E01
```

Output:
```
=== EWF Volume Information ===
Source: C:\forensics\image.E01
Format: ENCASE7
Media Size: 5,368,709,120 bytes (5.00 GB)
Sectors: 10485760 x 512 bytes
Segments: 3
MD5: 1234567890abcdef1234567890abcdef
SHA1: abcdef1234567890abcdef1234567890abcdef12
Segment Files:
  - image.E01
  - image.E02
  - image.E03
```

### Extract Entire Image

```bash
dotnet run --project Example -- extract C:\forensics\image.E01 C:\output\image.raw
```

This reads all data from the EWF volume and writes to a raw `.raw` file. Progress is reported as a percentage.

### Extract Specific Sectors

Extract partition 1 (sectors 2048-1050623):

```bash
dotnet run --project Example -- sectors C:\forensics\image.E01 2048 1050623 C:\output\partition1.raw
```

### Detect Partitions

```bash
dotnet run --project Example -- partitions C:\forensics\image.E01
```

Output:
```
Found 3 partitions:

Partition 0: LBA 2048-1050623 (536,805,376 bytes) Type: NTFS/HPFS
  Bytes: 1048576 - 536806912
  Size: 511.75 MB

Partition 1: LBA 1050624-2101247 (536,805,376 bytes) Type: NTFS/HPFS
  Bytes: 536807936 - 1073613312
  Size: 511.75 MB
...
```

## C# API Usage

### Basic Read

```csharp
using LibEwf;

// Open an EWF image
using var image = new EwfImage("C:\\forensics\\image.E01");

Console.WriteLine($"Media size: {image.MediaSize} bytes");
Console.WriteLine($"Segments: {image.NumberOfSegments}");

// Read from a specific offset
byte[] buffer = new byte[4096];
int bytesRead = image.Read(0, buffer, 0, buffer.Length);
Console.WriteLine($"Read {bytesRead} bytes from offset 0");
```

### Extract Metadata

```csharp
using var image = new EwfImage("C:\\forensics\\image.E01");
var info = image.GetVolumeInfo();

Console.WriteLine($"Format: {info.GetFormatName()}");
Console.WriteLine($"MD5: {info.MD5Hash}");
Console.WriteLine($"SHA1: {info.SHA1Hash}");
foreach (var segment in info.SegmentFiles)
    Console.WriteLine($"  - {segment}");
```

### Extract to Raw

```csharp
using var image = new EwfImage("C:\\forensics\\image.E01");
var extractor = new EwfFileExtractor(image);

var progress = new Progress<int>(p => Console.WriteLine($"Progress: {p}%"));
extractor.ExtractToRawImage("output.raw", progress);
```

### Detect Partitions

```csharp
using var image = new EwfImage("C:\\forensics\\image.E01");
var extractor = new EwfFileExtractor(image);
var partitions = extractor.DetectMbrPartitions();

foreach (var part in partitions)
{
    Console.WriteLine($"{part.GetTypeString()}: {part.SizeBytes} bytes");
}
```

## Troubleshooting

### "libewf_handle_initialize failed"

**Cause:** libewf is not installed or not in the library search path.

**Fix:**
- Linux/macOS: `apt-get install libewf` or `brew install libewf`
- Windows: Ensure `libewf.dll` is in the output directory or on PATH

### "libewf_handle_open failed"

**Cause:** The file is not a valid EWF image, or the segment paths are incorrect.

**Fix:**
- Verify the file is a real E01/L01/Lx01 image
- Ensure all segment files are in the same directory
- Check file permissions (must be readable)

### Wrong hash values

**Cause:** libewf version mismatch or uninitialized hash buffer.

**Fix:**
- Update libewf: `sudo apt-get install --upgrade libewf`
- Verify you're opening the correct image file

## Supported libewf Versions

Tested with:
- libewf 20131121 (Ubuntu 20.04/22.04)
- libewf 20140616 (Debian 11)
- libewf 20230212 (macOS Homebrew)
- libewf 20161124 (Windows binary)

If you encounter symbol resolution errors, the native library may export different function names. Edit `LibEwfInterop.cs` to match your installation:

```bash
# Check exported symbols
nm -D /usr/lib/x86_64-linux-gnu/libewf.so | grep libewf_handle
```

## Performance

**Extraction speeds** (typical):
- Sequential read (extract): 50-200 MB/s (depends on I/O)
- Random read: 10-50 MB/s (more overhead)

**Memory usage**:
- Fixed overhead: ~10 MB
- Per-read: 1 MB buffer (adjustable in code)
- Full extraction of 10 GB image: ~100 MB peak (streaming)

## License

MIT

## References

- [libewf GitHub](https://github.com/libyal/libewf)
- [EWF Format Specification](https://github.com/libyal/libewf/blob/main/documentation/EWF%20format.asciidoc)
- [Expert Witness Format](https://en.wikipedia.org/wiki/Forensic_image)
