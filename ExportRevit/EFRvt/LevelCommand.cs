using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using EFRvt.Revit;
using Elibre.Net.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFRvt
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    [Autodesk.Revit.Attributes.Journaling(Autodesk.Revit.Attributes.JournalingMode.NoCommandData)]
    public class DeleteAllLevelsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Events.m_doc = commandData.Application.ActiveUIDocument.Document;

                using (Transaction tran = new Transaction(Events.m_doc, "Delete All Levels"))
                {
                    tran.Start();
                    Events.m_doc.Delete(ExtensionMethods.GetSortedLevels(Events.m_doc).Select(x => x.Id).ToArray());
                    FailureHandlingOptions failopt = tran.GetFailureHandlingOptions();
                    failopt.SetFailuresPreprocessor(new RevitHandler());
                    tran.SetFailureHandlingOptions(failopt);

                    tran.Commit();
                }
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return Result.Failed;
            }
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    [Autodesk.Revit.Attributes.Journaling(Autodesk.Revit.Attributes.JournalingMode.NoCommandData)]
    public class GenerateLevelsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Events.m_doc = commandData.Application.ActiveUIDocument.Document;
                PickFloorForm frm = new PickFloorForm(new List<Level>());

                if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                { }
                if (frm.floorInfos != null && frm.floorInfos.Any())
                {

                    using (Transaction tran = new Transaction(Events.m_doc, "Delete All Levels"))
                    {
                        tran.Start();

                        GeneralCreator.CreateLevel(Events.m_doc, frm.floorInfos[0].Levels.BaseReferencelevel.Elevation, "Foundation");

                        for (int i = 0; i < frm.floorInfos.Length; i++)
                        {
                            string s = "Floor No." + (i + 1) + "-";
                            GeneralCreator.CreateLevel(Events.m_doc, frm.floorInfos[i].Levels.TopPlateReferencelevel.Elevation, s + "TopPlate Level");
                            GeneralCreator.CreateLevel(Events.m_doc, frm.floorInfos[i].Levels.FramingReferencelevel.Elevation, s + "Framing Level");
                            GeneralCreator.CreateLevel(Events.m_doc, frm.floorInfos[i].Levels.NextFloorBaseReferencelevel.Elevation, s + "Sub Level");

                        }
                        FailureHandlingOptions failopt = tran.GetFailureHandlingOptions();
                        failopt.SetFailuresPreprocessor(new RevitHandler());
                        tran.SetFailureHandlingOptions(failopt);

                        tran.Commit();
                    }



                }


                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return Result.Failed;
            }
        }
    }
}
