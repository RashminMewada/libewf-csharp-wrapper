using System;
using System.IO;
using System.Text;

namespace LibEwf;

/// <summary>
/// Utility for extracting raw sector data from EWF images to files.
/// Handles parsing of simple partition tables to enumerate files (basic functionality).
/// </summary>
public class EwfFileExtractor
{
    private readonly EwfImage _image;
    private const int SectorSize = 512; // Standard sector size

    public EwfFileExtractor(EwfImage image)
    {
        _image = image ?? throw new ArgumentNullException(nameof(image));
    }

    /// <summary>Extract all image data to a raw file (sector-by-sector copy).</summary>
    public void ExtractToRawImage(string outputPath, IProgress<int>? progress = null)
    {
        using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileBuffering.SequentialScan, 1024 * 1024);
        ExtractToStream(output, progress);
    }

    /// <summary>Extract all image data to a stream.</summary>
    public void ExtractToStream(Stream outputStream, IProgress<int>? progress = null)
    {
        if (outputStream is null)
            throw new ArgumentNullException(nameof(outputStream));

        byte[] buffer = new byte[1024 * 1024]; // 1 MB buffer
        long offset = 0;
        int totalProgress = 0;
        int progressInterval = (int)(_image.MediaSize / 100); // Report every 1% or 1 MB

        while (offset < _image.MediaSize)
        {
            int toRead = (int)Math.Min(buffer.Length, _image.MediaSize - offset);
            int bytesRead = _image.Read(offset, buffer, 0, toRead);

            if (bytesRead <= 0)
                break;

            outputStream.Write(buffer, 0, bytesRead);
            offset += bytesRead;
            totalProgress += bytesRead;

            if (totalProgress >= progressInterval && progressInterval > 0)
            {
                int percentComplete = (int)((offset * 100) / _image.MediaSize);
                progress?.Report(percentComplete);
                totalProgress = 0;
            }
        }
    }

    /// <summary>
    /// Extract raw sectors within a range (useful for extracting specific partitions or data regions).
    /// </summary>
    public void ExtractSectorRange(long startSector, long endSector, string outputPath, IProgress<int>? progress = null)
    {
        long startOffset = startSector * SectorSize;
        long endOffset = endSector * SectorSize;
        long bytesToExtract = endOffset - startOffset;

        using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileBuffering.SequentialScan, 1024 * 1024);

        byte[] buffer = new byte[1024 * 1024];
        long offset = startOffset;
        int totalProgress = 0;
        int progressInterval = (int)(bytesToExtract / 100);

        while (offset < endOffset)
        {
            int toRead = (int)Math.Min(buffer.Length, endOffset - offset);
            int bytesRead = _image.Read(offset, buffer, 0, toRead);

            if (bytesRead <= 0)
                break;

            output.Write(buffer, 0, bytesRead);
            offset += bytesRead;
            totalProgress += bytesRead;

            if (totalProgress >= progressInterval && progressInterval > 0)
            {
                int percentComplete = (int)(((offset - startOffset) * 100) / bytesToExtract);
                progress?.Report(percentComplete);
                totalProgress = 0;
            }
        }
    }

    /// <summary>Read a specific number of sectors starting at a given sector offset.</summary>
    public byte[] ReadSectors(long startSector, int sectorCount)
    {
        long offset = startSector * SectorSize;
        int bytesToRead = sectorCount * SectorSize;
        byte[] buffer = new byte[bytesToRead];

        int bytesRead = _image.Read(offset, buffer, 0, bytesToRead);
        if (bytesRead < bytesToRead)
            Array.Resize(ref buffer, bytesRead);

        return buffer;
    }

    /// <summary>Detect and enumerate MBR (Master Boot Record) partitions in the image.</summary>
    public List<MbrPartition> DetectMbrPartitions()
    {
        var partitions = new List<MbrPartition>();
        byte[] mbrData = ReadSectors(0, 1);

        if (mbrData.Length < 512)
            return partitions;

        // MBR signature check
        if (mbrData[510] != 0x55 || mbrData[511] != 0xAA)
            return partitions; // Not a valid MBR

        // Parse partition table (4 entries, starting at offset 446)
        const int partitionTableOffset = 446;
        const int partitionEntrySize = 16;

        for (int i = 0; i < 4; i++)
        {
            int offset = partitionTableOffset + (i * partitionEntrySize);
            byte status = mbrData[offset];
            byte type = mbrData[offset + 4];
            uint lba = BitConverter.ToUInt32(mbrData, offset + 8);
            uint size = BitConverter.ToUInt32(mbrData, offset + 12);

            if (type == 0) // Empty partition
                continue;

            partitions.Add(new MbrPartition
            {
                Index = i,
                Status = status,
                Type = type,
                StartLBA = lba,
                SizeInSectors = size
            });
        }

        return partitions;
    }
}

/// <summary>Represents an MBR partition entry.</summary>
public class MbrPartition
{
    public int Index { get; set; }
    public byte Status { get; set; }
    public byte Type { get; set; }
    public uint StartLBA { get; set; }
    public uint SizeInSectors { get; set; }
    public long StartBytes => StartLBA * 512L;
    public long SizeBytes => SizeInSectors * 512L;
    public long EndBytes => StartBytes + SizeBytes;

    public string GetTypeString() => Type switch
    {
        0x00 => "Empty",
        0x01 => "FAT12",
        0x04 => "FAT16",
        0x05 => "Extended",
        0x06 => "FAT16B",
        0x07 => "NTFS/HPFS",
        0x0B => "FAT32",
        0x0C => "FAT32 LBA",
        0x0E => "FAT16 LBA",
        0x0F => "Extended LBA",
        0x83 => "Linux",
        0x8E => "Linux LVM",
        0xA5 => "FreeBSD",
        0xA6 => "OpenBSD",
        0xA9 => "NetBSD",
        _ => $"Unknown (0x{Type:X2})"
    };

    public override string ToString()
    {
        return $"Partition {Index}: LBA {StartLBA}-{StartLBA + SizeInSectors} ({SizeBytes:N0} bytes) Type: {GetTypeString()}";
    }
}
