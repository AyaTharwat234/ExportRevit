using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elibre.eFramer;
using Elibre.Net.Debug;
using Elibre.eFramer.Objects;
using EFRvt;
using EFRvt.Revit;



//using EFRvtNameSpace.Revit;

namespace EFRvt
{
    public static class RevitToEfx
    {
        private const float MinFloorHeight = 9.8F;
        public static efxModelEx GetEfxModel(Document doc)
        {
            var efx_model = new efxModelEx();
            try
            {
                List<Level> sortedlvls = GetSortedLevels(doc);
                efx_model.SectionList = new List<efxSection>();
                efx_model.FramingObjectList = new List<efxObjectEx>();
                efx_model.FloorList = new List<efxFloorEx>();
                efx_model.FloorList = GetAllFloors(doc, sortedlvls);
                #region setopening
                List<Opening> allopening = new FilteredElementCollector(doc).OfClass(typeof(Opening)).Cast<Opening>().Where(x => x.Host is FootPrintRoof).ToList();
                #endregion
                AddAllRoofs(doc, efx_model, allopening);
                AddAllWalls(doc, efx_model, sortedlvls);
                AddTrusses(doc, efx_model);
                AddITWTrusses(doc, efx_model);
                var SingleBeams = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFraming).Cast<FamilyInstance>()
                    .Where(x => x.Location is LocationCurve).ToList();
                AddJoistareAndRafterarea(doc, efx_model, ref SingleBeams);
                AddAllBeams(doc, SingleBeams, efx_model);
                AddAllPosts(doc, efx_model);
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }

            return efx_model;
        }

        private static void AddITWTrusses(Document doc, efxModelEx efx_model)
        {
            try
            {
                List<FamilyInstance> ITWtrusses = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().Where(x => x.Symbol.FamilyName.Contains("ITWTruss")).ToList();
                foreach (var itwtruss in ITWtrusses)
                {

                    AddSingleITWTruss(doc, efx_model, itwtruss);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        private static void AddSingleITWTruss(Document doc, efxModelEx efx_model, FamilyInstance itwtruss)
        {
            try
            {
                // Get to parallel Main Faces
                GeometryElement geomElem = itwtruss.get_Geometry(new Options());

                XYZ center = (itwtruss.Location as LocationPoint).Point;
                List<Face> sidefaces = new List<Face>();
                #region ExtractingThesideFaces


                foreach (GeometryInstance geomInst in geomElem)
                {
                    foreach (GeometryObject geomObj in geomInst.SymbolGeometry)
                    {
                        Solid geomSolid = geomObj as Solid;
                        if (null != geomSolid)
                        {
                            foreach (Face face in geomSolid.Faces)
                            {
                                if (face.EdgeLoops.Size != 1)
                                    continue;

                                EdgeArray arr = face.EdgeLoops.get_Item(0);
                                if (arr.Size != 4)
                                {
                                    sidefaces.Add(face);
                                }
                                else
                                {
                                    double l0 = arr.get_Item(0).ApproximateLength;
                                    double l1 = arr.get_Item(1).ApproximateLength;
                                    double l2 = arr.get_Item(2).ApproximateLength;
                                    double l3 = arr.get_Item(3).ApproximateLength;

                                    if (Math.Abs(l0 - 12) < 0.0001 && Math.Abs(l1 - 13) < 0.0001)
                                    {
                                        sidefaces.Add(face);
                                    }
                                }
                            }

                        }
                    }
                }
                #endregion
                if (sidefaces.Count != 2)
                    return;

                #region MyRegion

                #endregion
                XYZ origin_1;
                XYZ origin_2;
                #region GettingMid profile
                List<XYZ> firstFacepoints = Getedgespointsandorigin(sidefaces[0], out origin_1);
                List<XYZ> secondFacepoints = Getedgespointsandorigin(sidefaces[1], out origin_2);
                double thickness = origin_1.DistanceTo(origin_2);
                List<XYZ> midpoints = new List<XYZ>();
                foreach (var point in firstFacepoints)
                {
                    XYZ secpoint = secondFacepoints.FirstOrDefault(p => Math.Abs(p.DistanceTo(point) - thickness) < 0.0001);
                    midpoints.Add(0.5 * (point + secpoint));
                    secondFacepoints.Remove(secpoint);
                }
                firstFacepoints.Clear(); secondFacepoints.Clear();

                XYZ Farpoint = midpoints.OrderBy(x => x.X).ThenBy(x => x.Y).Last();
                XYZ Direction2D = (new XYZ(Farpoint.X, Farpoint.Y, 0)).Normalize();
                #endregion
                #region projection

                double roationangle = 0;
                if (Math.Abs(Direction2D.X) < 0.00001)
                { roationangle = Math.PI / 2.00; }
                else if (Math.Abs(Direction2D.Y) < 0.0001)
                { roationangle = 0; }
                else
                {
                    roationangle = Math.Acos(Direction2D.X);
                    if (Direction2D.Y < 0)
                    { roationangle *= -1; }
                }

                List<XYZ> TopProfile = new List<XYZ>();
                foreach (var point in midpoints)
                {
                    XYZ _direction2D = (new XYZ(point.X, point.Y, 0)).Normalize();
                    if (_direction2D.DistanceTo(Direction2D) < 0.0001)
                    { TopProfile.Add(new XYZ(Math.Sqrt(Math.Pow(point.X, 2) + Math.Pow(point.Y, 2)), 0, point.Z)); }
                    else
                    { TopProfile.Add(new XYZ(-1 * Math.Sqrt(Math.Pow(point.X, 2) + Math.Pow(point.Y, 2)), 0, point.Z)); }

                }
                midpoints.Clear();
                TopProfile = TopProfile.OrderBy(x => Math.Round(x.X, 5)).ThenBy(x => Math.Round(x.Z, 5)).ToList();
                XYZ end = TopProfile.First();
                XYZ start = TopProfile.Last();
                TopProfile.Remove(end);
                TopProfile.Remove(start);
                TopProfile.RemoveAll(x => x.Z < 0 || Math.Abs(x.Z) < 0.0001);
                TopProfile.Add(end);
                TopProfile.Add(start);
                TopProfile = ClosePolygonVerticalAlgorithm(TopProfile);
                #endregion
                #region TopandBottom 


                XYZ[] bottomProfile = new XYZ[2];
                bottomProfile[0] = Revit.GeometryMathatics.Two_Lines_Intersect_vertical(TopProfile[0], TopProfile[1], new XYZ(0, 0, 0), new XYZ(1, 0, 0));
                bottomProfile[1] = GeometryMathatics.Two_Lines_Intersect_vertical(TopProfile[TopProfile.Count - 1], TopProfile[TopProfile.Count - 2], new XYZ(0, 0, 0), new XYZ(-1, 0, 0));


                TopProfile = ClosePolygonVerticalAlgorithm(TopProfile);
                for (int i = 0; i < TopProfile.Count; i++)
                {
                    TopProfile[i] = new XYZ(TopProfile[i].X * Math.Cos(roationangle), TopProfile[i].X * Math.Sin(roationangle), TopProfile[i].Z);
                }
                for (int i = 0; i < bottomProfile.Length; i++)
                {
                    bottomProfile[i] = new XYZ(bottomProfile[i].X * Math.Cos(roationangle), bottomProfile[i].X * Math.Sin(roationangle), bottomProfile[i].Z);
                }

                if (TopProfile[0].DistanceTo(bottomProfile[0]) > 0.001)
                { TopProfile.Insert(1, bottomProfile[0]); }

                if (TopProfile.Last().DistanceTo(bottomProfile[0]) > 0.001)
                { TopProfile.Insert(TopProfile.Count - 2, bottomProfile[1]); }


                #endregion
                #region Efx
                efxItwTrussEx result = new efxItwTrussEx();

                // result.Floor = efx_model.FloorList.OrderBy(x => Math.Abs(x.Sublevel-center.Z)).FirstOrDefault().Name;
                result.Name = FixDuplicatedObjectsNames(efx_model, result.Floor, typeof(efxItwTrussEx), itwtruss.Name);
                result.Thickness = thickness;
                result.TopChordPoints = new efxPoint3[TopProfile.Count];
                result.BottomChordPoints = new efxPoint3[bottomProfile.Length];
                for (int i = 0; i < TopProfile.Count; i++)
                {
                    XYZ temp = TopProfile[i] + center;
                    result.TopChordPoints[i] = new efxPoint3(temp.X, temp.Y, temp.Z);
                }
                for (int i = 0; i < bottomProfile.Length; i++)
                {
                    XYZ temp = bottomProfile[i] + center;
                    result.BottomChordPoints[i] = new efxPoint3(temp.X, temp.Y, temp.Z);
                }
                result.SupJointList = result.BottomChordPoints;
                TopProfile = GeometryMathatics.GetOnlyBoundaryPoints(TopProfile);
                XYZ normal = (TopProfile[0] - TopProfile[1]).CrossProduct(TopProfile[2] - TopProfile[1]).Normalize();
                result.Normal = new efxPoint3(normal.X, normal.Y, normal.Z);
                efx_model.FramingObjectList.Add(result);
                #endregion
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        private static List<XYZ> Getedgespointsandorigin(Face face, out XYZ origin)
        {
            try
            {
                List<XYZ> result = new List<XYZ>();
                EdgeArray arr = face.EdgeLoops.get_Item(0);
                double dx = 0, dy = 0, dz = 0;
                for (int i = 0; i < arr.Size; i++)
                {
                    var edgecorners = arr.get_Item(i).AsCurve().Tessellate();
                    foreach (var point in edgecorners)
                    {
                        if (null == result.FirstOrDefault(x => x.DistanceTo(point) < 0.00001))
                        {
                            result.Add(point);
                            dx += point.X; dy += point.Y; dz += point.Z;
                        }
                    }
                }

                origin = new XYZ(dx / arr.Size, dy / arr.Size, dz / arr.Size);
                return result;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                origin = null;
                return null;
            }
        }

        private static void AddTrusses(Document doc, efxModelEx efx_model)
        {
            try
            {
                AddCommonTrusses(doc, efx_model);

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        private static void AddCommonTrusses(Document doc, efxModelEx efx_model)
        {
            try
            {
                List<string> familiesNames = new List<string> { "KingPostTruss", "QueenPostTruss", "FinkTruss" ,  "HoweTruss","DoubleFanTruss" ,
                "ModifiedQueenTruss" , "DoubleFinkTruss" , "DoubleHoweTruss" , "TripleFanTruss" , "TripleFinkTruss" , "TripleHoweTruss" };

                List<FamilyInstance> CommonTrusses = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().Where(x => familiesNames.Contains(x.Name)).ToList();
                foreach (var commonTruss in CommonTrusses)
                {
                    try
                    {
                        efxCommonTrussEx ct = new efxCommonTrussEx();
                        #region Set the Type

                        switch (commonTruss.Symbol.Family.Name)
                        {
                            case "KingPostTruss":
                                ct.TrussType = 1;
                                break;
                            case "QueenPostTruss":
                                ct.TrussType = 2;
                                break;
                            case "FinkTruss":
                                ct.TrussType = 3;
                                break;
                            case "HoweTruss":
                                ct.TrussType = 4;
                                break;
                            case "DoubleFanTruss":
                                ct.TrussType = 5;
                                break;
                            case "ModifiedQueenTruss":
                                ct.TrussType = 6;
                                break;
                            case "DoubleFinkTruss":
                                ct.TrussType = 7;
                                break;
                            case "DoubleHoweTruss":
                                ct.TrussType = 8;
                                break;
                            case "TripleFanTruss":
                                ct.TrussType = 9;
                                break;
                            case "TripleFinkTruss":
                                ct.TrussType = 10;
                                break;
                            case "TripleHoweTruss":
                                ct.TrussType = 11;
                                break;

                            default:
                                ct.TrussType = 0;
                                break;
                        }
                        #endregion

                        SetEfxTrussSection(efx_model, commonTruss, ct);

                        #region SetStartEnd

                        LocationPoint location = commonTruss.Location as LocationPoint;
                        Level l = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(x => Math.Abs(x.Elevation - location.Point.Z))
                            .FirstOrDefault(x => efx_model.FloorList.Select(y => y.Name).Contains(x.Name));
                        ct.Floor = l.Name;
                        ct.Name = FixDuplicatedObjectsNames(efx_model, ct.Floor, typeof(efxCommonTrussEx), commonTruss.Name);

                        ct.StartOverHang = (float)commonTruss.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "OverhangRight").AsDouble();
                        ct.EndOverHang = (float)commonTruss.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "OverhangLeft").AsDouble();


                        float Rightspan = (float)commonTruss.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "SpanRight").AsDouble();
                        float Leftspan = (float)commonTruss.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "SpanLeft").AsDouble();

                        XYZ dir = new XYZ(location.Point.X + Math.Cos(location.Rotation), location.Point.Y + Math.Sin(location.Rotation), location.Point.Z);
                        XYZ a = location.Point + (Rightspan + ct.StartOverHang + 0.5) * (dir - location.Point).Normalize();
                        XYZ b = location.Point + (Leftspan + ct.EndOverHang + 0.5) * (location.Point - dir).Normalize();

                        ct.StartPoint = new efxPoint(a.X, a.Y);
                        ct.EndPoint = new efxPoint(b.X, b.Y);
                        #endregion

                        float Rightslope = (float)commonTruss.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "SlopeRight").AsDouble();
                        float Leftslope = (float)commonTruss.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "SlopeLeft").AsDouble();
                        float topdepth = (float)efx_model.SectionList.FirstOrDefault(x => x.Name == ct.TopChordSection).DepthInch / 12;
                        a = location.Point + (Rightspan + ct.StartOverHang) * (dir - location.Point).Normalize();
                        b = location.Point + (Leftspan + ct.EndOverHang) * (location.Point - dir).Normalize();
                        a = new XYZ(a.X, a.Y, a.Z - ct.StartOverHang * Math.Tan(Rightslope) + topdepth / Math.Cos(Rightslope));
                        b = new XYZ(b.X, b.Y, b.Z - ct.EndOverHang * Math.Tan(Leftslope) + topdepth / Math.Cos(Leftslope));
                        XYZ top = new XYZ(location.Point.X, location.Point.Y, a.Z);
                        top = new XYZ(location.Point.X, location.Point.Y, a.Z + (top.DistanceTo(a) * Math.Tan(Rightslope)));

                        //XYZ top = GeometryMathatics.Two_Lines_Intersect(a,new XYZ(a.X+Math.Cos(Rightslope) , a.Y + Math.Cos(Rightslope) , a.Z+Math.Sin(Rightslope))
                        //    ,b,new XYZ(b.X+Math.Cos(Leftslope),b.Y+Math.Cos(Leftslope),b.Z+Math.Sin(Leftslope)));
                        ct.StartPoint3D = new efxPoint3(a.X, a.Y, a.Z);
                        ct.EndPoint3D = new efxPoint3(b.X, b.Y, b.Z);
                        ct.topPoint3D = new efxPoint3(top.X, top.Y, top.Z);
                        efx_model.FramingObjectList.Add(ct);
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                }


            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        private static void SetEfxTrussSection(efxModelEx efx_model, FamilyInstance Truss, efxTrussEx efxtruss)
        {
            try
            {
                #region Sections

                #region TopSection
                efxSection Topsection = new efxSection();
                float w = (float)Truss.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "TopWidth").AsDouble() * 12;
                float d = (float)Truss.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "TopDepth").AsDouble() * 12;
                Topsection = efx_model.SectionList.FirstOrDefault(x => Math.Abs(x.WidthInch - w) < 0.0001 && Math.Abs(x.DepthInch - d) < 0.0001);
                if (Topsection == null)
                {
                    Topsection = AddNewRevitToModel(efx_model, w, d);
                }
                efxtruss.TopChordSection = Topsection.Name;
                #endregion

                #region Bottomsection

                efxSection bottomsection = new efxSection();
                w = (float)Truss.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "BottomWidth").AsDouble() * 12;
                d = (float)Truss.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "BottomDepth").AsDouble() * 12;
                bottomsection = efx_model.SectionList.FirstOrDefault(x => Math.Abs(x.WidthInch - w) < 0.0001 && Math.Abs(x.DepthInch - d) < 0.0001);
                if (bottomsection == null)
                {
                    bottomsection = AddNewRevitToModel(efx_model, w, d);
                }
                efxtruss.BottomChordSection = bottomsection.Name;

                #endregion

                #region WebSection
                efxSection Websection = new efxSection();
                w = (float)Truss.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "WebWidth").AsDouble() * 12;
                d = (float)Truss.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "WebDepth").AsDouble() * 12;
                Websection = efx_model.SectionList.FirstOrDefault(x => Math.Abs(x.WidthInch - w) < 0.0001 && Math.Abs(x.DepthInch - d) < 0.0001);
                if (Websection == null)
                {
                    Websection = AddNewRevitToModel(efx_model, w, d);
                }
                efxtruss.WebSection = Websection.Name;
                #endregion

                #endregion
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }

        }

        private static void AddAllRoofs(Document doc, efxModelEx efx_model, List<Opening> AllOpening)
        {
            try
            {
                IEnumerable<FootPrintRoof> roofs = new FilteredElementCollector(doc).OfClass(typeof(FootPrintRoof)).Cast<FootPrintRoof>();
                foreach (var roof in roofs)
                {
                    AddSingleRoof(roof, doc, efx_model, AllOpening);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        private static void AddSingleRoof(FootPrintRoof roof, Document doc, efxModelEx efx_model, List<Opening> AllOpening)
        {
            try
            {
                efxRoofEx result = new efxRoofEx();


                Level l = doc.GetElement(roof.get_Parameter(BuiltInParameter.ROOF_BASE_LEVEL_PARAM).AsElementId()) as Level;
                efxFloorEx rooffloor = efx_model.FloorList.FirstOrDefault(y => y.Name == l.Name);
                result.Floor = rooffloor.Name;
                double BaseOffset = roof.get_Parameter(BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM).AsDouble();
                result.Name = FixDuplicatedObjectsNames(efx_model, result.Floor, typeof(efxRoofEx), roof.Name);

                // AddRoofPlanes(result,roof);
                AddRoofFootprintEdges(result, roof, rooffloor, BaseOffset, l.Elevation);

                List<Opening> Myopenings = AllOpening.Where(x => x.Host.Id == roof.Id).ToList();

                Myopenings.ForEach(x => AllOpening.Remove(x));
                Addopening(efx_model, result, Myopenings);
                efx_model.FramingObjectList.Add(result);
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        private static void Addopening(efxModelEx efx_model, efxRoofEx result, List<Opening> allOpening)
        {
            try
            {
                foreach (var opening in allOpening)
                {
                    efxRoofOpeningEx newopening = new efxRoofOpeningEx();
                    newopening.Name = FixDuplicatedObjectsNames(efx_model, result.Floor, typeof(efxRoofOpeningEx), opening.Name);
                    newopening.Floor = result.Floor;
                    newopening.Roof = result.Name;
                    newopening.VertexList = new List<efxPoint>();
                    for (int i = 0; i < opening.BoundaryCurves.Size; i++)
                    {
                        XYZ startpoint = opening.BoundaryCurves.get_Item(i).Tessellate()[0];
                        newopening.VertexList.Add(new efxPoint(startpoint.X, startpoint.Y));
                    }
                    efx_model.FramingObjectList.Add(newopening);
                    result.RoofOpenings.Add(newopening.Name);

                }
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        private static void AddRoofFootprintEdges(efxRoofEx efxroof, FootPrintRoof roof, efxFloorEx rooffloor, double BaseOffset, double levelElvation)
        {
            try
            {
                efxroof.FootprintEdges = new List<efxFootprintEdgeEx>();

                //List<efxRoofPlane> copyList = new List<efxRoofPlane>();
                //copyList.AddRange(efxroof.Planes);

                ModelCurveArrArray arrarray = roof.GetProfiles();

                for (int i = 0; i < arrarray.Size; i++)
                {
                    ModelCurveArray array = arrarray.get_Item(i);

                    for (int j = 0; j < array.Size; j++)
                    {
                        ModelCurve curve = array.get_Item(j);
                        efxFootprintEdgeEx el = new efxFootprintEdgeEx();
                        el.Roof = efxroof.Name;
                        el.IsGable = !roof.get_DefinesSlope(curve);

                        try
                        {
                            el.OverHanging = (float)roof.get_Overhang(curve);
                        }
                        catch (Exception)
                        {
                            el.OverHanging = 0;
                        }

                        //Start And End Points
                        SetFootprintEdgeStartandEndPoints(el, curve);
                        double curveOffset = 0;

                        try
                        {
                            curveOffset = curve.get_Parameter(BuiltInParameter.ROOF_CURVE_HEIGHT_OFFSET).AsDouble();
                        }
                        catch { }

                        if (el.IsGable)
                        {
                            el.Pitch = 8;
                            el.HeelHeight = (float)(BaseOffset + curveOffset + rooffloor.FramingThickness + rooffloor.SheathingThickness);
                        }
                        else
                        {
                            el.HeelHeight = (float)((BaseOffset + curveOffset + rooffloor.FramingThickness + rooffloor.SheathingThickness)
                                + el.OverHanging * Math.Tan(roof.get_SlopeAngle(curve)));
                            el.Pitch = GeometryMathatics.AngleToPitch(roof.get_SlopeAngle(curve));
                            // AssignFootprintEdgeToFace(el , copyList , rooffloor , levelElvation);
                        }

                        efxroof.FootprintEdges.Add(el);
                    }
                }

                trimEndsOfFootprintEdges(efxroof);
                //

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        public static void trimEndsOfFootprintEdges(efxRoofEx efxroof)
        {
            try
            {
                efxroof.FootprintEdges.Add(efxroof.FootprintEdges[0]);
                List<efxPoint> newBasepolygon = new List<efxPoint>();

                for (int i = 0; i < efxroof.FootprintEdges.Count - 1; i++)
                {
                    newBasepolygon.Add(GeometryMathatics.Two_Lines_Intersect(efxroof.FootprintEdges[i].startpoint, efxroof.FootprintEdges[i].endpoint
                        , efxroof.FootprintEdges[i + 1].startpoint, efxroof.FootprintEdges[i + 1].endpoint));

                }
                for (int i = 0; i < efxroof.FootprintEdges.Count - 1; i++)
                {

                    efxroof.FootprintEdges[i].endpoint = efxroof.FootprintEdges[i + 1].startpoint = newBasepolygon[i];
                }
                efxroof.FootprintEdges[0].startpoint = efxroof.FootprintEdges.Last().startpoint;
                efxroof.FootprintEdges.Remove(efxroof.FootprintEdges.Last());
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        private static void AssignFootprintEdgeToFace(efxFootprintEdgeEx el, List<efxRoofPlaneEx> planeslist, efxFloorEx rooffloor, double levelElvation)
        {
            efxRoofPlaneEx result = planeslist.OrderBy(x => DistanceFootprintEdgeToFace(el, x)).FirstOrDefault();
            result.FootprintEdge = el;
            el.HeelHeight = (float)(levelElvation - result.Vertexlist.Select(X => X.Z).Min() + rooffloor.FramingThickness + rooffloor.SheathingThickness);
            planeslist.Remove(result);
        }

        private static double DistanceFootprintEdgeToFace(efxFootprintEdgeEx el, efxRoofPlaneEx plane)
        {
            try
            {
                efxPoint efxElMidPoint = new efxPoint((el.startpoint.X + el.endpoint.X) / 2.00, (el.endpoint.Y + el.startpoint.Y) / 2.00);
                efxPoint PlaneMidPoint = new efxPoint();
                for (int i = 0; i < plane.Vertexlist.Length; i++)
                {
                    PlaneMidPoint.X += plane.Vertexlist[i].X;
                    PlaneMidPoint.Y += plane.Vertexlist[i].Y;
                }
                PlaneMidPoint.X /= plane.Vertexlist.Length;
                PlaneMidPoint.Y /= plane.Vertexlist.Length;

                return Math.Sqrt(Math.Pow(efxElMidPoint.X - PlaneMidPoint.X, 2) + Math.Pow(efxElMidPoint.Y - PlaneMidPoint.Y, 2));

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return 0;
            }
        }

        private static void SetFootprintEdgeStartandEndPoints(efxFootprintEdgeEx el, ModelCurve curve)
        {
            try
            {
                XYZ start = curve.GeometryCurve.Tessellate()[0];
                XYZ end = curve.GeometryCurve.Tessellate()[1];
                XYZ direcrion = el.OverHanging * ((end - start).CrossProduct(new XYZ(0, 0, -1)).Normalize());
                start = start + direcrion;
                end = end + direcrion;

                el.startpoint = new efxPoint(start.X, start.Y);
                el.endpoint = new efxPoint(end.X, end.Y);

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        private static void AddRoofPlanes(efxRoofEx efxroof, FootPrintRoof roof)
        {
            try
            {
                efxroof.Planes = new List<efxRoofPlaneEx>();
                List<Reference> refs = HostObjectUtils.GetBottomFaces(roof).ToList();

                for (int i = 0; i < refs.Count; i++)
                {
                    Face face = roof.GetGeometryObjectFromReference(refs[i]) as Face;
                    EdgeArray edges = face.EdgeLoops.get_Item(0);

                    efxRoofPlaneEx plane = new efxRoofPlaneEx();

                    plane.Vertexlist = new efxPoint3[edges.Size];
                    plane.Roof = efxroof.Name;
                    plane.Floor = efxroof.Floor;
                    plane.Name = efxroof.Name + " F" + (i + 1);

                    for (int j = 0; j < edges.Size; j++)
                    {
                        List<XYZ> result = edges.get_Item(j).Tessellate().ToList();
                        bool flag = true;

                        for (int k = 0; k < j; k++)
                        {
                            if (Math.Abs(plane.Vertexlist[k].X - result.First().X) < 0.0001 &&
                                 Math.Abs(plane.Vertexlist[k].Y - result.First().Y) < 0.0001 &&
                                 Math.Abs(plane.Vertexlist[k].Z - result.First().Z) < 0.0001)
                            {
                                flag = false;
                                break;
                            }
                        }

                        if (flag)
                        {
                            plane.Vertexlist[j] = new efxPoint3(result.First().X, result.First().Y, result.First().Z);
                        }
                        else
                        {
                            plane.Vertexlist[j] = new efxPoint3(result.Last().X, result.Last().Y, result.Last().Z);
                        }
                    }

                    plane.Vertexlist = GeometryMathatics.CCWSort(plane.Vertexlist.ToList()).ToArray();
                    efxroof.Planes.Add(plane);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        private static void AddJoistareAndRafterarea(Document doc, efxModelEx efx_model, ref List<FamilyInstance> singleBeams)
        {
            try
            {
                List<Group> Groups = new FilteredElementCollector(doc).OfClass(typeof(Group)).OfCategory(BuiltInCategory.OST_IOSModelGroups).Cast<Group>().ToList();
                List<Group> Copygroups = new List<Group>();
                Copygroups.AddRange(Groups);
                foreach (var group in Copygroups)
                {
                    try
                    {
                        AddSingleJoistArea(group, doc, efx_model, ref singleBeams, ref Groups);
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }



                }
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }
        private static void AddSingleJoistArea(Group group, Document doc, efxModelEx efx_model, ref List<FamilyInstance> singleBeams, ref List<Group> groups)
        {
            try
            {
                efxJoistAreaEx efxarea = null;

                List<ElementId> ids = group.GetMemberIds().ToList();
                List<FamilyInstance> Joists = new List<FamilyInstance>();
                List<ModelCurve> Lines = new List<ModelCurve>();
                List<FamilyInstance> openings = new List<FamilyInstance>();

                #region Filteration

                foreach (var id in ids)
                {
                    try
                    {
                        Element e = doc.GetElement(id);
                        if (e is ModelCurve)
                        {
                            Lines.Add(e as ModelCurve);
                        }
                        if (e is FamilyInstance && e.Category.Name == "Structural Framing")
                        {
                            if ((e as FamilyInstance).Location is LocationPoint)
                            {
                                if ((e as FamilyInstance).Symbol.FamilyName == "EFRafter")
                                {
                                    Joists.Add(e as FamilyInstance);
                                }

                            }
                            else
                            {
                                Joists.Add(e as FamilyInstance);
                            }
                        }
                        if (e is FamilyInstance && e.Category.Name == "Generic Models")
                        {
                            if ((e as FamilyInstance).Symbol.FamilyName.Contains("FramingOpening") && InstanceVoidCutUtils.IsVoidInstanceCuttingElement(e))
                            { openings.Add(e as FamilyInstance); }
                        }
                    }
                    catch { continue; }
                }
                #endregion

                if (Joists.Count == 0)
                    return;

                XYZ MainDirection = null;

                #region MainDirection

                for (int i = 0; i < Joists.Count - 1; i++)
                {
                    XYZ dir1 = GetJoistOrRafter2DDirection(Joists[i]);
                    XYZ dir2 = GetJoistOrRafter2DDirection(Joists[i + 1]);
                    if (GeometryMathatics.IsTwoDirectionParallel(dir1, dir2))
                    {
                        MainDirection = dir1;
                        break;
                    }
                }
                if (MainDirection == null)
                    return;

                Joists.RemoveAll(x => !GeometryMathatics.IsTwoDirectionParallel(MainDirection, GetJoistOrRafter2DDirection(x)));
                List<int> Ids = Joists.Select(x => x.Id.IntegerValue).ToList();
                singleBeams.RemoveAll(x => Ids.Contains(x.Id.IntegerValue));
                #endregion

                if (MainDirection.Y < 0)
                {
                    MainDirection = new XYZ(0 - 1 * MainDirection.X, 0 - 1 * MainDirection.Y, 0);
                }
                else if (Math.Abs(MainDirection.Y) < 0.001 && MainDirection.X < 0)
                {
                    MainDirection = new XYZ(1, 0, 0);
                }
                else
                {
                    MainDirection = new XYZ(MainDirection.X, MainDirection.Y, 0);
                }

                XYZ PrendicularMainDirection = MainDirection.CrossProduct(new XYZ(0, 0, 1));
                List<XYZ> Boundarypoints = new List<XYZ>();

                #region Boundarypoints

                foreach (var line in Lines)
                {
                    XYZ[] arr = line.GeometryCurve.Tessellate().ToArray();
                    XYZ a = Boundarypoints.FirstOrDefault(r => GeometryMathatics.IsTwoPointsEquals(r, arr.First()));
                    XYZ B = Boundarypoints.FirstOrDefault(r => GeometryMathatics.IsTwoPointsEquals(r, arr.Last()));
                    if (a == null)
                    { Boundarypoints.Add(arr.First()); }
                    if (B == null)
                    { Boundarypoints.Add(arr.Last()); }
                }
                foreach (var joist in Joists)
                {
                    if (joist.Symbol.FamilyName == "EFRafter")
                    {
                        LocationPoint location = joist.Location as LocationPoint;
                        double slope = joist.Parameters.Cast<Parameter>().First(e => e.Definition.Name == "Slope").AsDouble();
                        double span = joist.Parameters.Cast<Parameter>().First(e => e.Definition.Name == "Span").AsDouble();
                        double height = Math.Tan(slope) * 0.5 * span;
                        XYZ top = location.Point + 0.5 * span * MainDirection + new XYZ(0, 0, height);
                        XYZ bottom = location.Point - 0.5 * span * MainDirection - new XYZ(0, 0, height);
                        XYZ a = Boundarypoints.FirstOrDefault(q => GeometryMathatics.IsTwoPointsEquals(q, top));
                        XYZ B = Boundarypoints.FirstOrDefault(q => GeometryMathatics.IsTwoPointsEquals(q, bottom));
                        if (a == null)
                        { Boundarypoints.Add(top); }
                        if (B == null)
                        { Boundarypoints.Add(bottom); }

                    }
                    else
                    {
                        XYZ[] arr = (joist.Location as LocationCurve).Curve.Tessellate().ToArray();
                        XYZ a = Boundarypoints.FirstOrDefault(p => GeometryMathatics.IsTwoPointsEquals(p, arr.First()));
                        XYZ B = Boundarypoints.FirstOrDefault(p => GeometryMathatics.IsTwoPointsEquals(p, arr.Last()));
                        if (a == null)
                        { Boundarypoints.Add(arr.First()); }
                        if (B == null)
                        { Boundarypoints.Add(arr.Last()); }
                    }
                }
                #endregion

                Boundarypoints = ClosePolygonAlgorithm(Boundarypoints);
                Boundarypoints = GeometryMathatics.GetOnlyBoundaryPoints(Boundarypoints);

                float minZ = (float)Boundarypoints.Select(x => x.Z).Min();
                float maxZ = (float)Boundarypoints.Select(x => x.Z).Max();

                if (Math.Abs(minZ - maxZ) < 0.0001)
                {
                    efxarea = new efxJoistAreaEx();
                }
                else
                {
                    efxarea = new efxRafterAreaEx();

                }

                Level l = doc.GetElement(group.get_Parameter(BuiltInParameter.GROUP_LEVEL).AsElementId()) as Level;
                // efxarea.Floor = efx_model.FloorList.Where(x => !x.Foundation).OrderBy(x => x.Sublevel - l.Elevation).First().Name;
                efxarea.Name = FixDuplicatedObjectsNames(efx_model, efxarea.Floor, efxarea.GetType(), group.GroupType.Name);
                efxarea.JoistAngle = (float)(Math.Acos(MainDirection.DotProduct(new XYZ(1, 0, 0))) * 180 / Math.PI);

                #region SetCorner
                efxarea.vertexlist = new efxPoint[Boundarypoints.Count];
                for (int i = 0; i < Boundarypoints.Count; i++)
                {
                    efxarea.vertexlist[i] = new efxPoint(Boundarypoints[i].X, Boundarypoints[i].Y);
                }

                efxPoint[] CopyVertexList = (efxPoint[])efxarea.vertexlist.Clone();
                if ((Math.Abs(efxarea.JoistAngle) < 0.001) || (efxarea.JoistAngle > 0 && efxarea.JoistAngle <= 45))
                {
                    CopyVertexList = CopyVertexList.OrderBy(x => x.Y).ThenByDescending(x => x.X).ToArray();
                }
                else if ((Math.Abs(efxarea.JoistAngle - 90) < 0.001) || (efxarea.JoistAngle > 45 && efxarea.JoistAngle < 90))
                {
                    CopyVertexList = CopyVertexList.OrderBy(x => x.X).ThenByDescending(x => x.Y).ToArray();
                }
                else if (efxarea.JoistAngle > 90 && efxarea.JoistAngle < 180)
                {
                    CopyVertexList = CopyVertexList.OrderBy(x => x.Y).ThenBy(x => x.X).ToArray();
                }
                #endregion

                //2D
                efxPoint min_corner2D = CopyVertexList.First();
                efxPoint max_corner2D = CopyVertexList.OrderByDescending(x => Math.Sqrt(Math.Pow(min_corner2D.X - x.X, 2) + Math.Pow(min_corner2D.Y - x.Y, 2))).FirstOrDefault();
                XYZ min_corner = new XYZ(min_corner2D.X, min_corner2D.Y, 0);
                XYZ max_corner = new XYZ(max_corner2D.X, max_corner2D.Y, 0);

                FamilySymbol Fs = Joists.GroupBy(x => x.Symbol).OrderBy(x => x.Count()).First().Key;

                #region Setsection
                efxSection section = new efxSection();
                float b, d;
                if (Fs.Family.Name == "EFBeam" || Fs.Family.Name == "EFRafter")
                {
                    b = (float)Fs.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "b").AsDouble() * 12;
                    d = (float)Fs.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "d").AsDouble() * 12;
                }
                else
                {
                    Parameter WidthParmeter = Fs.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_WIDTH) ??
                        Fs.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM) ??
                        Fs.get_Parameter(BuiltInParameter.GENERIC_WIDTH);
                    Parameter DepthParmeter = Fs.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_HEIGHT) ??
                        Fs.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM) ??
                        Fs.get_Parameter(BuiltInParameter.GENERIC_HEIGHT) ??
                        Fs.get_Parameter(BuiltInParameter.GENERIC_DEPTH);

                    b = (float)WidthParmeter.AsDouble() * 12;
                    d = (float)DepthParmeter.AsDouble() * 12;
                }
                section = efx_model.SectionList.FirstOrDefault(x => Math.Abs(x.WidthInch - b) < 0.0001 && Math.Abs(x.DepthInch - d) < 0.0001);
                if (section == null)
                {
                    section = AddNewRevitToModel(efx_model, b, d);
                }
                efxarea.Section = section.Name;
                #endregion

                #region Spacing


                max_corner = GeometryMathatics.Two_Lines_Intersect(min_corner, min_corner + PrendicularMainDirection
                , max_corner, max_corner + MainDirection);

                Dictionary<FamilyInstance, double> Joist_Position = new Dictionary<FamilyInstance, double>();

                foreach (var joist in Joists)
                {
                    try
                    {
                        XYZ location = null;
                        if (joist.Location is LocationPoint)
                        {
                            XYZ origin = (joist.Location as LocationPoint).Point;
                            location = new XYZ(origin.X, origin.Y, 0);
                        }
                        else if (joist.Location is LocationCurve)
                        {
                            XYZ origin = (joist.Location as LocationCurve).Curve.Tessellate()[0];
                            location = new XYZ(origin.X, origin.Y, 0);
                        }

                        double position = min_corner.DistanceTo(
                            GeometryMathatics.Two_Lines_Intersect(min_corner, min_corner + PrendicularMainDirection
                        , location, location + MainDirection));
                        Joist_Position.Add(joist, position);
                    }
                    catch
                    { continue; }
                }
                Joist_Position = Joist_Position.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

                efxarea.FirstJoistOffset = (float)Joist_Position.First().Value;
                efxarea.LastJoistOffset = (float)(min_corner.DistanceTo(max_corner) - Joist_Position.Last().Value);

                List<double> possibleSpacing1 = new List<double>();
                List<double> possibleSpacing2 = new List<double>();
                List<double> allpositions = Joist_Position.Values.ToList();
                Joists = Joist_Position.Keys.ToList();

                for (int j = 1; j < allpositions.Count; j++)
                {
                    possibleSpacing1.Add(Math.Round(allpositions[j] - allpositions[0], 3));
                }

                for (int i = 1; i < allpositions.Count; i++)
                {
                    for (int j = i + 1; j < allpositions.Count; j++)
                    {
                        possibleSpacing2.Add(Math.Round(allpositions[j] - allpositions[i], 3));
                    }
                }

                try
                {
                    efxarea.JoistsSpacing = (float)possibleSpacing2.GroupBy(x => x).OrderByDescending(x => x.Count()).FirstOrDefault(x => possibleSpacing1.Contains(x.Key)).Key;
                }
                catch
                {
                    efxarea.JoistsSpacing = (float)possibleSpacing1.Last();
                }

                efxarea.JoistPositionsUserAdded = new List<double>();
                for (int i = 0; i < allpositions.Count; i++)
                {
                    for (int j = i + 1; j < allpositions.Count; j++)
                    {
                        if (Math.Abs(Math.Round(allpositions[j] - allpositions[i], 3) - efxarea.JoistsSpacing) < 0.0001)
                        {
                            i = j;
                            break;
                        }
                        else
                        {
                            FamilySymbol jFs = Joists[j].Symbol;
                            double sectionwidth;
                            if (Fs.Family.Name == "EFBeam" || Fs.Family.Name == "EFRafter")
                            {
                                sectionwidth = (float)Fs.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "b").AsDouble() * 12;
                            }
                            else
                            {
                                try
                                {
                                    Parameter WidthParmeter = Fs.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_WIDTH) ??
                                                                        Fs.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM) ??
                                                                        Fs.get_Parameter(BuiltInParameter.GENERIC_WIDTH);
                                    sectionwidth = (float)WidthParmeter.AsDouble() * 12;
                                }
                                catch (Exception)
                                {

                                    throw;
                                }


                            }

                            if (null == allpositions.FirstOrDefault(pos => Math.Abs(pos - allpositions[j]) < (sectionwidth / 12 + 0.001)))
                            { efxarea.JoistPositionsUserAdded.Add(allpositions[j] / (min_corner.DistanceTo(max_corner))); }

                        }
                    }
                }
                #endregion

                efxRoofEx efxroof = null;

                if (efxarea is efxRafterAreaEx)
                {
                    // ((efxRafterArea)efxarea).FloorOffset = minZ - (float)efx_model.FloorList.FirstOrDefault(x => x.Name == efxarea.Floor).Sublevel;

                    double dx = 0, dy = 0, dz = 0;

                    for (int i = 0; i < efxarea.vertexlist.Length; i++)
                    {
                        dx += efxarea.vertexlist[i].X; dy += efxarea.vertexlist[i].Y;
                    }

                    XYZ areamidpoint = new XYZ(dx / efxarea.vertexlist.Length, dy / efxarea.vertexlist.Length, 0);
                    efxroof = efx_model.FramingObjectList.Where(x => x.Floor == efxarea.Floor && x.GetType() == typeof(efxRoofEx)).Cast<efxRoofEx>().FirstOrDefault(x => IsRoofBoundaringRafterArea(efx_model, areamidpoint, x));

                    Dictionary<efxFootprintEdgeEx, double> possibleFootprintEdge_distances = new Dictionary<efxFootprintEdgeEx, double>();
                    ((efxRafterAreaEx)efxarea).FootprintEdge = null;

                    foreach (var el in efxroof.FootprintEdges)
                    {
                        XYZ linestart = new XYZ(el.startpoint.X, el.startpoint.Y, 0); ;
                        XYZ lineEnd = new XYZ(el.endpoint.X, el.endpoint.Y, 0);

                        XYZ fpEdgeNorm = (lineEnd - linestart).CrossProduct(new XYZ(0, 0, -1)).Normalize();
                        XYZ areaNorm = (GeometryMathatics.Drop_Prependicular_From(areamidpoint, linestart, lineEnd) - areamidpoint).Normalize();
                        double dot = areaNorm.DotProduct(fpEdgeNorm);

                        if (dot < 0 && Math.Abs(dot + 1) < 0.001)
                        {
                            possibleFootprintEdge_distances.Add(el, GeometryMathatics.distancefrompointToline(areamidpoint, linestart, lineEnd));
                        }
                    }


                    if (possibleFootprintEdge_distances.Count == 0)
                        return;

                    ((efxRafterAreaEx)efxarea).RoofName = efxroof.Name;
                    ((efxRafterAreaEx)efxarea).FootprintEdge = possibleFootprintEdge_distances.OrderBy(x => x.Value).FirstOrDefault().Key;
                }

                foreach (var opening in openings)
                {
                    try
                    {
                        Options o = new Options();
                        o.IncludeNonVisibleObjects = true;
                        Solid geomElem = opening.get_Geometry(o).FirstOrDefault(x => x is Solid) as Solid;
                        Face HorizontalFace = null;
                        //geomElem.Faces.fir

                        FaceArrayIterator iterator = geomElem.Faces.ForwardIterator();
                        iterator.Reset();

                        while (iterator.MoveNext())
                        {
                            Face current = iterator.Current as Face;
                            XYZ normal = current.ComputeNormal(new UV(0, 0)).Normalize();
                            double d1 = normal.DistanceTo(XYZ.BasisZ);
                            double d2 = normal.DistanceTo(-1 * XYZ.BasisZ);

                            if (Math.Abs(d1) < 0.0001 || Math.Abs(d2) < 0.0001)
                            {
                                HorizontalFace = current;
                                break;
                            }
                        }

                        List<efxPoint> points = new List<efxPoint>();

                        if (HorizontalFace.EdgeLoops.Size != 1)
                            continue;

                        EdgeArray arr = HorizontalFace.EdgeLoops.get_Item(0);

                        for (int i = 0; i < arr.Size; i++)
                        {
                            XYZ point = arr.get_Item(i).Tessellate()[0];
                            points.Add(new efxPoint(point.X, point.Y));
                        }

                        efxFramingAreaOpeningEx efxopening;

                        if (efxarea is efxRafterAreaEx)
                        {
                            efxopening = new efxRoofOpeningEx();
                            efxopening.Name = FixDuplicatedObjectsNames(efx_model, efxarea.Floor, typeof(efxRoofOpeningEx), opening.Name);
                            (efxopening as efxRoofOpeningEx).Roof = (efxarea as efxRafterAreaEx).RoofName;
                            (efxopening as efxRoofOpeningEx).RafterAreaName = (efxarea as efxRafterAreaEx).Name;
                            if (efxroof != null)
                            { efxroof.RoofOpenings.Add(efxopening.Name); }

                        }
                        else
                        {
                            efxopening = new efxFramingAreaOpeningEx();
                            efxopening.Name = FixDuplicatedObjectsNames(efx_model, efxarea.Floor, typeof(efxFramingAreaOpeningEx), opening.Name);
                        }

                        efxopening.Floor = efxarea.Floor;
                        efxopening.VertexList = points;
                        efxarea.OpeningList.Add(efxopening.Name);
                        efx_model.FramingObjectList.Add(efxopening);

                    }
                    catch (Exception)
                    {

                        continue;
                    }
                }

                efx_model.FramingObjectList.Add(efxarea);
                groups.Remove(groups.FirstOrDefault(x => x.Id.IntegerValue == group.Id.IntegerValue));
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        private static List<XYZ> ClosePolygonAlgorithm(List<XYZ> boundarypoints)
        {
            boundarypoints = boundarypoints.OrderBy(x => Math.Round(x.X, 4)).ThenBy(x => Math.Round(x.Y, 4)).ToList();
            XYZ p = boundarypoints.FirstOrDefault();
            boundarypoints.Remove(p);
            XYZ q = boundarypoints.LastOrDefault();
            boundarypoints.Remove(q);

            List<XYZ> A = new List<XYZ>();
            List<XYZ> B = new List<XYZ>();

            foreach (var point in boundarypoints)
            {
                if (!GeometryMathatics.IsPointinLine(point, p, q))
                {
                    XYZ pre = GeometryMathatics.Drop_Prependicular_From(point, p, q);
                    if (pre.Y > point.Y)
                    { B.Add(point); }
                    else
                    { A.Add(point); }
                }
            }
            B = B.OrderBy(x => Math.Round(x.X, 4)).ThenBy(x => Math.Round(x.Y, 4)).ToList();
            A = A.OrderByDescending(x => Math.Round(x.X, 4)).ThenByDescending(x => Math.Round(x.Y, 4)).ToList();
            List<XYZ> result = new List<XYZ>();
            result.Add(p);
            result.AddRange(B);
            result.Add(q);
            result.AddRange(A);
            return result;
        }

        private static List<XYZ> ClosePolygonVerticalAlgorithm(List<XYZ> boundarypoints)
        {
            boundarypoints = boundarypoints.OrderBy(x => Math.Round(x.X, 5)).ThenBy(x => Math.Round(x.Z, 5)).ToList();
            XYZ p = boundarypoints.FirstOrDefault();
            boundarypoints.Remove(p);
            XYZ q = boundarypoints.LastOrDefault();
            boundarypoints.Remove(q);

            List<XYZ> A = new List<XYZ>();
            List<XYZ> B = new List<XYZ>();

            foreach (var point in boundarypoints)
            {
                if (!GeometryMathatics.IsPointinLine(point, p, q))
                {
                    XYZ pre = GeometryMathatics.Drop_Prependicular_From(point, p, q);
                    if (pre.Z > point.Z)
                    { B.Add(point); }
                    else
                    { A.Add(point); }
                }
            }
            B = B.OrderBy(x => Math.Round(x.X, 5)).ThenBy(x => Math.Round(x.Z, 5)).ToList();
            A = A.OrderByDescending(x => Math.Round(x.X, 5)).ThenByDescending(x => Math.Round(x.Z, 5)).ToList();
            List<XYZ> result = new List<XYZ>();
            result.Add(q);
            result.AddRange(A);
            result.Add(p);
            result.AddRange(B);
            return result;
        }
        private static XYZ GetJoistOrRafter2DDirection(FamilyInstance joist)
        {
            try
            {
                if (joist.Symbol.FamilyName == "EFRafter")
                {
                    LocationPoint location = joist.Location as LocationPoint;
                    double slope = joist.Parameters.Cast<Parameter>().First(e => e.Definition.Name == "Slope").AsDouble();
                    return (new XYZ(Math.Cos(location.Rotation), Math.Sin(location.Rotation), 0));
                    //return (xydir + new XYZ(0, 0, Math.Sin(slope))).Normalize();
                }
                else
                {
                    XYZ[] arr = (joist.Location as LocationCurve).Curve.Tessellate().ToArray();
                    return (arr.Last() - arr.First()).Normalize();
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static void AddAllWalls(Document doc, efxModelEx efx_model, List<Level> sortedLvls)
        {
            try
            {
                var result = new List<efxStraightWallEx>();
                foreach (var x in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
                {
                    addSingleWall(doc, efx_model, sortedLvls, x as Wall);
                }

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);

            }
        }

        private static void addSingleWall(Document doc, efxModelEx efx_model, List<Level> sortedLvls, Wall w)
        {
            try
            {
                if (w != null)
                {
                    var c = w.Location as LocationCurve;
                    if (c != null)
                    {
                        #region Creation

                        var efxWall = new efxStraightWallEx();

                        // efxWall.WallType = 0;
                        efxWall.SheathingLine = efxWall.CenterLine = new efxLine(new efxPoint(c.Curve.GetEndPoint(0).X, c.Curve.GetEndPoint(0).Y), new efxPoint(c.Curve.GetEndPoint(1).X, c.Curve.GetEndPoint(1).Y));
                        if ((doc.GetElement(w.LevelId) as Level).Name == sortedLvls.First().Name)
                        { efxWall.OnFoundation = true; }
                        else
                        { efxWall.OnFoundation = false; }

                        #endregion

                        #region Assign to level
                        Level toplevel;
                        Parameter Height = w.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                        if (Height.IsReadOnly)
                        {
                            Parameter toplvlID = w.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
                            toplevel = doc.GetElement(toplvlID.AsElementId()) as Level;
                        }
                        else
                        {
                            toplevel = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => sortedLvls.Last().Elevation + Height.AsDouble() - l.Elevation).FirstOrDefault();
                        }
                        efxWall.Floor = toplevel.Name;
                        efxWall.Name = FixDuplicatedObjectsNames(efx_model, efxWall.Floor, typeof(efxStraightWallEx), w.Name);
                        #endregion
                        Parameter is_attached = w.get_Parameter(BuiltInParameter.WALL_TOP_IS_ATTACHED);

                        if (is_attached.AsInteger() != 0)
                        {

                            List<efxRoofEx> intersectedRoofs = new List<efxRoofEx>();
                            var roofsGroups = efx_model.FramingObjectList.Where(y => y.GetType() == typeof(efxRoofEx)).Cast<efxRoofEx>()
                                .GroupBy(y => y.Floor);
                            foreach (var group in roofsGroups)
                            {
                                intersectedRoofs = group.Where(y => IsRoofBoundaringAWall(efx_model, c, y)).ToList();
                                if (intersectedRoofs.Count > 0)
                                {
                                    break;
                                }
                            }
                            efxRoofEx selectedRoof = new efxRoofEx();
                            if (intersectedRoofs.Count > 1)
                            {
                                List<Face> faces = new List<Face>();
                                foreach (var r in HostObjectUtils.GetSideFaces(w, ShellLayerType.Interior))
                                {
                                    faces.Add(w.GetGeometryObjectFromReference(r) as Face);
                                }
                                Face SideFace = faces.OrderByDescending(x => x.Area).First();
                                List<XYZ> profile = new List<XYZ>();
                                for (int i = 0; i < SideFace.EdgeLoops.Size; i++)
                                {
                                    EdgeArray e_array = SideFace.EdgeLoops.get_Item(i);
                                    for (int j = 0; j < e_array.Size; j++)
                                    {
                                        Edge e = e_array.get_Item(j);
                                        profile.Add(e.AsCurve().Tessellate()[0]);

                                    }
                                }
                                selectedRoof = intersectedRoofs.OrderByDescending(x => CountOfPointsBeneathRoof(efx_model, x, profile)).First();
                            }
                            else
                            { selectedRoof = intersectedRoofs[0]; }
                            selectedRoof.RoofAttachedWalls.Add(new efxRoofAttachedWallEx(efxWall.Name, efxWall.Floor));
                            //efxWall.AttachmentInfo.attachmentType = 4;
                            //efxWall.AttachmentInfo.attachedRoofName = selectedRoof.Name;
                            //efxWall.AttachmentInfo.attachedFloor = selectedRoof.Floor;

                        }

                        #region AddOpening
                        var wallOpeningsIds = w.FindInserts(true, true, true, true);
                        if (wallOpeningsIds.Count > 0)
                        {

                            #region DoorFilter
                            var doorFilter = new FilteredElementCollector(doc, wallOpeningsIds).OfCategory(BuiltInCategory.OST_Doors);
                            if (doorFilter.Any())
                            {
                                foreach (var door in doorFilter)
                                {
                                    efxDoorEx efx_door = new efxDoorEx();
                                    var mid_point = ((door as FamilyInstance).Location as LocationPoint).Point;
                                    List<double> widthprops = new List<double>();

                                    widthprops.Add((door as FamilyInstance).get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble());
                                    widthprops.Add((door as FamilyInstance).get_Parameter(BuiltInParameter.GENERIC_WIDTH).AsDouble());
                                    widthprops.Add((door as FamilyInstance).Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble());
                                    widthprops.Add((door as FamilyInstance).Symbol.get_Parameter(BuiltInParameter.GENERIC_WIDTH).AsDouble());

                                    var width = widthprops.FirstOrDefault(v => v != 0);

                                    efx_door.NetOpeningStartPoint = Get_RelativePoint(new efxPoint(mid_point.X, mid_point.Y), efxWall.CenterLine.P1, width / 2);
                                    efx_door.NetOpeningEndPoint = Get_RelativePoint(new efxPoint(mid_point.X, mid_point.Y), efxWall.CenterLine.P2, width / 2);
                                    efx_door.SillHeight = door.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).AsDouble();
                                    efx_door.HeadHeight = door.get_Parameter(BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM).AsDouble();
                                    efx_door.Name = FixDuplicatedObjectsNames(efx_model, efxWall.Floor, typeof(efxDoorEx), door.Name);
                                    efx_door.Floor = efxWall.Floor;
                                    efx_door.WallName = efxWall.Name;
                                    efx_model.FramingObjectList.Add(efx_door);
                                }
                            }
                            #endregion
                            #region WindowFilter
                            var windowFilter = new FilteredElementCollector(doc, wallOpeningsIds).OfCategory(BuiltInCategory.OST_Windows);
                            if (windowFilter.Any())
                            {
                                foreach (var window in windowFilter)
                                {
                                    efxWindowEx efx_window = new efxWindowEx();

                                    var mid_point = ((window as FamilyInstance).Location as LocationPoint).Point;
                                    List<double> widthprops = new List<double>();

                                    widthprops.Add((window as FamilyInstance).get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble());
                                    widthprops.Add((window as FamilyInstance).get_Parameter(BuiltInParameter.GENERIC_WIDTH).AsDouble());
                                    widthprops.Add((window as FamilyInstance).Symbol.get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble());
                                    widthprops.Add((window as FamilyInstance).Symbol.get_Parameter(BuiltInParameter.GENERIC_WIDTH).AsDouble());

                                    var width = widthprops.FirstOrDefault(v => v != 0);
                                    efx_window.NetOpeningStartPoint = Get_RelativePoint(new efxPoint(mid_point.X, mid_point.Y), efxWall.CenterLine.P1, width / 2);
                                    efx_window.NetOpeningEndPoint = Get_RelativePoint(new efxPoint(mid_point.X, mid_point.Y), efxWall.CenterLine.P2, width / 2);

                                    efx_window.SillHeight = window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).AsDouble();
                                    efx_window.HeadHeight = window.get_Parameter(BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM).AsDouble();
                                    efx_window.Name = FixDuplicatedObjectsNames(efx_model, efxWall.Floor, typeof(efxWindowEx), window.Name);
                                    efx_window.Floor = efxWall.Floor;
                                    efx_window.WallName = efxWall.Name;
                                    efx_model.FramingObjectList.Add(efx_window);
                                }
                            }
                            #endregion
                        }
                        #endregion
                        // set the top profile
                        efx_model.FramingObjectList.Add(efxWall);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }

        private static int CountOfPointsBeneathRoof(efxModelEx efx_model, efxRoofEx efxroof, List<XYZ> profile)
        {
            try
            {
                double minZ = profile.Select(x => x.Z).Min();
                //if (efx_model.FloorList.FirstOrDefault(x => x.Name == efxroof.Floor).Sublevel < minZ)
                { return 0; }
                minZ = Math.Round(minZ, 5);
                profile.RemoveAll(x => Math.Abs(Math.Round(x.Z, 5) - minZ) < 0.0001);
                int count = 0;
                List<XYZ> roofprintpolygon = new List<XYZ>();
                for (int i = 0; i < efxroof.FootprintEdges.Count; i++)
                {
                    roofprintpolygon.Add(new XYZ(efxroof.FootprintEdges[i].startpoint.X, efxroof.FootprintEdges[i].startpoint.Y, 0));
                }
                foreach (XYZ point in profile)
                {
                    EFRvt.Revit.IntersectionResult result = GeometryMathatics.Point_PolygonInterSection(new XYZ(point.X, point.Y, 0), roofprintpolygon);
                    if ((result == EFRvt.Revit.IntersectionResult.inSide) || (result == EFRvt.Revit.IntersectionResult.OnEdge) || (result == Revit.IntersectionResult.Corner))
                    { count++; }
                }
                return count;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return 0;
            }
        }

        private static bool IsRoofBoundaringAWall(efxModelEx efx_model, LocationCurve lc, efxRoofEx efxroof)
        {
            try
            {
                List<XYZ> wallLocation = lc.Curve.Tessellate().ToList();
                XYZ start = wallLocation[0];
                XYZ end = wallLocation.Last();
                //if (efx_model.FloorList.FirstOrDefault(x => x.Name == efxroof.Floor).Sublevel < start.Z)
                //{ return false; }

                List<XYZ> roofprintpolygon = new List<XYZ>();
                for (int i = 0; i < efxroof.FootprintEdges.Count; i++)
                {
                    roofprintpolygon.Add(new XYZ(efxroof.FootprintEdges[i].startpoint.X, efxroof.FootprintEdges[i].startpoint.Y, 0));
                }
                start = new XYZ(start.X, start.Y, 0);
                end = new XYZ(end.X, end.Y, 0);
                EFRvt.Revit.IntersectionResult result1 = GeometryMathatics.Point_PolygonInterSection(start, roofprintpolygon);
                EFRvt.Revit.IntersectionResult result2 = GeometryMathatics.Point_PolygonInterSection(end, roofprintpolygon);
                bool b1 = (result1 == EFRvt.Revit.IntersectionResult.inSide) || (result1 == EFRvt.Revit.IntersectionResult.OnEdge) || (result1 == EFRvt.Revit.IntersectionResult.Corner);
                bool b2 = (result2 == EFRvt.Revit.IntersectionResult.inSide) || (result2 == EFRvt.Revit.IntersectionResult.OnEdge) || (result2 == EFRvt.Revit.IntersectionResult.Corner);
                if (b1 && b2)
                { return true; }
                else if ((result1 == EFRvt.Revit.IntersectionResult.inSide) || (result2 == EFRvt.Revit.IntersectionResult.inSide))
                { return true; }
                else
                {
                    roofprintpolygon.Add(roofprintpolygon[0]);
                    for (int i = 0; i < roofprintpolygon.Count - 1; i++)
                    {
                        if (GeometryMathatics.DropRay(start, end, roofprintpolygon[i], roofprintpolygon[i + 1]) != null)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return false;
            }
        }

        private static bool IsRoofBoundaringRafterArea(efxModelEx efx_model, XYZ midPoint, efxRoofEx efxroof)
        {
            try
            {
                List<XYZ> roofprintpolygon = new List<XYZ>();
                for (int i = 0; i < efxroof.FootprintEdges.Count; i++)
                {
                    roofprintpolygon.Add(new XYZ(efxroof.FootprintEdges[i].startpoint.X, efxroof.FootprintEdges[i].startpoint.Y, 0));
                }

                return (EFRvt.Revit.IntersectionResult.inSide == GeometryMathatics.Point_PolygonInterSection(midPoint, roofprintpolygon));

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return false;
            }
        }

        private static efxPoint Get_RelativePoint(efxPoint p1, efxPoint p2, double v)
        {
            double length = Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
            return new efxPoint(p1.X - v * (p1.X - p2.X) / length, p1.Y - v * (p1.Y - p2.Y) / length);
        }
        public static List<Level> GetSortedLevels(Document doc)
        {
            try
            {
                //addding levels attatched to bottom edges of 
                // var lvlIds = new List<Element>();
                var lvls = new List<Level>();
                foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
                {
                    var w = e as Wall;
                    if (w != null)
                    {
                        var newlevel = doc.GetElement(w.LevelId) as Level;
                        if (newlevel != null)
                        {
                            if (lvls.Count > 0)
                            {
                                if (Math.Abs(newlevel.Elevation - lvls.Last().Elevation) > 9.8 && lvls.FirstOrDefault(l => l.UniqueId == newlevel.UniqueId) == null)
                                { lvls.Add(newlevel); }
                            }
                            else
                            { lvls.Add(newlevel); }
                        }
                    }
                }

                //var lvls = lvlIds.Select(eId => doc.GetElement(eId) as Level).ToList();
                var sortedLvls = lvls.OrderBy(l => l.Elevation).ToList();

                // adding top floors
                var lastfloorwalls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).Where(w => w.LevelId == sortedLvls.Last().Id);
                foreach (var x in lastfloorwalls)
                {
                    var w = x as Wall;
                    if (w == null)
                        continue;
                    Level toplevel;

                    Parameter is_attached = w.Parameters.Cast<Parameter>().First(e => e.Definition.Name == "Top is Attached");
                    if (is_attached.AsInteger() != 0)
                    {
                        Parameter Height = w.Parameters.Cast<Parameter>().First(e => e.Definition.Name == "Unconnected Height");
                        toplevel = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => sortedLvls.Last().Elevation + Height.AsDouble() - l.Elevation).FirstOrDefault();
                    }
                    else
                    {
                        Parameter toplvlID = w.Parameters.Cast<Parameter>().First(e => e.Definition.Name == "Top Constraint");
                        toplevel = doc.GetElement(toplvlID.AsElementId()) as Level;
                    }

                    if (toplevel != null && sortedLvls.FirstOrDefault(l => l.UniqueId == toplevel.UniqueId) == null)
                    {
                        if (Math.Abs(toplevel.Elevation - sortedLvls.Last().Elevation) > 9.8)
                        { sortedLvls.Add(toplevel); }
                    }

                }
                return sortedLvls.OrderBy(l => l.Elevation).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        public static List<efxFloorEx> GetAllFloors(Document doc, List<Level> sortedLvls)
        {
            try
            {
                List<efxFloorEx> floors = new List<efxFloorEx>();

                if (sortedLvls == null || sortedLvls.Count == 0)
                { return floors; }

                var firstlevel = sortedLvls.First();
                floors.Add(new efxFloorEx(firstlevel.Name, firstlevel.Elevation, firstlevel.Elevation, true));
                for (int i = 1; i < sortedLvls.Count; i++)
                {
                    try
                    {
                        Level lvl = sortedLvls[i];
                        Level prelvl = sortedLvls[i - 1];
                        floors.Add(new efxFloorEx(lvl.Name, lvl.Elevation, lvl.Elevation - prelvl.Elevation, false));
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.ReportException(ex);
                    }
                }

                return floors;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
        //public static List<List<efxPost>> GetAllPosts(Document doc)
        //{
        //    try
        //    {
        //        var rvt_posts = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralColumns).Where(p => p != null && (p.Document.GetElement(p.LevelId) as Level) != null)
        //            .OrderBy(p => (p.Document.GetElement(p.LevelId) as Level).Elevation)
        //            .GroupBy(p => (p.Document.GetElement(p.LevelId) as Level).Elevation).ToList();
        //        var result = new List<List<efxPost>>();
        //        var groups = rvt_posts.Select(grp => grp);
        //        foreach (var grp in groups)
        //        {
        //            result.Add(grp.Select(GetPost).ToList());
        //        }
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorHandler.ReportException(ex);
        //        return null;
        //    }
        //}
        public static void AddAllPosts(Document doc, efxModelEx model)
        {
            try
            {
                var rvt_posts_group = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralColumns).Cast<FamilyInstance>()
                    .Where(p => p != null && model.FloorList.FirstOrDefault(x => x.Name == (p.Document.GetElement(p.LevelId) as Level).Name) != null)
                    .GroupBy(p => p.Symbol);
                foreach (var Bysecgroup in rvt_posts_group)
                {
                    try
                    {
                        efxSection section = new efxSection();
                        if (Bysecgroup.Key.Family.Name == "EFPost")
                        {
                            float b = (float)Bysecgroup.Key.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "b").AsDouble() * 12;
                            float d = (float)Bysecgroup.Key.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "d").AsDouble() * 12;
                            section = model.SectionList.FirstOrDefault(x => Math.Abs(x.WidthInch - b) < 0.0001 && Math.Abs(x.DepthInch - d) < 0.0001);
                            if (section == null)
                            {
                                section = AddNewRevitToModel(model, b, d);
                            }
                        }

                        foreach (var post in Bysecgroup)
                        {
                            AddSinglePost(doc, post, section.Name, model);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.ReportException(ex);
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }
        public static void AddAllBeams(Document doc, List<FamilyInstance> SingleBeams, efxModelEx model)
        {
            try
            {
                // = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFraming).Cast<FamilyInstance>()
                var rvt_Beams_group = SingleBeams.Where(p => p != null && model.FloorList.FirstOrDefault(x => x.Name == (p.Document.GetElement(p.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM).AsElementId()) as Level).Name) != null)
                    .GroupBy(p => p.Symbol);

                foreach (var Bysecgroup in rvt_Beams_group)
                {
                    try
                    {
                        efxSection section = new efxSection();
                        float b, d;
                        if (Bysecgroup.Key.Family.Name == "EFBeam")
                        {
                            b = (float)Bysecgroup.Key.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "b").AsDouble() * 12;
                            d = (float)Bysecgroup.Key.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == "d").AsDouble() * 12;
                        }
                        else
                        {
                            Parameter WidthParmeter = Bysecgroup.Key.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_WIDTH) ??
                                Bysecgroup.Key.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM) ??
                                Bysecgroup.Key.get_Parameter(BuiltInParameter.GENERIC_WIDTH);
                            Parameter DepthParmeter = Bysecgroup.Key.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_HEIGHT) ??
                                Bysecgroup.Key.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM) ??
                                Bysecgroup.Key.get_Parameter(BuiltInParameter.GENERIC_HEIGHT) ??
                                Bysecgroup.Key.get_Parameter(BuiltInParameter.GENERIC_DEPTH);

                            b = (float)WidthParmeter.AsDouble() * 12;
                            d = (float)DepthParmeter.AsDouble() * 12;
                        }
                        section = model.SectionList.FirstOrDefault(x => Math.Abs(x.WidthInch - b) < 0.0001 && Math.Abs(x.DepthInch - d) < 0.0001);
                        if (section == null)
                        {
                            section = AddNewRevitToModel(model, b, d);
                        }

                        foreach (var Beam in Bysecgroup)
                        {
                            AddSingleBeam(doc, Beam, section.Name, model);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.ReportException(ex);
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }
        private static void AddSingleBeam(Document doc, FamilyInstance beam, string sectionname, efxModelEx model)
        {
            try
            {
                efxBeamEx efxbeam = new efxBeamEx();
                efxbeam.Floor = model.FloorList.FirstOrDefault(x => x.Name == (doc.GetElement(beam.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM).AsElementId()).Name)).Name;

                if (beam.Location is LocationCurve)
                {
                    var curve = (beam.Location as LocationCurve).Curve;
                    if (curve == null)
                        return;
                    var line = curve as Line;
                    if (line == null)
                        return;
                    XYZ start = line.GetEndPoint(0);
                    efxbeam.StartPoint = new efxPoint((float)start.X, (float)start.Y);
                    XYZ end = line.GetEndPoint(1);
                    efxbeam.EndPoint = new efxPoint((float)end.X, (float)end.Y);
                }
                else { return; }

                efxbeam.Section = sectionname;
                efxbeam.Name = FixDuplicatedObjectsNames(model, efxbeam.Floor, typeof(efxBeamEx), beam.Name);
                model.FramingObjectList.Add(efxbeam);
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }
        }
        public static void AddSinglePost(Document doc, FamilyInstance post, string sectionName, efxModelEx model)
        {
            try
            {


                XYZ position = null;
                if (post.Location is LocationCurve)
                {
                    var curve = (post.Location as LocationCurve).Curve;

                    if (curve == null)
                        return;
                    var line = curve as Line;
                    if (line == null)
                        return;
                    position = line.Origin;
                }
                else if (post.Location is LocationPoint)
                {
                    position = (post.Location as LocationPoint).Point;
                }
                if (position == null)
                { return; }

                efxPoint efxposition = new efxPoint(position.X, position.Y);

                efxFloorEx topfloor = model.FloorList.FirstOrDefault(x => x.Name ==
                (doc.GetElement(post.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).AsElementId()) as Level).Name);
                efxFloorEx bottomfloor = model.FloorList.FirstOrDefault(x => x.Name ==
                (doc.GetElement(post.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).AsElementId()) as Level).Name);
                int diff = model.FloorList.IndexOf(topfloor) - model.FloorList.IndexOf(bottomfloor);
                for (int i = model.FloorList.IndexOf(topfloor); i > model.FloorList.IndexOf(bottomfloor); i--)
                {
                    try
                    {
                        efxPostEx efxpost = new efxPostEx();
                        efxpost.Position = efxposition;
                        efxpost.Floor = model.FloorList[i].Name;
                        efxpost.Name = FixDuplicatedObjectsNames(model, efxpost.Floor, typeof(efxPostEx), post.Name);
                        efxpost.Section = sectionName;
                        model.FramingObjectList.Add(efxpost);
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.ReportException(ex);
                    }

                }
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
            }



        }
        public static efxSection AddNewRevitToModel(efxModelEx model, float widthInch, float depthInch)
        {
            efxSection section = new efxSection();
            int i = model.SectionList.Where(x => x.Name.Contains("Revit Section")).Count();
            section.Name = "Revit Section" + (i + 1).ToString(); section.WidthInch = widthInch; section.DepthInch = depthInch;
            model.SectionList.Add(section);
            return section;
        }

        public static string FixDuplicatedObjectsNames(efxModelEx model, string floorName, Type t, string name)
        {
            int n = model.FramingObjectList.Where(x => x.GetType() == t && x.Floor == floorName && x.Name.Contains(name)).Count();
            return name + " -" + (n + 1); /* Revit Model*/
        }


    }
}
