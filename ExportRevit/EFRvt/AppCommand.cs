



using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace EFRvt
{

    public class MainApplicationCommand : IExternalApplication
    {

        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {

            }
            catch (Exception ex)
            {
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);

            }
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                Events.showGUI = true;

                if (ReadAddonManifest())
                {

                    if (Events.showGUI)
                    {
                        application.CreateRibbonTab("Elibre Integration");
                        RibbonPanel panel = application.CreateRibbonPanel("Elibre Integration", "Import / Export");
                        // Reflection to look for this assembly path 
                        string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

                        #region Add button to panel 
                        PushButton button_import = panel.AddItem(new PushButtonData("ButtonImport", "Import eFramer", thisAssemblyPath, "EFRvt.ImportCommand")) as PushButton;
                        // Add tool tip 
                        button_import.ToolTip = "Import eFramer file to Revit";
                        Bitmap original = EFRvt.Properties.Resources.Import;
                        button_import.Image = button_import.LargeImage = ToBitmapImage(new Bitmap(original, new System.Drawing.Size(25, 25)));
                        #endregion

                        #region Add button to panel 
                        PushButton button_export = panel.AddItem(new PushButtonData("ButtonExport", "Export eFramer", thisAssemblyPath, "EFRvt.ExportCommand")) as PushButton;
                        // Add tool tip 
                        button_export.ToolTip = "Export Revit file to eFramer";
                        original = EFRvt.Properties.Resources.Export;
                        button_export.Image = button_export.LargeImage = ToBitmapImage(new Bitmap(original, new System.Drawing.Size(25, 25)));
                        #endregion

                        RibbonPanel panel2 = application.CreateRibbonPanel("Elibre Integration", "Levels Control");

                        #region Add button to panel 
                        PushButton button_Delete = panel2.AddItem(new PushButtonData("ButtonDeleteLevel", "Delete All Levels", thisAssemblyPath, "EFRvt.DeleteAllLevelsCommand")) as PushButton;
                        // Add tool tip 
                        button_Delete.ToolTip = "Delete all Existing Levels";
                        original = EFRvt.Properties.Resources.delete;
                        button_Delete.Image = button_Delete.LargeImage = ToBitmapImage(new Bitmap(original, new System.Drawing.Size(25, 25)));
                        #endregion

                        #region Add button to panel 
                        PushButton button_ReGenLevel = panel2.AddItem(new PushButtonData("ButtonRegenLevel", "Generate Levels", thisAssemblyPath, "EFRvt.GenerateLevelsCommand")) as PushButton;
                        // Add tool tip 
                        button_ReGenLevel.ToolTip = "Regenerate Levels with elements heights values";
                        original = EFRvt.Properties.Resources.reload;
                        button_ReGenLevel.Image = button_ReGenLevel.LargeImage = ToBitmapImage(new Bitmap(original, new System.Drawing.Size(25, 25)));
                        #endregion

                        //#region Add button to panel 
                        ////RibbonPanel efx_panel = application.CreateRibbonPanel("Elibre Integration", "Import / Export");

                        //PushButton efx_import = panel.AddItem(new PushButtonData("ButtonImportt2", "Import eFx", thisAssemblyPath, "EFRvt.EfxImportCommand")) as PushButton;
                        //// export efx Scema
                        //efx_import.ToolTip = "Import EFX Schema file to Revit";
                        //Bitmap import_bitmap = EFRvtNameSpace.Properties.Resources.Import;
                        //efx_import.Image = efx_import.LargeImage = ToBitmapImage(new Bitmap(import_bitmap, new System.Drawing.Size(25, 25)));
                        //#endregion

                        //#region Add button to panel 
                        ////RibbonPanel efx_panel = application.CreateRibbonPanel("Elibre Integration", "Import / Export");

                        //PushButton efx_export = panel.AddItem(new PushButtonData("ButtonExport2", "Export eFx", thisAssemblyPath, "EFRvt.EfxExportCommand")) as PushButton;
                        //// export efx Scema
                        //efx_export.ToolTip = "Export EFX Schema file to Revit";
                        //Bitmap export_bitmap = EFRvtNameSpace.Properties.Resources.Export;
                        //efx_export.Image = efx_export.LargeImage = ToBitmapImage(new Bitmap(export_bitmap, new System.Drawing.Size(25, 25)));
                        //#endregion

                        #region Trigger
                        ElementClassFilter filter = new ElementClassFilter(
                            typeof(FamilyInstance));

                        ChangingUpdater ChangingUpdater = new ChangingUpdater(application.ActiveAddInId);

                        UpdaterRegistry.RegisterUpdater(
                          ChangingUpdater);

                        UpdaterRegistry.AddTrigger(
                          ChangingUpdater.GetUpdaterId(), filter,
                          Element.GetChangeTypeElementDeletion());

                        #endregion


                        //PushButton button_export = panel.AddItem(new PushButtonData("ButtonExport", "Export eFramer", thisAssemblyPath, "EFRvt.ExportCommand")) as PushButton;
                        //// Add tool tip 
                        //button_export.ToolTip = "Export Revit file to eFramer";
                        //original = EFRvtNameSpace.Properties.Resources.Export;
                        //button_export.Image = button_export.LargeImage = ToBitmapImage(new Bitmap(original, new System.Drawing.Size(25, 25)));

                        // Reflection of path to image 

                        //    var img =
                        //    (BitmapImage)Imaging.CreateBitmapSourceFromHBitmap(
                        //EFRvtNameSpace.Properties.Resources.Import.GetHbitmap(),
                        //IntPtr.Zero,
                        //Int32Rect.Empty,
                        //BitmapSizeOptions.FromEmptyOptions());

                        //    BitmapImage largeImage = img as BitmapImage;
                        //    // Apply image to button 
                        //    button_import.LargeImage = largeImage;

                        //UIInit appUI = new UIInit(application, rb); // Create SS GUI
                    }


                    // UIInit uiLoadData = new UIInit(application, rb); // Create SS GUI

                    // application.ControlledApplication.
                    // uiInit.CreateUpdaters();

                    application.ControlledApplication.DocumentOpened += new EventHandler<DocumentOpenedEventArgs>(docOpen);
                    application.ControlledApplication.DocumentCreated += new EventHandler<DocumentCreatedEventArgs>(docCreate);
                    application.ControlledApplication.DocumentClosing += new EventHandler<DocumentClosingEventArgs>(ControlledApplication_DocumentClosing);

                    CreateErrorHandelr();
                    return Result.Succeeded;
                }

                TaskDialog.Show("eframing", "error. " + Ext_DllPath);

                return Autodesk.Revit.UI.Result.Failed;

            }
            catch (Exception ex)
            {
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
                //Util.SaveErrors(ex);
                return Result.Failed;
            }
        }

        private BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        async void ControlledApplication_DocumentClosing(object sender, DocumentClosingEventArgs e)
        {
            try
            {
                //Autodesk.Revit.ApplicationServices.Application app = sender as Autodesk.Revit.ApplicationServices.Application;

            }
            catch (Exception ex)
            {
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
                // Util.SaveErrors(ex);
            }

        }

        #region Event
        private async void docOpen(object sender, DocumentOpenedEventArgs e)
        {
            try
            {
                Autodesk.Revit.ApplicationServices.Application app = sender as Autodesk.Revit.ApplicationServices.Application;
                //  checkitems(app);

                checkFileExist(e.Document, app);

            }
            catch (Exception ex)
            {
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
                //Util.SaveErrors(ex);
            }
        }




        private async void docCreate(object sender, DocumentCreatedEventArgs e)
        {

            try
            {
                //Autodesk.Revit.ApplicationServices.Application app = sender as Autodesk.Revit.ApplicationServices.Application;



                // loadFamilys(app);
                //               addDocumentManager(app);

            }
            catch (Exception ex)
            {
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
                // Util.SaveErrors(ex);
            }
        }

        private void checkFileExist(Document doc, Autodesk.Revit.ApplicationServices.Application app)
        {
            try
            {
                FileInfo fInfo = new FileInfo(doc.PathName);
                string fileName = fInfo.Name.Replace(fInfo.Extension, "");
                string efFilePath = fInfo.FullName.Replace(fInfo.Name, "");
#if REVIT2020
                efFilePath += fileName + ".EFRvt020";


#elif REVIT2021
                 efFilePath += fileName + ".EFRvt021"; 
#elif REVIT2022
                 efFilePath += fileName + ".EFRvt022"; 
#elif REVIT2023
                 efFilePath += fileName + ".EFRvt023"; 
#else
#endif
                if (File.Exists(efFilePath))
                {
                    Events.m_doc = doc;

                    //if (Events.= null)


                     Events.StartImportRvt(efFilePath);

                    //Events.Initialize();
                    // Events.ReName(efFilePath);




                }
            }

            catch (Exception ex)
            {
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
            }
        }




        protected void CreateErrorHandelr()
        {
            //try
            //{
            //    SSErrorHandling errorHandel = new SSErrorHandling();
            //    Util.CurrentActiveErrorHandel = errorHandel;
            //}
            //catch
            //{

            //}
        }

        public RibbonPanel ribbonPanel(UIControlledApplication a)
        {
            // Tab name 
            string tab = "EFRvt";
            // Empty ribbon panel 
            RibbonPanel ribbonPanel = null;
            // Try to create ribbon tab. 
            try
            {
                //a.CreateRibbonPanel("My Test Tools");
                a.CreateRibbonTab(tab);
            }
            catch { }
            // Try to create ribbon panel. 
            try
            {
                RibbonPanel panel = a.CreateRibbonPanel(tab, "eFramer Revit");
            }
            catch { }
            // Search existing tab for your panel. 
            List<RibbonPanel> panels = a.GetRibbonPanels(tab);
            foreach (RibbonPanel p in panels)
            {
                if (p.Name == "eFramer Revit")
                {
                    ribbonPanel = p;
                    ribbonPanel.Title = "";
                }
            }
            //return panel 
            return ribbonPanel;
        }
        #region Init file pathes

        public static string AppAddInPath = "";
        public static string Ext_addinName = "EFRvt.addin";
        public static string dllName = "EFRvt.dll";
        public static string Ext_DllPath = "";
        public static string assemplyPath = "";
        private bool ReadAddonManifest()
        {
            try
            {
                string dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                assemplyPath = dir;
                Ext_DllPath = assemplyPath + "\\" + dllName;
                AppAddInPath = assemplyPath;
                return true;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("EFraming", "4. " + ex.Message);
                return false;
            }

        }



        #endregion
    }

    //internal class UIInit
    //{
    //    readonly string _imageFolder;
    //    const string _imageFolderName = "Images";
    //    private UIControlledApplication application;
    //    private RibbonPanel rb;

    //    public UIInit(UIControlledApplication _application, RibbonPanel _rb)
    //    {
    //        application = _application;
    //        rb = _rb;
    //        _imageFolder = FindFolderInParents(MainApplicationCommand.assemplyPath, _imageFolderName);
    //        PushButtonData pushBData = new PushButtonData("Import", "EF Impot", MainApplicationCommand.Ext_DllPath, "EFramingFamilyRevit.Command");
    //        pushBData.ToolTip = "Import EFRvt File";
    //        PushButton pushButton = rb.AddItem(pushBData) as PushButton;
    //        // bottonImage = NewBitmapImage("C.ico");//Utils.getBitmap2BitmapImage(global::EnrColumn.Res.Geometry);
    //        if (pushButton != null) pushButton.LargeImage = NewBitmapImage("import.ico");
    //    }

    //    public BitmapImage NewBitmapImage(string imageName)
    //    {
    //        return new BitmapImage(new Uri(Path.Combine(_imageFolder, imageName)));
    //    }
    //    public static string FindFolderInParents(string path, string target)
    //    {
    //        //  "expected an existing directory to start search in");
    //        try
    //        {
    //            string s;

    //            do
    //            {
    //                s = Path.Combine(path, target);
    //                if (Directory.Exists(s))
    //                {
    //                    return s;
    //                }
    //                path = Path.GetDirectoryName(path);
    //            } while (null != path);
    //        }
    //        catch (Exception ex)
    //        {
    //            Elibre.Net.Debug.ErrorHandler.ReportException(ex);
    //            // TaskDialog.Show("FindFolderInParents", ex.Message);
    //        }
    //        return null;
    //    }

    //}

    public class ChangingUpdater : IUpdater
    {
        private AddInId _appId;
        private FailureDefinitionId _failureId;
        private UpdaterId _updaterId;

        public ChangingUpdater(AddInId addInId)
        {
            _appId = addInId;

            _updaterId = new UpdaterId(_appId, new Guid(
              "6f453eba-4b9a-40df-b637-eb72a9ebf198"));

            _failureId = new FailureDefinitionId(
              new Guid("33ba8315-e931-493f-af92-4f417b6ccf71"));

            FailureDefinition failureDefinition
              = FailureDefinition.CreateFailureDefinition(
                _failureId, FailureSeverity.Error,
                "PreventDeletion: sorry, this element cannot be deleted.");
        }

        public void Execute(UpdaterData data)
        {
            Document doc = data.GetDocument();
            foreach (ElementId id in data.GetDeletedElementIds())
            {
                if (EfxExportCommand.IsProtected(id))
                {
                    FailureMessage failureMessage
                      = new FailureMessage(_failureId);

                    failureMessage.SetFailingElement(id);
                    doc.PostFailure(failureMessage);
                }
            }
        }

        public string GetAdditionalInformation()
        {
            return " This instance Belong To Group ";
        }

        public ChangePriority GetChangePriority()
        {
            return ChangePriority.Structure;
        }

        public UpdaterId GetUpdaterId()
        {
            return _updaterId;
        }

        public string GetUpdaterName()
        {
            return "Changing Updater";
        }
    }
}

#endregion