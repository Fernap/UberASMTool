// consider requiring quotes for filenames with spaces (and thus not caring about the extension)
// this would also allow ";" or "#" in filenames, which is nice (but not quotes themselves)
// a comma in a filename would also need to be quoted

using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace UberASMTool;

// A private class to hold a couple extension methods for Pidgin
public static class MyParserExtensions
{
    // p.OptionalThen(yes, no)
    // creates a new parser which returns the result of yes if the original parser succeeds,
    // or returns the result of no if it fails (without consuming input)
    public static Parser<TToken, U> OptionalThen<TToken, T, U>(this Parser<TToken, T> p, Parser<TToken, U> yes, Parser<TToken, U> no) =>
        p.Optional().Bind(t => t.HasValue ? yes : no);

    // p.OptionalThen(yes, no, f)
    // creates a new parser which returns f() applied to the return values of p and yes if the original parser succeeds,
    // or returns the result of no if it fails (without consuming input)
    // Currently unused, but it's potentially helpful down the road, and OrFail() could have been written in terms of it
    public static Parser<TToken, TReturn> OptionalThen<TToken, TThis, TYes, TReturn>
        (this Parser<TToken, TThis> p, Parser<TToken, TYes> yes, Parser<TToken, TReturn> no, Func<TThis, TYes, TReturn> f) =>
            p.Optional().Bind(t => t.HasValue ? yes.Select(u => f(t.Value, u)) : no);

    // p.OrFail(string msg)
    // creates a new parser which fails with the specified error message if the current parser fails without consuming input
    public static Parser<TToken, T> OrFail<TToken, T>(this Parser<TToken, T> p, string msg) =>
        p.Optional().Bind(t => t.HasValue ? Parser<TToken>.Return(t.Value) : Parser<TToken>.Fail<T>(msg));
    //  p.OptionalThen<TToken, T, Unit, T>(Return(Unit.Value), Fail<T>(msg), (x, _) => x);
}

public static class ListParser
{
    private static readonly Parser<char, char> comma = Char(',');
    private static readonly Parser<char, char> colon = Char(':');
    private static readonly Parser<char, char> wschar = OneOf(' ', '\t');              // not including eol
    private static readonly Parser<char, char> commentchar = OneOf(';', '#');

    private static readonly Parser<char, Unit> skip_ws = wschar.SkipMany();             // this similar method that comes with Pidgin matches other weird unicode stuff...
                                                                                        // but I can't use that anyway since I don't want to match EOL
    private static readonly Parser<char, Unit> eofeol = End.Or(Try(EndOfLine).IgnoreResult());                    // Try() might be overkill, but whatever
    private static readonly Parser<char, Unit> comment = commentchar.Then(Any.SkipUntil(eofeol));   // only consumes input if successful
    private static readonly Parser<char, Unit> trim_end = skip_ws.Then(comment.Or(eofeol).OrFail("Unexpected character found before end of line."));
    private static readonly Parser<char, Unit> skip_until_important = Not(End).Then(Try(trim_end)).Many().Then(skip_ws);

    private static Parser<char, string> filename(string extension) =>
        Not(Try(EndOfLine)).Then(AnyCharExcept(';', '#')).AtLeastOnceUntil(Try(String("." + extension))).
        Select(cs => string.Concat(cs) + "." + extension);

    private static readonly Parser<char, string> asm_file = filename("asm");
    private static readonly Parser<char, string> rom_file = Try(filename("smc")).Or(filename("sfc"));

    // ----------------------------------------------------------------
    // number stuff

    // .Net 7 only --
    //   private static readonly Parser<char, char> hex_digit = Try(Any.Where(c => System.Char.IsAsciiHexDigit(c)));
    //   private static readonly Parser<char, char> decimal_digit = Try(Any.Where(c => System.Char.IsAsciiDigit(c)));
    private static readonly Parser<char, char> hex_digit = OneOf("0123456789abcdefABCDEF".ToCharArray());
    private static readonly Parser<char, char> binary_digit = OneOf('0', '1');
    private static readonly Parser<char, char> decimal_digit = OneOf("0123456789".ToCharArray());

    // prefixes will not consume input on failure
    // not allowing 0b and 0d for binary and decimal respectively since things like
    // 0b0 could either mean binary 0 or hex B0, since hex is the default
    private static readonly Parser<char, Unit> hex_prefix =
        Try(String("0x").IgnoreResult()).Or(Char('$').IgnoreResult());  // this can also be Try(String("0x")).Or(String("$")).IgnoreResult()
    private static readonly Parser<char, Unit> binary_prefix =
        Char('%').IgnoreResult();
    private static readonly Parser<char, Unit> decimal_prefix =
        Char('@').IgnoreResult();  // maybe '&' instead?

    // this is a little hacky, but whatever
    private static readonly Parser<char, Unit> digit_terminator =
        OneOf(';', '#', ' ', '\t', ',').IgnoreResult().Or(eofeol.IgnoreResult());
    private static Parser<char, string> digits(Parser<char, char> digit, int max) =>
        from cs in digit.AtLeastOnce().Before(Lookahead(digit_terminator))
        let s = string.Concat(cs)
        where s.Length <= max
        select s;

    private static readonly Parser<char, int> hex_byte =
        hex_prefix.Optional().Then(digits(hex_digit, 2)).Select(s => System.Convert.ToInt32(s, 16));
    private static readonly Parser<char, int> binary_byte =
        binary_prefix.Then(digits(binary_digit, 8)).Select(s => System.Convert.ToInt32(s, 2));
    private static readonly Parser<char, int> decimal_byte =
        decimal_prefix.Then(digits(decimal_digit, 3)).Select(s => System.Convert.ToInt32(s, 10));

    private static readonly Parser<char, int> positive_byte =
        Try(binary_byte).
        Or(Try(decimal_byte).Where(n => n < 256)).
        Or(hex_byte);
    private static readonly Parser<char, int> signed_byte =
        Char('-').Optional().Then(positive_byte, (sign, x) => x > 0 && sign.HasValue ? 256 - x : x);

    // ----------------------------------------------------------------

    private static readonly Parser<char, int> snes_address =
        hex_prefix.Optional().Then(digits(hex_digit, 6)).Select(s => System.Convert.ToInt32(s, 16));
    private static readonly Parser<char, int> resource_num =
        Char('*').Before(Lookahead(digit_terminator)).ThenReturn(-1).
        Or(hex_prefix.Optional().Then(digits(hex_digit, 3)).Select(s => System.Convert.ToInt32(s, 16)));
    private static readonly Parser<char, IEnumerable<int>> extra_bytes =
        signed_byte.SeparatedAndOptionallyTerminatedAtLeastOnce(skip_ws);
    private static readonly Parser<char, bool> onoff =
        Try(CIString("on").ThenReturn(true)).
        Or(Try(CIString("off").ThenReturn(false)));
    private static readonly Parser<char, VerboseLevel> verbosity =
        Try(CIString("on").ThenReturn(VerboseLevel.Verbose)).
        Or(Try(CIString("off").ThenReturn(VerboseLevel.Normal))).
        Or(Try(CIString("quiet").ThenReturn(VerboseLevel.Quiet))).
        Or(Try(CIString("debug").ThenReturn(VerboseLevel.Debug)));

    // this allows a line break after resource num, but not an empty list, so e.g.
    // 105
    //    106 foo.asm
    // will parse, but will think it's 105 with file "106 foo.asm"
    //    private static readonly Parser<char, IEnumerable<ResourceStatement.UberResource>> files_and_bytes =
    //      file_and_bytes.SeparatedAtLeastOnce(Try(comma.Between(skip_until_important)));
    private static readonly Parser<char, ResourceStatement.Call> file_and_bytes =
        from f in asm_file.OrFail("Invalid resource filename.").Before(skip_ws)
        from bs in Char(':').OptionalThen(skip_ws.Then(Try(extra_bytes).OrFail("Invalid extra byte.")), Return(Enumerable.Empty<int>()))
        select new ResourceStatement.Call { Filename = f, Bytes = bs.ToList() };
    private static readonly Parser<char,IEnumerable<ResourceStatement.Call>> files_and_bytes =
        file_and_bytes.Before(skip_ws).SeparatedAtLeastOnce(comma.Then(skip_until_important));

    // ----------------------------------------------------------------------

    // making the "cmd:" part of cmd() not consume input upon failure since it's exclusively needed like that
    // but if any actual commands match the command part of the string..any subsequent failure will have consumed input
    // the skip_ws at the end is safe..some commands have nothing else after, but this doesn't hurt them
    private static Parser<char, T> cmd<T>(string name, Parser<char, T> arg, string err) =>
        Try(CIString(name).Then(Char(':'))).Then(skip_ws).
        Then(Try(arg).OrFail(err)).
        Before(trim_end);

    private static readonly Parser<char, ConfigStatement> verbose_statement =
        cmd("verbose", verbosity, "Invalid argument to \"verbose:\".  Must be \"on\", \"off\", \"quiet\", or \"debug\".").
        Select(b => (ConfigStatement) new VerboseStatement { Verbosity = b });
    private static readonly Parser<char, ConfigStatement> deprecations_statement =
        cmd("deprecations", onoff, "Invalid argument to \"deprecations:\".  Must be \"on\" of \"off\".").
        Select(b => (ConfigStatement) new DeprecationsStatement { Warn = b });

    private static Parser<char, ConfigStatement> mode_statement(string mode, UberContextType n) =>
        cmd(mode, Return(Unit.Value), "").ThenReturn((ConfigStatement) new ModeStatement { Mode = n });
    private static readonly Parser<char, ConfigStatement> level_statement = mode_statement("level", UberContextType.Level);
    private static readonly Parser<char, ConfigStatement> overworld_statement = mode_statement("overworld", UberContextType.Overworld);
    private static readonly Parser<char, ConfigStatement> gamemode_statement = mode_statement("gamemode", UberContextType.Gamemode);


    private static Parser<char, ConfigStatement> file_statement(string name, FileStatement statement) =>
        cmd(name, asm_file, $"Invalid filename in \"{name}:\" command.").
        Select(f => { statement.Filename = f; return (ConfigStatement) statement; } );
    private static readonly Parser<char, ConfigStatement> global_statement = file_statement("global", new GlobalFileStatement());
    private static readonly Parser<char, ConfigStatement> statusbar_statement = file_statement("statusbar", new StatusbarFileStatement());
    private static readonly Parser<char, ConfigStatement> macrolib_statement = file_statement("macrolib", new MacrolibFileStatement());
    private static readonly Parser<char, ConfigStatement> rom_statement =
        cmd("rom", rom_file, "Invalid filename in \"rom:\" command.").
        Select(f => (ConfigStatement) new ROMStatement { Filename = f } );

    private static readonly Parser<char, ConfigStatement> freeram_statement =
        cmd("freeram", snes_address, "Invalid SNES address in \"freeram:\" command.").Select(x => (ConfigStatement) new FreeramStatement { Addr = x });

    // these could just as well be separate statements, but ehh, whatever
    private static readonly Parser<char, ConfigStatement> sprite_statement =
        cmd("sprite", Fail<ConfigStatement>(), "\"sprite:\" and \"sprite-sa1:\" are no longer used.  See the readme for more information.").
        Or(cmd("sprite-sa1", Fail<ConfigStatement>(), "\"sprite:\" and \"sprite-sa1:\" are no longer used.  See the readme for more information."));

    private static readonly Parser<char, ConfigStatement> resource_statement =
        from num in resource_num.Before(skip_until_important)
        from fs in files_and_bytes
        select (ConfigStatement) new ResourceStatement { Number = num, Calls = fs.ToList() };

    private static readonly List<Parser<char, ConfigStatement>> all_statements =
        new List<Parser<char, ConfigStatement>> { verbose_statement, deprecations_statement,
                                                  level_statement, overworld_statement, gamemode_statement,
                                                  global_statement, statusbar_statement, macrolib_statement, rom_statement,
                                                  sprite_statement, freeram_statement,
                                                  resource_statement };

    private static readonly Parser<char, ConfigStatement> statement =
         CurrentPos.Then(OneOf(all_statements), (off, s) => { s.Line = off.Line; return s; } );

    // skip_until_important should fail only at EOF
    // statement can fail at EOF, which consumes no input, and will end the parse
    private static readonly Parser<char, IEnumerable<ConfigStatement>> main_parser =
        skip_until_important.Then(statement.SeparatedAndOptionallyTerminated(skip_until_important)).
        Before(End).OrFail("Unknown command.");

// --------------------------------------------------------------------------------------

    public static IEnumerable<ConfigStatement> ParseList(string listFile)
    {
        Result<char, IEnumerable<ConfigStatement>> res;
        string listText;

        if (!File.Exists(listFile))
        {
            MessageWriter.Write(VerboseLevel.Quiet, $"Cannot read list file \"{listFile}\"");
            return null;
        }

        try
        {
            listText = File.ReadAllText(listFile);
        }
        catch (Exception e)
        {
            MessageWriter.Write(VerboseLevel.Quiet, $"Cannot read list file \"{listFile}\": {e}");
            return null;
        }

        res = main_parser.Parse(listText, null);
        if (res.Success)
            return res.Value;

        // TODO: come up with better generic parse errors
        MessageWriter.Write(VerboseLevel.Quiet, res.Error.ToString());
        return null;
    }

}
