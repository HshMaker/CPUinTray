using System;
using System.Diagnostics;
using System.Management;
class CPUInTray
{
    private static readonly string cpuCategoryName = "Processor Information";
    private static readonly string cpuCounterName = "% Processor Utility";
    private static readonly string cpuInstanceName = "_Total";
    private static double totalRam = 0.0f;

    private static ManagementClass? ramClass = null;
    private static PerformanceCounter? cpuCounter = null;
    private static PerformanceCounter? ramCounter = null;
    static void Main()
    {
        Console.WriteLine("자 시작해볼까?");

        cpuCounter = new PerformanceCounter(cpuCategoryName, cpuCounterName, cpuInstanceName);
        ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        ramClass = new ManagementClass("Win32_OperatingSystem");

        totalRam = GetTotalRam();

        while(true)
        {
            Console.WriteLine($"CPU 사용량 : {GetCPURate()}%");
            Console.WriteLine($"RAM 사용량 : {(1 - (GetRamRate()/(totalRam/1024)) ) * 100}%");
            Thread.Sleep(2000);
        }
    }

    public static float GetCPURate()
    {
        if (cpuCounter == null) return 0.0f;

        float cpuPercent = cpuCounter.NextValue();

        return cpuPercent;
    }

    public static float GetRamRate()
    {
        if (ramCounter == null) return 0.0f;

        float ramPercent = ramCounter.NextValue();

        return ramPercent;
    }

    public static double GetTotalRam()
    {
        if (ramClass == null) return 0.0f;
        ManagementObjectCollection instances = ramClass.GetInstances();
        
        foreach (ManagementObject info in instances)
        {
            double totalRam = double.Parse(info["TotalVisibleMemorySize"].ToString()!);
            return totalRam;
        }

        return 0;
    }
}