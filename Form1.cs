using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
//using Excel = Microsoft.Office.Interop.Excel;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO.Ports;
using System.Text;
using System.Net.Sockets;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel; // For .xlsx files
using NPOI.HSSF.UserModel; // For .xls files
using TMath_Lib;
using DevTool.Save;
using System.Threading;
using Advantech.Motion;
using Aerotech.A3200;
using Aerotech.Common;
using Aerotech.A3200.Callbacks;
using Aerotech.A3200.Exceptions;
using Aerotech.A3200.Status;
using Aerotech.A3200.Variables;
using Aerotech.A3200.Status.Custom;
using CHPT;//---
using Module_Interface;
using Powermeter_API;
using PylonView;


//20251009 AZD-KED TEMP-CONTROL
namespace LCSBM
{
    enum enum_stepMoveOB
    {
        MoveOB_Ready,
        MoveOB_Start,
        LaserFileLoad,
        OpenAir,
        OpenLaser,
        CloseLaser,
        CloseAir,
    }
    enum enum_stepAutoMoveOB
    {
        AutoMoveOB_Ready,
        AutoMoveOB_Start,
        LaserFileLoad,
        OpenAir,
        OpenLaser,
        CloseLaser,
        CloseAir,
        MoveOB_End
    }

    enum enum_LaserType
    {

        Power = 1,
        GND,
        Signal,
    }

    public partial class Form1 : Form
    {
        #region 宣告      
        private Stopwatch tact_time2 = new Stopwatch();
        private ConfigSystem _ConfigSystem;
        private bool _Using = false;
        private string _version = "LCSBM v01.001";
        private const string _paramPath = @"C:\LCSBM Param";
        //資訊
        /// <summary>
        /// 機械位置資訊載入成功
        /// </summary>
        private bool _msLoadFlag = false;
        private string _tmpFilePath;
        private string _ezmPath = Application.StartupPath + @"\System\LCSBM.ezm";
        private Define.MS_PosData _msPosData;
        public Define.SB_File _recipeData;
        public Define.Position Pos;
        public string RecipeFileName = "";

        //紀錄
        public static DevTool.Save.TNewLog MList_Log;
        public static DevTool.Save.TNewLog Error_Log;
        // balser camera
        private PylonCamera _pylonCamera;
        PylonViewForm _viewForm;
        Stopwatch tact_time = new Stopwatch();

        //旗標
        public int SystemSetUp_Flag = 0;
        public bool M_UI_Running;
        private bool _DiskLockFlag = false;
        private readonly SemaphoreSlim _diskRotateSemaphore = new SemaphoreSlim(1, 1);  // 新增 async-friendly 鎖
        private bool _AZD_Rotating = false;
        /// <summary>
        /// 氣壓更新
        /// </summary>
        private bool _FestoUpdateFlag = false;
        /// <summary>
        /// 比例閥更新
        /// </summary>
        private bool _ProportionalValveFlag = false;
        //執行緒
        public Thread _Log_Thread;
        //P+F io-link
        public FESTO_SPAN FestoPressure;
        public EthernetIP_Connect P_F_EthernetIP;
        // device connected?
        private bool _AxMarkConnected = false;
        private bool _aerotechConnected = false;
        private bool _orientalConnected = false;
        private bool _CL_Connected = false;
        private bool _CVX_Connected = false;
        private bool _IO_Connected = false;
        private bool _heaterConnnected = false;
        private bool _IO_LinkConnect = false;
        private bool _PowerMeterConnect = false;
        private bool _TH_SensorConnect = false;
        private bool _PizeoConnect = false;

        /// <summary>
        /// ST Panel資訊
        /// </summary> 
        ST_POS_PanelGenerate[] ST_Panel = new ST_POS_PanelGenerate[1];
        ST_Select_PanelGenerate[] ST_SelectPanel = new ST_Select_PanelGenerate[1];

        #region 生產資訊
        /// <summary>
        /// 總植球顆數
        /// </summary>
        public int TotalSB_Count = 0;
        /// <summary>
        /// 已完成 數
        /// </summary>
        public int FinishSB_Count = 0;
        /// <summary>
        /// 尚未植球 數
        /// </summary>
        public int UnDoneSB_Count = 0;
        /// <summary>
        /// 載台真空(Kpa)
        /// </summary>
        public double StageVacuum = 0;
        /// <summary>
        /// 吸嘴當前壓力(Kpa)
        /// </summary>
        public double NowNozzlePressure = 0;
        /// <summary>
        /// 吸嘴當前MAX壓力(Kpa)
        /// </summary>
        public double NowNozzle_MAX_Pressure = 0;
        /// <summary>
        /// 吸嘴當前Min壓力(Kpa)
        /// </summary>
        public double NowNozzle_MIN_Pressure = 0;
        /// <summary>
        /// 吸嘴比例閥設定值
        /// </summary>
        public ushort NozzleValveSetValue = 0;
        /// <summary>
        /// 吸嘴比例閥實際值
        /// </summary>
        public ushort NozzleValveActValue = 0;
        /// <summary>
        /// 當前PadType設定
        /// </summary>
        public int Old_PadType = 0;
        #endregion

        #region Aerotech Axis
        //Thread _t_AxisStatus;
        private Controller _Aerotech_Controller;
        private CustomDiagnostics _customDiagnostics;
        CustomDiagnosticsResults _result;
        private int taskIndex;
        Aerotech.A3200.Tasks.TaskStatus _TaskStatus;


        /// <summary>
        /// 硬體狀態 - 原點
        /// </summary>
        bool HOME_X = false, HOME_Y = false, HOME_Z = false;
        /// <summary>
        /// 硬體狀態 - 移動完成
        /// </summary>
        bool MOVEDONE_X = false, MOVEDONE_Y = false, MOVEDONE_Z = false;
        /// <summary>
        /// 硬體狀態 - XYZ 原點
        /// </summary>
        bool Aerotech_Home = false;
        /// <summary>
        /// Server ON/OFF
        /// </summary>
        bool En_X = false, En_Y = false, En_Z = false;
        /// <summary>
        /// XYZ Server ON/OFF
        /// </summary>
        bool Aerotech_En = false;
        /// <summary>
        /// 軸是否待機中(InPosition)
        /// </summary>
        bool Motion_X = false, Motion_Y = false, Motion_Z = false;
        /// <summary>
        /// 硬體狀態 - 正極限
        /// </summary>
        bool Limit_XP, Limit_YP, Limit_ZP;
        /// <summary>
        /// 硬體狀態 - 負極限
        /// </summary>
        bool Limit_XN, Limit_YN, Limit_ZN;
        bool Thread_Enabled = true;
        double Distance_X = 0, Distance_Y = 0, Distance_Z = 0, M_SB_Auto_Distance_Z = 0;
        double Speed_X = 0, Speed_Y = 0, Speed_Z = 0, Speed_Q = 0;

        /// <summary>
        /// 軸現在位置
        /// </summary>
        double Now_X = 0, Now_Y = 0, Now_Z = 0, Now_Q = 0, Now_ActQ;
        #endregion

        #region 東方 Axis
        private AZD_Controller_OrientalMotor_Interface.AZD_Controller_OrientalMotor _AZD_Controller;
        DEV_LIST[] CurAvailableDevs = new DEV_LIST[Motion.MAX_DEVICES];
        uint deviceCount = 0;
        uint DeviceNum = 0;
        IntPtr m_DeviceHandle = IntPtr.Zero;
        IntPtr[] m_Axishand = new IntPtr[32];
        uint m_ulAxisCount = 0;
        uint _returnCode = 0;
        UInt16 AxState = new UInt16();
        string strTemp = "";
        UInt32 IOStatus = new UInt32();
        double azd_CMD_Pos = 0;
        double azd_Act_Pos = 0;
        //private AzInternalIO AZD_Status;
        private readonly double AZD_MotorResolution = 1000;
        /// <summary>
        /// Q Server ON/OFF
        /// </summary>
        private bool AZD_En;
        /// <summary>
        /// Q軸電機狀態
        /// </summary>
        private bool AZD_Rdy;
        /// <summary>
        /// Q軸alarm狀態
        /// </summary>
        private bool AZD_Alarm;
        /// <summary>
        /// 軸是否待機中(InPosition)
        /// </summary>
        private bool AZD_Motion;
        private int azd_value;


        int azd_axisspeed;
        uint azd_upRate;
        uint azd_downRate;

        private bool AZD_ReadLock;
        #endregion

        #region 測距        
        bool _M_Keyence_Height_flag = false;
        readonly string Keyence_Height_Send_Data = "MS,0,1" + "\r";
        readonly CHPT_TCP _Keyence_Height = new CHPT_TCP();
        Thread _t_Keyence_Height_Send;
        Thread _t_Keyence_Height_Rev;
        string KeyenceHeight_Value;
        #endregion

        #region 視覺
        bool _Keyence_CVX_flag = false;
        TCP_IP_Client.TCPServerAP _Keyence_CVX;
        VTWP_LSG075A_4_Control _LSG075A_4_Control;
        SerialPort _CoaxialLight, _RingLight;
        //調光器
        byte[] SendData_Light = new byte[9];
        byte Sum_Light;
        #endregion

        #region IO卡
        private Automation.BDaq.InstantDiCtrl instantDiCtrl1 = new Automation.BDaq.InstantDiCtrl();
        private Automation.BDaq.InstantDoCtrl instantDoCtrl1 = new Automation.BDaq.InstantDoCtrl();
        private Automation.BDaq.InstantDiCtrl instantDiCtrl2 = new Automation.BDaq.InstantDiCtrl();
        private Automation.BDaq.InstantDoCtrl instantDoCtrl2 = new Automation.BDaq.InstantDoCtrl();
        byte[] IOCard_Input1 = new byte[2];
        byte[] IOCard_Input2 = new byte[2];

        int[] IOCard_Input1_Value = new int[2];
        int[] IOCard_Input2_Value = new int[2];

        byte[,] IOCard_Output = new byte[2, 2];
        bool[,] IOCard_DOValue = new bool[2, 16];

        int[] IO_ON_Vaule = { 1, 2, 4, 8, 16, 32, 64, 128 };
        int[] IO_OFF_Vaule = { 0xfe, 0xfd, 0xfb, 0xf7, 0xef, 0xdf, 0xbf, 0x7f };
        #endregion

        #region ADAM-4024 E5CC
        //private ADAM4024_Control ADAM_4024 = new ADAM4024_Control("ADAM4024_Control");
        private TOMRON_E5CC E5CC = new TOMRON_E5CC();
        #endregion

        /// <summary>
        /// 溫溼度計
        /// </summary>
        private THSensor_Interface.THSensor _THSensor;
        /// <summary>
        /// 1、2 TH  3 oxygen
        /// </summary>
        private THSensor_Interface.THSensor.sensor[] _TH_Data = new THSensor_Interface.THSensor.sensor[3];

        /// <summary>
        /// 功率計
        /// </summary>
        Powermeter _powermeter;
        /// <summary>
        /// 雷射校驗資料
        /// </summary>
        List<LaserPos> _laserPosList = new List<LaserPos>();

        /// <summary>
        /// 壓電馬達
        /// </summary>
        Mechonics_CU30CL _CU30CL;
        private bool _pm_DataCoollect = false;
        bool PM_DataCollect
        {
            set
            {
                if (value)
                    _PM_Data.Clear();
                _pm_DataCoollect = value;
            }
            get
            {
                return _pm_DataCoollect;
            }
        }
        List<double> _PM_Data = new List<double>();
        /// <summary>
        /// 雷射校驗 PM值
        /// </summary>
        double AlignPM = 0;
        ///<summary>
        ///錯誤訊息
        ///</summary>
        string ReturnErr = "";

        #region MarkingMate雷射控制卡 
        ///<summary>
        ///圖層名稱
        /// </summary>
        string StrName = "";
        ///<summary>
        ///圖層子物件-點1名稱
        /// </summary>
        string StrSpotName1 = "";
        ///<summary>
        ///圖層子物件-點2名稱
        /// </summary>
        string StrSpotName2 = "";
        /// <summary>
        /// 圖層子物件-延遲時間
        /// </summary>
        string StrDelayName = "";
        ///<summary>
        ///出光狀態
        ///</summary>
        bool Trigger = false;
        /// <summary>
        /// 雷射關閉狀態
        /// </summary>
        bool LaserStatus_Emission = false;
        /// <summary>
        /// 出光秒數
        /// </summary>
        int SB_ALaserTimes = 0;
        #endregion

        #region UI物件
        PictureBox[] _DI1picture;
        Button[] _DO1button;
        PictureBox[] _DO1picture;

        PictureBox[] _DI2picture;
        Button[] _DO2button;
        PictureBox[] _DO2picture;
        #endregion ㄒ            

        #region 單循環 Thread


        int _t_M_SB_Laser_flag;
        Thread _t_M_SB_Laser;
        int _t_M_SB_Laser_flag2;
        Thread _t_M_SB_Laser2;

        int _t_M_AutoFocus_flag;
        Thread _t_M_AutoFocus;
        double _t_M_AF_Zoffset;
        bool _t_M_AF_UIUpdata;

        int _t_M_MoveToLaser_flag;
        Thread _t_M_MoveToLaser;
        int _t_M_MoveToLaser_flag2;
        Thread _t_M_MoveToLaser2;
        int _t_M_MoveToLaser_flag3;
        Thread _t_M_MoveToLaser3;

        Thread _t_M_Electric;
        bool _M_Laser_Air;

        #endregion

        #region 平台溫度補償
        /// <summary>
        /// 平台設定溫度
        /// </summary>
        double _TableTempSet = 70;
        Dictionary<string, double> TableTemp_Dict = new Dictionary<string, double>();
        int ControlTemp_Count = 5;
        /// <summary>
		/// tempurature list 讀取
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public Boolean TableTemp_Load(string file)
        {
            Boolean result = false;
            string tmp = "ControlTemp_";
            string key = "";
            int index = 0;
            int startIndex = 120;
            try
            {
                for (int i = 0; i < ControlTemp_Count; i++)
                {
                    index = (startIndex + 10 * i);
                    key = tmp + index.ToString();
                    TableTemp_Dict.Add(key, double.Parse(IniReadValue("ControlTemp", key, file)));
                }
                result = true;
                TempTableUpdate();
            }
            catch (Exception ex)
            {

            }
            return result;
        }

        public bool TempTableUpdate()
        {
            bool result = false;
            try
            {
                foreach (var data in TableTemp_Dict)
                {
                    dgv_TableTemp.Rows.Add(new object[] { data.Key, data.Value });
                }
            }
            catch (Exception ex)
            {
            }


            return result;

        }

        public bool TableTempeSave(string file)
        {
            bool result = false;
            string tmp = "ControlTemp_";
            string key = "";
            int index = 0;

            try
            {
                for (int i = 0; i < ControlTemp_Count; i++)
                {
                    index += 5;
                    key = tmp + index.ToString();
                    WritePrivateProfileString("ControlTemp", key, dgv_TableTemp.Rows[i].Cells[1].Value.ToString(), file);

                    TableTemp_Dict[key] = double.Parse(dgv_TableTemp.Rows[i].Cells[1].Value.ToString());

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"index[{index}] {ex.ToString()}");
            }

            return result;
        }

        /// <summary>
        /// 平台溫度轉換
        /// </summary>
        /// <param name="tableTemp">平台溫度</param>
        /// <param name="controlTemp">溫控器溫度</param>
        /// <returns></returns>
        public bool TableTempToAttenuator(double tableTemp, out double controlTemp)
        {
            bool result = false;
            string tmp = "ControlTemp_";
            string key = "";
            string pre_key = "";
            int _ControlTempIndex = 0;//起始設定溫度
            int _ControlStart = 120;//起始設定溫度
            double min_tableTemp, max_tableTemp;
            controlTemp = 0;
            try
            {
                for (int i = 0; i < ControlTemp_Count; i++)
                {
                    _ControlTempIndex = _ControlStart + 10 * i;
                    key = tmp + _ControlTempIndex.ToString();

                    if (tableTemp <= TableTemp_Dict[key])
                    {
                        if (i == 0)
                        {
                            min_tableTemp = TableTemp_Dict[key];
                            max_tableTemp = TableTemp_Dict[key];
                        }
                        else
                        {
                            pre_key = tmp + (_ControlTempIndex - 10).ToString();
                            min_tableTemp = TableTemp_Dict[pre_key];
                            max_tableTemp = TableTemp_Dict[key];
                        }

                        controlTemp = Select_SectionTemp(tableTemp, pre_key, key, min_tableTemp, max_tableTemp);

                        if (controlTemp != 0)
                        {
                            result = true;
                            return result;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
            }

            return result;
        }

        /// <summary>
        /// 溫度區間計算
        /// </summary>
        /// <param name="tableTemp">需求平台溫度</param>
        /// <param name="min_controlT">最小溫控器溫度</param>
        /// <param name="max_controlT">最大溫控器溫度</param>
        /// <param name="min_tableT">最小平台溫度</param>
        /// <param name="max_tableT">最大平台溫度</param>

        /// <returns>所需溫控器溫度</returns>
        private double Select_SectionTemp(double tableTemp, string min_controlT, string max_controlT, double min_tableT, double max_tableT)
        {
            double result = 0;
            int interval = 10; // 1度為調整單位
            try
            {
                double min_cT = double.Parse(min_controlT.Replace("ControlTemp_", ""));
                double max_cT = double.Parse(max_controlT.Replace("ControlTemp_", ""));
                double pitch_ControlTemp = (max_cT - min_cT) / interval;
                double pitch_TableTemp = (max_tableT - min_tableT) / interval;

                for (int i = 0; i <= interval; i++)
                {
                    if (tableTemp >= min_tableT && tableTemp <= (min_tableT + pitch_TableTemp * i))
                    {
                        result = min_cT + (pitch_ControlTemp * (i));
                        return result;
                    }
                    //else if (tableTemp > (min_tableT + pitch_TableTemp * i) && tableTemp <= (min_tableT + pitch_TableTemp * i ))
                    //{
                    //    result = min_cT + (pitch_ControlTemp * i);
                    //    return result;
                    //}
                }
            }
            catch (Exception ex)
            {
            }

            return result;
        }

        /// <summary>
		/// 平台工作溫度設定		
		/// </summary>
		/// <param name="temp">平台溫度</param>
		/// <returns>finish</returns>`
		private bool TableTemp_Set(double tabletemp)
        {
            bool result = false;
            double ctrTemp = 0;
            if (TableTempToAttenuator(tabletemp, out ctrTemp))
            {

                E5CC.SV_Set(ctrTemp);
            }

            return result;
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        private static string IniReadValue(string Section, string Key, string path)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp, 255, path);
            return temp.ToString();
        }
        #endregion
        delegate void dTimer();

        #endregion

        #region 循環流程用之變數宣告
        public BondData _BondData = new BondData();

        /// <summary>
        /// 集合ST編號給UI介面選取
        /// </summary>
        public List<int> List_ST_No = new List<int>();
        /// <summary>
        /// 集合PAD編號給UI介面選取
        /// </summary>
        public List<int> List_PAD_No = new List<int>();
        /// <summary>
        /// 集合PAD上錫珠數量編號給UI介面選取
        /// </summary>
        public List<int> List_SubPAD_No = new List<int>();
        public HMI_Position HMI;
        /// <summary>
        /// 人機位置(自動流程、單動循環、定點...等等相關位置)
        /// </summary>
        public class HMI_Position
        {
            /// <summary>
            /// 復歸原點位置(僅需XYZQ)
            /// </summary>
            public Define.Pos_Data Pos_Home;
            /// <summary>
            /// CCD中心對載台基準點(左下角)位置(僅需XYZQ)
            /// </summary>
            public Define.Pos_Data Pos_BaseCCD;
            /// <summary>
            /// 載台對位位置(僅需XYZQ)
            /// </summary>
            public Define.Pos_Data Pos_Grab_Table;
            /// <summary>
            /// 載台對位修正量(僅需XY)
            /// </summary>
            public Define.Pos_Data Offset_Grab_Table;
            /// <summary>
            /// 載台對位修正位置(僅需XY)(位置+修正量)
            /// </summary>
            public Define.Pos_Data Cal_Grab_Table;

            private int _Select_Num_ST = 0;
            /// <summary>
            /// 選擇第幾顆ST編號
            /// </summary>
            public int Select_Num_ST
            {
                get
                {
                    return _Select_Num_ST;
                }
                set
                {
                    if (Pos_ST_List != null && Check_Select_Num_ST(value))
                    {
                        _Select_Num_ST = value;
                    }
                }
            }
            public bool Check_Select_Num_ST(int _select_no)
            {
                bool result = false;
                if (Pos_ST_List.ContainsKey(_select_no))
                {
                    result = true;
                }

                return result;
            }

            private int _Select_Num_PAD = 0;
            /// <summary>
            /// 選擇第幾顆PAD編號
            /// </summary>
            public int Select_Num_PAD
            {
                get
                {
                    return _Select_Num_PAD;
                }
                set
                {
                    if (Pos_PAD_List != null && Check_Select_Num_PAD(value))
                    {
                        _Select_Num_PAD = value;
                    }
                }
            }
            public bool Check_Select_Num_PAD(int _select_no)
            {
                bool result = false;

                if (Pos_PAD_List.ContainsKey(_select_no))
                {
                    result = true;
                }

                return result;
            }


            private int _Select_Num_SubPAD = 0;
            /// <summary>
            /// PAD選擇第幾顆錫珠編號
            /// </summary>
            public int Select_Num_SubPAD
            {
                get
                {
                    return _Select_Num_SubPAD;
                }
                set
                {
                    _Select_Num_SubPAD = value;
                }
            }
            /// <summary>
            /// ST對位位置(僅需XYZQ)，由Pos_ST_List中取其一
            /// </summary>
            public Define.Pos_Data Pos_Grab_ST;
            /// <summary>
            /// ST對位修正量(僅需XY)
            /// </summary>
            public Define.Pos_Data Offset_Grab_ST;
            /// <summary>
            /// ST對位修正位置(僅需XY)(位置+修正量)，由Pos_ST_List中取其一
            /// </summary>
            public Define.Pos_Data Cal_Grab_ST;
            /// <summary>
            /// PAD位置(僅需XYZQ)，由Pos_PAD_List中取其一
            /// </summary>
            public Define.Pos_Data Pos_PAD;
            /// <summary>
            /// PAD修正位置(僅需XY)(位置+修正量)，由Pos_PAD_List中取其一
            /// </summary>
            public Define.Pos_Data Cal_PAD;
            /// <summary>
            /// PAD錫珠位置(僅需XYZQ)，由Pos_SubPAD_List中取其一
            /// </summary>
            public Define.Pos_Data Pos_SubPAD;
            /// <summary>
            /// PAD錫珠修正位置(僅需XY)(位置+修正量)，由Pos_SubPAD_List中取其一
            /// </summary>
            public Define.Pos_Data Cal_SubPAD;

            /// <summary>
            /// Table內所有ST位置資訊(讀檔後匯入)
            /// <para>TKey：ST編號，TValue：座標位置</para>
            /// </summary>
            public Dictionary<int, Define.Pos_Data> Pos_ST_List;
            /// <summary>
            /// ST內所有PAD位置資訊(讀檔後匯入)
            /// <para>TKey：PAD編號，TValue：座標位置</para>
            /// </summary>
            public Dictionary<int, Define.Pos_Data> Pos_PAD_List;
            /// <summary>
            /// ST內PAD上所有的錫珠位置資訊(讀檔後匯入)
            /// <para>TKey：PAD編號+錫珠編號(如1_1)，TValue：座標位置</para>
            /// </summary>
            public Dictionary<string, Define.Pos_Data> Pos_SubPAD_List;
            /// <summary>
            /// Table內所有ST位置資訊(位置+修正量)
            /// <para>TKey：ST編號，TValue：座標位置</para>
            /// </summary>
            public Dictionary<int, Define.Pos_Data> Cal_ST_List;
            /// <summary>
            /// ST內所有PAD位置資訊(位置+修正量)
            /// <para>TKey：PAD編號，TValue：座標位置</para>
            /// </summary>
            public Dictionary<int, Define.Pos_Data> Cal_PAD_List;
            /// <summary>
            /// ST內PAD上所有的錫珠位置資訊(位置+修正量)
            /// <para>TKey：PAD編號+錫珠編號(如1_1)，TValue：座標位置</para>
            /// </summary>
            public Dictionary<string, Define.Pos_Data> Cal_SubPAD_List;

            public HMI_Position()
            {
                Pos_Home = new Define.Pos_Data();
                Pos_BaseCCD = new Define.Pos_Data();
                Pos_Grab_Table = new Define.Pos_Data();
                Offset_Grab_Table = new Define.Pos_Data();
                Cal_Grab_Table = new Define.Pos_Data();
                Select_Num_ST = 0;
                Select_Num_PAD = 0;
                Pos_Grab_ST = new Define.Pos_Data();
                Offset_Grab_ST = new Define.Pos_Data();
                Cal_Grab_ST = new Define.Pos_Data();
                Pos_PAD = new Define.Pos_Data();
                Cal_PAD = new Define.Pos_Data();
                Pos_SubPAD = new Define.Pos_Data();
                Cal_SubPAD = new Define.Pos_Data();

                Pos_ST_List = new Dictionary<int, Define.Pos_Data>();
                Pos_PAD_List = new Dictionary<int, Define.Pos_Data>();
                Pos_SubPAD_List = new Dictionary<string, Define.Pos_Data>();

                Cal_ST_List = new Dictionary<int, Define.Pos_Data>();
                Cal_PAD_List = new Dictionary<int, Define.Pos_Data>();
                Cal_SubPAD_List = new Dictionary<string, Define.Pos_Data>();
            }
        }
        #endregion

        public Form1()
        {
            try
            {
                ST_Panel[0] = new ST_POS_PanelGenerate(168, 135, "ST_", 1);
                ST_SelectPanel[0] = new ST_Select_PanelGenerate(150, 95, "ST_", 1);
                this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
                this.UpdateStyles();

                InitializeComponent();

                //檔案相關
                _msPosData = new Define.MS_PosData(_paramPath);
                _recipeData = new Define.SB_File(_paramPath);
                TableTemp_Load(_paramPath + "\\System\\TableTemp.ini");
                //ST座標相關
                Pos = new Define.Position();
                HMI = new HMI_Position();

                //記錄相關
                MList_Log = new DevTool.Save.TNewLog();
                MList_Log.Save_Path = Application.StartupPath + "\\Log\\List\\";
                MList_Log.LogDays = 365;
                if (!Directory.Exists(MList_Log.Save_Path))
                {
                    Directory.CreateDirectory(MList_Log.Save_Path);
                }
                MList_Log.LogNote = "List_";

                Error_Log = new DevTool.Save.TNewLog();
                Error_Log.Save_Path = Application.StartupPath + "\\Log\\Error\\";
                Error_Log.LogDays = 365;
                if (!Directory.Exists(Error_Log.Save_Path))
                {
                    Directory.CreateDirectory(Error_Log.Save_Path);
                }
                Error_Log.LogNote = "Error_";

                _Log_Thread = new Thread(_Log);
                _Log_Thread.Start();

                lbl_M_AG_ZOffset.Text = "0";
            }
            catch (Exception ex)
            {
                Error_Log.Add($"[Init Error]:　{ex.ToString()}");
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                string tt = "";
                hsb_NozzleValve.Value = 0;
                //關閉機械位置頁
                enablePage(tabMS_Param, false);
                enablePage(tab_MountMap, false);
                enablePage(tabAxMMark, false);
                enablePage(tabTableTemp, false);

                ENG_ModeVisibleOff();
                MEM_TypeVisibleOff();
                //新增            
                _ConfigSystem = new ConfigSystem("ConfigSystem");
                _Using = bool.Parse(_ConfigSystem._UsingDevice);

                _version = $"{_ConfigSystem._EquipmentNo} {_ConfigSystem._Version}";
                this.Text = _version;
                ComponentInit();
                // laser ezm file
                if (!File.Exists(_ezmPath))
                {
                    MessageBox.Show("ezm檔載入失敗!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    LogMsgAdd(Error_Log, lb_ErrorList, "ezm檔載入失敗!", tmpErrStr);

                }

                //讀取資料            
                if (!_msPosData.Load_Data())
                {
                    MessageBox.Show("基準位置載入失敗!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    LogMsgAdd(Error_Log, lb_ErrorList, "基準位置載入失敗!", tmpErrStr);
                    _msLoadFlag = false;
                }
                else
                {
                    _msLoadFlag = true;
                }

                if (!RecipeLoad(_ConfigSystem._RecipeName) || !File.Exists(_ConfigSystem._RecipePath + _ConfigSystem._RecipeName))
                {
                    MessageBox.Show("Recipe 載入失敗!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    LogMsgAdd(Error_Log, lb_ErrorList, "Recipe 載入失敗!", tmpErrStr);
                }
                else
                {
                    this.Text = $"{_version}        Recipe[ {_ConfigSystem._RecipeName.Replace(".rcp", "")} ]";
                    L_Recipe.Text = $"Recipe[ {_ConfigSystem._RecipeName.Replace(".rcp", "")} ]";
                    LogMsgAdd(MList_Log, lb_HistoryList, $"Recipe[ {_ConfigSystem._RecipeName.Replace(".rcp", "")}]載入成功。", tmpListStr);
                    ST_TableGenerate(_recipeData.Panel.ST_Col, _recipeData.Panel.ST_Row);
                }

                if (_Using)
                {
                    //CCD
                    _pylonCamera = new PylonCamera(_ConfigSystem.ExposureTime, _ConfigSystem.BslBrightness);
                    //LIGHT
                    _CoaxialLight = new SerialPort();
                    _CoaxialLight.BaudRate = 115200;
                    _CoaxialLight.PortName = _ConfigSystem._CoaxialLight_COM;
                    _CoaxialLight.Open();
                    if (_CoaxialLight.IsOpen)
                        AlignLight(_CoaxialLight, _recipeData.SYSParam.Coaxial_Light);


                    _RingLight = new SerialPort();
                    _RingLight.BaudRate = 115200;
                    _RingLight.PortName = _ConfigSystem._RingLight_COM;
                    _RingLight.Open();
                    if (_RingLight.IsOpen)
                        AlignLight(_RingLight, _recipeData.SYSParam.Ring_Light);
                    //light box
                    _LSG075A_4_Control = new VTWP_LSG075A_4_Control(_ConfigSystem._LightCOM);
                    if (_LSG075A_4_Control.Connected)
                        SetLight();
                    if (!_LSG075A_4_Control.Connected)
                        LogMsgAdd(Error_Log, lb_ErrorList, "調光器 連線失敗!", tmpErrStr);

                    FestoPressure = new FESTO_SPAN(_ConfigSystem._IO_Link_IP, _ConfigSystem._IO_Link_Port, 1);
                    Thread.Sleep(20);
                    _IO_LinkConnect = FestoPressure.Connected;
                    if (!_IO_LinkConnect)
                        LogMsgAdd(Error_Log, lb_ErrorList, "IO Link連線失敗!", tmpErrStr);
                    else
                    {
                        tact_time.Restart();//歸零計時器
                        ushort value = (ushort)(_recipeData.SYSParam.NozzleSB_ValveNum * 10);
                        if (UpdateProportionalValve(value))
                        {
                            tact_time.Reset();//歸零計時器
                        }
                        else
                        {
                            if (tact_time.ElapsedMilliseconds > 5000)
                            {

                                _EQP_Status = enumEQP_Status.DOWN; // 設備狀態設為 DOWN(停機)
                                LogMsgAdd(Error_Log, lb_ErrorList, "比例閥更新超時", tmpErrStr);
                                Invoke(new dele_msgShow(ErrMSG_Show), "比例閥更新超時");
                                tact_time.Reset();
                            };
                        }
                    }

                    //軸控
                    _aerotechConnected = Axis_Connect();
                    if (!_aerotechConnected)
                        LogMsgAdd(Error_Log, lb_ErrorList, "Aerotech連線失敗!", tmpErrStr);

                    //_AZD_Controller = new AZD_Controller_OrientalMotor_Interface.AZD_Controller_OrientalMotor("AZD_Controller_OrientalMotor");
                    //AZD_Connect();
                    //_orientalConnected = _AZD_Controller.CheckPortOpen();
                    //if (!_orientalConnected)
                    //    LogMsgAdd(Error_Log, lb_ErrorList, "AZD Motor連線失敗!", tmpErrStr);

                    //ADVANTECH 
                    _orientalConnected = PCIE_1203_Open();
                    if (!_orientalConnected)
                        LogMsgAdd(Error_Log, lb_ErrorList, "AZD Motor連線失敗!", tmpErrStr);
                    //io
                    IO_Card_Init();
                    if (!_IO_Connected)
                        LogMsgAdd(Error_Log, lb_ErrorList, "IO卡連線失敗!", tmpErrStr);
                    // pressure set
                    ElectricValve("入球開");
                    ElectricValve("吸嘴&23層通道關");

                    //IPG laser 
                    if (!IPGLaserControl.CheckConnection())
                    {
                        IPGLaserControl.IP = _ConfigSystem._IPG_IP;
                        IPGLaserControl.Port = _ConfigSystem._IPG_Port;
                        if (!IPGLaserControl.Connection())
                        {
                            LogMsgAdd(Error_Log, lb_ErrorList, "IPG Laser連線失敗!", tmpErrStr);
                        }
                        else
                        {
                            _IPG_LaserConnected = true;
                            IPG_eventInit();
                        }
                    }

                    //mechonics pizeo 
                    LasetSetPosLoad();
                    _CU30CL = new Mechonics_CU30CL();
                    if (_CU30CL.Connected)
                        _PizeoConnect = true;

                    //測高
                    _CL_Connected = Keyence_Height_Connect();
                    if (!_CL_Connected)
                        LogMsgAdd(Error_Log, lb_ErrorList, "雷射測高連線失敗!", tmpErrStr);
                    //視覺
                    if (ConnectaxCVX(_ConfigSystem._CVX_IP, 8502, ref tt))
                    {
                        if (StartRemoteDesktopaxCVX(ref tt))
                        {
                            _CVX_Connected = Keyence_TCPConnect(_ConfigSystem._CVX_IP, _ConfigSystem._CVX_Port);
                            Thread.Sleep(200);
                            ChangeCVX_Run();
                            Thread.Sleep(200);
                            CVX_RecipeChange(_recipeData.SYSParam.CVX_RecipeNo);
                        }
                        else
                        {
                            MessageBox.Show("視覺遠端連線失敗");
                            LogMsgAdd(Error_Log, lb_ErrorList, "視覺遠端連線失敗", tmpErrStr);
                        }
                    }
                    else
                    {
                        MessageBox.Show("視覺連接失敗");
                        LogMsgAdd(Error_Log, lb_ErrorList, "視覺連接失敗", tmpErrStr);
                    }

                    //E5CC
                    E5CC.Com_Connect(_ConfigSystem._E5CC_COM, 38400, Parity.Even, 8, StopBits.One, Handshake.None, false);
                    E5CC.Thread_Start();
                    _heaterConnnected = E5CC.Connected;
                    if (!_heaterConnnected)
                        LogMsgAdd(Error_Log, lb_ErrorList, "溫控器 連線失敗!", tmpErrStr);

                    //pm init
                    PowerMeter_Init();
                    if (!_PowerMeterConnect)
                        LogMsgAdd(Error_Log, lb_ErrorList, "PowermMeter 連線失敗!", tmpErrStr);

                    //th sensor init
                    //TH_Init();
                    //if(!_TH_SensorConnect)
                    //    LogMsgAdd(Error_Log, lb_ErrorList, "溫濕度計 連線失敗!", tmpErrStr);
                    P_F_EthernetIP = new EthernetIP_Connect("192.168.1.250");





                }//using device

                //設定UI資訊
                Setting_UI();
                IO_UISetting();

                // mem type 是否顯示

                _t_M_Electric = new Thread(_M_Electric);
                _t_M_Electric.Start();

                //設定軸控資訊
                AxisSpeed_Setup();

                Type dgvType1 = this.dGV_test_STFile.GetType();
                PropertyInfo pi1 = dgvType1.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                pi1.SetValue(this.dGV_test_STFile, true, null);

                Type dgvType2 = this.dGV_test_STPos.GetType();
                PropertyInfo pi2 = dgvType2.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                pi2.SetValue(this.dGV_test_STPos, true, null);

                #region MarkingMate雷射控制卡初始化

                if (Initial())
                {
                    StatusInitial();
                    EditInitial();
                    CtrlObjInitial();

                    ////載入ezm專案檔
                    //axMMMark.LoadFile(_ezmPath);
                    ////取得圖層名稱
                    //axMMEdit.GetLayerName(1, ref StrName);
                    ////取得圖層底下子物件名稱-點1
                    //axMMEdit.GetChildObjectName(StrName, 1, ref StrSpotName1);
                    ////取得圖層底下子物件名稱-點2
                    //axMMEdit.GetChildObjectName(StrName, 3, ref StrSpotName2);
                    ////取得圖層底下子物件名稱-延遲時間
                    //axMMEdit.GetChildObjectName(StrName, 2, ref StrDelayName);

                    if (axMMMark.LoadFile(_ezmPath) == 0 && axMMEdit.GetLayerName(1, ref StrName) == 0 && axMMEdit.GetChildObjectName(StrName, 1, ref StrSpotName1) == 0)
                        _AxMarkConnected = true;
                }
                else
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, ReturnErr, tmpErrStr);
                }
                #endregion
                FlowStep_INIT();

                t_StatusUpData.Enabled = true;
                t_ConnectStatus.Enabled = true;
                t_SlowStatusUpdate.Enabled = true;
                Start_Updata_Thread();
                Add_Event();//先連結鈕功能                  
                MainFlowStart();
                DGV_SB_Path.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;

                //載台設定
                TableTemp_Set(_ConfigSystem.TableTemp);
                _TableTempSet = _ConfigSystem.TableTemp;
                chk_HeaterPower.Checked = _ConfigSystem.TableHeater;
                int count = 12;
                if (!_ConfigSystem.TableVacuum)
                {
                    IOCard_OutputRelay_OFF(0, count);
                    IOCard_DOValue[0, count] = false;
                    _DO1button[count].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.OFF];
                    btn_StageVacuum.BackColor = SystemColors.Control;
                }
                else
                {
                    IOCard_OutputRelay_ON(0, count);
                    IOCard_DOValue[0, count] = true;
                    _DO1button[count].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.ON];
                    btn_StageVacuum.BackColor = Color.Lime;
                }

            }
            catch (Exception ex)
            {
                Error_Log.Add($"[Form Load Error]:　{ex.ToString()}");
            }

        }

        private void SetLight()
        {
            byte v1, v2, v3, v4;
            v1 = (byte)(_recipeData.SYSParam.NozzleXY_Light);
            v2 = (byte)(_recipeData.SYSParam.NozzleZ_Light);
            v3 = 0;
            v4 = 0;
            if (_LSG075A_4_Control != null)
            {
                _LSG075A_4_Control.SetBright(v1, v2, v3, v4);
                Thread.Sleep(500);
                _LSG075A_4_Control.GetBright();

            }

        }
        /// <summary>
        /// advantech msg
        /// </summary>
        /// <param name="DetailMessage"></param>
        /// <param name="errorCode"></param>
        private void ShowMessages(string DetailMessage, uint errorCode)
        {
            StringBuilder ErrorMsg = new StringBuilder("", 100);
            //Get the error message according to error code returned from API
            Boolean res = Motion.mAcm_GetErrorMessage(errorCode, ErrorMsg, 100);
            string ErrorMessage = "";
            if (res)
                ErrorMessage = ErrorMsg.ToString();
            MessageBox.Show(DetailMessage + "\r\nError Message:" + ErrorMessage, "CMove", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private bool PCIE_1203_Open()
        {
            bool _result = false;
            uint Result;
            uint i = 0;
            uint[] slaveDevs = new uint[16];
            uint AxesPerDev = new uint();
            string strTemp;

            int Result_int = Motion.mAcm_GetAvailableDevs(CurAvailableDevs, Motion.MAX_DEVICES, ref deviceCount);
            if (Result_int != (int)ErrorCode.SUCCESS)
            {
                strTemp = "Get Device Numbers Failed With Error Code: [0x" + Convert.ToString(Result_int, 16) + "]";
                ShowMessages(strTemp, (uint)Result_int);
                return _result;
            }
            DeviceNum = CurAvailableDevs[0].DeviceNum;
            //Open a specified device to get device handle
            //you can call GetDevNum() API to get the devcie number of fixed equipment in this,as follow
            //DeviceNum = GetDevNum((uint)DevTypeID.PCI1285, 15, 0, 0);
            Result = Motion.mAcm_DevOpen(DeviceNum, ref m_DeviceHandle);
            if (Result != (uint)ErrorCode.SUCCESS)
            {
                strTemp = "Open Device Failed With Error Code: [0x" + Convert.ToString(Result, 16) + "]";
                ShowMessages(strTemp, Result);
                return _result;
            }
            //FT_DevAxesCount:Get axis number of this device.
            //if you device is fixed(for example: PCI-1245),You can not get FT_DevAxesCount property value
            //This step is not necessary
            //You can also use the old API: Motion.mAcm_GetProperty(m_DeviceHandle, (uint)PropertyID.FT_DevAxesCount, ref AxesPerDev, ref BufferLength);
            // UInt32 BufferLength;
            //BufferLength =4;  buffer size for the property
            Result = Motion.mAcm_GetU32Property(m_DeviceHandle, (uint)PropertyID.FT_DevAxesCount, ref AxesPerDev);
            if (Result != (uint)ErrorCode.SUCCESS)
            {
                strTemp = "Get Axis Number Failed With Error Code: [0x" + Convert.ToString(Result, 16) + "]";
                ShowMessages(strTemp, Result);
                return _result;
            }
            m_ulAxisCount = AxesPerDev;

            //if you device is fixed,for example: PCI-1245 m_ulAxisCount =4
            for (i = 0; i < m_ulAxisCount; i++)
            {
                //Open every Axis and get the each Axis Handle
                //And Initial property for each Axis 		
                //Open Axis 
                Result = Motion.mAcm_AxOpen(m_DeviceHandle, (UInt16)i, ref m_Axishand[i]);
                if (Result != (uint)ErrorCode.SUCCESS)
                {
                    strTemp = "Open Axis Failed With Error Code: [0x" + Convert.ToString(Result, 16) + "]";
                    ShowMessages(strTemp, Result);
                    return _result;
                }

            }

            UInt32 AxisNum;

            //Check the servoOno flag to decide if turn on or turn off the ServoOn output.            

            for (AxisNum = 0; AxisNum < m_ulAxisCount; AxisNum++)
            {
                // Set servo Driver ON,1: On
                Result = Motion.mAcm_AxSetSvOn(m_Axishand[AxisNum], 1);
                if (Result != (uint)ErrorCode.SUCCESS)
                {
                    strTemp = "Servo On Failed With Error Code: [0x" + Convert.ToString(Result, 16) + "]";
                    ShowMessages(strTemp, Result);
                    return false;
                }
            }

            _result = true;
            return _result;
        }

        private bool PCIE_1203_CLOSE()
        {
            UInt16[] usAxisState = new UInt16[32];
            uint AxisNum;
            for (AxisNum = 0; AxisNum < m_ulAxisCount; AxisNum++)
            {
                //Get the axis's current state
                Motion.mAcm_AxGetState(m_Axishand[AxisNum], ref usAxisState[AxisNum]);
                if (usAxisState[AxisNum] == (uint)AxisState.STA_AX_ERROR_STOP)
                {
                    // Reset the axis' state. If the axis is in ErrorStop state, the state will be changed to Ready after calling this function
                    Motion.mAcm_AxResetError(m_Axishand[AxisNum]);

                }
                //To command axis to decelerate to stop.
                Motion.mAcm_AxStopDec(m_Axishand[AxisNum]);
            }
            //Close Axes
            for (AxisNum = 0; AxisNum < m_ulAxisCount; AxisNum++)
            {
                Motion.mAcm_AxClose(ref m_Axishand[AxisNum]);
            }
            m_ulAxisCount = 0;
            //Close Device
            Motion.mAcm_DevClose(ref m_DeviceHandle);
            m_DeviceHandle = IntPtr.Zero;
            return true;
        }

        private void SetLight(byte v1, byte v2, byte v3, byte v4)
        {
            if (_LSG075A_4_Control != null)
            {
                _LSG075A_4_Control.SetBright(v1, v2, v3, v4);
                Thread.Sleep(500);
                _LSG075A_4_Control.GetBright();
            }
        }

        private void TH_Init()
        {
            _THSensor = new THSensor_Interface.THSensor("THSensor");
            _THSensor.Connect();
            _THSensor.Initialize();
            _TH_SensorConnect = _THSensor.Connected;
        }

        private void PowerMeter_Init()
        {
            _powermeter = new Powermeter("USB0::0x1313::0x8072::" + _ConfigSystem._PowerMeterSN + "::INSTR", 1, 0, "PM100USB");
            _powermeter.Init();
            _PowerMeterConnect = _powermeter.Connected;

        }

        private void IO_Card_Init()
        {
            try
            {
                instantDiCtrl1.SelectedDevice = new Automation.BDaq.DeviceInformation(_ConfigSystem._IO_CardID_1);
                instantDoCtrl1.SelectedDevice = new Automation.BDaq.DeviceInformation(_ConfigSystem._IO_CardID_1);
                instantDiCtrl2.SelectedDevice = new Automation.BDaq.DeviceInformation(_ConfigSystem._IO_CardID_2);
                instantDoCtrl2.SelectedDevice = new Automation.BDaq.DeviceInformation(_ConfigSystem._IO_CardID_2);

                _IO_Connected = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IO INIT Error");
            }
        }

        public void AxisSpeed_Setup()
        {
            cmb_Motion_Speed_X.Items.Add("0.1");
            cmb_Motion_Speed_X.Items.Add("1");
            cmb_Motion_Speed_X.Items.Add("10");
            cmb_Motion_Speed_X.Items.Add("20");
            cmb_Motion_Speed_X.SelectedIndex = 2;

            cmb_Motion_Speed_Y.Items.Add("0.1");
            cmb_Motion_Speed_Y.Items.Add("1");
            cmb_Motion_Speed_Y.Items.Add("10");
            cmb_Motion_Speed_Y.Items.Add("20");
            cmb_Motion_Speed_Y.SelectedIndex = 2;

            cmb_Motion_Speed_Z.Items.Add("0.1");
            cmb_Motion_Speed_Z.Items.Add("1");
            cmb_Motion_Speed_Z.Items.Add("10");
            cmb_Motion_Speed_Z.Items.Add("20");
            cmb_Motion_Speed_Z.SelectedIndex = 2;

            cmb_Motion_Speed_Q.Items.Add("0.1");
            cmb_Motion_Speed_Q.Items.Add("1");
            cmb_Motion_Speed_Q.Items.Add("15");
            cmb_Motion_Speed_Q.Items.Add("20");
            cmb_Motion_Speed_Q.SelectedIndex = 0;
        }

        /// <summary>
        /// 設定機械參數、生產參數
        /// </summary>
        public void Setting_UI()
        {
            try
            {

                #region Panel相關

                E_ST_Col.Text = _recipeData.Panel.ST_Col.ToString();
                E_ST_Row.Text = _recipeData.Panel.ST_Row.ToString();
                //ST 中心
                for (int i = 0; i < _recipeData.Panel.ST_Num; i++)
                {
                    ST_Panel[i].txt_PosX.Text = _recipeData.Panel.ST_CenterPos[i].X.ToString();
                    ST_Panel[i].txt_PosY.Text = _recipeData.Panel.ST_CenterPos[i].Y.ToString();
                }
                Num_ST_NO.Maximum = _recipeData.Panel.ST_Num;

                #endregion

                #region EXCEL ST相關
                //定位點1
                txt_STMark1X_Col.Text = _recipeData.ST.MarkX_CellCol[0].ToString();
                txt_STMark1X_Row.Text = _recipeData.ST.MarkX_CellRow[0].ToString();
                txt_STMark1Y_Col.Text = _recipeData.ST.MarkY_CellCol[0].ToString();
                txt_STMark1Y_Row.Text = _recipeData.ST.MarkY_CellRow[0].ToString();

                //定位點2
                txt_STMark2X_Col.Text = _recipeData.ST.MarkX_CellCol[1].ToString();
                txt_STMark2X_Row.Text = _recipeData.ST.MarkX_CellRow[1].ToString();
                txt_STMark2Y_Col.Text = _recipeData.ST.MarkY_CellCol[1].ToString();
                txt_STMark2Y_Row.Text = _recipeData.ST.MarkY_CellRow[1].ToString();

                //定位點3
                txt_STMark3X_Col.Text = _recipeData.ST.MarkX_CellCol[2].ToString();
                txt_STMark3X_Row.Text = _recipeData.ST.MarkX_CellRow[2].ToString();
                txt_STMark3Y_Col.Text = _recipeData.ST.MarkY_CellCol[2].ToString();
                txt_STMark3Y_Row.Text = _recipeData.ST.MarkY_CellRow[2].ToString();

                //定位點4
                txt_STMark4X_Col.Text = _recipeData.ST.MarkX_CellCol[3].ToString();
                txt_STMark4X_Row.Text = _recipeData.ST.MarkX_CellRow[3].ToString();
                txt_STMark4Y_Col.Text = _recipeData.ST.MarkY_CellCol[3].ToString();
                txt_STMark4Y_Row.Text = _recipeData.ST.MarkY_CellRow[3].ToString();

                //測高點1
                txt_STHeight1X_Col.Text = _recipeData.ST.HeightX_CellCol[0].ToString();
                txt_STHeight1X_Row.Text = _recipeData.ST.HeightX_CellRow[0].ToString();
                txt_STHeight1Y_Col.Text = _recipeData.ST.HeightY_CellCol[0].ToString();
                txt_STHeight1Y_Row.Text = _recipeData.ST.HeightY_CellRow[0].ToString();

                //測高點2
                txt_STHeight2X_Col.Text = _recipeData.ST.HeightX_CellCol[1].ToString();
                txt_STHeight2X_Row.Text = _recipeData.ST.HeightX_CellRow[1].ToString();
                txt_STHeight2Y_Col.Text = _recipeData.ST.HeightY_CellCol[1].ToString();
                txt_STHeight2Y_Row.Text = _recipeData.ST.HeightY_CellRow[1].ToString();

                //測高點3
                txt_STHeight3X_Col.Text = _recipeData.ST.HeightX_CellCol[2].ToString();
                txt_STHeight3X_Row.Text = _recipeData.ST.HeightX_CellRow[2].ToString();
                txt_STHeight3Y_Col.Text = _recipeData.ST.HeightY_CellCol[2].ToString();
                txt_STHeight3Y_Row.Text = _recipeData.ST.HeightY_CellRow[2].ToString();

                //測高點4
                txt_STHeight4X_Col.Text = _recipeData.ST.HeightX_CellCol[3].ToString();
                txt_STHeight4X_Row.Text = _recipeData.ST.HeightX_CellRow[3].ToString();
                txt_STHeight4Y_Col.Text = _recipeData.ST.HeightY_CellCol[3].ToString();
                txt_STHeight4Y_Row.Text = _recipeData.ST.HeightY_CellRow[3].ToString();

                //PAD中心點
                txt_PAD_X_StartCellCol.Text = _recipeData.ST.PAD_X_StartCellCol.ToString();
                txt_PAD_X_StartCellRow.Text = _recipeData.ST.PAD_X_StartCellRow.ToString();
                txt_PAD_Y_StartCellCol.Text = _recipeData.ST.PAD_Y_StartCellCol.ToString();
                txt_PAD_Y_StartCellRow.Text = _recipeData.ST.PAD_Y_StartCellRow.ToString();
                txt_PAD_Q_StartCellCol.Text = _recipeData.ST.PAD_Q_StartCellCol.ToString();
                txt_PAD_Q_StartCellRow.Text = _recipeData.ST.PAD_Q_StartCellRow.ToString();
                txt_PADType_StartCellCol.Text = _recipeData.ST.PAD_Type_StartCellCol.ToString();
                txt_PADType_StartCellRow.Text = _recipeData.ST.PAD_Type_StartCellRow.ToString();


                txt_PADPosNumber.Text = _recipeData.ST.PAD_CellNumber.ToString();

                for (int i = 0; i < _recipeData.Panel.ST_Num; i++)
                {
                    ST_SelectPanel[i]._numPad.Maximum = _recipeData.ST.PAD_CellNumber;
                }

                num_ST_Pad_No.Maximum = _recipeData.ST.PAD_CellNumber;
                Num_MovePOS_Pad_NO.Maximum = _recipeData.ST.PAD_CellNumber;

                //ST旋轉角度
                int type = _recipeData.ST.ST_RotateAngle / 90;
                cb_STPos_Rotate.SelectedIndex = type;
                #endregion

                #region SB相關
                //植球數量
                txt_SBNumber.Text = _recipeData.SB.SolderBall_Number.ToString();

                for (int i = 0; i < _recipeData.Panel.ST_Num; i++)
                {
                    ST_SelectPanel[i]._numBall.Maximum = _recipeData.SB.SolderBall_Number;
                }

                Num_MovePOS_Ball_NO.Maximum = _recipeData.SB.SolderBall_Number;
                num_ST_Ball_No.Maximum = _recipeData.SB.SolderBall_Number;
                //植球上升高度
                cb_SB_MountMoveZ_Flag.Checked = _recipeData.SB.SB_MountMoveZ_Flag;
                txt_SB_MountMoveZ.Text = _recipeData.SB.SB_MountMoveZ.ToString();


                //PAD長度
                txt_SBPADLength.Text = _recipeData.SB.PAD_Length.ToString();

                //錫球間距
                txt_SBPitch.Text = _recipeData.SB.SolderBall_Pitch.ToString();

                //錫球大小
                txt_SBSize.Text = _recipeData.SB.SolderBall_Diameter.ToString();

                //offset
                txt_SB_OffsetX.Text = (_recipeData.SB.SB_OffsetX * 1000).ToString();
                txt_SB_OffsetY.Text = (_recipeData.SB.SB_OffsetY * 1000).ToString();
                txt_SB_OffsetZ.Text = (_recipeData.SB.SB_OffsetZ * 1000).ToString();

                //吸嘴校驗數值
                txt_NozzleBase_X.Text = _msPosData.M_NozzleBase_X.ToString();
                txt_NozzleBase_Y.Text = _msPosData.M_NozzleBase_Y.ToString();
                txt_NozzleBase_Z.Text = _msPosData.M_NozzleBase_Z.ToString();
                txt_NozzleNow_X.Text = _recipeData.SB.NozzleNow_X.ToString();
                txt_NozzleNow_Y.Text = _recipeData.SB.NozzleNow_Y.ToString();
                txt_NozzleNow_Z.Text = _recipeData.SB.NozzleNow_Z.ToString();



                #endregion

                #region 出光相關                        
                // ============== signal
                //功率設定
                txt_Signal_ALaserPower.Text = _recipeData.Signal_SBLoad.ALaserPower.ToString();
                txt_MUI_SB_ALaserPower.Text = _recipeData.Signal_SBLoad.ALaserPower.ToString();
                txt_Signal_BLaserPower.Text = _recipeData.Signal_SBLoad.BLaserPower.ToString();
                txt_MUI_SB_BLaserPower.Text = _recipeData.Signal_SBLoad.BLaserPower.ToString();
                //二次功率切換延遲時間
                txt_Signal_LaserDelayTimes.Text = _recipeData.Signal_SBLoad.LaserDelayTimes.ToString();
                txt_MUI_SB_LaserDelayTimes.Text = _recipeData.Signal_SBLoad.LaserDelayTimes.ToString();
                //出光時間    
                txt_Signal_ALaserTimes.Text = _recipeData.Signal_SBLoad.ALaserTimes.ToString();
                txt_MUI_SB_ALaserTImes.Text = _recipeData.Signal_SBLoad.ALaserTimes.ToString();
                txt_Signal_BLaserTimes.Text = _recipeData.Signal_SBLoad.BLaserTimes.ToString();
                txt_MUI_SB_BLaserTImes.Text = _recipeData.Signal_SBLoad.BLaserTimes.ToString();
                //吹氣→出光Delay
                txt_Signal_BlowToLaser_DelayTimes.Text = _recipeData.Signal_SBLoad.BlowToLaser_DelayTimes.ToString();
                txt_MUI_SB_BlowToLaser_DelayTimes.Text = _recipeData.Signal_SBLoad.BlowToLaser_DelayTimes.ToString();
                //出光→關氣Delay
                txt_Signal_LaserToCloseAir_DelayTimes.Text = _recipeData.Signal_SBLoad.LaserToCloseAir_DelayTimes.ToString();
                txt_MUI_SB_LaserToCloseAir_DelayTimes.Text = _recipeData.Signal_SBLoad.LaserToCloseAir_DelayTimes.ToString();
                //============ power
                //功率設定
                txt_Power_ALaserPower.Text = _recipeData.Power_SBLoad.ALaserPower.ToString();
                txt_Power_BLaserPower.Text = _recipeData.Power_SBLoad.BLaserPower.ToString();
                //二次功率切換延遲時間
                txt_Power_LaserDelayTimes.Text = _recipeData.Power_SBLoad.LaserDelayTimes.ToString();
                //出光時間
                txt_Power_ALaserTimes.Text = _recipeData.Power_SBLoad.ALaserTimes.ToString();
                txt_Power_BLaserTimes.Text = _recipeData.Power_SBLoad.BLaserTimes.ToString();
                //吹氣→出光Delay
                txt_Power_BlowToLaser_DelayTimes.Text = _recipeData.Power_SBLoad.BlowToLaser_DelayTimes.ToString();
                //出光→關氣Delay
                txt_Power_LaserToCloseAir_DelayTimes.Text = _recipeData.Power_SBLoad.LaserToCloseAir_DelayTimes.ToString();
                // ============== ground
                //功率設定
                txt_Ground_ALaserPower.Text = _recipeData.Ground_SBLoad.ALaserPower.ToString();
                txt_Ground_BLaserPower.Text = _recipeData.Ground_SBLoad.BLaserPower.ToString();
                //二次功率切換延遲時間
                txt_Ground_LaserDelayTimes.Text = _recipeData.Ground_SBLoad.LaserDelayTimes.ToString();
                //出光時間
                txt_Ground_ALaserTimes.Text = _recipeData.Ground_SBLoad.ALaserTimes.ToString();
                txt_Ground_BLaserTimes.Text = _recipeData.Ground_SBLoad.BLaserTimes.ToString();
                //吹氣→出光Delay
                txt_Ground_BlowToLaser_DelayTimes.Text = _recipeData.Ground_SBLoad.BlowToLaser_DelayTimes.ToString();
                //出光→關氣Delay
                txt_Ground_LaserToCloseAir_DelayTimes.Text = _recipeData.Ground_SBLoad.LaserToCloseAir_DelayTimes.ToString();


                #endregion

                #region  出光二階相關          
                // ============== signal
                //功率設定
                txt_Signal_ALaserPower2.Text = _recipeData.Signal_SBLoad_2.ALaserPower.ToString();
                txt_Signal_BLaserPower2.Text = _recipeData.Signal_SBLoad_2.BLaserPower.ToString();
                //二次功率切換延遲時間
                txt_Signal_LaserDelayTimes2.Text = _recipeData.Signal_SBLoad_2.LaserDelayTimes.ToString();
                //出光時間
                txt_Signal_ALaserTimes2.Text = _recipeData.Signal_SBLoad_2.ALaserTimes.ToString();
                txt_Signal_BLaserTimes2.Text = _recipeData.Signal_SBLoad_2.BLaserTimes.ToString();
                //吹氣→出光Delay
                txt_Signal_BlowToLaser_DelayTimes2.Text = _recipeData.Signal_SBLoad_2.BlowToLaser_DelayTimes.ToString();
                //出光→關氣Delay
                txt_Signal_LaserToCloseAir_DelayTimes2.Text = _recipeData.Signal_SBLoad_2.LaserToCloseAir_DelayTimes.ToString();

                // ============== power
                // 功率設定
                txt_Power_ALaserPower2.Text = _recipeData.Power_SBLoad_2.ALaserPower.ToString();
                txt_Power_BLaserPower2.Text = _recipeData.Power_SBLoad_2.BLaserPower.ToString();
                //二次功率切換延遲時間
                txt_Power_LaserDelayTimes2.Text = _recipeData.Power_SBLoad_2.LaserDelayTimes.ToString();
                //出光時間
                txt_Power_ALaserTimes2.Text = _recipeData.Power_SBLoad_2.ALaserTimes.ToString();
                txt_Power_BLaserTimes2.Text = _recipeData.Power_SBLoad_2.BLaserTimes.ToString();
                //吹氣→出光Delay
                txt_Power_BlowToLaser_DelayTimes2.Text = _recipeData.Power_SBLoad_2.BlowToLaser_DelayTimes.ToString();
                //出光→關氣Delay
                txt_Power_LaserToCloseAir_DelayTimes2.Text = _recipeData.Power_SBLoad_2.LaserToCloseAir_DelayTimes.ToString();

                // ============== ground
                // 功率設定
                txt_Ground_ALaserPower2.Text = _recipeData.Ground_SBLoad_2.ALaserPower.ToString();
                txt_Ground_BLaserPower2.Text = _recipeData.Ground_SBLoad_2.BLaserPower.ToString();
                //二次功率切換延遲時間
                txt_Ground_LaserDelayTimes2.Text = _recipeData.Ground_SBLoad_2.LaserDelayTimes.ToString();
                //出光時間
                txt_Ground_ALaserTimes2.Text = _recipeData.Ground_SBLoad_2.ALaserTimes.ToString();
                txt_Ground_BLaserTimes2.Text = _recipeData.Ground_SBLoad_2.BLaserTimes.ToString();
                //吹氣→出光Delay
                txt_Ground_BlowToLaser_DelayTimes2.Text = _recipeData.Ground_SBLoad_2.BlowToLaser_DelayTimes.ToString();
                //出光→關氣Delay
                txt_Ground_LaserToCloseAir_DelayTimes2.Text = _recipeData.Ground_SBLoad_2.LaserToCloseAir_DelayTimes.ToString();

                // ============== IO
                txt_IO_ALaserPower.Text = _recipeData.IO_SBLoad.ALaserPower.ToString();
                txt_IO_ALaserTimes.Text = _recipeData.IO_SBLoad.ALaserTimes.ToString();
                // ============== NC
                txt_NC_ALaserPower.Text = _recipeData.NC_SBLoad.ALaserPower.ToString();
                txt_NC_ALaserTimes.Text = _recipeData.NC_SBLoad.ALaserTimes.ToString();

                #endregion

                #region 機械位置
                //加工基準高度
                txt_MPos_LaserZ.Text = _msPosData.M_H_LaserZ.ToString();
                //二階加工基準高度
                txt_MPos_LaserZ2.Text = _msPosData.M_H_LaserZ2.ToString();
                //視覺基準高度
                txt_MPos_VisionZ.Text = _msPosData.M_H_VisionZ.ToString();
                //測距基準高度
                txt_MPos_HeightZ.Text = _msPosData.M_H_HeightZ.ToString();

                //雷射到測距相對位置
                txt_MPos_LaserToHeightX.Text = _msPosData.M_Laser2Height_X.ToString();
                txt_MPos_LaserToHeightY.Text = _msPosData.M_Laser2Height_Y.ToString();
                //雷射到視覺相對位置
                txt_MPos_LaserToVisionX.Text = _msPosData.M_Laser2Vision_X.ToString();
                txt_MPos_LaserToVisionY.Text = _msPosData.M_Laser2Vision_Y.ToString();

                //出料位置
                txt_UnloadPos_X.Text = _msPosData.M_Unload_X.ToString();
                txt_UnloadPos_Y.Text = _msPosData.M_Unload_Y.ToString();
                txt_UnloadPos_Z.Text = _msPosData.M_Unload_Z.ToString();
                //入料位置
                txt_LoadPos_X.Text = _msPosData.M_Load_X.ToString();
                txt_LoadPos_Y.Text = _msPosData.M_Load_Y.ToString();
                txt_LoadPos_Z.Text = _msPosData.M_Load_Z.ToString();
                //吸嘴XY校驗位置
                txt_Nozzle_XY_CAL_X.Text = _msPosData.M_NozzleXY_X.ToString();
                txt_Nozzle_XY_CAL_Y.Text = _msPosData.M_NozzleXY_Y.ToString();
                txt_Nozzle_XY_CAL_Z.Text = _msPosData.M_NozzleXY_Z.ToString();
                //吸嘴Z校驗位置
                txt_NozzleZ_CAL_X.Text = _msPosData.M_NozzleZ_X.ToString();
                txt_NozzleZ_CAL_Y.Text = _msPosData.M_NozzleZ_Y.ToString();
                txt_NozzleZ_CAL_Z.Text = _msPosData.M_NozzleZ_Z.ToString();
                //PowerMeter位置
                txt_PM_X.Text = _msPosData.M_PowerMeter_X.ToString();
                txt_PM_Y.Text = _msPosData.M_PowerMeter_Y.ToString();
                txt_PM_Z.Text = _msPosData.M_PowerMeter_Z.ToString();
                //抽氣清潔位置
                txt_PumpOut_X.Text = _msPosData.M_PumpOut_X.ToString();
                txt_PumpOut_Y.Text = _msPosData.M_PumpOut_Y.ToString();
                txt_PumpOut_Z.Text = _msPosData.M_PumpOut_Z.ToString();
                //等待高度位置 Z
                txt_Wait_Z_Pos.Text = _msPosData.M_Wait_Z.ToString();
                //植球校驗位置
                txt_AlignBall_X.Text = _msPosData.M_AlignBall_X.ToString();
                txt_AlignBall_Y.Text = _msPosData.M_AlignBall_Y.ToString();
                txt_AlignBall_Z.Text = _msPosData.M_AlignBall_Z.ToString();
                //模組更換位置
                txt_ModelCH_X.Text = _msPosData.M_ModelCH_X.ToString();
                txt_ModelCH_Y.Text = _msPosData.M_ModelCH_Y.ToString();
                txt_ModelCH_Z.Text = _msPosData.M_ModelCH_Z.ToString();
                //噴嘴對位基準
                txt_MS_NozzleBase_X.Text = _msPosData.M_NozzleBase_X.ToString();
                txt_MS_NozzleBase_Y.Text = _msPosData.M_NozzleBase_Y.ToString();
                txt_MS_NozzleBase_Z.Text = _msPosData.M_NozzleBase_Z.ToString();

                // 正極限位置
                txt_P_LimitX.Text = _msPosData.M_PositiveLimit_X.ToString();
                txt_P_LimitY.Text = _msPosData.M_PositiveLimit_Y.ToString();
                txt_P_LimitZ.Text = _msPosData.M_PositiveLimit_Z.ToString();
                // 負極限位置
                txt_N_LimitX.Text = _msPosData.M_NegativeLimit_X.ToString();
                txt_N_LimitY.Text = _msPosData.M_NegativeLimit_Y.ToString();
                txt_N_LimitZ.Text = _msPosData.M_NegativeLimit_Z.ToString();

                //PM測量 功率 時間
                txt_PM_Power.Text = _msPosData.PM_Power.ToString();
                txt_PM_Time.Text = _msPosData.PM_Time.ToString();
                txt_LaserTriggerInterval.Text = _msPosData.LaserTriggerInterval.ToString();
                txt_LaserAlignPowerTarget.Text = _msPosData.LaserAlignTargetPower.ToString();

                //PAD TYPE
                txt_SENSE_PadType.Text = _msPosData.SENSE_PadType.ToString();
                txt_POWER_PadType.Text = _msPosData.POWER_PadType.ToString();
                txt_GND_PadType.Text = _msPosData.GND_PadType.ToString();
                txt_IO_PadType.Text = _msPosData.IO_PadType.ToString();
                txt_NC_PadType.Text = _msPosData.NC_PadType.ToString();
                #endregion

                #region 系統設定
                txt_NozzleBall_Pressure.Text = _recipeData.SYSParam.NozzleSB_Pressure.ToString();
                txt_NozzleNoBall_Pressure.Text = _recipeData.SYSParam.NozzleNoSB_Pressure.ToString();
                txt_NozzleBall_Valve.Text = _recipeData.SYSParam.NozzleSB_ValveNum.ToString();
                txt_RotateAngle.Text = _recipeData.SYSParam.RotateAngle.ToString();
                txt_SB_AirJudgeDelay.Text = _recipeData.SYSParam.SB_AirJudgeDelay.ToString();
                txt_NoSB_AirJudgeDelay.Text = _recipeData.SYSParam.NoSB_AirJudgeDelay.ToString();
                chk_BallAirDiff_On.Checked = _recipeData.SYSParam.SB_AirJudgDiff;
                txt_BallAirDiff.Text = _recipeData.SYSParam.NozzleSB_Diff_Value.ToString();

                txt_LoadBallRetry.Text = _recipeData.SYSParam.LoadBallRetry.ToString();
                txt_EmissionRetry.Text = _recipeData.SYSParam.EmissionRetry.ToString();
                txt_CleanRetry.Text = _recipeData.SYSParam.ClearRetry.ToString();
                txt_ClearSB_Count.Text = _recipeData.SYSParam.ClearSB_Count.ToString();
                txt_4P_HeightLimit.Text = _recipeData.SYSParam.FourP_HeightLimit.ToString();
                txt_NozzleXY_Light.Text = _recipeData.SYSParam.NozzleXY_Light.ToString();
                txt_NozzleZ_Light.Text = _recipeData.SYSParam.NozzleZ_Light.ToString();
                txt_CoaxialLight.Text = _recipeData.SYSParam.Coaxial_Light.ToString();
                txt_RingLight.Text = _recipeData.SYSParam.Ring_Light.ToString();
                txt_CVX_Recipe.Text = _recipeData.SYSParam.CVX_RecipeNo.ToString();
                //雷射吸嘴清潔參數
                txt_CleanLaserPower.Text = _recipeData.SYSParam.CleanLaserPower.ToString();
                txt_CleanLaserTime.Text = _recipeData.SYSParam.CleanLaserTime.ToString();
                txt_CleanAirTime.Text = _recipeData.SYSParam.CleanAirTime.ToString();
                txt_CleanAirValve.Text = _recipeData.SYSParam.CleanAirValve.ToString();
                txt_CleanVacuumTime.Text = _recipeData.SYSParam.CleanVacuumTime.ToString();

                E_BallOut_Angle.Text = _recipeData.SYSParam.RotateAngle.ToString();
                //分離盤參數
                txt_DiskRunSpeed.Text = _recipeData.SYSParam.DiskRunSpeed.ToString();
                txt_DiskRunACC.Text = _recipeData.SYSParam.DiskRunACC.ToString();
                txt_DiskRunDEC.Text = _recipeData.SYSParam.DiskRunDEC.ToString();

                #endregion
                //mem type 使用
                cb_MemUse.Checked = _ConfigSystem.MemUse;
                //生產參數
                txt_SB_AirJudgeNG_Delay.Text = _msPosData.SB_AirJudgeNG_Delay.ToString();
                txt_NoSB_AirJudge_NG_Delay.Text = _msPosData.NoSB_AirJudgeNG_Delay.ToString();
                cb_NozzleAir_Log.Checked = _msPosData.NozzleAir_Log;
                txt_VacuumThreshold.Text = _msPosData.VacuumThreshold.ToString();
            }
            catch (Exception ex)
            {
                Error_Log.Add($"Setting_UI Error: {ex.ToString()}");
            }
        }

        /// <summary>
        /// 設定IO UI物件
        /// </summary>
        private void IO_UISetting()
        {
            _DI1picture = new PictureBox[16];
            _DO1button = new Button[16];
            _DO1picture = new PictureBox[16];

            _DI1picture[0] = ptb_DI10;
            _DI1picture[1] = ptb_DI11;
            _DI1picture[2] = ptb_DI12;
            _DI1picture[3] = ptb_DI13;
            _DI1picture[4] = ptb_DI14;
            _DI1picture[5] = ptb_DI15;
            _DI1picture[6] = ptb_DI16;
            _DI1picture[7] = ptb_DI17;
            _DI1picture[8] = ptb_DI18;
            _DI1picture[9] = ptb_DI19;
            _DI1picture[10] = ptb_DI110;
            _DI1picture[11] = ptb_DI111;
            _DI1picture[12] = ptb_DI112;
            _DI1picture[13] = ptb_DI113;
            _DI1picture[14] = ptb_DI114;
            _DI1picture[15] = ptb_DI115;

            _DO1button[0] = btn_DO10;
            _DO1button[1] = btn_DO11;
            _DO1button[2] = btn_DO12;
            _DO1button[3] = btn_DO13;
            _DO1button[4] = btn_DO14;
            _DO1button[5] = btn_DO15;
            _DO1button[6] = btn_DO16;
            _DO1button[7] = btn_DO17;
            _DO1button[8] = btn_DO18;
            _DO1button[9] = btn_DO19;
            _DO1button[10] = btn_DO110;
            _DO1button[11] = btn_DO111;
            _DO1button[12] = btn_DO112;
            _DO1button[13] = btn_DO113;
            _DO1button[14] = btn_DO114;
            _DO1button[15] = btn_DO115;

            _DO1picture[0] = ptb_DO10;
            _DO1picture[1] = ptb_DO11;
            _DO1picture[2] = ptb_DO12;
            _DO1picture[3] = ptb_DO13;
            _DO1picture[4] = ptb_DO14;
            _DO1picture[5] = ptb_DO15;
            _DO1picture[6] = ptb_DO16;
            _DO1picture[7] = ptb_DO17;
            _DO1picture[8] = ptb_DO18;
            _DO1picture[9] = ptb_DO19;
            _DO1picture[10] = ptb_DO110;
            _DO1picture[11] = ptb_DO111;
            _DO1picture[12] = ptb_DO112;
            _DO1picture[13] = ptb_DO113;
            _DO1picture[14] = ptb_DO114;
            _DO1picture[15] = ptb_DO115;


            _DI2picture = new PictureBox[16];
            _DO2button = new Button[16];
            _DO2picture = new PictureBox[16];

            _DI2picture[0] = ptb_DI20;
            _DI2picture[1] = ptb_DI21;
            _DI2picture[2] = ptb_DI22;
            _DI2picture[3] = ptb_DI23;
            _DI2picture[4] = ptb_DI24;
            _DI2picture[5] = ptb_DI25;
            _DI2picture[6] = ptb_DI26;
            _DI2picture[7] = ptb_DI27;
            _DI2picture[8] = ptb_DI28;
            _DI2picture[9] = ptb_DI29;
            _DI2picture[10] = ptb_DI210;
            _DI2picture[11] = ptb_DI211;
            _DI2picture[12] = ptb_DI212;
            _DI2picture[13] = ptb_DI213;
            _DI2picture[14] = ptb_DI214;
            _DI2picture[15] = ptb_DI215;

            _DO2button[0] = btn_DO20;
            _DO2button[1] = btn_DO21;
            _DO2button[2] = btn_DO22;
            _DO2button[3] = btn_DO23;
            _DO2button[4] = btn_DO24;
            _DO2button[5] = btn_DO25;
            _DO2button[6] = btn_DO26;
            _DO2button[7] = btn_DO27;
            _DO2button[8] = btn_DO28;
            _DO2button[9] = btn_DO29;
            _DO2button[10] = btn_DO210;
            _DO2button[11] = btn_DO211;
            _DO2button[12] = btn_DO212;
            _DO2button[13] = btn_DO213;
            _DO2button[14] = btn_DO214;
            _DO2button[15] = btn_DO215;

            _DO2picture[0] = ptb_DO20;
            _DO2picture[1] = ptb_DO21;
            _DO2picture[2] = ptb_DO22;
            _DO2picture[3] = ptb_DO23;
            _DO2picture[4] = ptb_DO24;
            _DO2picture[5] = ptb_DO25;
            _DO2picture[6] = ptb_DO26;
            _DO2picture[7] = ptb_DO27;
            _DO2picture[8] = ptb_DO28;
            _DO2picture[9] = ptb_DO29;
            _DO2picture[10] = ptb_DO210;
            _DO2picture[11] = ptb_DO211;
            _DO2picture[12] = ptb_DO212;
            _DO2picture[13] = ptb_DO213;
            _DO2picture[14] = ptb_DO214;
            _DO2picture[15] = ptb_DO215;
        }

        private void btn_Test_KeyenceHeight_Click(object sender, EventArgs e)
        {
            _M_Keyence_Height_flag = false;
            _Keyence_Height.Send(Keyence_Height_Send_Data);
        }

        #region 週邊連線
        private bool Keyence_Height_Connect()
        {
            try
            {
                _Keyence_Height.IP = _ConfigSystem._CL_IP;
                _Keyence_Height.Port = _ConfigSystem._CL_Port;

                _Keyence_Height.Connect();

                this.Invoke(new dTimer(T_Start));

                //_M_Keyence_Height_flag = true;
                return true;
            }
            catch
            {
                return false;
            }
        }
        private void T_Start()
        {
            _t_Keyence_Height_Rev = new Thread(Keyence_Height_Rev);
            _t_Keyence_Height_Rev.Start();
            _t_Keyence_Height_Send = new Thread(Keyence_Height_Send);
            _t_Keyence_Height_Send.Start();
            //timer1.Start();
        }
        private void Keyence_Height_Rev()
        {
            byte[] data = new byte[200];
            int data_len;

            while (true)
            {
                string str;
                _Keyence_Height.Recv(out data, out data_len);
                str = System.Text.Encoding.ASCII.GetString(data, 0, data_len);

                if (str != null)
                {
                    if (str.Length > 4)
                    {
                        KeyenceHeight_Value = str.Substring(3);
                        KeyenceHeight_Value = KeyenceHeight_Value.TrimEnd('\r');

                    }
                }

                Thread.Sleep(10);
            }
        }
        private void Keyence_Height_Send()
        {
            //if (sr.ReadLine() != null)
            while (true)
            {
                if (_M_Keyence_Height_flag)
                    _Keyence_Height.Send(Keyence_Height_Send_Data);

                Thread.Sleep(50);
            }
        }

        #region Keyence CVX
        /// <summary> Return Value  
		/// <para> 0 -> Processing succeeded </para> 
		/// <para> 1001 -> Processing failed </para> 
		/// <para> 1002 -> Illegal argument </para>
		/// <para> 1003 -> Illegal operation </para>
		/// </summary> 
		int ConnectStatus = -1;
        /// <summary> ConnectStatus Error Descriptions 
        /// </summary> 
        readonly string[] ConStatusBox = new string[] { "Success",
                                                        "Communication functions are not initialized.Controller does not exist at the destination.Failed to connect to the specified port.Communication failure due to a fault in thecommunication path.",
                                                        "Incorrect format used for Address and/or Port properties.",
                                                        "A connection to the controller already exists." };
        /// <summary> Return Value  
        /// <para> 0 -> Processing succeeded </para> 
        /// <para> 1001 -> Processing failed </para> 
        /// <para> 1002 -> Illegal argument </para>
        /// <para> 1003 -> Illegal operation </para>
        /// <para> 1100 -> Communication exceptions </para>
        /// </summary> 
        int StartRemoteStatus = -1;
        /// <summary> StartRemoteStatusBox Error Descriptions 
        /// </summary> 
        readonly string[] StartRemoteStatusBox = new string[] { "Success",
                                                                "Communication functions are not initialized.The controller is not connected.",
                                                                "Argument value is out of range.",
                                                                "The Remote Desktop is already running.",
                                                                "Communication failure due to a fault in the communication path." };
        /// <summary> Return Value  
        /// <para> 0 -> Processing succeeded </para> 
        /// <para> 1001 -> Processing failed </para> 
        /// <para> 1003 -> Illegal operation </para>
        /// <para> 1100 -> Communication exceptions </para>
        /// </summary> 
        int StopRemoteStatus = -1;
        /// <summary> StopRemoteStatusBox Error Descriptions 
        /// </summary> 
        readonly string[] StopRemoteStatusBox = new string[] { "Success",
                                                               "Communication functions are not initialized.The controller is not connected.",
                                                               "The Remote Desktop is not running. A screen update triggered by the UpdateRemoteDesktop method is still in progress (waiting for the OnRemoteDesktopUpdated event).",
                                                               "Communication failure due to a fault in the communication path." };
        /// <summary> CaptureRemoteDesktop Return Value  
        /// <para> 0 -> Processing succeeded </para> 
        /// <para> 1001 -> Processing failed </para> 
        /// <para> 1002 -> Illegal argument </para>
        /// <para> 1004 -> Out of space </para>
        /// </summary> 
        int CaptureRemoteStatus = -1;
        /// <summary> CaptureRemoteStatusBox Error Descriptions 
        /// </summary> 
        readonly string[] CaptureRemoteStatusBox = new string[] { "Success",
                                                                  "File save failed.",
                                                                  "Illegal number of characters in path (NULL or exceeded maximum character count). Illegal file naming format.",
                                                                  "Not enough disk space available." };
        /// <summary> CaptureRemoteDesktop Return Value  
        /// <para> 0 -> Processing succeeded </para> 
        /// <para> 1001 -> Processing failed </para> 
        /// <para> 1002 -> Illegal argument </para>
        /// <para> 1003 -> Illegal operation </para>
        /// <para> 1100 -> Communication exceptions </para>
        /// </summary> 
        int StartResultLogStatus = -1;
        /// <summary> StartResultLogStatus Error Descriptions 
        /// </summary> 
        readonly string[] StartResultLogStatusBox = new string[] { "Success",
                                                                   "Communication functions are not initialized. The controller is not connected. The controller is already logging results to another PC Program.",
                                                                   "Illegal number of characters in folder name (NULL or exceeded maximum character count). Illegal folder naming format.",
                                                                   "Results logging is already in progress.",
                                                                   "Communication failure due to a fault in the communication path."};

        /// <summary> CaptureRemoteDesktop Return Value  
        /// <para> 0 -> Processing succeeded </para> 
        /// <para> 1001 -> Processing failed </para> 
        /// <para> 1003 -> Illegal operation </para>
        /// <para> 1100 -> Communication exceptions </para>
        /// </summary> 
        int StopResultLogStatus = -1;
        readonly string[] StopResultLogStatusBox = new string[] { "Success",
                                                                  "Communication functions are not initialized. The controller is not connected.",
                                                                  "Results logging has not been started.",
                                                                  "Communication failure due to a fault in the communication path."};
        /// <summary>  
        /// <para> Connect to CVX </para> 
        /// </summary> 
        public bool ConnectaxCVX(string IP, int Port, ref string result)
        {
            try
            {

                if (axCVX1.Connected)
                {
                    _Keyence_CVX_flag = true;
                    result = "Processing exist";
                    return true;
                }
                else
                {

                    axCVX1.Initialize();
                    axCVX1.Address = IP;
                    axCVX1.Port = Port;

                    ConnectStatus = axCVX1.Connect();
                    switch (ConnectStatus)
                    {
                        case 0:
                            result = ConStatusBox[0];
                            _Keyence_CVX_flag = true;
                            return true;
                        case 1001:
                            result = ConStatusBox[1];
                            _Keyence_CVX_flag = false;
                            return false;
                        case 1002:
                            result = ConStatusBox[2];
                            _Keyence_CVX_flag = false;
                            return false;
                        case 1003:
                            result = ConStatusBox[3];
                            _Keyence_CVX_flag = false;
                            return false;
                        default:
                            return false;
                    }
                }
            }
            catch (Exception ex)
            {
                result += ex.ToString();
                return false;
            }
        }
        /// <summary>  
        /// <para> DisConnect CVX </para> 
        /// </summary> 
        public void DisConnectaxCVX()
        {
            try
            {
                axCVX1.Disconnect();
                ClearRemoteDesktop();
            }
            catch (Exception ex)
            {

            }
        }
        /// <summary>  
        /// <para> Start RemoteDesktop in Auto Update Mode </para> 
        /// </summary> 
        public bool StartRemoteDesktopaxCVX(ref string result)
        {
            try
            {
                if (!axCVX1.Connected)
                {
                    result = "Device didn't Connected";
                    return false;
                }
                else if (axCVX1.RemoteDesktopStarted)
                {
                    result = "Device is running";
                    return true;
                }
                else
                {
                    StartRemoteStatus = axCVX1.StartRemoteDesktop(0);
                    switch (StartRemoteStatus)
                    {
                        case 0:
                            result = StartRemoteStatusBox[0];
                            return true;
                        case 1001:
                            result = StartRemoteStatusBox[1];
                            return false;
                        case 1002:
                            result = StartRemoteStatusBox[2];
                            return false;
                        case 1003:
                            result = StartRemoteStatusBox[3];
                            return false;
                        case 1100:
                            result = StartRemoteStatusBox[4];
                            return false;
                        default:
                            return false;
                    }
                }
            }
            catch (Exception ex)
            {
                result += ex.ToString();
                return false;
            }
        }
        /// <summary>  
        /// <para> Stop RemoteDesktop in Auto Update Mode </para> 
        /// </summary> 
        public bool StopRemoteDesktopaxCVX(ref string result)
        {
            try
            {
                if (axCVX1.RemoteDesktopStarted)
                {
                    StopRemoteStatus = axCVX1.StopRemoteDesktop();
                    axCVX1.Initialize();
                    switch (StopRemoteStatus)
                    {
                        case 0:
                            result = StopRemoteStatusBox[0];
                            ClearRemoteDesktop();
                            return true;
                        case 1001:
                            result = StopRemoteStatusBox[1];
                            return false;
                        case 1003:
                            result = StopRemoteStatusBox[2];
                            return false;
                        case 1100:
                            result = StopRemoteStatusBox[3];
                            return false;
                        default:
                            return false;
                    }
                }
                else
                {
                    result = "Process closed";
                    return true;
                }
            }
            catch (Exception ex)
            {
                result += ex.ToString();
                return false;
            }
        }
        /// <summary>  
        /// <para> ClearRemoteDesktop </para> 
        /// </summary> 
        public void ClearRemoteDesktopaxCVX()
        {
            axCVX1.ClearRemoteDesktop();
        }

        /// <summary>  
        /// <para> StartRemoveMouse </para> 
        /// </summary> 
        public bool StartRemoveMouseaxCVX(ref string result)
        {
            try
            {
                if (!axCVX1.Connected)
                {
                    result = "Device didn't Connected";
                    return false;
                }
                else if (!axCVX1.RemoteDesktopStarted)
                {
                    result = "Device  didn't Remoted";
                    return false;
                }
                else
                {
                    axCVX1.EnableRemoteMouseOperation = true;
                    result = "RemoveMouse On";
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>  
        /// <para> StopRemoveMouse </para> 
        /// </summary> 
        public bool StopRemoveMouseaxCVX(ref string result)
        {
            try
            {
                if (axCVX1.EnableRemoteMouseOperation)
                {
                    axCVX1.EnableRemoteMouseOperation = false;
                    result = "RemoveMouse Off";
                    return true;
                }
                else
                {
                    result = "RemoveMouse Off";
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        /// <summary>  
        /// <para> ClearRemoteDesktop </para> 
        /// </summary> 
        public void ClearRemoteDesktop()
        {
            axCVX1.ClearRemoteDesktop();
        }


        private bool Keyence_TCPConnect(string IP, int Port)
        {
            _Keyence_CVX = new TCP_IP_Client.TCPServerAP();
            _Keyence_CVX.IP = IP;
            _Keyence_CVX.Port = Port;
            _Keyence_CVX.Start();
            return _Keyence_CVX.Connected;
        }
        #endregion
        #endregion

        #region IO
        struct stIO_Input
        {
            public bool EMS_ON;
            public bool EQP_Start;
            public bool CDA_Alarm_1;
            public bool CDA_Alarm_2;
            public bool VACUUM_Alarm;
            public bool DoorOpen;
            public bool DoorLock;
            public bool DUST_ON;
            /// <summary>
            /// 分離盤到位sensor
            /// </summary>
            public bool DiskSensor;
            /// <summary>
            /// 高溫警報
            /// </summary>
            public bool HeaterHight;
            /// <summary>
            /// 低溫警報
            /// </summary>
            public bool HeaterLow;
            /// <summary>
            /// 超溫警報
            /// </summary>
            public bool OverHeater;

        }

        /// <summary>
        /// io-input 資訊
        /// </summary>
        stIO_Input IO_InputData = new stIO_Input();



        private void IOCard_Read()
        {
            //while (true)
            {
                instantDiCtrl1.Read(0, out IOCard_Input1[0]);
                IOCard_Input1_Value[0] = Convert.ToInt32(IOCard_Input1[0]);

                instantDiCtrl1.Read(1, out IOCard_Input1[1]);
                IOCard_Input1_Value[1] = Convert.ToInt32(IOCard_Input1[1]);

                instantDiCtrl2.Read(0, out IOCard_Input2[0]);
                IOCard_Input2_Value[0] = Convert.ToInt32(IOCard_Input2[0]);

                instantDiCtrl2.Read(1, out IOCard_Input2[1]);
                IOCard_Input2_Value[1] = Convert.ToInt32(IOCard_Input2[1]);


                if ((IOCard_Input1[0] & 0x01) > 0)
                    IO_InputData.EMS_ON = true;
                else
                    IO_InputData.EMS_ON = false;

                if ((IOCard_Input1[0] & 0x02) > 0)
                    IO_InputData.EQP_Start = true;
                else
                    IO_InputData.EQP_Start = false;

                if ((IOCard_Input1[0] & 0x04) > 0)
                    IO_InputData.CDA_Alarm_1 = true;
                else
                    IO_InputData.CDA_Alarm_1 = false;

                if ((IOCard_Input1[0] & 0x08) > 0)
                    IO_InputData.CDA_Alarm_2 = true;
                else
                    IO_InputData.CDA_Alarm_2 = false;

                if ((IOCard_Input1[0] & 0x10) > 0)
                    IO_InputData.VACUUM_Alarm = true;
                else
                    IO_InputData.VACUUM_Alarm = false;

                if ((IOCard_Input1[0] & 0x20) > 0)
                    IO_InputData.DoorLock = true;
                else
                    IO_InputData.DoorLock = false;

                if ((IOCard_Input1[0] & 0x40) > 0)
                    IO_InputData.DoorOpen = true;
                else
                    IO_InputData.DoorOpen = false;

                if ((IOCard_Input1[0] & 0x80) > 0)
                    IO_InputData.DUST_ON = true;
                else
                    IO_InputData.DUST_ON = false;

                //bit 12
                if ((IOCard_Input1[1] & 0x10) > 0)
                    IO_InputData.HeaterHight = true;
                else
                    IO_InputData.HeaterHight = false;

                if ((IOCard_Input1[1] & 0x20) > 0)
                    IO_InputData.HeaterLow = true;
                else
                    IO_InputData.HeaterLow = false;

                if ((IOCard_Input1[1] & 0x40) > 0)
                    IO_InputData.OverHeater = true;
                else
                    IO_InputData.OverHeater = false;

                if ((IOCard_Input1[1] & 0x80) > 0)
                    IO_InputData.DiskSensor = true;
                else
                    IO_InputData.DiskSensor = false;

            }
        }

        private void IOCard_Output_UI()
        {
            //Card1 2
            for (int i = 0; i < 16; i++)
            {
                if (IOCard_DOValue[0, i])
                {
                    _DO1picture[i].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusON];
                }
                else
                {
                    _DO1picture[i].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusOFF];
                }

                if (IOCard_DOValue[1, i])
                {
                    _DO2picture[i].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusON];
                }
                else
                {
                    _DO2picture[i].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusOFF];
                }
            }
        }

        private void IOCard_Input_UI()
        {
            //Card1 2
            for (int i = 0; i < 8; i++)
            {
                if ((IOCard_Input1_Value[0] & IO_ON_Vaule[i]) >= 1)
                {
                    _DI1picture[i].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusON];
                }
                else
                {
                    _DI1picture[i].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusOFF];
                }

                if ((IOCard_Input1_Value[1] & IO_ON_Vaule[i]) >= 1)
                {
                    _DI1picture[i + 8].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusON];
                }
                else
                {
                    _DI1picture[i + 8].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusOFF];
                }



                if ((IOCard_Input2_Value[0] & IO_ON_Vaule[i]) >= 1)
                {
                    _DI2picture[i].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusON];
                }
                else
                {
                    _DI2picture[i].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusOFF];
                }

                if ((IOCard_Input2_Value[1] & IO_ON_Vaule[i]) >= 1)
                {
                    _DI2picture[i + 8].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusON];
                }
                else
                {
                    _DI2picture[i + 8].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusOFF];
                }
            }


            //Card1 2
            for (int i = 0; i < 8; i++)
            {
                if ((IOCard_Input2_Value[0] & IO_ON_Vaule[i]) >= 1)
                {
                    _DI2picture[i].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusON];
                }
                else
                {
                    _DI2picture[i].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusOFF];
                }

                if ((IOCard_Input2_Value[1] & IO_ON_Vaule[i]) >= 1)
                {
                    _DI2picture[i + 8].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusON];
                }
                else
                {
                    _DI2picture[i + 8].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusOFF];
                }
            }
        }

        private void IOCard_OutputRelay_ON(int cardnumber, int relaynumber)
        {

            switch (cardnumber)
            {
                case 0:
                    if (relaynumber <= 7)
                    {
                        IOCard_Output[cardnumber, 0] = (byte)(IOCard_Output[cardnumber, 0] | IO_ON_Vaule[relaynumber]);

                        instantDoCtrl1.Write(0, (byte)IOCard_Output[cardnumber, 0]);
                    }
                    else
                    {
                        IOCard_Output[cardnumber, 1] = (byte)(IOCard_Output[cardnumber, 1] | IO_ON_Vaule[relaynumber - 8]);

                        instantDoCtrl1.Write(1, (byte)IOCard_Output[cardnumber, 1]);
                    }
                    break;
                case 1:
                    if (relaynumber <= 7)
                    {
                        IOCard_Output[cardnumber, 0] = (byte)(IOCard_Output[cardnumber, 0] | IO_ON_Vaule[relaynumber]);

                        instantDoCtrl2.Write(0, (byte)IOCard_Output[cardnumber, 0]);
                    }
                    else
                    {
                        IOCard_Output[cardnumber, 1] = (byte)(IOCard_Output[cardnumber, 1] | IO_ON_Vaule[relaynumber - 8]);

                        instantDoCtrl2.Write(1, (byte)IOCard_Output[cardnumber, 1]);
                    }
                    break;

            }

            IOCard_DOValue[cardnumber, relaynumber] = true;
        }

        private void IOCard_OutputRelay_OFF(int cardnumber, int relaynumber)
        {
            switch (cardnumber)
            {
                case 0:
                    if (relaynumber <= 7)
                    {
                        IOCard_Output[cardnumber, 0] = (byte)(IOCard_Output[cardnumber, 0] & IO_OFF_Vaule[relaynumber]);

                        instantDoCtrl1.Write(0, (byte)IOCard_Output[cardnumber, 0]);
                    }
                    else
                    {
                        IOCard_Output[cardnumber, 1] = (byte)(IOCard_Output[cardnumber, 1] & IO_OFF_Vaule[relaynumber - 8]);

                        instantDoCtrl1.Write(1, (byte)IOCard_Output[cardnumber, 1]);
                    }
                    break;
                case 1:
                    if (relaynumber <= 7)
                    {
                        IOCard_Output[cardnumber, 0] = (byte)(IOCard_Output[cardnumber, 0] & IO_OFF_Vaule[relaynumber]);

                        instantDoCtrl2.Write(0, (byte)IOCard_Output[cardnumber, 0]);
                    }
                    else
                    {
                        IOCard_Output[cardnumber, 1] = (byte)(IOCard_Output[cardnumber, 1] & IO_OFF_Vaule[relaynumber - 8]);

                        instantDoCtrl2.Write(1, (byte)IOCard_Output[cardnumber, 1]);
                    }
                    break;

            }


            IOCard_DOValue[cardnumber, relaynumber] = false;
        }

        private void btn_DO10_Click(object sender, EventArgs e)
        {
            int count = Convert.ToInt32(((Button)sender).Tag.ToString());

            if (IOCard_DOValue[0, count])
            {
                IOCard_OutputRelay_OFF(0, count);
                IOCard_DOValue[0, count] = false;
                _DO1button[count].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.OFF];
            }
            else
            {
                IOCard_OutputRelay_ON(0, count);
                IOCard_DOValue[0, count] = true;
                _DO1button[count].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.ON];
            }
        }


        private void btn_DO20_Click(object sender, EventArgs e)
        {
            int count = Convert.ToInt32(((Button)sender).Tag.ToString());

            if (IOCard_DOValue[1, count])
            {
                IOCard_OutputRelay_OFF(1, count);
                IOCard_DOValue[1, count] = false;
                _DO2button[count].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.OFF];
            }
            else
            {
                IOCard_OutputRelay_ON(1, count);
                IOCard_DOValue[1, count] = true;
                _DO2button[count].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.ON];
            }
        }
        #endregion

        #region Log執行緒
        public void _Log()
        {
            while (true)
            {
                try
                {
                    if (MList_Log != null)
                        MList_Log.Write();

                    if (Error_Log != null)
                        Error_Log.Write();
                }
                catch (Exception ex)
                {

                }

                Thread.Sleep(10);
            }
        }

        private delegate void dEnableTabPage(bool enable);
        private void EnableTabPages(bool enable)
        {
            if (tabPage_ManualMove.InvokeRequired)
            {
                var func = new dEnableTabPage(EnableTabPages);
                this.Invoke(func, enable);
            }
            else
            {
                tabPage_ManualMove.Enabled = enable;

            }
        }

        #endregion

        #region 手動單循環
        private void btn_M_SBLaser_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行雷射出光?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (IPG_WorkStaus())
                {
                    _t_M_SB_Laser_flag = 1;
                    _t_M_SB_Laser = new Thread(_M_SB_Laser);
                    _t_M_SB_Laser.Start();
                }
                else
                {
                    MessageBox.Show($"雷射狀態異常!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
        }

        private void _M_SB_Laser()
        {
            while (true)
            {
                M_SB_Laser();
            }
        }

        private void M_SB_Laser()
        {
            try
            {
                switch (_t_M_SB_Laser_flag)
                {
                    case 0:
                        Thread.Sleep(200);
                        break;
                    case 1:
                        //確認各軸訊號
                        if (Aerotech_En && AZD_En)// && Aerotech_Home && DeviceFlag)
                        {

                            M_UI_Running = true;
                            //關氣
                            //ElectricValve("吸嘴&23層通道關");
                            //Enable頁面避免人員再次誤觸循環
                            EnableTabPages(false);

                            if (Set_S_LaserData())
                                _t_M_SB_Laser_flag = 2;
                            else
                            {
                                Invoke(new dele_msgShow(ErrMSG_Show), "雷射參數套用失敗!");
                                _t_M_SB_Laser_flag = 7;
                            }
                        }
                        else
                        {
                            _t_M_SB_Laser_flag = 0;
                        }
                        break;

                    case 2:
                        //檔案ezm專案檔讀取,成功回傳0
                        if (axMMMark.LoadFile(_ezmPath) == 0)
                            _t_M_SB_Laser_flag = 21;
                        break;

                    case 21:
                        //提前吹氣
                        ElectricValve("吸嘴開");
                        Thread.Sleep(Convert.ToInt32(txt_Signal_BlowToLaser_DelayTimes.Text));
                        _t_M_SB_Laser_flag = 3;
                        break;

                    case 3:
                        //吹氣
                        this.Invoke((MethodInvoker)delegate () { SB_ALaserTimes = Convert.ToInt32(txt_Signal_LaserToCloseAir_DelayTimes.Text); });
                        _M_Laser_Air = true;

                        _t_M_SB_Laser_flag = 4;

                        break;

                    case 4:
                        //雷射開

                        if (StartMarking())
                        {
                            LaserStatus_Emission = true;
                            Invoke(new updatalaserstauts(updatalaser));//2024-05-24
                            Thread.Sleep(100);
                            _t_M_SB_Laser_flag = 5;
                        }
                        else
                            return;
                        break;

                    case 5:
                        //雷射雕刻完成
                        if (!LaserStatus_Emission)
                            _t_M_SB_Laser_flag = 6;
                        break;

                    case 6:
                        _t_M_SB_Laser_flag = 7;
                        break;

                    case 7:
                        //流程結束
                        EnableTabPages(true);
                        M_UI_Running = false;
                        _t_M_SB_Laser_flag = 0;
                        if (_t_M_SB_Laser != null)
                        {
                            _t_M_SB_Laser.Abort();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                //txt_Motion_ErrorMsg.Text = ex.ToString();
            }
        }

        delegate void updatalaserstauts();
        private void updatalaser()
        {
            //雷射狀態
            if (LaserStatus_Emission)
            {
                MUI_SB_LaserStatus_Emission.BackColor = Color.LawnGreen;
                UI_TB_Test_Laser_EmissionStation.BackColor = Color.LawnGreen;
            }
            else
            {
                MUI_SB_LaserStatus_Emission.BackColor = Color.Gray;
                UI_TB_Test_Laser_EmissionStation.BackColor = Color.Gray;
            }

            MessageParam();
        }

        delegate void dele_msgShow(string msg);
        private void ErrMSG_Show(string msg)
        {
            MessageBox.Show($"{msg}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void TipMSG_Show(string msg)
        {
            MessageBox.Show($"{msg}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void btn_M_SBLaser_Click2()
        {
            _t_M_SB_Laser_flag2 = 1;
            _t_M_SB_Laser2 = new Thread(_M_SB_Laser2);
            _t_M_SB_Laser2.Start();
        }
        private void _M_SB_Laser2()
        {
            while (true)
            {
                M_SB_Laser2();
            }
        }
        private void M_SB_Laser2()
        {
            try
            {
                switch (_t_M_SB_Laser_flag2)
                {
                    case 0:
                        Thread.Sleep(200);
                        break;
                    case 1:
                        //確認各軸訊號
                        if (Aerotech_En && AZD_En)// && Aerotech_Home && DeviceFlag)
                        {
                            M_UI_Running = true;
                            //關氣
                            ElectricValve("吸嘴&23層通道關");
                            //Enable頁面避免人員再次誤觸循環
                            EnableTabPages(false);

                            _t_M_SB_Laser_flag2 = 2;
                        }
                        else
                        {
                            _t_M_SB_Laser_flag2 = 0;
                        }
                        break;

                    case 2:
                        //檔案ezm專案檔讀取,成功回傳0
                        //int rlf = axMMMark.LoadFile(Data.SBLoad.MarkingMate_ezm_File);
                        //if (rlf == 0)
                        _t_M_SB_Laser_flag2 = 21;
                        break;

                    case 21:
                        ////提前吹氣
                        //ElectricValve("吸嘴開");
                        //Thread.Sleep(Convert.ToInt32(txt_SB_BlowToLaser_DelayTimes.Text));
                        //切換二階雷射設定
                        //btn_Set_SBLaserData2_Click(null, null);
                        //Thread.Sleep(1000);
                        _t_M_SB_Laser_flag2 = 3;
                        break;

                    case 3:
                        //吹氣
                        //this.Invoke((MethodInvoker)delegate () { SB_ALaserTimes = Convert.ToInt32(txt_SB_LaserToCloseAir_DelayTimes.Text); });
                        //_M_Laser_Air = true;
                        _t_M_SB_Laser_flag2 = 4;

                        break;

                    case 4:
                        //雷射開
                        if (StartMarking())
                        {
                            LaserStatus_Emission = true;

                            Invoke(new updatalaserstauts(updatalaser));//2024-05-24

                            Thread.Sleep(100);
                            _t_M_SB_Laser_flag2 = 5;
                        }

                        else
                            return;
                        break;

                    case 5:
                        //雷射雕刻完成
                        if (!LaserStatus_Emission)
                            _t_M_SB_Laser_flag2 = 6;
                        break;

                    case 6:
                        _t_M_SB_Laser_flag2 = 7;
                        break;

                    case 7:
                        //流程結束
                        EnableTabPages(true);
                        M_UI_Running = false;
                        _t_M_SB_Laser_flag2 = 0;
                        if (_t_M_SB_Laser2 != null)
                        {
                            _t_M_SB_Laser2.Abort();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                //txt_Motion_ErrorMsg.Text = ex.ToString();
            }
        }

        delegate void updatastauts();
        private void updata()
        {
            txt_MUI_SB_ALaserPower.Text = GetSpot_Power(StrSpotName1).ToString() + " %";
            //txt_MUI_SB_ALaserPower.Text = txt_SB_ALaserPower2.Text;
        }


        private void btn_M_AF_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行CCD對焦流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (FlowAllow())
                {
                    _EQP_Status = enumEQP_Status.MANU;
                    _t_M_AutoFocus = new Thread(_M_AutoFocus);
                    _t_M_AutoFocus.Start();
                    _t_M_AutoFocus_flag = 1;
                }

            }
        }

        private void _M_AutoFocus()
        {
            _RunFlag = true;
            while (_RunFlag)
            {
                M_AutoFocus();
                Thread.Sleep(10);
            }
        }

        private void M_AutoFocus()
        {
            try
            {
                switch (_t_M_AutoFocus_flag)
                {
                    case 0:
                        Thread.Sleep(200);
                        break;
                    case 1:
                        //確認各軸訊號
                        if (Aerotech_En && AZD_En)
                        {
                            M_UI_Running = true;
                            //Enable頁面避免人員再次誤觸循環
                            EnableTabPages(false);
                            _t_M_AutoFocus_flag = 21;
                        }
                        else
                        {
                            _t_M_AutoFocus_flag = 0;
                        }
                        break;

                    case 21: //上升安全高度
                        if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                        {
                            _t_M_AutoFocus_flag = 2;
                        }
                        break;

                    case 2:
                        //CCD->測高
                        if (_Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000))
                        {
                            if (Motion_X && Motion_Y && Motion_Z)
                            {
                                Distance_X = -(Convert.ToDouble(_msPosData.M_Laser2Vision_X) - Convert.ToDouble(_msPosData.M_Laser2Height_X));
                                Distance_Y = -(Convert.ToDouble(_msPosData.M_Laser2Vision_Y) - Convert.ToDouble(_msPosData.M_Laser2Height_Y));

                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(0, Distance_X, 60);
                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(1, Distance_Y, 60);

                                _t_M_AutoFocus_flag = 3;
                            }
                        }
                        else
                        {
                            _t_M_AutoFocus_flag = 0;
                        }
                        break;
                    case 3:
                        if (_Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000))
                        {
                            if (Motion_X && Motion_Y && Motion_Z)
                            {
                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(2, _msPosData.M_H_HeightZ, 20);

                                _t_M_AutoFocus_flag = 4;
                            }
                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                        break;
                    case 4:
                        //確認到位
                        if (_Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000))
                        {
                            if (Motion_X && Motion_Y && Motion_Z)
                            {
                                _t_M_AutoFocus_flag = 5;
                                Thread.Sleep(50);
                            }
                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                        break;
                    case 5:
                        //測高
                        KeyenceHeight_Value = "11";
                        _Keyence_Height.Send(Keyence_Height_Send_Data);
                        Thread.Sleep(50);
                        _t_M_AutoFocus_flag = 6;
                        break;
                    case 6:
                        //確認測高數值
                        if (KeyenceHeight_Value != null)
                        {
                            _t_M_AutoFocus_flag = 7;
                        }
                        else
                        {

                        }
                        break;
                    case 7:
                        //取得量測數值
                        _t_M_AF_Zoffset = Convert.ToDouble(KeyenceHeight_Value);
                        if (Math.Abs(_t_M_AF_Zoffset) > 7)
                        {
                            _t_M_AF_Zoffset = 0;
                            _t_M_AutoFocus_flag = 0;
                            MessageBox.Show("測高異常");

                            if (_t_M_AutoFocus != null)
                            {
                                _t_M_AutoFocus.Abort();

                            }
                        }
                        else
                        {
                            _t_M_AutoFocus_flag = 81;
                        }
                        break;

                    case 81: //上升安全高度
                        if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                        {
                            _t_M_AutoFocus_flag = 8;
                        }
                        break;

                    case 8:
                        //回到CCD位
                        if (_Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000))
                        {
                            if (Motion_X && Motion_Y && Motion_Z)
                            {
                                Distance_X = (Convert.ToDouble(_msPosData.M_Laser2Vision_X) - Convert.ToDouble(_msPosData.M_Laser2Height_X));
                                Distance_Y = (Convert.ToDouble(_msPosData.M_Laser2Vision_Y) - Convert.ToDouble(_msPosData.M_Laser2Height_Y));

                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(0, Distance_X, 60);
                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(1, Distance_Y, 60);

                                _t_M_AutoFocus_flag = 9;
                            }
                        }
                        else
                        {

                        }
                        break;
                    case 9:
                        //移動到CCD焦點
                        if (_Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000))
                        {
                            if (Motion_X && Motion_Y && Motion_Z)
                            {
                                Distance_Z = (_msPosData.M_H_VisionZ - _t_M_AF_Zoffset);
                                M_SB_Auto_Distance_Z = Distance_Z;
                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(2, Distance_Z, 20);
                                _t_M_AutoFocus_flag = 10;
                            }
                        }
                        else
                        {

                        }
                        break;
                    case 10:
                        if (_Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000))
                        {
                            if (Motion_X && Motion_Y && Motion_Z)
                            {
                                _EQP_Status = enumEQP_Status.IDLE;
                                _t_M_AutoFocus_flag = 0;
                                M_UI_Running = false;
                                EnableTabPages(true);
                                _t_M_AF_UIUpdata = true;
                                _RunFlag = false;
                                if (_t_M_AutoFocus != null)
                                {
                                    _t_M_AutoFocus.Abort();

                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _RunFlag = false;

            }
        }

        private void btn_M_MoveToLaser_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行當前CCD位置移動至噴嘴位置?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _t_M_MoveToLaser = new Thread(_M_MoveToLaser);
                _t_M_MoveToLaser.Start();
                _t_M_MoveToLaser_flag = 1;
            }
        }

        private void _M_MoveToLaser()
        {
            while (true)
            {
                M_MoveToLaser();
            }
        }

        private void M_MoveToLaser()
        {
            try
            {
                switch (_t_M_MoveToLaser_flag)
                {
                    case 0:
                        Thread.Sleep(100);
                        break;
                    case 1:
                        if (Aerotech_En && AZD_En)
                        {
                            M_UI_Running = true;
                            //Enable頁面避免人員再次誤觸循環
                            EnableTabPages(false);
                            _t_M_MoveToLaser_flag = 11;
                        }
                        else
                        {
                            _t_M_MoveToLaser_flag = 0;
                        }
                        break;

                    case 11: //z 上升至安全位置
                        if (Aerotech_En && AZD_En)
                        {
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                                _t_M_MoveToLaser_flag = 2;
                        }
                        else
                        {
                            _t_M_MoveToLaser_flag = 0;
                        }
                        break;

                    case 2:
                        if (_Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000))
                        {
                            if (Motion_X && Motion_Y && Motion_Z)
                            {
                                Distance_X = -Convert.ToDouble(_msPosData.M_Laser2Vision_X);
                                Distance_Y = -Convert.ToDouble(_msPosData.M_Laser2Vision_Y);

                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(0, Distance_X, 20);
                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(1, Distance_Y, 20);

                                _t_M_MoveToLaser_flag = 3;
                            }
                        }
                        else
                        {

                        }
                        break;
                    case 3:
                        if (_Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000))
                        {
                            if (Motion_X && Motion_Y && Motion_Z)
                            {
                                Distance_Z = Convert.ToDouble(_msPosData.M_H_LaserZ) - _t_M_AF_Zoffset + Convert.ToDouble(txt_M_MoveToLaser_OffsetZ.Text);

                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(2, Distance_Z, 5);

                                _t_M_MoveToLaser_flag = 4;
                            }
                        }
                        else
                        {

                        }
                        break;
                    case 4:
                        if (_Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000))
                        {
                            if (Motion_X && Motion_Y && Motion_Z)
                            {
                                _t_M_MoveToLaser_flag = 0;
                                M_UI_Running = false;
                                EnableTabPages(true);

                                if (_t_M_MoveToLaser != null)
                                {
                                    _t_M_MoveToLaser.Abort();

                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                //txt_Motion_ErrorMsg.Text = ex.ToString();
            }
        }
        /// <summary>
        /// 二階高度切回正常雷射Z出球高度
        /// </summary>
        private void btn_M_MoveToLaser_Click3()
        {
            _t_M_MoveToLaser3 = new Thread(_M_MoveToLaser3);
            _t_M_MoveToLaser3.Start();
            _t_M_MoveToLaser_flag3 = 1;
        }
        private void _M_MoveToLaser3()
        {
            while (true)
            {
                M_MoveToLaser3();
            }
        }
        private void M_MoveToLaser3()
        {
            try
            {
                switch (_t_M_MoveToLaser_flag3)
                {
                    case 0:
                        Thread.Sleep(100);
                        break;
                    case 1:
                        if (Aerotech_En && AZD_En)
                        {
                            M_UI_Running = true;
                            //Enable頁面避免人員再次誤觸循環
                            EnableTabPages(false);
                            _t_M_MoveToLaser_flag3 = 2;
                        }
                        else
                        {
                            _t_M_MoveToLaser_flag3 = 0;
                        }
                        break;
                    case 2:
                        if (_Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000))
                        {
                            if (Motion_X && Motion_Y && Motion_Z)
                            {
                                //Distance_X = -Convert.ToDouble(_msPosData.M_Laser2Vision_X);
                                //Distance_Y = -Convert.ToDouble(_msPosData.M_Laser2Vision_Y);

                                //this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(0, Distance_X, 20);
                                //this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(1, Distance_Y, 20);

                                _t_M_MoveToLaser_flag3 = 3;
                            }
                        }
                        else
                        {

                        }
                        break;
                    case 3:
                        if (_Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000))
                        {
                            if (Motion_X && Motion_Y && Motion_Z)
                            {
                                Distance_Z = Convert.ToDouble(_msPosData.M_H_LaserZ) - _t_M_AF_Zoffset + Convert.ToDouble(txt_M_MoveToLaser_OffsetZ.Text);

                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(2, Distance_Z, 5);

                                _t_M_MoveToLaser_flag3 = 4;
                            }
                        }
                        else
                        {

                        }
                        break;
                    case 4:
                        if (_Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000))
                        {
                            if (Motion_X && Motion_Y && Motion_Z)
                            {
                                _t_M_MoveToLaser_flag3 = 0;
                                M_UI_Running = false;
                                EnableTabPages(true);

                                if (_t_M_MoveToLaser3 != null)
                                {
                                    _t_M_MoveToLaser3.Abort();

                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                //txt_Motion_ErrorMsg.Text = ex.ToString();
            }
        }


        private void _M_Electric()
        {
            while (true)
            {
                M_Electric();

            }
        }

        private void M_Electric()
        {
            while (true)
            {
                try
                {
                    if (_M_Laser_Air)
                    {
                        M_UI_Running = true;


                        ElectricValve("吸嘴開");

                        Thread.Sleep(SB_ALaserTimes);

                        ElectricValve("吸嘴關");

                        _M_Laser_Air = false;
                        M_UI_Running = false;
                    }
                }
                catch
                {

                }

                Thread.Sleep(10);
            }
        }

        #endregion


        /// <summary>
        /// [通用]循環-驅動至定點位置
        /// <para>先驅動Z，之後在同動XYQ</para>
        /// </summary>
        /// <param name="_data">[in]欲移動位置參數</param>
        /// <param name="step">[ref]當前流程步驟</param>
        /// <param name="_up_z">[in]Z軸是否拉高</param>
        /// <returns>回饋是否完成全部流程</returns>
        public bool FixedPoint_Function(Define.Pos_Data _data, ref int step, bool _up_z)
        {
            bool result = false;

            try
            {
                //動作前都要先確認是否有無Server On
                if (Aerotech_En && AZD_En &&
                    Aerotech_Home && AZD_Rdy)
                {

                    switch (step)
                    {
                        case 0:
                            if (!_up_z)
                            {
                                step++;
                            }
                            else
                            {
                                if (!_data.Z.Flag/*Check_Z(HMI.Home.Z.Position, true)*/)
                                {
                                    MoveAbs_Z(_data.Z.Position, _data.Z.Speed);
                                }
                                else
                                {
                                    step++;
                                }
                            }
                            break;
                        case 1:
                            bool flagx = false;
                            bool flagy = false;
                            bool flagq = false;

                            if (!_data.X.Flag/*Check_X(HMI.Home.X.Position, true)*/)
                            {
                                MoveAbs_X(_data.X.Position, _data.X.Speed);
                            }
                            else
                            {
                                flagx = true;
                            }

                            if (!_data.Y.Flag/*Check_Y(HMI.Home.Y.Position, true)*/)
                            {
                                MoveAbs_Y(_data.Y.Position, _data.Y.Speed);
                            }
                            else
                            {
                                flagy = true;
                            }

                            if (!_data.Q.Flag/*Check_Q(HMI.Home.Q.Position, true)*/)
                            {
                                MoveAbs_Q(_data.Q.Position, _data.Q.Speed, _data.Q.Acceleration, _data.Q.Deceleration);
                            }
                            else
                            {
                                flagq = true;
                            }

                            if (flagx && flagy && flagq)
                            {
                                step++;
                                result = true;
                            }
                            break;
                        default: break;
                    }
                }
                else
                {
                    //step = -1;
                    Console.WriteLine("Server 沒有ON 或 硬體狀態異常 或 伺服軸移動中");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                step = -1;
            }

            return result;
        }
        private bool _emsstop = false;
        /// <summary>
        /// 是否即停
        /// </summary>
        public bool _EMS_Stop
        {
            get
            {
                return _emsstop;
            }
            set
            {
                if (value)//如果觸發急停，則復原觸發狀態
                {
                    _emsstop = false;
                    _RunFlag = false;
                    _DiskRunFlag = false;
                    _LaserRunFlag = false;
                    _ClearFlag = false;
                }
            }
        }

        #region HMI BT Event
        public void Add_Event()
        {
            //auto frame
            this.B_ST_FileLoad.Click += new System.EventHandler(this.B_PathFileLoad_Click);
            this.B_ST_FileLoad2.Click += new System.EventHandler(this.B_PathFileLoad_Click);
            this.B_RecipeFileLoad.Click += new System.EventHandler(this.B_RecipeFileLoad_Click);
            this.btn_AUTO_Start.Click += new System.EventHandler(this.btn_AUTO_Start_Click);
            this.btn_AUTO_Stop.Click += new System.EventHandler(this.btn_AUTO_Stop_Click);
        }

        #endregion

        #region 顯示各軸當前資訊 Timer 改用 Thread
        Thread _Festo_Thread, _UI_Updata_SlowThread, _AerotechThread;
        private bool _UpdataUI_Flag = false;
        public void Start_Updata_Thread()
        {
            _AerotechThread = new Thread(_AerotechUpdata);
            _Festo_Thread = new Thread(_FestoUpdata);
            _UI_Updata_SlowThread = new Thread(_UI_SlowUpdata);
            _UpdataUI_Flag = true;
            _AerotechThread.IsBackground = true;
            _Festo_Thread.IsBackground = true;
            _UI_Updata_SlowThread.IsBackground = true;
            _AerotechThread.Start();
            _Festo_Thread.Start();
            _UI_Updata_SlowThread.Start();
        }
        public void End_UI_Updata_Thread()
        {
            _UpdataUI_Flag = false;

            if (_Festo_Thread != null)
            {
                _Festo_Thread.Abort();
            }
        }
        private void _FestoUpdata()
        {
            string str;
            while (_UpdataUI_Flag)
            {
                try
                {
                    tact_time2.Restart();
                    UpdateFesto();
                    tact_time2.Stop();
                    //Console.WriteLine($"UpdateFesto[{tact_time2.ElapsedMilliseconds}]ms");
                    if (tact_time2.ElapsedMilliseconds > 60)
                    {
                        // LogMsgAdd(Error_Log, lb_ErrorList, $"UpdateFesto[{tact_time2.ElapsedMilliseconds}]ms", tmpErrStr);
                        Error_Log.Add($"UpdateFesto[{tact_time2.ElapsedMilliseconds}]ms");
                    }

                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted ||
                                   ex.Message.Contains("WSACancelBlockingCall"))
                {
                    Console.WriteLine($"[ST BallMountFlow SocketException ex Error]: {ex.ToString()}");
                    str = $"[ST BallMountFlow SocketException ex Error]: {ex.ToString()}";
                    //LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                    Error_Log.Add(str);
                }
                catch (System.IO.IOException ex) when (ex.InnerException is SocketException sockEx &&
                                             (sockEx.SocketErrorCode == SocketError.Interrupted ||
                                              sockEx.Message.Contains("WSACancelBlockingCall")))
                {
                    Console.WriteLine($"[ST BallMountFlow Error IOException ex]: {ex.ToString()}");
                    str = $"[ST BallMountFlow Error IOException ex]: {ex.ToString()}";
                    //LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                    Error_Log.Add(str);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    if (_FestoUpdateFlag)
                    {
                        _FestoUpdateFlag = false;
                        //LogMsgAdd(Error_Log, lb_ErrorList, "_FestoUpdateFlag = false", tmpErrStr);
                    }

                }

                Thread.Sleep(10);
            }
        }

        private void _AerotechUpdata()
        {
            while (_UpdataUI_Flag)
            {
                try
                {
                    //tact_time.Restart();
                    UpdateAerotech();
                    UpdataAZDStauts();
                    //tact_time.Stop();
                    //if (tact_time.ElapsedMilliseconds > 4)
                    //	Console.WriteLine($"UpdateAerotech[{tact_time.ElapsedMilliseconds}]ms");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                Thread.Sleep(1);
            }
        }

        private void _UI_SlowUpdata()
        {
            while (_UpdataUI_Flag)
            {
                try
                {

                    Invoke(new _UpdataIOStauts(UpdataSlowStauts));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                Thread.Sleep(300);
            }
        }


        private void UpdateAerotech()
        {
            if (_Using)
            {

                _result = _customDiagnostics.Retrieve();
                DriveStatus driveStatusX = _result.Axis[AxisStatusSignal.DriveStatus, "X"].ConvertValueDriveStatus();
                DriveStatus driveStatusY = _result.Axis[AxisStatusSignal.DriveStatus, "Y"].ConvertValueDriveStatus();
                DriveStatus driveStatusZ = _result.Axis[AxisStatusSignal.DriveStatus, "Z"].ConvertValueDriveStatus();
                AxisStatus axisStatusX = _result.Axis[AxisStatusSignal.AxisStatus, "X"].ConvertValueAxisStatus();
                AxisStatus axisStatusY = _result.Axis[AxisStatusSignal.AxisStatus, "Y"].ConvertValueAxisStatus();
                AxisStatus axisStatusZ = _result.Axis[AxisStatusSignal.AxisStatus, "Z"].ConvertValueAxisStatus();
                AxisFault axisFaultX = _result.Axis[AxisStatusSignal.AxisFault, "X"].ConvertValueAxisFault();
                AxisFault axisFaultY = _result.Axis[AxisStatusSignal.AxisFault, "Y"].ConvertValueAxisFault();
                AxisFault axisFaultZ = _result.Axis[AxisStatusSignal.AxisFault, "Z"].ConvertValueAxisFault();

                //參數
                HOME_X = axisStatusX.Homed;
                HOME_Y = axisStatusY.Homed;
                HOME_Z = axisStatusZ.Homed;
                En_X = driveStatusX.Enabled;
                En_Y = driveStatusY.Enabled;
                En_Z = driveStatusZ.Enabled;
                Motion_X = driveStatusX.InPosition;
                Motion_Y = driveStatusY.InPosition;
                Motion_Z = driveStatusZ.InPosition;
                Limit_XP = axisFaultX.CcwSoftwareLimitFault;
                Limit_YP = axisFaultY.CcwSoftwareLimitFault;
                Limit_ZN = axisFaultZ.CcwSoftwareLimitFault;
                Limit_XN = axisFaultX.CwSoftwareLimitFault;
                Limit_YN = axisFaultY.CwSoftwareLimitFault;
                Limit_ZP = axisFaultZ.CwSoftwareLimitFault;

                //Position
                Now_X = _result.Axis[AxisStatusSignal.PositionFeedback, "X"].Value;
                Now_Y = _result.Axis[AxisStatusSignal.PositionFeedback, "Y"].Value;
                Now_Z = _result.Axis[AxisStatusSignal.PositionFeedback, "Z"].Value;


                //Console.WriteLine($"Time{DateTime.Now.Millisecond }");
            }
        }

        delegate void _Updata_Aerotech();
        delegate void _Updata_KeyenceHeight();
        delegate void _UpdataIOStauts();
        delegate void _UpdataAZDStauts();
        private void UpdataKeyence()
        {
            if (txt_KeyenceHeight_Value.Text != KeyenceHeight_Value)
            {
                txt_KeyenceHeight_Value.Text = KeyenceHeight_Value;
            }
        }

        /// <summary>
        /// 更新噴嘴氣壓
        /// </summary>
        private void UpdateFesto()
        {
            if (_Using && !_FestoUpdateFlag/* && !_ProportionalValveFlag*/)
            {

                _FestoUpdateFlag = true;
                //雷射吸嘴				 
                //NowNozzlePressure = FestoPressure.P05_GetPressure(ushort.Parse(_ConfigSystem._FESTO_PSensor_Port));
                P_F_EthernetIP.Festo_GetPressure(ref NowNozzlePressure, ref NozzleValveActValue, ref StageVacuum);
                //Console.WriteLine($"TIME[{DateTime.Now.Millisecond}]: {NowNozzlePressure}");
                if (_msPosData.NozzleAir_Log)
                    Error_Log.Add($"[P05_GetPressure]: {NowNozzlePressure}");

                if (NowNozzle_MAX_Pressure < NowNozzlePressure)
                    NowNozzle_MAX_Pressure = NowNozzlePressure;
                //Console.WriteLine($"TIME{DateTime.Now.Millisecond}");

                _FestoUpdateFlag = false;

            }
        }

        /// <summary>
        /// 更新比例閥
        /// </summary>
        private bool UpdateProportionalValve(ushort value)
        {
            if (_Using /*&& !_ProportionalValveFlag*/)
            {
                _ProportionalValveFlag = true;
                Thread.Sleep(150);

                FestoPressure.PValveSetPressure(ushort.Parse(_ConfigSystem._FESTO_PPC_Valve_Port), value);//發送設定值
                Thread.Sleep(100);
                NozzleValveSetValue = FestoPressure.PValveGet_SetPressure(ushort.Parse(_ConfigSystem._FESTO_PPC_Valve_Port));//讀回設定值
                Thread.Sleep(100);
                NozzleValveActValue = FestoPressure.PValveGet_ActPressure(ushort.Parse(_ConfigSystem._FESTO_PPC_Valve_Port));//讀回實際值

                if (NozzleValveActValue >= 65535)//讀回實際值為異常
                    NozzleValveActValue = 0;

                _ProportionalValveFlag = false;

                return true;
                //            if (Math.Abs(NozzleValveSetValue - value) < 2 && Math.Abs(NozzleValveActValue - value) < 2)
                //{
                //                return true;
                //}


            }
            return false;
        }

        private void UpdataSlowStauts()
        {
            if (_Using)
            {
                //io
                IOCard_Input_UI();
                IOCard_Output_UI();
                IOCard_Read();
                IPGLaserControl_Func();


                //aerotech
                //極限
                if (Limit_XN)
                {
                    ptb_Motion_HStatus_X.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisLimitN];
                }
                else if (Limit_XP)
                {
                    ptb_Motion_HStatus_X.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisLimitP];
                }
                else if (HOME_X)
                {
                    ptb_Motion_HStatus_X.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisHome];
                }
                else
                {
                    ptb_Motion_HStatus_X.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisNone];
                }

                if (Limit_YN)
                {
                    ptb_Motion_HStatus_Y.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisLimitN];
                }
                else if (Limit_YP)
                {
                    ptb_Motion_HStatus_Y.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisLimitP];
                }
                else if (HOME_Y)
                {
                    ptb_Motion_HStatus_Y.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisHome];
                }
                else
                {
                    ptb_Motion_HStatus_Y.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisNone];
                }

                if (Limit_ZN)
                {
                    ptb_Motion_HStatus_Z.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisLimitN];
                }
                else if (Limit_ZP)
                {
                    ptb_Motion_HStatus_Z.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisLimitP];
                }
                else if (HOME_Z)
                {
                    ptb_Motion_HStatus_Z.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisHome];
                }
                else
                {
                    ptb_Motion_HStatus_Z.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisNone];
                }

                //電機狀態
                if (!Motion_X)
                {
                    btn_Motion_MotorStatus_X.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusOFF];
                    lbl_Motion_Status_X.Text = "移動";
                }
                else
                {
                    btn_Motion_MotorStatus_X.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusON];
                    lbl_Motion_Status_X.Text = "待機";
                }

                if (!Motion_Y)
                {
                    btn_Motion_MotorStatus_Y.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusOFF];
                    lbl_Motion_Status_Y.Text = "移動";
                }
                else
                {
                    btn_Motion_MotorStatus_Y.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusON];
                    lbl_Motion_Status_Y.Text = "待機";
                }

                if (!Motion_Z)
                {
                    btn_Motion_MotorStatus_Z.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusOFF];
                    lbl_Motion_Status_Z.Text = "移動";
                }
                else
                {
                    btn_Motion_MotorStatus_Z.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusON];
                    lbl_Motion_Status_Z.Text = "待機";
                }

                //Enable
                if (En_X)
                {
                    chk_Motion_Enable_X.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.ON];
                    chk_Motion_Enable_X.Checked = true;
                }
                else
                {
                    chk_Motion_Enable_X.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.OFF];
                    chk_Motion_Enable_X.Checked = false;
                }

                if (En_Y)
                {
                    chk_Motion_Enable_Y.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.ON];
                    chk_Motion_Enable_Y.Checked = true;
                }
                else
                {
                    chk_Motion_Enable_Y.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.OFF];
                    chk_Motion_Enable_Y.Checked = false;
                }

                if (En_Z)
                {
                    chk_Motion_Enable_Z.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.ON];
                    chk_Motion_Enable_Z.Checked = true;
                }
                else
                {
                    chk_Motion_Enable_Z.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.OFF];
                    chk_Motion_Enable_Z.Checked = false;
                }

                if (En_X && En_Y && En_Z)
                {
                    Aerotech_En = true;
                }
                else
                {
                    Aerotech_En = false;
                }

                if (HOME_X && HOME_Y && HOME_Z)
                {
                    Aerotech_Home = true;
                }
                else
                {
                    Aerotech_Home = false;
                }

                //
                if (AZD_En)
                {
                    chk_Motion_Enable_Q.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.ON];
                    chk_Motion_Enable_Q.Checked = true;
                }
                else
                {
                    chk_Motion_Enable_Q.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.OFF];
                    chk_Motion_Enable_Q.Checked = false;
                }

                if (AZD_Rdy)
                {
                    btn_Motion_MotorStatus_Q.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusON];
                }
                else
                {
                    btn_Motion_MotorStatus_Q.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusOFF];
                }
            }
        }

        private void UpdataAZDStauts()
        {
            try
            {

                if (_orientalConnected)
                {
                    ////移動
                    //if (_AZD_Controller.AZD_Status.MOVE == 1)
                    //{
                    //    AZD_Motion = false;
                    //}
                    ////停止
                    //else if (_AZD_Controller.AZD_Status.MOVE == 0)
                    //{
                    //    AZD_Motion = true;
                    //}

                    //if (_AZD_Controller.AZD_Status.CRNT == 1)
                    //{
                    //    AZD_En = true;
                    //}
                    //else
                    //{
                    //    AZD_En = false;
                    //}

                    //if (_AZD_Controller.AZD_Status.READY == 1)
                    //{
                    //    AZD_Rdy = true;
                    //}
                    //else
                    //{
                    //    AZD_Rdy = false;
                    //}


                    //if (!AZD_Motion)
                    //{
                    //    lbl_Motion_Status_Q.Text = "移動";
                    //}
                    //else
                    //{
                    //    lbl_Motion_Status_Q.Text = "待機";
                    //}


                    //位置讀取
                    //Now_Q = Convert.ToDouble(_AZD_Controller.ActPos / AZD_MotorResolution);
                    //this.txt_Motion_Position_Q.Text = Now_Q.ToString(); // (actualposition / AZD_MotorResolution).ToString();
                    //Now_Q = Convert.ToDouble(_AZD_Controller.CmdPos / AZD_MotorResolution);
                    //this.txt_Motion_CmdPosition_Q.Text = Now_Q.ToString();

                    //Get the motion I/O status of the axis.



                    _returnCode = Motion.mAcm_AxGetMotionIO(m_Axishand[0], ref IOStatus);
                    if (_returnCode == (uint)ErrorCode.SUCCESS)
                    {
                        GetMotionIOStatus(IOStatus);
                    }


                    //Get the Axis's current state
                    _returnCode = Motion.mAcm_AxGetState(m_Axishand[0], ref AxState);
                    if (_returnCode == (uint)ErrorCode.SUCCESS)
                    {
                        strTemp = ((AxisState)AxState).ToString();

                        if (AxState == (uint)AxisState.STA_AX_READY)
                        {
                            //lbl_Motion_Status_Q.Text = "待機";
                            AZD_Motion = false;
                        }
                        else
                        {
                            //lbl_Motion_Status_Q.Text = "移動";
                            AZD_Motion = true;

                        }
                    }

                    //Set actual position for the specified axis

                    Motion.mAcm_AxGetActualPosition(m_Axishand[0], ref azd_Act_Pos);

                    // set cmd

                    Motion.mAcm_AxGetCmdPosition(m_Axishand[0], ref azd_CMD_Pos);




                    //LogMsgAdd(Error_Log, lb_ErrorList, _AZD_Controller.ErrMsg, tmpErrStr);
                }
                else
                {
                    Now_Q = 0.0;
                    AZD_En = false;
                }

                ////雷射狀態
                //if (LaserStatus_Emission)
                //    MUI_SB_LaserStatus_Emission.BackColor = Color.LawnGreen;

                //雷射控制卡Error                  
                //LogMsgAdd(Error_Log, lb_ErrorList, ReturnErr, tmpErrStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"exception: {ex.ToString()} ");
            }
        }


        /// <summary>
        /// PCIE1203 iostatus
        /// </summary>
        /// <param name="IOStatus"></param>
        private void GetMotionIOStatus(uint IOStatus)
        {
            if ((IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_ALM) > 0)//ALM
            {
                AZD_Alarm = true;
            }
            else
            {
                AZD_Alarm = false;
            }

            if ((IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_RDY) > 0)
            {
                AZD_Rdy = true;
            }
            else
            {
                AZD_Rdy = false;
            }


            if ((IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_SVON) > 0)
            {
                AZD_En = true;
            }
            else
            {
                AZD_En = false;
            }
        }
        /// <summary>
        /// PowerMeter 量測
        /// </summary>
        private void PM_Status()
        {
            // PM
            _powermeter.GetPower(out double _power);
            _power *= 1000;
            L_PowerMeter.Text = $"Now Power = {_power.ToString("F2")}mW  ";
            //Console.WriteLine($"Now Power[{_power.ToString("F2")}]mW");
            try
            {
                if (PM_DataCollect)
                {
                    _PM_Data.Add(_power);
                }
                else
                {
                    if (_PM_Data.Count > 0)
                    {
                        L_PowerMeterMAX.Text = $"Max Power = {_PM_Data.Max().ToString("F2")}mW  ";
                        AlignPM = _PM_Data.Max();
                    }
                    else
                        L_PowerMeterMAX.Text = $"Max Power = 0 mW  ";
                }
            }
            catch (Exception ex)
            {

            }

        }
        /// <summary>
        /// 溫溼度 狀態
        /// </summary>
        private void getTHSensorStatus()
        {
            if (_THSensor.GetState().Equals(SteadyState.Ready))
            {
                try
                {
                    // Get the values from THController
                    _THSensor.ReadTH(out _TH_Data, out double[] results, out byte[] recvPackage);
                    txt_Temp1.Text = _TH_Data[0].temperature.ToString("F1");
                    txt_Temp2.Text = _TH_Data[1].temperature.ToString("F1");
                    txt_RH_1.Text = _TH_Data[0].humidity.ToString("F1");
                    txt_RH_2.Text = _TH_Data[1].humidity.ToString("F1");

                }
                catch (Exception ex)
                {
                }
            }
        }
        private void t_StatusUpData_Tick(object sender, EventArgs e)
        {
            t_StatusUpData.Enabled = false;
            if (_Using)
            {
                if (_PowerMeterConnect)
                    PM_Status();
                // TH
                //if(_TH_SensorConnect)
                //  getTHSensorStatus();


                if (_PizeoConnect)
                {
                    txt_PizeoNowX.Text = (_CU30CL.ctrRec.Position[1]).ToString();
                    txt_PizeoNowY.Text = (_CU30CL.ctrRec.Position[0]).ToString();
                    txt_PizeoNowZ.Text = (_CU30CL.ctrRec.Position[2]).ToString();

                    txt_PizeoVelX.Text = (_CU30CL.ctrRec.Speed[1]).ToString();
                    txt_PizeoVelY.Text = (_CU30CL.ctrRec.Speed[0]).ToString();
                    txt_PizeoVelZ.Text = (_CU30CL.ctrRec.Speed[2]).ToString();

                    chk_PizeoEnable_X.Checked = (_CU30CL.ctrRec.Enabled[1] > 0) ? true : false;
                    chk_PizeoEnable_Y.Checked = (_CU30CL.ctrRec.Enabled[0] > 0) ? true : false;
                    chk_PizeoEnable_Z.Checked = (_CU30CL.ctrRec.Enabled[2] > 0) ? true : false;

                    chk_PizeoHome_X.Checked = (_CU30CL.ctrRec.ReferenceValid[1] > 0) ? true : false;
                    chk_PizeoHome_Y.Checked = (_CU30CL.ctrRec.ReferenceValid[0] > 0) ? true : false;
                    chk_PizeoHome_Z.Checked = (_CU30CL.ctrRec.ReferenceValid[2] > 0) ? true : false;
                }
            }
            //pressure
            L_NozzlePressure.Text = $"NOW[{NowNozzlePressure.ToString("F2")}] Kpa";
            L_MaxPressure.Text = $"MAX[{NowNozzle_MAX_Pressure.ToString("F2")}] Kpa";
            L_MinPressure.Text = $"MIN[{NowNozzle_MIN_Pressure.ToString("F2")}] Kpa";
            L_PMax_PMin_Diff.Text = $"MAX - MIN [{(NowNozzle_MAX_Pressure - NowNozzle_MIN_Pressure).ToString("F2")}] Kpa";
            L_NozzleValveSetValve.Text = $"比例閥設定 [{NozzleValveSetValue / 10.0}] Kpa";
            L_NozzleValveActValve.Text = $"比例閥壓力 [{NozzleValveActValue / 10.0}] Kpa";
            L_LaserStep.Text = _LaserAlignFlowStep.ToString();
            if (_recipeData.SYSParam.SB_AirJudgDiff)
            {
                gp_Pressure.BackColor = ((NowNozzle_MAX_Pressure - NowNozzle_MIN_Pressure) >= _recipeData.SYSParam.NozzleSB_Diff_Value) ? Color.Red : Color.White;
            }
            else
            {
                gp_Pressure.BackColor = (NowNozzlePressure >= _recipeData.SYSParam.NozzleSB_Pressure) ? Color.Red : Color.White;
            }

            //單循環資訊更新
            if (_t_M_AF_UIUpdata)
            {
                lbl_M_AG_ZOffset.Text = _t_M_AF_Zoffset.ToString();
                lb_M_MoveToLaser_RelX.Text = "相對X: " + (-Convert.ToDouble(_msPosData.M_Laser2Vision_X)).ToString();
                lb_M_MoveToLaser_RelY.Text = "相對Y: " + (-Convert.ToDouble(_msPosData.M_Laser2Vision_Y)).ToString();
                lb_M_MoveToLaser_RelZ.Text = "相對Z: " + (Convert.ToDouble(_msPosData.M_H_LaserZ) - _t_M_AF_Zoffset + double.Parse(txt_M_MoveToLaser_OffsetZ.Text)).ToString();
                _t_M_AF_UIUpdata = false;
            }



            L_SB_SelectNo.Text = $"SB_No: {(SB_SelectIndex + 1).ToString()}";


            //加熱開門 斷電
            if (IO_InputData.DoorOpen)
            {
                //if (chk_HeaterPower.Checked)
                //    chk_HeaterPower.Checked = false;
            }
            //40度 強制lock
            if (E5CC.Now_PV > 40)
            {
                //IO_OutputControl("門鎖開");
            }

            L_ThickNo.Text = $"當前測高Pad No: [{Now_SB_ThickPadNO}]";
            L_SB_HeightZ.Text = $"SB 高度補償值 :{PublicData.SB_HeightZ}";

            UpdataKeyence();

            //azk
            Now_Q = azd_Act_Pos / AZD_MotorResolution;
            Now_Q %= 360;
            Now_ActQ = Now_Q;
            txt_Motion_Position_Q.Text = Now_Q.ToString("F3");
            Now_Q = azd_CMD_Pos / AZD_MotorResolution;
            Now_Q %= 360;
            txt_Motion_CmdPosition_Q.Text = Now_Q.ToString("F3");
            if (AxState == (uint)AxisState.STA_AX_READY)
            {
                lbl_Motion_Status_Q.Text = "待機";
                //AZD_Motion = false;
            }
            else
            {
                lbl_Motion_Status_Q.Text = "移動";
                //AZD_Motion = true;

            }

            //aeroteck
            if (_aerotechConnected)
            {
                txt_Motion_Position_X.Text = _result.Axis[AxisStatusSignal.PositionFeedback, "X"].Value.ToString("F3");
                txt_Motion_Position_Y.Text = _result.Axis[AxisStatusSignal.PositionFeedback, "Y"].Value.ToString("F3");
                txt_Motion_Position_Z.Text = _result.Axis[AxisStatusSignal.PositionFeedback, "Z"].Value.ToString("F3");
                txt_Motion_CmdPosition_X.Text = _result.Axis[AxisStatusSignal.PositionCommand, "X"].Value.ToString("F3");
                txt_Motion_CmdPosition_Y.Text = _result.Axis[AxisStatusSignal.PositionCommand, "Y"].Value.ToString("F3");
                txt_Motion_CmdPosition_Z.Text = _result.Axis[AxisStatusSignal.PositionCommand, "Z"].Value.ToString("F3");
            }


            t_StatusUpData.Enabled = true;
        }


        #endregion

        #region 座標相關
        private void btn_test_STPos_OpenFile_Click(object sender, EventArgs e)
        {
            try
            {
                //設定檔案限制
                openFileDialog1.Filter = "Excel Files (*.xls; *.xlsx)|*.xls;*.xlsx";

                MList_Log.Add("手動: 座標讀取測試 開啟");

                //開啟文件路徑
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    //取得Excel路徑
                    string stpos_file = openFileDialog1.FileName;
                    txt_test_STPos_File.Text = stpos_file;

                    MList_Log.Add("手動: 座標讀取測試 檔案- " + txt_test_STPos_File.Text);

                    //建立Excel物件
                    IWorkbook workbook;
                    int sheetnumber = 99;
                    DataTable dt = new DataTable();

                    //讀取Excel
                    using (FileStream file = new FileStream(stpos_file, FileMode.Open, FileAccess.Read))
                    {
                        //繪製資料表
                        workbook = WorkbookFactory.Create(file);

                        //取得分頁名稱
                        List<string> SheetName = new List<string>();

                        for (int i = 0; i < workbook.NumberOfSheets; i++)
                        {
                            SheetName.Add(workbook.GetSheetName(i));
                        }

                        //建立分頁選擇視窗 並回傳所選擇分頁index
                        ExcelSheet_Select excelSheet_Select = new ExcelSheet_Select();
                        excelSheet_Select.SheetNumber = (num) => { sheetnumber = num; };

                        excelSheet_Select.ShowSheetName(stpos_file, SheetName);

                        DialogResult result = excelSheet_Select.ShowDialog();

                        if (result == DialogResult.OK)
                        {
                            txt_test_STPos_Sheet.Text = workbook.GetSheetName(sheetnumber);

                            MList_Log.Add("手動: 座標讀取測試 選擇分頁- " + txt_test_STPos_Sheet.Text);

                            //根據index讀取分頁
                            ISheet sheet = workbook.GetSheetAt(sheetnumber);

                            MList_Log.Add("手動: 座標讀取測試 解析Excel");

                            //由第一列取標題做為欄位名稱
                            IRow headerRow = sheet.GetRow(0);
                            int cellCount = headerRow.LastCellNum; // 取欄位數
                            for (int i = headerRow.FirstCellNum; i < cellCount; i++)
                            {
                                //table.Columns.Add(new DataColumn(headerRow.GetCell(i).StringCellValue, typeof(double)));
                                dt.Columns.Add(new DataColumn("(標題" + (i + 1).ToString() + ")" + headerRow.GetCell(i, MissingCellPolicy.CREATE_NULL_AS_BLANK).StringCellValue));
                            }

                            //略過第零列(標題列)，一直處理至最後一列
                            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++)
                            {
                                IRow row = sheet.GetRow(i);
                                if (row == null) continue;

                                DataRow dataRow = dt.NewRow();

                                //依先前取得的欄位數逐一設定欄位內容
                                for (int j = row.FirstCellNum; j < cellCount; j++)
                                {
                                    ICell cell = row.GetCell(j, MissingCellPolicy.CREATE_NULL_AS_BLANK);
                                    if (cell != null)
                                    {
                                        //如要針對不同型別做個別處理，可善用.CellType判斷型別
                                        //再用.StringCellValue, .DateCellValue, .NumericCellValue...取值

                                        switch (cell.CellType)
                                        {
                                            case CellType.Numeric:
                                                dataRow[j] = cell.NumericCellValue;
                                                break;
                                            case CellType.Formula:
                                                dataRow[j] = "";
                                                break;
                                            default: // String
                                                     //此處只簡單轉成字串
                                                dataRow[j] = cell.StringCellValue;
                                                break;
                                        }
                                    }
                                }

                                dt.Rows.Add(dataRow);
                            }

                            MList_Log.Add("手動: 座標讀取測試 UI顯示");

                            //UI顯示
                            dGV_test_STFile.DataSource = null;
                            dGV_test_STFile.Rows.Clear();
                            dGV_test_STFile.Refresh();
                            dGV_test_STFile.Update();
                            dGV_test_STFile.DataSource = dt;

                            dGV_test_STFile.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToDisplayedHeaders;

                            for (int j = 0; j < sheet.LastRowNum; j++)
                            {
                                dGV_test_STFile.Rows[j].HeaderCell.Value = (j + 1).ToString();
                                Application.DoEvents();
                            }
                            for (int j = 0; j < dGV_test_STFile.ColumnCount; j++)
                            {
                                dGV_test_STFile.Columns[j].SortMode = DataGridViewColumnSortMode.NotSortable;
                            }

                            DialogResult dr = MessageBox.Show("分頁座標讀取完畢。\r\n是否進行座標轉換(當前座標轉向：" + cb_STPos_Rotate.Text + "度)", "提示", MessageBoxButtons.YesNo);
                            if (DialogResult.OK == dr || DialogResult.Yes == dr)
                            {
                                MList_Log.Add("手動: 從已讀取之檔案內容中篩選並取得相關座標資訊");

                                Cal_All_STPos(dt);
                                ////取得座標
                                //Get_STPos(dt);

                                ////計算旋轉後座標
                                //if (Get_STPos_Rotate())
                                //{
                                //    //計算錫球座標
                                //    if (Get_SBPos())
                                //    {

                                //    }
                                //}
                            }

                            workbook.Close();
                        }
                        else
                        {
                            MessageBox.Show("分頁選擇錯誤,請聯繫工程師確認");
                        }
                    }
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                Error_Log.Add("錯誤: " + ex.Message);
                MessageBox.Show(ex.ToString());
            }
        }

        private void B_Cal_All_STPos_Click(object sender, EventArgs e)
        {
            MList_Log.Add("手動: 從已讀取之檔案內容中篩選並取得相關座標資訊");

            Cal_All_STPos((DataTable)dGV_test_STFile.DataSource);
        }
        private void Cal_All_STPos(DataTable dt)
        {
            if (dt == null)
            {
                return;
            }

            //取得座標
            Get_STPos(dt);

            for (int i = 0; i < _recipeData.Panel.ST_Num; i++)
            {
                ST_SelectPanel[i]._numPad.Maximum = Pos.PAD_Pos.Count;
            }

            num_ST_Pad_No.Maximum = Pos.PAD_Pos.Count;
            Num_MovePOS_Pad_NO.Maximum = Pos.PAD_Pos.Count;

            //計算旋轉後座標
            if (Get_STPos_Rotate())
            {
                //計算錫球座標
                if (Get_SBPos())
                {
                    //Get_R_SBPos();
                }
            }
        }
        private void Get_STPos(DataTable dt)
        {
            try
            {
                //獲取數值
                MList_Log.Add("自動: 獲取座標 定位點");
                Pos.Mark_Pos.Clear();
                for (int i = 0; i < 4; i++)
                {
                    Define.Pos_List p = new Define.Pos_List();
                    p.X = Convert.ToDouble(dt.Rows[_recipeData.ST.MarkX_CellRow[i] - 1][_recipeData.ST.MarkX_CellCol[i] - 1]);
                    p.Y = Convert.ToDouble(dt.Rows[_recipeData.ST.MarkY_CellRow[i] - 1][_recipeData.ST.MarkY_CellCol[i] - 1]);
                    Pos.Mark_Pos.Add(p);
                }

                MList_Log.Add("自動: 獲取座標 測高點");
                Pos.Height_Pos.Clear();
                for (int i = 0; i < 4; i++)
                {
                    Define.Pos_List p = new Define.Pos_List();
                    p.X = Convert.ToDouble(dt.Rows[_recipeData.ST.HeightX_CellRow[i] - 1][_recipeData.ST.HeightX_CellCol[i] - 1]);
                    p.Y = Convert.ToDouble(dt.Rows[_recipeData.ST.HeightY_CellRow[i] - 1][_recipeData.ST.HeightY_CellCol[i] - 1]);
                    Pos.Height_Pos.Add(p);
                }

                MList_Log.Add("自動: 獲取座標 PAD中心點");
                Pos.PAD_Pos.Clear();
                List_PAD_No.Clear();
                for (int i = 0; i < _recipeData.ST.PAD_CellNumber; i++)
                {
                    Define.Pos_List p = new Define.Pos_List();
                    if (dt.Rows[_recipeData.ST.PAD_X_StartCellRow - 1 + i][_recipeData.ST.PAD_X_StartCellCol - 1].ToString() != "" &&
                        dt.Rows[_recipeData.ST.PAD_Y_StartCellRow - 1 + i][_recipeData.ST.PAD_Y_StartCellCol - 1].ToString() != "" &&
                        dt.Rows[_recipeData.ST.PAD_Q_StartCellRow - 1 + i][_recipeData.ST.PAD_Q_StartCellCol - 1].ToString() != "" &&
                        dt.Rows[_recipeData.ST.PAD_Type_StartCellRow - 1 + i][_recipeData.ST.PAD_Type_StartCellCol - 1].ToString() != "")
                    {
                        p.No = i + 1;
                        p.X = Convert.ToDouble(dt.Rows[_recipeData.ST.PAD_X_StartCellRow - 1 + i][_recipeData.ST.PAD_X_StartCellCol - 1]);
                        p.Y = Convert.ToDouble(dt.Rows[_recipeData.ST.PAD_Y_StartCellRow - 1 + i][_recipeData.ST.PAD_Y_StartCellCol - 1]);
                        p.Q = Convert.ToDouble(dt.Rows[_recipeData.ST.PAD_Q_StartCellRow - 1 + i][_recipeData.ST.PAD_Q_StartCellCol - 1]);
                        p.PadType = Convert.ToInt16(dt.Rows[_recipeData.ST.PAD_Type_StartCellRow - 1 + i][_recipeData.ST.PAD_Type_StartCellCol - 1]);
                        Pos.PAD_Pos.Add(p);

                        List_PAD_No.Add(i + 1);
                    }
                }

                MList_Log.Add("自動: 獲取座標 繪製UI");

                dGV_test_STPos.Columns.Clear();
                dGV_test_STPos.Rows.Clear();
                dGV_test_STPos.Columns.Add("定位點X", "定位點X");
                dGV_test_STPos.Columns.Add("定位點Y", "定位點Y");
                dGV_test_STPos.Columns.Add("測高點X", "測高點X");
                dGV_test_STPos.Columns.Add("測高點Y", "測高點Y");
                dGV_test_STPos.Columns.Add("PAD編號", "PAD編號");
                dGV_test_STPos.Columns.Add("PAD中心X", "PAD中心X");
                dGV_test_STPos.Columns.Add("PAD中心Y", "PAD中心Y");
                dGV_test_STPos.Columns.Add("PAD中心Q", "PAD中心Q");
                dGV_test_STPos.Columns.Add("PAD Type", "PAD Type");
                dGV_test_STPos.Columns["定位點X"].Width = 70;
                dGV_test_STPos.Columns["定位點Y"].Width = 70;
                dGV_test_STPos.Columns["測高點X"].Width = 70;
                dGV_test_STPos.Columns["測高點Y"].Width = 70;
                dGV_test_STPos.Columns["PAD編號"].Width = 40;
                dGV_test_STPos.Columns["PAD中心X"].Width = 70;
                dGV_test_STPos.Columns["PAD中心Y"].Width = 70;
                dGV_test_STPos.Columns["PAD中心Q"].Width = 70;
                dGV_test_STPos.Columns["PAD Type"].Width = 70;
                dGV_test_STPos.Columns["定位點X"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dGV_test_STPos.Columns["定位點Y"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dGV_test_STPos.Columns["測高點X"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dGV_test_STPos.Columns["測高點Y"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dGV_test_STPos.Columns["PAD編號"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dGV_test_STPos.Columns["PAD中心X"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dGV_test_STPos.Columns["PAD中心Y"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dGV_test_STPos.Columns["PAD中心Q"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dGV_test_STPos.Columns["PAD Type"].SortMode = DataGridViewColumnSortMode.NotSortable;

                int rowcount = Pos.PAD_Pos.Count > Pos.Mark_Pos.Count ? Pos.PAD_Pos.Count : Pos.Mark_Pos.Count;
                dGV_test_STPos.RowCount = rowcount;

                //HMI.Pos_PAD_List.Clear();
                //Define.Pos_Data _pos_data;

                for (int i = 0; i < rowcount; i++)
                {
                    dGV_test_STPos.Rows[i].HeaderCell.Value = (i + 1).ToString();

                    if (i < 4)
                    {
                        dGV_test_STPos.Rows[i].Cells["定位點X"].Value = Pos.Mark_Pos[i].X;
                        dGV_test_STPos.Rows[i].Cells["定位點Y"].Value = Pos.Mark_Pos[i].Y;
                        dGV_test_STPos.Rows[i].Cells["測高點X"].Value = Pos.Height_Pos[i].X;
                        dGV_test_STPos.Rows[i].Cells["測高點Y"].Value = Pos.Height_Pos[i].Y;
                    }
                    else
                    {
                        dGV_test_STPos.Rows[i].Cells["定位點X"].Value = "";
                        dGV_test_STPos.Rows[i].Cells["定位點Y"].Value = "";
                        dGV_test_STPos.Rows[i].Cells["測高點X"].Value = "";
                        dGV_test_STPos.Rows[i].Cells["測高點Y"].Value = "";
                    }
                    dGV_test_STPos.Rows[i].Cells["PAD編號"].Value = Pos.PAD_Pos[i].No;
                    dGV_test_STPos.Rows[i].Cells["PAD中心X"].Value = Pos.PAD_Pos[i].X;
                    dGV_test_STPos.Rows[i].Cells["PAD中心Y"].Value = Pos.PAD_Pos[i].Y;
                    dGV_test_STPos.Rows[i].Cells["PAD中心Q"].Value = Pos.PAD_Pos[i].Q;
                    dGV_test_STPos.Rows[i].Cells["PAD Type"].Value = Pos.PAD_Pos[i].PadType;

                }

                dGV_test_STPos.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToDisplayedHeaders;
                dGV_test_STPos.AutoResizeRowHeadersWidth(DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders);


            }
            catch (Exception ex)
            {

            }
        }

        private bool Get_STPos_Rotate()
        {
            bool result = false;
            try
            {
                if (Pos.PAD_Pos.Count == 0)
                {
                    result = false;
                }
                else
                {
                    //依據第一定位點進行旋轉
                    //ST定位點位置旋轉
                    MList_Log.Add("自動: 旋轉座標 定位點旋轉 旋轉基準點: " + Pos.Mark_Pos[0].X.ToString() + ", " + Pos.Mark_Pos[0].Y.ToString());
                    Pos.R_Mark_Pos.Clear();

                    if (_recipeData.ST.ST_RotateAngle == 0)
                    {
                        for (int i = 0; i < Pos.Mark_Pos.Count; i++)
                        {
                            Pos.R_Mark_Pos.Add(Pos.Mark_Pos[i]);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Pos.Mark_Pos.Count; i++)
                        {
                            Define.Pos_List r_p = new Define.Pos_List();

                            Cal_OriginRotateAngle(Pos.Mark_Pos[0], Pos.Mark_Pos[i], _recipeData.ST.ST_RotateAngle, ref r_p);

                            Pos.R_Mark_Pos.Add(r_p);
                        }
                    }
                    //ST測高點位置旋轉
                    MList_Log.Add("自動: 旋轉座標 測高點旋轉 旋轉基準點: " + Pos.Mark_Pos[0].X.ToString() + ", " + Pos.Mark_Pos[0].Y.ToString());
                    Pos.R_Height_Pos.Clear();

                    if (_recipeData.ST.ST_RotateAngle == 0)
                    {
                        for (int i = 0; i < Pos.Height_Pos.Count; i++)
                        {
                            Pos.R_Height_Pos.Add(Pos.Height_Pos[i]);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Pos.Height_Pos.Count; i++)
                        {
                            Define.Pos_List r_p = new Define.Pos_List();

                            Cal_OriginRotateAngle(Pos.Mark_Pos[0], Pos.Height_Pos[i], _recipeData.ST.ST_RotateAngle, ref r_p);

                            Pos.R_Height_Pos.Add(r_p);
                        }
                    }
                    //PAD位置旋轉
                    MList_Log.Add("自動: 旋轉座標 PAD中心點旋轉 旋轉基準點: " + Pos.Mark_Pos[0].X.ToString() + ", " + Pos.Mark_Pos[0].Y.ToString());
                    Pos.R_PAD_Pos.Clear();

                    if (_recipeData.ST.ST_RotateAngle == 0)
                    {
                        for (int i = 0; i < Pos.PAD_Pos.Count; i++)
                        {
                            Pos.R_PAD_Pos.Add(Pos.PAD_Pos[i]);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Pos.PAD_Pos.Count; i++)
                        {
                            Define.Pos_List r_p = new Define.Pos_List();

                            Cal_OriginRotateAngle(Pos.Mark_Pos[0], Pos.PAD_Pos[i], _recipeData.ST.ST_RotateAngle, ref r_p);

                            Pos.R_PAD_Pos.Add(r_p);
                        }
                    }

                    //輸出UI
                    MList_Log.Add("自動: 旋轉座標 繪製UI");
                    int rowcount = Pos.R_PAD_Pos.Count > Pos.R_Mark_Pos.Count ? Pos.R_PAD_Pos.Count : Pos.R_Mark_Pos.Count;

                    HMI.Pos_PAD_List.Clear();
                    Define.Pos_Data _pos_data;

                    for (int i = 0; i < rowcount; i++)
                    {
                        _pos_data = new Define.Pos_Data();
                        _pos_data = HMI.Pos_PAD.Copy();//複製速度、加減速度等資訊
                        _pos_data.X.Position = Pos.R_PAD_Pos[i].X;
                        _pos_data.Y.Position = Pos.R_PAD_Pos[i].Y;
                        _pos_data.Q.Position = Pos.R_PAD_Pos[i].Q;

                        HMI.Pos_PAD_List.Add(Pos.R_PAD_Pos[i].No, _pos_data);


                    }

                    result = true;
                }

                return result;
            }
            catch (Exception ex)
            {
                string log = String.Format("自動: 旋轉座標 異常: {0}", ex.Message);
                MList_Log.Add(log);
                Error_Log.Add(log);

                return result;
            }
        }


        private bool Cal_OriginRotateAngle(Define.Pos_List origin, Define.Pos_List point, double angle, ref Define.Pos_List repoint)
        {
            bool result = false;

            try
            {
                //徑度轉換
                double limit;
                int a;

                limit = 360;
                a = Convert.ToInt16(angle / limit);
                double d = angle - a * limit;
                double r = d * Math.PI / 180;

                double rx = ((point.X - origin.X) * Math.Cos(r)) - ((point.Y - origin.Y) * Math.Sin(r));// + origin.X;
                double ry = ((point.X - origin.X) * Math.Sin(r)) + ((point.Y - origin.Y) * Math.Cos(r));// + origin.Y;

                repoint.No = point.No;
                repoint.Num = point.Num;

                repoint.X = Math.Round(rx, 3);

                repoint.Y = Math.Round(ry, 3);

                result = true;

                return result;
            }
            catch (Exception ex)
            {
                return result;
            }
        }

        private bool Get_SBPos()
        {
            bool result = false;
            try
            {

                if (_recipeData.SB.PAD_Length == 0 ||
                    _recipeData.SB.SolderBall_Diameter == 0 ||
                    _recipeData.SB.SolderBall_Number == 0 ||
                    _recipeData.SB.SolderBall_Pitch == 0 ||
                    Pos.PAD_Pos.Count == 0)
                {
                    string log = String.Format("自動: 錫球座標 錫球座標計算異常: 參數為0");
                    MList_Log.Add(log);

                    return result;
                }
                else
                {
                    //計算錫球座標
                    string log = String.Format("自動: 錫球座標 錫球座標計算");
                    MList_Log.Add(log);
                    Pos.Solder_Pos.Clear();


                    List<Define.Pos_List> p_l = new List<Define.Pos_List>();

                    if (Cal_SBPos(Pos.R_PAD_Pos, ref Pos.Solder_Pos, 0))
                    {
                        log = String.Format("自動: 錫球座標 錫球座標計算完成");
                        MList_Log.Add(log);
                    }
                    else
                    {
                        result = false;
                        return result;
                    }


                    //UI顯示
                    log = String.Format("自動: 錫球座標 繪製UI");
                    MList_Log.Add(log);
                    dGV_SBPos.Columns.Clear();
                    dGV_SBPos.Rows.Clear();
                    dGV_SBPos.Columns.Add("PAD編號", "PAD編號");
                    dGV_SBPos.Columns.Add("錫球編號", "錫球編號");
                    dGV_SBPos.Columns.Add("錫球X", "錫球X");
                    dGV_SBPos.Columns.Add("錫球Y", "錫球Y");
                    dGV_SBPos.Columns["PAD編號"].Width = 40;
                    dGV_SBPos.Columns["錫球編號"].Width = 40;
                    dGV_SBPos.Columns["錫球X"].Width = 70;
                    dGV_SBPos.Columns["錫球Y"].Width = 70;
                    dGV_SBPos.Columns["PAD編號"].SortMode = DataGridViewColumnSortMode.NotSortable;
                    dGV_SBPos.Columns["錫球編號"].SortMode = DataGridViewColumnSortMode.NotSortable;
                    dGV_SBPos.Columns["錫球X"].SortMode = DataGridViewColumnSortMode.NotSortable;
                    dGV_SBPos.Columns["錫球Y"].SortMode = DataGridViewColumnSortMode.NotSortable;
                    //DataTable posdt = new DataTable();
                    //posdt.Columns.Add("錫球X", System.Type.GetType("System.String"));
                    //posdt.Columns.Add("錫球Y", System.Type.GetType("System.String"));

                    int rowcount = Pos.Solder_Pos.Count;
                    dGV_SBPos.RowCount = rowcount;

                    //HMI.Pos_SubPAD_List.Clear();
                    //Define.Pos_Data _pos_data;

                    for (int i = 0; i < rowcount; i++)
                    {
                        dGV_SBPos.Rows[i].HeaderCell.Value = (i + 1).ToString();
                        dGV_SBPos.Rows[i].Cells["PAD編號"].Value = Pos.Solder_Pos[i].No;
                        dGV_SBPos.Rows[i].Cells["錫球編號"].Value = Pos.Solder_Pos[i].Num;
                        dGV_SBPos.Rows[i].Cells["錫球X"].Value = Pos.Solder_Pos[i].X;
                        dGV_SBPos.Rows[i].Cells["錫球Y"].Value = Pos.Solder_Pos[i].Y;


                    }


                    dGV_SBPos.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToDisplayedHeaders;



                    dGV_SBPos.AutoResizeRowHeadersWidth(DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders);

                    result = true;
                }

                return result;
            }
            catch (Exception ex)
            {
                string log = String.Format("自動: 錫球座標 取得異常: {0}", ex.Message);
                MList_Log.Add(log);
                Error_Log.Add(log);

                return result;
            }
        }

        private bool Cal_SBPos(List<Define.Pos_List> point_list, ref List<Define.Pos_List> refpointlist, int sorttype)
        {
            bool result = false;
            try
            {
                int n = 0;

                List<Define.Pos_List> p_l = new List<Define.Pos_List>();

                //偶數計算
                if (_recipeData.SB.SolderBall_Number % 2 == 0)
                {
                    string log = String.Format("自動: 錫球座標 偶數計算 參數1.錫球間距 {0}, 2.錫球數量 {1}", _recipeData.SB.SolderBall_Pitch.ToString(), _recipeData.SB.SolderBall_Number.ToString());
                    MList_Log.Add(log);
                    do
                    {
                        double pitch = 0;
                        pitch = (_recipeData.SB.SolderBall_Pitch / 2) + (_recipeData.SB.SolderBall_Pitch * (n / 2));

                        if (sorttype == 0)
                        {
                            Define.Pos_List p1 = new Define.Pos_List();
                            p1.X = pitch;
                            p1.Y = 0;

                            p_l.Add(p1);

                            Define.Pos_List p2 = new Define.Pos_List();
                            p2.X = -pitch;
                            p2.Y = 0;

                            p_l.Add(p2);
                        }
                        else if (sorttype == 1)
                        {
                            Define.Pos_List p1 = new Define.Pos_List();
                            p1.X = 0;
                            p1.Y = pitch;

                            p_l.Add(p1);

                            Define.Pos_List p2 = new Define.Pos_List();
                            p2.X = 0;
                            p2.Y = -pitch;

                            p_l.Add(p2);
                        }
                        n = n + 2;

                    } while ((n) < _recipeData.SB.SolderBall_Number);
                }
                else //奇數計算
                {
                    string log = String.Format("自動: 錫球座標 奇數計算 參數1.錫球間距 {0}, 2.錫球數量 {1}", _recipeData.SB.SolderBall_Pitch.ToString(), _recipeData.SB.SolderBall_Number.ToString());
                    MList_Log.Add(log);

                    do
                    {
                        double pitch = 0;
                        //第一顆位置在中心
                        if (n == 0)
                        {
                            Define.Pos_List p = new Define.Pos_List();
                            if (sorttype == 0)
                            {
                                p.X = pitch;
                                p.Y = 0;
                            }
                            else if (sorttype == 1)
                            {
                                p.X = 0;
                                p.Y = pitch;
                            }

                            p_l.Add(p);

                            n++;
                        }
                        else
                        {
                            pitch = (_recipeData.SB.SolderBall_Pitch * (n/* - 1*/));

                            if (sorttype == 0)
                            {
                                Define.Pos_List p1 = new Define.Pos_List();
                                p1.X = pitch;
                                p1.Y = 0;

                                p_l.Add(p1);

                                Define.Pos_List p2 = new Define.Pos_List();
                                p2.X = -pitch;
                                p2.Y = 0;

                                p_l.Add(p2);
                            }
                            else if (sorttype == 1)
                            {
                                Define.Pos_List p1 = new Define.Pos_List();
                                p1.X = 0;
                                p1.Y = pitch;

                                p_l.Add(p1);

                                Define.Pos_List p2 = new Define.Pos_List();
                                p2.X = 0;
                                p2.Y = -pitch;

                                p_l.Add(p2);
                            }

                            n++;
                        }
                    } while ((n * 2) < _recipeData.SB.SolderBall_Number);
                }

                if (sorttype == 0)
                {
                    string log = String.Format("自動: 錫球座標 錫球排序X向");
                    MList_Log.Add(log);

                    //對X做排序
                    p_l.Sort((x, y) => x.X.CompareTo(y.X));
                }
                else if (sorttype == 1)
                {
                    string log = String.Format("自動: 錫球座標 錫球排序Y向");
                    MList_Log.Add(log);

                    //對Y做排序
                    p_l.Sort((x, y) => x.Y.CompareTo(y.Y));
                }
                for (int i = 0; i < point_list.Count; i++)
                {
                    for (int j = 0; j < p_l.Count; j++)
                    {
                        Define.Pos_List fp = new Define.Pos_List();
                        fp.No = point_list[i].No;
                        fp.Num = (j + 1);
                        fp.X = point_list[i].X + p_l[j].X;
                        fp.Y = point_list[i].Y + p_l[j].Y;

                        refpointlist.Add(fp);
                    }
                }

                result = true;

                return result;
            }
            catch (Exception ex)
            {
                string log = String.Format("自動: 錫球座標 計算異常: {0}", ex.Message);
                Error_Log.Add(log);

                return result;
            }
        }
        #endregion

        #region ST檔案儲存
        private void ST_InfoSave()
        {
            _recipeData.ST.MarkX_CellCol[0] = Convert.ToInt32(txt_STMark1X_Col.Text);
            _recipeData.ST.MarkX_CellRow[0] = Convert.ToInt32(txt_STMark1X_Row.Text);
            _recipeData.ST.MarkY_CellCol[0] = Convert.ToInt32(txt_STMark1Y_Col.Text);
            _recipeData.ST.MarkY_CellRow[0] = Convert.ToInt32(txt_STMark1Y_Row.Text);

            _recipeData.ST.MarkX_CellCol[1] = Convert.ToInt32(txt_STMark2X_Col.Text);
            _recipeData.ST.MarkX_CellRow[1] = Convert.ToInt32(txt_STMark2X_Row.Text);
            _recipeData.ST.MarkY_CellCol[1] = Convert.ToInt32(txt_STMark2Y_Col.Text);
            _recipeData.ST.MarkY_CellRow[1] = Convert.ToInt32(txt_STMark2Y_Row.Text);

            _recipeData.ST.MarkX_CellCol[2] = Convert.ToInt32(txt_STMark3X_Col.Text);
            _recipeData.ST.MarkX_CellRow[2] = Convert.ToInt32(txt_STMark3X_Row.Text);
            _recipeData.ST.MarkY_CellCol[2] = Convert.ToInt32(txt_STMark3Y_Col.Text);
            _recipeData.ST.MarkY_CellRow[2] = Convert.ToInt32(txt_STMark3Y_Row.Text);

            _recipeData.ST.MarkX_CellCol[3] = Convert.ToInt32(txt_STMark4X_Col.Text);
            _recipeData.ST.MarkX_CellRow[3] = Convert.ToInt32(txt_STMark4X_Row.Text);
            _recipeData.ST.MarkY_CellCol[3] = Convert.ToInt32(txt_STMark4Y_Col.Text);
            _recipeData.ST.MarkY_CellRow[3] = Convert.ToInt32(txt_STMark4Y_Row.Text);

            _recipeData.ST.HeightX_CellCol[0] = Convert.ToInt32(txt_STHeight1X_Col.Text);
            _recipeData.ST.HeightX_CellRow[0] = Convert.ToInt32(txt_STHeight1X_Row.Text);
            _recipeData.ST.HeightY_CellCol[0] = Convert.ToInt32(txt_STHeight1Y_Col.Text);
            _recipeData.ST.HeightY_CellRow[0] = Convert.ToInt32(txt_STHeight1Y_Row.Text);

            _recipeData.ST.HeightX_CellCol[1] = Convert.ToInt32(txt_STHeight2X_Col.Text);
            _recipeData.ST.HeightX_CellRow[1] = Convert.ToInt32(txt_STHeight2X_Row.Text);
            _recipeData.ST.HeightY_CellCol[1] = Convert.ToInt32(txt_STHeight2Y_Col.Text);
            _recipeData.ST.HeightY_CellRow[1] = Convert.ToInt32(txt_STHeight2Y_Row.Text);

            _recipeData.ST.HeightX_CellCol[2] = Convert.ToInt32(txt_STHeight3X_Col.Text);
            _recipeData.ST.HeightX_CellRow[2] = Convert.ToInt32(txt_STHeight3X_Row.Text);
            _recipeData.ST.HeightY_CellCol[2] = Convert.ToInt32(txt_STHeight3Y_Col.Text);
            _recipeData.ST.HeightY_CellRow[2] = Convert.ToInt32(txt_STHeight3Y_Row.Text);

            _recipeData.ST.HeightX_CellCol[3] = Convert.ToInt32(txt_STHeight4X_Col.Text);
            _recipeData.ST.HeightX_CellRow[3] = Convert.ToInt32(txt_STHeight4X_Row.Text);
            _recipeData.ST.HeightY_CellCol[3] = Convert.ToInt32(txt_STHeight4Y_Col.Text);
            _recipeData.ST.HeightY_CellRow[3] = Convert.ToInt32(txt_STHeight4Y_Row.Text);

            _recipeData.ST.PAD_X_StartCellCol = Convert.ToInt32(txt_PAD_X_StartCellCol.Text);
            _recipeData.ST.PAD_X_StartCellRow = Convert.ToInt32(txt_PAD_X_StartCellRow.Text);
            _recipeData.ST.PAD_Y_StartCellCol = Convert.ToInt32(txt_PAD_Y_StartCellCol.Text);
            _recipeData.ST.PAD_Y_StartCellRow = Convert.ToInt32(txt_PAD_Y_StartCellRow.Text);
            _recipeData.ST.PAD_Q_StartCellCol = Convert.ToInt32(txt_PAD_Q_StartCellCol.Text);
            _recipeData.ST.PAD_Q_StartCellRow = Convert.ToInt32(txt_PAD_Q_StartCellRow.Text);
            _recipeData.ST.PAD_Type_StartCellCol = Convert.ToInt32(txt_PADType_StartCellCol.Text);
            _recipeData.ST.PAD_Type_StartCellRow = Convert.ToInt32(txt_PADType_StartCellRow.Text);

            int type = cb_STPos_Rotate.SelectedIndex * 90;
            _recipeData.ST.ST_RotateAngle = type;

            _recipeData.ST.PAD_CellNumber = Convert.ToInt32(txt_PADPosNumber.Text);

            _recipeData.Save_Data(RecipeFileName);
        }

        private void btn_Save_STMark1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                ST_InfoSave();
                _recipeData.Save_Data(RecipeFileName);
                LogMsgAdd(MList_Log, lb_HistoryList, "執行ST座標檔儲存。", tmpListStr);
            }
        }
        #endregion

        #region SB檔案儲存
        private void btn_Save_SBData_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    LogMsgAdd(MList_Log, lb_HistoryList, "執行錫球參數儲存。", tmpListStr);
                    double pitch = Convert.ToDouble(txt_SBPitch.Text);
                    double oldpitch = _recipeData.SB.SolderBall_Pitch;
                    int num = Convert.ToInt32(txt_SBNumber.Text);
                    int oldnum = _recipeData.SB.SolderBall_Number;

                    _recipeData.SB.PAD_Length = Convert.ToDouble(txt_SBPADLength.Text);
                    _recipeData.SB.SolderBall_Diameter = Convert.ToDouble(txt_SBSize.Text);
                    _recipeData.SB.SolderBall_Pitch = pitch;
                    _recipeData.SB.SolderBall_Number = num;

                    _recipeData.SB.SB_MountMoveZ_Flag = cb_SB_MountMoveZ_Flag.Checked;
                    _recipeData.SB.SB_MountMoveZ = Math.Abs(Convert.ToDouble(txt_SB_MountMoveZ.Text));

                    _recipeData.Save_Data(RecipeFileName);

                    List_SubPAD_No.Clear();
                    for (int i = 0; i < num; i++)
                    {
                        List_SubPAD_No.Add(i + 1);
                    }

                    if (num != oldnum || pitch != oldpitch)
                    {
                        Get_SBPos();
                    }

                    for (int i = 0; i < _recipeData.Panel.ST_Num; i++)
                    {
                        ST_SelectPanel[i]._numBall.Maximum = num;
                    }

                    Num_MovePOS_Ball_NO.Maximum = num;
                    num_ST_Ball_No.Maximum = num;

                }
            }
            catch (Exception)
            {

            }

        }
        #endregion

        #region 雷射相關檔案儲存
        private void btn_Save_SBLaserData_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行雷射相關參數儲存。", tmpListStr);
                // signal
                _recipeData.Signal_SBLoad.ALaserPower = Convert.ToDouble(txt_Signal_ALaserPower.Text);
                _recipeData.Signal_SBLoad.ALaserTimes = Convert.ToDouble(txt_Signal_ALaserTimes.Text);
                _recipeData.Signal_SBLoad.BLaserPower = Convert.ToDouble(txt_Signal_BLaserPower.Text);
                _recipeData.Signal_SBLoad.BLaserTimes = Convert.ToDouble(txt_Signal_BLaserTimes.Text);
                _recipeData.Signal_SBLoad.LaserDelayTimes = Convert.ToDouble(txt_Signal_LaserDelayTimes.Text);
                _recipeData.Signal_SBLoad.BlowToLaser_DelayTimes = Convert.ToInt32(txt_Signal_BlowToLaser_DelayTimes.Text);
                _recipeData.Signal_SBLoad.LaserToCloseAir_DelayTimes = Convert.ToInt32(txt_Signal_LaserToCloseAir_DelayTimes.Text);
                // signal 2階
                _recipeData.Signal_SBLoad_2.ALaserPower = Convert.ToDouble(txt_Signal_ALaserPower2.Text);
                _recipeData.Signal_SBLoad_2.ALaserTimes = Convert.ToDouble(txt_Signal_ALaserTimes2.Text);
                _recipeData.Signal_SBLoad_2.BLaserPower = Convert.ToDouble(txt_Signal_BLaserPower2.Text);
                _recipeData.Signal_SBLoad_2.BLaserTimes = Convert.ToDouble(txt_Signal_BLaserTimes2.Text);
                _recipeData.Signal_SBLoad_2.LaserDelayTimes = Convert.ToDouble(txt_Signal_LaserDelayTimes2.Text);
                _recipeData.Signal_SBLoad_2.BlowToLaser_DelayTimes = Convert.ToInt32(txt_Signal_BlowToLaser_DelayTimes2.Text);
                _recipeData.Signal_SBLoad_2.LaserToCloseAir_DelayTimes = Convert.ToInt32(txt_Signal_LaserToCloseAir_DelayTimes2.Text);

                // power
                _recipeData.Power_SBLoad.ALaserPower = Convert.ToDouble(txt_Power_ALaserPower.Text);
                _recipeData.Power_SBLoad.ALaserTimes = Convert.ToDouble(txt_Power_ALaserTimes.Text);
                _recipeData.Power_SBLoad.BLaserPower = Convert.ToDouble(txt_Power_BLaserPower.Text);
                _recipeData.Power_SBLoad.BLaserTimes = Convert.ToDouble(txt_Power_BLaserTimes.Text);
                _recipeData.Power_SBLoad.LaserDelayTimes = Convert.ToDouble(txt_Power_LaserDelayTimes.Text);
                _recipeData.Power_SBLoad.BlowToLaser_DelayTimes = Convert.ToInt32(txt_Power_BlowToLaser_DelayTimes.Text);
                _recipeData.Power_SBLoad.LaserToCloseAir_DelayTimes = Convert.ToInt32(txt_Power_LaserToCloseAir_DelayTimes.Text);
                // power 2階
                _recipeData.Power_SBLoad_2.ALaserPower = Convert.ToDouble(txt_Power_ALaserPower2.Text);
                _recipeData.Power_SBLoad_2.ALaserTimes = Convert.ToDouble(txt_Power_ALaserTimes2.Text);
                _recipeData.Power_SBLoad_2.BLaserPower = Convert.ToDouble(txt_Power_BLaserPower2.Text);
                _recipeData.Power_SBLoad_2.BLaserTimes = Convert.ToDouble(txt_Power_BLaserTimes2.Text);
                _recipeData.Power_SBLoad_2.LaserDelayTimes = Convert.ToDouble(txt_Power_LaserDelayTimes2.Text);
                _recipeData.Power_SBLoad_2.BlowToLaser_DelayTimes = Convert.ToInt32(txt_Power_BlowToLaser_DelayTimes2.Text);
                _recipeData.Power_SBLoad_2.LaserToCloseAir_DelayTimes = Convert.ToInt32(txt_Power_LaserToCloseAir_DelayTimes2.Text);

                // Ground
                _recipeData.Ground_SBLoad.ALaserPower = Convert.ToDouble(txt_Ground_ALaserPower.Text);
                _recipeData.Ground_SBLoad.ALaserTimes = Convert.ToDouble(txt_Ground_ALaserTimes.Text);
                _recipeData.Ground_SBLoad.BLaserPower = Convert.ToDouble(txt_Ground_BLaserPower.Text);
                _recipeData.Ground_SBLoad.BLaserTimes = Convert.ToDouble(txt_Ground_BLaserTimes.Text);
                _recipeData.Ground_SBLoad.LaserDelayTimes = Convert.ToDouble(txt_Ground_LaserDelayTimes.Text);
                _recipeData.Ground_SBLoad.BlowToLaser_DelayTimes = Convert.ToInt32(txt_Ground_BlowToLaser_DelayTimes.Text);
                _recipeData.Ground_SBLoad.LaserToCloseAir_DelayTimes = Convert.ToInt32(txt_Ground_LaserToCloseAir_DelayTimes.Text);
                // Ground 2階
                _recipeData.Ground_SBLoad_2.ALaserPower = Convert.ToDouble(txt_Ground_ALaserPower2.Text);
                _recipeData.Ground_SBLoad_2.ALaserTimes = Convert.ToDouble(txt_Ground_ALaserTimes2.Text);
                _recipeData.Ground_SBLoad_2.BLaserPower = Convert.ToDouble(txt_Ground_BLaserPower2.Text);
                _recipeData.Ground_SBLoad_2.BLaserTimes = Convert.ToDouble(txt_Ground_BLaserTimes2.Text);
                _recipeData.Ground_SBLoad_2.LaserDelayTimes = Convert.ToDouble(txt_Ground_LaserDelayTimes2.Text);
                _recipeData.Ground_SBLoad_2.BlowToLaser_DelayTimes = Convert.ToInt32(txt_Ground_BlowToLaser_DelayTimes2.Text);
                _recipeData.Ground_SBLoad_2.LaserToCloseAir_DelayTimes = Convert.ToInt32(txt_Ground_LaserToCloseAir_DelayTimes2.Text);

                //IO 
                _recipeData.IO_SBLoad.ALaserPower = Convert.ToDouble(txt_IO_ALaserPower.Text);
                _recipeData.IO_SBLoad.ALaserTimes = Convert.ToDouble(txt_IO_ALaserTimes.Text);
                //NC
                _recipeData.NC_SBLoad.ALaserPower = Convert.ToDouble(txt_NC_ALaserPower.Text);
                _recipeData.NC_SBLoad.ALaserTimes = Convert.ToDouble(txt_NC_ALaserTimes.Text);

                _recipeData.Save_Data(RecipeFileName);
                Setting_UI();

                //MarkingMate雷射控制卡，讀存ezm專案檔
                LaserSettingSave();
            }
        }
        #endregion

        #region 機械位置檔案儲存
        private void btn_Save_MPos_Height_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _msPosData.M_H_VisionZ = Convert.ToDouble(txt_MPos_VisionZ.Text);
                _msPosData.M_H_HeightZ = Convert.ToDouble(txt_MPos_HeightZ.Text);
                _msPosData.M_H_LaserZ = Convert.ToDouble(txt_MPos_LaserZ.Text);
                _msPosData.M_H_LaserZ2 = Convert.ToDouble(txt_MPos_LaserZ2.Text);
                _msPosData.M_H_LaserZ2_Offset = Convert.ToDouble(txt_MPos_LaserZ2_Offset.Text);

                _msPosData.Save_Data();
                Setting_UI();
            }
        }

        private void btn_Save_MPos_LaserToHeight_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _msPosData.M_Laser2Height_X = Convert.ToDouble(txt_MPos_LaserToHeightX.Text);
                _msPosData.M_Laser2Height_Y = Convert.ToDouble(txt_MPos_LaserToHeightY.Text);

                _msPosData.Save_Data();
                Setting_UI();
            }
        }
        private void btn_Save_MPos_LaserToVision_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _msPosData.M_Laser2Vision_X = Convert.ToDouble(txt_MPos_LaserToVisionX.Text);
                _msPosData.M_Laser2Vision_Y = Convert.ToDouble(txt_MPos_LaserToVisionY.Text);

                _msPosData.Save_Data();
                Setting_UI();
            }
        }

        private double Check_UI_Object_Text(string _Value, double nimValue, double manValue)
        {
            double result = 10;
            try
            {
                double value = Convert.ToDouble(_Value);

                if (value < nimValue)
                {
                    value = nimValue;
                }
                if (value > manValue)
                {
                    value = manValue;
                }

                if (value >= nimValue && value <= manValue)
                {
                    result = value;
                }
            }
            catch
            {
                MessageBox.Show("數值不能為空白、符號、文字，僅能輸入0~9等浮點數或整數");
            }

            return result;
        }
        private int Check_UI_Object_Text(string _Value, int nimValue, int manValue)
        {
            int result = 500;
            try
            {
                double _value = Convert.ToDouble(_Value);
                int value = Convert.ToInt32(_value);

                if (value < nimValue)
                {
                    value = nimValue;
                }
                if (value > manValue)
                {
                    value = manValue;
                }

                if (value >= nimValue && value <= manValue)
                {
                    result = value;
                }
            }
            catch
            {
                MessageBox.Show("數值不能為空白、符號、文字，僅能輸入0~9等浮點數或整數");
            }

            return result;
        }

        #endregion

        #region 軸控相關
        private Boolean Axis_Connect()
        {
            try
            {
                this._Aerotech_Controller = Controller.Connect();
                // register task state and diagPackect arrived events
                //this._Aerotech_Controller.ControlCenter.Diagnostics.NewDiagPacketArrived += new EventHandler<NewDiagPacketArrivedEventArgs>(Diagnostics_NewDiagPacketArrived);
                this._Aerotech_Controller.Commands.Axes[0].Motion.Enable();
                this._Aerotech_Controller.Commands.Axes[1].Motion.Enable();
                this._Aerotech_Controller.Commands.Axes[2].Motion.Enable();


                _customDiagnostics = new CustomDiagnostics(_Aerotech_Controller);
                _customDiagnostics.Configuration.Axis.Add(AxisStatusSignal.PositionFeedback, "X");
                _customDiagnostics.Configuration.Axis.Add(AxisStatusSignal.PositionFeedback, "Y");
                _customDiagnostics.Configuration.Axis.Add(AxisStatusSignal.PositionFeedback, "Z");
                _customDiagnostics.Configuration.Axis.Add(AxisStatusSignal.PositionCommand, "X");
                _customDiagnostics.Configuration.Axis.Add(AxisStatusSignal.PositionCommand, "Y");
                _customDiagnostics.Configuration.Axis.Add(AxisStatusSignal.PositionCommand, "Z");
                _customDiagnostics.Configuration.Axis.Add(AxisStatusSignal.DriveStatus, "X");
                _customDiagnostics.Configuration.Axis.Add(AxisStatusSignal.DriveStatus, "Y");
                _customDiagnostics.Configuration.Axis.Add(AxisStatusSignal.DriveStatus, "Z");
                _customDiagnostics.Configuration.Axis.Add(AxisStatusSignal.AxisStatus, "X");
                _customDiagnostics.Configuration.Axis.Add(AxisStatusSignal.AxisStatus, "Y");
                _customDiagnostics.Configuration.Axis.Add(AxisStatusSignal.AxisStatus, "Z");
                _customDiagnostics.Configuration.Axis.Add(AxisStatusSignal.AxisFault, "X");
                _customDiagnostics.Configuration.Axis.Add(AxisStatusSignal.AxisFault, "Y");
                _customDiagnostics.Configuration.Axis.Add(AxisStatusSignal.AxisFault, "Z");

                return true;
            }
            catch (A3200Exception exception)
            {
                //LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                return false;
            }
        }
        private void Diagnostics_NewDiagPacketArrived(object sender, NewDiagPacketArrivedEventArgs e)
        {
            try
            {
                //URL: http://msdn.microsoft.com/en-us/library/ms171728.aspx
                //How to: Make Thread-Safe Calls to Windows Forms Controls               
                this.Invoke(new Action<NewDiagPacketArrivedEventArgs>(SetAxisState), e);
                //Console.WriteLine($"TIME{DateTime.Now.Millisecond}");

            }
            catch
            {
            }
        }
        private void SetAxisState(NewDiagPacketArrivedEventArgs e)
        {
            //參數
            HOME_X = e.Data[0].AxisStatus.Homed;
            HOME_Y = e.Data[1].AxisStatus.Homed;
            HOME_Z = e.Data[2].AxisStatus.Homed;
            En_X = e.Data[0].DriveStatus.Enabled;
            En_Y = e.Data[1].DriveStatus.Enabled;
            En_Z = e.Data[2].DriveStatus.Enabled;
            Motion_X = e.Data[0].DriveStatus.InPosition;
            Motion_Y = e.Data[1].DriveStatus.InPosition;
            Motion_Z = e.Data[2].DriveStatus.InPosition;
            Limit_XP = e.Data[0].AxisFault.CcwSoftwareLimitFault;
            Limit_YP = e.Data[1].AxisFault.CcwSoftwareLimitFault;
            Limit_ZN = e.Data[2].AxisFault.CcwSoftwareLimitFault;
            Limit_XN = e.Data[0].AxisFault.CwSoftwareLimitFault;
            Limit_YN = e.Data[1].AxisFault.CwSoftwareLimitFault;
            Limit_ZP = e.Data[2].AxisFault.CwSoftwareLimitFault;

            //極限
            if (Limit_XN)
            {
                ptb_Motion_HStatus_X.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisLimitN];
            }
            else if (Limit_XP)
            {
                ptb_Motion_HStatus_X.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisLimitP];
            }
            else if (HOME_X)
            {
                ptb_Motion_HStatus_X.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisHome];
            }
            else
            {
                ptb_Motion_HStatus_X.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisNone];
            }


            if (Limit_YN)
            {
                ptb_Motion_HStatus_Y.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisLimitN];
            }
            else if (Limit_YP)
            {
                ptb_Motion_HStatus_Y.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisLimitP];
            }
            else if (HOME_Y)
            {
                ptb_Motion_HStatus_Y.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisHome];
            }
            else
            {
                ptb_Motion_HStatus_Y.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisNone];
            }

            if (Limit_ZN)
            {
                ptb_Motion_HStatus_Z.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisLimitN];
            }
            else if (Limit_ZP)
            {
                ptb_Motion_HStatus_Z.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisLimitP];
            }
            else if (HOME_Z)
            {
                ptb_Motion_HStatus_Z.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisHome];
            }
            else
            {
                ptb_Motion_HStatus_Z.BackgroundImage = imageList_70_16.Images[(int)Define.UIImage_List.AxisNone];
            }

            //電機狀態
            if (!Motion_X)
            {
                btn_Motion_MotorStatus_X.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusOFF];
                lbl_Motion_Status_X.Text = "移動";
            }
            else
            {
                btn_Motion_MotorStatus_X.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusON];
                lbl_Motion_Status_X.Text = "待機";
            }

            if (!Motion_Y)
            {
                btn_Motion_MotorStatus_Y.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusOFF];
                lbl_Motion_Status_Y.Text = "移動";
            }
            else
            {
                btn_Motion_MotorStatus_Y.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusON];
                lbl_Motion_Status_Y.Text = "待機";
            }

            if (!Motion_Z)
            {
                btn_Motion_MotorStatus_Z.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusOFF];
                lbl_Motion_Status_Z.Text = "移動";
            }
            else
            {
                btn_Motion_MotorStatus_Z.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.StatusON];
                lbl_Motion_Status_Z.Text = "待機";
            }

            //Enable
            if (En_X)
            {
                chk_Motion_Enable_X.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.ON];
                chk_Motion_Enable_X.Checked = true;
            }
            else
            {
                chk_Motion_Enable_X.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.OFF];
                chk_Motion_Enable_X.Checked = false;
            }

            if (En_Y)
            {
                chk_Motion_Enable_Y.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.ON];
                chk_Motion_Enable_Y.Checked = true;
            }
            else
            {
                chk_Motion_Enable_Z.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.OFF];
                chk_Motion_Enable_Z.Checked = false;
            }

            if (En_Z)
            {
                chk_Motion_Enable_Z.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.ON];
                chk_Motion_Enable_Z.Checked = true;
            }
            else
            {
                chk_Motion_Enable_Z.BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.OFF];
                chk_Motion_Enable_Z.Checked = false;
            }

            if (En_X && En_Y && En_Z)
            {
                Aerotech_En = true;
            }
            else
            {
                Aerotech_En = false;
            }

            if (HOME_X && HOME_Y && HOME_Z)
            {
                Aerotech_Home = true;
            }
            else
            {
                Aerotech_Home = false;
            }

            //Position
            Now_X = e.Data[0].PositionFeedback;
            Now_Y = e.Data[1].PositionFeedback;
            Now_Z = e.Data[2].PositionFeedback;
            txt_Motion_Position_X.Text = e.Data[0].PositionFeedback.ToString("F3");
            txt_Motion_Position_Y.Text = e.Data[1].PositionFeedback.ToString("F3");
            txt_Motion_Position_Z.Text = e.Data[2].PositionFeedback.ToString("F3");
            txt_Motion_CmdPosition_X.Text = e.Data[0].PositionCommand.ToString("F3");
            txt_Motion_CmdPosition_Y.Text = e.Data[1].PositionCommand.ToString("F3");
            txt_Motion_CmdPosition_Z.Text = e.Data[2].PositionCommand.ToString("F3");
        }

        public void Aerotech_Test()
        {
            double status = _Aerotech_Controller.Commands.Status.TaskStatus(TaskId.T01, TaskStatusSignal.ExecutionMode);
            _Aerotech_Controller.Tasks[TaskId.T01].Callbacks.Custom[0].CallbackOccurred += new EventHandler<Aerotech.A3200.Callbacks.CallbackOccurredEventArgs>(NewCustomCallback);
        }

        private static void NewCustomCallback(object sender, CallbackOccurredEventArgs e)
        {
            // Display a few items concerning the callback
            Console.WriteLine("Task ID: " + e.TaskId);
            Console.WriteLine("Callback number: " + e.CallbackNumber);
            // Set the return value of the callback
            e.ReturnValue = 42.0;
        }


        /// <summary>
        /// X軸位置檢查[條件：等待移動完成(必) + 位置匹配(選) + 軸待機(必)]
        /// </summary>
        /// <param name="abs_position">目的絕對位置</param>
        /// <param name="check_position">是否匹配位置正確性</param>
        /// <returns>回饋是否在定點位置上</returns>
        public bool Check_X(double abs_position, bool check_position)
        {
            bool result = false;

            if (this._Aerotech_Controller != null)
            {
                //現在位置 - 目的位置 誤差值小於0.001 且 伺服軸確定到位
                //在此不能使用WaitForMotionDone，否則會導致伺服驅動卡住有問題
                if (//this._Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                    (!check_position || (Math.Abs(Now_X - abs_position/*Math.Abs(Now_X) - Math.Abs(abs_position)*/) < 0.001)) &&
                    Motion_X)
                {
                    result = true;
                }
            }

            return result;
        }
        /// <summary>
        /// Y軸位置檢查[條件：等待移動完成(必) + 位置匹配(選) + 軸待機(必)]
        /// </summary>
        /// <param name="abs_position">目的絕對位置</param>
        /// <param name="check_position">是否匹配位置正確性</param>
        /// <returns>回饋是否在定點位置上</returns>
        public bool Check_Y(double abs_position, bool check_position)
        {
            bool result = false;

            if (this._Aerotech_Controller != null)
            {
                //現在位置 - 目的位置 誤差值小於0.001 且 伺服軸確定到位
                //在此不能使用WaitForMotionDone，否則會導致伺服驅動卡住有問題
                if (//this._Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                (!check_position || (Math.Abs(Now_Y - abs_position/*Math.Abs(Now_Y) - Math.Abs(abs_position)*/) < 0.001)) &&
                Motion_Y)
                {
                    result = true;
                }
            }

            return result;
        }
        /// <summary>
        /// Z軸位置檢查[條件：等待移動完成(必) + 位置匹配(選) + 軸待機(必)]
        /// </summary>
        /// <param name="abs_position">目的絕對位置</param>
        /// <param name="check_position">是否匹配位置正確性</param>
        /// <returns>回饋是否在定點位置上</returns>
        public bool Check_Z(double abs_position, bool check_position)
        {
            bool result = false;

            if (this._Aerotech_Controller != null)
            {
                //現在位置 - 目的位置 誤差值小於0.001 且 伺服軸確定到位
                //在此不能使用WaitForMotionDone，否則會導致伺服驅動卡住有問題
                if (//this._Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000) &&
                (!check_position || (Math.Abs(Now_Z - abs_position/*Math.Abs(Now_Z) - Math.Abs(abs_position)*/) < 0.001)) &&
                Motion_Z)
                {
                    result = true;
                }
            }

            return result;
        }
        /// <summary>
        /// X軸相對距離移動
        /// </summary>
        /// <param name="distance">[in]距離</param>
        /// <param name="speed">[in]速度</param>
        public void MoveInc_X(double distance, double speed)
        {
            if (this._Aerotech_Controller != null && Motion_X)
            {
                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(0, distance, speed);
            }
        }
        /// <summary>
        /// Y軸相對距離移動
        /// </summary>
        /// <param name="distance">[in]距離</param>
        /// <param name="speed">[in]速度</param>
        public void MoveInc_Y(double distance, double speed)
        {
            if (this._Aerotech_Controller != null && Motion_Y)
            {
                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(1, distance, speed);
            }
        }
        /// <summary>
        /// Z軸相對距離移動
        /// </summary>
        /// <param name="distance">[in]距離</param>
        /// <param name="speed">[in]速度</param>
        public void MoveInc_Z(double distance, double speed)
        {
            if (this._Aerotech_Controller != null && Motion_Z)
            {
                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(2, distance, speed);
            }
        }
        /// <summary>
        /// X軸絕對位置移動
        /// </summary>
        /// <param name="position">[in]位置</param>
        /// <param name="speed">[in]速度</param>
        public void MoveAbs_X(double position, double speed)
        {
            if (this._Aerotech_Controller != null && Motion_X)
            {
                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(0, position, speed);
            }
        }
        /// <summary>
        /// Y軸絕對位置移動
        /// </summary>
        /// <param name="position">[in]位置</param>
        /// <param name="speed">[in]速度</param>
        public void MoveAbs_Y(double position, double speed)
        {
            if (this._Aerotech_Controller != null && Motion_Y)
            {
                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(1, position, speed);
            }
        }
        /// <summary>
        /// Z軸絕對位置移動
        /// </summary>
        /// <param name="position">[in]位置</param>
        /// <param name="speed">[in]速度</param>
        public void MoveAbs_Z(double position, double speed)
        {
            if (this._Aerotech_Controller != null && Motion_Z)
            {
                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(2, position, speed);
            }
        }


        private void PCIE_1203_SetParam(double speed, double acc, double dec)
        {
            UInt32 Result;
            double AxVelLow;
            double AxVelHigh;
            double AxAcc;
            double AxDec;
            double AxJerk;
            string strTemp;
            AxVelLow = speed / 20;
            //Set low velocity (start velocity) of this axis (Unit: PPU/S).
            //This property value must be smaller than or equal to PAR_AxVelHigh
            //You can also use the old API:Motion.mAcm_SetProperty(m_Axishand[CmbAxes.SelectedIndex], (uint)PropertyID.PAR_AxVelLow, ref AxVelLow, BufferLength);
            // UInt32  BufferLength;
            //BufferLength =8; buffer size for the property
            Result = Motion.mAcm_SetF64Property(m_Axishand[0], (uint)PropertyID.PAR_AxVelLow, AxVelLow);
            if (Result != (uint)ErrorCode.SUCCESS)
            {
                strTemp = "Set low velocity failed with error code: [0x" + Convert.ToString(Result, 16) + "]";
                ShowMessages(strTemp, Result);
                return;
            }
            AxVelHigh = speed;
            // Set high velocity (driving velocity) of this axis (Unit: PPU/s).
            //This property value must be smaller than CFG_AxMaxVel and greater than PAR_AxVelLow
            //You can also use the old API:Motion.mAcm_SetProperty(m_Axishand[CmbAxes.SelectedIndex], (uint)PropertyID.PAR_AxVelHigh,ref AxVelHigh,BufferLength)
            // UInt32  BufferLength;
            //BufferLength =8; buffer size for the property
            Result = Motion.mAcm_SetF64Property(m_Axishand[0], (uint)PropertyID.PAR_AxVelHigh, AxVelHigh);
            if (Result != (uint)ErrorCode.SUCCESS)
            {
                strTemp = "Set high velocity failed with error code: [0x" + Convert.ToString(Result, 16) + "]";
                ShowMessages(strTemp, Result);
                return;
            }
            AxAcc = acc;
            // Set acceleration of this axis (Unit: PPU/s2).
            //This property value must be smaller than or equal to CFG_AxMaxAcc
            //You can also use the old API:Motion.mAcm_SetProperty(m_Axishand[CmbAxes.SelectedIndex], (uint)PropertyID.PAR_AxAcc,ref AxAcc,BufferLength)
            // UInt32  BufferLength;
            //BufferLength =8; buffer size for the property
            Result = Motion.mAcm_SetF64Property(m_Axishand[0], (uint)PropertyID.PAR_AxAcc, AxAcc);
            if (Result != (uint)ErrorCode.SUCCESS)
            {
                strTemp = "Set acceleration failed with error code: [0x" + Convert.ToString(Result, 16) + "]";
                ShowMessages(strTemp, Result);
                return;
            }
            AxDec = dec;
            //Set deceleration of this axis (Unit: PPU/s2).
            //This property value must be smaller than or equal to CFG_AxMaxDec
            //You can also use the old API:Motion.mAcm_SetProperty(m_Axishand[CmbAxes.SelectedIndex], (uint)PropertyID.PAR_AxDcc,ref AxDec,BufferLength)
            // UInt32  BufferLength;
            //BufferLength =8; buffer size for the property
            Result = Motion.mAcm_SetF64Property(m_Axishand[0], (uint)PropertyID.PAR_AxDec, AxDec);
            if (Result != (uint)ErrorCode.SUCCESS)
            {
                strTemp = "Set deceleration failed with error code: [0x" + Convert.ToString(Result, 16) + "]";
                ShowMessages(strTemp, Result);
                return;
            }
            //if (rdb_T.Checked)
            //{
            //    AxJerk = 0;
            //}
            //else
            //{
            //    AxJerk = 1;
            //}
            ////Set the type of velocity profile: t-curve or s-curve
            ////You can also use the old API:Motion.mAcm_SetProperty(m_Axishand[CmbAxes.SelectedIndex], (uint)PropertyID.PAR_AxJerk,ref AxJerk,BufferLength)
            //// UInt32  BufferLength;
            ////BufferLength =8; buffer size for the property
            //Result = Motion.mAcm_SetF64Property(m_Axishand[CmbAxes.SelectedIndex], (uint)PropertyID.PAR_AxJerk, AxJerk);
            //if (Result != (uint)ErrorCode.SUCCESS)
            //{
            //    strTemp = "Set the type of velocity profile failed with error code: [0x" + Convert.ToString(Result, 16) + "]";
            //    ShowMessages(strTemp, Result);
            //    return;
            //}
            //GetAxisVelParam(); //Get Axis Velocity Param
        }

        /// <summary>
        /// Q軸絕對位置移動[有位置匹配(必)]
        /// </summary>
        /// <param name="position">[in]位置</param>
        /// <param name="speed">[in]速度</param>
        /// <param name="acceleration">[in]加速度</param>
        /// <param name="deceleration">[in]減速度</param>
        /// <returns>回饋是否在定點位置上</returns>
        public bool MoveAbs_Q(double position, double speed, double acceleration, double deceleration)
        {
            if (_orientalConnected)
            {
                int pos = Convert.ToInt32(position * 1000);
                int sp = Convert.ToInt32(speed * 1000);
                uint acc = Convert.ToUInt32(acceleration * 1000);
                uint dec = Convert.ToUInt32(deceleration * 1000);

                //return this._AZD_Controller.MoveAbs(pos, sp, acc, dec);
                PCIE_1203_SetParam(sp, acc, dec);
                //To command axis to make a never ending movement with a specified velocity.1: Negative direction.
                _returnCode = Motion.mAcm_AxMoveAbs(m_Axishand[0], pos);
                if (_returnCode == (uint)ErrorCode.SUCCESS)
                {
                    return true;

                }
                else
                {
                    strTemp = "Move Failed With Error Code[0x" + Convert.ToString(_returnCode, 16) + "]";
                    ShowMessages(strTemp, _returnCode);
                    return false;
                }

            }
            else
            {
                return false;
            }
        }

        private void chk_Motion_Enable_X_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (chk_Motion_Enable_X.Checked)
                {
                    this._Aerotech_Controller.Commands.Axes[0].Motion.Enable();
                }
                else
                {
                    this._Aerotech_Controller.Commands.Axes[0].Motion.Disable();
                }
            }
            catch (A3200Exception exception)
            {
                LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
            }
        }

        private void chk_Motion_Enable_Y_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (chk_Motion_Enable_Y.Checked)
                {
                    this._Aerotech_Controller.Commands.Axes[1].Motion.Enable();
                }
                else
                {
                    this._Aerotech_Controller.Commands.Axes[1].Motion.Disable();
                }
            }
            catch (A3200Exception exception)
            {
                LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
            }
        }

        private void chk_Motion_Enable_Z_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (chk_Motion_Enable_Z.Checked)
                {
                    this._Aerotech_Controller.Commands.Axes[2].Motion.Enable();
                }
                else
                {
                    this._Aerotech_Controller.Commands.Axes[2].Motion.Disable();
                }
            }
            catch (A3200Exception exception)
            {
                LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
            }
        }

        private void cmb_Motion_Speed_X_SelectedIndexChanged(object sender, EventArgs e)
        {
            Speed_X = double.Parse(cmb_Motion_Speed_X.Text);
        }

        private void cmb_Motion_Speed_Y_SelectedIndexChanged(object sender, EventArgs e)
        {
            Speed_Y = double.Parse(cmb_Motion_Speed_Y.Text);
        }

        private void cmb_Motion_Speed_Z_SelectedIndexChanged(object sender, EventArgs e)
        {
            Speed_Z = double.Parse(cmb_Motion_Speed_Z.Text);
        }

        private void cmb_Motion_Speed_Q_SelectedIndexChanged(object sender, EventArgs e)
        {
            Speed_Q = double.Parse(cmb_Motion_Speed_Q.Text);
        }

        #region Thread_motion
        Thread Axis_thMotionRun;
        public enum Motion_Num : int
        {
            Home = 0,
            Home_X = 1,
            Home_Y = 2,
            Home_Z = 3,
            Home_Q = 4,
            Rapid = 5
        }

        private void Axis_thMotionRun_Tick(Motion_Num motion_num)
        {
            if (Thread_Enabled)
            {
                Thread_Enabled = false;
                Axis_thMotionRun = new Thread(() =>
                {
                    try
                    {
                        switch (motion_num)
                        {
                            case Motion_Num.Home_X:
                                try
                                {
                                    this._Aerotech_Controller.Commands.Axes[0].Motion.Home();
                                }
                                catch (A3200Exception exception)
                                {
                                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                                }
                                Thread_Enabled = true;
                                Axis_thMotionRun.Abort();
                                break;
                            case Motion_Num.Home_Y:
                                try
                                {
                                    this._Aerotech_Controller.Commands.Axes[1].Motion.Home();
                                }
                                catch (A3200Exception exception)
                                {
                                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                                }
                                Thread_Enabled = true;
                                Axis_thMotionRun.Abort();
                                break;
                            case Motion_Num.Home_Z:
                                try
                                {
                                    this._Aerotech_Controller.Commands.Axes[2].Motion.Home();
                                }
                                catch (A3200Exception exception)
                                {
                                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                                }
                                Thread_Enabled = true;
                                Axis_thMotionRun.Abort();
                                break;
                            case Motion_Num.Home_Q:
                                try
                                {
                                    //if (_AZD_Controller.CheckPortOpen())
                                    //{
                                    //    _AZD_Controller.Homing();
                                    //}
                                }
                                catch (Exception exception)
                                {
                                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                                }
                                Thread_Enabled = true;
                                Axis_thMotionRun.Abort();
                                break;
                            case Motion_Num.Rapid:
                                try
                                {
                                    this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(0, Distance_X, 20);
                                    this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(1, Distance_Y, 20);
                                }
                                catch (A3200Exception exception)
                                {
                                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                                }
                                Thread_Enabled = true;
                                Axis_thMotionRun.Abort();

                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        // MessageBox.Show("動作完畢");
                        //MessageBox.Show(ex.ToString());
                        //LogMsgAdd(Error_Log, lb_ErrorList, ex.ToString(), tmpErrStr);
                    }
                });
                Axis_thMotionRun.IsBackground = true;
                Axis_thMotionRun.Priority = ThreadPriority.Normal;
                Axis_thMotionRun.Start();
            }

        }
        #endregion thread_motion


        private void btn_Motion_Homing_X_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行 X軸原點復歸?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Axis_thMotionRun_Tick(Motion_Num.Home_X);
            }
        }

        private void btn_Motion_Homing_Y_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行 Y軸原點復歸?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Axis_thMotionRun_Tick(Motion_Num.Home_Y);
            }
        }

        private void btn_Motion_Homing_Z_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行 Z軸原點復歸?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Axis_thMotionRun_Tick(Motion_Num.Home_Z);
            }
        }

        private void btn_Motion_MoveP_X_Click(object sender, EventArgs e)
        {
            if (rdo_Motion_Jog.Checked == false)
            {
                if (rdo_Motion_MoveRel.Checked == true)
                {
                    RelDistCheck(txt_Motion_Distance_X);
                    try
                    {
                        this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(0, Convert.ToDouble(txt_Motion_Distance_X.Text), Speed_X);

                    }
                    catch (A3200Exception exception)
                    {

                        LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                    }
                }
                else
                {
                    if (rdo_Motion_MoveAbs.Checked == true)
                    {
                        try
                        {
                            if (AbsDistCheck(txt_Motion_Distance_X, 1))
                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(0, Convert.ToDouble(txt_Motion_Distance_X.Text), Speed_X);
                            else
                                MessageBox.Show("超出軟體極限!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        }
                        catch (A3200Exception exception)
                        {
                            LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                        }
                    }
                }
            }
        }

        private void btn_Motion_MoveN_X_Click(object sender, EventArgs e)
        {
            if (rdo_Motion_Jog.Checked == false)
            {
                if (rdo_Motion_MoveRel.Checked == true)
                {
                    RelDistCheck(txt_Motion_Distance_X);
                    try
                    {
                        this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(0, -Convert.ToDouble(txt_Motion_Distance_X.Text), Speed_X);

                    }
                    catch (A3200Exception exception)
                    {
                        LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                    }
                }
                else
                {
                    if (rdo_Motion_MoveAbs.Checked == true)
                    {
                        try
                        {
                            if (AbsDistCheck(txt_Motion_Distance_X, 1))
                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(0, Convert.ToDouble(txt_Motion_Distance_X.Text), Speed_X);
                            else
                                MessageBox.Show("超出軟體極限!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        catch (A3200Exception exception)
                        {
                            LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                        }
                    }
                }
            }
        }

        private void btn_Motion_MoveP_Y_Click(object sender, EventArgs e)
        {
            if (rdo_Motion_Jog.Checked == false)
            {
                if (rdo_Motion_MoveRel.Checked == true)
                {
                    RelDistCheck(txt_Motion_Distance_Y);
                    try
                    {
                        this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(1, Convert.ToDouble(txt_Motion_Distance_Y.Text), Speed_Y);

                    }
                    catch (A3200Exception exception)
                    {
                        LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                    }
                }
                else
                {
                    if (rdo_Motion_MoveAbs.Checked == true)
                    {
                        try
                        {
                            if (AbsDistCheck(txt_Motion_Distance_Y, 2))
                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(1, Convert.ToDouble(txt_Motion_Distance_Y.Text), Speed_Y);
                            else
                                MessageBox.Show("超出軟體極限!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        catch (A3200Exception exception)
                        {
                            LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                        }
                    }
                }
            }
        }


        private void btn_Motion_MoveN_Y_Click(object sender, EventArgs e)
        {
            if (rdo_Motion_Jog.Checked == false)
            {
                if (rdo_Motion_MoveRel.Checked == true)
                {
                    RelDistCheck(txt_Motion_Distance_Y);
                    try
                    {
                        this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(1, -Convert.ToDouble(txt_Motion_Distance_Y.Text), Speed_Y);

                    }
                    catch (A3200Exception exception)
                    {
                        LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                    }
                }
                else
                {
                    if (rdo_Motion_MoveAbs.Checked == true)
                    {
                        try
                        {
                            if (AbsDistCheck(txt_Motion_Distance_Y, 2))
                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(1, Convert.ToDouble(txt_Motion_Distance_Y.Text), Speed_Y);
                            else
                                MessageBox.Show("超出軟體極限!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        catch (A3200Exception exception)
                        {
                            LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                        }
                    }
                }
            }
        }

        private void btn_Motion_MoveP_Z_Click(object sender, EventArgs e)
        {
            if (rdo_Motion_Jog.Checked == false)
            {
                if (rdo_Motion_MoveRel.Checked == true)
                {
                    RelDistCheck(txt_Motion_Distance_Z);
                    try
                    {
                        this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(2, Convert.ToDouble(txt_Motion_Distance_Z.Text), Speed_Z);

                    }
                    catch (A3200Exception exception)
                    {
                        LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                    }
                }
                else
                {
                    if (rdo_Motion_MoveAbs.Checked == true)
                    {
                        try
                        {
                            if (AbsDistCheck(txt_Motion_Distance_Z, 3))
                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(2, Convert.ToDouble(txt_Motion_Distance_Z.Text), Speed_Z);
                            else
                                MessageBox.Show("超出軟體極限!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        catch (A3200Exception exception)
                        {
                            LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                        }
                    }
                }
            }
        }

        private void btn_Motion_MoveN_Z_Click(object sender, EventArgs e)
        {
            if (rdo_Motion_Jog.Checked == false)
            {
                if (rdo_Motion_MoveRel.Checked == true)
                {
                    RelDistCheck(txt_Motion_Distance_Z);
                    try
                    {
                        this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(2, -Convert.ToDouble(txt_Motion_Distance_Z.Text), Speed_Z);

                    }
                    catch (A3200Exception exception)
                    {
                        LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                    }
                }
                else
                {
                    if (rdo_Motion_MoveAbs.Checked == true)
                    {
                        try
                        {
                            if (AbsDistCheck(txt_Motion_Distance_Z, 3))
                                this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(2, Convert.ToDouble(txt_Motion_Distance_Z.Text), Speed_Z);
                            else
                                MessageBox.Show("超出軟體極限!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        catch (A3200Exception exception)
                        {
                            LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                        }
                    }
                }
            }
        }


        private void btn_Motion_MoveP_X_MouseDown(object sender, MouseEventArgs e)
        {
            if (rdo_Motion_Jog.Checked == true)
            {
                try
                {
                    SafeJOGSpeedSetting();
                    this._Aerotech_Controller.Commands[this.taskIndex].Motion.FreeRun(0, Speed_X);

                }
                catch (A3200Exception exception)
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                }
            }
        }

        private void btn_Motion_MoveP_X_MouseUp(object sender, MouseEventArgs e)
        {
            if (rdo_Motion_Jog.Checked == true)
            {
                try
                {
                    this._Aerotech_Controller.Commands[this.taskIndex].Motion.FreeRunStop(0);
                }
                catch (A3200Exception exception)
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                }
            }
        }

        private void btn_Motion_MoveN_X_MouseDown(object sender, MouseEventArgs e)
        {
            if (rdo_Motion_Jog.Checked == true)
            {
                try
                {
                    SafeJOGSpeedSetting();
                    this._Aerotech_Controller.Commands[this.taskIndex].Motion.FreeRun(0, -Speed_X);

                }
                catch (A3200Exception exception)
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                }
            }
        }

        private void btn_Motion_MoveN_X_MouseUp(object sender, MouseEventArgs e)
        {
            if (rdo_Motion_Jog.Checked == true)
            {
                try
                {
                    this._Aerotech_Controller.Commands[this.taskIndex].Motion.FreeRunStop(0);
                }
                catch (A3200Exception exception)
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                }
            }
        }

        private void btn_Motion_MoveP_Y_MouseDown(object sender, MouseEventArgs e)
        {
            if (rdo_Motion_Jog.Checked == true)
            {
                try
                {
                    SafeJOGSpeedSetting();
                    this._Aerotech_Controller.Commands[this.taskIndex].Motion.FreeRun(1, Speed_Y);

                }
                catch (A3200Exception exception)
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                }
            }
        }

        private void btn_Motion_MoveP_Y_MouseUp(object sender, MouseEventArgs e)
        {
            if (rdo_Motion_Jog.Checked == true)
            {
                try
                {
                    this._Aerotech_Controller.Commands[this.taskIndex].Motion.FreeRunStop(1);
                }
                catch (A3200Exception exception)
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                }
            }
        }

        private void btn_Motion_MoveN_Y_MouseDown(object sender, MouseEventArgs e)
        {
            if (rdo_Motion_Jog.Checked == true)
            {
                try
                {
                    SafeJOGSpeedSetting();
                    this._Aerotech_Controller.Commands[this.taskIndex].Motion.FreeRun(1, -Speed_Y);

                }
                catch (A3200Exception exception)
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                }
            }
        }


        private void btn_Motion_MoveN_Y_MouseUp(object sender, MouseEventArgs e)
        {
            if (rdo_Motion_Jog.Checked == true)
            {
                try
                {
                    this._Aerotech_Controller.Commands[this.taskIndex].Motion.FreeRunStop(1);
                }
                catch (A3200Exception exception)
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                }
            }
        }

        private void btn_Motion_MoveP_Z_MouseDown(object sender, MouseEventArgs e)
        {
            if (rdo_Motion_Jog.Checked == true)
            {
                try
                {
                    SafeJOGSpeedSetting();
                    this._Aerotech_Controller.Commands[this.taskIndex].Motion.FreeRun(2, Speed_Z);

                }
                catch (A3200Exception exception)
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                }
            }
        }

        private void btn_Motion_MoveP_Z_MouseUp(object sender, MouseEventArgs e)
        {
            if (rdo_Motion_Jog.Checked == true)
            {
                try
                {
                    this._Aerotech_Controller.Commands[this.taskIndex].Motion.FreeRunStop(2);
                }
                catch (A3200Exception exception)
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                }
            }
        }

        private void btn_Motion_MoveN_Z_MouseDown(object sender, MouseEventArgs e)
        {
            if (rdo_Motion_Jog.Checked == true)
            {
                try
                {
                    this._Aerotech_Controller.Commands[this.taskIndex].Motion.FreeRun(2, -Speed_Z);

                }
                catch (A3200Exception exception)
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                }
            }
        }

        private void btn_Motion_MoveN_Z_MouseUp(object sender, MouseEventArgs e)
        {
            if (rdo_Motion_Jog.Checked == true)
            {
                try
                {
                    this._Aerotech_Controller.Commands[this.taskIndex].Motion.FreeRunStop(2);
                }
                catch (A3200Exception exception)
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, exception.Message, tmpErrStr);
                }
            }
        }

        private void btn_Motion_MoveP_Q_Click(object sender, EventArgs e)
        {
            if (_orientalConnected)
            {
                if (rdo_Motion_Jog.Checked == false)
                {
                    if (rdo_Motion_MoveRel.Checked == true)
                    {

                        try
                        {
                            azd_value = (int)(Convert.ToDouble(txt_Motion_Distance_Q.Text) * 1000);

                            int axisspeed = Convert.ToInt32(double.Parse(cmb_Motion_Speed_Q.Text) * 1000);
                            uint upRate = Convert.ToUInt32(txt_Motion_Acceleration_Q.Text) * 1000;
                            uint downRate = Convert.ToUInt32(txt_Motion_Deceleration_Q.Text) * 1000;

                            //_AZD_Controller.MoveRel(azd_value, axisspeed, upRate, downRate);
                            PCIE_1203_SetParam(axisspeed, upRate, downRate);
                            _returnCode = Motion.mAcm_AxMoveRel(m_Axishand[0], azd_value);
                        }
                        catch (Exception ex)
                        {

                            LogMsgAdd(Error_Log, lb_ErrorList, ex.Message, tmpErrStr);

                        }
                    }
                    else
                    {
                        if (rdo_Motion_MoveAbs.Checked == true)
                        {
                            try
                            {
                                azd_value = (int)(Convert.ToDouble(txt_Motion_Distance_Q.Text) * 1000);

                                int axisspeed = Convert.ToInt32(double.Parse(cmb_Motion_Speed_Q.Text) * 1000);
                                uint upRate = Convert.ToUInt32(txt_Motion_Acceleration_Q.Text) * 1000;
                                uint downRate = Convert.ToUInt32(txt_Motion_Deceleration_Q.Text) * 1000;

                                //_AZD_Controller.MoveAbs(azd_value, axisspeed, upRate, downRate);
                                PCIE_1203_SetParam(axisspeed, upRate, downRate);
                                _returnCode = Motion.mAcm_AxMoveAbs(m_Axishand[0], azd_value);

                            }
                            catch (Exception ex)
                            {
                                LogMsgAdd(Error_Log, lb_ErrorList, ex.Message, tmpErrStr);
                            }
                        }
                    }
                }
            }
        }

        private void btn_Motion_MoveN_Q_Click(object sender, EventArgs e)
        {
            if (_orientalConnected)
            {
                if (rdo_Motion_Jog.Checked == false)
                {
                    if (rdo_Motion_MoveRel.Checked == true)
                    {
                        try
                        {

                            azd_value = (int)(Convert.ToDouble(txt_Motion_Distance_Q.Text) * 1000);

                            int axisspeed = Convert.ToInt32(double.Parse(cmb_Motion_Speed_Q.Text) * 1000);
                            uint upRate = Convert.ToUInt32(txt_Motion_Acceleration_Q.Text) * 1000;
                            uint downRate = Convert.ToUInt32(txt_Motion_Deceleration_Q.Text) * 1000;
                            //_AZD_Controller.MoveRel(-azd_value, axisspeed, upRate, downRate);
                            PCIE_1203_SetParam(axisspeed, upRate, downRate);
                            _returnCode = Motion.mAcm_AxMoveRel(m_Axishand[0], -azd_value);




                        }
                        catch (Exception ex)
                        {
                            LogMsgAdd(Error_Log, lb_ErrorList, ex.Message, tmpErrStr);
                        }
                    }
                    else
                    {
                        if (rdo_Motion_MoveAbs.Checked == true)
                        {
                            try
                            {
                                azd_value = (int)(Convert.ToDouble(txt_Motion_Distance_Q.Text) * 1000);

                                int axisspeed = Convert.ToInt32(double.Parse(cmb_Motion_Speed_Q.Text) * 1000);
                                uint upRate = Convert.ToUInt32(txt_Motion_Acceleration_Q.Text) * 1000;
                                uint downRate = Convert.ToUInt32(txt_Motion_Deceleration_Q.Text) * 1000;

                                //_AZD_Controller.MoveAbs(-azd_value, axisspeed, upRate, downRate);
                                PCIE_1203_SetParam(axisspeed, upRate, downRate);
                                _returnCode = Motion.mAcm_AxMoveAbs(m_Axishand[0], azd_value);

                            }
                            catch (Exception ex)
                            {
                                LogMsgAdd(Error_Log, lb_ErrorList, ex.Message, tmpErrStr);
                            }
                        }
                    }
                }
            }
        }

        private void btn_Motion_MoveN_Q_MouseDown(object sender, MouseEventArgs e)
        {
            if (rdo_Motion_Jog.Checked == true)
            {
                try
                {
                    int axisspeed = Convert.ToInt32(double.Parse(cmb_Motion_Speed_Q.Text) * 1000);
                    uint upRate = Convert.ToUInt32(txt_Motion_Acceleration_Q.Text) * 1000;
                    uint downRate = Convert.ToUInt32(txt_Motion_Deceleration_Q.Text) * 1000;

                    //_AZD_Controller.ContinueMove(-axisspeed, upRate, downRate);
                    if (_orientalConnected)
                    {
                        PCIE_1203_SetParam(axisspeed, upRate, downRate);
                        _returnCode = Motion.mAcm_AxMoveVel(m_Axishand[0], 1);
                    }

                }
                catch (Exception ex)
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, ex.Message, tmpErrStr);
                }
            }
        }

        private void btn_Motion_MoveN_Q_MouseUp(object sender, MouseEventArgs e)
        {
            if (_orientalConnected)
            {
                if (!rdo_Motion_Jog.Checked)
                    return;
                //_AZD_Controller.Stop();

                //To command axis to decelerate to stop.
                _returnCode = Motion.mAcm_AxStopDec(m_Axishand[0]);
                if (_returnCode != (uint)ErrorCode.SUCCESS)
                {
                    strTemp = "Axis To decelerate Stop Failed With Error Code: [0x" + Convert.ToString(_returnCode, 16) + "]";
                    ShowMessages(strTemp, _returnCode);
                    return;
                }


            }
        }

        private void btn_Motion_MoveP_Q_MouseDown(object sender, MouseEventArgs e)
        {
            if (rdo_Motion_Jog.Checked == true)
            {
                try
                {
                    int axisspeed = Convert.ToInt32(double.Parse(cmb_Motion_Speed_Q.Text) * 1000);
                    uint upRate = Convert.ToUInt32(txt_Motion_Acceleration_Q.Text) * 1000;
                    uint downRate = Convert.ToUInt32(txt_Motion_Deceleration_Q.Text) * 1000;
                    //if (_Using)
                    //    _AZD_Controller.ContinueMove(axisspeed, upRate, downRate);
                    if (_orientalConnected)
                    {
                        PCIE_1203_SetParam(axisspeed, upRate, downRate);
                        _returnCode = Motion.mAcm_AxMoveVel(m_Axishand[0], 0);
                    }

                }
                catch (Exception ex)
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, ex.Message, tmpErrStr);
                }
            }
        }

        private void btn_Motion_MoveP_Q_MouseUp(object sender, MouseEventArgs e)
        {
            if (_orientalConnected)
            {
                if (!rdo_Motion_Jog.Checked)
                    return;
                //_AZD_Controller.Stop();

                //To command axis to decelerate to stop.
                _returnCode = Motion.mAcm_AxStopDec(m_Axishand[0]);
                if (_returnCode != (uint)ErrorCode.SUCCESS)
                {
                    strTemp = "Axis To decelerate Stop Failed With Error Code: [0x" + Convert.ToString(_returnCode, 16) + "]";
                    ShowMessages(strTemp, _returnCode);
                    return;
                }
            }
        }


        /// <summary>
        /// 定點安全限制
        /// </summary>
        private bool SafePositionSetting(double x, double y, double z)
        {
            bool save = true;
            if (x <= -50 && z >= 14) //右側CCD區
            {
                save = false;
            }

            return save;
        }


        /// <summary>
        /// JOG安全速度設定
        /// </summary>
        private void SafeJOGSpeedSetting()
        {
            // Z軸下降低於安全高度 JOG SPEED 強制改1mm以下
            if (double.Parse(txt_Motion_Position_Z.Text) >= 0)
            {
                if (Speed_Z > 1)
                {
                    cmb_Motion_Speed_Z.SelectedIndex = 1; //1mm
                }

                if (Speed_X > 1)
                {
                    cmb_Motion_Speed_X.SelectedIndex = 1; //1mm
                }

                if (Speed_Y > 1)
                {
                    cmb_Motion_Speed_Y.SelectedIndex = 1; //1mm
                }
            }
        }
        private void btn_Motion_Move_HeightToCCD_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "警告", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (!motioning())
                {
                    if (double.Parse(txt_Motion_Position_Z.Text) <= _msPosData.M_Wait_Z)
                    {
                        Distance_X = (Convert.ToDouble(_msPosData.M_Laser2Vision_X) - Convert.ToDouble(_msPosData.M_Laser2Height_X));
                        Distance_Y = (Convert.ToDouble(_msPosData.M_Laser2Vision_Y) - Convert.ToDouble(_msPosData.M_Laser2Height_Y));
                        Axis_thMotionRun_Tick(Motion_Num.Rapid);
                    }
                    else
                    {
                        MessageBox.Show($"高度需至等待高度[{_msPosData.M_Wait_Z}mm]以上。");
                    }

                }
                else
                {
                    MessageBox.Show("motioning");
                }
            }
        }

        private void btn_Motion_Move_CCDToHeight_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "警告", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (!motioning())
                {
                    if (double.Parse(txt_Motion_Position_Z.Text) <= _msPosData.M_Wait_Z)
                    {
                        Distance_X = -(Convert.ToDouble(_msPosData.M_Laser2Vision_X) - Convert.ToDouble(_msPosData.M_Laser2Height_X));
                        Distance_Y = -(Convert.ToDouble(_msPosData.M_Laser2Vision_Y) - Convert.ToDouble(_msPosData.M_Laser2Height_Y));
                        Axis_thMotionRun_Tick(Motion_Num.Rapid);
                    }
                    else { MessageBox.Show($"高度需至等待高度[{_msPosData.M_Wait_Z}mm]以上。"); }
                }
                else
                {
                    MessageBox.Show("motioning");
                }
            }
        }

        private void btn_Motion_Move_LaserToCCD_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "警告", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (!motioning())
                {
                    if (double.Parse(txt_Motion_Position_Z.Text) <= _msPosData.M_Wait_Z)
                    {
                        Distance_X = Convert.ToDouble(_msPosData.M_Laser2Vision_X);
                        Distance_Y = Convert.ToDouble(_msPosData.M_Laser2Vision_Y);
                        Axis_thMotionRun_Tick(Motion_Num.Rapid);
                    }
                    else { MessageBox.Show($"高度需至等待高度[{_msPosData.M_Wait_Z}mm]以上。"); }
                }
                else
                {
                    MessageBox.Show("motioning");
                }
            }
        }

        private void btn_Motion_Move_CCDToLaser_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "警告", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (!motioning())
                {
                    if (double.Parse(txt_Motion_Position_Z.Text) <= _msPosData.M_Wait_Z)
                    {
                        Distance_X = -Convert.ToDouble(_msPosData.M_Laser2Vision_X);
                        Distance_Y = -Convert.ToDouble(_msPosData.M_Laser2Vision_Y);
                        Axis_thMotionRun_Tick(Motion_Num.Rapid);
                    }
                    else { MessageBox.Show($"高度需至等待高度[{_msPosData.M_Wait_Z}mm]以上。"); }
                }
                else
                {
                    MessageBox.Show("motioning");
                }
            }
        }

        private void AZD_Connect()
        {
            //_AZD_Controller.Connect();

            //_AZD_Controller.Initialize();
            PCIE_1203_Open();

        }

        private void AZD_Disconnect()
        {
            //_AZD_Controller.Deinitialize();

            //_AZD_Controller.Close();
            PCIE_1203_CLOSE();
        }
        #endregion

        #region MarkingMate雷射控制卡

        /// <summary>
        /// 讀取ezm專案檔
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void but_Loadfile_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Load_AX_File();
                MessageParam();
            }
        }
        //讀存ezm專案檔
        private void but_Savefile_Click(object sender, EventArgs e)
        {
            Save_AX_File();
            MessageParam();
        }


        private void LaserSettingSave()
        {
            Save_AX_File();
            Invoke(new updatalaserstauts(updatalaser));//2024-05-24
        }

        //雷射開啟
        private void btn_Trigger_D_Click(object sender, EventArgs e)
        {
            MessageParam();
            if (StartMarking())
                UI_SB_LaserStatus_Emission.BackColor = Color.LawnGreen;
        }
        //雷射停止
        private void btn_Trigger_Stop_Click(object sender, EventArgs e)
        {
            StopMarking();
            MessageParam();
        }


        /// <summary>
        /// 切換第一段功率   (1:POWER 2:GND 3: SIGNAL)
        /// </summary>
        /// <returns></returns>
        private bool Set_SBLaserData(enum_LaserType type)
        {
            bool result = false;
            try
            {
                switch (type)
                {
                    case enum_LaserType.Power: //POWER
                        if (SetSpot_Power(StrSpotName1, double.Parse(txt_Power_ALaserPower.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_Power_ALaserTimes.Text)) && SetSpot_Power(StrSpotName2, double.Parse(txt_Power_BLaserPower.Text))
                    && SetSpot_Delay(StrSpotName2, double.Parse(txt_Power_BLaserTimes.Text)) && SetCtrlDelay_Time(StrDelayName, double.Parse(txt_Power_LaserDelayTimes.Text)))
                        {
                            result = true;
                        }
                        break;

                    case enum_LaserType.GND: //GND
                        if (SetSpot_Power(StrSpotName1, double.Parse(txt_Ground_ALaserPower.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_Ground_ALaserTimes.Text)) && SetSpot_Power(StrSpotName2, double.Parse(txt_Ground_BLaserPower.Text))
                    && SetSpot_Delay(StrSpotName2, double.Parse(txt_Ground_BLaserTimes.Text)) && SetCtrlDelay_Time(StrDelayName, double.Parse(txt_Ground_LaserDelayTimes.Text)))
                        {
                            result = true;
                        }
                        break;

                    case enum_LaserType.Signal: //SIGNAL
                        if (SetSpot_Power(StrSpotName1, double.Parse(txt_Signal_ALaserPower.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_Signal_ALaserTimes.Text)) && SetSpot_Power(StrSpotName2, double.Parse(txt_Signal_BLaserPower.Text))
                    && SetSpot_Delay(StrSpotName2, double.Parse(txt_Signal_BLaserTimes.Text)) && SetCtrlDelay_Time(StrDelayName, double.Parse(txt_Signal_LaserDelayTimes.Text)))
                        {
                            result = true;
                        }
                        break;

                    default: //SIGNAL
                        if (SetSpot_Power(StrSpotName1, double.Parse(txt_Signal_ALaserPower.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_Signal_ALaserTimes.Text)) && SetSpot_Power(StrSpotName2, double.Parse(txt_Signal_BLaserPower.Text))
                             && SetSpot_Delay(StrSpotName2, double.Parse(txt_Signal_BLaserTimes.Text)) && SetCtrlDelay_Time(StrDelayName, double.Parse(txt_Signal_LaserDelayTimes.Text)))
                        {
                            result = true;
                        }
                        break;
                }

                LaserSettingSave();

            }
            catch (Exception ex)
            {
                Error_Log.Add($"Set_SBLaserData Error: {ex.ToString()}");
            }
            return result;
        }




        /// <summary>
        /// 切換第二段功率  (1:POWER 2:GND 3: SIGNAL)
        /// </summary>
        /// <returns></returns>
        private bool Set_SBLaserData2(enum_LaserType type)
        {
            bool result = false;
            try
            {
                switch (type)
                {
                    case enum_LaserType.Power: //POWER
                        if (SetSpot_Power(StrSpotName1, double.Parse(txt_Power_ALaserPower2.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_Power_ALaserTimes2.Text)) && SetSpot_Power(StrSpotName2, double.Parse(txt_Power_BLaserPower2.Text))
                    && SetSpot_Delay(StrSpotName2, double.Parse(txt_Power_BLaserTimes2.Text)) && SetCtrlDelay_Time(StrDelayName, double.Parse(txt_Power_LaserDelayTimes2.Text)))
                        {
                            result = true;
                        }
                        break;

                    case enum_LaserType.GND: //GND
                        if (SetSpot_Power(StrSpotName1, double.Parse(txt_Ground_ALaserPower2.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_Ground_ALaserTimes2.Text)) && SetSpot_Power(StrSpotName2, double.Parse(txt_Ground_BLaserPower2.Text))
                    && SetSpot_Delay(StrSpotName2, double.Parse(txt_Ground_BLaserTimes2.Text)) && SetCtrlDelay_Time(StrDelayName, double.Parse(txt_Ground_LaserDelayTimes2.Text)))
                        {
                            result = true;
                        }
                        break;

                    case enum_LaserType.Signal: //SIGNAL
                        if (SetSpot_Power(StrSpotName1, double.Parse(txt_Signal_ALaserPower2.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_Signal_ALaserTimes2.Text)) && SetSpot_Power(StrSpotName2, double.Parse(txt_Signal_BLaserPower2.Text))
                    && SetSpot_Delay(StrSpotName2, double.Parse(txt_Signal_BLaserTimes2.Text)) && SetCtrlDelay_Time(StrDelayName, double.Parse(txt_Signal_LaserDelayTimes2.Text)))
                        {
                            result = true;
                        }
                        break;

                    default: //SIGNAL
                        if (SetSpot_Power(StrSpotName1, double.Parse(txt_Signal_ALaserPower2.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_Signal_ALaserTimes2.Text)) && SetSpot_Power(StrSpotName2, double.Parse(txt_Signal_BLaserPower2.Text))
                             && SetSpot_Delay(StrSpotName2, double.Parse(txt_Signal_BLaserTimes2.Text)) && SetCtrlDelay_Time(StrDelayName, double.Parse(txt_Signal_LaserDelayTimes2.Text)))
                        {
                            result = true;
                        }
                        break;
                }
                LaserSettingSave();
            }
            catch (Exception ex)
            {
                Error_Log.Add($"Set_SBLaserData2 Error: {ex.ToString()}");
            }
            return result;
        }


        /// <summary>
        /// 切換功率   (1:POWER 2:GND 3: SIGNAL)
        /// </summary>
        /// <returns></returns>
        private bool Set_SBLaserData(int Padtype)
        {
            bool result = false;
            try
            {
                // if(Old_PadType != Padtype)
                {
                    Old_PadType = Padtype;
                    if (Padtype == _msPosData.POWER_PadType)
                    {
                        if (Set_P_LaserData())
                        {
                            result = true;
                        }

                    }
                    else if (Padtype == _msPosData.GND_PadType)
                    {
                        if (Set_G_LaserData())
                        {
                            result = true;
                        }

                    }
                    else if (Padtype == _msPosData.SENSE_PadType)
                    {
                        if (Set_S_LaserData())
                        {
                            result = true;
                        }

                    }
                    else if (Padtype == _msPosData.IO_PadType)
                    {
                        if (SetIO_LaserData())
                        {
                            result = true;
                        }

                    }
                    else if (Padtype == _msPosData.NC_PadType)
                    {
                        if (SetNC_LaserData())
                        {
                            result = true;
                        }

                    }
                }
                //else
                //{
                //                result = true;
                //            }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"Set_SBLaserData Error: {ex.ToString()}");
            }
            return result;
        }


        /// <summary>
        /// 切換Signal 第一段功率
        /// </summary>
        /// <returns></returns>
        private bool Set_S_LaserData()
        {
            bool result = false;
            try
            {
                if (SetSpot_Power(StrSpotName1, double.Parse(txt_Signal_ALaserPower.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_Signal_ALaserTimes.Text)) /* && SetSpot_Power(StrSpotName2, double.Parse(txt_Signal_BLaserPower.Text))
                    && SetSpot_Delay(StrSpotName2, double.Parse(txt_Signal_BLaserTimes.Text)) && SetCtrlDelay_Time(StrDelayName, double.Parse(txt_Signal_LaserDelayTimes.Text))*/)
                {
                    //LaserSettingSave();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                result = false;
                Error_Log.Add($"Set_S_LaserData Error: {ex.ToString()}");
            }
            return result;
        }

        /// <summary>
        /// 切換Signal 第二段功率
        /// </summary>
        /// <returns></returns>
        private bool Set_S_LaserData2()
        {
            bool result = false;
            try
            {
                if (SetSpot_Power(StrSpotName1, double.Parse(txt_Signal_ALaserPower2.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_Signal_ALaserTimes2.Text)) /*&& SetSpot_Power(StrSpotName2, double.Parse(txt_Signal_BLaserPower2.Text))
                    && SetSpot_Delay(StrSpotName2, double.Parse(txt_Signal_BLaserTimes2.Text)) && SetCtrlDelay_Time(StrDelayName, double.Parse(txt_Signal_LaserDelayTimes2.Text))*/)
                {
                    //LaserSettingSave();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                result = false;
                Error_Log.Add($"Set_S_LaserData2 Error: {ex.ToString()}");
            }
            return result;
        }

        /// <summary>
        /// 切換Power 第一段功率
        /// </summary>
        /// <returns></returns>
        private bool Set_P_LaserData()
        {
            bool result = false;
            try
            {
                if (SetSpot_Power(StrSpotName1, double.Parse(txt_Power_ALaserPower.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_Power_ALaserTimes.Text)) /*&& SetSpot_Power(StrSpotName2, double.Parse(txt_Power_BLaserPower.Text))
                    && SetSpot_Delay(StrSpotName2, double.Parse(txt_Power_BLaserTimes.Text)) && SetCtrlDelay_Time(StrDelayName, double.Parse(txt_Power_LaserDelayTimes.Text))*/)
                {
                    //LaserSettingSave();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                result = false;
                Error_Log.Add($"Set_P_LaserData Error: {ex.ToString()}");
            }
            return result;
        }

        /// <summary>
        /// 切換Power 第二段功率
        /// </summary>
        /// <returns></returns>
        private bool Set_P_LaserData2()
        {
            bool result = false;
            try
            {
                if (SetSpot_Power(StrSpotName1, double.Parse(txt_Power_ALaserPower2.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_Power_ALaserTimes2.Text)) && SetSpot_Power(StrSpotName2, double.Parse(txt_Power_BLaserPower2.Text))
                    && SetSpot_Delay(StrSpotName2, double.Parse(txt_Power_BLaserTimes2.Text)) && SetCtrlDelay_Time(StrDelayName, double.Parse(txt_Power_LaserDelayTimes2.Text)))
                {
                    //LaserSettingSave();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                result = false;
                Error_Log.Add($"Set_P_LaserData2 Error: {ex.ToString()}");
            }
            return result;
        }

        /// <summary>
        /// 切換Ground 第一段功率
        /// </summary>
        /// <returns></returns>
        private bool Set_G_LaserData()
        {
            bool result = false;
            try
            {
                if (SetSpot_Power(StrSpotName1, double.Parse(txt_Ground_ALaserPower.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_Ground_ALaserTimes.Text)) /* && SetSpot_Power(StrSpotName2, double.Parse(txt_Ground_BLaserPower.Text))
                    && SetSpot_Delay(StrSpotName2, double.Parse(txt_Ground_BLaserTimes.Text)) && SetCtrlDelay_Time(StrDelayName, double.Parse(txt_Ground_LaserDelayTimes.Text))*/)
                {
                    //LaserSettingSave();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"Set_G_LaserData Error: {ex.ToString()}");
            }
            return result;
        }

        /// <summary>
        /// 切換Ground 第二段功率
        /// </summary>
        /// <returns></returns>
        private bool Set_G_LaserData2()
        {
            bool result = false;
            try
            {
                if (SetSpot_Power(StrSpotName1, double.Parse(txt_Ground_ALaserPower2.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_Ground_ALaserTimes2.Text)) && SetSpot_Power(StrSpotName2, double.Parse(txt_Ground_BLaserPower2.Text))
                    && SetSpot_Delay(StrSpotName2, double.Parse(txt_Ground_BLaserTimes2.Text)) && SetCtrlDelay_Time(StrDelayName, double.Parse(txt_Ground_LaserDelayTimes2.Text)))
                {
                    //LaserSettingSave();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"Set_G_LaserData2 Error: {ex.ToString()}");
            }
            return result;
        }

        /// <summary>
        /// IO 雷射參數設定
        /// </summary>
        /// <returns></returns>
        private bool SetIO_LaserData()
        {
            bool result = false;
            try
            {
                if (SetSpot_Power(StrSpotName1, double.Parse(txt_IO_ALaserPower.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_IO_ALaserTimes.Text)))
                {
                    //LaserSettingSave();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"SetIO_LaserData Error: {ex.ToString()}");
            }
            return result;
        }

        /// <summary>
        /// IO 雷射參數設定
        /// </summary>
        /// <returns></returns>
        private bool SetNC_LaserData()
        {
            bool result = false;
            try
            {
                if (SetSpot_Power(StrSpotName1, double.Parse(txt_NC_ALaserPower.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_NC_ALaserTimes.Text)))
                {
                    // LaserSettingSave();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"SetIO_LaserData Error: {ex.ToString()}");
            }
            return result;
        }

        /// <summary>
        /// 清潔 雷射參數設定
        /// </summary>
        /// <returns></returns>
        private bool Set_CleanLaserData()
        {
            bool result = false;
            try
            {
                if (SetSpot_Power(StrSpotName1, double.Parse(txt_CleanLaserPower.Text)) && SetSpot_Delay(StrSpotName1, double.Parse(txt_CleanLaserTime.Text)))
                {
                    //LaserSettingSave();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"Set_CleanLaserData Error: {ex.ToString()}");
            }
            return result;
        }

        /// <summary>
        /// Powermeter量測 雷射參數設定
        /// </summary>
        /// <returns></returns>
        private bool Set_PMLaserData()
        {
            bool result = false;
            try
            {   //預設power 14%   450ms
                if (SetSpot_Power(StrSpotName1, _msPosData.PM_Power) && SetSpot_Delay(StrSpotName1, _msPosData.PM_Time))
                {
                    //LaserSettingSave();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                result = false;
                Error_Log.Add($"Set_SBLaserData2 Error: {ex.ToString()}");
            }
            return result;
        }


        //二階參數設定
        private void btn_Set_SBLaserData2_Click(object sender, EventArgs e)
        {
            //預設 signal
            Set_SBLaserData2(enum_LaserType.Signal);

            //顯示切換
            //txt_MUI_SB_ALaserPower.Text = txt_SB_ALaserPower2.Text;
        }
        //雷射結束狀態顯示
        private void axMMStatus_MarkEnd(object sender, AxMMSTATUSLib._DMMStatusEvents_MarkEndEvent e)
        {
            LaserStatus_Emission = false;
            GetMarkEnd_Status(e);
            UI_SB_LaserStatus_Emission.BackColor = Color.Gray;
            MUI_SB_LaserStatus_Emission.BackColor = Color.Gray;
            UI_TB_Test_Laser_EmissionStation.BackColor = Color.Gray;
            lbl_MarkTime.Text = GetMark_Time().ToString() + " S";
        }

        /// <summary>
        /// signal 參數資訊
        /// </summary>
        public void MessageParam()
        {
            lbl_StrName.Text = StrName;
            lbl_Power.Text = GetSpot_Power(StrSpotName1).ToString() + " %";
            lbl_Power2.Text = GetSpot_Power(StrSpotName2).ToString() + " %";

            txt_MUI_SB_ALaserPower.Text = GetSpot_Power(StrSpotName1).ToString();
            txt_MUI_SB_BLaserPower.Text = GetSpot_Power(StrSpotName2).ToString();
            txt_MUI_SB_ALaserTImes.Text = GetSpot_Delay(StrSpotName1).ToString();
            txt_MUI_SB_BLaserTImes.Text = GetSpot_Delay(StrSpotName2).ToString();
        }





        #region Function
        /// <summary>
        /// 控制卡初始化
        /// </summary>
        /// <param name="回傳失敗字串"></param>
        /// <returns></returns>
        public bool Initial()
        {

            int _Return = axMMMark.Initial();

            //成功回傳0
            if (_Return == 0)
            {
                return true;
            }
            else
            {
                switch (_Return)
                {
                    case 1:
                        ReturnErr = "失敗!";
                        break;
                    case 2:
                        ReturnErr = "自動產生新的資料庫，失敗!";
                        break;
                    case 3:
                        ReturnErr = "找不到板卡!";
                        break;
                    case 4:
                        ReturnErr = "找不到保護鎖!";
                        break;
                    case 5:
                        ReturnErr = "找不到板卡及保護鎖!";
                        break;
                    case 6:
                        ReturnErr = "config.ini不存在!";
                        break;
                }

                ReturnErr = "控制卡初始化異常，原因: " + ReturnErr;

                return false;
            }
        }
        /// <summary>
        /// 狀態初始化
        /// </summary>
        /// <param name="回傳失敗字串"></param>
        /// <returns></returns>
        public bool StatusInitial()
        {

            int _Return = axMMStatus.Initial();

            //成功回傳0
            if (_Return == 0)
            {
                return true;
            }
            else
            {
                ReturnErr = "狀態初始化，失敗!";

                return false;
            }
        }
        /// <summary>
        /// 編輯模組初始化
        /// </summary>
        /// <returns></returns>
        public bool EditInitial()
        {

            int _Return = axMMEdit.Initial();

            //成功回傳0
            if (_Return == 0)
            {
                return true;
            }
            else
            {
                ReturnErr = "編輯模組初始化，失敗!";

                return false;
            }
        }



        private void SetMotionErrorMsg(TextBox textBox, string str)
        {
            if (textBox.InvokeRequired)
            {
                Action safeWrite = delegate { SetMotionErrorMsg(textBox, str); };
                this.Invoke(safeWrite);

            }
            else
            {
                textBox.Text = str;

            }
        }

        private void hsb_Valve_ValueChanged(object sender, EventArgs e)
        {
            E_NozzleValve.Text = hsb_NozzleValve.Value.ToString();
        }

        private void B_Valve_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否套用比例閥設定?", "警告", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                if (double.Parse(E_NozzleValve.Text) > 20)
                    E_NozzleValve.Text = "20";
                if (double.Parse(E_NozzleValve.Text) < 0)
                    E_NozzleValve.Text = "0";

                ushort value = (ushort)(double.Parse(E_NozzleValve.Text) * 10);

                if (!UpdateProportionalValve(value))
                {
                    MessageBox.Show("比例閥設定失敗", "警告", MessageBoxButtons.OKCancel);
                }
                // FestoPressure.PValveSetPressure(ushort.Parse(_ConfigSystem._FESTO_PPC_Valve_Port), value);
            }
        }

        private void btn_Home_ALL_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行各軸原點復歸?", "警告", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                Stopwatch timeT = new Stopwatch();
                TimeSpan timeOut = new TimeSpan(0, 3, 0); // 3min
                int[] axes = new int[] { 0, 1 };
                Task.Run(() =>
                {
                    timeT.Start();

                    this._Aerotech_Controller.Commands.AcknowledgeAll();
                    if (!En_X)
                        this._Aerotech_Controller.Commands.Axes[0].Motion.Enable(); ;
                    if (!En_Y)
                        this._Aerotech_Controller.Commands.Axes[1].Motion.Enable(); ;
                    if (!En_Z)
                        this._Aerotech_Controller.Commands.Axes[2].Motion.Enable(); ;

                    Thread.Sleep(300);

                    if (!HOME_Z)
                        this._Aerotech_Controller.Commands.Axes[2].Motion.Home();



                    while (!HOME_X || !HOME_Y)
                    {
                        if (HOME_Z)
                        {
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true)) //往上負
                            {
                                //if (!HOME_X)
                                //    this._Aerotech_Controller.Commands.Axes[0].Motion.Home();
                                //if (!HOME_Y)
                                //    this._Aerotech_Controller.Commands.Axes[1].Motion.Home();

                                this._Aerotech_Controller.Commands.Axes[axes].Motion.Home();
                            }
                            //Invoke(new Action(EnableActionPage));
                        }

                        if (timeT.Elapsed >= timeOut)
                        {
                            Invoke(new dele_msgShow(ErrMSG_Show), "設備復歸超時!");
                            break;
                        }
                    }

                });
            }
        }

        private void btn_Motion_MotorStatus_X_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Motion_X)
                {
                    if (MessageBox.Show("是否執行Servo On?", "警告", MessageBoxButtons.OKCancel) == DialogResult.OK)
                        this._Aerotech_Controller.Commands.Axes[0].Motion.Enable();
                }
                else
                {
                    if (MessageBox.Show("是否執行Servo Off?", "警告", MessageBoxButtons.OKCancel) == DialogResult.OK)
                        this._Aerotech_Controller.Commands.Axes[0].Motion.Disable();
                }
            }
            catch (A3200Exception exception)
            {

            }

        }

        private void btn_Motion_MotorStatus_Y_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Motion_Y)
                {
                    if (MessageBox.Show("是否執行Servo On?", "警告", MessageBoxButtons.OKCancel) == DialogResult.OK)
                        this._Aerotech_Controller.Commands.Axes[1].Motion.Enable();
                }
                else
                {
                    if (MessageBox.Show("是否執行Servo Off?", "警告", MessageBoxButtons.OKCancel) == DialogResult.OK)
                        this._Aerotech_Controller.Commands.Axes[1].Motion.Disable();
                }
            }
            catch (A3200Exception exception)
            {

            }
        }

        private void btn_Motion_MotorStatus_Z_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Motion_Z)
                {
                    if (MessageBox.Show("是否執行Servo On?", "警告", MessageBoxButtons.OKCancel) == DialogResult.OK)
                        this._Aerotech_Controller.Commands.Axes[2].Motion.Enable();
                }
                else
                {
                    if (MessageBox.Show("是否執行Servo Off?", "警告", MessageBoxButtons.OKCancel) == DialogResult.OK)
                        this._Aerotech_Controller.Commands.Axes[2].Motion.Disable();
                }
            }
            catch (A3200Exception exception)
            {

            }
        }


        private void B_ST_PosSave_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否儲存ST中心位置?", "警告", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _recipeData.Panel.ST_Col = int.Parse(E_ST_Col.Text);
                _recipeData.Panel.ST_Row = int.Parse(E_ST_Row.Text);
                _recipeData.Panel.ST_Num = _recipeData.Panel.ST_Col * _recipeData.Panel.ST_Row;
                Num_ST_NO.Maximum = _recipeData.Panel.ST_Num;
                _recipeData.Panel.SetST_Num(_recipeData.Panel.ST_Num);

                for (int i = 0; i < _recipeData.Panel.ST_Num; i++)
                {
                    _recipeData.Panel.ST_CenterPos[i].X = double.Parse(ST_Panel[i].txt_PosX.Text);
                    _recipeData.Panel.ST_CenterPos[i].Y = double.Parse(ST_Panel[i].txt_PosY.Text);
                }

                //_recipeData.Panel.ST_CenterPos[0].X = Convert.ToDouble(E_ST1_X.Text);
                //_recipeData.Panel.ST_CenterPos[0].Y = Convert.ToDouble(E_ST1_Y.Text);
                //_recipeData.Panel.ST_CenterPos[1].X = Convert.ToDouble(E_ST2_X.Text);
                //_recipeData.Panel.ST_CenterPos[1].Y = Convert.ToDouble(E_ST2_Y.Text);
                //_recipeData.Panel.ST_CenterPos[2].X = Convert.ToDouble(E_ST3_X.Text);
                //_recipeData.Panel.ST_CenterPos[2].Y = Convert.ToDouble(E_ST3_Y.Text);
                //_recipeData.Panel.ST_CenterPos[3].X = Convert.ToDouble(E_ST4_X.Text);
                //_recipeData.Panel.ST_CenterPos[3].Y = Convert.ToDouble(E_ST4_Y.Text);

                _recipeData.Save_Data(RecipeFileName);
            }
        }

        /// <summary>
        /// 路徑載入
        /// </summary>
        private void PathFileLoad()
        {
            using (OpenFileDialog _openDialog = new OpenFileDialog())
            {
                _fileLoad = false;
                try
                {
                    _openDialog.Filter = "Excel Files (*.xls; *.xlsx)|*.xls;*.xlsx";
                    _openDialog.InitialDirectory = @"C:\";

                    MList_Log.Add("手動: 座標讀取測試 開啟");

                    //開啟文件路徑
                    if (_openDialog.ShowDialog() == DialogResult.OK)
                    {

                        E_ST_FileName.Text = Path.GetFileName(_openDialog.FileName);
                        E_ST_FilePath.Text = _openDialog.FileName.ToString();
                        E_ST_FileName2.Text = Path.GetFileName(_openDialog.FileName);
                        E_ST_FilePath2.Text = _openDialog.FileName.ToString();
                        txt_test_STPos_File.Text = _openDialog.FileName;

                        MList_Log.Add("手動: 座標讀取測試 檔案- " + txt_test_STPos_File.Text);


                        //建立Excel物件
                        IWorkbook workbook;
                        int sheetnumber = 99;
                        DataTable dt = new DataTable();

                        //讀取Excel
                        using (FileStream file = new FileStream(_openDialog.FileName, FileMode.Open, FileAccess.Read))
                        {
                            //繪製資料表
                            workbook = WorkbookFactory.Create(file);

                            //取得分頁名稱
                            List<string> SheetName = new List<string>();

                            for (int i = 0; i < workbook.NumberOfSheets; i++)
                            {
                                SheetName.Add(workbook.GetSheetName(i));
                            }

                            //建立分頁選擇視窗 並回傳所選擇分頁index
                            ExcelSheet_Select excelSheet_Select = new ExcelSheet_Select();
                            excelSheet_Select.SheetNumber = (num) => { sheetnumber = num; };

                            excelSheet_Select.ShowSheetName(_openDialog.FileName, SheetName);

                            DialogResult result = excelSheet_Select.ShowDialog();

                            if (result == DialogResult.OK)
                            {
                                txt_test_STPos_Sheet.Text = workbook.GetSheetName(sheetnumber);

                                MList_Log.Add("手動: 座標讀取測試 選擇分頁- " + txt_test_STPos_Sheet.Text);

                                //根據index讀取分頁
                                ISheet sheet = workbook.GetSheetAt(sheetnumber);

                                MList_Log.Add("手動: 座標讀取測試 解析Excel");

                                //由第一列取標題做為欄位名稱
                                IRow headerRow = sheet.GetRow(0);
                                int cellCount = headerRow.LastCellNum; // 取欄位數
                                for (int i = headerRow.FirstCellNum; i < cellCount; i++)
                                {
                                    //table.Columns.Add(new DataColumn(headerRow.GetCell(i).StringCellValue, typeof(double)));
                                    dt.Columns.Add(new DataColumn("(標題" + (i + 1).ToString() + ")" + headerRow.GetCell(i, MissingCellPolicy.CREATE_NULL_AS_BLANK).StringCellValue));
                                }

                                //略過第零列(標題列)，一直處理至最後一列
                                for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++)
                                {
                                    IRow row = sheet.GetRow(i);
                                    if (row == null) continue;

                                    DataRow dataRow = dt.NewRow();

                                    //依先前取得的欄位數逐一設定欄位內容
                                    for (int j = row.FirstCellNum; j < cellCount; j++)
                                    {
                                        ICell cell = row.GetCell(j, MissingCellPolicy.CREATE_NULL_AS_BLANK);
                                        if (cell != null)
                                        {
                                            //如要針對不同型別做個別處理，可善用.CellType判斷型別
                                            //再用.StringCellValue, .DateCellValue, .NumericCellValue...取值

                                            switch (cell.CellType)
                                            {
                                                case CellType.Numeric:
                                                    dataRow[j] = cell.NumericCellValue;
                                                    break;
                                                case CellType.Formula:
                                                    dataRow[j] = "";
                                                    break;
                                                default: // String
                                                         //此處只簡單轉成字串
                                                    dataRow[j] = cell.StringCellValue;
                                                    break;
                                            }
                                        }
                                    }

                                    dt.Rows.Add(dataRow);
                                }

                                MList_Log.Add("手動: 座標讀取測試 UI顯示");

                                //UI顯示
                                dGV_test_STFile.DataSource = null;
                                dGV_test_STFile.Rows.Clear();
                                dGV_test_STFile.Refresh();
                                dGV_test_STFile.Update();
                                dGV_test_STFile.DataSource = dt;

                                dGV_test_STFile.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToDisplayedHeaders;

                                for (int j = 0; j < sheet.LastRowNum; j++)
                                {
                                    dGV_test_STFile.Rows[j].HeaderCell.Value = (j + 1).ToString();
                                    Application.DoEvents();
                                }
                                for (int j = 0; j < dGV_test_STFile.ColumnCount; j++)
                                {
                                    dGV_test_STFile.Columns[j].SortMode = DataGridViewColumnSortMode.NotSortable;
                                }


                                MList_Log.Add("手動: 從已讀取之檔案內容中篩選並取得相關座標資訊");
                                Cal_All_STPos(dt);
                                _fileLoad = true;

                                workbook.Close();

                            }
                            else
                            {
                                MessageBox.Show("分頁選擇錯誤,請聯繫工程師確認");
                            }
                        }
                    }
                    else
                    {

                    }
                }
                catch (Exception ex)
                {
                    Error_Log.Add("錯誤: " + ex.Message);
                    MessageBox.Show(ex.ToString());
                }

            }
        }

        private void B_PathFileLoad_Click(object sender, EventArgs e)
        {
            PathFileLoad();
        }

        private void B_RecipeFileLoad_Click(object sender, EventArgs e)
        {
            SaveAsNew_Form _form = new SaveAsNew_Form(_ConfigSystem._RecipePath);
            _form.LoadMode = true;
            if (_form.ShowDialog() == DialogResult.OK)
            {
                string tmp = _form.SaveFileName;
                if (!tmp.Contains(".rcp"))
                    tmp += ".rcp";

                if (tmp.Length > 0)
                {
                    if (!RecipeLoad(tmp))
                    {

                        MessageBox.Show("Recipe讀取失敗。", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        LogMsgAdd(Error_Log, lb_ErrorList, "Recipe讀取失敗。", tmpErrStr);
                    }
                    else
                    {
                        this.Text = $"{_version}        Recipe[ {_ConfigSystem._RecipeName.Replace(".rcp", "")} ]";
                        L_Recipe.Text = $"Recipe[ {_ConfigSystem._RecipeName.Replace(".rcp", "")} ]";
                        LogMsgAdd(MList_Log, lb_HistoryList, $"Recipe[ {_ConfigSystem._RecipeName.Replace(".rcp", "")}]載入成功。", tmpListStr);

                        ST_TableGenerate(_recipeData.Panel.ST_Col, _recipeData.Panel.ST_Row);
                        Setting_UI();
                        CVX_RecipeChange(int.Parse(txt_CVX_Recipe.Text));
                    }
                }
                else
                {
                    MessageBox.Show("檔名錯誤。", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    LogMsgAdd(Error_Log, lb_ErrorList, "Recipe讀取失敗。", tmpErrStr);

                }
            }
        }

        /// <summary>
        /// Recipe切換
        /// </summary>
        /// <param name="fileName">完整檔名(路徑)</param>
        /// <returns></returns>
        private bool RecipeLoad(string fileName)
        {
            bool result = false;

            RecipeFileName = _ConfigSystem._RecipePath + fileName;
            if (_recipeData.Load_Data(RecipeFileName))
            {
                _ConfigSystem._RecipeName = fileName;
                result = true;


            }

            return result;
        }


        /// <summary>
        /// Recipe儲存
        /// </summary>
        /// <param name="fileName">完整檔名(路徑)</param>
        /// <returns></returns>
        private bool RecipeSave(string fileName)
        {
            bool result = false;

            RecipeFileName = _ConfigSystem._RecipePath + fileName;
            if (_recipeData.Save_Data(RecipeFileName))
            {
                _ConfigSystem._RecipeName = fileName;
                this.Text = $"{_version}        Recipe[ {_ConfigSystem._RecipeName.Replace(".rcp", "")} ]";
                L_Recipe.Text = $"Recipe[ {_ConfigSystem._RecipeName.Replace(".rcp", "")} ]";
                LogMsgAdd(MList_Log, lb_HistoryList, $"Recipe[ {_ConfigSystem._RecipeName.Replace(".rcp", "")}]儲存成功。", tmpListStr);
                result = true;
            }

            return result;
        }


        /// <summary>
        /// 自動化元件初始化
        /// </summary>
        /// <returns></returns>
        public bool CtrlObjInitial()
        {
            int _Return = axMMCTRLOBJ.Initial();

            //成功回傳0
            if (_Return == 0)
            {
                return true;
            }
            else
            {
                ReturnErr = "自動化元件初始化，失敗!";

                return false;
            }
        }


        /// <summary>
        /// 檔案ezm專案檔讀取
        /// </summary>
        /// <returns></returns>
        public bool Load_AX_File()
        {
            int _Return = 99;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //取得ezm專案檔路徑
                _tmpFilePath = System.IO.Path.GetFullPath(openFileDialog1.FileName);
                //載入ezm專案檔
                _Return = axMMMark.LoadFile(openFileDialog1.FileName);
                //取得圖層名稱
                axMMEdit.GetLayerName(1, ref StrName);
                //取得圖層底下子物件名稱-點1
                axMMEdit.GetChildObjectName(StrName, 1, ref StrSpotName1);
                //取得圖層底下子物件名稱-點2
                axMMEdit.GetChildObjectName(StrName, 3, ref StrSpotName2);
                //取得圖層底下子物件名稱-延遲時間
                axMMEdit.GetChildObjectName(StrName, 2, ref StrDelayName);
            }


            //成功回傳0
            if (_Return == 0)
            {
                return true;
            }
            else
            {
                switch (_Return)
                {
                    case 1:
                        ReturnErr = "當前無資料庫、輸入的檔案路徑名稱有誤!";
                        break;
                    case 2:
                        ReturnErr = "重置資料庫錯誤!";
                        break;
                    case 3:
                        ReturnErr = "載入檔案錯誤!";
                        break;
                    case 4:
                        ReturnErr = "回覆資料庫錯誤!";
                        break;
                    case 5:
                        ReturnErr = "非阻斷式雕刻中或是圖元資料庫閉鎖中!";
                        break;
                    case 6:
                        ReturnErr = "雷射仍在雕刻或預覽狀態中!!";
                        break;
                }
                ReturnErr = "檔案讀取失敗，" + ReturnErr;

                return false;
            }
        }
        /// <summary>
        /// 儲存ezm專案檔
        /// </summary>
        /// <returns></returns>
        public bool Save_AX_File()
        {
            if (_tmpFilePath == "" || _tmpFilePath == null)
                _tmpFilePath = _ezmPath;

            if (_tmpFilePath != "")
            {
                int _Return = axMMMark.SaveFile(_tmpFilePath);

                //成功回傳0
                if (_Return == 0)
                {
                    return true;
                }
                else
                {
                    ReturnErr = "檔案儲存失敗";
                    return false;
                }
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        ///  執行雕刻功能(設定雕刻模式，
        ///  1: 阻斷式雕刻 (無雕刻對話盒) 。
        ///  2: 阻斷式雕刻 (有雕刻對話盒) 。
        ///  3: 預覽雕刻。(V2.7A-34.14 以上可使用SetMarkSelect 函式設定只預覽選取物件)
        ///  4: 非阻斷式雕刻 (無雕刻對話盒) 。)
        ///  5: 非阻斷式雕刻且雷射不出光。 
        /// </summary>
        /// <returns></returns>
        public bool StartMarking()
        {
            int _Return = axMMMark.StartMarking(4);

            //成功回傳0
            if (_Return == 0)
            {
                return true;
            }
            else
            {
                switch (_Return)
                {
                    case 1:
                        ReturnErr = "對應元件未初始化!";
                        break;
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        ReturnErr = "錯誤!可能原因:(1)保護鎖只有編輯模式。(2)內部流程錯誤。(3)找不到保護鎖。(4)已啟動自動化流程";
                        break;
                    case 6:
                        ReturnErr = "正在雕刻中/正在預覽!";
                        break;
                    case 7:
                    case 8:
                        ReturnErr = "失敗!";
                        break;
                    case 9:
                        ReturnErr = "板卡位連接!";
                        break;
                    case 10:
                        ReturnErr = "已啟動自動化流程!";
                        break;
                }

                ReturnErr = "執行雕刻失敗，" + ReturnErr;
                Error_Log.Add($"[StartMarking Error]: {ReturnErr}");
                return false;
            }
        }
        /// <summary>
        /// 停止雕刻
        /// </summary>
        /// <returns></returns>
        public bool StopMarking()
        {
            int _Return = axMMMark.StopMarking();

            //成功回傳0
            if (_Return == 0)
            {
                return true;
            }
            else
            {
                ReturnErr = "停止雕刻觸發失敗!";

                return false;
            }
        }

        /// <summary>
        /// 雕刻結束訊息
        /// </summary>
        /// <param 雕刻結束訊息狀態="e"></param>
        /// <returns></returns>
        public bool GetMarkEnd_Status(AxMMSTATUSLib._DMMStatusEvents_MarkEndEvent e)
        {
            int _Return = e.lStatus;


            //結束回傳1
            if (_Return == 1)
            {
                return true;
            }
            else
            {
                switch (_Return)
                {
                    case 0:
                        ReturnErr = "無雕刻資料!";
                        break;
                    case -1:
                        ReturnErr = "以ESC 按鍵結束雕刻!";
                        break;
                    case -2:
                        ReturnErr = "緊急停止!";
                        break;
                }

                ReturnErr = "雕刻結束訊息，" + ReturnErr;

                return false;
            }
        }


        /// <summary>
        /// 設定層子物件-點-雕刻時間
        /// </summary>
        /// <param 圖層子物件-點名稱="SpotName"></param>
        /// <param 點雕刻時間="lDelay"></param>
        /// 時間單位[微秒]，例子:1000毫秒=1000000微秒
        /// <returns></returns>
        public bool SetSpot_Delay(string SpotName, double Delay)
        {
            //點雕刻時間不行為0
            if (Delay == 0)
                Delay = 0.000001;

            //微秒換算毫秒
            int lDelay = Convert.ToInt32(Delay * 1000);
            int _Return = axMMMark.SetSpotDelay(SpotName, lDelay);

            //成功回傳0
            if (_Return == 0)
            {
                return true;
            }
            else
            {
                ReturnErr = "設定子物件-點-雕刻時間失敗!";

                return false;
            }
        }
        /// <summary>
        /// 設定圖層子物件-點-雷射功率
        /// </summary>
        /// <param 圖層子物件-點名稱="SpotName"></param>
        /// <param 功率="dPerc"></param>
        /// 雷射功率[%]，範圍0~100
        /// <returns></returns>
        public bool SetSpot_Power(string SpotName, double dPerc)
        {

            int _Return = axMMMark.SetPower(SpotName, dPerc);

            //成功回傳0
            if (_Return == 0)
            {
                return true;
            }
            else
            {
                ReturnErr = "設定子物件-點-雷射功率失敗!";

                return false;
            }
        }
        /// <summary>
        /// 設定圖層子物件-延遲時間
        /// </summary>
        /// <param 圖層子物件-延遲時間名稱="DelayTimeName"></param>
        /// <returns></returns>
        public bool SetCtrlDelay_Time(string DelayTimeName, double Delay)
        {
            //int lDelay = Convert.ToInt32(Delay* 1000);
            int _Return = axMMCTRLOBJ.SetCtrlDelayTime(DelayTimeName, Delay);

            //成功回傳0
            if (_Return == 0)
            {
                return true;
            }
            else
            {
                switch (_Return)
                {
                    case 1:
                        ReturnErr = "失敗，對應元件未初始化/未找到指定名稱物件!";
                        break;
                    case 2:
                        ReturnErr = "失敗，暫停時間小於0";
                        break;
                }
                ReturnErr = "設定延遲時間，" + ReturnErr;

                return false;
            }
        }


        /// <summary>
        /// 取得本次雕刻所耗時間
        /// </summary>
        /// <returns></returns>
        public double GetMark_Time()
        {
            double DMarkTime = axMMMark.GetMarkTime();
            double MarkTime = Math.Round(DMarkTime / 1000, 4);
            return MarkTime;

        }
        /// <summary>
        /// 取得圖層子物件-點-雕刻時間
        /// </summary>
        /// <param 圖層子物件-點名稱="SpotName"></param>
        /// 時間單位[微秒]，例子:1000毫秒=1000000微秒
        /// <returns></returns>
        public double GetSpot_Delay(string SpotName)
        {
            float SpotDelayTime = axMMMark.GetSpotDelay(SpotName);
            float dDelay = SpotDelayTime / 1000;
            return dDelay;
        }
        /// <summary>
        /// 取得圖層子物件-點-雷射功率
        /// </summary>
        /// <returns></returns>
        public double GetSpot_Power(string SpotName)
        {
            double DPower = +axMMMark.GetPower(SpotName);
            return DPower;
        }
        /// <summary>
        /// 取得圖層子物件-延遲時間
        /// </summary>
        /// <param 圖層子物件-延遲時間名稱="DelayTimeName"></param>
        /// <returns></returns>
        public double GetCtrlDelay_Time(string DelayTimeName)
        {
            double Delay_Tim = axMMCTRLOBJ.GetCtrlDelayTime(DelayTimeName);
            return Delay_Tim;
        }
        #endregion

        #endregion



        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("是否執行關閉程式?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {


                MList_Log.Add("關閉程式");
                _mainStopFlag = true;
                if (_Using)
                {
                    P_F_EthernetIP.Close();
                    //pm
                    _PowerMeterConnect = false;
                    Thread.Sleep(100);
                    _powermeter.Close();
                    //MarkingMate雷射控制卡
                    axMMMark.Finish();
                    axMMStatus.Finish();
                    axMMEdit.Finish();
                    axMMCTRLOBJ.Finish();

                    int[] ax = { 0, 1, 2 };
                    this._Aerotech_Controller.Commands.Motion.Abort(ax);
                    this._Aerotech_Controller.Commands.Motion.Abort(0);
                    this._Aerotech_Controller.Commands.Motion.Abort(1);
                    this._Aerotech_Controller.Commands.Motion.Abort(2);

                    //if (_AZD_Controller.CheckPortOpen())
                    //{
                    //    _AZD_Controller.Stop();
                    //}

                    AZD_Disconnect();

                    //laser off emission
                    IPGLaser.Emission = false;
                    IPGLaserControl_Func();
                    Thread.Sleep(200);
                    IPGLaserControl.DisConnection();
                    //align light
                    AlignLight(_CoaxialLight, 0);
                    _CoaxialLight.Close();
                    AlignLight(_RingLight, 0);
                    _RingLight.Close();
                    //nozzle light
                    SetLight(0, 0, 0, 0);
                }

                End_UI_Updata_Thread();


                if (_Log_Thread != null)
                    _Log_Thread.Abort();

                if (_t_Keyence_Height_Rev != null)
                    _t_Keyence_Height_Rev.Abort();

                if (_t_Keyence_Height_Send != null)
                    _t_Keyence_Height_Send.Abort();

                if (_t_M_Electric != null)
                    _t_M_Electric.Abort();

                _Keyence_Height.Close();
                LasetSetPosSave();
                //ini save
                _ConfigSystem.TableTemp = (int)_TableTempSet;
                _ConfigSystem.TableHeater = chk_HeaterPower.Checked;
                _ConfigSystem.TableVacuum = IOCard_DOValue[0, 12]; //table vacuum
                _ConfigSystem.WriteIntoIniFile();
                IO_OutputControl("門鎖關");

                Thread_Stop();
                FlowStop();
            }
            else
            {
                e.Cancel = true;

            }

        }


        /// <summary>
        /// 讀取雷射校正位置
        /// </summary>
        private void LasetSetPosLoad()
        {
            txt_PizeoSetX.Text = _ConfigSystem.LaserSetPOS.X.ToString();
            txt_PizeoSetY.Text = _ConfigSystem.LaserSetPOS.Y.ToString();
            txt_PizeoSetZ.Text = _ConfigSystem.LaserSetPOS.Z.ToString();

            txt_LaserXY_Range.Text = _ConfigSystem._FESTO_PSensor_Port.ToString();
            txt_LaserXY_Range.Text = _ConfigSystem.LaserXY_Range.ToString();
            txt_LaserXY_Step.Text = _ConfigSystem.LaserXY_Step.ToString();
            txt_LaserZ_Range.Text = _ConfigSystem.LaserZ_Range.ToString();
            txt_LaserZ_Step.Text = _ConfigSystem.LaserZ_Step.ToString();
        }
        /// <summary>
        /// 儲存雷射校正位置
        /// </summary>
        private void LasetSetPosSave()
        {
            _ConfigSystem.LaserSetPOS.X = double.Parse(txt_PizeoSetX.Text);
            _ConfigSystem.LaserSetPOS.Y = double.Parse(txt_PizeoSetY.Text);
            _ConfigSystem.LaserSetPOS.Z = double.Parse(txt_PizeoSetZ.Text);
            _ConfigSystem.LaserXY_Range = int.Parse(txt_LaserXY_Range.Text);
            _ConfigSystem.LaserXY_Step = int.Parse(txt_LaserXY_Step.Text);
            _ConfigSystem.LaserZ_Range = int.Parse(txt_LaserZ_Range.Text);
            _ConfigSystem.LaserZ_Step = int.Parse(txt_LaserZ_Step.Text);
        }

        private Boolean motioning()
        {
            return false;
        }

        /// <summary>
        /// PCI 1762-輸出控制
        /// </summary>
        /// <param name="name"></param>
        private void IO_OutputControl(string name)
        {
            if (_Using)
            {
                switch (name)
                {
                    case "三色燈關":
                        IOCard_OutputRelay_OFF(0, 0);
                        IOCard_OutputRelay_OFF(0, 1);
                        IOCard_OutputRelay_OFF(0, 2);
                        break;

                    case "紅燈開":
                        IOCard_OutputRelay_ON(0, 0);
                        IOCard_OutputRelay_OFF(0, 1);
                        IOCard_OutputRelay_OFF(0, 2);
                        break;

                    case "黃燈開":
                        IOCard_OutputRelay_ON(0, 1);
                        IOCard_OutputRelay_OFF(0, 0);
                        IOCard_OutputRelay_OFF(0, 2);
                        break;

                    case "綠燈開":
                        IOCard_OutputRelay_ON(0, 2);
                        IOCard_OutputRelay_OFF(0, 1);
                        IOCard_OutputRelay_OFF(0, 0);
                        break;

                    case "蜂鳴器開":
                        IOCard_OutputRelay_ON(0, 3);
                        break;
                    case "蜂鳴器關":
                        IOCard_OutputRelay_OFF(0, 3);
                        break;

                    case "集塵機開":
                        IOCard_OutputRelay_ON(0, 6);
                        break;
                    case "集塵機關":
                        IOCard_OutputRelay_OFF(0, 6);
                        break;

                    case "門鎖開":
                        IOCard_OutputRelay_ON(0, 5);
                        break;
                    case "門鎖關":
                        IOCard_OutputRelay_OFF(0, 5);
                        break;

                    case "溫控器開":
                        IOCard_OutputRelay_ON(0, 8);
                        break; ;
                    case "溫控器關":
                        IOCard_OutputRelay_OFF(0, 8);
                        break; ;

                    case "過溫保護器AlarmRST_ON":
                        IOCard_OutputRelay_ON(0, 9);
                        break; ;
                    case "過溫保護器AlarmRST_OFF":
                        IOCard_OutputRelay_OFF(0, 9);
                        break; ;
                    //清潔真空
                    case "清潔真空開":
                        IOCard_OutputRelay_ON(0, 10);
                        IOCard_OutputRelay_OFF(0, 11);
                        break;
                    case "清潔真空關":
                        IOCard_OutputRelay_OFF(0, 10);
                        break;
                    //清潔抽氣
                    case "清潔真空破":
                        IOCard_OutputRelay_ON(0, 11);
                        break;
                    case "清潔真空破OFF":
                        IOCard_OutputRelay_OFF(0, 11);
                        break;

                    //抽氣 (吸 SB/ 教驗片)
                    case "抽氣開":
                        IOCard_OutputRelay_ON(0, 12);
                        IOCard_OutputRelay_OFF(0, 13);
                        break;
                    case "抽氣關":
                        IOCard_OutputRelay_OFF(0, 12);
                        break;
                    //清潔抽氣
                    case "抽氣破":
                        IOCard_OutputRelay_ON(0, 13);
                        break;
                    case "抽氣破OFF":
                        IOCard_OutputRelay_OFF(0, 13);
                        break;
                }
            }
        }

        /// <summary>
        /// 電磁閥控制 usb-io
        /// </summary>
        /// <param name="name"></param>
        private void ElectricValve(string name)
        {
            switch (name)
            {
                case "吸嘴開":
                    IOCard_OutputRelay_ON(1, 1);
                    IOCard_OutputRelay_OFF(1, 4);
                    break;
                case "吸嘴關":
                    IOCard_OutputRelay_OFF(1, 1);
                    IOCard_OutputRelay_ON(1, 4);
                    break;
                case "23層通道開":
                    IOCard_OutputRelay_ON(1, 2);
                    IOCard_OutputRelay_OFF(1, 5);
                    break;
                case "23層通道關":
                    IOCard_OutputRelay_OFF(1, 2);
                    IOCard_OutputRelay_ON(1, 5);
                    break;
                case "入球開":
                    IOCard_OutputRelay_ON(1, 3);
                    IOCard_OutputRelay_OFF(1, 6);
                    break;
                case "入球關":
                    IOCard_OutputRelay_OFF(1, 3);
                    IOCard_OutputRelay_ON(1, 6);
                    break;
                case "吸嘴&23層通道關":
                    IOCard_OutputRelay_OFF(1, 1);
                    IOCard_OutputRelay_ON(1, 4);

                    IOCard_OutputRelay_OFF(1, 2);
                    IOCard_OutputRelay_ON(1, 5);
                    break;
            }
        }

        private void btn_Motion_Acknowledge_ALL_Click(object sender, EventArgs e)
        {
            ReturnErr = "";
        }

        private void btn_SaveNossleCAL_Z_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行吸嘴校驗Z位置參數儲存。", tmpListStr);
                _msPosData.M_NozzleZ_X = Convert.ToDouble(txt_NozzleZ_CAL_X.Text);
                _msPosData.M_NozzleZ_Y = Convert.ToDouble(txt_NozzleZ_CAL_Y.Text);
                _msPosData.M_NozzleZ_Z = Convert.ToDouble(txt_NozzleZ_CAL_Z.Text);
                _msPosData.Save_Data();
                Setting_UI();
            }
        }

        private void btn_Save_LoadPos_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行入料位置參數儲存。", tmpListStr);
                _msPosData.M_Load_X = Convert.ToDouble(txt_LoadPos_X.Text);
                _msPosData.M_Load_Y = Convert.ToDouble(txt_LoadPos_Y.Text);
                _msPosData.M_Load_Z = Convert.ToDouble(txt_LoadPos_Z.Text);

                _msPosData.Save_Data();
                Setting_UI();
            }
        }

        private void btn_Save_UnloadPos_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行出料位置參數儲存。", tmpListStr);
                _msPosData.M_Unload_X = Convert.ToDouble(txt_UnloadPos_X.Text);
                _msPosData.M_Unload_Y = Convert.ToDouble(txt_UnloadPos_Y.Text);
                _msPosData.M_Unload_Z = Convert.ToDouble(txt_UnloadPos_Z.Text);

                _msPosData.Save_Data();
                Setting_UI();
            }
        }

        private void btn_SaveNossleCAL_XY_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行吸嘴校驗XY位置參數儲存。", tmpListStr);
                _msPosData.M_NozzleXY_X = Convert.ToDouble(txt_Nozzle_XY_CAL_X.Text);
                _msPosData.M_NozzleXY_Y = Convert.ToDouble(txt_Nozzle_XY_CAL_Y.Text);
                _msPosData.M_NozzleXY_Z = Convert.ToDouble(txt_Nozzle_XY_CAL_Z.Text);

                _msPosData.Save_Data();
                Setting_UI();
            }
        }

        private void btn_SaveLimitPos_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行極限位置參數儲存。", tmpListStr);
                _msPosData.M_PositiveLimit_X = Convert.ToDouble(txt_P_LimitX.Text);
                _msPosData.M_PositiveLimit_Y = Convert.ToDouble(txt_P_LimitY.Text);
                _msPosData.M_PositiveLimit_Z = Convert.ToDouble(txt_P_LimitZ.Text);

                _msPosData.M_NegativeLimit_X = Convert.ToDouble(txt_N_LimitX.Text);
                _msPosData.M_NegativeLimit_Y = Convert.ToDouble(txt_N_LimitY.Text);
                _msPosData.M_NegativeLimit_Z = Convert.ToDouble(txt_N_LimitZ.Text);

                _msPosData.Save_Data();
                Setting_UI();
            }

        }

        private void btn_SavePM_Pos_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行PM位置參數儲存。", tmpListStr);
                _msPosData.M_PowerMeter_X = Convert.ToDouble(txt_PM_X.Text);
                _msPosData.M_PowerMeter_Y = Convert.ToDouble(txt_PM_Y.Text);
                _msPosData.M_PowerMeter_Z = Convert.ToDouble(txt_PM_Z.Text);

                _msPosData.Save_Data();
                Setting_UI();
            }
        }


        private void btn_SavePumpOut_Pos_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行清潔位置參數儲存。", tmpListStr);
                _msPosData.M_PumpOut_X = Convert.ToDouble(txt_PumpOut_X.Text);
                _msPosData.M_PumpOut_Y = Convert.ToDouble(txt_PumpOut_Y.Text);
                _msPosData.M_PumpOut_Z = Convert.ToDouble(txt_PumpOut_Z.Text);

                _msPosData.Save_Data();
                Setting_UI();
            }
        }

        private void button_emer_Click(object sender, EventArgs e)
        {
            Thread_Stop();

            if (_Using)
            {
                StopMarking();

                if (_orientalConnected)
                {
                    //_AZD_Controller.Stop();
                    Motion.mAcm_AxStopDec(m_Axishand[0]);
                }

                int[] ax = { 0, 1, 2 };
                this._Aerotech_Controller.Commands.Motion.Abort(ax);
                this._Aerotech_Controller.Commands.Motion.Abort(0);
                this._Aerotech_Controller.Commands.Motion.Abort(1);
                this._Aerotech_Controller.Commands.Motion.Abort(2);
            }
        }

        private void Thread_Stop()
        {
            _EMS_Stop = true;

            //一次雷射
            _t_M_SB_Laser_flag = 0;
            if (_t_M_SB_Laser != null)
                _t_M_SB_Laser.Abort();
            //二皆雷射
            _t_M_SB_Laser_flag2 = 0;
            if (_t_M_SB_Laser2 != null)
                _t_M_SB_Laser2.Abort();

            _t_M_AutoFocus_flag = 0;
            _t_M_AF_Zoffset = 0;
            if (_t_M_AutoFocus != null)
                _t_M_AutoFocus.Abort();

            _t_M_MoveToLaser_flag = 0;
            if (_t_M_MoveToLaser != null)
                _t_M_MoveToLaser.Abort();



            if (_t_M_Electric != null)
                _t_M_Electric.Abort();

            if (_mainFlowThread != null)
                _mainFlowThread.Abort();
        }

        private void button48_Click(object sender, EventArgs e)
        {
            ElectricValve("入球開");
        }

        private void button49_Click(object sender, EventArgs e)
        {
            ElectricValve("入球關");
        }


        private void txt_M_MoveToLaser_OffsetZ_TextChanged(object sender, EventArgs e)
        {
            lb_M_MoveToLaser_RelZ.Text = "絕對Z: " + (Convert.ToDouble(_msPosData.M_H_LaserZ) - _t_M_AF_Zoffset + double.Parse(txt_M_MoveToLaser_OffsetZ.Text)).ToString();
        }



        #region auto motion flow  
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        /// <summary>
        /// 設備流程Thread
        /// </summary>
        private Thread _mainFlowThread;
        /// <summary>
        /// Main thread flag
        /// </summary>
        private volatile bool _mainStopFlag = false;
        /// <summary>
        /// mem ST 計算完成
        /// </summary>
        private bool _stCal_OK = false;
        /// <summary>
        /// mem ST 測高完成
        /// </summary>
        private bool _stMHeight_OK = false;
        /// <summary>
        /// st座標檔載入
        /// </summary>
        private bool _fileLoad = false;

        private bool _ST_Use
        {
            get
            {
                foreach (Define.st_ST_Data tmp in PublicData.ST_Data)
                {
                    if (tmp.Used)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 主流程啟動
        /// </summary>
        private void MainFlowStart()
        {
            _mainStopFlag = false;
            _mainFlowThread = new Thread(MainFlow);
            _mainFlowThread.IsBackground = false;
            _mainFlowThread.Start();

        }

        enum enumEQP_Status
        {
            IDLE = 0,
            AUTO = 1,
            DOWN = 2,
            MANU = 3
        }

        /// <summary>
        /// 自動流程 Step
        /// </summary>
        enum enumAUTO_FlowStep
        {
            AutoWait = 0,
            Loading,
            Loading_2,
            ST_Grab,
            ST_ThickMeasure,
            ST_SB_Mounting,
            ST_WorkJudge,
            Unloading,
            Unloading_2,
            AutoFinish


        }


        /// <summary>
        /// ST 取像 Step
        /// </summary>
        enum enumAlignFlowStep
        {
            GrabStepWait = 200,
            GrabStep1 = 201,
            GrabStep2 = 202,
            GrabStep3 = 203,
            GrabStep4 = 204,
            GrabStep5 = 205,
            GrabStep6 = 206,
            GrabStep7 = 207,
            GrabStep8 = 208,
            GrabStep9 = 209,
        }

        /// <summary>
        /// SB 取像 Step
        /// </summary>
        enum enumSB_AlignFlowStep
        {
            GrabStepWait = 240,
            GrabStep1 = 241,
            GrabStep2 = 242,
            GrabStep3 = 243,
            GrabStep4 = 244,
            GrabStep5 = 245,
            GrabStep6 = 246,
            GrabStep7 = 247,
            GrabStep8 = 248,
            GrabStep9 = 249,
            GrabStep10 = 250,
            GrabStep11 = 251,
            GrabStep12 = 252,
            GrabStep13 = 253,
            GrabStep14 = 254,
            GrabStep15 = 255,
        }

        /// <summary>
        /// SB 測高 Step
        /// </summary>
        enum enumSB_H_MeasureFlowStep
        {
            H_MeasureStepWait = 340,
            H_MeasureStep1 = 341,
            H_MeasureStep2 = 342,
            H_MeasureStep3 = 343,
            H_MeasureStep4 = 344,
            H_MeasureStep5 = 345,
            H_MeasureStep6 = 346,
            H_MeasureStep7 = 347,
            H_MeasureStep8 = 348,
            H_MeasureStep9 = 349,
            H_MeasureStep10 = 350,
            H_MeasureStep11 = 351,
            H_MeasureStep12 = 352,
            H_MeasureStep13 = 353,
            H_MeasureStep14 = 354,
            H_MeasureStep15 = 355,
        }
        /// <summary>
        /// ST 測高 Step
        /// </summary>
        enum enumH_MeasureFlowStep
        {
            H_MeasureStepWait = 300,
            H_MeasureStep1 = 301,
            H_MeasureStep2 = 302,
            H_MeasureStep3 = 303,
            H_MeasureStep4 = 304,
            H_MeasureStep5 = 305,
            H_MeasureStep6 = 306,
            H_MeasureStep7 = 307,
            H_MeasureStep8 = 308,
            H_MeasureStep9 = 309,
        }

        /// <summary>
        /// disk run Step
        /// </summary>
        enum enumDiskRunFlowStep
        {
            DiskRunStepWait = 400,
            DiskRunStep1 = 401,
            DiskRunStep2 = 402,
            DiskRunStep3 = 403,
            DiskRunStep4 = 404,
            DiskRunStep5 = 405,
            DiskRunStep6 = 406,
            DiskRunStep7 = 407,
            DiskRunStep8 = 408,
            DiskRunStep9 = 409,
        }

        /// <summary>
        /// 植球step
        /// </summary>
        enum enumBallMountFlowStep
        {
            MountStepWait = 500,
            MountStep1,
            MountStep2,
            MountStep31,
            MountStep3,
            MountStep4,
            MountStep5,
            MountStep6,
            MountStep7,
            MountStep8,
            MountStep9,
        }
        /// <summary>
        /// 校正噴嘴 XY step
        /// </summary>
        enum enumNozzleXY_AlignFlowStep
        {
            NozzleXY_Wait = 600,
            NozzleXY_Step1,
            NozzleXY_Step2,
            NozzleXY_Step3,
            NozzleXY_Step4,
            NozzleXY_Step5,
            NozzleXY_Step6,
            NozzleXY_Step7,
            NozzleXY_Step8,
            NozzleXY_Step9,
        }

        /// <summary>
        /// 校正噴嘴 Z step
        /// </summary>
        enum enumNozzleZ_AlignFlowStep
        {
            NozzleZ_Wait = 620,
            NozzleZ_Step1,
            NozzleZ_Step2,
            NozzleZ_Step3,
            NozzleZ_Step4,
            NozzleZ_Step5,
            NozzleZ_Step6,
            NozzleZ_Step7,
            NozzleZ_Step8,
            NozzleZ_Step9,
        }

        /// <summary>
        /// 噴嘴清潔流程
        /// </summary>
        enum enumNozzleCleanFlowStep
        {
            NozzleClean_Wait = 660,
            NozzleClean_Step1,
            NozzleClean_Step2,
            NozzleClean_Step3,
            NozzleClean_Step4,
            NozzleClean_Step5,
            NozzleClean_Step6,
            NozzleClean_Step7,
            NozzleClean_Step8,
            NozzleClean_Step9,
        }

        enum enumPowerMeterFlowStep
        {
            PowerMeter_Wait = 700,
            PowerMeter_Step1,
            PowerMeter_Step2,
            PowerMeter_Step3,
            PowerMeter_Step4,
            PowerMeter_Step5,
            PowerMeter_Step6,
            PowerMeter_Step7,
            PowerMeter_Step8,
            PowerMeter_Step9,

        }

        //分離盤對位
        enum enumDisk_AlignFlowStep
        {
            DiskAlign_Wait = 740,
            DiskAlign_Step1,
            DiskAlign_Step2,
            DiskAlign_Step3,
            DiskAlign_Step4,
            DiskAlign_Step5,
            DiskAlign_Step6,
            DiskAlign_Step7,
            DiskAlign_Step8,
            DiskAlign_Step9,
        }

        /// <summary>
        /// 雷射校正 step
        /// </summary>
        enum enumLaser_AlignFlowStep
        {
            LaserAlign_Wait = 760,
            LaserAlign_Step1,
            LaserAlign_Step2,
            LaserAlign_Step3,
            LaserAlign_Step4,
            LaserAlign_Step5,
            LaserAlign_StepEnd,

            LaserAlign_XY_Step1,
            LaserAlign_XY_Step2,
            LaserAlign_XY_Step3,
            LaserAlign_XY_Step4,

            LaserAlign_Z_Step1,
            LaserAlign_Z_Step2,
            LaserAlign_Z_Step3,
            LaserAlign_Z_Step4,
        }

        private void B_MoveOB_AutoStart_Click(object sender, EventArgs e)
        {

        }



        private void btn_AUTO_Stop_Click(object sender, EventArgs e)
        {

            FlowStop();
        }

        private void FlowStep_INIT()
        {
            //ST
            _autoST_FlowStep = enumAUTO_FlowStep.AutoWait;
            _ST_Align_FlowStep = enumAlignFlowStep.GrabStepWait;
            _ST_H_Measure_FlowStep = enumH_MeasureFlowStep.H_MeasureStepWait;
            _ST_DiskRun_FlowStep = enumDiskRunFlowStep.DiskRunStepWait;
            _STballMount_FlowStep = enumBallMountFlowStep.MountStepWait;
            //Calibration
            _NozzleXY_AlignFlowStep = enumNozzleXY_AlignFlowStep.NozzleXY_Wait;
            _NozzleZ_AlignFlowStep = enumNozzleZ_AlignFlowStep.NozzleZ_Wait;
            _DiskAlignFlowStep = enumDisk_AlignFlowStep.DiskAlign_Wait;
            _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Wait;
            //tool 
            _NozzleCleanFlowStep = enumNozzleCleanFlowStep.NozzleClean_Wait;
            _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Wait;
            //sb
            _autoSB_FlowStep = enumAUTO_FlowStep.AutoWait;
            _SB_AlignFlowStep = enumSB_AlignFlowStep.GrabStepWait;
            _SB_H_MeasureFlowStep = enumSB_H_MeasureFlowStep.H_MeasureStepWait;
            _SBballMount_FlowStep = enumBallMountFlowStep.MountStepWait;

        }

        /// <summary>
        /// 判定流程是否允許
        /// </summary>
        /// <returns></returns>
        private bool FlowAllow()
        {
            bool result = false;

            if (DoorPass)
            {
                if (_EQP_Status == enumEQP_Status.IDLE && !CycleFlag && Aerotech_Home && DeviceFlag)
                {
                    return true;
                }
                else
                {

                    if (_EQP_Status != enumEQP_Status.IDLE || CycleFlag)
                    {
                        MessageBox.Show("當前狀態尚未停止或異常!", "警告");
                    }
                    else if (!Aerotech_Home)
                    {
                        MessageBox.Show("設備尚未原點復歸!", "警告");
                    }
                    else if (!DeviceFlag)
                    {
                        MessageBox.Show("確認所有裝置連線成功!", "警告");
                    }
                }
            }
            else
            {
                if (_EQP_Status == enumEQP_Status.IDLE && !CycleFlag && Aerotech_Home && DeviceFlag && !IO_InputData.DoorOpen)
                {
                    return true;
                }
                else
                {
                    if (IO_InputData.DoorOpen)
                    {
                        MessageBox.Show("請關閉安全門!", "警告");
                    }
                    else if (_EQP_Status != enumEQP_Status.IDLE || CycleFlag)
                    {
                        MessageBox.Show("當前狀態尚未停止或異常!", "警告");
                    }
                    else if (!Aerotech_Home)
                    {
                        MessageBox.Show("設備尚未原點復歸!", "警告");
                    }
                    else if (!DeviceFlag)
                    {
                        MessageBox.Show("確認所有裝置連線成功!", "警告");
                    }
                }
            }


            return result;
        }


        private void btn_AUTO_Start_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("請確認ST狀態是否為[尚未]植球狀態，確認後請點選同意並執行自動流程", "警告", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                // 需補上 home / alarm判定

                if (FlowAllow())
                {
                    if (_fileLoad && _ST_Use)
                    {
                        for (int i = 0; i < _recipeData.Panel.ST_Num; i++)
                        {
                            PublicData.ST_Data[i].Finish = false;
                        }

                        if (_mainFlowThread == null || !_mainFlowThread.IsAlive)
                        {
                            MainFlowStart();
                        }
                        ChangeCVX_Run();
                        _EQP_Status = enumEQP_Status.AUTO;
                        _autoST_FlowStep = enumAUTO_FlowStep.Loading;
                        LogMsgAdd(MList_Log, lb_HistoryList, "執行[ST]自動流程", tmpListStr);
                    }
                    else if (!_fileLoad)
                    {
                        MessageBox.Show("尚未載入座標檔!", "警告");
                    }
                    else if (!_ST_Use)
                    {
                        MessageBox.Show("未設定 ST 生產資訊!", "警告");
                    }
                }

            }
        }

        enumEQP_Status _EQP_Status;
        //ST
        enumAUTO_FlowStep _autoST_FlowStep;
        enumAlignFlowStep _ST_Align_FlowStep;
        enumH_MeasureFlowStep _ST_H_Measure_FlowStep;
        enumDiskRunFlowStep _ST_DiskRun_FlowStep;
        enumBallMountFlowStep _STballMount_FlowStep;
        //calibration
        enumNozzleXY_AlignFlowStep _NozzleXY_AlignFlowStep;
        enumNozzleZ_AlignFlowStep _NozzleZ_AlignFlowStep;
        enumDisk_AlignFlowStep _DiskAlignFlowStep;
        enumLaser_AlignFlowStep _LaserAlignFlowStep;
        //tool 
        enumNozzleCleanFlowStep _NozzleCleanFlowStep;
        enumPowerMeterFlowStep _PowerMeterFlowStep;
        //sb 
        enumAUTO_FlowStep _autoSB_FlowStep;
        enumSB_AlignFlowStep _SB_AlignFlowStep;
        enumSB_H_MeasureFlowStep _SB_H_MeasureFlowStep;
        enumBallMountFlowStep _SBballMount_FlowStep;

        /// <summary>
        /// 獨立流程while Flag
        /// </summary>
        private volatile bool _RunFlag = false;
        // <summary>
        /// 同動流程while Flag
        /// </summary>
        private volatile bool _DiskRunFlag = false;
        /// <summary>
        /// 同動流程while Flag
        /// </summary>
        private volatile bool _LaserRunFlag = false;
        /// <summary>
        /// 清潔 flag
        /// </summary>
        private volatile bool _ClearFlag = false;

        /// <summary>
        /// 分離盤資訊
        /// </summary>
        public RotateDiskData _rotateDiskData = new RotateDiskData();

        /// <summary>
        /// 流程flag
        /// </summary>
        public bool CycleFlag
        {
            get
            {
                if (_RunFlag || _DiskRunFlag || _LaserRunFlag || _ClearFlag || !_msLoadFlag || _ballOutRun)
                {
                    return true;
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// 硬體flag
        /// </summary>
        public bool DeviceFlag
        {
            get
            {
                if (_AxMarkConnected && _aerotechConnected && _orientalConnected && _CL_Connected && _CVX_Connected && _IO_Connected && _heaterConnnected && _IO_LinkConnect && _PowerMeterConnect && _IPG_LaserConnected
                    && _PizeoConnect)
                {
                    return true;
                }
                else
                    return false;
            }
        }

        private void btn_SaveCleanParam_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行雷射清潔參數儲存。", tmpListStr);
                _recipeData.SYSParam.CleanLaserPower = Convert.ToDouble(txt_CleanLaserPower.Text);
                _recipeData.SYSParam.CleanLaserTime = Convert.ToDouble(txt_CleanLaserTime.Text);
                _recipeData.SYSParam.CleanAirTime = Convert.ToDouble(txt_CleanAirTime.Text);
                _recipeData.SYSParam.CleanAirValve = Convert.ToInt32(txt_CleanAirValve.Text);
                _recipeData.SYSParam.CleanVacuumTime = Convert.ToDouble(txt_CleanVacuumTime.Text);

                _recipeData.Save_Data(RecipeFileName);
                Setting_UI();
            }
        }

        int thickPosCount = 0;
        int grabPosCount = 0;
        POS_Data[] grabPoint = new POS_Data[PublicData.GRAB_NUM];
        POS_Data[] baseGrabPoint = new POS_Data[PublicData.GRAB_NUM];

        private void btn_Save_NozzleOffset_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行吸嘴校驗參數儲存。", tmpListStr);
                //_recipeData.SB.NozzleBase_X = Convert.ToDouble(txt_NozzleBase_X.Text);
                //_recipeData.SB.NozzleBase_Y = Convert.ToDouble(txt_NozzleBase_Y.Text);
                //_recipeData.SB.NozzleBase_Z = Convert.ToDouble(txt_NozzleBase_Z.Text);
                _recipeData.SB.NozzleNow_X = Convert.ToDouble(txt_NozzleNow_X.Text);
                _recipeData.SB.NozzleNow_Y = Convert.ToDouble(txt_NozzleNow_Y.Text);
                _recipeData.SB.NozzleNow_Z = Convert.ToDouble(txt_NozzleNow_Z.Text);

                _recipeData.Save_Data(RecipeFileName);
                Setting_UI();
            }
        }
        /// <summary>
        /// 測高點  P1左下 P2左上 P3右下 P4右上
        /// </summary>
        POS_Data[] thickPoint = new POS_Data[PublicData.HMeasure_NUM];

        /// <summary>
        /// 物件init
        /// </summary>
        void ComponentInit()
        {
            for (int i = 0; i < PublicData.GRAB_NUM; i++)
            {
                baseGrabPoint[i] = new POS_Data();
                grabPoint[i] = new POS_Data();
            }

            for (int i = 0; i < PublicData.HMeasure_NUM; i++)
                thickPoint[i] = new POS_Data();

            for (int i = 0; i < PublicData.HMeasure_NUM; i++)
                SB_ThickPoint[i] = new POS_Data();
        }

        private void btn_NozzleXY_Flow_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行吸嘴 XY 校驗流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _NozzleXY_AlignFlowStep = enumNozzleXY_AlignFlowStep.NozzleXY_Step1;
                if (FlowAllow())
                {
                    Task.Run(Nozzle_XY_AlignFlow);
                    LogMsgAdd(MList_Log, lb_HistoryList, "執行吸嘴 XY 校驗流程。", tmpListStr);
                }
            }

        }

        private void btn_NozzleZ_Flow_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行吸嘴 Z 校驗流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _NozzleZ_AlignFlowStep = enumNozzleZ_AlignFlowStep.NozzleZ_Step1;

                if (FlowAllow())
                {
                    Task.Run(Nozzle_Z_AlignFlow);
                    LogMsgAdd(MList_Log, lb_HistoryList, "執行吸嘴 Z 校驗流程。", tmpListStr);
                }

            }
        }

        /// <summary>
        /// 當前可生產 ST_No
        /// </summary>
        private int NowST_NO
        {
            get
            {
                for (int i = 0; i < _recipeData.Panel.ST_Num; i++)
                {
                    if (PublicData.ST_Data[i].Used && !PublicData.ST_Data[i].Finish)
                    {
                        return i + 1;
                    }
                }
                return 0;
            }
        }
        private int NowPadNO = 1;
        private int NowBallNO = 1;

        /// <summary>
		/// 生產完成
		/// </summary>
		private bool ALL_ST_Finish
        {
            get
            {
                int used = 0, finish = 0;
                for (int i = 0; i < _recipeData.Panel.ST_Num; i++)
                {
                    if (PublicData.ST_Data[i].Used)
                        used++;

                    if (PublicData.ST_Data[i].Finish)
                        finish++;
                }

                if (used == finish)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// 剩餘ST 生產數
        /// </summary>
        private int RemainST_NUM
        {
            get
            {
                int count = 0;

                for (int i = 0; i < _recipeData.Panel.ST_Num; i++)
                {
                    if (PublicData.ST_Data[i].Used && !PublicData.ST_Data[i].Finish)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        private void btn_NozzleNowXY_Set_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否設定當前吸嘴 XY 校驗值? \n後續請至植球設定[XYZ補償值]頁面儲存參數。", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                txt_NozzleNow_X.Text = txt_NozzleAlign_X.Text;
                txt_NozzleNow_Y.Text = txt_NozzleAlign_Y.Text;
                btn_Save_NozzleOffset_Click(null, null);
                LogMsgAdd(MList_Log, lb_HistoryList, "設定吸嘴 XY 校驗值。", tmpListStr);
            }
        }

        private void btn_NozzleNowZ_Set_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否設定當前吸嘴 Z 校驗值? \n後續請至植球設定[XYZ補償值]頁面儲存參數。", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                txt_NozzleNow_Z.Text = txt_NozzleAlign_Z.Text;
                btn_Save_NozzleOffset_Click(null, null);
                LogMsgAdd(MList_Log, lb_HistoryList, "設定吸嘴 Z 校驗值。", tmpListStr);
            }
        }


        private void GrabPointMove(int stNo, int grabNo)
        {
            int step = 1;
            POS_Data tmpPos = new POS_Data();
            TJJS_Point outPoint = new TJJS_Point();
            tmpPos.X = _recipeData.Panel.ST_CenterPos[stNo - 1].X - Pos.Mark_Pos[grabNo - 1].X;
            tmpPos.Y = _recipeData.Panel.ST_CenterPos[stNo - 1].Y - Pos.Mark_Pos[grabNo - 1].Y;
            tmpPos.Z = _msPosData.M_H_VisionZ - PublicData.ST_HeightZ;
            TJJS_Point inPoint = new TJJS_Point(tmpPos.X, tmpPos.Y);

            tmpPos.X = _recipeData.Panel.ST_CenterPos[stNo - 1].X - Pos.Mark_Pos[0].X;
            tmpPos.Y = _recipeData.Panel.ST_CenterPos[stNo - 1].Y - Pos.Mark_Pos[0].Y;

            TJJS_Point basePoint = new TJJS_Point(tmpPos.X, tmpPos.Y);

            //if (_cal_OK)
            {
                PointMove(inPoint, basePoint, PublicData.ST_V_CalData, ref outPoint);
            }

            _RunFlag = true;
            while (_RunFlag)
            {
                switch (step)
                {
                    case 1:
                        if (StageABS_Move(outPoint.X, outPoint.Y, _msPosData.M_Wait_Z, false, false, true))
                            step++;
                        break;

                    case 2:
                        if (StageABS_Move(outPoint.X, outPoint.Y, _msPosData.M_Wait_Z, true, true, false))
                            step++;
                        break;

                    case 3:
                        if (StageABS_Move(outPoint.X, outPoint.Y, tmpPos.Z, true, true, true))
                            step++;
                        break;

                    case 4:
                        _RunFlag = false;
                        _EQP_Status = enumEQP_Status.IDLE;
                        break;
                }
                Thread.Sleep(10);
            }
        }

        private void MHeightPointMove(int stNo, int hightNo)
        {
            int step = 1;
            POS_Data tmpPos = new POS_Data();
            TJJS_Point outPoint = new TJJS_Point();
            tmpPos.X = _recipeData.Panel.ST_CenterPos[stNo - 1].X - Pos.Height_Pos[hightNo - 1].X;
            tmpPos.Y = _recipeData.Panel.ST_CenterPos[stNo - 1].Y - Pos.Height_Pos[hightNo - 1].Y;
            tmpPos.Z = _msPosData.M_H_VisionZ - PublicData.ST_HeightZ;
            TJJS_Point inPoint = new TJJS_Point(tmpPos.X, tmpPos.Y);

            tmpPos.X = _recipeData.Panel.ST_CenterPos[stNo - 1].X - Pos.Mark_Pos[0].X;
            tmpPos.Y = _recipeData.Panel.ST_CenterPos[stNo - 1].Y - Pos.Mark_Pos[0].Y;

            TJJS_Point basePoint = new TJJS_Point(tmpPos.X, tmpPos.Y);

            //if (_cal_OK)
            {
                PointMove(inPoint, basePoint, PublicData.ST_V_CalData, ref outPoint);
            }

            _RunFlag = true;
            while (_RunFlag)
            {
                switch (step)
                {
                    case 1:
                        if (StageABS_Move(outPoint.X, outPoint.Y, _msPosData.M_Wait_Z, false, false, true))
                            step++;
                        break;

                    case 2:
                        if (StageABS_Move(outPoint.X, outPoint.Y, _msPosData.M_Wait_Z, true, true, true))
                            step++;
                        break;

                    case 3:
                        if (StageABS_Move(outPoint.X, outPoint.Y, tmpPos.Z, true, true, true))
                            step++;
                        break;

                    case 4:
                        _RunFlag = false;
                        _EQP_Status = enumEQP_Status.IDLE;
                        step = 0;
                        break;
                }
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// memory st 單點取像位置移動
        /// </summary>
        /// <param name="stNo"></param>
        /// <param name="padNo"></param>
        /// <param name="ballNo"></param>
        private void ST_PointMove(int stNo, int padNo, int ballNo)
        {
            int step = 1;
            POS_Data tmpPos = new POS_Data();
            TJJS_Point outPoint = new TJJS_Point();
            if (CalST_PadMap(stNo))
            {

                if (Cal_SolderBallMap())
                {

                    outPoint.X = _BondData.BondPad[padNo - 1].SolderBall[ballNo - 1].X;
                    outPoint.Y = _BondData.BondPad[padNo - 1].SolderBall[ballNo - 1].Y;
                    tmpPos.Z = _msPosData.M_H_VisionZ - PublicData.ST_HeightZ;

                    _RunFlag = true;
                    while (_RunFlag)
                    {
                        switch (step)
                        {
                            case 1:
                                if (StageABS_Move(outPoint.X, outPoint.Y, _msPosData.M_Wait_Z, false, false, true))
                                    step++;
                                break;

                            case 2:
                                if (StageABS_Move(outPoint.X, outPoint.Y, _msPosData.M_Wait_Z, true, true, true))
                                    step++;
                                break;

                            case 3:
                                if (StageABS_Move(outPoint.X, outPoint.Y, tmpPos.Z, true, true, true))
                                    step++;
                                break;

                            case 4:
                                step = 0;
                                _RunFlag = false;
                                _EQP_Status = enumEQP_Status.IDLE;
                                break;
                        }
                        Thread.Sleep(10);
                    }
                }
            }
        }


        int _ballOutTotalCount = 0;
        int _ballOutCount = 0;
        bool _ballOutRun = false;
        /// <summary>
        /// 出球測試
        /// </summary>
        /// <param name="rotateDegree"></param>
        /// <param name="airTime"></param>
        /// <param name="delay"></param>
        /// <param name="ballCount"></param>
        private void BallOutTestFlow(double rotateDegree, int airTime, int delay)
        {
            int step = 1;
            int degreeIndex = 1;
            _ballOutCount = 0;
            _ballOutRun = true;
            _EQP_Status = enumEQP_Status.MANU;
            P_F_EthernetIP.DataUpdataOk = false;
            Stopwatch tmp1 = new Stopwatch();
            try
            {
                while (_ballOutRun)
                {

                    switch (step)
                    {
                        case 1:
                            if (P_F_EthernetIP.DataUpdataOk)
                            {
                                if (SB_Air_Check(true)) //有球強制結束
                                {
                                    _ballOutRun = false;
                                    Invoke(new dele_msgShow(ErrMSG_Show), "噴嘴已有球，停止供球流程!");
                                    break;
                                }
                                else
                                {
                                    if (AZD_Rdy && !AZD_Motion)
                                    {
                                        tmp1.Restart();

                                        if (AZD_RotateNext(rotateDegree))
                                        {
                                            step++;
                                        }

                                        tmp1.Stop();
                                        double tt = tmp1.ElapsedMilliseconds;
                                        Console.WriteLine("TT :" + tt);
                                    }
                                }

                            }

                            break;
                        case 2:
                            //ElectricValve("23層通道開");
                            Thread.Sleep(airTime);
                            //ElectricValve("23層通道關");
                            step++;
                            break;

                        case 3:
                            Thread.Sleep(delay);
                            step++;
                            break;

                        case 4:
                            _ballOutCount++;
                            Console.WriteLine("_ballOutCount :" + _ballOutCount);
                            if (_ballOutCount >= _ballOutTotalCount)
                            {
                                step = 0;
                                _ballOutRun = false;
                                _EQP_Status = enumEQP_Status.IDLE;
                                break;
                            }

                            degreeIndex++;
                            if (degreeIndex == 9)
                                degreeIndex = 1;
                            step = 1;
                            break;
                    }


                    Thread.Sleep(10);
                }

            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// PowerMeter持續出光測試流程
        /// </summary>
        /// <param name="rotateDegree"></param>
        /// <param name="airTime"></param>
        /// <param name="delay"></param>
        /// <param name="ballCount"></param>
        private void PowerMeterTestFlow(int delayT)
        {
            _RunFlag = true;

            int errC = 0;

            try
            {
                while (_RunFlag)
                {
                    switch (_PowerMeterFlowStep)
                    {
                        case enumPowerMeterFlowStep.PowerMeter_Step1:
                            if (Set_PMLaserData())
                                _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step2;
                            else
                            {
                                errC++;
                                if (errC > 50)
                                    throw new ArgumentException("功率更新超時");
                            }
                            break;


                        case enumPowerMeterFlowStep.PowerMeter_Step2:

                            //雷射開
                            if (StartMarking())
                            {
                                LaserStatus_Emission = true;
                                Invoke(new updatalaserstauts(updatalaser));//2024-05-24                                                              
                                _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step3;
                            }
                            else
                            {
                                _EQP_Status = enumEQP_Status.DOWN;
                                LogMsgAdd(Error_Log, lb_ErrorList, "雷射出光失敗", tmpErrStr);
                                Invoke(new dele_msgShow(ErrMSG_Show), "雷射出光失敗");
                                _RunFlag = false;
                            }

                            break;

                        case enumPowerMeterFlowStep.PowerMeter_Step3:
                            //雷射雕刻完成
                            if (!LaserStatus_Emission)
                            {
                                Thread.Sleep(delayT);
                                _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step2;
                            }

                            break;

                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                _EQP_Status = enumEQP_Status.DOWN;
                Error_Log.Add($"[PowerMeterFlow Error]: {ex.ToString()}");
                _RunFlag = false;

            }
        }



        int _arrayOutTotalCount = 0;
        int _arrayOutCount = 0;
        bool _arrayOutRun = false;
        /// <summary>
        /// array植球測試
        /// </summary>     
        private async void ArrayTestFlow()
        {
            int step = 1;
            _arrayOutCount = 0;
            _arrayOutRun = true;
            int x_count = 0, y_count = 0, retryCount = 0;
            double xPos = 0, yPos = 0, zPos = 0;
            string str = "";

            try
            {
                while (_arrayOutRun)
                {
                    switch (step)
                    {
                        case 1:
                            thickPosCount = 0;
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                                step = 2;
                            break;

                        case 2:
                            if (StageABS_Move(thickPoint[0].TX, thickPoint[0].TY, _msPosData.M_Wait_Z, true, true, true))
                                step = 3;
                            break;

                        //取像點測高
                        case 3:
                            if (thickPosCount >= PublicData.HMeasure_NUM)
                            {
                                step = 5;
                            }
                            else
                            {
                                if (StageABS_Move(thickPoint[thickPosCount].TX, thickPoint[thickPosCount].TY, thickPoint[thickPosCount].TZ, true, true, true))
                                {
                                    step = 4;
                                    Thread.Sleep(PublicData.MHeightDelay);
                                }

                            }
                            break;

                        case 4:
                            if (await TaskRun(ConFocusRead(), TimeOutTask(TimeSpan.FromSeconds(10))))
                            {
                                thickPoint[thickPosCount].MZ = double.Parse(KeyenceHeight_Value);
                                thickPosCount++;
                                step = 3;
                            }
                            else
                            {
                                _EQP_Status = enumEQP_Status.DOWN;
                                Invoke(new dele_msgShow(ErrMSG_Show), "測高異常");
                                _arrayOutRun = false;
                            }
                            break;

                        //測高平面計算
                        case 5:
                            //測高判定
                            if (!HeightJudge(thickPoint[0].MZ, thickPoint[1].MZ, thickPoint[2].MZ, thickPoint[3].MZ, _recipeData.SYSParam.FourP_HeightLimit))
                            {
                                LogMsgAdd(Error_Log, lb_ErrorList, $"測高超過限制量[{_recipeData.SYSParam.FourP_HeightLimit}]um!", tmpErrStr);
                                Invoke(new dele_msgShow(ErrMSG_Show), $"測高超過限制量[{_recipeData.SYSParam.FourP_HeightLimit}]um!");
                                _EQP_Status = enumEQP_Status.DOWN;
                                _arrayOutRun = false;
                            }


                            if (SB_ArrayZ_Cal(_arrayPitch, _array_CountX, _array_CountY, ref ArrayPos))
                                step = 6;
                            break;

                        case 6:
                            if (_arrayOutCount >= _arrayOutTotalCount)
                            {
                                step = 10;
                            }
                            else
                            {
                                step = 71;
                            }
                            break;

                        //move                        
                        case 71:
                            xPos = ArrayPos[y_count, x_count].LX;
                            yPos = ArrayPos[y_count, x_count].LY;
                            zPos = ArrayPos[y_count, x_count].LZ;

                            if (StageABS_Move(xPos, yPos, zPos, true, true, false))
                            {
                                step = 7;
                            }
                            break;
                        //move
                        case 7:
                            xPos = ArrayPos[y_count, x_count].LX;
                            yPos = ArrayPos[y_count, x_count].LY;
                            zPos = ArrayPos[y_count, x_count].LZ;

                            if (StageABS_Move(xPos, yPos, zPos, true, true, true)) //供球成功
                            {
                                if (await DiskReadyRun())//供球成功
                                    step = 8;
                            }
                            break;

                        case 8:     //signal walt
                            if (await LaserEmission(_msPosData.SENSE_PadType))  //出球成功
                            {
                                x_count++;
                                if (x_count >= _array_CountX)
                                {
                                    y_count++;
                                    x_count = 0;
                                }
                                _arrayOutCount++;
                                step = 61;
                            }
                            else
                            {
                                if (retryCount >= _recipeData.SYSParam.ClearRetry)
                                {
                                    str = $"Array[{y_count + 1}] [{x_count + 1}] 出球失敗，超過[{ _recipeData.SYSParam.ClearRetry}]次清潔流程.";
                                    LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                                    _EQP_Status = enumEQP_Status.DOWN;
                                    Invoke(new dele_msgShow(ErrMSG_Show), str);
                                    _arrayOutRun = false;
                                }
                                else //出球失敗 進行清潔
                                {
                                    _NozzleCleanFlowStep = enumNozzleCleanFlowStep.NozzleClean_Step1;
                                    str = $"Array[{y_count + 1}] [{x_count + 1}] 出球失敗，進行第[{retryCount + 1}]次清潔流程.";
                                    LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                                    if (await NozzleCleanFlow())
                                    {
                                        step = 71;
                                    }
                                    else
                                    {
                                        str = $"清潔流程失敗.";
                                        LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                                        _EQP_Status = enumEQP_Status.DOWN;
                                        Invoke(new dele_msgShow(ErrMSG_Show), str);
                                        _arrayOutRun = false;

                                    }
                                }
                                retryCount++;
                            }
                            break;

                        case 61:
                            if (_recipeData.SB.SB_MountMoveZ_Flag) //移動z軸上升
                            {
                                if (StageABS_Move(xPos, yPos, zPos - _recipeData.SB.SB_MountMoveZ, false, false, true)) //上移2mm 避開元件
                                {
                                    NowNozzle_MAX_Pressure = 0;
                                    NowNozzle_MIN_Pressure = NowNozzlePressure;
                                    step = 6;
                                }
                            }
                            else
                            {
                                step = 6;
                                NowNozzle_MAX_Pressure = 0;
                                NowNozzle_MIN_Pressure = NowNozzlePressure;
                            }
                            break;
                        case 10:
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                                step = 11;
                            break;

                        case 11:
                            xPos = ArrayPos[0, 0].X;
                            yPos = ArrayPos[0, 0].Y;
                            zPos = ArrayPos[0, 0].Z;

                            if (StageABS_Move(xPos, yPos, zPos, true, true, false))
                                step = 12;
                            break;

                        case 12:
                            xPos = ArrayPos[0, 0].X;
                            yPos = ArrayPos[0, 0].Y;
                            zPos = ArrayPos[0, 0].Z;

                            if (StageABS_Move(xPos, yPos, zPos, true, true, true))
                                step = 99;
                            break;


                        case 99:
                            step = 0;
                            Invoke(new dele_msgShow(TipMSG_Show), "矩陣植球完成");
                            LogMsgAdd(MList_Log, lb_HistoryList, "矩陣植球完成", tmpListStr);
                            _arrayOutRun = false;
                            _EQP_Status = enumEQP_Status.IDLE;
                            break;
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// 單pad值球
        /// </summary>  
        /// <param name="padNO"></param>
        /// <returns></returns>
        private async Task SinglePad_Flow(int padNO, int ballNO)
        {
            bool result = false;
            double xPos = 0, yPos = 0, zPos = 0;
            int _ballNo = ballNO;
            _RunFlag = true;
            int step = 1;
            try
            {
                while (_RunFlag)
                {
                    switch (step)
                    {
                        case 1:
                            if (!IO_InputData.DoorOpen)
                            {
                                //IO_OutputControl("門鎖開");
                                if (StageABS_Move(_msPosData.M_Load_X, _msPosData.M_Load_Y, _msPosData.M_Wait_Z, false, false, true))
                                {
                                    NowNozzle_MAX_Pressure = 0; step = 2;
                                    NowNozzle_MIN_Pressure = NowNozzlePressure;
                                }

                            }
                            break;

                        case 2:
                            if (_ballNo > _recipeData.SB.SolderBall_Number)//sb end
                                step = 10; //to ccd position
                            else
                                step = 3;


                            break;

                        case 3:
                            if (!IO_InputData.DoorOpen)
                            {
                                xPos = _BondData.BondPad[padNO - 1].SolderBall[_ballNo - 1].LX;
                                yPos = _BondData.BondPad[padNO - 1].SolderBall[_ballNo - 1].LY;
                                zPos = _BondData.BondPad[padNO - 1].SolderBall[_ballNo - 1].LZ;
                                step = 4;
                            }
                            break;

                        case 4:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, zPos, true, true, false))
                                {
                                    NowNozzle_MAX_Pressure = 0;
                                    NowNozzle_MIN_Pressure = NowNozzlePressure;
                                    step = 5;
                                }

                            }
                            break;

                        case 5:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, zPos, true, true, true))
                                {
                                    if (await DiskReadyRun())
                                        step = 6;
                                }

                            }
                            break;

                        case 6:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (await LaserEmission(_BondData.BondPad[padNO - 1].PadType))
                                {
                                    _ballNo++;
                                    step = 7;
                                }
                                else
                                {
                                    _EQP_Status = enumEQP_Status.DOWN;
                                    _RunFlag = false;
                                    Invoke(new dele_msgShow(ErrMSG_Show), "單Pad流程失敗");
                                }
                            }
                            break;

                        case 7:
                            if (_recipeData.SB.SB_MountMoveZ_Flag) //移動z軸上升
                            {
                                if (StageABS_Move(xPos, yPos, zPos - _recipeData.SB.SB_MountMoveZ, false, false, true)) //上移2mm 避開元件
                                {
                                    NowNozzle_MAX_Pressure = 0;
                                    NowNozzle_MIN_Pressure = NowNozzlePressure;
                                    step = 2;
                                }


                            }
                            else
                            {
                                NowNozzle_MAX_Pressure = 0;
                                NowNozzle_MIN_Pressure = NowNozzlePressure;
                                step = 2;
                            }


                            break;

                        case 10: //ccd
                            if (!IO_InputData.DoorOpen)
                            {
                                xPos = _BondData.BondPad[padNO - 1].SolderBall[0].X;
                                yPos = _BondData.BondPad[padNO - 1].SolderBall[0].Y;
                                zPos = _BondData.BondPad[padNO - 1].SolderBall[0].Z;
                                step = 11;
                            }
                            break;

                        case 11:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, _msPosData.M_Wait_Z, false, false, true))
                                {
                                    NowNozzle_MAX_Pressure = 0; step = 111;
                                    NowNozzle_MIN_Pressure = NowNozzlePressure;
                                }

                            }
                            break;

                        case 111:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, _msPosData.M_Wait_Z, true, true, true))
                                    step = 112;
                            }
                            break;

                        case 112:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, zPos, true, true, true))
                                    step = 12;
                            }
                            break;

                        case 12:
                            if (!IO_InputData.DoorOpen)
                            {
                                step = 0;
                                _EQP_Status = enumEQP_Status.IDLE;
                                _RunFlag = false;
                                LogMsgAdd(MList_Log, lb_HistoryList, "單Pad植球完成", tmpListStr);
                            }
                            break;
                    }

                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                _RunFlag = false;
            }

        }


        /// <summary>
        /// 單球植球
        /// </summary>
        /// <param name="padNO"></param>
        /// <param name="ballNO"></param>
        /// <returns></returns>
        private async Task SingleBall_Flow(int padNO, int ballNO)
        {
            double xPos = 0, yPos = 0, zPos = 0;

            _RunFlag = true;
            int step = 1;
            try
            {
                while (_RunFlag)
                {
                    switch (step)
                    {
                        case 1:
                            if (!IO_InputData.DoorOpen)
                            {
                                //IO_OutputControl("門鎖開");
                                if (StageABS_Move(_msPosData.M_Load_X, _msPosData.M_Load_Y, _msPosData.M_Wait_Z, false, false, true))
                                {
                                    NowNozzle_MAX_Pressure = 0;
                                    NowNozzle_MIN_Pressure = NowNozzlePressure;
                                    step = 2;
                                }

                            }
                            break;

                        case 2:
                            if (!IO_InputData.DoorOpen)
                            {
                                xPos = _BondData.BondPad[padNO - 1].SolderBall[ballNO - 1].LX;
                                yPos = _BondData.BondPad[padNO - 1].SolderBall[ballNO - 1].LY;
                                zPos = _BondData.BondPad[padNO - 1].SolderBall[ballNO - 1].LZ;
                                step = 3;

                            }
                            break;

                        case 3:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, zPos, true, true, false))
                                {
                                    step = 4;
                                }

                            }
                            break;

                        case 4:
                            if (!IO_InputData.DoorOpen)
                            {

                                if (StageABS_Move(xPos, yPos, zPos, true, true, true))
                                {
                                    if (await DiskReadyRun())
                                        step = 5;
                                }

                            }
                            break;

                        case 5:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (await LaserEmission(_BondData.BondPad[padNO - 1].PadType))
                                {
                                    step = 6;
                                }
                                else
                                {
                                    _EQP_Status = enumEQP_Status.DOWN;
                                    _RunFlag = false;
                                    Invoke(new dele_msgShow(ErrMSG_Show), "單球流程失敗");
                                }
                            }
                            break;

                        case 6: //ccd
                            if (!IO_InputData.DoorOpen)
                            {
                                xPos = _BondData.BondPad[padNO - 1].SolderBall[ballNO - 1].X;
                                yPos = _BondData.BondPad[padNO - 1].SolderBall[ballNO - 1].Y;
                                zPos = _BondData.BondPad[padNO - 1].SolderBall[ballNO - 1].Z;
                                step = 7;
                            }
                            break;

                        case 7:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, _msPosData.M_Wait_Z, false, false, true))
                                {
                                    NowNozzle_MAX_Pressure = 0;
                                    NowNozzle_MIN_Pressure = NowNozzlePressure;
                                    step = 71;
                                }

                            }
                            break;

                        case 71:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, _msPosData.M_Wait_Z, true, true, true))
                                    step = 72;
                            }
                            break;

                        case 72:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, zPos, true, true, true))
                                    step = 8;
                            }
                            break;

                        case 8:
                            if (!IO_InputData.DoorOpen)
                            {
                                LogMsgAdd(MList_Log, lb_HistoryList, "單球植球完成", tmpListStr);
                                step = 0;
                                _EQP_Status = enumEQP_Status.IDLE;
                                _RunFlag = false;
                            }
                            break;
                    }

                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {

            }

        }

        /// <summary>
        /// 單點植球
        /// </summary>
        /// <returns></returns>
        private async Task SinglePoint_Flow()
        {
            double xPos = 0, yPos = 0, zPos = 0;
            double CCD_xPos = 0, CCD_yPos = 0;
            double measureZ = 0;
            _RunFlag = true;
            int step = 1;

            try
            {
                while (_RunFlag)
                {
                    switch (step)
                    {
                        case 1:
                            if (!IO_InputData.DoorOpen)
                            {
                                //IO_OutputControl("門鎖開");
                                if (StageABS_Move(_msPosData.M_Load_X, _msPosData.M_Load_Y, _msPosData.M_Wait_Z, false, false, true))
                                {
                                    NowNozzle_MAX_Pressure = 0;
                                    NowNozzle_MIN_Pressure = NowNozzlePressure;
                                    step = 2;
                                }

                            }
                            break;

                        case 2:
                            if (!IO_InputData.DoorOpen)
                            {
                                CCD_xPos = Convert.ToDouble(txt_Motion_Position_X.Text);
                                CCD_yPos = Convert.ToDouble(txt_Motion_Position_Y.Text);
                                xPos = CCD_xPos - (Convert.ToDouble(_msPosData.M_Laser2Vision_X) - Convert.ToDouble(_msPosData.M_Laser2Height_X));
                                yPos = CCD_yPos - (Convert.ToDouble(_msPosData.M_Laser2Vision_Y) - Convert.ToDouble(_msPosData.M_Laser2Height_Y));
                                zPos = _msPosData.M_H_HeightZ;
                                step = 3;

                            }
                            break;

                        case 3:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, zPos, true, true, false))
                                    step = 4;
                            }
                            break;

                        case 4:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, zPos, true, true, true))
                                {
                                    Thread.Sleep(100);
                                    step = 5;
                                }

                            }
                            break;

                        case 5:
                            //測高
                            KeyenceHeight_Value = "11";
                            _Keyence_Height.Send(Keyence_Height_Send_Data);
                            Thread.Sleep(100);
                            step = 6;
                            break;
                        case 6:
                            //確認測高數值
                            if (KeyenceHeight_Value != "11" && Math.Abs(double.Parse(KeyenceHeight_Value)) <= 7) // 7mm
                            {
                                measureZ = double.Parse(KeyenceHeight_Value);
                                step = 7;
                            }
                            else
                            {
                                _EQP_Status = enumEQP_Status.DOWN;
                                _RunFlag = false;
                                Invoke(new dele_msgShow(ErrMSG_Show), "測高異常");
                            }
                            break;


                        case 7: //laser pos
                            if (!IO_InputData.DoorOpen)
                            {
                                stCalData nozzleOffset = NozzleAlignCal();
                                xPos = CCD_xPos - Convert.ToDouble(_msPosData.M_Laser2Vision_X) + _recipeData.SB.SB_OffsetX + nozzleOffset.DX;
                                yPos = CCD_yPos - Convert.ToDouble(_msPosData.M_Laser2Vision_Y) + _recipeData.SB.SB_OffsetY + nozzleOffset.DY;
                                zPos = _msPosData.M_H_LaserZ - measureZ + _recipeData.SB.SB_OffsetZ + nozzleOffset.DZ;
                                step = 8;
                            }
                            break;

                        case 8:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, _msPosData.M_Wait_Z, false, false, true))
                                {
                                    NowNozzle_MAX_Pressure = 0;
                                    NowNozzle_MIN_Pressure = NowNozzlePressure;
                                    step = 81;
                                }


                            }
                            break;

                        case 81:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, zPos, true, true, false))
                                    step = 9;

                            }
                            break;

                        case 9:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, zPos, true, true, true))
                                    if (await DiskReadyRun())
                                        step = 10;
                            }
                            break;

                        case 10:
                            if (!IO_InputData.DoorOpen)
                            {    //default signal setting
                                if (await LaserEmission(_msPosData.SENSE_PadType))
                                {
                                    step = 11;
                                }
                                else
                                {
                                    _EQP_Status = enumEQP_Status.DOWN;
                                    _RunFlag = false;
                                    Invoke(new dele_msgShow(ErrMSG_Show), "單點流程失敗");
                                }
                            }
                            break;

                        case 11: //ccd
                            if (!IO_InputData.DoorOpen)
                            {
                                xPos = CCD_xPos;
                                yPos = CCD_yPos;
                                zPos = _msPosData.M_H_VisionZ - measureZ;
                                step = 12;
                            }
                            break;

                        case 12:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, _msPosData.M_Wait_Z, false, false, true))
                                {
                                    step = 121;
                                    NowNozzle_MAX_Pressure = 0;
                                    NowNozzle_MIN_Pressure = NowNozzlePressure;
                                }

                            }
                            break;

                        case 121:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, _msPosData.M_Wait_Z, true, true, true))
                                    step = 122;
                            }
                            break;

                        case 122:
                            if (!IO_InputData.DoorOpen)
                            {
                                if (StageABS_Move(xPos, yPos, zPos, true, true, true))
                                    step = 13;
                            }
                            break;

                        case 13:
                            //if (!IO_InputData.DoorOpen)
                            {
                                LogMsgAdd(MList_Log, lb_HistoryList, "單點植球完成", tmpListStr);
                                step = 0;
                                _EQP_Status = enumEQP_Status.IDLE;
                                _RunFlag = false;
                            }
                            break;
                    }

                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// ST對位流程
        /// </summary>
        private async Task<int> ST_Align_Flow(int stNO)
        {
            int searchIndex = 0; //取像9宮格 1-8 (上~順時針)
            TJJS_Point tmpPoint = new TJJS_Point(0, 0);
            _RunFlag = true;
            try
            {
                _stCal_OK = false;
                while (_RunFlag)
                {
                    switch (_ST_Align_FlowStep)
                    {
                        case enumAlignFlowStep.GrabStep1:
                            thickPosCount = 0;

                            for (int i = 0; i < thickPoint.Length; i++)
                            {
                                thickPoint[i].MZ = 0;
                                thickPoint[i].X = _recipeData.Panel.ST_CenterPos[stNO - 1].X - Pos.Height_Pos[i].X;
                                thickPoint[i].Y = _recipeData.Panel.ST_CenterPos[stNO - 1].Y - Pos.Height_Pos[i].Y;
                                thickPoint[i].Z = _msPosData.M_H_VisionZ;
                                thickPoint[i].TX = _recipeData.Panel.ST_CenterPos[stNO - 1].X - Pos.Height_Pos[i].X + (_msPosData.M_Laser2Height_X - _msPosData.M_Laser2Vision_X);
                                thickPoint[i].TY = _recipeData.Panel.ST_CenterPos[stNO - 1].Y - Pos.Height_Pos[i].Y + (_msPosData.M_Laser2Height_Y - _msPosData.M_Laser2Vision_Y);
                                thickPoint[i].TZ = _msPosData.M_H_HeightZ;
                            }
                            _ST_Align_FlowStep = enumAlignFlowStep.GrabStep2;
                            break;

                        case enumAlignFlowStep.GrabStep2:
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                                _ST_Align_FlowStep = enumAlignFlowStep.GrabStep3;
                            break;

                        case enumAlignFlowStep.GrabStep3:
                            if (StageABS_Move(thickPoint[0].TX, thickPoint[0].TY, _msPosData.M_Wait_Z, true, true, true))
                                _ST_Align_FlowStep = enumAlignFlowStep.GrabStep4;
                            break;

                        //取像點測高
                        case enumAlignFlowStep.GrabStep4:
                            if (thickPosCount >= PublicData.GRAB_NUM)
                            {
                                _ST_Align_FlowStep = enumAlignFlowStep.GrabStep6;
                            }
                            else
                            {
                                if (StageABS_Move(thickPoint[thickPosCount].TX, thickPoint[thickPosCount].TY, thickPoint[thickPosCount].TZ, true, true, true))
                                {
                                    Thread.Sleep(PublicData.MHeightDelay);
                                    _ST_Align_FlowStep = enumAlignFlowStep.GrabStep5;
                                }

                            }
                            break;

                        case enumAlignFlowStep.GrabStep5:
                            if (await TaskRun(ConFocusRead(), TimeOutTask(TimeSpan.FromSeconds(10))))
                            {
                                thickPoint[thickPosCount].MZ = double.Parse(KeyenceHeight_Value);
                                thickPosCount++;
                                _ST_Align_FlowStep = enumAlignFlowStep.GrabStep4;
                            }
                            else
                            {
                                _RunFlag = false;
                                _EQP_Status = enumEQP_Status.DOWN;
                                Invoke(new dele_msgShow(ErrMSG_Show), "測高異常");
                                return -1;
                            }
                            break;

                        case enumAlignFlowStep.GrabStep6:
                            grabPosCount = 0;
                            for (int i = 0; i < PublicData.GRAB_NUM; i++)
                            {
                                grabPoint[i].X = _recipeData.Panel.ST_CenterPos[stNO - 1].X - Pos.Mark_Pos[i].X;
                                grabPoint[i].Y = _recipeData.Panel.ST_CenterPos[stNO - 1].Y - Pos.Mark_Pos[i].Y;
                                grabPoint[i].Z = _msPosData.M_H_VisionZ - thickPoint[i].MZ;

                                baseGrabPoint[i].X = -(_recipeData.Panel.ST_CenterPos[stNO - 1].X - Pos.Mark_Pos[i].X);
                                baseGrabPoint[i].Y = -(_recipeData.Panel.ST_CenterPos[stNO - 1].Y - Pos.Mark_Pos[i].Y);
                            }
                            _ST_Align_FlowStep = enumAlignFlowStep.GrabStep7;
                            break;

                        //取像 新增9宮格由上方順時針
                        case enumAlignFlowStep.GrabStep7:
                            if (grabPosCount >= PublicData.GRAB_NUM)
                            {
                                _ST_Align_FlowStep = enumAlignFlowStep.GrabStep9;
                            }
                            else
                            {
                                tmpPoint = CCD_MoveDisk(1, searchIndex);
                                if (StageABS_Move(grabPoint[grabPosCount].X + tmpPoint.X, grabPoint[grabPosCount].Y + tmpPoint.Y, grabPoint[grabPosCount].Z, true, true, true))
                                    _ST_Align_FlowStep = enumAlignFlowStep.GrabStep8;
                            }
                            break;

                        case enumAlignFlowStep.GrabStep8:
                            if (await TaskRun(ST_MarkGrab(grabPosCount + 1), TimeOutTask(TimeSpan.FromSeconds(10))))
                            {
                                grabPosCount++;
                                searchIndex = 0;
                                _ST_Align_FlowStep = enumAlignFlowStep.GrabStep7;
                            }
                            else
                            {
                                searchIndex++;
                                if (searchIndex > 8) //9宮格失敗
                                {
                                    _RunFlag = false;
                                    Invoke(new dele_msgShow(ErrMSG_Show), "取像異常");
                                    _EQP_Status = enumEQP_Status.DOWN;
                                    return -1;
                                }
                                _ST_Align_FlowStep = enumAlignFlowStep.GrabStep7;
                            }
                            break;

                        //calculate
                        case enumAlignFlowStep.GrabStep9:
                            if (ST_CalData())
                            {
                                _stCal_OK = true;
                                _ST_Align_FlowStep = enumAlignFlowStep.GrabStepWait;
                                _RunFlag = false;
                                if (_EQP_Status == enumEQP_Status.MANU)
                                    _EQP_Status = enumEQP_Status.IDLE;
                                //Invoke(new dele_msgShow(ErrMSG_Show), "計算完成");
                                LogMsgAdd(MList_Log, lb_HistoryList, "計算完成", tmpListStr);
                                return 0;
                            }
                            else
                            {
                                _ST_Align_FlowStep = enumAlignFlowStep.GrabStepWait;
                                _RunFlag = false;
                                _EQP_Status = enumEQP_Status.DOWN;
                                LogMsgAdd(Error_Log, lb_ErrorList, "計算異常", tmpErrStr);
                                Invoke(new dele_msgShow(ErrMSG_Show), "計算異常");

                                return -1;
                            }
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                _RunFlag = false;
                _EQP_Status = enumEQP_Status.DOWN;
                LogMsgAdd(Error_Log, lb_ErrorList, ex.ToString(), tmpErrStr);
                Invoke(new dele_msgShow(ErrMSG_Show), $"[ST_Align_Flow Error]: {ex.ToString()}");
                return -1;
            }
            return -1;
        }

        /// <summary>
        /// CCD FOV 9宮格搜尋 移動量
        /// </summary>
        /// <param name="ccdNo"></param>
        /// <param name="direct">1:上方 依序順時針移動 </param>
        /// <returns></returns>
        private TJJS_Point CCD_MoveDisk(int ccdNo, int direct)
        {
            TJJS_Point result = new TJJS_Point();
            result.X = _ConfigSystem._CCD_Pixel[ccdNo - 1] * 2432 / 1000;  //CCD1 X FOV移動量
            result.Y = _ConfigSystem._CCD_Pixel[ccdNo - 1] * 2050 / 1000;  //CCD1 Y FOV移動量

            switch (direct)
            {
                case 0: //上
                    result.X = 0;
                    result.Y = 0;
                    break;

                case 1: //上
                    result.X = 0;
                    result.Y = -result.Y;
                    break;

                case 2: //右上
                    result.X = -result.X;
                    result.Y = 0;
                    break;

                case 3: //右
                    result.X = 0;
                    result.Y = result.Y;
                    break;

                case 4:
                    result.X = 0;
                    result.Y = result.Y;
                    break;

                case 5:
                    result.X = -result.X;
                    result.Y = 0;
                    break;

                case 6:
                    result.X = -result.X;
                    result.Y = 0;
                    break;

                case 7:
                    result.X = 0;
                    result.Y = -result.Y;
                    break;

                case 8:
                    result.X = 0;
                    result.Y = -result.Y;
                    break;
            }
            return result;
        }

        private void btn_LoadPOS_Move_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行入料流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (FlowAllow())
                {
                    _EQP_Status = enumEQP_Status.MANU;
                    Task.Run(LoadPosMoveFlow);
                    LogMsgAdd(MList_Log, lb_HistoryList, "執行入料流程", tmpListStr);
                }
            }
        }

        private void btn_UnloadPOS_Move_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行出料流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (FlowAllow())
                {
                    _EQP_Status = enumEQP_Status.MANU;
                    Task.Run(UnloadPosMoveFlow);
                    LogMsgAdd(MList_Log, lb_HistoryList, "執行出料流程", tmpListStr);
                }

            }
        }

        private void btn_HMeasureTest_Click(object sender, EventArgs e)
        {
            if (_fileLoad)
            {
                if (MessageBox.Show("是否執行測高流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    int no = (int)Num_ST_NO.Value;
                    if (FlowAllow())
                    {
                        _ST_H_Measure_FlowStep = enumH_MeasureFlowStep.H_MeasureStep1;
                        Task.Run(() => { ST_ThickM_Flow(no); });
                        _EQP_Status = enumEQP_Status.MANU;
                    }
                }
            }
            else
            {
                MessageBox.Show("座標檔尚未載入!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }


        }



        //  P2---P4
        //  |     |
        //  P1---P3
        /// <summary>
        /// ST測高流程
        /// </summary>

        private async Task<int> ST_ThickM_Flow(int stNO)
        {
            _RunFlag = true;
            TJJS_Point basePoint, inputPoint, outPoint;
            outPoint = new TJJS_Point();
            _stMHeight_OK = false;
            try
            {
                while (_RunFlag)
                {
                    switch (_ST_H_Measure_FlowStep)
                    {
                        case enumH_MeasureFlowStep.H_MeasureStep1:
                            thickPosCount = 0;

                            basePoint = new TJJS_Point(grabPoint[0].X, grabPoint[0].Y);


                            for (int i = 0; i < PublicData.HMeasure_NUM; i++)
                            {
                                thickPoint[i].MZ = 0;
                                thickPoint[i].X = _recipeData.Panel.ST_CenterPos[stNO - 1].X - Pos.Height_Pos[i].X;
                                thickPoint[i].Y = _recipeData.Panel.ST_CenterPos[stNO - 1].Y - Pos.Height_Pos[i].Y;
                                thickPoint[i].Z = _msPosData.M_H_VisionZ;
                                inputPoint = new TJJS_Point(thickPoint[i].X, thickPoint[i].Y);
                                PointMove(inputPoint, basePoint, PublicData.ST_V_CalData, ref outPoint);
                                thickPoint[i].X = outPoint.X;
                                thickPoint[i].Y = outPoint.Y;
                                thickPoint[i].TX = outPoint.X + (_msPosData.M_Laser2Height_X - _msPosData.M_Laser2Vision_X);
                                thickPoint[i].TY = outPoint.Y + (_msPosData.M_Laser2Height_Y - _msPosData.M_Laser2Vision_Y);
                                thickPoint[i].TZ = _msPosData.M_H_HeightZ;
                            }
                            PublicData.ST_HeightZ = 0;

                            _ST_H_Measure_FlowStep = enumH_MeasureFlowStep.H_MeasureStep2;
                            break;


                        case enumH_MeasureFlowStep.H_MeasureStep2:
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                                _ST_H_Measure_FlowStep = enumH_MeasureFlowStep.H_MeasureStep3;
                            break;

                        case enumH_MeasureFlowStep.H_MeasureStep3:
                            if (StageABS_Move(thickPoint[0].TX, thickPoint[0].TY, _msPosData.M_Wait_Z, true, true, true))
                                _ST_H_Measure_FlowStep = enumH_MeasureFlowStep.H_MeasureStep4;
                            break;

                        //取像點測高
                        case enumH_MeasureFlowStep.H_MeasureStep4:
                            if (thickPosCount >= PublicData.HMeasure_NUM)
                            {
                                _ST_H_Measure_FlowStep = enumH_MeasureFlowStep.H_MeasureStep6;
                            }
                            else
                            {
                                if (StageABS_Move(thickPoint[thickPosCount].TX, thickPoint[thickPosCount].TY, thickPoint[thickPosCount].TZ, true, true, true))
                                {
                                    Thread.Sleep(PublicData.MHeightDelay);
                                    _ST_H_Measure_FlowStep = enumH_MeasureFlowStep.H_MeasureStep5;
                                }

                            }
                            break;

                        case enumH_MeasureFlowStep.H_MeasureStep5:
                            if (await TaskRun(ConFocusRead(), TimeOutTask(TimeSpan.FromSeconds(10))))
                            {
                                thickPoint[thickPosCount].MZ = double.Parse(KeyenceHeight_Value);
                                thickPosCount++;
                                _ST_H_Measure_FlowStep = enumH_MeasureFlowStep.H_MeasureStep4;
                            }
                            else
                            {
                                _EQP_Status = enumEQP_Status.DOWN;
                                Invoke(new dele_msgShow(ErrMSG_Show), "測高異常");
                                LogMsgAdd(Error_Log, lb_ErrorList, "測高異常", tmpErrStr);
                                _RunFlag = false;
                                return -1;
                            }
                            break;

                        //測高平面計算
                        case enumH_MeasureFlowStep.H_MeasureStep6:
                            PublicData.ST_HeightZ = (thickPoint[0].MZ + thickPoint[1].MZ + thickPoint[2].MZ + thickPoint[3].MZ) / 4;

                            if (CalST_PadMap(stNO))
                            {

                                if (Cal_SolderBallMap())
                                {
                                    //LogMsgAdd(MList_Log, lb_HistoryList,$"[測高量測 P1[{thickPoint[0].MZ}] P2[{thickPoint[1].MZ}] P3[{thickPoint[2].MZ}] P4[{thickPoint[3].MZ}]", tmpListStr);
                                    _ST_H_Measure_FlowStep = enumH_MeasureFlowStep.H_MeasureStepWait;
                                    _stMHeight_OK = true;
                                    _RunFlag = false;
                                    if (_EQP_Status == enumEQP_Status.MANU)
                                        _EQP_Status = enumEQP_Status.IDLE;

                                    //測高判定
                                    if (!HeightJudge(thickPoint[0].MZ, thickPoint[1].MZ, thickPoint[2].MZ, thickPoint[3].MZ, _recipeData.SYSParam.FourP_HeightLimit))
                                    {
                                        LogMsgAdd(Error_Log, lb_ErrorList, $"測高超過限制量[{_recipeData.SYSParam.FourP_HeightLimit}]um!", tmpErrStr);
                                        Invoke(new dele_msgShow(ErrMSG_Show), $"測高超過限制量[{_recipeData.SYSParam.FourP_HeightLimit}]um!");
                                        return -1;
                                    }

                                    //Invoke(new dele_msgShow(ErrMSG_Show), "測高流程完成");
                                    LogMsgAdd(MList_Log, lb_HistoryList, "測高流程完成", tmpListStr);
                                    return 0;
                                }
                                else
                                {
                                    _ST_H_Measure_FlowStep = enumH_MeasureFlowStep.H_MeasureStepWait;
                                    _RunFlag = false;
                                    _EQP_Status = enumEQP_Status.DOWN;
                                    Invoke(new dele_msgShow(ErrMSG_Show), "植球路徑計算失敗。");
                                    LogMsgAdd(Error_Log, lb_ErrorList, "植球路徑計算失敗。", tmpErrStr);
                                    return -1;
                                }
                            }
                            else
                            {
                                _ST_H_Measure_FlowStep = enumH_MeasureFlowStep.H_MeasureStepWait;
                                _EQP_Status = enumEQP_Status.DOWN;
                                Invoke(new dele_msgShow(ErrMSG_Show), "ST Pad座標計算失敗。");
                                LogMsgAdd(Error_Log, lb_ErrorList, "ST Pad座標計算失敗。", tmpErrStr);
                                _RunFlag = false;
                                return -1;
                            }
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                _RunFlag = false;
                _EQP_Status = enumEQP_Status.DOWN;
                Invoke(new dele_msgShow(ErrMSG_Show), "測高異常");
                LogMsgAdd(Error_Log, lb_ErrorList, ex.ToString(), tmpErrStr);
                return -1;
            }

            return -1;
        }

        private void tab_AlignCycle_Click(object sender, EventArgs e)
        {

        }

        private void btn_CleanNozzleFlow_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行吸嘴 清潔流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _NozzleCleanFlowStep = enumNozzleCleanFlowStep.NozzleClean_Step1;
                if (FlowAllow())
                    Task.Run(NozzleCleanFlow);

            }
        }

        private void btn_GrabTest_Click(object sender, EventArgs e)
        {
            if (_fileLoad)
            {
                if (MessageBox.Show("是否執行取像流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    int no = (int)Num_ST_NO.Value;

                    if (FlowAllow())
                    {
                        _ST_Align_FlowStep = enumAlignFlowStep.GrabStep1;
                        _EQP_Status = enumEQP_Status.MANU;
                        Task.Run(() => { ST_Align_Flow(no); });
                    }

                }
            }
            else
            {
                MessageBox.Show("座標檔尚未載入!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        private void btn_Motion_Homing_Q_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行 Q軸原點復歸?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Axis_thMotionRun_Tick(Motion_Num.Home_Q);
            }
        }

        private void chk_HeaterPower_Click(object sender, EventArgs e)
        {
            string msg = (chk_HeaterPower.Checked) ? "關閉" : "開啟";


            if (MessageBox.Show($"是否{msg}加熱模組?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                chk_HeaterPower.Checked = !chk_HeaterPower.Checked;

                if (!IO_InputData.OverHeater)
                {
                    if (chk_HeaterPower.Checked)
                    {
                        IO_OutputControl("溫控器開");
                        //IO_OutputControl("門鎖開");
                    }
                    else
                    {
                        IO_OutputControl("溫控器關");
                        IO_OutputControl("門鎖關");
                    }
                }
                else
                {
                    IO_OutputControl("溫控器關");
                    MessageBox.Show($"目前偵測過溫警報，禁止加熱!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnt_Heater_Set_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show($"是否設定工作溫度?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    //if (double.Parse(txt_Heater_Set.Text) > 120)
                    //    txt_Heater_Set.Text = "120";
                    //E5CC.SV_Set(double.Parse(txt_Heater_Set.Text));


                    double temp = double.Parse(txt_Heater_Set.Text);
                    TableTemp_Set(temp);
                    _TableTempSet = temp;
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void btn_FlowStop_Click(object sender, EventArgs e)
        {
            FlowStop();
        }

        /// <summary>
        /// 流程停止
        /// </summary>
        private void FlowStop()
        {
            _EQP_Status = enumEQP_Status.IDLE;
            EnableActionPage();
            EnableTabPages(true);
            CycleFlagInit();
            FlowStep_INIT();
            int[] ax = { 0, 1, 2 };
            if (_Using)
            {
                Thread.Sleep(1000); //等待旋轉完成
                this._Aerotech_Controller.Commands.Motion.Abort(ax);
                //_AZD_Controller.Stop();
                Motion.mAcm_AxResetError(m_Axishand[0]);
                Motion.mAcm_AxStopDec(m_Axishand[0]);
                AZD_ReadLock = false;
            }
            IO_OutputControl("門鎖關");
            LogMsgAdd(MList_Log, lb_HistoryList, "停止流程", tmpListStr);
        }

        /// <summary>
        /// 主流程完成
        /// </summary>
        private void FlowFinish()
        {
            _EQP_Status = enumEQP_Status.IDLE;
            EnableActionPage();
            EnableTabPages(true);
            CycleFlagInit();
            FlowStep_INIT();
            IO_OutputControl("門鎖關");
            MList_Log.Add("主流程完成.");
        }

        /// <summary>
        /// While flag init
        /// </summary>
        private void CycleFlagInit()
        {
            _mainStopFlag = true;
            _RunFlag = false;
            _DiskRunFlag = false;
            _LaserRunFlag = false;
            _ClearFlag = false;
            _arrayOutRun = false;
            _ballOutRun = false;
        }



        private void B_PowerMeter_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行功率量測流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step1;

                if (FlowAllow())
                    Task.Run(PowerMeterFlow);

            }
        }

        /// <summary>
        /// 吸嘴 XY校正
        /// </summary>
        private async Task<bool> Nozzle_XY_AlignFlow()
        {
            bool result = false;
            _RunFlag = true;
            try
            {
                while (_RunFlag)
                {
                    switch (_NozzleXY_AlignFlowStep)
                    {
                        case enumNozzleXY_AlignFlowStep.NozzleXY_Step1:
                            SetLight((byte)_recipeData.SYSParam.NozzleXY_Light, (byte)_recipeData.SYSParam.NozzleZ_Light, 0, 0);

                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                                _NozzleXY_AlignFlowStep = enumNozzleXY_AlignFlowStep.NozzleXY_Step2;
                            break;

                        case enumNozzleXY_AlignFlowStep.NozzleXY_Step2:
                            if (StageABS_Move(_msPosData.M_NozzleXY_X, _msPosData.M_NozzleXY_Y, _msPosData.M_Wait_Z, true, true, false))
                                _NozzleXY_AlignFlowStep = enumNozzleXY_AlignFlowStep.NozzleXY_Step3;
                            break;

                        case enumNozzleXY_AlignFlowStep.NozzleXY_Step3:
                            //新增nozzle z補正
                            stCalData nozzleOffset = NozzleAlignCal();
                            if (StageABS_Move(_msPosData.M_NozzleXY_X, _msPosData.M_NozzleXY_Y, _msPosData.M_NozzleXY_Z + nozzleOffset.DZ, true, true, true))
                                _NozzleXY_AlignFlowStep = enumNozzleXY_AlignFlowStep.NozzleXY_Step4;
                            break;

                        case enumNozzleXY_AlignFlowStep.NozzleXY_Step4:

                            if (await Nozzle_XY_Grab())
                            {
                                _NozzleXY_AlignFlowStep = enumNozzleXY_AlignFlowStep.NozzleXY_Step5;
                                result = true;
                                EnableTabPages(true);
                            }
                            else
                            {
                                Invoke(new dele_msgShow(ErrMSG_Show), "吸嘴 XY 校驗流程失敗[取像異常]");
                                Error_Log.Add($"吸嘴 XY 校驗流程失敗");
                                _RunFlag = false;
                                result = false;
                            }
                            break;

                        case enumNozzleXY_AlignFlowStep.NozzleXY_Step5:
                            SetLight(0, 0, 0, 0);
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                            {
                                LogMsgAdd(MList_Log, lb_HistoryList, "吸嘴 XY 校驗流程完成。", tmpListStr);
                                _EQP_Status = enumEQP_Status.IDLE;
                                _RunFlag = false;
                                Invoke(new dele_msgShow(TipMSG_Show), "吸嘴 XY 校驗流程完成。");
                            }

                            break;
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"[Nozzle_XY_AlignFlow Error]: {ex.ToString()}");
                Invoke(new dele_msgShow(ErrMSG_Show), $"[Nozzle_XY_AlignFlow Error]: {ex.ToString()}");
                _EQP_Status = enumEQP_Status.DOWN;
                _RunFlag = false;
                result = false;
            }
            return result;
        }



        /// <summary>
        /// 吸嘴 Z校正
        /// </summary>
        private async Task<bool> Nozzle_Z_AlignFlow()
        {
            bool result = false;
            _RunFlag = true;
            try
            {
                while (_RunFlag)
                {
                    switch (_NozzleZ_AlignFlowStep)
                    {
                        case enumNozzleZ_AlignFlowStep.NozzleZ_Step1:
                            SetLight((byte)_recipeData.SYSParam.NozzleXY_Light, (byte)_recipeData.SYSParam.NozzleZ_Light, 0, 0);
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                                _NozzleZ_AlignFlowStep = enumNozzleZ_AlignFlowStep.NozzleZ_Step2;
                            break;

                        case enumNozzleZ_AlignFlowStep.NozzleZ_Step2:
                            if (StageABS_Move(_msPosData.M_NozzleZ_X, _msPosData.M_NozzleZ_Y, _msPosData.M_Wait_Z, true, true, false))
                                _NozzleZ_AlignFlowStep = enumNozzleZ_AlignFlowStep.NozzleZ_Step3;
                            break;

                        case enumNozzleZ_AlignFlowStep.NozzleZ_Step3:
                            if (StageABS_Move(_msPosData.M_NozzleZ_X, _msPosData.M_NozzleZ_Y, _msPosData.M_NozzleZ_Z, true, true, true))
                            {
                                _NozzleZ_AlignFlowStep = enumNozzleZ_AlignFlowStep.NozzleZ_Step4;
                                Thread.Sleep(100);
                            }

                            break;

                        case enumNozzleZ_AlignFlowStep.NozzleZ_Step4:
                            if (await Nozzle_Z_Grab())
                            {
                                _NozzleZ_AlignFlowStep = enumNozzleZ_AlignFlowStep.NozzleZ_Step5;
                                result = true;
                            }
                            else
                            {
                                Invoke(new dele_msgShow(ErrMSG_Show), "吸嘴 Z 校驗流程失敗[取像異常]");
                                Error_Log.Add($"吸嘴 Z 校驗流程失敗");
                                result = false;
                                _RunFlag = false;
                            }
                            break;

                        case enumNozzleZ_AlignFlowStep.NozzleZ_Step5:
                            SetLight(0, 0, 0, 0);
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                            {
                                LogMsgAdd(MList_Log, lb_HistoryList, "吸嘴 Z 校驗流程完成。", tmpListStr);
                                _EQP_Status = enumEQP_Status.IDLE;
                                _RunFlag = false;
                                Invoke(new dele_msgShow(TipMSG_Show), "吸嘴 Z 校驗流程完成。");
                            }


                            break;

                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"[Nozzle_Z_AlignFlow Error]: {ex.ToString()}");
                Invoke(new dele_msgShow(ErrMSG_Show), $"[Nozzle_Z_AlignFlow Error]: {ex.ToString()}");
                _EQP_Status = enumEQP_Status.DOWN;
                _RunFlag = false;
                result = false;
            }
            return result;
        }

        /// <summary>
        /// 吸嘴清潔流程
        /// </summary>
        private async Task<bool> NozzleCleanFlow()
        {
            bool result = false;
            _ClearFlag = true;

            Stopwatch airT = new Stopwatch();
            Stopwatch vacuumT = new Stopwatch();
            ushort value;
            int count = 0;

            try
            {
                while (_ClearFlag)
                {
                    switch (_NozzleCleanFlowStep)
                    {
                        case enumNozzleCleanFlowStep.NozzleClean_Step1:
                            if (Set_CleanLaserData())
                            {
                                if (StageABS_Move(_msPosData.M_PumpOut_X, _msPosData.M_PumpOut_Y, _msPosData.M_Wait_Z, false, false, true))
                                {
                                    count++;
                                    value = (ushort)(_recipeData.SYSParam.CleanAirValve * 10);
                                    if (UpdateProportionalValve(value))
                                    {
                                        _NozzleCleanFlowStep = enumNozzleCleanFlowStep.NozzleClean_Step2;
                                        count = 0;
                                    }

                                    else
                                    {
                                        if (count > 500)
                                        {
                                            _ClearFlag = false;
                                            LogMsgAdd(Error_Log, lb_ErrorList, "比例閥更新超時", tmpErrStr);
                                            Invoke(new dele_msgShow(ErrMSG_Show), "比例閥更新超時");

                                        };
                                    }
                                }


                            }
                            else
                            {
                                LogMsgAdd(Error_Log, lb_ErrorList, "清潔雷射參數設定異常!", tmpErrStr);

                            }
                            break;

                        case enumNozzleCleanFlowStep.NozzleClean_Step2:
                            if (StageABS_Move(_msPosData.M_PumpOut_X, _msPosData.M_PumpOut_Y, _msPosData.M_Wait_Z, true, true, false))
                                _NozzleCleanFlowStep = enumNozzleCleanFlowStep.NozzleClean_Step3;
                            break;

                        case enumNozzleCleanFlowStep.NozzleClean_Step3:
                            if (StageABS_Move(_msPosData.M_PumpOut_X, _msPosData.M_PumpOut_Y, _msPosData.M_PumpOut_Z, true, true, true))
                                _NozzleCleanFlowStep = enumNozzleCleanFlowStep.NozzleClean_Step4;
                            break;

                        case enumNozzleCleanFlowStep.NozzleClean_Step4:
                            IO_OutputControl("清潔真空開");
                            ElectricValve("吸嘴開");
                            Thread.Sleep(100);
                            vacuumT.Start();
                            airT.Start();
                            _NozzleCleanFlowStep = enumNozzleCleanFlowStep.NozzleClean_Step6;
                            break;

                        //case enumNozzleCleanFlowStep.NozzleClean_Step5:
                        //    if (axMMMark.LoadFile(_ezmPath) == 0)
                        //        _NozzleCleanFlowStep = enumNozzleCleanFlowStep.NozzleClean_Step6;
                        //    break;

                        case enumNozzleCleanFlowStep.NozzleClean_Step6:
                            //雷射開
                            if (StartMarking())
                            {
                                LaserStatus_Emission = true;
                                _NozzleCleanFlowStep = enumNozzleCleanFlowStep.NozzleClean_Step7;

                            }
                            else
                            {
                                LogMsgAdd(Error_Log, lb_ErrorList, "[NozzleCleanFlow Error]: StartMarking異常!", tmpErrStr);
                                _ClearFlag = false;
                                result = false;
                            }
                            break;

                        case enumNozzleCleanFlowStep.NozzleClean_Step7:
                            if (vacuumT.ElapsedMilliseconds > _recipeData.SYSParam.CleanVacuumTime)
                            {
                                IO_OutputControl("清潔真空關");
                                IO_OutputControl("清潔真空破");
                            }
                            if (airT.ElapsedMilliseconds > _recipeData.SYSParam.CleanAirTime)
                            {
                                ElectricValve("吸嘴關");

                            }

                            if (vacuumT.ElapsedMilliseconds > _recipeData.SYSParam.CleanVacuumTime && airT.ElapsedMilliseconds > _recipeData.SYSParam.CleanAirTime)
                            {
                                _NozzleCleanFlowStep = enumNozzleCleanFlowStep.NozzleClean_Step8;
                            }
                            break;

                        case enumNozzleCleanFlowStep.NozzleClean_Step8:
                            if (StageABS_Move(_msPosData.M_PumpOut_X, _msPosData.M_PumpOut_Y, _msPosData.M_Wait_Z, false, false, true))
                            {
                                count++;
                                value = (ushort)(_recipeData.SYSParam.NozzleSB_ValveNum * 10);

                                if (UpdateProportionalValve(value))
                                {
                                    IO_OutputControl("清潔真空破OFF");
                                    _ClearFlag = false;
                                    //確認清潔成功 (無球)
                                    P_F_EthernetIP.DataUpdataOk = false;
                                    Thread.Sleep(200);

                                    if (P_F_EthernetIP.DataUpdataOk)
                                    {
                                        if (NoSB_Air_Check())
                                        {
                                            LogMsgAdd(MList_Log, lb_HistoryList, "清潔流程完成。", tmpListStr);
                                            result = true;

                                        }
                                        _NozzleCleanFlowStep = enumNozzleCleanFlowStep.NozzleClean_Wait;
                                    }


                                }
                                else
                                {
                                    if (count > 500)
                                    {
                                        _ClearFlag = false;
                                        LogMsgAdd(Error_Log, lb_ErrorList, "比例閥更新超時", tmpErrStr);
                                        Invoke(new dele_msgShow(ErrMSG_Show), "比例閥更新超時");
                                    };
                                }
                            }
                            break;

                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"[NozzleCleanFlow Error]: {ex.ToString()}");
                _ClearFlag = false;
                result = false;
            }
            return result;
        }

        /// <summary>
        /// 分離盤定位流程
        /// </summary>
        private async Task<bool> DiskAlignFlow()
        {
            bool result = false;
            _RunFlag = true;

            int azd_pos = (int)(_recipeData.SYSParam.RotateAngle * 1000);
            int azd_speed = (int)(_recipeData.SYSParam.DiskRunSpeed * 1000);
            int azd_searchSpeed = (int)(0.2 * 1000);
            //int holeRadius = (int)(-1.5 * 1000);
            int holeRadius = (int)(-0.3 * 1000); //光纖孔約0.68度移動距離(圖面)
            uint azd_acc = (uint)(500 * 1000);
            uint azd_dcc = (uint)(500 * 1000);

            try
            {
                while (_RunFlag)
                {
                    switch (_DiskAlignFlowStep)
                    {
                        case enumDisk_AlignFlowStep.DiskAlign_Step1:
                            PCIE_1203_SetParam(azd_searchSpeed, azd_acc, azd_dcc);
                            _returnCode = Motion.mAcm_AxMoveVel(m_Axishand[0], 1);

                            _DiskAlignFlowStep = enumDisk_AlignFlowStep.DiskAlign_Step2;
                            break;

                        case enumDisk_AlignFlowStep.DiskAlign_Step2:
                            if (IO_InputData.DiskSensor)
                            {
                                Motion.mAcm_AxStopDec(m_Axishand[0]);
                                _DiskAlignFlowStep = enumDisk_AlignFlowStep.DiskAlign_Step3;
                            }
                            break;

                        case enumDisk_AlignFlowStep.DiskAlign_Step3:
                            PCIE_1203_SetParam(azd_speed, azd_acc, azd_dcc);
                            if (Motion.mAcm_AxMoveRel(m_Axishand[0], holeRadius) == (uint)ErrorCode.SUCCESS)
                                _DiskAlignFlowStep = enumDisk_AlignFlowStep.DiskAlign_Step5;
                            break;

                        //case enumDisk_AlignFlowStep.DiskAlign_Step4:
                        //   PCIE_1203_SetParam(azd_speed, azd_acc, azd_dcc);
                        //    if (Motion.mAcm_AxMoveRel(m_Axishand[0], azd_pos) == (uint)ErrorCode.SUCCESS)
                        //        _DiskAlignFlowStep = enumDisk_AlignFlowStep.DiskAlign_Step5;
                        //    break;

                        case enumDisk_AlignFlowStep.DiskAlign_Step5:
                            if (IO_InputData.DiskSensor)
                            {
                                result = true;
                                _DiskAlignFlowStep = enumDisk_AlignFlowStep.DiskAlign_Step9;
                            }
                            else
                            {
                                Invoke(new dele_msgShow(ErrMSG_Show), "分離盤定位失敗!");
                                _DiskAlignFlowStep = enumDisk_AlignFlowStep.DiskAlign_Wait;
                                _RunFlag = false;
                                result = false;
                            }
                            break;

                        case enumDisk_AlignFlowStep.DiskAlign_Step9:
                            _EQP_Status = enumEQP_Status.IDLE;
                            _RunFlag = false;
                            _DiskAlignFlowStep = enumDisk_AlignFlowStep.DiskAlign_Wait;
                            LogMsgAdd(MList_Log, lb_HistoryList, "分離盤定位完成。", tmpListStr);
                            Invoke(new dele_msgShow(TipMSG_Show), "分離盤定位完成!");

                            break;
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"[Disk Align Error]: {ex.ToString()}");
                Invoke(new dele_msgShow(ErrMSG_Show), "分離盤定位失敗!");
                _EQP_Status = enumEQP_Status.DOWN;
                _RunFlag = false;
                result = false;
            }
            return result;
        }


        /// <summary>
        /// 雷射校正流程
        /// </summary>
        private async Task<bool> LaserAlignFlow()
        {
            bool result = false;
            _RunFlag = true;
            double xPos = 0, yPos = 0, zPos = 0;
            double limit = 300, pitch = 30;
            DateTime startT = DateTime.Now, nowT;
            TimeSpan _timeoutTS = new TimeSpan();
            int _timeoutMinute = 60;//10 min

            int posNum = 0; int runCount = 0;
            int xyRun = 0, zRun = 0;
            LaserPos maxPmLaserPos = new LaserPos();
            int delayTime = int.Parse(txt_PM_LaserOffTime.Text);

            int xyRange = 0, xyStep = 0, zRange = 0, zStep = 0;


            try
            {
                xyRange = int.Parse(txt_LaserXY_Range.Text);
                xyStep = int.Parse(txt_LaserXY_Step.Text);
                zRange = int.Parse(txt_LaserZ_Range.Text);
                zStep = int.Parse(txt_LaserZ_Step.Text);
                while (_RunFlag)
                {
                    switch (_LaserAlignFlowStep)
                    {
                        case enumLaser_AlignFlowStep.LaserAlign_Step1:
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                            {
                                IO_OutputControl("門鎖開");
                                startT = DateTime.Now;
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step2;
                            }
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Step2:
                            if (StageABS_Move(_msPosData.M_PowerMeter_X, _msPosData.M_PowerMeter_Y, _msPosData.M_Wait_Z, true, true, true))
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step3;
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Step3:
                            if (StageABS_Move(_msPosData.M_PowerMeter_X, _msPosData.M_PowerMeter_Y, _msPosData.M_PowerMeter_Z, true, true, true))
                            {
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step4; //PM 位置到
                            }
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Step4:
                            _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step1;
                            _laserPosList.Clear();
                            GetAlingLaserPM();


                            _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step5;
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Step5:
                            if (AlignPM < _msPosData.LaserAlignTargetPower) //校正功率門檻
                            {
                                if (xyRun == 0)
                                {
                                    xPos = double.Parse(txt_PizeoSetX.Text);
                                    yPos = double.Parse(txt_PizeoSetY.Text);
                                    zPos = double.Parse(txt_PizeoSetZ.Text);
                                    limit = xyRange; pitch = xyStep;
                                    _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_XY_Step1;
                                }
                                else if (xyRun == 1 && zRun == 0)
                                {
                                    xPos = double.Parse(txt_PizeoNowX.Text);
                                    yPos = double.Parse(txt_PizeoNowY.Text);
                                    zPos = double.Parse(txt_PizeoNowZ.Text);
                                    limit = zRange; pitch = zStep;
                                    _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Z_Step1;
                                }
                                else if (xyRun == 1 && zRun == 1)
                                {
                                    xPos = double.Parse(txt_PizeoNowX.Text);
                                    yPos = double.Parse(txt_PizeoNowY.Text);
                                    zPos = double.Parse(txt_PizeoNowZ.Text);
                                    limit = 25; pitch = 3;
                                    _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_XY_Step1;
                                }
                                else if (xyRun == 2 && zRun == 1)
                                {
                                    xPos = double.Parse(txt_PizeoNowX.Text);
                                    yPos = double.Parse(txt_PizeoNowY.Text);
                                    zPos = double.Parse(txt_PizeoNowZ.Text);
                                    limit = 100; pitch = 20;
                                    _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Z_Step1;
                                }
                                else
                                {
                                    _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_StepEnd;
                                }

                            }
                            else
                            {
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_StepEnd;
                            }
                            break;

                        #region  ==== xy align  ======
                        case enumLaser_AlignFlowStep.LaserAlign_XY_Step1:

                            AlignPM = 0;
                            if (LaserPOS_XY_SpiralSet(_laserPosList, xPos, yPos, zPos, limit, pitch))
                            {
                                posNum = _laserPosList.Count;
                                runCount = 0;
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_XY_Step2;
                            }
                            else
                            {
                                throw new AggregateException("校驗位置設定異常");
                            }
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_XY_Step2:
                            if (runCount >= posNum)
                            {
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_XY_Step4;      //校正結束        
                            }
                            else
                            {
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_XY_Step3;      // run loop
                            }

                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_XY_Step3:

                            if (PizeoABSMove(_laserPosList[runCount].X, _laserPosList[runCount].Y, _laserPosList[runCount].Z))
                            {
                                GetAlingLaserPM();
                                _laserPosList[runCount].PM = AlignPM;
                                AlignLogWrite(AlignPM);

                                if (AlignPM >= _msPosData.LaserAlignTargetPower) //校正功率門檻
                                    _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_XY_Step4;
                                else
                                    _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_XY_Step2;
                                runCount++;
                            }
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_XY_Step4:
                            if (_laserPosList.Any())
                            {
                                // 1. 取得最大的 PM 值
                                double maxPmValue = _laserPosList.Max(pos => pos.PM);

                                // 2. 找出所有 PM 等於最大值的物件 (可能有多筆)
                                List<LaserPos> maxPmPositions = _laserPosList.Where(pos => pos.PM == maxPmValue).ToList();

                                // 如果您只需要第一筆（通常情況下），可以這樣取：
                                maxPmLaserPos = maxPmPositions.First();
                                if (PizeoABSMove(maxPmLaserPos.X, maxPmLaserPos.Y, maxPmLaserPos.Z))
                                {
                                    _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step5;
                                    LogMsgAdd(MList_Log, lb_HistoryList, $"最大的 PM 值為: {maxPmValue}", tmpListStr);
                                    LogMsgAdd(MList_Log, lb_HistoryList, $" X 座標為: {maxPmLaserPos.X}  Y 座標為: {maxPmLaserPos.Y}  Z 座標為: {maxPmLaserPos.Z}", tmpListStr);
                                    xyRun++;
                                }
                            }
                            else
                            {
                                throw new AggregateException("校正位置列表為空，無法取得最大值。");
                            }
                            break;
                        #endregion

                        #region  ==== z align  ======
                        case enumLaser_AlignFlowStep.LaserAlign_Z_Step1:

                            AlignPM = 0;
                            if (LaserPOS_Z_Set(_laserPosList, xPos, yPos, zPos, limit, limit, limit, pitch))
                            {
                                posNum = _laserPosList.Count;
                                runCount = 0;
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Z_Step2;
                            }
                            else
                            {
                                throw new AggregateException("校驗位置設定異常");
                            }
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Z_Step2:
                            if (runCount >= posNum)
                            {
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Z_Step4;      //校正結束        
                            }
                            else
                            {
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Z_Step3;      // run loop
                            }

                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Z_Step3:

                            if (PizeoABSMove(_laserPosList[runCount].X, _laserPosList[runCount].Y, _laserPosList[runCount].Z))
                            {
                                GetAlingLaserPM();
                                _laserPosList[runCount].PM = AlignPM;
                                AlignLogWrite(AlignPM);
                                if (AlignPM >= _msPosData.LaserAlignTargetPower) //校正功率門檻
                                    _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Z_Step4;
                                else
                                    _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Z_Step2;
                                runCount++;
                            }
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Z_Step4:
                            if (_laserPosList.Any())
                            {
                                // 1. 取得最大的 PM 值
                                double maxPmValue = _laserPosList.Max(pos => pos.PM);

                                // 2. 找出所有 PM 等於最大值的物件 (可能有多筆)
                                List<LaserPos> maxPmPositions = _laserPosList.Where(pos => pos.PM == maxPmValue).ToList();

                                // 如果您只需要第一筆（通常情況下），可以這樣取：
                                maxPmLaserPos = maxPmPositions.First();


                                if (PizeoABSMove(maxPmLaserPos.X, maxPmLaserPos.Y, maxPmLaserPos.Z))
                                {
                                    _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step5;
                                    zRun++;
                                    LogMsgAdd(MList_Log, lb_HistoryList, $"最大的 PM 值為: {maxPmValue}", tmpListStr);
                                    LogMsgAdd(MList_Log, lb_HistoryList, $" X 座標為: {maxPmLaserPos.X}  Y 座標為: {maxPmLaserPos.Y}  Z 座標為: {maxPmLaserPos.Z}", tmpListStr);

                                }
                            }
                            else
                            {
                                throw new AggregateException("校正位置列表為空，無法取得最大值。");
                            }
                            break;


                        #endregion

                        case enumLaser_AlignFlowStep.LaserAlign_StepEnd: //end
                            _RunFlag = false;
                            _EQP_Status = enumEQP_Status.IDLE;
                            _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Wait;
                            LogMsgAdd(MList_Log, lb_HistoryList, "雷射校正完成。", tmpListStr);
                            Invoke(new dele_msgShow(TipMSG_Show), "雷射校正完成。");
                            IO_OutputControl("門鎖關");

                            break;
                    }
                    nowT = DateTime.Now;
                    _timeoutTS = nowT - startT;
                    if (_timeoutTS.Minutes > _timeoutMinute)
                    {
                        _RunFlag = false;
                        _EQP_Status = enumEQP_Status.DOWN;
                        Error_Log.Add($"[Laser Align Error]: 雷射對位超時!");
                        Invoke(new dele_msgShow(ErrMSG_Show), "雷射對位超時!");
                    }


                    Thread.Sleep(30);
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"[Laser Align Error]: {ex.ToString()}");
                Invoke(new dele_msgShow(ErrMSG_Show), "雷射定位失敗!");
                _EQP_Status = enumEQP_Status.DOWN;
                _RunFlag = false;
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Laser校正 log寫入
        /// </summary>
        /// <param name="laserPower"></param>
        private void AlignLogWrite(double laserPower)
        {
            try
            {
                if (cbAlignLog.Checked)
                    LogMsgAdd(MList_Log, lb_HistoryList, $"X:[{ double.Parse(txt_PizeoNowX.Text)}] Y:[{ double.Parse(txt_PizeoNowY.Text)}] Z:[{ double.Parse(txt_PizeoNowZ.Text)}] Power[{laserPower}]mW", tmpListStr);
            }
            catch (Exception ex)
            {

            }


        }

        /// <summary>
        /// 雷射平面校正流程
        /// </summary>
        private async Task<bool> LaserAreaFlow()
        {
            bool result = false;
            _RunFlag = true;
            double xPos = 0, yPos = 0, zPos = 0;
            double limit = 300, pitch = 30;
            DateTime startT = DateTime.Now, nowT;
            TimeSpan _timeoutTS = new TimeSpan();
            int _timeoutMinute = 60;//10 min

            int posNum = 0; int runCount = 0;
            int xyRun = 0, zRun = 0;
            LaserPos maxPmLaserPos = new LaserPos();
            int delayTime = int.Parse(txt_PM_LaserOffTime.Text);

            int xyRange = 0, xyStep = 0, zRange = 0, zStep = 0;

            try
            {
                xyRange = int.Parse(txt_LaserXY_Range.Text);
                xyStep = int.Parse(txt_LaserXY_Step.Text);
                zRange = int.Parse(txt_LaserZ_Range.Text);
                zStep = int.Parse(txt_LaserZ_Step.Text);
                while (_RunFlag)
                {
                    switch (_LaserAlignFlowStep)
                    {
                        case enumLaser_AlignFlowStep.LaserAlign_Step1:
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                            {
                                IO_OutputControl("門鎖開");
                                startT = DateTime.Now;
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step2;
                            }
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Step2:
                            if (StageABS_Move(_msPosData.M_PowerMeter_X, _msPosData.M_PowerMeter_Y, _msPosData.M_Wait_Z, true, true, true))
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step3;
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Step3:
                            if (StageABS_Move(_msPosData.M_PowerMeter_X, _msPosData.M_PowerMeter_Y, _msPosData.M_PowerMeter_Z, true, true, true))
                            {
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step4; //PM 位置到
                            }
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Step4:
                            _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step1;
                            _laserPosList.Clear();
                            GetAlingLaserPM();
                            _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step5;
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Step5:
                            //if (AlignPM < _msPosData.LaserAlignTargetPower) //校正功率門檻
                            {
                                //if (xyRun == 0)
                                {
                                    xPos = double.Parse(txt_PizeoSetX.Text);
                                    yPos = double.Parse(txt_PizeoSetY.Text);
                                    zPos = double.Parse(txt_PizeoSetZ.Text);
                                    limit = xyRange; pitch = xyStep;
                                    _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_XY_Step1;
                                }
                            }

                            break;

                        #region  ==== xy align  ======
                        case enumLaser_AlignFlowStep.LaserAlign_XY_Step1:

                            AlignPM = 0;
                            if (LaserPOS_XY_SpiralSet(_laserPosList, xPos, yPos, zPos, limit, pitch))
                            {
                                posNum = _laserPosList.Count;
                                runCount = 0;
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_XY_Step2;
                            }
                            else
                            {
                                throw new AggregateException("校驗位置設定異常");
                            }
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_XY_Step2:
                            if (runCount >= posNum)
                            {
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_XY_Step4;      //校正結束        
                            }
                            else
                            {
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_XY_Step3;      // run loop
                            }

                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_XY_Step3:

                            if (PizeoABSMove(_laserPosList[runCount].X, _laserPosList[runCount].Y, _laserPosList[runCount].Z))
                            {
                                GetAlingLaserPM();
                                _laserPosList[runCount].PM = AlignPM;
                                AlignLogWrite(AlignPM);
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_XY_Step2;
                                runCount++;
                            }
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_XY_Step4:
                            if (_laserPosList.Any())
                            {
                                // 1. 過濾小於目標值的座標
                                _laserPosList = _laserPosList.Where(pos => pos.PM >= _msPosData.LaserAlignTargetPower).ToList();

                                // 2. 顯示座標區間
                                foreach (LaserPos _pos in _laserPosList)
                                {
                                    LogMsgAdd(MList_Log, lb_HistoryList, $"[Cetner Area] X[{_pos.X}]um  Y[{_pos.Y}]um  Z[{_pos.Z}]um PM[{_pos.PM}].", tmpListStr);
                                }

                                //3 取得中心
                                LaserPos centerPos = CoordinateTool.GetRectangleCenter(_laserPosList);
                                if (PizeoABSMove(centerPos.X, centerPos.Y, centerPos.Z))
                                {
                                    _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_StepEnd;
                                    LogMsgAdd(MList_Log, lb_HistoryList, $"[Center] X[{centerPos.X}]um  Y[{centerPos.Y}]um  Z[{centerPos.Z}]um].", tmpListStr);
                                    //xyRun++;
                                }
                            }
                            else
                            {
                                throw new AggregateException("校正位置列表為空，無法取得最大值。");
                            }
                            break;
                        #endregion


                        case enumLaser_AlignFlowStep.LaserAlign_StepEnd: //end
                            _RunFlag = false;
                            _EQP_Status = enumEQP_Status.IDLE;
                            _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Wait;
                            LogMsgAdd(MList_Log, lb_HistoryList, "雷射水平校正完成。", tmpListStr);
                            Invoke(new dele_msgShow(TipMSG_Show), "雷射水平校正完成。");
                            IO_OutputControl("門鎖關");

                            break;
                    }

                    nowT = DateTime.Now;
                    _timeoutTS = nowT - startT;
                    if (_timeoutTS.Minutes > _timeoutMinute)
                    {
                        _RunFlag = false;
                        _EQP_Status = enumEQP_Status.DOWN;
                        Error_Log.Add($"[Laser Align Error]: 雷射對位超時!");
                        Invoke(new dele_msgShow(ErrMSG_Show), "雷射對位超時!");
                    }


                    Thread.Sleep(30);
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"[Laser Align Error]: {ex.ToString()}");
                Invoke(new dele_msgShow(ErrMSG_Show), "雷射定位失敗!");
                _EQP_Status = enumEQP_Status.DOWN;
                _RunFlag = false;
                result = false;
            }
            return result;
        }

        /// <summary>
        /// 雷射垂直校正流程
        /// </summary>
        private async Task<bool> LaserVerticalFlow()
        {
            bool result = false;
            _RunFlag = true;
            double xPos = 0, yPos = 0, zPos = 0;
            double limit = 300, pitch = 30;
            DateTime startT = DateTime.Now, nowT;
            TimeSpan _timeoutTS = new TimeSpan();
            int _timeoutMinute = 60;//10 min

            int posNum = 0; int runCount = 0;
            int xyRun = 0, zRun = 0;
            LaserPos maxPmLaserPos = new LaserPos();
            int delayTime = int.Parse(txt_PM_LaserOffTime.Text);

            int xyRange = 0, xyStep = 0, zRange = 0, zStep = 0;

            try
            {
                xyRange = int.Parse(txt_LaserXY_Range.Text);
                xyStep = int.Parse(txt_LaserXY_Step.Text);
                zRange = int.Parse(txt_LaserZ_Range.Text);
                zStep = int.Parse(txt_LaserZ_Step.Text);
                while (_RunFlag)
                {
                    switch (_LaserAlignFlowStep)
                    {
                        case enumLaser_AlignFlowStep.LaserAlign_Step1:
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                            {
                                IO_OutputControl("門鎖開");
                                startT = DateTime.Now;
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step2;
                            }
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Step2:
                            if (StageABS_Move(_msPosData.M_PowerMeter_X, _msPosData.M_PowerMeter_Y, _msPosData.M_Wait_Z, true, true, true))
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step3;
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Step3:
                            if (StageABS_Move(_msPosData.M_PowerMeter_X, _msPosData.M_PowerMeter_Y, _msPosData.M_PowerMeter_Z, true, true, true))
                            {
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step4; //PM 位置到
                            }
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Step4:
                            _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step1;
                            _laserPosList.Clear();
                            GetAlingLaserPM();


                            _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step5;
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Step5:
                            //if (AlignPM < _msPosData.LaserAlignTargetPower) //校正功率門檻
                            {

                                xPos = double.Parse(txt_PizeoNowX.Text);
                                yPos = double.Parse(txt_PizeoNowY.Text);
                                zPos = double.Parse(txt_PizeoNowZ.Text);
                                limit = zRange; pitch = zStep;
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Z_Step1;
                            }
                            break;


                        #region  ==== z align  ======
                        case enumLaser_AlignFlowStep.LaserAlign_Z_Step1:

                            AlignPM = 0;
                            if (LaserPOS_Z_Set(_laserPosList, xPos, yPos, zPos, limit, limit, limit, pitch))
                            {
                                posNum = _laserPosList.Count;
                                runCount = 0;
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Z_Step2;
                            }
                            else
                            {
                                throw new AggregateException("校驗位置設定異常");
                            }
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Z_Step2:
                            if (runCount >= posNum)
                            {
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Z_Step4;      //校正結束        
                            }
                            else
                            {
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Z_Step3;      // run loop
                            }

                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Z_Step3:

                            if (PizeoABSMove(_laserPosList[runCount].X, _laserPosList[runCount].Y, _laserPosList[runCount].Z))
                            {
                                GetAlingLaserPM();
                                _laserPosList[runCount].PM = AlignPM;
                                AlignLogWrite(AlignPM);
                                _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Z_Step2;
                                runCount++;
                            }
                            break;

                        case enumLaser_AlignFlowStep.LaserAlign_Z_Step4:
                            if (_laserPosList.Any())
                            {
                                // 1. 取得最大的 PM 值
                                double maxPmValue = _laserPosList.Max(pos => pos.PM);

                                // 2. 找出所有 PM 等於最大值的物件 (可能有多筆)
                                List<LaserPos> maxPmPositions = _laserPosList.Where(pos => pos.PM == maxPmValue).ToList();

                                // 如果您只需要第一筆（通常情況下），可以這樣取：
                                maxPmLaserPos = maxPmPositions.First();


                                if (PizeoABSMove(maxPmLaserPos.X, maxPmLaserPos.Y, maxPmLaserPos.Z))
                                {
                                    _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_StepEnd;
                                    zRun++;
                                    LogMsgAdd(MList_Log, lb_HistoryList, $"最大的 PM 值為: {maxPmValue}", tmpListStr);
                                    LogMsgAdd(MList_Log, lb_HistoryList, $" X 座標為: {maxPmLaserPos.X}  Y 座標為: {maxPmLaserPos.Y}  Z 座標為: {maxPmLaserPos.Z}", tmpListStr);
                                }
                            }
                            else
                            {
                                throw new AggregateException("校正位置列表為空，無法取得最大值。");
                            }
                            break;


                        #endregion

                        case enumLaser_AlignFlowStep.LaserAlign_StepEnd: //end
                            _RunFlag = false;
                            _EQP_Status = enumEQP_Status.IDLE;
                            _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Wait;
                            LogMsgAdd(MList_Log, lb_HistoryList, "雷射垂直校正完成。", tmpListStr);
                            Invoke(new dele_msgShow(TipMSG_Show), "雷射垂直校正完成。");
                            IO_OutputControl("門鎖關");

                            break;
                    }
                    nowT = DateTime.Now;
                    _timeoutTS = nowT - startT;
                    if (_timeoutTS.Minutes > _timeoutMinute)
                    {
                        _RunFlag = false;
                        _EQP_Status = enumEQP_Status.DOWN;
                        Error_Log.Add($"[Laser Align Error]: 雷射對位超時!");
                        Invoke(new dele_msgShow(ErrMSG_Show), "雷射對位超時!");
                    }


                    Thread.Sleep(30);
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"[Laser Align Error]: {ex.ToString()}");
                Invoke(new dele_msgShow(ErrMSG_Show), "雷射定位失敗!");
                _EQP_Status = enumEQP_Status.DOWN;
                _RunFlag = false;
                result = false;
            }
            return result;
        }


        public class CoordinateTool
        {
            public static LaserPos GetRectangleCenter(List<LaserPos> points)
            {
                LaserPos center = new LaserPos();
                if (points == null || points.Count == 0)
                    throw new ArgumentException("座標列表不能為空");

                double sumX = 0;
                double sumY = 0;
                double sumZ = 0;

                foreach (var p in points)
                {
                    sumX += p.X;
                    sumY += p.Y;
                    sumZ += p.Z;
                }

                double centerX = sumX / points.Count;
                double centerY = sumY / points.Count;
                double centerZ = sumZ / points.Count;

                center.X = centerX;
                center.Y = centerY;
                center.Z = centerZ;

                return center;
            }
        }



        /// <summary>
        /// 讀取PM值設定AlignLaserPM
        /// </summary>
        private void GetAlingLaserPM()
        {
            PM_DataCollect = true;
            int step = 1;
            bool _GetPM = true;
            TimeSpan t1 = new TimeSpan(0, 0, 10);
            Stopwatch tm = new Stopwatch();
            tm.Start();
            while (_GetPM)
            {
                switch (step)
                {
                    case 1:
                        if (Set_PMLaserData())
                        {
                            step = 2;
                        }
                        else
                        {
                            _EQP_Status = enumEQP_Status.DOWN;
                            LogMsgAdd(Error_Log, lb_ErrorList, "功率更新超時", tmpErrStr);
                            Invoke(new dele_msgShow(ErrMSG_Show), "功率更新超時");
                            _GetPM = false;
                        }
                        break;

                    case 2:
                        if (StartMarking())
                        {
                            LaserStatus_Emission = true;
                            step = 3;
                            Invoke(new updatalaserstauts(updatalaser));//2024-05-24
                        }
                        else
                        {
                            _EQP_Status = enumEQP_Status.DOWN;
                            LogMsgAdd(Error_Log, lb_ErrorList, "雷射出光失敗", tmpErrStr);
                            Invoke(new dele_msgShow(ErrMSG_Show), "雷射出光失敗");
                            _GetPM = false;
                        }
                        break;

                    case 3:
                        if (!LaserStatus_Emission)
                        {
                            _GetPM = false;
                            PM_DataCollect = false;

                        }
                        break;
                }

                if (tm.Elapsed >= t1)
                {
                    LogMsgAdd(Error_Log, lb_ErrorList, "雷射出光超時", tmpErrStr);
                    Invoke(new dele_msgShow(ErrMSG_Show), "雷射出光超時");
                    _GetPM = false;
                }

                Thread.Sleep(10);
            }

            Thread.Sleep((int)(_msPosData.LaserTriggerInterval));
        }

        /// <summary>
        /// 設定雷射校驗 XY 螺旋位置設定
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <param name="zPos"></param>
        /// <param name="xLimit"></param>
        /// <param name="yLimit"></param>
        /// <param name="zLimit"></param>
        /// <param name="pitch"></param>
        private bool LaserPOS_XY_SpiralSet(List<LaserPos> posList, double xPos, double yPos, double zPos, double areaLimit, double pitch)
        {
            try
            {
                List<(double, double)> path = GetSpiralPath2(xPos, yPos, areaLimit, pitch);
                posList.Clear();
                LaserPos tmpPos;

                foreach (var coor in path)
                {
                    tmpPos = new LaserPos();
                    tmpPos.X = coor.Item1;
                    tmpPos.Y = coor.Item2;
                    tmpPos.Z = zPos;
                    if (Math.Abs(tmpPos.X) <= 3800 && Math.Abs(tmpPos.Y) <= 3800 && Math.Abs(tmpPos.Z) <= 3800) //硬體極限
                        posList.Add(tmpPos);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        /// <summary>
        /// 設定雷射校驗 Z位置設定
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <param name="zPos"></param>
        /// <param name="xLimit"></param>
        /// <param name="yLimit"></param>
        /// <param name="zLimit"></param>
        /// <param name="pitch"></param>
        private bool LaserPOS_Z_Set(List<LaserPos> posList, double xPos, double yPos, double zPos, double xLimit, double yLimit, double zLimit, double pitch)
        {
            try
            {
                posList.Clear();
                int zNum = (int)(zLimit / pitch);
                double startz = zPos - zNum * pitch;
                LaserPos tmpPos;

                for (int z = 0; z <= 2 * zNum; z++)
                {
                    tmpPos = new LaserPos();
                    tmpPos.X = xPos;
                    tmpPos.Y = yPos;
                    tmpPos.Z = startz + pitch * z; ;
                    if (Math.Abs(tmpPos.X) <= 3800 && Math.Abs(tmpPos.Y) <= 3800 && Math.Abs(tmpPos.Z) <= 3800) //硬體極限
                        posList.Add(tmpPos);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool PizeoABSMove(double x, double y, double z)
        {
            double[] tmpPos = new double[3];

            x = (int)x; y = (int)y; z = (int)z;

            if (_CU30CL.ctrRec.Position[1] == x && _CU30CL.ctrRec.Position[0] == y && _CU30CL.ctrRec.Position[2] == z)
            {
                return true;
            }
            else
            {
                tmpPos[1] = x;
                tmpPos[0] = y;
                tmpPos[2] = z;
                _CU30CL.CloseLoopServoMove(100, tmpPos);
                return false;
            }
        }


        private void B_Mark_VisionMove_Click(object sender, EventArgs e)
        {
            int stNo = (int)Num_ST_NO.Value;
            int grabNo = (int)Num_GrabNo.Value;
            if (MessageBox.Show($"是否移動至 Mark[{grabNo}]位置?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (_fileLoad && _stCal_OK && _stMHeight_OK)
                {
                    if (FlowAllow())
                    {
                        Task.Run(() => { GrabPointMove(stNo, grabNo); });
                        _EQP_Status = enumEQP_Status.MANU;
                    }
                }
                else
                {
                    if (!_fileLoad)
                        MessageBox.Show("尚未載入座標檔!", "警告");
                    if (!_stCal_OK)
                        MessageBox.Show("尚未取像流程!", "警告");
                    if (!_stMHeight_OK)
                        MessageBox.Show("尚未測高流程!", "警告");
                }
            }
        }

        private void B_Measure_VisionMove_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行測高點位置流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                int stNo = (int)Num_ST_NO.Value;
                int hightNo = (int)Num_MHeight_No.Value;
                if (_fileLoad && _stCal_OK && _stMHeight_OK)
                {
                    if (FlowAllow())
                    {
                        Task.Run(() => { MHeightPointMove(stNo, hightNo); });
                        _EQP_Status = enumEQP_Status.MANU;
                    }
                }
                else
                {
                    if (!_fileLoad)
                        MessageBox.Show("尚未載入座標檔!", "警告");
                    if (!_stCal_OK)
                        MessageBox.Show("尚未取像流程!", "警告");
                    if (!_stMHeight_OK)
                        MessageBox.Show("尚未測高流程!", "警告");
                }

            }
        }

        private void B_SB_VisionMove_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行植球位置流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                int stNo = (int)Num_ST_NO.Value;
                int padNo = (int)Num_MovePOS_Pad_NO.Value;
                int ballNo = (int)Num_MovePOS_Ball_NO.Value;

                if (_fileLoad && _stCal_OK && _stMHeight_OK)
                {
                    if (FlowAllow())
                    {
                        Task.Run(() => { ST_PointMove(stNo, padNo, ballNo); });
                        _EQP_Status = enumEQP_Status.MANU;
                    }
                }
                else
                {
                    if (!_fileLoad)
                        MessageBox.Show("尚未載入座標檔!", "警告");
                    if (!_stCal_OK)
                        MessageBox.Show("尚未取像流程!", "警告");
                    if (!_stMHeight_OK)
                        MessageBox.Show("尚未測高流程!", "警告");
                }


            }
        }

        /// <summary>
        /// 功率量測流程
        /// </summary>
        private async Task<bool> PowerMeterFlow()
        {
            bool result = false;
            _RunFlag = true;

            Stopwatch airT = new Stopwatch();
            Stopwatch vacuumT = new Stopwatch();
            int errC = 0;

            try
            {
                while (_RunFlag)
                {
                    switch (_PowerMeterFlowStep)
                    {
                        case enumPowerMeterFlowStep.PowerMeter_Step1:
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                            {
                                if (Set_PMLaserData())
                                    _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step2;
                                else
                                {
                                    errC++;
                                    if (errC > 50)
                                        throw new ArgumentException("功率更新超時");
                                }
                            }

                            break;

                        case enumPowerMeterFlowStep.PowerMeter_Step2:
                            if (StageABS_Move(_msPosData.M_PowerMeter_X, _msPosData.M_PowerMeter_Y, _msPosData.M_Wait_Z, true, true, true))
                                _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step3;
                            break;

                        case enumPowerMeterFlowStep.PowerMeter_Step3:
                            if (StageABS_Move(_msPosData.M_PowerMeter_X, _msPosData.M_PowerMeter_Y, _msPosData.M_PowerMeter_Z, true, true, true))
                            {
                                _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step4;
                                Thread.Sleep(100);
                                PM_DataCollect = true;
                            }

                            break;

                        case enumPowerMeterFlowStep.PowerMeter_Step4:

                            //雷射開
                            if (StartMarking())
                            {
                                LaserStatus_Emission = true;
                                Invoke(new updatalaserstauts(updatalaser));//2024-05-24
                                Thread.Sleep(100);
                                result = true;
                                _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step5;
                            }
                            else
                            {
                                _EQP_Status = enumEQP_Status.DOWN;
                                LogMsgAdd(Error_Log, lb_ErrorList, "雷射出光失敗", tmpErrStr);
                                _RunFlag = false;
                                return false;
                            }

                            break;

                        case enumPowerMeterFlowStep.PowerMeter_Step5:
                            //雷射雕刻完成
                            if (!LaserStatus_Emission)
                            {
                                _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step6;
                                PM_DataCollect = false;
                            }

                            break;

                        case enumPowerMeterFlowStep.PowerMeter_Step6:
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                            {
                                _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Wait;
                                _EQP_Status = enumEQP_Status.IDLE;
                                _RunFlag = false;
                            }
                            break;
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                _EQP_Status = enumEQP_Status.DOWN;
                Error_Log.Add($"[PowerMeterFlow Error]: {ex.ToString()}");
                _RunFlag = false;
                result = false;
            }
            return result;
        }

        /// <summary>
        /// 功率量測位置移動流程
        /// </summary>
        private async Task<bool> PowerMeterPOS_MoveFlow()
        {
            bool result = false;
            _RunFlag = true;

            Stopwatch airT = new Stopwatch();
            Stopwatch vacuumT = new Stopwatch();
            int errC = 0;

            try
            {
                while (_RunFlag)
                {
                    switch (_PowerMeterFlowStep)
                    {
                        case enumPowerMeterFlowStep.PowerMeter_Step1:
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                            {
                                _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step2;
                            }

                            break;

                        case enumPowerMeterFlowStep.PowerMeter_Step2:
                            if (StageABS_Move(_msPosData.M_PowerMeter_X, _msPosData.M_PowerMeter_Y, _msPosData.M_Wait_Z, true, true, true))
                                _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step3;
                            break;

                        case enumPowerMeterFlowStep.PowerMeter_Step3:
                            if (StageABS_Move(_msPosData.M_PowerMeter_X, _msPosData.M_PowerMeter_Y, _msPosData.M_PowerMeter_Z, true, true, true))
                            {
                                _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Wait;
                                _RunFlag = false;
                                _EQP_Status = enumEQP_Status.IDLE;
                                return true;
                            }
                            break;
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                _EQP_Status = enumEQP_Status.DOWN;
                Error_Log.Add($"[PowerMeterPOS_MoveFlow Error]: {ex.ToString()}");
                _RunFlag = false;
                result = false;
            }
            return result;
        }

        /// <summary>
        /// 植球校驗位置移動流程
        /// </summary>
        private async Task<bool> BallAlignPOS_MoveFlow()
        {
            bool result = false;
            _RunFlag = true;
            int step = 1;
            try
            {
                while (_RunFlag)
                {
                    switch (step)
                    {
                        case 1:
                            if (StageABS_Move(0, 0, _msPosData.M_Wait_Z, false, false, true))
                            {
                                step = 2;
                            }

                            break;

                        case 2:
                            if (StageABS_Move(_msPosData.M_AlignBall_X, _msPosData.M_AlignBall_Y, _msPosData.M_Wait_Z, true, true, true))
                                step = 3;
                            break;

                        case 3:
                            if (StageABS_Move(_msPosData.M_AlignBall_X, _msPosData.M_AlignBall_Y, _msPosData.M_AlignBall_Z, true, true, true))
                            {
                                step = 0;
                                _RunFlag = false;
                                _EQP_Status = enumEQP_Status.IDLE;
                                return true;
                            }
                            break;
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                _EQP_Status = enumEQP_Status.DOWN;
                Error_Log.Add($"[BallAlignPOS_MoveFlow Error]: {ex.ToString()}");
                _RunFlag = false;

            }
            return false;
        }

        /// <summary>
        ///出料流程
        /// </summary>
        private bool UnloadPosMoveFlow()
        {
            bool result = false;
            _RunFlag = true;
            int step = 0;
            try
            {
                while (_RunFlag)
                {
                    switch (step)
                    {
                        case 0:
                            if (StageABS_Move(0, 0, _msPosData.M_Unload_Z, false, false, true))
                                step = 1;
                            break;

                        case 1:
                            if (StageABS_Move(_msPosData.M_Unload_X, _msPosData.M_Unload_Y, _msPosData.M_Unload_Z, true, true, true))
                                step = 2;
                            break;

                        case 2:
                            _RunFlag = false;
                            _EQP_Status = enumEQP_Status.IDLE;
                            return true;
                            break;


                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"[UnloadPosMoveFlow Error]: {ex.ToString()}");
                _EQP_Status = enumEQP_Status.DOWN;
                _RunFlag = false;
                result = false;
            }
            return result;
        }

        private void B_BallOutStart_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                double degree = double.Parse(E_BallOut_Angle.Text);
                int air = 0;
                int delay = int.Parse(E_BallOutDelay.Text);
                _ballOutTotalCount = int.Parse(E_BallOut_Num.Text);
                if (FlowAllow())
                    Task.Run(() => { BallOutTestFlow(degree, air, delay); });

            }
        }

        private void B_BallOutStop_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否停止流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _ballOutRun = false;
                //_AZD_Controller.Stop();
                Motion.mAcm_AxStopDec(m_Axishand[0]);
                AZD_ReadLock = false;
            }
        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btn_LaserValveSet_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (double.Parse(txt_NozzleBall_Valve.Text) > 20)
                        txt_NozzleBall_Valve.Text = "20";
                    if (double.Parse(txt_NozzleBall_Valve.Text) < 0)
                        txt_NozzleBall_Valve.Text = "0";


                    LogMsgAdd(MList_Log, lb_HistoryList, "執行出球參數儲存。", tmpListStr);

                    _recipeData.SYSParam.NozzleSB_Pressure = Convert.ToDouble(txt_NozzleBall_Pressure.Text);
                    _recipeData.SYSParam.NozzleNoSB_Pressure = Convert.ToDouble(txt_NozzleNoBall_Pressure.Text);
                    _recipeData.SYSParam.NozzleSB_ValveNum = Convert.ToDouble(txt_NozzleBall_Valve.Text);
                    _recipeData.SYSParam.SB_AirJudgeDelay = Convert.ToInt16(txt_SB_AirJudgeDelay.Text);
                    _recipeData.SYSParam.NoSB_AirJudgeDelay = Convert.ToInt16(txt_NoSB_AirJudgeDelay.Text);
                    _recipeData.SYSParam.SB_AirJudgDiff = Convert.ToBoolean(chk_BallAirDiff_On.Checked);
                    _recipeData.SYSParam.NozzleSB_Diff_Value = Convert.ToDouble(txt_BallAirDiff.Text);

                    _recipeData.SYSParam.LoadBallRetry = Convert.ToInt16(txt_LoadBallRetry.Text);
                    _recipeData.SYSParam.EmissionRetry = Convert.ToInt16(txt_EmissionRetry.Text);
                    _recipeData.SYSParam.ClearRetry = Convert.ToInt16(txt_CleanRetry.Text);
                    _recipeData.SYSParam.ClearSB_Count = Convert.ToInt16(txt_ClearSB_Count.Text);
                    _recipeData.SYSParam.FourP_HeightLimit = Convert.ToInt16(txt_4P_HeightLimit.Text);
                    _recipeData.Save_Data(RecipeFileName);
                    Setting_UI();
                }
            }
            catch (Exception ex)
            {

            }

        }

        private void btn_SB_Mode_Start_Click(object sender, EventArgs e)
        {
            string str = "";
            int index = 1;
            if (rd_SinglePad.Checked)
            {
                str = "單Pad模式";
                index = 1;
            }
            else if (rd_SingleBall.Checked)
            {
                str = "單球模式";
                index = 2;
            }
            else if (rd_SinglePoint.Checked)
            {
                str = "單點模式";
                index = 3;
            }

            if (MessageBox.Show($"執行[{str}]植球流程", "警告", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                // 需補上 home / alarm判定
                if (FlowAllow())
                {
                    ChangeCVX_Run();

                    tact_time.Restart();
                    ushort value = (ushort)(_recipeData.SYSParam.NozzleSB_ValveNum * 10);
                    if (UpdateProportionalValve(value))
                    {
                        tact_time.Reset();
                    }
                    else
                    {
                        if (tact_time.ElapsedMilliseconds > 5000)
                        {
                            index = 0;
                            _EQP_Status = enumEQP_Status.DOWN;
                            LogMsgAdd(Error_Log, lb_ErrorList, "比例閥更新超時", tmpErrStr);
                            Invoke(new dele_msgShow(ErrMSG_Show), "比例閥更新超時");
                            tact_time.Reset();
                        };
                    }

                    if (index == 1 || index == 2)
                    {
                        if (_fileLoad && _stCal_OK && _stMHeight_OK)
                        {

                            int padNo = (int)num_ST_Pad_No.Value;
                            int ballNo = (int)num_ST_Ball_No.Value;
                            if (index == 1)
                            {
                                LogMsgAdd(MList_Log, lb_HistoryList, $"執行[{str}]植球流程 Pad[{padNo}] SB[{ballNo}]。", tmpListStr);
                                _EQP_Status = enumEQP_Status.MANU;
                                Task.Run(() => { SinglePad_Flow(padNo, ballNo); });

                            }
                            else if (index == 2)
                            {
                                LogMsgAdd(MList_Log, lb_HistoryList, $"執行[{str}]植球流程 Pad[{padNo}] SB[{ballNo}]。", tmpListStr);
                                _EQP_Status = enumEQP_Status.MANU;
                                Task.Run(() => { SingleBall_Flow(padNo, ballNo); });
                            }
                        }
                        else
                        {
                            if (!_fileLoad)
                                MessageBox.Show("尚未載入座標檔!", "警告");
                            if (!_stCal_OK)
                                MessageBox.Show("尚未取像流程!", "警告");
                            if (!_stMHeight_OK)
                                MessageBox.Show("尚未測高流程!", "警告");
                        }
                    }
                    else if (index == 3)
                    {
                        LogMsgAdd(MList_Log, lb_HistoryList, $"執行[{str}]植球流程。", tmpListStr);
                        _EQP_Status = enumEQP_Status.MANU;
                        Task.Run(() => { SinglePoint_Flow(); });
                    }

                }

            }
        }

        /// <summary>
        /// ST 流程資料初始
        /// </summary>
        private void ST_FlowDataInit()
        {
            _stCal_OK = false;
            _stMHeight_OK = false;
            PublicData.ST_ExpansionContractionX = 1;
            PublicData.ST_ExpansionContractionY = 1;
            PublicData.ST_V_CalData.Init();
            PublicData.ST_H_CalData.Init();
            PublicData.ST_HeightZ = 0;
        }

        /// <summary>
        ///入料流程
        /// </summary>
        private bool LoadPosMoveFlow()
        {
            bool result = false;
            _RunFlag = true;
            int step = 0;
            try
            {
                while (_RunFlag)
                {
                    switch (step)
                    {
                        case 0:
                            if (StageABS_Move(0, 0, _msPosData.M_Unload_Z, false, false, true))
                                step = 1;
                            break;

                        case 1:
                            if (StageABS_Move(_msPosData.M_Load_X, _msPosData.M_Load_Y, _msPosData.M_Unload_Z, true, true, false))
                                step = 2;
                            break;

                        case 2:
                            if (StageABS_Move(_msPosData.M_Load_X, _msPosData.M_Load_Y, _msPosData.M_Load_Z, true, true, true))
                                step = 3;
                            break;

                        case 3:
                            _EQP_Status = enumEQP_Status.IDLE;
                            _RunFlag = false;
                            result = true;
                            break;


                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"[LoadPosMoveFlow Error]: {ex.ToString()}");
                _EQP_Status = enumEQP_Status.DOWN;
                _RunFlag = false;
                result = false;
            }
            return result;
        }

        /// <summary>
        ///模組更換位置流程
        /// </summary>
        private bool ModelCHMoveFlow()
        {
            bool result = false;
            _RunFlag = true;
            int step = 0;
            try
            {
                while (_RunFlag)
                {
                    switch (step)
                    {
                        case 0:
                            if (StageABS_Move(0, 0, _msPosData.M_ModelCH_Z, false, false, true))
                                step = 1;
                            break;

                        case 1:
                            if (StageABS_Move(_msPosData.M_ModelCH_X, _msPosData.M_ModelCH_Y, _msPosData.M_ModelCH_Z, true, true, false))
                                step = 2;
                            break;

                        case 2:
                            if (StageABS_Move(_msPosData.M_ModelCH_X, _msPosData.M_ModelCH_Y, _msPosData.M_ModelCH_Z, true, true, true))
                                step = 3;
                            break;

                        case 3:
                            _EQP_Status = enumEQP_Status.IDLE;
                            _RunFlag = false;
                            result = true;
                            break;


                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"[LoadPosMoveFlow Error]: {ex.ToString()}");
                _EQP_Status = enumEQP_Status.DOWN;
                _RunFlag = false;
                result = false;
            }
            return result;
        }


        private void btn_SB_OffsetSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    LogMsgAdd(MList_Log, lb_HistoryList, "執行植球Offset參數儲存。", tmpListStr);
                    //offset  x y 600um   z -1000um              um->mm
                    _recipeData.SB.SB_OffsetX = Convert.ToDouble(txt_SB_OffsetX.Text) / 1000;
                    _recipeData.SB.SB_OffsetY = Convert.ToDouble(txt_SB_OffsetY.Text) / 1000;
                    _recipeData.SB.SB_OffsetZ = Convert.ToDouble(txt_SB_OffsetZ.Text) / 1000;

                    if (_sbGrab1_OK && _sbMHeight_OK)
                    {
                        if (SB_CalData())
                        {
                            _sbCal_OK = true;
                            LogMsgAdd(MList_Log, lb_HistoryList, $"Offset重新計算完成。", tmpListStr);
                        }

                    }

                    _recipeData.Save_Data(RecipeFileName);
                }
            }
            catch (Exception)
            {

            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            _LSG075A_4_Control.SetBright(1, 2, 3, 4);
            //_LSG075A_4_Control.GetBright();
            //_LSG075A_4_Control.GetBrightCurrent();
        }

        private void btn_LightSave_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行光源參數儲存。", tmpListStr);
                _recipeData.SYSParam.NozzleXY_Light = Convert.ToInt32(txt_NozzleXY_Light.Text);
                _recipeData.SYSParam.NozzleZ_Light = Convert.ToInt32(txt_NozzleZ_Light.Text);
                _recipeData.SYSParam.Coaxial_Light = Convert.ToInt32(txt_CoaxialLight.Text);
                _recipeData.SYSParam.Ring_Light = Convert.ToInt32(txt_RingLight.Text);
                _recipeData.Save_Data(RecipeFileName);
                Task.Run(() =>
                {
                    SetLight();
                });
            }
        }

        /// <summary>
        /// 對位燈源
        /// </summary>
        /// <param name="_serialPort"></param>
        /// <param name="value"></param>
        private void AlignLight(SerialPort _serialPort, int value)
        {
            if (_serialPort.IsOpen)
            {
                Sum_Light = 0x00;
                SendData_Light[0] = 0x89;
                SendData_Light[1] = 0x55;
                SendData_Light[2] = 0xAA;
                SendData_Light[3] = 0x23;
                SendData_Light[4] = 0x00;
                if (value > 255)
                {
                    SendData_Light[5] = 0x10;
                    SendData_Light[6] = Convert.ToByte(value - 256);
                }
                else
                {
                    SendData_Light[5] = 0x00;
                    SendData_Light[6] = Convert.ToByte(value);
                }
                SendData_Light[7] = 0x00;
                for (int i = 0; i < 8; i++)
                {
                    Sum_Light += SendData_Light[i];
                    SendData_Light[8] = Convert.ToByte(Convert.ToInt16(Sum_Light) % 256);
                }
                _serialPort.Write(SendData_Light, 0, SendData_Light.Length);
            }
            else
            {
                MessageBox.Show("對位燈源控制器沒連線");
            }
        }

        private void btn_NozzleXY_Light_Set_Click(object sender, EventArgs e)
        {
            byte v1 = Convert.ToByte(txt_NozzleXY_Light.Text);
            byte v2 = Convert.ToByte(txt_NozzleZ_Light.Text);
            Task.Run(() =>
            {
                SetLight(v1, v2, 0, 0);
            });
        }

        private void btn_NozzleZ_Light_Set_Click(object sender, EventArgs e)
        {
            byte v1 = Convert.ToByte(txt_NozzleXY_Light.Text);
            byte v2 = Convert.ToByte(txt_NozzleZ_Light.Text);
            Task.Run(() =>
            {
                SetLight(v1, v2, 0, 0);
            });
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab.Text == "系統設定")
            {
                if (_LSG075A_4_Control != null)
                    _LSG075A_4_Control.GetBright();

                if (_Keyence_CVX != null && _Keyence_CVX.Connected)
                {
                    int no = 0;
                    if (CVX_RecipeNoGet(out no))
                    {
                        L_CVX_RecipeNo.Text = no.ToString();
                    }
                }
            }


        }



        private void btn_Set_S_LaserData_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Set_S_LaserData();

            }
        }

        private void btn_Set_P_LaserData_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Set_P_LaserData();

            }
        }

        private void btn_Set_G_LaserData_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Set_G_LaserData();

            }
        }

        private void btn_Set_S_LaserData2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Set_S_LaserData2();

            }
        }

        private void btn_Set_P_LaserData2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Set_P_LaserData2();

            }
        }

        private void btn_Set_G_LaserData2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Set_G_LaserData2();

            }
        }

        private void btn_Load_S_LaserData_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Load_AX_File();
                MessageParam();
            }
        }

        /// <summary>
        /// 軸比較
        /// </summary>
        /// <param name="nowPos"></param>
        /// <param name="targetPos"></param>
        bool PositionCheck(double nowPos, double targetPos)
        {
            bool result = false;
            if (Math.Abs(nowPos - targetPos) <= 0.003)
            {
                result = true;
            }
            return result;
        }


        /// <summary>
        /// 計算ST偏移
        /// </summary>
        /// <returns></returns>
        bool ST_CalData()
        {
            bool result = false;
            try
            {
                PublicData.ST_V_CalData.DX = 0; PublicData.ST_V_CalData.DY = 0; PublicData.ST_V_CalData.DQ = 0;

                PublicData.ST_ExpansionContractionX = Math.Abs((PublicData.ST_AlignH.End.X - PublicData.ST_AlignH.Start.X) / (grabPoint[2].X - grabPoint[0].X));
                PublicData.ST_ExpansionContractionY = Math.Abs((PublicData.ST_AlignV.End.Y - PublicData.ST_AlignV.Start.Y) / (grabPoint[1].Y - grabPoint[0].Y));
                TJJS_Line baseline = new TJJS_Line();
                TJJS_Line newbaseline = new TJJS_Line();

                baseline.Set_Value(new TJJS_Point(baseGrabPoint[0].X, baseGrabPoint[0].Y), new TJJS_Point(baseGrabPoint[1].X, baseGrabPoint[1].Y));

                PublicData.ST_V_CalData.DQ = PublicData.ST_AlignV.V.Angle.d - baseline.V.Angle.d;
                newbaseline = baseline.Rotate(baseline.Start, PublicData.ST_V_CalData.DQ);

                PublicData.ST_V_CalData.DX = newbaseline.Mid.X - PublicData.ST_AlignV.Mid.X;
                PublicData.ST_V_CalData.DY = newbaseline.Mid.Y - PublicData.ST_AlignV.Mid.Y;

                LogMsgAdd(MList_Log, lb_HistoryList, $"ST CalData:DX[{PublicData.ST_V_CalData.DX.ToString("0.000")}] DY[{PublicData.ST_V_CalData.DY.ToString("0.000")}] DQ[{PublicData.ST_V_CalData.DQ.ToString("0.000")}]", tmpListStr);

                result = true;

            }
            catch (Exception ex)
            {

            }
            return result;
        }




        /// <summary>
        /// 計算位置
        /// </summary>
        /// <param name="targetP"></param>
        /// <param name="rotateP"></param>
        /// <param name="calData"></param>
        /// <returns></returns>
        private bool PointMove(TJJS_Point targetP, TJJS_Point rotateP, stCalData calData, ref TJJS_Point resultP)
        {
            bool result = false;
            TJJS_Point tmpPoint = new TJJS_Point();

            try
            {
                tmpPoint = targetP.Rotate(rotateP, calData.DQ);


                tmpPoint += new TJJS_Point(calData.DX, calData.DY);
                resultP.X = tmpPoint.X;
                resultP.Y = tmpPoint.Y;


                result = true;

            }
            catch (Exception ex)
            {
                Error_Log.Add("[PointMove Error]: " + ex.ToString());
            }

            return result;
        }

        private void checkBox_EMode_CheckedChanged(object sender, EventArgs e)
        {
            // Prompt Dialog window to ask user password for security control
            CheckBox checkBox = (CheckBox)sender;
            bool enable = checkBox.Checked;
            string pwd = "123456";
            string admin = "27879121";

            if (enable)
            {
                string sPWD = showDialog("工程模式_密碼", "授權認證");

                if (sPWD == admin)
                {
                    ENG_ModeVisibleOn();
                }
                else if (sPWD == pwd)
                {
                    OP_ModeVisibleOn();
                }
                else
                {
                    checkBox.Checked = false;
                    MessageBox.Show("密碼錯誤!");
                    return;
                }
            }
            else
            {
                ENG_ModeVisibleOff();
            }
        }


        /// <summary>
        /// 工程模式顯示開啟
        /// </summary>
        private void OP_ModeVisibleOn()
        {
            //axis Q
            btn_Motion_MoveN_Q.Visible = true;
            btn_Motion_MoveP_Q.Visible = true;
        }


        /// <summary>
        /// 工程模式顯示開啟
        /// </summary>
        private void ENG_ModeVisibleOn()
        {
            enablePage(tabMS_Param, true);
            enablePage(tabAxMMark, true);
            enablePage(tabTableTemp, true);
            //enablePage(tab_PathTable, true);
            btn_HmeasureForce.Visible = true;
            CB_DoorPass.Visible = true;
            L_DiskAngle.Visible = true;
            L_DiskAngleUnit.Visible = true;
            txt_RotateAngle.Visible = true;
            gb_RotateTest.Visible = true;
            gb_CCD_Move_LaserZ.Visible = true;
            GB_RotateSpeed.Visible = true;
            gb_PowerMeterTest.Visible = true;

            //axis Q
            btn_Motion_MoveN_Q.Visible = true;
            btn_Motion_MoveP_Q.Visible = true;

            GB_CCD_Laser_Move.Visible = true;
            btn_SB_ThickM.Visible = true;
            btn_Test_KeyenceHeight.Visible = true;
        }
        /// <summary>
        /// 工程模式顯示關閉
        /// </summary>
        private void ENG_ModeVisibleOff()
        {
            enablePage(tabMS_Param, false);
            enablePage(tabAxMMark, false);
            enablePage(tabTableTemp, false);
            //enablePage(tab_PathTable, false);
            btn_HmeasureForce.Visible = false;
            CB_DoorPass.Visible = false;
            L_DiskAngle.Visible = false;
            L_DiskAngleUnit.Visible = false;
            txt_RotateAngle.Visible = false;
            gb_RotateTest.Visible = false;
            gb_CCD_Move_LaserZ.Visible = false;
            GB_RotateSpeed.Visible = false;
            gb_PowerMeterTest.Visible = false;
            //axis
            btn_Motion_MoveN_Q.Visible = false;
            btn_Motion_MoveP_Q.Visible = false;

            GB_CCD_Laser_Move.Visible = false;
            btn_SB_ThickM.Visible = false;
            btn_Test_KeyenceHeight.Visible = false;
        }

        /// <summary>
        /// 隱藏 MEM Type
        /// </summary>
        private void MEM_TypeVisibleOff()
        {
            enablePage(tabTableSet, false);
            enablePage(tabST_MapSet, false);
            gb_MEM_Pitch.Visible = false;
            Pic_AutoStep1.Visible = false;
            GB_AutoStep1.Visible = false;
            B_ST_FileLoad.Visible = false;
            GB_AutoStep2.Visible = false;
            Pic_AutoStep2.Visible = false;
            GB_AutoStep3.Visible = false;
            GB_AutoST_Step1.Visible = false;
            GB_AutoST_Step2.Visible = false;
            GB_AutoST_Step4.Visible = false;
            rd_SinglePad.Visible = false;
            rd_SingleBall.Visible = false;
            label224.Visible = false;
            label5.Visible = false;
            num_ST_Pad_No.Visible = false;
            num_ST_Ball_No.Visible = false;

        }
        /// <summary>
        /// 顯示 MEM Type
        /// </summary>
        private void MEM_TypeVisibleOn()
        {
            enablePage(tabTableSet, true);
            enablePage(tabST_MapSet, true);
            gb_MEM_Pitch.Visible = true;
            Pic_AutoStep1.Visible = true;
            GB_AutoStep1.Visible = true;
            B_ST_FileLoad.Visible = true;
            GB_AutoStep2.Visible = true;
            Pic_AutoStep2.Visible = true;
            GB_AutoStep3.Visible = true;
            GB_AutoST_Step1.Visible = true;
            GB_AutoST_Step2.Visible = true;
            GB_AutoST_Step4.Visible = true;
            rd_SinglePad.Visible = true;
            rd_SingleBall.Visible = true;
            label224.Visible = true;
            label5.Visible = true;
            num_ST_Pad_No.Visible = true;
            num_ST_Ball_No.Visible = true;
        }


        private delegate void dUpdateTabPageWithBool(TabPage tabPage, bool enable);
        private void enablePage(TabPage page, bool enable)
        {
            if (page.InvokeRequired)
            {
                var func = new dUpdateTabPageWithBool(enablePage);
                page.Invoke(func, page, enable);
            }
            else
            {
                if (enable)
                {
                    tabControl1.TabPages.Add(page);
                }
                else
                {
                    tabControl1.TabPages.Remove(page);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {


        }

        private void btn_CVX_RecipeSave_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行CV-X Recipe參數儲存。", tmpListStr);
                _recipeData.SYSParam.CVX_RecipeNo = Convert.ToInt32(txt_CVX_Recipe.Text);
                _recipeData.Save_Data(RecipeFileName);

                CVX_RecipeChange(int.Parse(txt_CVX_Recipe.Text));
                Thread.Sleep(500);
                int now_no = 0;
                if (CVX_RecipeNoGet(out now_no))
                    L_CVX_RecipeNo.Text = now_no.ToString();

            }
        }

        private void CVX_RecipeChange(int no)
        {
            try
            {
                if (_Using)
                {
                    ChangeCVX_RecipeNo(no);
                    Thread.Sleep(500);
                    int now_no = 0;
                    if (CVX_RecipeNoGet(out now_no))
                        L_CVX_RecipeNo.Text = now_no.ToString();
                }

            }
            catch (Exception ex)
            {

            }
        }

        private void btnCVX_RecipeSet_Click(object sender, EventArgs e)
        {
            CVX_RecipeChange(int.Parse(txt_CVX_Recipe.Text));
            Thread.Sleep(500);
            int now_no = 0;
            if (CVX_RecipeNoGet(out now_no))
                L_CVX_RecipeNo.Text = now_no.ToString();
        }

        /// <summary>
        /// 變更cv-x recipe
        /// </summary>
        /// <param name="no"></param>
        /// <returns></returns>
        private bool ChangeCVX_RecipeNo(int no)
        {
            bool result = false;
            _Keyence_CVX.ReadMsg = "";
            _Keyence_CVX.WriteLine($"PW,1,{no}");
            Thread.Sleep(100);
            if (_Keyence_CVX.ReadMsg.Contains("PW"))
                result = true;

            return result;
        }

        private bool CVX_RecipeNoGet(out int no)
        {
            bool result = false;
            no = 0;

            try
            {

                _Keyence_CVX.ReadMsg = "";
                _Keyence_CVX.WriteLine($"PR");
                Thread.Sleep(200);
                if (_Keyence_CVX.ReadMsg.Contains("PR"))
                {
                    result = true;
                    string[] tmp = _Keyence_CVX.ReadMsg.Split(',');
                    no = int.Parse(tmp[2]);
                }
            }
            catch (Exception ex)
            {

            }

            return result;
        }

        private bool ChangeCVX_Run()
        {
            bool result = false;
            _Keyence_CVX.ReadMsg = "";
            _Keyence_CVX.WriteLine($"R0");
            Thread.Sleep(300);
            if (_Keyence_CVX.ReadMsg.Contains("R0"))
                result = true;

            return result;
        }

        private void axCVX1_OnRemoteDesktopUpdated(object sender, AxCVXLib._DCVXEvents_OnRemoteDesktopUpdatedEvent e)
        {

        }

        private void btn_SaveAsNewFile_Click(object sender, EventArgs e)
        {
            SaveAsNew_Form _form = new SaveAsNew_Form(_ConfigSystem._RecipePath);
            if (_form.ShowDialog() == DialogResult.OK)
            {
                string tmp = _form.SaveFileName;
                if (!tmp.Contains(".rcp"))
                    tmp += ".rcp";

                if (tmp.Length > 0)
                {
                    if (!RecipeSave(tmp))
                        MessageBox.Show("Recipe另存新檔失敗。", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    else
                    {
                        Setting_UI();
                    }
                }
                else
                {
                    MessageBox.Show("檔名錯誤。", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private static string showDialog(string text, string caption)
        {
            System.Windows.Forms.Form prompt = new System.Windows.Forms.Form()
            {
                Width = 250,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            Label textLabel = new Label() { Left = 10, Top = 20, Text = text };
            TextBox textBox = new TextBox() { Left = 15, Top = 45, Width = 200 };
            textBox.PasswordChar = '*';
            Button confirmation = new Button() { Text = "OK", Left = 165, Width = 50, Top = 80, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }

        /// <summary>
        /// Task Run model
        /// </summary>
        /// <param name="task"></param>
        /// <param name="timeout"></param>
        /// <returns>完成</returns>
        public async Task<bool> TaskRun(Task<bool> task, Task<bool> timeout)
        {
            bool result = false;
            Task<bool> completedTask = await Task<bool>.WhenAny(task, timeout);
            if (completedTask == task && completedTask.Result)
            {
                result = true;
            }
            else
            {
                result = false;
            }


            return result;

        }

        /// <summary>
        /// Timeout
        /// </summary>
        /// <returns></returns>
        private async Task<bool> TimeOutTask(TimeSpan timeout)
        {
            await Task.Delay(timeout);
            return true;
        }




        /// <summary>
        /// 測高
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ConFocusRead()
        {
            bool result = false;
            string InitNum = "11";//預設錯誤值
            KeyenceHeight_Value = InitNum;
            if (_Keyence_Height.Check_Connect())
                _Keyence_Height.Send(Keyence_Height_Send_Data);

            for (int i = 0; i < 50; i++)
            {
                double AF_Z = Convert.ToDouble(KeyenceHeight_Value);
                if (AF_Z != 11 && Math.Abs(AF_Z) <= 7)  //limit 7mm
                {
                    result = true;
                    break;
                }
                Thread.Sleep(20);
            }

            return result;

        }

        /// <summary>
        /// ST Grab  pos1.左下 pos2.左上 pos3.右下
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ST_MarkGrab(int grabPos)
        {
            bool result = false;
            int step = 0;
            string stFindMark = "1";

            switch (grabPos)
            {
                case 1:
                case 3:
                    stFindMark = "0";
                    break;
                case 2:
                case 4:
                    stFindMark = "1";
                    break;
            }



            string CCD_NO = "T1";
            TJJS_Point stageXY = new TJJS_Point();
            int count = 0;
            bool _runflag = true;

            try
            {
                if (_Keyence_CVX_flag)
                {
                    while (_runflag)
                    {
                        switch (step)
                        {
                            case 0:
                                _Keyence_CVX.ReadMsg = "";
                                _Keyence_CVX.WriteLine("EXR");
                                Thread.Sleep(200);
                                step = 1;
                                break;

                            case 1:
                                if (_Keyence_CVX.ReadMsg == "EXR," + stFindMark)
                                {
                                    step = 2;
                                }
                                else
                                {
                                    _Keyence_CVX.ReadMsg = "";
                                    _Keyence_CVX.WriteLine("EXW," + stFindMark);
                                    Thread.Sleep(250);
                                    step = 0;
                                }
                                break;

                            case 2:
                                _Keyence_CVX.ReadMsg = "";
                                _Keyence_CVX.WriteLine(CCD_NO);
                                Thread.Sleep(250);
                                step = 3;
                                break;

                            case 3:
                                string[] s_im = _Keyence_CVX.ReadMsg.Split(',', '\r');// 一定是單引                           
                                if ((Convert.ToDouble(s_im[3]) >= 10) && s_im[0] == "T1")
                                {
                                    stageXY.X = -double.Parse(txt_Motion_Position_X.Text);
                                    stageXY.Y = -double.Parse(txt_Motion_Position_Y.Text);
                                    switch (grabPos)
                                    {
                                        case 1:
                                            PublicData.ST_AlignH.Start = PixelToDist(double.Parse(s_im[1]), double.Parse(s_im[2]), 1) + stageXY;
                                            PublicData.ST_AlignV.Start = PixelToDist(double.Parse(s_im[1]), double.Parse(s_im[2]), 1) + stageXY;
                                            result = true;
                                            _runflag = false;
                                            break;

                                        case 2:
                                            PublicData.ST_AlignV.End = PixelToDist(double.Parse(s_im[1]), double.Parse(s_im[2]), 1) + stageXY;
                                            result = true;
                                            _runflag = false;
                                            break;

                                        case 3:
                                            PublicData.ST_AlignH.End = PixelToDist(double.Parse(s_im[1]), double.Parse(s_im[2]), 1) + stageXY;
                                            result = true;
                                            _runflag = false;
                                            break;
                                    }
                                }
                                else
                                {
                                    return false;

                                }
                                break;
                        }
                        count++;
                        Thread.Sleep(10);

                        if (count > 500)
                            _runflag = false;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return result;

        }

        /// <summary>
        ///吸嘴XY 
        /// </summary>
        /// <returns></returns>
        private async Task<bool> Nozzle_XY_Grab()
        {
            bool runFlag = true;
            bool result = false;
            int step = 0;
            string stFindMark = "0";
            string CCD_NO = "T3";
            int count = 0;
            try
            {
                if (_Keyence_CVX_flag)
                {
                    while (runFlag)
                    {
                        count++;
                        switch (step)
                        {
                            case 0:
                                _Keyence_CVX.ReadMsg = "";
                                _Keyence_CVX.WriteLine("EXR");
                                await Task.Delay(250);
                                step = 1;
                                break;

                            case 1:
                                if (_Keyence_CVX.ReadMsg == "EXR," + stFindMark)
                                {
                                    step = 2;
                                }
                                else
                                {
                                    _Keyence_CVX.ReadMsg = "";
                                    _Keyence_CVX.WriteLine("EXW," + stFindMark);
                                    await Task.Delay(250);
                                    step = 0;
                                }
                                break;

                            case 2:
                                _Keyence_CVX.ReadMsg = "";
                                _Keyence_CVX.WriteLine(CCD_NO);
                                await Task.Delay(250);
                                step = 3;
                                break;

                            case 3:
                                string[] s_im = _Keyence_CVX.ReadMsg.Split(',', '\r');// 一定是單引                           
                                if ((Convert.ToDouble(s_im[3]) > 0) && s_im[0] == "T1")
                                {
                                    TJJS_Point tmpP = PixelToDist(Convert.ToDouble(s_im[1]), Convert.ToDouble(s_im[2]), 3);

                                    this.Invoke((MethodInvoker)delegate () { txt_NozzleAlign_X.Text = tmpP.X.ToString("F3"); });
                                    this.Invoke((MethodInvoker)delegate () { txt_NozzleAlign_Y.Text = tmpP.Y.ToString("F3"); });
                                    runFlag = false;
                                    result = true;
                                    break;
                                }
                                else
                                {
                                    result = false;

                                }
                                break;
                        }
                        Thread.Sleep(10);
                        if (count > 500)//time out
                            return false;
                    }

                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Error_Log.Add($"[Nozzle_XY_Grab]: {ex.ToString()}");
                runFlag = false;
                return false;
            }
            return result;

        }

        /// <summary>
        /// 吸嘴Z
        /// </summary>
        /// <returns></returns>
        private async Task<bool> Nozzle_Z_Grab()
        {
            bool runFlag = true;
            bool result = false;
            int step = 0;
            string stFindMark = "4";
            string CCD_NO = "T4";
            int count = 0;
            try
            {
                if (_Keyence_CVX_flag)
                {
                    while (runFlag)
                    {
                        count++;
                        switch (step)
                        {
                            case 0:
                                _Keyence_CVX.ReadMsg = "";
                                _Keyence_CVX.WriteLine("EXR");
                                await Task.Delay(250);
                                step = 1;
                                break;

                            case 1:
                                if (_Keyence_CVX.ReadMsg == "EXR," + stFindMark)
                                {
                                    step = 2;
                                }
                                else
                                {
                                    _Keyence_CVX.ReadMsg = "";
                                    _Keyence_CVX.WriteLine("EXW," + stFindMark);
                                    await Task.Delay(250);
                                    step = 0;
                                }
                                break;

                            case 2:
                                _Keyence_CVX.ReadMsg = "";
                                _Keyence_CVX.WriteLine(CCD_NO);
                                await Task.Delay(250);
                                step = 3;
                                break;

                            case 3:
                                string[] s_im = _Keyence_CVX.ReadMsg.Split(',', '\r');// 一定是單引                           
                                if ((Convert.ToDouble(s_im[3]) > 0) && s_im[0] == "T1")
                                {
                                    TJJS_Point tmpP = PixelToDist(Convert.ToDouble(s_im[1]), Convert.ToDouble(s_im[2]), 4);
                                    this.Invoke((MethodInvoker)delegate () { txt_NozzleAlign_Z.Text = tmpP.Y.ToString("F3"); });
                                    runFlag = false;
                                    result = true;
                                    break;
                                }
                                else
                                {
                                    result = false;

                                }
                                break;
                        }
                        Thread.Sleep(10);
                        if (count > 500)//time out
                            return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                runFlag = false;
                return false;
            }
            return result;


        }

        /// <summary>
        /// 分離盤供料預備
        /// </summary>
        /// <returns></returns>
        private async Task<bool> DiskReadyRun()
        {
            bool result = false;

            if (_DiskLockFlag)
                return false;

            _DiskLockFlag = true;


            try
            {
                _DiskRunFlag = true;
                int missCount = 0;

                string str = "";
                P_F_EthernetIP.DataUpdataOk = false;
                _ST_DiskRun_FlowStep = enumDiskRunFlowStep.DiskRunStep1;

                while (_DiskRunFlag && (_EQP_Status != enumEQP_Status.DOWN && _NozzleCleanFlowStep == enumNozzleCleanFlowStep.NozzleClean_Wait))
                {
                    switch (_ST_DiskRun_FlowStep)
                    {
                        case enumDiskRunFlowStep.DiskRunStep1: //judge
                            if (P_F_EthernetIP.DataUpdataOk)
                            {
                                if (SB_Air_Check(true))
                                {
                                    _ST_DiskRun_FlowStep = enumDiskRunFlowStep.DiskRunStep3;
                                    break;
                                }
                                else
                                {
                                    if (missCount > _recipeData.SYSParam.LoadBallRetry)
                                    {
                                        _RunFlag = false;
                                        str = $"供球流程超過[{_recipeData.SYSParam.LoadBallRetry}]次.";
                                        LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                                        _DiskRunFlag = false;
                                        FlowStop();

                                        result = false; ;
                                        Invoke(new dele_msgShow(ErrMSG_Show), str);
                                        new AggregateException(str);
                                    }
                                    else //優化無球判定時間
                                    {
                                        if (missCount >= 1) //第一次供球後
                                        {
                                            Thread.Sleep(_recipeData.SYSParam.SB_AirJudgeDelay + _msPosData.SB_AirJudgeNG_Delay);  //強制等待100ms + setting time
                                            Error_Log.Add($"供球延遲判定[{_recipeData.SYSParam.SB_AirJudgeDelay + _msPosData.SB_AirJudgeNG_Delay}ms]");
                                            if (SB_Air_Check(true))
                                                _ST_DiskRun_FlowStep = enumDiskRunFlowStep.DiskRunStep3;
                                            else
                                                _ST_DiskRun_FlowStep = enumDiskRunFlowStep.DiskRunStep2;
                                        }
                                        else
                                        {
                                            _ST_DiskRun_FlowStep = enumDiskRunFlowStep.DiskRunStep2;
                                        }

                                    }

                                }
                            }
                            break;


                        case enumDiskRunFlowStep.DiskRunStep2: //rotate

                            if (!AZD_Motion && !_AZD_Rotating)
                            {
                                if (AZD_RotateNext(_recipeData.SYSParam.RotateAngle))
                                {
                                    Error_Log.Add("供球完成");
                                    //tact_time.Stop();
                                    //LogMsgAdd(MList_Log, lb_HistoryList, $"DiskRun Time[{tact_time.ElapsedMilliseconds}]ms ", tmpListStr);

                                    Thread.Sleep(_recipeData.SYSParam.SB_AirJudgeDelay);
                                    missCount++;
                                    if (missCount > 1) //超過一次 紀錄
                                    {
                                        if (_autoSB_FlowStep != enumAUTO_FlowStep.AutoWait)
                                            str = $"SB[{Now_SB_PadNO}]  供球流程次數，第[{missCount}]次 角度[{Now_ActQ.ToString("F3")}].";
                                        else
                                            str = $"當前ST_No:[{NowST_NO}] 當前Pad_No:[{NowPadNO}] 當前SB_No:[{NowBallNO}]  供球流程次數，第[{missCount}]次 角度[{Now_ActQ.ToString("F3")}].";
                                        //LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                                        Error_Log.Add(str);
                                    }

                                    P_F_EthernetIP.DataUpdataOk = false;
                                    _ST_DiskRun_FlowStep = enumDiskRunFlowStep.DiskRunStep1;
                                    //   if (!IO_InputData.DiskSensor)
                                    //{
                                    //  	 	LogMsgAdd(Error_Log, lb_ErrorList, $"供球模組移動後定點未到位!", tmpErrStr);
                                    //}
                                }
                            }
                            break;

                        case enumDiskRunFlowStep.DiskRunStep3:  //finish         
                            _DiskRunFlag = false;
                            result = true;

                            break;
                    }
                    Thread.Sleep(3);

                }
            }
            catch (Exception ex)
            {
                // 可在此記錄例外，或直接 rethrow
                LogMsgAdd(Error_Log, lb_ErrorList, $"DiskReadyRun 例外: {ex.Message}", tmpErrStr);
                result = false;
                throw;  // 讓呼叫端決定如何處理
            }
            finally
            {
                _DiskLockFlag = false;
            }

            return result;
        }

        /// <summary>
        /// Solder 有ball 壓力表頭判定
        /// </summary>
        /// <param name="writelog">write log</param>
        /// <returns></returns>
        private bool SB_Air_Check(bool writelog)
        {
            bool result = false;
            string str = "";

            //壓差判斷
            if (_recipeData.SYSParam.SB_AirJudgDiff)
            {
                if ((NowNozzle_MAX_Pressure - NowNozzle_MIN_Pressure) >= _recipeData.SYSParam.NozzleSB_Diff_Value)
                {

                    if (_autoSB_FlowStep != enumAUTO_FlowStep.AutoWait)
                        str = $"[SB_Air_Check Diff] SB[{Now_SB_PadNO}]  OK-Pressure Diff[{ (NowNozzle_MAX_Pressure - NowNozzle_MIN_Pressure).ToString("F3")}] Q[{Now_ActQ.ToString("F3")}]";
                    else
                        str = $"[SB_Air_Check Diff] 當前ST_No:[{NowST_NO}] 當前Pad_No:[{NowPadNO}] 當前SB_No:[{NowBallNO}]  OK-Pressure Diff[{ (NowNozzle_MAX_Pressure - NowNozzle_MIN_Pressure).ToString("F3")}]  Q[{Now_ActQ.ToString("F3")}].";
                    if (writelog)
                        Error_Log.Add(str);

                    result = true;
                }
                else
                {
                    //堵球判定
                    //if (NowNozzle_MIN_Pressure > 1) 
                    //{
                    //                   _RunFlag = false;
                    //                   str = $"噴嘴壓力異常確認.";
                    //                   LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                    //                   _DiskRunFlag = false;
                    //                   FlowStop();

                    //                   result = false; ;
                    //                   Invoke(new dele_msgShow(ErrMSG_Show), str);
                    //                   new AggregateException(str);
                    //               }

                    if (_autoSB_FlowStep != enumAUTO_FlowStep.AutoWait)
                        str = $"[SB_Air_Check Diff] SB[{Now_SB_PadNO}] NG-Pressure Diff[{ (NowNozzle_MAX_Pressure - NowNozzle_MIN_Pressure).ToString("F3")}]  Q[{Now_ActQ.ToString("F3")}].";
                    else
                        str = $"[SB_Air_Check Diff] 當前ST_No:[{NowST_NO}] 當前Pad_No:[{NowPadNO}] 當前SB_No:[{NowBallNO}]  NG-Pressure Diff[{ (NowNozzle_MAX_Pressure - NowNozzle_MIN_Pressure).ToString("F3")}]  Q[{Now_ActQ.ToString("F3")}].";
                    if (writelog)
                        Error_Log.Add(str);
                }
            }
            else
            {
                if (NowNozzlePressure >= _recipeData.SYSParam.NozzleSB_Pressure)
                {
                    if (_autoSB_FlowStep != enumAUTO_FlowStep.AutoWait)
                        str = $"[SB_Air_Check] SB[{Now_SB_PadNO}]  OK-Pressure[{ NowNozzlePressure.ToString("F3")}].";
                    else
                        str = $"[SB_Air_Check] 當前ST_No:[{NowST_NO}] 當前Pad_No:[{NowPadNO}] 當前SB_No:[{NowBallNO}]  OK-Pressure[{ NowNozzlePressure.ToString("F3")}]  Q[{Now_ActQ.ToString("F3")}].";
                    if (writelog)
                        Error_Log.Add(str);

                    result = true;
                }
                else
                {
                    if (_autoSB_FlowStep != enumAUTO_FlowStep.AutoWait)
                        str = $"[SB_Air_Check] SB[{Now_SB_PadNO}] NG-Pressure[{ NowNozzlePressure.ToString("F3")}].";
                    else
                        str = $"[SB_Air_Check] 當前ST_No:[{NowST_NO}] 當前Pad_No:[{NowPadNO}] 當前SB_No:[{NowBallNO}]  NG-Pressure[{ NowNozzlePressure.ToString("F3")}]  Q[{Now_ActQ.ToString("F3")}].";
                    if (writelog)
                        Error_Log.Add(str);
                }
            }


            return result;
        }

        /// <summary>
        /// Solder 無ball 壓力表頭判定
        /// </summary>
        /// <returns></returns>
        private bool NoSB_Air_Check()
        {
            bool result = false; string str = "";

            if (NowNozzlePressure < _recipeData.SYSParam.NozzleNoSB_Pressure)
            {
                result = true;
                //if (_autoSB_FlowStep != enumAUTO_FlowStep.AutoWait)
                //    str = $"[No SB_Air_Check] SB[{Now_SB_PadNO}]  OK-Pressure[{NowNozzlePressure.ToString("F3")}].";
                //else
                //    str = $"[No SB_Air_Check] 當前ST_No:[{NowST_NO}] 當前Pad_No:[{NowPadNO}] 當前SB_No:[{NowBallNO}]  OK-Pressure[{NowNozzlePressure.ToString("F3")}].";
                //Error_Log.Add(str);

            }

            else
            {
                if (_autoSB_FlowStep != enumAUTO_FlowStep.AutoWait)
                    str = $"[No SB_Air_Check] SB[{Now_SB_PadNO}]  NG-Pressure[{NowNozzlePressure.ToString("F3")}]  Q[{Now_ActQ}].";
                else
                    str = $"[No SB_Air_Check] 當前ST_No:[{NowST_NO}] 當前Pad_No:[{NowPadNO}] 當前SB_No:[{NowBallNO}]  NG-Pressure[{NowNozzlePressure.ToString("F3")}]  Q[{Now_ActQ}].";
                Error_Log.Add(str);

            }
            return result;
        }

        /// <summary>
        /// AZD 移動一隔
        /// </summary>
        /// <param name="deg">角度 小數三位</param>
        /// <returns></returns>
        private bool AZD_RotateNext(double deg)
        {
            //旋轉中
            if (_AZD_Rotating)
            {
                return false;
            }

            _AZD_Rotating = true;

            try
            {

                bool result = false;
                Stopwatch timeout = new Stopwatch();
                azd_value = Convert.ToInt32(deg * 1000);
                azd_axisspeed = (int)(_recipeData.SYSParam.DiskRunSpeed * 1000);
                azd_upRate = (uint)(_recipeData.SYSParam.DiskRunACC * 1000);
                azd_downRate = (uint)(_recipeData.SYSParam.DiskRunDEC * 1000);

                int oriPos = (int)(azd_Act_Pos);
                int nextPos = ((int)azd_Act_Pos) + azd_value;
                //if (nextPos >= 360 * 1000)
                //	nextPos -= 360 * 1000;

                //if (nextPos < 0)
                //	nextPos += 360 * 1000;

                timeout.Start();
                //if (_AZD_Controller.MoveRel(azd_value, azd_axisspeed, azd_upRate, azd_downRate))

                PCIE_1203_SetParam(azd_axisspeed, azd_upRate, azd_downRate);
                if (Motion.mAcm_AxMoveRel(m_Axishand[0], azd_value) == (uint)ErrorCode.SUCCESS)
                {

                    while (CycleFlag && Math.Abs((int)((azd_Act_Pos) - nextPos)) >= 3 || (azd_Act_Pos) == oriPos)
                    {

                        Thread.Sleep(1);

                        if (timeout.ElapsedMilliseconds > 3000) //3 sec
                        {
                            Error_Log.Add("Q軸超時!");
                            if (SB_Air_Check(true))
                            {
                                return true;
                            }
                            else
                                return false;
                        }
                    }
                    result = true;
                }


                return result;
            }
            finally
            {
                _AZD_Rotating = false;
            }
        }


        /// <summary>
        /// MEM ST 植球流程
        /// </summary>
        /// <param name="padNo">起始Pad位置</param>
        /// <param name="ballNo">起始solderball位置</param>
        /// <returns></returns>
        private async Task<bool> ST_BallMountFlow(int padNo = 1, int ballNo = 1)
        {
            bool result = false;
            _RunFlag = true;
            string str = "";
            _BondData.SetStartPadNo(padNo, ballNo);
            int _padNo = padNo;
            int _ballNo = ballNo;
            double xPos = 0, yPos = 0, zPos = 0;
            int retryCount = 0;
            int CYCcleanCount = 0;


            while (_RunFlag)
            {
                try
                {
                    NowBallNO = _ballNo;
                    NowPadNO = _padNo;

                    switch (_STballMount_FlowStep)
                    {
                        case enumBallMountFlowStep.MountStep1:
                            if (_BondData.Finish)
                            {
                                _RunFlag = false;
                                return true;
                            }
                            else
                            {
                                _STballMount_FlowStep = enumBallMountFlowStep.MountStep2;
                            }

                            break;

                        //move
                        case enumBallMountFlowStep.MountStep2:
                            if (!_BondData.BondPad[_padNo - 1].Finish)
                            {
                                if (!_BondData.BondPad[_padNo - 1].SolderBall[_ballNo - 1].Finish)
                                {
                                    NowNozzle_MAX_Pressure = 0;
                                    NowNozzle_MIN_Pressure = NowNozzlePressure;
                                    _STballMount_FlowStep = enumBallMountFlowStep.MountStep3;
                                }
                                else
                                {
                                    _ballNo++;
                                    _STballMount_FlowStep = enumBallMountFlowStep.MountStep1;
                                }
                            }
                            else
                            {
                                _padNo++;
                                _ballNo = 1;
                                _STballMount_FlowStep = enumBallMountFlowStep.MountStep1;
                            }
                            break;

                        // 先移xy
                        case enumBallMountFlowStep.MountStep3:
                            xPos = _BondData.BondPad[_padNo - 1].SolderBall[_ballNo - 1].LX;
                            yPos = _BondData.BondPad[_padNo - 1].SolderBall[_ballNo - 1].LY;
                            zPos = _BondData.BondPad[_padNo - 1].SolderBall[_ballNo - 1].LZ;

                            //if (StageABS_Move(xPos, yPos, zPos, true, true, false) & await DiskReadyRun()) //供球成功
                            if (StageABS_Move(xPos, yPos, zPos, true, true, false))
                            {
                                _STballMount_FlowStep = enumBallMountFlowStep.MountStep4;
                            }
                            break;


                        //laser
                        case enumBallMountFlowStep.MountStep4:
                            xPos = _BondData.BondPad[_padNo - 1].SolderBall[_ballNo - 1].LX;
                            yPos = _BondData.BondPad[_padNo - 1].SolderBall[_ballNo - 1].LY;
                            zPos = _BondData.BondPad[_padNo - 1].SolderBall[_ballNo - 1].LZ;

                            //if (StageABS_Move(xPos, yPos, zPos, true, true, true) & SB_Air_Check(false)) //供球成功
                            if (StageABS_Move(xPos, yPos, zPos, true, true, true) & await DiskReadyRun()) //供球成功
                            {
                                _STballMount_FlowStep = enumBallMountFlowStep.MountStep5;
                            }

                            break;

                        case enumBallMountFlowStep.MountStep5:
                            if (await LaserEmission(_BondData.BondPad[_padNo - 1].PadType))  ////出球成功
                            {
                                Error_Log.Add("Emission On");
                                NowNozzle_MAX_Pressure = 0;
                                NowNozzle_MIN_Pressure = NowNozzlePressure;
                                _BondData.BondPad[_padNo - 1].SolderBall[_ballNo - 1].Finish = true;
                                _STballMount_FlowStep = enumBallMountFlowStep.MountStep6;

                                CYCcleanCount++;
                                //週期清潔
                                if (CYCcleanCount >= _recipeData.SYSParam.ClearSB_Count && _recipeData.SYSParam.ClearSB_Count > 0)
                                {
                                    CYCcleanCount = 0;
                                    _NozzleCleanFlowStep = enumNozzleCleanFlowStep.NozzleClean_Step1;
                                    str = $"ST[{NowST_NO}] Pad[{_padNo}] SB[{_ballNo}]，進行週期清潔流程.";
                                    LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                                    if (await NozzleCleanFlow())
                                    {
                                        _STballMount_FlowStep = enumBallMountFlowStep.MountStep2;
                                    }
                                    else
                                    {
                                        str = $"清潔流程失敗.";
                                        LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                                        _EQP_Status = enumEQP_Status.DOWN;
                                        Invoke(new dele_msgShow(ErrMSG_Show), str);
                                        _RunFlag = false;
                                        return false;
                                    }
                                }

                            }
                            else
                            {
                                if (retryCount >= _recipeData.SYSParam.ClearRetry)
                                {
                                    str = $"ST[{NowST_NO}] Pad[{_padNo}] SB[{_ballNo}] 植球失敗，超過[{ _recipeData.SYSParam.ClearRetry}]次清潔流程.";
                                    LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                                    _EQP_Status = enumEQP_Status.DOWN;
                                    Invoke(new dele_msgShow(ErrMSG_Show), str);
                                    return false;
                                }
                                else //出球失敗 進行清潔
                                {
                                    _NozzleCleanFlowStep = enumNozzleCleanFlowStep.NozzleClean_Step1;
                                    str = $"ST[{NowST_NO}] Pad[{_padNo}] SB[{_ballNo}] 出球失敗，進行第[{retryCount + 1}]次清潔流程.";
                                    LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                                    if (await NozzleCleanFlow())
                                    {
                                        _STballMount_FlowStep = enumBallMountFlowStep.MountStep2;
                                    }
                                    else
                                    {
                                        str = $"清潔流程失敗.";
                                        LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                                        _EQP_Status = enumEQP_Status.DOWN;
                                        Invoke(new dele_msgShow(ErrMSG_Show), str);
                                        _RunFlag = false;
                                        return false;
                                    }
                                }
                                retryCount++;
                            }
                            break;

                        case enumBallMountFlowStep.MountStep6:
                            if (_recipeData.SB.SB_MountMoveZ_Flag) //移動z軸上升
                            {
                                if (StageABS_Move(xPos, yPos, zPos - _recipeData.SB.SB_MountMoveZ, false, false, true)) //上移2mm 避開元件
                                {
                                    NowNozzle_MAX_Pressure = 0;
                                    NowNozzle_MIN_Pressure = NowNozzlePressure;
                                    _STballMount_FlowStep = enumBallMountFlowStep.MountStep1;
                                }
                            }
                            else
                            {
                                _STballMount_FlowStep = enumBallMountFlowStep.MountStep1;
                                NowNozzle_MAX_Pressure = 0;
                                NowNozzle_MIN_Pressure = NowNozzlePressure;
                            }


                            break;
                    }

                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted ||
                                    ex.Message.Contains("WSACancelBlockingCall"))
                {
                    Console.WriteLine($"[ST BallMountFlow SocketException ex Error]: {ex.ToString()}");
                    str = $"[ST BallMountFlow SocketException ex Error]: {ex.ToString()}";
                    //LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                    //Error_Log.Add(str);
                }
                catch (System.IO.IOException ex) when (ex.InnerException is SocketException sockEx &&
                                             (sockEx.SocketErrorCode == SocketError.Interrupted ||
                                              sockEx.Message.Contains("WSACancelBlockingCall")))
                {
                    Console.WriteLine($"[ST BallMountFlow Error IOException ex]: {ex.ToString()}");
                    str = $"[ST BallMountFlow Error IOException ex]: {ex.ToString()}";
                    //LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                    //Error_Log.Add(str);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ST BallMountFlow Error]: {ex.ToString()}");
                    str = $"[ST BallMountFlow Error]: {ex.ToString()}";
                    //LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                    //_EQP_Status = enumEQP_Status.DOWN;
                    //_RunFlag = false;
                    //return false;
                }

                Thread.Sleep(1);
            }

            return result;
        }

        /// <summary>
        /// Pixel轉換實際距離(mm)
        /// </summary>
        /// <param name="column"></param>
        /// <param name="row"></param>
        /// <param name="ccdNo"> 1~4 CCD </param>
        /// <returns></returns>
        private TJJS_Point PixelToDist(double column, double row, int ccdNo)
        {
            TJJS_Point result = new TJJS_Point();
            if (ccdNo >= 1 && ccdNo <= 4)
            {
                result.X = (column - 1216) / 1000 * _ConfigSystem._CCD_Pixel[ccdNo - 1];
                result.Y = -(row - 1025) / 1000 * _ConfigSystem._CCD_Pixel[ccdNo - 1];
            }


            return result;
        }




        /// <summary>
        /// Pad  位置計算
        /// </summary>
        /// <returns></returns>
        private bool CalST_PadMap(int stNo)
        {
            bool result = true;

            TJJS_Point baseP = new TJJS_Point();
            TJJS_Point targetP = new TJJS_Point();
            TJJS_Point rotateP = new TJJS_Point();


            try
            {
                baseP.X = _recipeData.Panel.ST_CenterPos[stNo - 1].X - Pos.Mark_Pos[0].X;
                baseP.Y = _recipeData.Panel.ST_CenterPos[stNo - 1].Y - Pos.Mark_Pos[0].Y;

                if (Pos.PAD_Pos.Count > 0)
                {
                    stCalData nozzleOffset = NozzleAlignCal();

                    _BondData.Init();
                    _BondData.PadNum = Pos.PAD_Pos.Count;

                    for (int i = 0; i < Pos.PAD_Pos.Count; i++)
                    {
                        targetP.X = _recipeData.Panel.ST_CenterPos[stNo - 1].X - Pos.PAD_Pos[i].X * PublicData.ST_ExpansionContractionX;
                        targetP.Y = _recipeData.Panel.ST_CenterPos[stNo - 1].Y - Pos.PAD_Pos[i].Y * PublicData.ST_ExpansionContractionY;

                        _BondData.BondPad[i].Q = Pos.PAD_Pos[i].Q + PublicData.ST_V_CalData.DQ; //植球Q + padQ                  
                        _BondData.BondPad[i].PadType = Pos.PAD_Pos[i].PadType;

                        if (PointMove(targetP, baseP, PublicData.ST_V_CalData, ref rotateP))
                        {
                            _BondData.BondPad[i].X = rotateP.X + _BondData.BondPad[i].DX;
                            _BondData.BondPad[i].Y = rotateP.Y + _BondData.BondPad[i].DY;
                            _BondData.BondPad[i].LX = rotateP.X + _BondData.BondPad[i].DX - _msPosData.M_Laser2Vision_X;
                            _BondData.BondPad[i].LY = rotateP.Y + _BondData.BondPad[i].DY - _msPosData.M_Laser2Vision_Y;


                            result = true;
                        }
                        else
                        {
                            result = false;
                            break;
                        }

                    }
                }
                else
                    result = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                result = false;
            }

            return result;

        }

        private void btn_PowerMeterPOS_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行功率量測位置移動?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step1;

                if (FlowAllow())
                    Task.Run(PowerMeterPOS_MoveFlow);
            }
        }

        private void Num_ST_NO_ValueChanged(object sender, EventArgs e)
        {
            _stCal_OK = false;
            _stMHeight_OK = false;
        }

        /// <summary>
        /// Solderball  位置計算
        /// </summary>
        /// <returns></returns>
        private bool Cal_SolderBallMap()
        {
            bool result = true;
            TJJS_Point baseP = new TJJS_Point();
            TJJS_Point targetP = new TJJS_Point();
            TJJS_Point rotateP = new TJJS_Point();
            stCalData tmpData = new stCalData();
            double sX, sY;
            try
            {
                if (_BondData.BondPad.Count > 0)
                {
                    stCalData nozzleOffset = NozzleAlignCal();

                    for (int i = 0; i < _BondData.BondPad.Count; i++)
                    {
                        _BondData.BondPad[i].Init();
                        _BondData.BondPad[i].BallNum = int.Parse(txt_SBNumber.Text);
                        _BondData.BondPad[i].BallPitch = double.Parse(txt_SBPitch.Text);
                        _BondData.BondPad[i].BallSize = double.Parse(txt_SBSize.Text);
                        baseP.X = _BondData.BondPad[i].X;
                        baseP.Y = _BondData.BondPad[i].Y;

                        if (i == 53)
                        {
                            int d = 0;
                        }

                        sX = (_BondData.BondPad[i].X) + ((double)(_BondData.BondPad[i].BallNum - 1) / 2.0 * (_BondData.BondPad[i].BallPitch));
                        sY = (_BondData.BondPad[i].Y);

                        for (int j = 0; j < _BondData.BondPad[i].SolderBall.Count; j++)
                        {
                            targetP.X = sX - j * (_BondData.BondPad[i].BallPitch);
                            targetP.Y = sY;
                            tmpData.DQ = _BondData.BondPad[i].Q;

                            if (PointMove(targetP, baseP, tmpData, ref rotateP))
                            {                                                                      //CCD 不+OFFSET
                                _BondData.BondPad[i].SolderBall[j].X = rotateP.X /*+ Data.SB.SB_OffsetX + nozzleOffset.DX*/;
                                _BondData.BondPad[i].SolderBall[j].Y = rotateP.Y/* + Data.SB.SB_OffsetY + nozzleOffset.DY*/;
                                _BondData.BondPad[i].SolderBall[j].Z = _msPosData.M_H_VisionZ - PublicData.ST_HeightZ;
                                //_BondData.BondPad[i].SolderBall[j].Z = _msPosData.M_H_VisionZ - BilinearInterpolate(thickPoint[0], thickPoint[2], thickPoint[3], thickPoint[1], _BondData.BondPad[i].SolderBall[j].X, _BondData.BondPad[i].SolderBall[j].Y);



                                _BondData.BondPad[i].SolderBall[j].LX = rotateP.X - _msPosData.M_Laser2Vision_X + _recipeData.SB.SB_OffsetX + nozzleOffset.DX;
                                _BondData.BondPad[i].SolderBall[j].LY = rotateP.Y - _msPosData.M_Laser2Vision_Y + _recipeData.SB.SB_OffsetY + nozzleOffset.DY;
                                _BondData.BondPad[i].SolderBall[j].LZ = _msPosData.M_H_LaserZ - PublicData.ST_HeightZ + _recipeData.SB.SB_OffsetZ + nozzleOffset.DZ;
                                //_BondData.BondPad[i].SolderBall[j].LZ = _msPosData.M_H_LaserZ - BilinearInterpolate(thickPoint[0], thickPoint[2], thickPoint[3], thickPoint[1], _BondData.BondPad[i].SolderBall[j].X, _BondData.BondPad[i].SolderBall[j].Y)
                                //  + _recipeData.SB.SB_OffsetZ + nozzleOffset.DZ;

                                _BondData.BondPad[i].SolderBall[j].Finish = false;
                                result = true;
                            }
                            else
                            {
                                //計算失敗
                                return false;
                            }

                        }
                    }
                }
                else
                    result = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                result = false;
            }

            return result;

        }



        private void button3_Click(object sender, EventArgs e)
        {
            PL_ST_Table.Controls.Clear();
        }

        /// <summary>
        /// 吸嘴補償計算  //limit 200um
        /// 
        /// </summary>
        /// <returns></returns>
        private stCalData NozzleAlignCal()
        {
            stCalData result;

            result.DX = _recipeData.SB.NozzleNow_X - _msPosData.M_NozzleBase_X;
            result.DY = _recipeData.SB.NozzleNow_Y - _msPosData.M_NozzleBase_Y;
            result.DZ = _recipeData.SB.NozzleNow_Z - _msPosData.M_NozzleBase_Z;
            result.DQ = 0;

            if (result.DX > 0.70)
                result.DX = 0.70;
            if (result.DX < -0.70)
                result.DX = -0.70;

            if (result.DY > 0.70)
                result.DY = 0.70;
            if (result.DY < -0.70)
                result.DY = -0.70;


            if (result.DZ > 0.50)
                result.DZ = 0.50;
            if (result.DZ < -0.50)
                result.DZ = -0.50;

            return result;
        }

        private void txt_ST_Col_TextChanged(object sender, EventArgs e)
        {

        }

        private void B_ST_PanelGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("是否重新生成 ST陣列", "警告", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    _recipeData.Panel.ST_Col = int.Parse(E_ST_Col.Text);
                    _recipeData.Panel.ST_Row = int.Parse(E_ST_Row.Text);
                    ST_TableGenerate(int.Parse(E_ST_Col.Text), int.Parse(E_ST_Row.Text));
                    Setting_UI();
                }
            }
            catch (Exception ex)
            {

            }

        }

        POS_Data[,] ArrayPos = new POS_Data[1, 1];
        double _array_SX;
        double _array_SY;
        int _array_CountX;
        int _array_CountY;
        double _arrayPitch;

        private async void B_SB_Array_Start_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行陣列測試流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (FlowAllow())
                {
                    if (int.Parse(E_SB_Array_X_Count.Text) <= 2)
                        E_SB_Array_X_Count.Text = "2";
                    if (int.Parse(E_SB_Array_Y_Count.Text) <= 2)
                        E_SB_Array_Y_Count.Text = "2";

                    _EQP_Status = enumEQP_Status.MANU;
                    _array_SX = double.Parse(E_SB_Array_SX.Text);
                    _array_SY = double.Parse(E_SB_Array_SY.Text);
                    _array_CountX = int.Parse(E_SB_Array_X_Count.Text);
                    _array_CountY = int.Parse(E_SB_Array_Y_Count.Text);
                    _arrayPitch = double.Parse(E_SB_Array_Pitch.Text);
                    _arrayOutTotalCount = int.Parse(E_SB_Array_X_Count.Text) * int.Parse(E_SB_Array_Y_Count.Text);
                    SB_ArrayCal(_array_SX, _array_SY, _array_CountX, _array_CountY, _arrayPitch, ref ArrayPos);
                    Set_S_LaserData();
                    await Task.Run(ArrayTestFlow);
                }
            }
        }

        /// <summary>
        /// 植球Array計算
        /// </summary>
        /// <param name="sX">CCD X</param>
        /// <param name="sY">CCD Y</param>
        /// <param name="xCount"></param>
        /// <param name="yCount"></param>
        private void SB_ArrayCal(double sX, double sY, int xCount, int yCount, double pitch, ref POS_Data[,] posArray)
        {
            stCalData nozzleOffset = NozzleAlignCal();
            posArray = new POS_Data[yCount, xCount];
            for (int i = 0; i < yCount; i++)
            {
                for (int j = 0; j < xCount; j++)
                {
                    ArrayPos[i, j] = new POS_Data();

                    if (i == 0 && j == 0)
                    {
                        ArrayPos[0, 0].X = sX;
                        ArrayPos[0, 0].Y = sY;
                        ArrayPos[0, 0].LX = ArrayPos[0, 0].X - _msPosData.M_Laser2Vision_X + _recipeData.SB.SB_OffsetX + nozzleOffset.DX;
                        ArrayPos[0, 0].LY = ArrayPos[0, 0].Y - _msPosData.M_Laser2Vision_Y + _recipeData.SB.SB_OffsetY + nozzleOffset.DY;
                    }
                    else
                    {
                        ArrayPos[i, j].X = sX - pitch * j;
                        ArrayPos[i, j].Y = sY - pitch * i;
                        ArrayPos[i, j].LX = ArrayPos[i, j].X - _msPosData.M_Laser2Vision_X + _recipeData.SB.SB_OffsetX + nozzleOffset.DX;
                        ArrayPos[i, j].LY = ArrayPos[i, j].Y - _msPosData.M_Laser2Vision_Y + _recipeData.SB.SB_OffsetY + nozzleOffset.DY;
                    }
                }
            }

            // 4-----3
            // |     |        
            // |     |        
            // 1-----2
            thickPoint[0].TX = ArrayPos[0, 0].X + (_msPosData.M_Laser2Height_X - _msPosData.M_Laser2Vision_X);
            thickPoint[0].TY = ArrayPos[0, 0].Y + (_msPosData.M_Laser2Height_Y - _msPosData.M_Laser2Vision_Y);
            thickPoint[0].TZ = _msPosData.M_H_HeightZ;

            thickPoint[1].TX = ArrayPos[0, xCount - 1].X + (_msPosData.M_Laser2Height_X - _msPosData.M_Laser2Vision_X);
            thickPoint[1].TY = ArrayPos[0, xCount - 1].Y + (_msPosData.M_Laser2Height_Y - _msPosData.M_Laser2Vision_Y);
            thickPoint[1].TZ = _msPosData.M_H_HeightZ;

            thickPoint[2].TX = ArrayPos[yCount - 1, xCount - 1].X + (_msPosData.M_Laser2Height_X - _msPosData.M_Laser2Vision_X);
            thickPoint[2].TY = ArrayPos[yCount - 1, xCount - 1].Y + (_msPosData.M_Laser2Height_Y - _msPosData.M_Laser2Vision_Y);
            thickPoint[2].TZ = _msPosData.M_H_HeightZ;

            thickPoint[3].TX = ArrayPos[yCount - 1, 0].X + (_msPosData.M_Laser2Height_X - _msPosData.M_Laser2Vision_X);
            thickPoint[3].TY = ArrayPos[yCount - 1, 0].Y + (_msPosData.M_Laser2Height_Y - _msPosData.M_Laser2Vision_Y);
            thickPoint[3].TZ = _msPosData.M_H_HeightZ;
        }

        /// <summary>
        /// array 高度計算
        /// </summary>
        private bool SB_ArrayZ_Cal(double pitch, int x_count, int y_count, ref POS_Data[,] posArray)
        {
            bool result = false;
            stCalData nozzleOffset = NozzleAlignCal();
            try
            {
                double avg_Z = (thickPoint[0].MZ + thickPoint[1].MZ + thickPoint[2].MZ + thickPoint[3].MZ) / 4;

                for (int i = 0; i < y_count; i++)
                {
                    for (int j = 0; j < x_count; j++)
                    {
                        posArray[i, j].Z = _msPosData.M_H_VisionZ - avg_Z;
                        posArray[i, j].LZ = _msPosData.M_H_LaserZ - avg_Z + _recipeData.SB.SB_OffsetZ + nozzleOffset.DZ;
                    }
                }
                result = true;
            }
            catch (Exception ex)
            {

            }
            return result;

        }
        private void B_SB_Array_End_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否停止流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _arrayOutRun = false;
            }
        }

        private void btnViewCCD_Click(object sender, EventArgs e)
        {
            try
            {
                if (_pylonCamera.Connected)
                {
                    if (_viewForm == null || _viewForm.IsDisposed)
                    {
                        _viewForm = new PylonViewForm(_pylonCamera.GetCamera);
                        _viewForm.Show();
                    }
                    else
                    {
                        _viewForm.Activate();

                    }
                }
                else
                {
                    MessageBox.Show("側向CCD尚未連接。", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

            }
            catch (Exception ex)
            {
                Error_Log.Add($"[ViewCCD Error]:　{ex.ToString()}");
            }
        }


        /// <summary>
        /// 雷射出光 
        /// </summary>
        /// <param name="type"> 0:IO-NC 1:Power 2:GND 3:Signal  </param>
        /// <returns></returns>
        private async Task<bool> LaserEmission(int Padtype)
        {
            _LaserRunFlag = true;
            bool result = false;
            int step = 0;
            int count = 0;
            int retryCount = 0;
            string str = "";
            ushort value = (ushort)(_recipeData.SYSParam.NozzleSB_ValveNum);

            try
            {
                while (_LaserRunFlag)
                {
                    switch (step)
                    {
                        case 0:
                            if (retryCount >= _recipeData.SYSParam.EmissionRetry)
                            {
                                _LaserRunFlag = false;
                                str = $"雷射出球超過次數[{_recipeData.SYSParam.EmissionRetry}].";
                                LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                                result = false; ;
                            }


                            if (Set_SBLaserData(Padtype))
                            {
                                step = 1;
                            }
                            else
                            {
                                str = ("一階雷射功率切換失敗.");
                                LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                                _LaserRunFlag = false;
                                result = false;
                            }
                            break;

                        case 1:
                            //確認各軸訊號
                            if (Aerotech_En && AZD_En)// && Aerotech_Home && DeviceFlag)
                            {
                                M_UI_Running = true;
                                EnableTabPages(false);
                                SB_ALaserTimes = _recipeData.Signal_SBLoad.LaserToCloseAir_DelayTimes;
                                step = 4;
                            }
                            else
                            {
                                Error_Log.Add($"Aerotech_En Or AZD_En異常");
                                step = 0;
                            }
                            break;

                        case 2:
                            //檔案ezm專案檔讀取,成功回傳0  
                            tact_time.Restart();
                            if (axMMMark.LoadFile(_ezmPath) == 0)
                            {
                                step = 21;
                            }
                            break;


                        case 4:
                            //雷射開                           
                            if (StartMarking())
                            {
                                //LogMsgAdd(MList_Log, lb_HistoryList, $"Laser Start Time", tmpListStr);
                                LaserStatus_Emission = true;
                                Invoke(new updatalaserstauts(updatalaser));//2024-05-24
                                Thread.Sleep(10);
                                result = true;
                                step = 20;
                            }
                            else
                            {
                                Error_Log.Add($"[StartMarking]雷射出光異常!");
                                return false;
                            }

                            break;

                        case 5:
                            //雷射雕刻完成
                            if (!LaserStatus_Emission)
                            {
                                step = 20;
                                //LogMsgAdd(MList_Log, lb_HistoryList, $"Laser End Time", tmpListStr);
                            }

                            break;
                        #region 二階雷射
                        //case 6:   //二階雷射
                        //    if ((CB_Signal_LaserTwoStep.Checked && Padtype == (int)enum_LaserType.Signal) || (CB_Ground_LaserTwoStep.Checked && Padtype == (int)enum_LaserType.GND) || (CB_Power_LaserTwoStep.Checked && Padtype == (int)enum_LaserType.Power) )
                        //    {                              
                        //        if (Set_SBLaserData2((enum_LaserType)Padtype)) 
                        //        {
                        //            step = 7;
                        //        }                                    
                        //        else
                        //        {
                        //            str = "二階雷射功率切換失敗.";
                        //            LogMsgAdd(Error_Log, lb_ErrorList, str, tmpErrStr);
                        //            _LaserRunFlag = false;
                        //            return false;
                        //        }
                        //    }
                        //    else
                        //        step = 20;
                        //    break;


                        //case 7:
                        //    if (_Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                        //   _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                        //   _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000))
                        //    {
                        //        if (Motion_X && Motion_Y && Motion_Z)
                        //        {                                            
                        //            double offsetZ = _msPosData.M_H_LaserZ2_Offset / 1000; //轉um
                        //            this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveInc(2, offsetZ, 20);
                        //            step = 8;
                        //        }
                        //    }
                        //    break;

                        //case 8:
                        //    if (_Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                        //   _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                        //   _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000))
                        //    {
                        //        if (Motion_X && Motion_Y && Motion_Z)
                        //        {
                        //            //確認各軸訊號
                        //            if (Aerotech_En && AZD_En)// && Aerotech_Home && DeviceFlag)
                        //            {
                        //                M_UI_Running = true;                     
                        //                EnableTabPages(false);
                        //                step = 9;
                        //            }                                                                     
                        //        }
                        //    }
                        //    break;      

                        //case 9:
                        //    //雷射開
                        //    if (StartMarking())
                        //    {
                        //        LaserStatus_Emission = true;

                        //        Invoke(new updatalaserstauts(updatalaser));//2024-05-24                                
                        //        step = 10;
                        //    }
                        //    else
                        //        return false;
                        //    break;

                        //case 10:
                        //    //雷射雕刻完成
                        //    if (!LaserStatus_Emission)
                        //    {                              
                        //        step = 20;
                        //    }

                        //    break;

                        #endregion 二階雷射

                        case 20:
                            //流程結束
                            EnableTabPages(true);
                            M_UI_Running = false;
                            Thread.Sleep(_recipeData.SYSParam.NoSB_AirJudgeDelay);
                            P_F_EthernetIP.DataUpdataOk = false;
                            step = 21;

                            break;

                        case 21:
                            if (P_F_EthernetIP.DataUpdataOk)
                            {
                                if (NoSB_Air_Check())
                                {
                                    NowNozzle_MAX_Pressure = 0;
                                    NowNozzle_MIN_Pressure = NowNozzlePressure;
                                    _LaserRunFlag = false;
                                    return true;
                                }
                                else
                                {
                                    Thread.Sleep(_recipeData.SYSParam.NoSB_AirJudgeDelay + _msPosData.NoSB_AirJudgeNG_Delay);
                                    Error_Log.Add($"無球球延遲判定[{_recipeData.SYSParam.NoSB_AirJudgeDelay + _msPosData.NoSB_AirJudgeNG_Delay}ms]");
                                    if (NoSB_Air_Check())
                                    {
                                        NowNozzle_MAX_Pressure = 0;
                                        NowNozzle_MIN_Pressure = NowNozzlePressure;
                                        _LaserRunFlag = false;
                                        return true;
                                    }
                                    else
                                    {
                                        retryCount++;
                                        step = 0;
                                    }
                                }
                            }

                            break;
                    }

                    Thread.Sleep(1);
                    if (count > 500)
                    {
                        Error_Log.Add($"[LaserEmission Error]:Emission Timeout!");
                        _LaserRunFlag = false;
                        return false;
                    }
                    count++;

                }

            }
            catch (Exception ex)
            {
                Error_Log.Add($"[LaserEmission Error]:" + ex.ToString());
                _LaserRunFlag = false;
                return false;
            }

            return result;
        }

        private void DG_Path_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void B_SB_Path_Save_Click(object sender, EventArgs e)
        {
            SB_PathSave();


        }

        /// <summary>
        /// 植球路徑儲存
        /// </summary>
        /// <returns></returns>
        private bool SB_PathSave()
        {
            bool result = false;
            try
            {
                SaveFileDialog savefile = new SaveFileDialog();

                savefile.Filter = "Excel Files (*.xlsx)|*.xlsx|Excel Files (*.xls)|*.xls|All Files (*.*)|*.*";
                savefile.InitialDirectory = $@"{_ConfigSystem._RecipePath}";

                if (savefile.ShowDialog() == DialogResult.OK)
                {
                    DataTable dt = new DataTable();
                    DataColumn idCol = new DataColumn("ID", typeof(string));
                    DataColumn xCol = new DataColumn("X", typeof(string));
                    DataColumn yCol = new DataColumn("Y", typeof(string));
                    dt.Columns.AddRange(new DataColumn[] { idCol, xCol, yCol });

                    DataRow tmprow = dt.NewRow();
                    tmprow["ID"] = "";
                    tmprow["X"] = txt_SB_PathAlignX1.Text;
                    tmprow["Y"] = txt_SB_PathAlignY1.Text;
                    tmprow["Bonded"] = "N";
                    dt.Rows.Add(tmprow);

                    tmprow = dt.NewRow();
                    tmprow[0] = "";
                    tmprow[1] = txt_SB_PathAlignX2.Text;
                    tmprow[2] = txt_SB_PathAlignY2.Text;

                    dt.Rows.Add(tmprow);

                    foreach (DataGridViewRow tmp in DGV_SB_Path.Rows)
                    {
                        tmprow = dt.NewRow();
                        tmprow["ID"] = tmp.Cells[0].Value;
                        tmprow["X"] = tmp.Cells[1].Value;
                        tmprow["Y"] = tmp.Cells[2].Value;
                        tmprow["Bonded"] = "N";
                        dt.Rows.Add(tmprow);
                    }

                    dt.ExportToExcel(savefile.FileName);

                    Define.st_SB_Path tmpData;
                    foreach (DataRow tdata in dt.Rows)
                    {
                        tmpData.ID = "";
                        tmpData.X_Pos = Convert.ToDouble(tdata.ItemArray[0]);
                        tmpData.Y_Pos = Convert.ToDouble(tdata.ItemArray[1]);
                        tmpData.PadType = Convert.ToInt16(tdata.ItemArray[2]);
                        tmpData.Mount = false;
                        SB_PathData.SB_FilePathPos.Add(tmpData);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"產生錯誤[{ex.ToString()}]", "異常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return result;
        }




        private void B_SB_Path_Load_Click(object sender, EventArgs e)
        {

        }
        /// <summary>
        /// 植球路徑讀取
        /// </summary>
        /// <returns></returns>
        private bool SB_PathLoad()
        {
            bool result = false;
            try
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show($"產生錯誤[{ex.ToString()}]", "異常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return result;
        }

        /// <summary>
        /// 行數編碼
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DG_Path_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            ///繪製行號
            using (SolidBrush brush = new SolidBrush(DGV_SB_Path.RowHeadersDefaultCellStyle.ForeColor))
            {
                //string rowNumber = (e.RowIndex + 1).ToString();
                //e.Graphics.DrawString(rowNumber, DGV_SB_Path.Font, brush, e.RowBounds.Location.X + 4, e.RowBounds.Location.Y + 4);

                if (e.RowIndex < 0) return;  // 跳過標頭行

                // 行號從 1 開始
                string rowNumber = (e.RowIndex + 1).ToString();

                // 使用 DataGridView 的字型（或指定 RowHeadersDefaultCellStyle.Font）
                Font font = DGV_SB_Path.RowHeadersDefaultCellStyle.Font ?? DGV_SB_Path.Font;

                // 計算文字實際大小（精準測量）
                SizeF textSize = e.Graphics.MeasureString(rowNumber, font);

                // 水平置中：左邊界 + (欄寬 - 文字寬) / 2
                float x = e.RowBounds.Left + (DGV_SB_Path.RowHeadersWidth - textSize.Width) / 2;

                // 垂直置中：上邊界 + (行高 - 文字高) / 2
                float y = e.RowBounds.Top + (e.RowBounds.Height - textSize.Height) / 2;

                // 畫文字（使用 TextRenderer 更好，支援高 DPI）
                TextRenderer.DrawText(e.Graphics, rowNumber,
                                      font,
                                      new Point((int)x, (int)y),
                                      DGV_SB_Path.RowHeadersDefaultCellStyle.ForeColor,
                                      TextFormatFlags.NoPadding);  // 可加 HorizontalCenter 如果需要再微調

            }


        }

        private void btn_DiskAlignFlow_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行分離盤對位流程? (請確認儲存槽無錫球) ", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _DiskAlignFlowStep = enumDisk_AlignFlowStep.DiskAlign_Step1;

                if (FlowAllow())
                {
                    Task.Run(DiskAlignFlow);
                    LogMsgAdd(MList_Log, lb_HistoryList, "執行分離盤對位流程。", tmpListStr);
                }

            }
        }

        private void btn_ArraytestGetPos_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否取得位置?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                E_SB_Array_SX.Text = txt_Motion_Position_X.Text;
                E_SB_Array_SY.Text = txt_Motion_Position_Y.Text;
            }
        }

        private void B_PM_ParamSave_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否儲存參數?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _msPosData.PM_Power = double.Parse(txt_PM_Power.Text);
                _msPosData.PM_Time = double.Parse(txt_PM_Time.Text);
                _msPosData.LaserAlignTargetPower = double.Parse(txt_LaserAlignPowerTarget.Text);
                _msPosData.LaserTriggerInterval = double.Parse(txt_LaserTriggerInterval.Text);
                _msPosData.Save_Data();
                Setting_UI();
            }
        }

        private void btn_DiskRunSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    LogMsgAdd(MList_Log, lb_HistoryList, "執行分離盤速度參數儲存。", tmpListStr);

                    _recipeData.SYSParam.DiskRunSpeed = Convert.ToDouble(txt_DiskRunSpeed.Text);
                    _recipeData.SYSParam.DiskRunACC = Convert.ToDouble(txt_DiskRunACC.Text);
                    _recipeData.SYSParam.DiskRunDEC = Convert.ToDouble(txt_DiskRunDEC.Text);
                    _recipeData.SYSParam.RotateAngle = Convert.ToDouble(txt_RotateAngle.Text);
                    _recipeData.Save_Data(RecipeFileName);
                    Setting_UI();
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void btn_ManualVacuum_Click(object sender, EventArgs e)
        {
            //抽氣
            int count = 12;

            if (IOCard_DOValue[0, count])
            {
                IOCard_OutputRelay_OFF(0, count);
                IOCard_DOValue[0, count] = false;
                _DO1button[count].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.OFF];
                btn_ManualVacuum.BackColor = SystemColors.Control;
            }
            else
            {
                IOCard_OutputRelay_ON(0, count);
                IOCard_DOValue[0, count] = true;
                _DO1button[count].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.ON];
                btn_ManualVacuum.BackColor = Color.Lime;
            }
        }

        private void btn_SaveWait_Z_Pos_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行等待位置 Z參數儲存。", tmpListStr);
                _msPosData.M_Wait_Z = Convert.ToDouble(txt_Wait_Z_Pos.Text);


                _msPosData.Save_Data();
                Setting_UI();
            }
        }

        private void groupBox39_Enter(object sender, EventArgs e)
        {

        }

        private void btn_DXF_Click(object sender, EventArgs e)
        {
            DXF_ViewForm _dxfForm = new DXF_ViewForm();
            _dxfForm.ShowDialog();
        }

        private void tabIPG_Laser_Click(object sender, EventArgs e)
        {

        }



        private void btnLaserAlignFlow_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行雷射校正流程?  ", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (FlowAllow())
                {
                    if (!_CU30CL.MotionFlag && _CU30CL.HomeFlag)
                    {
                        _EQP_Status = enumEQP_Status.MANU;
                        _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step1;
                        Task.Run(LaserAlignFlow);
                        LogMsgAdd(MList_Log, lb_HistoryList, "執行雷射校正流程。", tmpListStr);
                    }
                    else if (_CU30CL.MotionFlag)
                    {
                        MessageBox.Show("馬達移動中!", "Warning");
                    }
                    else if (!_CU30CL.HomeFlag)
                    {
                        MessageBox.Show("馬達尚未復歸!", "Warning");
                    }


                }
            }
        }



        private void groupBox21_Enter(object sender, EventArgs e)
        {

        }



        private void btn_PM_TestOn_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行PowerMeter測試流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                int delayTime = int.Parse(txt_PM_LaserOffTime.Text);
                _PowerMeterFlowStep = enumPowerMeterFlowStep.PowerMeter_Step1;
                //if (FlowAllow())
                Task.Run(() => { PowerMeterTestFlow(delayTime); });

            }
        }

        private void btn_PM_TestOff_Click(object sender, EventArgs e)
        {
            _RunFlag = false;
        }

        private void btn_PizeoHome_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行PizeoMotor復歸流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (!_CU30CL.MotionFlag)
                    _CU30CL.AllHome();
                else
                {
                    MessageBox.Show("馬達移動中?", "Warning");
                }
            }
        }

        private void btn_CoaxialLight_Set_Click(object sender, EventArgs e)
        {
            AlignLight(_CoaxialLight, byte.Parse(txt_CoaxialLight.Text));
        }

        private void chk_PizeoEnable_X_CheckedChanged(object sender, EventArgs e)
        {
            _CU30CL.AsyncEnableAxis(2);

        }

        private void chk_PizeoEnable_Y_CheckedChanged(object sender, EventArgs e)
        {
            _CU30CL.AsyncEnableAxis(1);

        }

        private void chk_PizeoEnable_Z_CheckedChanged(object sender, EventArgs e)
        {
            _CU30CL.AsyncEnableAxis(3);

        }

        private void t_SlowStatusUpdate_Tick(object sender, EventArgs e)
        {
            t_SlowStatusUpdate.Enabled = false;
            //  UI
            L_E5CC_PV.Text = $"PV = {E5CC.Now_PV.ToString("F1")} 度";
            L_E5CC_SV.Text = $"SV = {E5CC.Now_SV.ToString("F1")} 度";
            L_TableTemp.Text = $"平台 = {_TableTempSet} 度";
            // st
            L_ST_AutoProgress.Text = $"當前ST_No:[{NowST_NO}] 當前Pad_No:[{NowPadNO}] 當前SB_No:[{NowBallNO}]";
            //sb
            L_SB_AutoProgress.Text = $"當前SB_No:[{Now_SB_PadNO}] ";

            //刷新sb grid
            if (old_Now_SB_PadNO != Now_SB_PadNO)
            {
                old_Now_SB_PadNO = Now_SB_PadNO;
                DGV_SB_Path.Rows[old_Now_SB_PadNO - 1].Selected = true;
                DGV_SB_Path.CurrentCell = DGV_SB_Path.Rows[old_Now_SB_PadNO - 1].Cells[0];
                DGV_SB_Path.FirstDisplayedScrollingRowIndex = old_Now_SB_PadNO - 1;
            }



            if (_IPG_LaserConnected)
                IPG_Status_Update();
            //強制關溫控器
            if (IO_InputData.OverHeater)
                IO_OutputControl("溫控器關");

            if (_EQP_Status == enumEQP_Status.AUTO)
            {
                IO_OutputControl("綠燈開");
                btn_LightStatus.BackColor = Color.LawnGreen;
            }

            if (_EQP_Status == enumEQP_Status.DOWN)
            {
                IO_OutputControl("紅燈開");
                btn_LightStatus.BackColor = Color.Red;
            }

            if (_EQP_Status == enumEQP_Status.IDLE)
            {
                IO_OutputControl("黃燈開");
                btn_LightStatus.BackColor = Color.Yellow;
            }

            //IPG status 
            if (IPG_WorkStaus())
            {
                pl_LaserReady.BackColor = Color.LawnGreen;
            }
            else
            {
                pl_LaserReady.BackColor = Color.Gray;

            }

            if (_Using)
            {
                //light box
                L_NozzleXY_Light.Text = _LSG075A_4_Control.Bright_1.ToString();
                L_NozzleZ_Light.Text = _LSG075A_4_Control.Bright_2.ToString();
                L_Vacuum.Text = $"Vacuum: -{StageVacuum.ToString("F1")} Kpa";

                if (IOCard_DOValue[0, 12])
                    gb_Vacuum.BackColor = (StageVacuum >= _msPosData.VacuumThreshold) ? Color.Lime : Color.Red;
                else
                    gb_Vacuum.BackColor = Color.White;

            }

            L_EQP_Status.Text = _EQP_Status.ToString();

            //ST 植球當前對位位值
            L_NowAlignPos_X1.Text = SB_PathData.NowSB_AlignPos[0].X_Pos.ToString("0.000");
            L_NowAlignPos_Y1.Text = SB_PathData.NowSB_AlignPos[0].Y_Pos.ToString("0.000");
            L_NowAlignPos_X2.Text = SB_PathData.NowSB_AlignPos[1].X_Pos.ToString("0.000");
            L_NowAlignPos_Y2.Text = SB_PathData.NowSB_AlignPos[1].Y_Pos.ToString("0.000");


            //connected
            pic_AerotechConnect.BackgroundImage = (_aerotechConnected) ? Properties.Resources.status_on : Properties.Resources.status_off;
            pic_AxisQ_Connect.BackgroundImage = (_orientalConnected) ? Properties.Resources.status_on : Properties.Resources.status_off;
            pic_CVX_Connect.BackgroundImage = (_CVX_Connected) ? Properties.Resources.status_on : Properties.Resources.status_off;
            pic_CL_Connect.BackgroundImage = (_CL_Connected) ? Properties.Resources.status_on : Properties.Resources.status_off;
            pic_HeaterConnect.BackgroundImage = (_heaterConnnected) ? Properties.Resources.status_on : Properties.Resources.status_off;
            pic_IOCardConnect.BackgroundImage = (_IO_Connected) ? Properties.Resources.status_on : Properties.Resources.status_off;
            pic_IO_LinkConnect.BackgroundImage = (_IO_LinkConnect) ? Properties.Resources.status_on : Properties.Resources.status_off;
            pic_AXM_Connect.BackgroundImage = (_AxMarkConnected) ? Properties.Resources.status_on : Properties.Resources.status_off;
            pic_LaserConnect.BackgroundImage = (_IPG_LaserConnected) ? Properties.Resources.status_on : Properties.Resources.status_off;
            Pic_AutoStep1.BackgroundImage = (_fileLoad) ? Properties.Resources.status_on : Properties.Resources.status_off;
            Pic_SB_Step1.BackgroundImage = (_fileLoad) ? Properties.Resources.status_on : Properties.Resources.status_off;
            Pic_SB_Step2.BackgroundImage = (_stCal_OK && _stMHeight_OK) ? Properties.Resources.status_on : Properties.Resources.status_off;
            picSB_PathStep1.BackgroundImage = (_sbPathLoad) ? Properties.Resources.status_on : Properties.Resources.status_off;
            picSB_PathStep2.BackgroundImage = (_sbCal_OK && _sbMHeight_OK) ? Properties.Resources.status_on : Properties.Resources.status_off;
            Pic_AutoStep2.BackgroundImage = (_ST_Use) ? Properties.Resources.status_on : Properties.Resources.status_off;
            pic_PM_Connect.BackgroundImage = (_PowerMeterConnect) ? Properties.Resources.status_on : Properties.Resources.status_off;
            pic_TH_Connect.BackgroundImage = (_TH_SensorConnect) ? Properties.Resources.status_on : Properties.Resources.status_off;
            pic_PizeoConnect.BackgroundImage = (_PizeoConnect) ? Properties.Resources.status_on : Properties.Resources.status_off;
            //測試頁
            L_HMeasure_Result.Text = $"1.[{thickPoint[0].MZ.ToString("F3")}] 2.[{thickPoint[1].MZ.ToString("F3")}] 3.[{thickPoint[2].MZ.ToString("F3")}] 4.[{thickPoint[3].MZ.ToString("F3")}]  平均[{ PublicData.ST_HeightZ.ToString("F3")}]";
            L_ArrayHight.Text = $"1.[{thickPoint[0].MZ.ToString("F3")}] 2.[{thickPoint[1].MZ.ToString("F3")}] 3.[{thickPoint[2].MZ.ToString("F3")}] 4.[{thickPoint[3].MZ.ToString("F3")}]  平均[{ PublicData.ST_HeightZ.ToString("F3")}]";
            L_GrabTest_Result.Text = $"Cal DX[{PublicData.ST_V_CalData.DX.ToString("F3")}]  DY[{PublicData.ST_V_CalData.DY.ToString("F3")}]  DQ[{PublicData.ST_V_CalData.DQ.ToString("F3")}]";
            L_RunFlag.Text = $"Run = {_RunFlag},  Disk = {_DiskRunFlag},  Laser = {_LaserRunFlag},  Clear = {_ClearFlag},  Array = {_arrayOutRun}";


            L_HeatertHight.BackColor = (IO_InputData.HeaterHight) ? Color.Red : Color.WhiteSmoke;
            L_HeatertLow.BackColor = (IO_InputData.HeaterLow) ? Color.Red : Color.WhiteSmoke;
            L_OverHeat.BackColor = (IO_InputData.OverHeater) ? Color.Red : Color.WhiteSmoke;
            L_BallOut_Result.Text = $"總出球數量[{_ballOutTotalCount}]  目前出球數量[{_ballOutCount}]  剩餘出球數量[{_ballOutTotalCount - _ballOutCount}]  ";
            L_ArrayTotalCount.Text = $"總出球數量[{_arrayOutTotalCount}]  目前出球數量[{_arrayOutCount}]  剩餘出球數量[{_arrayOutTotalCount - _arrayOutCount}]  ";


            t_SlowStatusUpdate.Enabled = true;
        }

        private void btn_PizeoStop_Click(object sender, EventArgs e)
        {
            _CU30CL.PizeoStop();
        }



        private void btn_StageVacuum_Click(object sender, EventArgs e)
        {
            //平台真空
            int count = 12;

            if (IOCard_DOValue[0, count])
            {
                IOCard_OutputRelay_OFF(0, count);
                IOCard_DOValue[0, count] = false;
                _DO1button[count].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.OFF];
                btn_StageVacuum.BackColor = SystemColors.Control;
            }
            else
            {
                IOCard_OutputRelay_ON(0, count);
                IOCard_DOValue[0, count] = true;
                _DO1button[count].BackgroundImage = imageList_16_16.Images[(int)Define.UIImage_List.ON];
                btn_StageVacuum.BackColor = Color.Lime;
            }
        }

        private void btn_SetpointMove_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("是否執行PizeoMotor SetPoint 移動流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (!_CU30CL.MotionFlag && _CU30CL.HomeFlag)
                    {
                        double p1 = double.Parse(txt_PizeoSetX.Text);
                        double p2 = double.Parse(txt_PizeoSetY.Text);
                        double p3 = double.Parse(txt_PizeoSetZ.Text);

                        double[] abs = new double[] { p2, p1, p3 };
                        _CU30CL.CloseLoopServoMove(200, abs);
                    }
                    else if (_CU30CL.MotionFlag)
                    {
                        MessageBox.Show("馬達移動中!", "Warning");
                    }
                    else if (!_CU30CL.HomeFlag)
                    {
                        MessageBox.Show("馬達尚未復歸!", "Warning");
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void btn_SaveSetP_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否儲存SetPoint數值?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LasetSetPosSave();
                //ini save
                _ConfigSystem.WriteIntoIniFile();
            }
        }

        private void B_NowAlignSetPos_1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行[Reference Point-1] 設定?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                TJJS_Point stageXY = new TJJS_Point();
                POS_Data alignPOS = new POS_Data();

                _sbCal_OK = false;
                _sbGrab1_OK = false;

                stageXY.X = double.Parse(txt_Motion_Position_X.Text);
                stageXY.Y = double.Parse(txt_Motion_Position_Y.Text);
                SB_PathData.NowSB_AlignPos[0].X_Pos = stageXY.X;
                SB_PathData.NowSB_AlignPos[0].Y_Pos = stageXY.Y;
                alignPOS.X = SB_PathData.NowSB_AlignPos[0].X_Pos;
                alignPOS.Y = SB_PathData.NowSB_AlignPos[0].Y_Pos;
                alignPOS.Z = _msPosData.M_H_VisionZ;
                alignPOS.TX = SB_PathData.NowSB_AlignPos[0].X_Pos + (_msPosData.M_Laser2Height_X - _msPosData.M_Laser2Vision_X);
                alignPOS.TY = SB_PathData.NowSB_AlignPos[0].Y_Pos + (_msPosData.M_Laser2Height_Y - _msPosData.M_Laser2Vision_Y);
                alignPOS.TZ = _msPosData.M_H_HeightZ;


                stageXY.X = -double.Parse(txt_Motion_Position_X.Text);
                stageXY.Y = -double.Parse(txt_Motion_Position_Y.Text);

                PublicData.SB_AlignLine.Start = stageXY;
                PublicData.SB_BaseAlignLine.Start = stageXY;
                _sbGrab1_OK = true;
                LogMsgAdd(MList_Log, lb_HistoryList, "SB-P1 設定完成。", tmpListStr);

                if (FlowAllow())
                {
                    Task.Run(() => { SB_Ref_PointMove(2); }); //move ref-2
                }

            }
        }

        private void B_NowAlignSetPos_2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行[Reference Point-2] 設定?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (_sbGrab1_OK && _sbMHeight_OK)
                {
                    TJJS_Point stageXY = new TJJS_Point();
                    POS_Data alignPOS = new POS_Data();

                    //if (PactechFile_RadioSet())
                    //{ 
                    stageXY.X = double.Parse(txt_Motion_Position_X.Text);
                    stageXY.Y = double.Parse(txt_Motion_Position_Y.Text);
                    SB_PathData.NowSB_AlignPos[1].X_Pos = stageXY.X;
                    SB_PathData.NowSB_AlignPos[1].Y_Pos = stageXY.Y;
                    alignPOS.X = SB_PathData.NowSB_AlignPos[1].X_Pos;
                    alignPOS.Y = SB_PathData.NowSB_AlignPos[1].Y_Pos;
                    alignPOS.Z = _msPosData.M_H_VisionZ;
                    alignPOS.TX = SB_PathData.NowSB_AlignPos[1].X_Pos + (_msPosData.M_Laser2Height_X - _msPosData.M_Laser2Vision_X);
                    alignPOS.TY = SB_PathData.NowSB_AlignPos[1].Y_Pos + (_msPosData.M_Laser2Height_Y - _msPosData.M_Laser2Vision_Y);
                    alignPOS.TZ = _msPosData.M_H_HeightZ;

                    stageXY.X = -double.Parse(txt_Motion_Position_X.Text);
                    stageXY.Y = -double.Parse(txt_Motion_Position_Y.Text);

                    PublicData.SB_AlignLine.End = stageXY;
                    PublicData.SB_BaseAlignLine.End = PublicData.SB_BaseAlignLine.Start + new TJJS_Point(SB_PathData.SB_AlignPos[1].X_Pos - SB_PathData.SB_AlignPos[0].X_Pos
                        , SB_PathData.SB_AlignPos[1].Y_Pos - SB_PathData.SB_AlignPos[0].Y_Pos);

                    LogMsgAdd(MList_Log, lb_HistoryList, "SB-P2 設定完成。", tmpListStr);
                    _sbCal_OK = SB_CalData();
                    //}
                    //else
                    //{
                    //    MessageBox.Show("Reference Point轉換失敗!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //}
                }
                else if (!_sbGrab1_OK)
                {
                    MessageBox.Show("請先執行Reference Point-1 設定!", "Warning", MessageBoxButtons.OK);
                }
                else if (!_sbMHeight_OK)
                {
                    MessageBox.Show("請先執行高度設定!", "Warning", MessageBoxButtons.OK);
                }
            }
        }

        /// <summary>
        /// pactech 檔案比率設定
        /// </summary>
        private bool PactechFile_RadioSet()
        {
            bool result = false;
            try
            {
                double radio_X = 0; double radio_Y = 0;
                radio_X = Math.Abs((SB_PathData.NowSB_AlignPos[1].X_Pos - SB_PathData.NowSB_AlignPos[0].X_Pos) / (SB_PathData.SB_AlignPos[1].X_Pos - SB_PathData.SB_AlignPos[0].X_Pos));
                radio_Y = Math.Abs((SB_PathData.NowSB_AlignPos[1].Y_Pos - SB_PathData.NowSB_AlignPos[0].Y_Pos) / (SB_PathData.SB_AlignPos[1].Y_Pos - SB_PathData.SB_AlignPos[0].Y_Pos));
                //cal radio 
                SB_PathData.SB_AlignPos[0].X_Pos = SB_PathData.SB_AlignPos[0].X_Pos * radio_X;
                SB_PathData.SB_AlignPos[0].Y_Pos = SB_PathData.SB_AlignPos[0].Y_Pos * radio_Y;
                SB_PathData.SB_AlignPos[1].X_Pos = SB_PathData.SB_AlignPos[1].X_Pos * radio_X;
                SB_PathData.SB_AlignPos[1].Y_Pos = SB_PathData.SB_AlignPos[1].Y_Pos * radio_Y;

                Define.st_SB_Path tmp;
                for (int i = 0; i < SB_PathData.SB_FilePathPos.Count; i++)
                {
                    tmp = new Define.st_SB_Path();
                    tmp.X_Pos = SB_PathData.SB_FilePathPos[i].X_Pos * radio_X;
                    tmp.Y_Pos = SB_PathData.SB_FilePathPos[i].Y_Pos * radio_Y;
                    SB_PathData.SB_FilePathPos[i] = tmp;
                }

                txt_SB_PathAlignX1.Text = SB_PathData.SB_AlignPos[0].X_Pos.ToString("0.000");
                txt_SB_PathAlignY1.Text = SB_PathData.SB_AlignPos[0].Y_Pos.ToString("0.000");
                txt_SB_PathAlignX2.Text = SB_PathData.SB_AlignPos[1].X_Pos.ToString("0.000");
                txt_SB_PathAlignY2.Text = SB_PathData.SB_AlignPos[1].Y_Pos.ToString("0.000");

                for (int i = 0; i < DGV_SB_Path.RowCount; i++)
                {
                    DGV_SB_Path.Rows[i].Cells[0].Value = SB_PathData.SB_FilePathPos[i].ID;
                    DGV_SB_Path.Rows[i].Cells[1].Value = SB_PathData.SB_FilePathPos[i].X_Pos;
                    DGV_SB_Path.Rows[i].Cells[2].Value = SB_PathData.SB_FilePathPos[i].Y_Pos;
                }


                result = true;
            }
            catch (Exception ex)
            {
                LogMsgAdd(Error_Log, lb_ErrorList, $"[PactechFile_RadioSet Error]: {ex.ToString()}", tmpErrStr);
            }

            return result;
        }

        private void btn_SB_HeightSet_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show($"是否設定當前測高值[{lbl_M_AG_ZOffset.Text}]mm?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                double tmp = double.Parse(lbl_M_AG_ZOffset.Text);
                if (Math.Abs(tmp) <= 7)
                {
                    PublicData.SB_HeightZ = tmp;


                    for (int i = 0; i < SB_PathData.FilePathCount; i++)
                    {
                        DGV_Thick_Update(i, tmp);
                        SB_PathData.SB_PathPos[i].MZ = PublicData.SB_HeightZ;
                    }
                    _sbMHeight_OK = true;
                    LogMsgAdd(MList_Log, lb_HistoryList, "植球單點高度計算完成", tmpListStr);
                }
                else
                {
                    PublicData.SB_HeightZ = 0;
                    Invoke(new dele_msgShow(ErrMSG_Show), "測高讀值異常");
                }
            }
        }

        private void DGV_SB_Path_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (MessageBox.Show("是否設定Bonded狀態?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                //bonded 變更
                if (DGV_SB_Path.CurrentCell.Value.ToString() == "True")
                {
                    DGV_Path_Unfinished_Update(SB_SelectIndex);
                }
                else if (DGV_SB_Path.CurrentCell.Value.ToString() == "False")
                {
                    DGV_Path_Finish_Update(SB_SelectIndex);
                }
            }
        }

        private void B_Test_Click(object sender, EventArgs e)
        {

        }

        private void btn_Set_IO_LaserData_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                SetIO_LaserData();
            }
        }

        private void btn_PadTypeSave_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否儲存參數?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _msPosData.SENSE_PadType = int.Parse(txt_SENSE_PadType.Text);
                _msPosData.POWER_PadType = int.Parse(txt_POWER_PadType.Text);
                _msPosData.GND_PadType = int.Parse(txt_GND_PadType.Text);
                _msPosData.IO_PadType = int.Parse(txt_IO_PadType.Text);
                _msPosData.NC_PadType = int.Parse(txt_NC_PadType.Text);
                _msPosData.Save_Data();
                Setting_UI();
            }
        }

        private void btn_Set_NC_LaserData_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                SetNC_LaserData();
            }
        }

        private void pic_PizeoConnect_Click(object sender, EventArgs e)
        {
            if (_PizeoConnect)
            {
                if (MessageBox.Show("是否執行Pieo Motor Close?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _CU30CL.Close();
                    _PizeoConnect = false;
                }
            }
            else
            {
                if (MessageBox.Show("是否執行Pieo Motor Connect?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _CU30CL = new Mechonics_CU30CL();
                    if (_CU30CL.Connected)
                        _PizeoConnect = true;
                }
            }
        }

        private void t_ConnectStatus_Tick(object sender, EventArgs e)
        {
            t_ConnectStatus.Enabled = false;
            try
            {
                //Connect
                _aerotechConnected = Controller.IsRunning;

                _CVX_Connected = _Keyence_CVX.Connected;
                if (!_CVX_Connected)
                    _CVX_Connected = Keyence_TCPConnect(_ConfigSystem._CVX_IP, _ConfigSystem._CVX_Port);

                _CL_Connected = _Keyence_Height.Check_Connect();
                if (!_CL_Connected)
                {
                    _Keyence_Height.Close();
                    Thread.Sleep(200);
                    _Keyence_Height.Connect();
                }
                //_orientalConnected = _AZD_Controller.CheckPortOpen();
                //if (!_orientalConnected)
                //{
                //    AZD_Disconnect();
                //    Thread.Sleep(200);
                //    AZD_Connect();
                //}

                _heaterConnnected = E5CC.Connected;
                if (!_heaterConnnected)
                {
                    E5CC.Com_Close();
                    Thread.Sleep(200);
                    E5CC.Com_Connect(_ConfigSystem._E5CC_COM, 38400, Parity.Even, 8, StopBits.One, Handshake.None, false);
                }
                _IO_LinkConnect = FestoPressure.Connected;
                if (_IO_LinkConnect)
                {
                    FestoPressure.Disconnect();
                    FestoPressure = new FESTO_SPAN(_ConfigSystem._IO_Link_IP, _ConfigSystem._IO_Link_Port, 1);

                }
                _IPG_LaserConnected = IPGLaserControl.CheckConnection();
                if (!_IPG_LaserConnected)
                {
                    IPGLaserControl.DisConnection();
                    Thread.Sleep(200);
                    IPGLaserControl.Connection();
                }

                _PowerMeterConnect = _powermeter.Connected;
                //if (!_PowerMeterConnect)
                //{
                //    _powermeter.Close();
                //    Thread.Sleep(200);
                //    _powermeter.Init();

                //}


                //_PizeoConnect = _CU30CL.Connected;
                //if (!_PizeoConnect)
                //{
                //	_CU30CL.Close();
                //	Thread.Sleep(200);
                //	_CU30CL = new Mechonics_CU30CL();
                //}
            }
            catch (Exception ex)
            {

            }

            t_ConnectStatus.Enabled = true;
        }

        private void btn_AlignBall_Save_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行植球校驗位置 參數儲存。", tmpListStr);
                _msPosData.M_AlignBall_X = Convert.ToDouble(txt_AlignBall_X.Text);
                _msPosData.M_AlignBall_Y = Convert.ToDouble(txt_AlignBall_Y.Text);
                _msPosData.M_AlignBall_Z = Convert.ToDouble(txt_AlignBall_Z.Text);


                _msPosData.Save_Data();
                Setting_UI();
            }
        }

        private void btn_BallAlignMove_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行植球校驗位置移動?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {

                if (FlowAllow())
                {
                    Task.Run(BallAlignPOS_MoveFlow);
                    LogMsgAdd(MList_Log, lb_HistoryList, "執行植球校驗位置移動", tmpListStr);
                }

            }
        }



        private void button1_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 中心向外移動
        /// </summary>
        /// <param name="squareSize">中心向外邊長限制</param>
        /// <param name="step_size">移動量</param>
        /// <returns></returns>
        public List<(double, double)> GetSpiralPath2(double cX, double cY, double arealimit, double step_size)
        {
            var path = new List<(double, double)>();

            double x = cX, y = cY;
            path.Add((x, y));

            double[,] dirs = { { step_size, 0 }, { 0, step_size }, { -step_size, 0 }, { 0, -step_size } }; // 右、下、左、上  
            int dirIndex = 0;
            int steps = 1;     // 起始步數  
            int totalCells = (int)(((arealimit * 2) / step_size) * ((arealimit * 2) / step_size));

            while (path.Count < totalCells)
            {
                for (int repeat = 0; repeat < 2; repeat++) // 每一圈，步數走兩次方向  
                {
                    var dx = dirs[dirIndex, 0];
                    var dy = dirs[dirIndex, 1];

                    for (int move = 0; move < steps; move++)
                    {
                        x += dx;
                        y += dy;

                        //if (x >= 0 && x < n && y >= 0 && y < n)
                        if (x >= (cX - arealimit) && x <= (cX + arealimit) && y >= (cY - arealimit) && y <= (cY + arealimit))
                        {
                            path.Add((x, y));
                            if (path.Count >= totalCells)
                                return path;
                        }
                    }

                    dirIndex = (dirIndex + 1) % 4; // 換方向  
                }
                steps++;
            }

            return path;
        }

        private void btn_ModelCH_Move_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行模組切換位置?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (FlowAllow())
                {
                    _EQP_Status = enumEQP_Status.MANU;
                    Task.Run(ModelCHMoveFlow);
                    LogMsgAdd(MList_Log, lb_HistoryList, "執行模組切換位置流程", tmpListStr);
                }
            }
        }

        private void btn_ModelCH_Save_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行植球校驗位置 參數儲存。", tmpListStr);
                _msPosData.M_ModelCH_X = Convert.ToDouble(txt_ModelCH_X.Text);
                _msPosData.M_ModelCH_Y = Convert.ToDouble(txt_ModelCH_Y.Text);
                _msPosData.M_ModelCH_Z = Convert.ToDouble(txt_ModelCH_Z.Text);


                _msPosData.Save_Data();
                Setting_UI();
            }
        }

        private void btn_Save_MS_NozzleOffset_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行植球校驗位置 參數儲存。", tmpListStr);
                _msPosData.M_NozzleBase_X = Convert.ToDouble(txt_MS_NozzleBase_X.Text);
                _msPosData.M_NozzleBase_Y = Convert.ToDouble(txt_MS_NozzleBase_Y.Text);
                _msPosData.M_NozzleBase_Z = Convert.ToDouble(txt_MS_NozzleBase_Z.Text);

                _msPosData.Save_Data();
                Setting_UI();
            }
        }

        bool DoorPass = false;
        /// <summary>
        /// 安全門pass
        /// </summary>      
        private void CB_DoorPass_CheckedChanged(object sender, EventArgs e)
        {
            DoorPass = ((CheckBox)sender).Checked;
        }

        private void PL_SelectST_Table_Paint(object sender, PaintEventArgs e)
        {

        }

        private void axCVX1_OnRemoteDesktopUpdated_1(object sender, AxCVXLib._DCVXEvents_OnRemoteDesktopUpdatedEvent e)
        {

        }

        private void btn_SB_No_Set_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行[點位設定]?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                SB_SelectIndex = (int)num_SB_No_Set.Value - 1;
                DGV_SB_Path.Rows[SB_SelectIndex].Selected = true;
                DGV_SB_Path.CurrentCell = DGV_SB_Path.Rows[SB_SelectIndex].Cells[0];
                DGV_SB_Path.FirstDisplayedScrollingRowIndex = SB_SelectIndex;
            }
        }

        private void btnLaserAreaFlow_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行雷射平面校正流程?  ", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (FlowAllow())
                {
                    if (!_CU30CL.MotionFlag && _CU30CL.HomeFlag)
                    {
                        _EQP_Status = enumEQP_Status.MANU;
                        _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step1;
                        Task.Run(LaserAreaFlow);
                        LogMsgAdd(MList_Log, lb_HistoryList, "執行雷射平面校正流程。", tmpListStr);
                    }
                    else if (_CU30CL.MotionFlag)
                    {
                        MessageBox.Show("馬達移動中!", "Warning");
                    }
                    else if (!_CU30CL.HomeFlag)
                    {
                        MessageBox.Show("馬達尚未復歸!", "Warning");
                    }
                }
            }
        }

        private void btnLaserVerticalFlow_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行雷射垂校正流程?  ", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (FlowAllow())
                {
                    if (!_CU30CL.MotionFlag && _CU30CL.HomeFlag)
                    {
                        _EQP_Status = enumEQP_Status.MANU;
                        _LaserAlignFlowStep = enumLaser_AlignFlowStep.LaserAlign_Step1;
                        Task.Run(LaserVerticalFlow);
                        LogMsgAdd(MList_Log, lb_HistoryList, "執行雷射垂校校正流程。", tmpListStr);
                    }
                    else if (_CU30CL.MotionFlag)
                    {
                        MessageBox.Show("馬達移動中!", "Warning");
                    }
                    else if (!_CU30CL.HomeFlag)
                    {
                        MessageBox.Show("馬達尚未復歸!", "Warning");
                    }
                }
            }
        }

        private void btn_HmeasureForce_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行強制測高計算?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (FlowAllow())
                {
                    LogMsgAdd(MList_Log, lb_HistoryList, "強制測高計算完成", tmpListStr);
                    _sbMHeight_OK = SB_All_HeightZ_Cal();
                }
            }
        }


        private void cb_MemUse_CheckedChanged(object sender, EventArgs e)
        {
            _ConfigSystem.MemUse = cb_MemUse.Checked;

            if (cb_MemUse.Checked)
                MEM_TypeVisibleOn();
            else
                MEM_TypeVisibleOff();
        }

        private void btn_SaveProduceParam_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否執行?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LogMsgAdd(MList_Log, lb_HistoryList, "執行生產參數儲存。", tmpListStr);
                _msPosData.SB_AirJudgeNG_Delay = Convert.ToInt32(txt_SB_AirJudgeNG_Delay.Text);
                _msPosData.NoSB_AirJudgeNG_Delay = Convert.ToInt32(txt_NoSB_AirJudge_NG_Delay.Text);
                _msPosData.NozzleAir_Log = cb_NozzleAir_Log.Checked;
                _msPosData.VacuumThreshold = Convert.ToDouble(txt_VacuumThreshold.Text);

                _msPosData.Save_Data();
                Setting_UI();
            }
        }

        private void btn_Motion_MotorStatus_Q_Click(object sender, EventArgs e)
        {


        }

        private void btn_TempTable_Save_Click(object sender, EventArgs e)
        {
            TableTempeSave(_paramPath + "\\System\\TableTemp.ini");
        }

        private void btn_TempTable_Load_Click(object sender, EventArgs e)
        {
            TempTableUpdate();
        }

        private void chk_Motion_Enable_Q_CheckedChanged(object sender, EventArgs e)
        {
            _returnCode = Motion.mAcm_AxSetSvOn(m_Axishand[0], 1);
            if (_returnCode != (uint)ErrorCode.SUCCESS)
            {
                strTemp = "Servo On Failed With Error Code: [0x" + Convert.ToString(_returnCode, 16) + "]";
                ShowMessages(strTemp, _returnCode);
                return;
            }
        }

        private void btn_RingLight_Set_Click(object sender, EventArgs e)
        {
            AlignLight(_RingLight, byte.Parse(txt_RingLight.Text));
        }

        private void btn_PizeoMove_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("是否執行PizeoMotor PositionMove 移動流程?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (!_CU30CL.MotionFlag && _CU30CL.HomeFlag)
                    {
                        double p1 = double.Parse(txt_PizeoMoveX.Text);
                        double p2 = double.Parse(txt_PizeoMoveY.Text);
                        double p3 = double.Parse(txt_PizeoMoveZ.Text);

                        double[] abs = new double[] { p2, p1, p3 };
                        _CU30CL.CloseLoopServoMove(200, abs);
                    }
                    else if (_CU30CL.MotionFlag)
                    {
                        MessageBox.Show("馬達移動中!", "Warning");
                    }
                    else if (!_CU30CL.HomeFlag)
                    {
                        MessageBox.Show("馬達尚未復歸!", "Warning");
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }



        /// <summary>
        /// 主流程
        /// </summary>
        private async void MainFlow()
        {
            try
            {
                while (!_mainStopFlag)
                {
                    switch (_EQP_Status)
                    {
                        case enumEQP_Status.AUTO:

                            if (_autoSB_FlowStep != enumAUTO_FlowStep.AutoWait)
                            {
                                await SB_AutoFlow();
                            }
                            else if (_autoST_FlowStep != enumAUTO_FlowStep.AutoWait)
                            {
                                await ST_AutoFlow();
                            }

                            break;

                        case enumEQP_Status.MANU:
                            Manual_Status();
                            break;

                        case enumEQP_Status.IDLE:
                            IDLE_Status();
                            break;

                        case enumEQP_Status.DOWN:
                            DOWN_Status();
                            break;
                    }

                    //Console.WriteLine($"run: {DateTime.Now.Millisecond}");
                    Thread.Sleep(1);

                }
            }
            catch (Exception ex)
            {
                Error_Log.Add(ex.ToString());
            }
        }

        /// <summary>
        /// XYZ  絕對座標移動
        /// </summary>
        /// <param name="posX">座標</param>
        /// <param name="posY">座標</param>
        /// <param name="posZ">座標</param>
        /// <param name="moveX">是否移動</param>
        /// <param name="moveY">是否移動</param>
        /// <param name="moveZ">是否移動</param>
        /// <returns></returns>
        private bool StageABS_Move(double posX, double posY, double posZ, bool moveX, bool moveY, bool moveZ)
        {
            bool result = false;


            try
            {
                if (_Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 0, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 1, 2000) &&
                            _Aerotech_Controller.Commands.Motion.WaitForMotionDone(Aerotech.A3200.Commands.WaitOption.InPosition, 2, 2000))
                {
                    if (Motion_X && Motion_Y && Motion_Z)
                    {
                        if ((PositionCheck(Now_X, posX) || !moveX) && (PositionCheck(Now_Y, posY) || !moveY)
                                && (PositionCheck(Now_Z, posZ) || !moveZ))
                        {
                            result = true;
                        }
                        else
                        {
                            if (SafePositionSetting(posX, posY, posZ))
                            {
                                if (moveX)
                                {
                                    if (_msPosData.M_NegativeLimit_X <= posX && posX <= _msPosData.M_PositiveLimit_X)
                                        this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(0, posX, 200);
                                    else
                                        return false;
                                }

                                if (moveY)
                                {
                                    if (_msPosData.M_NegativeLimit_Y <= posY && posY <= _msPosData.M_PositiveLimit_Y)
                                        this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(1, posY, 200);
                                    else
                                        return false;
                                }

                                if (moveZ)
                                {
                                    if (_msPosData.M_NegativeLimit_Z <= posZ && posZ <= _msPosData.M_PositiveLimit_Z)
                                        this._Aerotech_Controller.Commands[this.taskIndex].Motion.MoveAbs(2, posZ, 50);
                                    else
                                        return false;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return result;
        }

        /// <summary>
        /// MEM ST 自動流程
        /// </summary>
        /// <returns></returns>
        private async Task ST_AutoFlow()
        {

            switch (_autoST_FlowStep)
            {
                case enumAUTO_FlowStep.AutoWait:
                    //idle
                    break;

                case enumAUTO_FlowStep.Loading:
                    Invoke(new Action(DisableActionPage));
                    if (!IO_InputData.DoorOpen)
                    {
                        IO_OutputControl("門鎖開");
                        if (StageABS_Move(_msPosData.M_Load_X, _msPosData.M_Load_Y, _msPosData.M_Load_Z, false, false, true))
                        {
                            tact_time.Restart();
                            NowNozzle_MAX_Pressure = 0;
                            NowNozzle_MIN_Pressure = NowNozzlePressure;
                            _autoST_FlowStep = enumAUTO_FlowStep.Loading_2;
                        }

                    }
                    break;

                case enumAUTO_FlowStep.Loading_2:
                    if (!IO_InputData.DoorOpen)
                    {
                        if (StageABS_Move(_msPosData.M_Load_X, _msPosData.M_Load_Y, _msPosData.M_Load_Z, true, true, true))
                        {
                            ushort value = (ushort)(_recipeData.SYSParam.NozzleSB_ValveNum * 10);
                            if (UpdateProportionalValve(value))
                            {
                                tact_time.Reset();
                                _autoST_FlowStep = enumAUTO_FlowStep.ST_Grab;
                            }
                            else
                            {
                                if (tact_time.ElapsedMilliseconds > 10000)
                                {
                                    _EQP_Status = enumEQP_Status.DOWN;
                                    LogMsgAdd(Error_Log, lb_ErrorList, "比例閥更新超時", tmpErrStr);
                                    Invoke(new dele_msgShow(ErrMSG_Show), "比例閥更新超時");
                                    tact_time.Reset();
                                };
                            }
                        }


                    }
                    break;

                case enumAUTO_FlowStep.ST_Grab:
                    if (!IO_InputData.DoorOpen)
                    {
                        if (NowST_NO > 0 && NowST_NO <= _recipeData.Panel.ST_Num)
                        {
                            _ST_Align_FlowStep = enumAlignFlowStep.GrabStep1;
                            int result = await ST_Align_Flow(NowST_NO);

                            if (result == 0)
                                _autoST_FlowStep = enumAUTO_FlowStep.ST_ThickMeasure;
                            else
                            {
                                _autoST_FlowStep = enumAUTO_FlowStep.AutoWait;
                                _EQP_Status = enumEQP_Status.DOWN;
                                Invoke(new dele_msgShow(ErrMSG_Show), "取像異常");
                                LogMsgAdd(Error_Log, lb_ErrorList, "取像異常", tmpErrStr);
                            }
                        }
                    }
                    break;

                case enumAUTO_FlowStep.ST_ThickMeasure:
                    if (!IO_InputData.DoorOpen)
                    {
                        if (NowST_NO > 0 && NowST_NO <= _recipeData.Panel.ST_Num)
                        {
                            _ST_H_Measure_FlowStep = enumH_MeasureFlowStep.H_MeasureStep1;
                            NowNozzle_MAX_Pressure = 0;
                            NowNozzle_MIN_Pressure = NowNozzlePressure;
                            int result = await ST_ThickM_Flow(NowST_NO);

                            if (result == 0)
                                _autoST_FlowStep = enumAUTO_FlowStep.ST_SB_Mounting;
                            else
                            {
                                _autoST_FlowStep = enumAUTO_FlowStep.AutoWait;
                                _EQP_Status = enumEQP_Status.DOWN;
                                Invoke(new dele_msgShow(ErrMSG_Show), "測高異常");
                                LogMsgAdd(Error_Log, lb_ErrorList, "測高異常", tmpErrStr);
                            }
                        }
                    }
                    break;

                case enumAUTO_FlowStep.ST_SB_Mounting:
                    if (!IO_InputData.DoorOpen)
                    {
                        if (NowST_NO > 0 && NowST_NO <= _recipeData.Panel.ST_Num)
                        {
                            _STballMount_FlowStep = enumBallMountFlowStep.MountStep1;
                            if (PublicData.ST_Data[NowST_NO - 1].Finish = await ST_BallMountFlow(PublicData.ST_Data[NowST_NO - 1].Start_PadNo, PublicData.ST_Data[NowST_NO - 1].Start_BallNo))
                            {
                                _autoST_FlowStep = enumAUTO_FlowStep.ST_WorkJudge;
                            }
                            else
                            {
                                _autoST_FlowStep = enumAUTO_FlowStep.AutoWait;
                                _EQP_Status = enumEQP_Status.DOWN;
                                Invoke(new dele_msgShow(ErrMSG_Show), "植球異常");
                                LogMsgAdd(Error_Log, lb_ErrorList, "植球異常", tmpErrStr);
                            }

                        }
                    }
                    break;

                case enumAUTO_FlowStep.ST_WorkJudge:
                    if (ALL_ST_Finish)
                    {
                        for (int i = 0; i < _recipeData.Panel.ST_Num; i++)
                            PublicData.ST_Data[i].Init();
                        _autoST_FlowStep = enumAUTO_FlowStep.Unloading;
                    }
                    else
                    {
                        ST_FlowDataInit();
                        _autoST_FlowStep = enumAUTO_FlowStep.ST_Grab;
                    }


                    break;

                case enumAUTO_FlowStep.Unloading:
                    if (!IO_InputData.DoorOpen)
                    {

                        if (StageABS_Move(_msPosData.M_Unload_X, _msPosData.M_Unload_Y, _msPosData.M_Unload_Z, false, false, true))
                        {
                            _autoST_FlowStep = enumAUTO_FlowStep.Unloading_2;

                        }
                    }
                    break;

                case enumAUTO_FlowStep.Unloading_2:
                    if (!IO_InputData.DoorOpen)
                    {
                        if (StageABS_Move(_msPosData.M_Unload_X, _msPosData.M_Unload_Y, _msPosData.M_Unload_Z, true, true, true))
                        {
                            _autoST_FlowStep = enumAUTO_FlowStep.AutoFinish;

                        }
                    }
                    break;

                case enumAUTO_FlowStep.AutoFinish:
                    FlowFinish();
                    _EQP_Status = enumEQP_Status.IDLE;
                    IO_OutputControl("門鎖關");
                    MList_Log.Add($"ST自動流程完成");
                    LogMsgAdd(MList_Log, lb_HistoryList, "ST自動流程完成", tmpListStr);




                    break;
                    break;
            }
        }

        private void Manual_Status()
        {
            Invoke(new Action(DisableActionPage));
        }

        private void IDLE_Status()
        {
            Invoke(new Action(EnableActionPage));
        }

        private void DOWN_Status()
        {
            _mainStopFlag = true;
            Invoke(new Action(EnableActionPage));
        }

        private async Task<bool> TestFlow()
        {
            bool result = false;
            this.Invoke((MethodInvoker)delegate () { txt_NozzleAlign_X.Text = "99"; });

            return result;
        }

        #endregion

        /// <summary>
        /// 相對位置移動限制 (1 mm) (X Y Z)
        /// </summary>
        private void RelDistCheck(TextBox tmpText)
        {
            double tmp;
            tmp = double.Parse(tmpText.Text);
            if (tmp > 1)
            {
                tmpText.Text = "1";
            }
            else if (tmp < -1)
            {
                tmpText.Text = "-1";
            }
        }

        /// <summary>
        /// 絕對位置移動限制 (正負極限) (X:1 Y:2 Z:3)
        /// </summary>
        private bool AbsDistCheck(TextBox tmpText, int axisNo)
        {
            bool result = false;

            double tmp;
            tmp = double.Parse(tmpText.Text);

            switch (axisNo)
            {
                case 1:
                    if (tmp >= _msPosData.M_NegativeLimit_X && tmp <= _msPosData.M_PositiveLimit_X)
                        result = true;
                    break;

                case 2:
                    if (tmp >= _msPosData.M_NegativeLimit_Y && tmp <= _msPosData.M_PositiveLimit_Y)
                        result = true;
                    break;

                case 3:
                    if (tmp >= _msPosData.M_NegativeLimit_Z && tmp <= _msPosData.M_PositiveLimit_Z)
                        result = true;
                    break;
            }

            return result;
        }

        /// <summary>
        /// 關閉動作頁
        /// </summary>
        public void DisableActionPage()
        {
            ComponmentEnable(group_PositionSpeed, false);
            ComponmentEnable(group_Movement, false);
            ComponmentEnable(group_MoveTo, false);


        }

        public void EnableActionPage()
        {
            ComponmentEnable(group_PositionSpeed, true);
            ComponmentEnable(group_Movement, true);
            ComponmentEnable(group_MoveTo, true);

        }

        private void ComponmentEnable(Control _control, bool _enable)
        {
            if (_control.InvokeRequired)
            {
                _control.Invoke(new Action(() => { _control.Enabled = _enable; }));

            }
            else
            {
                _control.Enabled = _enable;
            }
        }


        #region log save
        /// <summary>
        /// log str temp
        /// </summary>
        private static string tmpErrStr = "", tmpListStr = "";
        /// <summary>
        /// 資料列數
        /// </summary>
        const int LISTMSGNUM = 150;
        private delegate void d_ListLogMsgAdd(ListBox lb, string txt);
        /// <summary>
        /// listbox log寫入
        /// </summary>
        /// <param name="lb"></param>
        /// <param name="txt"></param>
        private void ListLogMsgAdd(ListBox lb, string txt)
        {
            if (lb.InvokeRequired)
            {
                var func = new d_ListLogMsgAdd(ListLogMsgAdd);
                this.Invoke(func, lb, txt);
            }
            else
            {
                if (lb.Items.Count >= LISTMSGNUM)
                {
                    lb.Items.RemoveAt(LISTMSGNUM - 1);
                }
                lb.Items.Insert(0, txt);

            }
        }

        /// <summary>
        /// log listbox 寫入
        /// </summary>
        /// <param name="log"></param>
        /// <param name="lb"></param>
        /// <param name="txt"></param>
        private void LogMsgAdd(TNewLog log, ListBox lb, string txt, string tmpStr)
        {
            try
            {
                if (tmpStr != txt)
                {
                    tmpStr = txt;
                    log.Add(txt);
                    txt = DateTime.Now.ToString("[MMdd-HH:mm:ss_ff]") + txt;
                    ListLogMsgAdd(lb, txt);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion

        /// <summary>
        /// ST panel產生
        /// </summary>
        /// <param name="stNo"></param>
        private void ST_TableGenerate(int stCol, int stRow)
        {
            PL_ST_Table.Controls.Clear();
            PL_SelectST_Table.Controls.Clear();
            int stNum = stCol * stRow;
            ST_Panel = new ST_POS_PanelGenerate[stNum];
            ST_SelectPanel = new ST_Select_PanelGenerate[stNum];

            int panel_X = 168; //168
            int panel_Y = 135;

            int selectPanel_X = 190;
            int selectPanel_Y = 95;

            for (int i = 0; i < stNum; i++)
            {
                //center pos
                ST_Panel[i] = new ST_POS_PanelGenerate(panel_X, panel_Y, $"ST_", i + 1);
                PL_ST_Table.Controls.Add(ST_Panel[i].ControlBody);
                ST_Panel[i].Location(new System.Drawing.Point(5 + (panel_X + 10) * (i % stCol), 5 + (panel_Y + 10) * (i / stCol)));

                // select
                ST_SelectPanel[i] = new ST_Select_PanelGenerate(selectPanel_X, selectPanel_Y, $"ST_", i + 1);
                PL_SelectST_Table.Controls.Add(ST_SelectPanel[i].ControlBody);
                ST_SelectPanel[i].Location(new System.Drawing.Point(5 + (selectPanel_X + 10) * (i % stCol), 5 + (selectPanel_Y + 10) * (i / stCol)));
            }

            PublicData.ST_Data = new Define.st_ST_Data[stNum];

        }

        /// <summary>
        /// ST 座標頁面
        /// </summary>
        public class ST_POS_PanelGenerate
        {
            Panel _panel = new Panel();
            Label _label_1 = new Label();
            Label _label_2 = new Label();
            Label _title = new Label();
            public TextBox txt_PosX = new TextBox();
            public TextBox txt_PosY = new TextBox();

            private int _widthX;

            public int WidthX
            {
                get { return _widthX; }
            }

            private int _heightY;

            public int HeightY
            {
                get { return _heightY; }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="x">width</param>
            /// <param name="y">height</param>
            public ST_POS_PanelGenerate(int x, int y, string title, int index)
            {
                _widthX = x;
                _heightY = y;
                _panel.Width = _widthX;
                _panel.Height = _heightY;
                _panel.BackColor = Color.LightGreen;
                _title.Text = title + index.ToString();
                _label_1.Text = "X                                      mm";
                _label_2.Text = "Y                                      mm";
                _label_1.AutoSize = true;
                _label_2.AutoSize = true;
                txt_PosX.Text = "0"; txt_PosX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
                txt_PosY.Text = "0"; txt_PosY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
                txt_PosX.Name = "txt_Pos_X";
                txt_PosY.Name = "txt_Pos_Y";
                _panel.Controls.Add(_title); _panel.Controls.Add(_label_1); _panel.Controls.Add(_label_2); _panel.Controls.Add(txt_PosX); _panel.Controls.Add(txt_PosY);
                _panel.BringToFront();
                _title.BringToFront(); _label_1.BringToFront(); _label_2.BringToFront(); txt_PosX.BringToFront(); txt_PosY.BringToFront();
                _title.Location = new System.Drawing.Point(48, 21);
                _label_1.Location = new System.Drawing.Point(13, 55);
                _label_2.Location = new System.Drawing.Point(13, 88);
                txt_PosX.Location = new System.Drawing.Point(36, 50);
                txt_PosY.Location = new System.Drawing.Point(36, 85);
            }

            public void Location(System.Drawing.Point p)
            {
                _panel.Location = p;
            }

            public Control ControlBody
            {
                get
                {
                    return _panel;
                }

            }
        }

        /// <summary>
        /// ST 生產選擇頁面
        /// </summary>
        public class ST_Select_PanelGenerate
        {
            Panel _panel = new Panel();
            Label _label_1 = new Label();
            Label _label_2 = new Label();
            Button _btn = new Button();
            public NumericUpDown _numPad = new NumericUpDown();
            public NumericUpDown _numBall = new NumericUpDown();


            private int _widthX;

            public int WidthX
            {
                get { return _widthX; }
            }

            private int _heightY;

            public int HeightY
            {
                get { return _heightY; }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="x">width</param>
            /// <param name="y">height</param>
            public ST_Select_PanelGenerate(int x, int y, string title, int index)
            {
                _widthX = x;
                _heightY = y;
                _panel.Width = _widthX;
                _panel.Height = _heightY;
                _panel.BackColor = Color.LightGreen;
                _label_1.Text = "Pad No:";
                _label_2.Text = "Ball No:";
                _label_1.AutoSize = true;
                _label_2.AutoSize = true;
                _numPad.Text = "0"; _numPad.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
                _numBall.Text = "0"; _numBall.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
                _numPad.Name = "Num_Pad";
                _numPad.Width = 50;
                _numPad.Minimum = 1;
                _numBall.Name = "Num_Ball";
                _numBall.Width = 50;
                _numBall.Minimum = 1;
                _btn.Name = "btnSelect";
                _btn.Text = title + index.ToString();
                _btn.Size = new Size(52, 37);
                _btn.BackColor = SystemColors.Control;
                _panel.Controls.Add(_btn); _panel.Controls.Add(_label_1); _panel.Controls.Add(_label_2); _panel.Controls.Add(_numPad); _panel.Controls.Add(_numBall);
                _panel.BringToFront();
                _btn.BringToFront(); _label_1.BringToFront(); _label_2.BringToFront(); _numPad.BringToFront(); _numBall.BringToFront();

                _label_1.Location = new System.Drawing.Point(12, 25);
                _label_2.Location = new System.Drawing.Point(12, 63);
                _numPad.Location = new System.Drawing.Point(73, 20);
                _numBall.Location = new System.Drawing.Point(73, 56);
                _btn.Location = new System.Drawing.Point(130, 31);
                _btn.Tag = index;
                _btn.Click += _btn_Click;
            }

            /// <summary>
            /// select panel btn event
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void _btn_Click(object sender, EventArgs e)
            {
                int i = (int)((Button)sender).Tag;
                if (PublicData.ST_Data[i - 1].Used)
                {
                    if (MessageBox.Show($"是否取消 [ST {i}]?", "警告", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        PublicData.ST_Data[i - 1].Used = false;
                        ((Button)sender).BackColor = SystemColors.Control;
                    }
                }
                else
                {
                    if (MessageBox.Show($"是否使用 [ST {i}]?", "警告", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        PublicData.ST_Data[i - 1].Used = true;
                        ((Button)sender).BackColor = Color.Lime;
                        PublicData.ST_Data[i - 1].Start_PadNo = (int)(_numPad.Value);
                        PublicData.ST_Data[i - 1].Start_BallNo = (int)(_numBall.Value);

                        Form1.MList_Log.Add($"設定ST-{i} Pad_No[{PublicData.ST_Data[i - 1].Start_PadNo}] Ball_No[{PublicData.ST_Data[i - 1].Start_BallNo}]");
                    }
                }
            }

            public void Location(System.Drawing.Point p)
            {
                _panel.Location = p;
            }

            public Control ControlBody
            {
                get
                {
                    return _panel;
                }

            }
        }




    }
}