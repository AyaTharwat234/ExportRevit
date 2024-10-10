using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EFRvt
{
    public partial class primaryFloorInput : UserControl
    {
        protected PickFloorForm ParentFrm = null;
        public primaryFloorInput(PickFloorForm frm)
        {
            InitializeComponent();
            ParentFrm = frm;
        }

        private void PrimaryFloorInput_Load(object sender, EventArgs e)
        {
            InitializeBaseLevelSelector();
            InitializeDefaultHeights();
        }

        private void InitializeDefaultHeights()
        {
            PlateHeight_TB.Text = Math.Round(ParentFrm.DefaultFloorheights.PlateHeight,4).ToString();
            FramingThickness_TB.Text = Math.Round(ParentFrm.DefaultFloorheights.FramingThickness,4).ToString();
            SheathingThickness_TB.Text = Math.Round(ParentFrm.DefaultFloorheights.SheathingThickness,4).ToString();
        }

        private void InitializeBaseLevelSelector()
        {
            BaseelevationSelector.SetInputLabel("Base Elevation");
            BaseelevationSelector.ReferanceLevel = ParentFrm.BaseElvation;
            BaseelevationSelector.DocumentsLevels = ParentFrm.DocumentsLevel;
            BaseelevationSelector.InformParentStateChanged = () => { };
            BaseelevationSelector.Lvls_txtBox_CheckedChanged(null, null);
        }

        private void Cancel_button_Click(object sender, EventArgs e)
        {
            ParentFrm.ValidData = false;
            ParentFrm.floorInfos = null;
            ParentFrm.Close();
        }

        private void OK_button_Click(object sender, EventArgs e)
        {
            string errorMessage = "";
            FloorHeights heights = null;
            if (!SetDefaultFloorHeights(out heights , out errorMessage))
            {
                MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                errorMessage = "";
                if (!ValidateFloorHeights(heights ,out errorMessage))
                {
                    MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                ParentFrm.DefaultFloorheights = heights;
                if (BaseelevationSelector.ReferanceLevel == null)
                {
                    MessageBox.Show("Base Level value isn't valid", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                {
                    ParentFrm.BaseElvation = BaseelevationSelector.ReferanceLevel;
                }
                ParentFrm.InitializeFloorInfos((int)numericUpDown1.Value);
            }
        }

        public static bool ValidateFloorHeights(FloorHeights heights, out string errorMessage)
        {
            errorMessage = "";
            bool result = true;
            if (heights.PlateHeight > ErrorMessages.MaxPlateHeight)
            {
                errorMessage += "Plate height couldn't be more than " + ErrorMessages.MaxPlateHeight + Environment.NewLine;
                result = false;
            }
            if (heights.PlateHeight < 0.0001)
            {
                errorMessage += "Plate height must be more than zero";
                result = false;
            }
            if (heights.FramingThickness < 0.0001)
            {
                errorMessage += "Framing Thickness must be more than zero";
                result = false;
            }
            if (heights.SheathingThickness < 0.0001)
            {
                errorMessage += "Sheathing Thickness must be more than zero";
                result = false;
            }
            return result;
        }

        private bool SetDefaultFloorHeights(out FloorHeights heights, out string errorMessage)
        {
            errorMessage = "value(s) of";
            bool result = true;
            heights = new FloorHeights(0.0,0.0,0.0);

            if (!double.TryParse(PlateHeight_TB.Text , out heights.PlateHeight))
            {
                result = false;
                errorMessage += " -Plate height";
            }
            if (!double.TryParse(FramingThickness_TB.Text, out heights.FramingThickness))
            {
                result = false;
                errorMessage += " -Framing Thickness";
            }
            if (!double.TryParse(SheathingThickness_TB.Text, out heights.SheathingThickness))
            {
                result = false;
                errorMessage += " -Sheathing Thickness";
            }

            errorMessage += "- can't be vaild";
            return result;
        }
    }
}
