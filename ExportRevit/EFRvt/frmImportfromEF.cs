using System;
using System.Windows.Forms;

namespace EFExt2017
{
    public enum ImportedFormat
    {
        EFRvt=0,
        Efx =1
    }
    public partial class frmImportfromEF : Form
    {
        protected Autodesk.Revit.DB.Document m_Doc;
        private ImportedFormat _format ;
        public string fileName = "";
       // public ImportOptions importOption = new ImportOptions();

        public frmImportfromEF(Autodesk.Revit.DB.Document uiApp , ImportedFormat format = 0)
        {
            m_Doc = uiApp; _format = format;
            InitializeComponent();
        }


        private void btmBrows_Click(object sender, EventArgs e)
        {
            // BrowseToGetFileName();
            BrowseToGetFileNameRvt();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtImport.Text = "";
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            
            ImportFileFromEF();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // Methods
        private bool ImportFileFromEF()
        {
            try
            {

                if (txtImport.Text != "")
                {
                    fileName = txtImport.Text;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show("Please Select an Exchange File to Open.", "Invalid File Name"/*, MessageBoxIcon.Error*/, MessageBoxButtons.OK);
                }
              //  GlobalsEvents.DocumentExport = false;
                return false;

             
            }
            catch (Exception ex)
            {
                //  Util.SaveErrors(ex);
                // GlobalsEvents.DocumentExport = false;
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
                return false;
            }
        }

        private void BrowseToGetFileName()
        {
          //  GlobalsEvents.DocumentExport = true;
            // New Imprt FRM File
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = "ts";
            ofd.Filter = "Revit Framing files (*.ts)|*.st|All files (*.*)|*.*";

            ofd.Multiselect = false;
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            ofd.Multiselect = false;

            DialogResult dResult = new DialogResult();
            dResult = ofd.ShowDialog();

            if (dResult == DialogResult.OK)
            {
                string cfileName = ofd.FileName;
                txtImport.Text = cfileName;
            }
        }

        private void BrowseToGetFileNameRvt()
        {
            //  GlobalsEvents.DocumentExport = true;
            // New Imprt FRM File
            
            OpenFileDialog ofd = new OpenFileDialog();
            if ( _format == 0)
            {
                ofd.DefaultExt = "ERVT";
                ofd.Filter = "Eframer Revit Files (*.EFRvt)|*.EFRvt|Eframer AutoRevit Files (*.EFRvtAuto)|*.EFRvtAuto|All files (*.*)|*.*";
            }
            else if (_format == ImportedFormat.Efx)
            {
                ofd.DefaultExt = "efx";
                ofd.Filter = "Eframer Revit Files (*.efx)|*.efx";
            }
            
            ofd.Multiselect = false;
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            ofd.Multiselect = false;

            DialogResult dResult = new DialogResult();
            dResult = ofd.ShowDialog();

            if (dResult == DialogResult.OK)
            {
                string cfileName = ofd.FileName;
                txtImport.Text = cfileName;
            }
        }

    }
}
