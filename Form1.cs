using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace RFID_EPC_Project
{
  public partial class Form1 : Form
  {
    #region 通讯类型
    enum ConnType
    {
      /// <summary>
      /// 串口
      /// </summary>
      Com = 0x01,
      /// <summary>
      /// USB
      /// </summary>
      USB = 0x02,
      /// <summary>
      /// TcpClient
      /// </summary>
      TcpCli = 0x03,
      /// <summary>
      /// TcpServer
      /// </summary>
      TcpSvr = 0x04,
      /// <summary>
      /// UDP
      /// </summary>
      UDP = 0x05,
    }
    #endregion
    int RetDword = 0;

    private String timeFormat = "yyyy/MM/dd HH:mm:ss.ff";

    DataTable basic_table = new DataTable();
    DataTable advanced_table = new DataTable();
    DataSet ds_basic = null;
    DataSet ds_advanced = null;
    int initDataTableLen = 1;
    int rowIndex = 0;

    string pc = string.Empty;
    string epc = string.Empty;
    string crc = string.Empty;
    string rssi = string.Empty;

    int FailEPCNum = 0;
    int SucessEPCNum = 0;
    double errnum = 0;
    double db_errEPCNum = 0;
    double db_LoopNum_cnt = 0;
    string per = "0.000";

    bool NetCfgBool = false;
    string NetCfgDescription = string.Empty;
    DataTable NetCfg_table = new DataTable();
    DataSet ds_NetCfg = null;
    int initNetTableLen = 1;
    int NetRowIndex = 0;

    void ShowRetDword(string CommandStr, string DataText = "")
    {
      textBox1.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff") + "\r\n");
      textBox1.AppendText("Command:" + CommandStr + ";Status:" + (RetDword == 0 ? "Succeed" : "Failure[" + RetDword.ToString("X2") + "]") + "\r\n");
      if (!string.IsNullOrEmpty(DataText))
      {
        textBox1.AppendText("DataText:" + DataText + "\r\n");
      }
      textBox1.AppendText("\r\n");
    }

    private DLLProject.CallReceive aReceive;
    public Form1()
    {
      InitializeComponent();
    }
    private void Form1_Load(object sender, EventArgs e)
    {
      cbxDR.SelectedIndex =
      cbxM.SelectedIndex =
      cbxTRext.SelectedIndex =
      cbxSel.SelectedIndex =
      cbxSession.SelectedIndex =
      cbxTarget.SelectedIndex =
      cbxQAdv.SelectedIndex =
      cbxLockKillAction.SelectedIndex =
      cbxLockAccessAction.SelectedIndex =
      cbxLockEPCAction.SelectedIndex =
      cbxLockTIDAction.SelectedIndex =
      cbxLockUserAction.SelectedIndex =
       0;
      cbxTRext.SelectedIndex = 1;
      cbxMixerGain.SelectedIndex = 3;
      cbxIFAmpGain.SelectedIndex = 6;

      this.dgvEpcBasic.DataBindingComplete += new DataGridViewBindingCompleteEventHandler(dgvEpcBasic_DataBindingComplete);
      this.dgv_epc2.DataBindingComplete += new DataGridViewBindingCompleteEventHandler(dgv_epc2_DataBindingComplete);

      ds_basic = new DataSet();
      ds_advanced = new DataSet();

      basic_table = BasicGetEPCHead();
      advanced_table = AdvancedGetEPCHead();
      ds_basic.Tables.Add(basic_table);
      ds_advanced.Tables.Add(advanced_table);

      cbxSelTarget.SelectedIndex = 0;
      cbxAction.SelectedIndex = 0;
      cbxSelMemBank.SelectedIndex = 1;
      cbxMemBank.SelectedIndex = 0;

      DataView BasicDataViewEpc = ds_basic.Tables[0].DefaultView;
      DataView AdvancedDataViewEpc = ds_advanced.Tables[0].DefaultView;
      this.dgvEpcBasic.DataSource = BasicDataViewEpc;
      this.dgv_epc2.DataSource = AdvancedDataViewEpc;
      Basic_DGV_ColumnsWidth(this.dgvEpcBasic);
      Advanced_DGV_ColumnsWidth(this.dgv_epc2);

      rbType_CheckedChanged(null, null);

      RefreshNetworkInterface();

      ds_NetCfg = new DataSet();
      NetCfg_table = NetCfg_Head();
      ds_NetCfg.Tables.Add(NetCfg_table);
      NetCfgGridView.DataBindingComplete += new DataGridViewBindingCompleteEventHandler(NetCfgGridView_DataBindingComplete);

      DataView NetCfgDataView = ds_NetCfg.Tables[0].DefaultView;
      NetCfgGridView.DataSource = NetCfgDataView;
      NetCfg_DGV_ColumnsWidth(NetCfgGridView);
      cbx_NetMode1.SelectedIndex =
        cbx_DNSFlag1.SelectedIndex =
        cbx_BaudRate1.SelectedIndex =
        cbx_DataSize1.SelectedIndex =
        cbx_StopBits1.SelectedIndex =
        cbx_Parity1.SelectedIndex =
        0;

      cbx_NetMode1_SelectedIndexChanged(null, null);
      cbx_NetMode2_SelectedIndexChanged(null, null);

      SvrType_CheckedChanged(null, null);
    }

    ulong BytesToUlong(byte[] Bytes, int startIndex = 0, int length = 0)
    {
      if (length <= 0)
      {
        length = Bytes.Length - startIndex;
      }
      int NumSize = 8 > length ? length : 8;
      ulong RetUlong = 0;
      for (int i = 0; i < NumSize; i++)
      {
        RetUlong <<= 8;
        RetUlong |= Convert.ToUInt64((Bytes[i] & 0x00ff));
      }
      return RetUlong;
    }
    int BytesToInt(byte[] Bytes, int startIndex = 0, int length = 0)
    {
      return Convert.ToInt32(BytesToUlong(Bytes, startIndex, length <= 0 || length > 4 ? 4 : length));
    }
    short BytesToShort(byte[] Bytes, int startIndex = 0, int length = 0)
    {
      return (short)BytesToUlong(Bytes, startIndex, length <= 0 || length > 2 ? 2 : length);
    }

    string BytesToHexString(byte[] Bytes, string Separator, int startIndex = 0, int length = 0)
    {
      if (length <= 0)
      {
        length = Bytes.Length - startIndex;
      }
      return BitConverter.ToString(Bytes, startIndex, length).Replace("-", " ");
    }
    byte[] HexStringToBytes(string HexString)
    {
      HexString = HexString.Replace(" ", "");
      if (string.IsNullOrEmpty(HexString))
      {
        return null;
      }
      if (HexString.Length % 2 > 0)
      {
        HexString = "0" + HexString;
      }
      int ByteInt = HexString.Length / 2;
      byte[] ByteAr = new byte[ByteInt];
      for (int i = 0; i < ByteInt; i++)
      {
        ByteAr[i] = Convert.ToByte(HexString.Substring(i * 2, 2), 16);
      }
      return ByteAr;
    }
    public int DataReceive(byte Type, byte Command, int LpRecSize, IntPtr LpRecByt)
    {
      Invoke((MethodInvoker)delegate ()
      {
        byte[] RecByt = null;
          if (LpRecSize > 0)
          {
              RecByt = new byte[LpRecSize];
              System.Runtime.InteropServices.Marshal.Copy(LpRecByt, RecByt, 0, LpRecSize);



              ShowRetDword("DataReceive", BitConverter.ToString(RecByt).Replace('-', ' '));


              string data = "testing_fail";
              string DataText2 = BitConverter.ToString(RecByt).Replace('-', ' ');
              if (!string.IsNullOrEmpty(DataText2))
              {
                  data = DataText2;
                  //textBox1.AppendText("DataText:" + DataText2 + "\r\n");
              }

              Connect_and_Send_SQL(data);
          }
        ProcReceive(Type, Command, RecByt);
      });
      return 1;
    }
    void ProcReceive(byte Type, byte Command, byte[] ParamData)
    {
      if (Type == 0x02 && Command == 0x22 && ParamData != null)
      {
        SucessEPCNum = SucessEPCNum + 1;
        db_errEPCNum = FailEPCNum;
        db_LoopNum_cnt = db_LoopNum_cnt + 1;
        errnum = (db_errEPCNum / db_LoopNum_cnt) * 100;
        per = string.Format("{0:0.000}", errnum);

        int rssidBm = ParamData[0];
        if (rssidBm > 127)
        {
          rssidBm = -((-rssidBm) & 0xFF);
        }
        rssidBm -= Convert.ToInt32(tbxCoupling.Text, 10);
        rssidBm -= Convert.ToInt32(tbxAntennaGain.Text, 10);
        rssi = rssidBm.ToString();

        int PCEPCLength = (ParamData[1] / 8) * 2;
        pc = BytesToHexString(ParamData, " ", 1, 2);
        epc = BytesToHexString(ParamData, " ", 3, PCEPCLength);
        crc = BytesToHexString(ParamData, " ", 3 + PCEPCLength, 2);
        GetEPC(pc, epc, crc, rssi, per);
      }
    }
    private void GetEPC(string pc, string epc, string crc, string rssi, string per)
    {
      this.dgv_epc2.ClearSelection();
      bool isFoundEpc = false;
      string newEpcItemCnt;
      int indexEpc = 0;

      int EpcItemCnt;
      if (rowIndex <= initDataTableLen)
      {
        EpcItemCnt = rowIndex;
      }
      else
      {
        EpcItemCnt = basic_table.Rows.Count;
        EpcItemCnt = advanced_table.Rows.Count;
      }

      for (int j = 0; j < EpcItemCnt; j++)
      {
        if (basic_table.Rows[j][2].ToString() == epc && basic_table.Rows[j][1].ToString() == pc)
        {
          indexEpc = j;
          isFoundEpc = true;
          break;
        }
      }

      if (EpcItemCnt < initDataTableLen)
      {
        if (!isFoundEpc || EpcItemCnt == 0)
        {
          if (EpcItemCnt + 1 < 10)
          {
            newEpcItemCnt = "0" + Convert.ToString(EpcItemCnt + 1);
          }
          else
          {
            newEpcItemCnt = Convert.ToString(EpcItemCnt + 1);
          }
          basic_table.Rows[EpcItemCnt][0] = newEpcItemCnt;
          basic_table.Rows[EpcItemCnt][1] = pc;
          basic_table.Rows[EpcItemCnt][2] = epc;
          basic_table.Rows[EpcItemCnt][3] = crc;
          basic_table.Rows[EpcItemCnt][4] = rssi;
          basic_table.Rows[EpcItemCnt][5] = 1;
          basic_table.Rows[EpcItemCnt][6] = "0.000";
          basic_table.Rows[EpcItemCnt][7] = System.DateTime.Now.ToString(timeFormat);

          advanced_table.Rows[EpcItemCnt][0] = newEpcItemCnt;
          advanced_table.Rows[EpcItemCnt][1] = pc;
          advanced_table.Rows[EpcItemCnt][2] = epc;
          advanced_table.Rows[EpcItemCnt][3] = crc;
          advanced_table.Rows[EpcItemCnt][4] = 1;

          rowIndex++;
        }
        else
        {
          if (indexEpc + 1 < 10)
          {
            newEpcItemCnt = "0" + Convert.ToString(indexEpc + 1);
          }
          else
          {
            newEpcItemCnt = Convert.ToString(indexEpc + 1);
          }
          basic_table.Rows[indexEpc][0] = newEpcItemCnt;
          basic_table.Rows[indexEpc][4] = rssi;
          basic_table.Rows[indexEpc][5] = Convert.ToInt32(basic_table.Rows[indexEpc][5].ToString()) + 1;
          basic_table.Rows[indexEpc][6] = per;
          basic_table.Rows[indexEpc][7] = System.DateTime.Now.ToString(timeFormat);

          advanced_table.Rows[indexEpc][0] = newEpcItemCnt;
          advanced_table.Rows[indexEpc][4] = Convert.ToInt32(advanced_table.Rows[indexEpc][4].ToString()) + 1;
        }
      }
      else
      {
        if (!isFoundEpc || EpcItemCnt == 0)
        {
          if (EpcItemCnt + 1 < 10)
          {
            newEpcItemCnt = "0" + Convert.ToString(EpcItemCnt + 1);
          }
          else
          {
            newEpcItemCnt = Convert.ToString(EpcItemCnt + 1);
          }
          basic_table.Rows.Add(new object[] { newEpcItemCnt, pc, epc, crc, rssi, "1", "0.000", DateTime.Now.ToString(timeFormat) });
          advanced_table.Rows.Add(new object[] { newEpcItemCnt, pc, epc, crc, "1" });
          rowIndex++;
        }
        else
        {
          if (indexEpc + 1 < 10)
          {
            newEpcItemCnt = "0" + Convert.ToString(indexEpc + 1);
          }
          else
          {
            newEpcItemCnt = Convert.ToString(indexEpc + 1);
          }
          basic_table.Rows[indexEpc][0] = newEpcItemCnt;
          basic_table.Rows[indexEpc][4] = rssi;
          basic_table.Rows[indexEpc][5] = Convert.ToInt32(basic_table.Rows[indexEpc][5].ToString()) + 1;
          basic_table.Rows[indexEpc][6] = per;
          basic_table.Rows[indexEpc][7] = System.DateTime.Now.ToString(timeFormat);

          advanced_table.Rows[indexEpc][0] = newEpcItemCnt;
          advanced_table.Rows[indexEpc][4] = Convert.ToInt32(advanced_table.Rows[indexEpc][4].ToString()) + 1;
        }
      }
    }
    private void dgvEpcBasic_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
    {
      this.dgvEpcBasic.ClearSelection();
    }
    private void dgv_epc2_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
    {
      for (int i = 0; i < this.dgv_epc2.Rows.Count; i++)
      {
        if (i % 2 == 0)
        {
          this.dgv_epc2.Rows[i].DefaultCellStyle.BackColor = Color.AliceBlue;
        }
      }
    }
    private void NetCfgGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
    {
      NetCfgGridView.ClearSelection();
    }
    private DataTable BasicGetEPCHead()
    {
      basic_table.TableName = "EPC";
      basic_table.Columns.Add("No.", typeof(string));
      basic_table.Columns.Add("PC", typeof(string));
      basic_table.Columns.Add("EPC", typeof(string));
      basic_table.Columns.Add("CRC", typeof(string));
      basic_table.Columns.Add("RSSI(dBm)", typeof(string));
      basic_table.Columns.Add("CNT", typeof(string));
      basic_table.Columns.Add("PER(%)", typeof(string));
      basic_table.Columns.Add("Time", typeof(string));

      for (int i = 0; i <= initDataTableLen - 1; i++)
      {
        basic_table.Rows.Add(new object[] { null });
      }
      basic_table.AcceptChanges();

      return basic_table;
    }

    private DataTable AdvancedGetEPCHead()
    {
      advanced_table.TableName = "EPC";
      advanced_table.Columns.Add("No.", typeof(string));
      advanced_table.Columns.Add("PC", typeof(string));
      advanced_table.Columns.Add("EPC", typeof(string));
      advanced_table.Columns.Add("CRC", typeof(string));
      advanced_table.Columns.Add("CNT", typeof(string));

      for (int i = 0; i <= initDataTableLen - 1; i++)
      {
        advanced_table.Rows.Add(new object[] { null });
      }
      advanced_table.AcceptChanges();

      return advanced_table;
    }
    private DataTable NetCfg_Head()
    {
      NetCfg_table.TableName = "NetCfg";
      NetCfg_table.Columns.Add("No.", typeof(string));
      NetCfg_table.Columns.Add("Device name", typeof(string));
      NetCfg_table.Columns.Add("IP", typeof(string));
      NetCfg_table.Columns.Add("MAC", typeof(string));
      NetCfg_table.Columns.Add("Version", typeof(string));

      for (int i = 0; i <= initNetTableLen - 1; i++)
      {
        NetCfg_table.Rows.Add(new object[] { null });
      }
      NetCfg_table.AcceptChanges();

      return NetCfg_table;
    }
    void SetColumn(DataGridViewColumn Dgvc, int Width, bool Visible = true)
    {
      Dgvc.Width = Width;
      Dgvc.Resizable = DataGridViewTriState.False;
      Dgvc.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
      Dgvc.Visible = Visible;
    }
    private void Basic_DGV_ColumnsWidth(DataGridView dataGridView1)
    {
      dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
      dataGridView1.ColumnHeadersHeight = 40;

      SetColumn(dataGridView1.Columns[0], 40);
      SetColumn(dataGridView1.Columns[1], 60);
      SetColumn(dataGridView1.Columns[2], 290);
      SetColumn(dataGridView1.Columns[3], 60);
      SetColumn(dataGridView1.Columns[4], 75);
      SetColumn(dataGridView1.Columns[5], 70);
      SetColumn(dataGridView1.Columns[6], 72);
      SetColumn(dataGridView1.Columns[7], 40, false);
    }

    private void Advanced_DGV_ColumnsWidth(DataGridView dataGridView1)
    {
      dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
      dataGridView1.ColumnHeadersHeight = 40;

      SetColumn(dataGridView1.Columns[0], 40);
      SetColumn(dataGridView1.Columns[1], 60);
      SetColumn(dataGridView1.Columns[2], 240);
      SetColumn(dataGridView1.Columns[3], 60);
      SetColumn(dataGridView1.Columns[4], 52);
    }
    private void NetCfg_DGV_ColumnsWidth(DataGridView dataGridView1)
    {
      dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
      dataGridView1.ColumnHeadersHeight = 40;

      SetColumn(dataGridView1.Columns[0], 40);
      SetColumn(dataGridView1.Columns[1], 120);
      SetColumn(dataGridView1.Columns[2], 120);
      SetColumn(dataGridView1.Columns[3], 200);
      SetColumn(dataGridView1.Columns[4], 80);
    }
    private void rbType_CheckedChanged(object sender, EventArgs e)
    {
      cbxValue.DropDownStyle = ComboBoxStyle.DropDownList;
      cbxValue.Items.Clear();
      btnConn.Enabled = false;
      cbxValue.Enabled = true;
      if (rbCom.Checked)
      {
        cbxValue.Items.AddRange(SerialPort.GetPortNames().OrderBy(a => a).ToArray());
        if (cbxValue.Items.Count > 0)
        {
          cbxValue.SelectedIndex = 0;
        }
      }
      else if (rbUSB.Checked)
      {
        cbxValue.Enabled = false;
      }
      else if (rbTCP.Checked)
      {
        cbxValue.DropDownStyle = ComboBoxStyle.Simple;
        cbxValue.Enabled = true;
      }
      btnConn.Enabled = true;
    }

    private void btnConn_Click(object sender, EventArgs e)
    {
      //btnInvtMulti.Enabled = false;
      string CommandStr = string.Empty;
      aReceive = DataReceive;
      switch (btnConn.Tag.ToString())
      {
        case "0":
          {
            CommandStr = "Connect";
            if (rbCom.Checked)
            {
              RetDword = DLLProject.Connect((byte)ConnType.Com, cbxValue.Text, aReceive);
            }
            else if (rbUSB.Checked)
            {
              //btnInvtMulti.Enabled = btnStopRD.Enabled = false;
              RetDword = DLLProject.Connect((byte)ConnType.USB, null, aReceive);
            }
            else if (rbTCP.Checked)
            {
              RetDword = DLLProject.Connect((byte)ConnType.TcpCli, cbxValue.Text, aReceive);
            }
            if (RetDword == 0)
            {
              flowLayoutPanel4.Enabled = cbxValue.Enabled = false;
              btnConn.Text = "Disconnect";
              btnConn.Tag = "1";
              this.btnConn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            }
          }
          break;
        default:
          {
            CommandStr = "Disconnect";
            RetDword = DLLProject.Disconnect();
            if (RetDword == 0)
            {
              flowLayoutPanel4.Enabled = cbxValue.Enabled = true;
              btnConn.Text = "Connect";
              btnConn.Tag = "0";
              this.btnConn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            }
          }
          break;
      }
      ShowRetDword(CommandStr);

    }
    public void dataGrid_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
    {
      int rowIndex = dgv_epc2.CurrentRow.Index;
      if (dgv_epc2.Rows[rowIndex].Cells[2].Value.ToString() != null)
      {
        txtSelMask.Text = dgv_epc2.Rows[rowIndex].Cells[2].Value.ToString();
      }
    }

    private void btnInvtBasic_Click(object sender, EventArgs e)
    {
      RetDword = DLLProject.ReadSingle();
      //ShowRetDword("ReadSingle");
    }

    private void btnSetSelect_Click(object sender, EventArgs e)
    {
      byte Target = (byte)cbxSelTarget.SelectedIndex;
      byte Action = (byte)cbxAction.SelectedIndex;
      byte MemBank = (byte)cbxSelMemBank.SelectedIndex;
      int Pointer = BytesToInt(HexStringToBytes(txtSelPrt3.Text + txtSelPrt2.Text + txtSelPrt1.Text + txtSelPrt0.Text));
      byte Truncated = ckxTruncated.Checked ? (byte)0x80 : (byte)0x00;
      byte[] MaskByt = HexStringToBytes(txtSelMask.Text);
      RetDword = DLLProject.SetSelectParam(Target, Action, MemBank, Pointer, Truncated, MaskByt, (byte)MaskByt.Length);
      ShowRetDword("SetSelectParam");
    }

    private void btnInvtAdv_Click(object sender, EventArgs e)
    {
      RetDword = DLLProject.ReadSingle();
      //ShowRetDword("ReadSingle");
    }

    private void inv_mode_CheckedChanged(object sender, EventArgs e)
    {
      RetDword = DLLProject.SetSelectMode(inv_mode.Checked ? (byte)0x00 : (byte)0x01);
      ShowRetDword("SetSelectMode");
    }

    void SetSelect()
    {
      byte Target = (byte)cbxSelTarget.SelectedIndex;
      byte Action = (byte)cbxAction.SelectedIndex;
      byte MemBank = 1;
      int Pointer = BytesToInt(HexStringToBytes(txtSelPrt3.Text + txtSelPrt2.Text + txtSelPrt1.Text + txtSelPrt0.Text));
      byte Truncated = ckxTruncated.Checked ? (byte)0x80 : (byte)0x00;
      byte[] MaskByt = HexStringToBytes(txtSelMask.Text);
      RetDword = DLLProject.SetSelectParam(Target, Action, MemBank, Pointer, Truncated, MaskByt, (byte)MaskByt.Length);
      ShowRetDword("SetSelectParam");
    }
    private void btn_invtread_Click(object sender, EventArgs e)
    {
      byte[] AccessPassword = HexStringToBytes(txtRwAccPassWord.Text);
      byte MemBank = (byte)cbxMemBank.SelectedIndex;
      int StartIndex = BytesToShort(HexStringToBytes(txtWordPtr1.Text + txtWordPtr0.Text));
      int Length = BytesToShort(HexStringToBytes(txtWordCnt1.Text + txtWordCnt0.Text));
      byte[] PC = new byte[2], EPC = new byte[12], Data = new byte[1024];
      int Size = 0;
      SetSelect();
      RetDword = DLLProject.ReadData(AccessPassword, MemBank, StartIndex, Length, PC, EPC, Data, ref Size);
      if (RetDword == 0)
      {
        txtInvtRWData.Text = BytesToHexString(Data, " ", 0, Size);
      }
      ShowRetDword("ReadData");

    }

    private void btnInvtWrtie_Click(object sender, EventArgs e)
    {
      byte[] AccessPassword = HexStringToBytes(txtRwAccPassWord.Text);
      byte MemBank = (byte)cbxMemBank.SelectedIndex;
      int StartIndex = BytesToShort(HexStringToBytes(txtWordPtr1.Text + txtWordPtr0.Text));
      int Length = BytesToShort(HexStringToBytes(txtWordCnt1.Text + txtWordCnt0.Text));
      byte[] PC = new byte[2], EPC = new byte[12];
      byte[] Data = HexStringToBytes(txtInvtRWData.Text);
      SetSelect();
      RetDword = DLLProject.WriteData(AccessPassword, MemBank, StartIndex, Data, Data.Length, PC, EPC);
      ShowRetDword("WriteData");
    }

    private void button1_Click(object sender, EventArgs e)
    {
      textBox1.Clear();
    }
    private void btn_clear_epc1_Click(object sender, EventArgs e)
    {
      basic_table.Clear();
      advanced_table.Clear();
      FailEPCNum = 0;
      SucessEPCNum = 0;
      db_LoopNum_cnt = 0;
      for (int i = 0; i <= initDataTableLen - 1; i++)
      {
        basic_table.Rows.Add(new object[] { null });
      }
      basic_table.AcceptChanges();
      for (int i = 0; i <= initDataTableLen - 1; i++)
      {
        advanced_table.Rows.Add(new object[] { null });
      }
      advanced_table.AcceptChanges();
      rowIndex = 0;
    }
    enum RegionType
    {
      China2 = 0x04,
      China1 = 0x01,
      US = 0x02,
      Europe = 0x03,
      Korea = 0x06,
    }

    private void btnSetCW_Click(object sender, EventArgs e)
    {
      bool CW = false;
      if (btnSetCW.Text == "CW OFF")
      {
        CW = false;
        btnSetCW.Text = "CW ON";
      }
      else
      {
        CW = true;
        btnSetCW.Text = "CW OFF";
      }
      RetDword = DLLProject.SetCW(CW);
      ShowRetDword("SetCW");
    }

    private void btnGetQuery_Click(object sender, EventArgs e)
    {
      byte DR = 0, TRext = 0, Target = 0;
      byte M = 0, Sel = 0, Session = 0, Q = 0;

      RetDword = DLLProject.GetQuery(ref DR, ref M, ref TRext, ref Sel, ref Session, ref Target, ref Q);
      ShowRetDword("GetQuery");

      cbxDR.SelectedIndex = DR;
      cbxM.SelectedIndex = M;
      cbxTRext.SelectedIndex = TRext;
      cbxSel.SelectedIndex = Sel;
      cbxSession.SelectedIndex = Session;
      cbxTarget.SelectedIndex = Target;
      cbxQAdv.SelectedIndex = Q;
    }

    private void btnSetQuery_Click(object sender, EventArgs e)
    {
      byte DR = (byte)cbxDR.SelectedIndex;
      byte M = (byte)cbxM.SelectedIndex;
      byte TRext = (byte)cbxTRext.SelectedIndex;
      byte Sel = (byte)cbxSel.SelectedIndex;
      byte Session = (byte)cbxSession.SelectedIndex;
      byte Target = (byte)cbxTarget.SelectedIndex;
      byte Q = (byte)cbxQAdv.SelectedIndex;

      RetDword = DLLProject.SetQuery(DR, M, TRext, Sel, Session, Target, Q);
      ShowRetDword("SetQuery");
    }

    private void btnMonzaQTRead_Click(object sender, EventArgs e)
    {
      byte[] PC = new byte[2], EPC = new byte[12], QTControl = new byte[16];
      byte[] AccessPassword = HexStringToBytes(tbxMonzaQTAccessPwd.Text);
      byte RW = 0x00;
      byte Persistence = 0x01;
      byte Payload = (byte)((cbxMonzaQT_SR.Checked ? 0x01 : 0x00) | (cbxMonzaQT_MEM.Checked ? 0x02 : 0x00));

      SetSelect();
      RetDword = DLLProject.ImpinjMonzaQT(AccessPassword, RW, Persistence, Payload, PC, EPC, QTControl);
      ShowRetDword("ImpinjMonzaQT");
    }

    private void btnMonzaQTWrite_Click(object sender, EventArgs e)
    {
      byte[] PC = new byte[2], EPC = new byte[12], QTControl = new byte[16];
      byte[] AccessPassword = HexStringToBytes(tbxMonzaQTAccessPwd.Text);
      byte RW = 0x01;
      byte Persistence = 0x01;
      byte Payload = (byte)((cbxMonzaQT_SR.Checked ? 0x01 : 0x00) | (cbxMonzaQT_MEM.Checked ? 0x02 : 0x00));

      SetSelect();
      RetDword = DLLProject.ImpinjMonzaQT(AccessPassword, RW, Persistence, Payload, PC, EPC, QTControl);
      ShowRetDword("ImpinjMonzaQT");
    }

    private void btnChangeConfig_Click(object sender, EventArgs e)
    {
      byte[] PC = new byte[2], EPC = new byte[12];
      byte[] AccessPassword = HexStringToBytes(tbxNxpCmdAccessPwd.Text);
      byte[] Config = HexStringToBytes(txtConfigData.Text);

      SetSelect();
      RetDword = DLLProject.NxpChangeConfig(AccessPassword, Config, PC, EPC);
      ShowRetDword("NxpChangeConfig");
    }

    private void btnChangeEas_Click(object sender, EventArgs e)
    {
      byte[] PC = new byte[2], EPC = new byte[12];
      byte[] AccessPassword = HexStringToBytes(tbxNxpCmdAccessPwd.Text);
      byte SetEas = (byte)(cbxSetEas.Checked ? 0x01 : 0x00);

      SetSelect();
      RetDword = DLLProject.NxpChangeEas(AccessPassword, SetEas, PC, EPC);
      ShowRetDword("NxpChangeEas");
    }

    private void btnReadProtect_Click(object sender, EventArgs e)
    {
      byte[] PC = new byte[2], EPC = new byte[12];
      byte[] AccessPassword = HexStringToBytes(tbxNxpCmdAccessPwd.Text);
      byte Protect = (byte)(cbxReadProtectReset.Checked ? 0x01 : 0x00);

      SetSelect();
      RetDword = DLLProject.NxpReadProtect(AccessPassword, Protect, PC, EPC);
      ShowRetDword("NxpReadProtect");
    }

    private void btnEasAlarm_Click(object sender, EventArgs e)
    {
      byte[] EASAlarmCode = new byte[64];
      RetDword = DLLProject.NxpEasAlarm(EASAlarmCode);
      ShowRetDword("NxpEasAlarm");
    }

    private void buttonLock_Click(object sender, EventArgs e)
    {
      byte[] LD = new byte[3];
      if (checkBoxKillPwd.Checked)
      {
        LD[2] |= 0x03 * 0x04;
        LD[1] |= (byte)cbxLockKillAction.SelectedIndex;
      }
      if (checkBoxAccessPwd.Checked)
      {
        LD[2] |= 0x03;
        LD[0] |= (byte)(cbxLockAccessAction.SelectedIndex * 0x40);
      }
      if (checkBoxEPC.Checked)
      {
        LD[1] |= 0x03 * 0x40;
        LD[0] |= (byte)(cbxLockEPCAction.SelectedIndex * 0x10);
      }
      if (checkBoxTID.Checked)
      {
        LD[1] |= 0x03 * 0x10;
        LD[0] |= (byte)(cbxLockTIDAction.SelectedIndex * 0x04);
      }
      if (checkBoxUser.Checked)
      {
        LD[1] |= 0x03 * 0x04;
        LD[0] |= (byte)cbxLockUserAction.SelectedIndex;
      }
      byte[] PC = new byte[2], EPC = new byte[12];
      byte[] AccessPassword = HexStringToBytes(textBoxLockAccessPwd.Text);

      SetSelect();
      RetDword = DLLProject.LockUnlock(AccessPassword, LD, PC, EPC);
      ShowRetDword("LockUnlock");
    }

    private void buttonKill_Click(object sender, EventArgs e)
    {
      byte[] PC = new byte[2], EPC = new byte[12];
      byte[] AccessPassword = HexStringToBytes(textBoxKillPwd.Text);

      SetSelect();
      RetDword = DLLProject.Kill(AccessPassword, PC, EPC);
      ShowRetDword("Kill");
    }

    private void btnScanJammer_Click(object sender, EventArgs e)
    {
      textBox2.Clear();
      byte CH_L = 0, CH_H = 0;
      byte[] JMR = new byte[128];
      RetDword = DLLProject.ScanJammer(ref CH_L, ref CH_H, JMR);
      ShowRetDword("ScanJammer");

      for (int i = 0; i < CH_H - CH_L; i++)
      {
        int jammer = JMR[i];
        if (jammer > 127)
        {
          jammer = -((-jammer) & 0xFF);
        }
        textBox2.AppendText("CH_" + i.ToString() + ":" + jammer.ToString() + "dBm" + "\r\n");
      }
    }

    private void btnScanRssi_Click(object sender, EventArgs e)
    {
      textBox3.Clear();
      byte CH_L = 0, CH_H = 0;
      byte[] JMR = new byte[128];
      RetDword = DLLProject.ScanRSSI(ref CH_L, ref CH_H, JMR);
      ShowRetDword("ScanRSSI");

      for (int i = 0; i < CH_H - CH_L; i++)
      {
        int jammer = JMR[i];
        if (jammer > 127)
        {
          jammer = -((-jammer) & 0xFF);
        }
        textBox3.AppendText("CH_" + i.ToString() + ":" + jammer.ToString() + "dBm" + "\r\n");
      }
    }

    private void btnSetModemPara_Click(object sender, EventArgs e)
    {
      byte Mixer_G = (byte)cbxMixerGain.SelectedIndex;
      byte IF_G = (byte)cbxIFAmpGain.SelectedIndex;
      int Thrd = Convert.ToInt32(tbxSignalThreshold.Text, 16);
      RetDword = DLLProject.SetModemPara(Mixer_G, IF_G, Thrd);
      ShowRetDword("SetModemPara");

    }

    private void btnGetModemPara_Click(object sender, EventArgs e)
    {
      byte Mixer_G = 0, IF_G = 0;
      int Thrd = 0;
      RetDword = DLLProject.GetModemPara(ref Mixer_G, ref IF_G, ref Thrd);
      ShowRetDword("GetModemPara");

      cbxMixerGain.SelectedIndex = Mixer_G;
      cbxIFAmpGain.SelectedIndex = IF_G;
      tbxSignalThreshold.Text = Thrd.ToString("X4");
    }

    private void button2_Click(object sender, EventArgs e)
    {
      byte InfoType = 0;
      StringBuilder InfoData = new StringBuilder(256);
      int DataSize = 0;
      RetDword = DLLProject.GetModuleInfo(ref InfoType, InfoData, ref DataSize);
      ShowRetDword("GetModuleInfo");
    }

    void RefreshNetworkInterface()
    {
      cbx_Netinterface.DataSource = null;
      cbx_Netinterface.DataSource = NetworkInterface.GetAllNetworkInterfaces();
      cbx_Netinterface.DisplayMember = "Description";

    }

    private void bt_Refresh_Click(object sender, EventArgs e)
    {
      RefreshNetworkInterface();
    }

    private void SetNetCfg(DLLProject.NET_DeviceInfo Ndi)
    {
      string Name = string.Empty, IP = string.Empty, MAC = string.Empty, VER = string.Empty;
      MAC = BitConverter.ToString(Ndi.MAC).Replace("-", ":");
      VER = Ndi.VER.ToString();
      IP = Ndi.IP[0].ToString() + "." + Ndi.IP[1].ToString() + "." + Ndi.IP[2].ToString() + "." + Ndi.IP[3].ToString();
      Name = Encoding.UTF8.GetString(Ndi.NAME, 0, Ndi.LEN).Replace("\0", "");
      bool isFoundNet = false;
      string newNetItemCnt;
      int indexNet = 0;

      int NetItemCnt;
      if (NetRowIndex <= initNetTableLen)
      {
        NetItemCnt = NetRowIndex;
      }
      else
      {
        NetItemCnt = NetCfg_table.Rows.Count;
      }

      for (int j = 0; j < NetItemCnt; j++)
      {
        if (/*NetCfg_table.Rows[j][2].ToString() == IP && */NetCfg_table.Rows[j][3].ToString() == MAC)
        {
          indexNet = j;
          isFoundNet = true;
          break;
        }
      }

      if (NetItemCnt < initNetTableLen)
      {
        if (!isFoundNet || NetItemCnt == 0)
        {
          if (NetItemCnt + 1 < 10)
          {
            newNetItemCnt = "0" + Convert.ToString(NetItemCnt + 1);
          }
          else
          {
            newNetItemCnt = Convert.ToString(NetItemCnt + 1);
          }
          NetCfg_table.Rows[NetItemCnt][0] = newNetItemCnt;
          NetCfg_table.Rows[NetItemCnt][1] = Name;
          NetCfg_table.Rows[NetItemCnt][2] = IP;
          NetCfg_table.Rows[NetItemCnt][3] = MAC;
          NetCfg_table.Rows[NetItemCnt][4] = VER;

          NetRowIndex++;
        }
        else
        {
          if (indexNet + 1 < 10)
          {
            newNetItemCnt = "0" + Convert.ToString(indexNet + 1);
          }
          else
          {
            newNetItemCnt = Convert.ToString(indexNet + 1);
          }
          NetCfg_table.Rows[indexNet][0] = newNetItemCnt;
          NetCfg_table.Rows[indexNet][1] = Name;
          NetCfg_table.Rows[indexNet][2] = IP;
          NetCfg_table.Rows[indexNet][3] = MAC;
          NetCfg_table.Rows[indexNet][4] = VER;
        }
      }
      else
      {
        if (!isFoundNet || NetItemCnt == 0)
        {
          if (NetItemCnt + 1 < 10)
          {
            newNetItemCnt = "0" + Convert.ToString(NetItemCnt + 1);
          }
          else
          {
            newNetItemCnt = Convert.ToString(NetItemCnt + 1);
          }
          NetCfg_table.Rows.Add(new object[] { newNetItemCnt, Name, IP, MAC, VER });
          NetRowIndex++;
        }
        else
        {
          if (indexNet + 1 < 10)
          {
            newNetItemCnt = "0" + Convert.ToString(indexNet + 1);
          }
          else
          {
            newNetItemCnt = Convert.ToString(indexNet + 1);
          }
          NetCfg_table.Rows[indexNet][0] = newNetItemCnt;
          NetCfg_table.Rows[indexNet][1] = Name;
          NetCfg_table.Rows[indexNet][2] = IP;
          NetCfg_table.Rows[indexNet][3] = MAC;
          NetCfg_table.Rows[indexNet][4] = VER;
        }
      }
    }
    private void button3_Click(object sender, EventArgs e)
    {
      button3.Enabled = false;
      NetworkInterface NetworkIntf = cbx_Netinterface.SelectedItem as NetworkInterface;
      if (!NetCfgBool || NetCfgDescription != NetworkIntf.Description)
      {
        RetDword = DLLProject.NetCfg_Close();
        ShowRetDword("NetCfg_Close");
        if (NetworkIntf == null)
        {
          ShowRetDword("未能取得网卡资源！");
          return;
        }
        IPInterfaceProperties IPip = NetworkIntf.GetIPProperties();

        if (IPip == null || IPip.UnicastAddresses == null)
        {
          ShowRetDword("未能取得网卡IP");
          return;
        }
        if (IPip.UnicastAddresses.Count <= 0)
        {
          ShowRetDword("未能取得网卡IP数量为0");
          return;
        }
        string IPv4Address = string.Empty;
        for (int i = 0; i < IPip.UnicastAddresses.Count; i++)
        {

          if (Regex.IsMatch(IPip.UnicastAddresses[i].Address.ToString(), @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$"))
          {
            IPv4Address = IPip.UnicastAddresses[i].Address.ToString();
          }
        }
        if (string.IsNullOrEmpty(IPv4Address))
        {
          ShowRetDword("未能取得网卡IPv4地址");
          return;
        }

        RetDword = DLLProject.NetCfg_Open(IPv4Address);
        ShowRetDword("NetCfg_Open");
        if (RetDword == 0)
        {
          NetCfgDescription = NetworkIntf.Description;
          NetCfgBool = true;
        }
      }

      int Count = 0, Length = 0;
      byte[] buffer = new byte[1024];
      RetDword = DLLProject.NetCfg_SearchForDevices(0, ref Count, buffer, ref Length);
      int NdiLength = DLLProject.GetStructLength(typeof(DLLProject.NET_DeviceInfo));
      for (int i = 0; i < Count; i++)
      {
        byte[] Data = new byte[NdiLength];
        Buffer.BlockCopy(buffer, i * NdiLength, Data, 0, NdiLength);
        DLLProject.NET_DeviceInfo Ndi = (DLLProject.NET_DeviceInfo)DLLProject.BytesToStruct(Data, typeof(DLLProject.NET_DeviceInfo));
        SetNetCfg(Ndi);

      }
      ShowRetDword("NetCfg_SearchForDevices");
      button3.Enabled = true;
    }
    private void NetCfgGridView_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
      tbx_DevMAC.Text = string.Empty;
      tbx_DevMAC.Tag = null;
      int rowIndex = NetCfgGridView.CurrentRow.Index;
      if (NetCfgGridView.Rows[rowIndex].Cells[3].Value.ToString() == null)
      {
        return;
      }
      string MAC = NetCfgGridView.Rows[rowIndex].Cells[3].Value.ToString();
      if (string.IsNullOrEmpty(MAC))
      {
        return;
      }
      tbx_DevMAC.Text = MAC;
      byte[] MacByt = HexStringToBytes(MAC.Replace(":", ""));
      int Length = 0;
      byte[] buffer = new byte[1024];
      RetDword = DLLProject.NetCfg_GetInfo(0, MacByt, buffer, ref Length);
      ShowRetDword("NetCfg_GetInfo");
      if (RetDword != 0)
      {
        return;
      }
      DLLProject._NET_DEVICE_CONFIG Ndc = (DLLProject._NET_DEVICE_CONFIG)DLLProject.BytesToStruct(buffer, typeof(DLLProject._NET_DEVICE_CONFIG));
      tbx_DevMAC.Tag = Ndc;
      tb_ModuleName.Text = Encoding.UTF8.GetString(Ndc.HWCfg.szModulename, 0, 21).Replace("\0", "");
      ibx_DevIP.SetIP(Ndc.HWCfg.bDevIP);
      ibx_DevIPMask.SetIP(Ndc.HWCfg.bDevIPMask);
      ibx_DevGWIP.SetIP(Ndc.HWCfg.bDevGWIP);
      cbx_DhcpEnable.Checked = Ndc.HWCfg.bDhcpEnable == 1 ? true : false;

      if (Ndc.PortCfg[0].bPortEn == 1)
      {
        tp_PortCfg1.Parent = tabControl2;
      }
      else
      {
        tp_PortCfg1.Parent = null;
      }
      cbx_NetMode1.SelectedIndex = Ndc.PortCfg[0].bNetMode;
      cbx_RandSportFlag1.Checked = Ndc.PortCfg[0].bRandSportFlag == 1 ? true : false;
      tbx_NetPort1.Text = Ndc.PortCfg[0].wNetPort.ToString();
      cbx_DNSFlag1.SelectedIndex = Ndc.PortCfg[0].bDNSFlag == 1 ? 1 : 0;
      ibx_DesIP1.SetIP(Ndc.PortCfg[0].bDesIP);
      tbx_DomainName1.Text = Encoding.UTF8.GetString(Ndc.PortCfg[0].szDomainname, 0, 20).Replace("\0", "");
      tbx_DesPort1.Text = Ndc.PortCfg[0].wDesPort.ToString();
      cbx_BaudRate1.Text = Ndc.PortCfg[0].dBaudRate.ToString();
      cbx_DataSize1.Text = Ndc.PortCfg[0].bDataSize.ToString();
      cbx_StopBits1.SelectedIndex = Ndc.PortCfg[0].bStopBits;
      cbx_Parity1.SelectedIndex = Ndc.PortCfg[0].bParity;
      cbx_PHYChangeHandle1.Checked = Ndc.PortCfg[0].bPHYChangeHandle == 1 ? true : false;
      tbx_RxPktlength1.Text = Ndc.PortCfg[0].dRxPktlength.ToString();
      tbx_RxPktTimeout1.Text = Ndc.PortCfg[0].dRxPktTimeout.ToString();
      cbx_ResetCtrl1.Checked = Ndc.PortCfg[0].bResetCtrl == 1 ? true : false;

      if (Ndc.PortCfg[1].bPortEn == 1)
      {
        tp_PortCfg2.Parent = tabControl2;
      }
      else
      {
        tp_PortCfg2.Parent = null;
      }
      cbx_NetMode2.SelectedIndex = Ndc.PortCfg[1].bNetMode;
      cbx_RandSportFlag2.Checked = Ndc.PortCfg[1].bRandSportFlag == 1 ? true : false;
      tbx_NetPort2.Text = Ndc.PortCfg[1].wNetPort.ToString();
      cbx_DNSFlag2.SelectedIndex = Ndc.PortCfg[1].bDNSFlag == 1 ? 1 : 0;
      ibx_DesIP2.SetIP(Ndc.PortCfg[1].bDesIP);
      tbx_DomainName2.Text = Encoding.UTF8.GetString(Ndc.PortCfg[1].szDomainname, 0, 20).Replace("\0", "");
      tbx_DesPort2.Text = Ndc.PortCfg[1].wDesPort.ToString();
      cbx_BaudRate2.Text = Ndc.PortCfg[1].dBaudRate.ToString();
      cbx_DataSize2.Text = Ndc.PortCfg[1].bDataSize.ToString();
      cbx_StopBits2.SelectedIndex = Ndc.PortCfg[1].bStopBits;
      cbx_Parity2.SelectedIndex = Ndc.PortCfg[1].bParity;
      cbx_PHYChangeHandle2.Checked = Ndc.PortCfg[1].bPHYChangeHandle == 1 ? true : false;
      tbx_RxPktlength2.Text = Ndc.PortCfg[1].dRxPktlength.ToString();
      tbx_RxPktTimeout2.Text = Ndc.PortCfg[1].dRxPktTimeout.ToString();
      cbx_ResetCtrl2.Checked = Ndc.PortCfg[1].bResetCtrl == 1 ? true : false;
    }

    private void button7_Click(object sender, EventArgs e)
    {
      DLLProject._NET_DEVICE_CONFIG Ndc = (DLLProject._NET_DEVICE_CONFIG)tbx_DevMAC.Tag;

      byte[] ModuleNameByt = Encoding.UTF8.GetBytes(tb_ModuleName.Text);
      Buffer.BlockCopy(ModuleNameByt, 0, Ndc.HWCfg.szModulename, 0, ModuleNameByt.Length);
      Ndc.HWCfg.bDevIP = ibx_DevIP.ToBytes();
      Ndc.HWCfg.bDevIPMask = ibx_DevIPMask.ToBytes();
      Ndc.HWCfg.bDevGWIP = ibx_DevGWIP.ToBytes();
      Ndc.HWCfg.bDhcpEnable = (byte)(cbx_DhcpEnable.Checked ? 1 : 0);

      Ndc.PortCfg[1].bNetMode = (byte)cbx_NetMode2.SelectedIndex;
      Ndc.PortCfg[1].bRandSportFlag = (byte)(cbx_RandSportFlag2.Checked ? 1 : 0);
      Ndc.PortCfg[1].wNetPort = Convert.ToUInt16(tbx_NetPort2.Text);
      Ndc.PortCfg[1].bDNSFlag = (byte)(cbx_DNSFlag2.SelectedIndex == 1 ? 1 : 0);
      Ndc.PortCfg[1].bDesIP = ibx_DesIP2.ToBytes();
      byte[] DomainNameByt = Encoding.UTF8.GetBytes(tbx_DomainName2.Text);
      if (DomainNameByt != null && DomainNameByt.Length > 0)
      {
        Buffer.BlockCopy(DomainNameByt, 0, Ndc.PortCfg[1].szDomainname, 0, DomainNameByt.Length);
      }
      Ndc.PortCfg[1].wDesPort = Convert.ToUInt16(tbx_DesPort2.Text);
      Ndc.PortCfg[1].dBaudRate = Convert.ToUInt32(cbx_BaudRate2.Text);
      Ndc.PortCfg[1].bDataSize = Convert.ToByte(cbx_DataSize2.Text);
      Ndc.PortCfg[1].bStopBits = Convert.ToByte(cbx_StopBits2.SelectedIndex);
      Ndc.PortCfg[1].bParity = Convert.ToByte(cbx_Parity2.SelectedIndex);
      Ndc.PortCfg[1].bPHYChangeHandle = (byte)(cbx_PHYChangeHandle2.Checked ? 1 : 0);
      Ndc.PortCfg[1].dRxPktlength = Convert.ToUInt32(tbx_RxPktlength2.Text);
      Ndc.PortCfg[1].dRxPktTimeout = Convert.ToUInt32(tbx_RxPktTimeout2.Text);
      Ndc.PortCfg[1].bResetCtrl = (byte)(cbx_ResetCtrl2.Checked ? 1 : 0);

      byte[] NdcByt = DLLProject.StructToBytes(Ndc);
      NetworkInterface NetworkIntf = cbx_Netinterface.SelectedItem as NetworkInterface;
      byte[] LocalMAC = NetworkIntf.GetPhysicalAddress().GetAddressBytes();
      byte[] DevMAC = HexStringToBytes(tbx_DevMAC.Text.Replace(":", ""));
      RetDword = DLLProject.NetCfg_SetInfo(0, LocalMAC, DevMAC, NdcByt, NdcByt.Length);
      ShowRetDword("NetCfg_SetInfo");







    }

    private void cbx_RandSportFlag1_CheckedChanged(object sender, EventArgs e)
    {
      tbx_NetPort1.Enabled = !cbx_RandSportFlag1.Checked;
    }

    private void cbx_DNSFlag1_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (cbx_DNSFlag1.SelectedIndex == 0)
      {
        tbx_DomainName1.Enabled = false;
        ibx_DesIP1.Enabled = true;
      }
      else
      {
        tbx_DomainName1.Enabled = true;
        ibx_DesIP1.Enabled = false;
      }
    }
    private void cbx_NetMode1_SelectedIndexChanged(object sender, EventArgs e)
    {
      cbx_DNSFlag1.Enabled = ibx_DesIP1.Enabled = tbx_DesPort1.Enabled = cbx_NetMode1.SelectedIndex == 0 ? false : true;
      cbx_DNSFlag1.SelectedIndex = 0;
    }

    private void cbx_NetMode2_SelectedIndexChanged(object sender, EventArgs e)
    {
      cbx_DNSFlag2.Enabled = ibx_DesIP2.Enabled = tbx_DesPort2.Enabled = cbx_NetMode2.SelectedIndex == 0 ? false : true;
      cbx_DNSFlag2.SelectedIndex = 0;
    }

    private void cbx_RandSportFlag2_CheckedChanged(object sender, EventArgs e)
    {
      tbx_NetPort2.Enabled = !cbx_RandSportFlag2.Checked;
    }

    private void cbx_DNSFlag2_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (cbx_DNSFlag2.SelectedIndex == 0)
      {
        tbx_DomainName2.Enabled = false;
        ibx_DesIP2.Enabled = true;
      }
      else
      {
        tbx_DomainName2.Enabled = true;
        ibx_DesIP2.Enabled = false;
      }
    }

    private void button4_Click(object sender, EventArgs e)
    {
      byte[] MacByt = HexStringToBytes(tbx_DevMAC.Text.Replace(":", ""));
      RetDword = DLLProject.NetCfg_FactoryReset(0, MacByt);
      ShowRetDword("NetCfg_FactoryReset");

    }

    private void btnStopRD_Click(object sender, EventArgs e)
    {
      RetDword = DLLProject.StopRead();
      ShowRetDword("StopRead");
    }

    private DLLProject.Svr_Receive SvrReceive;
    public int SvrCallReceive(byte Type, string IP, int Port, int LpRecSize, IntPtr LpRecByt)
    {
      Invoke((MethodInvoker)delegate ()
      {
        byte[] RecByt = null;
        textBox4.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff") + "\r\n");
        if (LpRecSize > 0)
        {
          RecByt = new byte[LpRecSize];
          System.Runtime.InteropServices.Marshal.Copy(LpRecByt, RecByt, 0, LpRecSize);
          string TmpStr = "Type:{0};IP:{1};Port:{2};Data:{3};";
          textBox4.AppendText(string.Format(TmpStr, Type.ToString(), IP, Port.ToString(), BitConverter.ToString(RecByt).Replace('-', ' ')) + "\r\n");
        }
        else
        {
          textBox4.AppendText(string.Format("Type:{0};IP:{1};Port:{2};", Type.ToString(), IP, Port.ToString()) + "\r\n");
        }
        textBox4.AppendText("\r\n");
      });
      return 1;
    }

    private void SvrType_CheckedChanged(object sender, EventArgs e)
    {
      cbxSvrValue.DropDownStyle = ComboBoxStyle.DropDownList;
      cbxSvrValue.Items.Clear();
      btnSvrConn.Enabled = false;
      cbxSvrValue.Enabled = true;
      if (rbSvrCom.Checked)
      {
        cbxSvrValue.Items.AddRange(SerialPort.GetPortNames().OrderBy(a => a).ToArray());
        if (cbxSvrValue.Items.Count > 0)
        {
          cbxSvrValue.SelectedIndex = 0;
        }
      }
      else if (rbSvrTcpCli.Checked || rbSvrTcpSvr.Checked || rbSvrUDP.Checked)
      {
        cbxSvrValue.DropDownStyle = ComboBoxStyle.Simple;
        cbxSvrValue.Enabled = true;
      }
      btnSvrConn.Enabled = true;

    }

    private void btnSvrConn_Click(object sender, EventArgs e)
    {
      string CommandStr = string.Empty;
      groupBox15.Enabled = false;
      SvrReceive = SvrCallReceive;
      switch (btnSvrConn.Tag.ToString())
      {
        case "0":
          {
            CommandStr = "Startup";
            if (rbSvrCom.Checked)
            {
              RetDword = DLLProject.Svr_Startup((byte)ConnType.Com, cbxSvrValue.Text, SvrReceive);
            }
            if (rbSvrUSB.Checked)
            {
              RetDword = DLLProject.Svr_Startup((byte)ConnType.USB, cbxSvrValue.Text, SvrReceive);
            }
            else if (rbSvrTcpCli.Checked)
            {
              RetDword = DLLProject.Svr_Startup((byte)ConnType.TcpCli, cbxSvrValue.Text, SvrReceive);
            }
            else if (rbSvrTcpSvr.Checked)
            {
              RetDword = DLLProject.Svr_Startup((byte)ConnType.TcpSvr, cbxSvrValue.Text, SvrReceive);
            }
            else if (rbSvrUDP.Checked)
            {
              RetDword = DLLProject.Svr_Startup((byte)ConnType.UDP, cbxSvrValue.Text, SvrReceive);
            }
            if (RetDword == 0)
            {
              flowLayoutPanel5.Enabled = cbxSvrValue.Enabled = false;
              btnSvrConn.Text = "CleanUp";
              btnSvrConn.Tag = "1";
              this.btnSvrConn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
              panel8.Enabled = rbSvrTcpSvr.Checked;
              groupBox15.Enabled = true;
            }
          }
          break;
        default:
          {
            CommandStr = "CleanUp";
            RetDword = DLLProject.Svr_CleanUp();
            if (RetDword == 0)
            {
              flowLayoutPanel5.Enabled = cbxSvrValue.Enabled = true;
              btnSvrConn.Text = "Startup";
              btnSvrConn.Tag = "0";
              this.btnSvrConn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            }
          }
          break;
      }
      ShowRetDword(CommandStr);

    }

    private void button5_Click(object sender, EventArgs e)
    {
      SvrType_CheckedChanged(null, null);
    }

    private void button6_Click(object sender, EventArgs e)
    {
      if (!string.IsNullOrEmpty(textBox5.Text))
      {
        byte[] SenByt= HexStringToBytes(textBox5.Text);
        RetDword = DLLProject.Svr_Send(ipInputBox1.Text+":"+ textBox6.Text, SenByt, SenByt.Length);
        ShowRetDword("Svr_Send");
      }
    }

        private void button8_Click(object sender, EventArgs e)
        {
            RetDword = DLLProject.ReadSingle();

            //ShowRetDword("ReadSingle");
            //ShowRetDword("Testing Chevacasoft");
        }

        private void Connect_and_Send_SQL(object data)
        {
            string queryString = "INSERT INTO dbo.vacas ([Nombre], [Estancia_ID], [Lista_animales_razas_ID], [Lista_animales_categorias_ID], [MGAP_ID]) VALUES ('testing_rfid',0,NULL,NULL,'" + data + "')";
            //string connectionString_LOCAL = @"Server = GBORDEROLLE1\SQLEXPRESS; Database=chevacadb;User Id=chevaca_login;Password=chevaca1234;";
            //string connectionString_REMOTE = @"metadata=res://*/Models.ChevacaDB.csdl|res://*/Models.ChevacaDB.ssdl|res://*/Models.ChevacaDB.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=sql5080.site4now.net;initial catalog=db_a4d7d8_chevacadb;user id=DB_A4D7D8_chevacadb_admin;password=chevaca1234;multipleactiveresultsets=True;application name=EntityFramework&quot;";
            string connectionString_REMOTE = @"Server = sql5080.site4now.net; Database=db_a4d7d8_chevacadb;User Id=DB_A4D7D8_chevacadb_admin;Password=chevaca1234;";

            using (SqlConnection connection = new SqlConnection(connectionString_REMOTE))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                //command.Parameters.AddWithValue("@tPatSName", "Your-Parm-Value");
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        //Console.WriteLine(String.Format("{0}, {1}",
                        //reader["tPatCulIntPatIDPk"], reader["tPatSFirstname"]));// etc
                    }
                }
                finally
                {
                    // Always call Close when done reading.
                    reader.Close();
                }
            }

        }
    }
}
