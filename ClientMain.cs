using SetbTankManager.Client.AmmoDisplay;
using SetbTankManager.Client.ImpactPoint;
using SetbTankManager.Client.TurretManager;

namespace AriUtils.Components
{
    public partial class ClientMain
    {
        private TurretManager turretManager = TurretManager.CreateWithOwner<ClientMain>();
        private AmmoDisplay ammoDisplay = AmmoDisplay.CreateWithOwner<ClientMain>();
        private ImpactPointDisplay impactDisplay = ImpactPointDisplay.CreateWithOwner<ClientMain>();
    }
}
