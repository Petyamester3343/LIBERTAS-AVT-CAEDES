using System.Numerics;

namespace LIBERTAS_AVT_CAEDES.World
{
    public static class Pathfinder
    {
        private static readonly (int dx, int dz)[] Neighbors =
        [
            (0, -1), (0, 1), (-1, 0), (1, 0)
        ];

        public static List<Vector3> FindPath(Vector3 startWorld, Vector3 tgtWorld, HashSet<(int, int)> walkableCells, float cellSize)
        {
            (int, int) startCell = ToCell(startWorld, cellSize);
            (int, int) goalCell = ToCell(tgtWorld, cellSize);

            if (!walkableCells.Contains(startCell) || !walkableCells.Contains(goalCell))
                return [tgtWorld];

            PriorityQueue<(int, int), float> openSet = new();
            Dictionary<(int, int), (int, int)> cameFrom = [];
            Dictionary<(int, int), float> gScore = new()
            {
                [startCell] = 0f
            };

            openSet.Enqueue(startCell, Heuristic(startCell, goalCell));

            while (openSet.Count > 0)
            {
                (int, int) curr = openSet.Dequeue();
                if (curr == goalCell)
                    return ReconstructPath(cameFrom, curr, cellSize, tgtWorld);

                foreach ((int dx, int dz) in Neighbors)
                {
                    (int, int) neighbor = (curr.Item1 + dx, curr.Item2 + dz);
                    if (!walkableCells.Contains(neighbor)) continue;

                    float tentativeG = gScore[curr] + 1f;
                    if (!gScore.TryGetValue(neighbor, out float existingG) || tentativeG < existingG)
                    {
                        cameFrom[neighbor] = curr;
                        gScore[neighbor] = tentativeG;
                        float fScore = tentativeG + Heuristic(neighbor, goalCell);
                        openSet.Enqueue(neighbor, fScore);
                    }
                }
            }

            return [tgtWorld];
        }

        public static (int, int) ToCell(Vector3 pos, float cellSize) =>
            ((int)MathF.Round(pos.X / cellSize), (int)MathF.Round(pos.Z / cellSize));

        private static float Heuristic((int x, int z) a, (int x, int z) b) =>
            Math.Abs(a.x - b.x) + Math.Abs(a.z - b.z);

        private static List<Vector3> ReconstructPath(Dictionary<(int, int), (int, int)> cameFrom, (int x, int z) curr, float cellSize, Vector3 exactTgt)
        {
            List<Vector3> path = [exactTgt];
            while (cameFrom.ContainsKey(curr))
            {
                path.Add(new(curr.x * cellSize, 0f, curr.z * cellSize));
                curr = cameFrom[curr];
            }
            path.Reverse();
            return path;
        }
    }
}
