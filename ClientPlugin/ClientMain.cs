using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
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
        private const float Sensitivity = 0.25f;
        private const float CrosshairSize = 2f;
        private MyStringId CrosshairTexture = MyStringId.GetOrCompute("RoundedSquare");
        private const float Deg2Rad = (float)Math.PI / 180f;
        private const double CastRange = 10000;
        private float AdjSensitivity = Sensitivity;
        private Vector3D _aimDir = Vector3D.Zero;
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
        
                if (MyAPIGateway.Session.GameplayFrameCounter % 10 == 0)
                {
                    AdjSensitivity = Sensitivity * MyAPIGateway.Input.GetMouseSensitivity();
                }
        
                // update aimpoint
                if (MyAPIGateway.Input.IsKeyPress(MyKeys.C))
                {
                    _aimDir = Vector3D.Zero;
                }
                if (!(MyAPIGateway.Input.IsKeyPress(MyKeys.Alt) || MyAPIGateway.Gui.IsCursorVisible || MyAPIGateway.Gui.ChatEntryVisible))
                {
                    _aimDir = GetLookDir(_aimDir);
                }

                if (_aimDir == Vector3D.Zero)
                {
                    _relativeAimpoint = Vector3D.Zero;
                }
                else
                {
                    _relativeAimpoint = GetLookPos(grid, _aimDir) - grid.GetPosition();
                }
        
                string cData = _relativeAimpoint.ToString();
                foreach (var pb in grid.GetFatBlocks<IMyProgrammableBlock>())
                {
                    pb.Run(cData);
                }
        
                // draw indicator
                if (_relativeAimpoint != Vector3D.Zero)
                {
                    //Vector3D drawPoint = _relativeAimpoint + grid.GetPosition();
                    const float camDist = 0.01f;
                    Vector3D drawPoint = _aimDir * camDist + MyAPIGateway.Session.Camera.WorldMatrix.Translation;
                    float depthScale = camDist; // move to 0.01m in front of camera
                    depthScale *= (float)(2 * Math.Tan(Deg2Rad * MyAPIGateway.Session.Camera.FieldOfViewAngle / 2.0)) * camDist; // scale by FoV
        
                    MatrixD camMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
                    MyTransparentGeometry.AddBillboardOriented( // TODO cache billboard
                        CrosshairTexture,
                        Color.White * 0.75f,
                        drawPoint,
                        camMatrix.Left,
                        camMatrix.Up,
                        CrosshairSize * depthScale,
                        MyBillboard.BlendTypeEnum.LDR
                        );
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.Error(ex.ToString());
            }
        }
        
        private Vector3D GetLookDir(Vector3D prev)
        {
            if (prev == Vector3D.Zero)
                return MyAPIGateway.Session.Camera.WorldMatrix.Forward;
            MatrixD camMatrix = MyAPIGateway.Session.Camera.WorldMatrix.GetOrientation();
        
            Vector2 rot = MyAPIGateway.Input.GetRotation() * AdjSensitivity * (MyAPIGateway.Session.Camera.FieldOfViewAngle/90f) / 60f;
            MatrixD rotMat = MatrixD.CreateFromYawPitchRoll(-rot.Y, -rot.X, 0) * camMatrix;
            MatrixD invCamMatrix = MatrixD.Invert(camMatrix);
            return Vector3D.Rotate(Vector3D.Rotate(prev, invCamMatrix), rotMat);
        }
        
        private Vector3D GetLookPos(IMyCubeGrid rootToIgnore, Vector3D aimDirection)
        {
            if (aimDirection == Vector3D.Zero)
                return Vector3D.Zero;
        
            var camMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            var hits = new List<IHitInfo>();
            MyAPIGateway.Physics.CastRay(camMatrix.Translation + aimDirection, camMatrix.Translation + aimDirection * CastRange, hits);
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
        
            return camMatrix.Translation + aimDirection * CastRange;
        }
        
        public static float ToAlwaysOnTop(ref Vector3D position)
        {
            var camMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            position = camMatrix.Translation + (position - camMatrix.Translation) * 0.01f;
        
            return 0.01f;
        }
    }
}