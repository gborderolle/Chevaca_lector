using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RFID_EPC_Project
{
  public class DLLProject
  {
    public struct NET_DeviceInfo
    {
      /// <summary>
      /// MAC地址
      /// </summary>
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
      public byte[] MAC;
      /// <summary>
      /// IP地址
      /// </summary>
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
      public byte[] IP;
      /// <summary>
      /// 版本
      /// </summary>
      public byte VER;
      /// <summary>
      /// 设备名长度
      /// </summary>
      public byte LEN;
      /// <summary>
      /// 设备名
      /// </summary>
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] NAME;
    };
    public struct _DEVICEHW_CONFIG
    {
      /// <summary>
      /// 设备类型,具体见设备类型表
      /// </summary>
      public byte bDevType;
      /// <summary>
      /// 设备子类型
      /// </summary>
      public byte bAuxDevType;
      /// <summary>
      /// 设备序号
      /// </summary>
      public byte bIndex;
      /// <summary>
      /// 设备硬件版本号
      /// </summary>
      public byte bDevHardwareVer;
      /// <summary>
      /// 设备软件版本号
      /// </summary>
      public byte bDevSoftwareVer;
      /// <summary>
      /// 模块名
      /// </summary>
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 21)]
      public byte[] szModulename;
      /// <summary>
      /// 模块网络MAC地址
      /// </summary>
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
      public byte[] bDevMAC;
      /// <summary>
      /// 模块IP地址
      /// </summary>
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
      public byte[] bDevIP;
      /// <summary>
      /// 模块网关IP
      /// </summary>
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
      public byte[] bDevGWIP;
      /// <summary>
      /// 模块子网掩码
      /// </summary>
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
      public byte[] bDevIPMask;
      /// <summary>
      /// DHCP 使能，是否启用DHCP,1:启用，0：不启用
      /// </summary>
      public byte bDhcpEnable;
      /// <summary>
      /// 保留
      /// </summary>
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1D)]
      public byte[] breserved;
    };

    public struct _DEVICEPORT_CONFIG
    {
      /// <summary>
      /// 端口序号
      /// </summary>
      public byte bIndex;
      /// <summary>
      /// 端口启用标志 1：启用后 ；0：不启用
      /// </summary>
      public byte bPortEn;
      /// <summary>
      /// 网络工作模式: 0: TCP SERVER;1: TCP CLENT; 2: UDP SERVER 3：UDP CLIENT;
      /// </summary>
      public byte bNetMode;
      /// <summary>
      /// TCP 客户端模式下随即本地端口号，1：随机 0: 不随机
      /// </summary>
      public byte bRandSportFlag;
      /// <summary>
      /// 网络通讯端口号
      /// </summary>
      public ushort wNetPort;
      /// <summary>
      /// 目的IP地址
      /// </summary>
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
      public byte[] bDesIP;
      /// <summary>
      /// 工作于TCP Server模式时，允许外部连接的端口号
      /// </summary>
      public ushort wDesPort;
      /// <summary>
      /// 串口波特率: 300---921600bps
      /// </summary>
      public uint dBaudRate;
      /// <summary>
      /// 串口数据位: 5---8位 
      /// </summary>
      public byte bDataSize;
      /// <summary>
      /// 串口停止位: 1表示1个停止位; 2表示2个停止位
      /// </summary>
      public byte bStopBits;
      /// <summary>
      /// 串口校验位: 0表示奇校验; 1表示偶校验; 2表示标志位(MARK,置1); 3表示空白位(SPACE,清0);
      /// </summary>
      public byte bParity;
      /// <summary>
      /// PHY断开，Socket动作，1：关闭Socket 2、不动作
      /// </summary>
      public byte bPHYChangeHandle;
      /// <summary>
      /// 串口RX数据打包长度，最大1024
      /// </summary>
      public uint dRxPktlength;
      /// <summary>
      /// 串口RX数据打包转发的最大等待时间,单位为: 10ms,0则表示关闭超时功能
      /// </summary>
      public uint dRxPktTimeout;
      /// <summary>
      /// 工作于TCP CLIENT时，连接TCP SERVER的最大重试次数
      /// </summary>
      public byte bReConnectCnt;
      /// <summary>
      /// 串口复位操作: 0表示不清空串口数据缓冲区; 1表示连接时清空串口数据缓冲区
      /// </summary>
      public byte bResetCtrl;
      /// <summary>
      /// 域名功能启用标志，1：启用 2：不启用
      /// </summary>
      public byte bDNSFlag;
      /// <summary>
      /// 域名
      /// </summary>
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
      public byte[] szDomainname;
      /// <summary>
      /// DNS 主机
      /// </summary>
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
      public byte[] bDNSHostIP;
      /// <summary>
      /// DNS 端口
      /// </summary>
      public ushort wDNSHostPort;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
      public byte[] breserved;
    };

    public struct _NET_DEVICE_CONFIG
    {
      /// <summary>
      /// 从硬件处获取的配置信息
      /// </summary>
      public _DEVICEHW_CONFIG HWCfg;
      /// <summary>
      /// 网络设备所包含的子设备的配置信息
      /// </summary>
      [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.Struct)]
      public _DEVICEPORT_CONFIG[] PortCfg;
    };

    //struct转换为byte[]
    public static byte[] StructToBytes(object structObj)
    {
      int size = Marshal.SizeOf(structObj);
      IntPtr buffer = Marshal.AllocHGlobal(size);
      try
      {
        Marshal.StructureToPtr(structObj, buffer, false);
        byte[] bytes = new byte[size];
        Marshal.Copy(buffer, bytes, 0, size);
        return bytes;
      }
      finally
      {
        Marshal.FreeHGlobal(buffer);
      }
    }

    //byte[]转换为struct
    public static object BytesToStruct(byte[] bytes, Type strcutType)
    {
      int size = Marshal.SizeOf(strcutType);
      IntPtr buffer = Marshal.AllocHGlobal(size);
      try
      {
        Marshal.Copy(bytes, 0, buffer, size);
        return Marshal.PtrToStructure(buffer, strcutType);
      }
      finally
      {
        Marshal.FreeHGlobal(buffer);
      }
    }

    public static int GetStructLength(Type strcutType)
    {
      return Marshal.SizeOf(strcutType);
    }

    /// <summary>回调函数</summary>
    public delegate int CallReceive(byte Type, byte Command, int LpRecSize, IntPtr LpRecByt);
    /// <summary>回调函数</summary>
    public delegate int Udp_Receive(string IP, int Port, int LpRecSize, IntPtr LpRecByt);
    /// <summary>回调函数</summary>
    public delegate int Svr_Receive(byte Type, string IP, int Port, int LpRecSize, IntPtr LpRecByt);


#if DllImport_x86
    #region DllImport_x86
    [DllImport("DLLProject_x86.dll")]
    public static extern int Connect(byte ConnType, string ConnChar, CallReceive rc);
    [DllImport("DLLProject_x86.dll")]
    public static extern int Disconnect();
    [DllImport("DLLProject_x86.dll")]
    public static extern int GetModuleInfo(ref byte InfoType, StringBuilder InfoData, ref int DataSize);
    [DllImport("DLLProject_x86.dll")]
    public static extern int ReadSingle();
    [DllImport("DLLProject_x86.dll")]
    public static extern int ReadMulti(int PollCount);
    [DllImport("DLLProject_x86.dll")]
    public static extern int StopRead();
    [DllImport("DLLProject_x86.dll")]
    public static extern int SetSelectParam(byte Target, byte Action, byte MemBank, int Pointer, byte Truncated, byte[] MaskData, byte MaskSize);
    [DllImport("DLLProject_x86.dll")]
    public static extern int SetSelectMode(byte Mode);
    [DllImport("DLLProject_x86.dll")]
    public static extern int ReadData(byte[] AccessPassword, byte MemBank, int StartIndex, int Length, byte[] PC, byte[] EPC, byte[] Data, ref int Size);
    [DllImport("DLLProject_x86.dll")]
    public static extern int WriteData(byte[] AccessPassword, byte MemBank, int StartIndex, byte[] Data, int Size, byte[] PC, byte[] EPC);
    [DllImport("DLLProject_x86.dll")]
    public static extern int SetRegion(byte Region);
    [DllImport("DLLProject_x86.dll")]
    public static extern int GetRfChannel(ref byte RfChannel);
    [DllImport("DLLProject_x86.dll")]
    public static extern int SetRfChannel(byte RfChannel);
    [DllImport("DLLProject_x86.dll")]
    public static extern int SetFhss(bool Fhss);
    [DllImport("DLLProject_x86.dll")]
    public static extern int GetPower(ref int Power);
    [DllImport("DLLProject_x86.dll")]
    public static extern int SetPower(int Power);
    [DllImport("DLLProject_x86.dll")]
    public static extern int SetCW(bool CW);
    [DllImport("DLLProject_x86.dll")]
    public static extern int GetQuery(ref byte DR, ref byte M, ref byte TRext, ref byte Sel, ref byte Session, ref byte Target, ref byte Q);
    [DllImport("DLLProject_x86.dll")]
    public static extern int SetQuery(byte DR, byte M, byte TRext, byte Sel, byte Session, byte Target, byte Q);
    [DllImport("DLLProject_x86.dll")]
    public static extern int ImpinjMonzaQT(byte[] AccessPassword, byte RW, byte Persistence, byte Payload, byte[] PC, byte[] EPC, byte[] QTControl);
    [DllImport("DLLProject_x86.dll")]
    public static extern int NxpChangeConfig(byte[] AccessPassword, byte[] Config, byte[] PC, byte[] EPC);
    [DllImport("DLLProject_x86.dll")]
    public static extern int NxpChangeEas(byte[] AccessPassword, byte Protect, byte[] PC, byte[] EPC);
    [DllImport("DLLProject_x86.dll")]
    public static extern int NxpReadProtect(byte[] AccessPassword, byte Protect, byte[] PC, byte[] EPC);
    [DllImport("DLLProject_x86.dll")]
    public static extern int NxpEasAlarm(byte[] EASAlarmCode);
    [DllImport("DLLProject_x86.dll")]
    public static extern int LockUnlock(byte[] AccessPassword, byte[] LD, byte[] PC, byte[] EPC);
    [DllImport("DLLProject_x86.dll")]
    public static extern int Kill(byte[] AccessPassword, byte[] PC, byte[] EPC);
    [DllImport("DLLProject_x86.dll")]
    public static extern int ScanJammer(ref byte CH_L, ref byte CH_H, byte[] JMR);
    [DllImport("DLLProject_x86.dll")]
    public static extern int ScanRSSI(ref byte CH_L, ref byte CH_H, byte[] JMR);
    [DllImport("DLLProject_x86.dll")]
    public static extern int GetModemPara(ref byte Mixer_G, ref byte IF_G, ref int Thrd);
    [DllImport("DLLProject_x86.dll")]
    public static extern int SetModemPara(byte Mixer_G, byte IF_G, int Thrd);

    [DllImport("DLLProject_x86.dll")]
    public static extern int NetCfg_Open(string IP);
    [DllImport("DLLProject_x86.dll")]
    public static extern int NetCfg_Close();
    [DllImport("DLLProject_x86.dll")]
    public static extern int NetCfg_SearchForDevices(byte DeviceType, ref int Count, byte[] Data, ref int Length);
    [DllImport("DLLProject_x86.dll")]
    public static extern int NetCfg_FactoryReset(byte DeviceType, byte[] MAC);
    [DllImport("DLLProject_x86.dll")]
    public static extern int NetCfg_GetInfo(byte DeviceType, byte[] MAC, byte[] Data, ref int Length);
    [DllImport("DLLProject_x86.dll")]
    public static extern int NetCfg_SetInfo(byte DeviceType, byte[] LocalMAC, byte[] DevMAC, byte[] Data, int Length);

    [DllImport("DLLProject_x86.dll")]
    public static extern int Svr_Startup(byte ConnType, string ConnChar, Svr_Receive rc);
    [DllImport("DLLProject_x86.dll")]
    public static extern int Svr_CleanUp();
    [DllImport("DLLProject_x86.dll")]
    public static extern int Svr_Send(string ConnChar, byte[] Data, int Length);
    #endregion
#endif

#if DllImport_x64
    #region DllImport_x64
    [DllImport("DLLProject_x64.dll")]
    public static extern int Connect(byte ConnType, string ConnChar, CallReceive rc);
    [DllImport("DLLProject_x64.dll")]
    public static extern int Disconnect();
    [DllImport("DLLProject_x64.dll")]
    public static extern int GetModuleInfo(ref byte InfoType, StringBuilder InfoData, ref int DataSize);
    [DllImport("DLLProject_x64.dll")]
    public static extern int ReadSingle();
    [DllImport("DLLProject_x64.dll")]
    public static extern int ReadMulti(int PollCount);
    [DllImport("DLLProject_x64.dll")]
    public static extern int StopRead();
    [DllImport("DLLProject_x64.dll")]
    public static extern int SetSelectParam(byte Target, byte Action, byte MemBank, int Pointer, byte Truncated, byte[] MaskData, byte MaskSize);
    [DllImport("DLLProject_x64.dll")]
    public static extern int SetSelectMode(byte Mode);
    [DllImport("DLLProject_x64.dll")]
    public static extern int ReadData(byte[] AccessPassword, byte MemBank, int StartIndex, int Length, byte[] PC, byte[] EPC, byte[] Data, ref int Size);
    [DllImport("DLLProject_x64.dll")]
    public static extern int WriteData(byte[] AccessPassword, byte MemBank, int StartIndex, byte[] Data, int Size, byte[] PC, byte[] EPC);
    [DllImport("DLLProject_x64.dll")]
    public static extern int SetRegion(byte Region);
    [DllImport("DLLProject_x64.dll")]
    public static extern int GetRfChannel(ref byte RfChannel);
    [DllImport("DLLProject_x64.dll")]
    public static extern int SetRfChannel(byte RfChannel);
    [DllImport("DLLProject_x64.dll")]
    public static extern int SetFhss(bool Fhss);
    [DllImport("DLLProject_x64.dll")]
    public static extern int GetPower(ref int Power);
    [DllImport("DLLProject_x64.dll")]
    public static extern int SetPower(int Power);
    [DllImport("DLLProject_x64.dll")]
    public static extern int SetCW(bool CW);
    [DllImport("DLLProject_x64.dll")]
    public static extern int GetQuery(ref byte DR, ref byte M, ref byte TRext, ref byte Sel, ref byte Session, ref byte Target, ref byte Q);
    [DllImport("DLLProject_x64.dll")]
    public static extern int SetQuery(byte DR, byte M, byte TRext, byte Sel, byte Session, byte Target, byte Q);
    [DllImport("DLLProject_x64.dll")]
    public static extern int ImpinjMonzaQT(byte[] AccessPassword, byte RW, byte Persistence, byte Payload, byte[] PC, byte[] EPC, byte[] QTControl);
    [DllImport("DLLProject_x64.dll")]
    public static extern int NxpChangeConfig(byte[] AccessPassword, byte[] Config, byte[] PC, byte[] EPC);
    [DllImport("DLLProject_x64.dll")]
    public static extern int NxpChangeEas(byte[] AccessPassword, byte Protect, byte[] PC, byte[] EPC);
    [DllImport("DLLProject_x64.dll")]
    public static extern int NxpReadProtect(byte[] AccessPassword, byte Protect, byte[] PC, byte[] EPC);
    [DllImport("DLLProject_x64.dll")]
    public static extern int NxpEasAlarm(byte[] EASAlarmCode);
    [DllImport("DLLProject_x64.dll")]
    public static extern int LockUnlock(byte[] AccessPassword, byte[] LD, byte[] PC, byte[] EPC);
    [DllImport("DLLProject_x64.dll")]
    public static extern int Kill(byte[] AccessPassword, byte[] PC, byte[] EPC);
    [DllImport("DLLProject_x64.dll")]
    public static extern int ScanJammer(ref byte CH_L, ref byte CH_H, byte[] JMR);
    [DllImport("DLLProject_x64.dll")]
    public static extern int ScanRSSI(ref byte CH_L, ref byte CH_H, byte[] JMR);
    [DllImport("DLLProject_x64.dll")]
    public static extern int GetModemPara(ref byte Mixer_G, ref byte IF_G, ref int Thrd);
    [DllImport("DLLProject_x64.dll")]
    public static extern int SetModemPara(byte Mixer_G, byte IF_G, int Thrd);

    [DllImport("DLLProject_x64.dll")]
    public static extern int NetCfg_Open(string IP);
    [DllImport("DLLProject_x64.dll")]
    public static extern int NetCfg_Close();
    [DllImport("DLLProject_x64.dll")]
    public static extern int NetCfg_SearchForDevices(byte DeviceType, ref int Count, byte[] Data, ref int Length);
    [DllImport("DLLProject_x64.dll")]
    public static extern int NetCfg_FactoryReset(byte DeviceType, byte[] MAC);
    [DllImport("DLLProject_x64.dll")]
    public static extern int NetCfg_GetInfo(byte DeviceType, byte[] MAC, byte[] Data, ref int Length);
    [DllImport("DLLProject_x64.dll")]
    public static extern int NetCfg_SetInfo(byte DeviceType, byte[] LocalMAC, byte[] DevMAC, byte[] Data, int Length);

    [DllImport("DLLProject_x64.dll")]
    public static extern int Svr_Startup(byte ConnType, string ConnChar, Svr_Receive rc);
    [DllImport("DLLProject_x64.dll")]
    public static extern int Svr_CleanUp();
    [DllImport("DLLProject_x64.dll")]
    public static extern int Svr_Send(string ConnChar, byte[] Data, int Length);
    #endregion
#endif

  }
}
