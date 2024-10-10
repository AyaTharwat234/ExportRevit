


using Autodesk.Revit.DB;

using Elibre.Net.Debug;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EFRvt
{
    public static class Creator
    {
        public static void CreateLevels()
        {

        }
        public static void SolveDuplicatedFamilySymbolName(Document doc, Family fam, ref string newName)
        {
            bool flag = true;
            foreach (ElementId id in fam.GetFamilySymbolIds())
            {
                FamilySymbol s = doc.GetElement(id) as FamilySymbol;
                if (s.Name == newName)
                {
                    flag = false; break;
                }
            }
            if (!flag)
            { newName += "*"; }
        }
        public static FamilySymbol UpdateSuitableSymbol(Document doc, Family fam, Dictionary<string, object> parameters, string newName)
        {
            if (fam == null) { return null; }
            FamilySymbol s;
            using (Transaction _t = new Transaction(doc, "UpdateSuitableSymbol"))
            {
                _t.Start();
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
                _t.Commit();
            }
            return s;
        }

        public static FamilySymbol SelectSuitableSymbol(Document doc, Family fam, Dictionary<string, object> parameters)
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
                        double RealValue = s.Parameters.Cast<Parameter>().FirstOrDefault(x => x.Definition.Name == param.Key).AsDouble();
                        flag = Math.Abs(RealValue - Convert.ToDouble(param.Value)) < 0.0001;
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

        //public static Family CheckIfFamilyIsUploaded(Document doc, string v, string path)
        //{
        //    try
        //    {
        //        Family fam = new FilteredElementCollector(doc).OfClass(typeof(Family)).Cast<Family>().FirstOrDefault(x => x.Name == v);
        //        if (fam == null)
        //        {
        //            using (Transaction t = new Transaction(doc, "LoadFamily"))
        //            {
        //                t.Start();
        //                fam = GeneralCreator.LoadFamily(doc, path, v);
        //                t.Commit();
        //            }

        //        }
        //        return fam;

        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorHandler.ReportException(ex);
        //        return null;
        //    }
        //}

        private static Family LoadFamily(Document doc, string path, string FamilyName)
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

        //public static List<XYZ> Convertpoints(List<efxPoint3> points)
        //{
        //    List<XYZ> result = new List<XYZ>();
        //    foreach (var point in points)
        //    {
        //        result.Add(new XYZ(point.X, point.Y, point.Z));
        //    }
        //    return result;
        //}

        //public static List<XYZ> Convertpoints(efxPoint3[] points)
        //{
        //    List<XYZ> result = new List<XYZ>();
        //    foreach (var point in points)
        //    {
        //        result.Add(new XYZ(point.X, point.Y, point.Z));
        //    }
        //    return result;
        //}
    }
    public class RevitHandler : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            failuresAccessor.DeleteAllWarnings();

            return FailureProcessingResult.Continue;
        }
    }
}
