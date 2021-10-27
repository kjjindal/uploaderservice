
using System;
using System.Data;
using CTSLibrary;

namespace UploaderService
{

    class ItemCompleter
    {
        CTSWrapper.clsGlobalDecl.WFInstrument udtInstrument;
        public string TechItemCompleter()
        {
            String sQuery;
            String sQueryType;
            String sReturnValue = "";
            Webservice.DBParameter[] udtDBParam = null;
            DataTable rsdatatab = new DataTable();

            OmniFlow.CTSError oRetVal = new OmniFlow.CTSError();
            OmniFlow.CTSError oRetValConnect = new OmniFlow.CTSError();

            try
            {
                sQuery = "FetchItemsFromDetailsTable";
                sQueryType = "storedproc";

                Array.Resize<Webservice.DBParameter>(ref udtDBParam, 1);

                udtDBParam[0].strParamName = "QueueName";
                udtDBParam[0].strParamType = "varchar";
                udtDBParam[0].strParamDirection = "in";
                udtDBParam[0].strParamValue = "TechSignVerify";
                udtDBParam[0].intParamLength = "TechSignVerify".Length;

                oRetVal = GlobalClass.objOmniFlow.FetchAndLockInstruments(sQuery, sQueryType, udtDBParam, ref rsdatatab);

                //Added for invalid/expired session handling
                if (oRetVal.main_code.ToUpper() == "Invalid session handle.".ToUpper())
                {
                    oRetValConnect = GlobalClass.objOmniFlow.ConnectToWorkFlow(GlobalClass.udtUService_DBEssential.CabinetName, GlobalClass.udtUService_Completer.UserName, GlobalClass.udtUService_Completer.Password, 1, ref GlobalClass.oudtWMTSessionHandle);
                    if (oRetValConnect.main_code != "SUCCESS")
                    {
                        GlobalClass.WriteEventLog("Session invalid/expired. Error(ConnectToWorkFlow): " + oRetValConnect.main_code,Service1.sEventSourceName, true);
                    }
                    else
                    {
                        GlobalClass.WriteEventLog("Session invalid/expired. Re-Connected to Workflow", Service1.sEventSourceName, false);
                    }
                    return oRetValConnect.main_code;
                }

                if (oRetVal.main_code.Contains("NO DATA"))
                {
                    udtDBParam[0].strParamName = "QueueName";
                    udtDBParam[0].strParamType = "varchar";
                    udtDBParam[0].strParamDirection = "in";
                    udtDBParam[0].strParamValue = "TechSignVerify_ERROR";
                    udtDBParam[0].intParamLength = "TechSignVerify_ERROR".Length;
                    oRetVal.main_code = string.Empty;

                    oRetVal = GlobalClass.objOmniFlow.FetchAndLockInstruments(sQuery, sQueryType, udtDBParam, ref rsdatatab);

                    if (oRetVal.main_code.ToUpper() == "Invalid session handle.".ToUpper())
                    {
                        oRetValConnect = GlobalClass.objOmniFlow.ConnectToWorkFlow(GlobalClass.udtUService_DBEssential.CabinetName, GlobalClass.udtUService_Completer.UserName, GlobalClass.udtUService_Completer.Password, 1, ref GlobalClass.oudtWMTSessionHandle);
                        if (oRetValConnect.main_code != "SUCCESS")
                        {
                            GlobalClass.WriteEventLog("Session invalid/expired. Error(ConnectToWorkFlow): " + oRetValConnect.main_code, Service1.sEventSourceName, true);
                        }
                        else
                        {
                            GlobalClass.WriteEventLog("Session invalid/expired. Re-Connected to Workflow", Service1.sEventSourceName, false);
                        }
                        return oRetValConnect.main_code;
                    }
                }

                if (oRetVal.main_code != GlobalClass.CTS_SUCCESS)
                {
                    GlobalClass.WriteEventLog("No WorkItems - " + oRetVal.main_code, Service1.sEventSourceName, false);
                    return oRetVal.main_code;
                }
                if (rsdatatab == null || rsdatatab.DataSet.Tables["Result"] == null) //if no record fetched into DataTable then do nothing
                {
                    GlobalClass.WriteEventLog("rsdatatab = null", Service1.sEventSourceName, false);
                    sReturnValue = "No WorkItems";
                    return sReturnValue;
                }
                else
                {
                    GlobalClass.WriteEventLog("Completer Started for TechSignVerifyCompleter.", Service1.sEventSourceName, false);

                    int count = rsdatatab.DataSet.Tables["Result"].Rows.Count;
                    CTSLibrary.OmniFlow.CTSError udtError;
                    int i = 0;
                    #region
                    foreach (DataRow dtRow in rsdatatab.DataSet.Tables["Result"].Rows)
                    {
                        udtInstrument = new CTSWrapper.clsGlobalDecl.WFInstrument();
                        string[] strAttribNames;
                        string[] strAttribValues;
                        string[] strAttribTypes;

                        string sChequeNumber = dtRow["CHEQUE_NUMBER"].ToString();
                        string sMICRAccountNo = dtRow["MICR_ACC_NO"].ToString();
                        string sAmount = dtRow["AMOUNT"].ToString();
                        //string sMICRAccNo = dtRow["AccountNo"].ToString().Trim();
                        //string sPayeeName = dtRow["PayeeName"].ToString().Trim();

                        udtInstrument.ProcessInstanceId = dtRow["ProcessInstanceID"].ToString();
                        udtInstrument.lWorkItemId = 1;

                        

                        strAttribNames = new string[8];
                        strAttribValues = new string[8];
                        strAttribTypes = new string[8];

                        strAttribNames[0] = "DECTECHVERIFY";
                        if (string.Compare(dtRow["Status"].ToString(), "ACCEPT", true) == 0)
                        {
                            strAttribValues[0] = "ACCEPTED";
                        }
                        else if (string.Compare(dtRow["Status"].ToString(), "REJECT", true) == 0)
                        {
                            strAttribValues[0] = "REJECTED";
                        }
                        else if (string.Compare(dtRow["Status"].ToString(), "REFER", true) == 0)
                        {
                            strAttribValues[0] = "REFERRAL";
                        }
                        strAttribTypes[0] = "0";

                        strAttribNames[1] = "TECHNICALREMARKS";
                        strAttribValues[1] = dtRow["Remarks"].ToString();
                        strAttribTypes[1] = "0";

                        strAttribNames[2] = "TECHNICALREJREASON";
                        strAttribValues[2] = dtRow["REJECT_DESC"].ToString();
                        strAttribTypes[2] = "0";

                        strAttribNames[3] = "USERTECHVERIFY";
                        strAttribValues[3] = GlobalClass.udtUService_Completer.UserName;
                        strAttribTypes[3] = "0";

                        strAttribNames[4] = "FinalRejectcode";
                        strAttribValues[4] = dtRow["REJECT_CODE"].ToString();
                        strAttribTypes[4] = "0";

                        strAttribNames[5] = "FINACLEFILENAME";
                        strAttribValues[5] = dtRow["FILE_NAME"].ToString();
                        strAttribTypes[5] = "0";

                        strAttribNames[6] = "FinalRejReason";
                        strAttribValues[6] = dtRow["REJECT_DESC"].ToString();
                        strAttribTypes[6] = "0";

                        strAttribNames[7] = "FinalChequeStatus";
                        if (string.Compare(dtRow["Status"].ToString(), "ACCEPT", true) == 0)
                        {
                            strAttribValues[7] = "ACCEPTED";
                        }

                        else if (string.Compare(dtRow["Status"].ToString(), "REJECT", true) == 0)
                        {
                            strAttribValues[7] = "REJECTED";
                        }

                        else if (string.Compare(dtRow["Status"].ToString(), "REFER", true) == 0)
                        {
                            strAttribValues[7] = "REFERRAL";
                        }
                        strAttribTypes[7] = "0";



                        i++;

                        udtError = GlobalClass.objOmniFlow.MarkInstrumentComplete(ref udtInstrument, ref strAttribNames, ref strAttribValues, ref strAttribTypes);
                        if (udtError.main_code != GlobalClass.CTS_SUCCESS)
                        {
                            GlobalClass.WriteEventLog("There is an error in completing instrument; instance id - " + udtInstrument.ProcessInstanceId, Service1.sEventSourceName, true);
                            UnlockWorkitem();
                            string sresult = GlobalClass.InsertErrinDB(dtRow["ProcessInstanceID"].ToString(), "Completion Error", udtError.main_code);
                            if (sresult != GlobalClass.CTS_SUCCESS)
                            {
                                return "Error in inserting errors";
                            }

                        }
                        else
                        {
                            GlobalClass.DeleteErrinDB(dtRow["ProcessInstanceID"].ToString());
                            GlobalClass.WriteEventLog("Instruments completed successfully - ", Service1.sEventSourceName, false);
                            oRetVal = DetailsTableUpdateStatus(sMICRAccountNo, sAmount, sChequeNumber);
                        }
                    }
                }


                return GlobalClass.CTS_SUCCESS;
            }
            catch (Exception ex)
            {
                if (udtInstrument.ProcessInstanceId != null)
                {
                    string sProcessInstanceId = udtInstrument.ProcessInstanceId;
                    UnlockWorkitem();
                    string sresult = GlobalClass.InsertErrinDB(sProcessInstanceId, "Exception", ex.Message);
                    if (sresult != GlobalClass.CTS_SUCCESS)
                    {
                        return "Error in inserting errors";
                    }
                }
                return ex.Message;
                #endregion
            }


        }
      
      
        public string UnlockWorkitem()
        {
            CTSLibrary.OmniFlow.CTSError udtError = new OmniFlow.CTSError();
            CTSLibrary.OmniFlow.CTSError oRetValConnect = new OmniFlow.CTSError();
            try
            {    
                if (udtInstrument.ProcessInstanceId != null)
                {
                    udtError = GlobalClass.objOmniFlow.UnLockWorkitem(ref udtInstrument);

                    if (udtError.main_code.ToUpper() == "Invalid session handle.".ToUpper())
                    {
                        oRetValConnect = GlobalClass.objOmniFlow.ConnectToWorkFlow(GlobalClass.udtUService_DBEssential.CabinetName, GlobalClass.udtUService_Completer.UserName, GlobalClass.udtUService_Completer.Password, 1, ref GlobalClass.oudtWMTSessionHandle);
                        if (oRetValConnect.main_code != "SUCCESS")
                        {
                            GlobalClass.WriteEventLog("Error in Unlocking Instrument - Session invalid/expired. Error(ConnectToWorkFlow): " + oRetValConnect.main_code, Service1.sEventSourceName, true);
                            return "FAILURE";
                        }
                        else
                        {
                            GlobalClass.WriteEventLog("Session invalid/expired. Re-Connected to Workflow", Service1.sEventSourceName, false);
                            udtError.main_code = string.Empty;
                            udtError = GlobalClass.objOmniFlow.UnLockWorkitem(ref udtInstrument);
                        }
                    }
                    if (udtError.main_code != GlobalClass.CTS_SUCCESS)
                    {
                        throw new Exception("Error in Unlocking Instrument: " + udtError.main_code);
                    }
                    GlobalClass.WriteEventLog("Instrument Unlocked Successfully. ProcessInstanceID:-" + udtInstrument.ProcessInstanceId, Service1.sEventSourceName, false);
                }
                return GlobalClass.CTS_SUCCESS;
            }

            catch (Exception Ex)
            {
                GlobalClass.WriteEventLog(Ex.Message, Service1.sEventSourceName, true);
                return Ex.Message;
            }
        }

        private static OmniFlow.CTSError DetailsTableUpdateStatus(string AccountNo, string Amount, string ChequeNo)
        {
            String sQuerType;
            String sSqlQuery;
            String sWebResult;
            DataTable dt = new DataTable();
            OmniFlow.CTSError udtError = new OmniFlow.CTSError();

            try
            {
                udtError.main_code = GlobalClass.CTS_FAIL;

                Webservice.DBParameter[] DBParam = new Webservice.DBParameter[3];

                DBParam[0].strParamName = "ChequeNo";
                DBParam[0].strParamType = "VARCHAR";
                DBParam[0].strParamValue = ChequeNo;
                DBParam[0].strParamDirection = "in";
                DBParam[0].intParamLength = ChequeNo.Length;

                DBParam[1].strParamName = "AccountNo";
                DBParam[1].strParamType = "VARCHAR";
                DBParam[1].strParamValue = AccountNo;
                DBParam[1].strParamDirection = "in";
                DBParam[1].intParamLength = AccountNo.Length;

                DBParam[2].strParamName = "Amount";
                DBParam[2].strParamType = "VARCHAR";
                DBParam[2].strParamValue = Amount;
                DBParam[2].strParamDirection = "in";
                DBParam[2].intParamLength = Amount.Length;


                sSqlQuery = "CHG_Inward_UpdateDetailsTable";
                sQuerType = "storedproc";

                sWebResult = Webservice.ExecuteDBOperation(sSqlQuery, sQuerType, DBParam, ref dt);
                if (sWebResult != "SUCCESS")
                {
                    udtError.main_code = GlobalClass.CTS_FAIL;
                    return udtError;
                }

                udtError.main_code = GlobalClass.CTS_SUCCESS;
                return udtError;
            }
            catch (Exception ex)
            {
                GlobalClass.WriteEventLog("Error in Updating IsUploaded Value in ReaderDetailsTable table." + ex.Message + ". StackTrace: " + ex.StackTrace, Service1.sEventSourceName, true);
                udtError.main_code = GlobalClass.CTS_FAIL;
                return udtError;
            }
        }
    }
}