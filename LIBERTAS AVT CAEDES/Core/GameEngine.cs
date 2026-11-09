using LIBERTAS_AVT_CAEDES.Entities;
using LIBERTAS_AVT_CAEDES.Geometry;
using LIBERTAS_AVT_CAEDES.Items;
using LIBERTAS_AVT_CAEDES.Telemetry;
using LIBERTAS_AVT_CAEDES.UI;
using LIBERTAS_AVT_CAEDES.World;
using Raylib_cs;
using System.Numerics;

namespace LIBERTAS_AVT_CAEDES.Core
{
    public enum ScreenState
    {
        ConsentScreen,
        Playing,
        PostGameForm,
        Finished
    }

    public sealed class GameEngine : IDisposable
    {
        private const int TOTAL_STAGES = 3;
        private int _currentStage = 1;
        private float
            _adaptiveHPMult = 1f,
            _adaptivePerceptionMult = 1f;

        private ScreenState _screen = ScreenState.ConsentScreen;
        private readonly Random _rng = new();
        private readonly Player _player = new();
        private readonly static LabyrinthLevel _level = new();
        private Camera3D _cam;

        private Vector3
            _vaultStartPos,
            _coverNormal;

        private int
            _surveyAggro = 3,
            _surveyStealth = 3,
            _surveyExploration = 3;

        private readonly Dictionary<int, double> LevelDiscoveryRates = []; // levelIdx --> level's discoveryRate

        public GameEngine()
        {
            _cam = new()
            {
                Position = new(0, 22, 8),
                Target = Vector3.Zero,
                Up = new(0, 1, 0),
                FovY = 45f,
                Projection = CameraProjection.Perspective
            };
        }

        public void Run()
        {
            Raylib.InitWindow(1280, 720, "LIBERTAS AVT CAEDES // Modular Raylib-cs 8.1.0");
            Raylib.SetTargetFPS(Raylib.GetMonitorRefreshRate(Raylib.GetCurrentMonitor()));
            Raylib.HideCursor();

            while (!Raylib.WindowShouldClose())
            {
                float dt = Raylib.GetFrameTime();
                Update(dt);
                Draw();
            }

            Raylib.CloseWindow();
        }

        private void Update(float dt)
        {
            switch (_screen)
            {
                case ScreenState.ConsentScreen:
                    UpdateConsentScreen();
                    break;
                case ScreenState.Playing:
                    UpdateGameplay(dt);
                    break;
                case ScreenState.PostGameForm:
                    UpdateSurvey();
                    break;
            }
        }

        private void StartSession(bool consent)
        {
            PlayerTelemetry.ResetSession();
            PlayerTelemetry.ConsentGranted = consent;
            LevelDiscoveryRates.Clear();
            _level.Generate(_currentStage, _adaptiveHPMult, _rng);
            _player.ResetForLevel(Vector3.Zero);
            Raylib.HideCursor();
            _screen = ScreenState.Playing;
        }

        private void UpdateConsentScreen()
        {
            Raylib.ShowCursor();
            Vector2 mousePos = Raylib.GetMousePosition();
            bool isLMBPressed = Raylib.IsMouseButtonPressed(MouseButton.Left);

            Rectangle
                consentBtn = new(190, 390, 410, 65),
                declineBtn = new(640, 390, 430, 65);

            if (Raylib.IsKeyPressed(KeyboardKey.Y) || (isLMBPressed && Raylib.CheckCollisionPointRec(mousePos, consentBtn)))
                StartSession(true);
            if (Raylib.IsKeyPressed(KeyboardKey.N) || (isLMBPressed && Raylib.CheckCollisionPointRec(mousePos, declineBtn)))
                StartSession(false);
        }

        private void UpdateGameplay(float dt)
        {
            Ray mouseRay = Raylib.GetScreenToWorldRay(Raylib.GetMousePosition(), _cam);
            float tGround = -mouseRay.Position.Y / (mouseRay.Direction.Y is 0 ? -.0001f : mouseRay.Direction.Y);
            Vector3 crosshairWorld = mouseRay.Position + mouseRay.Direction * tGround;
            crosshairWorld.Y = .5f;

            Vector3 toCursor = crosshairWorld - _player.Position;
            _player.RotationY = MathF.Atan2(toCursor.X, toCursor.Z);

            float wheel = Raylib.GetMouseWheelMove();
            if (wheel is not 0 && _player.OwnedWeapons.Count > 1)
            {
                List<WeaponType> list = [.. _player.OwnedWeapons];
                int idx = (list.IndexOf(_player.CurrentWeapon) + (wheel > 0 ? 1 : -1) + list.Count) % list.Count;
                _player.CurrentWeapon = list[idx];
            }
            if (Raylib.IsKeyPressed(KeyboardKey.F)) _player.FlashlightOn = !_player.FlashlightOn;

            UpdatePlayerMovementAndCover(dt);

            bool isAiming = Raylib.IsMouseButtonDown(MouseButton.Right) && _player.CanAim;
            float haloBonus = _player.HasNightHalo ? 4.5f : 0;
            Vector3 camFocus = isAiming ? Vector3.Lerp(_player.Position, crosshairWorld, .42f) : _player.Position;
            _cam.Target = Vector3.Lerp(_cam.Target, camFocus, 10f * dt);
            _cam.Position = _cam.Target + new Vector3(0, (isAiming ? 25f : 20f) + haloBonus, 7f);

            _player.FireTimer -= dt;
            bool triggerPulled =
                _player.CurrentWeapon is WeaponType.FullAutoRifle
                ? Raylib.IsMouseButtonDown(MouseButton.Left)
                : Raylib.IsMouseButtonPressed(MouseButton.Left);

            if (triggerPulled && Player.CanShoot && _player.FireTimer <= 0f &&
                _player.GetCurrentAmmo() > 0)
                FirePlayerWeapon(crosshairWorld);

            (int, int) currCell = Pathfinder.ToCell(_player.Position, LabyrinthLevel.CellSize);
            int sightCells = _player.HasNightHalo ? 2 : 1;
            bool newlyDiscovered = false;
            double ratio = 0d;

            for (int dx = -sightCells, dz = -sightCells; dx <= sightCells && dz <= sightCells; dx++, dz++)
            {
                (int, int) checkCell = (currCell.Item1 + dx, currCell.Item2 + dz);
                if (_level.WalkableCells.Contains(checkCell))
                {
                    Vector3 cellWorldPos = new(checkCell.Item1 * LabyrinthLevel.CellSize, 0f, checkCell.Item2 * LabyrinthLevel.CellSize);
                    if (HasLineOfSight(_player.Position, cellWorldPos) && _level.DiscoveredCells.Add(checkCell))
                        newlyDiscovered = true;
                }
            }

            if (newlyDiscovered) ratio = 100d * (_level.DiscoveredCells.Count / _level.WalkableCells.Count);

            _player.UpdateTelemetryDistance(dt);

            foreach (Hostage h in _level.Hostages.Where(h => !h.Saved && !h.Dead))
            {
                h.Update(dt, _player.Position);
                if (Vector3.Distance(h.Position, _level.BrightHypercubeArtifactPos) < 2.2f)
                {
                    h.Saved = true;
                    PlayerTelemetry.HostagesTransported = (uint)_level.Hostages.Count(x => x.Saved);
                }
            }

            foreach (PickupItem p in _level.Pickups.Where(p => !p.Acquired))
                if (Vector3.Distance(_player.Position, p.Position) < 1.5f)
                {
                    p.Acquired = true;
                    PlayerTelemetry.AcquiredPickups = (uint)_level.Pickups.Count(p => p.Acquired);
                    _player.ApplyPickup(p.Kind, _rng);
                }

            UpdateHostilesAndRituals(dt);

            bool LIBERTAS = (_level.Hostages.All(h => h.Saved || h.Dead) && _level.Hostages.Any(h => h.Saved));
            bool CAEDES = _level.Acolytes.All(a => a.Dead) && _level.Chimeras.All(c => c.Dead);
            if (LIBERTAS || CAEDES)
            {
                if (_currentStage < TOTAL_STAGES)
                {
                    _adaptiveHPMult = 1f + (_player.TotalPickupsAcquired * .12f);
                    _adaptivePerceptionMult = 1f + (_player.TotalPickupsAcquired * .08f);
                    LevelDiscoveryRates.Add(_currentStage, ratio);
                    _currentStage++;
                    _level.Generate(_currentStage, _adaptiveHPMult, _rng);
                    _player.ResetForLevel(Vector3.Zero);
                }
                else _screen = ScreenState.PostGameForm;
            }
        }

        private void UpdatePlayerMovementAndCover(float dt)
        {
            bool
                isCrouching = Raylib.IsKeyDown(KeyboardKey.LeftControl),
                isSprinting = Raylib.IsKeyDown(KeyboardKey.LeftShift) && !isCrouching && _player.Stamina > 5f;

            // Screen-Relative WASD: W = Up (-Z), S = Down (+Z), A = Strafe Left (-X), D = Strafe Right (+X)
            Vector3 moveInput = Vector3.Zero;
            if (Raylib.IsKeyDown(KeyboardKey.W)) moveInput.Z -= 1f;
            if (Raylib.IsKeyDown(KeyboardKey.S)) moveInput.Z += 1f;
            if (Raylib.IsKeyDown(KeyboardKey.A)) moveInput.X -= 1f;
            if (Raylib.IsKeyDown(KeyboardKey.D)) moveInput.X += 1f;

            if (moveInput.LengthSquared() > 1e-3f)
                moveInput = Vector3.Normalize(moveInput);

            // Toggle Cover with Q
            if (Raylib.IsKeyPressed(KeyboardKey.Q) && _player.CoverState is not CoverMode.Vaulting)
            {
                if (_player.CoverState is CoverMode.Free)
                {
                    foreach (var cov in _level.Covers)
                    {
                        Vector3 covCenter = (cov.Min + cov.Max) * .5f;
                        Vector3 covHalf = (cov.Max - cov.Min) * .5f;
                        Vector3 flatCenter = new(covCenter.X, 0, covCenter.Z);

                        if (Vector3.Distance(_player.Position, flatCenter) < 2.6f)
                        {
                            _player.ActiveCoverBox = cov;
                            Vector3 diff = _player.Position - flatCenter;

                            // Snap flush outside the thinner axis of the cover block
                            if (covHalf.Z < covHalf.X)
                            {
                                float signZ = diff.Z >= 0 ? 1f : -1f;
                                _coverNormal = new Vector3(0, 0, signZ);
                                _player.CoverAnchor = new Vector3(
                                    Math.Clamp(_player.Position.X, cov.Min.X + .3f, cov.Max.X - .3f),
                                    0f,
                                    flatCenter.Z + signZ * (covHalf.Z + 0.55f));
                            }
                            else
                            {
                                float signX = diff.X >= 0 ? 1f : -1f;
                                _coverNormal = new Vector3(signX, 0, 0);
                                _player.CoverAnchor = new Vector3(
                                    flatCenter.X + signX * (covHalf.X + .55f),
                                    0f,
                                    Math.Clamp(_player.Position.Z, cov.Min.Z + .3f, cov.Max.Z - .3f));
                            }

                            if (!_level.CollidesWithWorld(_player.CoverAnchor, ignoreCovers: true))
                            {
                                _player.Position = _player.CoverAnchor;
                                _player.CoverState = CoverMode.InCover;
                                break;
                            }
                        }
                    }
                }
                else _player.CoverState = CoverMode.Free;
            }

            // Cover, Peeking, and Vaulting State Machine
            if (_player.CoverState is CoverMode.InCover or CoverMode.Peeking)
            {
                Vector3
                    covCenter = (_player.ActiveCoverBox.Min + _player.ActiveCoverBox.Max) * .5f,
                    covHalf = (_player.ActiveCoverBox.Max - _player.ActiveCoverBox.Min) * .5f;

                // Pressing the WASD key pointing directly into the cover face
                bool pressingIntoCover = Vector3.Dot(moveInput, _coverNormal) < -.45f;

                if (Raylib.IsKeyPressed(KeyboardKey.Space) && pressingIntoCover)
                {
                    Vector3 oppositeOffset = new(
                        -_coverNormal.X * (covHalf.X + .65f),
                        0f,
                        -_coverNormal.Z * (covHalf.Z + .65f));

                    Vector3 candidateTarget = new(
                        _coverNormal.X != 0 ? covCenter.X + oppositeOffset.X : _player.CoverAnchor.X,
                        0f,
                        _coverNormal.Z != 0 ? covCenter.Z + oppositeOffset.Z : _player.CoverAnchor.Z);

                    if (!_level.CollidesWithWorld(candidateTarget, ignoreCovers: true))
                    {
                        _vaultStartPos = _player.Position;
                        _player.VaultTarget = candidateTarget;
                        _player.VaultProgress = 0f;
                        _player.CoverState = CoverMode.Vaulting;
                    }
                }
                else if (moveInput.LengthSquared() > 1e-2f)
                {
                    // Lean / Peek along the cover edge using WASD
                    Vector3 tangent = new(MathF.Abs(_coverNormal.Z), 0, MathF.Abs(_coverNormal.X));
                    Vector3 peekOffset = tangent * Vector3.Dot(moveInput, tangent) * 1.15f;
                    Vector3 peekCandidate = _player.CoverAnchor + peekOffset;
                    if (!_level.CollidesWithWorld(peekCandidate, ignoreCovers: true))
                    {
                        _player.CoverState = CoverMode.Peeking;
                        _player.Position = peekCandidate;
                    }
                }
                else
                {
                    _player.CoverState = CoverMode.InCover;
                    _player.Position = _player.CoverAnchor;
                }
            }
            else if (_player.CoverState is CoverMode.Vaulting)
            {
                _player.VaultProgress += dt * 2.8f;
                _player.Position = Vector3.Lerp(_vaultStartPos, _player.VaultTarget, Math.Clamp(_player.VaultProgress, 0f, 1f));
                if (_player.VaultProgress >= 1f)
                {
                    _player.Position = _player.VaultTarget;
                    _player.CoverState = CoverMode.Free;
                }
            }
            else
            {
                // Free Screen-Relative Movement (independent X and Z axis sliding)
                float speed = isCrouching ? 3.5f : (isSprinting ? 9.5f : 6f);
                _player.Stamina = (isSprinting && moveInput.LengthSquared() > 0)
                    ? MathF.Max(0, _player.Stamina - 22f * dt)
                    : MathF.Min(_player.MaxStamina, _player.Stamina + 14f * dt);

                Vector3 tryX = new(_player.Position.X + moveInput.X * speed * dt, 0f, _player.Position.Z);
                if (!_level.CollidesWithWorld(tryX))
                    _player.Position.X = tryX.X;

                Vector3 tryZ = new(_player.Position.X, 0f, _player.Position.Z + moveInput.Z * speed * dt);
                if (!_level.CollidesWithWorld(tryZ))
                    _player.Position.Z = tryZ.Z;

                float limit = (_level.CurrentHalfExtent - .85f) * LabyrinthLevel.CellSize;
                _player.Position.X = Math.Clamp(_player.Position.X, -limit, limit);
                _player.Position.Z = Math.Clamp(_player.Position.Z, -limit, limit);

                if (moveInput.LengthSquared() > 0 && !isCrouching && !_player.HasSilentBoots)
                    AlertAcolytesBySound(_player.Position, isSprinting ? 14f : 8f);
            }
        }

        private void FirePlayerWeapon(Vector3 tgt)
        {
            if (!_player.TryConsumeAmmo()) return;

            float baseDmg = _player.CurrentWeapon switch
            {
                WeaponType.SemiAutoPistol => 40f,
                WeaponType.SemiAutoShotgun => 24f,
                WeaponType.FullAutoRifle => 30f,
                _ => 35f
            };
            _player.FireTimer = _player.CurrentWeapon switch
            {
                WeaponType.SemiAutoPistol => .25f,
                WeaponType.SemiAutoShotgun => .65f,
                WeaponType.FullAutoRifle => .11f,
                _ => .25f
            };

            baseDmg *= (_player.HasBlessedMod ? 1.1f : 1) * (_rng.NextDouble() < _player.TalismanCrit * 1e-1 ? 2f : 1);

            int rays = _player.CurrentWeapon is WeaponType.SemiAutoShotgun ? 5 : 1;
            Vector3
                muzzle = _player.Position + new Vector3(0, .6f, 0),
                mainDir = Vector3.Normalize(new(tgt.X - _player.Position.X, 0, tgt.Z - _player.Position.Z));

            for (int r = 0; r < rays; r++)
            {
                float angleOffset = rays is 1 ? 0f : (r - 2) * .04f;
                Vector3
                    dir = Vector3.Transform(mainDir, Matrix4x4.CreateRotationY(angleOffset)),
                    rayEnd = muzzle + dir * 35f;

                foreach (BoundingBox w in _level.Walls)
                {
                    RayCollision wHit = Raylib.GetRayCollisionBox(new(muzzle, dir), w);
                    if (wHit.Hit && wHit.Distance < Vector3.Distance(muzzle, rayEnd))
                        rayEnd = muzzle + dir * wHit.Distance;
                }

                foreach (Acolyte a in _level.Acolytes.Where(a => !a.Dead))
                {
                    RayCollision col = Raylib.GetRayCollisionSphere(new(muzzle, dir), a.Position + new Vector3(0, .6f, 0), .85f);
                    if (col.Hit && col.Distance < Vector3.Distance(muzzle, rayEnd))
                    {
                        rayEnd = muzzle + dir * col.Distance;
                        a.HP -= baseDmg;
                        if (_player.HasIncendiaryMod) a.BurnDuration = 4f;
                        CheckAcolyteDeath(a);
                        if (!a.Dead) SpawnGore(a.Position, false, 6);
                        break;
                    }
                }

                foreach (Chimera c in _level.Chimeras.Where(c => !c.Dead))
                {
                    RayCollision col = Raylib.GetRayCollisionSphere(new(muzzle, dir), c.Position + new Vector3(0, .8f, 0), 1.1f);
                    if (col.Hit && col.Distance < Vector3.Distance(muzzle, rayEnd))
                    {
                        rayEnd = muzzle + dir * col.Distance;
                        c.HP -= baseDmg;
                        if (_player.HasIncendiaryMod) c.BurnDuration = 4f;
                        CheckChimeraDeath(c);
                        if (!c.Dead) SpawnGore(c.Position, false, 8);
                        break;
                    }
                }

                _level.Tracers.Add(new()
                {
                    Start = muzzle,
                    End = rayEnd,
                    Color = Color.Yellow,
                    TimeLeft = .12f
                });
            }

            if (!_player.HasSuppressor) AlertAcolytesBySound(_player.Position, 24f);
        }

        private void CheckAcolyteDeath(Acolyte a)
        {
            if (a.HP <= 0 && !a.Dead)
            {
                a.Dead = true;
                SpawnGore(a.Position, true, 14);
                PlayerTelemetry.AcolytesKilled = (uint)_level.Acolytes.Count(a => a.Dead);
            }
        }

        private void CheckChimeraDeath(Chimera c)
        {
            if (c.HP <= 0 && !c.Dead)
            {
                c.Dead = true;
                SpawnGore(c.Position, true, 22);
                PlayerTelemetry.ChimerasKilled = (uint)_level.Chimeras.Count(c => c.Dead);
            }
        }

        private void AlertAcolytesBySound(Vector3 origin, float radius)
        {
            float scaledRadius = radius * _adaptivePerceptionMult;
            foreach (var a in _level.Acolytes.Where(a => !a.Dead && a.State is not AcolyteState.Combat))
            {
                if (Vector3.Distance(a.Position, origin) < scaledRadius)
                {
                    a.State = AcolyteState.Chasing;
                    a.LastKnownPlayerPosition = origin;
                }
            }
        }

        private static bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 start = from + new Vector3(0, .8f, 0);
            Vector3 end = to + new Vector3(0, .8f, 0);
            Vector3 diff = end - start;
            float dist = diff.Length();
            if (dist < 1e-2f) return true;

            Ray ray = new(start, diff / dist);

            foreach (BoundingBox w in _level.Walls)
            {
                RayCollision hit = Raylib.GetRayCollisionBox(ray, w);
                if (hit.Hit && hit.Distance < dist) return false;
            }

            return true;
        }

        private static void MoveAlongAStar(
            ref Vector3 entityPos,
            Vector3 tgtPos,
            float speed,
            float dt,
            bool ignoreCovers = false,
            float entityHalfSize = .35f)
        {
            List<Vector3> path = Pathfinder.FindPath(entityPos, tgtPos, _level.WalkableCells, LabyrinthLevel.CellSize);

            Vector3 nextNode = path.Count > 1 ? path[1] : tgtPos;
            if (path.Count > 1 && Vector3.Distance(new(entityPos.X, 0, entityPos.Z), path[0]) > 1.8f)
                nextNode = path[0];

            Vector3 dir = nextNode - entityPos;
            dir.Y = 0;
            if (dir.LengthSquared() <= 1e-2f) return;

            dir = Vector3.Normalize(dir);

            if (!ignoreCovers)
            {
                Ray moveRay = new(entityPos + new Vector3(0, .5f, 0), dir);
                foreach (BoundingBox cov in _level.Covers)
                {
                    RayCollision hit = Raylib.GetRayCollisionBox(moveRay, cov);
                    if (hit.Hit && hit.Distance < 2f)
                    {
                        Vector3 perp = new(-dir.Z, 0f, dir.X),
                            covCenter = (cov.Min + cov.Max) * .5f;
                        float sideSign = Vector3.Dot(entityPos - covCenter, perp) >= 0 ? 1f : -1f;
                        dir = Vector3.Normalize(dir + perp * (sideSign * 1.2f));
                        break;
                    }
                }
            }

            float moveStep = speed * dt;

            Vector3
                tryX = new(entityPos.X + dir.X * moveStep, 0, entityPos.Z),
                tryZ = new(entityPos.X, 0, entityPos.Z + dir.Z * moveStep);

            if (!_level.CollidesWithWorld(tryX, ignoreCovers, entityHalfSize)) entityPos.X = tryX.X;
            if (!_level.CollidesWithWorld(tryZ, ignoreCovers, entityHalfSize)) entityPos.Z = tryZ.Z;
        }

        private void UpdateHostilesAndRituals(float dt)
        {
            float sightRange = (13f + (_player.FlashlightOn ? 9f : 0)) * _adaptivePerceptionMult;

            foreach (Acolyte a in _level.Acolytes.Where(a => !a.Dead))
            {
                if (a.BurnDuration > 0f)
                {
                    a.BurnDuration -= dt;
                    a.HP -= 12f * dt;
                    CheckAcolyteDeath(a);
                    if (a.Dead) continue;
                }

                float distToPlayer = Vector3.Distance(a.Position, _player.Position);
                bool playerHiddenInCover = _player.CoverState is CoverMode.InCover;

                if (a.State is AcolyteState.Idle)
                {
                    foreach (var g in _level.Gore.Where(g => g.IsCorpseShard))
                    {
                        if (Vector3.Distance(a.Position, g.Position) < 10f * _adaptivePerceptionMult)
                        {
                            a.State = AcolyteState.Investigating;
                            a.LastKnownPlayerPosition = g.Position;
                            break;
                        }
                    }
                }

                if (distToPlayer < sightRange && !playerHiddenInCover && HasLineOfSight(a.Position, _player.Position))
                {
                    if (a.State is not AcolyteState.Combat and not AcolyteState.Conjuring)
                    {
                        if (_level.ActiveRitual is null && _rng.NextDouble() < .2)
                        {
                            a.State = AcolyteState.Conjuring;
                            Vector3 safeRitualPos = _level.GetNearestWalkableCellCenter(a.Position, yHeight: 1.2f);

                            _level.ActiveRitual = new DarkHypercubeRitual
                            {
                                Position = safeRitualPos,
                                Countdown = 5f,
                                Active = true,
                                Conjurers = [a]
                            };
                        }
                        else
                        {
                            a.State = AcolyteState.Combat;
                            a.LastKnownPlayerPosition = _player.Position;
                            PlayerTelemetry.PlayerBusted++;
                        }
                    }
                }

                if (a.State is AcolyteState.Investigating or AcolyteState.Chasing)
                {
                    if (Vector3.Distance(a.Position, a.LastKnownPlayerPosition) > 1.2f)
                        MoveAlongAStar(ref a.Position, a.LastKnownPlayerPosition, 3.9f, dt, ignoreCovers: false, entityHalfSize: .45f);
                    else
                        a.State = AcolyteState.Idle;
                }
                else if (a.State is AcolyteState.Combat)
                {
                    if (distToPlayer > 7f || !HasLineOfSight(a.Position, _player.Position))
                        MoveAlongAStar(ref a.Position, _player.Position, 4.5f, dt, ignoreCovers: false, entityHalfSize: .45f);

                    a.FireCooldown -= dt;
                    if (a.FireCooldown <= 0 && HasLineOfSight(a.Position, _player.Position))
                    {
                        a.FireCooldown = 1.15f;
                        Hostage? nearbyHostage = _level.Hostages.FirstOrDefault(h =>
                            !h.Dead && !h.Saved && Vector3.Distance(a.Position, h.Position) < 10f && HasLineOfSight(a.Position, h.Position));
                        Vector3 tgtPos = (nearbyHostage is not null && _rng.NextDouble() < .35)
                            ? nearbyHostage.Position
                            : _player.Position;

                        _level.Tracers.Add(new()
                        {
                            Start = a.Position + new Vector3(0, .6f, 0),
                            End = tgtPos + new Vector3(0, .5f, 0),
                            Color = Color.Red,
                            TimeLeft = .14f
                        });

                        if (nearbyHostage is not null && tgtPos == nearbyHostage.Position)
                        {
                            nearbyHostage.HP -= 25f;
                            SpawnGore(nearbyHostage.Position, false, 5);
                            if (nearbyHostage.HP <= 0)
                            {
                                nearbyHostage.Dead = true;
                                SpawnGore(nearbyHostage.Position, true, 12);
                            }
                        }
                        else _player.TakeDamage(15f, _rng, SpawnGore);
                    }
                }
            }

            if (_level.ActiveRitual is { Active: true })
            {
                if (_level.ActiveRitual.AllConjurersKilled)
                {
                    _level.ActiveRitual.Active = false;
                    _level.ActiveRitual = null;
                }
                else
                {
                    _level.ActiveRitual.Countdown -= dt;
                    if (_level.ActiveRitual.Countdown <= 0f)
                    {
                        Vector3 safeChimeraSpawn = _level.GetNearestWalkableCellCenter(_level.ActiveRitual.Position, yHeight: 0f);

                        _level.Chimeras.Add(new()
                        {
                            Position = safeChimeraSpawn,
                            HP = 450f * _adaptiveHPMult
                        });
                        _level.ActiveRitual.Active = false;
                        _level.ActiveRitual = null;
                    }
                }
            }

            foreach (Chimera c in _level.Chimeras.Where(c => !c.Dead))
            {
                if (c.BurnDuration > 0f)
                {
                    c.BurnDuration -= dt;
                    c.HP -= 12f * dt;
                    CheckChimeraDeath(c);
                    if (c.Dead) continue;
                }

                Vector3 toPlayer = _player.Position - c.Position;
                toPlayer.Y = 0;
                c.RotationY = MathF.Atan2(toPlayer.X, toPlayer.Z);

                if (toPlayer.Length() > 1.85f)
                    MoveAlongAStar(ref c.Position, _player.Position, 5.4f, dt, ignoreCovers: true, entityHalfSize: .95f);
                else
                    _player.TakeDamage(25f * dt, _rng, SpawnGore);
            }

            for (int i = _level.Gore.Count - 1; i >= 0; i--)
            {
                GoreTriangle g = _level.Gore[i];
                g.Update(dt);
                if (g.Lifetime <= 0) _level.Gore.RemoveAt(i);
                else _level.Gore[i] = g;
            }

            for (int i = _level.Tracers.Count - 1; i >= 0; i--)
            {
                RayTracer t = _level.Tracers[i];
                t.TimeLeft -= dt;
                if (t.TimeLeft <= 0) _level.Tracers.RemoveAt(i);
                else _level.Tracers[i] = t;
            }
        }

        private void SpawnGore(Vector3 origin, bool isDead, int c)
        {
            for (int i = 0; i < c; i++)
                _level.Gore.Add(GoreTriangle.Create(_rng, origin + new Vector3(0, .3f, 0), isDead));
        }

        private void UpdateSurvey()
        {
            Raylib.ShowCursor();
            Vector2 mousePos = Raylib.GetMousePosition();
            bool isLMBClicked = Raylib.IsMouseButtonPressed(MouseButton.Left);

            int[] rowYCoords = [255, 330, 405];

            for (int r = 0, i = 1; r < 3 && i <= 5; r++, i++)
            {
                Rectangle box = new(190 + 560 + (i - 1) * 55, rowYCoords[r] - 4, 42, 32);
                if (isLMBClicked && Raylib.CheckCollisionPointRec(mousePos, box))
                {
                    switch (r)
                    {
                        case 0: _surveyAggro = i; break;
                        case 1: _surveyStealth = i; break;
                        case 2: _surveyExploration = i; break;
                        default: break;
                    }
                }
            }

            if (Raylib.IsKeyPressed(KeyboardKey.One)) _surveyAggro = (_surveyAggro % 5) + 1;
            if (Raylib.IsKeyPressed(KeyboardKey.Two)) _surveyStealth = (_surveyStealth % 5) + 1;
            if (Raylib.IsKeyPressed(KeyboardKey.Three)) _surveyExploration = (_surveyExploration % 5) + 1;

            Rectangle submitBtn = new(190, 495, 480, 60);
            if (Raylib.IsKeyPressed(KeyboardKey.Enter) || (isLMBClicked && Raylib.CheckCollisionPointRec(mousePos, submitBtn)))
            {
                PlayerTelemetry.SelfSurveyAggro = (ushort)_surveyAggro;
                PlayerTelemetry.SelfSurveyStealth = (ushort)_surveyStealth;
                PlayerTelemetry.SelfSurveyDiscovery = (ushort)_surveyExploration;

                PlayerTelemetry.EnemiesKilled = PlayerTelemetry.AcolytesKilled + PlayerTelemetry.ChimerasKilled;

                PlayerTelemetry.RealSurveyAggro = 100 * (1 / PlayerTelemetry.EnemiesKilled) % 5 + 1;
                PlayerTelemetry.RealSurveyStealth = 100 * (1 / PlayerTelemetry.PlayerBusted) % 5 + 1;
                PlayerTelemetry.RealSurveyDiscovery = 100 * (1 / PlayerTelemetry.TotalDiscoveryRate) % 5 + 1;

                _screen = ScreenState.Finished;
            }
        }

        public static Vector3 GetClippedFlashlightEndpoint(Vector3 start, Vector3 dir, float maxRange)
        {
            Vector3 end = start + dir * maxRange;
            Ray ray = new(start, dir);
            foreach (BoundingBox w in _level.Walls)
            {
                RayCollision hit = Raylib.GetRayCollisionBox(ray, w);
                if (hit.Hit && hit.Distance < Vector3.Distance(start, end))
                    end = start + dir * hit.Distance;
            }
            foreach (BoundingBox c in _level.Covers)
            {
                RayCollision hit = Raylib.GetRayCollisionBox(ray, c);
                if (hit.Hit && hit.Distance < Vector3.Distance(start, end))
                    end = start + dir * hit.Distance;
            }

            return end;
        }

        private void Draw()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(8, 6, 12, 255));

            switch (_screen)
            {
                case ScreenState.ConsentScreen: DrawConsentModal(); break;
                case ScreenState.Playing:
                    {
                        float t = (float)Raylib.GetTime();
                        Raylib.BeginMode3D(_cam);

                        _level.DrawEnvironment(t);
                        _level.ActiveRitual?.Draw(t, _player.Position, _level.WalkableCells, LabyrinthLevel.CellSize);

                        foreach (GoreTriangle g in _level.Gore) g.Draw();
                        foreach (PickupItem p in _level.Pickups) p.Draw(t);
                        foreach (Hostage h in _level.Hostages) h.Draw();
                        foreach (Acolyte a in _level.Acolytes) a.Draw();
                        foreach (Chimera c in _level.Chimeras) c.Draw(t);
                        _player.Draw();

                        foreach (RayTracer tracer in _level.Tracers)
                            Raylib.DrawLine3D(tracer.Start, tracer.End, tracer.Color);

                        Raylib.EndMode3D();

                        DrawTacticalHud();
                        break;
                    }
                case ScreenState.PostGameForm: DrawSurveyModal(); break;
                case ScreenState.Finished:
                    {
                        PlayerTelemetry.TotalDiscoveryRate = LevelDiscoveryRates.Average(x => x.Value);
                        Raylib.DrawRectangle(140, 140, 1000, 440, new Color(14, 12, 22, 245));
                        Raylib.DrawRectangleLinesEx(new Rectangle(140, 140, 1000, 440), 3f, Color.Green);
                        Raylib.DrawText("RESEARCH TELEMETRY & SURVEY LOGGED", 180, 175, 28, Color.Green);
                        Raylib.DrawText($"Session GUID: {PlayerTelemetry.SessionGuid}", 180, 225, 18, Color.Gold);
                        Raylib.DrawText($"Self-Reported Ratings -> Aggression: {PlayerTelemetry.SelfSurveyAggro}/5 | Stealth: {PlayerTelemetry.SelfSurveyStealth}/5 | Exploration: {PlayerTelemetry.SelfSurveyDiscovery}/5", 180, 265, 18, Color.RayWhite);
                        Raylib.DrawText($"Telemetry CSV successfully written to: telemetry_{PlayerTelemetry.SessionGuid}.csv", 180, 330, 18, Color.LightGray);
                        Raylib.DrawText("Press ESC to exit testing suite.", 180, 420, 20, Color.Yellow);
                        break;
                    }
            }

            DrawCrosshair();

            Raylib.EndDrawing();
        }

        private void DrawTacticalHud()
        {
            // Top-left Tactical Vitals Panel
            Raylib.DrawRectangle(16, 16, 430, 145, new(14, 12, 22, 220));
            Raylib.DrawRectangleLinesEx(new(16, 16, 430, 145), 2f, new(190, 25, 45, 255));

            Raylib.DrawText($"LIBERTAS AVT CAEDES || STAGE {_currentStage}/{TOTAL_STAGES}", 28, 24, 20, Color.Red);

            // HP Bar
            float hpPct = Math.Clamp(_player.HP / _player.MaxHP, 0f, 1f);
            Raylib.DrawText($"HP", 28, 55, 18, Color.RayWhite);
            Raylib.DrawRectangle(95, 55, 220, 16, new(40, 15, 20, 255));
            Raylib.DrawRectangle(95, 55, (int)(220 * hpPct), 16, new(210, 30, 45, 255));
            Raylib.DrawText($"{_player.HP:F0}/{_player.MaxHP:F0}", 325, 54, 18, Color.RayWhite);

            // Hex Shield Overlay Bar
            if (_player.ShieldHP > 0)
            {
                float shPct = Math.Clamp(_player.ShieldHP / 120f, 0f, 1f);
                Raylib.DrawRectangle(95, 73, (int)(220 * shPct), 5, Color.SkyBlue);
            }

            // Stamina Bar
            float stPct = Math.Clamp(_player.Stamina / _player.MaxStamina, 0f, 1f);
            Raylib.DrawText("STM", 28, 84, 18, Color.RayWhite);
            Raylib.DrawRectangle(95, 84, 20, 14, new(15, 35, 20, 255));
            Raylib.DrawRectangle(95, 84, (int)(220 * stPct), 14, Color.Lime);

            // Weapons, Munition & Cover Status
            MunitionType ammoType = Player.GetMunitionTypeForWeapon(_player.CurrentWeapon);
            Raylib.DrawText($"WPN: {_player.CurrentWeapon}\t|\t{ammoType}: {_player.GetCurrentAmmo()}", 28, 110, 17, Color.Gold);
            Color covCol = _player.CoverState is CoverMode.Free ? Color.Gray : Color.SkyBlue;
            Raylib.DrawText($"STANCE: {_player.CoverState}\t(Q: Cover | Space+Dir: Vault)", 28, 134, 15, covCol);

            // Top-Right Objectives &Telemetry Discovery Tracker
            MinimapRenderer.Draw(
                panelX: 1034,
                panelY: 16,
                panelSize: 230,
                level: _level,
                player: _player);

            // Bottom-Left Attachment & Talisman Badge Rack
            Raylib.DrawRectangle(16, 655, 760, 48, new Color(14, 12, 22, 210));
            Raylib.DrawRectangleLinesEx(new Rectangle(16, 655, 760, 48), 2f, Color.DarkGray);
            string mods = $"[F] Light:{(_player.FlashlightOn ? "ON" : "OFF")} | " +
                          $"Supp:{(_player.HasSuppressor ? "YES" : "NO")} | " +
                          $"Boots:{(_player.HasSilentBoots ? "YES" : "NO")} | " +
                          $"Incend:{(_player.HasIncendiaryMod ? "YES" : "NO")} | " +
                          $"Bless:{(_player.HasBlessedMod ? "YES" : "NO")} | " +
                          $"Talismans(C/H/S/I/Sh): {_player.TalismanCrit}/{_player.TalismanHP}/{_player.TalismanStamina}/{_player.TalismanIntangible}/{_player.TalismanHexShield}";
            Raylib.DrawText(mods, 28, 670, 15, Color.RayWhite);

            // Chimera Ritual Countdown Alert Banner
            if (_level.ActiveRitual is { Active: true })
            {
                Raylib.DrawRectangle(310, 175, 660, 54, new(45, 5, 55, 235));
                Raylib.DrawRectangleLinesEx(new(310, 175, 660, 54), 3f, Color.Magenta);
                Raylib.DrawText($"DARK HYPERCUBE CONJURING: {_level.ActiveRitual.Countdown:F1}s! FOLLOW A* GPS ARROW!", 330, 192, 20, Color.Magenta);
            }
        }

        private static void DrawConsentModal()
        {
            Raylib.DrawRectangle(140, 130, 1000, 440, new(16, 14, 24, 245));
            Raylib.DrawRectangleLinesEx(new(140, 130, 1000, 440), 3f, Color.Red);
            Raylib.DrawText("LIBERTAS AUT CAEDES // TELEMETRY CONSENT", 190, 175, 32, Color.Red);
            Raylib.DrawText($"Anonymous Tester ID: {PlayerTelemetry.SessionGuid}", 190, 235, 20, Color.Gold);
            Raylib.DrawText("This research build uses an Observer-Logger architecture to record anonymous", 190, 285, 20, Color.RayWhite);
            Raylib.DrawText("gameplay ratios (hostages, combat, 30s traversal distance, and DFS discovery).", 190, 315, 20, Color.RayWhite);

            Vector2 mousePos = Raylib.GetMousePosition();

            Rectangle consentBtn = new(190, 390, 410, 65);
            bool consentHover = Raylib.CheckCollisionPointRec(mousePos, consentBtn);
            Raylib.DrawRectangleRec(consentBtn, consentHover ? new(30, 95, 45, 255) : new(20, 65, 30, 255));
            Raylib.DrawRectangleLinesEx(consentBtn, consentHover ? 3f : 2f, consentHover ? Color.RayWhite : Color.Lime);
            Raylib.DrawText("[Press Y / Click] CONSENT & LAUNCH", 230, 412, 18, Color.Lime);

            Rectangle declineBtn = new(640, 390, 430, 65);
            bool declineHover = Raylib.CheckCollisionPointRec(mousePos, declineBtn);
            Raylib.DrawRectangleRec(declineBtn, declineHover ? new(95, 30, 35, 255) : new(65, 20, 25, 255));
            Raylib.DrawRectangleLinesEx(declineBtn, declineHover ? 3f : 2f, declineHover ? Color.RayWhite : Color.Red);
            Raylib.DrawText("[Press N] DECLINE (PLAY OFFLINE)", 685, 412, 18, Color.RayWhite);
        }

        private void DrawSurveyModal()
        {
            Raylib.DrawRectangle(140, 110, 1000, 500, new(16, 14, 24, 245));
            Raylib.DrawRectangleLinesEx(new(140, 110, 1000, 500), 3f, Color.Gold);
            Raylib.DrawText("POST-MISSION PLAYSTYLE SELF-ASSESSMENT", 190, 150, 30, Color.Gold);
            Raylib.DrawText("Compare your subjective experience against the Observer-Logger CSV data:", 190, 195, 20, Color.LightGray);

            Vector2 mousePos = Raylib.GetMousePosition();
            int[] rowYCoords = [255, 330, 405];

            DrawSurveySliderRow(190, rowYCoords[^1], "[Key 1] Aggression (Slaughter vs Avoidance)", _surveyAggro, Color.Red, mousePos);
            DrawSurveySliderRow(190, rowYCoords[^2], "[Key 2] Tactical Stealth & Cover Usage", _surveyStealth, Color.SkyBlue, mousePos);
            DrawSurveySliderRow(190, rowYCoords[^3], "[Key 3] Labyrinth Thoroughness & Loot", _surveyExploration, Color.Lime, mousePos);

            Rectangle submitBtn = new(190, 495, 480, 60);
            bool submitHover = Raylib.CheckCollisionPointRec(mousePos, submitBtn);
            Raylib.DrawRectangleRec(submitBtn, submitHover ? new(75, 65, 25, 255) : new(45, 40, 15, 255));
            Raylib.DrawRectangleLinesEx(submitBtn, submitHover ? 3f : 2f, submitHover ? Color.White : Color.Yellow);
            Raylib.DrawText("Press [ENTER] to Save & Conclude", 225, 515, 24, Color.Yellow);
        }

        private static void DrawSurveySliderRow(int x, int y, string lbl, int rating, Color col, Vector2 mousePos)
        {
            Raylib.DrawText(lbl, x, y, 22, Color.RayWhite);
            for (int i = 1; i <= 5; i++)
            {
                Rectangle box = new(x + 560 + (i - 1) * 55, y - 4, 42, 32);
                bool hovered = Raylib.CheckCollisionPointRec(mousePos, box);

                if (i <= rating) Raylib.DrawRectangleRec(box, col);
                else if (hovered) Raylib.DrawRectangleRec(box, new(60, 55, 80, 255));

                Raylib.DrawRectangleLinesEx(box, hovered ? 2.5f : 2f, hovered ? Color.Yellow : Color.White);
                Raylib.DrawText(i.ToString(), (int)box.X + 15, (int)box.Y + 6, 20, i <= rating ? Color.Black : Color.White);
            }
        }

        private static void DrawCrosshair()
        {
            Vector2 mPos = Raylib.GetMousePosition();
            Raylib.DrawCircleLines((int)mPos.X, (int)mPos.Y, 9f, Color.Lime);
            Raylib.DrawLineEx(mPos - new Vector2(14, 0), mPos - new Vector2(4, 0), 2f, Color.Lime);
            Raylib.DrawLineEx(mPos + new Vector2(4, 0), mPos + new Vector2(14, 0), 2f, Color.Lime);
            Raylib.DrawLineEx(mPos - new Vector2(0, 14), mPos - new Vector2(0, 4), 2f, Color.Lime);
            Raylib.DrawLineEx(mPos + new Vector2(0, 4), mPos + new Vector2(0, 14), 2f, Color.Lime);
        }

        public void Dispose() => PlayerTelemetry.ExportSessionCsv();
    }
}
