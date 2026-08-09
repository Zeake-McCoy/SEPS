using System.Buffers.Binary;
using System.IO.Compression;

namespace SEPS.IO;

/// <summary>
/// Minimal PNG decoder for Space Engineers 16-bit grayscale heightmaps.
///
/// Supported format:
/// - PNG
/// - 16-bit grayscale
/// - non-interlaced
/// - standard PNG filtering
///
/// The decoded samples are returned as native ushort values.
/// </summary>
public static class Png16Reader
{
    private static readonly byte[] PngSignature =
    {
        0x89, 0x50, 0x4E, 0x47,
        0x0D, 0x0A, 0x1A, 0x0A
    };

    public static (int Width, int Height, ushort[] Samples) Read(
        string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "PNG heightmap was not found.",
                path);
        }

        using var stream =
            File.OpenRead(path);

        return Read(stream);
    }

    private static (
        int Width,
        int Height,
        ushort[] Samples) Read(
        Stream stream)
    {
        byte[] signature = new byte[8];

        ReadExactly(
            stream,
            signature);

        if (!signature.SequenceEqual(PngSignature))
        {
            throw new InvalidDataException(
                "The file is not a valid PNG.");
        }

        int width = 0;
        int height = 0;
        byte bitDepth = 0;
        byte colorType = 0;
        byte compressionMethod = 0;
        byte filterMethod = 0;
        byte interlaceMethod = 0;

        using var compressedData =
        new MemoryStream();

        bool foundHeader = false;
        bool foundEnd = false;

        byte[] type = new byte[4];

        while (stream.Position < stream.Length)
        {
            uint length =
                ReadUInt32BE(stream);

            ReadExactly(
                stream,
                type);

            if (length > int.MaxValue)
            {
                throw new InvalidDataException(
                    "PNG chunk is too large.");
            }

            var data =
                new byte[length];

            ReadExactly(
                stream,
                data);

            // CRC is intentionally not used here.
            // The PNG container and decoded data are
            // still validated by the structural checks below.
            ReadUInt32BE(stream);

            var chunkType =
                System.Text.Encoding.ASCII.GetString(type);

            switch (chunkType)
            {
                case "IHDR":
                {
                    if (length != 13)
                    {
                        throw new InvalidDataException(
                            "Invalid PNG IHDR chunk.");
                    }

                    width =
                        BinaryPrimitives.ReadInt32BigEndian(
                            data.AsSpan(0, 4));

                    height =
                        BinaryPrimitives.ReadInt32BigEndian(
                            data.AsSpan(4, 4));

                    bitDepth = data[8];
                    colorType = data[9];
                    compressionMethod = data[10];
                    filterMethod = data[11];
                    interlaceMethod = data[12];

                    foundHeader = true;

                    break;
                }

                case "IDAT":
                    compressedData.Write(
                        data,
                        0,
                        data.Length);
                    break;

                case "IEND":
                    foundEnd = true;
                    break;
            }

            if (foundEnd)
                break;
        }

        if (!foundHeader)
        {
            throw new InvalidDataException(
                "PNG does not contain an IHDR chunk.");
        }

        if (!foundEnd)
        {
            throw new InvalidDataException(
                "PNG does not contain an IEND chunk.");
        }

        ValidateFormat(
            width,
            height,
            bitDepth,
            colorType,
            compressionMethod,
            filterMethod,
            interlaceMethod);

        return DecodePixels(
            width,
            height,
            compressedData.ToArray());
    }

    private static void ValidateFormat(
        int width,
        int height,
        byte bitDepth,
        byte colorType,
        byte compressionMethod,
        byte filterMethod,
        byte interlaceMethod)
    {
        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException(
                "PNG has invalid dimensions.");
        }

        if (bitDepth != 16)
        {
            throw new InvalidDataException(
                $"Unsupported heightmap bit depth: {bitDepth}. " +
                "SEPS expects a 16-bit grayscale PNG.");
        }

        // PNG color type 0 = grayscale.
        if (colorType != 0)
        {
            throw new InvalidDataException(
                $"Unsupported heightmap color type: {colorType}. " +
                "SEPS expects a 16-bit grayscale PNG.");
        }

        if (compressionMethod != 0)
        {
            throw new InvalidDataException(
                "Unsupported PNG compression method.");
        }

        if (filterMethod != 0)
        {
            throw new InvalidDataException(
                "Unsupported PNG filter method.");
        }

        // We deliberately keep the first implementation
        // non-interlaced. Space Engineers planet heightmaps
        // do not require Adam7 decoding.
        if (interlaceMethod != 0)
        {
            throw new InvalidDataException(
                "Interlaced heightmaps are not supported.");
        }
    }

    private static (
        int Width,
        int Height,
        ushort[] Samples) DecodePixels(
        int width,
        int height,
        byte[] compressedData)
    {
        int rowBytes =
            checked(width * 2);

        int expectedBytes =
            checked(height * (rowBytes + 1));

        var filteredData =
            new byte[expectedBytes];

        using var compressedStream =
            new MemoryStream(
                compressedData);

        using var zlib =
            new ZLibStream(
                compressedStream,
                CompressionMode.Decompress);

        ReadExactly(
            zlib,
            filteredData);

        var samples =
            new ushort[
                checked(width * height)];

        var previousRow =
            new byte[rowBytes];

        var currentRow =
            new byte[rowBytes];

        int sourceOffset = 0;

        for (var y = 0; y < height; y++)
        {
            byte filterType =
                filteredData[sourceOffset++];

            filteredData.AsSpan(
                    sourceOffset,
                    rowBytes)
                .CopyTo(currentRow);

            sourceOffset += rowBytes;

            ApplyFilter(
                filterType,
                currentRow,
                previousRow,
                2);

            int sampleOffset =
                y * width;

            for (var x = 0; x < width; x++)
            {
                int byteOffset =
                    x * 2;

                samples[sampleOffset + x] =
                    BinaryPrimitives.ReadUInt16BigEndian(
                        currentRow.AsSpan(byteOffset, 2));
            }

            (
                previousRow,
                currentRow) =
                (currentRow, previousRow);
        }

        return (
            width,
            height,
            samples);
    }

    private static void ApplyFilter(
        byte filterType,
        byte[] row,
        byte[] previousRow,
        int bytesPerPixel)
    {
        switch (filterType)
        {
            case 0:
                // None
                return;

            case 1:
                // Sub
                for (var i = bytesPerPixel;
                     i < row.Length;
                     i++)
                {
                    row[i] =
                        unchecked(
                            (byte)(
                                row[i] +
                                row[i - bytesPerPixel]));
                }

                return;

            case 2:
                // Up
                for (var i = 0;
                     i < row.Length;
                     i++)
                {
                    row[i] =
                        unchecked(
                            (byte)(
                                row[i] +
                                previousRow[i]));
                }

                return;

            case 3:
                // Average
                for (var i = 0;
                     i < row.Length;
                     i++)
                {
                    int left =
                        i >= bytesPerPixel
                            ? row[i - bytesPerPixel]
                            : 0;

                    int up =
                        previousRow[i];

                    row[i] =
                        unchecked(
                            (byte)(
                                row[i] +
                                ((left + up) / 2)));
                }

                return;

            case 4:
                // Paeth
                for (var i = 0;
                     i < row.Length;
                     i++)
                {
                    int left =
                        i >= bytesPerPixel
                            ? row[i - bytesPerPixel]
                            : 0;

                    int up =
                        previousRow[i];

                    int upperLeft =
                        i >= bytesPerPixel
                            ? previousRow[i - bytesPerPixel]
                            : 0;

                    int predictor =
                        PaethPredictor(
                            left,
                            up,
                            upperLeft);

                    row[i] =
                        unchecked(
                            (byte)(
                                row[i] +
                                predictor));
                }

                return;

            default:
                throw new InvalidDataException(
                    $"Unsupported PNG filter type: {filterType}.");
        }
    }

    private static int PaethPredictor(
        int left,
        int up,
        int upperLeft)
    {
        int p =
            left + up - upperLeft;

        int pa =
            Math.Abs(p - left);

        int pb =
            Math.Abs(p - up);

        int pc =
            Math.Abs(p - upperLeft);

        if (pa <= pb && pa <= pc)
            return left;

        if (pb <= pc)
            return up;

        return upperLeft;
    }

    private static uint ReadUInt32BE(
        Stream stream)
    {
        byte[] buffer = new byte[4];

        ReadExactly(
            stream,
            buffer);

        return BinaryPrimitives.ReadUInt32BigEndian(
            buffer);
    }

    private static void ReadExactly(
        Stream stream,
        Span<byte> buffer)
    {
        while (!buffer.IsEmpty)
        {
            int read =
                stream.Read(buffer);

            if (read == 0)
            {
                throw new EndOfStreamException(
                    "Unexpected end of PNG data.");
            }

            buffer =
                buffer[read..];
        }
    }
}