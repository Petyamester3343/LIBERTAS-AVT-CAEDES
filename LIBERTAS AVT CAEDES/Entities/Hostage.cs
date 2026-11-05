using Raylib_cs;
using System.Numerics;

namespace LIBERTAS_AVT_CAEDES.Entities
{

    public class Hostage
    {
        public Vector3 Position;
        public float HP = 60f;
        public bool
            Saved, Dead;

        public void Update(float dt, Vector3 playerPos)
        {
            if (Saved || Dead) return;

            float dist = Vector3.Distance(Position, playerPos);
            if (dist < 5.5f && dist > 1.8f)
            {
                Vector3 dir = Vector3.Normalize(playerPos - Position);
                Position += dir * 4.8f * dt;
            }
        }

        public void Draw()
        {
            if (Saved || Dead) return;
            Vector3 center = Position + new Vector3(0, .55f, 0);
            Raylib.DrawSphere(center, .5f, Color.RayWhite);
            Raylib.DrawCylinderWiresEx(center - new Vector3(0, .03f, 0), center + new Vector3(0, .03f, 0),
                .54f, .54f, 16, Color.Black);
        }
    }
}
