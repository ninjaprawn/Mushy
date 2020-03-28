using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mushy
{
    public class UserAccountPanel : StackPanel
    {
        public TextBlock statusLabel;
        public TextBlock processStatusLabel;
        private List<Viewbox> seperators;
        public Button loginNexonButton;
        public Button loginGoogleButton;
        public Button featureButton;
        public int pid;
        public UserAccountPanel()
        {
            pid = -1;

            seperators = new List<Viewbox>();

            statusLabel = new TextBlock
            {
                Text = "Not Logged In",
                TextAlignment = TextAlignment.Center,
                FontSize = 16
            };
            this.Children.Add(statusLabel);

            processStatusLabel = new TextBlock
            {
                Text = "Not Running",
                TextAlignment = TextAlignment.Center,
                FontSize = 14
            };
            this.Children.Add(processStatusLabel);

            Viewbox seperator1 = new Viewbox
            {
                Height = 20
            };
            this.Children.Add(seperator1);
            this.seperators.Add(seperator1);

            loginNexonButton = new Button
            {
                Content = "Login (Nexon)"
            };
            this.Children.Add(loginNexonButton);

            Viewbox seperator2 = new Viewbox
            {
                Height = 8
            };
            this.Children.Add(seperator2);
            this.seperators.Add(seperator2);

            loginGoogleButton = new Button
            {
                Content = "Login (Google)"
            };
            this.Children.Add(loginGoogleButton);

            Viewbox seperator3 = new Viewbox
            {
                Height = 8
            };
            this.Children.Add(seperator3);
            this.seperators.Add(seperator3);

            featureButton = new Button
            {
                Content = "Coming Soon",
                IsEnabled = false
            };
            this.Children.Add(featureButton);
        }

        public void setPID(int pid)
        {            
            this.pid = pid;
            Dispatcher.Invoke(new Action(() =>
            {
                if (pid == -1)
                {
                    processStatusLabel.Text = "Not Running";
                    statusLabel.Text = "Not Logged In";

                    loginGoogleButton.IsEnabled = true;
                    loginNexonButton.IsEnabled = true;
                }
                else
                {
                    processStatusLabel.Text = "Running - PID=" + pid;
                }
            }));
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public List<UserAccountPanel> accountPanels;
        private LoginManager lm;
        private MS2Utils ut;


        public bool IsElevated()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                bool isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
                return isElevated;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

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
                MessageBox.Show("Users have been created. Application will now exit.");
                Environment.Exit(0);
            }

            ut = new MS2Utils();
            lm = new LoginManager(this);
            accountPanels = new List<UserAccountPanel>();

            for (int i = 0; i < 4; i++)
            {
                UserAccountPanel current = new UserAccountPanel();
                current.Margin = new Thickness(10);

                if (i % 2 == 0)
                {
                    current.SetValue(Grid.RowProperty, (i == 2 ? 1 : 0));
                    current.SetValue(Grid.ColumnProperty, 0);
                }
                else
                {
                    current.SetValue(Grid.RowProperty, (i == 3 ? 1 : 0));
                    current.SetValue(Grid.ColumnProperty, 1);
                }

                current.loginNexonButton.Click += LoginButton_Click;
                current.loginGoogleButton.Click += LoginButton_Click;
                current.featureButton.Click += FeatureButton_Click;
                //current.featureButton.IsEnabled = true;

                accountPanels.Add(current);

                accountGrid.Children.Add(current);
            }

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        UserAccountPanel p = accountPanels[i];
                        if (p.pid != -1)
                        {
                            if (!Process.GetProcesses().Any(x => x.Id == p.pid))
                            {
                                p.setPID(-1);
                                ut.removePID(p.pid);
                            }
                        }
                    }
                    Thread.Sleep(500);
                };
            });

        }

        private void FeatureButton_Click(object sender, RoutedEventArgs e)
        {
            // :)
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            Button actualSender = (Button)sender;
            UserAccountPanel parent = (UserAccountPanel)actualSender.Parent;
            bool isNexonRequest = actualSender.Content.ToString().Contains("Nexon");

            parent.loginGoogleButton.IsEnabled = false;
            parent.loginNexonButton.IsEnabled = false;

            if (isNexonRequest)
            {
                parent.statusLabel.Text = "Logging in via Nexon...";

                string nexonToken = lm.getNexonIDToken();
                if (nexonToken == "")
                {
                    parent.statusLabel.Text = "Failed to get Nexon Token";
                    parent.loginGoogleButton.IsEnabled = true;
                    parent.loginNexonButton.IsEnabled = true;
                }
                else
                {
                    string ticket = "";
                    try
                    {
                        ticket = ut.getTicketForNexonToken(nexonToken);
                        parent.statusLabel.Text = "Obtained Ticket. Running...";
                    }
                    catch (Exception exc)
                    {
                        parent.statusLabel.Text = "Error: " + exc.Message;
                        parent.loginGoogleButton.IsEnabled = true;
                        parent.loginNexonButton.IsEnabled = true;
                    }

                    if (ticket != "")
                    {
                        //Console.WriteLine(ticket);
                        int pid = ut.launchMaple(ticket);
                        parent.setPID(pid);
                    }
                }
            }
            else
            {
                parent.statusLabel.Text = "Logging in via Google...";
                string googleToken = lm.getGoogleIDToken();

                if (googleToken == "")
                {
                    parent.statusLabel.Text = "Failed to get Google Token";
                    parent.loginGoogleButton.IsEnabled = true;
                    parent.loginNexonButton.IsEnabled = true;
                }
                else
                { 
                    string ticket = "";
                    try
                    {
                        ticket = ut.getTicketForGoogleToken(googleToken);
                        parent.statusLabel.Text = "Obtained Ticket. Running...";
                    }
                    catch (Exception exc)
                    {
                        parent.statusLabel.Text = "Error: " + exc.Message;
                        parent.loginGoogleButton.IsEnabled = true;
                        parent.loginNexonButton.IsEnabled = true;
                    }

                    if (ticket != "")
                    {
                        int pid = ut.launchMaple(ticket);
                        parent.setPID(pid);
                    }
                }
            }
        }
    }
}
