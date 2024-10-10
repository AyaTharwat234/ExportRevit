using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EFRvt
{
    [XmlType(NodesNames.LINE)]
    public class EFLine
    {
        [XmlElement(NodesNames.P1)]
        public EFXYZ P1 { get; set; }
        [XmlElement(NodesNames.P2)]
        public EFXYZ P2 { get; set; }

        public EFLine()
        {

        }
        public EFLine(EFXYZ p1, EFXYZ p2)
        {
            P1 = p1;
            P2 = p2;
        }
        public bool IsHorizontal()
        {
            return Math.Abs(P1.Z - P2.Z) < 0.001;
        }

        internal EFLine To2D()
        {
            EFXYZ p1 = new EFXYZ(P1.X, P1.Y, 0.0);
            EFXYZ p2 = new EFXYZ(P2.X, P2.Y, 0.0);
            return new EFLine(p1, p2);
        }

        public EFLine Reverse()
        {
            return new EFLine(this.P2, this.P1);
        }
    }
    [XmlType(NodesNames.POINT_LOCATION)]
    public class EFPointLocation
    {
        [XmlElement(NodesNames.POSITION)]
        public EFXYZ Position { get; set; }
        [XmlElement(NodesNames.ROTATION)]
        public double Rotation { get; set; }

        public EFPointLocation()
        {

        }
        public EFPointLocation(EFXYZ postion, double rotation)
        {
            Position = postion;
            Rotation = rotation;
        }
    }
    [XmlType(NodesNames.POINT)]
    public class EFXYZ
    {
        [XmlElement(NodesNames.Z)]
        public double Z { get; set; } = 0.0;
        [XmlElement(NodesNames.Y)]
        public double Y { get; set; } = 0.0;
        [XmlElement(NodesNames.X)]
        public double X { get; set; } = 0.0;

        public EFXYZ()
        {

        }
        public EFXYZ(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

    }

    [XmlType(NodesNames.SECTION)]
    public class EFRecSection
    {
        [XmlElement(NodesNames.WIDTH)]
        public double Width;
        [XmlElement(NodesNames.DEPTH)]
        public double Depth;
    }
    public abstract class EFObject
    {

    }

    [XmlType(NodesNames.BEAM)]
    public class EFBeam : EFObject
    {
        [XmlElement(NodesNames.LINE)]
        public EFLine Line { get; set; }

        [XmlElement(NodesNames.TOP_OFFSET)]
        public double TopOffset = 0.0;

        //[XmlElement(NodesNames.SECTION, typeof(MapRecSection))]
        //[XmlElement(NodesNames.ISECTION, typeof(MapISection))]
        //public MapSection Section { get; set; } = null;

        //[XmlElement(NodesNames.IS_INVERTED)]
        //public bool IsInverted = false;
    }
    [XmlType(NodesNames.POST)]
    public class EFPost : EFObject
    {
        [XmlElement(NodesNames.POINT_LOCATION)]
        public EFPointLocation location = null;

        [XmlElement(NodesNames.TOP_OFFSET)]
        public double TopOffset = 0.0;

        [XmlElement(NodesNames.BOTTOM_OFFSET)]
        public double BottomOffset = 0.0;
    }

    [XmlType(NodesNames.ROOF_BASE_LINE)]
    public class EFFootprintEdge
    {
        [XmlElement(NodesNames.LINE)]
        public EFLine Line;
        [XmlElement(NodesNames.OVERHANG)]
        public double OverHang { get; set; }
        [XmlElement(NodesNames.PITCH)]
        public double Slope { get; set; }
        [XmlElement(NodesNames.OFFSET)]
        public double Offset { get; set; }
        [XmlElement(NodesNames.SlOPEARROW)]
        public bool SlopeArrow { get; set; } = false;

        [XmlElement(NodesNames.SlOPEARROWDIR)]
        public EFXYZ SlopeArrowDirection { get; set; } = new EFXYZ();
        [XmlElement(NodesNames.GABLE)]
        public string IsGable
        {
            get { return Gable.ToString(); }
            set { Gable = Convert.ToBoolean(value); }
        }

        public bool Gable { get; set; }

        public double Z = 0.0;
    }

    [XmlType(NodesNames.PROFILE)]
    public class EFProfile
    {
        [XmlArray(NodesNames.POINT_LIST)]
        [XmlArrayItem(NodesNames.POINT, typeof(EFXYZ))]
        public List<EFXYZ> PointList = new List<EFXYZ>();
    }

    public abstract class EFBasicRoof
    {
        [XmlElement(NodesNames.ID)]
        public string GuId
        {
            get { return ID.ToString(); }
            set { ID = Guid.Parse(value); }
        }

        public Guid ID = Guid.Empty;

        [XmlArray(NodesNames.OPENING_LIST)]
        [XmlArrayItem(NodesNames.PROFILE, typeof(EFProfile))]
        public List<EFProfile> OpeningList = new List<EFProfile>();

    }

    [XmlType(NodesNames.BASE_POLYGON_ROOF)]
    public class EFRoof : EFBasicRoof
    {
        [XmlArray(NodesNames.ROOF_BASE_LINE_LIST)]
        [XmlArrayItem(NodesNames.ROOF_BASE_LINE, typeof(EFFootprintEdge))]
        public List<EFFootprintEdge> eFFootprintEdges = new List<EFFootprintEdge>();
    }
    [XmlType(NodesNames.PLANES_GENERIC_ROOF)]
    public class EFGenericRoof : EFBasicRoof
    {
        [XmlArray(NodesNames.ROOF_PLANE_LIST)]
        [XmlArrayItem(NodesNames.ROOF_PLANE_LIST, typeof(EFProfile))]
        public List<EFProfile> RoofPolygons = new List<EFProfile>();

        [XmlArray(NodesNames.WALL_ATTATCHMENT)]
        [XmlArrayItem(NodesNames.LINE, typeof(EFLine))]
        public List<EFLine> AttachedWallsLines = new List<EFLine>();
    }

    [XmlType(NodesNames.WALL)]
    public class EFWall : EFObject
    {
        [XmlElement(NodesNames.LINE)]
        public EFLine Line;

        [XmlElement(NodesNames.POINT)]
        public EFXYZ Orienation;

        [XmlArray(NodesNames.OPENING_LIST)]
        [XmlArrayItem(NodesNames.DOOR, typeof(EFDoor))]
        [XmlArrayItem(NodesNames.WINDOW, typeof(EFWindow))]
        public List<EFWallOpening> Opening_List = new List<EFWallOpening>();

        [XmlElement(NodesNames.TOP_OFFSET)]
        public double TopOffset = 0.0;

        [XmlElement(NodesNames.BOTTOM_OFFSET)]
        public double BottomOffset = 0.0;

        [XmlElement(NodesNames.PROFILE)]
        public EFProfile SideFace;

    }

    [XmlType(NodesNames.WALLOPENING)]
    public class EFWallOpening
    {
        [XmlElement(NodesNames.POINT)]
        public EFXYZ midPoint;
        [XmlElement(NodesNames.WIDTH)]
        public double Width;
        [XmlElement(NodesNames.HEADER_HEIGHT)]
        public double HeaderHeight;
    }

    [XmlType(NodesNames.DOOR)]
    public class EFDoor : EFWallOpening
    {

    }
    [XmlType(NodesNames.WINDOW)]
    public class EFWindow : EFWallOpening
    {
        public double SillHeight;

    }

    [XmlType(NodesNames.JOIST_AREA)]
    public class EFBeamSystem : EFObject
    {
        [XmlElement(NodesNames.PROFILE)]
        public EFProfile Boundary;

        [XmlArray(NodesNames.FLOOR_OBJECTS)]
        [XmlArrayItem(NodesNames.BEAM, typeof(EFBeam))]
        public List<EFBeam> beams = new List<EFBeam>();

        [XmlElement(NodesNames.TOP_OFFSET)]
        public double TopOffset = 0.0;
    }

}
