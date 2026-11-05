using Raylib_cs;
using System.Numerics;

namespace LIBERTAS_AVT_CAEDES.Geometry
{
    public static class EldritchGeomertyDrawer
    {
        public static void DrawThickLine3D(Vector3 start, Vector3 end, float thickness, Color color)
        {
            if (Vector3.DistanceSquared(start, end) < .0001f) return;
            Raylib.DrawCylinderEx(start, end, thickness, thickness, 6, color);
        }

        public static void DrawThickWireBox(Vector3 center, Vector3 size, float thickness, Color color)
        {
            Vector3
                h = size * 0.5f,
                v000 = center + new Vector3(-h.X, -h.Y, -h.Z),
                v100 = center + new Vector3(h.X, -h.Y, -h.Z),
                v101 = center + new Vector3(h.X, -h.Y, h.Z),
                v001 = center + new Vector3(-h.X, -h.Y, h.Z),
                v010 = center + new Vector3(-h.X, h.Y, -h.Z),
                v110 = center + new Vector3(h.X, h.Y, -h.Z),
                v111 = center + new Vector3(h.X, h.Y, h.Z),
                v011 = center + new Vector3(-h.X, h.Y, h.Z);

            // Bottom face edges
            DrawThickLine3D(v000, v100, thickness, color);
            DrawThickLine3D(v100, v101, thickness, color);
            DrawThickLine3D(v101, v001, thickness, color);
            DrawThickLine3D(v001, v000, thickness, color);

            // Top face edges
            DrawThickLine3D(v010, v110, thickness, color);
            DrawThickLine3D(v110, v111, thickness, color);
            DrawThickLine3D(v111, v011, thickness, color);
            DrawThickLine3D(v011, v010, thickness, color);

            // Vertical pillars
            DrawThickLine3D(v000, v010, thickness, color);
            DrawThickLine3D(v100, v110, thickness, color);
            DrawThickLine3D(v101, v111, thickness, color);
            DrawThickLine3D(v001, v011, thickness, color);
        }

        // 1. FLEXIBLE RIGHT-ANGLED TRAPEZOIDAL PRISM (Chimera)
        // A prism whose base is a right-angled trapezoid (90 deg at A and D) that morphs ("flexes") via time phase.
        public static void DrawFlexibleRightTrapezoidalPrism(Vector3 center, float rotationY, float time, Color color)
        {
            float
                flex = MathF.Sin(time * 6f) * .28f,
                widthX = 1.64f + MathF.Cos(time * 4f) * .2f,
                bottomLengthZ = 1.5f + flex,
                topLengthZ = .65f - flex * .35f,
                heightY = 1.35f + flex * .25f,
                halfW = widthX * .5f,
                backZ = -.7f;

            Matrix4x4 rot = Matrix4x4.CreateRotationY(rotationY);
            Vector3 ToWorld(Vector3 local) => center + Vector3.Transform(local, rot);

            Vector3
                lBotBack = ToWorld(new(-halfW, .05f, backZ)),
                lBotFront = ToWorld(new(-halfW, .05f, backZ + bottomLengthZ)),
                lTopFront = ToWorld(new(-halfW, .05f + heightY, backZ + topLengthZ)),
                lTopBack = ToWorld(new(-halfW, .05f + heightY, backZ)),
                rBotBack = ToWorld(new(halfW, .05f, backZ)),
                rBotFront = ToWorld(new(halfW, .05f, backZ + bottomLengthZ)),
                rTopFront = ToWorld(new(halfW, .05f + heightY, backZ + topLengthZ)),
                rTopBack = ToWorld(new(halfW, .05f + heightY, backZ));



            Color
                topColor = color,
                rampColor = new(
                    (byte)Math.Min(255, color.R + 25),
                    (byte)Math.Min(255, color.G + 15),
                    (byte)Math.Min(255, color.B + 25),
                    color.A
                    ),
                sideColor = new(
                (byte)Math.Max(0, color.R - 35),
                (byte)Math.Max(0, color.G - 15),
                (byte)Math.Max(0, color.B - 35),
                color.A
                );

            // 2. Draw all 6 Faces (Double-sided quads guarantee zero backface culling issues)
            DrawQuad3D(lTopBack, rTopBack, rTopFront, lTopFront, topColor);     // Top flat face
            DrawQuad3D(lTopFront, rTopFront, rBotFront, lBotFront, rampColor);  // Front sloped ramp face
            DrawQuad3D(lBotBack, rBotBack, rTopBack, lTopBack, sideColor);      // Back vertical 90° wall
            DrawQuad3D(lBotBack, rBotBack, rBotFront, lBotFront, sideColor);    // Bottom floor base
            DrawQuad3D(lBotBack, lBotFront, lTopFront, lTopBack, sideColor);    // Left trapezoid cap
            DrawQuad3D(rBotBack, rBotFront, rTopFront, rTopBack, sideColor);    // Right trapezoid cap

            // 3. Thick Black Structural Edges (12 struts)
            const float edgeThick = 0.05f;

            // Left Trapezoid Perimeter
            DrawThickLine3D(lBotBack, lBotFront, edgeThick, Color.Black);
            DrawThickLine3D(lBotFront, lTopFront, edgeThick, Color.Black);
            DrawThickLine3D(lTopFront, lTopBack, edgeThick, Color.Black);
            DrawThickLine3D(lTopBack, lBotBack, edgeThick, Color.Black);

            // Right Trapezoid Perimeter
            DrawThickLine3D(rBotBack, rBotFront, edgeThick, Color.Black);
            DrawThickLine3D(rBotFront, rTopFront, edgeThick, Color.Black);
            DrawThickLine3D(rTopFront, rTopBack, edgeThick, Color.Black);
            DrawThickLine3D(rTopBack, rBotBack, edgeThick, Color.Black);

            // 4 Horizontal Cross-Bars connecting Left and Right Trapezoids
            DrawThickLine3D(lBotBack, rBotBack, edgeThick, Color.Black);
            DrawThickLine3D(lBotFront, rBotFront, edgeThick, Color.Black);
            DrawThickLine3D(lTopFront, rTopFront, edgeThick, Color.Black);
            DrawThickLine3D(lTopBack, rTopBack, edgeThick, Color.Black);
        }

        // 2. 4D HYPERCUBE (Tesseract Projection) for Extraction Artifact & Dark Conjuring Portal
        public static void DrawHypercube(Vector3 center, float scale, float t, Color outerColor, Color innerColor)
        {
            float innerScale = scale * (0.45f + 0.1f * MathF.Sin(3f * t));
            DrawThickWireBox(center, new(scale), .04f, outerColor);
            DrawThickWireBox(center, new(innerScale), .035f, innerColor);

            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3
                            vOuter = center + new Vector3(x, y, z) * (scale * 0.5f),
                            vInner = center + new Vector3(x, y, z) * (innerScale * 0.5f);
                        DrawThickLine3D(vOuter, vInner, .03f, outerColor);
                    }
        }

        // 3. FLATTENED OCTAHEDRON (Gold Treasures) & ELONGATED SQUARE PYRAMID (Munition)
        public static void DrawOctahedron(Vector3 center, Vector3 scale, Color color)
        {
            Vector3
                top = center + new Vector3(0, scale.Y, 0),
                bottom = center - new Vector3(0, scale.Y, 0),
                p0 = center + new Vector3(scale.X, 0, 0),
                p1 = center + new Vector3(0, 0, scale.Z),
                p2 = center + new Vector3(-scale.X, 0, 0),
                p3 = center + new Vector3(0, 0, -scale.Z);

            Raylib.DrawTriangle3D(top, p1, p0, color);
            Raylib.DrawTriangle3D(top, p2, p1, color);
            Raylib.DrawTriangle3D(top, p3, p2, color);
            Raylib.DrawTriangle3D(top, p0, p3, color);

            Raylib.DrawTriangle3D(bottom, p0, p1, color);
            Raylib.DrawTriangle3D(bottom, p1, p2, color);
            Raylib.DrawTriangle3D(bottom, p2, p3, color);
            Raylib.DrawTriangle3D(bottom, p3, p0, color);

            DrawThickLine3D(p0, p1, .025f, Color.Black);
            DrawThickLine3D(p1, p2, .025f, Color.Black);
            DrawThickLine3D(p2, p3, .025f, Color.Black);
            DrawThickLine3D(p3, p0, .025f, Color.Black);
        }

        // 4. PENTAGONAL PRISM (Stackable Talismans) & CYLINDER (Acquirable Guns)
        public static void DrawRegularPrism(Vector3 center, int sides, float radiusX, float radiusZ, float height, Color color)
        {
            for (int i = 0; i < sides; i++)
            {
                float
                    a1 = i * MathF.Tau / sides,
                    a2 = (i + 1) * MathF.Tau / sides;
                Vector3
                    b1 = center + new Vector3(MathF.Cos(a1) * radiusX, 0, MathF.Sin(a1) * radiusZ),
                    b2 = center + new Vector3(MathF.Cos(a2) * radiusX, 0, MathF.Sin(a2) * radiusZ),
                    t1 = b1 + new Vector3(0, height, 0),
                    t2 = b2 + new Vector3(0, height, 0);

                Raylib.DrawTriangle3D(center + new Vector3(0, height, 0), t2, t1, color);
                DrawQuad3D(b1, b2, t2, t1, color);
                DrawThickLine3D(t1, t2, .02f, Color.Black);
            }
        }

        private static void DrawQuad3D(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color col)
        {
            Raylib.DrawTriangle3D(v0, v1, v2, col);
            Raylib.DrawTriangle3D(v0, v2, v3, col);
            Raylib.DrawTriangle3D(v0, v2, v1, col);
            Raylib.DrawTriangle3D(v0, v3, v2, col);
        }
    }
}
