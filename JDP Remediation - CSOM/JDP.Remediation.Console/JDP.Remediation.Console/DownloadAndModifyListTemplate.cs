﻿using JDP.Remediation.Console.Common.Base;
using JDP.Remediation.Console.Common.CSV;
using JDP.Remediation.Console.Common.Utilities;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace JDP.Remediation.Console
{
    public class DownloadAndModifyListTemplate
    {
        public static string filePath = string.Empty;
        public static string outputPath = string.Empty;
        public static List<string> lstContentTypeIDs;
        public static List<string> lstCustomFieldIDs;
        public static List<string> lstCustomErs;

        public static void DoWork()
        {
            try
            {
                bool processInputFile = false;
                bool processFarm = false;
                bool processSiteCollections = false;
                string webApplicationUrl = string.Empty;
                string ListTemplateInputFile = string.Empty;
                string processOption = string.Empty;
                string siteCollectionUrlsList = string.Empty;
                string[] siteCollectionUrls = null;

                try
                {
                    //Output files
                    outputPath = Environment.CurrentDirectory;

                    //Trace Log TXT File Creation Command
                    Logger.OpenLog("DownloadAndModifyListTemplate");
                    Logger.LogInfoMessage("[DownloadAndModifyListTemplate: DoWork] Logger and Exception files will be available in path: " + outputPath);

                    //User Options
                    if (!ReadInputOptions(ref processInputFile, ref processFarm, ref processSiteCollections))
                    {
                        System.Console.ForegroundColor = System.ConsoleColor.Red;
                        Logger.LogInfoMessage("Invalid option selected. Operation aborted!");
                        System.Console.ResetColor();
                        return;
                    }

                    //Web Application Urls [If option 1 Selected]
                    if (processFarm)
                    {
                        if (!ReadWebApplication(ref webApplicationUrl))
                        {
                            System.Console.ForegroundColor = System.ConsoleColor.Red;
                            Logger.LogInfoMessage("WebApplicationUrl is not valid. So, Operation aborted!");
                            System.Console.ResetColor();
                            return;
                        }
                    }

                    //SiteCollection Urls separated by comma(,) -  [If option 2 Selected]
                    if (processSiteCollections)
                    {
                        if (!ReadSiteCollectionList(ref siteCollectionUrlsList))
                        {
                            System.Console.ForegroundColor = System.ConsoleColor.Red;
                            Logger.LogInfoMessage("SiteCollectionUrls is not valid. So, Operation aborted!");
                            System.Console.ResetColor();
                            return;
                        }
                        siteCollectionUrls = siteCollectionUrlsList.Split(',');
                    }

                    //List Template CSV Path [If option 3 Selected]
                    if (processInputFile)
                    {
                        if (!ReadInputFile(ref ListTemplateInputFile))
                        {
                            System.Console.ForegroundColor = System.ConsoleColor.Red;
                            Logger.LogInfoMessage("ListTemplate input file is not valid or available. So, Operation aborted!");
                            Logger.LogInfoMessage("Please enter path like: E.g. C:\\<Working Directory>\\<InputFile>.csv");
                            System.Console.ResetColor();
                            return;
                        }
                    }

                    //Validating Input File Path/Folder
                    if (processInputFile || processFarm || processSiteCollections)
                    {
                        //Input Files Path: EventReceivers.csv, ContentTypes.csv and CustomFields.csv
                        if (!ReadInputFilesPath())
                        {
                            System.Console.ForegroundColor = System.ConsoleColor.Red;
                            Logger.LogInfoMessage("Input files directory is not valid. So, Operation aborted!");
                            System.Console.ResetColor();
                            return;
                        }
                        ReadInputFiles();
                    }

                    //Output File - Intermediate File, which will have info about Customization
                    string csvFileName = outputPath + @"\" + Constants.ListTemplateCustomizationUsage;
                    bool headerOfCsv = true;
                    List<ListTemplateFTCAnalysisOutputBase> lstMissingListTempaltesInGalleryBase = new List<ListTemplateFTCAnalysisOutputBase>();

                    if (!(lstContentTypeIDs != null && lstContentTypeIDs.Count > 0)
                        && !(lstCustomErs != null && lstCustomErs.Count > 0)
                        && !(lstCustomFieldIDs != null && lstCustomFieldIDs.Count > 0))
                    {
                        System.Console.ForegroundColor = System.ConsoleColor.Red;
                        Logger.LogInfoMessage("[DownloadAndModifyListTemplate: DoWork] No records present in input files (EventReceivers.csv, ContentTypes.csv & CustomFields.csv). So, Operation aborted!");
                        WriteOutputReport(null, csvFileName, ref headerOfCsv);
                        System.Console.ResetColor();
                        return;
                    }

                    #region ListTemplate Report based on  Input File
                    if (processInputFile)
                    {
                        if (System.IO.File.Exists(ListTemplateInputFile))
                        {
                            //Process InputFile for Custom ListTemplates
                            ProcessListTemplateInputFile(ListTemplateInputFile, ref lstMissingListTempaltesInGalleryBase);
                            WriteOutputReport(lstMissingListTempaltesInGalleryBase, csvFileName, ref headerOfCsv);
                        }
                        else
                        {
                            Logger.LogErrorMessage("[DownloadAndModifyListTemplate: DoWork]. Exception Message: List Template file " + filePath + " is not present", true);
                        }
                    }
                    #endregion

                    lstMissingListTempaltesInGalleryBase.Clear();

                    #region ListTemplate Report based on WebApplicationUrl
                    if (processFarm)
                    {
                        //Process WebApplicationUrl for Custom ListTemplates
                        ProcessWebApplicationUrl(webApplicationUrl, ref lstMissingListTempaltesInGalleryBase);
                        WriteOutputReport(lstMissingListTempaltesInGalleryBase, csvFileName, ref headerOfCsv);
                    }
                    #endregion

                    lstMissingListTempaltesInGalleryBase.Clear();

                    #region ListTemplate report based on SiteCollectionUrls list
                    if (processSiteCollections)
                    {
                        //Process SiteCollections for Custom ListTemplates
                        ProcessSiteCollectionUrlList(siteCollectionUrls, ref lstMissingListTempaltesInGalleryBase);
                        WriteOutputReport(lstMissingListTempaltesInGalleryBase, csvFileName, ref headerOfCsv);
                    }
                    #endregion

                    if (processFarm || processInputFile || processSiteCollections)
                    {
                        //Delete Downloaded ListTemplates files/folders
                        DeleteDownloadedListTemplates();

                        System.Console.ForegroundColor = System.ConsoleColor.Green;
                        Logger.LogSuccessMessage("[DownloadAndModifyListTemplate: DoWork] Successfully completed all ListTemplates and output file is present at the path: "
                            + csvFileName, true);
                        System.Console.ResetColor();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogErrorMessage("[DownloadAndModifyListTemplate: DoWork]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: DoWork]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
            }
            finally
            {
                filePath = null;
                outputPath = null;
                lstContentTypeIDs = null;
                lstCustomErs = null;
                lstCustomFieldIDs = null;
            }
            Logger.CloseLog();
        }

        public static bool DownloadListTemplate(string filePath, string ListGalleryPath, string ListTemplateName,
            string SiteCollection, string WebUrl)
        {
            Logger.LogInfoMessage("[DownloadAndModifyListTemplate: DownloadListTemplate] Downloading the List Template: " + ListTemplateName, true);
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            bool isDownloaded = false;

            try
            {
                using (ClientContext userContext = Helper.CreateAuthenticatedUserContext(Program.AdminDomain, Program.AdminUsername, Program.AdminPassword, SiteCollection))
                {
                    userContext.ExecuteQuery();

                    Site site = userContext.Site;
                    Web web = userContext.Web;
                    userContext.Load(site);
                    userContext.Load(web);
                    userContext.ExecuteQuery();

                    List lstTemplateGallery = site.GetCatalog(114);
                    userContext.Load(lstTemplateGallery);
                    userContext.Load(lstTemplateGallery.RootFolder);
                    userContext.ExecuteQuery();

                    string lstGalleryServerRelativeUrl = lstTemplateGallery.RootFolder.ServerRelativeUrl;

                    //To remove errors resulting from Eval Sites
                    if (lstGalleryServerRelativeUrl == ListGalleryPath && SiteCollection == WebUrl)
                    {
                        string fileUrl = ListGalleryPath + "/" + ListTemplateName; ///sites/EvalSitetesting-eval

                        FileInformation info = Microsoft.SharePoint.Client.File.OpenBinaryDirect(userContext, fileUrl);
                        string fileName = fileUrl.Substring(fileUrl.LastIndexOf("/") + 1);

                        var fileNamePath = Path.Combine(filePath, fileName);
                        using (var fileStream = System.IO.File.Create(fileNamePath))
                        {
                            info.Stream.CopyTo(fileStream);
                            isDownloaded = true;
                            Logger.LogInfoMessage("[DownloadAndModifyListTemplate: DownloadListTemplate] Successfully Downloaded List Template " + ListTemplateName, true);
                        }
                    }
                    else
                    {
                        Logger.LogInfoMessage("[DownloadAndModifyListTemplate: DownloadListTemplate] Download Failed for " + ListTemplateName + ". ListGalleryPath is not present in the current Site Collection: ", true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: DownloadListTemplate]. Exception Message: " + ex.Message + ", Exception Comments: ListGalleryPath is not present in the current Site Collection", true);
            }

            return isDownloaded;
        }

        public static bool ProcessStpFile(string filePath, string solFileName, ref ListTemplateFTCAnalysisOutputBase objListCustOutput)
        {
            bool isCustomizationPresent = false;
            bool isCustomContentType = false;
            bool isCustomEventReceiver = false;
            bool isCustomSiteColumn = false;
            string fileName = objListCustOutput.ListTemplateName;
            string downloadFolder = filePath + @"\" + Constants.DownloadPathListTemplates;

            Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessStpFile] Processing the List Template: " + objListCustOutput.ListTemplateName, true);
            try
            {
                string cabDir = string.Empty;
                string newFilePath = solFileName.Replace(".stp", ".cab");
                if (System.IO.File.Exists(newFilePath))
                    System.IO.File.Delete(newFilePath);
                System.IO.File.Move(solFileName, newFilePath);

                var destDir = newFilePath.Substring(0, newFilePath.LastIndexOf(@"\"));
                Directory.SetCurrentDirectory(destDir);
                string newFileName = newFilePath.Substring(newFilePath.LastIndexOf(@"\") + 1);

                FileInfo solFileObj = new FileInfo(newFileName);
                Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessStpFile] Extracting the List Template: " + objListCustOutput.ListTemplateName, true);
                string cmd = "/e /a /y /L \"" + newFileName.Replace(".", "_") + "\" \"" + newFileName + "\"";
                ProcessStartInfo pI = new ProcessStartInfo("extrac32.exe", cmd);
                Process p = Process.Start(pI);
                p.WaitForExit();

                if (!destDir.EndsWith(@"\"))
                    cabDir = destDir + @"\" + newFileName.Replace(".", "_");
                else
                    cabDir = destDir + newFileName.Replace(".", "_");
                Directory.SetCurrentDirectory(newFileName.Replace(".", "_"));
                Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessStpFile] Extracted the List Template: " + objListCustOutput.ListTemplateName + " in path: " + cabDir, true);

                string[] webTempFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "Manifest.xml", SearchOption.AllDirectories);
                XmlDocument xmlReceiver = new XmlDocument();
                string xmlString = webTempFiles[0];

                Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessStpFile] Searching for customized elements in: " + xmlString, true);
                var reader = new XmlTextReader(xmlString);

                reader.Namespaces = false;
                reader.Read();
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);

                //Initiallizing all the nodes required to check
                XmlNodeList receiverNodes = doc.SelectNodes("/ListTemplate/UserLists/List/MetaData/Receivers/Receiver");
                XmlNodeList xmlCTReceivers = doc.SelectNodes("/ListTemplate/UserLists/List/MetaData/ContentTypes/ContentType");
                XmlNodeList xmlFields = doc.SelectNodes("/ListTemplate/UserLists/List/MetaData/Fields/Field");

                #region Custom_EventReceivers
                //Checking for Customization
                Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessStpFile] Searching for customized List Event Receivers", true);
                if (receiverNodes != null && receiverNodes.Count > 0)
                {
                    if (lstCustomErs != null && lstCustomErs.Count > 0)
                    {
                        foreach (XmlNode node in receiverNodes)
                        {
                            try
                            {
                                string assemblyValue = node["Assembly"].InnerText;

                                if (lstCustomErs.Where(c => assemblyValue.Equals(c, StringComparison.CurrentCultureIgnoreCase)).Any())
                                {
                                    isCustomEventReceiver = true;
                                    Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessStpFile] Customized List Event Receiver Found for " + objListCustOutput.ListTemplateName, true);
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessStpFile]. Exception Message: " + ex.Message + ", Exception Comments: Exception while reading List Receivers tag", true);
                            }
                        }
                    }
                }
                #endregion

                Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessStpFile] Searching for customized Event Receivers associated with Content Types", true);

                if (xmlCTReceivers != null && xmlCTReceivers.Count > 0)
                {
                    #region Custom ContentTypeEventReceivers
                    //checking Event Receivers in Content Types
                    if (lstCustomErs != null && lstCustomErs.Count > 0 && !isCustomEventReceiver)
                    {
                        for (int i = 0; i < xmlCTReceivers.Count; i++)
                        {
                            try
                            {
                                var docList = xmlCTReceivers[i]["XmlDocuments"];
                                if (docList != null)
                                {
                                    XmlNodeList xmlDocList = docList.ChildNodes;

                                    for (int j = 0; j < xmlDocList.Count; j++)
                                    {
                                        try
                                        {
                                            var namespaceURl = xmlDocList[j].Attributes["NamespaceURI"].Value;
                                            if (namespaceURl.Contains("http://schemas.microsoft.com/sharepoint/events"))
                                            {
                                                byte[] data = Convert.FromBase64String(xmlDocList[j].InnerText);
                                                string decodedString = Encoding.UTF8.GetString(data);

                                                XmlDocument docReceivers = new XmlDocument();
                                                docReceivers.LoadXml(decodedString);

                                                XmlNamespaceManager nsmgr = new XmlNamespaceManager(docReceivers.NameTable);
                                                nsmgr.AddNamespace("spe", "http://schemas.microsoft.com/sharepoint/events");
                                                XmlNodeList receiverChilds = docReceivers.SelectNodes("/spe:Receivers/Receiver", nsmgr);

                                                if (receiverChilds != null && receiverChilds.Count > 0)
                                                {
                                                    for (int y = 0; y < receiverChilds.Count; y++)
                                                    {
                                                        try
                                                        {
                                                            string ctAssemblyValue = receiverChilds[y]["Assembly"].InnerText;
                                                            if (lstCustomErs.Where(c => ctAssemblyValue.Equals(c, StringComparison.CurrentCultureIgnoreCase)).Any())
                                                            {
                                                                isCustomEventReceiver = true;
                                                                Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessStpFile] Customized Event Receiver associated with Content Type Found for : " + objListCustOutput.ListTemplateName, true);
                                                                break;
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessStpFile]. Exception Message: " + ex.Message + ", Exception Comments: Exception while reading Receivers tag in Content Types", true);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessStpFile]. Exception Message: " + ex.Message + ", Exception Comments: Exception while reading Receivers tag in Content Types", true);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessStpFile]. Exception Message: " + ex.Message + ", Exception Comments: Exception while reading Receivers tag in Content Types", true);
                            }
                        }
                    }
                    #endregion

                    #region custom contenttypes
                    if (lstContentTypeIDs != null && lstContentTypeIDs.Count > 0)
                    {
                        //Iterate all ContentTypes in manifest.xml
                        Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessStpFile] Searching for customized Content Types", true);
                        for (int i = 0; i < xmlCTReceivers.Count; i++)
                        {
                            try
                            {
                                var docList = xmlCTReceivers[i].Attributes["ID"].Value;

                                //Remove contenttype tag if ContentTypeId present in custom ContentTypes file ContentTypes.csv
                                if (lstContentTypeIDs.Where(c => docList.StartsWith(c)).Any())
                                {
                                    isCustomContentType = true;
                                    Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessStpFile] Customized Content Type Found for: " + objListCustOutput.ListTemplateName, true);
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessStpFile]. Exception Message: " + ex.Message + ", Exception Comments: Exception while reading Content Types", true);
                            }
                        }
                    }
                    #endregion

                    #region CustomFields_In_Contenttyeps
                    if (lstCustomFieldIDs != null && lstCustomFieldIDs.Count > 0)
                    {
                        //Checking Site Columns presence in Content Types
                        Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessStpFile] Searching for customized Site Columns associated with Content Types", true);
                        for (int i = 0; i < xmlCTReceivers.Count; i++)
                        {
                            try
                            {
                                var fieldRefs = xmlCTReceivers[i]["FieldRefs"];
                                if (fieldRefs != null)
                                {
                                    XmlNodeList xmlFieldRefList = fieldRefs.ChildNodes;

                                    for (int j = 0; j < xmlFieldRefList.Count; j++)
                                    {
                                        try
                                        {
                                            string fieldRefId = xmlFieldRefList[j].Attributes["ID"].Value;
                                            if (lstCustomFieldIDs.Where(c => fieldRefId.Equals(c)).Any())
                                            {
                                                isCustomSiteColumn = true;
                                                Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessStpFile] Customized Site Column associated with Content Type Found for: " + objListCustOutput.ListTemplateName, true);
                                                break;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessStpFile]. Exception Message: " + ex.Message + ", Exception Comments: Exception while reading Site Columns tag in Content Types", true);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessStpFile]. Exception Message: " + ex.Message + ", Exception Comments: Exception while reading Site Columns tag in Content Types", true);
                            }
                        }
                    }
                    #endregion
                }

                #region CustomFields
                if (lstCustomFieldIDs != null && lstCustomFieldIDs.Count > 0 && !isCustomSiteColumn)
                {
                    Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessStpFile] Searching for customized Site Columns", true);
                    if (!isCustomSiteColumn && xmlFields != null && xmlFields.Count > 0)
                    {
                        //Get all fields Column in manifest.xml
                        for (int i = 0; i < xmlFields.Count; i++)
                        {
                            try
                            {
                                var fieldList = xmlFields[i].Attributes["ID"].Value;

                                //Remove contenttype tag if ContentTypeId present in custom ContentTypes file ContentTypes.csv
                                if (lstCustomFieldIDs.Where(c => fieldList.Equals(c)).Any())
                                {
                                    isCustomSiteColumn = true;
                                    Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessStpFile] Customized Site ColumnFound for : " + objListCustOutput.ListTemplateName, true);
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessStpFile]. Exception Message: " + ex.Message + ", Exception Comments: Exception while reading Site Columns", true);
                            }
                        }
                    }
                }
                #endregion

                if (isCustomContentType || isCustomEventReceiver || isCustomSiteColumn)
                {
                    objListCustOutput.IsCustomizationPresent = "YES";
                    isCustomizationPresent = true;

                    if (lstCustomErs != null && lstCustomErs.Count > 0)
                        objListCustOutput.IsCustomizedEventReceiver = isCustomEventReceiver ? "YES" : "NO";
                    else
                        objListCustOutput.IsCustomizedEventReceiver = Constants.NoInputFile;

                    if (lstContentTypeIDs != null && lstContentTypeIDs.Count > 0)
                        objListCustOutput.IsCustomizedContentType = isCustomContentType ? "YES" : "NO";
                    else
                        objListCustOutput.IsCustomizedContentType = Constants.NoInputFile;

                    if (lstCustomFieldIDs != null && lstCustomFieldIDs.Count > 0)
                        objListCustOutput.IsCustomizedSiteColumn = isCustomSiteColumn ? "YES" : "NO";
                    else
                        objListCustOutput.IsCustomizedSiteColumn = Constants.NoInputFile;
                }
                else
                {
                    isCustomizationPresent = false;
                }
                reader.Dispose();

                Directory.SetCurrentDirectory(downloadFolder);
                System.IO.DirectoryInfo directory = new System.IO.DirectoryInfo(downloadFolder);
                foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories())
                {
                    Directory.SetCurrentDirectory(downloadFolder);
                    subDirectory.Delete(true);
                }
                foreach (System.IO.FileInfo file in directory.GetFiles())
                {
                    file.Delete();
                }
            }

            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessStpFile], Exception Message: " + ex.Message, true);
            }
            return isCustomizationPresent;
        }

        public static void DeleteListTemplate()
        {
            //Output files
            outputPath = Environment.CurrentDirectory;

            Logger.OpenLog("DeleteListTemplates");
            string listTemplateInputFile = string.Empty;

            Logger.LogMessage("Enter Complete Input File Path of List Template Data. E.g. C:\\Test\\Test.csv ");
            listTemplateInputFile = System.Console.ReadLine();

            if (string.IsNullOrEmpty(listTemplateInputFile) || !System.IO.File.Exists(listTemplateInputFile))
            {
                System.Console.ForegroundColor = System.ConsoleColor.Red;
                Logger.LogInfoMessage("ListTemplateInputFile is not valid/available. So, Operation aborted.");
                System.Console.ResetColor();
                return;
            }


            Logger.LogInfoMessage("Entered Input File of List Template Data " + listTemplateInputFile);
            Logger.LogInfoMessage("Entered Output direcotry: " + outputPath);

            DataTable dtListTemplatesInput = new DataTable();
            dtListTemplatesInput = ImportCSV.Read(listTemplateInputFile, Constants.CsvDelimeter);

            if (dtListTemplatesInput.Rows.Count > 0)
            {
                Logger.LogInfoMessage(String.Format("\nPreparing to delete a total of {0} files ...", dtListTemplatesInput.Rows.Count), true);
                for (int i = 0; i < dtListTemplatesInput.Rows.Count; i++)
                {
                    try
                    {
                        Delete(dtListTemplatesInput.Rows[i]);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogErrorMessage("[DeleteListTemplate]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
                    }
                    Logger.LogInfoMessage(String.Format("Scan completed {0}", DateTime.Now.ToString()), true);

                }
                Logger.LogInfoMessage(String.Format("Scan completed {0}", DateTime.Now.ToString()), true);
            }
            else
            {
                Logger.LogInfoMessage("[DeleteListTemplate] There is nothing to delete from the '" + listTemplateInputFile + "' File ", true);
            }
            Logger.CloseLog();
        }

        private static void Delete(DataRow listTemplate)
        {
            if (listTemplate == null)
            {
                return;
            }
            string webAppUrl = listTemplate["WebApplication"].ToString();
            string listGalleryPath = listTemplate["ListGalleryPath"].ToString();
            string listTemplateName = listTemplate["ListTemplateName"].ToString();
            string webUrl = listTemplate["WebUrl"].ToString();

            if (webUrl.IndexOf("http", StringComparison.InvariantCultureIgnoreCase) < 0)
            {
                // ignore the header row in case it is still present
                return;
            }

            // clean the inputs
            if (listGalleryPath.EndsWith("/"))
            {
                listGalleryPath = listGalleryPath.TrimEnd(new char[] { '/' });
            }
            if (!listGalleryPath.StartsWith("/"))
            {
                listGalleryPath = "/" + listGalleryPath;
            }
            if (listTemplateName.StartsWith("/"))
            {
                listTemplateName = listTemplateName.TrimStart(new char[] { '/' });
            }
            if (webUrl.EndsWith("/"))
            {
                webUrl = webUrl.TrimEnd(new char[] { '/' });
            }
            if (webAppUrl.EndsWith("/"))
            {
                webAppUrl = webAppUrl.TrimEnd(new char[] { '/' });
            }

            // e.g., "https://ppeTeams.contoso.com/sites/test/_catalogs/masterpage/Sample.master"
            string serverRelativeFilePath = listGalleryPath + '/' + listTemplateName;

            try
            {
                Logger.LogInfoMessage(String.Format("\n\nProcessing List Template File: {0} ...", serverRelativeFilePath), true);

                // we have to open the web because Helper.DeleteFileByServerRelativeUrl() needs to update the web in order to commit the change
                using (ClientContext userContext = Helper.CreateAuthenticatedUserContext(Program.AdminDomain, Program.AdminUsername, Program.AdminPassword, webUrl))
                {
                    Web web = userContext.Web;
                    userContext.Load(web);
                    userContext.ExecuteQuery();

                    Helper.DeleteFileByServerRelativeUrl(web, serverRelativeFilePath);
                    Logger.LogInfoMessage(listTemplateName + " deleted successfully");
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage(String.Format("DeleteMissingListTemplatesInGalleryFile() failed for {0}: Error={1}", serverRelativeFilePath, ex.Message), false);
            }
        }

        public static bool GetCustomizedListTemplate(ref ListTemplateFTCAnalysisOutputBase objListCustOutput,
            Microsoft.SharePoint.Client.File ltFile, string siteCollection, string webAppUrl)
        {
            bool isCustomizationPresent = false;
            string fileName = string.Empty;
            string listGalleryPath = string.Empty;

            try
            {
                fileName = ltFile.Name;
                listGalleryPath = ltFile.ServerRelativeUrl.Substring(0, ltFile.ServerRelativeUrl.LastIndexOf('/'));
                objListCustOutput.SiteCollection = siteCollection;
                objListCustOutput.WebApplication = GetWebapplicationUrlFromSiteCollectionUrl(siteCollection);
                objListCustOutput.WebUrl = siteCollection;
                objListCustOutput.ListTemplateName = fileName;
                objListCustOutput.ListGalleryPath = listGalleryPath;

                bool isDownloaded = DownloadListTemplate(outputPath + @"\" + Constants.DownloadPathListTemplates,
                    listGalleryPath, fileName, objListCustOutput.WebUrl, objListCustOutput.WebUrl);
                if (isDownloaded)
                {
                    isCustomizationPresent = ProcessStpFile(outputPath, outputPath + @"\" + Constants.DownloadPathListTemplates + @"\" + fileName,
                        ref objListCustOutput);
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: GetCustomizedListTemplate] Exception Message: " + ex.Message + ", Exception Comments: ", true);
            }
            return isCustomizationPresent;
        }

        public static void WriteOutputReport(List<ListTemplateFTCAnalysisOutputBase> ltListTemplateOutputBase, string csvFileName, ref bool headerOfCsv)
        {
            try
            {
                Logger.LogInfoMessage("[DownloadAndModifyListTemplate: WriteOutputReport] Writing the Output file " + Constants.ListTemplateCustomizationUsage, true);
                if (System.IO.File.Exists(csvFileName))
                    System.IO.File.Delete(csvFileName);
                if (ltListTemplateOutputBase != null && ltListTemplateOutputBase.Any())
                {
                    //Export the result(Missing Workflow Details) in CSV file                   
                    FileUtility.WriteCsVintoFile(csvFileName, ref ltListTemplateOutputBase, ref headerOfCsv);
                }
                else
                {
                    headerOfCsv = false;
                    ListTemplateFTCAnalysisOutputBase objListTemplatesNoInstancesFound = new ListTemplateFTCAnalysisOutputBase();
                    FileUtility.WriteCsVintoFile(csvFileName, objListTemplatesNoInstancesFound, ref headerOfCsv);
                    objListTemplatesNoInstancesFound = null;
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: WriteOutputReport] Exception Message: " + ex.Message + ", Exception Comments: ", true);
            }
        }

        public static void ProcessSiteCollectionUrl(string siteCollectionUrl,
            ref List<ListTemplateFTCAnalysisOutputBase> lstMissingListTempaltesInGalleryBase, string webApplicationUrl)
        {
            try
            {
                using (ClientContext userContext = Helper.CreateAuthenticatedUserContext(Program.AdminDomain, Program.AdminUsername, Program.AdminPassword, siteCollectionUrl))
                {
                    userContext.ExecuteQuery();
                    Web web = userContext.Web;
                    Folder folder = userContext.Web.GetFolderByServerRelativeUrl("_catalogs/lt");
                    userContext.Load(web.Folders);
                    userContext.Load(folder);

                    userContext.Load(folder.Files);
                    userContext.Load(web);

                    //Execute the query to the server    
                    userContext.ExecuteQuery();
                    // Loop through all the list templates    
                    foreach (Microsoft.SharePoint.Client.File ltFile in folder.Files)
                    {
                        try
                        {
                            bool isCustomizationPresent = false;
                            ListTemplateFTCAnalysisOutputBase objListCustOutput = new ListTemplateFTCAnalysisOutputBase();
                            System.Console.WriteLine("File Name: " + ltFile.Name);

                            isCustomizationPresent = GetCustomizedListTemplate(ref objListCustOutput, ltFile, siteCollectionUrl, webApplicationUrl);

                            if (isCustomizationPresent)
                            {
                                userContext.Load(ltFile.Author);
                                userContext.Load(ltFile.ModifiedBy);
                                userContext.ExecuteQuery();
                                objListCustOutput.CreatedBy = ltFile.Author.LoginName;
                                objListCustOutput.CreatedDate = ltFile.TimeCreated.ToString();
                                objListCustOutput.ModifiedBy = ltFile.ModifiedBy.LoginName;
                                objListCustOutput.ModifiedDate = ltFile.TimeLastModified.ToString();
                                lstMissingListTempaltesInGalleryBase.Add(objListCustOutput);
                            }
                            objListCustOutput = null;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessSiteCollectionUrl]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessSiteCollectionUrl]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
            }
        }

        public static void ReadInputFiles()
        {
            IEnumerable<ContentTypeInput> objCtInput;
            IEnumerable<CustomFieldInput> objFtInput;
            IEnumerable<EventReceiverInput> objErInput;
            try
            {
                //Content Type Input
                if (System.IO.File.Exists(filePath + @"\" + Constants.ContentTypeInput))
                {
                    Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ReadInputFiles] Reading the ContentTypes.csv Input file", true);
                    objCtInput = ImportCSV.ReadMatchingColumns<ContentTypeInput>(filePath + @"\" + Constants.ContentTypeInput, Constants.CsvDelimeter);
                    lstContentTypeIDs = objCtInput.Select(c => c.ContentTypeID).ToList();
                    lstContentTypeIDs = lstContentTypeIDs.Distinct().ToList();
                }
                else
                {
                    Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ReadInputFiles]. Exception Message: " + filePath + @"\" + Constants.ContentTypeInput + " is not present", true);
                }
                //Custom Field Input
                if (System.IO.File.Exists(filePath + @"\" + Constants.CustomFieldsInput))
                {
                    Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ReadInputFiles] Reading the CustomFields.csv Input file", true);
                    objFtInput = ImportCSV.ReadMatchingColumns<CustomFieldInput>(filePath + @"\" + Constants.CustomFieldsInput, Constants.CsvDelimeter);
                    lstCustomFieldIDs = objFtInput.Select(c => c.ID).ToList();
                    lstCustomFieldIDs = lstCustomFieldIDs.Distinct().ToList();
                }
                else
                {
                    Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ReadInputFiles]. Exception Message: " + filePath + @"\" + Constants.CustomFieldsInput + " is not present", true);
                }
                //EventReceivers Input
                if (System.IO.File.Exists(filePath + @"\" + Constants.EventReceiversInput))
                {
                    Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ReadInputFiles] Reading the EventReceivers.csv Input file", true);
                    objErInput = ImportCSV.ReadMatchingColumns<EventReceiverInput>(filePath + @"\" + Constants.EventReceiversInput, Constants.CsvDelimeter);
                    lstCustomErs = objErInput.Select(c => c.Assembly.ToLower()).ToList();
                    lstCustomErs = lstCustomErs.Distinct().ToList();
                }
                else
                {
                    Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ReadInputFiles]. Exception Message: " + filePath + @"\" + Constants.EventReceiversInput + " is not present", false);
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ReadInputFiles]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
            }
            finally
            {
                objCtInput = null;
                objErInput = null;
                objFtInput = null;
            }
        }

        public static bool ReadInputOptions(ref bool processInputFile, ref bool processFarm, ref bool processSiteCollections)
        {
            string processOption = string.Empty;

            Logger.LogMessage("Please select any of following options:");
            Logger.LogMessage("1 - Process with Auto-generated Site Collection Report");
            Logger.LogMessage("2 - Process with PreMT/Discovery ListTemplate Report");
            Logger.LogMessage("3 - Process with SiteCollectionUrls separated by comma (,)");

            processOption = System.Console.ReadLine();

            if (processOption.Equals("2"))
                processInputFile = true;
            else if (processOption.Equals("1"))
                processFarm = true;
            else if (processOption.Equals("3"))
                processSiteCollections = true;
            else
                return false;

            return true;
        }

        public static bool ReadWebApplication(ref string webApplicationUrl)
        {
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            Logger.LogMessage("!!! NOTE !!!");
            Logger.LogMessage("This operation is intended for use only in PPE; use on PROD at your own risk.");
            Logger.LogMessage("This operation is based on Search Service Result. it would be possible list of Site returned would be different than actual due to permission issue or stale crawl data.");
            Logger.LogMessage("For PROD, it is safer to generate the report via the o365 Self-Service Admin Portal.");

            System.Console.ResetColor();

            Logger.LogMessage("Please enter any one of the web application URL for Context making: ");
            webApplicationUrl = System.Console.ReadLine();

            if (string.IsNullOrEmpty(webApplicationUrl))
                return false;

            return true;
        }

        public static bool ReadSiteCollectionList(ref string siteCollectionUrlsList)
        {
            Logger.LogMessage("Enter SiteCollection URLs separated by comma (,): ");
            siteCollectionUrlsList = System.Console.ReadLine();

            if (string.IsNullOrEmpty(siteCollectionUrlsList))
                return false;

            return true;
        }

        public static bool ReadInputFile(ref string ListTemplateInputFile)
        {
            Logger.LogMessage("Enter Complete Input File Path of List Template Report Either Pre-Scan OR Discovery Report.");
            ListTemplateInputFile = System.Console.ReadLine();
            Logger.LogMessage("[DownloadAndModifyListTemplate: ReadInputFile] Entered Input File of List Template Data " + ListTemplateInputFile, false);
            if (string.IsNullOrEmpty(ListTemplateInputFile) || !System.IO.File.Exists(ListTemplateInputFile))
                return false;
            return true;
        }

        public static bool ReadInputFilesPath()
        {
            Logger.LogMessage("Enter the directory of input files for customization analysis (EventReceivers.csv, ContentTypes.csv and CustomFields.csv)");
            Logger.LogMessage("Please refer document for how to create input files to analyze the customization. These files are required to find what customization we are looking inside a template");
            filePath = System.Console.ReadLine();
            Logger.LogMessage("[DownloadAndModifyListTemplate: ReadInputFilesPath] Entered Input files directory: " + filePath, false);
            if (string.IsNullOrEmpty(filePath) || !System.IO.Directory.Exists(filePath))
                return false;
            return true;
        }

        public static void DeleteDownloadedListTemplates()
        {
            try
            {
                //Delete DownloadedListTemplate directory if exists
                if (Directory.Exists(outputPath + @"\" + Constants.DownloadPathListTemplates))
                {
                    if (Environment.CurrentDirectory.Equals(outputPath + @"\" + Constants.DownloadPathListTemplates))
                    {
                        Environment.CurrentDirectory = outputPath;
                        Directory.Delete(outputPath + @"\" + Constants.DownloadPathListTemplates, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: DeleteDownloadedListTemplates]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
            }
        }

        public static void ProcessListTemplateInputFile(string ListTemplateInputFile, ref List<ListTemplateFTCAnalysisOutputBase> lstMissingListTempaltesInGalleryBase)
        {
            try
            {
                Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessListTemplateInputFile] Checking the Customization Status for List Templates", true);

                Logger.LogInfoMessage("[DownloadAndModifyListTemplate: ProcessListTemplateInputFile] Reading the ListTemplates Input file: " + ListTemplateInputFile, true);

                DataTable dtListTemplatesInput = new DataTable();
                dtListTemplatesInput = ImportCSV.Read(ListTemplateInputFile, Constants.CsvDelimeter);

                List<string> lstSiteCollectionUrls = dtListTemplatesInput.AsEnumerable()
                                                    .Select(r => r.Field<string>("SiteCollection"))
                                                    .ToList();
                lstSiteCollectionUrls = lstSiteCollectionUrls.Distinct().ToList();
                foreach (string siteCollection in lstSiteCollectionUrls)
                {
                    string webApplicationUrl = string.Empty;
                    try
                    {
                        Logger.LogInfoMessage("Processing the site: " + siteCollection, true);
                        webApplicationUrl = GetWebapplicationUrlFromSiteCollectionUrl(siteCollection);

                        //Record SiteCollection Url in SiteCollections.txt
                        ProcessSiteCollectionUrl(siteCollection, ref lstMissingListTempaltesInGalleryBase, webApplicationUrl);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessListTemplateInputFile]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessListTemplateInputFile]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
            }
        }

        public static void ProcessWebApplicationUrl(string webApplicationUrl, ref List<ListTemplateFTCAnalysisOutputBase> lstMissingListTempaltesInGalleryBase)
        {
            try
            {
                //Delete SiteCollections.txt file if it already exists
                if (System.IO.File.Exists(outputPath + @"\" + Constants.SiteCollectionsTextFile))
                    System.IO.File.Delete(outputPath + @"\" + Constants.SiteCollectionsTextFile);
                //Create SiteCollections.txt file
                System.IO.StreamWriter file = new System.IO.StreamWriter(outputPath + @"\" + Constants.SiteCollectionsTextFile);

                List<SiteEntity> siteUrls = GenerateSiteCollectionReport.GetAllSites(webApplicationUrl);
                if (siteUrls != null && siteUrls.Count > 0)
                {
                    foreach (SiteEntity siteUrlEntity in siteUrls)
                    {
                        try
                        {
                            string siteCollection = siteUrlEntity.Url;
                            Logger.LogInfoMessage("Processing the site: " + siteUrlEntity.Url, true);
                            //Record SiteCollection Url in SiteCollections.txt
                            file.WriteLine(siteUrlEntity.Url);
                            ProcessSiteCollectionUrl(siteUrlEntity.Url, ref lstMissingListTempaltesInGalleryBase, webApplicationUrl);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessWebApplicationUrl]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
                        }
                    }
                }
                file.Close();
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessWebApplicationUrl]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
            }
        }

        public static void ProcessSiteCollectionUrlList(string[] siteCollectionUrls, ref List<ListTemplateFTCAnalysisOutputBase> lstMissingListTempaltesInGalleryBase)
        {
            try
            {
                foreach (string siteCollectionUrl in siteCollectionUrls)
                {
                    string webApplicationUrl = string.Empty;
                    string siteCollection = siteCollectionUrl.Trim();
                    try
                    {
                        Logger.LogInfoMessage("Processing the site: " + siteCollectionUrl, true);
                        webApplicationUrl = GetWebapplicationUrlFromSiteCollectionUrl(siteCollection);

                        //Record SiteCollection Url in SiteCollections.txt
                        ProcessSiteCollectionUrl(siteCollection, ref lstMissingListTempaltesInGalleryBase, webApplicationUrl);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessSiteCollectionUrlList]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: ProcessSiteCollectionUrlList]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
            }
        }

        public static string GetWebapplicationUrlFromSiteCollectionUrl(string siteCollection)
        {
            Uri uri;
            try
            {
                uri = new Uri(siteCollection);
                if (uri != null)
                {
                    return uri.Scheme + @"://" + uri.Host;
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DownloadAndModifyListTemplate: GetWebapplicationUrlFromSiteCollectionUrl]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
            }
            finally
            {
                uri = null;
            }
            return string.Empty;
        }
    }
}
