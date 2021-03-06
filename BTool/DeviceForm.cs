﻿using BTool.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using TI.Toolbox;

namespace BTool
{
	public class DeviceForm : Form
	{

		public delegate void DisplayMsgDelegate(SharedAppObjs.MsgType msgType, string msg);
		public delegate void DisplayMsgTimeDelegate(SharedAppObjs.MsgType msgType, string msg, string time);
		public delegate void DeviceTxDataDelegate(TxDataOut txDataOut);
		public delegate void DeviceRxDataDelegate(RxDataIn rxDataIn);
		public delegate bool HandleRxTxMessageDelegate(RxTxMgrData rxTxMgrData);

		private delegate void RxDataHandlerDelegate(byte[] data, int length);
		private delegate void DisplayRxCmdDelegate(RxDataIn rxDataIn, bool displayBytes);

		public enum EventType
		{
			Init,
			Scan,
			Establish,
			PairBond,
		}

		public enum GAPGetConnectionParams
		{
			None,
			MinConnIntSeq,
			MaxConnIntSeq,
			SlaveLatencySeq,
			SupervisionTimeoutSeq,
			MinConnIntSingle,
			MaxConnIntSingle,
			SlaveLatencySingle,
			SupervisionTimeoutSingle,
		}

		public static string moduleName = "DeviceForm";
		private const string s_delimeter = "------------------------------------------------------------------------------------------------------------------------\n";

		public event EventHandler BDAddressNotify;
		public event EventHandler ConnectionNotify;
		public event EventHandler DisconnectionNotify;
		public event EventHandler ChangeActiveRoot;
		public event EventHandler CloseActiveDevice;

		public DeviceInfo devInfo = new DeviceInfo();
		public AttrData attrData = new AttrData();
		public string BDAddressStr = "";
		public ConnectInfo disconnectInfo = new ConnectInfo();
		public List<ConnectInfo> Connections = new List<ConnectInfo>();
		public DeviceTabsForm devTabsForm;
		public ThreadMgr threadMgr;
		public SendCmds sendCmds;
		public int numConnections;
		public DeviceForm.GAPGetConnectionParams ConnParamState;

		#region HCICmds
		public HCICmds.HCIExtCmds.HCIExt_SetRxGain HCIExt_SetRxGain = new HCICmds.HCIExtCmds.HCIExt_SetRxGain();
		public HCICmds.HCIExtCmds.HCIExt_SetTxPower HCIExt_SetTxPower = new HCICmds.HCIExtCmds.HCIExt_SetTxPower();
		public HCICmds.HCIExtCmds.HCIExt_OnePktPerEvt HCIExt_OnePktPerEvt = new HCICmds.HCIExtCmds.HCIExt_OnePktPerEvt();
		public HCICmds.HCIExtCmds.HCIExt_ClkDivideOnHalt HCIExt_ClkDivideOnHalt = new HCICmds.HCIExtCmds.HCIExt_ClkDivideOnHalt();
		public HCICmds.HCIExtCmds.HCIExt_DeclareNvUsage HCIExt_DeclareNvUsage = new HCICmds.HCIExtCmds.HCIExt_DeclareNvUsage();
		public HCICmds.HCIExtCmds.HCIExt_Decrypt HCIExt_Decrypt = new HCICmds.HCIExtCmds.HCIExt_Decrypt();
		public HCICmds.HCIExtCmds.HCIExt_SetLocalSupportedFeatures HCIExt_SetLocalSupportedFeatures = new HCICmds.HCIExtCmds.HCIExt_SetLocalSupportedFeatures();
		public HCICmds.HCIExtCmds.HCIExt_SetFastTxRespTime HCIExt_SetFastTxRespTime = new HCICmds.HCIExtCmds.HCIExt_SetFastTxRespTime();
		public HCICmds.HCIExtCmds.HCIExt_ModemTestTx HCIExt_ModemTestTx = new HCICmds.HCIExtCmds.HCIExt_ModemTestTx();
		public HCICmds.HCIExtCmds.HCIExt_ModemHopTestTx HCIExt_ModemHopTestTx = new HCICmds.HCIExtCmds.HCIExt_ModemHopTestTx();
		public HCICmds.HCIExtCmds.HCIExt_ModemTestRx HCIExt_ModemTestRx = new HCICmds.HCIExtCmds.HCIExt_ModemTestRx();
		public HCICmds.HCIExtCmds.HCIExt_EndModemTest HCIExt_EndModemTest = new HCICmds.HCIExtCmds.HCIExt_EndModemTest();
		public HCICmds.HCIExtCmds.HCIExt_SetBDADDR HCIExt_SetBDADDR = new HCICmds.HCIExtCmds.HCIExt_SetBDADDR();
		public HCICmds.HCIExtCmds.HCIExt_SetSCA HCIExt_SetSCA = new HCICmds.HCIExtCmds.HCIExt_SetSCA();
		public HCICmds.HCIExtCmds.HCIExt_EnablePTM HCIExt_EnablePTM = new HCICmds.HCIExtCmds.HCIExt_EnablePTM();
		public HCICmds.HCIExtCmds.HCIExt_SetFreqTune HCIExt_SetFreqTune = new HCICmds.HCIExtCmds.HCIExt_SetFreqTune();
		public HCICmds.HCIExtCmds.HCIExt_SaveFreqTune HCIExt_SaveFreqTune = new HCICmds.HCIExtCmds.HCIExt_SaveFreqTune();
		public HCICmds.HCIExtCmds.HCIExt_SetMaxDtmTxPower HCIExt_SetMaxDtmTxPower = new HCICmds.HCIExtCmds.HCIExt_SetMaxDtmTxPower();
		public HCICmds.HCIExtCmds.HCIExt_MapPmIoPort HCIExt_MapPmIoPort = new HCICmds.HCIExtCmds.HCIExt_MapPmIoPort();
		public HCICmds.HCIExtCmds.HCIExt_DisconnectImmed HCIExt_DisconnectImmed = new HCICmds.HCIExtCmds.HCIExt_DisconnectImmed();
		public HCICmds.HCIExtCmds.HCIExt_PER HCIExt_PER = new HCICmds.HCIExtCmds.HCIExt_PER();
		public HCICmds.L2CAPCmds.L2CAP_InfoReq L2CAP_InfoReq = new HCICmds.L2CAPCmds.L2CAP_InfoReq();
		public HCICmds.L2CAPCmds.L2CAP_ConnParamUpdateReq L2CAP_ConnParamUpdateReq = new HCICmds.L2CAPCmds.L2CAP_ConnParamUpdateReq();
		public HCICmds.ATTCmds.ATT_ErrorRsp ATT_ErrorRsp = new HCICmds.ATTCmds.ATT_ErrorRsp();
		public HCICmds.ATTCmds.ATT_ExchangeMTUReq ATT_ExchangeMTUReq = new HCICmds.ATTCmds.ATT_ExchangeMTUReq();
		public HCICmds.ATTCmds.ATT_ExchangeMTURsp ATT_ExchangeMTURsp = new HCICmds.ATTCmds.ATT_ExchangeMTURsp();
		public HCICmds.ATTCmds.ATT_FindInfoReq ATT_FindInfoReq = new HCICmds.ATTCmds.ATT_FindInfoReq();
		public HCICmds.ATTCmds.ATT_FindInfoRsp ATT_FindInfoRsp = new HCICmds.ATTCmds.ATT_FindInfoRsp();
		public HCICmds.ATTCmds.ATT_FindByTypeValueReq ATT_FindByTypeValueReq = new HCICmds.ATTCmds.ATT_FindByTypeValueReq();
		public HCICmds.ATTCmds.ATT_FindByTypeValueRsp ATT_FindByTypeValueRsp = new HCICmds.ATTCmds.ATT_FindByTypeValueRsp();
		public HCICmds.ATTCmds.ATT_ReadByTypeReq ATT_ReadByTypeReq = new HCICmds.ATTCmds.ATT_ReadByTypeReq();
		public HCICmds.ATTCmds.ATT_ReadByTypeRsp ATT_ReadByTypeRsp = new HCICmds.ATTCmds.ATT_ReadByTypeRsp();
		public HCICmds.ATTCmds.ATT_ReadReq ATT_ReadReq = new HCICmds.ATTCmds.ATT_ReadReq();
		public HCICmds.ATTCmds.ATT_ReadRsp ATT_ReadRsp = new HCICmds.ATTCmds.ATT_ReadRsp();
		public HCICmds.ATTCmds.ATT_ReadBlobReq ATT_ReadBlobReq = new HCICmds.ATTCmds.ATT_ReadBlobReq();
		public HCICmds.ATTCmds.ATT_ReadBlobRsp ATT_ReadBlobRsp = new HCICmds.ATTCmds.ATT_ReadBlobRsp();
		public HCICmds.ATTCmds.ATT_ReadMultiReq ATT_ReadMultiReq = new HCICmds.ATTCmds.ATT_ReadMultiReq();
		public HCICmds.ATTCmds.ATT_ReadMultiRsp ATT_ReadMultiRsp = new HCICmds.ATTCmds.ATT_ReadMultiRsp();
		public HCICmds.ATTCmds.ATT_ReadByGrpTypeReq ATT_ReadByGrpTypeReq = new HCICmds.ATTCmds.ATT_ReadByGrpTypeReq();
		public HCICmds.ATTCmds.ATT_ReadByGrpTypeRsp ATT_ReadByGrpTypeRsp = new HCICmds.ATTCmds.ATT_ReadByGrpTypeRsp();
		public HCICmds.ATTCmds.ATT_WriteReq ATT_WriteReq = new HCICmds.ATTCmds.ATT_WriteReq();
		public HCICmds.ATTCmds.ATT_WriteRsp ATT_WriteRsp = new HCICmds.ATTCmds.ATT_WriteRsp();
		public HCICmds.ATTCmds.ATT_PrepareWriteReq ATT_PrepareWriteReq = new HCICmds.ATTCmds.ATT_PrepareWriteReq();
		public HCICmds.ATTCmds.ATT_PrepareWriteRsp ATT_PrepareWriteRsp = new HCICmds.ATTCmds.ATT_PrepareWriteRsp();
		public HCICmds.ATTCmds.ATT_ExecuteWriteReq ATT_ExecuteWriteReq = new HCICmds.ATTCmds.ATT_ExecuteWriteReq();
		public HCICmds.ATTCmds.ATT_ExecuteWriteRsp ATT_ExecuteWriteRsp = new HCICmds.ATTCmds.ATT_ExecuteWriteRsp();
		public HCICmds.ATTCmds.ATT_HandleValueNotification ATT_HandleValueNotification = new HCICmds.ATTCmds.ATT_HandleValueNotification();
		public HCICmds.ATTCmds.ATT_HandleValueIndication ATT_HandleValueIndication = new HCICmds.ATTCmds.ATT_HandleValueIndication();
		public HCICmds.ATTCmds.ATT_HandleValueConfirmation ATT_HandleValueConfirmation = new HCICmds.ATTCmds.ATT_HandleValueConfirmation();
		public HCICmds.GATTCmds.GATT_ExchangeMTU GATT_ExchangeMTU = new HCICmds.GATTCmds.GATT_ExchangeMTU();
		public HCICmds.GATTCmds.GATT_DiscAllPrimaryServices GATT_DiscAllPrimaryServices = new HCICmds.GATTCmds.GATT_DiscAllPrimaryServices();
		public HCICmds.GATTCmds.GATT_DiscPrimaryServiceByUUID GATT_DiscPrimaryServiceByUUID = new HCICmds.GATTCmds.GATT_DiscPrimaryServiceByUUID();
		public HCICmds.GATTCmds.GATT_FindIncludedServices GATT_FindIncludedServices = new HCICmds.GATTCmds.GATT_FindIncludedServices();
		public HCICmds.GATTCmds.GATT_DiscAllChars GATT_DiscAllChars = new HCICmds.GATTCmds.GATT_DiscAllChars();
		public HCICmds.GATTCmds.GATT_DiscCharsByUUID GATT_DiscCharsByUUID = new HCICmds.GATTCmds.GATT_DiscCharsByUUID();
		public HCICmds.GATTCmds.GATT_DiscAllCharDescs GATT_DiscAllCharDescs = new HCICmds.GATTCmds.GATT_DiscAllCharDescs();
		public HCICmds.GATTCmds.GATT_ReadCharValue GATT_ReadCharValue = new HCICmds.GATTCmds.GATT_ReadCharValue();
		public HCICmds.GATTCmds.GATT_ReadUsingCharUUID GATT_ReadUsingCharUUID = new HCICmds.GATTCmds.GATT_ReadUsingCharUUID();
		public HCICmds.GATTCmds.GATT_ReadLongCharValue GATT_ReadLongCharValue = new HCICmds.GATTCmds.GATT_ReadLongCharValue();
		public HCICmds.GATTCmds.GATT_ReadMultiCharValues GATT_ReadMultiCharValues = new HCICmds.GATTCmds.GATT_ReadMultiCharValues();
		public HCICmds.GATTCmds.GATT_WriteNoRsp GATT_WriteNoRsp = new HCICmds.GATTCmds.GATT_WriteNoRsp();
		public HCICmds.GATTCmds.GATT_SignedWriteNoRsp GATT_SignedWriteNoRsp = new HCICmds.GATTCmds.GATT_SignedWriteNoRsp();
		public HCICmds.GATTCmds.GATT_WriteCharValue GATT_WriteCharValue = new HCICmds.GATTCmds.GATT_WriteCharValue();
		public HCICmds.GATTCmds.GATT_WriteLongCharValue GATT_WriteLongCharValue = new HCICmds.GATTCmds.GATT_WriteLongCharValue();
		public HCICmds.GATTCmds.GATT_ReliableWrites GATT_ReliableWrites = new HCICmds.GATTCmds.GATT_ReliableWrites();
		public HCICmds.GATTCmds.GATT_ReadCharDesc GATT_ReadCharDesc = new HCICmds.GATTCmds.GATT_ReadCharDesc();
		public HCICmds.GATTCmds.GATT_ReadLongCharDesc GATT_ReadLongCharDesc = new HCICmds.GATTCmds.GATT_ReadLongCharDesc();
		public HCICmds.GATTCmds.GATT_WriteCharDesc GATT_WriteCharDesc = new HCICmds.GATTCmds.GATT_WriteCharDesc();
		public HCICmds.GATTCmds.GATT_WriteLongCharDesc GATT_WriteLongCharDesc = new HCICmds.GATTCmds.GATT_WriteLongCharDesc();
		public HCICmds.GATTCmds.GATT_Notification GATT_Notification = new HCICmds.GATTCmds.GATT_Notification();
		public HCICmds.GATTCmds.GATT_Indication GATT_Indication = new HCICmds.GATTCmds.GATT_Indication();
		public HCICmds.GATTCmds.GATT_AddService GATT_AddService = new HCICmds.GATTCmds.GATT_AddService();
		public HCICmds.GATTCmds.GATT_DelService GATT_DelService = new HCICmds.GATTCmds.GATT_DelService();
		public HCICmds.GATTCmds.GATT_AddAttribute GATT_AddAttribute = new HCICmds.GATTCmds.GATT_AddAttribute();
		public HCICmds.GAPCmds.GAP_DeviceInit GAP_DeviceInit = new HCICmds.GAPCmds.GAP_DeviceInit();
		public HCICmds.GAPCmds.GAP_ConfigDeviceAddr GAP_ConfigDeviceAddr = new HCICmds.GAPCmds.GAP_ConfigDeviceAddr();
		public HCICmds.GAPCmds.GAP_DeviceDiscoveryRequest GAP_DeviceDiscoveryRequest = new HCICmds.GAPCmds.GAP_DeviceDiscoveryRequest();
		public HCICmds.GAPCmds.GAP_DeviceDiscoveryCancel GAP_DeviceDiscoveryCancel = new HCICmds.GAPCmds.GAP_DeviceDiscoveryCancel();
		public HCICmds.GAPCmds.GAP_MakeDiscoverable GAP_MakeDiscoverable = new HCICmds.GAPCmds.GAP_MakeDiscoverable();
		public HCICmds.GAPCmds.GAP_UpdateAdvertisingData GAP_UpdateAdvertisingData = new HCICmds.GAPCmds.GAP_UpdateAdvertisingData();
		public HCICmds.GAPCmds.GAP_EndDiscoverable GAP_EndDiscoverable = new HCICmds.GAPCmds.GAP_EndDiscoverable();
		public HCICmds.GAPCmds.GAP_EstablishLinkRequest GAP_EstablishLinkRequest = new HCICmds.GAPCmds.GAP_EstablishLinkRequest();
		public HCICmds.GAPCmds.GAP_TerminateLinkRequest GAP_TerminateLinkRequest = new HCICmds.GAPCmds.GAP_TerminateLinkRequest();
		public HCICmds.GAPCmds.GAP_Authenticate GAP_Authenticate = new HCICmds.GAPCmds.GAP_Authenticate();
		public HCICmds.GAPCmds.GAP_PasskeyUpdate GAP_PasskeyUpdate = new HCICmds.GAPCmds.GAP_PasskeyUpdate();
		public HCICmds.GAPCmds.GAP_SlaveSecurityRequest GAP_SlaveSecurityRequest = new HCICmds.GAPCmds.GAP_SlaveSecurityRequest();
		public HCICmds.GAPCmds.GAP_Signable GAP_Signable = new HCICmds.GAPCmds.GAP_Signable();
		public HCICmds.GAPCmds.GAP_Bond GAP_Bond = new HCICmds.GAPCmds.GAP_Bond();
		public HCICmds.GAPCmds.GAP_TerminateAuth GAP_TerminateAuth = new HCICmds.GAPCmds.GAP_TerminateAuth();
		public HCICmds.GAPCmds.GAP_UpdateLinkParamReq GAP_UpdateLinkParamReq = new HCICmds.GAPCmds.GAP_UpdateLinkParamReq();
		public HCICmds.GAPCmds.GAP_SetParam GAP_SetParam = new HCICmds.GAPCmds.GAP_SetParam();
		public HCICmds.GAPCmds.GAP_GetParam GAP_GetParam = new HCICmds.GAPCmds.GAP_GetParam();
		public HCICmds.GAPCmds.GAP_ResolvePrivateAddr GAP_ResolvePrivateAddr = new HCICmds.GAPCmds.GAP_ResolvePrivateAddr();
		public HCICmds.GAPCmds.GAP_SetAdvToken GAP_SetAdvToken = new HCICmds.GAPCmds.GAP_SetAdvToken();
		public HCICmds.GAPCmds.GAP_RemoveAdvToken GAP_RemoveAdvToken = new HCICmds.GAPCmds.GAP_RemoveAdvToken();
		public HCICmds.GAPCmds.GAP_UpdateAdvTokens GAP_UpdateAdvTokens = new HCICmds.GAPCmds.GAP_UpdateAdvTokens();
		public HCICmds.GAPCmds.GAP_BondSetParam GAP_BondSetParam = new HCICmds.GAPCmds.GAP_BondSetParam();
		public HCICmds.GAPCmds.GAP_BondGetParam GAP_BondGetParam = new HCICmds.GAPCmds.GAP_BondGetParam();
		public HCICmds.UTILCmds.UTIL_Reset UTIL_Reset = new HCICmds.UTILCmds.UTIL_Reset();
		public HCICmds.UTILCmds.UTIL_NVRead UTIL_NVRead = new HCICmds.UTILCmds.UTIL_NVRead();
		public HCICmds.UTILCmds.UTIL_NVWrite UTIL_NVWrite = new HCICmds.UTILCmds.UTIL_NVWrite();
		public HCICmds.UTILCmds.UTIL_ForceBoot UTIL_ForceBoot = new HCICmds.UTILCmds.UTIL_ForceBoot();
		public HCICmds.HCIOtherCmds.HCIOther_ReadRSSI HCIOther_ReadRSSI = new HCICmds.HCIOtherCmds.HCIOther_ReadRSSI();
		public HCICmds.HCIOtherCmds.HCIOther_LEClearWhiteList HCIOther_LEClearWhiteList = new HCICmds.HCIOtherCmds.HCIOther_LEClearWhiteList();
		public HCICmds.HCIOtherCmds.HCIOther_LEAddDeviceToWhiteList HCIOther_LEAddDeviceToWhiteList = new HCICmds.HCIOtherCmds.HCIOther_LEAddDeviceToWhiteList();
		public HCICmds.HCIOtherCmds.HCIOther_LERemoveDeviceFromWhiteList HCIOther_LERemoveDeviceFromWhiteList = new HCICmds.HCIOtherCmds.HCIOther_LERemoveDeviceFromWhiteList();
		public HCICmds.HCIOtherCmds.HCIOther_LEConnectionUpdate HCIOther_LEConnectionUpdate = new HCICmds.HCIOtherCmds.HCIOther_LEConnectionUpdate();
		public HCICmds.MISCCmds.MISC_GenericCommand MISC_GenericCommand = new HCICmds.MISCCmds.MISC_GenericCommand();
		public HCICmds.MISCCmds.MISC_RawTxMessage MISC_RawTxMessage = new HCICmds.MISCCmds.MISC_RawTxMessage();
		#endregion

		private MsgBox msgBox = new MsgBox();
		private int[] EventTimeout = new int[4]
	    {
			10000,
			75000,
			30000,
			50000
		};

		private DisplayCmdUtils dspCmdUtils = new DisplayCmdUtils();
		private CommSelectForm commSelectForm;
		private AttributesForm attributesForm;
		private MsgLogForm msgLogForm;
		private CommParser commParser = new CommParser();
		private DataUtils dataUtils = new DataUtils();
		private SharedObjects sharedObjs = new SharedObjects();
		private DisplayTxCmds dspTxCmds = new DisplayTxCmds();
		private Mutex dspMsgMutex = new Mutex();
		private CommManager commMgr = new CommManager();
		private DeviceFormUtils devUtils = new DeviceFormUtils();
		private ConnectInfo connectInfo = new ConnectInfo();

		private Thread processRxProc;
		private bool DeviceStarted;
		private bool formClosing;
		private IContainer components;
		private SplitContainer scTopLeftRight;
		private System.Windows.Forms.Timer scanTimer;
		private System.Windows.Forms.Timer initTimer;
		private System.Windows.Forms.Timer establishTimer;
		private System.Windows.Forms.Timer pairBondTimer;
		private Panel plUserTabs;
		private SplitContainer scTopBottom;
		private Panel plLog;
		private Panel plAttributes;

		static DeviceForm()
		{
		}

		public DeviceForm()
		{
			devInfo.DevForm = this;
			connectInfo.BDA = "00:00:00:00:00:00";
			connectInfo.Handle = 0;
			connectInfo.AddrType = 0;
			disconnectInfo.BDA = "00:00:00:00:00:00";
			disconnectInfo.Handle = 0;
			disconnectInfo.AddrType = 0;
			Connections.Clear();
			commMgr.InitCommManager();
			msgLogForm = new MsgLogForm(this);
			commSelectForm = new CommSelectForm();

			InitializeComponent();

			Text = FormMain.ProgramTitle + FormMain.ProgramVersion;
			threadMgr = new ThreadMgr(this);
			sendCmds = new SendCmds(this);
			attrData.sendAutoCmds = false;
			attributesForm = new AttributesForm(this);
			devTabsForm = new DeviceTabsForm(this);

			LoadUserInitializeValues();
			LoadUserSettings();

			sendCmds.DisplayMsgCallback = new DeviceForm.DisplayMsgDelegate(DisplayMsg);

			threadMgr.txDataOut.DeviceTxDataCallback = new DeviceForm.DeviceTxDataDelegate(DeviceTxData);
			threadMgr.txDataOut.DisplayMsgCallback = new DeviceForm.DisplayMsgDelegate(DisplayMsg);
			threadMgr.rxDataIn.DeviceRxDataCallback = new DeviceForm.DeviceRxDataDelegate(DeviceRxData);
			threadMgr.rxTxMgr.HandleRxTxMessageCallback = new DeviceForm.HandleRxTxMessageDelegate(HandleRxTxMessage);

			dspTxCmds.DisplayMsgCallback = new DeviceForm.DisplayMsgDelegate(DisplayMsg);
			dspTxCmds.DisplayMsgTimeCallback = new DeviceForm.DisplayMsgTimeDelegate(DisplayMsgTime);
			attributesForm.DisplayMsgCallback = new DeviceForm.DisplayMsgDelegate(DisplayMsg);
			msgLogForm.DisplayMsgCallback = new DeviceForm.DisplayMsgDelegate(DisplayMsg);
			devTabsForm.DisplayMsgCallback = new DeviceForm.DisplayMsgDelegate(DisplayMsg);

			threadMgr.Init(this);

			msgLogForm.TopLevel = false;
			msgLogForm.Parent = plLog;
			msgLogForm.Visible = true;
			msgLogForm.Dock = DockStyle.Fill;
			msgLogForm.ControlBox = false;
			msgLogForm.ShowIcon = false;
			msgLogForm.FormBorderStyle = FormBorderStyle.None;
			msgLogForm.StartPosition = FormStartPosition.Manual;
			msgLogForm.Show();

			devTabsForm.TopLevel = false;
			devTabsForm.Parent = plUserTabs;
			devTabsForm.Visible = true;
			devTabsForm.Dock = DockStyle.Fill;
			devTabsForm.ControlBox = false;
			devTabsForm.ShowIcon = false;
			devTabsForm.FormBorderStyle = FormBorderStyle.None;
			devTabsForm.StartPosition = FormStartPosition.Manual;
			devTabsForm.Show();

			attributesForm.TopLevel = false;
			attributesForm.Parent = plAttributes;
			attributesForm.Visible = true;
			attributesForm.Dock = DockStyle.Fill;
			attributesForm.ControlBox = false;
			attributesForm.ShowIcon = false;
			attributesForm.FormBorderStyle = FormBorderStyle.None;
			attributesForm.StartPosition = FormStartPosition.Manual;
			attributesForm.Show();
		}

		~DeviceForm()
		{
			DeviceFormClose(true);
		}

		public void DeviceFormClose(bool closeDevice)
		{
			if (closeDevice && !formClosing)
			{
				formClosing = true;
				CloseActiveDevice(this, null);
			}
			threadMgr.PauseThreads();
			threadMgr.WaitForPause();
			threadMgr.ClearQueues();
			commMgr.ClosePort();
			if (processRxProc != null)
				while (processRxProc.IsAlive)
					;
			msgLogForm.ResetMsgNumber();
			threadMgr.ExitThreads();
			SaveUserSettings();
		}

		protected void OnBDAddressNotify(string deviceBDAddressStr)
		{
			BDAddressStr = deviceBDAddressStr;
			BDAddressNotify(this, EventArgs.Empty);
		}

		protected void OnConnectionNotify(ref ConnectInfo tmpConnectInfo)
		{
			++numConnections;
			connectInfo = tmpConnectInfo;
			Connections.Add(connectInfo);
			ConnectionNotify(this, EventArgs.Empty);
			DisplayMsg(SharedAppObjs.MsgType.Info, "Device Connected\nHandle = 0x" + connectInfo.Handle.ToString("X4") + "\nAddr Type = 0x" + connectInfo.AddrType.ToString("X2") + " (" + devUtils.GetGapAddrTypeStr(connectInfo.AddrType) + ")\nBDAddr = " + connectInfo.BDA + "\n");
			attributesForm.RemoveData(connectInfo.Handle);
		}

		protected void OnDisconnectionNotify(ref ConnectInfo tmpDisconnectInfo)
		{
			disconnectInfo = tmpDisconnectInfo;
			for (int index = 0; index < Connections.Count; ++index)
			{
				if (Connections[index].Handle == disconnectInfo.Handle)
				{
					DisplayMsg(SharedAppObjs.MsgType.Info, "Device Disconnected\nHandle = 0x" + disconnectInfo.Handle.ToString("X4") + "\nAddr Type = 0x" + Connections[index].AddrType.ToString("X2") + " (" + devUtils.GetGapAddrTypeStr(Connections[index].AddrType) + ")\nBDAddr = " + Connections[index].BDA + "\n");
					Connections.RemoveAt(index);
					DisconnectionNotify(this, EventArgs.Empty);
					if (numConnections > 0)
						--numConnections;
					attributesForm.RemoveData(disconnectInfo.Handle);
					break;
				}
			}
		}

		public bool DeviceFormInit()
		{
			bool flag = false;
			if (commSelectForm.ShowDialog() == DialogResult.OK)
			{
				commMgr.PortName = commSelectForm.cbPorts.Text;
				commMgr.BaudRate = commSelectForm.cbBaud.Text;
				commMgr.DataBits = commSelectForm.cbDataBits.Text;
				commMgr.Parity = commSelectForm.cbParity.Text;
				commMgr.StopBits = commSelectForm.cbStopBits.Text;
				commMgr.HandShake = (Handshake)commSelectForm.cbFlow.SelectedIndex;
				commMgr.CurrentTransmissionType = CommManager.TransmissionType.Hex;
				commMgr.DisplayMsgCallback = new DisplayMsgDelegate(DisplayMsg);

				if (commMgr.OpenPort())
				{
					Text = commSelectForm.cbPorts.Text;

					devInfo.DevName = commMgr.PortName;
					devInfo.ConnectStatus = "None";
					devInfo.ComPortInfo.BaudRate = commMgr.BaudRate;
					devInfo.ComPortInfo.ComPort = commMgr.PortName;
					devInfo.ComPortInfo.Flow = commSelectForm.cbFlow.Text;
					devInfo.ComPortInfo.DataBits = commMgr.DataBits;
					devInfo.ComPortInfo.Parity = commMgr.Parity;
					devInfo.ComPortInfo.StopBits = commMgr.StopBits;

					commMgr.RxDataInd = new CommManager.FP_ReceiveDataInd(RxDataHandler);

					processRxProc = new Thread(new ThreadStart(ProcessRxProc));
					processRxProc.Name = "ProcessRxProcThread";
					processRxProc.Start();
					while (!processRxProc.IsAlive)
					{ }
					flag = true;
				}
				else
				{
					string msg = string.Format("Failed Connecting To {0}\n", commSelectForm.cbPorts.SelectedItem);
					msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, msg);
					DisplayMsg(SharedAppObjs.MsgType.Error, msg);
				}
			}
			return flag;
		}

		private void LoadUserInitializeValues()
		{
			initTimer.Interval = EventTimeout[0];
			initTimer.Tick += new EventHandler(timerInitEvent);

			scanTimer.Interval = EventTimeout[1];
			scanTimer.Tick += new EventHandler(timerScanEvent);

			establishTimer.Interval = EventTimeout[2];
			establishTimer.Tick += new EventHandler(timerEstablishEvent);

			pairBondTimer.Interval = EventTimeout[3];
			pairBondTimer.Tick += new EventHandler(timerPairBondEvent);

			devTabsForm.TabAdvCommandsInitValues();
			devTabsForm.TabDiscoverConnectInitValues();
			devTabsForm.TabPairBondInitValues();
			devTabsForm.TabReadWriteInitValues();
		}

		public void SendGAPDeviceInit()
		{
			devTabsForm.ShowProgress(true);
			devTabsForm.UserTabAccess(false);
			StartTimer(DeviceForm.EventType.Init);
			sendCmds.SendGAP(GAP_DeviceInit);
		}

		private void DeviceTxData(TxDataOut txDataOut)
		{
			if (InvokeRequired)
			{
				try
				{
					BeginInvoke((Delegate)new DeviceForm.DeviceTxDataDelegate(DeviceTxData), txDataOut);
				}
				catch { }
			}
			else
			{
				if (formClosing)
					return;
				threadMgr.rxTxMgr.dataQ.AddQTail(
					new RxTxMgrData()
					{
						rxDataIn = null,
						txDataOut = txDataOut
					});
			}
		}

		private void DeviceRxData(RxDataIn rxDataIn)
		{
			if (InvokeRequired)
			{
				try
				{
					BeginInvoke((Delegate)new DeviceForm.DeviceRxDataDelegate(DeviceRxData), rxDataIn);
				}
				catch { }
			}
			else
			{
				if (formClosing)
					return;
				threadMgr.rxTxMgr.dataQ.AddQTail((object)new RxTxMgrData()
				{
					rxDataIn = rxDataIn,
					txDataOut = null
				});
			}
		}

		internal void RxDataHandler(byte[] data, int length)
		{
			if (InvokeRequired)
				BeginInvoke((Delegate)new DeviceForm.RxDataHandlerDelegate(RxDataHandler), data, length);
			else
				commParser.EnQueueData(data);
		}

		private void ProcessRxProc()
		{
			byte type = 0;
			ushort opCode = 0xFFFF;
			ushort eventOpCode = 0xFFFF;
			byte length = 0;
			byte[] data = null;
			SharedObjects.Log.Write(Logging.MsgType.Debug, "ProcessRxProc", "Starting Thread");
			while (!formClosing)
			{
				if (commParser.GetDataSize() == 0)
					Thread.Sleep(10);

				if (!commParser.ParseData(ref type, ref opCode, ref eventOpCode, ref length, ref data))
					continue;

				if (formClosing)
					break;

				threadMgr.rxDataIn.DataQueue.AddQTail(
					new RxDataIn()
					{
						RxType = type,
						CmdOpcode = opCode,
						EventOpcode = eventOpCode,
						Length = length,
						Data = data
					});
				type = 0;
				opCode = 0xFFFF;
				eventOpCode = 0xFFFF;
				length = 0;
				data = null;
			}
			SharedObjects.Log.Write(Logging.MsgType.Debug, "ProcessRxProc", "Exiting Thread");
		}

		private bool HandleRxTxMessage(RxTxMgrData rxTxMgrData)
		{
			bool flag = true;
			if (InvokeRequired)
			{
				try
				{
					Invoke((Delegate)new DeviceForm.HandleRxTxMessageDelegate(HandleRxTxMessage), rxTxMgrData);
				}
				catch { }
			}
			else
			{
				if (formClosing)
					return flag;
				if (rxTxMgrData.rxDataIn != null)
					DisplayRxCmd(rxTxMgrData.rxDataIn, msgLogForm.GetDisplayRxDumps());
				else if (rxTxMgrData.txDataOut != null)
				{
					if (commMgr.ComPort.IsOpen)
					{
						dspTxCmds.DisplayTxCmd(rxTxMgrData.txDataOut, msgLogForm.GetDisplayTxDumps());
						string str = "";
						foreach (byte num in rxTxMgrData.txDataOut.Data)
							str = str + string.Format("{0:X2} ", num);
						flag = commMgr.WriteData(str.Trim());
						if (!flag && threadMgr.rxDataIn.DeviceTxStopWaitCallback != null)
							threadMgr.rxDataIn.DeviceTxStopWaitCallback(false);
					}
					else
					{
						msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Warning, string.Format("Attempt To Send Empty Message Detected\nRequest Ignored\n", commSelectForm.cbPorts.SelectedItem));
						flag = false;
					}
				}
			}
			return flag;
		}

		public void DisplayMsg(SharedAppObjs.MsgType msgType, string msg)
		{
			if (InvokeRequired)
			{
				try
				{
					BeginInvoke((Delegate)new DeviceForm.DisplayMsgDelegate(DisplayMsg), msgType, msg);
				}
				catch { }
			}
			else
				msgLogForm.DisplayLogMsg(msgType, msg, null);
		}

		public void DisplayMsgTime(SharedAppObjs.MsgType msgType, string msg, string time)
		{
			dspMsgMutex.WaitOne();
			if (InvokeRequired)
			{
				try
				{
					BeginInvoke((Delegate)new DeviceForm.DisplayMsgTimeDelegate(DisplayMsgTime), msgType, msg, time);
				}
				catch { }
			}
			else
				msgLogForm.DisplayLogMsg(msgType, msg, time);
			dspMsgMutex.ReleaseMutex();
		}

		private void deviceForm_Activated(object sender, EventArgs e)
		{
			ChangeActiveRoot(this, null);
		}

		private void DeviceForm_Load(object sender, EventArgs e)
		{
			if (SharedObjects.IsMonoRunning())
				scTopBottom.SplitterDistance = 550;
			else
				scTopBottom.SplitterDistance = 530;
		}

		private void DeviceForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			DeviceFormClose(true);
		}

		private void LoadUserSettings()
		{
			attributesForm.LoadUserSettings();
		}

		private void SaveUserSettings()
		{
			attributesForm.SaveUserSettings();
			Settings.Default.Save();
		}

		private void scTopLeftRightPanel2_SizeChanged(object sender, EventArgs e)
		{
			int width = scTopLeftRight.Panel2.Width;
			int maxWidth = 427;
			if (devTabsForm != null)
				maxWidth = devTabsForm.GetTcDeviceTabsWidth() + 15;
			int split = Width - maxWidth - 10;
			if (width > maxWidth && split > 1)
				scTopLeftRight.SplitterDistance = split;
			scTopLeftRight.Update();
			if (devTabsForm != null)
				devTabsForm.DeviceTabsUpdate();
		}

		private void DeviceForm_LocationChanged(object sender, EventArgs e)
		{
			Location = new Point(0, 0);
		}

		public ConnectInfo GetConnectInfo()
		{
			return connectInfo;
		}

		#region InitializeComponent
		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
				components.Dispose();
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.scTopLeftRight = new System.Windows.Forms.SplitContainer();
			this.plLog = new System.Windows.Forms.Panel();
			this.plUserTabs = new System.Windows.Forms.Panel();
			this.scanTimer = new System.Windows.Forms.Timer(this.components);
			this.initTimer = new System.Windows.Forms.Timer(this.components);
			this.establishTimer = new System.Windows.Forms.Timer(this.components);
			this.pairBondTimer = new System.Windows.Forms.Timer(this.components);
			this.scTopBottom = new System.Windows.Forms.SplitContainer();
			this.plAttributes = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this.scTopLeftRight)).BeginInit();
			this.scTopLeftRight.Panel1.SuspendLayout();
			this.scTopLeftRight.Panel2.SuspendLayout();
			this.scTopLeftRight.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.scTopBottom)).BeginInit();
			this.scTopBottom.Panel1.SuspendLayout();
			this.scTopBottom.Panel2.SuspendLayout();
			this.scTopBottom.SuspendLayout();
			this.SuspendLayout();
			// 
			// scTopLeftRight
			// 
			this.scTopLeftRight.BackColor = System.Drawing.SystemColors.Highlight;
			this.scTopLeftRight.Dock = System.Windows.Forms.DockStyle.Fill;
			this.scTopLeftRight.Location = new System.Drawing.Point(0, 0);
			this.scTopLeftRight.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.scTopLeftRight.Name = "scTopLeftRight";
			// 
			// scTopLeftRight.Panel1
			// 
			this.scTopLeftRight.Panel1.Controls.Add(this.plLog);
			// 
			// scTopLeftRight.Panel2
			// 
			this.scTopLeftRight.Panel2.Controls.Add(this.plUserTabs);
			this.scTopLeftRight.Panel2.SizeChanged += new System.EventHandler(this.scTopLeftRightPanel2_SizeChanged);
			this.scTopLeftRight.Size = new System.Drawing.Size(780, 530);
			this.scTopLeftRight.SplitterDistance = 376;
			this.scTopLeftRight.TabIndex = 11;
			// 
			// plLog
			// 
			this.plLog.BackColor = System.Drawing.SystemColors.Control;
			this.plLog.Dock = System.Windows.Forms.DockStyle.Fill;
			this.plLog.Location = new System.Drawing.Point(0, 0);
			this.plLog.Name = "plLog";
			this.plLog.Size = new System.Drawing.Size(376, 530);
			this.plLog.TabIndex = 0;
			// 
			// plUserTabs
			// 
			this.plUserTabs.AutoScroll = true;
			this.plUserTabs.BackColor = System.Drawing.SystemColors.Control;
			this.plUserTabs.Dock = System.Windows.Forms.DockStyle.Fill;
			this.plUserTabs.Location = new System.Drawing.Point(0, 0);
			this.plUserTabs.Name = "plUserTabs";
			this.plUserTabs.Size = new System.Drawing.Size(400, 530);
			this.plUserTabs.TabIndex = 0;
			// 
			// scTopBottom
			// 
			this.scTopBottom.BackColor = System.Drawing.SystemColors.Highlight;
			this.scTopBottom.Dock = System.Windows.Forms.DockStyle.Fill;
			this.scTopBottom.Location = new System.Drawing.Point(0, 0);
			this.scTopBottom.Name = "scTopBottom";
			this.scTopBottom.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// scTopBottom.Panel1
			// 
			this.scTopBottom.Panel1.Controls.Add(this.scTopLeftRight);
			// 
			// scTopBottom.Panel2
			// 
			this.scTopBottom.Panel2.Controls.Add(this.plAttributes);
			this.scTopBottom.Size = new System.Drawing.Size(780, 659);
			this.scTopBottom.SplitterDistance = 530;
			this.scTopBottom.TabIndex = 1;
			// 
			// plAttributes
			// 
			this.plAttributes.BackColor = System.Drawing.SystemColors.Control;
			this.plAttributes.Dock = System.Windows.Forms.DockStyle.Fill;
			this.plAttributes.Location = new System.Drawing.Point(0, 0);
			this.plAttributes.Name = "plAttributes";
			this.plAttributes.Size = new System.Drawing.Size(780, 125);
			this.plAttributes.TabIndex = 0;
			// 
			// DeviceForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(780, 659);
			this.ControlBox = false;
			this.Controls.Add(this.scTopBottom);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DeviceForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Device";
			this.Activated += new System.EventHandler(this.deviceForm_Activated);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DeviceForm_FormClosing);
			this.Load += new System.EventHandler(this.DeviceForm_Load);
			this.LocationChanged += new System.EventHandler(this.DeviceForm_LocationChanged);
			this.scTopLeftRight.Panel1.ResumeLayout(false);
			this.scTopLeftRight.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.scTopLeftRight)).EndInit();
			this.scTopLeftRight.ResumeLayout(false);
			this.scTopBottom.Panel1.ResumeLayout(false);
			this.scTopBottom.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.scTopBottom)).EndInit();
			this.scTopBottom.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		public void SendAllForever()
		{
			int count = 0;
			while (true)
			{
				msgLogForm.AppendLog(string.Format("Msg Loop # {0:D}", count++));
				SendAllMsgs();
				SendEventWaves(true);
				Thread.Sleep(1000);
			}
		}

		public void SendAllMsgs()
		{
			sendCmds.SendHCIExt(HCIExt_SetRxGain);
			sendCmds.SendHCIExt(HCIExt_SetTxPower);
			sendCmds.SendHCIExt(HCIExt_OnePktPerEvt);
			sendCmds.SendHCIExt(HCIExt_ClkDivideOnHalt);
			sendCmds.SendHCIExt(HCIExt_DeclareNvUsage);
			sendCmds.SendHCIExt(HCIExt_Decrypt);
			sendCmds.SendHCIExt(HCIExt_SetLocalSupportedFeatures);
			sendCmds.SendHCIExt(HCIExt_SetFastTxRespTime);
			sendCmds.SendHCIExt(HCIExt_ModemTestTx);
			sendCmds.SendHCIExt(HCIExt_ModemHopTestTx);
			sendCmds.SendHCIExt(HCIExt_ModemTestRx);
			sendCmds.SendHCIExt(HCIExt_EndModemTest);
			sendCmds.SendHCIExt(HCIExt_SetBDADDR);
			sendCmds.SendHCIExt(HCIExt_SetSCA);
			sendCmds.SendHCIExt(HCIExt_EnablePTM);
			sendCmds.SendHCIExt(HCIExt_SetFreqTune);
			sendCmds.SendHCIExt(HCIExt_SaveFreqTune);
			sendCmds.SendHCIExt(HCIExt_SetMaxDtmTxPower);
			sendCmds.SendHCIExt(HCIExt_MapPmIoPort);
			sendCmds.SendHCIExt(HCIExt_DisconnectImmed);
			sendCmds.SendHCIExt(HCIExt_PER);
			sendCmds.SendL2CAP(L2CAP_InfoReq);
			sendCmds.SendL2CAP(L2CAP_ConnParamUpdateReq);
			sendCmds.SendATT(ATT_ErrorRsp);
			sendCmds.SendATT(ATT_ExchangeMTUReq);
			sendCmds.SendATT(ATT_ExchangeMTURsp);
			sendCmds.SendATT(ATT_FindInfoReq, TxDataOut.CmdTypes.General);
			sendCmds.SendATT(ATT_FindInfoRsp);
			sendCmds.SendATT(ATT_FindByTypeValueReq);
			sendCmds.SendATT(ATT_FindByTypeValueRsp);
			sendCmds.SendATT(ATT_ReadByTypeReq);
			sendCmds.SendATT(ATT_ReadByTypeRsp);
			sendCmds.SendATT(ATT_ReadReq, TxDataOut.CmdTypes.General, null);
			sendCmds.SendATT(ATT_ReadRsp);
			sendCmds.SendATT(ATT_ReadBlobReq, TxDataOut.CmdTypes.General, null);
			sendCmds.SendATT(ATT_ReadBlobRsp);
			sendCmds.SendATT(ATT_ReadMultiReq);
			sendCmds.SendATT(ATT_ReadMultiRsp);
			sendCmds.SendATT(ATT_ReadByGrpTypeReq, TxDataOut.CmdTypes.General);
			sendCmds.SendATT(ATT_ReadByGrpTypeRsp);
			sendCmds.SendATT(ATT_WriteReq, null);
			sendCmds.SendATT(ATT_WriteRsp);
			sendCmds.SendATT(ATT_PrepareWriteReq);
			sendCmds.SendATT(ATT_PrepareWriteRsp);
			sendCmds.SendATT(ATT_ExecuteWriteReq, null);
			sendCmds.SendATT(ATT_ExecuteWriteRsp);
			sendCmds.SendATT(ATT_HandleValueNotification);
			sendCmds.SendATT(ATT_HandleValueIndication);
			sendCmds.SendATT(ATT_HandleValueConfirmation);
			sendCmds.SendGATT(GATT_ExchangeMTU);
			sendCmds.SendGATT(GATT_DiscAllPrimaryServices, TxDataOut.CmdTypes.General);
			sendCmds.SendGATT(GATT_DiscPrimaryServiceByUUID);
			sendCmds.SendGATT(GATT_FindIncludedServices);
			sendCmds.SendGATT(GATT_DiscAllChars);
			sendCmds.SendGATT(GATT_DiscCharsByUUID);
			sendCmds.SendGATT(GATT_DiscAllCharDescs, TxDataOut.CmdTypes.General);
			sendCmds.SendGATT(GATT_ReadCharValue, TxDataOut.CmdTypes.General, null);
			sendCmds.SendGATT(GATT_ReadUsingCharUUID);
			sendCmds.SendGATT(GATT_ReadLongCharValue, TxDataOut.CmdTypes.General, null);
			sendCmds.SendGATT(GATT_ReadMultiCharValues);
			sendCmds.SendGATT(GATT_WriteNoRsp);
			sendCmds.SendGATT(GATT_SignedWriteNoRsp);
			sendCmds.SendGATT(GATT_WriteCharValue, null);
			sendCmds.SendGATT(GATT_WriteLongCharValue, null, null);
			sendCmds.SendGATT(GATT_ReliableWrites);
			sendCmds.SendGATT(GATT_ReadCharDesc);
			sendCmds.SendGATT(GATT_ReadLongCharDesc);
			sendCmds.SendGATT(GATT_WriteCharDesc);
			sendCmds.SendGATT(GATT_WriteLongCharDesc);
			sendCmds.SendGATT(GATT_Notification);
			sendCmds.SendGATT(GATT_Indication);
			sendCmds.SendGATT(GATT_AddService);
			sendCmds.SendGATT(GATT_DelService);
			sendCmds.SendGATT(GATT_AddAttribute);
			sendCmds.SendGAP(GAP_DeviceInit);
			sendCmds.SendGAP(GAP_ConfigDeviceAddr);
			sendCmds.SendGAP(GAP_DeviceDiscoveryRequest);
			sendCmds.SendGAP(GAP_DeviceDiscoveryCancel);
			sendCmds.SendGAP(GAP_MakeDiscoverable);
			sendCmds.SendGAP(GAP_UpdateAdvertisingData);
			sendCmds.SendGAP(GAP_EndDiscoverable);
			sendCmds.SendGAP(GAP_EstablishLinkRequest);
			sendCmds.SendGAP(GAP_TerminateLinkRequest);
			sendCmds.SendGAP(GAP_Authenticate);
			sendCmds.SendGAP(GAP_PasskeyUpdate);
			sendCmds.SendGAP(GAP_SlaveSecurityRequest);
			sendCmds.SendGAP(GAP_Signable);
			sendCmds.SendGAP(GAP_Bond);
			sendCmds.SendGAP(GAP_TerminateAuth);
			sendCmds.SendGAP(GAP_UpdateLinkParamReq);
			sendCmds.SendGAP(GAP_SetParam);
			sendCmds.SendGAP(GAP_GetParam);
			sendCmds.SendGAP(GAP_ResolvePrivateAddr);
			sendCmds.SendGAP(GAP_SetAdvToken);
			sendCmds.SendGAP(GAP_RemoveAdvToken);
			sendCmds.SendGAP(GAP_UpdateAdvTokens);
			sendCmds.SendGAP(GAP_BondSetParam);
			sendCmds.SendGAP(GAP_BondGetParam);
			sendCmds.SendUTIL(UTIL_NVRead);
			sendCmds.SendUTIL(UTIL_NVWrite);
			sendCmds.SendHCIOther(HCIOther_ReadRSSI);
			sendCmds.SendHCIOther(HCIOther_LEClearWhiteList);
			sendCmds.SendHCIOther(HCIOther_LEAddDeviceToWhiteList);
			sendCmds.SendHCIOther(HCIOther_LERemoveDeviceFromWhiteList);
			sendCmds.SendHCIOther(HCIOther_LEConnectionUpdate);
		}

		public void SendEventWaves(bool skipCase)
		{
			int num1 = 255;
			byte[] data = new byte[num1];
			for (int index = 0; index < num1; ++index)
				data[index] = (byte)index;

			byte length1 = (byte)(num1 - 4);
			SendAllEvents(data, length1);

			msgLogForm.AppendLog(s_delimeter);
			if (!skipCase)
			{
				int num2 = (int)length1 - 1;
				int index = 0;
				while (index < (int)length1)
				{
					data[index] = (byte)num2;
					++index;
					--num2;
				}
				length1 -= 4;
				SendAllEvents(data, length1);
				msgLogForm.AppendLog(s_delimeter);
			}

			for (int index = 0; index < (int)length1; ++index)
				data[index] = 0;

			byte length2 = (byte)(length1 - 4);
			SendAllEvents(data, length2);
			msgLogForm.AppendLog(s_delimeter);
			if (skipCase)
				return;
			for (int index = 0; index < (int)length2; ++index)
				data[index] = byte.MaxValue;
			byte length3 = (byte)(length2 - 4);
			SendAllEvents(data, length3);
			msgLogForm.AppendLog(s_delimeter);
		}

		public void SendAllEvents(byte[] data, byte length)
		{
			bool dataErr = false;
			RxDataIn rxDataIn = new RxDataIn();
			rxDataIn.RxType = 4;
			rxDataIn.CmdOpcode = byte.MaxValue;
			rxDataIn.Length = length;
			rxDataIn.Data = data;
			msgLogForm.AppendLog(s_delimeter);

			rxDataIn.EventOpcode = 1024;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1025;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1026;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1027;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1028;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1029;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1030;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1031;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1032;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1033;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1034;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1035;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1036;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1037;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1038;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1039;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1040;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1041;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1042;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1043;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1044;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1153;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1163;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1171;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1281;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1282;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1283;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1284;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1285;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1286;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1287;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1288;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1289;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1290;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1291;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1292;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1293;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1294;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1295;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1295;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1296;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1297;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1298;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1299;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1302;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1303;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1304;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1305;
			DisplayRxCmd(rxDataIn, true);

			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1307;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1309;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1310;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1536;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1537;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1538;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1539;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1540;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1541;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1542;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1543;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1544;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1545;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1546;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1547;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1548;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1549;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1550;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1551;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
			rxDataIn.EventOpcode = 1663;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);

			int index1 = 0;
			rxDataIn.CmdOpcode = 14;
			rxDataIn.EventOpcode = 0;
			dataUtils.Load8Bits(ref data, ref index1, 1, ref dataErr);
			dataUtils.Load16Bits(ref data, ref index1, 5125, ref dataErr, false);
			rxDataIn.Data = data;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);

			int index2 = 0;
			dataUtils.Load8Bits(ref data, ref index2, 1, ref dataErr);
			dataUtils.Load16Bits(ref data, ref index2, 8208, ref dataErr, false);
			rxDataIn.Data = data;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);

			int index3 = 0;
			dataUtils.Load8Bits(ref data, ref index3, 1, ref dataErr);
			dataUtils.Load16Bits(ref data, ref index3, 8209, ref dataErr, false);
			rxDataIn.Data = data;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);

			int index4 = 0;
			dataUtils.Load8Bits(ref data, ref index4, 1, ref dataErr);
			dataUtils.Load16Bits(ref data, ref index4, 8210, ref dataErr, false);
			rxDataIn.Data = data;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);

			int index5 = 0;
			dataUtils.Load8Bits(ref data, ref index5, 1, ref dataErr);
			dataUtils.Load16Bits(ref data, ref index5, 8211, ref dataErr, false);
			rxDataIn.Data = data;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);

			rxDataIn.CmdOpcode = 19;
			DisplayRxCmd(rxDataIn, true);
			msgLogForm.AppendLog(s_delimeter);
		}

		public void SendAttrDataCmds()
		{
			sendCmds.SendGATT(GATT_DiscAllPrimaryServices, TxDataOut.CmdTypes.General);
			sendCmds.SendGATT(GATT_DiscPrimaryServiceByUUID);
			sendCmds.SendGATT(GATT_FindIncludedServices);
			sendCmds.SendGATT(GATT_DiscAllChars);
			sendCmds.SendGATT(GATT_DiscCharsByUUID);
			sendCmds.SendGATT(GATT_DiscAllCharDescs, TxDataOut.CmdTypes.General);
		}

		public void TestCase()
		{
			sendCmds.SendUTIL(
				new HCICmds.UTILCmds.UTIL_Reset()
				{
					resetType = HCICmds.UTIL_ResetType.Hard_Reset
				});
			sendCmds.SendHCIExt(
				new HCICmds.HCIExtCmds.HCIExt_SetBDADDR()
				{
					bleDevAddr = "70:55:44:33:22:11"
				});
			HCICmds.GAPCmds.GAP_DeviceInit gapDeviceInit = new HCICmds.GAPCmds.GAP_DeviceInit();
			gapDeviceInit.broadcasterProfileRole = HCICmds.GAP_EnableDisable.Disable;
			gapDeviceInit.observerProfileRole = HCICmds.GAP_EnableDisable.Disable;
			gapDeviceInit.peripheralProfileRole = HCICmds.GAP_EnableDisable.Disable;
			gapDeviceInit.centralProfileRole = HCICmds.GAP_EnableDisable.Enable;
			gapDeviceInit.maxScanResponses = 3;
			gapDeviceInit.irk = "33:42:CF:14:BC:55:17:31:75:4F:BB:A4:C7:F2:8C:13";
			gapDeviceInit.csrk = "45:0A:F4:B0:03:07:B0:40:87:F4:18:23:75:4A:FB:A4";
			gapDeviceInit.signCounter = 0;
			sendCmds.SendGAP(gapDeviceInit);
			sendCmds.SendGAP(
				new HCICmds.GAPCmds.GAP_EstablishLinkRequest()
				{
					highDutyCycle = HCICmds.GAP_EnableDisable.Disable,
					whiteList = HCICmds.GAP_EnableDisable.Disable,
					addrTypePeer = HCICmds.GAP_AddrType.Public,
					peerAddr = "60:55:44:33:22:11"
				});
			sendCmds.SendGAP(
				new HCICmds.GAPCmds.GAP_Authenticate()
				{
					connHandle = 0,
					secReq_ioCaps = HCICmds.GAP_IOCaps.KeyboardDisplay,
					secReq_oobAvailable = HCICmds.GAP_TrueFalse.False,
					secReq_oob = "4d:9f:88:5a:6e:03:12:fe:00:00:00:00:00:00:00:00",
					secReq_authReq = 1,
					secReq_maxEncKeySize = 16,
					secReq_keyDist = 0,
					pairReq_Enable = HCICmds.GAP_EnableDisable.Disable,
					pairReq_ioCaps = HCICmds.GAP_IOCaps.KeyboardDisplay,
					pairReq_oobDataFlag = HCICmds.GAP_EnableDisable.Disable,
					pairReq_authReq = 1,
				});
			sendCmds.SendGAP(
				new HCICmds.GAPCmds.GAP_TerminateLinkRequest()
				{
					connHandle = (ushort)0,
					discReason = HCICmds.GAP_DisconnectReason.Remote_User_Terminated
				});
		}

		public void StartTimer(DeviceForm.EventType eType)
		{
			switch (eType)
			{
				case DeviceForm.EventType.Init:
					initTimer.Start();
					break;
				case DeviceForm.EventType.Scan:
					scanTimer.Start();
					break;
				case DeviceForm.EventType.Establish:
					establishTimer.Start();
					break;
				case DeviceForm.EventType.PairBond:
					pairBondTimer.Start();
					break;
			}
		}

		public void StopTimer(DeviceForm.EventType eType)
		{
			switch (eType)
			{
				case DeviceForm.EventType.Init:
					initTimer.Stop();
					break;
				case DeviceForm.EventType.Scan:
					scanTimer.Stop();
					break;
				case DeviceForm.EventType.Establish:
					establishTimer.Stop();
					break;
				case DeviceForm.EventType.PairBond:
					pairBondTimer.Stop();
					break;
			}
		}

		private void timerScanEvent(object obj, EventArgs args)
		{
			StopTimer(DeviceForm.EventType.Scan);
			devTabsForm.ShowProgress(false);
			Cursor = Cursors.Default;

			devTabsForm.discoverConnectStatus = DeviceTabsForm.DiscoverConnectStatus.Idle;
			devTabsForm.DiscoverConnectUserInputControl();

			string msg = "Device Scan Timeout.\n";
			DisplayMsg(SharedAppObjs.MsgType.Warning, msg);
			msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, msg);
		}

		private void timerInitEvent(object obj, EventArgs args)
		{
			StopTimer(DeviceForm.EventType.Init);
			devTabsForm.ShowProgress(false);
			Cursor = Cursors.Default;

			string msg = "GAP Device Initialization Timeout.\nDevice May Not Function Properly.\n";
			DisplayMsg(SharedAppObjs.MsgType.Warning, msg);
			msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, msg);
		}

		private void timerEstablishEvent(object obj, EventArgs args)
		{
			StopTimer(DeviceForm.EventType.Establish);
			devTabsForm.ShowProgress(false);
			Cursor = Cursors.Default;

			string msg = "GAP Link Establish Request Timeout.\n";
			DisplayMsg(SharedAppObjs.MsgType.Warning, msg);
			msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, msg);
		}

		private void timerPairBondEvent(object obj, EventArgs args)
		{
			StopTimer(DeviceForm.EventType.PairBond);
			devTabsForm.ShowProgress(false);
			Cursor = Cursors.Default;

			devTabsForm.TabPairBondInitValues();
			devTabsForm.SetPairingStatus(DeviceTabsForm.PairingStatus.NotPaired);
			devTabsForm.PairBondUserInputControl();

			string msg = "Pairing Bonding Request Timeout.\n";
			DisplayMsg(SharedAppObjs.MsgType.Warning, msg);
			msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, msg);
		}

		private void DisplayRxCmd(RxDataIn rxDataIn, bool displayBytes)
		{
			if (InvokeRequired)
			{
				Invoke((Delegate)new DeviceForm.DisplayRxCmdDelegate(DisplayRxCmd), rxDataIn, displayBytes);
			}
			else
			{
				byte packetType = rxDataIn.RxType;
				ushort opCode1 = rxDataIn.CmdOpcode;
				ushort opCode2 = rxDataIn.EventOpcode;
				byte num1 = rxDataIn.Length;
				byte[] data1 = rxDataIn.Data;
				string msg1 = string.Empty;
				byte[] addr = new byte[6];
				string msg2 =
					(packetType == 4)
					? string.Format("-Type\t\t: 0x{0:X2} ({1:S})\n-EventCode\t: 0x{2:X2} ({3:S})\n-Data Length\t: 0x{4:X2} ({5:D}) bytes(s)\n", packetType, devUtils.GetPacketTypeStr(packetType), opCode1, devUtils.GetOpCodeName(opCode1), num1, num1)
					: string.Format("-Type\t\t: 0x{0:X2} ({1:S})\n-OpCode\t\t: 0x{2:X4} ({3:S})\n-Data Length\t: 0x{4:X2} ({5:D}) bytes(s)\n", packetType, devUtils.GetPacketTypeStr(packetType), opCode1, devUtils.GetOpCodeName(opCode1), num1, num1)
					;
				int index1 = 0;
				byte bits1 = 0;
				ushort bits2 = 0;
				string str2 = string.Empty;
				bool dataErr = false;
				switch (opCode1)
				{
					case 14:
						dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
						if (!dataErr)
						{
							msg2 += string.Format(" Packets\t\t: 0x{0:X2} ({1:D})\n", bits1, bits1);
							ushort opCode3 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
							if (!dataErr)
							{
								msg2 += string.Format(" Opcode\t\t: 0x{0:X4} ({1:S})\n", opCode3, devUtils.GetOpCodeName(opCode3));
								switch (opCode3)
								{
									case 5125:
										dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
										if (!dataErr)
										{
											msg2 += string.Format(" Status\t\t: 0x{0:X2} ({1:S})\n", bits1, devUtils.GetStatusStr(bits1));
											dspCmdUtils.AddHandle(data1, ref index1, ref dataErr, ref msg2);
											if (!dataErr)
											{
												dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
												if (!dataErr)
													msg2 += string.Format(" RSSI\t\t: 0x{0:X2} ({1:D})\n", bits1, bits1);
											}
										}
										break;
									case 8208:
									case 8209:
									case 8210:
										dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
										if (!dataErr)
											msg2 += string.Format(" Status\t\t: 0x{0:X2} ({1:S})\n", bits1, devUtils.GetStatusStr(bits1));
										break;
									case 64526:
									case 64527:
									case 64528:
										dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
										if (!dataErr)
										{
											msg2 += string.Format(" Status\t\t: 0x{0:X2} ({1:S})\n", bits1, devUtils.GetHCIExtStatusStr(bits1));
										}
										break;
									default:
										devUtils.BuildRawDataStr(data1, ref msg2, data1.Length);
										break;
								}
							}
						}
						break;
					case 19:
						if (num1 == 5)
						{
							dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
							if (!dataErr)
							{
								msg2 = msg2 + string.Format(" NumOfHandles\t: 0x{0:X2} ({1:D})\n", bits1, bits1);
								dspCmdUtils.AddConnectHandle(data1, ref index1, ref dataErr, ref msg2);
								if (!dataErr)
								{
									dataUtils.Unload16Bits(data1, ref index1, ref bits2, ref dataErr, false);
									if (!dataErr)
										msg2 = msg2 + string.Format(" PktsCompleted\t: 0x{0:X4} ({1:D})\n", bits2, bits2);
									else
										break;
								}
								else
									break;
							}
							else
								break;
						}
						devUtils.BuildRawDataStr(data1, ref msg2, data1.Length);
						break;
					case 0xff:
						ushort num8 = (ushort)((opCode2 & 0x380) >> 7);
						byte status = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
						if (!dataErr)
						{
							string str4 = (num8 != 0) ? devUtils.GetStatusStr(status) : devUtils.GetHCIExtStatusStr(status);
							msg2 = msg2 + string.Format(" Event\t\t: 0x{0:X4} ({1:S})\n Status\t\t: 0x{2:X2} ({3:S})\n", opCode2, devUtils.GetOpCodeName(opCode2), status, str4);
							ushort num3 = opCode2;
							byte num4;
							if (num3 <= 1171)
							{
								if (num3 <= 1153)
								{
									switch (num3)
									{
										case 1024:
										case 1025:
										case 1026:
										case 1027:
										case 1028:
										case 1029:
										case 1030:
										case 1031:
										case 1032:
										case 1033:
										case 1034:
										case 1035:
										case 1036:
										case 1037:
										case 1038:
										case 1039:
										case 1040:
										case 1041:
										case 1042:
										case 1043:
										case 1044:
											int num5 = (int)dataUtils.Unload16Bits(data1, ref index1, ref bits2, ref dataErr, false);
											if (!dataErr)
											{
												msg2 = msg2 + string.Format(" Cmd Opcode\t: 0x{0:X4} ({1:S})\n", bits2, devUtils.GetOpCodeName(bits2));
												goto label_319;
											}
											else
												goto label_319;
										case 1153:
											int num6 = (int)dspCmdUtils.AddHandle(data1, ref index1, ref dataErr, ref msg2);
											if (!dataErr)
											{
												int num7 = (int)dataUtils.Unload16Bits(data1, ref index1, ref bits2, ref dataErr, false);
												if (!dataErr)
												{
													msg2 = msg2 + string.Format(" RejectReason\t: 0x{0:X4} ({1:S})\n", bits2, devUtils.GetL2CapRejectReasonsStr(bits2));
													goto label_319;
												}
												else
													goto label_319;
											}
											else
												goto label_319;
									}
								}
								else if (num3 != 1163)
								{
									if (num3 == 1171)
									{
										int num7 = (int)dspCmdUtils.AddHandle(data1, ref index1, ref dataErr, ref msg2);
										if (!dataErr)
										{
											int num9 = (int)dataUtils.Unload16Bits(data1, ref index1, ref bits2, ref dataErr, false);
											if (!dataErr)
											{
												msg2 = msg2 + string.Format(" Result\t\t: 0x{0:X4} ({1:S})\n", bits2, devUtils.GetL2CapConnParamUpdateResultStr(bits2));
												break;
											}
											else
												break;
										}
										else
											break;
									}
								}
								else
								{
									int num7 = (int)dataUtils.Unload16Bits(data1, ref index1, ref bits2, ref dataErr, false);
									if (!dataErr)
									{
										msg2 = msg2 + string.Format(" Cmd Opcode\t: 0x{0:X4} ({1:S})\n", bits2, devUtils.GetOpCodeName(bits2));
										break;
									}
									else
										break;
								}
							}
							else if (num3 <= 1408)
							{
								switch (num3)
								{
									case 1281:
										num4 = 0;
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
										{
											byte data2 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
											if (!dataErr)
											{
												msg2 = msg2 + string.Format(" ReqOpcode\t: 0x{0:X2} ({1:S})\n", data2, devUtils.GetHciReqOpCodeStr(data2));
												dspCmdUtils.AddHandle(data1, ref index1, ref dataErr, ref msg2);
												if (!dataErr)
												{
													byte errorStatus = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
													if (!dataErr)
													{
														msg2 = msg2 + string.Format(" ErrorCode\t: 0x{0:X2} ({1:S})\n", errorStatus, devUtils.GetShortErrorStatusStr(errorStatus));
														msg2 = msg2 + string.Format("       \t\t: {0:S}\n", devUtils.GetErrorStatusStr(errorStatus));
														if (devTabsForm.GetSelectedTab() == 1)
														{
															if (data2 == 10 || data2 == 8)
																devTabsForm.SetTbReadStatusText(string.Format("{0:S}", devUtils.GetShortErrorStatusStr(errorStatus)));
															if (data2 == 18)
																devTabsForm.SetTbWriteStatusText(string.Format("{0:S}", devUtils.GetShortErrorStatusStr(errorStatus)));
														}
													}
												}
											}
										}
										goto label_319;
									case 1282:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
										{
											ushort num7 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
											if (!dataErr)
												msg2 = msg2 + string.Format(" ClientRxMTU\t: 0x{0:X4} ({1:D})\n", num7, num7);
										}
										goto label_319;
									case 1283:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
										{
											ushort num7 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
											if (!dataErr)
												msg2 = msg2 + string.Format(" ServerRxMTU\t: 0x{0:X4} ({1:D})\n", num7, num7);
										}
										goto label_319;
									case 1284:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
											dspCmdUtils.AddStartEndHandle(data1, ref index1, ref dataErr, ref msg2);
										goto label_319;
									case 1285:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
										{
											byte num7 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
											if (!dataErr)
											{
												msg2 = msg2 + string.Format(" Format\t\t: 0x{0:X2} ({1:S})\n", num7, devUtils.GetFindFormatStr(num7));
												int uuidLength = devUtils.GetUuidLength(num7, ref dataErr);
												if (!dataErr)
												{
													int dataLength = uuidLength + 2;
													int totalLength = (int)num1 - index1;
													msg2 = msg2 + devUtils.UnloadHandleValueData(data1, ref index1, totalLength, dataLength, ref dataErr, "Uuid");
												}
											}
										}
										goto label_319;
									case 1286:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
										{
											dspCmdUtils.AddStartEndHandle(data1, ref index1, ref dataErr, ref msg2);
											if (!dataErr)
											{
												ushort num7 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
												if (!dataErr)
													msg2 = msg2 + string.Format(" Type\t\t: 0x{0:X4} ({1:D})\n", num7, num7);
											}
										}
										goto label_319;
									case 1287:
										byte num10 = devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr);
										if (num10 != 0 && !dataErr)
										{
											if (num10 >= 2)
											{
												int num7 = num10 / 2;
												for (uint index2 = 0U; (long)index2 < (long)num7 && !dataErr; ++index2)
												{
													ushort num9 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
													if (!dataErr)
														msg2 = msg2 + string.Format(" Handle\t\t: {0:X2}:{1:X2}\n", num9 & 0xFF, num9 >> 8);
													else
														break;
												}
											}
										}
										goto label_319;
									case 1288:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
										{
											dspCmdUtils.AddStartEndHandle(data1, ref index1, ref dataErr, ref msg2);
											if (!dataErr)
												msg2 = msg2 + string.Format(" Type\t\t: {0:S}\n", devUtils.UnloadColonData(data1, ref index1, (int)num1 - 3 - index1 + 1, ref dataErr));
										}
										goto label_319;
									case 1289:
										byte num11 = devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr);
										if (num11 != 0 && !dataErr)
										{
											int dataLength = (int)dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
											if (!dataErr)
											{
												msg2 = msg2 + string.Format(" Length\t\t: 0x{0:X2} ({1:D})\n", dataLength, dataLength);
												int num7 = num11 - 1;
												if (dataLength != 0)
												{
													string handleStr = string.Empty;
													string valueStr = string.Empty;
													msg2 = msg2 + devUtils.UnloadHandleValueData(data1, ref index1, num7, dataLength, ref handleStr, ref valueStr, ref dataErr, "Data");
													if (!dataErr && devTabsForm.GetSelectedTab() == 1)
													{
														devTabsForm.SetTbReadValueTag(valueStr);
														if (devTabsForm.GetRbASCIIReadChecked())
															devTabsForm.SetTbReadValueText(devUtils.HexStr2UserDefinedStr(valueStr, SharedAppObjs.StringType.ASCII));
														else if (devTabsForm.GetRbDecimalReadChecked())
															devTabsForm.SetTbReadValueText(devUtils.HexStr2UserDefinedStr(valueStr, SharedAppObjs.StringType.DEC));
														else
															devTabsForm.SetTbReadValueText(devUtils.HexStr2UserDefinedStr(valueStr, SharedAppObjs.StringType.HEX));
														if (!string.IsNullOrEmpty(handleStr))
															devTabsForm.SetTbReadAttrHandleText(handleStr);
													}
												}
											}
										}
										goto label_319;
									case 1290:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
											dspCmdUtils.AddHandle(data1, ref index1, ref dataErr, ref msg2);
										goto label_319;
									case 1291:
										byte num12 = devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr);
										if (num12 != 0 && !dataErr)
										{
											string msg3 = string.Empty;
											for (uint index2 = 0U; index2 < (uint)num12 && !dataErr; ++index2)
											{
												byte num7 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
												msg3 = msg3 + string.Format("{0:X2} ", num7);
											}
											if (!dataErr)
											{
												msg3.Trim();
												msg2 = msg2 + string.Format(" Value\t\t: {0:S}\n", msg3);
												if (devTabsForm.GetSelectedTab() == 1)
												{
													devTabsForm.SetTbReadValueTag(msg3);
													if (devTabsForm.GetRbASCIIReadChecked())
														devTabsForm.SetTbReadValueText(devUtils.HexStr2UserDefinedStr(msg3, SharedAppObjs.StringType.ASCII));
													else if (devTabsForm.GetRbDecimalReadChecked())
														devTabsForm.SetTbReadValueText(devUtils.HexStr2UserDefinedStr(msg3, SharedAppObjs.StringType.DEC));
													else
														devTabsForm.SetTbReadValueText(devUtils.HexStr2UserDefinedStr(msg3, SharedAppObjs.StringType.HEX));
												}
											}
										}
										goto label_319;
									case 1292:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
											dspCmdUtils.AddHandleOffset(data1, ref index1, ref dataErr, ref msg2);
										goto label_319;
									case 1293:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
											msg2 = msg2 + string.Format(" Value\t\t: {0:S}\n", devUtils.UnloadColonData(data1, ref index1, (int)num1 - 3 - index1 + 1, ref dataErr));
										goto label_319;
									case 1294:
										byte num13 = devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr);
										if (num13 != 0 && !dataErr)
										{
											for (byte index2 = 0; index2 < num13 && !dataErr; ++index2)
												msg1 = msg1 + string.Format("{0:X2} ", dataUtils.Unload8Bits(data1, ref index1, ref dataErr));
											if (!dataErr)
											{
												msg1.Trim();
												msg2 = msg2 + string.Format(" Handles\t\t: {0:S}\n", msg1);
											}
										}
										goto label_319;
									case 1295:
										byte num14 = devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr);
										if (num14 != 0 && !dataErr)
										{
											for (byte index2 = 0; index2 < num14 && !dataErr; ++index2)
												msg1 = msg1 + string.Format("{0:X2} ", dataUtils.Unload8Bits(data1, ref index1, ref dataErr));
											if (!dataErr)
											{
												msg1.Trim();
												msg2 = msg2 + string.Format(" Values\t\t: {0:S}\n", msg1);
												if (devTabsForm.GetSelectedTab() == 1)
												{
													devTabsForm.SetTbReadValueTag(msg1);
													if (devTabsForm.GetRbASCIIReadChecked())
														devTabsForm.SetTbReadValueText(devUtils.HexStr2UserDefinedStr(msg1, SharedAppObjs.StringType.ASCII));
													else if (devTabsForm.GetRbDecimalReadChecked())
														devTabsForm.SetTbReadValueText(devUtils.HexStr2UserDefinedStr(msg1, SharedAppObjs.StringType.DEC));
													else
														devTabsForm.SetTbReadValueText(devUtils.HexStr2UserDefinedStr(msg1, SharedAppObjs.StringType.HEX));
												}
											}
										}
										goto label_319;
									case 1296:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
										{
											dspCmdUtils.AddStartEndHandle(data1, ref index1, ref dataErr, ref msg2);
											if (!dataErr)
												msg2 = msg2 + string.Format(" GroupType\t: {0:S}\n", devUtils.UnloadColonData(data1, ref index1, (int)num1 - 3 - index1 + 1, ref dataErr));
										}
										goto label_319;
									case 1297:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
										{
											byte num7 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
											if (!dataErr)
											{
												msg2 = msg2 + string.Format(" Length\t\t: 0x{0:X2} ({1:D})\n", num7, num7);
												if (num7 != 0)
												{
													int dataLength = (int)num7;
													int totalLength = (int)num1 - 3 - index1 + 1;
													msg2 = msg2 + string.Format(" DataList\t:\n{0:S}\n", devUtils.UnloadHandleHandleValueData(data1, ref index1, totalLength, dataLength, ref dataErr));
												}
											}
										}
										goto label_319;
									case 1298:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
										{
											byte sigAuth = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
											if (!dataErr)
											{
												msg2 = msg2 + string.Format(" Signature\t: 0x{0:X2} ({1:S})\n", sigAuth, devUtils.GetSigAuthStr(sigAuth));
												byte gapYesNo = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
												if (!dataErr)
												{
													msg2 = msg2 + string.Format(" Command\t: 0x{0:X2} ({1:S})\n", gapYesNo, devUtils.GetGapYesNoStr(gapYesNo));
													int num7 = (int)dspCmdUtils.AddHandle(data1, ref index1, ref dataErr, ref msg2);
													if (!dataErr)
													{
														msg1.Trim();
														msg2 = msg2 + string.Format(" Value\t\t: {0:S}\n", devUtils.UnloadColonData(data1, ref index1, (int)num1 - 3 - index1 + 1, ref dataErr));
													}
												}
											}
										}
										goto label_319;
									case 1299:
										devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr);
										goto label_319;
									case 1302:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
										{
											dspCmdUtils.AddHandleOffset(data1, ref index1, ref dataErr, ref msg2);
											if (!dataErr)
												msg2 = msg2 + string.Format(" Value\t\t: {0:S}\n", devUtils.UnloadColonData(data1, ref index1, (int)num1 - 3 - index1 + 1, ref dataErr));
										}
										goto label_319;
									case 1303:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
										{
											dspCmdUtils.AddHandleOffset(data1, ref index1, ref dataErr, ref msg2);
											if (!dataErr)
												msg2 = msg2 + string.Format(" Value\t\t: {0:S}\n", devUtils.UnloadColonData(data1, ref index1, (int)num1 - 3 - index1 + 1, ref dataErr));
										}
										goto label_319;
									case 1304:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
											msg2 = msg2 + string.Format(" Flages\t\t: 0x{0:X2}\n", dataUtils.Unload8Bits(data1, ref index1, ref dataErr));
										goto label_319;
									case 1305:
										devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr);
										goto label_319;
									case 1307:
										if (devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr) != 0 && !dataErr)
										{
											dspCmdUtils.AddHandle(data1, ref index1, ref dataErr, ref msg2);
											if (!dataErr)
												msg2 = msg2 + string.Format(" Value\t\t: {0:S}\n", devUtils.UnloadColonData(data1, ref index1, (int)num1 - 3 - index1 + 1, ref dataErr));
										}
										goto label_319;
									case 1309:
										try
										{
											if ((int)(num4 = devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr)) != 0)
											{
												if (!dataErr)
												{
													dspCmdUtils.AddHandle(data1, ref index1, ref dataErr, ref msg2);
													if (!dataErr)
														msg2 = msg2 + string.Format(" Value\t\t: {0:S}\n", devUtils.UnloadColonData(data1, ref index1, (int)num1 - 3 - index1 + 1, ref dataErr));
												}
											}
										}
										catch (Exception ex)
										{
											msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, string.Format("Message Data Conversion Issue.\n\n{0}\n", ex.Message));
											DisplayMsg(SharedAppObjs.MsgType.Error, "Could Not Convert All The Data In The Following Message\n(Message Is Missing Data Bytes To Process)\n");
											dataErr = true;
										}
										goto label_319;
									case 1310:
										devUtils.UnloadAttMsgHeader(ref data1, ref index1, ref msg2, ref dataErr);
										goto label_319;
									case 1408:
										dspCmdUtils.AddConnectHandle(data1, ref index1, ref dataErr, ref msg2);
										if (!dataErr)
										{
											dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
											if (!dataErr)
											{
												msg2 = msg2 + string.Format(" PduLen\t\t: 0x{0:X2} ({1:D})\n", bits1, bits1);
												ushort num9 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
												if (!dataErr)
												{
													msg2 = msg2 + string.Format(" AttrHandle\t: 0x{0:X4} ({1:D})\n", num9, num9);
													int num15 = (int)dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
													if (!dataErr)
														msg2 = msg2 + string.Format(" Value\t\t: 0x{0:X2} ({1:D})\n", bits1, bits1);
												}
											}
										}
										goto label_319;
								}
							}
							else
							{
								switch (num3)
								{
									case 1536:
										string deviceBDAddressStr = devUtils.UnloadDeviceAddr(data1, ref addr, ref index1, false, ref dataErr);
										if (!dataErr)
										{
											msg2 = msg2 + string.Format(" DevAddr\t\t: {0:S}\n", deviceBDAddressStr);
											OnBDAddressNotify(deviceBDAddressStr);
											ushort num7 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
											if (!dataErr)
											{
												msg2 = msg2 + string.Format(" DataPktLen\t: 0x{0:X4} ({1:D})\n", num7, num7);
												byte num9 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
												if (!dataErr)
												{
													msg2 = msg2 + string.Format(" NumDataPkts\t: 0x{0:X2} ({1:D})\n", num9, num9) + string.Format(" IRK\t\t: {0:S}\n", devUtils.UnloadColonData(data1, ref index1, 16, ref dataErr));
													if (!dataErr)
													{
														msg2 = msg2 + string.Format(" CSRK\t\t: {0:S}\n", devUtils.UnloadColonData(data1, ref index1, 16, ref dataErr));
														if (!dataErr && !DeviceStarted)
														{
															StopTimer(DeviceForm.EventType.Init);
															devTabsForm.ShowProgress(false);
															devTabsForm.UserTabAccess(true);
															DeviceStarted = true;
															devTabsForm.GetConnectionParameters();
														}
													}
												}
											}
										}
										goto label_319;
									case 1537:
										StopTimer(DeviceForm.EventType.Scan);
										devTabsForm.ShowProgress(false);
										if (status != 0 && status != 48)
											msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, string.Format("GAP_DeviceDiscoveryDone Failed.\n{0}\n", str4));

										byte num16 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
										if (!dataErr)
										{
											msg2 = msg2 + string.Format(" NumDevs\t: 0x{0:X2} ({1:D})\n", num16, num16);
											if ((int)num16 > 0)
											{
												for (byte index2 = 0; index2 < num16 && !dataErr; ++index2)
												{
													string str5 = msg2 + string.Format(" Device #{0:D}\n", index2);
													byte eventType = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
													string str6 = str5 + string.Format(" EventType\t: 0x{0:X2} ({1:S})\n", eventType, devUtils.GetGapEventTypeStr(eventType));
													byte addrType = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
													msg2 = str6 + string.Format(" AddrType\t: 0x{0:X2} ({1:S})\n", addrType, devUtils.GetGapAddrTypeStr(addrType)) + string.Format(" Addr\t\t: {0:S}\n", devUtils.UnloadDeviceAddr(data1, ref addr, ref index1, false, ref dataErr));
													DeviceTabsForm.LinkSlave linkSlave;
													linkSlave.slaveBDA = addr;
													linkSlave.addrBDA = "";
													linkSlave.addrType = (HCICmds.GAP_AddrType)addrType;
													devTabsForm.AddSlaveDevice(linkSlave);
												}
											}
										}
										goto label_319;
									case 1538:
										dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
										if (!dataErr)
											msg2 = msg2 + string.Format(" AdType\t\t: 0x{0:X2} ({1:S})\n", bits1, devUtils.GetGapAdventAdTypeStr(bits1));
										goto label_319;
									case 1539:
									case 1540:
										goto label_319;
									case 1541:
										StopTimer(DeviceForm.EventType.Establish);
										devTabsForm.ShowProgress(false);
										byte addrType1 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
										if (!dataErr)
										{
											msg2 = msg2 + string.Format(" DevAddrType\t: 0x{0:X2} ({1:S})\n", addrType1, devUtils.GetGapAddrTypeStr(addrType1));
											string str5 = devUtils.UnloadDeviceAddr(data1, ref addr, ref index1, false, ref dataErr);
											if (!dataErr)
											{
												msg2 = msg2 + string.Format(" DevAddr\t\t: {0:S}\n", str5);
												ushort num7 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
												if (!dataErr)
												{
													msg2 = msg2 + string.Format(" ConnHandle\t: 0x{0:X4} ({1:D})\n", num7, num7);
													if ((int)status == 0)
													{
														ConnectInfo tmpConnectInfo = new ConnectInfo();
														tmpConnectInfo.Handle = num7;
														tmpConnectInfo.AddrType = addrType1;
														tmpConnectInfo.BDA = str5;
														OnConnectionNotify(ref tmpConnectInfo);
														devTabsForm.SetConnHandles(tmpConnectInfo.Handle);
													}
													ushort num9 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
													if (!dataErr)
													{
														msg2 = msg2 + string.Format(" ConnInterval\t: 0x{0:X4} ({1:D})\n", num9, num9);
														ushort num15 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
														if (!dataErr)
														{
															msg2 = msg2 + string.Format(" ConnLatency\t: 0x{0:X4} ({1:D})\n", num15, num15);
															ushort num18 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
															if (!dataErr)
															{
																msg2 = msg2 + string.Format(" ConnTimeout\t: 0x{0:X4} ({1:D})\n", num18, num18);
																byte num19 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
																if (!dataErr)
																	msg2 = msg2 + string.Format(" ClockAccuracy\t: 0x{0:X2} ({1:D})\n", num19, num19);
															}
														}
													}
												}
											}
										}
										goto label_319;
									case 1542:
										ushort num20 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
										if (!dataErr)
										{
											msg2 = msg2 + string.Format(" ConnHandle\t: 0x{0:X4} ({1:D})\n", num20, num20);
											int num7 = dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
											if (!dataErr)
											{
												msg2 = msg2 + string.Format(" Reason\t\t: 0x{0:X2} ({1:S})\n", bits1, devUtils.GetGapTerminationReasonStr(bits1));
												if (status == 0)
												{
													ConnectInfo connectInfo = new ConnectInfo()
													{
														Handle = num20,
														BDA = "00:00:00:00:00:00",
														AddrType = 0
													};
													OnDisconnectionNotify(ref connectInfo);
													devTabsForm.SetPairingStatus(DeviceTabsForm.PairingStatus.NotConnected);
												}
											}
										}
										goto label_319;
									case 1543:
										dspCmdUtils.AddConnectHandle(data1, ref index1, ref dataErr, ref msg2);
										if (!dataErr)
										{
											ushort num7 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
											if (!dataErr)
											{
												msg2 = msg2 + string.Format(" ConnInterval\t: 0x{0:X4} ({1:D})\n", num7, num7);
												ushort num9 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
												if (!dataErr)
												{
													msg2 = msg2 + string.Format(" ConnLatency\t: 0x{0:X4} ({1:D})\n", num9, num9);
													ushort num15 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
													if (!dataErr)
														msg2 = msg2 + string.Format(" ConnTimeout\t: 0x{0:X4} ({1:D})\n", num15, num15);
												}
											}
										}
										goto label_319;
									case 1544:
										dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
										if (!dataErr)
											msg2 = msg2 + string.Format(" AddrType\t: 0x{0:X2} ({1:S})\n", bits1, devUtils.GetGapAddrTypeStr(bits1)) + string.Format(" NewRandAddr\t: {0:S}\n", devUtils.UnloadDeviceAddr(data1, ref addr, ref index1, false, ref dataErr));
										goto label_319;
									case 1545:
										dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
										if (!dataErr)
										{
											msg2 = msg2 + string.Format(" AddrType\t: 0x{0:X2} ({1:S})\n", bits1, devUtils.GetGapAddrTypeStr(bits1)) + string.Format(" DevAddr\t\t: {0:S}\n", devUtils.UnloadDeviceAddr(data1, ref addr, ref index1, false, ref dataErr));
											if (!dataErr)
											{
												uint num7 = dataUtils.Unload32Bits(data1, ref index1, ref dataErr, false);
												if (!dataErr)
													msg2 = msg2 + string.Format(" SignCounter\t: 0x{0:X8} ({1:D})\n", num7, num7);
											}
										}
										goto label_319;
									case 1546:
										HCICmds.GAPEvts.GAP_AuthenticationComplete authenticationComplete = new HCICmds.GAPEvts.GAP_AuthenticationComplete();
										dspCmdUtils.AddConnectHandle(data1, ref index1, ref dataErr, ref msg2);
										if (!dataErr)
										{
											authenticationComplete.connHandle = bits2;
											byte authReq = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
											if (!dataErr)
											{
												msg2 = msg2 + string.Format(" AuthState\t: 0x{0:X2} ({1:S})\n", authReq,devUtils.GetGapAuthReqStr(authReq));
												authenticationComplete.authState = authReq;
												byte num7 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
												if (!dataErr)
												{
													msg2 = msg2 + string.Format(" SecInf.Enable\t: 0x{0:X2} ({1:D})\n",num7,num7);
													authenticationComplete.secInfo_enable = (HCICmds.GAP_EnableDisable)num7;
													byte num9 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
													if (!dataErr)
													{
														msg2 = msg2 + string.Format(" SecInf.LTKSize\t: 0x{0:X2} ({1:D})\n",num9,num9);
														authenticationComplete.secInfo_LTKsize = num9;
														string str5 = devUtils.UnloadColonData(data1, ref index1, 16, ref dataErr);
														if (!dataErr)
														{
															msg2 = msg2 + string.Format(" SecInf.LTK\t: {0:S}\n",str5);
															authenticationComplete.secInfo_LTK = str5;
															ushort num15 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
															if (!dataErr)
															{
																msg2 = msg2 + string.Format(" SecInf.DIV\t: 0x{0:X4} ({1:D})\n",num15,num15);
																authenticationComplete.secInfo_DIV = num15;
																string str6 = devUtils.UnloadColonData(data1, ref index1, 8, ref dataErr);
																if (!dataErr)
																{
																	msg2 = msg2 + string.Format(" SecInf.Rand\t: {0:S}\n",str6);
																	authenticationComplete.secInfo_RAND = str6;
																	byte num18 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
																	if (!dataErr)
																	{
																		msg2 = msg2 + string.Format(" DSInf.Enable\t: 0x{0:X2} ({1:D})\n",num18,num18);
																		authenticationComplete.devSecInfo_enable = (HCICmds.GAP_EnableDisable)num18;
																		byte num19 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
																		if (!dataErr)
																		{
																			msg2 = msg2 + string.Format(" DSInf.LTKSize\t: 0x{0:X2} ({1:D})\n",num19,num19);
																			authenticationComplete.devSecInfo_LTKsize = num19;
																			string str7 = devUtils.UnloadColonData(data1, ref index1, 16, ref dataErr);
																			if (!dataErr)
																			{
																				msg2 = msg2 + string.Format(" DSInf.LTK\t: {0:S}\n",str7);
																				authenticationComplete.devSecInfo_LTK = str7;
																				ushort num23 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
																				if (!dataErr)
																				{
																					msg2 = msg2 + string.Format(" DSInf.DIV\t: 0x{0:X4} ({1:D})\n",num23,num23);
																					authenticationComplete.devSecInfo_DIV = num23;
																					string str8 = devUtils.UnloadColonData(data1, ref index1, 8, ref dataErr);
																					if (!dataErr)
																					{
																						msg2 = msg2 + string.Format(" DSInf.Rand\t: {0:S}\n",str8);
																						authenticationComplete.devSecInfo_RAND = str8;
																						byte num24 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
																						if (!dataErr)
																						{
																							msg2 = msg2 + string.Format(" IdInfo.Enable\t: 0x{0:X2} ({1:D})\n",num24,num24);
																							authenticationComplete.idInfo_enable = (HCICmds.GAP_EnableDisable)num24;
																							string str9 = devUtils.UnloadColonData(data1, ref index1, 16, ref dataErr);
																							if (!dataErr)
																							{
																								msg2 = msg2 + string.Format(" IdInfo.IRK\t: {0:S}\n",str9);
																								authenticationComplete.idInfo_IRK = str9;
																								string str10 = devUtils.UnloadDeviceAddr(data1, ref addr, ref index1, false, ref dataErr);
																								if (!dataErr)
																								{
																									msg2 = msg2 + string.Format(" IdInfo.BD_Addr\t: {0:S}\n",str10);
																									authenticationComplete.idInfo_BdAddr = str10;
																									byte num25 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
																									if (!dataErr)
																									{
																										msg2 = msg2 + string.Format(" SignInfo.Enable\t: 0x{0:X2} ({1:D})\n",num25,num25);
																										authenticationComplete.signInfo_enable = (HCICmds.GAP_EnableDisable)num25;
																										string str11 = devUtils.UnloadColonData(data1, ref index1, 16, ref dataErr);
																										if (!dataErr)
																										{
																											msg2 = msg2 + string.Format(" SignInfo.CSRK\t: {0:S}\n",str11);
																											authenticationComplete.signInfo_CSRK = str11;
																											uint num26 = dataUtils.Unload32Bits(data1, ref index1, ref dataErr, false);
																											if (!dataErr)
																											{
																												msg2 = msg2 + string.Format(" SignCounter\t: 0x{0:X8} ({1:D})\n",num26,num26);
																												authenticationComplete.signCounter = num26;
																												if (devTabsForm.GetSelectedTab() == 2)
																												{
																													StopTimer(DeviceForm.EventType.PairBond);
																													devTabsForm.ShowProgress(false);
																													if (status == 23)
																													{
																														devTabsForm.SetPairingStatus(DeviceTabsForm.PairingStatus.NotPaired);
																														goto label_319;
																													}
																													else if (status == 4)
																													{
																														devTabsForm.SetPairingStatus(DeviceTabsForm.PairingStatus.PasskeyIncorrect);
																														goto label_319;
																													}
																													else if (status == 0)
																													{
																														byte num27 = 1;
																														if (((int)authenticationComplete.authState & (int)num27) == (int)num27)
																															devTabsForm.SetPairingStatus(DeviceTabsForm.PairingStatus.DevicesPairedBonded);
																														else
																															devTabsForm.SetPairingStatus(DeviceTabsForm.PairingStatus.DevicesPaired);
																														byte num28 = 4;
																														if (((int)authenticationComplete.authState & (int)num28) == (int)num28)
																															devTabsForm.SetAuthenticatedBond(true);
																														else
																															devTabsForm.SetAuthenticatedBond(false);
																														devTabsForm.SetGapAuthCompleteInfo(authenticationComplete);
																													}
																												}
																											}
																										}
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
										goto label_319;
									case 1547:
										msg2 = msg2 + string.Format(" DevAddr\t\t: {0:S}\n",devUtils.UnloadDeviceAddr(data1, ref addr, ref index1, false, ref dataErr));
										if (!dataErr)
										{
											dspCmdUtils.AddConnectHandle(data1, ref index1, ref dataErr, ref msg2);
											if (!dataErr)
											{
												int num7 = (int)dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
												if (!dataErr)
												{
													msg2 = msg2 + string.Format(" UiInput\t\t: 0x{0:X2} ({1:S})\n",bits1,devUtils.GetGapUiInputStr(bits1));
													if (devTabsForm.GetSelectedTab() == 2 && (int)bits1 == 1)
														devTabsForm.SetPairingStatus(DeviceTabsForm.PairingStatus.PasskeyNeeded);
													int num9 = (int)dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
													if (!dataErr)
													{
														msg2 = msg2 + string.Format(" UiOutput\t\t: 0x{0:X2} ({1:S})\n",bits1,devUtils.GetGapUiOutputStr(bits1));
														if (devTabsForm.GetSelectedTab() == 2)
														{
															StopTimer(DeviceForm.EventType.PairBond);
															devTabsForm.ShowProgress(false);
															devTabsForm.UsePasskeySecurity((HCICmds.GAP_UiOutput)bits1);
														}
													}
												}
											}
										}
										goto label_319;

									case 1548:
										dspCmdUtils.AddConnectHandle(data1, ref index1, ref dataErr, ref msg2);
										if (!dataErr)
										{
											msg2 += string.Format(" DevAddr\t\t: {0:S}\n", devUtils.UnloadDeviceAddr(data1, ref addr, ref index1, false, ref dataErr));
											if (!dataErr)
											{
												int num7 = (int)dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
												if (!dataErr)
													msg2 = msg2 + string.Format(" AuthReq\t\t: 0x{0:X2} ({1:D})\n", bits1, bits1);
											}
										}
										goto label_319;
									case 1549:
										byte eventType1 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
										if (!dataErr)
										{
											msg2 += string.Format(" EventType\t: 0x{0:X2} ({1:S})\n", eventType1, devUtils.GetGapEventTypeStr(eventType1));
											byte addrType2 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
											if (!dataErr)
											{
												msg2 = msg2 + string.Format(" AddrType\t: 0x{0:X2} ({1:S})\n", addrType2, devUtils.GetGapAddrTypeStr(addrType2)) + string.Format(" Addr\t\t: {0:S}\n",devUtils.UnloadDeviceAddr(data1, ref addr, ref index1, false, ref dataErr));
												if (!dataErr)
												{
													byte num7 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
													if (!dataErr)
													{
														msg2 += string.Format(" Rssi\t\t: 0x{0:X2} ({1:D})\n", num7, num7);
														byte num9 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
														if (!dataErr)
														{
															msg2 = msg2 + string.Format(" DataLength\t: 0x{0:X2} ({1:D})\n", num9, num9);
															if (num9 != 0)
															{
																msg2 = msg2 + string.Format(" Data\t\t: {0:S}\n", devUtils.UnloadColonData(data1, ref index1, num9, ref dataErr));
																if (!dataErr && (eventType1 == 0 || eventType1 == 4))
																{
																	DeviceTabsForm.LinkSlave linkSlave;
																	linkSlave.slaveBDA = addr;
																	linkSlave.addrBDA = "";
																	linkSlave.addrType = (HCICmds.GAP_AddrType)addrType2;
																	devTabsForm.AddSlaveDevice(linkSlave);
																}
															}
														}
													}
												}
											}
										}
										goto label_319;
									case 1550:
										dspCmdUtils.AddConnectHandle(data1, ref index1, ref dataErr, ref msg2);
										if (!dataErr && devTabsForm.GetSelectedTab() == 2)
										{
											StopTimer(DeviceForm.EventType.PairBond);
											devTabsForm.ShowProgress(false);
											if (status == 0)
											{
												devTabsForm.SetPairingStatus(DeviceTabsForm.PairingStatus.DevicesPairedBonded);
											}
											else
											{
												devTabsForm.SetPairingStatus(DeviceTabsForm.PairingStatus.NotPaired);
												msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, string.Format("GAP_BondComplete: Failed.\n{0}\n", str4));
											}
											devTabsForm.PairBondUserInputControl();
										}
										goto label_319;
									case 1551:
										dspCmdUtils.AddConnectHandle(data1, ref index1, ref dataErr, ref msg2);
										if (!dataErr)
										{
											dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
											if (!dataErr)
											{
												msg2 += string.Format(" IOCap\t\t: 0x{0:X2} ({1:S})\n", bits1, devUtils.GetGapIOCapsStr(bits1));
												int num9 = (int)dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
												if (!dataErr)
												{
													msg2 += string.Format(" OobDataFlag\t: 0x{0:X2} ({1:S})\n", bits1, devUtils.GetGapOobDataFlagStr(bits1));
													int num15 = (int)dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
													if (!dataErr)
													{
														msg2 += string.Format(" AuthReq\t\t: 0x{0:X2} ({1:S})\n",bits1,devUtils.GetGapAuthReqStr(bits1));
														bits1 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
														if (!dataErr)
														{
															msg2 = msg2 + string.Format(" MaxEncKeySiz\t: 0x{0:X4} ({1:D})\n",bits1,bits1);
															int num18 = (int)dataUtils.Unload8Bits(data1, ref index1, ref bits1, ref dataErr);
															if (!dataErr)
																msg2 = msg2 + string.Format(" KeyDist\t\t: 0x{0:X2} ({1:S})\n",bits1,devUtils.GetGapKeyDiskStr(bits1));
														}
													}
												}
											}
										}
										goto label_319;
									case 1663:
										ushort opCode3 = dataUtils.Unload16Bits(data1, ref index1, ref dataErr, false);
										if (!dataErr)
										{
											msg2 += string.Format(" OpCode\t\t: 0x{0:X4} ({1:S})\n", opCode3, devUtils.GetOpCodeName(opCode3));
											byte num7 = dataUtils.Unload8Bits(data1, ref index1, ref dataErr);
											if (!dataErr)
											{
												msg2 += string.Format(" DataLength\t: 0x{0:X2} ({1:D})\n", num7, num7);
												ushort num9 = opCode3;
												if (num9 <= 64918U)
												{
													if (num9 <= 64658U)
													{
														if (num9 == 64650 || num9 == 64658)
															goto label_319;
													}
													else
													{
														switch (num9)
														{
															case 64769:
															case 64770:
															case 64771:
															case 64772:
															case 64773:
															case 64774:
															case 64775:
															case 64776:
															case 64777:
															case 64778:
															case 64779:
															case 64780:
															case 64781:
															case 64782:
															case 64783:
															case 64784:
															case 64785:
															case 64786:
															case 64787:
															case 64790:
															case 64791:
															case 64792:
															case 64793:
															case 64795:
															case 64797:
															case 64798:
															case 64898:
															case 64900:
															case 64902:
															case 64908:
															case 64912:
															case 64918:
																goto label_319;
															case 64904:
																if (devTabsForm.GetTbReadStatusText() == "Reading...")
																	devTabsForm.SetTbReadStatusText(string.Format("{0:S}",devUtils.GetStatusStr(status)));
																goto label_319;
															case 64906:
																if (devTabsForm.GetTbReadStatusText() == "Reading...")
																	devTabsForm.SetTbReadStatusText(string.Format("{0:S}",devUtils.GetStatusStr(status)));
																goto label_319;
															case 64910:
																if (devTabsForm.GetTbReadStatusText() == "Reading...")
																	devTabsForm.SetTbReadStatusText(string.Format("{0:S}",devUtils.GetStatusStr(status)));
																goto label_319;
															case 64914:
																if (devTabsForm.GetTbWriteStatusText() == "Writing...")
																	devTabsForm.SetTbWriteStatusText(string.Format("{0:S}",devUtils.GetStatusStr(status)));
																goto label_319;
														}
													}
												}
												else if ((uint)num9 <= 64962U)
												{
													switch (num9)
													{
														case 64923:
														case 64925:
														case 64944:
														case 64946:
														case 64950:
														case 64952:
														case 64954:
														case 64956:
														case 64958:
														case 64960:
														case 64962:
															goto label_319;
														case 64948:
															if (devTabsForm.GetTbReadStatusText() == "Reading...")
																devTabsForm.SetTbReadStatusText(string.Format("{0:S}",devUtils.GetStatusStr(status)));
															goto label_319;
													}
												}
												else
												{
													switch (num9)
													{
														case 65020:
														case 65021:
														case 65022:
														case 65024:
														case 65027:
														case 65030:
														case 65031:
														case 65032:
														case 65036:
														case 65037:
														case 65038:
														case 65040:
														case 65041:
														case 65074:
														case 65075:
														case 65076:
														case 65077:
														case 65078:
														case 65079:
														case 65152:
														case 65154:
														case 65155:
															goto label_319;
														case 65028:
															if ((int)status != 0)
															{
																msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, string.Format("GAP_DeviceDiscoveryRequest Failed.\n{0}\n", str4));
																devTabsForm.discoverConnectStatus = DeviceTabsForm.DiscoverConnectStatus.Idle;
																devTabsForm.DiscoverConnectUserInputControl();
															}
															goto label_319;
														case 65029:
															if (status != 0)
																msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, string.Format("GAP_DeviceDiscoveryCancel Failed.\n{0}\n", str4));

															devTabsForm.discoverConnectStatus = DeviceTabsForm.DiscoverConnectStatus.Idle;
															devTabsForm.DiscoverConnectUserInputControl();
															goto label_319;
														case 65033:
															StopTimer(DeviceForm.EventType.Establish);
															devTabsForm.ShowProgress(false);
															if (status != 0)
																msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, string.Format("GAP_EstablishLinkRequest Failed.\n{0}\n", str4));

															devTabsForm.discoverConnectStatus = DeviceTabsForm.DiscoverConnectStatus.Idle;
															devTabsForm.DiscoverConnectUserInputControl();
															goto label_319;
														case 65034:
															if (status != 0)
																msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, string.Format("GAP_TerminateLinkRequest Failed.\n{0}\n", str4));

															devTabsForm.discoverConnectStatus = DeviceTabsForm.DiscoverConnectStatus.Idle;
															devTabsForm.DiscoverConnectUserInputControl();
															goto label_319;
														case 65035:
															if (status != 0)
															{
																StopTimer(DeviceForm.EventType.PairBond);
																devTabsForm.ShowProgress(false);
																Cursor = Cursors.Default;
																devTabsForm.TabPairBondInitValues();
																devTabsForm.SetPairingStatus(DeviceTabsForm.PairingStatus.NotPaired);
																devTabsForm.PairBondUserInputControl();
																msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, string.Format("GAP Authenticate Failed.\n{0}\n",str4));
															}
															goto label_319;
														case 65039:
															if (devTabsForm.GetSelectedTab() == 2 && (int)status != 0)
															{
																StopTimer(DeviceForm.EventType.PairBond);
																devTabsForm.ShowProgress(false);
																devTabsForm.SetPairingStatus(DeviceTabsForm.PairingStatus.NotPaired);
																devTabsForm.PairBondUserInputControl();
																msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, string.Format("GAP_Bond: Failed.\n{0}\n",str4));
															}
															goto label_319;
														case 65072:
															if (status != 0)
																msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, string.Format("GAP_SetParam: Failed.\n{0}\n", str4));

															devTabsForm.discoverConnectStatus = DeviceTabsForm.DiscoverConnectStatus.Idle;
															devTabsForm.DiscoverConnectUserInputControl();
															goto label_319;
														case 65073:
															if (status != 0)
																msgBox.UserMsgBox(SharedObjects.MainWin, MsgBox.MsgTypes.Error, string.Format("GAP_GetParam: Failed.\n{0}\n", str4));

															devTabsForm.discoverConnectStatus = DeviceTabsForm.DiscoverConnectStatus.Idle;
															devTabsForm.DiscoverConnectUserInputControl();
															if (num7 != 0)
															{
																int num15 = (int)dataUtils.Unload16Bits(data1, ref index1, ref bits2, ref dataErr, false);
																if (!dataErr)
																{
																	msg2 = msg2 + string.Format(" ParamValue\t: 0x{0:X4} ({1:D})\n",bits2,bits2);
																	switch (ConnParamState)
																	{
																		case DeviceForm.GAPGetConnectionParams.MinConnIntSeq:
																			devTabsForm.SetMinConnectionInterval((uint)bits2);
																			ConnParamState = DeviceForm.GAPGetConnectionParams.MaxConnIntSeq;
																			break;
																		case DeviceForm.GAPGetConnectionParams.MaxConnIntSeq:
																			devTabsForm.SetMaxConnectionInterval((uint)bits2);
																			ConnParamState = DeviceForm.GAPGetConnectionParams.SlaveLatencySeq;
																			break;
																		case DeviceForm.GAPGetConnectionParams.SlaveLatencySeq:
																			devTabsForm.SetSlaveLatency((uint)bits2);
																			ConnParamState = DeviceForm.GAPGetConnectionParams.SupervisionTimeoutSeq;
																			break;
																		case DeviceForm.GAPGetConnectionParams.SupervisionTimeoutSeq:
																			devTabsForm.SetSupervisionTimeout((uint)bits2);
																			ConnParamState = DeviceForm.GAPGetConnectionParams.None;
																			break;
																		case DeviceForm.GAPGetConnectionParams.MinConnIntSingle:
																			devTabsForm.SetNudMinConnIntValue((int)bits2);
																			ConnParamState = DeviceForm.GAPGetConnectionParams.None;
																			break;
																		case DeviceForm.GAPGetConnectionParams.MaxConnIntSingle:
																			devTabsForm.SetNudMaxConnIntValue((int)bits2);
																			ConnParamState = DeviceForm.GAPGetConnectionParams.None;
																			break;
																		case DeviceForm.GAPGetConnectionParams.SlaveLatencySingle:
																			devTabsForm.SetNudSlaveLatencyValue((int)bits2);
																			ConnParamState = DeviceForm.GAPGetConnectionParams.None;
																			break;
																		case DeviceForm.GAPGetConnectionParams.SupervisionTimeoutSingle:
																			devTabsForm.SetNudSprVisionTimeoutValue((int)bits2);
																			ConnParamState = DeviceForm.GAPGetConnectionParams.None;
																			break;
																		default:
																			break;
																	}
																}
															}
															goto label_319;
														case 65153:
															if (num7 != 0)
																msg2 = msg2 + string.Format(" nvData\t\t: {0:S}\n",devUtils.UnloadColonData(data1, ref index1, (int)num7, ref dataErr));
															goto label_319;
													}
												}
												devUtils.BuildRawDataStr(data1, ref msg2, data1.Length);
											}
										}
										goto label_319;
								}
							}
							devUtils.BuildRawDataStr(data1, ref msg2, data1.Length);
						}
						break;
					default:
						devUtils.BuildRawDataStr(data1, ref msg2, data1.Length);
						break;
				}

			label_319:
				if (dataErr)
					DisplayMsg(SharedAppObjs.MsgType.Error, "Could Not Convert All The Data In The Following Message\n(Message Is Missing Data Bytes To Process)\n");

				DisplayMsgTime(SharedAppObjs.MsgType.Incoming, msg2, rxDataIn.Time);
				if (displayBytes)
				{
					string msg4 = string.Format("{0:X2} {1:X2} {2:X2} ", packetType, (opCode1 & 0xFF), num1);
					if (opCode1 == 19 || opCode1 == 0xFF)
						msg4 = string.Format("{0:X2} {1:X2} {2:X2} {3:X2} {4:X2} ", packetType, (opCode1 & 0xFF), num1, (opCode2 & 0xFF), ((opCode2 >> 8) & 0xFF));
					byte num29 = 5;
					foreach (byte num3 in data1)
					{
						msg4 = msg4 + string.Format("{0:X2} ", num3);
						devUtils.CheckLineLength(ref msg4, num29++, false);
					}
					DisplayMsg(SharedAppObjs.MsgType.RxDump, msg4);
				}
			}
		}
	}
}
