using System.Numerics;
using Raylib_cs;
using LIBERTAS_AVT_CAEDES.Core;
using LIBERTAS_AVT_CAEDES.Telemetry;

namespace LIBERTAS_AVT_CAEDES.UI
{
    public enum MenuAction
    {
        None,
        StartRun,
        OpenOptions,
        BackToMain,
        ExitGame
    }

    public static class MenuRenderer
    {
        public static MenuAction UpdateAndDrawMainMenu()
        {
            Raylib.ShowCursor();
            Vector2 mouse = Raylib.GetMousePosition();
            bool clicked = Raylib.IsMouseButtonPressed(MouseButton.Left);

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // Atmospheric Backdrop Frame
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(8, 6, 12, 255));
            Raylib.DrawRectangleLinesEx(new Rectangle(24, 24, screenW - 48, screenH - 48), 2f, new Color(110, 20, 35, 255));

            // Title & Subtitle
            Raylib.DrawText("LIBERTAS AUT CAEDES", 120, 110, 56, new Color(220, 25, 45, 255));
            Raylib.DrawText("LIBERATE OR SLAUGHTER // TACTICAL ELDRITCH GEOMETRIC SHOOTER", 122, 176, 18, Color.Gold);
            Raylib.DrawLine(120, 208, 860, 208, new Color(90, 40, 110, 255));

            // Menu Buttons
            Rectangle startBtn = new(120, 255, 360, 62);
            Rectangle optionsBtn = new(120, 338, 360, 62);
            Rectangle exitBtn = new(120, 421, 360, 62);

            if (DrawMenuButton(startBtn, "START RUN", Color.Lime, mouse, clicked))
                return MenuAction.StartRun;

            if (DrawMenuButton(optionsBtn, "OPTIONS", Color.SkyBlue, mouse, clicked))
                return MenuAction.OpenOptions;

            if (DrawMenuButton(exitBtn, "EXIT", Color.Red, mouse, clicked))
                return MenuAction.ExitGame;

            // Footer Tester Session Info
            string consentTag = PlayerTelemetry.ConsentGranted ? "ACTIVE (CSV ON)" : "DISABLED (OFFLINE)";
            Color consentCol = PlayerTelemetry.ConsentGranted ? Color.Lime : Color.Gray;
            Raylib.DrawText($"TESTER GUID: {PlayerTelemetry.SessionGuid}", 120, screenH - 85, 16, Color.DarkGray);
            Raylib.DrawText($"TELEMETRY STATUS: {consentTag}", 120, screenH - 62, 16, consentCol);

            return MenuAction.None;
        }

        public static MenuAction UpdateAndDrawOptionsMenu(GameSettings settings)
        {
            Raylib.ShowCursor();
            Vector2 mouse = Raylib.GetMousePosition();
            bool clicked = Raylib.IsMouseButtonPressed(MouseButton.Left);

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(8, 6, 12, 255));
            Raylib.DrawRectangleLinesEx(new Rectangle(24, 24, screenW - 48, screenH - 48), 2f, Color.SkyBlue);

            Raylib.DrawText("OPTIONS // SYSTEM & TELEMETRY", 120, 85, 40, Color.SkyBlue);
            Raylib.DrawLine(120, 138, 920, 138, Color.DarkGray);

            // --- CATEGORY 1: GRAPHICS ---
            Raylib.DrawText("GRAPHICS", 120, 165, 24, Color.Gold);

            Rectangle aaBtn = new(120, 205, 680, 58);
            if (DrawToggleRow(aaBtn, "Anti-Aliasing (4x MSAA)", settings.AntiAliasingEnabled, mouse, clicked))
            {
                settings.ToggleAA();
            }

            Rectangle bloomBtn = new(120, 278, 680, 58);
            if (DrawToggleRow(bloomBtn, "Bloom (Eldritch Post-Processing Glow)", settings.BloomEnabled, mouse, clicked))
            {
                settings.ToggleBloom();
            }

            // --- CATEGORY 2: OTHER ---
            Raylib.DrawText("OTHER", 120, 368, 24, Color.Gold);

            Rectangle telemetryBtn = new(120, 408, 680, 58);
            if (DrawToggleRow(telemetryBtn, "Consent to Telemetry Gathering (Anonymous CSV)", settings.TelemetryConsent, mouse, clicked))
            {
                settings.ToggleTelemetryConsent();
            }

            // Back Button
            Rectangle backBtn = new(120, 520, 280, 56);
            if (DrawMenuButton(backBtn, "< BACK TO MAIN MENU", Color.Yellow, mouse, clicked) ||
                Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                return MenuAction.BackToMain;
            }

            return MenuAction.None;
        }

        private static bool DrawMenuButton(Rectangle rect, string label, Color accent, Vector2 mouse, bool clicked)
        {
            bool hover = Raylib.CheckCollisionPointRec(mouse, rect);
            Color bg = hover ? new Color(34, 28, 48, 255) : new Color(18, 15, 28, 255);
            Color border = hover ? Color.RayWhite : accent;

            Raylib.DrawRectangleRec(rect, bg);
            Raylib.DrawRectangleLinesEx(rect, hover ? 3f : 2f, border);

            // Left accent bar
            Raylib.DrawRectangle((int)rect.X, (int)rect.Y, 8, (int)rect.Height, accent);
            Raylib.DrawText(label, (int)rect.X + 26, (int)rect.Y + 19, 21, hover ? Color.RayWhite : accent);

            return hover && clicked;
        }

        private static bool DrawToggleRow(Rectangle rect, string label, bool isEnabled, Vector2 mouse, bool clicked)
        {
            bool hover = Raylib.CheckCollisionPointRec(mouse, rect);
            Color bg = hover ? new Color(30, 26, 44, 255) : new Color(16, 14, 24, 255);

            Raylib.DrawRectangleRec(rect, bg);
            Raylib.DrawRectangleLinesEx(rect, hover ? 2.5f : 1.5f, hover ? Color.RayWhite : Color.DarkGray);

            Raylib.DrawText(label, (int)rect.X + 22, (int)rect.Y + 18, 20, Color.RayWhite);

            // ON / OFF Status Pill on the right side of the row
            Rectangle pill = new(rect.X + rect.Width - 130, rect.Y + 12, 108, 34);
            Color pillColor = isEnabled ? new Color(25, 110, 45, 255) : new Color(95, 25, 30, 255);
            Color pillBorder = isEnabled ? Color.Lime : Color.Red;
            string stateText = isEnabled ? "ON" : "OFF";

            Raylib.DrawRectangleRec(pill, pillColor);
            Raylib.DrawRectangleLinesEx(pill, 2f, pillBorder);
            Raylib.DrawText(stateText, (int)pill.X + (isEnabled ? 38 : 33), (int)pill.Y + 7, 20, Color.RayWhite);

            return hover && clicked;
        }
    }
}
