using LIBERTAS_AVT_CAEDES.Core;
using LIBERTAS_AVT_CAEDES.Geometry;
using LIBERTAS_AVT_CAEDES.Items;
using LIBERTAS_AVT_CAEDES.Telemetry;
using Raylib_cs;
using System.Numerics;

namespace LIBERTAS_AVT_CAEDES.Entities
{
    public enum CoverMode { Free, InCover, Peeking, Vaulting }

    public enum MunitionType { PistolRounds, ShotgunShells, RifleRounds }

    public class Player
    {
        public Vector3 Position = Vector3.Zero;
        public float
            RotationY = 0f, // Yaw in radians
            HP = 100f,
            MaxHP = 100f,
            Stamina = 100f,
            MaxStamina = 100f;

        public Dictionary<MunitionType, int> AmmoPools { get; } = new()
        {
            [MunitionType.PistolRounds] = 50,
            [MunitionType.ShotgunShells] = 14,
            [MunitionType.RifleRounds] = 90
        };

        public WeaponType CurrentWeapon = WeaponType.SemiAutoPistol;
        public HashSet<WeaponType> OwnedWeapons { get; } = [WeaponType.SemiAutoPistol];
        public float FireTimer = 0f; // Time until next shot can be fired

        public CoverMode CoverState = CoverMode.Free;
        public BoundingBox ActiveCoverBox;
        public Vector3 CoverAnchor, VaultTarget;
        public float VaultProgress = 0f;

        public bool
            HasSuppressor, HasSilentBoots, HasIncendiaryMod, HasBlessedMod, HasNightHalo,
            FlashlightOn = true;

        public int
            TalismanCrit, TalismanHP, TalismanStamina, TalismanIntangible, TalismanHexShield;

        public float ShieldHP = 0f;
        public int TotalPickupsAcquired = 0;

        private Vector3 _prevPos = Vector3.Zero;
        private float
            _dist30sAcc = 0f,
            _dist30sTimer = 0f;

        public int
            LevelDeaths = 0;

        public bool CanAim => CoverState is not CoverMode.Vaulting;
        public static bool CanShoot => true;

        public static MunitionType GetMunitionTypeForWeapon(WeaponType weapon) =>
            weapon switch
            {
                WeaponType.SemiAutoPistol => MunitionType.PistolRounds,
                WeaponType.SemiAutoShotgun => MunitionType.ShotgunShells,
                WeaponType.FullAutoRifle => MunitionType.RifleRounds,
                _ => MunitionType.PistolRounds
            };

        public int GetCurrentAmmo() =>
            AmmoPools[GetMunitionTypeForWeapon(CurrentWeapon)];

        public bool TryConsumeAmmo()
        {
            MunitionType type = GetMunitionTypeForWeapon(CurrentWeapon);
            if (AmmoPools[type] <= 0) return false;
            AmmoPools[type]--;
            return true;
        }

        public void ResetForLevel(Vector3 spawnPos)
        {
            Position = spawnPos;
            _prevPos = spawnPos;
            CoverState = CoverMode.Free;
            LevelDeaths = 0;
        }

        public void UpdateTelemetryDistance(float dt, int stage)
        {
            _dist30sAcc += Vector3.Distance(Position, _prevPos);
            _prevPos = Position;
            _dist30sTimer += dt;

            if (_dist30sTimer >= 30f)
            {
                // Log telemetry
                TelemetryBus.Publish(stage, TelemetryEventType.DistanceTraversedIn30s, Position, $"Meters={_dist30sAcc:F2}");
                _dist30sAcc = _dist30sTimer = 0f;
            }
        }

        public void ApplyPickup(PickupKind kind, Random rng)
        {
            switch (kind)
            {
                case PickupKind.ShotgunCylinder:
                    OwnedWeapons.Add(WeaponType.SemiAutoShotgun);
                    CurrentWeapon = WeaponType.SemiAutoShotgun;
                    TotalPickupsAcquired++;
                    break;
                case PickupKind.RifleCylinder:
                    OwnedWeapons.Add(WeaponType.FullAutoRifle);
                    CurrentWeapon = WeaponType.FullAutoRifle;
                    TotalPickupsAcquired++;
                    break;
                case PickupKind.MunitionPyramid:
                    AmmoPools[MunitionType.PistolRounds] += rng.Next(12, 25);
                    AmmoPools[MunitionType.ShotgunShells] += rng.Next(4, 10);
                    AmmoPools[MunitionType.RifleRounds] += rng.Next(20, 45);
                    break;
                case PickupKind.AnkhRation:
                    HP = MathF.Min(MaxHP, HP + rng.Next(25, 50));
                    break;
                case PickupKind.Suppressor:
                    HasSuppressor = true;
                    TotalPickupsAcquired++;
                    break;
                case PickupKind.SilentBoots:
                    HasSilentBoots = true;
                    TotalPickupsAcquired++;
                    break;
                case PickupKind.IncendiaryMod:
                    HasIncendiaryMod = true;
                    TotalPickupsAcquired++;
                    break;
                case PickupKind.BlessedMod:
                    HasBlessedMod = true;
                    TotalPickupsAcquired++;
                    break;
                case PickupKind.NightHalo:
                    HasNightHalo = true;
                    TotalPickupsAcquired++;
                    break;
                case PickupKind.TalismanCrit:
                    TalismanCrit++;
                    TotalPickupsAcquired++;
                    break;
                case PickupKind.TalismanHP:
                    TalismanHP++;
                    MaxHP += 20f;
                    HP += 20f;
                    TotalPickupsAcquired++;
                    break;
                case PickupKind.TalismanStamina:
                    TalismanStamina++;
                    MaxStamina += 25f;
                    TotalPickupsAcquired++;
                    break;
                case PickupKind.TalismanIntangible:
                    TalismanIntangible++;
                    TotalPickupsAcquired++;
                    break;
                case PickupKind.TalismanHexShield:
                    TalismanHexShield++;
                    ShieldHP += 40f;
                    TotalPickupsAcquired++;
                    break;
            }
        }

        public void TakeDamage(float dmg, string src, int stage, Random rng, Action<Vector3, bool, int> spawnGore)
        {
            if (rng.NextDouble() < MathF.Min(.25f, TalismanIntangible * .08f)) return;

            if (ShieldHP > 0f)
            {
                float absorbed = MathF.Min(ShieldHP, dmg);
                ShieldHP -= absorbed;
                dmg -= absorbed;
            }

            if (dmg <= 0f) return;

            HP -= dmg;
            spawnGore(Position, false, 4);

            if (HP <= 0f)
            {
                LevelDeaths++;
                TelemetryBus.Publish(stage, TelemetryEventType.PlayerDied, Position, $"DeathCountOnLevel={LevelDeaths};Killer={src}");
                HP = MaxHP;
                Position = Vector3.Zero;
                CoverState = CoverMode.Free;
            }
        }

        public void Draw()
        {
            // Parabolic vertical arc while vaulting over cover
            float vaultArcY = CoverState is CoverMode.Vaulting
                ? MathF.Sin(VaultProgress * MathF.PI) * 1.25f
                : 0f;

            Vector3 basePos = Position + new Vector3(0, vaultArcY, 0);
            float pyramidH = CoverState is CoverMode.InCover ? .95f : 1.55f;
            Vector3 apex = basePos + new Vector3(0, pyramidH, 0);

            // Playable PYRAMID
            Raylib.DrawCylinderEx(basePos, apex, .65f, 0, 4, Color.Green);
            for (int i = 0; i < 4; i++)
            {
                float
                    angle = i * MathF.PI * .5f,
                    nextAngle = (i + 1) * MathF.PI * .5f;
                Vector3
                    c1 = basePos + new Vector3(MathF.Cos(angle) * .65f, .02f, MathF.Sin(angle) * .65f),
                    c2 = basePos + new Vector3(MathF.Cos(nextAngle) * .65f, .02f, MathF.Sin(nextAngle) * .65f);
                EldritchGeomertyDrawer.DrawThickLine3D(c1, apex, .035f, Color.Black);
                EldritchGeomertyDrawer.DrawThickLine3D(c1, c2, .035f, Color.Black);
            }

            // Stackable Pentagonal Prism Talismans
            int totalTalismans = TalismanCrit + TalismanHP + TalismanStamina + TalismanIntangible + TalismanHexShield;
            for (int i = 0; i < Math.Min(totalTalismans, 8); i++)
            {
                Vector3 stackPos = Position + new Vector3(-.75f, .3f + i * .15f, -.5f);
                EldritchGeomertyDrawer.DrawRegularPrism(stackPos, 5, .18f, .18f, .08f, Color.Gold);
            }

            // Night goggle HALO
            if (HasNightHalo)
                Raylib.DrawCylinderWiresEx(
                    Position + new Vector3(0, pyramidH + .3f, 0),
                    Position + new Vector3(0, pyramidH + .36f, 0),
                    .45f, .45f, 16, Color.Lime);


            // HEX MESH SHIELD
            if (ShieldHP > 0)
                Raylib.DrawCylinderWiresEx(Position, Position + new Vector3(0, 1.3f, 0), .95f, .95f, 6, Color.SkyBlue);

            // FLASHLIGHT
            if (FlashlightOn)
            {
                Vector3
                    flDir = new(MathF.Sin(RotationY), 0, MathF.Cos(RotationY)),
                    tip = GameEngine.GetClippedFlashlightEndpoint(Position + new Vector3(0, .5f, 0), flDir, 12f);
                EldritchGeomertyDrawer
                    .DrawThickLine3D(
                    Position + new Vector3(0, .5f, 0),
                    tip,
                    .04f, new(220, 255, 190, 140));
            }
        }
    }
}
