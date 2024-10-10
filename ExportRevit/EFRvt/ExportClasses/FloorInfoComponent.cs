using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace EFRvt
{
    public partial class FloorInfoComponent : UserControl
    {
        private List<Level> _documentsLevels = new List<Level>();
        protected PickFloorForm ParentFrm = null;
        public FloorInfo FloorInfo = null;

        private ElevationSelector topPlateElevationSelector = null;
        private ElevationSelector framingElevationSelector = null;
        private ElevationSelector sheathingElevationSelector = null;
        private levelsDrawer _levelDrawer = null;
        public FloorInfoComponent(PickFloorForm parentFrm, FloorInfo floorInfo , List<Level> levels)
        {
            InitializeComponent();
            this.ParentFrm = parentFrm;
            this.FloorInfo = floorInfo;
            _documentsLevels = levels;
        }

        private void FloorInfoComponent_Load(object sender, EventArgs e)
        {
            index_label.Text = (this.FloorInfo.index + 1).ToString();
            BaseLevel_label.Text = this.FloorInfo.Levels.BaseReferencelevel.ToString();
            UpdateHeightsLabels();
            InitializeElvationSelectors();
            InitializeDrawer();
        }

        private void InitializeDrawer()
        {
            panel1.Controls.Clear();
            _levelDrawer = new levelsDrawer(this.FloorInfo);
            panel1.Controls.Add(_levelDrawer);
            _levelDrawer.Dock = DockStyle.Fill;
        }

        private void UpdateHeightsLabels()
        {
            PlateHeight_label.Text = Math.Round(this.FloorInfo.Heights.PlateHeight, 4).ToString();
            framingThickness_height.Text = Math.Round(this.FloorInfo.Heights.FramingThickness, 4).ToString();
            Sheathing_label.Text = Math.Round(this.FloorInfo.Heights.SheathingThickness, 4).ToString();
        }

        private void Back_button_Click(object sender, EventArgs e)
        {
            
            if (this.FloorInfo.index == 0)
            {
                this.ParentFrm.AddPrimaryFloorsInput();
            }
            else
            {
                this.ParentFrm.InitializeFloorInfoUserControl(this.FloorInfo.index - 1);
            }
        }

        public void InitializeElvationSelectors()
        {
            this.topPlateElevationSelector = new ElevationSelector();
            InitializeElvationSelectors(this.topPlateElevationSelector, TopPlate_gbox, "TopPlateSelector"
                , "Top plate Level", this.FloorInfo.Levels.TopPlateReferencelevel , TopPlateReferenceLevelChanged);
            this.framingElevationSelector = new ElevationSelector();
            InitializeElvationSelectors(this.framingElevationSelector, FramingThickness_gbox, "FramingSelector"
                , "Framing Level", this.FloorInfo.Levels.FramingReferencelevel,FramingReferenceLevelChanged);
            this.sheathingElevationSelector = new ElevationSelector();
            InitializeElvationSelectors(this.sheathingElevationSelector, SheathingThickness_gbox, "SheathingSelector"
                , "Sheathing Level", this.FloorInfo.Levels.NextFloorBaseReferencelevel, SheatingReferenceLevelChanged);
        }
        private void InitializeElvationSelectors(ElevationSelector selector, GroupBox gb, string Name, string text, ReferanceLevel level ,Action action)
        {
            gb.Controls.Clear();
            selector.Location = new System.Drawing.Point(1,2);
            selector.Name = Name;
            selector.Size = new System.Drawing.Size(311, 51);
            selector.TabIndex = 0;

            selector.SetInputLabel(text);
            selector.ReferanceLevel = level;
            selector.DocumentsLevels = _documentsLevels;
            gb.Controls.Add(selector);
            selector.InformParentStateChanged = action;
            selector.Lvls_txtBox_CheckedChanged(null, null);
        }

        private void TopPlateReferenceLevelChanged()
        {
            this.FloorInfo.Levels.TopPlateReferencelevel = this.topPlateElevationSelector.ReferanceLevel;
            ReferenceLevelChanged();
        }
        private void FramingReferenceLevelChanged()
        {
            this.FloorInfo.Levels.FramingReferencelevel = this.framingElevationSelector.ReferanceLevel;
            ReferenceLevelChanged();
        }
        private void SheatingReferenceLevelChanged()
        {
            this.FloorInfo.Levels.NextFloorBaseReferencelevel = this.sheathingElevationSelector.ReferanceLevel;
            ReferenceLevelChanged();
        }
        private void ReferenceLevelChanged()
        {
            this.FloorInfo.UpdateHeights();
            UpdateHeightsLabels();
            _levelDrawer?.UpdateData();
        }
        private void OK_button_Click(object sender, EventArgs e)
        {
            string errorMessage = "";
            if (!primaryFloorInput.ValidateFloorHeights(this.FloorInfo.Heights, out errorMessage))
            {
                System.Windows.Forms.MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.ParentFrm.floorInfos[this.FloorInfo.index] = this.FloorInfo;
            if (this.FloorInfo.index == this.ParentFrm.floorInfos.Length-1)
            {
                ParentFrm.Close();
            }
            else
            {
                this.ParentFrm.InitializeFloorInfoUserControl(this.FloorInfo.index + 1);
            } 
        }

   
    }
}
