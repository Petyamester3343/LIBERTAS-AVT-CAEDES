using Raylib_cs;
using System.Numerics;

using LIBERTAS_AVT_CAEDES.Geometry;

namespace LIBERTAS_AVT_CAEDES.Items
{
    public enum WeaponType { SemiAutoPistol, SemiAutoShotgun, FullAutoRifle }

    public enum PickupKind
    {
        ShotgunCylinder,
        RifleCylinder,
        MunitionPyramid,
        AnkhRation,
        GoldOctahedron,
        Suppressor,
        SilentBoots,
        IncendiaryMod,
        BlessedMod,
        NightHalo,
        TalismanCrit,
        TalismanHP,
        TalismanStamina,
        TalismanIntangible,
        TalismanHexShield
    }

    public class PickupItem
    {
        public Vector3 Position { get; set; }
        public PickupKind Kind { get; set; }
        public bool Acquired { get; set; } = false;

        public void Draw(float t)
        {
            if (Acquired) return;

            Vector3 pos = Position + new Vector3(0, .35f * MathF.Sin(3f * t) * .1f, 0);

            switch (Kind)
            {
                case PickupKind.GoldOctahedron:
                    {
                        EldritchGeomertyDrawer.DrawOctahedron(pos, new(.45f, .2f, .45f), Color.Gold);
                        break;
                    }
                case PickupKind.ShotgunCylinder:
                    {
                        // Elliptical cylinder for the semi-auto shotgun
                        EldritchGeomertyDrawer.DrawRegularPrism(pos, 16, .45f, .22f, .3f, Color.Orange);
                        break;
                    }
                case PickupKind.RifleCylinder:
                    {
                        // Slim circular cylinder for the full-auto assault rifle
                        Raylib.DrawCylinderEx(pos, pos + new Vector3(0, .7f, 0), .12f, .12f, 12, Color.LightGray);
                        break;
                    }
                case PickupKind.MunitionPyramid:
                    {
                        // Elongated square pyramid munition
                        Raylib.DrawCylinderEx(pos, pos + new Vector3(0, .75f, 0), .28f, 0, 4, Color.Lime);
                        break;
                    }
                case PickupKind.AnkhRation:
                    {
                        // Ankh healing ration (sponsored by ANVBIS INCORPORATED)
                        Raylib.DrawCube(pos, .15f, .5f, .15f, Color.RayWhite);
                        Raylib.DrawCube(pos + new Vector3(0, .1f, 0), .4f, .12f, .12f, Color.RayWhite);
                        Raylib.DrawCylinderWiresEx(pos + new Vector3(0, .35f, 0), pos + new Vector3(0, .42f, 0), .18f, .18f, 12, Color.Gold);
                        break;
                    }
                default:
                    {
                        Color talismanColor = Kind switch
                        {
                            PickupKind.TalismanCrit => Color.Red,
                            PickupKind.TalismanHP => Color.Green,
                            PickupKind.TalismanStamina => Color.Yellow,
                            PickupKind.TalismanIntangible => Color.Violet,
                            PickupKind.TalismanHexShield => Color.SkyBlue,
                            _ => Color.White
                        };
                        EldritchGeomertyDrawer.DrawRegularPrism(pos, 5, .35f, .35f, .14f, talismanColor);
                        break;
                    }
            }
        }
    }
}
