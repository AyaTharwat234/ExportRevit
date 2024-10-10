using Autodesk.Revit.DB;
using EFRvt.Revit;
using Elibre.Net.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace EFRvt
{
    [XmlType(NodesNames.FLOOR_LEVELS)]
    public class FloorLevels
    {
        [XmlElement(NodesNames.FLOOR_LEVEL)]
        public double? FloorLevel { get; set; } = null;

        [XmlElement(NodesNames.FLOOR_TOP_PLATE_LEVEL)]
        public double? FloorTopPlateLevel { get; set; } = null;

        [XmlElement(NodesNames.FLOOR_SUB_LEVEL)]
        public double? FloorSubPlateLevel { get; set; } = null;

    }
    [XmlType(NodesNames.FLOOR)]
    public class MapFloor
    {
        [XmlElement(NodesNames.BUILDING)]
        public MapBuilding Building { get; set; }

        [XmlElement(NodesNames.NAME)]
        public string FloorName { get; set; }

        [XmlElement(NodesNames.FLOOR_LEVELS)]
        public FloorLevels Levels { get; set; }

        [XmlArray(NodesNames.FLOOR_OBJECTS)]
        [XmlArrayItem(NodesNames.BEAM, typeof(MapBeam))]
        [XmlArrayItem(NodesNames.POST, typeof(MapPost))]
        [XmlArrayItem(NodesNames.PRE_MANFACTURED_WALL, typeof(MapPreManfactureWall))]
        [XmlArrayItem(NodesNames.RAFTER, typeof(MapRafter))]
        [XmlArrayItem(NodesNames.JOIST_AREA, typeof(MapJoistArea))]
        [XmlArrayItem(NodesNames.RAFTER_AREA, typeof(MapRafterArea))]
        [XmlArrayItem(NodesNames.FOUNDATION, typeof(MapFoundation))]
        [XmlArrayItem(NodesNames.GENERIC_GIRDER_TRUSS, typeof(MapGenericGirderTruss))]
        [XmlArrayItem(NodesNames.GENERIC_TRUSS, typeof(MapGenericTruss))]
        [XmlArrayItem(NodesNames.TRUSS_AREA, typeof(MapTrussArea))]
        [XmlArrayItem(NodesNames.WALL, typeof(MapWall))]
        [XmlArrayItem(NodesNames.PLANES_GENERIC_ROOF, typeof(MapGenericRoof))]
        [XmlArrayItem(NodesNames.BASE_POLYGON_ROOF, typeof(MapBasePolygonRoof))]
        [XmlArrayItem(NodesNames.SLAB, typeof(MapSlab))]

        [XmlArrayItem(NodesNames.ShellWall, typeof(MapShellWall))]
        [XmlArrayItem(NodesNames.FLOORSLAB, typeof(MapShellFloorSlab))]
        [XmlArrayItem(NodesNames.ROOFSlab, typeof(MapShellcRoofSlab))]
        [XmlArrayItem(NodesNames.CeilingSlab, typeof(MapShellCeilingSlab))]
        [XmlArrayItem(NodesNames.CeilingSubstrate, typeof(MapShellCeilingSubstrate))]
        [XmlArrayItem(NodesNames.SHELL_PLANES_GENERIC_ROOF, typeof(MapShellGenericRoof))]
        [XmlArrayItem(NodesNames.SHELL_BASE_POLYGON_ROOF, typeof(MapShellBasePolygonRoof))]
        public List<MapObject> FloorObjects { get; set; } = new List<MapObject>();

        internal bool SetData()
        {
            try
            {
                //string name = XMLParser.ReadXmlNode(floorNode, NodesNames.FLOOR_NAME , "");
                //double elevation = Convert.ToDouble(XMLParser.ReadXmlNode(floorNode, NodesNames.FLOOR_LEVEL, 0.0));

                Level level = null;
                if (this.Levels != null)
                {
                    if (Levels.FloorLevel != null)
                    {
                        level = GeneralCreator.CreateLevel(Events.m_doc, Levels.FloorLevel.Value, FloorName);
                    }
                    if (Levels.FloorTopPlateLevel != null)
                    {
                        level = GeneralCreator.CreateLevel(Events.m_doc, Levels.FloorTopPlateLevel.Value, FloorName + "-Top Plate");
                    }
                    if (Levels.FloorSubPlateLevel != null)
                    {
                        level = GeneralCreator.CreateLevel(Events.m_doc, Levels.FloorSubPlateLevel.Value, FloorName + "-Sub Plate");
                    }

                }

                if (level == null)
                    return false;

                foreach (var obj in FloorObjects)
                {
                    if (obj == null)
                        continue;

                    obj.Floor = this;
                    obj.SetData(level);
                }

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
