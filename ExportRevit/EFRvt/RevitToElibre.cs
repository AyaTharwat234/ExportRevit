using Autodesk.Revit.DB;
using Elibre.Geometry2D;
using Elibre.Net.Core;
using Elibre.Net.Debug;
using Elibre.Net.Revit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EFRvt
{
    public static class RevitToElibre
    {
        public static RevitBuildingInfo GetRevitBuildingInfo(Document doc)
        {
            var revitBuildingInfo = new RevitBuildingInfo();
            try
            {
                revitBuildingInfo.Materials = GetAllMaterials(doc);
                revitBuildingInfo.GridLines = GetAllGridLines(doc);
                revitBuildingInfo.GridTypes = GetAllGridTypes(doc);

                var wallTypes = revitBuildingInfo.WallTypes;
                var floorTypes = revitBuildingInfo.FloorTypes;
                var roofTypes = revitBuildingInfo.RoofTypes;
                var ceilingTypes = revitBuildingInfo.CeilingTypes;

                GetAllTypes(doc, ref wallTypes, ref floorTypes, ref roofTypes, ref ceilingTypes);

                revitBuildingInfo.DoorSymbols = GetAllDoorSymbols(doc);
                revitBuildingInfo.WindowSymbols = GetAllWindowSymbols(doc);

                var walls = GetAllWalls(doc);
                var levels = GetAllLevels(doc);
                var topWallLevels = GetAllTopWallLevels(doc);

                AssignWallsToLevels(ref walls, ref levels);
                AssignWallsToLevels(ref walls, ref topWallLevels);

                GetAllCeilings(doc, ref topWallLevels);
                GetAllFloors(doc, ref levels);

                revitBuildingInfo.Levels = levels;
                revitBuildingInfo.TopWallLevels = topWallLevels;

                revitBuildingInfo.RoofPolygons3D = GetAllRoofs(doc);
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }

            return revitBuildingInfo;
        }
        public static RevitWall GetWall(Element e)
        {
            try
            {
                var wall = e as Wall;
                if (wall == null)
                    return null;
                var rvtWall = new RevitWall
                {
                    UnconnectedHeight = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble() * 12,
                    TopLevelName =
                        wall.Document.GetElement(wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsElementId()) !=
                        null
                            ? wall.Document
                                .GetElement(
                                    wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsElementId()).Name
                            : "Unconnected",
                    BaseLevelName = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).AsValueString(),
                    TopLevelOffset = wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET).AsDouble(),
                    BaseLevelOffset = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).AsDouble(),
                    WallOptions = { Type = wall.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() },
                    IsBearing = wall.StructuralUsage.ToString() != "NonBearing"
                };
                var c = wall.Location as LocationCurve;
                if (c != null)
                {
                    var rvtWallLocationEnd_0 = new Point3D(c.Curve.GetEndPoint(0).X, c.Curve.GetEndPoint(0).Y, c.Curve.GetEndPoint(0).Z);
                    var rvtWallLocationEnd_1 = new Point3D(c.Curve.GetEndPoint(1).X, c.Curve.GetEndPoint(1).Y, c.Curve.GetEndPoint(1).Z);
                    if (rvtWall.TopLevelName != "Unconnected")
                    {
                        rvtWall.Height = (((Level)wall.Document.GetElement(wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsElementId())).Elevation
                                          - ((Level)wall.Document.GetElement(wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).AsElementId())).Elevation) * 12;
                    }
                    else
                    {
                        var allPoints = new List<Point3D>();
                        var topPoints = new List<Point3D>();
                        var sideFaces = HostObjectUtils.GetSideFaces(wall, ShellLayerType.Exterior);
                        foreach (var refSideFace in sideFaces)
                        {
                            var eSideFace = wall.Document.GetElement(refSideFace);
                            var face = eSideFace.GetGeometryObjectFromReference(refSideFace) as Face;
                            if (face == null)
                                continue;
                            var edgeLoops = face.EdgeLoops;
                            foreach (EdgeArray edgeArray in edgeLoops)
                            {
                                foreach (Edge edge in edgeArray)
                                {
                                    var curve = edge.AsCurve();
                                    var line = curve as Line;
                                    allPoints.Add(new Point3D(line.GetEndPoint(0).X, line.GetEndPoint(0).Y, line.GetEndPoint(0).Z));
                                    allPoints.Add(new Point3D(line.GetEndPoint(1).X, line.GetEndPoint(1).Y, line.GetEndPoint(1).Z));
                                    if (line != null && Math.Abs(line.Origin.Z - rvtWallLocationEnd_0.Z) <= 0) continue;
                                    if ((int)(rvtWallLocationEnd_0.X - rvtWallLocationEnd_1.X) == 0)
                                    {
                                        if (Math.Abs(Math.Round(line.Origin.Y, 3) - Math.Round(rvtWallLocationEnd_0.Y, 3)) < 1 || Math.Abs(Math.Round(line.Origin.Y, 3) - Math.Round(rvtWallLocationEnd_1.Y, 3)) < 1)
                                        {
                                            topPoints.Add(new Point3D(line.Origin.X, line.Origin.Y, line.Origin.Z));
                                        }
                                    }
                                    else if ((int)(rvtWallLocationEnd_0.Y - rvtWallLocationEnd_1.Y) == 0)
                                    {
                                        if (Math.Abs(Math.Round(line.Origin.X, 3) - Math.Round(rvtWallLocationEnd_0.X, 3)) < 1 || Math.Abs(Math.Round(line.Origin.X, 3) - Math.Round(rvtWallLocationEnd_1.X, 3)) < 1)
                                        {
                                            topPoints.Add(new Point3D(line.Origin.X, line.Origin.Y, line.Origin.Z));
                                        }
                                    }
                                }
                            }
                        }
                        rvtWall.Height = (topPoints.Min(p => p.Z)) * 12;
                    }

                    rvtWall.Profile = new List<Point3D>
                {
                    new Point3D(rvtWallLocationEnd_0.X * 12, -rvtWallLocationEnd_0.Y * 12,
                        rvtWallLocationEnd_0.Z * 12),
                    new Point3D(rvtWallLocationEnd_1.X * 12, -rvtWallLocationEnd_1.Y * 12,
                        rvtWallLocationEnd_1.Z * 12),
                    new Point3D(rvtWallLocationEnd_1.X * 12, -rvtWallLocationEnd_1.Y * 12,
                        rvtWallLocationEnd_1.Z * 12 + rvtWall.Height),
                    new Point3D(rvtWallLocationEnd_0.X * 12, -rvtWallLocationEnd_0.Y * 12,
                        rvtWallLocationEnd_0.Z * 12 + rvtWall.Height)
                };
                }


                var wallOpeningsIds = wall.FindInserts(true, true, true, true);

                if (wallOpeningsIds.Count <= 0)
                    return rvtWall;
                var doorFilter = new FilteredElementCollector(wall.Document, wallOpeningsIds).OfCategory(BuiltInCategory.OST_Doors);
                if (doorFilter.Any())
                {
                    rvtWall.Doors = new List<RevitDoor>();
                    foreach (var door in doorFilter)
                    {
                        rvtWall.Doors.Add(GetDoor(door));
                    }
                }

                var windowFilter = new FilteredElementCollector(wall.Document, wallOpeningsIds).OfCategory(BuiltInCategory.OST_Windows);
                if (windowFilter.Any())
                {
                    rvtWall.Windows = new List<RevitWindow>();
                    foreach (var window in windowFilter)
                    {

                        rvtWall.Windows.Add(GetWindow(window));
                    }
                }
                return rvtWall;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }

        }
        public static RevitWindow GetWindow(Element e)
        {
            try
            {
                var rvtWindow = new RevitWindow();
                var window = e as FamilyInstance;
                if (window != null)
                {
                    rvtWindow.SillHeight =
                        window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).AsDouble() * 12;
                    rvtWindow.HeadHeight =
                        window.get_Parameter(BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM).AsDouble() * 12;
                }
                rvtWindow.WindowOptions.Type = e.Name;
                var loc = (e.Location as LocationPoint)?.Point;
                if (loc != null)
                    rvtWindow.Location = new Point2D(loc.X * 12, -loc.Y * 12);
                return rvtWindow;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }


        }
        public static RevitDoor GetDoor(Element e)
        {
            try
            {
                var rvtDoor = new RevitDoor();
                var door = e as FamilyInstance;
                if (door != null)
                {
                    rvtDoor.SillHeight = door.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).AsDouble() * 12;
                    rvtDoor.HeadHeight = door.get_Parameter(BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM).AsDouble() * 12;
                }
                rvtDoor.DoorOptions.Type = e.Name;
                var loc = (e.Location as LocationPoint)?.Point;
                if (loc != null)
                    rvtDoor.Location = new Point2D(loc.X * 12, -loc.Y * 12);
                return rvtDoor;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }

        }
        public static List<RevitWall> GetAllWalls(Document doc)
        {
            try
            {
                return new FilteredElementCollector(doc).OfClass(typeof(Wall)).Select(GetWall).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }

        #region Symbol Methods
        public static RevitNewSymbol GetSymbol(Element e)
        {
            try
            {
                var fi = e as FamilyInstance;
                if (fi == null)
                    return null;
                var fs = fi.Symbol;

                var s = new RevitNewSymbol
                {
                    SymbolName = e.Name,
                    WallClosure = fs.get_Parameter(BuiltInParameter.TYPE_WALL_CLOSURE) != null ? (int)(fs.get_Parameter(BuiltInParameter.TYPE_WALL_CLOSURE).AsDouble()) : 0,
                    Width = fs.get_Parameter(BuiltInParameter.DOOR_WIDTH) != null ? 12 * fs.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble() : 0,
                    Height = fs.get_Parameter(BuiltInParameter.GENERIC_HEIGHT) != null ? 12 * fs.get_Parameter(BuiltInParameter.GENERIC_HEIGHT).AsDouble() : 0,
                    RoughWidth = fs.get_Parameter(BuiltInParameter.FAMILY_ROUGH_WIDTH_PARAM) != null ? 12 * fs.get_Parameter(BuiltInParameter.FAMILY_ROUGH_WIDTH_PARAM).AsDouble() : 0,
                    RoughHeight = fs.get_Parameter(BuiltInParameter.FAMILY_ROUGH_HEIGHT_PARAM) != null ? 12 * fs.get_Parameter(BuiltInParameter.FAMILY_ROUGH_HEIGHT_PARAM).AsDouble() : 0,
                    Keynote = fs.get_Parameter(BuiltInParameter.KEYNOTE_PARAM) != null ? fs.get_Parameter(BuiltInParameter.KEYNOTE_PARAM).AsValueString() : "",
                    Model = fs.get_Parameter(BuiltInParameter.ALL_MODEL_MODEL) != null ? fs.get_Parameter(BuiltInParameter.ALL_MODEL_MODEL).AsValueString() : "",
                    Manufacturer = fs.get_Parameter(BuiltInParameter.ALL_MODEL_MANUFACTURER) != null ? fs.get_Parameter(BuiltInParameter.ALL_MODEL_MANUFACTURER).AsValueString() : "",
                    TypeComments = fs.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_COMMENTS) != null ? fs.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_COMMENTS).AsValueString() : "",
                    URL = fs.get_Parameter(BuiltInParameter.ALL_MODEL_URL) != null ? fs.get_Parameter(BuiltInParameter.ALL_MODEL_URL).AsValueString() : "",
                    Discription = fs.get_Parameter(BuiltInParameter.ALL_MODEL_DESCRIPTION) != null ? fs.get_Parameter(BuiltInParameter.ALL_MODEL_DESCRIPTION).AsValueString() : "",
                    TypeMark = fs.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK) != null ? fs.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK).AsValueString() : "",
                    Cost = fs.get_Parameter(BuiltInParameter.ALL_MODEL_COST) != null ? fs.get_Parameter(BuiltInParameter.ALL_MODEL_COST).AsDouble() : 0
                };
                return s;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static List<RevitNewSymbol> GetAllDoorSymbols(Document doc)
        {
            try
            {
                return new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Doors).Select(GetSymbol).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }

        }
        public static List<RevitNewSymbol> GetAllWindowSymbols(Document doc)
        {
            try
            {
                return new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Windows).Select(GetSymbol).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }

        #endregion

        #region Type Methods
        public static RevitNewType GetType(Element e)
        {
            try
            {
                var rnt = new RevitNewType();
                var wt = e as WallType;
                rnt.TypeName = e.Name;
                if (wt == null)
                    return rnt;
                if (wt.ThermalProperties != null)
                {
                    rnt.Absorptance = wt.ThermalProperties.Absorptance;
                    rnt.Roughness = wt.ThermalProperties.Roughness;
                }
                rnt.Keynote = wt.get_Parameter(BuiltInParameter.KEYNOTE_PARAM) != null ? wt.get_Parameter(BuiltInParameter.KEYNOTE_PARAM).AsValueString() : "";
                rnt.Model = wt.get_Parameter(BuiltInParameter.ALL_MODEL_MODEL) != null ? wt.get_Parameter(BuiltInParameter.ALL_MODEL_MODEL).AsValueString() : "";
                rnt.Manufacturer = wt.get_Parameter(BuiltInParameter.ALL_MODEL_MANUFACTURER) != null ? wt.get_Parameter(BuiltInParameter.ALL_MODEL_MANUFACTURER).AsValueString() : "";
                rnt.TypeComments = wt.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_COMMENTS) != null ? wt.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_COMMENTS).AsValueString() : "";
                rnt.URL = wt.get_Parameter(BuiltInParameter.ALL_MODEL_URL) != null ? wt.get_Parameter(BuiltInParameter.ALL_MODEL_URL).AsValueString() : "";
                rnt.Discription = wt.get_Parameter(BuiltInParameter.ALL_MODEL_DESCRIPTION) != null ? wt.get_Parameter(BuiltInParameter.ALL_MODEL_DESCRIPTION).AsValueString() : "";
                rnt.TypeMark = wt.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK) != null ? wt.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK).AsValueString() : "";
                rnt.Cost = wt.get_Parameter(BuiltInParameter.ALL_MODEL_COST) != null ? wt.get_Parameter(BuiltInParameter.ALL_MODEL_COST).AsDouble() : 0;
                var cs = wt.GetCompoundStructure();
                if (cs == null)
                    return rnt;
                var csls = cs.GetLayers();
                var rtl = new RevitTypeLayer();
                foreach (var csl in csls)
                {
                    var materialId = csl.MaterialId;
                    rtl.StructuralMaterial = (csl.Function == MaterialFunctionAssignment.Structure ||
                                              csl.Function == MaterialFunctionAssignment.StructuralDeck);
                    rtl.Function = (int)csl.Function;
                    rtl.Thickness = csl.Width * 12;
                    if (materialId.IntegerValue > -1)
                    {
                        var eMaterial = e.Document.GetElement(materialId);
                        rtl.Material = eMaterial.Name;
                        var material = eMaterial as Material;
                        rtl.Manufacturer = material?.get_Parameter(BuiltInParameter.ALL_MODEL_MANUFACTURER) != null ? material.get_Parameter(BuiltInParameter.ALL_MODEL_MANUFACTURER).AsValueString() : "";
                    }
                    rnt.mainLayers.Add(rtl);
                }
                return rnt;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static List<RevitNewType> GetAllWallTypes(Document doc)
        {
            try
            {
                var fec = new FilteredElementCollector(doc).OfClass(typeof(WallType));
                return fec.Select(GetType).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }

        }
        public static List<RevitNewType> GetAllFloorTypes(Document doc)
        {
            try
            {
                var fec = new FilteredElementCollector(doc).OfClass(typeof(FloorType));
                return fec.Select(GetType).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }

        }
        public static List<RevitNewType> GetAllRoofTypes(Document doc)
        {
            try
            {
                var fec = new FilteredElementCollector(doc).OfClass(typeof(RoofType));
                return fec.Select(GetType).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static List<RevitNewType> GetAllCeilingTypes(Document doc)
        {
            try
            {
                var fec = new FilteredElementCollector(doc).OfClass(typeof(CeilingType));
                return fec.Select(GetType).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static List<RevitNewType> GetAllTypes(Document doc, TypeEnum type)
        {
            try
            {
                switch (type)
                {
                    case TypeEnum.Wall:
                        return new FilteredElementCollector(doc).OfClass(typeof(WallType)).Select(GetType).ToList();
                    case TypeEnum.Floor:
                        return new FilteredElementCollector(doc).OfClass(typeof(FloorType)).Select(GetType).ToList();
                    case TypeEnum.Roof:
                        return new FilteredElementCollector(doc).OfClass(typeof(RoofType)).Select(GetType).ToList();
                    case TypeEnum.Ceiling:
                        return new FilteredElementCollector(doc).OfClass(typeof(CeilingType)).Select(GetType).ToList();
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static void GetAllTypes(Document doc, ref List<RevitNewType> wallTypes, ref List<RevitNewType> floorTypes,
            ref List<RevitNewType> roofTypes, ref List<RevitNewType> ceilingTypes)
        {
            try
            {
                if (wallTypes == null)
                {
                    wallTypes = new List<RevitNewType>();
                }
                if (floorTypes == null)
                {
                    floorTypes = new List<RevitNewType>();
                }
                if (roofTypes == null)
                {
                    roofTypes = new List<RevitNewType>();
                }
                if (ceilingTypes == null)
                {
                    ceilingTypes = new List<RevitNewType>();
                }
                wallTypes.AddRange(new FilteredElementCollector(doc).OfClass(typeof(WallType)).Select(GetType).ToList());
                floorTypes.AddRange(new FilteredElementCollector(doc).OfClass(typeof(FloorType)).Select(GetType).ToList());
                roofTypes.AddRange(new FilteredElementCollector(doc).OfClass(typeof(RoofType)).Select(GetType).ToList());
                ceilingTypes.AddRange(new FilteredElementCollector(doc).OfClass(typeof(CeilingType)).Select(GetType).ToList());
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }
        #endregion

        public static List<RevitMaterial> GetAllMaterials(Document doc)
        {
            try
            {
                var fec = new FilteredElementCollector(doc).OfClass(typeof(FootPrintRoof));
                return (from e in fec
                        let m = e as Material
                        where m != null
                        select new RevitMaterial
                        {
                            Name = e.Name,
                            ColorARGB = m.get_Parameter(BuiltInParameter.MATERIAL_PARAM_COLOR).AsInteger()
                        }).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static List<RevitGridType> GetAllGridTypes(Document doc)
        {
            try
            {
                var fec = new FilteredElementCollector(doc).OfClass(typeof(GridType));
                return (from e in fec
                        let gt = e as GridType
                        where gt != null
                        select new RevitGridType
                        {
                            Name = e.Name,
                            Color = gt.get_Parameter(BuiltInParameter.GRID_END_SEGMENT_COLOR).AsInteger(),
                            Weight = gt.get_Parameter(BuiltInParameter.GRID_END_SEGMENT_WEIGHT).AsDouble(),
                        }).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static List<RevitGridLine> GetAllGridLines(Document doc)
        {
            try
            {
                var fec = new FilteredElementCollector(doc).OfClass(typeof(Grid));
                return (from e in fec
                        let gl = e as Grid
                        where gl != null
                        select new RevitGridLine
                        {
                            Name = e.Name,
                            Start = new Point2D(gl.Curve.GetEndPoint(0).X, -gl.Curve.GetEndPoint(0).Y),
                            End = new Point2D(gl.Curve.GetEndPoint(1).X, -gl.Curve.GetEndPoint(1).Y)
                        }).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }

        #region Floor Methods
        public static RevitFloorPolygons GetElibreFloor(Element e)
        {
            try
            {
                var rvtFloorPolygons = new RevitFloorPolygons
                {
                    Slices = GetRevitRoofBorderPolygons(e, new List<RevitPolygonSlice>()),
                    FloorOptions = { Type = e.Name }
                };
                return rvtFloorPolygons;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }

        }
        public static List<RevitFloorPolygons> GetAllFloors(Document doc)
        {
            try
            {
                var fec = new FilteredElementCollector(doc).OfClass(typeof(Floor));
                return fec.Select(GetElibreFloor).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static void GetAllFloors(Document doc, ref List<RevitLevel> rvtLvls)
        {
            try
            {
                var fec = new FilteredElementCollector(doc).OfClass(typeof(Floor));
                var floorPolygons = new List<RevitFloorPolygons>();
                foreach (var e in fec)
                {
                    floorPolygons.Add(GetElibreFloor(e));
                    var lvlIndex = rvtLvls.FindIndex(l => l.LevelName == (doc.GetElement(e.LevelId) as Level)?.Name);
                    if (lvlIndex <= -1) continue;
                    if (rvtLvls[lvlIndex].FloorPolygons != null)
                    {
                        rvtLvls[lvlIndex].FloorPolygons.Add(GetElibreFloor(e));
                    }
                    else
                    {
                        rvtLvls[lvlIndex].FloorPolygons = new List<RevitFloorPolygons> { GetElibreFloor(e) };
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }

        }
        #endregion

        #region Levels Methods
        public static RevitLevel GetLevel(Element e)
        {
            try
            {
                var lvl = e as Level;
                var rvtLvl = new RevitLevel();
                if (lvl == null)
                    return rvtLvl;
                rvtLvl.Elevation = lvl.Elevation * 12;
                rvtLvl.LevelName = lvl.Name;
                rvtLvl.BuildingStory = lvl.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY) != null
                    ? (int)lvl.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY).AsDouble()
                    : 0;
                rvtLvl.ElevationString = lvl.get_Parameter(BuiltInParameter.LEVEL_ELEV) != null
                    ? lvl.get_Parameter(BuiltInParameter.LEVEL_ELEV).AsValueString()
                    : "'";
                return rvtLvl;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static RevitLevel GetLevel(Level lvl)
        {
            try
            {
                var rvtLvl = new RevitLevel
                {
                    Elevation = lvl.Elevation * 12,
                    LevelName = lvl.Name,
                    BuildingStory = lvl.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY) != null
                   ? (int)lvl.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY).AsDouble()
                   : 0,
                    ElevationString = lvl.get_Parameter(BuiltInParameter.LEVEL_ELEV) != null
                   ? lvl.get_Parameter(BuiltInParameter.LEVEL_ELEV).AsValueString()
                   : "'"
                };
                return rvtLvl;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static List<RevitLevel> GetAllLevels(Document doc)
        {
            try
            {
                var lvlIds = new List<ElementId>();

                foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
                {
                    var w = e as Wall;
                    if (w != null && !lvlIds.Contains(w.LevelId))
                    {
                        lvlIds.Add(w.LevelId);
                    }
                }

                var lvls = lvlIds.Select(eId => doc.GetElement(eId) as Level).ToList();
                var sortedLvls = lvls.OrderBy(l => l.Elevation).ToList();
                return sortedLvls.Select(GetLevel).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static List<RevitLevel> GetAllTopWallLevels(Document doc, ref List<RevitWall> walls)
        {
            try
            {
                var fec = new FilteredElementCollector(doc).OfClass(typeof(Level)).ToList();
                var topWallLevels = new List<RevitLevel>();
                foreach (var wall in walls)
                {
                    if (string.IsNullOrEmpty(wall.TopLevelName)) continue;
                    var topWallLevelIndex = fec.FindIndex(e => e.Name == wall.TopLevelName);
                    if (topWallLevelIndex <= -1) continue;
                    if (topWallLevels.Any()) continue;
                    if (topWallLevels.FindIndex(twl => twl.LevelName == fec[topWallLevelIndex].Name) >= 0)
                        continue;
                    var topWallLevel = GetLevel(fec[topWallLevelIndex]);
                    if (topWallLevel != null) topWallLevels.Add(topWallLevel);
                }
                if (topWallLevels.Any())
                {
                    topWallLevels.OrderBy(l => l.Elevation).ToList();
                }
                return topWallLevels;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static List<RevitLevel> GetAllTopWallLevels(Document doc)
        {
            try
            {
                var fecWall = new FilteredElementCollector(doc).OfClass(typeof(Wall)).ToList();
                var topWallLevels = new List<RevitLevel>();
                RevitLevel topWallLevel;
                foreach (var wall in fecWall)
                {
                    if (doc.GetElement(wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsElementId()) ==
                        null) continue;
                    topWallLevel =
                        GetLevel(doc.GetElement(wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE)
                            .AsElementId()));
                    if (topWallLevel == null)
                        continue;
                    if (!topWallLevels.Any())
                    {
                        topWallLevels.Add(topWallLevel);
                    }
                    else
                    {
                        if (topWallLevels.Exists(l => l.LevelName == topWallLevel.LevelName)) continue;
                        topWallLevels.Add(topWallLevel);
                    }
                }
                if (!topWallLevels.Any())
                    return topWallLevels;
                else
                {
                    topWallLevels.OrderBy(l => l.Elevation).ToList();
                }
                return topWallLevels;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }

        }
        public static void AssignWallsToLevels(ref List<RevitWall> walls, ref List<RevitLevel> lvls)
        {
            try
            {
                foreach (var wall in walls)
                {
                    if (lvls.FindIndex(l => l.LevelName == wall.BaseLevelName) <= -1) continue;
                    {
                        var index = lvls.FindIndex(l => l.LevelName == wall.BaseLevelName);
                        if (lvls[index].Walls == null)
                        {
                            lvls[index].Walls = new List<RevitWall>();
                        }
                        lvls[index].Walls.Add(wall);
                        wall.LevelIndex = index;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }
        #endregion

        #region Ceiling Methods
        public static CeilingPolygon GetCeiling(Element e)
        {
            try
            {
                var ceilingPolygon = new CeilingPolygon
                {
                    Ceilings = new RevitRoofBorderPolygons
                    {
                        RoofOptions = { Type = e.Name },
                        outerLineSlices = GetRevitRoofBorderPolygons(e, new List<RevitPolygonSlice>())
                    }
                };
                return ceilingPolygon;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static List<CeilingPolygon> GetAllCeilings(Document doc)
        {
            try
            {
                var fec = new FilteredElementCollector(doc).OfClass(typeof(FootPrintRoof));
                return (from e in fec where e.Name.Contains("Ceiling") select GetCeiling(e)).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }

        }
        public static void GetAllCeilings(Document doc, ref List<RevitLevel> rvtTopWallLvls)
        {
            try
            {
                var fec = new FilteredElementCollector(doc).OfClass(typeof(FootPrintRoof));
                var ceilingPolygons = new List<CeilingPolygon>();
                foreach (var e in fec)
                {
                    if (e.Name.Contains("Ceiling"))
                    {
                        ceilingPolygons.Add(GetCeiling(e));
                    }
                    var lvlIndex = rvtTopWallLvls.FindIndex(l => l.LevelName == (doc.GetElement(e.LevelId) as Level)?.Name);
                    if (lvlIndex <= -1) continue;
                    if (rvtTopWallLvls[lvlIndex].Ceilings != null)
                    {
                        rvtTopWallLvls[lvlIndex].Ceilings.Add(GetCeiling(e));
                        rvtTopWallLvls[lvlIndex].Ceilings.Last().Ceilings.LevelIndex = lvlIndex;
                    }
                    else
                    {
                        rvtTopWallLvls[lvlIndex].Ceilings = new List<CeilingPolygon> { GetCeiling(e) };
                        rvtTopWallLvls[lvlIndex].Ceilings.Last().Ceilings.LevelIndex = lvlIndex;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }

        }
        #endregion

        public static List<RevitPolygonSlice> GetRevitRoofBorderPolygons(Element e, List<RevitPolygonSlice> slices)
        {
            try
            {
                if (slices == null)
                    slices = new List<RevitPolygonSlice>();

                var opt = new Options { ComputeReferences = true };
                var geo = e.get_Geometry(opt);
                foreach (var o in geo)
                {
                    var solid = (Solid)o;
                    foreach (Face face in solid.Faces)
                    {
                        var pface = face as PlanarFace;
                        if (pface != null && !(pface.FaceNormal.Z > 0)) continue;
                        var edgeLoops = face.EdgeLoops;
                        foreach (EdgeArray edgeLoop in edgeLoops)
                        {
                            foreach (Edge edge in edgeLoop)
                            {
                                var rvtPolygonSlice = new RevitPolygonSlice();
                                var curve = edge.AsCurve();
                                var line = curve as Line;
                                if (line != null)
                                {
                                    rvtPolygonSlice.Start = new Point3D(line.GetEndPoint(0).X * 12, -line.GetEndPoint(0).Y * 12, line.GetEndPoint(0).Z * 12);
                                    rvtPolygonSlice.End = new Point3D(line.GetEndPoint(1).X * 12, -line.GetEndPoint(1).Y * 12, line.GetEndPoint(1).Z * 12);
                                    rvtPolygonSlice.IsArc = false;
                                    rvtPolygonSlice.OnArc = new Point3D(0, 0, 0);
                                    rvtPolygonSlice.Angle = e.get_Parameter(BuiltInParameter.ROOF_SLOPE) != null ? e.get_Parameter(BuiltInParameter.ROOF_SLOPE).AsDouble() : 0;
                                    rvtPolygonSlice.Slope = rvtPolygonSlice.Angle != 0;
                                    slices.Add(rvtPolygonSlice);
                                }
                                var arc = curve as Arc;
                                if (arc == null)
                                    continue;
                                rvtPolygonSlice.IsArc = true;
                                rvtPolygonSlice.OnArc = new Point3D(arc.Center.X * 12, -arc.Center.Y * 12, arc.Center.Z * 12);
                                slices.Add(rvtPolygonSlice);
                            }
                        }
                    }
                }
                return slices;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }

        public static List<Polygon3DInfo> GetRoof(Element e)
        {
            try
            {
                var roofs = new List<Polygon3DInfo>();
                var opt = new Options { ComputeReferences = true };
                var geo = e.get_Geometry(opt);
                var roofLinesList = new List<List<RoofLine>>();
                foreach (var o in geo)
                {
                    var solid = (Solid)o;
                    foreach (Face face in solid.Faces)
                    {

                        var pface = face as PlanarFace;
                        if (pface != null && !(pface.FaceNormal.Z > 0)) continue;
                        var edgeLoops = face.EdgeLoops;
                        foreach (EdgeArray edgeLoop in edgeLoops)
                        {
                            roofLinesList.Add(new List<RoofLine>());
                            foreach (Edge edge in edgeLoop)
                            {
                                var curve = edge.AsCurve();
                                var line = curve as Line;
                                if (line == null)
                                    continue;
                                roofLinesList.Last().Add(new RoofLine(new Line3d(new Point3d(line.GetEndPoint(0).X * 12, line.GetEndPoint(0).Y * -12, line.GetEndPoint(0).Z * 12), new Point3d(line.GetEndPoint(1).X * 12, line.GetEndPoint(1).Y * -12, line.GetEndPoint(1).Z * 12)), false));
                            }
                        }
                    }
                }
                foreach (var roofLines in roofLinesList)
                {
                    var roofPolygon = new Polygon3DInfo { Points = new List<Point3D>() };
                    roofPolygon.Points.Add(new Point3D(roofLines[0].Line.P1.x, roofLines[0].Line.P1.y, roofLines[0].Line.P1.z));
                    roofPolygon.Points.Add(new Point3D(roofLines[0].Line.P2.x, roofLines[0].Line.P2.y, roofLines[0].Line.P2.z));
                    roofLines[0].Taken = true;
                    for (int i = 1; i < roofLines.Count; i++)
                    {
                        var notTakenLines = roofLines.Where(ed => ed.Taken == false).ToList();
                        var lastPoint = new Point3d(roofPolygon.Points.Last().X, roofPolygon.Points.Last().Y, roofPolygon.Points.Last().Z);
                        int index = notTakenLines.FindIndex(item => item.Line.P1.DistanceTo(lastPoint) < 0.1 || item.Line.P2.DistanceTo(lastPoint) < 0.1);
                        int indexInRoofSlices = roofLines.FindIndex(ed2 => ed2 == notTakenLines[index]);
                        if (indexInRoofSlices > -1)
                        {
                            if (roofLines[indexInRoofSlices].Line.P1.DistanceTo(lastPoint) < 0.1)
                            {
                                roofPolygon.Points.Add(new Point3D(roofLines[indexInRoofSlices].Line.P2.x, roofLines[indexInRoofSlices].Line.P2.y, roofLines[indexInRoofSlices].Line.P2.z));
                            }
                            else
                            {
                                roofPolygon.Points.Add(new Point3D(roofLines[indexInRoofSlices].Line.P1.x, roofLines[indexInRoofSlices].Line.P2.y, roofLines[indexInRoofSlices].Line.P1.z));
                            }
                            roofLines[indexInRoofSlices].Taken = true;
                        }
                    }
                    if (roofPolygon.Points.Any())
                    {
                        roofs.Add(roofPolygon);
                    }

                }
                return roofs;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }

        public static List<Polygon3DInfo> GetAllRoofs(Document doc)
        {
            try
            {
                var fec = new FilteredElementCollector(doc).OfClass(typeof(FootPrintRoof));
                var result = new List<Polygon3DInfo>();
                foreach (var roof in fec)
                {
                    if (roof.Name.Contains("Ceiling")) continue;
                    result.AddRange(GetRoof(roof));
                }
                return result;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }

        }

    }
}
