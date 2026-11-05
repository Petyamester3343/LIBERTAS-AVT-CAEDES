using LIBERTAS_AVT_CAEDES.Entities;
using LIBERTAS_AVT_CAEDES.Geometry;
using Raylib_cs;
using System.Numerics;

namespace LIBERTAS_AVT_CAEDES.World
{
    public class DarkHypercubeRitual
    {
        public Vector3 Position;
        public float Countdown = 30f;
        public bool Active;
        public List<Acolyte> Conjurers { get; init; } = [];

        public bool AllConjurersKilled => Conjurers.All(c => c.Dead);

        public void Draw(float dt, Vector3 playerPos, HashSet<(int, int)> walkableCells, float cellSize)
        {
            if (!Active) return;

            // Draw the dark portal
            EldritchGeomertyDrawer.DrawHypercube(Position, 2f, 2f * dt, Color.Purple, Color.Maroon);

            // A*
            List<Vector3> path = Pathfinder.FindPath(playerPos, Position, walkableCells, cellSize);
            Vector3 nextWaypoint = path.Count > 1 ? path[1] : Position;

            Vector3 dirToWaypoint = nextWaypoint - playerPos;
            dirToWaypoint.Y = 0;
            dirToWaypoint = (dirToWaypoint.LengthSquared() < .01f) ? Vector3.UnitZ : Vector3.Normalize(dirToWaypoint);

            Vector3
                arrowBase = playerPos + dirToWaypoint * 2.1f + new Vector3(0, 1.5f, 0),
                arrowShaftEnd = arrowBase + dirToWaypoint * .75f,
                arrowTip = arrowShaftEnd + dirToWaypoint * .65f;

            Raylib.DrawCylinderEx(arrowBase, arrowShaftEnd, .12f, .12f, 8, Color.Magenta);
            Raylib.DrawCylinderEx(arrowShaftEnd, arrowTip, .32f, 0, 8, Color.Gold);

            for (int i = 0; i < path.Count - 1; i++)
                EldritchGeomertyDrawer
                    .DrawThickLine3D(path[i] + new Vector3(0, .12f, 0),
                                     path[i + 1] + new Vector3(0, .12f, 0),
                                     .04f,
                                     new(220, 40, 255, 130));
        }
    }
}
