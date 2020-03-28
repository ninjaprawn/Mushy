using Microsoft.Win32;
using mshtml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Mushy
{
    [ComVisible(true)]
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    public class LoginManager
    {
        [DllImport("urlmon.dll", CharSet = CharSet.Ansi)]
        private static extern int UrlMkSetSessionOption(int dwOption, string pBuffer, int dwBufferLength, int dwReserved);
        const int URLMON_OPTION_USERAGENT = 0x10000001;

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int lpdwBufferLength);
        private const int INTERNET_OPTION_END_BROWSER_SESSION = 42;

        public MainWindow window;
        private WebBrowser browser;
        private Window googleForm;
        private string token;
        private Random rand;

        public LoginManager(MainWindow window)
        {
            this.window = window;
            this.rand = new Random();

            // Force Web Browser to be Chrome
            List<string> userAgent = new List<string>();
            string ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.87 Safari/537.36 NexonLauncher";
            UrlMkSetSessionOption(URLMON_OPTION_USERAGENT, ua, ua.Length, 0);
        }

        public static string GetWMICUUID()
        {
            try
            {
                string ComputerName = "localhost";
                ManagementScope Scope;
                Scope = new ManagementScope(String.Format("\\\\{0}\\root\\CIMV2", ComputerName), null);
                Scope.Connect();
                ObjectQuery Query = new ObjectQuery("SELECT UUID FROM Win32_ComputerSystemProduct");
                ManagementObjectSearcher Searcher = new ManagementObjectSearcher(Scope, Query);

                foreach (ManagementObject WmiObject in Searcher.Get())
                {
                    return ((string)WmiObject["UUID"]).Replace("\n", "").Replace("\r", "");                
                }
                return "";
            }
            catch (Exception e)
            {
                return "";
            }
        }

        public static string GetMachineGUID()
        {
            RegistryKey localMachineX64View = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            RegistryKey sqlsrvKey = localMachineX64View.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            return (string)sqlsrvKey.GetValue("MachineGuid");
        }

        public static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static string GetDeviceID()
        {
            Console.WriteLine(GetWMICUUID());
            Console.WriteLine(GetMachineGUID());
            return ComputeSha256Hash(GetWMICUUID() + GetMachineGUID());
        }

        // Used to prevent script errors on the web browser
        public void HideScriptErrors(WebBrowser wb, bool hide)
        {
            var fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;
            var objComWebBrowser = fiComWebBrowser.GetValue(wb);
            if (objComWebBrowser == null)
            {
                wb.Loaded += (o, s) => HideScriptErrors(wb, hide); //In case we are to early
                return;
            }
            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { hide });
        }

        public string getGoogleIDToken()
        {
            token = "";

            googleForm = new Window();
            googleForm.Width = 520;
            googleForm.Height = 660;
            googleForm.ResizeMode = ResizeMode.CanMinimize;
            googleForm.Title = "Google Sign-in";

            browser = new WebBrowser();
            browser.ObjectForScripting = this;
            browser.AllowDrop = false;
            HideScriptErrors(browser, true);
            browser.Navigating += browser_Navigating;
            browser.LoadCompleted += browser_LoadCompleted;
            browser.Navigate("https://www.nexon.com/account/en/login/");
            googleForm.Content = browser;

            googleForm.ShowDialog();
            
            return token;
        }
        public string getNexonIDToken()
        {
            token = "";

            googleForm = new Window();
            googleForm.Width = 520;
            googleForm.Height = 660;
            googleForm.ResizeMode = ResizeMode.CanMinimize;
            googleForm.Title = "Nexon Sign-in";

            browser = new WebBrowser();
            browser.ObjectForScripting = this;
            browser.AllowDrop = false;
            HideScriptErrors(browser, true);
            browser.Navigating += browser_Navigating;
            browser.LoadCompleted += browser_LoadCompleted;
            browser.Navigate("https://www.nexon.com/account/en/login/?r=" + rand.Next());
            googleForm.Content = browser;

            googleForm.ShowDialog();

            return token;
        }

        private void browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            WebBrowser browser = (WebBrowser)sender;
            if (e.Uri == null || e.Uri.ToString() == "about:blank")
            {
                Console.WriteLine("Navigating to custom HTML");
            }
            else if (e.Uri.AbsolutePath == "/account/en/login/")
            {
                HTMLDocument doc = (HTMLDocument)browser.Document;
                
                HTMLHeadElement head = doc.getElementsByTagName("head").Cast<HTMLHeadElement>().First();
                IHTMLScriptElement script = (IHTMLScriptElement)doc.createElement("script");

                // Nexon will ask to verify only if device_id is different.
                script.text = "document.cookie = 'AToken =; expires=Thu, 01 Jan 1970 00:00:01 GMT;';document.cookie = 'LSession =; expires=Thu, 01 Jan 1970 00:00:01 GMT;'; function myf() {gapi.auth2.getAuthInstance().signIn({scope: 'email', prompt: 'select_account', ux_mode: 'redirect', redirect_uri: 'https://www.nexon.com/account/login/callback/google'});}; window.launcherData = Object.assign({client_id: '7853644408',device_id: '" + GetDeviceID() + "',scope: 'us.launcher.all'}); window.launcherLogin = function(data) {window.external.RetrievedNexonToken(data['id_token'])}; setTimeout(function() {window.external.StartGoogleSignin();}, 500); ";
                head.appendChild((IHTMLDOMNode)script);
            }
        }

        public void CWL(string s)
        {
            Console.WriteLine(s);
        }
        
        public void StartGoogleSignin()
        {
            if (googleForm.Title == "Google Sign-in")
            {
                browser.InvokeScript("myf");
            }
        }

        public void RetrievedNexonToken(string token)
        {
            this.token = token;
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_END_BROWSER_SESSION, IntPtr.Zero, 0);

            browser.Dispose();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            googleForm.Close();
        }

        public void browser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            Console.WriteLine(e.Uri.OriginalString);
            if (e.Uri != null && e.Uri.AbsolutePath == "/account/login/callback/google")
            {
                char[] sep = { '&' };
                String[] par = e.Uri.OriginalString.Split(sep);
                token = "";
                foreach (String p in par)
                {
                    if (p.Contains("id_token"))
                    {
                        char[] sep2 = { '=' };
                        token = p.Split(sep2)[1];
                        break;
                    }
                }

                e.Cancel = true;
                browser.Dispose();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                googleForm.Close();
            }
        }

    }
}
