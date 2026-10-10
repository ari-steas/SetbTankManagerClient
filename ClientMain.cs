using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SetbTankManager.Client;

namespace AriUtils.Components
{
    public partial class ClientMain
    {
        private TurretManager turretManager = TurretManager.CreateWithOwner<ClientMain>();
        private AmmoDisplay ammoDisplay = AmmoDisplay.CreateWithOwner<ClientMain>();
    }
}
