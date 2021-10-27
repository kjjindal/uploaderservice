using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Data;
using CTSLibrary;
using System.Diagnostics;
using CTSWrapper;


namespace UploaderService
{
    class GlobalClass
    {
        public const string CTS_SUCCESS = "SUCCESS";
        public const string CTS_FAIL = "FAIL";
        public const string sdbURL = "";
        public const string sdbConString = "";
        public const string sdbType = "";
        public const string sReadConfig = "";

        public static XmlDocument xml_ConfigDoc;
        public static XmlNodeList xml_ConfigFileNodeList;
        //public static DBEssentialStructure DBStructure;


        public static OmniFlow objOmniFlow = new OmniFlow();
        public static OmniFlow objOmniFlowDes = new OmniFlow();
        public static CTSWrapper.clsGlobalDecl.WMTSessionHandle oudtWMTSessionHandle = new CTSWrapper.clsGlobalDecl.WMTSessionHandle();
        public static CTSWrapper.clsGlobalDecl.WFInstrument oudtWFInstrument = new CTSWrapper.clsGlobalDecl.WFInstrument();
        public static CTSWrapper.clsGlobalDecl.WMTBulkComplete[] udtWMTBulkComplete;
        public static UService_DBEssential udtUService_DBEssential;
        public static UService_Completer udtUService_Completer;
        public static UService_ReadCsvFile udtUService_ReadCsvFile;

        //no refrence found
        public static bool bIsLogEnabled;
        public static string s_strLogPath;
        public static bool bClearEventLogsOnStart;
        //public struct DBEssentialStructure
        //{
        //    public String DBType;
        //    public String DBConnectionString;
        //    public String DBServerURL;
        //}

        public struct UService_DBEssential
        {
            public String DBType;
            public String DBConnectionString;
            public String DBServerURL;
            public String CabinetName;
            public String ImageServerIP;
            public Int32 PortId;
            public String VolumeName;
            public Int16 VolId;
        }
        public struct UService_ReadCsvFile
        {
            public string AccountNo;
            public string ChequeNo;
            public string Amount;
            public string ImageIndex;
            public string ImageLength;
            
        }
        public struct UService_Completer
        {
            public Int32 QueueID;
            public Int32 ProcessID;
            public String UserName;
            public String Password;
            public Int32 TimeInterval;
            public string useServiceForTechVerification;
        }

        //GlobalClass.UService_ReadCsvFile[] objReadCsvFile = null;
        // event logger
        public static void WriteEventLog(string sMsg, string strEventSourceName, bool bFlag)
        {
            string sSource = strEventSourceName;
            string sLog = strEventSourceName;
            string sEvent;
            sEvent = sMsg;

            // Creating custom event log
            if (!EventLog.SourceExists(sSource))
            {
                EventLog.CreateEventSource(sSource, sLog);

                // Setting custom event log property as OverwriteAsNeeded
                EventLog[] eventLogs = EventLog.GetEventLogs();
                foreach (EventLog e in eventLogs)
                    if (e.LogDisplayName == sSource)
                        e.ModifyOverflowPolicy(OverflowAction.OverwriteAsNeeded, 1);
            }
            if (!bFlag)
                EventLog.WriteEntry(sSource, sEvent);
            else
                EventLog.WriteEntry(sSource, sEvent, EventLogEntryType.Error);

        }


        public static string ReadConfig()
        {
            int TagCount = 0;
            int iLoop = 0;
            //bool bNumeric;
            string sPassword = "";

            try
            {
                NGCTSEncryption.Encryption objEnc = new NGCTSEncryption.Encryption();
                string argString = Environment.GetEnvironmentVariable("CTS", EnvironmentVariableTarget.Machine);
                //String ConfigFilePath = argString + "\\" + Environment.GetCommandLineArgs()[1] + "\\" + "NGCTSCHGConfig.xml";
                String ConfigFilePath = argString + "\\" + "BOB" +"\\" + "NGCTSCHGConfig.xml";

                StreamReader ConfigFileReader = new StreamReader(ConfigFilePath);

                xml_ConfigDoc = new XmlDocument();
                xml_ConfigDoc.Load(ConfigFileReader);
                var number = xml_ConfigDoc.GetElementsByTagName("UPLOADERSERVICE").Count;
                if (xml_ConfigDoc.GetElementsByTagName("UPLOADERSERVICE").Count == 1)
                {
                    xml_ConfigFileNodeList = xml_ConfigDoc.GetElementsByTagName("UPLOADERSERVICE");
                    xml_ConfigFileNodeList = xml_ConfigFileNodeList.Item(0).ChildNodes;
                    TagCount = xml_ConfigFileNodeList.Count;
                    iLoop = 0;
                    while (TagCount > iLoop)
                    {
                        if (xml_ConfigFileNodeList.Item(iLoop).Name == "ProcessID")
                            udtUService_Completer.ProcessID = Convert.ToInt32(xml_ConfigFileNodeList.Item(iLoop).InnerText);
                        //set
                        else if (xml_ConfigFileNodeList.Item(iLoop).Name == "UserName")
                            udtUService_Completer.UserName = xml_ConfigFileNodeList.Item(iLoop).InnerText;
                        //set
                        else if (xml_ConfigFileNodeList.Item(iLoop).Name == "Password")
                        {
                            sPassword = xml_ConfigFileNodeList.Item(iLoop).InnerText;
                            if (sPassword == "")
                                udtUService_Completer.Password = "";
                            else
                                udtUService_Completer.Password = objEnc.Decrypt(sPassword);
                        }
                        else if (xml_ConfigFileNodeList.Item(iLoop).Name == "useServiceForTechVerification")
                        {
                            udtUService_Completer.useServiceForTechVerification = xml_ConfigFileNodeList.Item(iLoop).InnerText;
                        }
                        //set
                        else if (xml_ConfigFileNodeList.Item(iLoop).Name == "TimeInterval")
                            udtUService_Completer.TimeInterval = Convert.ToInt32(xml_ConfigFileNodeList.Item(iLoop).InnerText);

                        iLoop++;
                    }
                }
                else
                {
                    WriteEventLog("Error in reading ICCSConfig.Xml : InwardAccountDetails Tag is present more than once.", Service1.sEventSourceName, true);
                    return CTS_FAIL;
                }

                if (xml_ConfigDoc.GetElementsByTagName("DBEssential").Count == 1)
                {
                    xml_ConfigFileNodeList = null;
                    xml_ConfigFileNodeList = xml_ConfigDoc.GetElementsByTagName("DBEssential");
                    xml_ConfigFileNodeList = xml_ConfigFileNodeList.Item(0).ChildNodes;
                    iLoop = 0;
                    TagCount = 0;
                    TagCount = xml_ConfigFileNodeList.Count;
                    while (TagCount > iLoop)
                    {
                        if (xml_ConfigFileNodeList.Item(iLoop).Name == "DBType")
                            udtUService_DBEssential.DBType = xml_ConfigFileNodeList.Item(iLoop).InnerText;
                        else if (xml_ConfigFileNodeList.Item(iLoop).Name == "DBConnectionString")
                            udtUService_DBEssential.DBConnectionString = xml_ConfigFileNodeList.Item(iLoop).InnerText;
                        else if (xml_ConfigFileNodeList.Item(iLoop).Name == "DBServerURL")
                            udtUService_DBEssential.DBServerURL = xml_ConfigFileNodeList.Item(iLoop).InnerText;
                        else if (xml_ConfigFileNodeList.Item(iLoop).Name == "CabinetName")
                            udtUService_DBEssential.CabinetName = xml_ConfigFileNodeList.Item(iLoop).InnerText;
                        else if (xml_ConfigFileNodeList.Item(iLoop).Name == "ImageServerIP")
                            udtUService_DBEssential.ImageServerIP = xml_ConfigFileNodeList.Item(iLoop).InnerText;
                        else if (xml_ConfigFileNodeList.Item(iLoop).Name == "PortId")
                            udtUService_DBEssential.PortId = Convert.ToInt32(xml_ConfigFileNodeList.Item(iLoop).InnerText);
                        else if (xml_ConfigFileNodeList.Item(iLoop).Name == "VolumeName")
                            udtUService_DBEssential.VolumeName = xml_ConfigFileNodeList.Item(iLoop).InnerText;
                        else if (xml_ConfigFileNodeList.Item(iLoop).Name == "VolId")
                            udtUService_DBEssential.VolId = Convert.ToInt16(xml_ConfigFileNodeList.Item(iLoop).InnerText);
                        iLoop++;
                    }
                }
                else
                {
                    WriteEventLog("DBEssential Tag is present more than once.",Service1.sEventSourceName, true);
                    return CTS_FAIL;
                }

             

                return CTS_SUCCESS;
            }
            catch (Exception ex)
            {
                WriteEventLog("Exception in Config: " + ex.Message, Service1.sEventSourceName, true);
                return CTS_FAIL;
            }
        }

        public static void DBInteractionWithErrorTable(string sFileName, string sErrorDescription)
        {

            String sWebResult;
            String sSqlQuery;
            String sQueryType;
            String sServiceName = "Uploader Service";
            DataTable dt = new DataTable();

            Webservice.DBType = udtUService_DBEssential.DBType;
            Webservice.DBConnectionString = udtUService_DBEssential.DBConnectionString;
            Webservice.WebserviceURL = udtUService_DBEssential.DBServerURL;
            Webservice.DBParameter[] DBParam = new Webservice.DBParameter[3];


            if (udtUService_DBEssential.DBType == "SQL")
            {

            }
            else if (udtUService_DBEssential.DBType == "ORACLE")
            {
            }
            DBParam[0].strParamName = "OSERVICENAME";
            DBParam[0].strParamType = "varchar";
            DBParam[0].strParamDirection = "in";
            DBParam[0].strParamValue = sServiceName;
            DBParam[0].intParamLength = sServiceName.Length;

            DBParam[1].strParamName = "OFILENAME";
            DBParam[1].strParamType = "varchar";
            DBParam[1].strParamDirection = "in";
            DBParam[1].strParamValue = sFileName;
            DBParam[1].intParamLength = sFileName.Length;

            DBParam[2].strParamName = "OERRORDESCRIPTION";
            DBParam[2].strParamType = "varchar";
            DBParam[2].strParamDirection = "in";
            DBParam[2].strParamValue = sErrorDescription;
            DBParam[2].intParamLength = sErrorDescription.Length;


            sSqlQuery = "CHG_UpdateErrorTable";
            sQueryType = "storedproc";

            sWebResult = Webservice.ExecuteDBOperation(sSqlQuery, sQueryType, DBParam, ref dt);
            if (sWebResult == "SUCCESS")
            {
                //   GlobalClass.WriteLog("File:" + CBFileName + "Inserted To Error Table", false);
                //  return CTS_SUCCESS;
            }
            else
            {
                GlobalClass.WriteEventLog("Error While Inserting to Error Table.",Service1.sEventSourceName, true);
                //return CTS_FAIL;
            }
        }
        public static string UploadDataInDb(GlobalClass.UService_ReadCsvFile oInstDetail)
        {

            // OmniFlow objOmniFlow = new OmniFlow();
            OmniFlow.CTSError udtError = new OmniFlow.CTSError();
            string[] strAttribNames = { "" };
            string[] strAttribValues = { "" };
            string[] strAttribTypes = { "" };
            int iImageIndex = 0;
            long lImageLength = 0;

            try
            {


                Array.Resize<string>(ref strAttribNames, 51);
                Array.Resize<string>(ref strAttribValues, 51);
                Array.Resize<string>(ref strAttribTypes, 51);




                strAttribNames[0] = "ACCOUNTNO";
                strAttribValues[0] = oInstDetail.AccountNo;
                strAttribTypes[0] = "0";

                strAttribNames[1] = "CHEQUENO";
                strAttribValues[1] = oInstDetail.ChequeNo;
                strAttribTypes[1] = "0";

             

                strAttribNames[2] = "AMOUNT";
                strAttribValues[2] = oInstDetail.Amount;
                strAttribTypes[2] = "0";

              

                //udtError = GlobalClass.objOmniFlow.UploadNewWorkItem(ref strAttribNames, ref strAttribValues, ref strAttribTypes, "Image.tif",false);                
                // Associate Image already uploaded in OD with workitem 
                udtError = GlobalClass.objOmniFlow.UploadNewWorkItem_AssociateImage(ref strAttribNames, ref strAttribValues, ref strAttribTypes, iImageIndex, lImageLength);
                if (udtError.main_code != "SUCCESS")
                {
                    WriteEventLog("Error in Uploading WorkItem." + udtError.main_code, Service1.sEventSourceName,true);

                    if (udtError.main_code == "Invalid session handle.")
                    {
                        WriteEventLog("going to reconnect ", Service1.sEventSourceName, false);

                        udtError = GlobalClass.objOmniFlow.ConnectToWorkFlow(GlobalClass.udtUService_DBEssential.CabinetName, GlobalClass.udtUService_Completer.UserName, GlobalClass.udtUService_Completer.Password, 1, ref GlobalClass.oudtWMTSessionHandle);
                        if (udtError.main_code != "SUCCESS")
                        {
                            WriteEventLog("Error(ConnectToWorkFlow): " + udtError.main_code, Service1.sEventSourceName, true);

                            return GlobalClass.CTS_FAIL;
                        }
                        else
                        {
                            udtError = GlobalClass.objOmniFlow.UploadNewWorkItem_AssociateImage(ref strAttribNames, ref strAttribValues, ref strAttribTypes, iImageIndex, lImageLength);
                            if (udtError.main_code != "SUCCESS")
                            {
                        WriteEventLog("Error in Uploading WorkItem." + udtError.main_code, Service1.sEventSourceName, true);

                                //GlobalClass.DBInteractionWithErrorTable(oInstDetail.sCBFILENAME, "Error in uploading cheque with itemsequence number " + oInstDetail.sITEMSEQUENCENO + ". Maincode error=" + udtError.main_code + " and SubCode error=" + udtError.sub_code);
                                return GlobalClass.CTS_FAIL;
                            }

                        }
                    }
                    else
                    {
                        WriteEventLog("Error in Uploading WorkItem.", Service1.sEventSourceName, true);

                        //GlobalClass.DBInteractionWithErrorTable(oInstDetail.sCBFILENAME, "Error in uploading cheque with itemsequence number " + oInstDetail.sITEMSEQUENCENO + ". Maincode error=" + udtError.main_code + " and SubCode error=" + udtError.sub_code);
                        return GlobalClass.CTS_FAIL;
                    }

                }
                return GlobalClass.CTS_SUCCESS;
            }
            catch (Exception ex)
            {
              
                WriteEventLog(ex.Message, Service1.sEventSourceName, true);

                //GlobalClass.DBInteractionWithErrorTable(oInstDetail.sCBFILENAME, "Error in uploading cheque. Error is=" + ex.Message);
                return GlobalClass.CTS_FAIL;
            }
        }

        public static string ReadCsvFile(string FilePath,ref GlobalClass.UService_ReadCsvFile[] objReadCsvFile)
        {
            int loop = 0;
            try
            {
                using (var reader = new StreamReader($"{FilePath}"))
                {
                    objReadCsvFile = new GlobalClass.UService_ReadCsvFile[3];

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(';');


                        //udtUService_ReadCsvFile.AccountNo = values[0].Split(',')[0];
                        //udtUService_ReadCsvFile.ChequeNo = values[0].Split(',')[1];
                        //udtUService_ReadCsvFile.Amount = values[0].Split(',')[2];

                        objReadCsvFile[loop].AccountNo = values[0].Split(',')[0];
                        objReadCsvFile[loop].ChequeNo = values[0].Split(',')[1];
                        objReadCsvFile[loop].Amount = values[0].Split(',')[2];
                        objReadCsvFile[loop].ImageIndex = values[0].Split(',')[3];
                        objReadCsvFile[loop].ImageLength = values[0].Split(',')[4];


                        loop++;

                    }

                }

                return "SUCCESS";

            }
           catch(Exception E)
            {
                GlobalClass.WriteEventLog("During read csv file  =" + E.Message + "trace=" + E.StackTrace, Service1.sEventSourceName, true);
                return "FAILED";
            }


        }




        public static string DeleteErrinDB(string sPId)
        {
            Webservice.DBConnectionString = GlobalClass.udtUService_DBEssential.DBConnectionString;
            Webservice.DBType = GlobalClass.udtUService_DBEssential.DBType;
            Webservice.WebserviceURL = GlobalClass.udtUService_DBEssential.DBServerURL;


            DataTable dtQueryResult;
            dtQueryResult = null;
            string strWebRetval;
            string strQueryType;
            Webservice.DBParameter[] webParameters;
            string strResult;
            try
            {
                string strQuery = "DELETEACCOUNTDETAILSERROR";
                strQueryType = "storedproc";
                strWebRetval = String.Empty;
                // webParameters = null;
                webParameters = new Webservice.DBParameter[1];

                webParameters[0].strParamName = "sPId";
                webParameters[0].strParamType = "VARCHAR";
                webParameters[0].strParamDirection = "in";
                webParameters[0].strParamValue = sPId;
                webParameters[0].intParamLength = webParameters[0].strParamValue.Length;
                strResult = CTSLibrary.Webservice.ExecuteDBOperation(strQuery, strQueryType, webParameters, ref dtQueryResult);

                if (strResult == CTS_SUCCESS)
                {
                    return CTS_SUCCESS;
                }
                else
                {
                    GlobalClass.WriteEventLog("Error in deleteing in DeleteErrinDB : " + strResult, Service1.sEventSourceName, true);
                    return "There is some error in executing DBOperation at deleteErrinDB() . Please contact administrator. result=" + strResult;
                }

            }
            catch (Exception ex)
            {
                GlobalClass.WriteEventLog("Exception at DeleteErrinDB msg =" + ex.Message + "trace=" + ex.StackTrace, Service1.sEventSourceName, true);
                return "Exception at DeleteErrinDB msg =" + ex.Message + "trace=" + ex.StackTrace;
            }


        }
        public static string InsertErrinDB(string sPId, string ErrType, string ErrDescription)
        {
            Webservice.DBConnectionString = GlobalClass.udtUService_DBEssential.DBConnectionString;
            Webservice.DBType = GlobalClass.udtUService_DBEssential.DBType;
            Webservice.WebserviceURL = GlobalClass.udtUService_DBEssential.DBServerURL;


            DeleteErrinDB(sPId);

            DataTable dtQueryResult;
            dtQueryResult = null;
            string strWebRetval;
            string strQueryType;
            Webservice.DBParameter[] webParameters;
            string strResult;
            try
            {
                string strQuery = "INSERTTECHSIGNERROR";
                strQueryType = "storedproc";
                strWebRetval = String.Empty;
                // webParameters = null;
                webParameters = new Webservice.DBParameter[3];

                webParameters[0].strParamName = "sPId";
                webParameters[0].strParamType = "VARCHAR";
                webParameters[0].strParamDirection = "in";
                webParameters[0].strParamValue = sPId;
                webParameters[0].intParamLength = webParameters[0].strParamValue.Length;

                webParameters[1].strParamName = "ErrType";
                webParameters[1].strParamType = "VARCHAR";
                webParameters[1].strParamDirection = "in";
                webParameters[1].strParamValue = ErrType;
                webParameters[1].intParamLength = webParameters[1].strParamValue.Length;

                webParameters[2].strParamName = "ErrDescription";
                webParameters[2].strParamType = "VARCHAR";
                webParameters[2].strParamDirection = "in";
                if (ErrDescription == null)
                {
                    ErrDescription = "";
                }
                webParameters[2].strParamValue = ErrDescription;
                webParameters[2].intParamLength = webParameters[2].strParamValue.Length;

                strResult = CTSLibrary.Webservice.ExecuteDBOperation(strQuery, strQueryType, webParameters, ref dtQueryResult);

                if (strResult == CTS_SUCCESS)
                {
                    return CTS_SUCCESS;
                }
                else
                {
                    GlobalClass.WriteEventLog("Error in inserting in Db" + strResult, Service1.sEventSourceName, true);
                    return "There is some error in executing DBOperation at InsertErrinDB() . Please contact administrator. result=" + strResult;
                }

            }
            catch (Exception ex)
            {
                GlobalClass.WriteEventLog("Exception at InsertErrinDB msg =" + ex.Message + "trace=" + ex.StackTrace,Service1.sEventSourceName, true);
                return "Exception at DeleteErrinDB msg =" + ex.Message + "trace=" + ex.StackTrace;
            }


        }
        public static string WF_DUploadNewInstrument(GlobalClass.UService_ReadCsvFile oInstDetail)
        {

            // OmniFlow objOmniFlow = new OmniFlow();
            OmniFlow.CTSError udtError = new OmniFlow.CTSError();
            string[] strAttribNames = { "" };
            string[] strAttribValues = { "" };
            string[] strAttribTypes = { "" };
            int iImageIndex = 0;
            long lImageLength = 0;
            try
            {

                Array.Resize<string>(ref strAttribNames,5);
                Array.Resize<string>(ref strAttribValues,5);
                Array.Resize<string>(ref strAttribTypes, 5);

              

           

                strAttribNames[1] = "CHEQUENO";
                strAttribValues[1] = oInstDetail.ChequeNo;
                strAttribTypes[1] = "0";

               


                strAttribNames[2] = "AMOUNT";
                strAttribValues[2] = oInstDetail.Amount;
                strAttribTypes[2] = "0";

            

                strAttribNames[0] = "ACCOUNTNO";
                strAttribValues[0] = oInstDetail.AccountNo;
                strAttribTypes[0] = "0";

            



              




                strAttribNames[3] = "IMAGEINDEX";
                strAttribValues[3] = oInstDetail.ImageIndex;
                strAttribTypes[3] = "0";

                strAttribNames[4] = "IMAGELENGTH";
                strAttribValues[4] = oInstDetail.ImageLength;
                strAttribTypes[4] = "0";












                //ends here

                iImageIndex = Convert.ToInt32(oInstDetail.ImageIndex);
                lImageLength = Convert.ToInt32(oInstDetail.ImageLength);

                //udtError = GlobalClass.objOmniFlow.UploadNewWorkItem(ref strAttribNames, ref strAttribValues, ref strAttribTypes, "Image.tif",false);                
                // Associate Image already uploaded in OD with workitem 
                udtError = GlobalClass.objOmniFlow.UploadNewWorkItem_AssociateImage(ref strAttribNames, ref strAttribValues, ref strAttribTypes, iImageIndex, lImageLength);
                if (udtError.main_code != "SUCCESS")
                {
                    GlobalClass.WriteEventLog("Error in Uploading WorkItem." + udtError.main_code,Service1.sEventSourceName,true);
                    if (udtError.main_code == "Invalid session handle.")
                    {
                        GlobalClass.WriteEventLog("Going to reconnect!", Service1.sEventSourceName, false);
                        udtError = GlobalClass.objOmniFlow.ConnectToWorkFlow(GlobalClass.udtUService_DBEssential.CabinetName, GlobalClass.udtUService_Completer.UserName, GlobalClass.udtUService_Completer.Password, 1, ref GlobalClass.oudtWMTSessionHandle);
                        if (udtError.main_code != "SUCCESS")
                        {
                            GlobalClass.WriteEventLog("Error(ConnectToWorkFlow): " + udtError.main_code, Service1.sEventSourceName, true);
                            return GlobalClass.CTS_FAIL;
                        }
                        else
                        {
                            udtError = GlobalClass.objOmniFlow.UploadNewWorkItem_AssociateImage(ref strAttribNames, ref strAttribValues, ref strAttribTypes, iImageIndex, lImageLength);
                            if (udtError.main_code != "SUCCESS")
                            {
                                GlobalClass.WriteEventLog("Error in Uploading WorkItem." + udtError.main_code,Service1.sEventSourceName,true);
                                return GlobalClass.CTS_FAIL;
                            }

                        }
                    }
                    else
                    {
                        GlobalClass.WriteEventLog("Error in Uploading WorkItem." + udtError.main_code,Service1.sEventSourceName, true);
                        return GlobalClass.CTS_FAIL;
                    }

                }
                return GlobalClass.CTS_SUCCESS;
            }
            catch (Exception ex)
            {
                GlobalClass.WriteEventLog(ex.Message,Service1.sEventSourceName, true);
                return GlobalClass.CTS_FAIL;
            }
        }








    }
}
