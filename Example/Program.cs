using LibEwf;
using System.Globalization;

if (args.Length == 0)
{
    PrintUsage();
    return;
}

string command = args[0].ToLower();

try
{
    switch (command)
    {
        case "info":
            CommandInfo(args);
            break;
        case "extract":
            CommandExtract(args);
            break;
        case "sectors":
            CommandSectors(args);
            break;
        case "partitions":
            CommandPartitions(args);
            break;
        default:
            Console.WriteLine($"Unknown command: {command}");
            PrintUsage();
            break;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    if (!string.IsNullOrEmpty(ex.InnerException?.Message))
        Console.Error.WriteLine($"Details: {ex.InnerException.Message}");
    Environment.ExitCode = 1;
}

void PrintUsage()
{
    Console.WriteLine("LibEWF C# Wrapper - Forensic Image Reader");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project Example -- info <ewf-file>");
    Console.WriteLine("      Display metadata about an EWF image (E01, L01, Lx01)");
    Console.WriteLine();
    Console.WriteLine("  dotnet run --project Example -- extract <ewf-file> <output-file>");
    Console.WriteLine("      Extract the entire image to a raw file");
    Console.WriteLine();
    Console.WriteLine("  dotnet run --project Example -- sectors <ewf-file> <start-sector> <end-sector> <output-file>");
    Console.WriteLine("      Extract a range of sectors");
    Console.WriteLine();
    Console.WriteLine("  dotnet run --project Example -- partitions <ewf-file>");
    Console.WriteLine("      Detect and list MBR partitions");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run --project Example -- info C:\\forensics\\image.E01");
    Console.WriteLine("  dotnet run --project Example -- extract C:\\forensics\\image.L01 C:\\output\\image.raw");
    Console.WriteLine("  dotnet run --project Example -- partitions C:\\forensics\\image.Lx01");
}

void CommandInfo(string[] args)
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage: info <ewf-file>");
        return;
    }

    string imagePath = args[1];
    Console.WriteLine($"Opening: {imagePath}");

    using var image = new EwfImage(imagePath);
    var info = image.GetVolumeInfo();
    Console.WriteLine();
    Console.WriteLine(info);
}

void CommandExtract(string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: extract <ewf-file> <output-file>");
        return;
    }

    string imagePath = args[1];
    string outputPath = args[2];

    Console.WriteLine($"Opening: {imagePath}");
    using var image = new EwfImage(imagePath);
    Console.WriteLine($"Media size: {FormatBytes(image.MediaSize)}");
    Console.WriteLine($"Extracting to: {outputPath}");
    Console.WriteLine();

    var extractor = new EwfFileExtractor(image);
    var progress = new Progress<int>(p => Console.WriteLine($"Progress: {p}%"));
    extractor.ExtractToRawImage(outputPath, progress);

    Console.WriteLine();
    Console.WriteLine("Extraction complete.");
}

void CommandSectors(string[] args)
{
    if (args.Length < 5)
    {
        Console.WriteLine("Usage: sectors <ewf-file> <start-sector> <end-sector> <output-file>");
        return;
    }

    string imagePath = args[1];
    if (!long.TryParse(args[2], out long startSector))
    {
        Console.Error.WriteLine($"Invalid start sector: {args[2]}");
        return;
    }
    if (!long.TryParse(args[3], out long endSector))
    {
        Console.Error.WriteLine($"Invalid end sector: {args[3]}");
        return;
    }
    string outputPath = args[4];

    Console.WriteLine($"Opening: {imagePath}");
    using var image = new EwfImage(imagePath);
    Console.WriteLine($"Extracting sectors {startSector}-{endSector} to: {outputPath}");

    var extractor = new EwfFileExtractor(image);
    var progress = new Progress<int>(p => Console.WriteLine($"Progress: {p}%"));
    extractor.ExtractSectorRange(startSector, endSector, outputPath, progress);

    Console.WriteLine("Extraction complete.");
}

void CommandPartitions(string[] args)
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage: partitions <ewf-file>");
        return;
    }

    string imagePath = args[1];
    Console.WriteLine($"Opening: {imagePath}");

    using var image = new EwfImage(imagePath);
    var extractor = new EwfFileExtractor(image);
    var partitions = extractor.DetectMbrPartitions();

    Console.WriteLine($"Found {partitions.Count} partitions:");
    Console.WriteLine();

    foreach (var partition in partitions)
    {
        Console.WriteLine(partition);
        Console.WriteLine($"  Bytes: {partition.StartBytes:N0} - {partition.EndBytes:N0}");
        Console.WriteLine($"  Size: {FormatBytes(partition.SizeBytes)}");
        Console.WriteLine();
    }
}

string FormatBytes(long bytes)
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
