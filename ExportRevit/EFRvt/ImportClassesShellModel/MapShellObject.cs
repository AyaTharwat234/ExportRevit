using Autodesk.Revit.DB;
using EFRvt;
using EFRvt.Revit;
using Elibre.Net.Geometry;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace EFRvt
{
    public class MapShellObject
    {
        //        public MapShellFloor Floor { get; set; }
        //        //internal virtual bool SetData(Level level)
        //        //{
        //        //    return true;
        //        //}
        //        internal virtual Element SetData(Level level)
        //        {
        //            return null;
        //        }
        //    }

        //    public enum WallOpeningType
        //    {
        //        Generic,
        //        Door,
        //        Window,
        //        Arch,
        //        GarageDoor
        //    }
        //    [XmlType(NodesNames.WALLOPENING)]
        //    public class MapShellOpening : MapShellObject
        //    {
        //        [XmlElement(NodesNames.StartPoint)]
        //        public MapXYZ startpoint { get; set; }
        //        [XmlElement(NodesNames.EndPoint)]
        //        public MapXYZ endpoint { get; set; }
        //        [XmlElement(NodesNames.HEADHEIGHT)]
        //        public double HeadHeight { get; set; }
        //        [XmlElement(NodesNames.OpeningHeight)]
        //        public double OpeningHeight { get; set; }
        //        [XmlElement(NodesNames.SillHeight)]
        //        public double SillHeight { get; set; }
        //        [XmlElement(NodesNames.OpeningType)]
        //        public WallOpeningType OpeningType { get; set; }
        //        public MapShellWall ParentWall { get; set; }

        //    }
        //    [XmlType(NodesNames.ShellWall)]
        //    public class MapShellWall : MapShellObject
        //    {
        //        //[XmlArray(NodesNames.ShellWall)]
        //        //[XmlArrayItem(NodesNames.ShellWall, typeof(MapShellWall))]

        //        [XmlElement(NodesNames.NAME)]
        //        public string Name { get; set; }

        //        [XmlElement(NodesNames.StartPoint)]
        //        public MapXYZ start { get; set; }

        //        [XmlElement(NodesNames.EndPoint)]
        //        public MapXYZ end { get; set; }

        //        [XmlElement(NodesNames.Height)]
        //        public double height { get; set; }

        //        [XmlElement(NodesNames.Thickness)]
        //        public double thickness { get; set; }
        //        [XmlElement(NodesNames.BOTTOM_OFFSET)]
        //        public double bottomoffset { get; set; }
        //        public List<MapXYZ> geom2d { get; set; }

        //        [XmlArray(NodesNames.WALLOPENINGLIST)]
        //        [XmlArrayItem(NodesNames.WALLOPENING, typeof(MapShellOpening))]
        //        public List<MapShellOpening> mapShellOpenings = new List<MapShellOpening>();



        //        //public bool _onFoundation;
        //        //[XmlElement(NodesNames.ON_FOUNDATION)]
        //        //public bool OnFoundation
        //        //{
        //        //    get { return _onFoundation ; }
        //        //    set { _onFoundation = Convert.ToBoolean(value); }
        //        //}

        //        internal override Element SetData(Level level)
        //        {
        //            try
        //            {


        //                return GeneralCreator.CreateWall(Events.m_doc, level, start, end, height, thickness, bottomoffset, mapShellOpenings);



        //                //    //return GeneralCreator.CreateTheGroup(Events.m_doc, elements.Where(x => x != null).Select(x => x.Id).ToList(), Name, level);
        //            }
        //            catch (Exception e)
        //            {
        //                return null;
        //            }

        //        }
        //        //internal override void CreateCustomFamily(Document doc)
        //        //{

        //        //    Dictionary<List<XYZ>, double> dict = new Dictionary<List<XYZ>, double>();
        //        //    _subFolderPath = GeneralCreator.CreateShellWallFamily(doc, ref _name, dict);




        //        //}



        //    }

        //    [XmlType(NodesNames.SHELL_PLANES_GENERIC_ROOF)]
        //    public class MapShellGenericRoof : MapShellObject
        //    {
        //        [XmlElement(NodesNames.POINT_LOCATION)]
        //        public PointLocation PointLocation { get; set; }

        //        [XmlElement(NodesNames.NAME)]
        //        public string Name { get; set; }

        //        internal override Element SetData(Level level)
        //        {
        //            try
        //            {
        //                if (string.IsNullOrEmpty(Name))
        //                    return null;

        //                MapShellRoofFamily fam = Floor.Building.Families.Where(x => x is MapShellRoofFamily).Cast<MapShellRoofFamily>().FirstOrDefault(x => x.Name == Name);
        //                if (fam == null)
        //                    return null;

        //                XYZ basePoint = PointLocation.Position.GetReference();
        //                var element = GeneralCreator.CreateInstanceFromFamily(Events.m_doc, basePoint, Name, fam.GetSupFolderPathName(), Autodesk.Revit.DB.Structure.StructuralType.NonStructural);


        //                return element;
        //            }
        //            catch (Exception e)
        //            {
        //                return null;
        //            }
        //        }
        //    }


        //    [XmlType(NodesNames.ROOFSlab)]

        //    public class MapShellcRoofSlab : MapShellObject
        //    {


        //        [XmlElement(NodesNames.ROOFSlab)]
        //        public string Name { get; set; }
        //        [XmlElement(NodesNames.SLAB_THICKNESS)]
        //        public double thickness { get; set; }
        //        [XmlArray(NodesNames.VertexList)]
        //        [XmlArrayItem(NodesNames.POINT, typeof(MapXYZ))]
        //        public List<MapXYZ> BASEPLYGON { get; set; }


        //        internal override Element SetData(Level level)

        //        {

        //            try
        //            {

        //                return GeneralCreator.CreateRoofSlabs(Events.m_doc, level, BASEPLYGON, thickness);





        //            }
        //            catch (Exception e)
        //            {
        //                return null;
        //            }



        //            //}
        //        }
        //    }
        //    [XmlType(NodesNames.SHELL_BASE_POLYGON_ROOF)]
        //    public class MapShellBasePolygonRoof : MapShellObject
        //    {
        //        [XmlElement(NodesNames.OFFSET)]
        //        public double Offset { get; set; }

        //        [XmlElement(NodesNames.PROFILE)]
        //        public MapProfile Profile { get; set; }

        //        [XmlArray(NodesNames.ROOF_BASE_LINE_LIST)]
        //        [XmlArrayItem(NodesNames.ROOF_BASE_LINE, typeof(MapRoofBaseLine))]
        //        public List<MapRoofBaseLine> RoofLines { get; set; } = new List<MapRoofBaseLine>();

        //        [XmlArray(NodesNames.OPENING_LIST)]
        //        [XmlArrayItem(NodesNames.PROFILE, typeof(MapProfile))]
        //        public List<MapProfile> openingList { get; set; } = new List<MapProfile>();

        //        [XmlArray(NodesNames.SUB_ROOF_LIST)]
        //        [XmlArrayItem(NodesNames.BASE_POLYGON_ROOF, typeof(MapShellBasePolygonRoof))]
        //        public List<MapShellBasePolygonRoof> SupRoofList { get; set; } = new List<MapShellBasePolygonRoof>();


        //        internal override Element SetData(Level level)
        //        {
        //            try
        //            {
        //                Element mainRoof = RoofGenerator.CreateRoof(Events.m_doc, level, Offset, Profile.ConvertToXYZList()
        //                    , openingList.Select(x => x.ConvertToXYZList()).ToList(), RoofLines);
        //                SupRoofList.ForEach(x => x.SetData(level));
        //                return mainRoof;
        //            }
        //            catch (Exception e)
        //            {
        //                return null;
        //            }
        //        }


        //    }

        //    //[XmlType(NodesNames.ROOF_BASE_LINE)]
        //    //public class MapShellRoofBaseLine
        //    //{
        //    //    [XmlElement(NodesNames.OVERHANG)]
        //    //    public double OverHang { get; set; }
        //    //    [XmlElement(NodesNames.PITCH)]
        //    //    public double Slope { get; set; }
        //    //    [XmlElement(NodesNames.OFFSET)]
        //    //    public double Offset { get; set; }
        //    //    [XmlElement(NodesNames.GABLE)]
        //    //    public string IsGable
        //    //    {
        //    //        get { return Gable.ToString(); }
        //    //        set { Gable = Convert.ToBoolean(value); }
        //    //    }
        //    //    public bool Gable { get; set; }
        //    //}

        //    [XmlType(NodesNames.CeilingSubstrate)]
        //    public class MapShellCeilingSubstrate : MapShellObject
        //    {
        //        [XmlArray(NodesNames.VertexList)]
        //        [XmlArrayItem(NodesNames.POINT, typeof(MapXYZ))]
        //        public List<MapXYZ> BASEPLYGON { get; set; }
        //        [XmlArray(NodesNames.HoleList)]
        //        [XmlArrayItem(NodesNames.HOLE, typeof(MapShellHoles))]
        //        public List<MapShellHoles> holes { get; set; }
        //        [XmlElement(NodesNames.SLAB_HEIGHT)]
        //        public double height { get; set; }
        //        [XmlElement(NodesNames.SLAB_THICKNESS)]
        //        public double thickness { get; set; }


        //        internal override Element SetData(Level level)

        //        {

        //            try
        //            {

        //                return efrvt.CreateCeilingSubstrate(Events.m_doc, level, BASEPLYGON, height, thickness, holes);




        //                //return GeneralCreator.CreateTheGroup(Events.m_doc, elements.Where(x => x != null).Select(x => x.Id).ToList(), Name, level);
        //            }
        //            catch (Exception e)
        //            {
        //                return null;
        //            }



        //            //}
        //        }
        //    }
        //    [XmlType(NodesNames.CeilingSlab)]
        //    public class MapShellCeilingSlab : MapShellObject
        //    {
        //        [XmlArray(NodesNames.VertexList)]
        //        [XmlArrayItem(NodesNames.POINT, typeof(MapXYZ))]
        //        public List<MapXYZ> BASEPLYGON { get; set; }


        //        [XmlArray(NodesNames.HoleList)]
        //        [XmlArrayItem(NodesNames.HOLE, typeof(MapShellHoles))]
        //        public List<MapShellHoles> holes { get; set; }
        //        [XmlElement(NodesNames.SLAB_HEIGHT)]
        //        public double height { get; set; }
        //        [XmlElement(NodesNames.SLAB_THICKNESS)]
        //        public double thickness { get; set; }


        //        internal override Element SetData(Level level)

        //        {

        //            try
        //            {


        //                return efrvt.CreateCeilingSlabs(Events.m_doc, level, BASEPLYGON, height, thickness);




        //            }
        //            catch (Exception e)
        //            {
        //                return null;
        //            }



        //        }
        //    }
        //    [XmlType(NodesNames.HOLE)]
        //    public class MapShellHoles : MapShellObject

        //    {

        //        [XmlArray(NodesNames.POINT_LIST)]
        //        [XmlArrayItem(NodesNames.POINT, typeof(MapXYZ))]
        //        public List<MapXYZ> points = new List<MapXYZ>();
        //        public MapShellFloor floor { get; set; }
        //    }


        //    [XmlType(NodesNames.FLOORSLAB)]
        //    public class MapShellFloorSlab : MapShellObject
        //    {
        //        [XmlArray(NodesNames.VertexList)]
        //        [XmlArrayItem(NodesNames.POINT, typeof(MapXYZ))]
        //        public List<MapXYZ> BASEPLYGON { get; set; }



        //        [XmlArray(NodesNames.HoleList)]
        //        [XmlArrayItem(NodesNames.HOLE, typeof(MapShellHoles))]
        //        public List<MapShellHoles> holes { get; set; }

        //        [XmlElement(NodesNames.SLAB_OFFSET)]
        //        public double offset { get; set; }
        //        [XmlElement(NodesNames.SLAB_THICKNESS)]
        //        public double thickness { get; set; }


        //        internal override Element SetData(Level level)
        //        {
        //            try
        //            {
        //                //if (holes.Count == 0)

        //                //{

        //                //if (holes.Count != 0)

        //                //{

        //                //    return efrvt.CreateOpening(Events.m_doc, level, BASEPLYGON, offset, thickness, holes);

        //                //}

        //                return efrvt.CreateFloorSlabs(Events.m_doc, level, BASEPLYGON, offset, thickness, holes);

        //                //}
        //                //else
        //                //    return null;

        //                //return null;
        //                //return GeneralCreator.CreateTheGroup(Events.m_doc, elements.Where(x => x != null).Select(x => x.Id).ToList(), Name, level);
        //            }
        //            catch (Exception e)
        //            {
        //                return null;
        //            }

        //        }
        //        //internal override Element SetData(Level level)
        //        //{
        //        //    try
        //        //    {
        //        //        return GeneralCreator.CreateFoundation(Events.m_doc, level, Location.Position.GetReference(), Location.Direction.GetReference()
        //        //            , Width, Length, Depth);
        //        //    }
        //        //    catch (Exception e)
        //        //    {
        //        //        return null;
        //        //    }
        //        //}
        //    }
        //    [XmlType(NodesNames.FOUNDATION)]
        //    public class MapShellFoundation : MapShellObject
        //    {
        //        [XmlElement(NodesNames.WIDTH)]
        //        public double Width { get; set; }
        //        [XmlElement(NodesNames.LENGTH)]
        //        public double Length { get; set; }
        //        [XmlElement(NodesNames.DEPTH)]
        //        public double Depth { get; set; }
        //        [XmlElement(NodesNames.POINT_LOCATION)]
        //        public PointLocation Location { get; set; }
        //        //internal override Element SetData(Level level)
        //        //{
        //        //    try
        //        //    {
        //        //        return GeneralCreator.CreateFoundation(Events.m_doc, level, Location.Position.GetReference(), Location.Direction.GetReference()
        //        //            , Width, Length, Depth);
        //        //    }
        //        //    catch (Exception e)
        //        //    {
        //        //        return null;
        //        //    }
        //        //}
    }

}