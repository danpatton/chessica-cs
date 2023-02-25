// See https://aka.ms/new-console-template for more information

using Chessica;
using Chessica.Uci;
using Serilog;

Console.Out.NewLine = "\n";

var now = DateTime.Now;
using var logger = new LoggerConfiguration()
    // add console as logging target
    .WriteTo.File($@"C:\Users\HP\AppData\Local\Chessica\log_{now:yyyyMMdd_HHmmss}.txt")
    // add debug output as logging target
    .WriteTo.Debug()
    // set minimum level to log
    .MinimumLevel.Debug()
    .CreateLogger();

// var moveGenerator = new PrecannedOpeningMoveGenerator(10, new MiniMaxMoveGenerator(4));
var moveGenerator = new MiniMaxMoveGenerator(4);

var uciSession = new UciSession(Console.In, Console.Out, logger, moveGenerator);
uciSession.Run();

// var pgnGame = SelfPlay.Run(moveGenerator);
// pgnGame.WriteToFile(@"C:\Users\HP\AppData\Local\Chessica\selfplay.pgn");