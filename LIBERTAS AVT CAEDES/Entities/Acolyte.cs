using LIBERTAS_AVT_CAEDES.Geometry;
using Raylib_cs;
using System.Numerics;

namespace LIBERTAS_AVT_CAEDES.Entities
{
    public enum AcolyteState
    {
        Idle, // 0: Wandering around, not aware of player
        Suspicious, // .25-.5: Stops, turns toward stimuls, meter filling
        Investigating, // Player has been spotted, but not yet in attack range
        Chasing, // .5-.99 or sound heard: Moves to inspect last seen coordinates
        Combat, // 1: Fully busted, strafing, shhoting, alerting narby comrades
        Conjuring // Performing a ritual to summon more enemies
    }

    public class Acolyte
    {
        public Vector3 Position, LastKnownPlayerPosition;
        public float
            RotationY,
            HP = 120f,
            MaxHP = 120f,
            Awareness = 0f,
            StateTimer = 0f,
            StrafeTimer = 0f,
            StrafeDirection = 1f,
            FireCooldown = 1f,
            BurnDuration = 0f;
        public AcolyteState State = AcolyteState.Idle;
        public bool Dead;

        public void Draw(Camera3D cam, float t)
        {
            if (Dead) return;

            // Scarlet CUBE
            Vector3
                center = Position + new Vector3(0, .65f, 0),
                size = new(.95f, 1.3f, .95f);

            Color acolyteBodyColor = State switch
            {
                AcolyteState.Conjuring => new(160, 20, 140, 255),
                AcolyteState.Combat => new(240, 25, 40, 255),
                _ => new(200, 20, 35, 255)
            };

            Raylib.DrawCubeV(Vector3.Zero, size, acolyteBodyColor);
            EldritchGeometryDrawer.DrawThickWireBox(center, size, .045f, Color.Black);

            Matrix4x4 rot = Matrix4x4.CreateRotationY(RotationY);
            Vector3 visorLeft = center + Vector3.Transform(new Vector3(-0.28f, 0.25f, 0.49f), rot);
            Vector3 visorRight = center + Vector3.Transform(new Vector3(0.28f, 0.25f, 0.49f), rot);
            EldritchGeometryDrawer.DrawThickLine3D(visorLeft, visorRight, 0.065f, Color.Black);
        }

        public void DrawPerceptionGauge(Camera3D cam, float t)
        {
            if (Dead) return;

            // Show gauge whenever awareness > 0 or Acolyte is actively investigating/combating
            bool activeAlert = Awareness > 0.005f ||
                               State is
                               AcolyteState.Investigating or
                               AcolyteState.Chasing or
                               AcolyteState.Combat or
                               AcolyteState.Conjuring;

            if (!activeAlert) return;

            Vector3 headWorld = Position + new Vector3(0, 1.95f, 0);
            Vector2 screenPos = Raylib.GetWorldToScreen(headWorld, cam);

            if (screenPos.X < -50 || screenPos.X > Raylib.GetScreenWidth() + 50 ||
                screenPos.Y < -50 || screenPos.Y > Raylib.GetScreenHeight() + 50)
                return;

            float fill = Math.Clamp(Awareness, 0f, 1f);
            bool isBusted = State is
                AcolyteState.Combat or
                AcolyteState.Conjuring || fill >= 1.0f;

            Color gaugeColor;
            if (isBusted)
            {
                bool flash = MathF.Sin(t * 18f) > 0f;
                gaugeColor = flash ? Color.Red : Color.Gold;
            }
            else if (fill >= 0.5f)
                gaugeColor = Color.Orange;
            else
                gaugeColor = Color.RayWhite;

            float radius = isBusted ? 16f + MathF.Sin(t * 14f) * 2.5f : 14f;
            Vector2
                top = screenPos + new Vector2(0, -radius),
                right = screenPos + new Vector2(radius * 0.75f, 0),
                bottom = screenPos + new Vector2(0, radius),
                left = screenPos + new Vector2(-radius * 0.75f, 0);

            // Dark backing diamond (counter-clockwise vertex order for 2D Raylib triangles)
            Raylib.DrawTriangle(top, left, right, new Color(10, 8, 16, 235));
            Raylib.DrawTriangle(bottom, right, left, new Color(10, 8, 16, 235));

            // Inner fill meter proportional to Awareness (0.0 -> 1.0)
            float
                barHeight = (radius * 1.5f) * fill,
                barWidth = radius * 0.65f;
            if (barHeight > 1f)
                Raylib.DrawRectangle(
                    (int)(screenPos.X - barWidth * 0.5f),
                    (int)(screenPos.Y + radius * 0.75f - barHeight),
                    (int)barWidth,
                    (int)barHeight,
                    gaugeColor);


            // Bold outer diamond frame
            Raylib.DrawLineEx(top, right, 2.5f, gaugeColor);
            Raylib.DrawLineEx(right, bottom, 2.5f, gaugeColor);
            Raylib.DrawLineEx(bottom, left, 2.5f, gaugeColor);
            Raylib.DrawLineEx(left, top, 2.5f, gaugeColor);

            // Status glyph (! when busted, ? when filling)
            string glyph = isBusted ? "!" : "?";
            Raylib.DrawText(glyph, (int)screenPos.X - 4, (int)(screenPos.Y - radius - 20), 20, gaugeColor);
        }
    }
}
