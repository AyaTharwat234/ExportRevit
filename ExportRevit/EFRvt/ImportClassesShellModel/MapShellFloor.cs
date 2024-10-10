using Autodesk.Revit.DB;
using EFRvt.Revit;
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
    public class ShellFloorlevels
    {
        [XmlElement(NodesNames.FLOOR_LEVEL)]
        public double? FloorLevel { get; set; } = null;

        [XmlElement(NodesNames.FLOOR_TOP_PLATE_LEVEL)]
        public double? PlateLevel { get; set; } = null;

        [XmlElement(NodesNames.FLOOR_SUB_LEVEL)]
        public double? SubFloorLevel { get; set; } = null;

    }

    [XmlType(NodesNames.FLOOR)]
    public class MapShellFloor
    {
        [XmlElement(NodesNames.MapShellBuilding)]
        public MapShellBuilding Building { get; set; }

        [XmlElement(NodesNames.NAME)]
        public string FloorName { get; set; }

        [XmlElement(NodesNames.FLOOR_LEVELS)]
        public ShellFloorlevels Levels { get; set; }

        [XmlArray(NodesNames.FLOOR_OBJECTS)]
        [XmlArrayItem(NodesNames.ShellWall, typeof(MapShellWall))]
        [XmlArrayItem(NodesNames.FLOORSLAB, typeof(MapShellFloorSlab))]
        [XmlArrayItem(NodesNames.ROOFSlab, typeof(MapShellcRoofSlab))]
        [XmlArrayItem(NodesNames.CeilingSlab, typeof(MapShellCeilingSlab))]
        [XmlArrayItem(NodesNames.CeilingSubstrate, typeof(MapShellCeilingSubstrate))]
        [XmlArrayItem(NodesNames.SHELL_PLANES_GENERIC_ROOF, typeof(MapShellGenericRoof))]
        [XmlArrayItem(NodesNames.SHELL_BASE_POLYGON_ROOF, typeof(MapShellBasePolygonRoof))]
        //[XmlArrayItem(NodesNames.SLAB, typeof(MapSlab))]
        public List<MapShellObject> FloorObjects { get; set; } = new List<MapShellObject>();

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
                    if (Levels.PlateLevel != null)
                    {
                        GeneralCreator.CreateLevel(Events.m_doc, Levels.PlateLevel.Value, FloorName + "-Top Plate");
                    }
                    if (Levels.SubFloorLevel != null)
                    {
                        GeneralCreator.CreateLevel(Events.m_doc, Levels.SubFloorLevel.Value, FloorName + "-Sub Plate");
                    }

                }

                if (level == null)
                    return false;

                //foreach (var obj in FloorObjects)
                //{
                //    if (obj == null)
                //        continue;

                //    obj.Floor = this;
                //    obj.SetData(level);

                //}
                //GeneralCreator.JoinAndExtendWalls(Events.m_doc);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
