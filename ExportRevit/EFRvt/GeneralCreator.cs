
using Elibre.Net.Debug;
using System;
using System.Collections.Generic;

using System.IO;
using System.Linq;

using Autodesk.Revit.DB;
using Elibre.eFramer;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Visual;

namespace EFRvt.Revit
{
    static class GeneralCreator
    {
        public static string rootpath = Path.Combine(Directory.GetParent((System.Reflection.Assembly.GetExecutingAssembly().Location)).FullName, "Revit");

        #region paths
        private static string DB_folder = Path.Combine(rootpath, "DB");
        private static string Studs_Folder = Path.Combine(DB_folder, "Studs");
        private static string newStuds_Folder = Path.Combine(DB_folder, "newStuds");
        private static string CommonTruss_folder = Path.Combine(rootpath, "CommonTrussFamilies");
        #endregion

        #region create Walls
        private static float studheightoffset = 0.1f;


        private static List<MapXYZ> RemoveDuplicatePoints(List<MapXYZ> points)
        {
            List<MapXYZ> result = new List<MapXYZ>();
            MapXYZ previousPoint = points.First();
            result.Add(previousPoint);
            foreach (MapXYZ point in points)
            {
                if (!point.Equals(previousPoint))
                {
                    result.Add(point);
                    previousPoint = point;
                }
            }
            return result;
        }

        private static List<MapXYZ> SimplifyGeometry(List<MapXYZ> points)
        {
            List<MapXYZ> result = new List<MapXYZ>();
            MapXYZ previousPoint = points.First();
            result.Add(previousPoint);
            for (int i = 1; i < points.Count - 1; i++)
            {
                MapXYZ currentPoint = points[i];
                MapXYZ nextPoint = points[i + 1];
                XYZ vector1 = currentPoint.GetReference() - previousPoint.GetReference();
                XYZ vector2 = nextPoint.GetReference() - currentPoint.GetReference();
                if (!vector1.IsAlmostEqualTo(vector2))
                {
                    result.Add(currentPoint);
                    previousPoint = currentPoint;
                }
            }
            result.Add(points.Last());
            return result;
        }

        private static bool IsClosedLoop(List<MapXYZ> points)
        {

            return (points.First().GetReference()).DistanceTo(points.Last().GetReference()) < 0.001;
        }

        private static bool HasSelfIntersectingCurves(List<MapXYZ> points)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Line line1 = Line.CreateBound(points[i].GetReference(), points[i + 1].GetReference());
                for (int j = i + 2; j < points.Count - 1; j++)
                {
                    Line line2 = Line.CreateBound(points[j].GetReference(), points[j + 1].GetReference());
                    if (line1.Intersect(line2) != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool IsValidProfile(CurveArray profile)
        {
            // Check if the profile has at least 3 points
            if (profile.Size < 3) return false;

            // Check if the start and end points of the profile match
            XYZ startPoint = profile.get_Item(0).GetEndPoint(0);
            XYZ endPoint = profile.get_Item(profile.Size - 1).GetEndPoint(1);
            if (!startPoint.IsAlmostEqualTo(endPoint)) return false;

            // Check if all the curves in the profile are connected
            for (int i = 0; i < profile.Size - 1; i++)
            {
                Curve currentCurve = profile.get_Item(i);
                Curve nextCurve = profile.get_Item(i + 1);
                if (!currentCurve.GetEndPoint(1).IsAlmostEqualTo(nextCurve.GetEndPoint(0)))
                    return false;
            }

            return true;
        }

        // Adjust the profile curves to form a closed loop
        public static void AdjustProfile(CurveArray profile)
        {
            // Check if the start and end points of the profile match
            XYZ startPoint = profile.get_Item(0).GetEndPoint(0);
            XYZ endPoint = profile.get_Item(profile.Size - 1).GetEndPoint(1);
            if (!startPoint.IsAlmostEqualTo(endPoint))
            {
                // Connect the end point of the last curve to the start point of the first curve
                Line connectionLine = Line.CreateBound(endPoint, startPoint);
                profile.Append(connectionLine);
            }
        }


        private static List<XYZ> GetSplitPoints(List<XYZ> points, XYZ splitPoint, int startIndex, int endIndex)
        {
            List<XYZ> newPoints = new List<XYZ>();
            for (int i = 0; i < points.Count; i++)
            {
                if (i >= startIndex && i <= endIndex)
                {
                    newPoints.Add(points[i]);
                }
                if (i == endIndex)
                {
                    newPoints.Add(splitPoint);
                }
            }
            return newPoints;
        }

        public static Element CreateRoofSlab(Document doc, Level level, List<MapXYZ> points, double thickness)
        {
            try
            {
                // Get the highest level in the project
                FilteredElementCollector collector
                     = new FilteredElementCollector(doc)
                     .OfClass(typeof(Level));


                // Get the default roof type in the project
                collector = new FilteredElementCollector(doc);
                collector.OfClass(typeof(RoofType));
                RoofType roofType = collector.LastOrDefault() as RoofType;


                // Create the profile curves
                CurveArray profile = new CurveArray();
                for (int i = 0; i < points.Count - 1; i++)
                {
                    profile.Append(Line.CreateBound(points[i].GetReference(),
                        points[i + 1].GetReference()));

                    // Check if the profile curves are valid
                    if (!IsValidProfile(profile))
                    {
                        // If the profile curves are not valid, adjust them
                        AdjustProfile(profile);
                    }
                }
                ReferencePlane referencePlane = CreateReferencePlane(doc, profile);
                ExtrusionRoof roof = doc.Create.NewExtrusionRoof(profile, referencePlane, level, roofType, thickness, 0);
                return roof;

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        private static ReferencePlane CreateReferencePlane(Document doc, CurveArray curves)
        {
            XYZ normal = XYZ.BasisZ;
            XYZ origin = curves.get_Item(0).GetEndPoint(0);
            return doc.Create.NewReferencePlane(origin, normal, XYZ.BasisX, doc.ActiveView);
        }

        private static List<Curve> AdjustCurves(Document doc, List<MapXYZ> points)
        {
            List<Curve> curves = new List<Curve>();
            int n = points.Count;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                XYZ start = points[i].GetReference();
                XYZ end = points[j].GetReference();
                Line line = Line.CreateBound(start, end);
                curves.Add(line);
            }
            return curves;
        }
        private static bool IsValidCurveLength(XYZ point1, XYZ point2)
        {
            double curveLength = (point1 - point2).GetLength();
            if (curveLength < 0.01) // 0.01 is an example of a minimum curve length
            {
                return false;
            }
            return true;
        }
        public static Element CreateRoofSlabs(Document doc, Level level, List<MapXYZ> points, double thickness)
        {
            try
            {
                if (!((points[0]).GetReference()).IsAlmostEqualTo(((points[points.Count - 1]).GetReference())))
                {
                    points.Add(points[0]);
                }

                FilteredElementCollector collector
                     = new FilteredElementCollector(doc)
                     .OfClass(typeof(Level));


                collector = new FilteredElementCollector(doc);
                collector.OfClass(typeof(RoofType));
                RoofType roofType = collector.LastOrDefault() as RoofType;
                #region checks
                //Simplify and validate the input curve
                //List<MapXYZ> newPoints = RemoveDuplicatePoints(points);
                //points = SimplifyGeometry(points);
                //if (!IsClosedLoop(newPoints))
                //{
                //    return false;
                //}
                //if (HasSelfIntersectingCurves(points))
                //{

                //return false;

                //}
                // Check if the curve array is null
                //if (newPoints.Count < 2)
                //{
                //    return false;
                //}
                #endregion
                CurveArray curves = new CurveArray();

                for (int i = 0; i < points.Count - 1; i++)
                {


                    Curve curve1 = Line.CreateBound(points[i].GetReference(), points[i + 1].GetReference());
                    //if (curve1.Length < 1e-9)
                    //{
                    //    return false;
                    //}
                    curves.Append(curve1);

                }
                //Curve finalCurve = Line.CreateBound(points[points.Count - 1].GetReference(), points[0].GetReference());
                //if (finalCurve.Length < 1e-9)
                //{
                //    return false;
                //}
                //curves.Append(finalCurve);


                //// Create a FootPrintRoof object
                //FootPrintRoof footPrintRoof = FootPrintRoof.Create(doc, curveArray, false);







                ModelCurveArray modelCurveArrArray = new ModelCurveArray();
                FootPrintRoof footPrintRoof = doc.Create.NewFootPrintRoof(curves, level, roofType, out modelCurveArrArray);
                //if (footPrintRoof == null)
                //{
                //    return false;
                //}
                //int counter = 0;
                //string paramName = "Thickness";
                //while (footPrintRoof.LookupParameter(paramName) != null)
                //{
                //    paramName = "Thickness" + counter;
                //    counter++;
                //}
                //footPrintRoof.get_Parameter(BuiltInParameter.ROOF_SLOPE).Set(thickness);
                // Set the roof thickness
                //footPrintRoof.get_Parameter(BuiltInParameter.ROOF_THICKNESS_PARAM).Set(thickness);
                ModelCurveArrayIterator iterator = modelCurveArrArray.ForwardIterator();
                iterator.Reset();
                while (iterator.MoveNext())
                {
                    ModelCurve modelCurve = iterator.Current as ModelCurve;
                    footPrintRoof.set_DefinesSlope(modelCurve, true);
                    footPrintRoof.set_SlopeAngle(modelCurve, 0.8);

                }

                //// Set the roof's thickness
                //roof.get_Parameter(BuiltInParameter.ROOF_ATTR_DEFAULT_THICKNESS_PARAM).Set(thickness);
                return footPrintRoof;

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;

            }


        }


        #region Create Top / Bottom plate
        public static List<Element> CreateWallTopVoidPlate(Document doc, List<XYZ> topprofile, float width, float depth)
        {

            try
            {
                if (doc == null || topprofile.Count < 2 || topprofile == null || width < 0 || depth < 0)
                    return null;

                Family fam = CheckIfFamilyIsUploaded(doc, "EFVoidPlate", DB_folder);
                FamilySymbol symbol = SelectSuitableBeamFamilySymbol(doc, fam, width, depth);
                List<Element> results = new List<Element>();
                for (int i = 0; i < topprofile.Count - 1; i++)
                {
                    XYZ top = topprofile[i];
                    XYZ bottom = topprofile[i + 1];

                    if (Math.Abs(bottom.Z - top.Z) < 0.0001)
                    {
                        results.Add(null);
                        continue;
                    }
                    else if (bottom.Z > top.Z)
                    {
                        XYZ temp = bottom;
                        bottom = top;
                        top = temp;
                    }

                    XYZ center = new XYZ((top.X + bottom.X) / 2, (top.Y + bottom.Y) / 2, (top.Z + bottom.Z) / 2);
                    XYZ top_temp = new XYZ(top.X, top.Y, center.Z);
                    XYZ bottom_temp = new XYZ(bottom.X, bottom.Y, center.Z);
                    double cosSlope = (top - center).Normalize().DotProduct((top_temp - center).Normalize());
                    double upDistance = depth / (12.00 * cosSlope);
                    FamilyInstance fi = doc.Create.NewFamilyInstance(center + upDistance * XYZ.BasisZ, symbol, top_temp - bottom_temp, null, Autodesk.Revit.DB.Structure.StructuralType.Beam);
                    fi.Parameters.Cast<Parameter>().First(e => e.Definition.Name == "Slope").Set(GeometryMathatics.GetAngleFrom3Points(top, center, top_temp));
                    fi.Parameters.Cast<Parameter>().First(e => e.Definition.Name == "Span").Set(bottom_temp.DistanceTo(top_temp));
                    results.Add(fi);
                }

                return results;

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;

            }
        }
        public static List<Element> CreateWallTopPlate(Document doc, List<XYZ> topprofile, int platesnumbers, float width, float depth, out List<XYZ> finalprofile)
        {
            finalprofile = topprofile;

            try
            {
                if (doc == null || topprofile.Count < 2 || topprofile == null || platesnumbers < 1 || width < 0 || depth < 0)
                    return new List<Element>();

                Family fam = CheckIfFamilyIsUploaded(doc, "EFPlate", DB_folder);
                FamilySymbol symbol = SelectSuitableBeamFamilySymbol(doc, fam, width, depth);
                List<Element> results = new List<Element>();

                for (int i = 0; i < platesnumbers; i++)
                {
                    results.AddRange(CreateOneWallPlate(doc, symbol, finalprofile));
                    finalprofile = GetOffsetProfile(finalprofile, (float)(depth / 12.00), true);
                }
                return results;

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;

            }
        }
        public static List<Element> CreateWallBottomPlate(Document doc, List<XYZ> bottomprofile, int platesnumbers, float width, float depth, out List<XYZ> finalProfile)
        {
            finalProfile = bottomprofile;


            try
            {
                if (doc == null || bottomprofile.Count < 2 || bottomprofile == null || platesnumbers < 1 || width < 0 || depth < 0)
                    return new List<Element>();

                Family fam = CheckIfFamilyIsUploaded(doc, "EFPlate", DB_folder);
                FamilySymbol symbol = SelectSuitableBeamFamilySymbol(doc, fam, width, depth);

                List<Element> results = new List<Element>();
                for (int i = 0; i < platesnumbers; i++)
                {
                    finalProfile = GetOffsetProfile(finalProfile, (float)(depth / 12.00), false);
                    results.AddRange(CreateOneWallPlate(doc, symbol, finalProfile));
                }

                return results;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;

            }
        }
        #region Create shellmodel wall



        //private static void JoinWalls(Document doc, List<Wall> walls)
        //{
        //    // Loop through walls to join them
        //    for (int i = 0; i < walls.Count - 1; i++)
        //    {
        //        Wall currentWall = walls[i];
        //        Wall nextWall = walls[i + 1];

        //        // Join walls
        //        JoinGeometryUtils.JoinGeometry(doc, currentWall, nextWall);
        //    }
        //}
        public static IList<ElementId> FindIntersectingWalls(Document doc, Curve curve)
        {
            // Find intersecting walls using a filtered element collector
            var walls = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .ToElements();

            var intersectingWallIds = new List<ElementId>();

            foreach (Element wall in walls)
            {
                if (wall.Location is LocationCurve locationCurve)
                {
                    Curve wallCurve = locationCurve.Curve;

                    // Check if wall curve intersects with the input curve
                    SetComparisonResult result = wallCurve.Intersect(curve);

                    if (result == SetComparisonResult.Overlap || result == SetComparisonResult.Subset)
                    {
                        intersectingWallIds.Add(wall.Id);
                    }
                }
            }

            return intersectingWallIds;
        }

        private static Wall FindAdjacentWall(Document doc, Wall wall)
        {
            Curve wallCurve = ((LocationCurve)wall.Location).Curve;
            // Find intersecting walls
            IList<ElementId> intersectingWallIds = FindIntersectingWalls(doc, wallCurve);

            // Loop through intersecting walls to find adjacent wall
            foreach (ElementId intersectingWallId in intersectingWallIds)
            {
                Element intersectingWall = doc.GetElement(intersectingWallId);
                if (intersectingWall is Wall adjacentWall)
                {
                    return adjacentWall;
                }
            }

            // No adjacent wall found
            return null;
        }


        public static List<Curve> TrimExtendCurves(List<Curve> curves, double distance)
        {
            List<Curve> result = new List<Curve>();
            foreach (Curve curve in curves)
            {
                XYZ startPoint = curve.GetEndPoint(0);
                XYZ endPoint = curve.GetEndPoint(1);

                if (curve.Length <= distance)
                {
                    result.Add(curve);
                }
                else
                {
                    XYZ newStartPoint = startPoint + (endPoint - startPoint).Normalize() * distance;
                    XYZ newEndPoint = endPoint - (endPoint - startPoint).Normalize() * distance;

                    Curve newCurve = Line.CreateBound(newStartPoint, newEndPoint);

                    //if (curve is Arc)
                    //{
                    //    Arc arc = curve as Arc;
                    //    double startAngle = arc.GetParameterAtPoint(newStartPoint);
                    //    double endAngle = arc.GetParameterAtPoint(newEndPoint);
                    //    newCurve = Arc.Create(newStartPoint, newEndPoint, arc.Evaluate(startAngle), startAngle, endAngle, arc.ViewDirection);
                    //}

                    //if (curve.IsCyclic)
                    //{
                    //    newCurve.MakeClosed(1e-9);
                    //}

                    result.Add(newCurve);
                }
            }

            return result;
        }

        //public static Curve TrimExtendCurves(Document doc, Curve curve)
        //{
        //    Get all the elements that intersect with the curve
        //   var intersectingElements = new FilteredElementCollector(doc)
        //       .OfClass(typeof(Wall))
        //       .WhereElementIsNotElementType()
        //       .Where(x => x.get_BoundingBox(null).Intersects(curve))
        //       .ToList();

        //    Trim and extend the curve using the intersection points
        //    foreach (Element element in intersectingElements)
        //    {
        //        LocationCurve locationCurve = element.Location as LocationCurve;
        //        if (locationCurve == null) continue;

        //        Curve elementCurve = locationCurve.Curve;
        //        if (elementCurve == null) continue;

        //        SetComparisonResult result = curve.Intersect(elementCurve, out IntersectionResultArray results);
        //        if (result != SetComparisonResult.Overlap) continue;

        //        Trim and extend the curve based on the intersection points
        //        foreach (IntersectionResult intersection in results)
        //        {
        //            if (intersection == null) continue;
        //            if (intersection.XYZPoint == null) continue;

        //            if (intersection.Distance < 0.1)
        //            {
        //                curve = curve.CreateTrimmed(intersection.ParameterA, curve.GetEndParameter(true));
        //            }
        //            else if (intersection.Distance > curve.Length - 0.1)
        //            {
        //                curve = curve.CreateTrimmed(curve.GetEndParameter(false), intersection.ParameterA);
        //            }
        //            else
        //            {
        //                curve = curve.CreateTrimmed(intersection.ParameterA, curve.GetEndParameter(true));
        //                curve = curve.CreateTrimmed(curve.GetEndParameter(false), intersection.ParameterA);
        //            }
        //        }
        //    }

        //    return curve;
        //}


        public static Curve TrimCurve(Curve curve, XYZ startPoint, XYZ endPoint)
        {
            // Get the parameter values for the start and end points of the curve
            double startParam = curve.Project(startPoint).Parameter;
            double endParam = curve.Project(endPoint).Parameter;

            // Create a new curve that is trimmed to the specified endpoints
            Curve trimmedCurve = curve.Clone();
            trimmedCurve.MakeBound(startParam, endParam);

            return trimmedCurve;
        }

        public static Element CreateWall(Document doc, Level level, MapXYZ start, MapXYZ end, double height, double thickness, double bottomOffset, List<MapShellOpening> shellOpenings)
        {

            try
            {

                // Create curves for the wall
                Curve leftline = Line.CreateBound(start.GetReference(), end.GetReference());


                // Trim and extend the curves to ensure they connect properly
                // Get the generic wall type
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                //         collector.OfClass(typeof(WallType));
                //         WallType wallType = (WallType)collector
                //.First(x => x.Name == "Generic - 12\"");

                // Create the wall profile and loop
                //WallType wallType = GetDefaultWallType(doc);
                var wallType = GetSuitableWallTypeId(doc, thickness);

                //Level level = GetLevel(doc, start.Z + bottomOffset);doc.GetDefaultElementTypeId(ElementTypeGroup.WallType)
                // Join curves to create wall
                Wall wall = Wall.Create(doc, leftline, wallType, level.Id, height, bottomOffset, false, false);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(height);
                wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(bottomOffset);
                //wall.get_Parameter(BuiltInParameter.w)

                // Join wall to adjacent walls
                //Wall adjacentWall = FindAdjacentWall(doc, wall);

                //if (adjacentWall != null)
                //{
                //    // Join walls
                //    JoinGeometryUtils.JoinGeometry(doc, wall, adjacentWall);
                //}
                //JoinGeometryUtils.JoinGeometry(doc, wall, FindAdjacentWall(doc, wall));
                // Move unjoined walls to connect with adjacent walls




                //foreach (Wall wal in walls)
                //        {
                if (shellOpenings != null)
                {
                    //doc.Regenerate();

                    //Call Opening Methoda
                    foreach (var opening in shellOpenings)
                    {

                        CreateDoorsInWall(doc, wall, level, opening.startpoint, opening.endpoint, opening.OpeningHeight, opening.HeadHeight, opening.OpeningType);
                        //}
                        //doc.Regenerate();
                    }


                }
                return wall;
                //  }
                //}


            }



            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;

            }
        }












        private static bool CanJoinWalls(Wall wall1, Wall wall2)
        {
            XYZ wall1Start = wall1.get_BoundingBox(null).Min;
            XYZ wall1End = wall1.get_BoundingBox(null).Max;
            XYZ wall2Start = wall2.get_BoundingBox(null).Min;
            XYZ wall2End = wall2.get_BoundingBox(null).Max;

            if (wall1Start.IsAlmostEqualTo(wall2Start) || wall1Start.IsAlmostEqualTo(wall2End) ||
                wall1End.IsAlmostEqualTo(wall2Start) || wall1End.IsAlmostEqualTo(wall2End))
            {
                return true;
            }

            return false;
        }









        private static bool AreWallsAdjacent(Wall wall1, Wall wall2, double tolerance)
        {
            LocationCurve wall1Curve = wall1.Location as LocationCurve;
            XYZ wall1StartPoint = wall1Curve.Curve.GetEndPoint(0);
            XYZ wall1EndPoint = wall1Curve.Curve.GetEndPoint(1);

            LocationCurve wall2Curve = wall2.Location as LocationCurve;
            XYZ wall2StartPoint = wall2Curve.Curve.GetEndPoint(0);
            XYZ wall2EndPoint = wall2Curve.Curve.GetEndPoint(1);

            if (wall1StartPoint.IsAlmostEqualTo(wall2EndPoint, tolerance) || wall1EndPoint.IsAlmostEqualTo(wall2StartPoint, tolerance))
            {
                return true;
            }
            return false;
        }
        private static ElementId GetAdjacentWallId(Wall wall, double tolerance)
        {
            tolerance = 0.1;
            LocationCurve wallCurve = wall.Location as LocationCurve;
            XYZ startPoint = wallCurve.Curve.GetEndPoint(0);
            XYZ endPoint = wallCurve.Curve.GetEndPoint(1);
            FilteredElementCollector walls = new FilteredElementCollector(wall.Document).OfCategory(BuiltInCategory.OST_Walls);
            foreach (Wall otherWall in walls)
            {
                if (otherWall.Id != wall.Id)
                {
                    LocationCurve otherWallCurve = otherWall.Location as LocationCurve;
                    XYZ otherStartPoint = otherWallCurve.Curve.GetEndPoint(0);
                    XYZ otherEndPoint = otherWallCurve.Curve.GetEndPoint(1);
                    if (startPoint.IsAlmostEqualTo(otherEndPoint, tolerance) || endPoint.IsAlmostEqualTo(otherStartPoint, tolerance))
                    {
                        return otherWall.Id;
                    }
                }
            }
            return null;
        }



        public static ElementId GetSuitableWallTypeId(Document doc, double thickness)
        {
            //width = Math.Round(width, 2);


            WallType wallType = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls).WhereElementIsElementType()
                .Cast<WallType>().FirstOrDefault(x => x.Name.StartsWith("EF_") && x.get_Parameter(BuiltInParameter.WALL_ATTR_WIDTH_PARAM).AsDouble() == thickness);
            if (wallType != null)
                return wallType.Id;

            //using (var trans = new Transaction(doc, "CreateType"))
            //{
            //    trans.Start();
            var genericWallType = doc.GetDefaultElementTypeId(ElementTypeGroup.WallType);
            var sym = doc.GetElement(genericWallType) as WallType;
            if (sym == null)
                return null;
            wallType = sym?.Duplicate($"EF_{thickness}") as WallType;
            //doc.Regenerate();
            var materialId = wallType.GetCompoundStructure().GetMaterialId(0);
            var structure = CompoundStructure.CreateSingleLayerCompoundStructure(MaterialFunctionAssignment.Structure, thickness, materialId);
            wallType.SetCompoundStructure(structure);
            //    trans.Commit();
            //}

            return wallType.Id;

        }

        internal static string CreateShellWallFamily(Document doc, ref string FamilyName, Dictionary<List<XYZ>, double> dict)

        {
            try
            {
                string folderName = "ShellWalls";
                string folderPath = Path.Combine(rootpath, folderName);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                SolveDuplicatedFamilyName(folderPath, ref FamilyName);

                Document fdoc = CreateNemFamilyDocument(doc, folderName, "shellmodel.rft");
                if (null == fdoc)
                    return null;

                double extrusionthickness = (0.05 / 12.00);
                XYZ Normal = XYZ.BasisZ * extrusionthickness;
                using (Transaction t = new Transaction(fdoc, "GenericWall"))
                {
                    t.Start();

                    FailureHandlingOptions failopt = t.GetFailureHandlingOptions();
                    failopt.SetFailuresPreprocessor(new RevitHandler());
                    t.SetFailureHandlingOptions(failopt);
                    Material m = GetMaterial(fdoc, "Wood", new Color(154, 107, 61));
                    foreach (var item in dict)
                    {
                        Extrusion e = CreateExtrusion(fdoc, item.Key, item.Value, m);
                    }

                    t.Commit();
                }

                SaveAsOptions opt = new SaveAsOptions();
                opt.OverwriteExistingFile = true;

                fdoc.SaveAs(Path.Combine(folderPath, FamilyName + ".rfa"), opt);
                fdoc.Close(true);

                return folderPath;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        #endregion
        private static List<Element> CreateOneWallPlate(Document doc, FamilySymbol sam, List<XYZ> profile)
        {
            try
            {
                if (doc == null || sam == null || profile == null || profile.Count < 2)
                    return null;

                List<Element> results = new List<Element>();

                for (int i = 0; i < profile.Count - 1; i++)
                {
                    results.Add(CreateOneWallPlateSegment(doc, sam, profile[i], profile[i + 1]));
                }


                return results;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        private static Element CreateOneWallPlateSegment(Document doc, FamilySymbol sam, XYZ top, XYZ bottom)
        {
            try
            {
                if (doc == null || sam == null || top == null || bottom == null)
                    return null;

                if (bottom.Z > top.Z)
                {
                    XYZ temp = bottom;
                    bottom = top;
                    top = temp;
                }

                XYZ center = new XYZ((top.X + bottom.X) / 2, (top.Y + bottom.Y) / 2, (top.Z + bottom.Z) / 2);
                XYZ top_temp = new XYZ(top.X, top.Y, center.Z);
                XYZ bottom_temp = new XYZ(bottom.X, bottom.Y, center.Z);
                FamilyInstance fi = doc.Create.NewFamilyInstance(center, sam, top_temp - bottom_temp, null, Autodesk.Revit.DB.Structure.StructuralType.Beam);
                fi.Parameters.Cast<Parameter>().First(e => e.Definition.Name == "Slope").Set(GeometryMathatics.GetAngleFrom3Points(top, center, top_temp));
                fi.Parameters.Cast<Parameter>().First(e => e.Definition.Name == "Span").Set(bottom_temp.DistanceTo(top_temp));
                return fi;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        private static List<XYZ> GetOffsetProfile(List<XYZ> profile, float depth, bool top)
        {
            try
            {
                if (profile == null || profile.Count < 2 || depth < 0)
                    return null;

                if (depth == 0)
                    return profile;

                List<XYZ> newProfile = new List<XYZ>();

                newProfile.Add(GetoffsetPoint(null, profile[0], profile[1], depth, top));
                for (int i = 1; i < profile.Count - 1; i++)
                {
                    newProfile.Add(GetoffsetPoint(profile[i - 1], profile[i], profile[i + 1], depth, top));

                }
                newProfile.Add(GetoffsetPoint(profile[profile.Count - 2], profile[profile.Count - 1], null, depth, top));

                return newProfile;

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;

            }
        }
        private static XYZ GetoffsetPoint(XYZ before, XYZ intersect, XYZ after, double depth, bool top)
        {
            try
            {
                if (intersect == null || depth < 0)
                    return null;


                if (before == null)
                {
                    XYZ afterMid = GeometryMathatics.get_mid_point(intersect, after);
                    XYZ AfterVector = (intersect - after).Normalize();
                    XYZ tempUp = new XYZ(0, 0, 1);
                    XYZ afterUp = (AfterVector.CrossProduct(tempUp)).CrossProduct(AfterVector).Normalize();

                    if (top)
                    {
                        if (afterUp.Z > 0.00)
                            afterUp *= -1;
                    }
                    else
                    {
                        if (afterUp.Z < 0.00)
                            afterUp *= -1;
                    }
                    afterUp = afterUp * depth;
                    return GeometryMathatics.Two_Lines_Intersect_vertical(intersect, intersect + tempUp, afterMid + afterUp, after + afterUp);

                }
                else if (after == null)
                {
                    XYZ beforMid = GeometryMathatics.get_mid_point(intersect, before);
                    XYZ beforeVector = (intersect - before).Normalize();
                    XYZ tempUp = new XYZ(0, 0, 1);

                    XYZ beforeUp = (beforeVector.CrossProduct(tempUp)).CrossProduct(beforeVector).Normalize();
                    if (top)
                    {
                        if (beforeUp.Z > 0.00)
                            beforeUp *= -1;
                    }
                    else
                    {
                        if (beforeUp.Z < 0.00)
                            beforeUp *= -1;
                    }
                    beforeUp = beforeUp * depth;
                    return GeometryMathatics.Two_Lines_Intersect_vertical(beforMid + beforeUp, before + beforeUp, intersect, intersect + tempUp);

                }
                else
                {
                    XYZ beforMid = GeometryMathatics.get_mid_point(intersect, before);
                    XYZ afterMid = GeometryMathatics.get_mid_point(intersect, after);

                    XYZ beforeVector = (intersect - before).Normalize();
                    XYZ AfterVector = (intersect - after).Normalize();

                    XYZ tempUp = new XYZ(0, 0, 1);
                    XYZ beforeUp = (beforeVector.CrossProduct(tempUp)).CrossProduct(beforeVector).Normalize();
                    XYZ afterUp = (AfterVector.CrossProduct(tempUp)).CrossProduct(AfterVector).Normalize();

                    if (top)
                    {
                        if (beforeUp.Z > 0.00)
                            beforeUp *= -1;

                        if (afterUp.Z > 0.00)
                            afterUp *= -1;
                    }
                    else
                    {
                        if (beforeUp.Z < 0.00)
                            beforeUp *= -1;

                        if (afterUp.Z < 0.00)
                            afterUp *= -1;
                    }

                    beforeUp = beforeUp * depth;
                    afterUp = afterUp * depth;

                    return GeometryMathatics.Two_Lines_Intersect_vertical(beforMid + beforeUp, before + beforeUp, afterMid + afterUp, after + afterUp);
                }

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;

            }
        }
        #endregion

        #region CreateWallOpening

        public static Element CreateWallOpeningBeams(Document doc, Level l, Line line, double width, double depth)
        {
            try
            {

                XYZ start = line.GetEndPoint(0);
                XYZ end = line.GetEndPoint(1);

                Family fam = GeneralCreator.CheckIfFamilyIsUploaded(doc, "EFHeader", DB_folder);
                FamilySymbol symbol = SelectSuitableBeamFamilySymbol(doc, fam, (float)width, (float)depth);

                Element e = AddHorizontalBeam(doc, l, Line.CreateBound(start + depth * XYZ.BasisZ / 12.00, end + depth * XYZ.BasisZ / 12.00)
                    , new MapRecSection() { Depth = depth, Width = width, Plies = 1 }, symbol);

                return e;
            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }
        }
        public static bool CreateDoorsInWall(Document doc, Element element, Level level, MapXYZ start, MapXYZ end, double openingheight, double headheight, WallOpeningType type)
        {
            try
            {
                // get wall's level for door creation        
                //Level level = doc.GetElement(element.LevelId) as Level;
                Curve c = (element.Location as LocationCurve).Curve;
                XYZ walStartPoint = c.GetEndPoint(0);
                XYZ wallEndPoint = c.GetEndPoint(1);
                XYZ wallDirection = (wallEndPoint - walStartPoint).Normalize();
                var openingWidth = Math.Abs((end.GetReference() - start.GetReference()).DotProduct(wallDirection));
                //var wallThickness =()
                //var wallThickness = element.get_Parameter(W);
                var wallThinkness = ((Wall)element).Width;
                XYZ location = (start.GetReference() + end.GetReference()) / 2;
                var parameters = new Dictionary<string, object>
            {
                { "Width", openingWidth },
                { "Height", openingheight },

            };
                if (type == WallOpeningType.Door)
                    parameters.Add("Thickness", wallThinkness);
                Family fam = null;
                switch (type)
                {
                    case (WallOpeningType.Generic):
                    case (WallOpeningType.Door):
                        {
                            fam = CheckIfFamilyIsUploaded(doc, "Single-Flush", DB_folder);
                            FamilySymbol symbol = SelectSuitableSymbol(doc, fam, parameters);

                            if (symbol == null)
                            {
                                FamilySymbol firstSymobl = doc.GetElement(fam.GetFamilySymbolIds().First()) as FamilySymbol;
                                symbol = firstSymobl.Duplicate($"EF_{openingWidth}x{openingheight}") as FamilySymbol;
                                symbol.LookupParameter("Width").Set(openingWidth);
                                symbol.LookupParameter("Height").Set(openingheight);
                                symbol.LookupParameter("Thickness")?.Set(wallThinkness);
                            }

                            if (!symbol.IsActive)
                                symbol.Activate();
                            FamilyInstance instance = doc.Create.NewFamilyInstance(location, symbol, element, level, StructuralType.NonStructural);
                            doc.Regenerate();
                            //instance.LookupParameter("Width").Set(openingWidth);
                            //instance.LookupParameter("Height").Set(openingheight);
                            instance.LookupParameter("Sill Height").Set((headheight - openingheight));

                        }
                        break;

                    case (WallOpeningType.Window):
                    case (WallOpeningType.Arch):
                        {
                            fam = CheckIfFamilyIsUploaded(doc, "Window-Casement-Double", DB_folder);
                            FamilySymbol sym = SelectSuitableSymbol(doc, fam, parameters);

                            if (sym == null)
                            {
                                FamilySymbol firstSymobl = doc.GetElement(fam.GetFamilySymbolIds().First()) as FamilySymbol;
                                sym = firstSymobl.Duplicate($"EF_{openingWidth}x{openingheight}") as FamilySymbol;
                                sym.LookupParameter("Width").Set(openingWidth);
                                sym.LookupParameter("Height").Set(openingheight);
                                //symbol.LookupParameter("Thickness")?.Set(wallThinkness);
                            }

                            if (!sym.IsActive)
                                sym.Activate();
                            FamilyInstance inst = doc.Create.NewFamilyInstance(location, sym, element, level, StructuralType.NonStructural);
                            doc.Regenerate();
                            //instance.LookupParameter("Width")?.Set(openingWidth);
                            ////instance.LookupParameter("Height")?.Set(openingheight);
                            //instance.LookupParameter("Wall Thickness")?.Set(wallThinkness);

                            //#endregion
                            //if (type == WallOpeningType.Window)
                            inst.LookupParameter("Sill Height").Set((headheight - openingheight));

                        }
                        break;
                };


                return true;

            }

            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return false;
            }


        }
        //public static ElementId GetSuitableFamilySymbolType(Document doc, double width, double height , WallOpeningType type)
        //{
        //    switch(type) 
        //    {
        //        case (WallOpeningType.Door):
        //            FamilySymbol familySymbol = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsElementType()
        //        .Cast<FamilySymbol>().FirstOrDefault(x => x.Name.StartsWith("EF_") && x.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble() == width && x.get_Parameter(BuiltInParameter.DOOR_HEIGHT).AsDouble() == height);
        //            if (familySymbol != null)
        //                return familySymbol.Id;
        //            break;
        //        case (WallOpeningType.Window):
        //             familySymbol = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsElementType()
        //      .Cast<FamilySymbol>().FirstOrDefault(x => x.Name.StartsWith("EF_") && x.get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble() == width && x.get_Parameter(BuiltInParameter.WINDOW_HEIGHT).AsDouble() == height);
        //            if (familySymbol != null)
        //                return familySymbol.Id;
        //            break;  
        //    }
        //    var familytype = doc.GetDefaultElementTypeId(ElementTypeGroup.);
        //    var sym = doc.GetElement(familytype) as FamilySymbol;
        //    if (sym == null)
        //        return null;
        //    FamilySymbol Symbol = sym?.Duplicate($"EF_{width}") as FamilySymbol;
        //    return Symbol.Id;
        //}


        internal static Element CreateWallSteelStruts(Document doc, Level l, Line line, double width)
        {
            try
            {
                XYZ start = line.GetEndPoint(0);
                XYZ end = line.GetEndPoint(1);

                double depth = (0.5 / 12.00);
                Family fam = GeneralCreator.CheckIfFamilyIsUploaded(doc, "EFSteelStrap", DB_folder);
                FamilySymbol symbol = SelectSuitableBeamFamilySymbol(doc, fam, (float)width, (float)depth);

                Element e = AddHorizontalBeam(doc, l, Line.CreateBound(start + depth * XYZ.BasisZ / 12.00, end + depth * XYZ.BasisZ / 12.00)
                        , new MapRecSection() { Depth = depth, Width = width, Plies = 1 }, symbol);

                return e;

            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }

        }


        #endregion

        #endregion

        #region CreatePosts
        public static Element CreatePost(Document doc, string familyName, string name, MapRecSection section, double minZ, double maxZ, XYZ position, XYZ direction, bool OnFoundation)
        {
            try
            {
                Family fam = CheckIfFamilyIsUploaded(doc, familyName, newStuds_Folder);
                if (fam == null)
                    return null;

                FamilySymbol sam = SelectSuitableBeamFamilySymbol(doc, fam, (float)section.Width, (float)section.Depth);
                if (sam == null)
                    return null;

                Level l = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(x => Math.Abs(x.Elevation - minZ)).FirstOrDefault();
                if (l == null)
                    return null;

                Element e = null;

                if (section.Plies == 1)
                {
                    e = createPost(doc, sam, l, position, direction, name, minZ, maxZ, OnFoundation);
                }
                else
                {
                    List<XYZ> positions = GetCreationPositionsFromPlies(position, direction, section.Width, section.Plies);
                    List<FamilyInstance> postParts = new List<FamilyInstance>();
                    foreach (XYZ point in positions)
                    {
                        postParts.Add(createPost(doc, sam, l, point, direction, "", minZ, maxZ, OnFoundation));
                    }
                    e = CreateTheGroup(doc, postParts.Select(x => x.Id).ToList(), name, l);
                }


                return e;
            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }
        }
        private static List<XYZ> GetCreationPositionsFromPlies(XYZ position, XYZ direction, double width, int plies)
        {
            try
            {
                if (plies == 0)
                    return null;
                if (plies == 1)
                    return new List<XYZ>() { position };
                XYZ widthdir = direction.Normalize();
                width /= 12.00;
                double totalWidth = width * plies;
                XYZ start = position - 0.5 * totalWidth * widthdir;

                List<XYZ> creationPositions = new List<XYZ>();
                for (int i = 0; i < plies; i++)
                {
                    creationPositions.Add(start + (i + 0.5) * width * widthdir);
                }

                return creationPositions;
            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }
        }
        private static FamilyInstance createPost(Document doc, FamilySymbol sam, Level l, XYZ location, XYZ direction, string name, double minZ, double maxZ, bool OnFoundation)
        {
            try
            {
                FamilyInstance fi = null;
                location = new XYZ(location.X, location.Y, l.Elevation);
                fi = doc.Create.NewFamilyInstance(location, sam, null, l, StructuralType.Column);
                double rotationangle = RotationAngleWithXAxis(direction);

                //Math.Asin((direction.Normalize().DotProduct(XYZ.BasisX)));

                if (Math.Abs(rotationangle) > 0.0001)
                {
                    ElementTransformUtils.RotateElement(doc, fi.Id, Line.CreateBound(location, location + XYZ.BasisZ), rotationangle);
                }

                fi.get_Parameter(BuiltInParameter.SCHEDULE_BASE_LEVEL_PARAM).Set(l.Id);
                SetSingleFamilyInstanceParameterIndependentPost(fi, (float)(minZ - l.Elevation), (float)(maxZ - minZ), name);

                if (OnFoundation && fi != null)
                {
                    // CreateHoldDownStrap(s.holdDown,Location, baselevel, post_ins);
                }
                return fi;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static bool SetSingleFamilyInstanceParameterIndependentPost(Element fInstance, float elev, float height, string parentname)//, double Width, double Depth, double height, double _topAngle, double _bottomAngle)
        {
            try
            {
                if (fInstance == null)
                    return false;

                Parameter param = fInstance.GetParameters("Height").FirstOrDefault();
                if (param != null)
                    param.Set(height);

                param = fInstance.get_Parameter(BuiltInParameter.SCHEDULE_BASE_LEVEL_OFFSET_PARAM);
                if (param != null)
                    param.Set(elev);

                param = fInstance.GetParameters("ParentName").FirstOrDefault();
                if (param != null)
                    param.Set(parentname);

                return true;
            }
            catch (Exception ex)
            {
                // Util.SaveErrors(ex);
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
                return false;
            }
        }

        internal static Element CreateSlab(Document m_doc, Level level, List<XYZ> Profile, List<List<XYZ>> OpeningList, double thickness)
        {
            try
            {
                //CurveArray arr = new CurveArray();
                //for (int i = 0; i < Profile.Count; i++)
                //{
                //    int j = (i + 1) % Profile.Count;
                //    arr.Append( Line.CreateBound(Profile[i], Profile[j]));
                //}
                //FloorType floorType = GetFloorType(m_doc, thickness , level.Name +"_Sheathing");
                //return  m_doc.Create.NewFloor(arr,floorType, level, false,XYZ.BasisZ);
                return null;
            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }
        }

        private static FloorType GetFloorType(Document m_doc, double thickness, string name)
        {
            List<FloorType> types = new FilteredElementCollector(m_doc).OfClass(typeof(FloorType)).Cast<FloorType>().ToList();

            FloorType type = types.FirstOrDefault(x => x.Name == name);
            if (type != null)
                return type;

            type = types[0].Duplicate(name) as FloorType;
            Material m = GetMaterial(m_doc, "Wood", new Color(154, 107, 61));
            int i = type.GetCompoundStructure().StructuralMaterialIndex;
            type.GetCompoundStructure().SetLayer(i, new CompoundStructureLayer(thickness, MaterialFunctionAssignment.Structure, m.Id));
            return type;
        }
        #endregion

        #region createGroup
        public static Group CreateTheGroup(Document doc, List<ElementId> ids, string groupname, Level level)
        {

            Group modelGroup = null;
            if (ids == null)
            {
                return null;
            }

            ids.RemoveAll(x => x == null);

            if (ids.Count == 0)
            {
                return null;
            }

            string name = groupname;
            modelGroup = doc.Create.NewGroup(ids);
            SolveDuplicatedGroupsName(doc, ref name);
            modelGroup.GroupType.Name = name;
            if (level != null)
            {
                double otherlevel = (doc.GetElement(modelGroup.get_Parameter(BuiltInParameter.GROUP_LEVEL).AsElementId()) as Level).Elevation;
                double otheroffset = modelGroup.get_Parameter(BuiltInParameter.GROUP_OFFSET_FROM_LEVEL).AsDouble();
                modelGroup.get_Parameter(BuiltInParameter.GROUP_LEVEL).Set(level.Id);
                modelGroup.get_Parameter(BuiltInParameter.GROUP_OFFSET_FROM_LEVEL).Set(otherlevel + otheroffset - level.Elevation);

            }
            return modelGroup;
        }

        #endregion

        #region Create Beams / Hip rafter / Ridgebeam
        private static Element AddHorizontalBeam(Document doc, Level level, Line line, MapSection section, FamilySymbol symbol)
        {
            try
            {
                if (symbol == null || section == null)
                    return null;
                Autodesk.Revit.DB.Structure.StructuralType structuralType = Autodesk.Revit.DB.Structure.StructuralType.Beam;
                FamilyInstance e = null;
                if (section.Plies == 1)
                {
                    e = doc.Create.NewFamilyInstance(line, symbol, level, structuralType);
                    return e;
                }
                else
                {
                    List<Line> lines = GetCreationLineFromPlies(line, section.GetWidth(), section.Plies);
                    List<ElementId> ids = new List<ElementId>();

                    foreach (var creationLine in lines)
                    {
                        e = doc.Create.NewFamilyInstance(creationLine, symbol, level, structuralType);
                        if (e == null)
                            continue;
                        ids.Add(e.Id);
                    }

                    string ModelName = symbol.Name + "-" + section.Plies.ToString();
                    return CreateTheGroup(doc, ids, ModelName, level);
                }

            }
            catch (Exception e)
            {
                return null;
            }
        }
        public static Element CreateHorizontalBeam(Document doc, Level level, Line line, MapSection section, string FName)
        {
            try
            {
                if (level == null || section == null || section.Plies == 0)
                    return null;
                Family fam = GeneralCreator.CheckIfFamilyIsUploaded(doc, FName, DB_folder);
                if (section is MapRecSection)
                {
                    MapRecSection recSection = section as MapRecSection;
                    FamilySymbol symbol = SelectSuitableBeamFamilySymbol(doc, fam, (float)recSection.Width, (float)recSection.Depth);
                    return AddHorizontalBeam(doc, level, line, recSection, symbol);
                }
                else if (section is MapISection)
                {
                    MapISection Isection = section as MapISection;
                    FamilySymbol symbol = SelectSuitableIBeamFamilySymbol(doc, fam, (float)Isection.Width, (float)Isection.Depth, (float)Isection.PlateDepth, (float)Isection.WebWidth);
                    return AddHorizontalBeam(doc, level, line, Isection, symbol);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }

        }
        private static Element AddInclinedBeam(Document doc, Level level, Line line, MapSection section, FamilySymbol symbol)
        {
            try
            {
                if (symbol == null || section == null)
                    return null;

                Autodesk.Revit.DB.Structure.StructuralType structuralType = Autodesk.Revit.DB.Structure.StructuralType.Beam;
                FamilyInstance e = null;
                if (section.Plies == 1)
                {
                    e = CreateRafter(doc, symbol, level, line);
                    return e;
                }
                else
                {
                    List<Line> lines = GetCreationLineFromPlies(line, section.GetWidth(), section.Plies);
                    List<ElementId> ids = new List<ElementId>();
                    foreach (var creationLine in lines)
                    {
                        e = CreateRafter(doc, symbol, level, creationLine);
                        if (e == null)
                            continue;
                        ids.Add(e.Id);
                    }
                    string ModelName = symbol.Name + "-" + section.Plies.ToString();
                    return CreateTheGroup(doc, ids, ModelName, level);
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }



        public static Element CreateInclinedBeam(Document doc, Level level, Line line, MapSection section, string FName)
        {
            try
            {
                if (level == null || section == null || section.Plies == 0)
                    return null;
                Family fam = GeneralCreator.CheckIfFamilyIsUploaded(doc, FName, DB_folder);
                if (section is MapRecSection)
                {
                    FamilySymbol symbol = SelectSuitableBeamFamilySymbol(doc, fam, (float)section.GetWidth(), (float)section.Depth);
                    return AddInclinedBeam(doc, level, line, section, symbol);
                }
                else if (section is MapISection)
                {
                    MapISection Isection = section as MapISection;
                    FamilySymbol symbol = SelectSuitableIBeamFamilySymbol(doc, fam, (float)Isection.Width, (float)Isection.Depth, (float)Isection.PlateDepth, (float)Isection.WebWidth);
                    return AddInclinedBeam(doc, level, line, section, symbol);
                }
                else
                {
                    return null;
                }


            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }

        }
        private static FamilyInstance CreateRafter(Document doc, FamilySymbol symbol, Level level, Line line)
        {
            try
            {
                XYZ top = line.GetEndPoint(0);
                XYZ bottom = line.GetEndPoint(1);
                if (bottom.Z > top.Z)
                {
                    XYZ temp = bottom;
                    bottom = top;
                    top = temp;
                }

                XYZ center = new XYZ((top.X + bottom.X) / 2, (top.Y + bottom.Y) / 2, (top.Z + bottom.Z) / 2);
                XYZ top_temp = new XYZ(top.X, top.Y, center.Z);
                XYZ bottom_temp = new XYZ(bottom.X, bottom.Y, center.Z);
                FamilyInstance fi = doc.Create.NewFamilyInstance(center, symbol, top_temp - bottom_temp, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                fi.Parameters.Cast<Parameter>().First(e => e.Definition.Name == "Slope").Set(GeometryMathatics.GetAngleFrom3Points(top, center, top_temp));
                fi.Parameters.Cast<Parameter>().First(e => e.Definition.Name == "Span").Set(bottom_temp.DistanceTo(top_temp));
                return fi;

            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }

        }
        private static List<Line> GetCreationLineFromPlies(Line line, double width, int plies)
        {
            try
            {
                XYZ p1 = line.GetEndPoint(0);
                XYZ p2 = line.GetEndPoint(1);

                XYZ dir = (p2 - p1).Normalize();
                XYZ widthdir = dir.CrossProduct(XYZ.BasisZ);
                XYZ up = widthdir.CrossProduct(dir);
                widthdir = dir.CrossProduct(up).Normalize();
                width /= 12.00;
                double totalWidth = width * plies;
                p1 -= 0.5 * (totalWidth - width) * widthdir;
                p2 -= 0.5 * (totalWidth - width) * widthdir;
                List<Line> lines = new List<Line>();

                for (int i = 0; i < plies; i++)
                {
                    lines.Add(Line.CreateBound(p1 + i * width * widthdir, p2 + i * width * widthdir));
                }

                return lines;
            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }
        }
        private static FamilySymbol SelectSuitableBeamFamilySymbol(Document doc, Family fam, float widthInch, float depthInch)
        {
            try
            {
                if (doc == null || fam == null)
                    return null;

                var parameters = new Dictionary<string, object>();
                parameters.Add("b", widthInch / 12.0);
                parameters.Add("d", depthInch / 12.0);
                FamilySymbol symbol = GeneralCreator.SelectSuitableSymbol(doc, fam, parameters);
                if (symbol == null)
                {
                    string NewName = fam.Name + " " + Math.Round(widthInch, 3) + "X" + Math.Round(depthInch, 3);
                    GeneralCreator.SolveDuplicatedFamilySymbolName(doc, fam, ref NewName);
                    symbol = GeneralCreator.UpdateSuitableSymbol(doc, fam, parameters, NewName);
                }

                return symbol;
            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }
        }
        private static FamilySymbol SelectSuitableIBeamFamilySymbol(Document doc, Family fam, float PlatewidthInch, float depthInch, float PlatedepthInch, float WebThicknessInch)
        {
            try
            {
                if (doc == null || fam == null)
                    return null;

                var parameters = new Dictionary<string, object>();
                parameters.Add("PL_b", PlatewidthInch / 12.0);
                parameters.Add("d", depthInch / 12.0);
                parameters.Add("PL_d", PlatedepthInch / 12.0);
                parameters.Add("Web_b", WebThicknessInch / 12.0);
                FamilySymbol symbol = GeneralCreator.SelectSuitableSymbol(doc, fam, parameters);
                if (symbol == null)
                {
                    string NewName = "IJoist-Plate " + Math.Round(PlatewidthInch, 3) + "X" + Math.Round(PlatedepthInch, 3)
                        + "Web" + Math.Round(WebThicknessInch, 3) + "X" + Math.Round(depthInch - 2 * PlatedepthInch, 3);
                    GeneralCreator.SolveDuplicatedFamilySymbolName(doc, fam, ref NewName);
                    symbol = GeneralCreator.UpdateSuitableSymbol(doc, fam, parameters, NewName);
                }

                return symbol;
            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }
        }

        #endregion

        #region Joist Area And Rafter Area
        public static List<ModelCurve> AppendPolygon(Document doc, List<XYZ> vetexlist)
        {
            try
            {
                List<ModelCurve> lines = new List<ModelCurve>();

                SketchPlane p = SketchPlane.Create(doc, Plane.CreateByThreePoints(vetexlist[0], vetexlist[1], vetexlist[2]));
                int n = vetexlist.Count;
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    lines.Add(doc.Create.NewModelCurve(Line.CreateBound(vetexlist[i], vetexlist[j]), p));
                }
                return lines;
            }
            catch (Exception e)
            {
                return new List<ModelCurve>();
            }
        }
        public static Element CreateJoistArea(Document doc, Level level, string name, List<XYZ> profile, List<List<XYZ>> openings, MapSection section, List<Line> lines, List<MapBeam> mapRimBeams, List<MapBeam> mapBlockBeams)
        {
            try
            {
                if (level == null || section == null || section.Plies == 0)
                    return null;
                FamilySymbol symbol = null;
                if (section is MapRecSection)
                {
                    Family fam = GeneralCreator.CheckIfFamilyIsUploaded(doc, "EFJoist", DB_folder);
                    symbol = SelectSuitableBeamFamilySymbol(doc, fam, (float)section.GetWidth(), (float)section.Depth);
                }
                else if (section is MapISection)
                {
                    MapISection Isection = section as MapISection;
                    Family fam = GeneralCreator.CheckIfFamilyIsUploaded(doc, "EFHorizontal_Ijoist", DB_folder);
                    symbol = SelectSuitableIBeamFamilySymbol(doc, fam, (float)Isection.Width, (float)section.Depth, (float)Isection.PlateDepth, (float)Isection.WebWidth);
                }
                else
                {
                    return null;
                }
                List<Element> elements = new List<Element>();
                if (profile != null)
                {
                    elements.AddRange(AppendPolygon(doc, profile));
                }

                if (openings != null)
                {
                    foreach (List<XYZ> openingPolygon in openings)
                    {
                        elements.AddRange(AppendPolygon(doc, openingPolygon));
                    }
                }

                if (lines != null)
                {
                    foreach (var line in lines)
                    {
                        elements.Add(AddHorizontalBeam(doc, level, line, section, symbol));
                    }
                }
                if (mapRimBeams != null)
                {
                    elements.AddRange(mapRimBeams.Select(x => x.SetData(level)));
                }

                if (mapBlockBeams != null)
                {
                    elements.AddRange(mapBlockBeams.Select(x => x.SetData(level)));
                }

                return CreateTheGroup(doc, elements.Where(x => x != null).Select(x => x.Id).ToList(), name, level);

            }
            catch (Exception e)
            {
                return null;
            }
        }
        public static Element CreateRafterArea(Document doc, Level level, string name, List<XYZ> profile, List<List<XYZ>> openings, MapSection section, List<Line> lines, List<MapBeam> mapRimBeams, List<MapBeam> mapBlockBeams)
        {
            try
            {
                if (level == null || section == null || section.Plies == 0)
                    return null;

                FamilySymbol symbol = null;
                if (section is MapRecSection)
                {
                    Family fam = GeneralCreator.CheckIfFamilyIsUploaded(doc, "EFRafter", DB_folder);
                    symbol = SelectSuitableBeamFamilySymbol(doc, fam, (float)section.GetWidth(), (float)section.Depth);
                }
                else if (section is MapISection)
                {
                    MapISection Isection = section as MapISection;
                    Family fam = GeneralCreator.CheckIfFamilyIsUploaded(doc, "EFIjoist", DB_folder);
                    symbol = SelectSuitableIBeamFamilySymbol(doc, fam, (float)Isection.Width, (float)Isection.Depth, (float)Isection.PlateDepth, (float)Isection.WebWidth);
                }
                else
                {
                    return null;
                }

                List<Element> elements = new List<Element>();
                if (profile != null)
                {
                    elements.AddRange(AppendPolygon(doc, profile));
                }

                if (openings != null)
                {
                    foreach (List<XYZ> openingPolygon in openings)
                    {
                        elements.AddRange(AppendPolygon(doc, openingPolygon));
                    }
                }

                if (lines != null)
                {
                    foreach (var line in lines)
                    {
                        elements.Add(AddInclinedBeam(doc, level, line, section, symbol));
                    }
                }

                if (mapRimBeams != null)
                {
                    elements.AddRange(mapRimBeams.Select(x => x.SetData(level)));
                }

                if (mapBlockBeams != null)
                {
                    elements.AddRange(mapBlockBeams.Select(x => x.SetData(level)));
                }

                return CreateTheGroup(doc, elements.Where(x => x != null).Select(x => x.Id).ToList(), name, level);

            }
            catch (Exception e)
            {
                return null;
            }
        }
        #endregion

        #region CreateLevels
        public static Level CreateLevel(Document doc, double elevation, string name)
        {
            #region searchForSimilarLevel
            var sameLevel = new FilteredElementCollector(doc).OfClass(typeof(Level))
                .Cast<Level>().FirstOrDefault(x => Math.Abs(x.Elevation - elevation) < 0.001);

            if (sameLevel != null)
            {
                //if (sameLevel.Name != name)
                //{
                //    sameLevel.Name = name;
                //}

                return sameLevel;
            }
            #endregion

            #region AddNewLevel
            Level level = Level.Create(doc, elevation);
            if (null == level)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Error", "close and open Revit ");
            }
            else
            {
                //if (name != null)
                //    "Level " + name = level.Name;// Change the level name


                CreateViewPlan(doc, level);
            }
            return level;

            #endregion
        }
        private static void CreateViewPlan(Document doc, Level level)
        {
            try
            {
                IEnumerable<ViewFamilyType> viewFamilyTypes
                    = from elem in new FilteredElementCollector(doc)
                        .OfClass(typeof(ViewFamilyType))
                      let type = elem as ViewFamilyType
                      where type.ViewFamily == ViewFamily.FloorPlan
                      select type;

                ViewPlan.Create(doc, viewFamilyTypes.First().Id, level.Id);
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }
        #endregion

        #region Foundation
        public static Element CreateFoundation(Document doc, Level level, XYZ center, XYZ dir, double width, double length, double depth)
        {
            try
            {
                Family fam = GeneralCreator.CheckIfFamilyIsUploaded(doc, "Footing-Rectangular", DB_folder);
                FamilySymbol symbol = SelectSuitableFoundationFamilySymbol(doc, fam, width, length, depth);
                return CreateFoundationInstance(doc, level, symbol, center, dir);
            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }
        }
        private static Element CreateFoundationInstance(Document doc, Level level, FamilySymbol symbol, XYZ center, XYZ dir)
        {
            try
            {
                FamilyInstance fi = null;

                center = new XYZ(center.X, center.Y, level.Elevation);
                fi = doc.Create.NewFamilyInstance(center, symbol, null, level, StructuralType.Footing);

                double rotationangle = RotationAngleWithYAxis(dir);
                if (Math.Abs(rotationangle) > 0.0001)
                {
                    ElementTransformUtils.RotateElement(doc, fi.Id, Line.CreateBound(center, center + XYZ.BasisZ), -1 * rotationangle);
                }

                return fi;
            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }
        }
        private static FamilySymbol SelectSuitableFoundationFamilySymbol(Document doc, Family fam, double width, double length, double depth)
        {
            if (doc == null || fam == null)
                return null;

            var parameters = new Dictionary<string, object>();
            parameters.Add("Length", length);
            parameters.Add("Width", width);
            parameters.Add("Foundation Thickness", depth);
            FamilySymbol symbol = GeneralCreator.SelectSuitableSymbol(doc, fam, parameters);
            if (symbol == null)
            {
                string NewName = +Math.Round(length / 12.00, 1) + " X " + Math.Round(width / 12.0, 1) + " X " + Math.Round(depth / 12.0, 1);
                GeneralCreator.SolveDuplicatedFamilySymbolName(doc, fam, ref NewName);
                symbol = GeneralCreator.UpdateSuitableSymbol(doc, fam, parameters, NewName);
            }

            return symbol;
        }
        #endregion

        #region Trusses
        internal static Element CreateTruss(Document doc, int type, List<XYZ> profile, double startOverHang, double endOverHang, double width, double topDepth, double webDepth, double bottomDepth, double bottomChordLevel)
        {
            try
            {
                string FamilyName = "";
                FamilyInstance result = null;
                switch (type)
                {
                    case 1:
                        FamilyName = "KingPostTruss";
                        result = CreateTriangleProfileTruss(doc, FamilyName, profile, startOverHang, endOverHang, width, topDepth, webDepth, bottomDepth, bottomChordLevel);
                        break;
                }
                return result;
            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }
        }
        private static FamilyInstance CreateTriangleProfileTruss(Document doc, string familyName, List<XYZ> profile, double startOverHang, double endOverHang, double width, double topDepth, double webDepth, double bottomDepth, double bottomChordLevel)
        {
            try
            {
                return null;
            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;

            }
        }
        internal static double RotationAngleWithYAxis(XYZ dir)
        {
            try
            {
                if (dir.IsAlmostEqualTo(XYZ.BasisX))
                {
                    return Math.PI / 2.00;
                }
                else if (dir.IsAlmostEqualTo(-1 * XYZ.BasisX))
                {
                    return -1 * Math.PI / 2.00;
                }
                else if (dir.IsAlmostEqualTo(XYZ.BasisY) || dir.IsAlmostEqualTo(-1 * XYZ.BasisY))
                {
                    return 0.00;
                }
                else if (dir.X >= 0.0 && dir.Y >= 0.0)
                {
                    return Math.PI / 2.00 - Math.Acos(dir.DotProduct(XYZ.BasisX));
                }
                else if (dir.X >= 0.0 && dir.Y <= 0.0)
                {
                    return Math.PI / 2.00 + Math.Acos(dir.DotProduct(XYZ.BasisX));
                }
                else if (dir.X <= 0.0 && dir.Y <= 0.0)
                {
                    return -1 * (Math.PI / 2.00 + Math.Acos((-1 * dir).DotProduct(XYZ.BasisX)));
                }
                else if (dir.X <= 0.0 && dir.Y >= 0.0)
                {
                    return -1 * (Math.PI / 2.00 - Math.Acos((-1 * dir).DotProduct(XYZ.BasisX)));
                }
                return 0.0;

            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return 0;
            }
        }

        internal static double RotationAngleWithXAxis(XYZ dir)
        {
            try
            {
                if (dir.IsAlmostEqualTo(XYZ.BasisX))
                {
                    return 0.0;
                }
                else if (dir.IsAlmostEqualTo(-1 * XYZ.BasisX))
                {
                    return -1 * Math.PI;
                }
                else if (dir.IsAlmostEqualTo(XYZ.BasisY))
                {
                    return -1 * Math.PI / 2.00;
                }
                else if (dir.IsAlmostEqualTo(-1 * XYZ.BasisY))
                {
                    return Math.PI / 2.00;
                }
                else if (dir.X >= 0.0 && dir.Y >= 0.0)
                {
                    return Math.Asin(dir.DotProduct(XYZ.BasisY));
                }
                else if (dir.X >= 0.0 && dir.Y <= 0.0)
                {
                    return Math.Asin(dir.DotProduct(XYZ.BasisY));
                }
                else if (dir.X <= 0.0 && dir.Y <= 0.0)
                {
                    return Math.Asin((-1 * dir).DotProduct(XYZ.BasisY));
                }
                else if (dir.X <= 0.0 && dir.Y >= 0.0)
                {
                    return Math.Asin((-1 * dir).DotProduct(XYZ.BasisY));
                }
                return 0.0;

            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return 0;
            }
        }

        internal static FamilyInstance CreateGeneralTruss(Document doc, string Familyname, string FolderPath, XYZ center, XYZ dir)
        {
            try
            {
                Dictionary<List<XYZ>, double> dict = new Dictionary<List<XYZ>, double>();
                double RotationAngle = RotationAngleWithXAxis(dir);
                FamilyInstance fi = CreateInstanceFromFamily(doc, center, Familyname, FolderPath, StructuralType.Brace);

                if (Math.Abs(RotationAngle) > 0.0001)
                {
                    ElementTransformUtils.RotateElement(doc, fi.Id, Line.CreateBound(center, center + XYZ.BasisZ), -1 * RotationAngle);
                }

                return fi;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        internal static void ClearGenericFamiliesFolder()
        {
            try
            {
                string folderName = "GenericTrussess";
                string folderPath = Path.Combine(rootpath, folderName);
                System.IO.DirectoryInfo di = new DirectoryInfo(folderPath);

                foreach (FileInfo file in di.GetFiles())
                {
                    if (file.Name.EndsWith(".rfa"))
                        file.Delete();
                }

            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
            }
        }

        internal static string GetRevitBaseTemplate()
        {
            try
            {
                string folderName = "Templates";
                string folderPath = Path.Combine(rootpath, folderName);
                string templateFilePath = Path.Combine(folderPath, "baseTemplate.rte");

                return templateFilePath;
            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }
        }

        internal static string CreateTrussFamily(Document doc, ref string FamilyName, Dictionary<List<XYZ>, double> dict)
        {
            try
            {
                string folderName = "GenericTrussess";
                string folderPath = Path.Combine(rootpath, folderName);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                SolveDuplicatedFamilyName(folderPath, ref FamilyName);

                Document fdoc = CreateNemFamilyDocument(doc, folderName, "Generic Model.rft");
                if (null == fdoc)
                    return null;

                using (Transaction t = new Transaction(fdoc, "GenericTruss"))
                {
                    t.Start();

                    FailureHandlingOptions failopt = t.GetFailureHandlingOptions();
                    failopt.SetFailuresPreprocessor(new RevitHandler());
                    t.SetFailureHandlingOptions(failopt);

                    Material m = GetMaterial(fdoc, "Wood", new Color(154, 107, 61));
                    foreach (var item in dict)
                    {
                        Extrusion e = CreateExtrusion(fdoc, item.Key, item.Value, m);
                    }

                    t.Commit();
                }

                SaveAsOptions opt = new SaveAsOptions();
                opt.OverwriteExistingFile = true;

                fdoc.SaveAs(Path.Combine(folderPath, FamilyName + ".rfa"), opt);
                fdoc.Close(true);

                return folderPath;


            }
            catch (Exception e)
            {
                return null;
            }
        }

        internal static string CreateRoofByPlanes(Document doc, double thickness, ref string FamilyName, List<List<XYZ>> SolidPlanes,
                List<List<XYZ>> openingList, List<List<XYZ>> PopUpOpeningList, List<List<Line>> SurfaceList)
        {
            try
            {
                string folderName = "GenericTrussess";
                string folderPath = Path.Combine(rootpath, folderName);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                SolveDuplicatedFamilyName(folderPath, ref FamilyName);

                Document fdoc = CreateNemFamilyDocument(doc, folderName, "Generic Model Adaptive.rft");
                if (null == fdoc)
                    return null;

                //double extrusionthickness = (0.05 / 12.00);
                XYZ Normal = XYZ.BasisZ * thickness;
                using (Transaction t = new Transaction(fdoc, "GenericRoof"))
                {
                    t.Start();

                    FailureHandlingOptions failopt = t.GetFailureHandlingOptions();
                    failopt.SetFailuresPreprocessor(new RevitHandler());
                    t.SetFailureHandlingOptions(failopt);

                    foreach (var pointList in SolidPlanes)
                    {
                        CreateExtrusionForm(fdoc, pointList, Normal, true);
                    }
                    foreach (var pointList in openingList)
                    {
                        CreateExtrusionForm(fdoc, pointList, Normal, false);
                    }
                    foreach (var pointList in PopUpOpeningList)
                    {
                        CreateExtrusionForm(fdoc, pointList, Normal, false);
                    }
                    foreach (var LineList in SurfaceList)
                    {
                        CreateExtrusionForm(fdoc, LineList, Normal, true);
                    }
                    t.Commit();
                }

                SaveAsOptions opt = new SaveAsOptions();
                opt.OverwriteExistingFile = true;

                fdoc.SaveAs(Path.Combine(folderPath, FamilyName + ".rfa"), opt);
                fdoc.Close(true);

                return folderPath;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        #endregion

        #region General Family and Family Instance Functions
        public static void CreateRoofFootPrint(Document doc, List<MapXYZ> points)
        {
            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                collector.OfClass(typeof(Level));
                //var elements = from element in collector where element.Name == "Roof" select element;
                Level level = collector.Cast<Level>().LastOrDefault();

                collector = new FilteredElementCollector(doc);
                collector.OfClass(typeof(RoofType));
                RoofType roofType = collector.LastOrDefault() as RoofType;

                // Get the handle of the application
                //Autodesk.Revit.ApplicationServices.Application application = doc.Application;

                // Define the footprint for the roof based on user selection
                CurveArray curves = new CurveArray();

                for (int i = 0; i < points.Count - 1; i++)
                {
                    Line line = Line.CreateBound(points[i].GetReference(), points[i + 1].GetReference());

                    curves.Append(line);
                    //Plane p = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, fp[0]);
                    //SketchPlane skplane = SketchPlane.Create(doc, p);

                    //var modelcurve = doc.Create.NewModelCurve(Line.CreateBound(fp[i], fp[j]), skplane);
                    //footprint.Append(curve);
                }

                ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
                FootPrintRoof footprintRoof = doc.Create.NewFootPrintRoof(curves, level, roofType, out footPrintToModelCurveMapping);
                ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
                iterator.Reset();
                while (iterator.MoveNext())
                {
                    ModelCurve modelCurve = iterator.Current as ModelCurve;
                    footprintRoof.set_DefinesSlope(modelCurve, true);
                    footprintRoof.set_SlopeAngle(modelCurve, 0.5);
                }
            }
            catch (Exception e)
            {


            }

        }



        private static Autodesk.Revit.DB.Form CreateExtrusionForm(Document fdoc, List<Line> SurfaceLines, XYZ Normal, bool IsSolid)
        {
            ReferencePointArray rpa1 = new ReferencePointArray();
            ReferencePointArray rpa2 = new ReferencePointArray();
            ReferenceArray referenceArray = new ReferenceArray();
            ReferenceArrayArray referenceArrayArray = new ReferenceArrayArray();
            try
            {
                for (int i = 0; i < SurfaceLines.Count; i++)
                {
                    ReferencePoint rp1 = fdoc.FamilyCreate.NewReferencePoint(SurfaceLines[i].GetEndPoint(0));
                    ReferencePoint rp2 = fdoc.FamilyCreate.NewReferencePoint(SurfaceLines[i].GetEndPoint(1));
                    rpa1.Append(rp1);
                    rpa2.Append(rp2);
                }

                var curve1 = fdoc.FamilyCreate.NewCurveByPoints(rpa1);
                var curve2 = fdoc.FamilyCreate.NewCurveByPoints(rpa2);

                referenceArray.Append(curve1.GeometryCurve.Reference);
                referenceArrayArray.Append(referenceArray);

                referenceArray = new ReferenceArray();

                referenceArray.Append(curve2.GeometryCurve.Reference);
                referenceArrayArray.Append(referenceArray);

                return fdoc.FamilyCreate.NewLoftForm(IsSolid, referenceArrayArray);
            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }


        }
        //private static Autodesk.Revit.DB.Form CreateExtrusionForm(Document fdoc, List<Line> SurfaceLines, XYZ Normal, bool IsSolid)
        //{

        //    try
        //    {
        //        ReferenceArrayArray ref_ar_ar = new ReferenceArrayArray();
        //        ReferenceArray ref_ar = new ReferenceArray();
        //        for (int i = 0; i < SurfaceLines.Count; i++)
        //        {
        //            Plane p = Plane.CreateByNormalAndOrigin(new XYZ(SurfaceLines[i].Direction.Y, SurfaceLines[i].Direction.X, 0), SurfaceLines[i].GetEndPoint(0));
        //            SketchPlane skplane = SketchPlane.Create(fdoc, p);
        //            ModelCurve modelcurve = fdoc.FamilyCreate.NewModelCurve(SurfaceLines[i], skplane);
        //            ref_ar.Append(modelcurve.GeometryCurve.Reference);
        //            ref_ar_ar.Append(ref_ar);
        //            ref_ar = new ReferenceArray();
        //        }
        //        return fdoc.FamilyCreate.NewLoftForm(IsSolid, ref_ar_ar);
        //    }
        //    catch (Exception e)
        //    {
        //        ErrorHandler.ReportException(e);
        //        return null;
        //    }


        //}
        private static Autodesk.Revit.DB.Form CreateExtrusionForm(Document fdoc, List<XYZ> planePoints, XYZ Normal, bool IsSolid)
        {
            try
            {
                ReferenceArray ref_ar = new ReferenceArray();
                Plane p = null;
                if (planePoints.Count > 10)
                {
                    p = Plane.CreateByThreePoints(planePoints.FirstOrDefault(), planePoints[(int)(planePoints.Count / 2)], planePoints.LastOrDefault());
                }
                else
                {
                    p = Plane.CreateByThreePoints(planePoints[0], planePoints[1], planePoints[2]);
                }
                SketchPlane skplane = SketchPlane.Create(fdoc, p);
                for (int i = 0; i < planePoints.Count; i++)
                {
                    int j = (i + 1) % planePoints.Count;
                    if (planePoints[i].DistanceTo(planePoints[j]) < fdoc.Application.ShortCurveTolerance)
                    {
                        planePoints.Remove(planePoints[j]);
                        planePoints.ToList();
                        i--;
                        continue;
                    }

                    ModelCurve modelcurve = fdoc.FamilyCreate.NewModelCurve(Line.CreateBound(planePoints[i], planePoints[j]), skplane);
                    ref_ar.Append(modelcurve.GeometryCurve.Reference);
                }
                return fdoc.FamilyCreate.NewExtrusionForm(IsSolid, ref_ar, Normal);

            }
            catch (Exception e)
            {
                ErrorHandler.ReportException(e);
                return null;
            }
        }

        private static void CreateTessellatedShape(Document fdoc, List<Line> SurfaceLines, XYZ Normal, bool IsSolid, ElementId materialId)
        {
            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(fdoc);
                var m = collector.OfCategory(BuiltInCategory.OST_Materials).Cast<Material>().ToList();
                materialId = m.FirstOrDefault().Id;

                List<XYZ> loopVertices = new List<XYZ>();
                TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
                builder.OpenConnectedFaceSet(true);

                for (int i = 0; i < SurfaceLines.Count - 1; i++)
                {
                    loopVertices.Add(SurfaceLines[i].GetEndPoint(0));
                    loopVertices.Add(SurfaceLines[i].GetEndPoint(1));
                    loopVertices.Add(SurfaceLines[i + 1].GetEndPoint(1));
                    loopVertices.Add(SurfaceLines[i + 1].GetEndPoint(0));
                    builder.AddFace(new TessellatedFace(loopVertices, materialId));
                    loopVertices.Clear();

                }
                builder.CloseConnectedFaceSet();
                builder.Build();
                TessellatedShapeBuilderResult result = builder.GetBuildResult();
                DirectShape ds = DirectShape.CreateElement(fdoc, new ElementId(BuiltInCategory.OST_GenericModel));
                ds.ApplicationId = "Application id";
                ds.ApplicationDataId = "Geometry object id";
                ds.SetShape(result.GetGeometricalObjects());

            }
            catch (Exception e)
            {


            }
        }


        private static Extrusion CreateExtrusion(Document fdoc, List<XYZ> profile, double width, Material m)
        {
            try
            {
                CurveArray curveArray = new CurveArray();
                profile = profile.Select(x => x - 0.5 * width * XYZ.BasisY).ToList();
                for (int i = 0; i < profile.Count; i++)
                {
                    curveArray.Append(Line.CreateBound(profile[i], profile[(i + 1) % profile.Count]));
                }
                CurveArrArray curveArrayArray = new CurveArrArray();
                curveArrayArray.Append(curveArray);
                Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisY, XYZ.Zero);
                Extrusion extrusion = null;
                SketchPlane sketchPlane = SketchPlane.Create(fdoc, plane);
                extrusion = fdoc.FamilyCreate.NewExtrusion(true, curveArrayArray, sketchPlane, width);
                //extrusion.StartOffset = width * offfset;
                if (m != null)
                {
                    extrusion.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM).Set(m.Id);
                }

                return extrusion;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        private static Material GetMaterial(Document doc, string MaterialClassName, Color c)
        {
            try
            {
                var materials = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>();
                Material m = materials.FirstOrDefault(x => x.MaterialClass == MaterialClassName || x.MaterialCategory == MaterialClassName);
                if (m == null)
                {
                    m = materials.FirstOrDefault();
                    m = m.Duplicate("WoodMaterial");
                    m.Transparency = 4;

                    m.CutForegroundPatternColor = c;
                    m.CutBackgroundPatternColor = c;

                    m.SurfaceForegroundPatternColor = c;
                    m.SurfaceBackgroundPatternColor = c;

                    m.Color = c;
                    m.Shininess = 64;
                    m.Smoothness = 50;
                }

                ElementId appearanceAssetId = m.AppearanceAssetId;
                AppearanceAssetElement assetElem = m.Document.GetElement(appearanceAssetId) as AppearanceAssetElement;

                using (AppearanceAssetEditScope editScope = new AppearanceAssetEditScope(assetElem.Document))
                {
                    Asset editableAsset = editScope.Start(assetElem.Id);
                    AssetPropertyDoubleArray4d genericDiffuseProperty = editableAsset.FindByName("generic_diffuse") as AssetPropertyDoubleArray4d;

                    if (genericDiffuseProperty != null)
                    {
                        genericDiffuseProperty.SetValueAsColor(c);
                        editScope.Commit(true);
                    }
                }

                return m;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static Document CreateNemFamilyDocument(Document doc, string folderName, string TemplateName)
        {
            try
            {
                string folderPath = Path.Combine(rootpath, folderName);
                string TemplatePath = Path.Combine(folderPath, TemplateName);

                return doc.Application.NewFamilyDocument(TemplatePath);
            }
            catch (Exception e)
            {
                return null;
            }

        }
        public static void SolveDuplicatedFamilySymbolName(Document doc, Family fam, ref string newName)
        {
            try
            {
                string name = newName;
                Random r = new Random();
                while (true)
                {
                    FamilySymbol Group = fam.GetFamilySymbolIds().Select(x => doc.GetElement(x)).Cast<FamilySymbol>().FirstOrDefault(x => x.Name == name);
                    if (Group == null)
                        break;
                    else
                        name = newName + "(" + r.Next(0, 1000).ToString() + ")";
                }
                newName = name;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }
        private static void SolveDuplicatedGroupsName(Document doc, ref string newName)
        {
            try
            {
                string name = newName;
                Random r = new Random();
                while (true)
                {
                    Group Group = new FilteredElementCollector(doc).OfClass(typeof(Group)).OfCategory(BuiltInCategory.OST_IOSModelGroups).Cast<Group>().FirstOrDefault(x => x.GroupType.Name == name);
                    if (Group == null)
                        break;
                    else
                        name = newName + "(" + r.Next(0, 1000).ToString() + ")";
                }
                newName = name;

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }
        private static void SolveDuplicatedFamilyName(string path, ref string FamilyName)
        {
            try
            {
                string name = FamilyName;
                Random r = new Random();
                while (true)
                {
                    if (!File.Exists(Path.Combine(path, name + ".rfa")))
                        break;
                    else
                        name = FamilyName + "(" + r.Next(0, 1000).ToString() + ")";
                }
                FamilyName = name;

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }
        public static FamilySymbol UpdateSuitableSymbol(Document doc, Family fam, Dictionary<string, object> parameters, string newName)
        {
            try
            {
                if (fam == null) { return null; }
                FamilySymbol s;

                s = (doc.GetElement(fam.GetFamilySymbolIds().FirstOrDefault()) as FamilySymbol).Duplicate(newName) as FamilySymbol;
                foreach (var param in parameters)
                {
                    Parameter p = s.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == param.Key);
                    if (p == null) { continue; }
                    Type t = param.Value.GetType();

                    if (t == typeof(double) || t == typeof(float)) { p.Set(Convert.ToDouble(param.Value)); }
                    else if (t == typeof(string)) { p.Set(Convert.ToString(param.Value)); }
                    else if (t == typeof(int) || t == typeof(bool)) { p.Set(Convert.ToInt32(param.Value)); }
                }

                return s;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static FamilySymbol SelectSuitableSymbol(Document doc, Family fam, Dictionary<string, object> parameters)
        {
            try
            {
                bool flag;
                foreach (ElementId id in fam.GetFamilySymbolIds())
                {
                    flag = true;
                    FamilySymbol s = doc.GetElement(id) as FamilySymbol;
                    foreach (var param in parameters)
                    {
                        Type t = param.Value.GetType();
                        if (t == typeof(double) || t == typeof(float))
                        {
                            var par = s.LookupParameter(param.Key);
                            if (par == null)
                            {
                                flag = false;

                            }
                            else
                            {
                                var RealValue = par.AsDouble();
                                flag = Math.Abs(RealValue - Convert.ToDouble(param.Value)) < 0.0001;
                            }

                        }
                        else
                        {
                            string RealValue = s.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == param.Key).AsValueString();
                            flag = (RealValue == param.Value.ToString());
                        }
                        if (!flag)
                        { break; }
                    }
                    if (flag)
                    { return s; }
                }
                return null;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }

        }
        public static Family CheckIfFamilyIsUploaded(Document doc, string v, string path)
        {
            try
            {
                Family fam = new FilteredElementCollector(doc).OfClass(typeof(Family)).Cast<Family>().FirstOrDefault(x => x.Name == v);
                if (fam == null)
                {
                    fam = LoadFamily(doc, path, v);
                }
                return fam;

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        internal static Family LoadFamily(Document doc, string path, string FamilyName)
        {
            try
            {
                Family fam = new FilteredElementCollector(doc).OfClass(typeof(Family)).Cast<Family>().FirstOrDefault(q => q.Name == FamilyName);
                if (fam == null)
                {
                    if (doc.LoadFamily(Path.Combine(path, FamilyName) + @".rfa", out fam))
                    {
                        foreach (ElementId s in fam.GetFamilySymbolIds())
                        {
                            FamilySymbol symbol = doc.GetElement(s) as FamilySymbol;
                            if (!symbol.IsActive)
                                symbol.Activate();

                        }
                        return fam;
                    }
                    else { return null; }
                }
                else { return fam; }
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }

        }

        internal static FamilyInstance CreateInstanceFromFamily(Document doc, XYZ basePoint, string FamilyName, string FolderPath, Autodesk.Revit.DB.Structure.StructuralType type)
        {
            try
            {
                FamilyInstance Familyinstance = null;
                FamilySymbol sam = null;
                Family fam = GeneralCreator.LoadFamily(doc, FolderPath, FamilyName);
                if (fam != null)
                {
                    sam = doc.GetElement(fam.GetFamilySymbolIds().FirstOrDefault()) as FamilySymbol;
                    if (sam != null)
                    {
                        Familyinstance = doc.Create.NewFamilyInstance(basePoint, sam, type);
                    }
                }

                return Familyinstance;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        #endregion

    }
    public class RevitHandler : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            failuresAccessor.DeleteAllWarnings();

            return FailureProcessingResult.Continue;
        }
    }

    public static class GeometryMathatics
    {
        public static IntersectionResult Point_PolygonInterSection(XYZ point, List<XYZ> polygonpoints)
        {
            try
            {
                IntersectionResult result = IntersectionResult.Outside;
                int NotNull = 0;
                var xvalues = polygonpoints.Select(x => x.X);
                var yvalues = polygonpoints.Select(x => x.Y);
                XYZ direction = new XYZ((xvalues.Min() + xvalues.Max()) / 2.00, (yvalues.Min() + yvalues.Max()) / 2.00, 0);
                bool flag = false;
                polygonpoints.Add(polygonpoints[0]);
                for (int i = 0; i < polygonpoints.Count - 1; i++)
                {
                    if (polygonpoints[i].DistanceTo(point) < 0.0001)
                    {
                        result = IntersectionResult.Corner;
                        flag = true;
                        break;
                    }
                    else if (IsPointinLine(point, polygonpoints[i], polygonpoints[i + 1]))
                    {
                        result = IntersectionResult.OnEdge;
                        flag = true;
                        break;
                    }
                    if (DropRay(point, direction, polygonpoints[i], polygonpoints[i + 1]) != null)
                    {
                        NotNull++;
                    }
                }
                polygonpoints.Remove(polygonpoints.Last());
                if (!flag)
                {
                    if (NotNull % 2 == 0)
                    { result = IntersectionResult.Outside; }
                    else
                    { result = IntersectionResult.inSide; }
                }


                return result;
            }
            catch (Exception)
            {
                return IntersectionResult.Unknown;
            }


        }
        public static bool IsPointinLine(XYZ point, XYZ a, XYZ b)
        {
            return Math.Abs(point.DistanceTo(a) + point.DistanceTo(b) - a.DistanceTo(b)) < 0.00001;
        }
        public static double distancefrompointToline(XYZ point, XYZ a, XYZ b)
        {
            XYZ v1 = point - a;
            XYZ v2 = b - a;
            XYZ v3 = v1.CrossProduct(v2);

            return v3.GetLength() / v2.GetLength();
        }
        public static List<XYZ> GetOnlyBoundaryPoints(List<XYZ> Points)
        {
            List<XYZ> result = new List<XYZ>();

            XYZ predir = (Points[0] - Points[Points.Count - 1]).Normalize();
            XYZ nextdir = (Points[1] - Points[0]).Normalize();
            if (!IsTwoDirectionParallel(predir, nextdir))
            {
                result.Add(Points[0]);
            }

            for (int i = 1; i < Points.Count - 1; i++)
            {
                predir = (Points[i] - Points[i - 1]).Normalize();
                nextdir = (Points[i + 1] - Points[i]).Normalize();
                if (!IsTwoDirectionParallel(predir, nextdir))
                {
                    result.Add(Points[i]);
                }
            }

            predir = (Points[Points.Count - 1] - Points[Points.Count - 2]).Normalize();
            nextdir = (Points[0] - Points[Points.Count - 1]).Normalize();
            if (!IsTwoDirectionParallel(predir, nextdir))
            {
                result.Add(Points[Points.Count - 1]);
            }
            return result;
        }

        public static bool IsTwoPointsEquals(XYZ a, XYZ b)
        {
            return (a == b ||
                (Math.Abs(a.X - b.X) < 0.0001 && Math.Abs(a.Y - b.Y) < 0.0001 && Math.Abs(a.Z - b.Z) < 0.0001));

        }

        public static List<efxPoint3> CCWSort(List<efxPoint3> points)
        {
            efxPoint3 center = new efxPoint3();
            foreach (var point in points)
            {
                center.X += point.X; center.Y += point.Y; center.Z += point.Z;
            }
            center.X /= points.Count;
            center.Y /= points.Count;
            center.Z /= points.Count;

            points.Sort((a, b) =>
            {
                double a1 = ((Math.Atan2(a.X - center.X, a.Y - center.Y) * 180 / Math.PI) + 360) % 360;
                double a2 = ((Math.Atan2(b.X - center.X, b.Y - center.Y) * 180 / Math.PI) + 360) % 360;
                return (int)(a1 - a2);
            });
            return points;
        }

        public static XYZ DropRay(XYZ source, XYZ direction, XYZ c, XYZ d)
        {
            XYZ point = Two_Lines_Intersect(source, direction, c, d);

            if (point == null)
                return null;

            if (!IsPointinLine(point, c, d))
                return null;

            XYZ dir1 = (source - point).Normalize();
            XYZ dir2 = (source - direction).Normalize();
            double dot = dir1.DotProduct(dir2);
            if (dot > 0 && Math.Abs(dot - 1) < 0.001)
            { return point; }
            else
            { return null; }
        }

        public static XYZ Two_Lines_Intersect(XYZ a, XYZ b, XYZ c, XYZ d)
        {
            XYZ dir1 = (b - a).Normalize();
            XYZ dir2 = (d - c).Normalize();
            double dot = dir1.DotProduct(dir2);
            if (Math.Abs(Math.Abs(dot) - 1) < 0.00001)
            { return null; }


            XYZ ab = b - a;
            XYZ cd = d - c;


            double A = ab.X; //a
            double B = -cd.X; //b
            double Y1 = c.X - a.X; //y1
            double C = ab.Y; //c
            double D = -cd.Y; //d
            double Y2 = c.Y - a.Y; //y2

            double q1;
            double q2;
            //  Solve(ref M);
            q1 = (Y1 * D - Y2 * B) / (A * D - C * B);
            q2 = (Y1 * C - Y2 * A) / (B * C - D * A);

            if (Math.Abs(ab.Z * q1 - cd.Z * q2 + a.Z - c.Z) < 0.000001)
            { return a + q1 * ab; }
            else
            { return null; }
        }

        public static XYZ Two_Lines_Intersect_vertical(XYZ a, XYZ b, XYZ c, XYZ d)
        {
            XYZ dir1 = (b - a).Normalize();
            XYZ dir2 = (d - c).Normalize();
            double dot = dir1.DotProduct(dir2);
            if (Math.Abs(Math.Abs(dot) - 1) < 0.00001)
            { return null; }


            XYZ ab = b - a;
            XYZ cd = d - c;


            double A = ab.X; //a
            double B = -cd.X; //b
            double Y1 = c.X - a.X; //y1
            double C = ab.Z; //c
            double D = -cd.Z; //d
            double Y2 = c.Z - a.Z; //y2
            if (Math.Abs(A * D - C * B) < 0.001 || Math.Abs(B * C - D * A) < 0.001)
            {
                A = ab.Y; //a
                B = -cd.Y; //b
                Y1 = c.Y - a.Y; //y1


                double q1;
                double q2;
                //  Solve(ref M);
                q1 = (Y1 * D - Y2 * B) / (A * D - C * B);
                q2 = (Y1 * C - Y2 * A) / (B * C - D * A);

                if (Math.Abs(ab.X * q1 - cd.X * q2 + a.X - c.X) < 0.000001)
                { return a + q1 * ab; }
                else
                { return null; }
            }
            else
            {
                double q1;
                double q2;
                //  Solve(ref M);
                q1 = (Y1 * D - Y2 * B) / (A * D - C * B);
                q2 = (Y1 * C - Y2 * A) / (B * C - D * A);

                if (Math.Abs(ab.Y * q1 - cd.Y * q2 + a.Y - c.Y) < 0.000001)
                { return a + q1 * ab; }
                else
                { return null; }

            }


        }

        public static efxPoint Two_Lines_Intersect(efxPoint a, efxPoint b, efxPoint c, efxPoint d)
        {
            XYZ result = Two_Lines_Intersect(new XYZ(a.X, a.Y, 0), new XYZ(b.X, b.Y, 0), new XYZ(c.X, c.Y, 0), new XYZ(d.X, d.Y, 0));
            return new efxPoint(result.X, result.Y);
        }

        public static EFXYZ Two_Lines_Intersect(EFXYZ a, EFXYZ b, EFXYZ c, EFXYZ d)
        {
            XYZ result = Two_Lines_Intersect(new XYZ(a.X, a.Y, 0), new XYZ(b.X, b.Y, 0), new XYZ(c.X, c.Y, 0), new XYZ(d.X, d.Y, 0));
            return new EFXYZ(result.X, result.Y, 0.0);
        }
        public static XYZ GetAnother_pointInDirection(XYZ point, float angle)
        {
            return point + new XYZ(Math.Cos(angle * Math.PI / 180), Math.Sin(angle * Math.PI / 180), 0);
        }

        public static float AngleToPitch(double angle)
        {
            return (float)(12 * Math.Tan(angle));
        }

        public static XYZ get_mid_point(XYZ a, XYZ b)
        {
            // this function take two inputs :
            // XYZ a ,XYZ b :  vectors from orgin to points to a , b 
            // return vector to a midpoint between a & b 
            return new XYZ((a.X + b.X) / 2.00, (a.Y + b.Y) / 2.00, (a.Z + b.Z) / 2.00);
        }

        public static XYZ Drop_Prependicular_From(XYZ from, XYZ a, XYZ b)
        {


            if (from.DistanceTo(a) < 0.0001)
            {
                return a;
            }
            else if (from.DistanceTo(b) < 0.0001)
            {
                return b;
            }
            else
            {
                XYZ ab = b - a;
                XYZ pa = a - from;

                double q = -pa.DotProduct(ab) / (ab.DotProduct(ab));
                return a.Add(ab.Multiply(q));
            }


        }

        public static double GetAngleFrom3Points(XYZ first, XYZ intersect, XYZ second)
        {
            // this Function Return the value of angle (abc)
            // XYZ a ,XYZ b , XYZ b :  vectors from orgin to points to a , b ,c
            XYZ dir1 = intersect - first;
            XYZ dir2 = intersect - second;
            return dir1.AngleTo(dir2);
        }
        public static bool IsTwoDirectionParallel(XYZ dir1, XYZ dir2)
        {
            return (Math.Abs(Math.Abs((dir1.Normalize()).DotProduct(dir2.Normalize())) - 1) < 0.0001);
        }

        public static bool IsTwoDirectionSame(XYZ dir1, XYZ dir2)
        {
            return (Math.Abs((dir1.Normalize()).DotProduct(dir2.Normalize()) - 1) < 0.0001);
        }

        public static double PointLineDist(XYZ P0, XYZ P1, XYZ P2)
        {
            // Cross product V1 x V2 = (V1)(V2)(sin(theta))
            // D = |V1|sin(theta) = (V1 . V2) / |V2|

            XYZ V1 = P0 - P1;
            XYZ V2 = P2 - P1;
            XYZ V3 = V1.CrossProduct(V2);

            double L1 = V1.GetLength();
            double L2 = V2.GetLength();

            if (Math.Abs(L2) < 0.00001)
                return L1;

            double cp = V3.GetLength();
            return cp / L2;
        }

        public static bool IsPointOnLine(XYZ P0, XYZ P1, XYZ P2)
        {

            double d = PointLineDist(P0, P1, P2);
            return d < 0.001;
        }
        public static bool IsPointOnLineSegment(XYZ P0, XYZ P1, XYZ P2)
        {

            double d = PointLineDist(P0, P1, P2);

            if (d > 0.001) // Not collinear
                return false;

            XYZ projectedPoint = PointToLineProjection(P0, P1, P2);

            XYZ V = P2 - P1;
            XYZ V1 = P1 - projectedPoint;
            XYZ V2 = P2 - projectedPoint;

            double L = V.GetLength();
            double L1 = V1.GetLength();
            double L2 = V2.GetLength();

            return (Math.Abs(L - (L1 + L2)) <= 0.001);
        }

        public static XYZ PointToLineProjection(XYZ P0, XYZ P1, XYZ P2)
        {
            XYZ V1 = P0 - P1;
            XYZ V2 = P2 - P1;

            float L2 = (float)V2.GetLength();

            if (L2 <= 0.0001)
                return P1;

            float dp = (float)V1.DotProduct(V2);
            float d = dp / L2;
            float t = d / L2;

            return (P1 + t * V2);

        }
    }

    public enum IntersectionResult
    {
        Corner,
        OnEdge,
        inSide,
        Outside,
        Unknown

    }

}
