using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;

namespace EFRvt
{
    public partial class levelsDrawer : UserControl
    {
        private int marigin = 10;

        private FloorInfo _floorInfo = null;
        private bool _valid = false;

        private int _baseLevel;
        private int _PlateLevel;
        private int _FramingLevel;
        public levelsDrawer(FloorInfo FloorInfo)
        {
            InitializeComponent();
            _floorInfo = FloorInfo ;
        }
        public void UpdateData()
        {
            this.Invalidate();
            if (_floorInfo == null)
            {
                _valid = false;
                return;
            }

            NextFloor_Label.Text = FraminLevel_Label.Text = TopPlate_Label.Text = 
            BaseLevel_label.Text = Plate_Height_Label.Text = Framing_thick_Label.Text = 
            Sheathing_thick_label.Text = "";

            string errorMessage = "";
            if (!primaryFloorInput.ValidateFloorHeights(_floorInfo.Heights, out errorMessage))
            {
                _valid = false;
                return;
            }
            _valid = true;
            double totalHeight = this._floorInfo.Levels.NextFloorBaseReferencelevel.Elevation -
                this._floorInfo.Levels.BaseReferencelevel.Elevation;
            int actulaSize = this.Size.Height - 2 * marigin;

            _baseLevel = this.Size.Height - marigin;
            _FramingLevel = (int)(marigin + actulaSize * this._floorInfo.Heights.SheathingThickness  / totalHeight);
            _PlateLevel = (int)(_baseLevel - actulaSize * this._floorInfo.Heights.PlateHeight  / totalHeight);

            NextFloor_Label.Location = new System.Drawing.Point(marigin, marigin - NextFloor_Label.Size.Height/ 2);
            NextFloor_Label.Text = Math.Round(_floorInfo.Levels.NextFloorBaseReferencelevel.Elevation, 4).ToString();

            FraminLevel_Label.Location = new System.Drawing.Point(marigin,_FramingLevel - FraminLevel_Label.Size.Height / 2);
            FraminLevel_Label.Text = Math.Round(_floorInfo.Levels.FramingReferencelevel.Elevation,4).ToString();

            TopPlate_Label.Location = new System.Drawing.Point(marigin, _PlateLevel - TopPlate_Label.Size.Height / 2);
            TopPlate_Label.Text = Math.Round(_floorInfo.Levels.TopPlateReferencelevel.Elevation, 4).ToString();

            BaseLevel_label.Location = new System.Drawing.Point(marigin, _baseLevel - BaseLevel_label.Size.Height / 2);
            BaseLevel_label.Text = Math.Round(_floorInfo.Levels.BaseReferencelevel.Elevation, 4).ToString();

            int x = this.Size.Width - 3 * marigin;

            Plate_Height_Label.Location = new System.Drawing.Point(x,(_baseLevel + _PlateLevel - Plate_Height_Label.Size.Height) /2);
            Plate_Height_Label.Text = Math.Round(_floorInfo.Heights.PlateHeight,4).ToString();

            Framing_thick_Label.Location = new System.Drawing.Point(x, (_FramingLevel + _PlateLevel - Framing_thick_Label.Size.Height) / 2);
            Framing_thick_Label.Text = Math.Round(_floorInfo.Heights.FramingThickness, 4).ToString();

            Sheathing_thick_label.Location = new System.Drawing.Point(x, (_FramingLevel + marigin - Sheathing_thick_label.Size.Height) / 2);
            Sheathing_thick_label.Text = Math.Round(_floorInfo.Heights.SheathingThickness, 4).ToString();
        }

        private void LevelsDrawer_Load(object sender, EventArgs e)
        {
            UpdateData();
        }

        private void LevelsDrawer_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if (!_valid)
            {
                return;
            }
            Pen blue = new Pen(System.Drawing.Color.Blue,3);
            DrawHorizontalLine(g,blue,marigin);
            DrawHorizontalLine(g,blue,_baseLevel);
            Pen red = new Pen(System.Drawing.Color.Red,3);
            DrawHorizontalLine(g, red, _FramingLevel);
            Pen green = new Pen(System.Drawing.Color.Green, 3);
            DrawHorizontalLine(g, green, _PlateLevel);
            Pen dimension = new Pen(System.Drawing.Color.Black, 2);
            int x = this.Size.Width - 3 * marigin;
            DrawVerticalLine(g, dimension, x, marigin, _baseLevel);
            DrawIndication(g,dimension,x,marigin);
            DrawIndication(g,dimension,x,_FramingLevel);
            DrawIndication(g,dimension,x,_PlateLevel);
            DrawIndication(g,dimension,x,_baseLevel);
        }
        private void DrawHorizontalLine(Graphics g , Pen p , int y)
        {
            g.DrawLine(p, new System.Drawing.Point(0,y), new System.Drawing.Point(this.Size.Width, y));
        }
        private void DrawVerticalLine(Graphics g, Pen p  , int x, int y1 , int y2)
        {
            g.DrawLine(p, new System.Drawing.Point(x, y1), new System.Drawing.Point(x, y2));
        }
        private void DrawIndication(Graphics g, Pen p, int x, int y)
        {
            g.DrawLine(p, new System.Drawing.Point(x+2, y+2), new System.Drawing.Point(x-2, y-2));
        }

    }
}
