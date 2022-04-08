using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Tulpep.NotificationWindow;
namespace Screenshot_Helper
{
    public partial class Form1 : Form
    {
        //-----------------------從外部引入函數---------------------- 


        //idHook 代表掛鉤的類型 此處為監聽鍵盤
        //lpfn 代表掛鉤會呼叫的函式
        //hMod 代表當前模塊的句柄 對應DLL
        //dwThreadId 代表要鉤的執行緒 0代表所有執行緒
        //當方法成功時 返回值為呼叫函式的句柄
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        //解除掛鉤 _hookID代表掛鉤呼叫函式的句柄
        //返回值為是否成功的bool
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        //當掛鉤綁定的函式結束時 要執行此函式來呼叫下一個掛鉤
        //hhk代表當前呼叫函式的句柄
        //nCode代表當前掛鉤執行代號
        //wParam和lParam是掛鉤抓到訊息後會傳入的參數 不同類型掛鉤有不同參數
        //此處wParam代表按鍵行為 lParam代表按鍵
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        //用模塊名查詢該模塊句柄
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        //---------------------------------------------------------



        //掛勾類型代碼13號為監聽鍵盤
        private const int WH_KEYBOARD_LL = 13;
        //256 代表按下按鍵
        private const int WM_KEYDOWN = 0x0100;
        //宣告函式指標類別
        private delegate IntPtr LowLevelKeyboardProc(
        int nCode, IntPtr wParam, IntPtr lParam);
        //實體化函式指標並賦值給它
        private static LowLevelKeyboardProc _proc = HookCallback;
        //用來儲存掛勾呼叫函式的句柄
        private static IntPtr _hookID = IntPtr.Zero;
        //紀錄要存在哪一個資料夾 預設為C:\Downloads
        public static String dir = @"C:\Downloads\";


        //設定掛鉤
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                //是外部函式
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        //掛勾會呼叫的函式 也就是實際要操作的地方
        //nCode代表此掛鉤執行代號
        //wParam和lParam是掛鉤抓到訊息後會傳入的參數 不同類型掛鉤有不同參數
        //此處wParam代表按鍵行為 lParam代表按鍵
        private static IntPtr HookCallback(
        int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if ((Keys)vkCode == Keys.S)
                {
                    try
                    {
                        //利用時間戳當作檔名
                        String stamp = Stopwatch.GetTimestamp().ToString()+".png";
                        String Path = dir + stamp;
                        saveImage(Path);

                        PopupNotifier pn = new PopupNotifier();
                        pn.Image = Properties.Resources.OK;
                        pn.TitleText = "訊息";
                        pn.ContentText = "儲存成功";
                        pn.AnimationDuration = 10;
                        pn.Popup();

                    }
                    catch (Exception e){ Console.WriteLine(e.ToString()); };
                }
            }

            //結束前記得呼叫下一個函式 要把同一類型的掛鉤(參數一樣)串起來 
            //返回值若是0 代表放了訊息 若是1 代表消滅訊息
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        public Form1()
        {
            InitializeComponent();
            //在表單初始化的同時掛上掛鉤 並儲存呼叫函式的句柄
            _hookID = SetHook(_proc);

            if (_hookID != IntPtr.Zero)
                Console.WriteLine("掛鉤安裝成功");
            else
                Console.WriteLine("掛鉤安裝失敗");
        }

        //關閉時 記得解除掛鉤
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {

            bool isUnhook = UnhookWindowsHookEx(_hookID);
            if (isUnhook)
                Console.WriteLine("掛鉤解除成功");
            else
                Console.WriteLine("掛鉤解除失敗");
        }

        public static void saveImage(String path)
        {
            Bitmap b = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics g = Graphics.FromImage(b);
            g.CopyFromScreen(new Point(0, 0), new Point(0, 0), new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));
            b.Save(path);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if(fbd.ShowDialog() == DialogResult.OK)
            {
                dir = fbd.SelectedPath +@"\";
                label2.Text = fbd.SelectedPath;
            }
        }
    }
}