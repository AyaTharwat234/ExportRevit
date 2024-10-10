
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EFRvt
{
    public partial class ExportingConfigrationFrm : Form
    {
        public static string wallsText = "Walls";
        public static string ColumnsText = "Columns / Posts";
        public static string BeamsText = "Beams";
        public static string BeamsSystemsText = "Beams systems";
        public static string FootPrintRoofsText = "Footprint Roofs";
        public static string GenericRoofsText = "Basic Roofs";
        public ExportingOptions Options { get; set; } = new ExportingOptions();
        public ExportingConfigrationFrm()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            var selectedItems = checkedListBox1.CheckedItems.Cast<string>().ToList();
            Options.ExportWalls = selectedItems.Contains(wallsText);
            Options.ExportBeams = selectedItems.Contains(BeamsText);
            Options.ExportColumns = selectedItems.Contains(ColumnsText);
            Options.ExportBeamsSystems = selectedItems.Contains(BeamsSystemsText);
            Options.ExportFootPrintRoofs = selectedItems.Contains(FootPrintRoofsText);
            Options.ExportGenericRoofs = selectedItems.Contains(GenericRoofsText);
            this.Close();
        }

        private void ExportingConfigrationFrm_Load(object sender, EventArgs e)
        {
            List<string> items = new List<string>()
            {
              wallsText, ColumnsText, BeamsText,  BeamsSystemsText,   FootPrintRoofsText,    GenericRoofsText
            };
            checkedListBox1.DataSource = items;
        }
    }
    public class ExportingOptions
    {
        public bool ExportWalls { get; set; }
        public bool ExportBeams { get; set; }
        public bool ExportColumns { get; set; }
        public bool ExportBeamsSystems { get; set; }
        public bool ExportFootPrintRoofs { get; set; }
        public bool ExportGenericRoofs { get; set; }
    }
}
