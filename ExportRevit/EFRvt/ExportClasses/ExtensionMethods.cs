using Autodesk.Revit.DB;
using EFRvt.Revit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EFRvt
{
    public static class ExtensionMethods
    {
        private static List<int> beamsIds = new List<int>();
        private static Dictionary<int, List<EFLine>> wallAttatchmentsDict;
        private static List<int> RoofsIds = new List<int>();
        private static List<ModelLine> SlopeArrows = new List<ModelLine>();
        public static EFBuilding GetInstance(Document doc, out string failMessage)
        {
            FloorInfo[] floorsinfos = null;
            failMessage = "";

            if (MessageBox.Show("Set Eframer Floors specifically ?", "Floors Mapping", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                PickFloorForm frm = new PickFloorForm(GetSortedLevels(doc));
                if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {

                }
                failMessage = ErrorMessages.NoFloorErrors;
                floorsinfos = frm.floorInfos;
            }
            else
            {
                floorsinfos = CreateFloorInfos(GetSortedLevels(doc), out failMessage);
            }

            if (floorsinfos == null || !floorsinfos.Any())
                return null;

            ExportingConfigrationFrm Configfrm = new ExportingConfigrationFrm();
            ExportingOptions options = Configfrm.Options;
            if (Configfrm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

            }
            EFBuilding result = new EFBuilding();
            result.InitializeMinMax();
            result.BaseElevation = floorsinfos[0].Levels.BaseReferencelevel.Elevation;
            if (!result.AddFlooList(floorsinfos))
                return null;
            beamsIds.Clear();
            RoofsIds.Clear();
            SlopeArrows.Clear();
            wallAttatchmentsDict = new Dictionary<int, List<EFLine>>();
            if (options.ExportColumns)
            {
                result.AddAllPosts(doc);
            }
            List<RoofBase> allRoofs = GetAllRoofs(doc);
            List<Opening> allopenings = GetAllRoofOpenings(doc);

            List<FootPrintRoof> footPrintRoofs = allRoofs.Where(x => x is FootPrintRoof).Cast<FootPrintRoof>().ToList();
            footPrintRoofs.ForEach(x => allRoofs.Remove(x));

            List<Opening> FootPrintsopenings = allopenings.Where(x => x.Host is FootPrintRoof).ToList();
            FootPrintsopenings.ForEach(x => allopenings.Remove(x));

            List<Wall> walls = GetAllWalls(doc);
            if (options.ExportFootPrintRoofs || options.ExportGenericRoofs)
            {
                walls.ForEach(x => AddWallattachmentToDict(doc, x));
            }
            if (options.ExportFootPrintRoofs)
            {
                SlopeArrows = new FilteredElementCollector(doc).OfClass(typeof(CurveElement)).Where(x => x is ModelLine).Cast<ModelLine>().Where(x => x.Name == "Slope Arrow").ToList();
                result.AddAllFootPrintRoof(doc, footPrintRoofs, FootPrintsopenings);
            }
            if (options.ExportGenericRoofs)
            {
                result.AddAllBasicRoofs(doc, allRoofs, allopenings);
            }
            if (options.ExportWalls)
            {
                walls.ForEach(x => result.AddSingleWall(doc, x));
            }
            if (options.ExportBeamsSystems)
            {
                result.AddAllBeamSystems(doc);
            }
            else
            {
                result.AddAllBeamSystemsIDs(doc);
            }
            if (options.ExportBeams)
            {
                result.AddAllBeams(doc);
            }
            return result;
        }

        private static void AddWallattachmentToDict(Document doc, Wall wall)
        {
            EFLine line = ConvertLineLocationToMapLine(wall.Location);
            if (line == null)
                return;
            List<RoofBase> roofs = wall.GetAttatchedRoof(doc);
            if (roofs == null || !roofs.Any())
                return;

            List<string> result = new List<string>();
            foreach (var roof in roofs)
            {
                int intValue = roof.Id.IntegerValue;
                if (wallAttatchmentsDict.ContainsKey(intValue))
                {
                    wallAttatchmentsDict[intValue].Add(line);
                }
                else
                {
                    wallAttatchmentsDict.Add(intValue, new List<EFLine>() { line });
                }

            }

        }

        private static FloorInfo[] CreateFloorInfos(List<Level> SortedLevels, out string failMessage)
        {
            failMessage = "";
            List<FloorInfo> floorInfos = new List<FloorInfo>();
            ReferanceLevel baseLevel = new ReferanceLevel();
            int LvlsCount = SortedLevels.Count;
            if (LvlsCount < 3)
            {
                failMessage = ErrorMessages.NoFloorErrors;
                return null;
            }

            for (int i = 0; i < LvlsCount - 1; i += 3)
            {
                ReferanceLevel BottomLevel = new ReferanceLevel() { ModelLevel = SortedLevels[i], Elevation = SortedLevels[i].Elevation };
                if (i + 1 >= LvlsCount - 1)
                {
                    failMessage = ErrorMessages.NoFloorErrors;
                    return null;
                }
                ReferanceLevel PlateLevel = new ReferanceLevel() { ModelLevel = SortedLevels[i + 1], Elevation = SortedLevels[i + 1].Elevation };
                if (i + 2 >= LvlsCount)
                {
                    failMessage = ErrorMessages.NoFloorErrors;
                    return null;
                }
                ReferanceLevel FramingLevel = new ReferanceLevel() { ModelLevel = SortedLevels[i + 2], Elevation = SortedLevels[i + 2].Elevation };
                if (i + 3 >= LvlsCount)
                {
                    failMessage = ErrorMessages.NoFloorErrors;
                    return null;
                }
                ReferanceLevel SheathingLevel = new ReferanceLevel() { ModelLevel = SortedLevels[i + 3], Elevation = SortedLevels[i + 3].Elevation };

                FloorReferenceLevels levels = new FloorReferenceLevels()
                {
                    BaseReferencelevel = BottomLevel,
                    TopPlateReferencelevel = PlateLevel,
                    FramingReferencelevel = FramingLevel,
                    NextFloorBaseReferencelevel = SheathingLevel
                };

                FloorInfo info = new FloorInfo(i / 3);
                info.Levels = levels;
                info.Heights = new FloorHeights();
                info.UpdateHeights();

                floorInfos.Add(info);

            }

            return floorInfos.ToArray();
        }

        public static bool AddFlooList(this EFBuilding eFBuilding, FloorInfo[] floorsinfos)
        {
            for (int i = 0; i < floorsinfos.Length; i++)
            {
                EFFloor floor = new EFFloor(floorsinfos[i].Heights, floorsinfos[i].Levels.BaseReferencelevel.Elevation);
                eFBuilding.FloorList.Add(floor);
            }
            return true;
        }
        public static List<Level> GetSortedLevels(Document doc)
        {
            return new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(x => x.Elevation).ToList();
        }
        internal static void ResetMinMax(this EFBuilding eFBuilding, BoundingBoxXYZ boundingBox)
        {
            if (boundingBox == null)
                return;
            if (boundingBox.Max != null)
            {
                eFBuilding.MaxX = Math.Max(eFBuilding.MaxX, boundingBox.Max.X);
                eFBuilding.MaxY = Math.Max(eFBuilding.MaxY, boundingBox.Max.Y);
            }
            if (boundingBox.Min != null)
            {
                eFBuilding.MinX = Math.Min(eFBuilding.MinX, boundingBox.Min.X);
                eFBuilding.MinY = Math.Min(eFBuilding.MinY, boundingBox.Min.Y);
            }
        }

        #region Beams
        public static List<FamilyInstance> GetAllBeams(Document doc)
        {
            return new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFraming).Cast<FamilyInstance>()
                   .Where(x => x.Location is LocationCurve).ToList();
        }
        public static void AddAllBeams(this EFBuilding eFBuilding, Document doc)
        {
            List<FamilyInstance> Beams = GetAllBeams(doc);
            Beams.ForEach(x => eFBuilding.AddSingleBeam(doc, x));
        }
        public static void AddSingleBeam(this EFBuilding eFBuilding, Document doc, FamilyInstance beam)
        {
            if (beamsIds.Contains(beam.Id.IntegerValue))
                return;
            if (beam.get_Parameter(BuiltInParameter.Z_JUSTIFICATION).AsValueString() != "Top")
                return;

            double z = (doc.GetElement(beam.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM).AsElementId()) as Level).Elevation;
            z += beam.get_Parameter(BuiltInParameter.Z_OFFSET_VALUE).AsDouble();


            EFFloor floor = eFBuilding.FloorList.Where(x => z > x.BaseElevation && Math.Abs(z - x.BaseElevation) > 0.0001)
                                       .OrderBy(x => z - x.BaseElevation).FirstOrDefault();
            if (floor == null)
                return;
            EFLine centerLine = ConvertLineLocationToMapLine(beam.Location);
            if (centerLine == null || !centerLine.IsHorizontal())
                return;
            EFBeam eFBeam = new EFBeam();
            eFBeam.Line = centerLine;
            eFBeam.TopOffset = z - floor.BaseElevation;
            floor.FloorObjects.Add(eFBeam);

            eFBuilding.ResetMinMax(beam.get_BoundingBox(doc.ActiveView));
        }
        #endregion

        #region Posts
        public static List<FamilyInstance> GetAllPosts(Document doc)
        {
            return new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralColumns).Cast<FamilyInstance>()
                .Concat(new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Columns).Cast<FamilyInstance>()).ToList();
        }
        public static void AddAllPosts(this EFBuilding eFBuilding, Document doc)
        {
            List<FamilyInstance> Posts = GetAllPosts(doc);
            Posts.ForEach(x => eFBuilding.AddSinglePost(x, doc));
        }
        public static void AddSinglePost(this EFBuilding eFBuilding, FamilyInstance post, Document doc)
        {
            double Z1 = (doc.GetElement(post.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).AsElementId()) as Level).Elevation;
            Z1 += post.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).AsDouble();

            if (Z1 < eFBuilding.BaseElevation)
            {
                Z1 = eFBuilding.BaseElevation;
            }
            double Z2 = (doc.GetElement(post.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).AsElementId()) as Level).Elevation;
            Z2 += post.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).AsDouble();


            EFFloor firstFloor = eFBuilding.FloorList.Where(x => Z1 > x.BaseElevation || Math.Abs(Z1 - x.BaseElevation) < 0.0001)
                                            .OrderBy(x => Z1 - x.BaseElevation).FirstOrDefault();
            EFFloor lastFloor = eFBuilding.FloorList.Where(x => Z2 > x.BaseElevation && Math.Abs(Z2 - x.BaseElevation) > 0.0001)
                                            .OrderBy(x => Z2 - x.BaseElevation).FirstOrDefault();

            if (firstFloor == null || lastFloor == null)
                return;

            LocationPoint location = post.Location as LocationPoint;
            if (location == null)
                return;

            double baseOffset = Z1 - firstFloor.BaseElevation;
            if (baseOffset < 0.0001)
            {
                baseOffset = 0.0;
            }
            double TopOffset = Z2 - lastFloor.BaseElevation;
            if (TopOffset < 0.0001)
            {
                TopOffset = 0.0;
            }

            EFPointLocation EFlocation = new EFPointLocation(CreateEFPoint(location.Point), location.Rotation);
            if (firstFloor == lastFloor)
            {
                EFPost eFpost = new EFPost();
                eFpost.location = EFlocation;
                eFpost.BottomOffset = baseOffset;
                eFpost.TopOffset = TopOffset;
                lastFloor.FloorObjects.Add(eFpost);
            }
            else
            {
                int k = eFBuilding.FloorList.IndexOf(firstFloor);
                int j = eFBuilding.FloorList.IndexOf(lastFloor);
                {
                    EFFloor f = eFBuilding.FloorList[k];
                    EFPost eFpost = new EFPost();
                    eFpost.location = EFlocation;
                    eFpost.BottomOffset = baseOffset;
                    eFpost.TopOffset = f.SubLevelElevation - f.BaseElevation;
                    f.FloorObjects.Add(eFpost);
                }
                for (int i = k + 1; i < j; i++)
                {
                    EFFloor f = eFBuilding.FloorList[i];
                    EFPost eFpost = new EFPost();
                    eFpost.location = EFlocation;
                    eFpost.TopOffset = f.SubLevelElevation - f.BaseElevation;
                    f.FloorObjects.Add(eFpost);
                }
                {
                    EFPost eFpost = new EFPost();
                    eFpost.location = EFlocation;
                    eFpost.TopOffset = TopOffset;
                    lastFloor.FloorObjects.Add(eFpost);
                }
            }

            eFBuilding.ResetMinMax(post.get_BoundingBox(doc.ActiveView));
        }
        #endregion

        #region Foot Print Roof
        public static List<RoofBase> GetAllRoofs(Document doc)
        {
            return new FilteredElementCollector(doc).OfClass(typeof(RoofBase)).Cast<RoofBase>().ToList();
        }
        public static List<Opening> GetAllRoofOpenings(Document doc)
        {
            return new FilteredElementCollector(doc).OfClass(typeof(Opening)).Cast<Opening>().Where(x => x.Host is RoofBase).ToList();
        }
        public static void AddAllFootPrintRoof(this EFBuilding eFBuilding, Document doc, IEnumerable<FootPrintRoof> Roofs, IEnumerable<Opening> allopening)
        {
            foreach (var Roof in Roofs)
            {
                eFBuilding.AddSingleFootPrintRoof(doc, Roof, allopening.Where(o => o.Host.Id.IntegerValue == Roof.Id.IntegerValue).ToList());
            }
        }
        public static void AddAllBasicRoofs(this EFBuilding eFBuilding, Document doc, IEnumerable<RoofBase> Roofs, IEnumerable<Opening> allopening)
        {
            foreach (var Roof in Roofs)
            {
                eFBuilding.AddSingleBasicRoof(doc, Roof, allopening.Where(o => o.Host.Id.IntegerValue == Roof.Id.IntegerValue).ToList());
            }
        }
        internal static EFProfile GetExteriorFaceProfile(Face face, double BaseLevel)
        {
            EFProfile listFace = new EFProfile();
            double Area = double.MinValue;
            foreach (CurveLoop ea in face.GetEdgesAsCurveLoops())
            {
                List<XYZ> clistFace = new List<XYZ>();
                var iterator = ea.GetCurveLoopIterator();
                iterator.Reset();
                while (iterator.MoveNext())
                {
                    Line line = iterator.Current as Line;
                    if (line == null)
                        return null;

                    XYZ edgeStart = iterator.Current.GetEndPoint(0);
                    clistFace.Add(new XYZ(edgeStart.X, edgeStart.Y, edgeStart.Z - BaseLevel));
                }

                double cArea;
                double dist;
                XYZ normal;
                GetPolygonPlane(clistFace, out normal, out dist, out cArea);
                if (cArea > Area)
                {
                    listFace.PointList = clistFace.Select(x => new EFXYZ(x.X, x.Y, x.Z)).ToList();
                    Area = cArea;
                }
            }
            return listFace;
        }
        public static bool GetPolygonPlane(List<XYZ> polygon, out XYZ normal, out double dist, out double area)
        {
            normal = XYZ.Zero;
            dist = area = 0.0;
            int n = (null == polygon) ? 0 : polygon.Count;
            bool rc = (2 < n);
            if (3 == n)
            {
                XYZ a = polygon[0];
                XYZ b = polygon[1];
                XYZ c = polygon[2];
                XYZ v = b - a;
                normal = v.CrossProduct(c - a);
                dist = normal.DotProduct(a);
            }
            else if (4 == n)
            {
                XYZ a = polygon[0];
                XYZ b = polygon[1];
                XYZ c = polygon[2];
                XYZ d = polygon[3];

                double x = (c.Y - a.Y) * (d.Z - b.Z)
                  + (c.Z - a.Z) * (b.Y - d.Y);
                double y = (c.Z - a.Z) * (d.X - b.X)
                  + (c.X - a.X) * (b.Z - d.Z);
                double z = (c.X - a.X) * (d.Y - b.Y)
                  + (c.Y - a.Y) * (b.X - d.X);

                normal = new XYZ(x, y, z);
                dist = 0.25 *
                  (normal.X * (a.X + b.X + c.X + d.X)
                  + normal.Y * (a.Y + b.Y + c.Y + d.Y)
                  + normal.Z * (a.Z + b.Z + c.Z + d.Z));
            }
            else if (4 < n)
            {
                XYZ a;
                XYZ b = polygon[n - 2];
                XYZ c = polygon[n - 1];
                XYZ s = XYZ.Zero;

                for (int i = 0; i < n; ++i)
                {
                    a = b;
                    b = c;
                    c = polygon[i];

                    normal += b.Y * (c.Z - a.Z) * XYZ.BasisX;
                    normal += b.Z * (c.X - a.X) * XYZ.BasisY;
                    normal += b.X * (c.Y - a.Y) * XYZ.BasisZ;

                    s += c;
                }
                dist = s.DotProduct(normal) / n;
            }
            if (rc)
            {
                double length = normal.GetLength();
                rc = length > 0.0;

                if (rc)
                {
                    normal /= length;
                    dist /= length;
                    area = 0.5 * length;
                }
            }
            return rc;
        }

        internal static void AddSingleBasicRoof(this EFBuilding eFBuilding, Document doc, RoofBase roof, List<Opening> allOpenings)
        {
            List<Face> bottomFaces = GetFaces(roof, doc);
            List<EFProfile> points = new List<EFProfile>();
            double zMin = Double.MaxValue;
            foreach (var face in bottomFaces)
            {
                if (face == null)
                    continue;
                EFProfile listFace = GetExteriorFaceProfile(face, eFBuilding.BaseElevation);

                if (listFace != null)
                {
                    points.Add(listFace);
                }
            }

            EFGenericRoof efroof = new EFGenericRoof();
            efroof.RoofPolygons = points;
            if (allOpenings.Any())
            {
                efroof.OpeningList = AddRoofOpenings(allOpenings);
            }
            efroof.ID = Guid.NewGuid();
            eFBuilding.LowRoofs.Add(efroof);
            if (wallAttatchmentsDict.ContainsKey(roof.Id.IntegerValue))
            {
                efroof.AttachedWallsLines = wallAttatchmentsDict[roof.Id.IntegerValue];
            }
            eFBuilding.ResetMinMax(roof.get_BoundingBox(doc.ActiveView));

        }
        internal static List<Face> GetFaces(RoofBase roof, Document doc)
        {
            List<Face> faces = new List<Face>();
            IList<Reference> references = HostObjectUtils.GetBottomFaces(roof);
            foreach (var referenece in references)
            {
                Element e2 = doc.GetElement(referenece);
                faces.Add(e2.GetGeometryObjectFromReference(referenece) as Face);
            }
            return faces;
        }
        public static void AddSingleFootPrintRoof(this EFBuilding eFBuilding, Document doc, FootPrintRoof roof, List<Opening> allOpenings)
        {
            try
            {
                ElementId TopLevelId = roof.get_Parameter(BuiltInParameter.ROOF_BASE_LEVEL_PARAM).AsElementId();
                double Z0 = (doc.GetElement(TopLevelId) as Level).Elevation;
                Z0 += roof.get_Parameter(BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM).AsDouble();

                EFRoof efroof = new EFRoof();
                efroof.eFFootprintEdges = AddRoofBaseLines(roof, Z0);
                if (efroof.eFFootprintEdges.Count(x => !x.Gable) < 2)
                {
                    eFBuilding.AddSingleBasicRoof(doc, roof, allOpenings);
                    return;
                }
                double minZ = efroof.eFFootprintEdges.Select(x => x.Z).Min();

                EFFloor floor = eFBuilding.FloorList.Where(x => minZ > x.TopPlateLevelElevation || Math.Abs(x.TopPlateLevelElevation - minZ) < 0.0001)
                    .OrderBy(x => minZ - x.TopPlateLevelElevation).FirstOrDefault();

                double datum = (floor == null) ? eFBuilding.BaseElevation : floor.TopPlateLevelElevation;
                efroof.eFFootprintEdges.ForEach(x =>
                {
                    x.Offset = x.Z - datum;
                    if (x.Offset < 0.0001)
                    {
                        x.Offset = 0;
                    }
                });
                if (allOpenings.Any())
                {
                    efroof.OpeningList = AddRoofOpenings(allOpenings);
                }
                efroof.ID = Guid.NewGuid();
                if (floor == null)
                {
                    eFBuilding.LowRoofs.Add(efroof);
                }
                else
                {
                    floor.Roofs.Add(efroof);
                }
                eFBuilding.ResetMinMax(roof.get_BoundingBox(doc.ActiveView));
            }
            catch (Exception e)
            { }

        }

        private static List<EFProfile> AddRoofOpenings(List<Opening> allOpenings)
        {
            List<EFProfile> result = new List<EFProfile>();
            foreach (var opening in allOpenings)
            {
                EFProfile p = new EFProfile();
                for (int i = 0; i < opening.BoundaryCurves.Size; i++)
                {
                    XYZ startpoint = opening.BoundaryCurves.get_Item(i).Tessellate()[0];
                    p.PointList.Add(new EFXYZ(startpoint.X, startpoint.Y, 0.0));
                }
                result.Add(p);
            }
            return result;
        }

        private static List<EFFootprintEdge> AddRoofBaseLines(FootPrintRoof roof, double Z0)
        {
            List<EFFootprintEdge> result = new List<EFFootprintEdge>();
            ModelCurveArrArray arrarray = roof.GetProfiles();
            for (int i = 0; i < arrarray.Size; i++)
            {
                ModelCurveArray array = arrarray.get_Item(i);
                for (int j = 0; j < array.Size; j++)
                {
                    ModelCurve curve = array.get_Item(j);
                    EFFootprintEdge el = new EFFootprintEdge();
                    el.Gable = !roof.get_DefinesSlope(curve);
                    el.Z = Z0;
                    try
                    {
                        el.OverHang = roof.get_Overhang(curve);
                        el.Z += curve.get_Parameter(BuiltInParameter.ROOF_CURVE_HEIGHT_AT_WALL).AsDouble();
                    }
                    catch
                    {
                        el.Z += curve.get_Parameter(BuiltInParameter.ROOF_CURVE_HEIGHT_OFFSET).AsDouble();
                    }

                    SetFootprintEdgeStartandEndPoints(el, curve);

                    if (el.Gable)
                    {
                        el.Slope = 12.00;
                    }
                    else
                    {
                        double slope = roof.get_SlopeAngle(curve);
                        el.Offset += slope * el.OverHang;
                        el.Slope = slope * 12;

                    }
                    ModelLine SlopeArrow = SlopeArrows.FirstOrDefault(x => IsGeometrallyMatch(x, curve));
                    if (SlopeArrow != null)
                    {
                        el.SlopeArrow = true;
                        el.Slope = SlopeArrow.get_Parameter(BuiltInParameter.ROOF_SLOPE).AsDouble() * 12;
                        Line l = SlopeArrow.GeometryCurve as Line;
                        XYZ dir = (l.GetEndPoint(1) - l.GetEndPoint(0)).Normalize();
                        el.SlopeArrowDirection = new EFXYZ(dir.X, dir.Y, 0.0);
                        SlopeArrows.Remove(SlopeArrow);
                    }
                    result.Add(el);
                }
            }

            trimEndsOfFootprintEdges(result);
            return result;
        }

        private static bool IsGeometrallyMatch(ModelLine modelLine, ModelCurve curve)
        {
            Line l1 = modelLine.GeometryCurve as Line;
            Line l2 = curve.GeometryCurve as Line;
            if (l1 == null || l2 == null)
                return false;

            return IsEqualLines(l1, l2);
        }

        private static bool IsEqualLines(Line l1, Line l2)
        {
            double t = 0.001;
            return (l1.GetEndPoint(0).IsAlmostEqualTo(l2.GetEndPoint(0), t) && l1.GetEndPoint(1).IsAlmostEqualTo(l2.GetEndPoint(1), t))
                    ||
                    (l1.GetEndPoint(1).IsAlmostEqualTo(l2.GetEndPoint(0), t) && l1.GetEndPoint(0).IsAlmostEqualTo(l2.GetEndPoint(1), t));
        }

        public static void trimEndsOfFootprintEdges(List<EFFootprintEdge> efFootprintEdges)
        {
            try
            {
                List<EFXYZ> newBasepolygon = new List<EFXYZ>();

                for (int i = 0; i < efFootprintEdges.Count; i++)
                {
                    int j = (i + 1) % efFootprintEdges.Count;

                    newBasepolygon.Add((GeometryMathatics.Two_Lines_Intersect(efFootprintEdges[i].Line.P1, efFootprintEdges[i].Line.P2
                        , efFootprintEdges[j].Line.P1, efFootprintEdges[j].Line.P2)));
                }

                for (int i = 0; i < efFootprintEdges.Count; i++)
                {
                    int j = (i + 1) % efFootprintEdges.Count;

                    efFootprintEdges[i].Line.P2 = newBasepolygon[i];
                    efFootprintEdges[j].Line.P1 = newBasepolygon[i];
                }

            }
            catch (Exception ex)
            {
            }
        }

        private static void SetFootprintEdgeStartandEndPoints(EFFootprintEdge el, ModelCurve curve)
        {
            try
            {
                XYZ start = curve.GeometryCurve.Tessellate()[0];
                XYZ end = curve.GeometryCurve.Tessellate()[1];
                XYZ direcrion = el.OverHang * ((end - start).CrossProduct(new XYZ(0, 0, -1)).Normalize());
                start = start + direcrion;
                end = end + direcrion;
                el.Line = new EFLine(CreateEFPoint(start), CreateEFPoint(end)).To2D();
            }
            catch (Exception ex)
            {
            }
        }
        #endregion

        #region Wall
        public static List<Wall> GetAllWalls(Document doc)
        {
            return new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().ToList();
        }

        public static void AddSingleWall(this EFBuilding eFBuilding, Document doc, Wall wall)
        {
            if (wall.get_Parameter(BuiltInParameter.WALL_BOTTOM_IS_ATTACHED).AsInteger() != 0)
                return;

            double z1 = (doc.GetElement(wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).AsElementId()) as Level).Elevation;
            z1 += wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).AsDouble();

            if (z1 < eFBuilding.BaseElevation)
            {
                z1 = eFBuilding.BaseElevation;
            }

            double z2 = z1;
            if (wall.get_Parameter(BuiltInParameter.WALL_TOP_IS_ATTACHED).AsInteger() == 0)
            {
                z2 = (doc.GetElement(wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsElementId()) as Level).Elevation;
                z2 += wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET).AsDouble();
            }
            else
            {
                z2 += wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble();
            }

            EFFloor floor = eFBuilding.FloorList.Where(x => z1 > x.BaseElevation || Math.Abs(z1 - x.BaseElevation) < 0.0001)
                                            .OrderBy(x => z1 - x.BaseElevation).FirstOrDefault();
            if (floor == null)
                return;
            double baseOffset = z1 - floor.BaseElevation;
            if (baseOffset < 0.0001)
            {
                baseOffset = 0.0;
            }
            EFWall efWall = new EFWall();
            efWall.Line = ConvertLineLocationToMapLine(wall.Location);
            if (efWall.Line == null)
                return;
            efWall.BottomOffset = baseOffset;
            efWall.TopOffset = z2 - floor.BaseElevation;
            int linePosition = wall.get_Parameter(BuiltInParameter.WALL_KEY_REF_PARAM).AsInteger();
            if (linePosition == 0 || linePosition == 1)
            {
                efWall.Orienation = new EFXYZ();
            }
            else
            {
                efWall.Orienation = CreateEFPoint(wall.Orientation);
            }

            IList<Reference> references = HostObjectUtils.GetSideFaces(wall, ShellLayerType.Interior);
            foreach (var referenece in references)
            {
                Element e2 = doc.GetElement(referenece);
                Face SideFace = e2.GetGeometryObjectFromReference(referenece) as Face;
                if (SideFace != null)
                {
                    efWall.SideFace = GetExteriorFaceProfile(SideFace, eFBuilding.BaseElevation);
                    break;
                }
            }
            //efWall.AttatchedRoofs_IDs = wall.GetAttatchedRoofID(doc);
            efWall.AddWallOpenings(wall, floor, doc);
            floor.FloorObjects.Add(efWall);
            eFBuilding.ResetMinMax(wall.get_BoundingBox(doc.ActiveView));
        }
        public static List<RoofBase> GetAttatchedRoof(this Wall wall, Document doc)
        {
            ICollection<ElementId> joinedElements = new Collection<ElementId>(); // collection to store the walls joined to the selected wall
            List<RoofBase> result = new List<RoofBase>();
            // find all faces of the selected wall
            GeometryElement geometryElement = wall.get_Geometry(new Options());
            foreach (GeometryObject geometryObject in geometryElement)
            {
                if (geometryObject is Solid)
                {
                    Solid solid = geometryObject as Solid;
                    foreach (Face face in solid.Faces)
                    {
                        // for each face, find the other elements that generated the geometry of that face
                        ICollection<ElementId> generatingElementIds = wall.GetGeneratingElementIds(face);

                        generatingElementIds.Remove(wall.Id); // remove the originally selected wall, leaving only other elements joined to it
                        foreach (ElementId id in generatingElementIds)
                        {
                            if (!(joinedElements.Contains(id)))
                                joinedElements.Add(id); // add each wall joined to this face to the overall collection 
                        }
                    }
                }
            }

            foreach (ElementId id in joinedElements)
            {
                RoofBase roof = doc.GetElement(id) as RoofBase;
                if (roof != null)
                {
                    result.Add(roof);
                }
            }

            return result;
        }
        public static void AddWallOpenings(this EFWall eFwall, Wall wall, EFFloor baseFloor, Document doc)
        {
            #region AddOpening
            var wallOpeningsIds = wall.FindInserts(true, true, true, true);
            if (wallOpeningsIds.Count > 0)
            {
                #region DoorFilter
                var doorFilter = new FilteredElementCollector(doc, wallOpeningsIds).OfCategory(BuiltInCategory.OST_Doors);
                if (doorFilter.Any())
                {
                    foreach (var door in doorFilter)
                    {
                        EFDoor efdoor = new EFDoor();
                        efdoor.midPoint = CreateEFPoint(((door as FamilyInstance).Location as LocationPoint).Point);
                        List<double> widthprops = new List<double>();

                        widthprops.Add((door as FamilyInstance).get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble());
                        widthprops.Add((door as FamilyInstance).get_Parameter(BuiltInParameter.GENERIC_WIDTH).AsDouble());
                        widthprops.Add((door as FamilyInstance).Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble());
                        widthprops.Add((door as FamilyInstance).Symbol.get_Parameter(BuiltInParameter.GENERIC_WIDTH).AsDouble());

                        efdoor.Width = widthprops.FirstOrDefault(v => v > 0.0001);
                        efdoor.HeaderHeight = door.get_Parameter(BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM).AsDouble();
                        if (door.LevelId != null)
                        {
                            efdoor.HeaderHeight += (doc.GetElement(door.LevelId) as Level).Elevation - baseFloor.BaseElevation;
                        }
                        eFwall.Opening_List.Add(efdoor);
                    }
                }
                #endregion

                #region WindowFilter
                var windowFilter = new FilteredElementCollector(doc, wallOpeningsIds).OfCategory(BuiltInCategory.OST_Windows);
                if (windowFilter.Any())
                {
                    foreach (var window in windowFilter)
                    {
                        EFWindow efwindow = new EFWindow();
                        efwindow.midPoint = CreateEFPoint(((window as FamilyInstance).Location as LocationPoint).Point);
                        List<double> widthprops = new List<double>();

                        widthprops.Add((window as FamilyInstance).get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble());
                        widthprops.Add((window as FamilyInstance).get_Parameter(BuiltInParameter.GENERIC_WIDTH).AsDouble());
                        widthprops.Add((window as FamilyInstance).Symbol.get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble());
                        widthprops.Add((window as FamilyInstance).Symbol.get_Parameter(BuiltInParameter.GENERIC_WIDTH).AsDouble());

                        efwindow.Width = widthprops.FirstOrDefault(v => v > 0);
                        efwindow.SillHeight = window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).AsDouble();
                        efwindow.HeaderHeight = window.get_Parameter(BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM).AsDouble();
                        if (window.LevelId != null)
                        {
                            double z = (doc.GetElement(window.LevelId) as Level).Elevation - baseFloor.BaseElevation;
                            efwindow.SillHeight += z;
                            efwindow.HeaderHeight += z;
                        }
                        eFwall.Opening_List.Add(efwindow);
                    }
                }
                #endregion
            }
            #endregion
        }
        #endregion

        #region JoistArea

        public static List<BeamSystem> GetAllBeamSystem(Document doc)
        {
            return new FilteredElementCollector(doc).OfClass(typeof(BeamSystem)).OfCategory(BuiltInCategory.OST_StructuralFramingSystem).Cast<BeamSystem>().ToList();
        }
        public static void AddAllBeamSystems(this EFBuilding eFBuilding, Document doc)
        {
            List<BeamSystem> Beams = GetAllBeamSystem(doc);
            Beams.ForEach(x => eFBuilding.AddSingleBeamSystem(doc, x));
        }
        public static void AddAllBeamSystemsIDs(this EFBuilding eFBuilding, Document doc)
        {
            List<BeamSystem> Beams = GetAllBeamSystem(doc);
            Beams.ForEach(x =>
            {
                beamsIds.AddRange(x.GetBeamIds().Select(i => i.IntegerValue));
            });
        }
        internal static void AddSingleBeamSystem(this EFBuilding eFBuilding, Document doc, BeamSystem beamSystem)
        {
            double z = beamSystem.Level.Elevation + beamSystem.Elevation;

            EFFloor floor = eFBuilding.FloorList.Where(x => z > x.BaseElevation && Math.Abs(z - x.BaseElevation) > 0.0001)
                                      .OrderBy(x => z - x.BaseElevation).FirstOrDefault();
            if (floor == null)
                return;

            EFProfile profile = new EFProfile();
            foreach (Curve curve in beamSystem.Profile)
            {
                Line line = curve as Line;
                if (line == null)
                    return;
                profile.PointList.Add(CreateEFPoint(line.GetEndPoint(0)));
            }
            List<EFBeam> beams = new List<EFBeam>();
            List<ElementId> beamids = beamSystem.GetBeamIds().ToList();
            if (beamids.Count < 2)
                return;
            foreach (ElementId BeamId in beamids)
            {
                FamilyInstance beam = doc.GetElement(BeamId) as FamilyInstance;
                if (beam == null)
                    continue;
                EFBeam efbeam = AddSingleBeam(beam);
                if (efbeam == null)
                    continue;
                beams.Add(efbeam);
                beamsIds.Add(BeamId.IntegerValue);
            }
            EFBeamSystem eFBeamSystem = new EFBeamSystem();
            eFBeamSystem.Boundary = profile;
            eFBeamSystem.beams = beams;
            eFBeamSystem.TopOffset = z - floor.BaseElevation;
            floor.FloorObjects.Add(eFBeamSystem);

            eFBuilding.ResetMinMax(beamSystem.get_BoundingBox(doc.ActiveView));
        }

        private static EFBeam AddSingleBeam(FamilyInstance beam)
        {
            EFLine centerLine = ConvertLineLocationToMapLine(beam.Location);
            if (centerLine == null || !centerLine.IsHorizontal())
                return null;
            EFBeam eFBeam = new EFBeam();
            eFBeam.Line = centerLine;
            return eFBeam;
        }
        #endregion

        public static EFLine ConvertLineLocationToMapLine(Location location)
        {
            return ConvertLineLocationToMapLine((location as LocationCurve).Curve);
        }
        public static EFLine ConvertLineLocationToMapLine(Curve curve)
        {
            if (curve == null)
                return null;
            var line = curve as Line;
            if (line == null)
                return null;

            return new EFLine(CreateEFPoint(line.GetEndPoint(0)), CreateEFPoint(line.GetEndPoint(1)));
        }

        public static EFXYZ CreateEFPoint(XYZ p)
        {
            return new EFXYZ(p.X, p.Y, p.Z);
        }
    }
}