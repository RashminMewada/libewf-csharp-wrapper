using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LibEwf;

/// <summary>
/// Metadata and volume information extracted from an EWF image.
/// </summary>
public class EwfVolumeInfo
{
    public string? SourcePath { get; set; }
    public ulong MediaSize { get; set; }
    public long MediaSizeInBytes => (long)MediaSize;
    public int NumberOfSegments { get; set; }
    public uint BytesPerSector { get; set; }
    public ulong NumberOfSectors { get; set; }
    public int Format { get; set; }
    public string? Description { get; set; }
    public string? MD5Hash { get; set; }
    public string? SHA1Hash { get; set; }
    public string? SHA256Hash { get; set; }
    public List<string> SegmentFiles { get; } = new();

    public string GetFormatName() => Format switch
    {
        0 => "EWFX",
        1 => "EWF",
        2 => "SMART",
        3 => "ENCASE1",
        4 => "ENCASE2",
        5 => "ENCASE3",
        6 => "ENCASE4",
        7 => "ENCASE5",
        8 => "ENCASE6",
        9 => "ENCASE7",
        10 => "ENCASE7_BLOCK_IO",
        _ => $"Unknown({Format})"
    };

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== EWF Volume Information ===");
        sb.AppendLine($"Source: {SourcePath}");
        sb.AppendLine($"Format: {GetFormatName()}");
        sb.AppendLine($"Media Size: {MediaSize:N0} bytes ({FormatBytes(MediaSize)})");
        sb.AppendLine($"Sectors: {NumberOfSectors:N0} x {BytesPerSector} bytes");
        sb.AppendLine($"Segments: {NumberOfSegments}");
        if (!string.IsNullOrEmpty(Description))
            sb.AppendLine($"Description: {Description}");
        if (!string.IsNullOrEmpty(MD5Hash))
            sb.AppendLine($"MD5: {MD5Hash}");
        if (!string.IsNullOrEmpty(SHA1Hash))
            sb.AppendLine($"SHA1: {SHA1Hash}");
        if (!string.IsNullOrEmpty(SHA256Hash))
            sb.AppendLine($"SHA256: {SHA256Hash}");
        if (SegmentFiles.Count > 0)
        {
            sb.AppendLine($"Segment Files:");
            foreach (var file in SegmentFiles)
                sb.AppendLine($"  - {file}");
        }
        return sb.ToString();
    }

    private static string FormatBytes(ulong bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
