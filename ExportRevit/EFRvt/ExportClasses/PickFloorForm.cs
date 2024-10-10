using Autodesk.Revit.DB;
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
    public class FloorInfo
    {
        public FloorReferenceLevels Levels;
        public FloorHeights Heights;
        public int index = 0;
        public FloorInfo(int i)
        {
            index = i;
        }
        public FloorInfo(int i, FloorHeights heights, ReferanceLevel baselevel)
        {
            index = i;
            Heights = heights;
            InitializeLevels(baselevel);
        }
        public void InitializeLevels(ReferanceLevel baseReferance)
        {
            Levels = new FloorReferenceLevels();
            Levels.BaseReferencelevel = baseReferance;
            Levels.TopPlateReferencelevel = new ReferanceLevel() { Elevation = Levels.BaseReferencelevel.Elevation + Heights.PlateHeight };
            Levels.FramingReferencelevel = new ReferanceLevel() { Elevation = Levels.TopPlateReferencelevel.Elevation + Heights.FramingThickness };
            Levels.NextFloorBaseReferencelevel = new ReferanceLevel() { Elevation = Levels.FramingReferencelevel.Elevation + Heights.SheathingThickness };
        }
        public void UpdateHeights()
        {
            Heights.PlateHeight = Levels.TopPlateReferencelevel.Elevation - Levels.BaseReferencelevel.Elevation;
            Heights.FramingThickness = Levels.FramingReferencelevel.Elevation - Levels.TopPlateReferencelevel.Elevation;
            Heights.SheathingThickness = Levels.NextFloorBaseReferencelevel.Elevation - Levels.FramingReferencelevel.Elevation;
        }

    }
    
    public class ReferanceLevel
    {
        public Level ModelLevel = null;
        public double Elevation = 0.0;
        public override string ToString()
        {
            string result = "";
            if (ModelLevel !=  null)
            {
                result += " " +ModelLevel.Name+" : ";
            }
            result += Math.Round(Elevation,4);
            return result;
        }
    }
    public class FloorReferenceLevels
    {
        public ReferanceLevel BaseReferencelevel = null;
        public ReferanceLevel TopPlateReferencelevel = null;
        public ReferanceLevel FramingReferencelevel = null;
        public ReferanceLevel NextFloorBaseReferencelevel = null;
    }
    public partial class PickFloorForm : System.Windows.Forms.Form
    {
        public bool ValidData = false;
        public List<Level> DocumentsLevel = new List<Level>();
        public FloorInfo[] floorInfos = null;
        public ReferanceLevel BaseElvation = null;
        public FloorHeights DefaultFloorheights = null;
        public PickFloorForm(List<Level> documentsLevels)
        {
            InitializeComponent();
            this.DocumentsLevel = documentsLevels;
            DefaultFloorheights = new FloorHeights(ErrorMessages.DefaultPlateHeight , ErrorMessages.DefaultFloorFramingThick , ErrorMessages.DefaultFloorSheathingThickness);
            BaseElvation = new ReferanceLevel();
        }

        private void PickFloorForm_Load(object sender, EventArgs e)
        {
            AddPrimaryFloorsInput();
        }

        public void AddPrimaryFloorsInput()
        {
            panel1.Controls.Clear();
            primaryFloorInput input = new primaryFloorInput(this);
            panel1.Controls.Add(input);
            input.Dock = DockStyle.Fill;
        }

        internal void InitializeFloorInfos(int value)
        {
            floorInfos = new FloorInfo[value];
            InitializeFloorInfoUserControl(0);
        }

        internal  void InitializeFloorInfoUserControl(int v)
        {
            panel1.Controls.Clear();
            ReferanceLevel baseElvetion = v == 0 ? BaseElvation : floorInfos[v - 1].Levels.NextFloorBaseReferencelevel;
            FloorInfoComponent input = new FloorInfoComponent(this
            , new FloorInfo(v, new FloorHeights(this.DefaultFloorheights), baseElvetion)
            , this.DocumentsLevel.Where(x => x.Elevation > baseElvetion.Elevation).ToList());
            panel1.Controls.Add(input);
            input.Dock = DockStyle.Fill;
        }
    }
}
