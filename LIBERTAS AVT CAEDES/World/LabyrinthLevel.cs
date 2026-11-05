using Raylib_cs;
using System.Numerics;

using LIBERTAS_AVT_CAEDES.Entities;
using LIBERTAS_AVT_CAEDES.Geometry;
using LIBERTAS_AVT_CAEDES.Items;

namespace LIBERTAS_AVT_CAEDES.World
{
    public class LabyrinthLevel
    {
        public const float CellSize = 6f;
        public int CurrentHalfExtent { get; private set; } = 6;

        public List<BoundingBox> Walls { get; } = [];
        public List<BoundingBox> Covers { get; } = [];
        public List<Vector3> TriangularLights { get; } = [];
        public HashSet<(int, int)> WalkableCells { get; } = [];
        public HashSet<(int, int)> DiscoveredCells { get; } = [];

        public Vector3 BrightHypercubeArtifactPos { get; private set; }
        public List<Hostage> Hostages { get; } = [];
        public List<Acolyte> Acolytes { get; } = [];
        public List<Chimera> Chimeras { get; } = [];
        public List<PickupItem> Pickups { get; } = [];
        public List<GoreTriangle> Gore { get; } = [];
        public List<RayTracer> Tracers { get; } = [];
        public DarkHypercubeRitual? ActiveRitual { get; set; }

        public void Generate(int stage, float adaptiveHPMult, Random rng)
        {
            Walls.Clear(); Covers.Clear(); TriangularLights.Clear(); WalkableCells.Clear(); DiscoveredCells.Clear();
            Hostages.Clear(); Acolytes.Clear(); Chimeras.Clear(); Pickups.Clear(); Gore.Clear(); Tracers.Clear();
            ActiveRitual = null;

            int halfExtent = 5 + stage * 2;
            CurrentHalfExtent = halfExtent;

            Stack<(int x, int z)> stack = [];
            WalkableCells.Add((0, 0));
            stack.Push((0, 0));

            (int dx, int dz)[] dirs = [(0, -2), (0, 2), (-2, 0), (2, 0)];

            while (stack.Count > 0)
            {
                (int x, int z) = stack.Peek();
                List<(int nx, int nz, int wx, int wz)> unvisited = [];

                foreach ((int dx, int dz) in dirs)
                {
                    int
                        nx = x + dx,
                        nz = z + dz;
                    if (Math.Abs(nx) <= halfExtent && Math.Abs(nz) <= halfExtent && !WalkableCells.Contains((nx, nz)))
                        unvisited.Add((nx, nz, x + dx / 2, z + dz / 2));
                }

                if (unvisited.Count > 0)
                {
                    (int nx, int nz, int wx, int wz) = unvisited[rng.Next(unvisited.Count)];
                    WalkableCells.Add((wx, wz));
                    WalkableCells.Add((nx, nz));
                    stack.Push((nx, nz));
                }
                else stack.Pop();

                WalkableCells.Add((0, 0));
                WalkableCells.Add((1, 0));
                WalkableCells.Add((-1, 0));
                WalkableCells.Add((0, 1));
                WalkableCells.Add((0, -1));

                WalkableCells.RemoveWhere(c => halfExtent <= Math.Abs(c.Item1) || halfExtent <= Math.Abs(c.Item2));
            }

            for (int gx = -halfExtent; gx <= halfExtent; gx++)
                for (int gz = -halfExtent; gz <= halfExtent; gz++)
                {
                    Vector3 cellCenter = new(gx * CellSize, 0, gz * CellSize);

                    if (!WalkableCells.Contains((gx, gz)))
                    {
                        Walls.Add(MakeBox(cellCenter + new Vector3(0, 1.6f, 0), new(CellSize, 3.2f, CellSize)));
                        continue;
                    }

                    if ((Math.Abs(gx) + Math.Abs(gz)) % 2 is 0)
                        TriangularLights.Add(cellCenter + new Vector3(0, .02f, 0));

                    if ((gx is not 0 || gz is not 0) && rng.NextDouble() < .24)
                        Covers.Add(MakeBox(cellCenter + new Vector3(0, .6f, 0), new(2.6f, 1.2f, .85f)));
                }

            (int, int) furthestCell = WalkableCells.OrderByDescending(c => Math.Abs(c.Item1) + Math.Abs(c.Item2)).First();
            BrightHypercubeArtifactPos = new(furthestCell.Item1 * CellSize, 1f, furthestCell.Item2 * CellSize);
            Covers.RemoveAll(c => Vector3.Distance((c.Min + c.Max) * .5f, BrightHypercubeArtifactPos) < 3f);

            int hostageCount = 3 + stage;
            for (int i = 0; i < hostageCount; i++)
                Hostages.Add(new()
                {
                    Position = RandomFloorPos(rng)
                });

            int acolyteCount = 5 + stage * 2;
            for (int i = 0; i < acolyteCount; i++)
                Acolytes.Add(new()
                {
                    Position = RandomFloorPos(rng),
                    HP = 110f * adaptiveHPMult
                });

            PickupKind[] allPickupTypes = Enum.GetValues<PickupKind>();
            int pickupCount = 12 + stage * 3;
            for (int i = 0; i < pickupCount; i++)
                Pickups.Add(new()
                {
                    Position = RandomFloorPos(rng),
                    Kind = allPickupTypes[rng.Next(allPickupTypes.Length)]
                });
        }

        public bool CollidesWithWorld(Vector3 pos, bool ignoreCovers = false, float halfSize = .35f)
        {
            float maxCoord = (CurrentHalfExtent - .5f) * CellSize;
            if (MathF.Abs(pos.X) + halfSize >= maxCoord || MathF.Abs(pos.Z) + halfSize >= maxCoord)
                return true;

            (float cx, float cz)[] corners =
            [
                (pos.X - halfSize, pos.Z - halfSize),
                (pos.X + halfSize, pos.Z - halfSize),
                (pos.X - halfSize, pos.Z + halfSize),
                (pos.X + halfSize, pos.Z + halfSize)
            ];

            foreach ((float cx, float cz) in corners)
            {
                (int, int) cell = Pathfinder.ToCell(new(cx, 0, cz), CellSize);
                if (!WalkableCells.Contains(cell)) return true;
            }

            BoundingBox pBox = MakeBox(pos + new Vector3(0, .5f, 0), new(halfSize * 2f, 1f, halfSize * 2f));
            if (Walls.Any(w => Raylib.CheckCollisionBoxes(pBox, w))) return true;
            if (!ignoreCovers && Covers.Any(c => Raylib.CheckCollisionBoxes(pBox, c))) return true;

            return false;
        }

        private Vector3 RandomFloorPos(Random rng)
        {
            List<(int, int)> list = [.. WalkableCells.Where(c => Math.Abs(c.Item1) + Math.Abs(c.Item2) > 2)];
            List<(int, int)> safeList = [.. list.Where(c =>
            {
                Vector3 pos = new(c.Item1 * CellSize, 0f, c.Item2 * CellSize);
                BoundingBox entityBox = MakeBox(pos + new Vector3(0, .5f, 0), new(.9f, 1f, .9f));
                return !Covers.Any(cov => Raylib.CheckCollisionBoxes(entityBox, cov));
            })];

            if (safeList.Count is 0) safeList = list;

            (int x, int z) = safeList[rng.Next(safeList.Count)];
            return new(x * CellSize, 0, z * CellSize);
        }

        public static BoundingBox MakeBox(Vector3 center, Vector3 size) =>
            new(center - size * .5f, center + size * .5f);

        public void DrawEnvironment(float dt)
        {
            foreach (Vector3 light in TriangularLights)
            {
                Vector3
                    v0 = light + new Vector3(0, 0, -2f),
                    v1 = light + new Vector3(-1.8f, 0, 1.5f),
                    v2 = light + new Vector3(1.8f, 0, 1.5f);
                Raylib.DrawTriangle3D(v0, v1, v2, new(48, 22, 40, 190));
                EldritchGeomertyDrawer.DrawThickLine3D(v0, v1, .03f, new(110, 45, 90, 255));
                EldritchGeomertyDrawer.DrawThickLine3D(v1, v2, .03f, new(110, 45, 90, 255));
                EldritchGeomertyDrawer.DrawThickLine3D(v2, v0, .03f, new(110, 45, 90, 255));
            }

            foreach (BoundingBox w in Walls)
            {
                Vector3
                    c = (w.Min + w.Max) * .5f,
                    s = w.Max - w.Min;
                Raylib.DrawCubeV(c, s, new(24, 22, 32, 255));
                EldritchGeomertyDrawer.DrawThickWireBox(c, s, .045f, new(60, 40, 75, 255));
            }

            foreach (BoundingBox cov in Covers)
            {
                Vector3
                    c = (cov.Min + cov.Max) * .5f,
                    s = cov.Max - cov.Min;
                Raylib.DrawCubeV(c, s, new(45, 48, 60, 255));
                EldritchGeomertyDrawer.DrawThickWireBox(c, s, .04f, Color.Gray);
            }

            EldritchGeomertyDrawer.DrawHypercube(BrightHypercubeArtifactPos, 1.8f, dt, Color.Gold, Color.SkyBlue);
        }

        public Vector3 GetNearestWalkableCellCenter(Vector3 worldPos, float yHeight = 0f)
        {
            (int, int) candidate = Pathfinder.ToCell(worldPos, CellSize);

            if (WalkableCells.Contains(candidate))
            {
                Vector3 exactCenter = new(candidate.Item1 * CellSize, yHeight, candidate.Item2 * CellSize);
                if (!CollidesWithWorld(new(exactCenter.X, 0f, exactCenter.Z), ignoreCovers: true))
                    return exactCenter;
            }

            (int, int) bestCell = WalkableCells
                .OrderBy(c => Vector2.DistanceSquared(
                    new(c.Item1 * CellSize, c.Item2 * CellSize),
                    new(worldPos.X, worldPos.Z)))
                .First();

            return new(bestCell.Item1 * CellSize, yHeight, bestCell.Item2 * CellSize);
        }
    }
}
