using AriUtils;
using CoreSystems.Api;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.ModAPI;
using VRage.Game.Models;
using VRage.Utils;
using VRageMath;
using static CoreSystems.Api.WcApiManager;

namespace SetbTankManager.Client.ImpactPoint
{
    internal class ImpactPointDisplay : SingletonBase<ImpactPointDisplay>
    {
        private const string Font = "Monospaced";

        private readonly Dictionary<string, ProjectileInfo> _projectileData = new Dictionary<string, ProjectileInfo>();
        private IMyCubeGrid _playerGrid = null;
        private bool _awaitSetGrid = false;
        private readonly List<MyCubeBlock> _weapons = new List<MyCubeBlock>();
        private readonly List<IMyCubeGrid> _grids = new List<IMyCubeGrid>();
        private MyPlanet _gridPlanet = null;

        private readonly List<MyTuple<Vector3D, Vector3D, Vector3D, Vector3D, MatrixD, MatrixD>> _muzzleInfoCache =
            new List<MyTuple<Vector3D, Vector3D, Vector3D, Vector3D, MatrixD, MatrixD>>();

        public override void Init(object[] args)
        {
            TurretManager.TurretManager.I.OnPlayerChangeGrid += SetGrid;
            WcApiManager.OnWcApiReady += () =>
            {
                foreach (var wepDef in WcApiManager.WcApi.WeaponDefinitions)
                {
                    if (wepDef?.Ammos == null)
                        continue;
                    foreach (var ammo in wepDef.Ammos)
                    {
                        if (_projectileData.ContainsKey(ammo.AmmoRound))
                            continue;
                        _projectileData.Add(ammo.AmmoRound, new ProjectileInfo(ammo, wepDef.HardPoint));
                    }
                }
            };
        }

        public override void Update()
        {
            if (_awaitSetGrid)
            {
                if (WcApi == null || !WcApi.IsReady)
                    return;

                _weapons.Clear();
                if (_playerGrid == null)
                    return;

                _playerGrid.GetGridGroup(GridLinkTypeEnum.Logical).GetGrids(_grids);
                foreach (var iGrid in _grids)
                {
                    MyCubeGrid grid = (MyCubeGrid)iGrid;
                    foreach (var block in grid.GetFatBlocks())
                    {
                        if (WcApi.HasCoreWeapon(block))
                        {
                            _weapons.Add(block);
                        }
                    }
                }

                _grids.Clear();
                _awaitSetGrid = false;
            }

            if (_playerGrid == null)
                return;

            if (MyAPIGateway.Session.GameplayFrameCounter % 31 == 0)
            {
                _gridPlanet = MiscUtils.GetEntityPlanet(_playerGrid);
            }

            if (_gridPlanet == null || !(MyAPIGateway.Session.CameraController.Entity is MyCameraBlock))
                return;

            foreach (var weapon in _weapons)
            {
                if (weapon.Closed || !weapon.IsWorking || !((IMyTerminalBlock)weapon).ShowOnHUD)
                    continue;

                string ammoId = WcApi.GetActiveAmmo(weapon, 0);
                ProjectileInfo pInfo;
                if (!_projectileData.TryGetValue(ammoId, out pInfo))
                {
                    MyAPIGateway.Utilities.ShowNotification($"Missing ammo \"{ammoId}\" on weapon [{weapon.DisplayNameText}]", 1000/60);
                    _muzzleInfoCache.Clear();
                    continue;
                }
                WcApi.GetMuzzleInfo(weapon, 0, _muzzleInfoCache);
                Vector3D impactPos;
                double impactTime;
                bool didImpact = CalcTrajectory(
                    _muzzleInfoCache[0].Item1,
                    _muzzleInfoCache[0].Item3,
                    weapon.CubeGrid.Physics.GetVelocityAtPoint(_muzzleInfoCache[0].Item1),
                    pInfo, _gridPlanet,
                    out impactPos,
                    out impactTime);
                _muzzleInfoCache.Clear();

                double dist = double.PositiveInfinity;
                if (didImpact)
                {
                    dist = Vector3D.Distance(impactPos, MyAPIGateway.Session.Camera.Position);
                }

                string text = $"{dist:N0}m @ {impactTime:N1}s";
                Vector2 textSize = MyScreenSpaceText.Measure("Monospace", text, 0.5f);
                MyScreenSpaceText.Draw(
                    "Monospace",
                    text,
                    new Vector2(textSize.X/2 + 0.5f + 2*MyScreenSpaceText.GetLineHeight("Monospace", 0.5f), 0.5f),
                    0.5f, Color.White, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            }
        }

        public override void Draw()
        {
            // camera gravity stabilizer
            if (_gridPlanet == null || _playerGrid == null)
                return;
            MyCameraBlock cam = MyAPIGateway.Session.CameraController.Entity as MyCameraBlock;
            if (cam == null)
                return;

            Vector3 gravVec = -Vector3.Transform(_gridPlanet.PositionComp.WorldMatrixRef.Translation - cam.PositionComp.WorldMatrixRef.Translation, MatrixD.Invert(cam.CubeGrid.PositionComp.GetOrientation()));
            Matrix m = Matrix.CreateWorld(cam.PositionComp.LocalMatrixRef.Translation, cam.PositionComp.LocalMatrixRef.Forward, gravVec.Normalized());
            cam.PositionComp.SetLocalMatrix(ref m);
        }

        private bool CalcTrajectory(Vector3D muzzlePos, Vector3D muzzleDir, Vector3D gridVelocity, ProjectileInfo pInfo, MyPlanet planet, out Vector3D impactPos, out double impactTime)
        {
            impactPos = Vector3D.Zero;
            impactTime = double.PositiveInfinity;

            Vector3D planetPos = planet.WorldMatrix.Translation;
            float gravityConst = pInfo.Trajectory.GravityMultiplier * 9.81f * planet.GetInitArguments.SurfaceGravity / 60;

            //DebugDraw.AddLine(muzzlePos, muzzlePos + muzzleDir * 10, Color.Red, 0);
            //DebugDraw.AddLine(muzzlePos, muzzlePos + gridVelocity * 10, Color.Green, 0);
            float dist = 0;
            int time = 0;
            Vector3D pos = muzzlePos;
            Vector3 vel = gridVelocity + muzzleDir * pInfo.Trajectory.DesiredSpeed;
            Vector3D lastCastPos = muzzlePos;
            while (dist < pInfo.Trajectory.MaxTrajectory && time < pInfo.Trajectory.MaxLifeTime)
            {
                Vector3D newPos = pos + vel / 60;
                dist += vel.Length()/60;
                vel += gravityConst * Vector3D.Normalize(planetPos - pos); // applying acceleration second seems to be more accurate
                if (time % 4 == 0)
                {
                    #if DEBUG
                    DebugDraw.AddLine(pos, newPos, new Color(2000f, 0f, 0f, 2000f), 0);
                    #endif
                    LineD interLine = new LineD(lastCastPos, newPos);
                    MyIntersectionResultLineTriangleEx? tri;
                    if (planet.GetIntersectionWithLine(ref interLine, out tri)) // voxel layer
                    {
                        #if DEBUG
                        DebugDraw.AddGps("Impact Point", tri.Value.IntersectionPointInWorldSpace, 1/60f);

                        MatrixD impactMatrix = MatrixD.CreateWorld(tri.Value.IntersectionPointInWorldSpace, Vector3.CalculatePerpendicularVector(tri.Value.NormalInWorldSpace), tri.Value.NormalInWorldSpace);
                        Vector4 color = new Color(2000f, 0f, 0f, 2000f);
                        float maxDeviation = pInfo.HardPoint.DeviateShotAngle;
                        if (isSmallGrid && pInfo.HardPoint.DeviateShotAngleSGModifier > 0)
                            maxDeviation *= pInfo.HardPoint.DeviateShotAngleSGModifier;
                        maxDeviation = (float) Math.Tan(MathHelper.ToRadians(maxDeviation)) * dist;
                        MyAPIGateway.Utilities.ShowNotification($"{pInfo.HardPoint.DeviateShotAngle * (isSmallGrid ? pInfo.HardPoint.DeviateShotAngleSGModifier : 1):N}deg -> {maxDeviation:N}m deviation @ {dist:N}m travelled", 1000/60);
                        MySimpleObjectDraw.DrawTransparentCylinder(ref impactMatrix, maxDeviation, maxDeviation/2, 0, ref color, true, 360 / 15, 1f);
                        #endif

                        if (tri == null)
                            continue;
                        impactPos = tri.Value.IntersectionPointInWorldSpace;
                        impactTime = time/60d;
                        return true;
                    }
                    lastCastPos = newPos;
                }
                pos = newPos;
       
                time++;
            }

            return false;
        }


        private void SetGrid(IMyCubeGrid playerGrid)
        {
            _playerGrid = playerGrid;
            _awaitSetGrid = true;
        }

        private struct ProjectileInfo
        {
            public WcApiDef.WeaponDefinition.AmmoDef.TrajectoryDef Trajectory;
            public WcApiDef.WeaponDefinition.HardPointDef HardPoint;

            public ProjectileInfo(WcApiDef.WeaponDefinition.AmmoDef ammo, WcApiDef.WeaponDefinition.HardPointDef hardPoint)
            {
                Trajectory = ammo.Trajectory;
                HardPoint = hardPoint;
            }
        }
    }
}
