using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFRvt
{
    public static class NodesNames
    {
        public const string MapShellBuilding = "MapShellBuilding";
        public const string BUILDING = "Building";
        public const string FLOOR_LIST = "FloorList";
        public const string NAME = "Name";
        public const string BUILDING_EX = "Building_ex";
        #region Floor
        public const string MIN_X = "MinX";
        public const string MIN_Y = "MinY";
        public const string MAX_X = "MaxX";
        public const string MAX_Y = "MaxY";
        public const string FLOOR = "Floor";
        public const string FLOORSLAB = "FloorSlab";
        public const string CeilingSlab = "CeilingSlab";
        public const string BasePolygon = "BasePolygon";
        public const string LEVEL = "Level";
        public const string BOTTOM_LEVEL = "BottomLevel";
        public const string PLATE_LEVEL = "PlateLevel";
        public const string TOP_LEVEL = "TopLevel";
        public const string SUB_LEVEL = "SubLevel";
        public const string LEVEL_ID = "LevelId";
        public const string ID = "Id";
        public const string FLOOR_LEVELS = "FloorLevels";
        public const string FLOOR_LEVEL = "FloorLevel";
        public const string FLOOR_TOP_PLATE_LEVEL = "FloorTopPlateLevel";
        public const string FOUNDATION_LEVEL = "FoundationLevel";
        public const string FLOOR_SUB_LEVEL = "FloorSubLevel";
        public const string FLOOR_OBJECTS = "ObjectList";
        public const string TOP_PLATE_HEIGHT = "TopPlateHeight";
        public const string FRAMING_HEIGHT = "FramingHeight";
        public const string SHEATHING_HEIGHT = "SheathingHeight";
        public const string FLOOR_HEIGHTS = "FLoorHeights";
        #endregion

        #region Geometry
        public const string PROFILE = "Profile";
        public const string POINT_LIST = "PointList";
        public const string POINT_LOCATION = "PointLocation";
        public const string HoleList = "HoleList";
        public const string HOLE = "Hole";

        public const string POSITION = "position";
        public const string DIRECTION = "Direction";
        public const string ROTATION = "Rotation";
        public const string LINE = "Line";
        public const string P1 = "P1";
        public const string P2 = "P2";
        public const string POINT = "point";
        public const string X = "X";
        public const string Y = "Y";
        public const string Z = "Z";
        #endregion

        #region Section
        public const string DEPTH = "Depth";
        public const string PlIES = "Plies";
        public const string LENGTH = "Length";
        #region Rectangular
        public const string SECTION = "Section";
        public const string WIDTH = "Width";
        #endregion
        #region ISection
        public const string ISECTION = "ISection";
        public const string PLATE_WIDTH = "PlateWidth";
        public const string PLATE_DEPTH = "PlateDepth";
        public const string WEB_WIDTH = "WebWidth";
        #endregion
        #endregion

        #region Beam
        public const string BEAM = "Beam";
        public const string IS_INVERTED = "Inverted";
        #endregion

        #region Rafter
        public const string RAFTER = "Rafter";
        #endregion

        #region Joist/RafterArea
        public const string JOIST_AREA = "joistArea";
        public const string RAFTER_AREA = "rafterArea";
        public const string OPENING_LIST = "openingList";
        public const string MEMBERS_LINES = "membersLines";
        public const string RIM_LIST = "RimList";
        public const string RIM = "Rim";
        public const string BLOCK_LIST = "BlockList";
        public const string BLOCK = "Block";
        public const string START_TRIM_MEMBER_INDEX = "StartTrimMemberIndex";
        public const string END_TRIM_MEMBER_INDEX = "EndTrimMemberIndex";
        #endregion

        #region Roof
        public const string ROOF = "Roof";
        public const string ROOFSlab = "Roofslab";
        public const string ROOFPolygon = "RoofPolygon";
        public const string ROOFS = "RoofList";
        public const string SHELL_BASE_POLYGON_ROOF = "ShellBasePolygonRoof";
        public const string BASE_POLYGON_ROOF = "BasePolygonRoof";
        public const string PLANES_GENERIC_ROOF = "PlanesGenericRoof";
        public const string SHELL_PLANES_GENERIC_ROOF = "ShellPlanesGenericRoof";
        public const string SUB_ROOF = "SubRoof";
        public const string ROOF_PLANE_LIST = "roofPlaneList";
        public const string SUB_ROOF_LIST = "subRoofList";
        public const string ROOF_BASE_LINE_LIST = "BaseLinesList";
        public const string ROOF_BASE_LINE = "BaseLine";
        public const string OFFSET = "Offset";
        public const string PITCH = "Pitch";
        public const string OVERHANG = "OverHang";
        public const string GABLE = "GABLE";
        public const string SlOPEARROW = "SlopeArrow";
        public const string SlOPEARROWDIR = "SlopeArrowDir";
        public const string POPUP_OPENING_LIST = "PopUpOpeningList";
        public const string SURFACE_LIST = "SurfaceList";
        public const string LINE_LIST = "LineList";
        public const string CeilingSubstrate = "CeilingSubstrate";
        #endregion

        #region Slab
        public const string SLAB = "Slab";
        public const string SLAB_THICKNESS = "Slab_Thickness";
        public const string SLAB_OFFSET = "Slab_Offset";
        public const string SLAB_HEIGHT = "Slab_Height";
        #endregion

        #region Post
        public const string PRE_MANFACTURED_WALL = "PreManfactureWall";
        public const string POST = "Post";
        public const string MIN_Z = "MinZ";
        public const string MAX_Z = "MaxZ";
        public const string ON_FOUNDATION = "OnFoundation";
        public const string TOP_OFFSET = "TOP_OFFSET";
        public const string BOTTOM_OFFSET = "BOTTOM_OFFSET";
        public const string leftline = "leftline";
        public const string rightline = "rightline";
        #endregion

        #region ShellWall
        public const string Shell = "Shell";
        public const string ShellWall_LIST = "ShellWall_LIST";
        public const string ShellWall = "ShellWall";
        public const string StartPoint = "StartPoint";
        public const string EndPoint = "EndPoint";
        public const string Height = "Height";
        public const string Thickness = "Thickness";
        public const string BottomOffset = "BottomOffset";
        public const string VertexList = "VertexList";
        public const string MinCorner = "MinCorner";
        public const string MaxCorner = "MaxCorner";
        public const string EdgeList = "EdgeList";

        public const string Start = "Start";
        public const string End = "End";
        public const string HEADHEIGHT = "headHeight";
        public const string OpeningHeight = "OpeningHeight";
        public const string SillHeight = "SillHeight";
        public const string OpeningType = "OpeningType";
        public const string ParentWall = "ParentWall";
        #endregion
        #region StraightWall
        public const string WALL = "Wall";
        public const string WALLOPENINGLIST = "wallOpeningList";
        public const string WALLOPENING = "WallOpening";
        public const string DOOR = "Door";
        public const string WINDOW = "Window";
        public const string WALL_ATTATCHMENT = "WallAttatchment";
        public const string HEADER_HEIGHT = "HeaderHeight";
        public const string SILL_HEIGHT = "SillHeight";
        public const string WALL_DEPTH = "WallDepth";
        public const string WALL_HEADER = "Header";
        public const string WALL_HEADER_LIST = "HeaderList";
        public const string WALL_STEEL_HEADER_LIST = "SteelHeaderList";
        public const string STUD_LIST = "StudList";
        public const string STUD = "Stud";
        public const string WALL_POST_LIST = "WallPostList";
        public const string WALL_MANFACTURED_WALL = "WallPreManfacuredWall";
        public const string WALL_MANFACTURED_WALL_LIST = "WallPreManfacuredWallsList";
        public const string WALL_POST = "WallPost";
        public const string IN_WALL_WIDTH = "InWallWidth";
        public const string IN_WALL_DISTANCE = "InWallDistance";
        public const string TOP_PLATES_DEPTH = "TopPlatesDepth";
        public const string TOP_PLATES_NUMBER = "TopPlatesNumber";
        public const string BOTTOM_PLATES_DEPTH = "BottomPlatesDepth";
        public const string BOTTOM_PLATES_NUMBER = "BottomPlatesNumber";
        public const string TOP_PROFILE = "TopProfile";
        public const string BOTTOM_PROFILE = "BottomProfile";

        #endregion

        #region Foundation
        public const string FOUNDATION = "Foundation";
        #endregion

        #region Trusses
        public const string TRUSS_AREA = "TrussArea";
        public const string GIRDER_TRUSS = "GirderTruss";
        public const string GIRDER_TRUSS_LIST = "GirderTrussList";
        public const string TRUSS_LIST = "TrussList";
        public const string TRUSS = "Truss";
        public const string TRUSS_TYPE = "TrussType";
        public const string START_OVERHANG = "StartOverHang";
        public const string End_OVERHANG = "EndOverHang";
        public const string BOTTOM_CHORD_LEVEL = "BottomChordLevel";
        public const string BOTTOM_CHORD_DEPTH = "BottomChordDepth";
        public const string TOP_CHORD_DEPTH = "TopChordDepth";
        public const string WEB_DEPTH = "WebDepth";
        #endregion

        #region GenericTrusses
        public const string GENERIC_GIRDER_TRUSS = "GenericGirderTruss";
        public const string GENERIC_TRUSS_POSITION_LIST = "GenericTrussPositionList";
        public const string GENERIC_TRUSS = "GenericTruss";
        public const string GENERIC_FAMILY_LIST = "ShellGenericFamilyList";
        public const string SHELL_GENERIC_FAMILY_LIST = "ShellGenericFamilyList";
        public const string GENERIC_TRUSS_FAMILY = "GenericTrussFamily";
        public const string GENERIC_ROOF_FAMILY = "GenericRoofFamily";
        public const string SHELL_GENERIC_ROOF_FAMILY = "ShellGenericRoofFamily";
        public const string GENERIC_TRUSS_MEMBER_LIST = "GenericTrussMemeberList";
        public const string GENERIC_TRUSS_MEMBER = "GenericTrussMember";
        #endregion
    }
}
