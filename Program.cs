using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

static class Program
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    [STAThread]
    static void Main()
    {
        // 先隐藏控制台窗口
        var handle = GetConsoleWindow();
        ShowWindow(handle, SW_HIDE);

        // 判断管理员权限
        if (!IsAdministrator())
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Assembly.GetExecutingAssembly().Location,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(startInfo);
            }
            catch
            {
                MessageBox.Show("需要管理员权限才能运行此程序。", "权限不足", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        TrayIconApp.RunTrayIconInBackground(); // 运行托盘程序

        var config = YamlConfigLoader.LoadConfig();
        var timeout = config.timeout;
        var startup = config.startup;
        var settings = config.settings;
        var wait = config.wait;
        var debug = config.debug;

        // 如果debug为true，显示控制台窗口
        if (debug)
        {
            ShowWindow(handle, SW_SHOW);
        }

        AutoStartHelper.SetAutoStart(startup);

        while (true)
        {
            // 调用Ring0Init，确保驱动加载
            try
            {
                DriverLoader.InitializeDriver();
                foreach (var dict in config.settings)
                {
                    foreach (var kv in dict)
                    {
                        // key 按10进制转换
                        byte keyByte = Convert.ToByte(kv.Key, 10);

                        // value 按16进制转换，直接转换，不用去除0x
                        byte valueByte = Convert.ToByte(kv.Value, 16);

                        EcAccess.WriteEC(keyByte, valueByte, wait);
                    }
                }

                // 向 EC 寄存器 0x93 写入 0x55
            }
            catch (Exception ex)
            {
                MessageBox.Show("EC 写入失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // === 你要执行的逻辑 ===
            Console.WriteLine("跑一次：" + DateTime.Now);

            Thread.Sleep(timeout);
        }
    }

    static bool IsAdministrator()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
