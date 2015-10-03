using Mono.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace SvgToIco
{

struct IcoResolution
{
    public readonly int Width;

    public byte IcoHeaderWidth
    {
        get
        {
            return (byte) Width;
        }
    }

    public readonly int Height;

    public byte IcoHeaderHeight
    {
        get
        {
            return (byte) Height;
        }
    }
    
    public IcoResolution(IcoSize size)
    {
        Width = size.Value;
        Height = size.Value;
    }
    
    public IcoResolution(IcoSize width, IcoSize height)
    {
        Width = width.Value;
        Height = height.Value;
    }
}

class IcoSize
{
    public readonly int Value;

    public static bool TryGetInstance(int value, out IcoSize size)
    {
        if (value < 0 || value > 256)
        {
            size = null;
            return false;
        } else
        {
            size = new IcoSize(value);
            return true;
        }
    }

    private IcoSize(int value)
    {
        Value = value;
    }
}

class Svg2IcoArguments
{
    public static bool TryParse(string[] arguments, out Svg2IcoArguments parsedArguments)
    {
        var showHelp = false;
        var recurse = false;
        var outdir = "";
        var resolutionsStr = new List<string>();
        var unprocessedArgs = new OptionSet()
            .Add("h|help", value => showHelp = true)
            .Add("r|recurse", value => recurse = true)
            .Add<string>("o=|outdir=", value => outdir = value)
            .Add<string>("s=|size=", value => resolutionsStr.Add(value))
            .Parse(arguments);

        // The option parser doesn't distinguish between unrecognized options, and
        // options after a '--', so we have to do this ourselves. *sigh*
        for (int i = 0; i < arguments.Length; ++i)
        {
            if (arguments[i] == "--")
            {
                int numTrailingArgs = arguments.Length - i - 1;
                int numInvalidArgs = unprocessedArgs.Count - numTrailingArgs;
                if (numInvalidArgs > 0)
                {
                    Console.Write("Unknown options:");
                    for (int j = 0; j < numInvalidArgs; ++j)
                    {
                        Console.Write(' ' + unprocessedArgs[j]);
                    }
                    Console.WriteLine("\n\n    Use the --help option to show available options.\n");
                    parsedArguments = null;
                    return false;
                }
                break;
            }
        }

        var inputPaths = new HashSet<string>(unprocessedArgs);

        var invalidResolutions = new List<string>();
        var allResolutions = new HashSet<IcoResolution>();
        foreach (var sizeStr in resolutionsStr)
        {
            IEnumerable<IcoResolution> resolutions;
            if (SizeSpecParser.TryParse(sizeStr, out resolutions))
            {
                allResolutions.UnionWith(resolutions);
            } else
            {
                invalidResolutions.Add(sizeStr);
            }
        }

        if (invalidResolutions.Count > 0)
        {
            foreach (var invalidResolution in invalidResolutions)
            {
                Console.WriteLine("Invalid resolution: " + invalidResolution);
            }
            parsedArguments = null;
            return false;
        } else
        {
            parsedArguments = new Svg2IcoArguments(
                showHelp,
                outdir,
                recurse,
                allResolutions,
                inputPaths);
            return true;
        }
    }

    public readonly bool ShowHelp;

    private readonly string outputDirectory;
    
    public bool HasOutputDirectory
    {
        get
        {
            return outputDirectory.Length > 0;
        }
    }

    public string OutputDirectory
    {
        get
        {
            if (!HasOutputDirectory)
            {
                throw new InvalidOperationException("No output directory is present");
            }
            return outputDirectory;
        }
    }

    public readonly bool Recurse;
    
    public readonly IReadOnlyCollection<IcoResolution> Resolutions;

    public readonly IReadOnlyCollection<string> InputPaths;

    public Svg2IcoArguments(
        bool showHelp,
        string outputDirectory,
        bool recurse,
        ISet<IcoResolution> resolutions,
        ISet<string> inputPaths)
    {
        ShowHelp = showHelp;
        this.outputDirectory = outputDirectory;
        Recurse = recurse;
        Resolutions = new ReadOnlyCollection<IcoResolution>(
            new List<IcoResolution>(resolutions));
        InputPaths = new ReadOnlyCollection<string>(
            new List<string>(inputPaths));
    }
}

static class SizeSpecParser
{
    private enum TokenType
    {
        EndOfInput,
        Error,
        DimensionSeparator,
        Number,
        RangeSeparator
    }

    public static bool TryParse(
        string sizeStr, out IEnumerable<IcoResolution> resolutions)
    {
        LexerEnumerator lexerEnumerator = new LexerEnumerator(sizeStr);
        var token1 = lexerEnumerator.NextToken();
        var token2 = lexerEnumerator.NextToken();
        var token3 = lexerEnumerator.NextToken();
        if (token1.Item1 == TokenType.Number
            && token2.Item1 == TokenType.EndOfInput
            && token3.Item1 == TokenType.EndOfInput)
        {
            int size;
            IcoSize icoSize;
            if (int.TryParse(token1.Item2, out size)
                && IcoSize.TryGetInstance(size, out icoSize))
            {
                resolutions = new IcoResolution[] { new IcoResolution(icoSize) };
                return true;
            }
        } else if (token1.Item1 == TokenType.Number
            && token2.Item1 == TokenType.DimensionSeparator
            && token3.Item1 == TokenType.Number)
        {
            int width;
            int height;
            IcoSize icoWidth;
            IcoSize icoHeight;
            if (int.TryParse(token1.Item2, out width)
                && int.TryParse(token3.Item2, out height)
                && IcoSize.TryGetInstance(width, out icoWidth)
                && IcoSize.TryGetInstance(height, out icoHeight))
            {
                resolutions = new IcoResolution[] { new IcoResolution(icoWidth, icoHeight) };
                return true;
            }
        } else if (token1.Item1 == TokenType.Number
            && token2.Item1 == TokenType.RangeSeparator
            && token3.Item1 == TokenType.Number)
        {
            uint start;
            uint end;
            if (uint.TryParse(token1.Item2, out start)
                && uint.TryParse(token3.Item2, out end)
                && start >= 0
                && end <= 8
                && end >= start)
            {
                int[] sizeLookupTable =
                {
                    1, 2, 4, 8, 16, 32, 64, 128, 256
                };

                var result = new List<IcoResolution>();
                for (uint i = start; i <= end; ++i)
                {
                    IcoSize icoSize;
                    // No need to do check the return value of GetInstance.
                    // Values are guaranteed to be in the right range anyways.
                    IcoSize.TryGetInstance(sizeLookupTable[i], out icoSize);
                    Debug.Assert(icoSize != null, "Expected icoSize to be set");
                    result.Add(new IcoResolution(icoSize));
                }
                resolutions = result;
                return true;
            }
        }

        resolutions = null;
        return false;
    }

    private class LexerEnumerator
    {
        private enum State
        {
            Start,
            NewToken,
            Number,
            OneDot,
            Error
        }

        private readonly CharEnumerator inputEnumerator;

        private State state;
        
        private readonly StringBuilder tokenBuilder = new StringBuilder();

        private bool endOfInput;

        public LexerEnumerator(string input)
        {
            inputEnumerator = input.GetEnumerator();
            state = State.Start;
            tokenBuilder = new StringBuilder();
            endOfInput = false;
        }

        private void moveNextCharacter()
        {
            endOfInput = !inputEnumerator.MoveNext();
        }

        public Tuple<TokenType, string> NextToken()
        {
            while (true)
            {
                switch (state)
                {
                    case State.Start:
                        moveNextCharacter();
                        if (endOfInput)
                        {
                            state = State.Error;
                        } else
                        {
                            state = State.NewToken;
                        }
                        break;
                    case State.NewToken:
                        if (endOfInput)
                        {
                            return Tuple.Create(TokenType.EndOfInput, "");
                        }

                        var c = inputEnumerator.Current;
                        if (char.IsDigit(c))
                        {
                            state = State.Number;
                        } else if (c == 'x')
                        {
                            moveNextCharacter();
                            return Tuple.Create(TokenType.DimensionSeparator, "x");
                        } else if (c == '.')
                        {
                            moveNextCharacter();
                            state = State.OneDot;
                        }
                        break;
                    case State.Number:
                        if (endOfInput || !char.IsDigit(inputEnumerator.Current))
                        {
                            var number = tokenBuilder.ToString();
                            tokenBuilder.Clear();
                            state = State.NewToken;
                            return Tuple.Create(TokenType.Number, number);
                        } else
                        {
                            tokenBuilder.Append(inputEnumerator.Current);
                            moveNextCharacter();
                        }

                        break;
                    case State.OneDot:
                        if (endOfInput || inputEnumerator.Current != '.')
                        {
                            state = State.Error;
                        } else
                        {
                            moveNextCharacter();
                            state = State.NewToken;
                            return Tuple.Create(TokenType.RangeSeparator, "..");
                        }
                        break;
                    case State.Error:
                        return Tuple.Create(TokenType.Error, "");
                }
            }
        }
    }
}

}
