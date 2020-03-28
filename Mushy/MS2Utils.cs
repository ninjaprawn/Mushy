using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Mushy
{
    class MS2Utils
    {
        private int type1 = 0;
        private int type2 = 0;
        private int type3 = 0;
        private int type4 = 0;

        public static bool UserExists(string username)
        {
            using (PrincipalContext pc = new PrincipalContext(ContextType.Machine))
            {
                UserPrincipal up = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, username);

                bool UserExists = (up != null);
                return UserExists;
            }
        }

        public static bool CreateLocalWindowsAccount(string username, string password, string description = "", bool canChangePwd = false, bool pwdExpires = true)
        {
            try
            {
                PrincipalContext context = new PrincipalContext(ContextType.Machine);
                UserPrincipal user = new UserPrincipal(context);
                user.SetPassword(password);
                user.DisplayName = username;
                user.Name = username;
                user.Description = description;
                user.UserCannotChangePassword = canChangePwd;
                user.PasswordNeverExpires = pwdExpires;
                user.Save();
                //now add user to "Users" group so it displays in Control Panel
                GroupPrincipal group = GroupPrincipal.FindByIdentity(context, "Users");
                group.Members.Add(user);
                group.Save();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

        }

        public string GetMaplestory2Path(bool x64 = true)
        {
            try
            {
                string installDBPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NexonLauncher", "installed-apps.db");
                string installDBString = File.ReadAllText(installDBPath);

                dynamic installDB = new JavaScriptSerializer().Deserialize<dynamic>(installDBString);
                dynamic installedApps = installDB["installedApps"];
                dynamic ms2App = installedApps["40000"];
                string installPath = ms2App["installPath"];

                if (x64)
                {
                    return Path.Combine(installPath, "x64", "MapleStory2.exe");
                }
                else
                {
                    return Path.Combine(installPath, "MapleStory2.exe");
                }
            }
            catch (Exception e)
            {

            }
            return "";
        }

        public int launchMaple(string ticket)
        {
            int pid = 0;

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = GetMaplestory2Path();
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.Arguments = "30000 --nxapp=nxsteam --ticket=" + ticket;
            info.UseShellExecute = false;

            /*
             * Vuln: Ability to run multiple clients of Maplestory 2 on the same machine
             * Running MS2 clients under different users will allow them to co-exist
             */

            if (type1 == 0)
            {
                info.UserName = "mushyuser1";
                info.PasswordInClearText = "mushypass";
            }
            else if (type2 == 0)
            {
                info.UserName = "mushyuser2";
                info.PasswordInClearText = "mushypass";
            }
            else if (type3 == 0)
            {
                info.UserName = "mushyuser3";
                info.PasswordInClearText = "mushypass";
            }
            else if (type4 == 0)
            {
                info.UseShellExecute = true;
            }

            Process a = Process.Start(info);
            pid = a.Id;

            if (type1 == 0)
            {
                type1 = pid;
            }
            else if (type2 == 0)
            {
                type2 = pid;
            }
            else if (type3 == 0)
            {
                type3 = pid;
            }
            else if (type4 == 0)
            {
                type4 = pid;
            }

            return pid;
        }

        public void removePID(int pid)
        {
            if (type1 == pid)
            {
                type1 = 0;
            }
            else if (type2 == pid)
            {
                type2 = 0;
            }
            else if (type3 == pid)
            {
                type3 = 0;
            }
            else if (type4 == pid)
            {
                type4 = 0;
            }
        }

        private HttpWebResponse PostRequest(String url, CookieContainer cc, object obj)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.CookieContainer = cc;
            req.ContentType = "application/json";
            req.Method = "POST";

            using (StreamWriter streamWriter = new StreamWriter(req.GetRequestStream()))
            {
                string json = new JavaScriptSerializer().Serialize(obj);
                streamWriter.Write(json);
            }

            return (HttpWebResponse)req.GetResponse();
        }

        private string GetResponse(HttpWebResponse resp)
        {
            string r = "";
            using (var streamReader = new StreamReader(resp.GetResponseStream()))
            {
                r = streamReader.ReadToEnd();
            }
            return r;
        }

        private dynamic GetJSONResponse(HttpWebResponse resp)
        {
            string r = GetResponse(resp);
            Console.WriteLine(r);
            return new JavaScriptSerializer().Deserialize<dynamic>(r);
        }

        public string getTicketForGoogleToken(string token)
        {
            if (token == "")
            {
                throw new Exception("Invalid Google token given");
            }

            CookieContainer cookies = new CookieContainer();

            HttpWebResponse tpaSessionResponse = PostRequest("https://www.nexon.com/account-webapi/login/tpa/tpa_session_cookie", cookies, new
            {
                tpa_type = "google",
                code = token
            });


            string tpaSession = "";
            foreach (Cookie c in tpaSessionResponse.Cookies)
            {
                if (c.Name == "TpaSession")
                {
                    tpaSession = c.Value;
                }
            }

            if (tpaSession == "")
            {
                throw new Exception("tpaSession could not be created");
            }
            //Console.WriteLine(tpaSession);

            HttpWebResponse idTokenResponse = PostRequest("https://www.nexon.com/account-webapi/login/tpa/launcher", cookies, new
            {
                client_id = "7853644408",
                device_id = LoginManager.GetDeviceID()
            });

            string idToken = "";
            try
            {
                idToken = GetJSONResponse(idTokenResponse)["id_token"];
            }
            catch
            {
                throw new Exception("Failed to retrieve Nexon ID Token");
            }

            if (idToken == "")
            {
                throw new Exception("Failed to retrieve Nexon ID Token");
            }

            //Console.WriteLine(idToken);

            return getTicketForNexonToken(idToken);
        }


        public string getTicketForNexonToken(string idToken)
        {
            if (idToken == "")
            {
                throw new Exception("Invalid Nexon token given");
            }

            CookieContainer cookies = new CookieContainer();

            HttpWebResponse productIDResponse = PostRequest("https://api.nexon.io/game-auth/v2/check-playable", cookies, new
            {
                id_token = idToken,
                product_id = "560380",
                device_id = LoginManager.GetDeviceID(),
                is_steam = "true"
            });

            string productID = "";
            try
            {
                productID = GetJSONResponse(productIDResponse)["product_id"];
            }
            catch
            {
                throw new Exception("Failed to retrieve the Product ID");
            }

            if (productID == "")
            {
                throw new Exception("Failed to retrieve the Product ID");
            }

            //Console.WriteLine(productID);

            HttpWebResponse ticketResponse = PostRequest("https://api.nexon.io/game-auth/v2/ticket", cookies, new
            {
                id_token = idToken,
                product_id = productID,
                device_id = LoginManager.GetDeviceID(),
                is_steam = "true"
            });

            string ticket;
            try
            {
                ticket = GetJSONResponse(ticketResponse)["ticket"];
            }
            catch
            {
                throw new Exception("Failed to retrieve the ticket");
            }

            if (ticket == null || ticket == "")
            {
                throw new Exception("Failed to retrieve the ticket");
            }

            return ticket;
        }
    }
}
