using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32; // To access Registry

using Elibre.Net.Debug;
using Elibre.Net.Remtoing;

namespace EFRvt
{
    public class RunEF
    {
        public static void RunEFramer(string fileName)
        {
            string eFramerPath = "";
            string registryKeyName = @"Software\Elibre\eFramer\2017\";
            RegistryKey eFramerKey = Registry.CurrentUser.OpenSubKey(registryKeyName);

            if (eFramerKey != null)
            {
                eFramerPath = eFramerKey.GetValue("ExecutablePath") as string;
            }

            // Run exe from c#
            //string fileName = Globals.GetWorkingFolder() + "\\" + "result.xml";

            fileName = "\"" + fileName + "\"";
            System.Diagnostics.Process.Start(eFramerPath, ((int)ModelType.StitcherModel).ToString() + " " + fileName);
        }

        public static void RunEFramer_efx()
        {
            string _registryKeyName = @"Software\Elibre\eFramer\2017\";
            string eFramerPath = "";
            RegistryKey eFramerKey = Registry.CurrentUser.OpenSubKey(_registryKeyName);

            if (eFramerKey != null)
            {
                eFramerPath = eFramerKey.GetValue("ExecutablePath") as string;
            }

            // Run exe from c#

            string fileName = Globals.GetWorkingFolder() + "\\" + "result.efx";

            fileName = "\"" + fileName + "\"";
            System.Diagnostics.Process.Start(eFramerPath, ((int)ModelType.VisionREZModel).ToString() + " " + fileName);

        }

        public static void LaunchEFramer()
        {
            try
            {
                ApplicationLauncher launcher = new ApplicationLauncher();

                if (!launcher.IsApplicationAvailable)
                    return;

                string version = launcher.ApplicationVersion;
                string inputFile = Globals.GetWorkingFolder() + "\\" + "result.efx";
                string outputFile = "";
                string appCode = Guid.NewGuid().ToString();

                if (launcher.LaunchApplication(appCode, inputFile, out outputFile))
                {
                    MessageBox.Show("Output File = " + outputFile, "Application Launcher");
                }
                else
                {
                    MessageBox.Show("eFramer Failed", "Application Launcher");
                    // Handle Errors
                }

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        public static void OnApplicationClosed(string eFramerFile, string revitFile)
        {
            try
            {
                //txtEframerFile.Text = eFramerFile;
                //txtRevitFile.Text = revitFile;

                MessageBox.Show("eFramer has been closed", "Application Launcher");
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        enum ModelType : int
        {
            eFramerModel = 0,
            StitcherModel = 1,
            VisionREZModel = 2
        }
    }
}

