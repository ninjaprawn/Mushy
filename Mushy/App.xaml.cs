using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;

namespace Mushy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    
        /*
        [STAThread]
        public static void Main()
        {
            if (IsElevated())
            {
                Console.WriteLine("Creating necessary users");
                if (!MS2Utils.UserExists("mushyuser1"))
                {
                    MS2Utils.CreateLocalWindowsAccount("mushyuser1", "mushypass");
                }
                if (!MS2Utils.UserExists("mushyuser2"))
                {
                    MS2Utils.CreateLocalWindowsAccount("mushyuser2", "mushypass");
                }
                if (!MS2Utils.UserExists("mushyuser3"))
                {
                    MS2Utils.CreateLocalWindowsAccount("mushyuser3", "mushypass");
                }
            } 
            else
            {
                App application = new App();
                application.InitializeComponent();
                application.Run();
            }
        }*/
    }
}
