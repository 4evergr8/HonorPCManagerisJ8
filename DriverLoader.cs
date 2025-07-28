using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Threading;

public static class DriverLoader
{
    private const string DRIVER_ID = "WinRing0_1_2_0";
    private static readonly string DriverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WinRing0x64.sys");
    private const int MaxRetry = 2;

    public static void InitializeDriver()
    {
        StringBuilder report = new StringBuilder();

        for (int i = 0; i < MaxRetry; i++)
        {
            if (TryOpenDriver())
            {
                report.AppendLine("驱动已加载并可打开。");
                break;
            }

            if (!File.Exists(DriverPath))
            {
                report.AppendLine("驱动文件不存在：" + DriverPath);
                break;
            }

            report.AppendLine($"第 {i + 1} 次尝试安装驱动...");

            string error;
            if (!InstallDriver(DriverPath, out error))
            {
                report.AppendLine("安装失败：" + error);
                DeleteDriverService();
                Thread.Sleep(2000);
                continue;
            }

            Thread.Sleep(1000); // 等待驱动加载

            if (TryOpenDriver())
            {
                report.AppendLine("驱动安装并打开成功。");
                break;
            }
            else
            {
                report.AppendLine("安装成功但无法打开，准备删除后重试...");
                DeleteDriverService();
                Thread.Sleep(2000);
            }
        }

        Console.WriteLine(report.ToString());
    }

    private static bool TryOpenDriver()
    {
        SafeFileHandle handle = new SafeFileHandle(NativeMethods.CreateFile(@"\\.\" + DRIVER_ID,
            FileAccessFlags.GENERIC_READ | FileAccessFlags.GENERIC_WRITE, 0, IntPtr.Zero,
            CreationDisposition.OPEN_EXISTING, FileAttributesFlags.FILE_ATTRIBUTE_NORMAL,
            IntPtr.Zero), true);

        if (handle.IsInvalid)
        {
            handle.Dispose();
            return false;
        }

        try
        {
            FileSecurity security = File.GetAccessControl(@"\\.\" + DRIVER_ID);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool InstallDriver(string path, out string errorMessage)
    {
        IntPtr manager = NativeMethods.OpenSCManager(null, null,
            ServiceControlManagerAccessRights.SC_MANAGER_ALL_ACCESS);

        if (manager == IntPtr.Zero)
        {
            errorMessage = "OpenSCManager 返回 NULL。";
            return false;
        }

        IntPtr service = NativeMethods.CreateService(manager, DRIVER_ID, DRIVER_ID,
            ServiceAccessRights.SERVICE_ALL_ACCESS,
            ServiceType.SERVICE_KERNEL_DRIVER, StartType.SERVICE_SYSTEM_START,
            ErrorControl.SERVICE_ERROR_NORMAL, path, null, null, null, null, null);

        if (service == IntPtr.Zero)
        {
            int hr = Marshal.GetHRForLastWin32Error();
            if (hr == ERROR_SERVICE_EXISTS)
            {
                errorMessage = "服务已存在。";
                NativeMethods.CloseServiceHandle(manager);
                return false;
            }
            else
            {
                errorMessage = "CreateService 错误: " +
                    Marshal.GetExceptionForHR(hr).Message;
                NativeMethods.CloseServiceHandle(manager);
                return false;
            }
        }

        if (!NativeMethods.StartService(service, 0, null))
        {
            int hr = Marshal.GetHRForLastWin32Error();
            if (hr != ERROR_SERVICE_ALREADY_RUNNING)
            {
                errorMessage = "StartService 错误: " +
                    Marshal.GetExceptionForHR(hr).Message;
                NativeMethods.CloseServiceHandle(service);
                NativeMethods.CloseServiceHandle(manager);
                return false;
            }
        }

        NativeMethods.CloseServiceHandle(service);
        NativeMethods.CloseServiceHandle(manager);

        try
        {
            FileSecurity fileSecurity = File.GetAccessControl(@"\\.\" + DRIVER_ID);
            fileSecurity.SetSecurityDescriptorSddlForm("O:BAG:SYD:(A;;FA;;;SY)(A;;FA;;;BA)");
            File.SetAccessControl(@"\\.\" + DRIVER_ID, fileSecurity);
        }
        catch { }

        errorMessage = null;
        return true;
    }

    private static void DeleteDriverService()
    {
        IntPtr manager = NativeMethods.OpenSCManager(null, null,
            ServiceControlManagerAccessRights.SC_MANAGER_ALL_ACCESS);

        if (manager == IntPtr.Zero)
            return;

        IntPtr service = NativeMethods.OpenService(manager, DRIVER_ID,
            ServiceAccessRights.SERVICE_ALL_ACCESS);

        if (service == IntPtr.Zero)
        {
            NativeMethods.CloseServiceHandle(manager);
            return;
        }

        ServiceStatus status = new ServiceStatus();
        NativeMethods.ControlService(service, ServiceControl.SERVICE_CONTROL_STOP, ref status);
        NativeMethods.DeleteService(service);
        NativeMethods.CloseServiceHandle(service);
        NativeMethods.CloseServiceHandle(manager);
    }

    #region Native and Structs

    private enum ServiceAccessRights : uint
    {
        SERVICE_ALL_ACCESS = 0xF01FF
    }

    private enum ServiceControlManagerAccessRights : uint
    {
        SC_MANAGER_ALL_ACCESS = 0xF003F
    }

    private enum ServiceType : uint
    {
        SERVICE_KERNEL_DRIVER = 1
    }

    private enum StartType : uint
    {
        SERVICE_SYSTEM_START = 1
    }

    private enum ErrorControl : uint
    {
        SERVICE_ERROR_NORMAL = 1
    }

    private enum ServiceControl : uint
    {
        SERVICE_CONTROL_STOP = 1
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ServiceStatus
    {
        public uint dwServiceType;
        public uint dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
    }

    private enum FileAccessFlags : uint
    {
        GENERIC_READ = 0x80000000,
        GENERIC_WRITE = 0x40000000
    }

    private enum CreationDisposition : uint
    {
        OPEN_EXISTING = 3
    }

    private enum FileAttributesFlags : uint
    {
        FILE_ATTRIBUTE_NORMAL = 0x80
    }

    private const int
      ERROR_SERVICE_EXISTS = unchecked((int)0x80070431),
      ERROR_SERVICE_ALREADY_RUNNING = unchecked((int)0x80070420);

    private static class NativeMethods
    {
        private const string ADVAPI = "advapi32.dll";
        private const string KERNEL = "kernel32.dll";

        [DllImport(ADVAPI, SetLastError = true)]
        public static extern IntPtr OpenSCManager(string machineName,
            string databaseName, ServiceControlManagerAccessRights dwAccess);

        [DllImport(ADVAPI)]
        public static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport(ADVAPI, SetLastError = true)]
        public static extern IntPtr CreateService(IntPtr hSCManager,
            string lpServiceName, string lpDisplayName,
            ServiceAccessRights dwDesiredAccess, ServiceType dwServiceType,
            StartType dwStartType, ErrorControl dwErrorControl,
            string lpBinaryPathName, string lpLoadOrderGroup, string lpdwTagId,
            string lpDependencies, string lpServiceStartName, string lpPassword);

        [DllImport(ADVAPI, SetLastError = true)]
        public static extern IntPtr OpenService(IntPtr hSCManager,
            string lpServiceName, ServiceAccessRights dwDesiredAccess);

        [DllImport(ADVAPI, SetLastError = true)]
        public static extern bool DeleteService(IntPtr hService);

        [DllImport(ADVAPI, SetLastError = true)]
        public static extern bool StartService(IntPtr hService,
            uint dwNumServiceArgs, string[] lpServiceArgVectors);

        [DllImport(ADVAPI, SetLastError = true)]
        public static extern bool ControlService(IntPtr hService,
            ServiceControl dwControl, ref ServiceStatus lpServiceStatus);

        [DllImport(KERNEL, SetLastError = true)]
        public static extern IntPtr CreateFile(string lpFileName,
            FileAccessFlags dwDesiredAccess, uint dwShareMode,
            IntPtr lpSecurityAttributes, CreationDisposition dwCreationDisposition,
            FileAttributesFlags dwFlagsAndAttributes, IntPtr hTemplateFile);
    }

    #endregion
}
