using System.ComponentModel;
using System.Configuration.Install;

namespace ClamAVService
{
    [RunInstaller(true)]
    public partial class ClamAVInstaller : Installer
    {
        public ClamAVInstaller()
        {
            InitializeComponent();
        }
    }
}