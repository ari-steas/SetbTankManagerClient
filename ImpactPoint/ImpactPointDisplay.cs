using System;
using AriUtils;
using CoreSystems.Api;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.Models;
using VRageMath;
using static CoreSystems.Api.WcApiManager;

namespace SetbTankManager.Client.ImpactPoint
{
    internal class ImpactPointDisplay : SingletonBase<ImpactPointDisplay>
    {
        private Dictionary<string, ProjectileInfo> _projectileData = new Dictionary<string, ProjectileInfo>();
        private IMyCubeGrid _playerGrid = null;
        private bool _awaitSetGrid = false;
        private List<MyCubeBlock> _weapons = new List<MyCubeBlock>();
        private List<IMyCubeGrid> _grids = new List<IMyCubeGrid>();

        private List<MyTuple<Vector3D, Vector3D, Vector3D, Vector3D, MatrixD, MatrixD>> _muzzleInfoCache =
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

            MyPlanet gridPlanet = null;
            foreach (var planet in GlobalData.Planets)
            {
                float gravLimit = planet.Components.Get<MyGravityProviderComponent>().GravityLimit;
                if (gravLimit * gravLimit < Vector3D.DistanceSquared(planet.PositionComp.WorldMatrixRef.Translation, _playerGrid.PositionComp.WorldMatrixRef.Translation))
                    continue;
                gridPlanet = planet;
                break;
            }

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
                CalcTrajectory(_muzzleInfoCache[0].Item1, _muzzleInfoCache[0].Item3, weapon.CubeGrid.Physics.GetVelocityAtPoint(_muzzleInfoCache[0].Item1), pInfo, gridPlanet, weapon.CubeGrid.GridSizeEnum == MyCubeSize.Small);
                _muzzleInfoCache.Clear();
            }
        }

        private void CalcTrajectory(Vector3D muzzlePos, Vector3D muzzleDir, Vector3D gridVelocity, ProjectileInfo pInfo, MyPlanet planet, bool isSmallGrid)
        {
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
                    DebugDraw.AddLine(pos, newPos, new Color(2000f, 0f, 0f, 2000f), 0);
                    LineD interLine = new LineD(lastCastPos, newPos);
                    MyIntersectionResultLineTriangleEx? tri;
                    if (planet.GetIntersectionWithLine(ref interLine, out tri)) // voxel layer
                    {
                        DebugDraw.AddGps("Impact Point", tri.Value.IntersectionPointInWorldSpace, 1/60f);

                        MatrixD impactMatrix = MatrixD.CreateWorld(tri.Value.IntersectionPointInWorldSpace, Vector3.CalculatePerpendicularVector(tri.Value.NormalInWorldSpace), tri.Value.NormalInWorldSpace);
                        Vector4 color = new Color(2000f, 0f, 0f, 2000f);
                        float maxDeviation = pInfo.HardPoint.DeviateShotAngle;
                        if (isSmallGrid && pInfo.HardPoint.DeviateShotAngleSGModifier > 0)
                            maxDeviation *= pInfo.HardPoint.DeviateShotAngleSGModifier;
                        maxDeviation = (float) Math.Tan(MathHelper.ToRadians(maxDeviation)) * dist;
                        MyAPIGateway.Utilities.ShowNotification($"{pInfo.HardPoint.DeviateShotAngle * (isSmallGrid ? pInfo.HardPoint.DeviateShotAngleSGModifier : 1):N}deg -> {maxDeviation:N}m deviation @ {dist:N}m travelled", 1000/60);
                        MySimpleObjectDraw.DrawTransparentCylinder(ref impactMatrix, maxDeviation, maxDeviation/2, 0, ref color, true, 360 / 15, 1f);
                        break;
                    }
                    lastCastPos = newPos;
                }
                pos = newPos;
       
                time++;
            }
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
