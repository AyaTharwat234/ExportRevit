using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using EFRvt.Revit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace EFRvt
{

    [Serializable, XmlRoot(NodesNames.MapShellBuilding), XmlType("MapShellBuilding")]
    public class MapShellBuilding
    {


        [XmlArray(NodesNames.FLOOR_LIST)]
        [XmlArrayItem(NodesNames.FLOOR)]
        public List<MapShellFloor> Floors { get; set; } = new List<MapShellFloor>();

        [XmlArray(NodesNames.SHELL_GENERIC_FAMILY_LIST)]
        [XmlArrayItem(NodesNames.SHELL_GENERIC_ROOF_FAMILY, typeof(MapShellRoofFamily))]
        public List<GenericShellFamily> Families { get; set; } = new List<GenericShellFamily>();
        //public List<MapShellOpening> Openings { get; set; } = new List<MapShellOpening>();
        //public List<MapShellFloorSlab> FloorSlabs { get; set; } = new List<MapShellFloorSlab>();
        public bool SetData()
        {
            try
            {
         
                // Create a new list to store floors with FloorObjects
                List<MapShellFloor> floorsWithObjects = new List<MapShellFloor>();

                foreach (MapShellFloor floor in Floors)
                {
                    if (floor == null || floor.FloorObjects.Count == 0)
                        continue;

                    // Add floors with FloorObjects to the new list
                    floorsWithObjects.Add(floor);

                    floor.Building = this;
                    if (!floor.SetData())
                        continue;
                }

                // Replace the original list of Floors with the updated list containing only floors with FloorObjects
                Floors = floorsWithObjects;
                GeneralCreator.ClearGenericFamiliesFolder();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        internal async void PrepareRunTimeFamiles(Document doc)
        {
            try
            {
                Families.ForEach(x => x.CreateCustomFamily(doc));
            }
            catch (Exception e)
            {

            }


        }




    }

    public class GenericShellFamily
    {
        protected string _name;
        protected string _subFolderPath;

        internal virtual void CreateCustomFamily(Document doc)
        { }
        internal string GetName() { return _name; }
        internal string GetSupFolderPathName() { return _subFolderPath; }

    }

    [XmlType(NodesNames.SHELL_GENERIC_ROOF_FAMILY)]
    public class MapShellRoofFamily : GenericShellFamily
    {
        [XmlElement(NodesNames.NAME)]
        public string Name { get { return _name; } set { _name = value; } }
        [XmlElement(NodesNames.Thickness)]
        public double thickness { get; set; }
        [XmlArray(NodesNames.ROOF_PLANE_LIST)]
        [XmlArrayItem(NodesNames.PROFILE, typeof(MapProfile))]
        public List<MapProfile> Planes { get; set; }

        [XmlArray(NodesNames.OPENING_LIST)]
        [XmlArrayItem(NodesNames.PROFILE, typeof(MapProfile))]
        public List<MapProfile> openings { get; set; }

        [XmlArray(NodesNames.POPUP_OPENING_LIST)]
        [XmlArrayItem(NodesNames.PROFILE, typeof(MapProfile))]
        public List<MapProfile> PopUpOpenings { get; set; }

        [XmlArray(NodesNames.SURFACE_LIST)]
        [XmlArrayItem(NodesNames.PROFILE, typeof(MapProfile))]
        public List<MapProfile> Surfaces { get; set; }

        internal override void CreateCustomFamily(Document doc)
        {
            try
            {
                _subFolderPath = GeneralCreator.CreateRoofByPlanes(doc, thickness, ref _name, Planes.Select(x => x.ConvertToXYZList()).ToList(), openings.Select(x => x.ConvertToXYZList()).ToList()
                    , PopUpOpenings.Select(x => x.ConvertToXYZList()).ToList(), Surfaces.Select(x => x.ConvertToSurfaceLineList()).ToList());
            }
            catch (Exception e)
            {

            }
        }

    }
}
