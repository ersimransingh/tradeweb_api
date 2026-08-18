using System;
using System.IO;
using QuestPDF.Drawing;

namespace TradeWeb.API.QuestPdfTemplates
{
    // Registers the "Outfit" font (bundled under /Fonts, OFL licensed) so generated
    // PDFs match tradewebx's on-screen font instead of falling back to Helvetica/Lato.
    public static class FontSetup
    {
        public const string FontFamily = "Outfit";

        private static bool _registered;
        private static readonly object _lock = new object();

        public static void EnsureRegistered()
        {
            if (_registered) return;

            lock (_lock)
            {
                if (_registered) return;

                var fontsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts");
                RegisterIfExists(Path.Combine(fontsDir, "Outfit-Regular.ttf"));
                RegisterIfExists(Path.Combine(fontsDir, "Outfit-SemiBold.ttf"));
                RegisterIfExists(Path.Combine(fontsDir, "Outfit-Bold.ttf"));

                _registered = true;
            }
        }

        private static void RegisterIfExists(string path)
        {
            if (!File.Exists(path)) return;

            using (var stream = File.OpenRead(path))
                FontManager.RegisterFont(stream);
        }
    }
}
