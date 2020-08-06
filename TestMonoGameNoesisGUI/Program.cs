using System;

namespace TestMonoGameNoesisGUI
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Noesis.Log.SetLogCallback((level, channel, message) =>
            {
                if (channel == "")
                {
                    // [TRACE] [DEBUG] [INFO] [WARNING] [ERROR]
                    string[] prefixes = new string[] { "T", "D", "I", "W", "E" };
                    string prefix = (int)level < prefixes.Length ? prefixes[(int)level] : " ";
                    Console.WriteLine("[NOESIS/" + prefix + "] " + message);
                }
            });

            // Noesis initialization. This must be the first step before using any NoesisGUI functionality
            Noesis.GUI.Init("Daniel Off", "m47xftbG8X5ZzpXKwf0PoTf+hXx/9Mp4hSehQX5ZJU5GuR1C");

            // Setup theme
            //NoesisApp.Application.SetThemeProviders();
            //Noesis.GUI.LoadApplicationResources("Theme/NoesisTheme.DarkBlue.xaml");

            using (var game = new GameWithNoesis())
                game.Run();
        }
    }
#endif
}
