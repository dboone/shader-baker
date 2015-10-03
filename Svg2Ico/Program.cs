//svg2ico [--help] [--outdir <directory>] [--recurse] [--size <sizespec>] --[<path>...]
//
// # Options
//
// <files>
//
//     A list of files and directories in which to search for .svg files
//     to convert.
//
// <directory>
//
//     The directory in which to write .ico files. Writes them next to the
//     .svg image that generated them if not specified.
//
// <path>
//
//     An SVG file, or a directory in which to look for .svg files.
//		
// <sizespec>
//
//     Resolution(s) to write in each .ico file. See below for the
//     format specification.
//
// -h, --help
//
//     Prints some help text
//
// -o, --outdir
//
//     Specifies the output directory
//
// -r, --recurse
//
//     Recurse into any input directories when looking for .svg files.
//
// -s, --size
//
//     Specifies a resolution to place in each output .ico file. Use this
//     option multiple times to specify multiple resolutions.
//
// # Size Specification
//
// Square: <s>
//
//     Where `s` is a number in the range [0, 256].
//
// Rectangle: <w>x<h>
//
//     Where `w` and `h` are numbers in the range [0, 256].
//
// Square powers of two: <n>..<m>
//
//     Where `n` and `m` are numbers in the range [0, 8] and m>= n. Represents
//     images at resolutions of 2^n x 2^n, 2^(n+1) x 2^(n+1),..., 2^m x 2^m.

using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Xml;

namespace Svg2Ico
{

class Program
{
    private const string ICO_EXTENSION = ".ico";

    private static void Main(string[] args)
    {
        Svg2IcoArguments parsedArgs;
        if (!Svg2IcoArguments.TryParse(args, out parsedArgs))
        {
            return;
        }

        if (parsedArgs.ShowHelp)
        {
            ShowHelp();
        }

        var converterInputs = readSvgPaths(
            parsedArgs.InputPaths,
            parsedArgs.HasOutputDirectory ? parsedArgs.OutputDirectory : "",
            parsedArgs.Recurse);
        
        foreach (var item in Enumerable.Select(converterInputs, (v, i) => new { v, i }))
        {
            var bitmaps = parsedArgs.Resolutions
                .Select(res => DrawBitmap(item.v.SvgDocument, res.Width, res.Height))
                .ToList();
            WriteToIcoFile(item.v.OutputFileName, bitmaps);
            Console.WriteLine(
                " (" + (item.i + 1) + "/" + converterInputs.Count
                + ") Icon written to: " + item.v.OutputFileName);
        }
    }

    private static ICollection<ConverterInputs> readSvgPaths(
        IEnumerable<string> inputPaths,
        string outputDirectory,
        bool recurse)
    {
        var results = new List<ConverterInputs>();
        foreach (var path in inputPaths)
        {
            if (Directory.Exists(path))
            {
                var searchOption = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (
                    var fileName
                    in Directory.EnumerateFiles(path, "*.svg", searchOption))
                {
                    readSvgFile(fileName, outputDirectory, results);
                }
            } else
            {
                readSvgFile(path, outputDirectory, results);
            }
        }
        return results;
    }

    private static void readSvgFile(
        string fileName, string outputDirectory, ICollection<ConverterInputs> results)
    {
        try
        {
            var doc = SvgDocument.Open(fileName);
            string outputFileName;
            if (outputDirectory.Length > 0)
            {
                outputFileName = Path.Combine(
                    outputDirectory, Path.GetFileNameWithoutExtension(fileName) + ICO_EXTENSION);
            } else
            {
                outputFileName = Path.ChangeExtension(fileName, ICO_EXTENSION);
            }
            results.Add(new ConverterInputs(doc, outputFileName));
        } catch (FileNotFoundException)
        {
            Console.WriteLine("File does not exist: " + fileName);
        } catch (DirectoryNotFoundException)
        {
            Console.WriteLine("File does not exist: " + fileName);
        } catch (PathTooLongException)
        {
            Console.WriteLine("File name is not valid: " + fileName);
        } catch (IOException)
        {
            Console.WriteLine("File is in use by another process: " + fileName);
        } catch (ArgumentException)
        {
            Console.WriteLine("File name is not valid: " + fileName);
        } catch (NotSupportedException)
        {
            Console.WriteLine("File name is not valid: " + fileName);
        } catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Insufficient permission to access file: " + fileName);
        } catch (XmlException)
        {
            Console.WriteLine("SVG file does not contain valid XML: " + fileName);
        } catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    
    private static Bitmap DrawBitmap(SvgDocument doc, int width, int height)
    {
        doc.Width = width;
        doc.Height = height;
        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        doc.Draw(bmp);
        return bmp;
    }

    private static void WriteToIcoFile(string fileName, IList<Bitmap> bitmaps)
    {
        using (var outputStream = new FileStream(fileName, FileMode.Create))
        {
            WriteIcoToStream(outputStream, bitmaps);
        }
    }

    private static void WriteIcoToStream(Stream stream, IList<Bitmap> bitmaps)
    {
        if (bitmaps.Any(bmp => bmp.Width> 256 || bmp.Height> 256))
        {
            throw new ArgumentException(
                "ICO files cannot contain images with dimensions greater than 256x256 pixels");
        }
        bitmaps = bitmaps.Where(bmp => bmp.Width> 0 && bmp.Height> 0).ToList();

        if (bitmaps.Count> ushort.MaxValue)
        {
            throw new ArgumentException(
                "ICO files can only contain up to " + short.MaxValue + " images.");
        }

        IList<byte[]> pngImages = new List<byte[]>(bitmaps.Count);
        foreach (var bmp in bitmaps)
        {
            using (var pngStream = new MemoryStream())
            {
                bmp.Save(pngStream, ImageFormat.Png);
                pngImages.Add(pngStream.ToArray());
            }
        }
        
        var numImages = (ushort) bitmaps.Count;
        WriteIcoMainHeader(stream, numImages);

        uint headerSize = 6;
        uint imageHeaderSize = 16;
        uint offset = headerSize + numImages * imageHeaderSize;
        foreach (var images in bitmaps.Zip(pngImages, Tuple.Create))
        {
            var bmp = images.Item1;
            var png = images.Item2;
            var imageSize = (uint) png.Length;
            // The direct cast to a byte here is safe. We validate the image size above,
            // so all images have a width/height in the range [1, 256]. Casting [1, 255]
            // to a byte returns the same number, and casting 256 returns 0, which represents
            // an image of size 256 bytes in the ICO format.
            WriteIcoImageHeader(stream, (byte) bmp.Width, (byte) bmp.Height, imageSize, offset);
            offset += imageSize;
        }

        foreach (var png in pngImages)
        {
            stream.Write(png, 0, png.Length);
        }
    }
    
    private static void WriteIcoMainHeader(Stream stream, ushort numberImages)
    {
        var numberImagesLe = ToBytesLittleEndian(numberImages);
        byte[] header = {
            0, 0,
            1, 0,
            numberImagesLe[0], numberImagesLe[1]};
        stream.Write(header, 0, header.Length);
    }

    private static void WriteIcoImageHeader(
        Stream stream,
        byte imageWidth,
        byte imageHeight,
        uint imageSizeBytes,
        uint imageOffsetBytes)
    {
        var sizeLe = ToBytesLittleEndian(imageSizeBytes);
        var offsetLe = ToBytesLittleEndian(imageOffsetBytes);
        byte[] header = {
            imageWidth,
            imageHeight,
            0,
            0,
            0, 0,
            32, 0,
            sizeLe[0], sizeLe[1], sizeLe[2], sizeLe[3],
            offsetLe[0], offsetLe[1], offsetLe[2], offsetLe[3]};
        stream.Write(header, 0, header.Length);
    }

    private static byte[] ReverseIfBigEndian(byte[] bytes)
    {
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return bytes;
    }

    private static byte[] ToBytesLittleEndian(ushort value)
    {
        return ReverseIfBigEndian(BitConverter.GetBytes(value));
    }

    private static byte[] ToBytesLittleEndian(uint value)
    {
        return ReverseIfBigEndian(BitConverter.GetBytes(value));
    }

    private static void ShowHelp()
    {
        Console.WriteLine(
@"svg2ico[--help][--outdir<directory>][--recurse][--size<sizespec>]--[< path>...]

# Options

<files>

    A list of files and directories in which to search for .svg files
    to convert.

<directory>

    The directory in which to write.ico files.Writes them next to the
    .svg image that generated them if not specified.

<path>

    An SVG file, or a directory in which to look for .svg files.

<sizespec>

    Resolution(s) to write in each.ico file.See below for the
    format specification.

-h, --help

    Prints this text

-o, --outdir

    Specifies the output directory

-r, --recurse

    Recurse into any input directories when looking for .svg files.

-s, --size

    Specifies a resolution to place in each output.ico file. Use this
    option multiple times to specify multiple resolutions.

# Size Specification

Square: <s>

    Where `s` is a number in the range[0, 256]

Rectangle: <w> x <h>

    Where `w` and `h` are numbers in the range[0, 256]

Square powers of two: <n>..<m>

    Where `n` and `m` are numbers in the range[0, 8] and m >= n. Represents
    images at resolutions of 2 ^ n x 2 ^ n, 2 ^ (n + 1) x 2 ^ (n + 1),..., 2 ^ m x 2 ^ m");

    }

    private class ConverterInputs
    {
        public readonly SvgDocument SvgDocument;
        public readonly string OutputFileName;

        public ConverterInputs(
            SvgDocument svgDocument, string outputFileName)
        {
            SvgDocument = svgDocument;
            OutputFileName = outputFileName;
        }
    }
}

}
