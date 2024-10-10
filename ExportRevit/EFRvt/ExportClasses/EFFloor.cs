using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
namespace EFRvt
{
    [XmlType(NodesNames.FLOOR_HEIGHTS)]
    public class FloorHeights
    {
        [XmlElement(NodesNames.TOP_PLATE_HEIGHT)]
        public double PlateHeight;
        [XmlElement(NodesNames.FRAMING_HEIGHT)]
        public double FramingThickness;
        [XmlElement(NodesNames.SHEATHING_HEIGHT)]
        public double SheathingThickness;
        public FloorHeights()
        {

        }
        public FloorHeights(FloorHeights defaultFloorheights)
        {
            this.PlateHeight = defaultFloorheights.PlateHeight;
            this.FramingThickness = defaultFloorheights.FramingThickness;
            this.SheathingThickness = defaultFloorheights.SheathingThickness;
        }

        public FloorHeights(double plateHeight, double framingThickness, double sheathingThickness)
        {
            this.PlateHeight = plateHeight;
            this.FramingThickness = framingThickness;
            this.SheathingThickness = sheathingThickness;
        }
    }


    [XmlType(NodesNames.FLOOR)]
    public class EFFloor
    {
        [XmlElement(NodesNames.FLOOR_HEIGHTS)]
        public FloorHeights Heights = new FloorHeights();

        #region Not For Exporting
        public double BaseElevation = 0.0;
        public double TopPlateLevelElevation = 0.0;
        public double FramingLevelElevation = 0.0;
        public double SubLevelElevation = 0.0;
        #endregion

        [XmlArray(NodesNames.FLOOR_OBJECTS)]
        [XmlArrayItem(NodesNames.POST, typeof(EFPost))]
        [XmlArrayItem(NodesNames.BEAM, typeof(EFBeam))]
        [XmlArrayItem(NodesNames.JOIST_AREA, typeof(EFBeamSystem))]
        [XmlArrayItem(NodesNames.WALL, typeof(EFWall))]
        public List<EFObject> FloorObjects { get; set; } = new List<EFObject>();

        [XmlArray(NodesNames.ROOFS)]
        [XmlArrayItem(NodesNames.BASE_POLYGON_ROOF, typeof(EFRoof))]
        [XmlArrayItem(NodesNames.PLANES_GENERIC_ROOF, typeof(EFGenericRoof))]
        public List<EFBasicRoof> Roofs { get; set; } = new List<EFBasicRoof>();

        public EFFloor()
        {

        }
        public EFFloor(FloorHeights heights, double baseElevation)
        {
            Heights = new FloorHeights(heights);
            BaseElevation = baseElevation;
            GenerateElevations();
        }

        private void GenerateElevations()
        {
            TopPlateLevelElevation = BaseElevation + Heights.PlateHeight;
            FramingLevelElevation = TopPlateLevelElevation + Heights.FramingThickness;
            SubLevelElevation = FramingLevelElevation + Heights.SheathingThickness;
        }
    }


}
