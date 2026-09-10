using System;
using System.Runtime.InteropServices;
class TestLoad {
  [DllImport("kernel32.dll", SetLastError=true)]
  static extern IntPtr LoadLibrary(string lpFileName);
  [DllImport("kernel32.dll")]
  static extern uint GetLastError();

  static void Main() {
    IntPtr h = LoadLibrary(@"D:\path\to\DistanceDll.dll");
    Console.WriteLine("LoadLibrary returned: " + h);
    Console.WriteLine("GetLastError: 0x" + GetLastError().ToString("X"));
  }
}