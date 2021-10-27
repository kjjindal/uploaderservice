using System;
using System.Diagnostics;
//using System.Linq;
using System.ServiceProcess;
using System.Threading;
using CTSLibrary;

namespace UploaderService
{
    public partial class Service1 : ServiceBase
    {
        private static Thread workerThread;
        public static string sEventSourceName = "uploadservice";
        public static string Filepath = @"C:\Users\kalpit.jindal\Desktop\ChequeDetails.csv";
        public static int iSleepInterval;
        private bool bStopFlag = false;
        private static object locker = new object();
        private static bool isStopCommandFired;
        private static AutoResetEvent ev;
        GlobalClass.UService_ReadCsvFile[] objCXFDet = null;
        TaskInfo ti;
       
        public class TaskInfo
        {
            public RegisteredWaitHandle Handle = null;
            public string OtherInfo = "default";
        }
        CTSWrapper.clsGlobalDecl.WFInstrument udtInstrument;



        private static void ItemComplete()
        {
            bool bIterate = true;
            string sRetval = "";
            try
            {
                while (bIterate)
                {
                    lock (locker)
                    {
                        if (!isStopCommandFired)
                        {
                            ItemCompleter objItemCompleter = new ItemCompleter();
                            sRetval = objItemCompleter.TechItemCompleter();
                            if (sRetval != GlobalClass.CTS_SUCCESS)
                            {
                                GlobalClass.WriteEventLog("Error in TechItemCompleter() " + sRetval, sEventSourceName, true);
                                Thread.Sleep(TimeSpan.FromSeconds(GlobalClass.udtUService_Completer.TimeInterval));
                            }
                            ev.Set();  //Service is still running
                        }
                        else
                        {
                            //To stop the thread from polling after the stop command is fired.
                            bIterate = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalClass.WriteEventLog("Exception in ItemComplete()" + ex.Message, sEventSourceName, true);
            }

        }
        public static void WaitProcedure(object state, bool timedOut)
        {
            TaskInfo tinfo = (TaskInfo)state;
            if (timedOut)
            {
                GlobalClass.WriteEventLog("Service Hanged: Tech Sign Verify Completer Service.", sEventSourceName, false);
            }
        }

        public Service1()
        {
            InitializeComponent();
            //AutoResetEvent maintains a boolean variable in memory.
            //If the boolean variable is false then it blocks the thread and
            //if the boolean variable is true it unblocks the thread.
            //When we instantiate an AutoResetEvent object,
            //we pass the default value of boolean value in the constructor.
            ev = new AutoResetEvent(false);
            ti = new TaskInfo();
            ti.OtherInfo = "First time";
            //The RegisterWaitForSingleObject method checks the current state of the specified object's WaitHandle.
            ti.Handle = ThreadPool.RegisterWaitForSingleObject(ev, new WaitOrTimerCallback(WaitProcedure), ti, TimeSpan.FromMinutes(10), false);
        }



        public void OnStart()
        {
            var sReadCsvFile=GlobalClass.ReadCsvFile(Filepath,ref objCXFDet);
            if (sReadCsvFile == "SUCCESS")
            {
                for(Int32 rowcount = 0; rowcount <=objCXFDet.GetUpperBound(0); rowcount++)
                {
                   var sResult = GlobalClass.WF_DUploadNewInstrument(objCXFDet[rowcount]);


                }


            }
            else
            {
                GlobalClass.WriteEventLog("Failed during read csv file ",sEventSourceName,true);
            }



            string sRetVal = "";

            string sCabname, sCabUser, sCabPswd;
            OmniFlow.CTSError oRetVal = new OmniFlow.CTSError();
            try
            {
                string[] cmdLineArgs = Environment.GetCommandLineArgs();

                if (cmdLineArgs.Length == 0)
                {
                    bStopFlag = true;
                    this.OnStop();
                    return;
                }
                sEventSourceName = cmdLineArgs[1] + sEventSourceName;
                if (EventLog.SourceExists(sEventSourceName))
                {
                    EventLog elog = new EventLog(sEventSourceName);
                    elog.Clear();
                }

                sRetVal = GlobalClass.ReadConfig();
                if (sRetVal != GlobalClass.CTS_SUCCESS)
                {
                    bStopFlag = true;
                    GlobalClass.WriteEventLog("Error in reading Config File.", sEventSourceName, true);
                    OnStop();
                    return;
                }

                //if (string.Compare(GlobalClass.udtUService_Completer.useServiceForTechVerification, "Y", true) != 0)
                //{
                //    bStopFlag = true;
                //    GlobalClass.WriteEventLog("Service is Not Required for Tech Verification. Therefore, Service Stopped.", sEventSourceName, true);
                //    OnStop();
                //    return;
                //}

                Webservice.WebserviceURL = GlobalClass.udtUService_DBEssential.DBServerURL;
                Webservice.DBType = GlobalClass.udtUService_DBEssential.DBType;
                Webservice.DBConnectionString = GlobalClass.udtUService_DBEssential.DBConnectionString;

                sCabname = GlobalClass.udtUService_DBEssential.CabinetName;
                sCabUser = GlobalClass.udtUService_Completer.UserName;
                sCabPswd = GlobalClass.udtUService_Completer.Password;
                GlobalClass.WriteEventLog(sCabname + " " + sCabUser + " ", sEventSourceName, false);

                GlobalClass.objOmniFlow.ImageServerIP = GlobalClass.udtUService_DBEssential.ImageServerIP;
                GlobalClass.objOmniFlow.VolumeName = GlobalClass.udtUService_DBEssential.VolumeName;
                GlobalClass.objOmniFlow.PortId = GlobalClass.udtUService_DBEssential.PortId;
                GlobalClass.objOmniFlow.VolId = GlobalClass.udtUService_DBEssential.VolId;
                GlobalClass.objOmniFlow.ProcessId = GlobalClass.udtUService_Completer.ProcessID;




                oRetVal = GlobalClass.objOmniFlow.ConnectToWorkFlow(sCabname, sCabUser, sCabPswd, 1, ref GlobalClass.oudtWMTSessionHandle);
                if (oRetVal.main_code != "SUCCESS")
                {
                    bStopFlag = true;
                    GlobalClass.WriteEventLog("Error(ConnectToWorkFlow): " + oRetVal.main_code, sEventSourceName, true);
                    OnStop();
                    return;
                }
                else
                    GlobalClass.WriteEventLog("Connected to Workflow", sEventSourceName, false);

                workerThread = new Thread(new ThreadStart(ItemComplete));
                workerThread.Name = "Worker Thread";
                workerThread.IsBackground = true;
                Thread.CurrentThread.Name = "Start Thread";
                workerThread.Start();
                GlobalClass.WriteEventLog(Thread.CurrentThread.Name, sEventSourceName, false);

            }
            catch (Exception ex)
            {
                GlobalClass.WriteEventLog("Exception(On Start): " + ex.Message, sEventSourceName, true);
            }


        }

        public void OnStop()
        {
            OmniFlow.CTSError oRetValConnect = new OmniFlow.CTSError();
            oRetValConnect = GlobalClass.objOmniFlow.DisconnectFromWorkflow();
            if (oRetValConnect.main_code != "SUCCESS")
            {
                GlobalClass.WriteEventLog("Error in disconecting from workflow" + oRetValConnect.main_code, Service1.sEventSourceName, true);
            }
            else
            {
                GlobalClass.WriteEventLog("Disconnected from workflow.",Service1.sEventSourceName, false);
            }
            GlobalClass.WriteEventLog("Service is Stopped.", sEventSourceName, false);
        }
    }
}
