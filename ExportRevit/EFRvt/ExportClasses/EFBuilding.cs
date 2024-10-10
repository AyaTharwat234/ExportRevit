using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Autodesk.Revit.DB;

namespace EFRvt
{
    public static class ErrorMessages
    {
        public static int MaxNumFloors { set; get; } = 8;
        public static double MaxPlateHeight { set; get; } = 13;
        public static double DefaultPlateHeight { set; get; } = 9;
        public static double DefaultFloorFramingThick { set; get; } = 9.25 / 12.0;
        public static double DefaultFloorSheathingThickness { set; get; } = (5.0 / 8.0) / 12.0;

        public static string NoFloorErrors = "Revit levels can't be mapped to EFramer floors";
        public static string MaxFloorErrors = "Number of Floors are more than" + MaxNumFloors;
    }

    [XmlRoot(NodesNames.BUILDING)]
    public class EFBuilding
    {
        [XmlArray(NodesNames.FLOOR_LIST)]
        [XmlArrayItem(NodesNames.FLOOR)]
        public List<EFFloor> FloorList = new List<EFFloor>();

        [XmlElement(NodesNames.MIN_X)]
        public double MinX;
        [XmlElement(NodesNames.MIN_Y)]
        public double MinY;
        [XmlElement(NodesNames.MAX_X)]
        public double MaxX;
        [XmlElement(NodesNames.MAX_Y)]
        public double MaxY;

        [XmlArray(NodesNames.ROOFS)]
        [XmlArrayItem(NodesNames.BASE_POLYGON_ROOF, typeof(EFRoof))]
        [XmlArrayItem(NodesNames.PLANES_GENERIC_ROOF, typeof(EFGenericRoof))]
        public List<EFBasicRoof> LowRoofs { get; set; } = new List<EFBasicRoof>();

        public double BaseElevation = 0.0;
        public EFBuilding()
        {

        }
        public void InitializeMinMax()
        {
            MinX = MinX = double.MaxValue;
            MaxX = MaxY = double.MinValue;
        }

    }
}

