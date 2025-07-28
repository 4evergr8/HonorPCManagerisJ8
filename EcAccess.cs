using System;
using System.Runtime.InteropServices;
using System.Threading;

public static class EcAccess
{
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint OPEN_EXISTING = 3;

    private const uint OLS_TYPE = 40000;
    private const uint METHOD_BUFFERED = 0;
    private const uint FILE_READ_ACCESS = 0x0001;
    private const uint FILE_WRITE_ACCESS = 0x0002;

    private const byte EC_SC = 0x66;
    private const byte EC_DATA = 0x62;

    private static IntPtr handle = IntPtr.Zero;

    // 计算 IOCTL 控制码
    private static uint CTL_CODE(uint deviceType, uint function, uint method, uint access)
    {
        return (deviceType << 16) | (access << 14) | (function << 2) | method;
    }

    // 计算出的IOCTL读写端口代码，与调用的硬编码数字保持一致
    private const uint IOCTL_READ_PORT = 2621464780;   // = CTL_CODE(40000, 0x09, 0, 0x0001)
    private const uint IOCTL_WRITE_PORT = 2621481176;  // = CTL_CODE(40000, 0x0A, 0, 0x0002)

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct WriteIoPortInput
    {
        public uint PortNumber;
        public byte Value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ReadIoPortInput
    {
        public uint PortNumber;
    }

    public static void Init()
    {
        handle = CreateFileW(
            @"\\.\WinRing0_1_2_0",
            GENERIC_READ | GENERIC_WRITE,
            0,
            IntPtr.Zero,
            OPEN_EXISTING,
            0,
            IntPtr.Zero
        );

        if (handle.ToInt64() == -1)
        {
            throw new Exception("打开 WinRing0 设备失败：" + Marshal.GetLastWin32Error());
        }

        Console.WriteLine("驱动设备打开成功");
    }


    public static void WriteEC(byte address, byte data,int wait)
    {
        Console.WriteLine("[1] 等待 EC 空闲");
        WaitIbfClear();
        Thread.Sleep(wait);

        Console.WriteLine("[2] 写入命令 0x81 到 EC_SC");
        WritePort(EC_SC, 0x81);
        Thread.Sleep(wait);

        Console.WriteLine("[3] 等待 EC 空闲");
        WaitIbfClear();
        Thread.Sleep(wait);

        Console.WriteLine($"[4] 写入地址 0x{address:X2} 到 EC_DATA");
        WritePort(EC_DATA, address);
        Thread.Sleep(wait);

        Console.WriteLine("[5] 等待 EC 空闲");
        WaitIbfClear();
        Thread.Sleep(wait);

        Console.WriteLine($"[6] 写入数据 0x{data:X2} 到 EC_DATA");
        WritePort(EC_DATA, data);
        Thread.Sleep(wait);

        Console.WriteLine("[7] 等待 EC 空闲");
        WaitIbfClear();
        Thread.Sleep(wait);
        Console.WriteLine($"[完成] 成功写入 EC 寄存器 0x{address:X2} = 0x{data:X2}");



    }



    private static void WaitIbfClear()
    {
        for (int i = 0; i < 100; i++)
        {
            byte status = ReadPort(EC_SC);
            if ((status & 0x02) == 0) // IBF = 0
                return;
            Thread.Sleep(5);
        }
        throw new TimeoutException("等待 IBF 清零超时");
    }



    private static byte ReadPort(byte port)
    {
        ReadIoPortInput input = new ReadIoPortInput { PortNumber = port };
        byte[] outBuffer = new byte[1];
        uint bytesReturned = 0;

        bool success = DeviceIoControl(
            handle,
            IOCTL_READ_PORT, // 这里用常量，不改调用代码里的硬编码数字
            GetBytes(input), (uint)Marshal.SizeOf(input),
            outBuffer, (uint)outBuffer.Length,
            ref bytesReturned,
            IntPtr.Zero
        );

        if (!success)
            throw new Exception("DeviceIoControl 读取端口失败：" + Marshal.GetLastWin32Error());

        return outBuffer[0];
    }

    private static void WritePort(byte port, byte value)
    {
        WriteIoPortInput input = new WriteIoPortInput { PortNumber = port, Value = value };
        uint bytesReturned = 0;

        bool success = DeviceIoControl(
            handle,
            IOCTL_WRITE_PORT, // 这里用常量，不改调用代码里的硬编码数字
            GetBytes(input), (uint)Marshal.SizeOf(input),
            null, 0,
            ref bytesReturned,
            IntPtr.Zero
        );

        if (!success)
            throw new Exception("DeviceIoControl 写入端口失败：" + Marshal.GetLastWin32Error());
    }

    private static byte[] GetBytes<T>(T str) where T : struct
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
        return arr;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice,
        uint dwIoControlCode,
        byte[] lpInBuffer,
        uint nInBufferSize,
        [Out] byte[] lpOutBuffer,
        uint nOutBufferSize,
        ref uint lpBytesReturned,
        IntPtr lpOverlapped
    );
}
