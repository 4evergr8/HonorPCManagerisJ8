using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main()
    {
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

        // 加载驱动DLL并调用PrintDump函数
        try
        {
            LoadAndCallPrintDumpFunction();
        }
        catch (Exception ex)
        {
            MessageBox.Show("调用 EmbeddedController.dll 失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // 运行托盘程序
        TrayIconApp.Run();
    }

    static bool IsAdministrator()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    // 加载 DLL 并调用PrintDump函数
    static void LoadAndCallPrintDumpFunction()
    {
        string dllPath = ExtractEmbeddedDll("HonorPCManagerisJ8.EmbeddedController.dll");

        if (!File.Exists(dllPath))
            throw new Exception("DLL 文件未写出成功：" + dllPath);

        IntPtr hModule = LoadLibrary(dllPath);
        if (hModule == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            throw new Exception("LoadLibrary 加载失败，错误码：" + error);
        }

        IntPtr pDump = GetProcAddress(hModule, "printDump");

        if (pDump == IntPtr.Zero)
        {
            throw new Exception("PrintDump 函数地址获取失败！");
        }

        var dump = (PrintDumpDelegate)Marshal.GetDelegateForFunctionPointer(pDump, typeof(PrintDumpDelegate));

        IntPtr ec = IntPtr.Zero; // 这里传递适当的 ec 实例（如果有需要创建）
        dump(ec);

        // 可选：删除临时的 DLL 文件
        File.Delete(dllPath);
    }

    static string ExtractEmbeddedDll(string resourceName)
    {
        string outputPath = Path.Combine(Path.GetTempPath(), "EmbeddedController.dll");
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
        {
            if (stream == null)
                throw new Exception("嵌入资源未找到：" + resourceName);
            using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fs);
            }
        }
        return outputPath;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr LoadLibrary(string dllToLoad);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

    // 函数委托
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void PrintDumpDelegate(IntPtr ecInstance);
}
