using System;
using System.Runtime.InteropServices;

namespace LibEwf;

/// <summary>
/// P/Invoke declarations for libewf (Expert Witness Format library).
/// Tested against libewf versions 20131121 and later (Linux/macOS/Windows).
/// </summary>
public static class LibEwfInterop
{
    private const string DllName = "libewf";

    // Access and open flags
    public const int LIBEWF_ACCESS_FLAG_READ = 0x01;
    public const int LIBEWF_ACCESS_FLAG_WRITE = 0x02;
    public const int LIBEWF_OPEN_FLAG_READ = 0x01;

    // Hash types
    public const int LIBEWF_HASH_TYPE_MD5 = 2;
    public const int LIBEWF_HASH_TYPE_SHA1 = 3;
    public const int LIBEWF_HASH_TYPE_SHA256 = 4;

    // Error handling
    public const int LIBEWF_ERROR_SUCCESSFUL = 0;

    static LibEwfInterop()
    {
        // Attempt to pre-load libewf from common paths.
        // This helps with runtime symbol resolution.
        string[] candidates = { "libewf.so", "libewf.so.3", "libewf.so.4", "libewf.dylib", "libewf.dll" };
        foreach (string candidate in candidates)
        {
            if (NativeLibrary.TryLoad(candidate, out _))
                return;
        }
    }

    /// <summary>Initialize a new libewf handle.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_initialize", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_initialize(out IntPtr handle);

    /// <summary>Free an initialized libewf handle.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_free", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_free(ref IntPtr handle);

    /// <summary>Open EWF segment files.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_open", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_open(IntPtr handle, string[] filenames, int numberOfFiles, int accessFlags);

    /// <summary>Close an open EWF volume.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_close", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_close(IntPtr handle, int error);

    /// <summary>Get the total media size in bytes.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_get_media_size", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_get_media_size(IntPtr handle, out ulong mediaSize);

    /// <summary>Get the number of segments in the volume.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_get_number_of_segments", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_get_number_of_segments(IntPtr handle, out int numberOfSegments);

    /// <summary>Get the filename size of a specific segment.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_get_segment_filename_size", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_get_segment_filename_size(IntPtr handle, int segmentNumber, out int filenameSize);

    /// <summary>Get the filename of a specific segment.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_get_segment_filename", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_get_segment_filename(IntPtr handle, int segmentNumber, IntPtr filename, int filenameSize);

    /// <summary>Read data from the image at a random offset.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_read_random", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_read_random(IntPtr handle, IntPtr buffer, int bufferSize, long offset, int readFlags);

    /// <summary>Read data sequentially from the current position.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_read_buffer", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_read_buffer(IntPtr handle, IntPtr buffer, int bufferSize, out int numberOfBytesRead);

    /// <summary>Seek to an offset in the image.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_seek_offset", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern long libewf_handle_seek_offset(IntPtr handle, long offset, int whence);

    /// <summary>Get the current offset in the image.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_get_offset", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_get_offset(IntPtr handle, out long offset);

    /// <summary>Get a UTF-8 encoded hash value (MD5, SHA1, SHA256).</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_get_utf8_hash_value", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_get_utf8_hash_value(IntPtr handle, int hashType, IntPtr hashString, int hashStringSize);

    /// <summary>Get the size of a hash value string buffer.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_get_utf8_hash_value_size", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_get_utf8_hash_value_size(IntPtr handle, int hashType, out int hashValueSize);

    /// <summary>Get the bytes per sector.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_get_bytes_per_sector", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_get_bytes_per_sector(IntPtr handle, out uint bytesPerSector);

    /// <summary>Get the number of sectors.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_get_number_of_sectors", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_get_number_of_sectors(IntPtr handle, out ulong numberOfSectors);

    /// <summary>Get acquisition format as string.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_get_format", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_get_format(IntPtr handle, out int format);

    /// <summary>Get the UTF-8 encoded description (case description or similar metadata).</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_get_utf8_description", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_get_utf8_description(IntPtr handle, IntPtr description, int descriptionSize);

    /// <summary>Get the UTF-8 description size.</summary>
    [DllImport(DllName, EntryPoint = "libewf_handle_get_utf8_description_size", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int libewf_handle_get_utf8_description_size(IntPtr handle, out int descriptionSize);
}
