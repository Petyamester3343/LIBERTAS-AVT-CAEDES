using LIBERTAS_AVT_CAEDES.Geometry;
using System.Numerics;

namespace LIBERTAS_AVT_CAEDES.Entities
{
    public class Chimera
    {
        public Vector3 Position;
        public float
            HP = 450f,
            RotationY,
            BurnDuration = 0f;
        public bool Dead;

        public void Draw(float dt)
        {
            if (Dead) return;
            EldritchGeomertyDrawer
                .DrawFlexibleRightTrapezoidalPrism(Position, RotationY, dt, new(135, 30, 190, 255));
        }
    }
}
