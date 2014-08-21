using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace Bookpouch
{
    /// <summary>
    /// Interaction logic for AboutBox.xaml
    /// </summary>
    public partial class AboutBox
    {
        public AboutBox()
        {
            InitializeComponent();

            Version.Text = "Version: " + Application.ProductVersion;
        }
    }
}
