

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Elibre.eFramer;
using Elibre.Net.Debug;

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using System.Xml.Serialization;


namespace EFRvt
{
    /// <summary>
    /// The entrance of this example, implements the Execute method of IExternalCommand
    /// </summary>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    [Autodesk.Revit.Attributes.Journaling(Autodesk.Revit.Attributes.JournalingMode.NoCommandData)]
    public class ExportCommand : IExternalCommand
    {
        #region IExternalCommand Members Implementation
        ///<summary>
        /// Implement this method as an external command for Revit.
        /// </summary>
        /// <param name="commandData">An object that is passed to the external application 
        /// which contains data related to the command, 
        /// such as the application object and active view.</param>
        /// <param name="message">A message that can be set by the external application 
        /// which will be displayed if a failure or cancellation is returned by 
        /// the external command.</param>
        /// <param name="elements">A set of elements to which the external application 
        /// can add elements that are to be highlighted in case of failure or cancellation.</param>
        /// <returns>Return the status of the external command. 
        /// A result of Succeeded means that the API external method functioned as expected. 
        /// Cancelled can be used to signify that the user cancelled the external operation 
        /// at some point. Failure should be returned if the application is unable to proceed with 
        /// the operation.</returns>
        public Result Execute(ExternalCommandData commandData,
            ref string message, ElementSet elements)
        {
            try
            {
                var uidoc = commandData.Application.ActiveUIDocument;
                var doc = uidoc.Document;

                var serializer = new XmlSerializer(typeof(EFBuilding));
                //var rvtBuildingInfo = RevitToElibre.GetRevitBuildingInfo(doc);
                string failMessage = "";
                EFBuilding efBuilding = ExtensionMethods.GetInstance(doc, out failMessage);
                if (efBuilding == null)
                {
                    TaskDialog.Show("Error", failMessage, TaskDialogCommonButtons.Ok);
                    return Result.Failed;
                }
                uidoc.RefreshActiveView();
                //string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                //string workingPath = Globals.GetWorkingFolder();
                string xslLocation = doc.PathName;
                if (string.IsNullOrEmpty(xslLocation))
                {
                    //  Not saved Revit File
                    xslLocation = Globals.GetWorkingFolder();
                }
                xslLocation = Path.ChangeExtension(xslLocation, ".xml");
                TextWriter writer = new StreamWriter(xslLocation);
                serializer.Serialize(writer, efBuilding);
                writer.Close();
                RunEF.RunEFramer(xslLocation);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return Result.Failed;
            }

        }
        #endregion
    }


    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    [Autodesk.Revit.Attributes.Journaling(Autodesk.Revit.Attributes.JournalingMode.NoCommandData)]
    public class EfxExportCommand : IExternalCommand
    {
        static List<ElementId> _protectedIds = new List<ElementId>();
        static public void beProtected(IEnumerable<ElementId> ids)
        {
            foreach (var id in ids)
            {
                if (!IsProtected(id))
                { _protectedIds.Add(id); }
            }

        }

        static public void beProtected(ElementId id)
        {
            if (!IsProtected(id))
            { _protectedIds.Add(id); }
        }

        static public bool IsProtected(ElementId id)
        {
            return _protectedIds.Contains(id);
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;
            var model = RevitToEfx.GetEfxModel(doc);

            if (model == null)
            {
                return Result.Failed;
            }

            uidoc.RefreshActiveView();

            //string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string workingPath = Globals.GetWorkingFolder();
            string xslLocation = Path.Combine(workingPath + "\\", "result.efx");
       efxWriter.WriteEfxEx(model, xslLocation);

            ///////////////////////////////////////////////////////
            RunEF.RunEFramer_efx(); // Commented for testing only
            //RunEF.LaunchEFramer();
            ///////////////////////////////////////////////////////

            return Result.Succeeded;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    [Autodesk.Revit.Attributes.Journaling(Autodesk.Revit.Attributes.JournalingMode.NoCommandData)]
    public class EfxAppLauncher : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {


            using (frmMain frm = new frmMain())
            {
                frm.ShowDialog();
            }
            return Result.Succeeded;
        }
    }
}
