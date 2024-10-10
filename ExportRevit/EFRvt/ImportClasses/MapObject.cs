using Autodesk.Revit.DB;
using EFRvt.Revit;
using Elibre.Net.Geometry;
using Elibre.Net.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace EFRvt
{
    public class MapObject
    {
        public MapFloor Floor { get; set; }
        internal virtual Element SetData(Level level)
        {
            return null;
        }
    }

    [XmlType(NodesNames.PRE_MANFACTURED_WALL)]
    public class MapPreManfactureWall : MapObject
    {
        [XmlElement(NodesNames.POINT_LOCATION)]
        public PointLocation PointLocation { get; set; }

        [XmlElement(NodesNames.MIN_Z)]
        public double MinZ { get; set; }

        [XmlElement(NodesNames.MAX_Z)]
        public double MaxZ { get; set; }

        private bool _onFoundation;

        [XmlElement(NodesNames.ON_FOUNDATION)]
        public string OnFoundation
        {
            get { return _onFoundation.ToString(); ; }
            set { _onFoundation = Convert.ToBoolean(value); }
        }

        [XmlElement(NodesNames.NAME)]
        public string Name { get; set; }

        [XmlElement(NodesNames.SECTION)]
        public MapRecSection Section { get; set; }

        internal override Element SetData(Level level)
        {
            try
            {
                return GeneralCreator.CreatePost(Events.m_doc, "EFPreManfactureWall", Name, Section, MinZ, MaxZ,
                    PointLocation.Position.GetReference(), PointLocation.Direction.GetReference(), _onFoundation);
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
    [XmlType(NodesNames.TRUSS_AREA)]
    public class MapTrussArea : MapObject
    {
        [XmlElement(NodesNames.NAME)]
        public string Name { get; set; }

        [XmlArray(NodesNames.GIRDER_TRUSS_LIST)]
        [XmlArrayItem(NodesNames.GENERIC_GIRDER_TRUSS, typeof(MapGenericGirderTruss))]
        public List<MapGenericGirderTruss> GirderTrusses { get; set; }

        [XmlArray(NodesNames.TRUSS_LIST)]
        [XmlArrayItem(NodesNames.GENERIC_TRUSS, typeof(MapGenericTruss))]
        public List<MapGenericTruss> Trusses { get; set; }
        internal override Element SetData(Level level)
        {
            try
            {
                List<ElementId> ids = new List<ElementId>();

                foreach (var Truss in Trusses)
                {
                    Truss.Floor = Floor;
                    Element e = Truss.SetData(level);
                    if (e != null)
                    { ids.Add(e.Id); }
                }

                foreach (var Truss in GirderTrusses)
                {
                    Truss.Floor = Floor;
                    Element e = Truss.SetData(level);
                    if (e != null)
                    { ids.Add(e.Id); }
                }

                return GeneralCreator.CreateTheGroup(Events.m_doc, ids, Name, null);
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
    [XmlType(NodesNames.GENERIC_GIRDER_TRUSS)]
    public class MapGenericGirderTruss : MapObject
    {
        [XmlElement(NodesNames.GENERIC_TRUSS)]
        public MapGenericTruss Truss { get; set; }

        [XmlElement(NodesNames.NAME)]
        public string Name { get; set; }

        [XmlElement(NodesNames.PROFILE)]
        public MapProfile Profile { get; set; }

        internal override Element SetData(Level level)
        {
            try
            {
                Truss.Floor = Floor;
                Element firstTruss = Truss.SetData(level);
                if (firstTruss != null)
                {
                    List<ElementId> elements = new List<ElementId>();
                    XYZ firstTrussPosition = Truss.PointLocation.Position.GetReference();
                    elements.Add(firstTruss.Id);
                    using (SubTransaction t = new SubTransaction(Events.m_doc))
                    {
                        t.Start();
                        foreach (var position in Profile.ConvertToXYZList())
                        {
                            elements.AddRange(ElementTransformUtils.CopyElement(Events.m_doc, firstTruss.Id, position - firstTrussPosition));
                        }
                        t.Commit();
                    }
                    return GeneralCreator.CreateTheGroup(Events.m_doc, elements, Name, null);
                }
                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

    }
    [XmlType(NodesNames.GENERIC_TRUSS_MEMBER)]
    public class MapGenericTrussMember : MapObject
    {
        [XmlElement(NodesNames.WIDTH)]
        public double Width { get; set; }
        [XmlElement(NodesNames.PROFILE)]
        public MapProfile BaseProfile { get; set; }
    }
    [XmlType(NodesNames.GENERIC_TRUSS)]
    public class MapGenericTruss : MapObject
    {
        [XmlElement(NodesNames.POINT_LOCATION)]
        public PointLocation PointLocation { get; set; }

        [XmlElement(NodesNames.NAME)]
        public string Name { get; set; }


        internal override Element SetData(Level level)
        {
            MapTrussFamily fam = Floor.Building.Families.Where(x => x is MapTrussFamily).Cast<MapTrussFamily>().FirstOrDefault(x => x.Name == Name);
            if (fam == null)
                return null;

            return GeneralCreator.CreateGeneralTruss(Events.m_doc, Name, fam.GetSupFolderPathName()
                , PointLocation.Position.GetReference(), PointLocation.Direction.GetReference());
        }

    }
    public class MapTruss : MapObject
    {
        //internal override Element SetData(XmlNode objectNode, Document doc, Level level)
        //{
        //    try
        //    {
        //        double width = Convert.ToDouble(XMLParser.ReadXmlNode(objectNode, NodesNames.WIDTH,0.0));
        //        double TopDepth = Convert.ToDouble(XMLParser.ReadXmlNode(objectNode, NodesNames.TOP_CHORD_DEPTH, 0.0));
        //        double WebDepth = Convert.ToDouble(XMLParser.ReadXmlNode(objectNode, NodesNames.WEB_DEPTH, 0.0));
        //        double bottomDepth = Convert.ToDouble(XMLParser.ReadXmlNode(objectNode, NodesNames.BOTTOM_CHORD_DEPTH, 0.0));
        //        double bottomChordLevel = Convert.ToDouble(XMLParser.ReadXmlNode(objectNode, NodesNames.BOTTOM_CHORD_LEVEL, 0.0));
        //        double startOverHang = Convert.ToDouble(XMLParser.ReadXmlNode(objectNode, NodesNames.START_OVERHANG, 0.0));
        //        double endOverHang = Convert.ToDouble(XMLParser.ReadXmlNode(objectNode, NodesNames.End_OVERHANG, 0.0));
        //        int type = Convert.ToInt32(XMLParser.ReadXmlNode(objectNode, NodesNames.TRUSS_TYPE, 0.0));
        //        List<XYZ> profile = GetProfile(objectNode.SelectSingleNode(NodesNames.TOP_PROFILE));
        //        return GeneralCreator.CreateTruss(doc, type, profile, startOverHang, endOverHang, width, TopDepth, WebDepth, bottomDepth , bottomChordLevel);
        //    }
        //    catch (Exception e)
        //    {
        //        return null;
        //    }
        //}
    }
    [XmlType(NodesNames.FOUNDATION)]
    public class MapFoundation : MapObject
    {
        [XmlElement(NodesNames.WIDTH)]
        public double Width { get; set; }
        [XmlElement(NodesNames.LENGTH)]
        public double Length { get; set; }
        [XmlElement(NodesNames.DEPTH)]
        public double Depth { get; set; }
        [XmlElement(NodesNames.POINT_LOCATION)]
        public PointLocation Location { get; set; }
        internal override Element SetData(Level level)
        {
            try
            {
                return GeneralCreator.CreateFoundation(Events.m_doc, level, Location.Position.GetReference(), Location.Direction.GetReference()
                    , Width, Length, Depth);
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
    public class MapWallPost : MapObject
    {
        [XmlElement(NodesNames.NAME)]
        public string Name { get; set; }

        [XmlElement(NodesNames.MIN_Z)]
        public double MinZ { get; set; }

        [XmlElement(NodesNames.MAX_Z)]
        public double MaxZ { get; set; }

        [XmlElement(NodesNames.IN_WALL_WIDTH)]
        public double InWallWidth { get; set; }

        [XmlElement(NodesNames.IN_WALL_DISTANCE)]
        public double InWallDistance { get; set; }
    }
    public class MapWallHeader
    {
        [XmlElement(NodesNames.LINE)]
        public MapLine Line { get; set; }

        [XmlElement(NodesNames.DEPTH)]
        public double Depth { get; set; }
    }
    [XmlType(NodesNames.WALL)]
    public class MapWall : MapObject
    {
        [XmlElement(NodesNames.NAME)]
        public string Name { get; set; }

        [XmlElement(NodesNames.WALL_DEPTH)]
        public double WallDepth { get; set; }

        [XmlElement(NodesNames.TOP_PROFILE)]
        public MapProfile TopProfile { get; set; }

        [XmlElement(NodesNames.BOTTOM_PROFILE)]
        public MapProfile BottomProfile { get; set; }

        [XmlElement(NodesNames.TOP_PLATES_NUMBER)]
        public int TopPlatesCount { get; set; }

        [XmlElement(NodesNames.BOTTOM_PLATES_NUMBER)]
        public int BottomPlatesCount { get; set; }

        [XmlElement(NodesNames.TOP_PLATES_DEPTH)]
        public double TopPlatesDepth { get; set; }

        [XmlElement(NodesNames.BOTTOM_PLATES_DEPTH)]
        public double BottomPlatesDepth { get; set; }

        [XmlArray(NodesNames.WALL_POST_LIST)]
        [XmlArrayItem(NodesNames.WALL_POST, typeof(MapWallPost))]
        public List<MapWallPost> Posts = new List<MapWallPost>();

        [XmlArray(NodesNames.STUD_LIST)]
        [XmlArrayItem(NodesNames.STUD, typeof(MapWallPost))]
        public List<MapWallPost> Studs = new List<MapWallPost>();

        [XmlArray(NodesNames.WALL_MANFACTURED_WALL_LIST)]
        [XmlArrayItem(NodesNames.WALL_MANFACTURED_WALL, typeof(MapWallPost))]
        public List<MapWallPost> PreManfactureWalls = new List<MapWallPost>();

        [XmlArray(NodesNames.WALL_HEADER_LIST)]
        [XmlArrayItem(NodesNames.WALL_HEADER, typeof(MapWallHeader))]
        public List<MapWallHeader> Headers = new List<MapWallHeader>();

        [XmlArray(NodesNames.WALL_STEEL_HEADER_LIST)]
        [XmlArrayItem(NodesNames.WALL_HEADER, typeof(MapWallHeader))]
        public List<MapWallHeader> SteelHeaders = new List<MapWallHeader>();

        private double maxPostWidth = 0;
        internal override Element SetData(Level level)
        {
            try
            {
                List<Element> elements = new List<Element>();

                List<XYZ> topProfile;
                elements.AddRange(GeneralCreator.CreateWallTopPlate(Events.m_doc, TopProfile.ConvertToXYZList(),
                  TopPlatesCount, (float)WallDepth, (float)TopPlatesDepth, out topProfile));

                List<XYZ> bottomProfile;
                elements.AddRange(GeneralCreator.CreateWallBottomPlate(Events.m_doc, BottomProfile.ConvertToXYZList(),
                   BottomPlatesCount, (float)WallDepth, (float)BottomPlatesDepth, out bottomProfile));

                XYZ walldirection = (bottomProfile.Last() - bottomProfile.First()).Normalize();
                Dictionary<Element, XYZ> postsLocationDictionary = new Dictionary<Element, XYZ>();

                foreach (var post in Posts)
                {
                    elements.Add(AddWallPost(post, Events.m_doc, bottomProfile[0], walldirection, "EFWallPost", WallDepth, postsLocationDictionary));
                }
                foreach (var stud in Studs)
                {
                    elements.Add(AddWallPost(stud, Events.m_doc, bottomProfile[0], walldirection, "EFWallStud", WallDepth, postsLocationDictionary));
                }
                foreach (var preManfactureWall in PreManfactureWalls)
                {
                    elements.Add(AddWallPost(preManfactureWall, Events.m_doc, bottomProfile[0], walldirection, "EFPreManfactureWall", WallDepth, postsLocationDictionary));
                }
                foreach (var header in Headers)
                {
                    elements.Add(GeneralCreator.CreateWallOpeningBeams(Events.m_doc, level, header.Line.ConvertToLine(), WallDepth, header.Depth));
                }
                foreach (var header in SteelHeaders)
                {
                    elements.Add(GeneralCreator.CreateWallSteelStruts(Events.m_doc, level, header.Line.ConvertToLine(), WallDepth));
                }

                List<Element> voidTopPlates = GeneralCreator.CreateWallTopVoidPlate(Events.m_doc, topProfile, (float)WallDepth, (float)maxPostWidth);
                if (voidTopPlates.Count == topProfile.Count - 1)
                {
                    for (int i = 0; i < topProfile.Count - 1; i++)
                    {
                        if (voidTopPlates[i] == null)
                            continue;

                        XYZ p1 = new XYZ(topProfile[i].X, topProfile[i].Y, 0.0);
                        XYZ p2 = new XYZ(topProfile[i + 1].X, topProfile[i + 1].Y, 0.0);
                        List<Element> CutElements = postsLocationDictionary.Where(x => GeometryMathatics.IsPointOnLineSegment(new XYZ(x.Value.X, x.Value.Y, 0.0), p1, p2)).Select(x => x.Key).ToList();
                        foreach (var e in CutElements)
                        {
                            if (e == null)
                                continue;
                            if (InstanceVoidCutUtils.CanBeCutWithVoid(e))
                            {
                                InstanceVoidCutUtils.AddInstanceVoidCut(Events.m_doc, e, voidTopPlates[i]);
                            }
                        }

                    }

                }
                return GeneralCreator.CreateTheGroup(Events.m_doc, elements.Where(x => x != null).Select(x => x.Id).ToList(), Name, level);
            }
            catch (Exception e)
            {
                return null;
            }

        }
        private Element AddWallPost(MapWallPost post, Document doc, XYZ wallStart, XYZ walldirection, string familyName, double WallDepth, Dictionary<Element, XYZ> postsLocationDictionary = null)
        {
            try
            {
                if (post.InWallWidth > maxPostWidth)
                { maxPostWidth = post.InWallWidth; }
                XYZ position = wallStart + post.InWallDistance * walldirection;
                Element e = GeneralCreator.CreatePost(doc, familyName, post.Name, new MapRecSection() { Width = post.InWallWidth, Depth = WallDepth, Plies = 1 },
                    post.MinZ, post.MaxZ, position, walldirection, false);
                if (postsLocationDictionary != null)
                    postsLocationDictionary.Add(e, position);
                return e;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
    [XmlType(NodesNames.POST)]
    public class MapPost : MapObject
    {
        [XmlElement(NodesNames.POINT_LOCATION)]
        public PointLocation PointLocation { get; set; }

        [XmlElement(NodesNames.MIN_Z)]
        public double MinZ { get; set; }

        [XmlElement(NodesNames.MAX_Z)]
        public double MaxZ { get; set; }

        private bool _onFoundation;

        [XmlElement(NodesNames.ON_FOUNDATION)]
        public string OnFoundation
        {
            get { return _onFoundation.ToString(); ; }
            set { _onFoundation = Convert.ToBoolean(value); }
        }

        [XmlElement(NodesNames.NAME)]
        public string Name { get; set; }

        [XmlElement(NodesNames.SECTION)]
        public MapRecSection Section { get; set; }

        internal override Element SetData(Level level)
        {
            try
            {
                return GeneralCreator.CreatePost(Events.m_doc, "EFPost", Name, Section, MinZ, MaxZ, PointLocation.Position.GetReference(), PointLocation.Direction.GetReference(), _onFoundation);
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
    [XmlType(NodesNames.BEAM)]
    public class MapBeam : MapObject
    {
        [XmlElement(NodesNames.LINE)]
        public MapLine Line { get; set; }

        [XmlElement(NodesNames.SECTION, typeof(MapRecSection))]
        [XmlElement(NodesNames.ISECTION, typeof(MapISection))]
        public MapSection Section { get; set; }
        internal override Element SetData(Level level)
        {
            try
            {
                Line line = Line.ConvertToLine();
                XYZ p1 = line.GetEndPoint(0);
                XYZ p2 = line.GetEndPoint(1);

                if (Math.Abs(p1.Z - p2.Z) < 0.0001)
                {
                    return GeneralCreator.CreateHorizontalBeam(Events.m_doc, level, line, Section, Section is MapISection ? "EFHorizontal_Ijoist" : "EFBeam");
                }
                else
                {
                    return GeneralCreator.CreateInclinedBeam(Events.m_doc, level, line, Section, Section is MapISection ? "EFIjoist" : "EFRidgeBeam");
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
    [XmlType(NodesNames.RAFTER)]
    public class MapRafter : MapBeam
    {
        internal override Element SetData(Level level)
        {
            try
            {
                return GeneralCreator.CreateInclinedBeam(Events.m_doc, level, Line.ConvertToLine(), Section, Section is MapISection ? "EFIjoist" : "EFRafter");
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
    [XmlType(NodesNames.SLAB)]
    public class MapSlab : MapObject
    {
        [XmlElement(NodesNames.PROFILE)]
        public MapProfile Profile { get; set; }

        [XmlArray(NodesNames.OPENING_LIST)]
        [XmlArrayItem(NodesNames.PROFILE, typeof(MapProfile))]
        public List<MapProfile> Openings { get; set; }

        [XmlElement(NodesNames.SLAB_THICKNESS)]
        public double Thickness { get; set; }
        internal override Element SetData(Level level)
        {
            try
            {
                return GeneralCreator.CreateSlab(Events.m_doc, level, Profile.ConvertToXYZList(),
                    Openings.Select(x => x.ConvertToXYZList()).ToList(), Thickness);
            }
            catch (Exception e)
            {
                return null;
            }
        }

    }
    public class MapFramingArea : MapObject
    {
        [XmlElement(NodesNames.SECTION, typeof(MapRecSection))]
        [XmlElement(NodesNames.ISECTION, typeof(MapISection))]
        public MapSection Section { get; set; }

        [XmlElement(NodesNames.NAME)]
        public string Name { get; set; }

        [XmlElement(NodesNames.PROFILE)]
        public MapProfile VertexList { get; set; }

        [XmlArray(NodesNames.OPENING_LIST)]
        [XmlArrayItem(NodesNames.PROFILE, typeof(MapProfile))]
        public List<MapProfile> OpeningList { get; set; } = new List<MapProfile>();

        [XmlArray(NodesNames.MEMBERS_LINES)]
        [XmlArrayItem(NodesNames.LINE, typeof(MapLine))]
        public List<MapLine> MembersLines { get; set; } = new List<MapLine>();

        [XmlArray(NodesNames.RIM_LIST)]
        [XmlArrayItem(NodesNames.RIM, typeof(MapBeam))]
        public List<MapBeam> RimBeams { get; set; } = new List<MapBeam>();

        [XmlArray(NodesNames.BLOCK_LIST)]
        [XmlArrayItem(NodesNames.BLOCK, typeof(MapBeam))]
        public List<MapBeam> BlockBeams { get; set; } = new List<MapBeam>();

    }

    [XmlType(NodesNames.JOIST_AREA)]
    public class MapJoistArea : MapFramingArea
    {
        internal override Element SetData(Level level)
        {
            try
            {

                return GeneralCreator.CreateJoistArea(Events.m_doc, level, Name, VertexList.ConvertToXYZList(),
                    OpeningList.Select(x => x.ConvertToXYZList()).ToList(), Section, MembersLines.Select(x => x.ConvertToLine()).ToList()
                    , RimBeams, BlockBeams);
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }

    [XmlType(NodesNames.RAFTER_AREA)]
    public class MapRafterArea : MapFramingArea
    {
        internal override Element SetData(Level level)
        {
            try
            {

                return GeneralCreator.CreateRafterArea(Events.m_doc, level, Name, VertexList.ConvertToXYZList(),
                    OpeningList.Select(x => x.ConvertToXYZList()).ToList(), Section, MembersLines.Select(x => x.ConvertToLine()).ToList()
                    , RimBeams, BlockBeams);
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }

    [XmlType(NodesNames.PLANES_GENERIC_ROOF)]
    public class MapGenericRoof : MapObject
    {
        [XmlElement(NodesNames.POINT_LOCATION)]
        public PointLocation PointLocation { get; set; }

        [XmlElement(NodesNames.NAME)]
        public string Name { get; set; }

        internal override Element SetData(Level level)
        {
            try
            {
                if (string.IsNullOrEmpty(Name))
                    return null;

                MapRoofFamily fam = Floor.Building.Families.Where(x => x is MapRoofFamily).Cast<MapRoofFamily>().FirstOrDefault(x => x.Name == Name);
                if (fam == null)
                    return null;

                XYZ basePoint = PointLocation.Position.GetReference();
                var element = GeneralCreator.CreateInstanceFromFamily(Events.m_doc, basePoint, Name, fam.GetSupFolderPathName(), Autodesk.Revit.DB.Structure.StructuralType.NonStructural);


                return element;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }

    [XmlType(NodesNames.BASE_POLYGON_ROOF)]
    public class MapBasePolygonRoof : MapObject
    {
        [XmlElement(NodesNames.OFFSET)]
        public double Offset { get; set; }

        [XmlElement(NodesNames.PROFILE)]
        public MapProfile Profile { get; set; }

        [XmlArray(NodesNames.ROOF_BASE_LINE_LIST)]
        [XmlArrayItem(NodesNames.ROOF_BASE_LINE, typeof(MapRoofBaseLine))]
        public List<MapRoofBaseLine> RoofLines { get; set; } = new List<MapRoofBaseLine>();

        [XmlArray(NodesNames.OPENING_LIST)]
        [XmlArrayItem(NodesNames.PROFILE, typeof(MapProfile))]
        public List<MapProfile> openingList { get; set; } = new List<MapProfile>();

        [XmlArray(NodesNames.SUB_ROOF_LIST)]
        [XmlArrayItem(NodesNames.BASE_POLYGON_ROOF, typeof(MapBasePolygonRoof))]
        public List<MapBasePolygonRoof> SupRoofList { get; set; } = new List<MapBasePolygonRoof>();


        internal override Element SetData(Level level)
        {
            try
            {
                Element mainRoof = RoofGenerator.CreateRoof(Events.m_doc, level, Offset, Profile.ConvertToXYZList()
                    , openingList.Select(x => x.ConvertToXYZList()).ToList(), RoofLines);
                SupRoofList.ForEach(x => x.SetData(level));
                return mainRoof;
            }
            catch (Exception e)
            {
                return null;
            }
        }


    }

    [XmlType(NodesNames.ROOF_BASE_LINE)]
    public class MapRoofBaseLine
    {
        [XmlElement(NodesNames.OVERHANG)]
        public double OverHang { get; set; }
        [XmlElement(NodesNames.PITCH)]
        public double Slope { get; set; }
        [XmlElement(NodesNames.OFFSET)]
        public double Offset { get; set; }
        [XmlElement(NodesNames.GABLE)]
        public string IsGable
        {
            get { return Gable.ToString(); }
            set { Gable = Convert.ToBoolean(value); }
        }
        public bool Gable { get; set; }
    }

    [XmlInclude(typeof(MapRecSection))
    , XmlInclude(typeof(MapISection))]
    public class MapSection
    {
        [XmlElement(NodesNames.PlIES)]
        public int Plies { get; set; }
        [XmlElement(NodesNames.DEPTH)]
        public double Depth { get; set; }

        internal virtual double GetWidth()
        { return 0; }
    }

    [XmlType(NodesNames.SECTION)]
    public class MapRecSection : MapSection
    {
        [XmlElement(NodesNames.WIDTH)]
        public double Width { get; set; }
        internal override double GetWidth()
        { return Width; }
    }
    [XmlType(NodesNames.ISECTION)]
    public class MapISection : MapSection
    {
        [XmlElement(NodesNames.PLATE_WIDTH)]
        public double Width { get; set; }
        [XmlElement(NodesNames.WEB_WIDTH)]
        public double WebWidth { get; set; }
        [XmlElement(NodesNames.PLATE_DEPTH)]
        public double PlateDepth { get; set; }

        internal override double GetWidth()
        { return Width; }
    }
    public class MapProfile
    {
        [XmlArray(NodesNames.POINT_LIST)]
        [XmlArrayItem(NodesNames.POINT, typeof(MapXYZ))]
        public List<MapXYZ> Points = new List<MapXYZ>();

        [XmlArray(NodesNames.LINE_LIST)]
        [XmlArrayItem(NodesNames.LINE, typeof(MapLine))]
        public List<MapLine> Lines = new List<MapLine>();

        internal List<XYZ> ConvertToXYZList()
        {
            return Points.Select(x => x.GetReference()).ToList();
        }
        internal List<Line> ConvertToLineList()
        {
            List<Line> lines = new List<Line>();
            try
            {
                List<XYZ> points = ConvertToXYZList();
                if (points.Count < 2)
                {
                }
                else if (points.Count == 2)
                {
                    lines.Add(Line.CreateBound(points[0], points[1]));
                }
                else
                {
                    for (int i = 0; i < points.Count; i++)
                    {
                        int j = (i + 1) % points.Count;
                        lines.Add(Line.CreateBound(points[i], points[j]));
                    }
                }

                return lines;
            }
            catch (Exception e)
            {
                return lines;
            }
        }
        internal List<Line> ConvertToSurfaceLineList()
        {
            List<Line> lines = new List<Line>();
            try
            {
                for (int i = 0; i < Lines.Count; i++)
                {
                    XYZ P1 = new XYZ(Lines[i].P1.X, Lines[i].P1.Y, Lines[i].P1.Z);
                    XYZ P2 = new XYZ(Lines[i].P2.X, Lines[i].P2.Y, Lines[i].P2.Z);
                    if (P1.DistanceTo(P2) == 0 && (i == 0 || i == Lines.Count - 1))
                    {
                        var _line = Lines[1].ConvertToLine();
                        lines.Add(Line.CreateBound(P1, P1 + _line.Direction.Normalize() * 0.0026)); // 0.0026 is the shortest length for revit tolerance
                    }
                    else if (P1.DistanceTo(P2) < 0.0026)
                        continue;

                    else
                        lines.Add(Line.CreateBound(P1, P2));

                }

                return lines;
            }
            catch (Exception e)
            {
                return lines;
            }
        }
    }
    public class MapLine
    {
        [XmlElement(NodesNames.P1)]
        public MapXYZ P1 { get; set; }
        [XmlElement(NodesNames.P2)]
        public MapXYZ P2 { get; set; }

        internal Line ConvertToLine()
        {
            try
            {
                return Line.CreateBound(P1.GetReference(), P2.GetReference());
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
    public class PointLocation
    {
        [XmlElement(NodesNames.POSITION)]
        public MapXYZ Position { get; set; }
        [XmlElement(NodesNames.DIRECTION)]
        public MapXYZ Direction { get; set; }
    }
    public class MapXYZ
    {
        [XmlElement(NodesNames.Z)]
        public double Z { get; set; }
        [XmlElement(NodesNames.Y)]
        public double Y { get; set; }
        [XmlElement(NodesNames.X)]
        public double X { get; set; }

        //private XYZ _reference;
        public XYZ GetReference()
        {
            return new XYZ(X, Y, Z);
        }
    }
    public enum WallOpeningType
    {
        Generic,
        Door,
        Window,
        Arch,
        GarageDoor
    }
    [XmlType(NodesNames.WALLOPENING)]
    public class MapShellOpening : MapObject
    {
        [XmlElement(NodesNames.StartPoint)]
        public MapXYZ startpoint { get; set; }
        [XmlElement(NodesNames.EndPoint)]
        public MapXYZ endpoint { get; set; }
        [XmlElement(NodesNames.HEADHEIGHT)]
        public double HeadHeight { get; set; }
        [XmlElement(NodesNames.OpeningHeight)]
        public double OpeningHeight { get; set; }
        [XmlElement(NodesNames.SillHeight)]
        public double SillHeight { get; set; }
        [XmlElement(NodesNames.OpeningType)]
        public WallOpeningType OpeningType { get; set; }
        public MapShellWall ParentWall { get; set; }

    }
    [XmlType(NodesNames.ShellWall)]
    public class MapShellWall : MapObject
    {
        //[XmlArray(NodesNames.ShellWall)]
        //[XmlArrayItem(NodesNames.ShellWall, typeof(MapShellWall))]

        [XmlElement(NodesNames.NAME)]
        public string Name { get; set; }

        [XmlElement(NodesNames.StartPoint)]
        public MapXYZ start { get; set; }

        [XmlElement(NodesNames.EndPoint)]
        public MapXYZ end { get; set; }

        [XmlElement(NodesNames.Height)]
        public double height { get; set; }

        [XmlElement(NodesNames.Thickness)]
        public double thickness { get; set; }
        [XmlElement(NodesNames.BOTTOM_OFFSET)]
        public double bottomoffset { get; set; }
        public List<MapXYZ> geom2d { get; set; }

        [XmlArray(NodesNames.WALLOPENINGLIST)]
        [XmlArrayItem(NodesNames.WALLOPENING, typeof(MapShellOpening))]
        public List<MapShellOpening> mapShellOpenings = new List<MapShellOpening>();



        //public bool _onFoundation;
        //[XmlElement(NodesNames.ON_FOUNDATION)]
        //public bool OnFoundation
        //{
        //    get { return _onFoundation ; }
        //    set { _onFoundation = Convert.ToBoolean(value); }
        //}

        internal override Element SetData(Level level)
        {
            try
            {


                return GeneralCreator.CreateWall(Events.m_doc, level, start, end, height, thickness/12, bottomoffset, mapShellOpenings);



                //    //return GeneralCreator.CreateTheGroup(Events.m_doc, elements.Where(x => x != null).Select(x => x.Id).ToList(), Name, level);
            }
            catch (Exception e)
            {
                return null;
            }

        }
        //internal override void CreateCustomFamily(Document doc)
        //{

        //    Dictionary<List<XYZ>, double> dict = new Dictionary<List<XYZ>, double>();
        //    _subFolderPath = GeneralCreator.CreateShellWallFamily(doc, ref _name, dict);




        //}



    }

    [XmlType(NodesNames.SHELL_PLANES_GENERIC_ROOF)]
    public class MapShellGenericRoof : MapObject
    {
        [XmlElement(NodesNames.POINT_LOCATION)]
        public PointLocation PointLocation { get; set; }

        [XmlElement(NodesNames.NAME)]
        public string Name { get; set; }

        internal override Element SetData(Level level)
        {
            try
            {
                if (string.IsNullOrEmpty(Name))
                    return null;

                MapShellRoofFamily fam = Floor.Building.Families.Where(x => x is MapShellRoofFamily).Cast<MapShellRoofFamily>().FirstOrDefault(x => x.Name == Name);
                if (fam == null)
                    return null;

                XYZ basePoint = PointLocation.Position.GetReference();
                var element = GeneralCreator.CreateInstanceFromFamily(Events.m_doc, basePoint, Name, fam.GetSupFolderPathName(), Autodesk.Revit.DB.Structure.StructuralType.NonStructural);


                return element;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }


    [XmlType(NodesNames.ROOFSlab)]

    public class MapShellcRoofSlab : MapObject
    {


        [XmlElement(NodesNames.ROOFSlab)]
        public string Name { get; set; }
        [XmlElement(NodesNames.SLAB_THICKNESS)]
        public double thickness { get; set; }
        [XmlArray(NodesNames.VertexList)]
        [XmlArrayItem(NodesNames.POINT, typeof(MapXYZ))]
        public List<MapXYZ> BASEPLYGON { get; set; }


        internal override Element SetData(Level level)

        {

            try
            {

                return GeneralCreator.CreateRoofSlabs(Events.m_doc, level, BASEPLYGON, thickness);





            }
            catch (Exception e)
            {
                return null;
            }



            //}
        }
    }
    [XmlType(NodesNames.SHELL_BASE_POLYGON_ROOF)]
    public class MapShellBasePolygonRoof : MapObject
    {
        [XmlElement(NodesNames.OFFSET)]
        public double Offset { get; set; }

        [XmlElement(NodesNames.PROFILE)]
        public MapProfile Profile { get; set; }

        [XmlArray(NodesNames.ROOF_BASE_LINE_LIST)]
        [XmlArrayItem(NodesNames.ROOF_BASE_LINE, typeof(MapRoofBaseLine))]
        public List<MapRoofBaseLine> RoofLines { get; set; } = new List<MapRoofBaseLine>();

        [XmlArray(NodesNames.OPENING_LIST)]
        [XmlArrayItem(NodesNames.PROFILE, typeof(MapProfile))]
        public List<MapProfile> openingList { get; set; } = new List<MapProfile>();

        [XmlArray(NodesNames.SUB_ROOF_LIST)]
        [XmlArrayItem(NodesNames.BASE_POLYGON_ROOF, typeof(MapShellBasePolygonRoof))]
        public List<MapShellBasePolygonRoof> SupRoofList { get; set; } = new List<MapShellBasePolygonRoof>();


        internal override Element SetData(Level level)
        {
            try
            {
                Element mainRoof = RoofGenerator.CreateRoof(Events.m_doc, level, Offset, Profile.ConvertToXYZList()
                    , openingList.Select(x => x.ConvertToXYZList()).ToList(), RoofLines);
                SupRoofList.ForEach(x => x.SetData(level));
                return mainRoof;
            }
            catch (Exception e)
            {
                return null;
            }
        }


    }

    //[XmlType(NodesNames.ROOF_BASE_LINE)]
    //public class MapShellRoofBaseLine
    //{
    //    [XmlElement(NodesNames.OVERHANG)]
    //    public double OverHang { get; set; }
    //    [XmlElement(NodesNames.PITCH)]
    //    public double Slope { get; set; }
    //    [XmlElement(NodesNames.OFFSET)]
    //    public double Offset { get; set; }
    //    [XmlElement(NodesNames.GABLE)]
    //    public string IsGable
    //    {
    //        get { return Gable.ToString(); }
    //        set { Gable = Convert.ToBoolean(value); }
    //    }
    //    public bool Gable { get; set; }
    //}

    [XmlType(NodesNames.CeilingSubstrate)]
    public class MapShellCeilingSubstrate : MapObject
    {
        [XmlArray(NodesNames.VertexList)]
        [XmlArrayItem(NodesNames.POINT, typeof(MapXYZ))]
        public List<MapXYZ> BASEPLYGON { get; set; }
        [XmlArray(NodesNames.HoleList)]
        [XmlArrayItem(NodesNames.HOLE, typeof(MapShellHoles))]
        public List<MapShellHoles> holes { get; set; }
        [XmlElement(NodesNames.SLAB_HEIGHT)]
        public double height { get; set; }
        [XmlElement(NodesNames.SLAB_THICKNESS)]
        public double thickness { get; set; }


        internal override Element SetData(Level level)

        {

            try
            {

                return efrvt.CreateCeilingSubstrate(Events.m_doc, level, BASEPLYGON, height, thickness, holes);




                //return GeneralCreator.CreateTheGroup(Events.m_doc, elements.Where(x => x != null).Select(x => x.Id).ToList(), Name, level);
            }
            catch (Exception e)
            {
                return null;
            }



            //}
        }
    }
    [XmlType(NodesNames.CeilingSlab)]
    public class MapShellCeilingSlab : MapObject
    {
        [XmlArray(NodesNames.VertexList)]
        [XmlArrayItem(NodesNames.POINT, typeof(MapXYZ))]
        public List<MapXYZ> BASEPLYGON { get; set; }


        [XmlArray(NodesNames.HoleList)]
        [XmlArrayItem(NodesNames.HOLE, typeof(MapShellHoles))]
        public List<MapShellHoles> holes { get; set; }
        [XmlElement(NodesNames.SLAB_HEIGHT)]
        public double height { get; set; }
        [XmlElement(NodesNames.SLAB_THICKNESS)]
        public double thickness { get; set; }


        internal override Element SetData(Level level)

        {

            try
            {


                return efrvt. CreateCeilingSlabs(Events.m_doc, level, BASEPLYGON, height, thickness);




            }
            catch (Exception e)
            {
                return null;
            }



        }
    }
    [XmlType(NodesNames.HOLE)]
    public class MapShellHoles : MapObject

    {

        [XmlArray(NodesNames.POINT_LIST)]
        [XmlArrayItem(NodesNames.POINT, typeof(MapXYZ))]
        public List<MapXYZ> points = new List<MapXYZ>();
        //public MapShellFloor floor { get; set; }
        public MapFloor floor { get; set; }
    }


    [XmlType(NodesNames.FLOORSLAB)]
    public class MapShellFloorSlab : MapObject
    {
        [XmlArray(NodesNames.VertexList)]
        [XmlArrayItem(NodesNames.POINT, typeof(MapXYZ))]
        public List<MapXYZ> BASEPLYGON { get; set; }



        [XmlArray(NodesNames.HoleList)]
        [XmlArrayItem(NodesNames.HOLE, typeof(MapShellHoles))]
        public List<MapShellHoles> holes { get; set; }

        [XmlElement(NodesNames.SLAB_OFFSET)]
        public double offset { get; set; }
        [XmlElement(NodesNames.SLAB_THICKNESS)]
        public double thickness { get; set; }


        internal override Element SetData(Level level)
        {
            try
            {
                //if (holes.Count == 0)

                //{

                //if (holes.Count != 0)

                //{

                //    return efrvt.CreateOpening(Events.m_doc, level, BASEPLYGON, offset, thickness, holes);

                //}

                return efrvt.CreateFloorSlabs(Events.m_doc, level, BASEPLYGON, offset, thickness, holes);

                //}
                //else
                //    return null;

                //return null;
                //return GeneralCreator.CreateTheGroup(Events.m_doc, elements.Where(x => x != null).Select(x => x.Id).ToList(), Name, level);
            }
            catch (Exception e)
            {
                return null;
            }

        }
        //internal override Element SetData(Level level)
        //{
        //    try
        //    {
        //        return GeneralCreator.CreateFoundation(Events.m_doc, level, Location.Position.GetReference(), Location.Direction.GetReference()
        //            , Width, Length, Depth);
        //    }
        //    catch (Exception e)
        //    {
        //        return null;
        //    }
        //}
    }
}
