using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32; // To access Registry

using Elibre.Net.Debug;
using Elibre.Net.Remtoing;

namespace EFRvt
{
    public partial class frmMain : Form
    {
        #region Fields
        protected ApplicationLauncher _launcher = new ApplicationLauncher();
        #endregion

        #region Properties
        #endregion

        #region Constructor(s)
        public frmMain()
        {
            InitializeComponent();
        }
        #endregion

        private void frmMain_Load(object sender, EventArgs e)
        {
            txtAvailabe.Text = _launcher.IsApplicationAvailable.ToString();
            txtVersion.Text = _launcher.ApplicationVersion;
        }

        private void btnBrowseFileName_Click(object sender, EventArgs e)
        {
            try
            {
                ShowOpenFileDialog();
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        public void OnApplicationClosed(string eFramerFile, string revitFile)
        {
            try
            {
                txtEframerFile.Text = eFramerFile;
                txtRevitFile.Text = revitFile;

                MessageBox.Show("eFramer has been closed", "Application Launcher");
            }
            catch(Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        private void ShowOpenFileDialog()
        {
            try
            {
                System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
                ofd.DefaultExt = "efx";
                ofd.Filter = "eFramer Model (*.efx)|*.efx|All files (*.*)|*.*";
                ofd.Multiselect = false;
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;
                ofd.Multiselect = false;
                DialogResult dResult = new DialogResult();

                dResult = ofd.ShowDialog();

                if (dResult == DialogResult.OK)
                {
                    string fName = ofd.FileName;
                    txtFileName.Text = fName;
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        private void btnRunEfr_Click(object sender, EventArgs e)
        {
            try
            {
                if (_launcher != null)
                {
                    _launcher.Dispose();
                    _launcher = null;
                }

                _launcher = new ApplicationLauncher();

                if (!_launcher.IsApplicationAvailable)
                    return;

                string version = _launcher.ApplicationVersion;
                string fileName = txtFileName.Text;

                if (!_launcher.LaunchApplication(fileName, OnApplicationClosed /* CallBack function */))
                {
                    // Handle Errors
                }
            }
            catch(Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
