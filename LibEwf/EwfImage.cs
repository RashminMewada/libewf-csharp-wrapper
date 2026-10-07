using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LibEwf;

/// <summary>
/// Managed wrapper for reading EWF forensic images (E01, L01, Lx01, etc.).
/// Supports random-access reads and metadata extraction.
/// </summary>
public sealed class EwfImage : IDisposable
{
    private IntPtr _handle = IntPtr.Zero;
    private readonly string[] _segmentPaths;
    private bool _disposed = false;

    /// <summary>Initialize EwfImage with the first segment path. Additional segments are auto-discovered.</summary>
    public EwfImage(string firstSegmentPath)
        : this(new[] { firstSegmentPath })
    {
    }

    /// <summary>Initialize EwfImage with explicit segment paths.</summary>
    public EwfImage(IEnumerable<string> segmentPaths)
    {
        if (segmentPaths is null)
            throw new ArgumentNullException(nameof(segmentPaths));

        _segmentPaths = segmentPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path))
            .ToArray();

        if (_segmentPaths.Length == 0)
            throw new ArgumentException("At least one segment path is required.", nameof(segmentPaths));

        Open();
    }

    private void Open()
    {
        int result = LibEwfInterop.libewf_handle_initialize(out _handle);
        if (result != 0)
            throw new InvalidOperationException($"libewf_handle_initialize failed with result {result}. Make sure libewf is installed.");

        result = LibEwfInterop.libewf_handle_open(
            _handle,
            _segmentPaths,
            _segmentPaths.Length,
            LibEwfInterop.LIBEWF_OPEN_FLAG_READ);

        if (result != 0)
        {
            SafeClose();
            throw new InvalidOperationException(
                $"libewf_handle_open failed with result {result}. The file may not be a valid EWF image, or libewf may not be correctly installed. " +
                $"Check that all segment files exist and are readable. Tried to open: {string.Join(", ", _segmentPaths)}");
        }

        result = LibEwfInterop.libewf_handle_get_media_size(_handle, out ulong size);
        if (result != 0)
            throw new InvalidOperationException($"libewf_handle_get_media_size failed with result {result}.");

        MediaSize = (long)size;
    }

    /// <summary>Total media size in bytes.</summary>
    public long MediaSize { get; private set; }

    /// <summary>Number of segments in the volume.</summary>
    public int NumberOfSegments
    {
        get
        {
            int result = LibEwfInterop.libewf_handle_get_number_of_segments(_handle, out int count);
            if (result != 0)
                throw new InvalidOperationException($"libewf_handle_get_number_of_segments failed with result {result}.");
            return count;
        }
    }

    /// <summary>Get metadata about this EWF volume.</summary>
    public EwfVolumeInfo GetVolumeInfo()
    {
        var info = new EwfVolumeInfo
        {
            SourcePath = _segmentPaths[0],
            MediaSize = (ulong)MediaSize,
            NumberOfSegments = NumberOfSegments
        };

        // Get bytes per sector
        if (LibEwfInterop.libewf_handle_get_bytes_per_sector(_handle, out uint bytesPerSector) == 0)
            info.BytesPerSector = bytesPerSector;

        // Get number of sectors
        if (LibEwfInterop.libewf_handle_get_number_of_sectors(_handle, out ulong numberOfSectors) == 0)
            info.NumberOfSectors = numberOfSectors;

        // Get format
        if (LibEwfInterop.libewf_handle_get_format(_handle, out int format) == 0)
            info.Format = format;

        // Get description
        try
        {
            if (LibEwfInterop.libewf_handle_get_utf8_description_size(_handle, out int descSize) == 0 && descSize > 0)
            {
                IntPtr descPtr = Marshal.AllocHGlobal(descSize);
                try
                {
                    if (LibEwfInterop.libewf_handle_get_utf8_description(_handle, descPtr, descSize) == 0)
                    {
                        info.Description = Marshal.PtrToStringUTF8(descPtr) ?? string.Empty;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(descPtr);
                }
            }
        }
        catch { /* ignore errors reading description */ }

        // Get hashes
        try { info.MD5Hash = GetHash(LibEwfInterop.LIBEWF_HASH_TYPE_MD5); } catch { }
        try { info.SHA1Hash = GetHash(LibEwfInterop.LIBEWF_HASH_TYPE_SHA1); } catch { }
        try { info.SHA256Hash = GetHash(LibEwfInterop.LIBEWF_HASH_TYPE_SHA256); } catch { }

        // Get segment file names
        for (int i = 0; i < NumberOfSegments; i++)
        {
            try
            {
                if (LibEwfInterop.libewf_handle_get_segment_filename_size(_handle, i, out int nameSize) == 0 && nameSize > 0)
                {
                    IntPtr namePtr = Marshal.AllocHGlobal(nameSize);
                    try
                    {
                        if (LibEwfInterop.libewf_handle_get_segment_filename(_handle, i, namePtr, nameSize) == 0)
                        {
                            string? segmentName = Marshal.PtrToStringUTF8(namePtr);
                            if (!string.IsNullOrEmpty(segmentName))
                                info.SegmentFiles.Add(segmentName);
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(namePtr);
                    }
                }
            }
            catch { /* ignore errors reading segment names */ }
        }

        return info;
    }

    private string? GetHash(int hashType)
    {
        if (LibEwfInterop.libewf_handle_get_utf8_hash_value_size(_handle, hashType, out int hashSize) != 0 || hashSize <= 0)
            return null;

        IntPtr hashPtr = Marshal.AllocHGlobal(hashSize);
        try
        {
            if (LibEwfInterop.libewf_handle_get_utf8_hash_value(_handle, hashType, hashPtr, hashSize) == 0)
            {
                return Marshal.PtrToStringUTF8(hashPtr);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(hashPtr);
        }

        return null;
    }

    /// <summary>Read data from the image at a specific offset.</summary>
    public int Read(long offset, byte[] buffer, int bufferOffset, int count)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EwfImage));

        if (buffer is null)
            throw new ArgumentNullException(nameof(buffer));

        if (bufferOffset < 0 || count < 0 || bufferOffset + count > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(buffer));

        if (count == 0)
            return 0;

        if (offset < 0 || offset >= MediaSize)
            return 0;

        IntPtr managedBuffer = Marshal.AllocHGlobal(count);
        try
        {
            int result = LibEwfInterop.libewf_handle_read_random(_handle, managedBuffer, count, offset, 0);
            if (result < 0)
                return 0;

            Marshal.Copy(managedBuffer, buffer, bufferOffset, result);
            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(managedBuffer);
        }
    }

    /// <summary>Read all data from the image (memory-intensive for large images).</summary>
    public byte[] ReadAll()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EwfImage));

        byte[] data = new byte[MediaSize];
        long offset = 0;
        int blockSize = 1024 * 1024; // 1 MB chunks

        while (offset < MediaSize)
        {
            int remaining = (int)Math.Min(blockSize, MediaSize - offset);
            int bytesRead = Read(offset, data, (int)offset, remaining);
            if (bytesRead <= 0)
                break;
            offset += bytesRead;
        }

        return data;
    }

    private void SafeClose()
    {
        if (_handle != IntPtr.Zero)
        {
            try
            {
                LibEwfInterop.libewf_handle_close(_handle, 0);
                LibEwfInterop.libewf_handle_free(ref _handle);
            }
            catch { /* ignore errors during cleanup */ }
            _handle = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        SafeClose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~EwfImage()
    {
        Dispose();
    }
}
