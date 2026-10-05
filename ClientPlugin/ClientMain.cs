//using AriUtils;

using EmptyKeys.UserInterface.Media;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace SETBTankManager.Client
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class ClientMain : MySessionComponentBase
    {
        private const double CastRange = 10000;
        private Vector3D _relativeAimpoint = Vector3D.Zero;

        public override void Draw()
        {
            try
            {
                if (MyAPIGateway.Session?.Player == null || MyAPIGateway.Input == null)
                    return;
                var cockpit = MyAPIGateway.Session.Player?.Controller?.ControlledEntity as IMyCockpit;
                if (cockpit == null)
                {
                    MyAPIGateway.Utilities.ShowNotification($"[SETBTM] No cockpit.", 1000 / 60);
                    return;
                }
                var grid = cockpit.CubeGrid;

                // update aimpoint
                if (MyAPIGateway.Input.IsKeyPress(MyKeys.RightButton))
                {
                    Vector3D aimPoint = GetLookPos(grid);
                    _relativeAimpoint = aimPoint - grid.GetPosition();

                    string cData = aimPoint.ToString();
                    foreach (var pb in grid.GetFatBlocks<IMyProgrammableBlock>())
                    {
                        pb.Run(cData);
                    }
                }

                // draw indicator
                if (_relativeAimpoint != Vector3D.Zero)
                {
                    Vector3D drawPoint = _relativeAimpoint + grid.GetPosition();
                    var depthScale = ToAlwaysOnTop(ref drawPoint);
                    MyTransparentGeometry.AddPointBillboard(MyStringId.GetOrCompute("WhiteDot"), Color.White, drawPoint, 0.35f * depthScale,
                        0,
                        blendType: MyBillboard.BlendTypeEnum.LDR);
                }
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage("SETBTest", ex.ToString());
            }
        }

        private Vector3D GetLookPos(IMyCubeGrid rootToIgnore)
        {
            var camMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            var hits = new List<IHitInfo>();
            MyAPIGateway.Physics.CastRay(camMatrix.Translation + camMatrix.Forward, camMatrix.Translation + camMatrix.Forward * CastRange, hits);
            foreach (var hit in hits)
            {
                var ent = hit.HitEntity;

                if (ent?.Physics == null || ent is IMyCharacter)
                    continue;

                if (ent is IMyCubeGrid)
                {
                    IMyCubeGrid g = (IMyCubeGrid) ent;
                    if (g.IsInSameLogicalGroupAs(rootToIgnore))
                        continue;
                }

                return hit.Position;
            }

            return camMatrix.Translation + camMatrix.Forward * CastRange;
        }

        public static float ToAlwaysOnTop(ref Vector3D position)
        {
            var camMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            position = camMatrix.Translation + (position - camMatrix.Translation) * 0.01f;

            return 0.01f;
        }
    }
}
