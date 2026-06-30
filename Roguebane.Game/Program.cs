using System;
using System.IO;

// Crash log: an unhandled exception otherwise dies to stderr (lost unless launched from a terminal).
// Write the full exception+stack to crash.log next to the exe, echo to stderr, then rethrow.
try
{
    using var game = new Roguebane.Game.Game1();
    game.Run();
}
catch (Exception ex)
{
    try
    {
        var log = Path.Combine(AppContext.BaseDirectory, "crash.log");
        File.WriteAllText(log, $"{DateTime.Now:u}\n{ex}\n");
        Console.Error.WriteLine($"[CRASH] written to {log}\n{ex}");
    }
    catch { /* logging must never mask the original crash */ }
    throw;
}
