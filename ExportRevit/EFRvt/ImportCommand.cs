using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using EFRvt.Revit;
using Elibre.Net.Debug;
using System;
using System.IO;
using System.Linq;

namespace EFRvt
{
    /// <summary>
    /// The entrance of this example, implements the Execute method of IExternalCommand
    /// </summary>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    [Autodesk.Revit.Attributes.Journaling(Autodesk.Revit.Attributes.JournalingMode.NoCommandData)]
    public class ImportCommand : IExternalCommand
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
                Events.m_doc = commandData.Application.ActiveUIDocument.Document;
                //Events.m_UIApplication = commandData.Application;
                //Events.Initialize();

                EFExt2017.frmImportfromEF frmImport = new EFExt2017.frmImportfromEF(Events.m_doc);
                if (false == frmImport.IsDisposed)
                {
                    if (frmImport.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        // Get the .EFRvt or .EFRvtAuto file path
                        string filePath = frmImport.fileName;

                        // Create a .rvt file path from the .EFRvt or the .EFRvtAuto file path
                        string rvtFilePath = CreateRvtFilePath(filePath);

                        // Create a new Revit document and save it to the .rvt file path
                        Document newDoc = CreateNewRevitDocument(commandData.Application, rvtFilePath);

                        // Open and activate the new Revit file document and set the static document to it
                        Events.m_doc = commandData.Application.OpenAndActivateDocument(rvtFilePath).Document;

                        // Change the view of the Revit document to the default 3d view
                        ChangeViewTo3d(commandData.Application, Events.m_doc);

                        // Delete Revit backup file for the .rvt file
                        DeleteBackupRvtFile(rvtFilePath);

                        // Start importation from the .EFRvt or .EFRvtAuto file
                        //Events.StartRvt(mapBuilding);
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
        #endregion

        /// <summary>
        /// Create a .rvt file path from a file path of another extension.
        /// </summary>
        /// <param name="filePath">The file path of the another extension.</param>
        /// <returns>The path of the .rvt file.</returns>
        private string CreateRvtFilePath(string filePath)
        {
            FileInfo finfo = new FileInfo(filePath);
            string filename = finfo.Name.Replace(finfo.Extension, ""); // file name only without extension or directory

            filename = filename.Trim();

            string docPath = finfo.DirectoryName; // directory path only

            if (!Directory.Exists(docPath))
                Directory.CreateDirectory(docPath);

            string rvtFilePath = docPath + "\\" + filename + ".rvt"; // .rvt full file path

            return rvtFilePath;
        }

        /// <summary>
        /// Create a new Revit document and save it to a certain .rvt file path.
        /// </summary>
        /// <param name="application">The Revit UIApplication.</param>
        /// <param name="rvtFilePath">The .rvt file path where the file will be saved.</param>
        /// <returns>The created document after saving.</returns>
        private Document CreateNewRevitDocument(UIApplication application, string rvtFilePath)
        {
            // Create a new Revit document
            Document newDoc;

            //string templatePath = "C:/ProgramData/Autodesk/RVT 2020/Templates/US Imperial/default.rte";

            string revitTemplatePath = GeneralCreator.GetRevitBaseTemplate();
            if (!String.IsNullOrEmpty(revitTemplatePath))
                newDoc = application.Application.NewProjectDocument(revitTemplatePath);
            //else if (File.Exists(templatePath))
            //    newDoc = commandData.Application.Application.NewProjectDocument(templatePath);
            else
                newDoc = application.Application.NewProjectDocument(UnitSystem.Imperial);

            // Save the new document as Revit .rvt file
            newDoc.SaveAs(rvtFilePath, new SaveAsOptions() { OverwriteExistingFile = true });

            return newDoc;
        }

        /// <summary>
        /// Change the view of a Revit document to the default 3d view.
        /// </summary>
        /// <param name="application">The Revit UIApplication.</param>
        /// <param name="document">The Revit document where we change the view.</param>
        private void ChangeViewTo3d(UIApplication application, Document document)
        {
            // Change the view to the default 3d view
            View3D view3d = new FilteredElementCollector(document).OfClass(typeof(View3D))
                                                                   .Cast<View3D>()
                                                                   .Where(v => v.Name == "{3D}")
                                                                   .FirstOrDefault();
            if (view3d != null)
            {
                UIDocument uiDoc = application.ActiveUIDocument;
                uiDoc.ActiveView = view3d;
            }
            else
            {
                RevitCommandId commandId = RevitCommandId.LookupPostableCommandId(PostableCommand.Default3DView);

                if (application.CanPostCommand(commandId))
                    application.PostCommand(commandId);
            }

            //newUiDoc.ActiveView.DisplayStyle = DisplayStyle.RealisticWithEdges;
            //newUiDoc.ActiveView.AreAnnotationCategoriesHidden = true;

            document.Save();
        }

        /// <summary>
        /// Delete Revit backup file for a certain .rvt file.
        /// </summary>
        /// <param name="rvtFilePath">The path of the .rvt file.</param>
        private void DeleteBackupRvtFile(string rvtFilePath)
        {
            FileInfo finfo = new FileInfo(rvtFilePath);
            string filename = finfo.Name.Replace(finfo.Extension, ""); // file name only without extension or directory
            filename = filename.Trim();

            string folderPath = finfo.DirectoryName; // directory path only
            System.IO.DirectoryInfo di = new DirectoryInfo(folderPath);

            var backupFilePath = filename + ".0001.rvt"; // duplicated .rvt full file path

            foreach (FileInfo file in di.GetFiles())
            {
                if (file.Name.Equals(backupFilePath))
                    file.Delete();
            }
        }
    }
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    [Autodesk.Revit.Attributes.Journaling(Autodesk.Revit.Attributes.JournalingMode.NoCommandData)]
    public class EfxImportCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Events.m_doc = commandData.Application.ActiveUIDocument.Document;
                //Events.m_UIApplication = commandData.Application;
                // Events.Initialize();

                EFExt2017.frmImportfromEF frmImport = new EFExt2017.frmImportfromEF(Events.m_doc, EFExt2017.ImportedFormat.Efx);
                if (false == frmImport.IsDisposed)
                {
                    if (frmImport.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {

                        //EfxCreator creator = new  EfxCreator( frmImport.fileName, Events.m_doc , commandData);
                    }
                }
                //var options = DoubleClickOptions.GetDoubleClickOptions();
                //options.SetAction(DoubleClickTarget.Group, DoubleClickAction.NoAction);

                //UIFramework.RevitRibbonControl.RibbonControl.FindTab("Modify").FindPanel("").IsEnabled = false;
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                ErrorHandler.ReportException(ex);
                return Result.Failed;
            }
        }
    }



    public class SymbolMap
    {
        readonly string m_symbolName = "";
        readonly FamilySymbol m_symbol = null;

        /// <summary>
        /// constructor without parameter is forbidden
        /// </summary>
        private SymbolMap()
        {
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="symbol">family symbol</param>
        public SymbolMap(FamilySymbol symbol)
        {
            m_symbol = symbol;
            string familyName = "";
            if (null != symbol.Family)
            {
                familyName = symbol.Family.Name;
            }
            m_symbolName = familyName + " : " + symbol.Name;
        }

        /// <summary>
        /// SymbolName property
        /// </summary>
        public string SymbolName
        {
            get
            {
                return m_symbolName;
            }
        }
        /// <summary>
        /// ElementType property
        /// </summary>
        public FamilySymbol ElementType
        {
            get
            {
                return m_symbol;
            }
        }
    }
}
