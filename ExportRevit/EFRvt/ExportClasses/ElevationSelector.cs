using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using System.Windows.Forms;

namespace EFRvt
{
    public partial class ElevationSelector : UserControl
    {
        private List<Level> _documentsLevels = new List<Level>();
        private ReferanceLevel _referanceLevel = null;
        public Action InformParentStateChanged = null;
        public ReferanceLevel ReferanceLevel
        {
            set
            {
                _referanceLevel = value;
                if (InformParentStateChanged != null)
                {
                    InformParentStateChanged();
                }
            }
            get { return _referanceLevel; }
        }

        public List<Level> DocumentsLevels
        {
            get { return _documentsLevels; }
            set
            {
                _documentsLevels = value;
                if (_documentsLevels == null || !_documentsLevels.Any())
                {
                    Lvls_txtBox.Checked = false;
                    Lvls_txtBox_CheckedChanged(null, null);
                    Lvls_txtBox.Visible = false;
                }
                else
                {
                    Lvls_txtBox.Visible = true;
                }
            }
        }
        public ElevationSelector()
        {
            InitializeComponent();
        }

        public void SetInputLabel(string inputLabel)
        {
            Input_Lb.Text = inputLabel;
        }

        public void Lvls_txtBox_CheckedChanged(object sender, EventArgs e)
        {
            if (Lvls_txtBox.Checked)
            {
                Input_Lb.Visible = false;
                BaseElvation_TextBox.Visible = false;
                outputLevel_LB.Visible = true;
                Lvls_comboBox.Visible = true;
                Lvls_comboBox.DisplayMember = "Name";
                Lvls_comboBox.DataSource = new List<Level>(DocumentsLevels);
                Lvls_comboBox_SelectedIndexChanged(null,null);
                if (ReferanceLevel != null)
                {
                    outputLevel_LB.Text = Math.Round(ReferanceLevel.Elevation, 4).ToString();
                }
            }
            else
            {
                Input_Lb.Visible = true;
                BaseElvation_TextBox.Visible = true;
                outputLevel_LB.Visible = false;
                Lvls_comboBox.Visible = false;

                if (ReferanceLevel != null)
                {
                    BaseElvation_TextBox.Text = Math.Round(ReferanceLevel.Elevation, 4).ToString();
                }
            }
            if (InformParentStateChanged != null)
            {
                InformParentStateChanged();
            }
        }

        private void Lvls_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _referanceLevel = null;
            if (Lvls_comboBox.SelectedItem != null)
            {
                Level level = Lvls_comboBox.SelectedItem as Level;
                if (level != null)
                {
                    _referanceLevel = new ReferanceLevel() {  ModelLevel = level , Elevation = level.Elevation};
                    outputLevel_LB.Text = Math.Round(ReferanceLevel.Elevation, 4).ToString();
                }
            }
            if (InformParentStateChanged != null)
            {
                InformParentStateChanged();
            }
        }

        private void BaseElvation_TextBox_TextChanged(object sender, EventArgs e)
        {
            _referanceLevel = null;
            double elevation = 0.0;
            if (double.TryParse(BaseElvation_TextBox.Text , out elevation))
            {
                _referanceLevel = new ReferanceLevel() { Elevation = elevation };
            }
            if (InformParentStateChanged != null)
            {
                InformParentStateChanged();
            }
        }
    }
}
