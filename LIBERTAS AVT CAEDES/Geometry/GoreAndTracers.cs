using Raylib_cs;
using System.Numerics;

namespace LIBERTAS_AVT_CAEDES.Geometry
{
    public struct RayTracer
    {
        public Vector3 Start, End;
        public Color Color;
        public float TimeLeft;
    }
    
    public struct GoreTriangle
    {
        public Vector3
            Position, Velocity,
            V0, V1, V2; // Local vertices obeying Triangle Inequality
        public bool IsCorpseShard; // If true, persists indefinitely & triggers Acolyte search!
        public float Lifetime;

        public static GoreTriangle Create(Random rng, Vector3 origin, bool isCorpseShard)
        {
            float
                min = isCorpseShard ? .20f : .09f,
                max = isCorpseShard ? .60f : .26f,
                a = min + (float)rng.NextDouble() * (max - min),
                b = min + (float)rng.NextDouble() * (max - min);

            // OBEY THE PRIMAL LAW!!! -> |a - b| < c < a + b (Triangle Inequality Theorem)
            float
                cMin = MathF.Abs(a - b) + 0.01f,
                cMax = (a + b) - 0.01f,
                c = cMin + (float)rng.NextDouble() * (cMax - cMin),
                cosGamma = Math.Clamp((a * a + b * b - c * c) / (2f * a * b), -0.99f, 0.99f),
                sinGamma = MathF.Sqrt(1f - cosGamma * cosGamma);

            return new()
            {
                Position = origin,
                Velocity = new(
                    ((float)rng.NextDouble() - 0.5f) * 6f,
                    (float)rng.NextDouble() * 4f,
                    ((float)rng.NextDouble() - 0.5f) * 6f),
                V0 = Vector3.Zero,
                V1 = new(a, 0, 0),
                V2 = new(b * cosGamma, 0, b * sinGamma),
                IsCorpseShard = isCorpseShard,
                Lifetime = isCorpseShard ? float.PositiveInfinity : 12.0f
            };
        }

        public void Update(float dt)
        {
            if (Position.Y >= .03f)
            {
                Position += Velocity * dt;
                Velocity.Y -= 12f * dt;
                if (Position.Y < .03f) Position.Y = .03f; // Clamp to ground
            }
            Lifetime -= dt;
        }

        public readonly void Draw()
        {
            Color c = IsCorpseShard ? new(190, 15, 30, 255) : Color.Red;
            Raylib.DrawTriangle3D(Position + V0, Position + V2, Position + V1, c);
        }
    }
}
