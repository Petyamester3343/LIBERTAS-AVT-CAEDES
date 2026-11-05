using LIBERTAS_AVT_CAEDES.Entities;
using LIBERTAS_AVT_CAEDES.Items;
using LIBERTAS_AVT_CAEDES.World;
using Raylib_cs;
using System.Numerics;

namespace LIBERTAS_AVT_CAEDES.UI
{
    public static class MinimapRenderer
    {
        public static void Draw(int panelX, int panelY, int panelSize, LabyrinthLevel level, Player player)
        {
            float t = (float)Raylib.GetTime();

            // 1. Background Frame & Header
            Raylib.DrawRectangle(panelX, panelY, panelSize, panelSize + 68, new Color(12, 10, 20, 230));
            Raylib.DrawRectangleLinesEx(new Rectangle(panelX, panelY, panelSize, panelSize + 68), 2f, Color.Gold);

            int gridSpan = level.CurrentHalfExtent * 2 + 1;
            float cellPx = (float)(panelSize - 16) / gridSpan;
            float mapOriginX = panelX + 8f;
            float mapOriginY = panelY + 8f;

            // Helper: Convert Grid (gx, gz) to Minimap Pixel Center
            Vector2 CellToMapPx(int gx, int gz) => new(
                mapOriginX + (gx + level.CurrentHalfExtent + 0.5f) * cellPx,
                mapOriginY + (gz + level.CurrentHalfExtent + 0.5f) * cellPx);

            // Helper: Convert World Vector3 to Minimap Pixel Position
            Vector2 WorldToMapPx(Vector3 worldPos)
            {
                float gx = worldPos.X / LabyrinthLevel.CellSize;
                float gz = worldPos.Z / LabyrinthLevel.CellSize;
                return new Vector2(
                    mapOriginX + (gx + level.CurrentHalfExtent + 0.5f) * cellPx,
                    mapOriginY + (gz + level.CurrentHalfExtent + 0.5f) * cellPx);
            }

            // 2. Render Discovered DFS Floor Cells & Adjacent Wall Outlines (Fog of War)
            for (int gx = -level.CurrentHalfExtent; gx <= level.CurrentHalfExtent; gx++)
                for (int gz = -level.CurrentHalfExtent; gz <= level.CurrentHalfExtent; gz++)
                {
                    Rectangle cellRect = new(
                        mapOriginX + (gx + level.CurrentHalfExtent) * cellPx,
                        mapOriginY + (gz + level.CurrentHalfExtent) * cellPx,
                        MathF.Ceiling(cellPx),
                        MathF.Ceiling(cellPx));

                    if (level.DiscoveredCells.Contains((gx, gz)))
                    {
                        // Discovered Corridor Floor
                        Raylib.DrawRectangleRec(cellRect, new Color(42, 38, 58, 255));

                        if ((Math.Abs(gx) + Math.Abs(gz)) % 2 is 0)
                        {
                            Vector2 centerPx = CellToMapPx(gx, gz);
                            Raylib.DrawCircleV(centerPx, MathF.Max(1.5f, cellPx * .1f), new(75, 35, 65, 200));
                        }
                    }
                    else if (!level.WalkableCells.Contains((gx, gz)) && HasDiscoveredNeighbor(gx, gz, level.DiscoveredCells))
                    {
                        // Revealed Labyrinth Wall bordering a discovered cell
                        Raylib.DrawRectangleRec(cellRect, new Color(90, 50, 115, 255));
                    }
                }

            // 3. Render Pickups on Discovered Cells
            foreach (PickupItem p in level.Pickups.Where(p => !p.Acquired))
            {
                (int, int) cell = Pathfinder.ToCell(p.Position, LabyrinthLevel.CellSize);
                if (level.DiscoveredCells.Contains(cell))
                {
                    Vector2 px = WorldToMapPx(p.Position);
                    Raylib.DrawCircleV(px, MathF.Max(2f, cellPx * 0.16f), Color.SkyBlue);
                }
            }

            // 4. Render Hostages (White Spheres) on Discovered Cells
            foreach (Hostage h in level.Hostages.Where(h => !h.Saved && !h.Dead))
            {
                (int, int) cell = Pathfinder.ToCell(h.Position, LabyrinthLevel.CellSize);
                if (level.DiscoveredCells.Contains(cell))
                {
                    Vector2 px = WorldToMapPx(h.Position);
                    Raylib.DrawCircleV(px, MathF.Max(3.2f, cellPx * 0.25f), Color.RayWhite);
                }
            }

            // 5. Render Acolytes (Scarlet Cubes) if on a Discovered Cell or in Combat
            foreach (Acolyte a in level.Acolytes.Where(a => !a.Dead))
            {
                (int, int) cell = Pathfinder.ToCell(a.Position, LabyrinthLevel.CellSize);
                if (level.DiscoveredCells.Contains(cell) || a.State == AcolyteState.Combat || a.State == AcolyteState.Conjuring)
                {
                    Vector2 px = WorldToMapPx(a.Position);
                    float s = MathF.Max(4f, cellPx * 0.35f);
                    Raylib.DrawRectanglePro(new Rectangle(px.X, px.Y, s, s), new Vector2(s * 0.5f, s * 0.5f), 0f, Color.Red);
                }
            }

            // 6. Render Active Dark Hypercube Ritual & A* GPS Path
            if (level.ActiveRitual is { Active: true })
            {
                List<Vector3> path = Pathfinder.FindPath(
                    player.Position,
                    level.ActiveRitual.Position,
                    level.WalkableCells,
                    LabyrinthLevel.CellSize);

                for (int i = 0; i < path.Count - 1; i++)
                {
                    Vector2 p0 = WorldToMapPx(path[i]);
                    Vector2 p1 = WorldToMapPx(path[i + 1]);
                    Raylib.DrawLineEx(p0, p1, 2.5f, Color.Magenta);
                }

                Vector2 ritualPx = WorldToMapPx(level.ActiveRitual.Position);
                float pulse = 4f + MathF.Sin(t * 8f) * 2f;
                Raylib.DrawCircleLines((int)ritualPx.X, (int)ritualPx.Y, pulse, Color.Magenta);
                Raylib.DrawCircleV(ritualPx, 3.5f, Color.Purple);
            }

            // 7. Render Chimeras (Always tracked on radar due to transdimensional link)
            foreach (var c in level.Chimeras.Where(c => !c.Dead))
            {
                Vector2 px = WorldToMapPx(c.Position);
                Raylib.DrawCircleV(px, MathF.Max(4.5f, cellPx * 0.38f), Color.Purple);
                Raylib.DrawCircleLines((int)px.X, (int)px.Y, MathF.Max(6f, cellPx * 0.5f), Color.Magenta);
            }

            // 8. Render Ancient Bright Hypercube Artifact (Extraction Beacon)
            Vector2 artPx = WorldToMapPx(level.BrightHypercubeArtifactPos);
            float artSize = MathF.Max(6f, cellPx * 0.55f);
            Raylib.DrawRectangleLinesEx(
                new Rectangle(artPx.X - artSize * 0.5f, artPx.Y - artSize * 0.5f, artSize, artSize),
                2f,
                Color.Gold);
            Raylib.DrawCircleV(artPx, 2.5f, Color.SkyBlue);

            // 9. Render Player (Green Directional Triangle)
            Vector2 playerPx = WorldToMapPx(player.Position);
            Vector2 forward = new(MathF.Sin(player.RotationY), MathF.Cos(player.RotationY));
            Vector2 right = new(-forward.Y, forward.X);
            float pRadius = MathF.Max(5.5f, cellPx * 0.45f);

            Vector2 tip = playerPx + forward * pRadius * 1.3f;
            Vector2 backLeft = playerPx - forward * pRadius * 0.8f - right * pRadius * 0.8f;
            Vector2 backRight = playerPx - forward * pRadius * 0.8f + right * pRadius * 0.8f;

            // DrawTriangle expects counter-clockwise vertices in 2D screen space
            Raylib.DrawTriangle(tip, backLeft, backRight, Color.Lime);
            Raylib.DrawTriangleLines(tip, backLeft, backRight, Color.Black);

            // 10. Telemetry Stats Footer inside Minimap Frame
            int saved = level.Hostages.Count(h => h.Saved);
            float discPct = level.WalkableCells.Count > 0
                ? 100f * level.DiscoveredCells.Count / level.WalkableCells.Count
                : 0f;

            int footerY = panelY + panelSize + 6;
            Raylib.DrawLine(panelX, footerY - 4, panelX + panelSize, footerY - 4, Color.Gold);
            Raylib.DrawText($"HOSTAGES: {saved}/{level.Hostages.Count} SAVED", panelX + 10, footerY, 16, Color.RayWhite);
            Raylib.DrawText($"DISCOVERED: {discPct:F1}%", panelX + 10, footerY + 20, 16, Color.Lime);
            Raylib.DrawText($"FPS: {Raylib.GetFPS()}", panelX + 10, footerY + 40, 15, Color.Gray);
        }

        private static bool HasDiscoveredNeighbor(int gx, int gz, HashSet<(int, int)> discovered) =>
            discovered.Contains((gx + 1, gz)) ||
            discovered.Contains((gx - 1, gz)) ||
            discovered.Contains((gx, gz + 1)) ||
            discovered.Contains((gx, gz - 1));
    }
}
