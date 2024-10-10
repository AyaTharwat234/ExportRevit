using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using EFRvt.Revit;
using Elibre.Net.Debug;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFRvt
{
    public class efrvt
    {
        public static string rootpath = Path.Combine(Directory.GetParent((System.Reflection.Assembly.GetExecutingAssembly().Location)).FullName, "Revit");

        #region paths
        private static string DB_folder = Path.Combine(rootpath, "DB");
        public static FloorType GetSuitableSlabType(Document doc, double thickness)
        {
            //thickness = Math.Round(thickness, 2);

            FloorType floorType = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_FloorsDefault)
                    .WhereElementIsElementType()
                    .Cast<FloorType>()
                    .FirstOrDefault(x => x.Name.StartsWith("EF_") && x.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble() == thickness);


            // If a suitable floor type was not found, create a new one
            if (floorType == null)
            {
                // Get the generic floor type
                var genericFloorType = doc.GetDefaultElementTypeId(ElementTypeGroup.FloorType);
                var genericFloorTypeElem = doc.GetElement(genericFloorType) as FloorType;

                // Duplicate the generic floor type and set the thickness parameter
                floorType = genericFloorTypeElem.Duplicate($"EF_{thickness}_{Guid.NewGuid()}") as FloorType;
                var thicknessParam = floorType.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);
                if (thicknessParam != null)
                {
                    thicknessParam.Set(thickness);
                }

                // Set the compound structure of the new floor type
                var materialId = Material.Create(doc, $"EF_{thickness}_{Guid.NewGuid()}");
                var structure = CompoundStructure.CreateSingleLayerCompoundStructure(MaterialFunctionAssignment.Structure, thickness, materialId);
                structure.EndCap = EndCapCondition.NoEndCap;
                floorType.SetCompoundStructure(structure);

                // Set the new floor type as the suitable floor type

            }

            return floorType;
        }
        public static Element CreateCeilingSubstrate(Document doc, Level level, List<MapXYZ> Points, double height, double thickness, List<MapShellHoles> holes)
        {
            try
            {


                // create a list to hold the curves
                CurveArray curves = new CurveArray();


                for (int i = 0; i < Points.Count - 1; i++)
                {
                    // Offset the points upward
                    XYZ point1 = new XYZ(Points[i].X, Points[i].Y, Points[i].Z);
                    XYZ point2 = new XYZ(Points[i + 1].X, Points[i + 1].Y, Points[i + 1].Z);
                    Curve curv = Line.CreateBound(point1, point2);
                    curves.Append(curv);
                }

                XYZ lastPoint1 = new XYZ(Points[Points.Count - 1].X, Points[Points.Count - 1].Y, Points[Points.Count - 1].Z);
                XYZ lastPoint2 = new XYZ(Points[0].X, Points[0].Y, Points[0].Z);

                Curve curveLast = Line.CreateBound(lastPoint1, lastPoint2);
                curves.Append(curveLast);
                // Get the ceiling type
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                CeilingType ceilingType = collector.OfClass(typeof(CeilingType)).Cast<CeilingType>().FirstOrDefault();
                if (ceilingType != null)
                {
                    var thicknessParam = ceilingType.get_Parameter(BuiltInParameter.CEILING_HAS_THICKNESS_PARAM);
                    if (thicknessParam != null)
                    {
                        thicknessParam.Set(thickness);
                    }
                }
                // Set the height of the ceiling slab
                //ceilingType.get_Parameter(BuiltInParameter.CEILING_HAS_THICKNESS_PARAM).Set(thickness);

                // Create the ceiling slab
                // Create the floor slab

                //Wall ceiling = Wall.Create(doc, curves, ceilingType.Id, Level.Create(doc, 0).Id, thickness, 0, false, false);
                Element ceiling = doc.Create.NewSlab(curves, level, (Line)curveLast, 0, false);

                Parameter heightOffsetParam = ceiling.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
                if (heightOffsetParam != null)
                {
                    double maxWallHeight = Points.Max(p => p.Z);
                    double heightOffset = height;
                    heightOffsetParam.Set(maxWallHeight + thickness);
                }
                if (holes.Count != 0)
                {


                    foreach (var opening in holes)
                    {
                        //CreateOpening(doc, ceiling, opening.points, 0);
                        CreateSlabOpenings(doc, ceiling, opening.points, level, 0);
                    }
                }
                return ceiling;

            }
            //        return newFloor;


            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;

            }
        }
        public static Element CreateCeilingSlabs(Document doc, Level level, List<MapXYZ> Points, double height, double thickness)
        {
            try
            {




                // create a list to hold the curves

                CurveArray curves = new CurveArray();




                // create curves from the points
                for (int i = 0; i < Points.Count - 1; i++)
                {
                    XYZ currentPoint = Points[i].GetReference();
                    XYZ nextPoint = Points[i + 1].GetReference();
                    double distance = currentPoint.DistanceTo(nextPoint);
                    // Check if the distance is smaller than ShortCurveTolerance
                    if (distance < doc.Application.ShortCurveTolerance)
                    {
                        // Replace the 2 points with their midpoint
                        XYZ midpoint = (currentPoint + nextPoint) / 2.0;


                        Points[i].X = midpoint.X;
                        Points[i].Y = midpoint.Y;
                        Points[i].Z = midpoint.Z;
                        Points.RemoveAt(i + 1);
                        i--;

                    }

                }

                // Offset the points upward
                for (int i = 0; i < Points.Count - 1; i++)
                {
                    XYZ startPoint = new XYZ(Points[i].X, Points[i].Y, Points[i].Z + thickness);
                    XYZ endPoint = new XYZ(Points[i + 1].X, Points[i + 1].Y, Points[i + 1].Z + thickness);

                    Curve firstcurve = Line.CreateBound(startPoint, endPoint);
                    curves.Append(firstcurve);


                    //}
                }
                //Check if the curves form a closed loop

                //Offset the last point upward
                XYZ lastPoint1 = new XYZ(Points[Points.Count - 1].X, Points[Points.Count - 1].Y, Points[Points.Count - 1].Z + thickness);
                XYZ lastPoint2 = new XYZ(Points[0].X, Points[0].Y, Points[0].Z + thickness);


                Curve curve = Line.CreateBound(lastPoint1, lastPoint2);

                curves.Append(curve);

                // Get the ceiling type
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                //SketchPlane sketchPlane = SketchPlane.Create(doc, level.GetPlaneReference());
                var floorType = GetSuitableSlabType(doc, thickness);


                // Check if the curves form a closed loop

                //Floor floor = doc.Create.NewSlab(curves, level, line, 0, false);
                // Create the floor slab

                Floor floor = doc.Create.NewFloor(curves, floorType, level, true);

                Parameter thicknessParam = floorType.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);
                if (thicknessParam != null)
                {
                    thicknessParam.Set(thickness);
                }
                Parameter heightOffsetParam = floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
                if (heightOffsetParam != null)
                {
                    double maxWallHeight = Points.Max(p => p.Z);
                    double heightOffset = maxWallHeight;
                    heightOffsetParam.Set(heightOffset + thickness);
                }



                return floor;
            }

            catch (Exception ex)
            {

                return null;

            }
        }
        public static Element CreateFloorSlabs(Document doc, Level level, List<MapXYZ> points, double offset, double thickness, List<MapShellHoles> holes)
        {
            try
            {

                //if (!((Points[0]).GetReference()).IsAlmostEqualTo(((Points[Points.Count - 1]).GetReference())))
                //{
                //    Points.Add(Points[0]);
                //}


                // create a list to hold the curves
                CurveArray curveList = new CurveArray();
                //foreach (var curve in curveList)
                //{
                for (int i = 0; i < points.Count - 1; i++)
                {
                    XYZ currentPoint = points[i].GetReference();
                    XYZ nextPoint = points[i + 1].GetReference();
                    double distance = currentPoint.DistanceTo(nextPoint);
                    // Check if the distance is smaller than ShortCurveTolerance
                    if (distance < doc.Application.ShortCurveTolerance)
                    {
                        // Replace the 2 points with their midpoint
                        XYZ midpoint = (currentPoint + nextPoint) / 2.0;

                        midpoint = new XYZ(midpoint.X, midpoint.Y, midpoint.Z);
                        midpoint = points[i].GetReference();
                        points.RemoveAt(i + 1);
                        i--;

                    }

                }
                for (int i = 0; i < points.Count - 1; i++)
                {
                    XYZ point1 = new XYZ(points[i].X, points[i].Y, points[i].Z + offset);
                    XYZ point2 = new XYZ(points[i + 1].X, points[i + 1].Y, points[i + 1].Z + offset);
                    Curve curv = Line.CreateBound(point1, point2);
                    curveList.Append(curv);
                }

                // Offset the last point upward
                XYZ lastPoint1 = new XYZ(points[points.Count - 1].X, points[points.Count - 1].Y, points[points.Count - 1].Z + offset);
                XYZ lastPoint2 = new XYZ(points[0].X, points[0].Y, points[0].Z + offset);

                Curve curveLast = Line.CreateBound(lastPoint1, lastPoint2);
                curveList.Append(curveLast);


                // Create the floor type
                var floorType = GetSuitableSlabType(doc, thickness);
                //}

                // Create the floor slab

                //Element floor = Floor.Create(doc ,curveList, floorType.Id, level.Id);
                Element floor = doc.Create.NewFloor(curveList, floorType, level, false);
                floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(offset);
                //Parameter thicknessParam = floorType.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);

                if (holes.Count != 0)
                {


                    foreach (var opening in holes)
                    {
                        //CreateOpening(doc, holes);
                        //CopyFloor1(floor);
                        CreateSlabOpenings(doc, floor, opening.points, level, offset);
                    }

                }
                // Return the floor slab
                return floor;

                //        return newFloor;

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;

            }
        }
        public static bool CreateSlabOpenings(Document doc, Element floor, List<MapXYZ> holes, Level level, double offset)
        {
            try
            {
                // Get the floor as a Floor element
                Floor floorElement = floor as Floor;
                if (floorElement == null)
                {
                    throw new Exception("The selected element is not a floor.");
                }

                //foreach (var hole in holes)
                //{
                Curve line = null;



                XYZ start = new XYZ(holes[0].X, holes[0].Y, holes[0].Z);
                XYZ end = new XYZ(holes[holes.Count - 1].X, holes[holes.Count - 1].Y, holes[holes.Count - 1].Z);
                XYZ wallDirection = (end - start).Normalize();
                var openingWidth = Math.Abs((end - start).DotProduct(wallDirection));
                // Calculate the location point
                XYZ location = (start + end) / 2;

                line = Line.CreateBound(start, end);

                // Get the thickness parameter of the floor element
                Parameter thicknessParam = floorElement.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);
                if (thicknessParam == null)
                {
                    throw new Exception("The selected floor has no thickness parameter.");
                }

                // Create a dictionary of parameter values for the void family instance
                var parameters = new Dictionary<string, object>
            {
                { "Opening Width", openingWidth },
                { "Opening Length", offset },
                { "Depth", thicknessParam }
            };

                Family fam = GeneralCreator.CheckIfFamilyIsUploaded(doc, "Slab Opening", DB_folder);

                // Check if the void family is already loaded in the project
                FamilySymbol symbol = GeneralCreator.SelectSuitableSymbol(doc, fam, parameters);


                // Get the first family symbol and duplicate it
                if (symbol == null)
                {
                    FamilySymbol firstSymobl = doc.GetElement(fam.GetFamilySymbolIds().First()) as FamilySymbol;
                    symbol = firstSymobl.Duplicate($"Void - {openingWidth}") as FamilySymbol;
                    // Add the 'Opening Width' parameter to the new symbol
                    Parameter myParam = symbol.LookupParameter("Opening Width");
                    if (myParam != null)
                    {
                        myParam.Set(openingWidth);
                    }

                    //symbol.LookupParameter("Opening Width").Set(openingWidth);
                    //symbol.LookupParameter("Opening Length").Set(thicknessParam.AsDouble());
                    //symbol.LookupParameter("Height").Equals(offset);
                }
                if (!symbol.IsActive)
                    symbol.Activate();
                // Create a new instance of the void family and place it on the floor
                FamilyInstance instance = doc.Create.NewFamilyInstance(location, symbol, floorElement, level, StructuralType.NonStructural);
                doc.Regenerate();

                instance.LookupParameter("Opening Width").Set(openingWidth);
                instance.LookupParameter("Depth").Set(thicknessParam.AsDouble());
                // create a reference to the created family instance
                Reference instanceRef = new Reference(instance);


                // Cut the opening in the floor

                // Get the bottom face of the floor

                return true;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return false;
            }
        }
    }
}
#endregion