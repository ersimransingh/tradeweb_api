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

        // Report tables (DashboardPdfDocument's ComposeReportTable) use this instead of
        // the headline Outfit family - a plain grotesque reads more neutral for dense
        // rows of financial figures. Reuses the Roboto files already bundled for
        // devextreme rather than adding a new font.
        public const string TableFontFamily = "Roboto";

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
                RegisterIfExists(Path.Combine(fontsDir, "Roboto-Regular.ttf"));
                RegisterIfExists(Path.Combine(fontsDir, "Roboto-Bold.ttf"));

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
