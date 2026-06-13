using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConfocalLaser_Interface;
using Aerotech.A3200.Status;
using System.Diagnostics;
using NCScriptParser_Interface;
using static NCScriptParser_Interface.NCScriptParser;
using System.Text.RegularExpressions;
using System.Timers;
using Module_Interface;
using CameraCVX_Interface;

namespace LaserProcess_LCH
{

    public partial class MainForm
    {/// <summary>
     /// 用來判斷每個微孔加工完成時間是否過短(振鏡當機，可能會一直回報加工完成，會發生 生產進度完成但是卻沒加工到的情況;會伴隨者振鏡公板軟體當機的問題)
     /// </summary>
        private DateTime _lastAutoProcessLogTime = DateTime.MinValue;

        /// <summary>
        /// 自動加工流程的總步驟數量，用來初始化流程旗標、定時器與 UI 控制組件
        /// </summary>
        private static int _stepNumber = 7;

        /// <summary>
        /// 指示當前是否正處於某步驟的進行中狀態（背景執行緒中判斷用）
        /// </summary>
        private bool _stepProgressON = false;

        /// <summary>
        /// 各個步驟是否已完成的旗標陣列，配合 UI 狀態更新與流程控制用
        /// </summary>
        private bool[] _stepFlags = new bool[_stepNumber];

        /// <summary>
        /// 每個流程步驟對應的 ManualResetEvent，提供背景執行緒等待訊號或完成通知
        /// </summary>
        private ManualResetEvent[] _stepTimers = new ManualResetEvent[_stepNumber];

        /// <summary>
        /// 對應每個步驟的 UI GroupBox 控制組件，用於依流程順序啟用或禁用按鈕群組
        /// </summary>
        private GroupBox[] _groupSteps;

        /// <summary>
        /// 儲存加工原點座標位置（X, Y, Z），供流程啟動階段初始化與加工定位參考使用
        /// </summary>
        private double[] _orgPoint = new double[3];

        /// <summary>
        /// 記錄整段自動加工流程的執行時間，提供 UI 顯示與日誌記錄用
        /// </summary>
        private Stopwatch _stopwatch_ElapsedTime = new Stopwatch();


        /// <summary>
        /// 左側的 X 軸偏移量，通常用於校正左邊參考點的誤差
        /// </summary>
        public double _offset_X_Left;

        /// <summary>
        /// 右側的 X 軸偏移量，通常用於校正右邊參考點的誤差
        /// </summary>
        public double _offset_X_Right;

        /// <summary>
        /// 上側的 Y 軸偏移量，常用於校正上緣參考點對位誤差
        /// </summary>
        public double _offset_Y_Up;

        /// <summary>
        /// 下側的 Y 軸偏移量，常用於校正下緣參考點對位誤差
        /// </summary>
        public double _offset_Y_Down;

        /// <summary>
        /// 中心點的 X 軸偏移量，可用於計算物件整體偏移或鏡像對位
        /// </summary>
        public double _offset_Center_X;

        /// <summary>
        /// 中心點的 Y 軸偏移量，可用於計算物件整體偏移或鏡像對位
        /// </summary>
        public double _offset_Center_Y;

        /// <summary>
        /// 加工中的 X 軸偏移量，加工定位時
        /// </summary>
        private double _process_Offset_X;

        /// <summary>
        /// 加工中的 Y 軸偏移量，加工定位時
        /// </summary>
        private double _process_Offset_Y;
        /// <summary>
        /// 上報用層別
        /// </summary>
        string UD_or_LD;
         
        /// <summary>
        /// Auto-mode 自動流程的步驟枚舉型別。
        /// 每個 Step 代表流程中的一個明確操作階段，依序執行並搭配 UI 狀態更新與背景執行緒進度判斷。
        /// 通常搭配 StepFlag 陣列、ManualResetEvent 與 GroupBox 控制 UI 互動順序。
        /// </summary>
        private enum Step
        {
            /// <summary>
            /// Auto-mode 步驟流程編號（Step1）
            /// ↳ 對應載入 NC 加工檔案、解析參數
            /// </summary>
            s1,

            /// <summary>
            /// Auto-mode 步驟流程編號（Step2）
            /// ↳ 對應吸附樣品、啟用真空 Chuck
            /// </summary>
            s2,

            /// <summary>
            /// Auto-mode 步驟流程編號（Step3）
            /// ↳ 對應設定加工原點（OrgPoint）座標
            /// </summary>
            s3,

            /// <summary>
            /// Auto-mode 步驟流程編號（Step4）
            /// ↳ 對應鎖門（電子門鎖啟動）以確保安全
            /// </summary>
            s4,

            /// <summary>
            /// Auto-mode 步驟流程編號（Step5）
            /// ↳ 對應啟用測高流程（使用共焦雷射）
            /// </summary>
            s5,

            /// <summary>
            /// Auto-mode 步驟流程編號（Step6）
            /// ↳ 對應進入正式加工流程（包含微孔與指令排程）
            /// </summary>
            s6,

            /// <summary>
            /// Auto-mode 步驟流程編號（Step7）
            /// ↳ 表示加工流程完成
            /// </summary>
            s7,

        }

        /// <summary>
        /// Auto-mode 中每個流程步驟的執行狀態描述枚舉，用於控制 UI 外觀與進度邏輯。
        /// 常搭配 Panel 元件變色或步驟旗標陣列（_stepFlags）使用。
        /// </summary>
        private enum StepState
        {
            /// <summary>
            /// 尚未啟用該步驟，表示流程尚未走到此階段，或尚未完成前置條件
            /// </summary>
            inactive,

            /// <summary>
            /// 該步驟已成功執行完畢，且符合作業條件
            /// </summary>
            OK,

            /// <summary>
            /// 該步驟執行失敗或發生異常（例如設備錯誤或條件不符）
            /// </summary>
            NO
        }


        private double[] _ThickResult;
        /// <summary>
        /// 儲存每個量測點的 Z 軸補償值，來源為共焦雷射位移計。
        /// </summary>
        /// <remarks>
        /// 此陣列承載整個厚度流程的 Z 補償核心資料，具備下列功能：
        /// <list type="bullet">
        ///   <item><description>作為 <see cref="ThickProcess"/> 與 <see cref="AutoThickProcess"/> 厚度計算基礎。</description></item>
        ///   <item><description>用於加工前與加工中高度差異比對與誤差判斷。</description></item>
        ///   <item><description>提供 Z 軸位移補償參考依據，確保加工高度精度。</description></item>
        /// </list>
        /// 資料的索引位置需配合不同流程階段（加工前：<c>i</c>；加工後：<c>zoneType</c>）以避免覆蓋。
        /// </remarks>
        /// <example>
        /// 假設第 3 點的補償值為 4.528，可計算該點加工前厚度如下：
        /// <code>
        /// ThickProcess[3] = Z_feedback - _CompensatePosition[3] + LaserHeight;
        /// </code>
        /// </example>

        private double[] _CompensatePosition;
        /// <summary>
        /// 預期要執行的厚度測高加工筆數總計
        /// </summary>
        private int _NbrOfThickProcess_Total = 0;
        /// <summary>
        /// 已完成的厚度測高加工筆數
        /// </summary>
        private int _NbrOfThickProcess_Done = 0;
        /// <summary>
        /// 自動流程總加工筆數
        /// </summary>
        private int _NbrOfProcess_Total = 0;
        /// <summary>
        /// 已完成的自動流程總加工筆數
        /// </summary>
        private int _NbrOfProcess_Done = 0;
        /// <summary>
        /// 微孔特徵總數
        /// </summary>
        private int _NbrOfFeature_Total = 0;
        /// <summary>
        /// 已完成的微孔特徵總數
        /// </summary>
        private int _NbrOfFeature_Done = 0;
        private int progressPercentage = 0;
        private int _nbrOfThickProcess_Total = 0;
        /// <summary>
        /// 設定的功率(上拋用)
        /// </summary>
        double SET_W;
        /// <summary>
        /// 實際功率(上拋用)
        /// </summary>
        double W;

        /// <summary>
        /// 加工微孔總數
        /// </summary>
        int Microhole_Total;

        /// <summary>
        /// 已加工之微孔數
        /// </summary>
        int Microhole_Done;

        /// <summary>
        /// 未加工之微孔數
        /// </summary>
        int Microhole_Remain;

        /// <summary>
        /// 平均微孔加工時間(切削率)
        /// </summary>
        string Microhole_Time;
        /// <summary>
        /// 儲存每一微孔的加工時間(要算平均)
        /// </summary>
        List<double> Microhole_time_average = new List<double>();//不用限定範圍可一直儲存不同筆資料，(像似動態陣列)，資料筆數會一直向上加上去
        /// <summary>
        /// 儲存微孔加工開始時間
        /// </summary>
        DateTime MicroholeStartTime;   // 微孔加工開始時間
        /// <summary>
        /// 儲存微孔加工結束時間
        /// </summary>
        DateTime MicroholeEndTime;   // 微孔加工結束時間

        /// <summary>
        /// 初始化 Auto-mode 執行環境，包括流程旗標、同步事件、原點座標顯示、
        /// 群組控制元件 UI 以及 OrgPoint 設定表單。此方法為自動流程開始前的必要步驟。
        /// </summary>
        private void AutoProcess_Initialize()
        {
            // Initialize the parameters
            for (int i = 0; i < _stepNumber; i++)
            {
                _stepFlags[i] = false;
                _stepTimers[i] = new ManualResetEvent(false);
            }
            // Initialize Origin Point
            updateControlTxtWithString(lbl_AutoMode_OrgPoint, string.Format("OrgPoint:{2} ({0:0.###}, {1:0.###})", _ConfigEquip._OrgPoint.X, _ConfigEquip._OrgPoint.Y, Environment.NewLine));
            _orgPoint[(int)MotionAxis.X] = _ConfigEquip._OrgPoint.X;
            _orgPoint[(int)MotionAxis.Y] = _ConfigEquip._OrgPoint.Y;
            _orgPoint[(int)MotionAxis.Z] = _ConfigEquip._Position_Camera.Z;

            // Initialize the group steps for Enable group control
            _groupSteps = new GroupBox[] { group_AutoMode_Step3, group_AutoMode_Step4, group_AutoMode_Step2, group_AutoMode_Step5, group_AutoMode_Step6, group_AutoMode_Step7 };

            foreach (var groupBox in _groupSteps)
            {
                enableGroupControl(groupBox, false);
            }

            // Initialize the Form of SetOrgPoint
            _formSetOrgPoint = new FormSetOrgPoint(this);
            //_formSetOrgPoint.confirmChecked += _formSetOrgPoint_confirmChecked;
            _formSetOrgPoint.confirmCheck += _formSetOrgPoint_confirmCheck;

            // Write Event Log
            writeEvent("MainForm.AutoProcess", "AutoProcess initialized!");
        }

        #region GUI Event Method

        private void chk_RD_NCfile_CheckedChanged(object sender, EventArgs e)
        {
            if (chk_RD_NCfile.Checked)
            {
                MessageBox.Show("選擇刀具資料夾路徑", "Warning！！");

                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.Description = "請選擇檔案路徑";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _ConfigSystem._RDJobxFolderPath = dialog.SelectedPath;
                    //Write Event
                    writeEvent("UserEvent", "選擇刀具資料夾路徑： " + dialog.SelectedPath);
                    //[BM:自訂Log紀錄內容-手動研發]
                    if (_AutoProcessLog_flag)
                        writeEventLog_Automatic_processing("加工流程微孔會被略過", $"研發手動NC檔已被勾選");
                }
                else
                {
                    chk_RD_NCfile.Checked = false;
                    MessageBox.Show("請重新勾選，並選擇刀具資料夾路徑", "Warning！！");
                    //[BM:自訂Log紀錄內容-手動研發]
                    if (_AutoProcessLog_flag)
                        writeEventLog_Automatic_processing("加工流程微孔會執行", $"研發手動NC檔沒有被勾選");
                }
            }
        }

        private void btn_AutoMode_SelectFile_Click(object sender, EventArgs e)
        {           
         


            //[BM:指定加工起始項目-禁用pl內之按鈕]
            pl_operation_setting2.Enabled = false;
            //if (txt_AutoMode_LotMumber.Text == "" )
            //{
            //    MessageBox.Show("請確認加工案件是否有工單LotNumber", "Warning！！");
            //    //return;
            //}

            //Write Event
            writeEvent("UserEvent", "Select Process File.");

            chk_Distance.Checked = false;
            OpenFileDialog_NC.InitialDirectory = _NCParser._InitialDirectory;
            OpenFileDialog_NC.DefaultExt = "mlproj";
            OpenFileDialog_NC.Filter = "project files (*.mlproj)|*.mlproj";
            if (OpenFileDialog_NC.ShowDialog() == DialogResult.OK)
            {
                try
                {

                    string fileName = OpenFileDialog_NC.FileName;
                    int index1 = fileName.LastIndexOf('\\');
                    int index2 = fileName.LastIndexOf('.');

                    //string[] fileDir = fileName.Split('\\');
                    //string[] fileDirExclude = new string[fileDir.Length - 1];
                    //Array.Copy(fileDir, fileDirExclude, fileDir.Length - 1);

                    char[] projectDir = new char[index1];
                    // Get project directory
                    fileName.CopyTo(0, projectDir, 0, projectDir.Length);
                    _NCParser._ProjectDirectory = new string(projectDir);
                    _NCParser._InitialDirectory = new string(projectDir);
                    //[BM:刀具檔讀取通解]//=================================================
                    if (_isSysSimulated)
                    {
                        _ConfigSystem._JobxFolderPath = "C:\\Users\\250319\\Desktop\\job\\LCH_job";//10f電腦自看測試用
                    }

                    //=====20260206修改的LD後面只會接一個數字，UD後面只會接 數字-數字
                    string fileName_UDorLD = fileName.Substring(0, index1);

                    // LD 後面只抓一位數字
                    string patternLD = @"LD(\d)";

                    // UD 後面抓一位數字，-後面也只抓一位數字
                    string patternUD = @"UD(\d)(?:-(\d))?";

                    Match matchLD = Regex.Match(fileName_UDorLD, patternLD);
                    Match matchUD = Regex.Match(fileName_UDorLD, patternUD);

                    List<string> extractedKeywords = new List<string>();

                    if (matchLD.Success)
                    {
                        // 只取 LD 後面的那個一位數字
                        extractedKeywords.Add("LD" + matchLD.Groups[1].Value);
                        UD_or_LD = $"LD{matchLD.Groups[1].Value}";
                    }
                    if (matchUD.Success)
                    {
                        // 取 UD 後面的數字
                        string udValue = "UD" + matchUD.Groups[1].Value;
                        // 如果有 -數字，就加上，但只取一位
                        if (matchUD.Groups[2].Success)
                        {
                            udValue += "-" + matchUD.Groups[2].Value;
                        }
                        extractedKeywords.Add(udValue);

                        UD_or_LD = $"{udValue}";
                    }
                    //=====


                    //string fileName_UDorLD = fileName.Substring(0, index1); // 取得資料夾名稱
                    //string patternLD = @"LD\d+(-\d+)?"; // LD + 數字，檢查 "-" 後是否為數字
                    //string patternUD = @"UD\d+(-\d+)?"; // UD + 數字，檢查 "-" 後是否為數字

                    //Match matchLD = Regex.Match(fileName_UDorLD, patternLD);
                    //Match matchUD = Regex.Match(fileName_UDorLD, patternUD);

                    //List<string> extractedKeywords = new List<string>();

                    //if (matchLD.Success)
                    //{
                    //    extractedKeywords.Add(matchLD.Value); // 加入匹配的 LD 結果
                    //}
                    //if (matchUD.Success)
                    //{
                    //    extractedKeywords.Add(matchUD.Value); // 加入匹配的 UD 結果
                    //}


                        if (extractedKeywords.Count == 0)
                        {
                            MessageBox.Show("確認Case資料夾名稱內具有單一層之名稱");
                            writeError("NC檔轉檔失敗", "轉檔名稱:" + _NCParser._NCName);
                            return;
                        }
                        else if (extractedKeywords.Count > 1)
                        {
                            MessageBox.Show("確認Case資料夾名稱內只有單一層之名稱");
                            writeError("NC檔轉檔失敗", "轉檔名稱:" + _NCParser._NCName);
                            return;
                        }
                    
                    else
                    {
                        if (_isSysSimulated)
                        {
                            _ConfigSystem._JobxFolderPath = Path.Combine(_ConfigSystem._JobxFolderPath, extractedKeywords[0]);
                            _ConfigSystem.WriteIntoIniFile();
                        }
                        else
                        {
                            _ConfigSystem._JobxFolderPath = Path.Combine(_ConfigSystem._JobxFolderPath1, extractedKeywords[0]);
                            _ConfigSystem.WriteIntoIniFile();
                        }
                        #region 檢查暖機檔是否存在
                        string source = Path.Combine(C_Path, "Warmup_File"); // 來源資料夾(比對刀具資料夾中是否有Warmup_File資料夾中的刀具)
                        string destination = _ConfigSystem._JobxFolderPath;                        // 目標資料夾
                        bool missingFiles = false;                                                 // 用於追蹤是否有缺少的檔案
                        List<string> missingFileList = new List<string>();                         // 儲存缺少的檔案清單
                        int w = 300, h = 100;                                                      //提醒視窗尺寸長寬
                        if (!Directory.Exists(destination))
                        {
                            System.Windows.Forms.Form customMessageBox = new System.Windows.Forms.Form();
                            customMessageBox.Text = "資料夾缺失!!";
                            customMessageBox.Size = new Size(w, h);
                            customMessageBox.FormBorderStyle = FormBorderStyle.FixedDialog;  // 固定視窗大小
                            customMessageBox.TopMost = true;                                 // 確保視窗顯示在最前方
                            customMessageBox.Icon = SystemIcons.Warning;                     // 使用內建警告圖示
                            customMessageBox.StartPosition = FormStartPosition.CenterScreen; // 讓視窗顯示在螢幕中央
                            customMessageBox.MinimumSize = new Size(w, w);                   // 強制視窗不會小於這個大小
                            RichTextBox richTextBox = new RichTextBox();
                            richTextBox.Dock = DockStyle.Fill;
                            richTextBox.ReadOnly = true;
                            richTextBox.BackColor = Color.White;
                            richTextBox.Font = new Font("微軟正黑體", 12);
                            richTextBox.ForeColor = Color.FromArgb(85, 85, 85);
                            richTextBox.Text = $"LCH_job 資料夾內不存在名為：\n{extractedKeywords[0]}\n之資料夾請手動建立。";
                            
                            richTextBox.SelectAll();
                            richTextBox.SelectionAlignment = HorizontalAlignment.Center;     // 文字置中 (適用於 `RichTextBox` 需要使用 RTF 設定)
                           
                            int keywordPosition = richTextBox.Text.IndexOf(extractedKeywords[0]);
                            if (keywordPosition >= 0) 
                            {
                                richTextBox.Select(keywordPosition, extractedKeywords[0].Length);
                                richTextBox.SelectionColor = Color.Red;                      // 確保 extractedKeywords[0] 顯示為紅色
                                richTextBox.SelectionLength = 0;
                            }
                            customMessageBox.Controls.Add(richTextBox);
                            customMessageBox.ShowDialog();
                            return;
                        }

                        // 檢查來源資料夾內的所有檔案是否存在於目標資料夾
                        foreach (string file1 in Directory.GetFiles(source))
                        {
                            string destFile = Path.Combine(destination, Path.GetFileName(file1)); // 取得目標完整路徑

                            if (!File.Exists(destFile)) // 如果目標資料夾內沒有該檔案
                            {
                                missingFiles = true;
                                missingFileList.Add(Path.GetFileName(file1)); // 只存檔案名稱
                            }
                        }

                        if (missingFiles)
                        {
                            string missingFilesText = string.Join("\r", missingFileList); // 將缺少的檔案合併成一個字串
                            System.Windows.Forms.Form customMessageBox = new System.Windows.Forms.Form();
                            customMessageBox.Text = "檔案缺失提醒!!";
                            customMessageBox.Size = new Size(w + 600, h + 600);
                            customMessageBox.FormBorderStyle = FormBorderStyle.FixedDialog; // 固定視窗大小
                            customMessageBox.TopMost = true; // 確保視窗顯示在最前方
                            customMessageBox.Icon = SystemIcons.Warning; // 使用內建警告圖示
                            customMessageBox.StartPosition = FormStartPosition.CenterScreen;  //讓視窗顯示在螢幕中央
                            customMessageBox.MinimumSize = new Size(w, w); // 強制視窗不會小於這個大小
                            RichTextBox richTextBox = new RichTextBox();
                            richTextBox.Dock = DockStyle.Fill;
                            richTextBox.ReadOnly = true;
                            richTextBox.BackColor = Color.White;
                            richTextBox.Font = new Font("微軟正黑體", 12);
                            richTextBox.ForeColor = Color.FromArgb(85, 85, 85);
                            richTextBox.Text = $"{extractedKeywords[0]} 資料夾內缺少以下暖機檔案：\n{missingFilesText}\n請手動建立。"; // 文字置中 (適用於 `RichTextBox` 需要使用 RTF 設定)
                            richTextBox.SelectAll();
                            richTextBox.SelectionAlignment = HorizontalAlignment.Center;
                            richTextBox.DeselectAll(); // 取消所有選取狀態
                            customMessageBox.Controls.Add(richTextBox);
                            customMessageBox.ShowDialog();
                            return;
                        }
                        #endregion
                    }
                    //=================================================
                    char[] file = new char[fileName.Length - index1 - 1 - OpenFileDialog_NC.DefaultExt.Length - 1];
                    // Get NC file name
                    fileName.CopyTo(index1 + 1, file, 0, file.Length);
                    _NCParser._NCName = new string(file);

                    // Display NC file name
                    txt_AutoMode_FileName.Text = _NCParser._NCName;

                    // Reset step status
                    _stepFlags[(int)Step.s1] = false;
                    updatePnlStepState(pnl_AutoMode_LoadFile_Status, StepState.inactive);
                    updatePnlStepState(pnl_AutoMode_MoveZ_Status, StepState.inactive);
                    updatePnlStepState(pnl_AutoMode_OrgPoint_Status, StepState.inactive);
                    //[BM:不放開吸附功能，狀態顯示不會放開(所有程式只有UI介面的取消吸附才可以解除吸附)]
                    // updatePnlStepState(pnl_AutoMode_Chuck_Status, StepState.inactive);

                    //解析NC檔為 研發手動NC檔模式 chk_RD_NCfile.Checked
                    _NCParser.RD_NCfile = chk_RD_NCfile.Checked;



                    // Load file and parse parameters
                    _NCParser.ParseData();

                    bool Checked =CheckScanheadFiles(_ConfigSystem._JobxFolderPath, _NCParser._ToolCheckList);//檢查刀具是否齊全

                    if (!Checked)
                    {
                        //[BM:防呆檢查刀具-解除急停後清除載檔顯示框]
                        txt_AutoMode_FileName.Clear(); //清除載入檔案顯示的txtbox
                        return;
                    }

                    if (_isSysSimulated)//Path.Combine(Environment.CurrentDirectory, "xuan","Comparing Automation and Cagila"); 
                    {
                        NCScriptParser data = _NCParser;
                        string NCParser_Cagila_CsvPath = Path.Combine(Environment.CurrentDirectory, "xuan", "Comparing Automation and Cagila", "NCParser" + ".csv");   // 組合對應的解析結果CSV儲存路徑
                        using (StreamWriter stream = new StreamWriter(NCParser_Cagila_CsvPath, false))                         // 建立輸出串流並覆寫模式開啟檔案
                        {
                            stream.WriteLine("PropertyName,Value"); // ⇦ 表頭列：屬性名稱與對應值

                            var props = data.GetType().GetProperties(); // ⇦ 取得 data 物件的所有屬性（public）

                            foreach (var prop in props)
                            {
                                object value = prop.GetValue(data); // ⇦ 取出屬性值

                                if (value == null)
                                {
                                    stream.WriteLine($"{prop.Name},null"); // ⇦ 若為 null 則記錄屬性名稱與 null 字串
                                }
                            }

                            var fields = data.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public); // ⇦ 若你還要抓取欄位（不是屬性）

                            stream.WriteLine("FieldName,Value"); // ⇦ 如果你只抓欄位（Fields）

                            foreach (var field in fields)
                            {
                                object value = field.GetValue(data); // ⇦ 取出欄位值

                                if (value == null)
                                {
                                    stream.WriteLine($"{field.Name},null"); // ⇦ 若欄位為 null 則記錄
                                }
                            }
                        }
                        return;
                    }


                    // Write InitialDirectory back to the .ini file
                    _NCParser.WriteIntoIniFile();

                    // Do Data Parsing
                    _formNCParser = new FormNCParser(_NCParser);

                    // Start Step Check Timer with checkAutoModeProgress
                    autoMode_StartStepWithTimer(Step.s1);

                    // No need to check
                    _stepTimers[(int)Step.s1].Set();

                    // Update GUI Control
                    btn_AutoMode_ViewFile.Enabled = true;
                    updatePnlStepState(pnl_AutoMode_LoadFile_Status, StepState.OK);

                    // Enable Load button
                    //btn_AutoMode_LoadFile.Enabled = true;

                    //MES LOG批號記錄
                    _CHPT_MES.Batch_Number = txt_AutoMode_LotMumber.Text;
                    _CHPT_MES.Serial_Number = txt_AutoMode_LotMumber.Text;
                    _CHPT_MES.Product_Name = _NCParser._NCName;
                    _CHPT_MES.Laser_Process_File = fileName;
                    _CHPT_MES.Laser_Pos_File = fileName;


                    //Write Event
                    writeEvent("UserEvent", "加工轉檔路徑： " + OpenFileDialog_NC.FileName);

                    // Write Process
                    writeProcess("Step1", "檔案已選擇載入");
                }
                catch (Exception ex)
                {
                   // MessageBox.Show("確認轉檔為研發手動檔或是加工自動檔", "Error！！");
                    MessageBox.Show($"確認轉檔為研發手動檔或是加工自動檔，NC檔轉檔失敗：{ex.Message}", "Error！！");
                    writeError("NC檔轉檔失敗", "轉檔名稱： " + _NCParser._NCName);
                    return;
                }
            }
            else
            {
                //[BM:指定加工起始項目-一般流程若取消則啟用pl內按鈕]
                pl_operation_setting2.Enabled = true;
            }
        }
        /// <summary>
        /// 條碼載檔進程式
        /// </summary>
        private void Barcode_Load_File()
        {
            try
            {
                string fileName = Processing_Path;


                string targetPath = fileName; // 要檢查的路徑

                // 取得所有 .mlproj 檔案
                string[] mlprojFiles = Directory.GetFiles(targetPath, "*.mlproj", SearchOption.TopDirectoryOnly);

                if (mlprojFiles.Length == 1)
                {
                    Console.WriteLine($"找到唯一的 .mlproj 檔案: {Path.GetFileName(mlprojFiles[0])}");
                    fileName = mlprojFiles[0];
                }
                else if (mlprojFiles.Length > 1)
                {
                    DateTime currentTime = DateTime.Now;
                    using (StreamWriter sw = new StreamWriter(Path.Combine(Application.StartupPath, "MES_FilePathLog", currentTime.ToString("yyyyMMdd") + "_MES_FilePath_EventLog.txt"), true))// 寫入 LOG，檔案路徑在 
                    {
                        sw.WriteLine($"時間:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}，資料夾路徑:{targetPath}內。\n同時存在多個 .mlproj檔");
                        sw.WriteLine("----------------------------------------");
                    }
                    this.Close();
                    MessageBox.Show($"資料夾路徑:{targetPath}內。\n同時存在多個 .mlproj檔");
                    txt_AutoMode_LotMumber.Clear();
                    return;
                }
                else
                {
                    DateTime currentTime = DateTime.Now;
                    using (StreamWriter sw = new StreamWriter(Path.Combine(Application.StartupPath, "MES_FilePathLog", currentTime.ToString("yyyyMMdd") + "_MES_FilePath_EventLog.txt"), true))// 寫入 LOG，檔案路徑在 
                    {
                        sw.WriteLine($"時間:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}，資料夾路徑:{targetPath}內。\n中沒有找到任何 .mlproj 檔案。");
                        sw.WriteLine("----------------------------------------");
                    }
                    this.Close();
                    MessageBox.Show($"資料夾路徑:{targetPath}內。\n中沒有找到任何 .mlproj 檔案。");
                    txt_AutoMode_LotMumber.Clear();
                    return;
                }





                int index1 = fileName.LastIndexOf('\\');
                int index2 = fileName.LastIndexOf('.');


                //string[] fileDir = fileName.Split('\\');
                //string[] fileDirExclude = new string[fileDir.Length - 1];
                //Array.Copy(fileDir, fileDirExclude, fileDir.Length - 1);

                char[] projectDir = new char[index1];
                // Get project directory
                fileName.CopyTo(0, projectDir, 0, projectDir.Length);
                _NCParser._ProjectDirectory = new string(projectDir);
                _NCParser._InitialDirectory = new string(projectDir);
                //[BM:刀具檔讀取通解]//=================================================
                if (_isSysSimulated)
                {
                    _ConfigSystem._JobxFolderPath = "C:\\Users\\250319\\Desktop\\job\\LCH_job";//10f電腦自看測試用
                }

                //=====20260206修改的LD後面只會接一個數字，UD後面只會接 數字-數字














                string fileName_UDorLD = Path.GetFileNameWithoutExtension(fileName);
             //   string fileName_UDorLD = fileName.Substring(index1+1, index1);






                // LD 後面只抓一位數字
                string patternLD = @"LD(\d)";

                // UD 後面抓一位數字，-後面也只抓一位數字
                string patternUD = @"UD(\d)(?:-(\d))?";

                Match matchLD = Regex.Match(fileName_UDorLD, patternLD);
                Match matchUD = Regex.Match(fileName_UDorLD, patternUD);

                List<string> extractedKeywords = new List<string>();

                if (matchLD.Success)
                {
                    // 只取 LD 後面的那個一位數字
                    extractedKeywords.Add("LD" + matchLD.Groups[1].Value);
                }
                if (matchUD.Success)
                {
                    // 取 UD 後面的數字
                    string udValue = "UD" + matchUD.Groups[1].Value;
                    // 如果有 -數字，就加上，但只取一位
                    if (matchUD.Groups[2].Success)
                    {
                        udValue += "-" + matchUD.Groups[2].Value;
                    }
                    extractedKeywords.Add(udValue);
                }

                //=====



                if (extractedKeywords.Count == 0)
                {
                    MessageBox.Show("確認Case資料夾名稱內具有單一層之名稱");
                    writeError("NC檔轉檔失敗", "轉檔名稱:" + _NCParser._NCName);
                    return;
                }
                else if (extractedKeywords.Count > 1)
                {
                    MessageBox.Show("確認Case資料夾名稱內只有單一層之名稱");
                    writeError("NC檔轉檔失敗", "轉檔名稱:" + _NCParser._NCName);
                    return;
                }

                else
                {
                    if (_isSysSimulated)
                    {
                        _ConfigSystem._JobxFolderPath = Path.Combine(_ConfigSystem._JobxFolderPath, extractedKeywords[0]);
                        _ConfigSystem.WriteIntoIniFile();
                    }
                    else
                    {
                        _ConfigSystem._JobxFolderPath = Path.Combine(_ConfigSystem._JobxFolderPath1, extractedKeywords[0]);
                        _ConfigSystem.WriteIntoIniFile();
                    }
                    #region 檢查暖機檔是否存在
                    string source = Path.Combine(C_Path, "Warmup_File"); // 來源資料夾
                    string destination = _ConfigSystem._JobxFolderPath;                        // 目標資料夾
                    bool missingFiles = false;                                                 // 用於追蹤是否有缺少的檔案
                    List<string> missingFileList = new List<string>();                         // 儲存缺少的檔案清單
                    int w = 300, h = 100;                                                      //提醒視窗尺寸長寬
                    if (!Directory.Exists(destination))
                    {
                        System.Windows.Forms.Form customMessageBox = new System.Windows.Forms.Form();
                        customMessageBox.Text = "資料夾缺失!!";
                        customMessageBox.Size = new Size(w, h);
                        customMessageBox.FormBorderStyle = FormBorderStyle.FixedDialog;  // 固定視窗大小
                        customMessageBox.TopMost = true;                                 // 確保視窗顯示在最前方
                        customMessageBox.Icon = SystemIcons.Warning;                     // 使用內建警告圖示
                        customMessageBox.StartPosition = FormStartPosition.CenterScreen; // 讓視窗顯示在螢幕中央
                        customMessageBox.MinimumSize = new Size(w, w);                   // 強制視窗不會小於這個大小
                        RichTextBox richTextBox = new RichTextBox();
                        richTextBox.Dock = DockStyle.Fill;
                        richTextBox.ReadOnly = true;
                        richTextBox.BackColor = Color.White;
                        richTextBox.Font = new Font("微軟正黑體", 12);
                        richTextBox.ForeColor = Color.FromArgb(85, 85, 85);
                        richTextBox.Text = $"LCH_job 資料夾內不存在名為：\n{extractedKeywords[0]}\n之資料夾請手動建立。";

                        richTextBox.SelectAll();
                        richTextBox.SelectionAlignment = HorizontalAlignment.Center;     // 文字置中 (適用於 `RichTextBox` 需要使用 RTF 設定)

                        int keywordPosition = richTextBox.Text.IndexOf(extractedKeywords[0]);
                        if (keywordPosition >= 0)
                        {
                            richTextBox.Select(keywordPosition, extractedKeywords[0].Length);
                            richTextBox.SelectionColor = Color.Red;                      // 確保 extractedKeywords[0] 顯示為紅色
                            richTextBox.SelectionLength = 0;
                        }
                        customMessageBox.Controls.Add(richTextBox);
                        customMessageBox.ShowDialog();
                        return;
                    }

                    // 檢查來源資料夾內的所有檔案是否存在於目標資料夾
                    foreach (string file1 in Directory.GetFiles(source))
                    {
                        string destFile = Path.Combine(destination, Path.GetFileName(file1)); // 取得目標完整路徑

                        if (!File.Exists(destFile)) // 如果目標資料夾內沒有該檔案
                        {
                            missingFiles = true;
                            missingFileList.Add(Path.GetFileName(file1)); // 只存檔案名稱
                        }
                    }

                    if (missingFiles)
                    {
                        string missingFilesText = string.Join("\r", missingFileList); // 將缺少的檔案合併成一個字串
                        System.Windows.Forms.Form customMessageBox = new System.Windows.Forms.Form();
                        customMessageBox.Text = "檔案缺失提醒!!";
                        customMessageBox.Size = new Size(w + 600, h + 600);
                        customMessageBox.FormBorderStyle = FormBorderStyle.FixedDialog; // 固定視窗大小
                        customMessageBox.TopMost = true; // 確保視窗顯示在最前方
                        customMessageBox.Icon = SystemIcons.Warning; // 使用內建警告圖示
                        customMessageBox.StartPosition = FormStartPosition.CenterScreen;  //讓視窗顯示在螢幕中央
                        customMessageBox.MinimumSize = new Size(w, w); // 強制視窗不會小於這個大小
                        RichTextBox richTextBox = new RichTextBox();
                        richTextBox.Dock = DockStyle.Fill;
                        richTextBox.ReadOnly = true;
                        richTextBox.BackColor = Color.White;
                        richTextBox.Font = new Font("微軟正黑體", 12);
                        richTextBox.ForeColor = Color.FromArgb(85, 85, 85);
                        richTextBox.Text = $"{extractedKeywords[0]} 資料夾內缺少以下暖機檔案：\n{missingFilesText}\n請手動建立。"; // 文字置中 (適用於 `RichTextBox` 需要使用 RTF 設定)
                        richTextBox.SelectAll();
                        richTextBox.SelectionAlignment = HorizontalAlignment.Center;
                        richTextBox.DeselectAll(); // 取消所有選取狀態
                        customMessageBox.Controls.Add(richTextBox);
                        customMessageBox.ShowDialog();
                        return;
                    }
                    #endregion
                }
                //=================================================

                char[] file = new char[fileName.Length - index1 - 1 - Path.GetExtension(Processing_Path).Length];
                // Get NC file name
                fileName.CopyTo(index1 + 1, file, 0, file.Length);

                _NCParser._NCName = new string(file);
                _NCParser._NCName = Path.GetFileNameWithoutExtension(_NCParser._NCName );//移除副檔名部分

                // Display NC file name
                txt_AutoMode_FileName.Text = _NCParser._NCName;

                // Reset step status
                _stepFlags[(int)Step.s1] = false;
                updatePnlStepState(pnl_AutoMode_LoadFile_Status, StepState.inactive);
                updatePnlStepState(pnl_AutoMode_MoveZ_Status, StepState.inactive);
                updatePnlStepState(pnl_AutoMode_OrgPoint_Status, StepState.inactive);
                //[BM:不放開吸附功能，狀態顯示不會放開(所有程式只有UI介面的取消吸附才可以解除吸附)]
               // updatePnlStepState(pnl_AutoMode_Chuck_Status, StepState.inactive);

                //解析NC檔為 研發手動NC檔模式 chk_RD_NCfile.Checked
                _NCParser.RD_NCfile = chk_RD_NCfile.Checked;



                // Load file and parse parameters
                _NCParser.ParseData();

                bool Checked = CheckScanheadFiles(_ConfigSystem._JobxFolderPath, _NCParser._ToolCheckList);//檢查刀具是否齊全

                if (!Checked)
                {
                    //[BM:防呆檢查刀具-解除急停後清除載檔顯示框]
                    txt_AutoMode_FileName.Clear(); //清除載入檔案顯示的txtbox
                    return;
                }

                if (_isSysSimulated)//Path.Combine(Environment.CurrentDirectory, "xuan","Comparing Automation and Cagila"); 
                {
                    NCScriptParser data = _NCParser;
                    string NCParser_Cagila_CsvPath = Path.Combine(Environment.CurrentDirectory, "xuan", "Comparing Automation and Cagila", "NCParser" + ".csv");   // 組合對應的解析結果CSV儲存路徑
                    using (StreamWriter stream = new StreamWriter(NCParser_Cagila_CsvPath, false))                         // 建立輸出串流並覆寫模式開啟檔案
                    {
                        stream.WriteLine("PropertyName,Value"); // ⇦ 表頭列：屬性名稱與對應值

                        var props = data.GetType().GetProperties(); // ⇦ 取得 data 物件的所有屬性（public）

                        foreach (var prop in props)
                        {
                            object value = prop.GetValue(data); // ⇦ 取出屬性值

                            if (value == null)
                            {
                                stream.WriteLine($"{prop.Name},null"); // ⇦ 若為 null 則記錄屬性名稱與 null 字串
                            }
                        }

                        var fields = data.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public); // ⇦ 若你還要抓取欄位（不是屬性）

                        stream.WriteLine("FieldName,Value"); // ⇦ 如果你只抓欄位（Fields）

                        foreach (var field in fields)
                        {
                            object value = field.GetValue(data); // ⇦ 取出欄位值

                            if (value == null)
                            {
                                stream.WriteLine($"{field.Name},null"); // ⇦ 若欄位為 null 則記錄
                            }
                        }
                    }
                    return;
                }


                // Write InitialDirectory back to the .ini file
                _NCParser.WriteIntoIniFile();

                // Do Data Parsing
                _formNCParser = new FormNCParser(_NCParser);

                // Start Step Check Timer with checkAutoModeProgress
                autoMode_StartStepWithTimer(Step.s1);

                // No need to check
                _stepTimers[(int)Step.s1].Set();

                // Update GUI Control
                btn_AutoMode_ViewFile.Enabled = true;
                updatePnlStepState(pnl_AutoMode_LoadFile_Status, StepState.OK);

                // Enable Load button
                //btn_AutoMode_LoadFile.Enabled = true;

                //MES LOG批號記錄
                _CHPT_MES.Batch_Number = txt_AutoMode_LotMumber.Text;
                _CHPT_MES.Serial_Number = txt_AutoMode_LotMumber.Text;
                _CHPT_MES.Product_Name = _NCParser._NCName;
                _CHPT_MES.Laser_Process_File = fileName;
                _CHPT_MES.Laser_Pos_File = fileName;


                //Write Event
                writeEvent("UserEvent", "加工轉檔路徑： " + OpenFileDialog_NC.FileName);

                // Write Process
                writeProcess("Step1", "檔案已選擇載入");
            }
            catch (Exception ex)
            {
                // MessageBox.Show("確認轉檔為研發手動檔或是加工自動檔", "Error！！");
                MessageBox.Show($"確認轉檔為研發手動檔或是加工自動檔，NC檔轉檔失敗：{ex.Message}", "Error！！");
                writeError("NC檔轉檔失敗", "轉檔名稱： " + _NCParser._NCName);
                return;
            }

        }
        //[BM:防呆檢查刀具方法]
        public bool CheckScanheadFiles(string folderPath, List<string> toolCheckList)
        {
            List<string> missingFileList = new List<string>(); // 儲存缺失檔案名稱

            // 使用 HashSet 去除重複檔案名稱（忽略大小寫）
            HashSet<string> uniqueFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string fileName in toolCheckList)
            {
                string jobxFileName = fileName.EndsWith(".jobx", StringComparison.OrdinalIgnoreCase)
                    ? fileName
                    : fileName + ".jobx"; // 確保副檔名為 .jobx

                if (!uniqueFileNames.Contains(jobxFileName)) // 若尚未處理過
                {
                    uniqueFileNames.Add(jobxFileName); // 加入已處理清單

                    string fullPath = Path.Combine(folderPath, jobxFileName); // 組合完整路徑

                    if (!File.Exists(fullPath)) // 若檔案不存在
                    {
                        missingFileList.Add(jobxFileName); // 加入缺失清單
                    }
                }
            }

            if (missingFileList.Count > 0) // 若有缺失
            {
                string message = "以下檔案在資料夾中找不到：\n" + string.Join("\n", missingFileList);
                MessageBox.Show(message, "檔案缺失警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //[BM:自訂Log紀錄內容-防呆檢查刀具]
                if (_AutoProcessLog_flag)
                    writeEventLog_Automatic_processing("刀具防呆檢查", $"缺少刀具檔案:{message}");
                setEmergencyStopLock();//觸發急停，要案解除。
                return false;
            }
            else
            {                

                MessageBox.Show("所有指定檔案皆存在", "檢查完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //[BM:自訂Log紀錄內容-防呆檢查刀具]
                if (_AutoProcessLog_flag)
                    writeEventLog_Automatic_processing("刀具防呆檢查", $"所有刀具檔案皆存在");
                return true;

            }
        }


        private void btn_AutoMode_LoadFile_Click(object sender, EventArgs e)
        {
            // Check file can be opened or not
            // If not, prompt a dialog to warn User

            //Write Event
            writeEvent("UserEvent", "Load Process File.");

            // Reset step status
            _stepFlags[(int)Step.s1] = false;
            updatePnlStepState(pnl_AutoMode_LoadFile_Status, StepState.inactive);

            // Load file and parse parameters
            _NCParser.ParseData();

            // Write InitialDirectory back to the .ini file
            _NCParser.WriteIntoIniFile();

            // Do Data Parsing
            _formNCParser = new FormNCParser(_NCParser);

            // Start Step Check Timer with checkAutoModeProgress
            autoMode_StartStepWithTimer(Step.s1);

            // No need to check
            _stepTimers[(int)Step.s1].Set();

            // Update GUI Control
            btn_AutoMode_ViewFile.Enabled = true;
            updatePnlStepState(pnl_AutoMode_LoadFile_Status, StepState.OK);

            //Write Event
            writeEvent("UserEvent", "Load Process File Success.");

            // Write Process
            writeProcess("Step1", "檔案已載入");
        }

        private void btn_AutoMode_ViewFile_Click(object sender, EventArgs e)
        {
            //Write Event
            writeEvent("UserEvent", "View Process File.");

            if (_formNCParser.IsDisposed)
                _formNCParser = new FormNCParser(_NCParser);
            _formNCParser.Show();
        }

        private void btn_AutoMode_MoveZ_Move_Click(object sender, EventArgs e)
        {
            if (_isSysSimulated)
                return;
            btn_Move_Position_Camera_Click("AutoProcess", null);
        }

        private void btn_AutoMode_MoveZ_Start_Click(object sender, EventArgs e)
        {
            //Skip if isSimulated
            if (_isSysSimulated)
                return;

            // Disable GUI Controls
            //EnableGUIMotionControls(false);

            // Disable UI control
            enableUIGroup(false);

            // Call BackgroundWorker to do asynchronous operation
            if (!BW_AutoFocus.IsBusy)
            {
                // Start the asynchronous operation.
                BW_AutoFocus.RunWorkerAsync(AutoFocusType.AutoProcess);
            }

            // Update panel
            updatePnlStepState(pnl_AutoMode_MoveZ_Status, StepState.inactive);

            /*
            // Read Current ConfocalLaser result
            _isConfocalLaserBusy = true;
            ConfocalLaser.evalResult result = _ConfocalLaser.Trigger(1, out _ConfocalLaser_Output, 0);
            _isConfocalLaserBusy = false;
            double currentDistance = 0;
            if (_ConfocalLaser_Output < -99999)
            {
                // Write Event
                writeEvent("UserEvent", "Out of Compensate zone.");
                MessageBox.Show("請先移動Z軸至測高計量測範圍!");
                return;
            }
            else
            {
                currentDistance = -1 * _ConfocalLaser_Output * 0.001; //Since confocal laser result unit: um
            }

            // Update axisIndex depends on Control.Name of the sender
            Button button = (Button)sender;
            int axisIndex = (int)MotionAxis.Z;
            double distance = currentDistance - _ConfigEquip._Height_Equip.ConfocalLaser;
            double speed = _ConfigEquip._Velocity_Default.Z;
            string msg = "Move Z to ConfocalLaser Focal Plane.";

            //Write Event
            writeEvent("UserEvent", msg);

            //Skip if isSimulated
            if (_isSysSimulated)
                return;

            // Update Status
            updatePnlStepState(pnl_AutoMode_MoveZ_Status, StepState.inactive);

            try
            {
                // MotionAxis.Z direction is reversed
                distance = -1 * distance;

                moveRel(axisIndex, distance, speed);

                // Update Status
                updatePnlStepState(pnl_AutoMode_MoveZ_Status, StepState.OK);
            }
            catch (Exception ex)
            {
                // Update Status
                updatePnlStepState(pnl_AutoMode_MoveZ_Status, StepState.NO);
                writeError("MainForm.MotionControl", msg + " failed!" + ex);
                updateControlTxtWithString(txt_Motion_ErrorMsg, ex.ToString());
                return;
            }
            */

            // Check already done or not
            //if (_stepFlags[(int)Step.s2])
            //    return;

            //// Start Step Check Timer with checkAutoModeProgress
            //autoMode_StartStepWithTimer(Step.s2);

            //updatePnlStepState(pnl_AutoMode_OrgPoint_Status, StepState.OK);

            //// No need to check
            //_stepTimers[(int)Step.s2].Set();

            //// Write Process
            //writeProcess("Step2", "已移動至測高焦平面");
        }

        private void btn_AutoMode_OrgPoint_Set_Click(object sender, EventArgs e)
        {
            if (_isSysSimulated)
                return;
            //if (chk_EngineerMode.Checked)
            //{
            //    // EngineerMode operation
            //
            //    _orgPoint[(int)MotionAxis.X] = _positionFeedback[(int)MotionAxis.X];
            //    _orgPoint[(int)MotionAxis.Y] = _positionFeedback[(int)MotionAxis.Y];
            //    _orgPoint[(int)MotionAxis.Z] = _positionFeedback[(int)MotionAxis.Z];
            //
            //    //Write Event
            //    string msg = string.Format("OrgPoint:{3}({0:0.##}, {1:0.##}, {2:0.##})", _orgPoint[(int)MotionAxis.X], _orgPoint[(int)MotionAxis.Y], _orgPoint[(int)MotionAxis.Z], Environment.NewLine);
            //    lbl_AutoMode_OrgPoint.Text = msg;
            //    writeEvent("UserEvent", "Set " + msg);
            //
            //    // Check already done or not
            //    if (_stepFlags[(int)Step.s3])
            //        return;
            //
            //    // Start Step Check Timer with checkAutoModeProgress
            //    autoMode_StartStepWithTimer(Step.s3);
            //
            //    updatePnlStepState(pnl_AutoMode_OrgPoint_Status, StepState.OK);
            //
            //    // No need to check
            //    _stepTimers[(int)Step.s3].Set();
            //
            //    // Write Process
            //    writeProcess("Step3", "加工原點已設定");
            //}
            //else
            {
                // OperationMode operation
                //Write Event
                writeEvent("UserEvent", "Set Origin Point.");

                _offset_X_Left = _ConfigEquip._PositionX.Left;
                _offset_X_Right = _ConfigEquip._PositionX.Right;
                _offset_Y_Down = _ConfigEquip._PositionY.Down;
                _offset_Y_Up = _ConfigEquip._PositionY.Up;
                _offset_Center_X = _ConfigEquip._OrgPoint.X;
                _offset_Center_Y = _ConfigEquip._OrgPoint.Y;

                if (_formSetOrgPoint.IsDisposed)
                {
                    _formSetOrgPoint = new FormSetOrgPoint(this);
                    //_formSetOrgPoint.confirmChecked += _formSetOrgPoint_confirmChecked;
                    _formSetOrgPoint.confirmCheck += _formSetOrgPoint_confirmCheck;
                }
                _formSetOrgPoint.Show();
            }
        }

        private void _formSetOrgPoint_confirmCheck(object sender, TupleEventArgs data)
        {
            // Parse data
            _ConfigEquip._PositionX.Left = data.Data.Item1;
            _ConfigEquip._PositionX.Right = data.Data.Item2;
            _ConfigEquip._PositionY.Up = data.Data.Item3;
            _ConfigEquip._PositionY.Down = data.Data.Item4;
            _ConfigEquip._OrgPoint.X = data.Data.Item5;
            _ConfigEquip._OrgPoint.Y = data.Data.Item6;
            
            _orgPoint[(int)MotionAxis.X] = _ConfigEquip._OrgPoint.X;
            _orgPoint[(int)MotionAxis.Y] = _ConfigEquip._OrgPoint.Y;
            _orgPoint[(int)MotionAxis.Z] = _positionFeedback[(int)MotionAxis.Z];

            // Update Origin Point
            updateControlTxtWithString(lbl_AutoMode_OrgPoint, string.Format("OrgPoint:{2} ({0:0.###}, {1:0.###})", _ConfigEquip._OrgPoint.X, _ConfigEquip._OrgPoint.Y, Environment.NewLine));

            // Write parameters back to the ini file
            _ConfigEquip.WriteIntoIniFile();

            // Check already done or not
            if (_stepFlags[(int)Step.s4])
                return;

            // Start Step Check Timer with checkAutoModeProgress
            autoMode_StartStepWithTimer(Step.s4);

            updatePnlStepState(pnl_AutoMode_OrgPoint_Status, StepState.OK);

            // No need to check
            _stepTimers[(int)Step.s4].Set();

            // Write Process
            writeProcess("Step4", "加工原點已設定");
        }

        private void _formSetOrgPoint_confirmChecked(object sender, EventArgs e)
        {
            // Update Origin Point
            updateControlTxtWithString(lbl_AutoMode_OrgPoint, string.Format("OrgPoint:{2} ({0:0.###}, {1:0.###})", _ConfigEquip._OrgPoint.X, _ConfigEquip._OrgPoint.Y, Environment.NewLine));

            // Write parameters back to the ini file
            _ConfigEquip.WriteIntoIniFile();

            // Check already done or not
            if (_stepFlags[(int)Step.s3])
                return;

            // Start Step Check Timer with checkAutoModeProgress
            autoMode_StartStepWithTimer(Step.s3);

            updatePnlStepState(pnl_AutoMode_OrgPoint_Status, StepState.OK);

            // No need to check
            _stepTimers[(int)Step.s3].Set();

            // Write Process
            writeProcess("Step3", "加工原點已設定");
        }

        private void btn_AutoMode_OrgPoint_MoveTo_Click(object sender, EventArgs e)
        {
            if (_isSysSimulated)
                return;
            // Replace the params with _iniDic
            _ConfigEquip.UpdatePublicParams();

            // Write Event
            writeEvent("UserEvent", "Move to Origin Point.");

            try
            {
                this.moveTo("OrgPoint", _ConfigEquip._OrgPoint.X, _ConfigEquip._OrgPoint.Y, _ConfigEquip._Position_Camera.Z, _ConfigEquip._Velocity_Default.Z);
                //this.moveTo("OrgPoint", _orgPoint[(int)MotionAxis.X], _orgPoint[(int)MotionAxis.Y], _orgPoint[(int)MotionAxis.Z], _ConfigEquip._Velocity_Default.Z);
            }
            catch (Exception ex)
            {
                // Update Status
                writeError("MainForm.AutoProcess", "MoveToOrgPoint failed!" + ex);
                updateControlTxtWithString(txt_Motion_ErrorMsg, ex.ToString());
            }

            // Write Process
            writeProcess("Step4", "移動至加工原點");
        }

        private void btn_AutoMode_Chuck_ON_Click(object sender, EventArgs e)
        {
            _CameraCVX.CamShutterSetting(1, CameraCVX.ShutterSpeed.S15);//正常模式下讓操作人員能夠看到影像而要設定快門速度


            //[BM:指定加工起始項目-設置完成吸附後執行動作]
            gb__operation_setting.Visible = false;
            pl_operation_setting2.Enabled = false;
            operation_closed.Visible = false;
            operation_setting.Visible = true;
            listBox_operation_setting1.Items.Clear();
            listBox_operation_setting2.Items.Clear();
            txt_AutoMode_operation_setting_LotMumber.Text = "";
            lb_operation_setting5.BackColor = Color.LightCoral;
            lb_operation_setting5.Text = "未設置完成";

            if (_isSysSimulated)
                return;
            // Enable Chuck vacuum
            enableChuckVacuum(true);

            UpdateDIPnlBackColor(pnl_AutoMode_Chuck_Status, true);

            // Check already done or not
            if (_stepFlags[(int)Step.s2])
                return;

            // Start Step Check Timer with checkAutoModeProgress
            autoMode_StartStepWithTimer(Step.s2);
            _stepTimers[(int)Step.s2].Set();
            // Write Process
            writeProcess("Step2", "樣品已吸附");
        }

        private void btn_AutoMode_Chuck_OFF_Click(object sender, EventArgs e)
        {
            if (_isSysSimulated)
                return;
            // Enable Chuck vacuum
            enableChuckVacuum(false);

            UpdateDIPnlBackColor(pnl_AutoMode_Chuck_Status, false);

            _stepFlags[(int)Step.s2] = false;

            // Write Process
            writeProcess("Step2", "樣品已解除吸附");

            //[BM:自訂Log紀錄內容-操作設定]
            if (_AutoProcessLog_flag)
                writeEventLog_Automatic_processing($"操作設定", $"人員解除吸附");
        }

        private void btn_AutoMode_LockDoor_ON_Click(object sender, EventArgs e)
        {
            if (_isSysSimulated)
                return;
            
            //Write Event
            writeEvent("UserEvent", "Enable Door Lock.");

            this.controlDO(_controlDoorLock, true);


            // Check already done or not
            if (_stepFlags[(int)Step.s5])
                return;

            // Start Step Check Timer with checkAutoModeProgress
            autoMode_StartStepWithTimer(Step.s5);

            // Move to FeedIn position
            //this.moveTo(_pointName4, _ConfigEquip._Position_FeedIn.X, _ConfigEquip._Position_FeedIn.Y, _ConfigEquip._Position_FeedIn.Z, _ConfigEquip._Velocity_Default.Z);

            // Write Process
            writeProcess("Step5", "電子門鎖已上鎖");
        }

        private void btn_AutoMode_LockDoor_OFF_Click(object sender, EventArgs e)
        {
            if (_isSysSimulated)
                return;

            //Write Event
            writeEvent("UserEvent", "Disable Door Lock.");
       
            this.controlDO(_controlDoorLock, false);

            UpdateDIPnlBackColor(pnl_AutoMode_LockDoor_Status, false);

            _stepFlags[(int)Step.s5] = false;

            // Move to FeedOut position
            //this.moveTo(_pointName5, _ConfigEquip._Position_FeedOut.X, _ConfigEquip._Position_FeedOut.Y, _ConfigEquip._Position_FeedOut.Z, _ConfigEquip._Velocity_Default.Z);

            // Write Process
            writeProcess("Step5", "電子門鎖已解除");
        }

        private void btn_AutoMode_Thick_Start_Click(object sender, EventArgs e)
        {
            if (_isSysSimulated)
                return;

            //Write Event
            writeEvent("UserEvent", "Start Thick measurement process.");
            chk_Distance.Checked = false;

            if (BW_ThickProcess.IsBusy != true)
            {
                // Start the asynchronous operation.
                BW_ThickProcess.RunWorkerAsync();
            }

            // Write Process
            writeProcess("Step6", "測高流程已開始");
        }

        private void btn_AutoMode_Thick_Stop_Click(object sender, EventArgs e)
        {
            if (_isSysSimulated)
                return;

            //Write Event
            writeEvent("UserEvent", "Abort Thick measurement process.");

            if (BW_ThickProcess.WorkerSupportsCancellation == true)
            {
                // Cancel the asynchronous operation.
                BW_ThickProcess.CancelAsync();
            }

            // Write Process
            writeProcess("Step6", "中斷測高流程");
        }

        private void btn_AutoMode_Thick_View_Click(object sender, EventArgs e)
        {
            //Write Event
            writeEvent("UserEvent", "View Process File.");

            double[] _compensatePosition = new double[_nbrOfThickProcess_Total]; ;
            int sum = 0;
            for (int i = 0; i < _CompensatePosition.Length; i++)
            {
                if (_CompensatePosition[i] != 0)
                {
                    _compensatePosition[sum] = _CompensatePosition[i];
                    sum++;
                }
            }

            //if (_formThickProcessView.IsDisposed)
            _formThickProcessView = new FormThickProcessView(Tuple.Create(_formNCParser.data._DDS_ZprobePositionX, _formNCParser.data._DDS_ZprobePositionY, _compensatePosition));
            _formThickProcessView.Show();
        }
        /// <summary>
        /// 是否為接續加工
        /// </summary>
        bool Continuous_Processing_flag = false;
        /// <summary>
        /// 接續加工
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_ContinuousProcessing_Click(object sender, EventArgs e)
        {
            this.controlDO(_controlDoorLock, true);//門上鎖

            _stepTimers[(int)Step.s6].Set();//接續加工時 測高已經測完了

            //給予異常前的測高值
            _CompensatePosition = Backup_CompensatePosition;
            ThickProcess = Backup_ThickProcess;
            AutoThickProcess = Backup_AutoThickProcess;
         


            Continuous_Processing_flag = true;

            StartTime = DateTime.Now;//紀錄自動加工開始時間

            _isProcessing = true;

            if (_isSysSimulated)
                return;

            var cleaner = new TxtFileCleaner($"{Path.Combine(C_Path, "EventLog")}");//清理大於180天的LOG
            cleaner.CleanOldTxtFiles();

            //Write Event
            writeEvent("UserEvent", "Continuous processing.");
            chk_Distance.Checked = false;

            // Disable group control
            //foreach (var groupBox in _groupSteps)
            //{
            //    enableGroupControl(groupBox, false);
            //}
            btn_AutoMode_Process_Stop.Enabled = true;

            //Initialize status
            updatePnlStepState(pnl_AutoMode_Process_Status, StepState.inactive);
            lbl_AlignmentProcess_Step7.Text= lbl_Progress_Step7.Text = string.Format("Progress = {0}% ({1}/{2})", 0, 0, _NbrOfFeature_Total);
            progressBar_AlignmentProcess_Step7.Value=progressBar_AutoMode_Step7.Value = 0;


            if (BW_AutoProcess.IsBusy != true && !string.IsNullOrEmpty(txt_AutoMode_LotMumber.Text))
            {
                // Start the asynchronous operation.
                BW_AutoProcess.RunWorkerAsync();
            }
            else
            {
                MessageBox.Show($"請於LotNumber:欄位輸入該加工項目之批號，並再次按下自動加工");
                return;
            }
            // Write Process
            writeProcess("Step7", "加工流程已開始");

            //Send MES 更改狀態為生產
            if (_ConfigSystem._ConnectSetting.ChptMES)
            {
                //解析NC檔
                if (!chk_RD_NCfile.Checked)
                {
                    //MES LOG記錄 開始時間
                    _CHPT_MES.Start();
                    //一般自動模式
                    Description = "生產";
                    //[BM:將機台資訊傳送給MES]
                    _CHPT_MES.Send_MES_Data(EquipmentNo, (int)EquipmentState.AutoProcess, UserNo, Description, IsEQPTrigger, _CHPT_MES.Batch_Number);

                }
                else
                {
                    //RD手動模式
                    Description = "實驗與借機";
                    //[BM:將機台資訊傳送給MES]
                    _CHPT_MES.Send_MES_Data(EquipmentNo, (int)EquipmentState.Experiment, UserNo, Description, IsEQPTrigger, LotNo);
                }
            }
        }

        private void btn_AutoMode_Process_Start_Click(object sender, EventArgs e)
        {
            StartTime = DateTime.Now;//紀錄自動加工開始時間

            _isProcessing = true;

          

            var cleaner = new TxtFileCleaner($"{Path.Combine(C_Path, "EventLog")}");//清理大於180天的LOG
            cleaner.CleanOldTxtFiles();

            //Write Event
            writeEvent("UserEvent", "Start processing.");
            chk_Distance.Checked = false;

            // Disable group control
            //foreach (var groupBox in _groupSteps)
            //{
            //    enableGroupControl(groupBox, false);
            //}
            btn_AutoMode_Process_Stop.Enabled = true;

            //Initialize status
            updatePnlStepState(pnl_AutoMode_Process_Status, StepState.inactive);
            lbl_AlignmentProcess_Step7.Text=lbl_Progress_Step7.Text = string.Format("Progress = {0}% ({1}/{2})", 0, 0, _NbrOfFeature_Total);
            progressBar_AlignmentProcess_Step7.Value=progressBar_AutoMode_Step7.Value = 0;

          

            if (BW_AutoProcess.IsBusy != true && !string.IsNullOrEmpty(txt_AutoMode_LotMumber.Text))
            {
                // Start the asynchronous operation.
                BW_AutoProcess.RunWorkerAsync();
            }
            else
            {
                MessageBox.Show($"請於LotNumber:欄位輸入該加工項目之批號，並再次按下自動加工");
                return;
            }
            // Write Process
            writeProcess("Step7", "加工流程已開始");

            if (_isSysSimulated)
                return;

            //Send MES 更改狀態為生產
            if (_ConfigSystem._ConnectSetting.ChptMES)
            {
                //解析NC檔
                if (!chk_RD_NCfile.Checked)
                {
                    //MES LOG記錄 開始時間
                    _CHPT_MES.Start();
                    //一般自動模式
                    Description = "生產";
                    //[BM:將機台資訊傳送給MES]
                    _CHPT_MES.Send_MES_Data(EquipmentNo, (int)EquipmentState.AutoProcess, UserNo, Description, IsEQPTrigger, _CHPT_MES.Batch_Number);

                }
                else
                {
                    //RD手動模式
                    Description = "實驗與借機";
                    //[BM:將機台資訊傳送給MES]
                    _CHPT_MES.Send_MES_Data(EquipmentNo, (int)EquipmentState.Experiment, UserNo, Description, IsEQPTrigger, LotNo);
                }
            }

        }
        /// <summary>
        /// 自動加工停止按鈕
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_AutoMode_Process_Stop_Click(object sender, EventArgs e)
        {

            if (_isSysSimulated)
                return;
            doSimLasing(false);

            //Write Event
            writeEvent("UserEvent", "Abort processing.");

            if (BW_AutoProcess.WorkerSupportsCancellation == true)
            {
                // Cancel the asynchronous operation.
                BW_AutoProcess.CancelAsync();

                // Stop the Laser scanner job
                _LaserScanner.AbortJob();

                // Stop Process Elapsed Time
                _stopwatch_ElapsedTime.Stop();
            }

            // Write Process
            writeProcess("Step7", "中斷加工流程");

            //MES 故障狀態
            //[BM:將機台資訊傳送給MES]
            if (_ConfigSystem._ConnectSetting.ChptMES && SendErr_ToMES_Req)
            {
                SendErr_ToMES_Req = false;
                Description = "故障與維修";
                //[BM:將機台資訊傳送給MES]
                _CHPT_MES.Send_MES_Data(EquipmentNo, (int)EquipmentState.Error, UserNo, Description, IsEQPTrigger, _CHPT_MES.Batch_Number);
            }


            ////[BM:停止自動流程，關閉雷射綠色格子之方法]
            setEmergencyStopLock();//觸發急停，要案解除。

        }

        private void btn_AutoMode_Reset_Click(object sender, EventArgs e)
        {

            //=================LCH對位加工設置重置
            UpdateDIPnlBackColor(pnl_AlignmentProcess_SelectFile_Status, false);
            txt_AlignmentProcess_FileName.Text = "";

            lbl_AlignmentProcess_Point1.Text = $"定位點1:{Environment.NewLine}(X,Y)";
            lbl_AlignmentProcess_Point2.Text = $"定位點2:{Environment.NewLine}(X,Y)";
            lbl_AlignmentProcess_Angle.Text = $"旋轉角度:°";

            // 建立面板清單
            var panels = new List<Panel>
{
    pnl_AlignmentProcess_SelectFile_Status,
    pnl_AlignmentProcess_Chuck_Status,
    pnl_AlignmentProcess_MoveZ_Status,
    pnl_AlignmentProcess_Point1_Status,
    pnl_AlignmentProcess_Point2_Status,
    pnl_AlignmentProcess_Angle_Status,
    pnl_AlignmentProcess_OrgPoint_Status,
    pnl_AlignmentProcess_LockDoor_Status,
    pnl_AlignmentProcess_Thick_Status,
    pnl_AlignmentProcess_Status
};

            // 統一設為 false
            foreach (var pnl in panels)
            {
                UpdateDIPnlBackColor(pnl, false);
            }

            foreach (var gb in new GroupBox[]
{
    gb_AlignmentProcess_Chuck,
    gb_AlignmentProcess_MoveZ,
    gb_AlignmentProcess_PointAngle,
    gb_AlignmentProcess_OrgPoint,
    gb_AlignmentProcess_LockDoor,
    gb_AlignmentProcess_Thick,
    gb_AlignmentProcess
})
            {
                gb.Enabled = false;
            }
            //=================LCH對位加工設置重置


            //  LCH2CutSetLaserPower(0);

            //[BM:指定加工起始項目-啟用pl內之按鈕]
            pl_operation_setting2.Enabled = true;
            //[BM:防呆-重置鈕按下才啟用控制選項]
            enableUIGroup(true);

            if (_isSysSimulated)
                return;

            //[BM:確保燈號正常]
            _isProcessing = false;

            // Reset whole status
            // Initialize the parameters
            for (int i = 0; i < _stepNumber; i++)
            {
                _stepFlags[i] = false;
            }

            // Release DoorLock
            this.controlDO(_controlDoorLock, false);

            // Reset UI status
            progressBar_AlignmentProcess_Step6.Value = progressBar_AutoMode_Step6.Value = 0;

            progressBar_AlignmentProcess_Step7.Value = progressBar_AutoMode_Step7.Value = 0;

            txt_AutoMode_LotMumber.Text = "";
            txt_AutoMode_FileName.Text = "";
            //lbl_AutoMode_OrgPoint.Text = string.Format("OrgPoint:{0}(x, y)", Environment.NewLine);
            lbl_AlignmentProcess_Step6.Text = lbl_Progress_Step6.Text = "Progress = 0% (0/100)";
            lbl_AlignmentProcess_Step7.Text = lbl_Progress_Step7.Text = "Progress = 0% (0/100)";
            lbl_AlignmentProcess_ElapsedTime.Text = lbl_AutoMode_Process_ElapsedTime.Text = "已加工時間 : 00:00:00";
            //拍攝功能-開門關閉雷射二次加工計數
            AutoThickProcess_Video_Count = 0;

            AfterSeconds = "";
            SET_W = 0;
            W = 0;
            UD_or_LD = "";
            Progress_Percentage = "";

            //pin_time_average.Clear();

            // Stop Process Elapsed Time
            _stopwatch_ElapsedTime.Reset();

            // Disable group control
            foreach (var groupBox in _groupSteps)
            {
                enableGroupControl(groupBox, false);
            }

            // Disable button control
            btn_AutoMode_ViewFile.Enabled = false;
            btn_AutoMode_Thick_View.Enabled = false;
            btn_AutoMode_SelectFile.Enabled = true;
            chk_RD_NCfile.Checked = false;

            // Update panels
            updatePnlStepState(pnl_AutoMode_LoadFile_Status, StepState.inactive);
            //[BM:不放開吸附功能，狀態顯示不會放開(所有程式只有UI介面的取消吸附才可以解除吸附)]
            //updatePnlStepState(pnl_AutoMode_Chuck_Status, StepState.inactive);
            updatePnlStepState(pnl_AutoMode_LockDoor_Status, StepState.inactive);
            updatePnlStepState(pnl_AutoMode_MoveZ_Status, StepState.inactive);
            updatePnlStepState(pnl_AutoMode_OrgPoint_Status, StepState.inactive);
            updatePnlStepState(pnl_AutoMode_Thick_Status, StepState.inactive);
            updatePnlStepState(pnl_AutoMode_Process_Status, StepState.inactive);

            //MES LOG記錄 參數清除
            _CHPT_MES.Clear();

            // Write Event Log
            writeEvent("自動流程", "自動流程，重置!");

            // Write Process
            writeProcess("Step0", "重置流程已完成");
        }

        #endregion

        #region GUI Control Method

        private delegate void dEnableGroupWithBool(GroupBox groupBox, bool enable);
        private void enableGroupControl(GroupBox groupBox, bool enable)
        {
            if (groupBox.InvokeRequired)
            {
                var func = new dEnableGroupWithBool(enableGroupControl);
                groupBox.Invoke(func, groupBox, enable);
            }
            else
            {
                groupBox.Enabled = enable;
            }
        }

        private delegate void dUpdatePnlStepState(Panel panel, StepState state);
        private void updatePnlStepState(Panel panel, StepState state)
        {
            if (panel.InvokeRequired)
            {
                var func = new dUpdatePnlStepState(updatePnlStepState);
                panel.Invoke(func, panel, state);
            }
            else
            {
                switch (state)
                {
                    case StepState.inactive:
                        panel.BackColor = Color.DarkGray;
                        break;
                    case StepState.OK:
                        panel.BackColor = Color.Green;
                        break;
                    case StepState.NO:
                        panel.BackColor = Color.Red;
                        break;
                    default:
                        throw new Exception("Undefined state = " + state);
                }
            }
        }

        #endregion

        #region Private Method

        private void autoMode_StartStepWithTimer(Step step)
        {
            // Raise up flag
            _stepProgressON = true;

            // Reset timer
            _stepTimers[(int)step].Reset();

            // Call timer check method in new thread
            Thread thread = new Thread(autoMode_StepTimerWait);
            thread.Start(step);
        }

        private void autoMode_StepTimerWait(object obj)
        {
            Step step = (Step)obj;
            int timeoutMSec = 10000;

            if (_stepTimers[(int)step].WaitOne(timeoutMSec, false))
            {
                _stepFlags[(int)step] = true;
                switch (step)
                {
                    case Step.s1:
                        writeEvent("MainForm.AutoProcess", string.Format("Step.{0} process completed.", step));
                        enableGroupControl(group_AutoMode_Step2, true);
                        break;
                    case Step.s2:
                        writeEvent("MainForm.AutoProcess", string.Format("Step.{0} process completed.", step));
                        enableGroupControl(group_AutoMode_Step3, true);
                        break;
                    case Step.s3:
                        writeEvent("MainForm.AutoProcess", string.Format("Step.{0} process completed.", step));
                        enableGroupControl(group_AutoMode_Step4, true);
                        break;
                    case Step.s4:
                        writeEvent("MainForm.AutoProcess", string.Format("Step.{0} process completed.", step));
                        enableGroupControl(group_AutoMode_Step5, true);
                        break;
                    case Step.s5:
                        writeEvent("MainForm.AutoProcess", string.Format("Step.{0} process completed.", step));
                        enableGroupControl(group_AutoMode_Step6, true);
                        break;
                    case Step.s6:
                        writeEvent("MainForm.AutoProcess", string.Format("Step.{0} process completed.", step));
                        enableGroupControl(group_AutoMode_Step7, true);
                        break;
                    default:
                        writeError("MainForm.AutoProcess", "Received invalid Step." + step);
                        setEquipmentFailLock();

                        break;
                }
            }
            else
                _stepFlags[(int)step] = false;

            // Raise off flag
            _stepProgressON = false;
        }

        private void checkAutoModeProgress()
        {
            if (!_stepProgressON)
                return;
            // Step 1 check
            // No need to check
            // Step 2 check
            // No need to check, it completed while move to Focal plane completed
            // Step 3 check
            // No need to check, it completed while _orgPoint updated from _positionFeedback
            // Step 4 check
            if (this.pnl_AutoMode_Chuck_Status.BackColor == Color.Green)
                _stepTimers[(int)Step.s2].Set();
            // Step 5 check
            if (this.pnl_AutoMode_LockDoor_Status.BackColor == Color.Green)
                _stepTimers[(int)Step.s5].Set();
            // Step 6 check
            // No need to check, it completed while BackgroundWorker done
            // Step 7 check
            // No need to check, it completed while BackgroundWorker done
        }

        private delegate void dDoLasing(bool enable);
        private void doSimLasing(bool enable)
        {
            if (pnl_Lasing_Inner.InvokeRequired)
            {
                var func = new dDoLasing(doSimLasing);
                pnl_Lasing_Inner.Invoke(func, enable);
            }
            else
            {
                _isLasing = enable;
                if (enable)
                {
                    lbl_Lasing_Indicator.ForeColor = Color.Green;
                    pnl_Lasing_Inner.BackColor = Color.LightGreen;
                    pnl_Lasing_Outer.BackColor = Color.Green;
                }
                else
                {
                    lbl_Lasing_Indicator.ForeColor = Color.LightGray;
                    pnl_Lasing_Inner.BackColor = Color.LightGray;
                    pnl_Lasing_Outer.BackColor = Color.LightGray;
                }

                string msg = "";
                if (enable)
                    msg = "Simulation ON";
                else
                    msg = "Simulation OFF";

                // Write Log
                writeDataQueue("Laser", msg);
            }
        }

        private void doLasing(bool enable)
        {
            enable = !enable;
            try
            {
                _isLaserBusy = true;
                //_Laser.SetExtModGateSyncEnable(enable);
                _isLaserBusy = false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void switchLasingFlag(bool enable)
        {
            if (pnl_Lasing_Inner.InvokeRequired)
            {
                var func = new dDoLasing(switchLasingFlag);
                pnl_Lasing_Inner.Invoke(func, enable);
            }
            else
            {
                _isLasing = enable;
                if (enable)
                {
                    lbl_Lasing_Indicator.ForeColor = Color.Green;
                    pnl_Lasing_Inner.BackColor = Color.LightGreen;
                    pnl_Lasing_Outer.BackColor = Color.Green;
                }
                else
                {
                    lbl_Lasing_Indicator.ForeColor = Color.LightGray;
                    pnl_Lasing_Inner.BackColor = Color.LightGray;
                    pnl_Lasing_Outer.BackColor = Color.LightGray;
                }
            }
        }

        private void scannerCheckDone()
        {
            while (true)
            {
                if (_LaserScanner_LastPLCStates.PLC_JOB_COMPLETED)
                {

                }
                Thread.Sleep(10);
            }
        }

        private void scannerCheckStatus()
        {
            // Check Status is available or not
            
        }

        private double getCompesatePosition()
        {
            int _Retrycount = 0;
            int _Retry = 5;

            // Do Thickness measurement
            _isConfocalLaserBusy = true;
            //Thread.Sleep(50);

            for (int i = 0; i < _Retry; i++)
            {
                ConfocalLaser.evalResult result = _ConfocalLaser.Trigger(1, out _ConfocalLaser_Output, 0);
                //Thread.Sleep(50);

                _isConfocalLaserBusy = false;

                _Retrycount++;

                if (_ConfocalLaser_Output < -99999 && _Retrycount == 5)
                {
                    writeError("MeasureErr", "GetConfocalLaserStatus fail.");
                    throw new Exception("Invalid Thickness measurement, please check the Z position");
                }
                else if (_ConfocalLaser_Output > -100)
                {
                    _Retry = 0;
                }
            }
            //測高數據換算成um，單位為mm
            //Z軸原點0為最高點，-1為反向因Z軸向下是負  (HI數據正值:樣品靠近雷射, LO數據負值:樣品遠離雷射)
            double ThickResult = -1 * _ConfocalLaser_Output * 0.001; //Since confocal laser result unit: um
            writeEvent("微孔測高數據", ThickResult.ToString() + "mm");

            //測高數據 - 測高與雷射相對位置(Height_Equip：-16.376) (計算測高與雷射相對位置)
            //Ex：11.9961 = -4.3799-(-16.376)
            double distance = ThickResult - _ConfigEquip._Height_Equip.Laser;

            // 測高基準 - distance
            //Ex：-259.6251 = -247.629 - 11.9961
            return _positionFeedback[(int)MotionAxis.Z] - distance;
        }

        //[BM:抓取刀具方法]
        private void uploadJob(string JobxName)
        {
            bool FileJobxCheck = false;
            bool JobxNamesCheck = false;
            string filePath = "";
            string[] filxes;

            _isLaserScannerBusy = true;

            //清除振鏡上刀具
            _LaserScanner.ClearJobs();

            //解析NC檔
            if (!chk_RD_NCfile.Checked || _ConfigSystem._RDJobxFolderPath == "")
            {
                //一般自動模式
                filePath = Path.Combine(_ConfigSystem._JobxFolderPath, JobxName + ".jobx");
                filxes = Directory.GetFiles(_ConfigSystem._JobxFolderPath, "*.jobx");
            }
            else
            {
                //RD手動模式
                filePath = Path.Combine(_ConfigSystem._RDJobxFolderPath, JobxName + ".jobx");
                filxes = Directory.GetFiles(_ConfigSystem._RDJobxFolderPath, "*.jobx");
            }

            //確認指定路徑是否有此專案刀具
            foreach (var file in filxes)
            {
                if (file == filePath)
                {
                    FileJobxCheck = true;
                    break;
                }
            }
            if (FileJobxCheck == false)
            {
                writeError("載入刀具錯誤", "找尋不到刀具名稱！！刀具名稱： " + JobxName);
                throw new Exception("載入刀具錯誤");
            }

            //載入刀具
            _LaserScanner.LoadJobFromFile(filePath);
            Thread.Sleep(100);

            //讀取振鏡上Jobx刀具名稱，與專案刀具進行檢查
            if (_ConfigProcess._IsAutoProcessCheckJobx)
            {
                //讀取振鏡上所有刀具
                List<string> JobxNames = new List<string>();
                _LaserScanner.GetJobNames(out JobxNames);

                //檢查振鏡上刀具是否跟專案刀具一樣                             
                foreach (var _jobnames in JobxNames)
                {
                    string jobSubnames = _jobnames.Substring(8);
                    if (jobSubnames == JobxName)
                    {
                        JobxNamesCheck = true;
                        break;
                    }
                }

                if (JobxNamesCheck == false)
                {
                    writeError("振鏡刀具錯誤", "振鏡上刀具與專案刀具不符！！刀具名稱： " + JobxName);
                    throw new Exception("振鏡刀具錯誤");
                }
            }

            //_LaserScanner.SelectJobByName(JobxName);
            _isLaserScannerBusy = false;
        }

        private void uploadJobWithSpecifiedFilePath(string filePath)
        {
            _isLaserScannerBusy = true;

            //[BM:清除振鏡上之刀具]
            _LaserScanner.ClearJobs(); //清除振鏡上刀具

            _LaserScanner.LoadJobFromFile(filePath);
            _isLaserScannerBusy = false;
        }

        private void uploadJobListWithCompensateZ(string fileName, double offsetZ)
        {
            // Upload Scanner XML file
            // Delete all JobList Scanner XML file
            _isLaserScannerBusy = true;

            //清除振鏡上刀具
            _LaserScanner.ClearJobs();

            string filePathFeature = Path.Combine(_ConfigSystem._JobxFolderPath, fileName + ".xml");

            // Import XML file
            string xml = _XMLParser.ImportXML(filePathFeature);
            string filePathNew = Path.Combine(_ConfigSystem._JobxFolderPath, fileName + "_" + offsetZ + ".xml");
            double orgOffset = _XMLParser.readOffsetZ(xml);
            xml = _XMLParser.writeOffsetZ(xml, orgOffset + offsetZ);
            // Export XML file
            _XMLParser.ExportXML(xml, filePathNew);

            // Upload new Scanner XML file
            _LaserScanner.LoadJobFromFile(filePathNew);
            // Delete the new XML file
            _XMLParser.DeleteXML(filePathNew);
            _isLaserScannerBusy = false;
        }

        private void uploadJobListWithCompensateZWithSpecifiedFilePath(string filePath, double offsetZ)
        {
            // Upload Scanner XML file
            // Delete all JobList Scanner XML file
            _isLaserScannerBusy = true;

            //清除振鏡上刀具
            _LaserScanner.ClearJobs();

            // Import XML file
            string xml = _XMLParser.ImportXML(filePath);
            string filePathNew = Path.Combine(_ConfigSystem._JobxFolderPath, "temp_" + offsetZ + ".xml");
            double orgOffset = _XMLParser.readOffsetZ(xml);
            xml = _XMLParser.writeOffsetZ(xml, orgOffset + offsetZ);
            // Export XML file
            _XMLParser.ExportXML(xml, filePathNew);

            // Upload new Scanner XML file
            _LaserScanner.LoadJobFromFile(filePathNew);
            // Delete the new XML file
            _XMLParser.DeleteXML(filePathNew);
            _isLaserScannerBusy = false;
        }

        /// <summary>
        /// 功率量測流程：載入 Powermeter 刀具、出光、量測並與設定值比對。
        /// 回傳 true = 量測通過(可繼續)；false = 量測異常或無法驗證(應中止)。
        /// </summary>
        private bool RunPowerCheck(double laserPowerPct)
        {
            //================================================此範圍放置功率檢測的內容
            enableProcessGas(false);//關氮氣

            enableDustcover(true);    // 開啟防塵蓋

            OutputPower.Clear();      // 記錄前清除值
            _isReadPowerMeter = true; // 開始獲取功率值

            try
            {
                // 1) 設定功率
                if (!SetLaserPowerPercentage(laserPowerPct))
                {
                    MessageBox.Show($"雷射功率輸出設定失敗");
                    return false;
                }

                // 2) 載入 Powermeter 刀具
                #region 載入加工刀具
                try
                {
                    bool FileJobxCheck = false;
                    bool JobxNamesCheck = false;
                    string filePath;
                    string[] filxes;
                    string JobxName = "Powermeter";

                    _isLaserScannerBusy = true;
                    _LaserScanner.ClearJobs(); // 清除振鏡上刀具

                    filePath = Path.Combine(C_Path, "LCNjob", "Powermeter_Jobx", JobxName + ".jobx");
                    filxes = Directory.GetFiles(Path.Combine(C_Path, "LCNjob", "Powermeter_Jobx"), "*.jobx");

                    foreach (var file in filxes)
                        if (file == filePath) { FileJobxCheck = true; break; }

                    if (!FileJobxCheck)
                    {
                        _AutoDwring_flag = 0;
                        writeError("載入刀具錯誤", "找尋不到刀具名稱！！刀具名稱： " + JobxName);
                        return false;
                    }

                    _LaserScanner.LoadJobFromFile(filePath);
                    Thread.Sleep(100);

                    if (_ConfigProcess._IsAutoProcessCheckJobx)
                    {
                        List<string> JobxNames = new List<string>();
                        _LaserScanner.GetJobNames(out JobxNames);

                        foreach (var _jobnames in JobxNames)
                        {
                            string jobSubnames = _jobnames.Substring(8);
                            if (jobSubnames == JobxName) { JobxNamesCheck = true; break; }
                        }

                        if (!JobxNamesCheck)
                        {
                            _AutoDwring_flag = 0;
                            writeError("振鏡刀具錯誤", "振鏡上刀具與專案刀具不符！！刀具名稱： " + JobxName);
                            return false;
                        }
                    }

                    _isLaserScannerBusy = false;
                }
                catch (Exception)
                {
                    _AutoDwring_flag = 0;
                    MessageBox.Show("載入刀具錯誤，請確認是否有這項刀具。", "Error！！");
                    return false;
                }
                #endregion

                // 3) 出光
                Thread.Sleep(10);


                if (!_isLaserScannerBusy)
                {
                    if (!DoLaserProcessing())
                        DoLaserReconnect(); // 重新連線
                }

                // 4) job 未完成 → 無法驗證功率
                if (_isLaserScannerBusy || !_FormLaserScanner.States.PLC_JOB_COMPLETED)
                {
                    writeError("功率量測異常", "雷射加工未完成(振鏡忙碌或 JOB 未完成)，無法取得功率。");
                    return false;
                }

                //Stop Laser cutting
              //  StopLaserCutting();

               
                Thread.Sleep(10);

                _isReadPowerMeter = false; // 先停止記錄，再讀清單，降低與 timer1_Tick 的競爭

                // 5) 沒收集到樣本
                if (OutputPower.Count == 0)
                {
                    if (_AutoProcessLog_flag)
                        writeEventLog_Automatic_processing($"功率量測10秒值為0", $"無法取得功率，中止");
                    writeError("功率量測異常", "功率量測樣本數為 0，無法驗證功率。");
                    return false;   // ←★ 若要維持原本「繼續加工」，這行改成 return true;
                }

                // 6) 比對
                W = _OutputPower = OutputPower.Average();      // 實際量測 (W)
                var powerResult = Percentage_conversion_W(laserPowerPct);
                double expectedPower_W = powerResult.Item2;   // 設定/預期 (W)

                if (powerResult.Item2 <= 0)
                {
                    writeError("功率量測異常", $"功率換算異常值為{powerResult.Item2}，請確認 ConfigPower.ini 設定。");
                    MessageBox.Show($"功率換算異常值為{powerResult.Item2}，請確認 ConfigPower.ini 設定。", "Error！！");
                    return false;
                }

                SET_W = expectedPower_W;

                if (Math.Abs(_OutputPower - expectedPower_W) >= 0.05) // 差值 >= 0.05W(=50mW) 視為異常
                {
                    writeError("功率量測異常", $"量測值:{_OutputPower:F3}W，預期值:{expectedPower_W:F3}W，差值:{Math.Abs(_OutputPower - expectedPower_W):F3}W");
                    MessageBox.Show($"功率量測異常！\n量測值：{_OutputPower:F3} W\n預期值：{expectedPower_W:F3} W\n差值：{Math.Abs(_OutputPower - expectedPower_W):F3} W", "Error！！");
                    return false;
                }

                writeEvent("功率量測正常", $"量測值:{_OutputPower:F3}W，預期值:{expectedPower_W:F3}W");

                if (_AutoProcessLog_flag)
                    writeEventLog_Automatic_processing($"功率量測正常", $"量測值:{_OutputPower:F3}W，預期值:{expectedPower_W:F3}W");

                return true; // 量測通過
            }
            finally
            {
                _isReadPowerMeter = false; // #4：不論從哪個 return 離開都停止記錄，避免旗標洩漏

                enableDustcover(false);    // 關閉防塵蓋                   
                enableProcessGas(true); // ProcessGas ON 開啟氮氣
              

            }
        }
        ///// <summary>
        ///// 功率量測流程
        ///// </summary>
        //private bool RunPowerCheck()
        //{
        //    //================================================此範圍放置功率檢測的內容
        //    enableDustcover(true);//開啟防塵蓋
        //    OutputPower.Clear();//記錄前清除值
        //    _isReadPowerMeter = true;//開始獲取功率值

        //    if (SetLaserPowerPercentage(laserPowerPct))//更改功率值  // Set Laser Output Power
        //    {
        //        #region 載入加工刀具
        //        try
        //        {
        //            bool FileJobxCheck = false;
        //            bool JobxNamesCheck = false;
        //            string filePath = "";
        //            string[] filxes;
        //            string JobxName = "Powermeter";

        //            _isLaserScannerBusy = true;

        //            //清除振鏡上刀具
        //            _LaserScanner.ClearJobs();

        //            filePath = Path.Combine(C_Path, "LCNjob", "Powermeter_Jobx", JobxName + ".jobx");
        //            filxes = Directory.GetFiles(Path.Combine(C_Path, "LCNjob", "Powermeter_Jobx"), "*.jobx");//子翔設定的 持續輸出10秒打在Powermeter上量測功率

        //            //確認指定路徑是否有此專案刀具
        //            foreach (var file in filxes)
        //            {
        //                if (file == filePath)
        //                {
        //                    FileJobxCheck = true;
        //                    break;
        //                }
        //            }
        //            if (FileJobxCheck == false)
        //            {
        //                _AutoDwring_flag = 0;
        //                writeError("載入刀具錯誤", "找尋不到刀具名稱！！刀具名稱： " + JobxName);
        //                //  e.Cancel = true;
        //                //  throw new Exception("載入刀具錯誤");
        //                return false;

        //            }

        //            //載入刀具
        //            _LaserScanner.LoadJobFromFile(filePath);
        //            Thread.Sleep(100);

        //            //讀取振鏡上Jobx刀具名稱，與專案刀具進行檢查
        //            if (_ConfigProcess._IsAutoProcessCheckJobx)
        //            {
        //                //讀取振鏡上所有刀具
        //                List<string> JobxNames = new List<string>();
        //                _LaserScanner.GetJobNames(out JobxNames);

        //                //檢查振鏡上刀具是否跟專案刀具一樣                             
        //                foreach (var _jobnames in JobxNames)
        //                {
        //                    string jobSubnames = _jobnames.Substring(8);
        //                    if (jobSubnames == JobxName)
        //                    {
        //                        JobxNamesCheck = true;
        //                        break;
        //                    }
        //                }

        //                if (JobxNamesCheck == false)
        //                {
        //                    _AutoDwring_flag = 0;
        //                    writeError("振鏡刀具錯誤", "振鏡上刀具與專案刀具不符！！刀具名稱： " + JobxName);
        //                    //e.Cancel = true;
        //                    //throw new Exception("振鏡刀具錯誤");
        //                    return false;

        //                }
        //            }

        //            //_LaserScanner.SelectJobByName(JobxName);
        //            _isLaserScannerBusy = false;
        //        }
        //        catch (Exception)
        //        {
        //            _AutoDwring_flag = 0;
        //            MessageBox.Show("載入刀具錯誤，請確認是否有這項刀具。", "Error！！");
        //            //e.Cancel = true;
        //            //return;
        //            return false;

        //        }
        //        #endregion
        //        return true;

        //    }
        //    else
        //    {
        //        MessageBox.Show($"雷射功率輸出設定失敗");
        //        //e.Cancel = true;
        //        //return ;
        //        return false;

        //    }

        //    Thread.Sleep(10);

        //    if (!_isLaserScannerBusy)
        //    {
        //        if (!DoLaserProcessing()) // 執行一次並取得回傳結果是true 或是false
        //        {

        //            DoLaserReconnect();//重新連線
        //        }
        //    }

        //    if (!_isLaserScannerBusy && _FormLaserScanner.States.PLC_JOB_COMPLETED)//加工完成且雷射狀態為true
        //    {
        //        Thread.Sleep(10);


        //        _isReadPowerMeter = false;//停止紀錄功率量測值


        //        if (OutputPower.Count == 0)//若功率值儲存內容為0
        //        {
        //            if (_AutoProcessLog_flag)
        //                writeEventLog_Automatic_processing($"功率量測10秒值為0", $"會繼續加工");

        //        }
        //        else
        //        {
        //            W = _OutputPower = OutputPower.Average();//實際量測到到W值
        //                                                     // 用內插法計算當前 laserPowerPct 對應的功率值 (W)
        //            var powerResult = Percentage_conversion_W(laserPowerPct);
        //            double expectedPower_W = powerResult.Item2;


        //            if (powerResult.Item2 <= 0)
        //            {

        //                writeError("功率量測異常", $"功率換算異常值為{powerResult.Item2}，請確認 ConfigPower.ini 設定。");

        //                // 內插法回傳異常（-1 或 0）
        //                MessageBox.Show($"功率換算異常值為{powerResult.Item2}，請確認 ConfigPower.ini 設定。", "Error！！");
        //                return false;

        //            }
        //            SET_W = expectedPower_W;

        //            if (Math.Abs(_OutputPower - expectedPower_W) >= 0.05) // 差值 >= 0.05W 顯示異常
        //            {
        //                writeError("功率量測異常",
        //                    $"量測值:{_OutputPower:F3}W，預期值:{expectedPower_W:F3}W，差值:{Math.Abs(_OutputPower - expectedPower_W):F3}W");
        //                MessageBox.Show(
        //                    $"功率量測異常！\n量測值：{_OutputPower:F3} W\n預期值：{expectedPower_W:F3} W\n差值：{Math.Abs(_OutputPower - expectedPower_W):F3} W",
        //                    "Error！！");
        //                //e.Cancel = true;
        //                //return;
        //                return false;

        //            }
        //            else
        //            {
        //                writeEvent("功率量測正常", $"量測值:{_OutputPower:F3}W，預期值:{expectedPower_W:F3}W");

        //                if (_AutoProcessLog_flag)
        //                    writeEventLog_Automatic_processing($"功率量測正常", $"量測值:{_OutputPower:F3}W，預期值:{expectedPower_W:F3}W");
        //                return true;





        //            }
        //        }

        //    }
        //    //================================================此範圍放置功率檢測的內容

        //}
        private void enableProcessGas(bool flag)
        {
            if (flag)
            {
                //Write Event
                writeEvent("Event", "開啟氮氣.");
            }
            else
            {
                //Write Event
                writeEvent("Event", "關閉氮氣.");
            }
            this.controlDO(_controlProcessGasSwitch, flag);
        }
        /// <summary>
        /// IO輸出 執行吸附與否
        /// </summary>
        /// <param name="flag"></param>
        private void enableChuckVacuum(bool flag)
        {
            if (flag)
            {
                //Write Event
                writeEvent("Event", "開啟真空.");

                //this.controlDO(_controlDisableChuckVacuum, false);

                Thread.Sleep(100);

                this.controlDO(_controlDustCollect, true);
                _isVacuum = true;

            }
            else
            {
                //Write Event
                writeEvent("Event", "關閉真空.");

                this.controlDO(_controlDustCollect, false); 

                Thread.Sleep(100);
                _isVacuum = false;

                //this.controlDO(_controlDisableChuckVacuum, true);
            }
        }
        /// <summary>
        /// 是否開啟功率檢測之防塵蓋
        /// </summary>
        /// <param name="flag"></param>
        private void enableDustcover(bool flag)
        {
            if (flag)
            {                //Write Event
                writeEvent("Event", "Dustcover open.");

                this.controlDO(_controlDustcoverVacuum, true);

                Thread.Sleep(100);

                writeEvent("Event", "Enable DustcoverVacuum.");

                this.controlDO(_controlDustcover, false);

            }
            else
            {
                //Write Event
                writeEvent("Event", "Dustcover close.");

                this.controlDO(_controlDustcoverVacuum, false);

                Thread.Sleep(100);

                writeEvent("Event", "Disable DustcoverVacuum.");

                this.controlDO(_controlDustcover, true);
            }
        }

        private void updateLasingPanel(bool flag)
        {
            if (pnl_Lasing_Inner.InvokeRequired)
            {
                var func = new dDoLasing(updateLasingPanel);
                pnl_Lasing_Inner.Invoke(func, flag);
            }
            else
            {
                if (flag)
                {
                    lbl_Lasing_Indicator.ForeColor = Color.Green;
                    pnl_Lasing_Inner.BackColor = Color.LightGreen;
                    pnl_Lasing_Outer.BackColor = Color.Green;
                }
                else
                {
                    lbl_Lasing_Indicator.ForeColor = Color.LightGreen;
                    pnl_Lasing_Inner.BackColor = Color.Green;
                    pnl_Lasing_Outer.BackColor = Color.LightGreen;
                }
            }
        }

        private void updateErroringPanel(bool flag)
        {
            if (pnl_Error_Inner.InvokeRequired)
            {
                var func = new dDoLasing(updateErroringPanel);
                pnl_Error_Inner.Invoke(func, flag);
            }
            else
            {
                if (flag)
                {
                    lbl_Error_Indicator.ForeColor = Color.Black;
                    pnl_Error_Inner.BackColor = Color.Yellow;
                    pnl_Error_Outer.BackColor = Color.Red;
                }
                else
                {
                    lbl_Error_Indicator.ForeColor = Color.Black;
                    pnl_Error_Inner.BackColor = Color.Red;
                    pnl_Error_Outer.BackColor = Color.Yellow;
                }
            }
        }

        private double laserParamConvert(double laserPower, double gain, double offset)
        {
            return laserPower * gain + offset;
        }

        private double motionParamConvert(double motionSpeed, double gain, double offset)
        {
            return motionSpeed * gain + offset;
        }

       



        //=============
        //[BM:斷線從連復工-設定時間超過此時間為當機要重連(分鐘)]
        /// <summary>
        /// 逾時時間門檻-當前設定5分鐘(分鐘)
        /// </summary>
        int Overdue_threshold = 5;
        //[BM:斷線從連復工-儲存該筆微孔的開始時間]
        /// <summary>
        /// 儲存該筆項目的開始時間
        /// </summary>
        static DateTime lastUpdateTime;
        //[BM:斷線從連復工-表達是否有重新連線的狀態]
        /// <summary>
        /// <para>0=沒有重連</para>
        /// <para>1=有重連</para>
        /// </summary>
        int reconnect_state = 0;
        //[BM:斷線從連復工-表達是哪個階段的加工]
        /// <summary>
        /// 加工旗標狀態
        /// <para>0=微孔加工</para>
        /// <para>1=其餘加工</para>
        /// </summary>
        int Processing_projects = 0;
        //[BM:斷線從連復工-儲存斷線前加工到的微孔筆數]
        /// <summary>
        /// 紀錄加工到哪一個微孔中有異常而離開重連，復連後從此孔繼續加工。
        /// </summary>
        int reconnect_state_micropores_count = 0;
        //[BM:斷線從連復工-儲存斷線前微孔以外的加工內容]
        /// <summary>
        /// 紀錄微孔加工以外的加工命令，復連後從斷線前的命令繼續。
        /// </summary>
       Queue<QueueCmd> reconnect_state_queueCmds;
   /// <summary>
   /// 是否已顯示重連對話框(若該旗標為false才會顯示是否要重連接)
   /// </summary>
        private bool _isReconnectDialogShown = false;   // 新增這個旗標
        //[BM:斷線從連復工-每30秒檢查一次是否於微孔加工當機5分鐘]
        private void CheckForUpdate(Object source, ElapsedEventArgs e)
        {
            /* 
        Timer 每30秒觸發一次的逾時檢查機制
    
        功能說明：
        1. 定期檢查 lastUpdateTime 是否超過設定門檻（Overdue_threshold），判斷當前加工是否超時。
        2. 若已顯示過重新連線對話框（_isReconnectDialogShown），則直接返回，避免重複彈窗。
        3. 確認 BackgroundWorker 正在執行（IsBusy）後，才執行 CancelAsync() 中斷自動加工流程。
        4. 標記 _isReconnectDialogShown = true，防止 Timer 下次再次觸發而重複顯示對話框。
        5. 顯示「是否重新連線」提示給操作人員。
        6. 若人員選擇 Yes → 執行 DoLaserReconnect() 進行重新連線，重連成功後才會將_isReconnectDialogShown狀態改為false。
        7. 若人員選擇 No  → 執行緊急停止 (setEmergencyStopLock)，解除即停後才會將_isReconnectDialogShown狀態改為false。
        8.
    */
            // 如果已經顯示過對話框，就不再重複檢查和彈窗
            if (_isReconnectDialogShown)
                return;

            if ((DateTime.Now - lastUpdateTime).TotalMinutes > Overdue_threshold)
            {

                if (BW_AutoProcess.IsBusy)//若該執行續有在運作
                    BW_AutoProcess.CancelAsync();//外部請求 停止加工流程(執行續若有遇到檢查點時才會停止)

                _isReconnectDialogShown = true;   // ← 關鍵：標記已顯示對話框

                string logMessage = Processing_projects == 1
                    ? "該筆微孔加工超過5分鐘"
                    : "該筆微孔以外加工超過5分鐘";

                if (_AutoProcessLog_flag)
                    writeEventLog_Automatic_processing(logMessage, "已停止自動加工流程");

                DialogResult result = MessageBox.Show(
                    "此筆工作項目超過5分鐘！是否要重新連線？",
                    "警告",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (_AutoProcessLog_flag)
                    writeEventLog_Automatic_processing("顯示提示框", "是否要重新連線");

                if (result == DialogResult.Yes)
                {
                    reconnect_state = 1;
                    DoLaserReconnect();
                    if (_AutoProcessLog_flag)
                        writeEventLog_Automatic_processing("人員按下 Yes", "進入重新連線流程");
                }
                else
                {
                    reconnect_state = 1;
                    if (_AutoProcessLog_flag)
                        writeEventLog_Automatic_processing("人員按下 No", "不執行動作，顯示異常跳故障");
                    setEmergencyStopLock();
                }
            }
        }
        //private void CheckForUpdate(Object source, ElapsedEventArgs e)
        //{
        //    // 如果已經顯示過對話框，就不再重複檢查和彈窗
        //    if (_isReconnectDialogShown)
        //        return;

        //    if ((DateTime.Now - lastUpdateTime).TotalMinutes > Overdue_threshold)// 檢查變數是否更新(當下時間-該筆微孔開始時間)
        //    {
        //        BW_AutoProcess.CancelAsync();              // 停止加工流程的背景執行緒

        //        switch (Processing_projects)
        //        {
        //            case 1:

        //                if (_AutoProcessLog_flag)
        //                    writeEventLog_Automatic_processing("該筆微孔加工超過5分鐘", $"已停止自動加工流程");

        //                DialogResult result = MessageBox.Show("此筆工作項目超過5分鐘！是否要重新連線？", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);  // 顯示提示框並等待用戶反饋

        //                if (_AutoProcessLog_flag)
        //                    writeEventLog_Automatic_processing("顯示提示框", $"是否要重新連線");

        //                if (result == DialogResult.Yes)//按下是
        //                {
        //                    reconnect_state = 1;//設定旗標為 有重新連接
        //                    DoLaserReconnect(); // 重新連線
        //                    if (_AutoProcessLog_flag)
        //                        writeEventLog_Automatic_processing($"人員按下{result}", $"進入重新連線流程");
        //                }
        //                else
        //                {
        //                    reconnect_state = 1;//設定旗標為 有重新連接
        //                    if (_AutoProcessLog_flag)
        //                        writeEventLog_Automatic_processing($"人員按下{result}", $"不執行動作 顯示異常跳故障");

        //                    setEmergencyStopLock();//起動急停顯示異常
        //                    return;
        //                }
        //                break;

        //            case 2:
        //                if (_AutoProcessLog_flag)
        //                    writeEventLog_Automatic_processing("該筆微孔以外加工超過5分鐘", $"已停止自動加工流程");

        //                DialogResult result1 = MessageBox.Show("此筆工作項目超過5分鐘！是否要重新連線？", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);  // 顯示提示框並等待用戶反饋
        //                if (_AutoProcessLog_flag)
        //                    writeEventLog_Automatic_processing("顯示提示框", $"是否要重新連線");

        //                if (result1 == DialogResult.Yes)//按下是
        //                {
        //                    reconnect_state = 1;//設定旗標為 有重新連接
        //                    DoLaserReconnect(); // 重新連線
        //                    if (_AutoProcessLog_flag)
        //                        writeEventLog_Automatic_processing($"人員按下{result1}", $"進入重新連線流程");
        //                }
        //                else
        //                {
        //                    reconnect_state = 1;//設定旗標為 有重新連接
        //                    if (_AutoProcessLog_flag)
        //                        writeEventLog_Automatic_processing($"人員按下{result1}", $"不執行動作 顯示異常跳故障");

        //                    setEmergencyStopLock();//起動急停顯示異常
        //                    return;
        //                }
        //                break;
        //        }
        //        //Console.WriteLine("警告：超過5分鐘未更新變數！");
        //    }
        //}
        //=============

        #endregion

        #region BackgroundWorker
        /// <summary>
        /// 開始加工後經過多少秒
        /// </summary>
        string AfterSeconds;
        private void BW_AutoProcess_DoWork(object sender, DoWorkEventArgs e)
        {

            #region Check before Processing
            if (!_isSysSimulated)
            {


                // Check LockDoor
                if (!_stepFlags[(int)Step.s5])
                {
                    if (_isSafetyInterlock)
                    {
                        MessageBox.Show("電子門鎖狀態異常，請確認門板是否正確闔上並啟動上鎖。", "Error！！");
                        e.Cancel = true;
                        return;
                    }
                }

                // Check ThickProcess
                if (!_stepFlags[(int)Step.s6])
                {
                    MessageBox.Show("測導板厚度狀態異常，請確認是否已執行過。", "Error！！");
                    e.Cancel = true;
                    return;
                }

                // Check MonitorStatus
                if (_isSysMonitorFailed)
                {
                    MessageBox.Show("系統監控狀態異常，請確認設備裝置功能，並點擊「重置 Reset」。", "Error！！");
                    e.Cancel = true;

                    //MES 故障狀態
                    if (_ConfigSystem._ConnectSetting.ChptMES && SendErr_ToMES_Req)
                    {
                        SendErr_ToMES_Req = false;
                        Description = "故障與維修";
                        //[BM:將機台資訊傳送給MES]

                        _CHPT_MES.Send_MES_Data(EquipmentNo, (int)EquipmentState.Error, UserNo, Description, IsEQPTrigger, _CHPT_MES.Batch_Number);
                    }

                    return;
                }
            }
            #endregion

            #region Initialization


            // Disable GUI Controls
            //EnableGUIMotionControls(false);

            // Disable UI control
            enableUIGroup(false);

            // Get NC code data
            NCScriptParser data = _NCParser;
            if (_isSysSimulated)
            {
                string NCParser_Cagila_CsvPath = Path.Combine(_NCParser._ProjectDirectory, "ProgramObjects", "NCParser" + ".csv");   // 組合對應的解析結果CSV儲存路徑
                using (StreamWriter stream = new StreamWriter(NCParser_Cagila_CsvPath, false))                         // 建立輸出串流並覆寫模式開啟檔案
                {
                    stream.WriteLine("PropertyName,Value"); // ⇦ 表頭列：屬性名稱與對應值

                    var props = data.GetType().GetProperties(); // ⇦ 取得 data 物件的所有屬性（public）

                    foreach (var prop in props)
                    {
                        object value = prop.GetValue(data); // ⇦ 取出屬性值

                        if (value == null)
                        {
                            stream.WriteLine($"{prop.Name},null"); // ⇦ 若為 null 則記錄屬性名稱與 null 字串
                        }
                    }

                    var fields = data.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public); // ⇦ 若你還要抓取欄位（不是屬性）

                    stream.WriteLine("FieldName,Value"); // ⇦ 如果你只抓欄位（Fields）

                    foreach (var field in fields)
                    {
                        object value = field.GetValue(data); // ⇦ 取出欄位值

                        if (value == null)
                        {
                            stream.WriteLine($"{field.Name},null"); // ⇦ 若欄位為 null 則記錄
                        }
                    }
                }
            }
            // Evaluate total number of feature
            int count = data._DDS_RowCount;
            _NbrOfProcess_Total = 0;
            _NbrOfProcess_Done = 0;
            _NbrOfFeature_Total = 0;
            _NbrOfFeature_Done = 0;
            AfterSeconds = "";
            //pin_time_average.Clear();


            //解析NC檔
            if (!chk_RD_NCfile.Checked)
            {
                //RD手動模式
                _NbrOfFeature_Total = data._NewDrillFeatures.Length;

            }
            _NbrOfProcess_Total = _NbrOfFeature_Total + data._QueueCmds.Count;

            // Process parameters
            int[] axisIndexZ = { (int)MotionAxis.Z };
            int[] axisIndexXY = { (int)MotionAxis.X, (int)MotionAxis.Y };
            int[] axisAllIndexes = { (int)MotionAxis.X, (int)MotionAxis.Y, (int)MotionAxis.Z };
            double targetPositionX;
            double targetPositionY;
            double LP_Gain = _ConfigProcess._LaserParam_Convert.Gain;
            double LP_Offset = _ConfigProcess._LaserParam_Convert.Offset;
            double MP_Gain = _ConfigProcess._MotionParam_Convert.Gain;
            double MP_Offset = _ConfigProcess._MotionParam_Convert.Offset;
            bool enableWarmup = _ConfigProcess._IsAutoProcessWarmup;
            bool enableSuspend = _ConfigProcess._IsAutoProcessSuspend;
            bool enableHold = _ConfigProcess._IsAutoProcessHold;
            bool enblePowerCheck = _ConfigSystem._ConnectSetting.Powermeter;
            double PowerCheck_USL = _ConfigSystem._AutoPowerCheck_USL;
            double PowerCheck_LSL = _ConfigSystem._AutoPowerCheck_LSL;
            double PowerCheck_value = _ConfigSystem._AutoPowerCheck_value;
            int AutoPowerCheck_count = 0;

            string Before_AutoPowerCheck_jobxfilename;

            double Before_AutoPowerCheck_ZPosition = 0;


            double PowerCheck_Ofs = 0;

            int warmup_TimeToWait = _ConfigProcess._Warmup_TimeToWait * 1000;   // Convert sec to ms
            int suspend_IntervalTime = _ConfigProcess._Suspend_IntervalTime * 1000;   // Convert sec to ms
            int hold_IntervalTime = _ConfigProcess._Hold_IntervalTime * 1000;   // Convert sec to ms
            int suspend_TimeToWait = _ConfigProcess._Suspend_TimeToWait * 1000;   // Convert sec to ms
            int hold_TimeToWait = _ConfigProcess._Hold_TimeToWait * 1000;   // Convert sec to ms

            int retrycount = 0;

            double laserPowerPct = 0;
            double CutSpeed = 0;
            double FastSpeed = 0;
            double _offsetZ = 0;
            double resultdistance = 0.0;
            double _OutputPower = 0.0;

            bool ProbeFinish = false;//微孔以外加工，g-code指令的測高 是否測高完成

            bool LinearZFinish = false;




            Stopwatch stopwatch_Warmup = new Stopwatch();
            Stopwatch stopwatch_Suspend = new Stopwatch();
            Stopwatch stopwatch_Hold = new Stopwatch();
            //double lastPosition_X, lastPosition_Y, lastPosition_Z;

            _process_Offset_X = _ConfigEquip._OrgPoint.X - _ConfigEquip._RelPosition_Laser2Camera.X;
            _process_Offset_Y = _ConfigEquip._OrgPoint.Y - _ConfigEquip._RelPosition_Laser2Camera.Y;


            // Initialization process
            // IO display flag
            _isProcessing = true;


            // Start Process Elapsed Time
            _stopwatch_ElapsedTime.Start();

            // ProcessGas ON 開啟氮氣
            //enableProcessGas(true);
            string code;
            if (!_isSysSimulated)
            {
                // Open Vacuum 開啟真空
                enableChuckVacuum(true);



                // 0. Initial setup
                // G00:Rapid position
                // G90: Absolute mode
                // G21: Units selection = millimeter
                // G17: XY plane
                code = "G00 G90 G17";    //G21 failed???
                executeGCode(code);
                Thread.Sleep(100);

                //[BM:暖機流程]
                if (enableWarmup) // Check Warm-up or not 加工前暖機
                {
                    // Write Process
                    writeProcess("Step7", "加工流程暖機開始");

                    _Laser.ChangePPDivider(1);
                    Thread.Sleep(100);

                    // Change Laser power
                    //雷射功率補償功能開啟
                    //if (enblePowerCheck)
                    //    laserPowerPct = laserParamConvert(data._LP_PowerPct[data._Op_LaserParamSet[0]], LP_Gain, LP_Offset);
                    //else
                        laserPowerPct = 2;

                    // Set Laser Output Power
                    SetLaserPowerPercentage(laserPowerPct);

                    // Change LaserScanner File 讀取振鏡DryRunJob.jobx檔案
                    try
                    {
                        //雷射功率補償功能開啟
                        //if (enblePowerCheck)
                        //  uploadJob("Powermeter");
                        //else
                            uploadJob("DryRunJob");
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("載入刀具錯誤，請確認是否有這項刀具。", "Error！！");
                        e.Cancel = true;
                        return;
                    }

                    // Move to PowerMonitor position                
                    linearMove(_ConfigEquip._Position_PowerMonitor.X, _ConfigEquip._Position_PowerMonitor.Y, 50);

                    // Wait in position
                    waitMotionDone(axisIndexXY, 10000);

                    btn_Move_Position_PowerMonitor_Click(this, null);

                    // Wait in position
                    waitMotionDone(axisIndexXY, 10000);

                    // Do Laser cutting
                    DoLaserCutting();

                    // Start stopwatch
                    stopwatch_Warmup.Start();

                    while (stopwatch_Warmup.ElapsedMilliseconds < warmup_TimeToWait)//讓機器暖機一段時間，暖機同時又能隨時被取消
                    {
                        // Check cancel
                        if (BW_AutoProcess.CancellationPending)//外部請求 停止加工流程(執行續若有遇到檢查點時才會停止)
                        {
                            e.Cancel = true;
                            return;
                        }
                        Thread.Sleep(1000);
                    }


                    // Write Process
                    writeProcess("Step7", "加工流程暖機結束");

                    laserPowerPct = laserParamConvert(data._LP_PowerPct[data._Op_LaserParamSet[0]], LP_Gain, LP_Offset);//暖機結束，更新要加工用的雷射百分比，功率檢測要用

                }
                else
                {
                    // 不論是否暖機，都先算出 zone 0 的實際加工功率(含 gain/offset)
                    laserPowerPct = laserParamConvert(data._LP_PowerPct[data._Op_LaserParamSet[0]], LP_Gain, LP_Offset);
                }
                // Disable UI control
                enableUIGroup(false);

                //加工前確認雷射功率
                if (enblePowerCheck)
                {
                    // Write Process
                    writeProcess("Step7", "雷射功率檢查開始");








                    this.moveAbs((int)MotionAxis.Z, _ConfigEquip._Position_PowerMonitor.Z, _ConfigEquip._Velocity_Default.Z);

                    // Wait for Motion completed
                    waitMotionDone(axisIndexZ, 10000);

                    //// 1. 清除 G92 偏移
                    //code = "G82";
                    //executeGCode(code);
                    //Thread.Sleep(100);

                    linearMove(_ConfigEquip._Position_PowerMonitor.X, _ConfigEquip._Position_PowerMonitor.Y, _ConfigProcess._Speed_Fast);

                    waitMotionDone(axisAllIndexes, 10000);


                    if (RunPowerCheck(laserPowerPct) == false)
                    {
                        MessageBox.Show($"功率量測異常，請查看ErrorLog");
                        e.Cancel = true;//主動異常結束背景執行續
                        return;
                    }



                    //暖機功能未開啟
                    //if (!enableWarmup)
                    //{
                    //    _Laser.ChangePPDivider(1);
                    //    Thread.Sleep(100);

                    //    // Change Laser power
                    //    laserPowerPct = laserParamConvert(data._LP_PowerPct[data._Op_LaserParamSet[0]], LP_Gain, LP_Offset);

                    //    // Set Laser Output Power
                    //    SetLaserPowerPercentage(laserPowerPct);

                    //    // Change LaserScanner File 讀取振鏡DryRunJob.jobx檔案
                    //    try
                    //    {
                    //        uploadJob("Powermeter");
                    //    }
                    //    catch (Exception)
                    //    {
                    //        MessageBox.Show("載入刀具錯誤，請確認是否有這項刀具。", "Error！！");
                    //        e.Cancel = true;
                    //        return;
                    //    }

                    //    // Move to PowerMonitor position

                    //    linearMove(_ConfigEquip._Position_PowerMonitor.X, _ConfigEquip._Position_PowerMonitor.Y, 50);

                    //    // Wait in position
                    //    waitMotionDone(axisIndexXY, 10000);

                    //    btn_Move_Position_PowerMonitor_Click(this, null);

                    //    // Wait in position
                    //    waitMotionDone(axisIndexXY, 10000);

                    //    // Do Laser cutting
                    //    DoLaserCutting();

                    //    // Disable UI control
                    //    enableUIGroup(false);

                    //    // Start stopwatch
                    //    stopwatch_Warmup.Start();
                    //}

                    ////取得雷射功率(W)
                    //_isPowermeterBusy = true;
                    //_OutputPower = _Powermeter.measurementStr;    // Convert Unit from W              
                    //_isPowermeterBusy = false;


                    ////功率量測實際值小於設定值進行功率補償(W)  
                    //while (_OutputPower < PowerCheck_value)
                    //{
                    //    if (laserPowerPct <= PowerCheck_USL && laserPowerPct >= PowerCheck_LSL)
                    //    {
                    //        //換算功率補償% = 預設值(W) - 量測值(W) / 0.06W(大約功率1% =0.06W)
                    //        PowerCheck_Ofs = (PowerCheck_value - _OutputPower) / 0.06;
                    //        //四捨五入為整數
                    //        PowerCheck_Ofs = Math.Round(PowerCheck_Ofs, 1);
                    //        //雷射功率補償
                    //        laserPowerPct = Math.Round(laserPowerPct + PowerCheck_Ofs, 1);
                    //        // Set Laser Output Power
                    //        SetLaserPowerPercentage(laserPowerPct);

                    //        Thread.Sleep(5000);

                    //        //功率計取得雷射功率(W)
                    //        _OutputPower = _Powermeter.measurementStr;    // Convert Unit from W

                    //        // Write Log
                    //        writeEvent("AutoPowerCheck", "補償完雷射功率(W)： " + _OutputPower.ToString());
                    //    }
                    //    else
                    //    {
                    //        //Stop Laser cutting
                    //        StopLaserCutting();
                    //        MessageBox.Show("雷射功率補償異常，補償完後超出上下限，請確認雷射功率。", "Error！！");
                    //        e.Cancel = true; //主動異常結束背景執行續
                    //        return;
                    //    }

                    //    // Check cancel
                    //    if (BW_AutoProcess.CancellationPending)//外部請求 停止加工流程(執行續若有遇到檢查點時才會停止)
                    //    {
                    //        e.Cancel = true;//主動異常結束背景執行續
                    //        return;
                    //    }
                    //    Thread.Sleep(1000);
                    //}

                    //if (laserPowerPct >= PowerCheck_USL || laserPowerPct <= PowerCheck_LSL)
                    //{
                    //    //Stop Laser cutting
                    //    StopLaserCutting();
                    //    MessageBox.Show("雷射功率補償異常，補償完後超出上下限，請確認雷射功率。", "Error！！");
                    //    e.Cancel = true;
                    //    return;
                    //}

                    // Write Process
                    writeProcess("Step7", "雷射功率檢查結束");
                }

              

                // Set software origin based on offset
                //小數點後四位四捨五入
                targetPositionX = Math.Round(_process_Offset_X, 4);
                targetPositionY = Math.Round(_process_Offset_Y, 4);

                // Move to target position
                linearMove(targetPositionX, targetPositionY, _ConfigEquip._Velocity_Default.X);

                // Wait in position
                waitMotionDone(axisIndexXY, 10000);


                if (!AlignmentProcess_flag)//若不是則代表為正常LCH流程
                {
                    //[BM:G-CODE-將目前位置設定為軟體原點（偏移量）]
                    // Set current position as Software origin (Offset)
                    code = "G92 X0 Y0";
                    executeGCode(code);
                    Thread.Sleep(100);
                }







                #endregion
            }
            /*持續加工測試，此迴圈會一直無限重複微孔加工及微孔以外的加工事項*/
            //bool keep = true;
            //while (keep == true)
            //{



            #region Feature Process

            // [Feature process]
            if (_NbrOfFeature_Total == 0)
            {
                //解析NC檔 RD手動模式
                if (!chk_RD_NCfile.Checked)
                {
                    // Write Log
                    writeEvent("自動流程", "加工微孔略過");
                    // Write Process
                    writeProcess("Step7", "加工流程微孔略過 (總數 = 0)");

                    //[BM:自訂Log紀錄內容-手動研發]
                    if (_AutoProcessLog_flag)
                        writeEventLog_Automatic_processing("加工流程微孔會被略過", $"研發手動NC檔已被勾選");

                    Microhole_Total = 0;
                }
            }

            //[BM:斷線從連復工-若是於微孔以外的地方當機重連回來 要跳過微孔加工]
            else if (reconnect_state == 1 && Processing_projects == 1)//有重連且是在做微孔以外加工
            {
                // Write Log
                writeEvent("自動流程", "加工微孔略過(已加工完成振鏡斷線重連進行加工)");
                // Write Process
                writeProcess("Step7", "加工流程微孔略過 (總數 = 0)");
                //[BM:自訂Log紀錄內容-手動研發]
                if (_AutoProcessLog_flag)
                    writeEventLog_Automatic_processing("加工流程微孔會被略過", $"已加工完成振鏡斷線重連進行加工");

                Microhole_Total = 0;

            }
            else if (Continuous_Processing_flag)
            {
                if(Processing_projects == 1)
                {
                    // Write Log
                    writeEvent("接續自動流程", "微孔加工已完成而略過");
                    // Write Process
                    writeProcess("Step7", "加工流程微孔略過 (總數 = 0)");
                    //[BM:自訂Log紀錄內容-手動研發]
                    if (_AutoProcessLog_flag)
                        writeEventLog_Automatic_processing("微孔加工已完成而略過", $"接續自動流程");

                    Microhole_Total = 0;

                }
            }
            else
            {
                if (BW_AutoProcess.CancellationPending) //外部請求 停止加工流程(執行續若有遇到檢查點時才會停止)
                {
                    e.Cancel = true;//主動異常結束背景執行續
                    return;
                }
                writeEvent("自動流程", "微孔加工開始");    // Write Log
                writeProcess("Step7", "加工流程微孔開始 (總數 = " + _NbrOfFeature_Total + ")");  // Write Process

                Microhole_Total = _NbrOfFeature_Total;


                //[BM:自訂Log紀錄內容-微孔加工開始]
                if (_AutoProcessLog_flag)
                    writeEventLog_Automatic_processing("加工流程微孔開始", $"總數 =  ({_NbrOfFeature_Total  })");

                // Start stopwatch
                stopwatch_Suspend.Start();
                stopwatch_Hold.Start();

                // Start processing
                double featureX = 0;
                double featureY = 0;
                int zoneType = -1;
                int LaserRunCount = 0; //雷射休息計數
                //[BM:斷線從連復工-建立變數，使微孔加工時可以從上次當機的位置開始加工]
                int Start_Hole = 0; //開始微孔加工的起始筆數




                //[BM:斷線從連復工-如果有從新連線或是接續加工，給予斷線前加工的微孔加工序號]
                if (Processing_projects == 0 && (reconnect_state == 1 || Continuous_Processing_flag)) 
                {
                    _NbrOfProcess_Done = Convert.ToInt16(lb_NbrOfProcess_Done.Text);
                    Start_Hole = reconnect_state_micropores_count;
                    if (_AutoProcessLog_flag)
                        writeEventLog_Automatic_processing("接續開始加工", $"從第{Start_Hole}筆微孔開始加工");
                }

                //================
                _updateTimer.Start();  // 啟動計時器
                Processing_projects = 0;//設定加工項目為微孔加工
                //================

                for (int i = Start_Hole; i < data._NewDrillFeatures.Length; i++)//微孔加工迴圈
                {




                    AutoPowerCheck_count++;//計數已加工之微孔
                    if (_ConfigSystem._AutoPowerCheck_count == AutoPowerCheck_count)//檢查是否到了要進行檢查的次數
                    {                                     /*第 i 筆迴圈開始
                                                        │
                                                        ├─ AutoPowerCheck_count++
                                                        │
                                                        ├─ [達到設定數量？]
                                                        │   ├─ 記錄目前 Z (_positionFeedback)，Z一直都是絕對座標 沒有被G92過
                                                        │   ├─ Z 退到 PowerMonitor 高度
                                                        │   ├─ G82 清除 G92 X0 Y0
                                                        │   ├─ XY 移到功率計位置

                                                        │   ├─ ===功率檢測===  
                                                                關氮氣輸出
                                                                開防塵蓋
                                                                清空功率值儲存之LIST
                                                                開始獲取功率值
                                                                載入功率量測刀具
                                                                開始出光
                                                                等待出光完成
                                                                停止獲取功率值
                                                                計算實際值與設定值
                                                                關閉防塵蓋
                                                                開啟氮氣
                                                             ─ ===功率檢測===  

                                                        │   ├─ XY 移回 _process_Offset_X/Y
                                                        │   ├─ waitMotionDone (XY)
                                                        │   ├─ G92 X0 Y0 重設原點
                                                        │   ├─ Z 恢復加工高度
                                                        │   └─ AutoPowerCheck_count = 0
                                                        │
                                                        ├─ ZoneType 判斷 / 測高補償
                                                        ├─ 移到 featureX, featureY
                                                        └─ DoLaserProcessing()
                                                                        */
                        Before_AutoPowerCheck_ZPosition = _positionFeedback[(int)MotionAxis.Z];//Convert.ToDouble(txt_Motion_Position_Z.Text);//紀錄前往功率量測前的z軸加工高度，量測完之後還要復原z軸高度使用

                        this.moveAbs((int)MotionAxis.Z, _ConfigEquip._Position_PowerMonitor.Z, _ConfigEquip._Velocity_Default.Z);

                        // Wait for Motion completed
                        waitMotionDone(axisIndexZ, 10000);

                        // 1. 清除 G92 偏移
                        code = "G82";
                        executeGCode(code);
                        Thread.Sleep(100);

                        linearMove(_ConfigEquip._Position_PowerMonitor.X, _ConfigEquip._Position_PowerMonitor.Y, _ConfigProcess._Speed_Fast);

                        waitMotionDone(axisAllIndexes, 10000);


                        if (RunPowerCheck(laserPowerPct) == false)
                        {
                            MessageBox.Show($"功率量測異常，請查看ErrorLog");
                            e.Cancel = true;//主動異常結束背景執行續
                            return;
                        }


                        linearMove(Math.Round(_process_Offset_X, 4), Math.Round(_process_Offset_Y, 4), _ConfigEquip._Velocity_Default.X);//移到第一次設定g92 X0 Y0的位置
                        waitMotionDone(axisIndexXY, 10000);

                        //[BM:G-CODE-將目前位置設定為軟體原點（偏移量）]
                        // Set current position as Software origin (Offset)
                        code = "G92 X0 Y0";
                        executeGCode(code);
                        Thread.Sleep(100);

                        this.moveAbs((int)MotionAxis.Z, Before_AutoPowerCheck_ZPosition, _ConfigEquip._Velocity_Default.Z);
                        // Wait for Motion completed
                        waitMotionDone(axisIndexZ, 10000);


                        uploadJob(data._DDS_ScanheadFilePath[zoneType]);//將原本加工的刀具載回去

                        AutoPowerCheck_count = 0;//檢查完歸零計數
                    }

                    //[BM:斷線從連復工-獲取當次迴圈加工到哪裡，並獲取此次迴圈開始時間]
                    //================
                    reconnect_state_micropores_count = i;//儲存當前的i值，復連之後從此加工
                    lastUpdateTime = DateTime.Now; // 更新最後變更時間
                    //================

                    // Check cancel
                    if (BW_AutoProcess.CancellationPending)//外部請求 停止加工流程(執行續若有遇到檢查點時才會停止)
                    {
                        e.Cancel = true;//主動異常結束背景執行續
                        return;
                    }

                    // Parse data
                    featureX = data._NewDrillFeatures[i].X;
                    featureY = data._NewDrillFeatures[i].Y;

                    //雷射自動補償Z軸
                    // If type changed, change Z compensate and power
                    if (zoneType != data._NewDrillFeatures[i].Type)
                    {
                        zoneType = data._NewDrillFeatures[i].Type;

                        // Change Laser power
                        laserPowerPct = laserParamConvert(data._LP_PowerPct[data._Op_LaserParamSet[zoneType]], LP_Gain, LP_Offset);
                        laserPowerPct = laserPowerPct + PowerCheck_Ofs;

                        //Change Laser Divider
                        //_Laser.ChangePPDivider(1);
                        _Laser.ChangePPDivider(data._DDS_LaserDivider[zoneType]);
                        Thread.Sleep(500);
                        var _status = _Laser.GetLaserStatus();
                        writeEvent("LaserDivider", _status.PpDivider);

                        //while (_status.PpDivider != "1")
                        while (_status.PpDivider != data._DDS_LaserDivider[zoneType].ToString())
                        {
                            retrycount++;

                            //_Laser.ChangePPDivider(1);
                            _Laser.ChangePPDivider(data._DDS_LaserDivider[zoneType]);
                            Thread.Sleep(500);

                            _status = _Laser.GetLaserStatus();

                            writeEvent("LaserDivider", _status.PpDivider + "; retrycount " + retrycount);
                            writeEvent("LaserOutputPower", _status.OutputPower + "; retrycount " + retrycount);
                            writeEvent("LaserOutputFrequency", _status.OutputFrequency + "; retrycount " + retrycount);



                            if (retrycount > 50)
                            {
                                MessageBox.Show("微孔 LaserDivider設定異常，請確認雷射振鏡。", "Error！！");
                                e.Cancel = true;
                                return;
                            }
                        }
                        retrycount = 0;

                        // Set Laser Output Power
                        //CheckAndSetLaserPower(laserPowerPct);
                        SetLaserPowerPercentage(laserPowerPct);

                        //MES LOG記錄
                        _CHPT_MES.Laser_Power = laserPowerPct.ToString();




                        //var result = Percentage_conversion_W(laserPowerPct);
                        //if (result.Item1 != 0 && result.Item2 != 0)
                        //{
                        //    W = result.Item1;
                        //    SET_W = result.Item2;
                        //}
                        //else
                        //{
                        //    W = 0;
                        //    SET_W = 0;
                        //}




                        //MES LOG微孔總數記錄
                        _CHPT_MES.Total_Hole = _NbrOfFeature_Total;

                        // Change LaserScanner File 讀取刀具檔案
                        try
                        {
                            uploadJob(data._DDS_ScanheadFilePath[zoneType]);

                            Before_AutoPowerCheck_jobxfilename= data._DDS_ScanheadFilePath[zoneType];
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("載入刀具錯誤，請確認是否有這項刀具。", "Error！！");
                            e.Cancel = true;
                            return;
                        }

                        // Change Motion param
                        CutSpeed = data._MP_CutSpeed[data._Op_MotionParamSet[zoneType]];
                        FastSpeed = data._MP_FastSpeed[data._Op_MotionParamSet[zoneType]];

                        // Check ZProbe or not 測高補償雷射Z軸
                        if (data._DDS_ZProbe[zoneType])
                        {
                            // Set Positioning to Absolute Mode
                            code = "G90";
                            executeGCode(code);
                            Thread.Sleep(100);

                            this.moveAbs((int)MotionAxis.Z, _ConfigEquip._Position_ConfocalLaser.Z, _ConfigEquip._Velocity_Default.Z);

                            // Wait for Motion completed
                            waitMotionDone(axisIndexZ, 10000);

                            targetPositionX = data._DDS_ZprobePositionX[zoneType] + _ConfigEquip._RelPosition_Laser2ConfocalLaser.X;
                            targetPositionY = data._DDS_ZprobePositionY[zoneType] + _ConfigEquip._RelPosition_Laser2ConfocalLaser.Y;

                            // Wait in position
                            waitMotionDone(axisIndexXY, 10000);

                            // Move to target position
                            double speed = _ConfigProcess._Speed_Fast;
                            linearMove(targetPositionX, targetPositionY, speed);

                            // Wait in position
                            waitMotionDone(axisIndexXY, 10000);


                            //微孔加工數大於設定數量，雷射休息
                            if (_ConfigProcess._IsAutoLaserSleep)
                            {
                                if (LaserRunCount >= _ConfigProcess._LaserSleep_Count)
                                {
                                    Thread.Sleep(_ConfigProcess._LaserSleep_Time * 1000);
                                    LaserRunCount = 0;
                                }
                            }

                            // Get the CompensatePosition from ConfocalLaser
                            try
                            {
                                // Wait for Motion completed
                                waitMotionDone(axisIndexZ, 10000);
                                Thread.Sleep(50);
                                _CompensatePosition[zoneType] = getCompesatePosition();

                                //測高比對-加工中測高紀錄                               
                                AutoThickProcess[zoneType] = Math.Round(_positionFeedback[(int)MotionAxis.Z] - _CompensatePosition[zoneType] + _ConfigEquip._Height_Equip.Laser, 4);

                            }
                            catch (Exception)
                            {
                                MessageBox.Show("測厚異常，請確認是否已擺放樣品並完成光學對焦。", "Error！！");
                                e.Cancel = true;
                                return;
                            }

                            //[BM:測高加工前、後比對功能]
                            if (_ConfigProcess._IsThickComparison)
                            {

                                double _ThickComparison = Math.Round(ThickProcess[zoneType] - AutoThickProcess[zoneType], 4);
                                double max = +_ConfigProcess._ThickComparison;
                                double min = -_ConfigProcess._ThickComparison;

                                writeEvent("加工前測高No." + (zoneType + 1).ToString(), ThickProcess[zoneType].ToString());
                                writeEvent("加工中測高No." + (zoneType + 1).ToString(), AutoThickProcess[zoneType].ToString());
                                writeEvent("測高差值No." + (zoneType + 1).ToString(), _ThickComparison.ToString());

                                //[BM:測高高度限制]
                                //測高比對超出預設差值設備跳Err停止
                                if (_ThickComparison < min || _ThickComparison > max)
                                {
                                    writeError("測高比對超出預設值", "測高差值No." + (zoneType + 1).ToString() + "：" + _ThickComparison.ToString());

                                    targetPositionX = Math.Round(_ConfigProcess._Check_ThickLaserX - _process_Offset_X, 4);
                                    targetPositionY = Math.Round(_ConfigProcess._Check_ThickLaserY - _process_Offset_Y, 4);
                                    //移動到測高檢查基準點
                                    linearMove(targetPositionX, targetPositionY, speed);
                                    waitMotionDone(axisIndexXY, 100000);
                                    this.moveAbs((int)MotionAxis.Z, _ConfigProcess._Check_ThickLaserZ, _ConfigEquip._Velocity_Default.Z);
                                    waitMotionDone(axisIndexZ, 10000);

                                    //記錄基測高檢查基準點的測高數值
                                    try
                                    {
                                        Thread.Sleep(50);
                                        _CompensatePosition[zoneType] = getCompesatePosition();
                                        writeError("測高比對超出預設值", "測高檢查基準點： " + Math.Round(_positionFeedback[(int)MotionAxis.Z] - _CompensatePosition[zoneType] + _ConfigEquip._Height_Equip.Laser, 4).ToString());
                                    }
                                    catch (Exception)
                                    {
                                        MessageBox.Show("測高異常，請確認是否已擺放樣品並完成光學對焦。", "Error！！");
                                        e.Cancel = true;
                                        return;
                                    }

                                    //異常急停
                                    setEmergencyStopLock();

                                    e.Cancel = true;
                                    return;
                                }
                            }


                            // Change Compensate offsetZ
                            // Evaluate the offsetZ
                            _offsetZ = _CompensatePosition[zoneType] - _positionFeedback[(int)MotionAxis.Z];

                            //string formatOffsetZ = string.Format("{0:0.####}", offsetZ);
                            moveRel((int)MotionAxis.Z, _offsetZ, _ConfigEquip._Velocity_Default.Z);//依據需補償高度值進行移動

                            // Wait in position
                            waitMotionDone(axisIndexZ, 10000);
                        }

                        // Wait in position
                        waitMotionDone(axisIndexZ, 10000);
                    }

                    //targetPositionX = _process_Offset_X + featureX;
                    //targetPositionY = _process_Offset_Y + featureY;
                    targetPositionX = featureX;
                    targetPositionY = featureY;

                    // Wait in position
                    waitMotionDone(axisIndexXY, 10000);

                    // Move to target position
                    double fastSpeed = motionParamConvert(FastSpeed, MP_Gain, MP_Offset);
                    linearMove(targetPositionX, targetPositionY, fastSpeed);

                    // Wait in position
                    waitMotionDone(axisIndexXY, 10000);

                    //[BM:自訂Log紀錄內容-微孔加工-位置是否到達]
                    //if (_AutoProcessLog_flag)
                    //    writeEventLog_Automatic_processing($"第{i}比座標位置", $"X={targetPositionX},Y={targetPositionY}已確實到達");


                    //流程是否有取消訊號檢查點
                    if (BW_AutoProcess.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }

                    // Do Laser processing 雷射加工
                    try
                    {
                        MicroholeStartTime = DateTime.Now; // 微孔加工開始時間

                        DateTime now = DateTime.Now;

                        // bool result = DoLaserProcessing(); // 執行一次並取得結果
                        if (!DoLaserProcessing()) // 執行一次並取得回傳結果是true 或是false
                        {
                            //[BM:自訂Log紀錄內容-振鏡加工-是否成功(此處為失敗進行記錄)]
                            if (_AutoProcessLog_flag)
                                writeEventLog_Automatic_processing($"第{i+1}筆微孔雷射加工出問題", $"加工總數={data._NewDrillFeatures.Length}。問題內容:振鏡狀態錯誤 ");

                            DoLaserReconnect();//重新連線
                                               // setEmergencyStopLock();//即停
                                               //   return;//終止自動加工流程
                        }

                        //[BM:自訂Log紀錄內容-振鏡加工-是否成功(此處為成功進行記錄)]
                        if (_AutoProcessLog_flag && (now - _lastAutoProcessLogTime).TotalSeconds >= 1.0)//每個微孔加工完成時間至少大於一秒
                        {

                            writeEventLog_Automatic_processing($"第{i+1}筆微孔雷射加工完成", $"加工總數={data._NewDrillFeatures.Length}");

                            Microhole_Done = i + 1;

                            Microhole_Remain = Microhole_Total - Microhole_Done;

                            MicroholeEndTime = DateTime.Now; // 微孔加工結束時間

                            double holeSeconds = (MicroholeEndTime - MicroholeStartTime).TotalSeconds; // 方向正確
                            if (holeSeconds > 0)                 // 過濾負值/異常
                                Microhole_time_average.Add(holeSeconds);

                        }
                        else
                        {

                            writeEventLog_Automatic_processing($"第{i+1}筆微孔雷射加工異常", $"加工總數={data._NewDrillFeatures.Length}。問題內容:微孔之間加工時間過短(振鏡當機)");
                            //[BM:斷線從連復工-進行重新連線]
                            DoLaserReconnect();//重新連線
                        }
                    }
                    catch (Exception ex)
                    {
                        //[BM:自訂Log紀錄內容-振鏡加工-(例外錯誤)]
                        if (_AutoProcessLog_flag)
                            writeEventLog_Automatic_processing($"第{i+1}筆微孔雷射加工出問題", $"加工總數={data._NewDrillFeatures.Length}。問題內容:{ex} ");
                        throw ex;
                    }


                 


                    // Report progress
                    BW_AutoProcess.ReportProgress(0);

                    // Check Suspend or not
                    if (enableSuspend)
                    {
                        if (stopwatch_Suspend.ElapsedMilliseconds > suspend_IntervalTime)
                        {
                            //[BM:自訂Log紀錄內容-微孔加工-有正常執行暫停]
                            if (_AutoProcessLog_flag)
                                writeEventLog_Automatic_processing($"第{i+1}筆加工流程完成後有執行暫停", $" ");
                            // Write Data log
                            writeDataQueue("AutoProcess", "Suspend(ms) = " + suspend_TimeToWait);
                            stopwatch_Suspend.Restart();

                            while (stopwatch_Suspend.ElapsedMilliseconds < suspend_TimeToWait)
                            {
                                // Check cancel
                                if (BW_AutoProcess.CancellationPending)//外部請求 停止加工流程(執行續若有遇到檢查點時才會停止)
                                {
                                    e.Cancel = true;//主動異常結束背景執行續
                                    return;
                                }
                                Thread.Sleep(1000);
                            }
                            stopwatch_Suspend.Restart();
                        }
                    }

                    // Check Hold or not
                    if (enableHold)
                    {
                        if (stopwatch_Hold.ElapsedMilliseconds > hold_IntervalTime)
                        {
                            // Write Data log
                            writeDataQueue("AutoProcess", "Calibrate Power = " + laserPowerPct); ;

                            //// Check and set laser power
                            //CheckAndSetLaserPower(laserPowerPct);

                            // Re-upload JobList
                            // Evaluate the offsetZ
                            double offsetZ = _CompensatePosition[zoneType] - _positionFeedback[(int)MotionAxis.Z];
                            string formatOffsetZ = string.Format("{0:0.####}", offsetZ);
                            double.TryParse(formatOffsetZ, out offsetZ);

                            // Upload JobList with Compensate offsetZ by changing JobList
                            string fileName = data._DDS_ScanheadFilePath[zoneType];
                            uploadJobListWithCompensateZ(fileName, offsetZ);

                            stopwatch_Hold.Restart();
                        }
                    }

                    //流程是否有取消訊號檢查點
                    if (BW_AutoProcess.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }
                }





















                // Stop stopwatch
                stopwatch_Warmup.Stop();
                stopwatch_Suspend.Stop();
                stopwatch_Hold.Stop();

                //Write Log
                writeEvent("自動加工流程", "微孔加工結束");

                // Write Process
                writeProcess("Step7", "加工流程微孔結束");

                //[BM:斷線從連復工-微孔加工完成後 將是否有重連的狀態設為沒有重連]
                reconnect_state = 0;


            }
            #endregion

            #region Else Process

            Queue<QueueCmd> queueCmds;
            //[BM:斷線從連復工-如果有從新連線或接續加工，給與斷線前還未加工的內容]
            if (Processing_projects == 1 &&  (reconnect_state == 1 || Continuous_Processing_flag))
            {
                queueCmds = new Queue<QueueCmd>(reconnect_state_queueCmds);
                _NbrOfProcess_Done = Convert.ToInt16(lb_NbrOfProcess_Done.Text);
            }
            else
            {
                queueCmds = new Queue<QueueCmd>(data._QueueCmds); //正常流程都沒斷線會執行這邊
            }
            string jobName = "";
            double setLaserPower = 0;
            int setLaserDivider = 0;
            //ControllerDiagPacket ctrlDiagPacket = _MC_Controller.DataCollection.RetrieveDiagnostics();
            int opType = -1;
            Processing_projects = 1;//設定加工項目為其餘的加工
            while (queueCmds.Count != 0)
            {
                // Check cancel
                if (BW_AutoProcess.CancellationPending)//外部請求 停止加工流程(執行續若有遇到檢查點時才會停止)
                {
                    e.Cancel = true;//主動異常結束背景執行續
                    return;
                }
                //================
                //[BM:斷線從連復工-儲存斷線前的加工命令]
                reconnect_state_queueCmds = queueCmds;
             
                lastUpdateTime = DateTime.Now; // 更新最後變更時間
                //================

                //[BM:微孔外之命令排程(依次取出命令使用即消失1條命令)]
                QueueCmd cmd = queueCmds.Dequeue();

                switch (cmd.CmdModule)
                {
                    case NCScriptParser.Module.Annotation:
                        switch (cmd.Command)
                        {
                            case AnnotCmd.Operation:
                                if (!_isSysSimulated)
                                {

                                    logAnnotOperation((string)cmd.Arguments);
                                    opType = (int)cmd.Else;

                                    // Write Data log
                                    writeDataQueue("AnnotCmd", "opType = " + opType);
                                }
                                break;
                        }
                        break;

                    case NCScriptParser.Module.Laser:
                        switch (cmd.Command)
                        {
                            case LaserCmd.SetLaserPower:
                                if (!_isSysSimulated)
                                {

                                    // Parse Laser power
                                    setLaserPower = (int)cmd.Arguments;
                                    setLaserPower = laserParamConvert(setLaserPower, LP_Gain, LP_Offset);
                                    // If Laser power changed, do process
                                    if (laserPowerPct != setLaserPower)
                                    {
                                        laserPowerPct = setLaserPower;
                                        // Set Laser Output Power
                                        //CheckAndSetLaserPower(laserPowerPct);
                                        SetLaserPowerPercentage(laserPowerPct);

                                        //MES LOG記錄
                                        _CHPT_MES.Laser_Power = laserPowerPct.ToString();
                                        // Reupload the JobFile
                                        //uploadJob(jobName);

                                        // Write Data log
                                        writeDataQueue("LaserCmd", "setLaserPower = " + setLaserPower);
                                    }
                                }
                                break;

                            case LaserCmd.SetLaserDivider:
                                if (!_isSysSimulated)
                                {

                                    //Change Laser Divider
                                    //_Laser.ChangePPDivider(1);
                                    setLaserDivider = (int)cmd.Arguments;
                                    _Laser.ChangePPDivider(setLaserDivider);
                                    Thread.Sleep(500);
                                    var _otherstatus = _Laser.GetLaserStatus();
                                    writeEvent("LaserDivider", _otherstatus.PpDivider);

                                    while (_otherstatus.PpDivider != setLaserDivider.ToString())
                                    {
                                        retrycount++;

                                        //_Laser.ChangePPDivider(1);
                                        _Laser.ChangePPDivider(setLaserDivider);
                                        Thread.Sleep(500);
                                        _otherstatus = _Laser.GetLaserStatus();
                                        writeEvent("LaserDivider", _otherstatus.PpDivider + "; retrycount " + retrycount);

                                        if (retrycount > 50)
                                        {
                                            MessageBox.Show("GH LaserDivider設定異常，請確認雷射振鏡。", "Error！！");
                                            e.Cancel = true;
                                            return;
                                        }
                                    }
                                    retrycount = 0;
                                }
                                break;
                        }
                        break;

                    case NCScriptParser.Module.Scanner:
                        if (!_isSysSimulated)
                        {

                            switch (cmd.Command)
                            {
                                case ScannerCmd.SelectJobByName:

                                    jobName = (string)cmd.Arguments;
                                    uploadJob(jobName);

                                    // Write Data log
                                    writeDataQueue("ScannerCmd", "uploadJob = " + jobName);
                                    break;

                                case ScannerCmd.GasOnOff:
                                    bool flag = (bool)cmd.Arguments;
                                    enableProcessGas(flag);

                                    // Write Data log
                                    writeDataQueue("ScannerCmd", "enableProcessGas = " + flag);
                                    break;

                                case ScannerCmd.StartJob:
                                    DoLaserCutting();

                                    // Write Data log
                                    writeDataQueue("ScannerCmd", "DoLaserCutting");
                                    break;

                                case ScannerCmd.AbortJob:
                                    StopLaserCutting();

                                    //MES LOG微孔總數+其他孔總數記錄
                                    _CHPT_MES.Total_Hole++;

                                    // Write Data log
                                    writeDataQueue("ScannerCmd", "StopLaserCutting");
                                    break;

                                //研發手動NC檔 打微孔模式
                                case ScannerCmd.ExecuteJob:
                                    try
                                    {
                                        // Do Laser processing 雷射開啟
                                        DoLaserProcessing();

                                        // Write Data log
                                        writeDataQueue("ScannerCmd", "DoLaserProcessing");
                                    }
                                    catch (Exception ex)
                                    {
                                        throw ex;
                                    };
                                    break;
                            }
                        }
                        break;

                    case NCScriptParser.Module.Equipment:
                        if (!_isSysSimulated)
                        {

                            switch (cmd.Command)
                            {
                                case EquipCmd.VacuumOnOff:
                                    bool flag = (bool)cmd.Arguments;
                                    //[BM:不放開吸附功能(所有程式只有UI介面的取消吸附才可以解除吸附)]
                                    // enableChuckVacuum(flag);
                                    // Write Data log
                                    // writeDataQueue("EquipCmd", "enableChuckVacuum = " + flag);
                                    break;
                            }
                        }
                        break;

                    //測高
                    case NCScriptParser.Module.Probe:
                        switch (cmd.Command)
                        {
                            case ProbeCmd.ProbeSurface:
                                //// Get current position command X, Y
                                ////double posCmdX = ctrlDiagPacket[(int)MotionAxis.X].PositionCommand;
                                ////double posCmdY = ctrlDiagPacket[(int)MotionAxis.Y].PositionCommand;
                                ////double speed = data._MP_FastSpeed[data._Op_MotionParamSet[opType]];
                                ////speed = motionParamConvert(speed, MP_Gain, MP_Offset);
                                //// Profe surface and upload the JobFile
                                ////probeSurfaceAndUploadCompensateJobList(posCmdX, posCmdY, speed, jobName);

                                // Probe surface and compensate the difference
                                try
                                {
                                    if (!_isSysSimulated)
                                    {

                                        probeSurfaceAndCompensateDifference(out resultdistance);

                                        AutoThickProcess_Video_Count++;

                                        ProbeFinish = true;  //微孔以外加工，g-code指令的測高 是否測高完成
                                    }
                                }
                                catch (Exception)
                                {
                                    MessageBox.Show("測厚異常，請確認是否已擺放樣品並完成光學對焦。", "Error！！");
                                    e.Cancel = true;
                                    return;
                                }

                                // Write Data log
                                writeDataQueue("ProbeCmd", "probeSurfaceAndCompensateDifference");
                                break;
                        }
                        break;

                    case NCScriptParser.Module.GCode:
                        switch (cmd.Command)
                        {
                            case GCodeCmd.Set:
                                if (!_isSysSimulated)
                                {
                                    //[BM:G-code-輸入G-code]
                                    code = (string)cmd.Arguments;
                                    executeGCode(code);
                                    Thread.Sleep(100);

                                }
                                break;

                            //Z軸下降
                            case GCodeCmd.LinearZ2ProcessPlane:
                                if (!_isSysSimulated)
                                {

                                    //測高完成 或 Z軸上升完成
                                    if (ProbeFinish || LinearZFinish)
                                    {
                                        // Due to Gantry instable issues, abandon GCode related to axis-Z
                                        code = "G91";
                                        executeGCode(code);
                                        Thread.Sleep(100);


                                        moveRel((int)MotionAxis.Z, resultdistance, _ConfigEquip._Velocity_Default.Z);


                                        // Wait for Motion completed
                                        waitMotionDone(axisIndexZ, 10000);

                                        // Write Data log
                                        writeDataQueue("LinearZ2ProcessPlane", "Move DownZ = " + code + " " + resultdistance.ToString());

                                        code = "G90";
                                        executeGCode(code);
                                        Thread.Sleep(100);

                                        ProbeFinish = false;
                                        LinearZFinish = false;
                                    }
                                }
                                break;

                            case GCodeCmd.CircleXY:

                            case GCodeCmd.LinearXY:
                                if (!_isSysSimulated)
                                {

                                    code = (string)cmd.Arguments;
                                    if (cmd.Else != null)
                                    {
                                        // Parse Motion speed
                                        double speed = (int)cmd.Else;
                                        speed = motionParamConvert(speed, MP_Gain, MP_Offset);
                                        // Replace GCode Motion speed
                                        Regex regex = new Regex(".*F");
                                        Match match = regex.Match(code);
                                        code = match.Value + string.Format("{0:0.###}", speed);
                                    }
                                    executeGCode(code);
                                    Thread.Sleep(100);

                                }
                                break;

                            //Z軸上升
                            case GCodeCmd.LinearZ:
                                if (!_isSysSimulated)
                                {

                                    //case GCodeCmd.RapidZ:
                                    // Due to Gantry instable issues, abandon GCode related to axis-Z
                                    // Otherwise, these cmds are the same as CircleXY & LinearXY
                                    code = "G90";
                                    executeGCode(code);
                                    Thread.Sleep(100);

                                    this.moveAbs((int)MotionAxis.Z, _ConfigEquip._Position_ConfocalLaser.Z, _ConfigEquip._Velocity_Default.Z);

                                    // Wait for Motion completed
                                    waitMotionDone(axisIndexZ, 10000);

                                    //Z軸上升完成
                                    LinearZFinish = true;

                                    // Write Data log
                                    writeDataQueue("LinearZ & RapidZ", "Move UpZ " + code);
                                }
                                break;
                        }
                        break;
                }
                // Report progress
                BW_AutoProcess.ReportProgress(0);
            }



            #endregion

          

            //} /*持續加工測試，此迴圈會一直無限重複微孔加工及微孔以外的加工事項(下括號)*/
        }

        private void BW_AutoProcess_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            int progressPercentage = ++_NbrOfProcess_Done * 100/ _NbrOfProcess_Total;
            lbl_AlignmentProcess_Step7.Text = lbl_Progress_Step7.Text = string.Format("Progress = {0}% ({1}/{2})", progressPercentage, _NbrOfProcess_Done, _NbrOfProcess_Total);
            progressBar_AlignmentProcess_Step7.Value=progressBar_AutoMode_Step7.Value = progressPercentage;
            Progress_Percentage = $"{progressPercentage}";
            lb_StartTime.Text = StartTime.ToString("yyyy-MM-dd HH:mm:ss");//更新開始時間顯示於lb_StartTime.Text



            if (_NbrOfProcess_Done > 0 && !string.IsNullOrEmpty(AfterSeconds))
            {
                double totalSeconds = Convert.ToDouble(AfterSeconds);
                double averageTimePerPiece = totalSeconds / _NbrOfProcess_Done;

                lb_PinTime.Text = averageTimePerPiece.ToString("F2");   //單位s 所有加工項目的平均時間

                lb_TimeR.Text = (averageTimePerPiece * (_NbrOfProcess_Total - _NbrOfProcess_Done) / 60).ToString("F2");//更新 剩餘時間UI.TEXT儲存計算結果 每項加工時間*未完成加工項目數量

                lb_EndTime.Text = DateTime.Now.AddMinutes(averageTimePerPiece * (_NbrOfProcess_Total - _NbrOfProcess_Done) / 60).ToString("yyyy-MM-dd HH:mm:ss");

                //全部加工計算
                lb_NbrOfProcess_Total.Text = _NbrOfProcess_Total.ToString();
                lb_NbrOfProcess_Done1.Text = _NbrOfProcess_Done.ToString();
                lb_NbrOfProcess_Remain.Text = (_NbrOfProcess_Total - _NbrOfProcess_Done).ToString();
                //微孔加工數計算
                lb_Microhole_Total.Text = Microhole_Total.ToString();//將微孔總數顯示在UI.TEXT介面上
                lb_Microhole_Done.Text = Microhole_Done.ToString();//將已加工微孔數顯示在UI.TEXT介面上
                lb_Microhole_Remain.Text = Microhole_Remain.ToString();//將已加工微孔數顯示在UI.TEXT介面上



                //上報需求，平均微孔加工時間，會將此資訊透過Pin_time欄位拋，原先的lb_PinTime.Text 顯示的是全部加工項目的平均時間。
                lb_Microhole_Time.Text = Microhole_time_average.Any()
       ? Microhole_time_average.Average().ToString("F2")
       : "0";

            }
        }

        private void BW_AutoProcess_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // IO display flag
            _isProcessing = false;


            //pin_time_average.Clear();


            // Deinitialization process
            // Stop Process Elapsed Time
            _stopwatch_ElapsedTime.Stop();
            _stopwatch_ElapsedTime.Reset();

            // Close Process Gas
            enableProcessGas(false);
            //[BM:不放開吸附功能(所有程式只有UI介面的取消吸附才可以解除吸附)]
            // Close Vacuum
            //enableChuckVacuum(false);

            // Clear software origin
            string code = "G82";
            executeGCode(code);
            Thread.Sleep(100);

            if (AlignmentProcess_flag)//清除旋轉角度
            {
                 code = "G84 X Y";
                executeGCode(code);
                Thread.Sleep(100);
                AlignmentProcess_flag = false;
            }

            //=======================================
            //[BM:斷線從連復工-關閉計時器]
            _updateTimer.Stop();
            //=======================================


            if (e.Cancelled == true)
            {
                // Cancelled
                updatePnlStepState(pnl_AutoMode_Process_Status, StepState.NO);

                // Write Process
                writeProcess("Step7", "加工流程已中斷");
            }
            else if (e.Error != null)
            {
                // Error
                updatePnlStepState(pnl_AutoMode_Process_Status, StepState.NO);
                writeError("自動流程", "自動過程失敗." + e.Error);

                // Write Process
                writeProcess("Step7", "錯誤發生致加工流程已中斷");

                //MES 故障狀態
                if (_ConfigSystem._ConnectSetting.ChptMES && SendErr_ToMES_Req)
                {
                    SendErr_ToMES_Req = false;
                    Description = "故障與維修";
                    //[BM:將機台資訊傳送給MES]

                    _CHPT_MES.Send_MES_Data(EquipmentNo, (int)EquipmentState.Error, UserNo, Description, IsEQPTrigger, _CHPT_MES.Batch_Number);
                }
            }
            else
            {
                // Completed
                updatePnlStepState(pnl_AutoMode_Process_Status, StepState.OK);

                progressBar_AlignmentProcess_Step7.Value=progressBar_AutoMode_Step7.Value = 100;

                //拍攝功能-開門關閉雷射二次加工計數
                AutoThickProcess_Video_Count = 0;

                // Write Process
                writeProcess("Step7", "加工流程已完成");

                // Enable GUI Controls
                //EnableGUIMotionControls(true);

                //[BM:防呆-自動流程完成後保持禁用按鍵]
                // Disable UI control
                enableUIGroup(true);

                //MES LOG記錄 結束時間
                _CHPT_MES.End();

                //MES 待料狀態
                //[BM:將機台資訊傳送給MES]
                if (_ConfigSystem._ConnectSetting.ChptMES)
                {
                    Description = "待料";
                    _CHPT_MES.Send_MES_Data(EquipmentNo, (int)EquipmentState.Waitmaterial, UserNo, Description, IsEQPTrigger, LotNo);
                }


                //[BM:斷線從連復工-所有加工流程完成重置狀態]
                Processing_projects = 0;//正常加工結束　將加工項目設為０下一次也是從微孔開始做
                reconnect_state = 0;//正常加工結束　將是否有重新連線的狀態為0
                reconnect_state_queueCmds.Clear();//正常加工結束　將微孔以外的加工命令全部清除
                reconnect_state_micropores_count = 0;//正常加工結束　將微孔最後加工筆數設成0
            }

            //===============儲存異常前之資訊

            Backup_Progress_Percentage = Progress_Percentage;
            Backup_NbrOfProcess_Done = _NbrOfProcess_Done;

            if (_CompensatePosition != null)
                Backup_CompensatePosition = (double[])_CompensatePosition.Clone();

            if (ThickProcess != null)
                Backup_ThickProcess = (double[])ThickProcess.Clone();   // 注意你的變數名稱

            if (AutoThickProcess != null)
                Backup_AutoThickProcess = (double[])AutoThickProcess.Clone();

            SaveThickBackupToLog();    
            //===============儲存異常前之資訊


            _NbrOfProcess_Done = 0;//流程結束清除 清除所有已完成加工項目
            _NbrOfProcess_Total = 0;//流程結束清除 清除所有加工項目總數

            AfterSeconds = "";//流程結束清除 開始加工後過多少時間
            SET_W = 0;//流程結束清除 設定之功率值(W)
            W = 0; //流程結束清除 實際功率值(W)
            UD_or_LD = "";//流程結束清除 當前是UD還是LD之層別
            Progress_Percentage = "";//流程結束清除 加工進度百分比

            Microhole_time_average.Clear();   //流程結束清除 微孔加工平均時間
            lb_Microhole_Time.Text = "0"; //流程結束清除 UI顯示微孔加工平均時間
            Microhole_Total = 0;  //流程結束清除 微孔加工總數
            Microhole_Done = 0;   //流程結束清除 已加工微孔數量
            Microhole_Remain = 0; //流程結束清除 未加工微孔數量
            Microhole_Time = "0"; //流程結束清除 UI顯示微孔平均加工時間
            lb_TimeR.Text = "0";//流程結束清除 UI顯示剩餘加工時間(分鐘包含小數點第二位)
            lb_EndTime.Text = "0";//流程結束清除 UI顯示預計結束時間
            lb_StartTime.Text = "0";//流程結束清除 UI顯示開始加工時間


            //NowNozzlePressure = 0;
            //NowNozzle_MAX_Pressure = 0;
            //NowNozzle_MIN_Pressure = 0;
            //NozzleValveSetValue = 0;
            //NozzleValveActValue = 0;


           
        }

        private void BW_ThickProcess_DoWork(object sender, DoWorkEventArgs e)
        {
            // IO display flag
            _isProcessing = true;


            NCScriptParser data = _formNCParser.data;
            _process_Offset_X = _ConfigEquip._OrgPoint.X - _ConfigEquip._RelPosition_Laser2Camera.X;
            _process_Offset_Y = _ConfigEquip._OrgPoint.Y - _ConfigEquip._RelPosition_Laser2Camera.Y;

            // Evaluate total number of Measurement
            int count = data._DDS_RowCount;
            for (int i = 0; i < count; i++)
            {
                // Check LoadData or not
                if (data._DDS_LoadData[i])
                {
                    // Check ZProbe or not
                    if (data._DDS_ZProbe[i])
                    {
                        _NbrOfThickProcess_Total++;
                        _nbrOfThickProcess_Total++;
                    }
                }
            }

            // Write Process
            writeProcess("Step6", "測高流程總數 = " + _NbrOfThickProcess_Total);

            // 0. Initial setup
            //G00:Rapid position
            //G90: Absolute mode
            //G21: Units selection = millimeter
            //G17: XY plane
            string code = "G00 G90 G17";    //G21 failed???
            executeGCode(code);
            Thread.Sleep(100);

            //executeGCode(code);
            this.moveAbs((int)MotionAxis.Z, _ConfigEquip._Position_ConfocalLaser.Z, _ConfigEquip._Velocity_Default.Z);
            int[] axisIndexZ = { (int)MotionAxis.Z };
            // Wait for Motion completed
            waitMotionDone(axisIndexZ, 10000);

            // Process parameters
            int[] axisIndexes = { (int)MotionAxis.X, (int)MotionAxis.Y };
            _ThickResult = new double[count];
            _CompensatePosition = new double[count];
            //測高加工前後比對功能
            ThickProcess = new double[count];
            AutoThickProcess = new double[count];

            // Start ThickProcess
            for (int i = 0; i < count; i++)
            {
                // Check LoadData or not
                if (data._DDS_LoadData[i])
                {
                    // Check cancel
                    if (BW_ThickProcess.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }

                    // Check ZProbe or not
                    if (data._DDS_ZProbe[i])
                    {
                        double targetPositionX = _process_Offset_X + data._DDS_ZprobePositionX[i] + _ConfigEquip._RelPosition_Laser2ConfocalLaser.X;
                        double targetPositionY = _process_Offset_Y + data._DDS_ZprobePositionY[i] + _ConfigEquip._RelPosition_Laser2ConfocalLaser.Y;

                        // Wait in position
                        waitMotionDone(axisIndexes, 10000);

                        // Move to target position
                        double speed = _ConfigProcess._Speed_Fast;
                        linearMove(targetPositionX, targetPositionY, speed);

                        // Wait in position
                        waitMotionDone(axisIndexes, 10000);

                        // Get the CompensatePosition from ConfocalLaser
                        try
                        {
                            _CompensatePosition[i] = getCompesatePosition();

                            //測高比對-加工前測高紀錄                            
                            ThickProcess[i] = Math.Round(_positionFeedback[(int)MotionAxis.Z] - _CompensatePosition[i] + _ConfigEquip._Height_Equip.Laser, 4);
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("測厚異常，請確認是否已擺放樣品並完成光學對焦。", "Error！！");
                            e.Cancel = true;
                            return;
                        }

                        // Write Log
                        string msg = string.Format("Get Compensate position[{0}] (z) = {1:0.###}", i, _CompensatePosition[i]);
                        writeDataQueue("ConfocalLaser", msg);

                        //進度條
                        progressPercentage = (++_NbrOfThickProcess_Done * 100) / _NbrOfThickProcess_Total;
                        updateControlTxtWithString(lbl_Progress_Step6, string.Format("Progress = {0}% ({1}/{2})", progressPercentage, _NbrOfThickProcess_Done, _NbrOfThickProcess_Total));
                        updateControlTxtWithString(lbl_AlignmentProcess_Step6, string.Format("Progress = {0}% ({1}/{2})", progressPercentage, _NbrOfThickProcess_Done, _NbrOfThickProcess_Total));

                    }

                    // Wait processing completed
                    BW_ThickProcess.ReportProgress(0);
                }
            }
        }

        private void BW_ThickProcess_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar_AlignmentProcess_Step6.Value = progressBar_AutoMode_Step6.Value = progressPercentage;
        }

        private void BW_ThickProcess_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // IO display flag
            _isProcessing = false;

            _NbrOfThickProcess_Done = 0;
            _NbrOfThickProcess_Total = 0;

            if (e.Cancelled == true)
            {
                // Cancelled
                updatePnlStepState(pnl_AutoMode_Thick_Status, StepState.NO);

                // Write Process
                writeProcess("Step6", "測高流程已中斷");
            }
            else if (e.Error != null)
            {
                // Error
                updatePnlStepState(pnl_AutoMode_Thick_Status, StepState.NO);
                writeError("自動流程", "測高過程失敗." + e.Error);

                // Write Process
                writeProcess("Step6", "錯誤發生致測高流程已中斷");
            }
            else
            {
                // Completed
                // Start Step Check Timer with checkAutoModeProgress
                autoMode_StartStepWithTimer(Step.s6);

                updatePnlStepState(pnl_AutoMode_Thick_Status, StepState.OK);

                UpdateDIPnlBackColor(pnl_AlignmentProcess_Thick_Status, true);
                // No need to check
                _stepTimers[(int)Step.s6].Set();

                progressBar_AlignmentProcess_Step6.Value = progressBar_AutoMode_Step6.Value = 100;


                // Update GUI Control
                btn_AutoMode_Thick_View.Enabled = true;

                // Write Process
                writeProcess("Step6", "測高流程已完成");
            }
        }

        private void probeSurfaceAndUploadCompensateJobList(double posCmdX, double posCmdY, double speed, string jobName)
        {
            // Define parameters
            int[] axisIndexes = { (int)MotionAxis.X, (int)MotionAxis.Y };

            // Move to ConfocalLaser point
            double targetPositionX = posCmdX + _ConfigEquip._RelPosition_Laser2ConfocalLaser.X;
            double targetPositionY = posCmdY + _ConfigEquip._RelPosition_Laser2ConfocalLaser.Y;

            // Wait in position
            waitMotionDone(axisIndexes, 10000);

            // Move to target position
            linearMove(targetPositionX, targetPositionY, speed);

            // Wait in position
            waitMotionDone(axisIndexes, 10000);

            // Get compensage position
            double compensatePosition = getCompesatePosition();

            // Evaluate the offsetZ
            double offsetZ = compensatePosition - _positionFeedback[(int)MotionAxis.Z];
            string formatOffsetZ = string.Format("{0:0.####}", offsetZ);
            double.TryParse(formatOffsetZ, out offsetZ);

            // Upload JobList with Compensate offsetZ by changing JobList
            uploadJobListWithCompensateZ(jobName, offsetZ);

            // Move back to initial point
            targetPositionX = posCmdX;
            targetPositionY = posCmdY;

            // Move to target position
            linearMove(targetPositionX, targetPositionY, speed);

            // Wait in position
            waitMotionDone(axisIndexes, 10000);
        }
        /// <summary>
        /// 先移動到共焦雷射測高儀位置 → 量測工件表面高度 → 計算與加工雷射的高度差 → 回到加工雷射位置 → 得到補償距離。
        /// </summary>
        /// <param name="_distance"></param>
        private void probeSurfaceAndCompensateDifference(out double _distance)
        {
            int[] axisIndexZ = new int[] { (int)MotionAxis.Z };
            int[] axisIndexXY = new int[] { (int)MotionAxis.X, (int)MotionAxis.Y };
            double[] speeds = new double[] { _ConfigEquip._Velocity_Default.X, _ConfigEquip._Velocity_Default.Y };
            double[] distances = new double[] { _ConfigEquip._RelPosition_Laser2ConfocalLaser.X, _ConfigEquip._RelPosition_Laser2ConfocalLaser.Y };

            // Set Positioning to Absolute Mode
            string code = "G90";
            executeGCode(code);
            Thread.Sleep(100);

            this.moveAbs((int)MotionAxis.Z, _ConfigEquip._Position_ConfocalLaser.Z, _ConfigEquip._Velocity_Default.Z);

            // Wait for Motion completed
            waitMotionDone(axisIndexZ, 10000);

            // Set Positioning to Relative Mode
            code = "G91";
            executeGCode(code);
            Thread.Sleep(100);

            // Move from Laser to ConfocalLaser
            // Do Motion Movement to specified target position
            this.rapidMove(axisIndexXY, distances, speeds);

            // Wait for Motion completed
            waitMotionDone(axisIndexXY, 10000);

            // Read Current ConfocalLaser result
            _isConfocalLaserBusy = true;
            ConfocalLaser.evalResult result = _ConfocalLaser.Trigger(1, out _ConfocalLaser_Output, 0);
            _isConfocalLaserBusy = false;
            double currentDistance = 0;
            if (_ConfocalLaser_Output < -99999)
            {
                // Write Event
                writeEvent("UserEvent", "Out of Compensate zone.");
                MessageBox.Show("請先移動Z軸至測高計量測範圍!", "Error！！");
                throw new Exception("Invalid Thickness measurement, please check the Z position");
                _distance = 0;
            }
            else
            {
                //正常關門加工測高
                //if (_isSafetyInterlock)
                //{
                    currentDistance = -1 * _ConfocalLaser_Output * 0.001; //Since confocal laser result unit: um
                //    AutoThickProcess_Video[AutoThickProcess_Video_Count] = currentDistance;
                //}
                ////拍攝功能-開門關閉雷射二次加工
                //else
                //{
                //    currentDistance = AutoThickProcess_Video[AutoThickProcess_Video_Count];
                //
                //}
                writeEvent("GH+Orher孔測高數據", currentDistance.ToString() + "mm");
            }

            distances = new double[] { -_ConfigEquip._RelPosition_Laser2ConfocalLaser.X, -_ConfigEquip._RelPosition_Laser2ConfocalLaser.Y };

            // Move from ConfocalLaser to Laser
            // Do Motion Movement to specified target position
            this.rapidMove(axisIndexXY, distances, speeds);

            // Wait for Motion completed
            waitMotionDone(axisIndexXY, 10000);

            double distance = currentDistance - _ConfigEquip._Height_Equip.Laser;

            // MotionAxis.Z direction is reversed
            distance = -1 * distance;
            _distance = distance;
            //moveRel((int)MotionAxis.Z, distance, _ConfigEquip._Velocity_Default.Z);

            // Wait for Motion completed
            //waitMotionDone(axisIndexZ, 10000);

            // Set Positioning to Absolute Mode
            code = "G90";
            executeGCode(code);
            Thread.Sleep(100);

        }

        private void logAnnotOperation(string msg)
        {
            //Write Log
            writeEvent("自動流程", msg);

            // Write Process
            writeProcess("Step7", msg);
        }

        #endregion
    }
}
