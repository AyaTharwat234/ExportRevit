using Autodesk.Revit.DB;
using EFRvt;

using Elibre.Net.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFRvt
{
    internal static class RoofGenerator
    {
        private static RoofType defaultroofType = null;
        private static double roofThickness = 0.1;

        public static FootPrintRoof CreateRoof(Document doc, Level l, double offset, List<XYZ> profile, List<List<XYZ>> openings, List<MapRoofBaseLine> baseLines)
        {
            try
            {
                SetDefaultRoofType(doc);
                profile = trimEndsOfFootprintEdges(FootprintEdgeShiftOut(profile, baseLines));

                ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
                FootPrintRoof footprintRoof;
                CurveArray footprint = doc.Application.Create.NewCurveArray();
                int n = profile.Count;
                for (int i = 0; i < n; i++)
                {
                    int x = (i + 1) % n;
                    footprint.Append(Line.CreateBound(profile[i], profile[x]));
                }

                footprintRoof = doc.Create.NewFootPrintRoof(footprint, l, defaultroofType, out footPrintToModelCurveMapping);

                ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
                iterator.Reset();
                int k = 0;
                while (iterator.MoveNext())
                {
                    ModelCurve modelCurve = iterator.Current as ModelCurve;
                    MapRoofBaseLine el = baseLines[k];
                    if (el.Gable)
                    {
                        footprintRoof.set_DefinesSlope(modelCurve, false);
                    }
                    else
                    {
                        footprintRoof.set_DefinesSlope(modelCurve, true);
                        footprintRoof.set_SlopeAngle(modelCurve, el.Slope / 12.00);
                    }
                    footprintRoof.set_Offset(modelCurve, el.Offset);
                    //footprintRoof.set_Overhang(modelCurve, el.OverHang);
                    k++;
                }

                footprintRoof.get_Parameter(BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM).Set(offset);
                foreach (List<XYZ> opening in openings)
                {
                    if (opening != null)
                    {
                        CurveArray openingfootprint = doc.Application.Create.NewCurveArray();
                        n = opening.Count;
                        for (int i = 0; i < n; i++)
                        {
                            int x = (i + 1) % n;
                            openingfootprint.Append(Line.CreateBound(opening[i], opening[x]));
                        }

                        doc.Create.NewOpening(footprintRoof, openingfootprint, false);
                    }
                }

                return footprintRoof;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static void SetDefaultRoofType(Document doc)
        {
            try
            {
                defaultroofType = new FilteredElementCollector(doc).OfClass(typeof(RoofType)).Cast<RoofType>().FirstOrDefault(x => x.Name.Contains("EFRoofType"));
                if (defaultroofType == null)
                {
                    defaultroofType = new FilteredElementCollector(doc).OfClass(typeof(RoofType)).Cast<RoofType>().FirstOrDefault(x => x.Name.Contains("Generic"));
                    //RoofType newType = defaultroofType.Duplicate("EFRoofType") as RoofType;
                    //Material mat = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>().Where(x=>x.MaterialClass.Contains("Generic")).FirstOrDefault();
                    //newType.SetCompoundStructure(CompoundStructure.CreateSingleLayerCompoundStructure(MaterialFunctionAssignment.Structure,0.05/12.00,mat.Id));
                    //defaultroofType = newType;
                }
            }
            catch (Exception e)
            { }
        }

        private static List<Line> FootprintEdgeShiftOut(List<XYZ> profile, List<MapRoofBaseLine> baseLine)
        {
            try
            {
                List<Line> lines = new List<Line>();
                int n = profile.Count;
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    XYZ start = profile[i];
                    XYZ end = profile[j];
                    XYZ direcrion = baseLine[i].OverHang * (end - start).CrossProduct(XYZ.BasisZ).Normalize();
                    start = start + direcrion;
                    end = end + direcrion;
                    lines.Add(Line.CreateBound(start, end));
                }
                return lines;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }

        }

        private static List<XYZ> trimEndsOfFootprintEdges(List<Line> fpEdges)
        {
            try
            {

                List<XYZ> newBasepolygon = new List<XYZ>();

                newBasepolygon.Add(Revit.GeometryMathatics.Two_Lines_Intersect(fpEdges[fpEdges.Count - 1].GetEndPoint(0), fpEdges[fpEdges.Count - 1].GetEndPoint(1)
                        , fpEdges[0].GetEndPoint(0), fpEdges[0].GetEndPoint(1)));

                for (int i = 0; i < fpEdges.Count - 1; i++)
                {
                    newBasepolygon.Add(Revit.GeometryMathatics.Two_Lines_Intersect(fpEdges[i].GetEndPoint(0), fpEdges[i].GetEndPoint(1)
                        , fpEdges[i + 1].GetEndPoint(0), fpEdges[i + 1].GetEndPoint(1)));

                }

                return newBasepolygon;

            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return null;
            }
        }
    }
}

