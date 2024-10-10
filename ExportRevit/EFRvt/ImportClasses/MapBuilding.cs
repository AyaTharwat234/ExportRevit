using Autodesk.Revit.DB;
using EFRvt.Revit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace EFRvt
{
    [Serializable, XmlRoot(NodesNames.BUILDING), XmlType("BUILDING")]

    public class MapBuilding
    {
        [XmlArray(NodesNames.FLOOR_LIST)]
        [XmlArrayItem(NodesNames.FLOOR)]
        public List<MapFloor> Floors { get; set; } = new List<MapFloor>();

        [XmlArray(NodesNames.GENERIC_FAMILY_LIST)]
        [XmlArrayItem(NodesNames.GENERIC_ROOF_FAMILY, typeof(MapRoofFamily))]
        [XmlArrayItem(NodesNames.GENERIC_TRUSS_FAMILY, typeof(MapTrussFamily))]
        public List<GenericFamily> Families { get; set; } = new List<GenericFamily>();

        //public bool SetData()
        //{
        //    try
        //    {
        //        foreach (MapFloor floor in Floors)
        //        {
        //            if (floor == null)
        //                continue;
        //            if (floor.FloorObjects == null)
        //                continue;
        //            floor.Building = this;
        //            if (!floor.SetData())
        //                continue;
        //        }

        //        GeneralCreator.ClearGenericFamiliesFolder();
        //        return true;
        //    }
        //    catch (Exception e)
        //    {
        //        return false;
        //    }
        //}
        public bool SetData()
        {
            try
            {
                // Create a new list to store floors with FloorObjects
                List<MapFloor> floorsWithObjects = new List<MapFloor>();

                foreach (MapFloor floor in Floors)
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

    public class GenericFamily
    {
        protected string _name;
        protected string _subFolderPath;

        internal virtual void CreateCustomFamily(Document doc)
        { }
        internal string GetName() { return _name; }
        internal string GetSupFolderPathName() { return _subFolderPath; }

    }

    [XmlType(NodesNames.GENERIC_ROOF_FAMILY)]
    public class MapRoofFamily : GenericFamily
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

    [XmlType(NodesNames.GENERIC_TRUSS_FAMILY)]
    public class MapTrussFamily : GenericFamily
    {

        [XmlElement(NodesNames.NAME)]
        public string Name { get { return _name; } set { _name = value; } }


        [XmlArray(NodesNames.GENERIC_TRUSS_MEMBER_LIST)]
        [XmlArrayItem(NodesNames.GENERIC_TRUSS_MEMBER, typeof(MapGenericTrussMember))]
        public List<MapGenericTrussMember> membersList { get; set; }

        internal override void CreateCustomFamily(Document doc)
        {

            Dictionary<List<XYZ>, double> dict = new Dictionary<List<XYZ>, double>();
            foreach (MapGenericTrussMember genericTrussMember in membersList)
            {
                dict.Add(genericTrussMember.BaseProfile.ConvertToXYZList(), genericTrussMember.Width / 12.00);
            }

            _subFolderPath = GeneralCreator.CreateTrussFamily(doc, ref _name, dict);
        }

    }
}