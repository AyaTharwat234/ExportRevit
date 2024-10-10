

using Autodesk.Revit.DB;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace EFRvt
{
    public class Events
    {
        public static Autodesk.Revit.DB.Document m_doc = null;
        public static List<WallType> m_efWallType = new List<WallType>();
        public static SortedList levels = new SortedList();
        public static WallType efWallType;
        public static double tolerance = 0.25;
        public static string AppAddInPath
        {
            get
            {
                return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
        }
        public static string m_2X4_name = "EF - 2x4";
        public static double m_default_2X4_width = 3.5 / 12.0;
        public static WallType defaultwType = null;
        internal static bool showGUI;


        public static bool Initialize()
        {
            try
            {
                defaultwType = null;
                m_efWallType = new List<WallType>();
                levels = new SortedList();
                FilteredElementIterator i = new FilteredElementCollector(m_doc).OfClass(typeof(Level)).GetElementIterator();
                i.Reset();
                while (i.MoveNext())
                {
                    //add level to list
                    Level level = i.Current as Level;
                    if (null != level)
                    {
                        levels.Add(level.Elevation, level);

                    }
                }


                m_efWallType = new List<WallType>();
                defaultwType = null;
                FilteredElementCollector collector = new FilteredElementCollector(m_doc);
                ICollection<Element> collection = collector.OfClass(typeof(WallType)).ToElements();
                foreach (Element e in collection)
                {
                    WallType tmpType = e as WallType;
                    if (tmpType?.Kind == WallKind.Basic)
                    {
                        if (defaultwType == null)
                            defaultwType = tmpType;


                        if (tmpType.Name == m_2X4_name)
                        {
                            m_efWallType.Add(tmpType);
                            defaultwType = tmpType;
                            // break;
                        }
                        else if (tmpType.Name.Contains("EF"))
                        {
                            m_efWallType.Add(tmpType);
                        }

                    }
                }


            }
            catch (Exception ex)
            {
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="width">inch</param>
        /// <param name="depth">inch</param>
        /// <returns></returns>
        internal static string getWallTypeName(float width, float depth)
        {
            return "EF - " + Math.Ceiling(width) + "X" + Math.Ceiling(depth);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="width">feet</param>
        /// <param name="oldType"></param>
        /// <param name="bCreateTransaction"></param>
        /// <returns></returns>
        public static WallType getEFWallType(string name, double width, WallType oldType, bool bCreateTransaction)
        {
            try
            {
                Autodesk.Revit.DB.Document doc = null;
                foreach (WallType wt in m_efWallType)
                {
                    doc = wt.Document;
                    if (wt.Name.Contains(name) || wt.Name.ToLower().Equals(name.ToLower()))
                        return wt;
                }

                WallType wType = CreateNewWallSectionType(oldType, name, width, bCreateTransaction);
                if (wType != null)
                    m_efWallType.Add(wType);

                return wType;
            }
            catch (Exception ex)
            {
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
                // Util.SaveErrors(ex);
                return null;
            }
        }
        internal static string cahngeExt(string fileName)
        {
            try
            {
                string strFileName = fileName.Replace(".EFRvtAuto", ".EFRvt");
                return strFileName;
            }
            catch (Exception ex)
            {
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
                return fileName;
            }
        }

        internal static void ReName(string efFilePath)
        {
            try
            {
                if (File.Exists(efFilePath))
                {
                    // Delete the existing file if exists
                    string newFileName = cahngeExt(efFilePath);
                    if (File.Exists(newFileName))
                        File.Delete(newFileName);
                    File.Move(efFilePath, newFileName); // Rename the oldFileName into newFileName
                    if (File.Exists(efFilePath))
                        File.Delete(efFilePath);
                }
            }
            catch (Exception ex)
            {
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
            }
        }

        private static WallType CreateNewWallSectionType(WallType oldWallType, string secName, double secWidth, bool startNewTransAction = true)
        {

            secName = addPrefixSecName(secName);

            TransactionManager trans = null;
            SubTransaction subTrans = null;
            WallType newWallType = null;
            Autodesk.Revit.DB.Document revitDoc = oldWallType.Document;
            try
            {
                if (startNewTransAction)
                {
                    trans = new TransactionManager(revitDoc, "Double Wall Type");
                    trans.Start();
                }
                else
                {
                    subTrans = new SubTransaction(revitDoc);
                    subTrans.Start();
                }

                newWallType = oldWallType.Duplicate(secName) as WallType;

                m_efWallType.Add(newWallType);

                if (newWallType != null)
                {
                    CompoundStructure cs = newWallType.GetCompoundStructure();
                    int nCount = cs.LayerCount;

                    if (nCount > 0)
                        cs.SetLayerWidth(0, secWidth);

                    newWallType.SetCompoundStructure(cs);
                }

                if (startNewTransAction)
                    trans.Commit();
                else
                    subTrans.Commit();

                return newWallType;
            }
            catch (Exception ex)
            {
                subTrans?.Commit();

                trans?.Commit();

                // Util.SaveErrors(ex);
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
                return null;
            }
        }

        public static bool DeleteRevitElement(List<ElementId> elementIds, Autodesk.Revit.DB.Document doc, bool bCreateNewTransaction = true)
        {
            //check the element Exist first
            int n = elementIds.Count;
            if (n == 0)
                return true;
            for (int i = n - 1; i >= 0; i--)
            {
                if (elementIds[i] == null)
                    elementIds.RemoveAt(i);
                else
                {
                    Element element = doc.GetElement(elementIds[i]);
                    if (element == null)
                    {
                        elementIds.RemoveAt(i);
                    }
                }
            }

            TransactionManager tran = null;
            if (bCreateNewTransaction)
                tran = new TransactionManager(doc, "Delete Elements");
            if (tran != null)
                tran.Start();
            try
            {
                doc.Delete(elementIds);
                if (tran != null)
                    tran.Commit();
                return true;
            }
            catch
            {
                if (tran != null)
                    tran.RollBack();
                return false;
            }
        }

        private static string addPrefixSecName(string secName)
        {
            if (secName != null)
            {
                if (!secName.Contains("EF"))
                {
                    return "EF - " + secName;
                }
            }
            return secName;
        }
        public async static void ImportRvtBuilding(string fileName)
        {
            try
            {
                XmlSerializer xmlserializer = new XmlSerializer(typeof(MapBuilding));
                StreamReader fs = new StreamReader(fileName);
                MapBuilding mapBuilding = xmlserializer.Deserialize(fs) as MapBuilding;

                if (mapBuilding != null)
                {
                    mapBuilding.PrepareRunTimeFamiles(m_doc);
                    View3D view3d = null;
                    using (Transaction tran = new Transaction(m_doc, "NewView3D"))
                    {
                        tran.Start();
                        try
                        {
                            Events.m_doc.Delete(ExtensionMethods.GetSortedLevels(Events.m_doc).Select(x => x.Id).ToArray());
                        }
                        catch { }
                        mapBuilding.SetData();
                        //if (m_UIApplication != null)
                        //{ 
                        //    //m_UIApplication.PostCommand(RevitCommandId.LookupPostableCommandId(PostableCommand.RoofByFace));
                        //}
                        // Find a 3D view type
                        IEnumerable<ViewFamilyType> viewFamilyTypes = from elem in new FilteredElementCollector(m_doc).OfClass(typeof(ViewFamilyType))
                                                                      let type = elem as ViewFamilyType
                                                                      where type.ViewFamily == ViewFamily.ThreeDimensional
                                                                      select type;

                        // Create a new Perspective View3D
                        View3D view3D = View3D.CreateIsometric(m_doc, viewFamilyTypes.First().Id);
                        if (null != view3D)
                        {
                            // By default, the 3D view uses a default orientation.
                            // Change the orientation by creating and setting a ViewOrientation3D
                            XYZ eye = new XYZ(0, -100, 10);
                            XYZ up = new XYZ(0, 0, 1);
                            XYZ forward = new XYZ(0, 1, 0);
                            ViewOrientation3D vo = new ViewOrientation3D(eye, up, forward);
                            view3D.SetOrientation(vo);

                        }

                        FailureHandlingOptions failopt = tran.GetFailureHandlingOptions();
                        failopt.SetFailuresPreprocessor(new RevitHandler());
                        tran.SetFailureHandlingOptions(failopt);

                        tran.Commit();
                    }

                }


            }
            catch (Exception ex)
            {
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
            }
        }

        public async static void ImportRvtShellBuilding(string fileName)
        {
            try
            {
                XmlSerializer xmlserializer = new XmlSerializer(typeof(MapShellBuilding));
                StreamReader fs = new StreamReader(fileName);
                MapShellBuilding mapBuilding = xmlserializer.Deserialize(fs) as MapShellBuilding;

                if (mapBuilding != null)
                {
                    mapBuilding.PrepareRunTimeFamiles(m_doc);
                    View3D view3d = null;
                    using (Transaction tran = new Transaction(m_doc, "NewView3D"))
                    {
                        tran.Start();
                        try
                        {
                            Events.m_doc.Delete(ExtensionMethods.GetSortedLevels(Events.m_doc).Select(x => x.Id).ToArray());
                        }
                        catch { }
                        mapBuilding.SetData();

                        IEnumerable<ViewFamilyType> viewFamilyTypes = from elem in new FilteredElementCollector(m_doc).OfClass(typeof(ViewFamilyType))
                                                                      let type = elem as ViewFamilyType
                                                                      where type.ViewFamily == ViewFamily.ThreeDimensional
                                                                      select type;

                        // Create a new Perspective View3D
                        View3D view3D = View3D.CreateIsometric(m_doc, viewFamilyTypes.First().Id);
                        if (null != view3D)
                        {
                            // By default, the 3D view uses a default orientation.
                            // Change the orientation by creating and setting a ViewOrientation3D
                            XYZ eye = new XYZ(0, -100, 10);
                            XYZ up = new XYZ(0, 0, 1);
                            XYZ forward = new XYZ(0, 1, 0);
                            ViewOrientation3D vo = new ViewOrientation3D(eye, up, forward);
                            view3D.SetOrientation(vo);

                        }

                        FailureHandlingOptions failopt = tran.GetFailureHandlingOptions();
                        failopt.SetFailuresPreprocessor(new RevitHandler());
                        tran.SetFailureHandlingOptions(failopt);

                        tran.Commit();
                    }

                }


            }
            catch (Exception ex)
            {
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
            }
        }
        public enum Models
        {
            MapShellBuilding,
            MapBuilding
        }
        public static Models GetType(string fileName)
        {
            string content = File.ReadAllText(fileName);
            if (content.Contains("MapShellBuilding"))
                return Models.MapShellBuilding;
            else if (content.Contains("Building"))
                return Models.MapBuilding;
            else
            {
                throw new Exception("Invalid model type");
            }
        }
        public static Models GetModelType(string xml)
        {
            XmlDocument xDoc = new XmlDocument();

            xDoc.LoadXml(xml.Substring(xml.IndexOf(Environment.NewLine)));
            XmlNode root = xDoc.DocumentElement;
            if (root.Name.Equals("MapShellBuilding"))
            {
                return Models.MapShellBuilding;
            }
            else if (root.Name.Equals("Building"))
            {
                return Models.MapBuilding;
            }
            else
            {
                throw new Exception("Invalid model type");
            }
        }


        public async static void StartImportRvt(string fileName)
        {
            try
            {
                Models model = GetType(fileName);
                if (model == Models.MapShellBuilding)
                {
                    ImportRvtShellBuilding(fileName);
                }
                else if (model == Models.MapBuilding)
                {
                    ImportRvtBuilding(fileName);
                }
            }

            catch (Exception ex)
            {
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
            }
        }

        public static View3D Get3dView()
        {
            FilteredElementCollector collector
              = new FilteredElementCollector(m_doc)
                .OfClass(typeof(View3D));

            foreach (View3D v in collector.Cast<View3D>())
            {


                if (!v.IsTemplate)
                {
                    return v;
                }
            }
            return null;
        }

    }

    public enum TransMode
    {
        Normal,
        SubTrans,
        GroupTrans
    }
    public class TransactionManager : IDisposable
    {
        readonly Transaction trans;
        readonly TransactionGroup transGroup;
        readonly SubTransaction subTrans;
        readonly TransMode mode;
        protected FailureHandler m_preproccessor = new FailureHandler();

        public TransactionManager(Autodesk.Revit.DB.Document doc, string name)
        {
            mode = TransMode.Normal;

            trans = new Transaction(doc, name);
            FailureHandlingOptions options = trans.GetFailureHandlingOptions();
            options.SetFailuresPreprocessor(m_preproccessor);
            trans.SetFailureHandlingOptions(options);
        }
        public TransactionManager(Autodesk.Revit.DB.Document doc, bool createNew, TransMode _mode = TransMode.Normal, string name = "newTrans")
        {
            mode = _mode;
            switch (mode)
            {
                case TransMode.Normal:
                    if (doc != null && createNew)
                    {
                        trans = new Transaction(doc, name);
                        FailureHandlingOptions options = trans.GetFailureHandlingOptions();
                        options.SetFailuresPreprocessor(m_preproccessor);
                        trans.SetFailureHandlingOptions(options);
                    }
                    break;
                case TransMode.GroupTrans:
                    if (doc != null && createNew)
                        transGroup = new TransactionGroup(doc, "transGroup");

                    break;
                case TransMode.SubTrans:
                    if (doc != null && createNew)
                        subTrans = new SubTransaction(doc);
                    break;
            }

        }

        public TransactionStatus Start(string name = "new")
        {
            // if (trans != null)
            //   return  trans.Start(name);
            // if (transGroup != null)
            //   return transGroup.Start(name);

            switch (mode)
            {
                case TransMode.Normal:
                    if (trans != null)
                        return trans.Start(name);
                    break;
                case TransMode.GroupTrans:
                    if (transGroup != null)
                        return transGroup.Start(name);
                    break;
                case TransMode.SubTrans:
                    if (subTrans != null)
                        return subTrans.Start();
                    break;

            }
            return TransactionStatus.Uninitialized;
        }

        public TransactionStatus Commit()
        {
            switch (mode)
            {
                case TransMode.Normal:
                    if (trans != null)
                    {
                        TransactionStatus status = trans.Commit();
                        if (m_preproccessor.ErrorSeverity != FailureSeverity.None)
                            return TransactionStatus.Error;
                        else
                            return status;
                    }

                    break;

                case TransMode.GroupTrans:
                    if (transGroup != null)
                        return transGroup.Commit();
                    break;

                case TransMode.SubTrans:
                    if (subTrans != null)
                        return subTrans.Commit();
                    break;

            }
            return TransactionStatus.Uninitialized;
        }

        public TransactionStatus RollBack()
        {
            switch (mode)
            {
                case TransMode.Normal:
                    if (trans != null)
                        return trans.RollBack();
                    break;

                case TransMode.GroupTrans:
                    if (transGroup != null)
                        return transGroup.RollBack();
                    break;
                case TransMode.SubTrans:
                    if (subTrans != null)
                        return subTrans.RollBack();
                    break;

            }
            return TransactionStatus.Uninitialized;
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }
    }

    #region Error handaler
    public class ErrorHandling
    {
        #region Members
        /// <FD ID Warning>
        /// The failure definition id for warning
        /// </summary>
        private FailureDefinitionId m_failureDefinitionIdWarning;
        /// <FD ID Error>
        /// The failure definition id for error
        /// </summary>
        private FailureDefinitionId m_failureDefinitionIdError;
        /// <FD Warning>
        /// The failure definition for warning
        /// </summary>
        private FailureDefinition m_fdWarning;
        /// <FD Error>
        /// The failure definition for error
        /// </summary>
        private FailureDefinition m_fdError;
        /// <Application>
        /// The Revit application
        /// </summary>
        //private Autodesk.Revit.ApplicationServices.Application m_revitApp;//Clean UP
        /// <Active Document>
        /// The active document
        /// </summary>
        private Autodesk.Revit.DB.Document m_revitDoc;
        #endregion

        #region Prop.
        public FailureDefinitionId failureDefinitionIdWarning
        {
            get { return m_failureDefinitionIdWarning; }
        }
        public FailureDefinitionId failureDefinitionIdError
        {
            get { return m_failureDefinitionIdError; }
        }
        public FailureDefinition failureDefinitionWarning
        {
            get { return m_fdWarning; }
        }
        public FailureDefinition failureDefinitionError
        {
            get { return m_fdError; }
        }
        #endregion

        #region Constructor(s)

        public ErrorHandling()
        {
            //m_revitDoc = doc;
            initiateDefinitions();
        }
        #endregion

        public void initiateDefinitions()
        {
            try
            {
                // Create failure definition Ids
                Guid guid1 = Guid.NewGuid();       //new Guid("0C3F66B5-3E26-4d24-A228-7A8358C76D39");
                Guid guid2 = Guid.NewGuid();       //new Guid("93382A45-89A9-4cfe-8B94-E0B0D9542D34");
                //Guid guid3 = Guid.NewGuid();       //new Guid("A16D08E2-7D06-4bca-96B0-C4E4CC0512F8");

                m_failureDefinitionIdWarning = new FailureDefinitionId(guid1);
                m_failureDefinitionIdError = new FailureDefinitionId(guid2);

                // Create failure definitions and add resolutions
                m_fdWarning = FailureDefinition.CreateFailureDefinition(m_failureDefinitionIdWarning, FailureSeverity.Warning, "I am the warning.");
                m_fdError = FailureDefinition.CreateFailureDefinition(m_failureDefinitionIdError, FailureSeverity.Error, "I am the error");

                m_fdWarning.AddResolutionType(FailureResolutionType.MoveElements, "MoveElements", typeof(DeleteElements));
                m_fdWarning.AddResolutionType(FailureResolutionType.DeleteElements, "DeleteElements", typeof(DeleteElements));
                m_fdWarning.AddResolutionType(FailureResolutionType.CreateElements, "DeleteElements", typeof(DeleteElements));
                m_fdWarning.SetDefaultResolutionType(FailureResolutionType.DeleteElements);

                m_fdError.AddResolutionType(FailureResolutionType.DetachElements, "DetachElements", typeof(DeleteElements));
                m_fdError.AddResolutionType(FailureResolutionType.DeleteElements, "DeleteElements", typeof(DeleteElements));
                m_fdError.SetDefaultResolutionType(FailureResolutionType.DeleteElements);
            }
            catch (Exception ex)
            {
                //  Util.SaveErrors(ex);
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
            }
        }

        public void SetDocument(Autodesk.Revit.DB.Document _revitDoc)
        {
            m_revitDoc = _revitDoc;
        }
    }

    public class FailureHandler : IFailuresPreprocessor
    {
        public string ErrorMessage { set; get; }
        public FailureSeverity ErrorSeverity { set; get; }

        public FailureHandler()
        {
            ErrorMessage = "";
            ErrorSeverity = FailureSeverity.None;
        }

        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            IList<FailureMessageAccessor> failureMessages = failuresAccessor.GetFailureMessages();

            foreach (FailureMessageAccessor failureMessageAccessor in failureMessages)
            {
                // We're just deleting all of the warning level 
                // failures and rolling back any others

                failureMessageAccessor.GetFailureDefinitionId();

                try
                {
                    ErrorMessage = failureMessageAccessor.GetDescriptionText();
                }
                catch
                {
                    ErrorMessage = "Unknown Error";
                }

                try
                {
                    ErrorSeverity = failureMessageAccessor.GetSeverity();

                    if (ErrorSeverity == FailureSeverity.Warning)
                    {
                        failuresAccessor.DeleteWarning(failureMessageAccessor);
                    }
                    else
                    {
                        // return FailureProcessingResult.ProceedWithRollBack;
                        List<ElementId> erroredElementIds = failureMessageAccessor.GetFailingElementIds().ToList();
                        failureMessageAccessor.GetAdditionalElementIds().ToList();


                        failureMessageAccessor.GetNumberOfResolutions();

                        failureMessageAccessor.HasResolutions();


                        failureMessageAccessor.GetDefaultResolutionCaption();

                        List<FailureResolutionType> AttemptedResolutionTypes = failuresAccessor.GetAttemptedResolutionTypes(failureMessageAccessor).ToList();
                        failuresAccessor.GetFailureHandlingOptions();

                        bool isPermitted = failuresAccessor.IsElementsDeletionPermitted(erroredElementIds);

                        if (isPermitted && AttemptedResolutionTypes.Count == 0)
                        {
                            // failuresAccessor.DeleteElements(erroredElementIds);

                            failuresAccessor.ResolveFailure(failureMessageAccessor);

                            return FailureProcessingResult.ProceedWithCommit;
                        }

                        //FailureResolutionType resoluteType = failureMessageAccessor.GetCurrentResolutionType();
                        //failureMessageAccessor.SetCurrentResolutionType(resoluteType);
                    }
                }
                catch (Exception ex)
                {
                    Elibre.Net.Debug.ErrorHandler.ReportException(ex);
                }
            }
            return FailureProcessingResult.Continue;
        }


    }
    public class FailuresProcessor : IFailuresProcessor
    {
        protected ErrorHandling m_errorHandeling = null;
        ///// <FailuresProcessor>
        ///// This method is being called in case of exception or document destruction to dismiss any possible pending failure UI that may have left on the screen 
        ///// </summary>
        ///// <param name="document">Document for which pending failures processing UI should be dismissed </param>
        ///// 

        public FailuresProcessor(ErrorHandling _errorHandel)
        {
            m_errorHandeling = _errorHandel;
        }

        public void Dismiss(Autodesk.Revit.DB.Document document)
        {
        }

        /// <ProcessFailures>
        /// Method that Revit will invoke to process failures at the end of transaction. 
        /// </summary>
        /// <param name="failuresAccessor">Provides all necessary data to perform the resolution of failures.</param>
        /// <returns></returns>
        public FailureProcessingResult ProcessFailures(FailuresAccessor failuresAccessor)
        {
            IList<FailureMessageAccessor> fmas = failuresAccessor.GetFailureMessages();
            if (fmas.Count == 0)
            {
                return FailureProcessingResult.Continue;
            }

            String transactionName = failuresAccessor.GetTransactionName();
            if (transactionName.Equals("Error_FailuresProcessor"))
            {
                foreach (FailureMessageAccessor fma in fmas)
                {
                    FailureDefinitionId id = fma.GetFailureDefinitionId();
                    if (m_errorHandeling != null && id == m_errorHandeling.failureDefinitionIdError)
                    {
                        failuresAccessor.ResolveFailure(fma);
                    }
                }
                return FailureProcessingResult.ProceedWithCommit;
            }
            else
            {
                return FailureProcessingResult.Continue;
            }
        }
    }

    #endregion
}
