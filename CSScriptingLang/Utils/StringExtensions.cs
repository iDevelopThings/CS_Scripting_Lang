using System.Reflection;
using System.Text;

namespace CSScriptingLang.Utils;

public static class StringExtensions
{
    public static string ToColoredTimeString(this TimeSpan @this) {

        var formattedTime = @this.FormattedTime();

        var timeColor = @this.TotalMilliseconds switch {
            < 1    => AnsiCodes.BrightGreen,
            < 10   => AnsiCodes.Green,
            < 100  => AnsiCodes.Yellow,
            < 1000 => AnsiCodes.Red,
            _      => AnsiCodes.BrightRed
        };

        return formattedTime.Colored(timeColor);
    }
    private static string FormattedTime(this TimeSpan @this) {
        string formattedTime;
        if (@this.TotalMilliseconds < 1) {
            formattedTime = $"{@this.TotalMicroseconds}us";
        } else if (@this.TotalSeconds < 1) {
            formattedTime = $"{@this.TotalMilliseconds:F1}ms";
        } else {
            formattedTime = $"{@this.TotalSeconds:F1}s";
        }

        return formattedTime;
    }

    public static string FirstCharToLower(this string @this) {
        if (string.IsNullOrEmpty(@this)) {
            return @this;
        }

        var a = @this.ToCharArray();
        a[0] = char.ToLower(a[0]);
        return new string(a);
    }

    public static string ToSnakeCase(this string @this) {
        if (string.IsNullOrEmpty(@this)) {
            return @this;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < @this.Length; i++) {
            var c = @this[i];
            if (char.IsUpper(c)) {
                if (i > 0) {
                    sb.Append('_');
                }

                sb.Append(char.ToLower(c));
            } else {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    public static string ToClassNameSpaceMethodName(this MethodInfo method, bool includeNs = true) {
        var name      = method.Name;
        var className = method.DeclaringType?.Name;
        var ns        = method.DeclaringType?.Namespace;

        if (!includeNs)
            return $"{className}.{name}";

        return $"{ns}.{className}.{name}";
    }

    public static string ToShortName(this Type @this) {
        var name = @this.Name;
        name = name.Split('.').Last();

        if (@this.IsGenericType) {
            var index = name.IndexOf('`');
            if (index != -1) {
                name = name[..index];
            }

            name += "<";
            var genericArgs = @this.GetGenericArguments();
            for (var i = 0; i < genericArgs.Length; i++) {
                name += genericArgs[i].ToShortName();
                if (i < genericArgs.Length - 1) {
                    name += ", ";
                }
            }

            name += ">";
        }

        return name;
    }
    
    public static string ToFullLinkedName(this Type @this) {
        var name = @this.Name;
        name = name.Split('.').Last();

        if (@this.IsGenericType) {
            var index = name.IndexOf('`');
            if (index != -1) {
                name = name[..index];
            }

            name += "<";
            var genericArgs = @this.GetGenericArguments();
            for (var i = 0; i < genericArgs.Length; i++) {
                name += genericArgs[i].ToShortName();
                if (i < genericArgs.Length - 1) {
                    name += ", ";
                }
            }

            name += ">";
        }

        return $"{@this.Namespace}.{name}";
    }
}

public enum AnsiColorCodes
{
    Gray,
    BrightGray,
    Red,
    BrightRed,
    Green,
    BrightGreen,
    Yellow,
    BrightYellow,
    Blue,
    BrightBlue,
    Magenta,
    BrightMagenta,
    Cyan,
    BrightCyan,
    White,
    BrightWhite
}

public static class AnsiColorExtensions
{
    public static string ToAnsiColor(this AnsiColorCodes colorCodes) {
        return colorCodes switch {
            AnsiColorCodes.Gray          => AnsiCodes.Gray,
            AnsiColorCodes.BrightGray    => AnsiCodes.BrightGray,
            AnsiColorCodes.Red           => AnsiCodes.Red,
            AnsiColorCodes.BrightRed     => AnsiCodes.BrightRed,
            AnsiColorCodes.Green         => AnsiCodes.Green,
            AnsiColorCodes.BrightGreen   => AnsiCodes.BrightGreen,
            AnsiColorCodes.Yellow        => AnsiCodes.Yellow,
            AnsiColorCodes.BrightYellow  => AnsiCodes.BrightYellow,
            AnsiColorCodes.Blue          => AnsiCodes.Blue,
            AnsiColorCodes.BrightBlue    => AnsiCodes.BrightBlue,
            AnsiColorCodes.Magenta       => AnsiCodes.Magenta,
            AnsiColorCodes.BrightMagenta => AnsiCodes.BrightMagenta,
            AnsiColorCodes.Cyan          => AnsiCodes.Cyan,
            AnsiColorCodes.BrightCyan    => AnsiCodes.BrightCyan,
            AnsiColorCodes.White         => AnsiCodes.White,
            AnsiColorCodes.BrightWhite   => AnsiCodes.BrightWhite,
            _                       => AnsiCodes.Reset
        };
    }
}

public static class AnsiCodes
{
    // ANSI escape code definitions
    public const string Reset     = "\u001b[0m";
    public const string Bold      = "\u001b[1m";
    public const string Underline = "\u001b[4m";

    public const string Gray       = "\u001b[30m";
    public const string BrightGray = "\u001b[90m";

    public const string Red       = "\u001b[31m";
    public const string BrightRed = "\u001b[91m";

    public const string Green       = "\u001b[32m";
    public const string BrightGreen = "\u001b[92m";

    public const string Yellow       = "\u001b[33m";
    public const string BrightYellow = "\u001b[93m";

    public const string Blue       = "\u001b[34m";
    public const string BrightBlue = "\u001b[94m";

    public const string Magenta       = "\u001b[35m";
    public const string BrightMagenta = "\u001b[95m";

    public const string Cyan       = "\u001b[36m";
    public const string BrightCyan = "\u001b[96m";

    public const string White       = "\u001b[37m";
    public const string BrightWhite = "\u001b[97m";

    public static string[] AllModifiers      = [Reset, Bold, Underline];
    public static string[] AllModifiersNames = ["Reset", "Bold", "Underline"];

    public static string[] AllColorNames = [
        "Gray", "BrightGray",
        "Red", "BrightRed",
        "Green", "BrightGreen",
        "Yellow", "BrightYellow",
        "Blue", "BrightBlue",
        "Magenta", "BrightMagenta",
        "Cyan", "BrightCyan",
        "White", "BrightWhite"
    ];

    public static string[] AllColors = {
        Gray, BrightGray,
        Red, BrightRed,
        Green, BrightGreen,
        Yellow, BrightYellow,
        Blue, BrightBlue,
        Magenta, BrightMagenta,
        Cyan, BrightCyan,
        White, BrightWhite
    };

    public static Dictionary<string, string> ColorMap      = AllColorNames.Zip(AllColors).ToDictionary();
    public static Dictionary<string, string> ColorMapLower = AllColorNames.Select(s => s.ToLower()).Zip(AllColors).ToDictionary();

    public static Dictionary<string, string> ModifierMap      = AllModifiersNames.Zip(AllModifiers).ToDictionary();
    public static Dictionary<string, string> ModifierMapLower = AllModifiersNames.Select(s => s.ToLower()).Zip(AllModifiers).ToDictionary();

    public static string[] AllColorsAndModifiers      = AllColors.Concat(AllModifiers).ToArray();
    public static string[] AllColorsAndModifiersNames = AllColorNames.Concat(AllModifiersNames).ToArray();

    public static Dictionary<string, string> AllColorsAndModifiersMap      = AllColorsAndModifiersNames.Zip(AllColorsAndModifiers).ToDictionary();
    public static Dictionary<string, string> AllColorsAndModifiersMapLower = AllColorsAndModifiersNames.Select(s => s.ToLower()).Zip(AllColorsAndModifiers).ToDictionary();

    public static bool LookupColorOrModifier(string tag, out List<string> colorsAndModifiers) {
        colorsAndModifiers = new List<string>();

        // Split by '.' and check if each part is a color or modifier
        var parts = tag.Split('.');
        foreach (var part in parts) {
            if (AllColorsAndModifiersMapLower.TryGetValue(part.ToLower(), out var colorOrModifier)) {
                colorsAndModifiers.Add(colorOrModifier);
            }
        }

        return colorsAndModifiers.Count > 0;
    }
}

public static class AnsiStringExtensions
{
    public static string Reset(this     string str) => $"{AnsiCodes.Reset}{str}{AnsiCodes.Reset}";
    public static string Bold(this      string str) => $"{AnsiCodes.Bold}{str}{AnsiCodes.Reset}";
    public static string Underline(this string str) => $"{AnsiCodes.Underline}{str}{AnsiCodes.Reset}";

    public static string Colored(this string str, string color) => $"{color}{str}{AnsiCodes.Reset}";

    public static string Gray(this           string str) => $"{AnsiCodes.Gray}{str}{AnsiCodes.Reset}";
    public static string BoldGray(this       string str) => str.Bold().Gray();
    public static string BrightGray(this     string str) => $"{AnsiCodes.BrightGray}{str}{AnsiCodes.Reset}";
    public static string BoldBrightGray(this string str) => str.Bold().BrightGray();

    public static string Red(this           string str) => $"{AnsiCodes.Red}{str}{AnsiCodes.Reset}";
    public static string BoldRed(this       string str) => str.Bold().Red();
    public static string BrightRed(this     string str) => $"{AnsiCodes.BrightRed}{str}{AnsiCodes.Reset}";
    public static string BoldBrightRed(this string str) => str.Bold().BrightRed();

    public static string Green(this           string str) => $"{AnsiCodes.Green}{str}{AnsiCodes.Reset}";
    public static string BoldGreen(this       string str) => str.Bold().Green();
    public static string BrightGreen(this     string str) => $"{AnsiCodes.BrightGreen}{str}{AnsiCodes.Reset}";
    public static string BoldBrightGreen(this string str) => str.Bold().BrightGreen();

    public static string Yellow(this           string str) => $"{AnsiCodes.Yellow}{str}{AnsiCodes.Reset}";
    public static string BoldYellow(this       string str) => str.Bold().Yellow();
    public static string BrightYellow(this     string str) => $"{AnsiCodes.BrightYellow}{str}{AnsiCodes.Reset}";
    public static string BoldBrightYellow(this string str) => str.Bold().BrightYellow();

    public static string Blue(this           string str) => $"{AnsiCodes.Blue}{str}{AnsiCodes.Reset}";
    public static string BoldBlue(this       string str) => str.Bold().Blue();
    public static string BrightBlue(this     string str) => $"{AnsiCodes.BrightBlue}{str}{AnsiCodes.Reset}";
    public static string BoldBrightBlue(this string str) => str.Bold().BrightBlue();

    public static string Magenta(this           string str) => $"{AnsiCodes.Magenta}{str}{AnsiCodes.Reset}";
    public static string BoldMagenta(this       string str) => str.Bold().Magenta();
    public static string BrightMagenta(this     string str) => $"{AnsiCodes.BrightMagenta}{str}{AnsiCodes.Reset}";
    public static string BoldBrightMagenta(this string str) => str.Bold().BrightMagenta();

    public static string Cyan(this           string str) => $"{AnsiCodes.Cyan}{str}{AnsiCodes.Reset}";
    public static string BoldCyan(this       string str) => str.Bold().Cyan();
    public static string BrightCyan(this     string str) => $"{AnsiCodes.BrightCyan}{str}{AnsiCodes.Reset}";
    public static string BoldBrightCyan(this string str) => str.Bold().BrightCyan();

    public static string White(this           string str) => $"{AnsiCodes.White}{str}{AnsiCodes.Reset}";
    public static string BoldWhite(this       string str) => str.Bold().White();
    public static string BrightWhite(this     string str) => $"{AnsiCodes.BrightWhite}{str}{AnsiCodes.Reset}";
    public static string BoldBrightWhite(this string str) => str.Bold().BrightWhite();

    public static string ColorIf(this string str, bool condition, AnsiColorCodes colorCodes) => str.ColorIf(condition, colorCodes.ToAnsiColor());
    public static string ColorIf(this string str, bool condition, string color) {
        return condition ? str.Colored(color) : str;
    }

    /// <summary>
    /// This will parse our string and replace <colorName>some text</> with the appropriate color tag
    /// </summary>
    public static string ApplyColorTags(this string str, bool allowColors = true) {
        if (!allowColors) {
            return str;
        }

        var sb         = new StringBuilder();
        var colorStack = new Stack<string>();

        var i = 0;
        while (i < str.Length) {
            if (str[i] == '<') {
                var end = str.IndexOf('>', i);
                if (end == -1) {
                    sb.Append(str[i..]);
                    break;
                }

                var colorName = str[(i + 1)..end];
                if (colorName == "/") {
                    if (colorStack.Count == 0) {
                        sb.Append(str[i..(end + 1)]);
                    } else {
                        sb.Append(colorStack.Pop());
                    }
                } else if (AnsiCodes.LookupColorOrModifier(colorName, out var colors)) {
                    foreach (var color in colors) {
                        sb.Append(color);
                        colorStack.Push(AnsiCodes.Reset);
                    }
                } else {
                    sb.Append(str[i..(end + 1)]);
                }

                i = end + 1;
            } else {
                sb.Append(str[i]);
                i++;
            }
        }

        while (colorStack.Count > 0) {
            sb.Append(AnsiCodes.Reset);
            colorStack.Pop();
        }

        return sb.ToString();
    }
}