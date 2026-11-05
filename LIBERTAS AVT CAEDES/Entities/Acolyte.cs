using LIBERTAS_AVT_CAEDES.Geometry;
using Raylib_cs;
using System.Numerics;

namespace LIBERTAS_AVT_CAEDES.Entities
{
    public enum AcolyteState
    {
        Idle, // Wandering around, not aware of player
        Investigating, // Player has been spotted, but not yet in attack range
        Chasing, // Player is in attack range and being attacked
        Combat, // Player was lost from sight, searching for them
        Conjuring // Performing a ritual to summon more enemies
    }
    
    public class Acolyte
    {
        public Vector3 Position, LastKnownPlayerPosition;
        public float
            HP = 120f,
            FireCooldown = 1f,
            BurnDuration = 0f;
        public AcolyteState State = AcolyteState.Idle;
        public bool Dead;

        public void Draw()
        {
            if (Dead) return;
            // Scarlet CUBE
            Vector3
                center = Position + new Vector3(0, .65f, 0),
                size = new(.95f, 1.3f, .95f);
            Raylib.DrawCubeV(center, size, new(215, 20, 35, 255));
            EldritchGeomertyDrawer.DrawThickWireBox(center, size, .045f, Color.Black);
        }
    }
}
