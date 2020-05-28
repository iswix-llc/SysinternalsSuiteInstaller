using System;
using System.IO;
using System.Net;
using System.Security.Policy;
using System.Windows.Forms;
using Ionic.Zip;
using Microsoft.Deployment.WindowsInstaller;

namespace Actions
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult ValidateDotNet(Session session)
        {
            session["DotNetValid"] = "1";
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CostDownloadAndExtractZip(Session session)
        {
            CustomActionData customActionData = new CustomActionData();
            customActionData.Add("INSTALLLOCATION", session["INSTALLLOCATION"]);
            customActionData.Add("URL", session["URL"]);
            customActionData.Add("UILEVEL", session["UILevel"]);
            session.DoAction("DownloadAndExtractZip", customActionData);
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult DownloadAndExtractZip(Session session)
        {
            ActionResult actionResult = ActionResult.Success;
            try
            {
                string url = session.CustomActionData["URL"];
                string installLocation = session.CustomActionData["INSTALLLOCATION"];

                session.Log($"Downloading {url}");
                StatusMessage(session, $"Downloading {url}");
                WebClient webClient = new WebClient();
                byte[] bytes = webClient.DownloadData(url);
                session.Log($"Downloaded {bytes.Length} bytes");

                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    using (ZipFile zipFile = ZipFile.Read(stream))
                    {
                        session.Log($"Extracting ZIP to {installLocation}");
                        StatusMessage(session, $"Extracting ZIP to {installLocation}");
                        zipFile.ExtractAll(installLocation, ExtractExistingFileAction.OverwriteSilently);
                    }
                }
            }
            catch(Exception ex)
            {
                string message = "An error occurred downloading and extracting SysInternals Suite. Please check the URL provided and try again. You may need to self host the SysInternals.zip archive if your firewall/proxy settings don't allow for download. Please check the installer log for more details.";
                session.Log(message);
                session.Log(ex.ToString());
                if(session.CustomActionData["UILEVEL"]=="5")
                {
                    MessageBox.Show(message, "Sysinternals Suite Setup");
                    actionResult = ActionResult.Failure;
                }
            }
            return actionResult;
        }


        [CustomAction]
        public static ActionResult CostUninstallFiles(Session session)
        {
            CustomActionData customActionData = new CustomActionData();
            customActionData.Add("INSTALLLOCATION", session["INSTALLLOCATION"]);
            session.DoAction("UninstallFiles", customActionData);
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult UninstallFiles(Session session)
        {
            string installLocation = session.CustomActionData["INSTALLLOCATION"];
            session.Log($"Deleting directory {installLocation}");
            try
            {
                Directory.Delete(installLocation, true);
            }
            catch(Exception ex)
            {
                session.Log($"Unexpected error: {ex.Message} ");
                session.Log("Continuing");
            }
            return ActionResult.Success;
        }


        [CustomAction]
        public static ActionResult ValidateURL(Session session)
        {
            bool valid = false;
            try
            {
                Uri uri = new Uri(session["URL"]);
                if(
                    (uri.OriginalString.ToLower().StartsWith("http://") || uri.OriginalString.ToLower().StartsWith("https://")) &&
                    uri.OriginalString.ToLower().EndsWith("sysinternalssuite.zip")
                  )
                {
                    valid = true;
                }
            }
            catch(Exception ex)
            {
                session.Log($"Unexpected Error: {ex.Message}");
            }

            if(valid)
            {
                session["UrlValid"] = "1";
            }
            else
            {
                MessageBox.Show("Download URL must be a valid HTTP/HTTPS URL ending in SysinternalsSuite.zip", "Sysinternals Suite Setup");
            }

            return ActionResult.Success;
        }

        internal static void StatusMessage(Session session, string status)
        {
            Record record = new Record(3);
            record[1] = "callAddProgressInfo";
            record[2] = status;
            record[3] = "Incrementing tick [1] of [2]";

            session.Message(InstallMessage.ActionStart, record);
            Application.DoEvents();
        }

    }
}
