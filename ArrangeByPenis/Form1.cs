using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace ArrangeByPenis
{
	public partial class Form1 : Form
	{

		const uint MEM_COMMIT = 0x1000;
		const uint PAGE_READWRITE = 4;
		const uint LVM_GETITEMCOUNT = 4100;
		const uint LVM_GETITEMPOSITION = 4112;
		const uint LVM_SETITEMPOSITION = 4111;
		const uint LVM_GETITEMTEXT = 4141;
		const ushort LVIF_TEXT = 1;

		[DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr FindWindow(string lpszClass, string lpszWindow);

		[DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className,  IntPtr windowTitle);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int dwSize, out int lpNumberOfBytesRead);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

		public Form1()
		{
			InitializeComponent();
		}

		private IntPtr CalculateCoordinates(int x, int y)
		{
			return (IntPtr)(x + (y << 16));
		}

		private void Arrange()
		{

		}

		private IntPtr listviewWindow;

		private void Form1_Load(object sender, EventArgs e)
		{
			// Check whether we're running under Vista or XP
			Version version = Environment.OSVersion.Version;
			if (version.Major != 6 && version.Major != 5)
			{
				MessageBox.Show("This application only works under Windows Vista or XP, and not "+ version.ToString());
				Application.Exit();
			}

			// Find a handle to the listview that makes up the desktop
			IntPtr programmanagerWindow = FindWindow(null, "Program Manager");
			IntPtr desktopWindow = FindWindowEx(programmanagerWindow, IntPtr.Zero, "SHELLDLL_DefView", null);
			listviewWindow = FindWindowEx(desktopWindow, IntPtr.Zero, "SysListView32", null);
		}

		private void ArrangeByPenis(IntPtr listviewWindow)
		{
			Size monitorSize = SystemInformation.PrimaryMonitorSize;

			Graph g = new Graph();

			// Convert the desktop listview handle to a handle reference so we can later use it in unsafe code
			HandleRef desktopReference = new HandleRef(null, listviewWindow);

			// Get the number of icons that's currently on the desktop
			int iconCount = (int)SendMessage(desktopReference, LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);

			double step = g.End / (double)iconCount;

			double v = 0.0;
			for (int i = 0; i < iconCount; i++)
			{
				Point p = g.getPoint(v, monitorSize.Width, monitorSize.Height);
				SendMessage(desktopReference, LVM_SETITEMPOSITION, (IntPtr)i, CalculateCoordinates(p.x, p.y));
				v += step;
			}
		}

		struct LVITEM { 
			public UInt32 mask;
			public int iItem;
			public int iSubItem;
			public UInt32 state;
			public UInt32 stateMask;
			public IntPtr pszText;
			public int cchTextMax;
			public int iImage;
			public UInt32 lParam;
			int iIndent;
			int iGroupId;
			uint cColumns;
			IntPtr puColumns;
			IntPtr piColFmt;
			int iGroup;
		};

		private void StoreIconPositions(IntPtr listviewWindow)
		{
			// Get the process handle to the currently running explorer
			Process[] processes = Process.GetProcessesByName("explorer");

			LinkedList<IconInfo> iconInfos = new LinkedList<IconInfo>();

			foreach (Process process in processes)
			{
				// Convert the desktop listview handle to a handle reference so we can later use it in unsafe code
				HandleRef desktopReference = new HandleRef(null, listviewWindow);

				// Allocate some memory in explorer's space so we can use that as temporary storage
				IntPtr ptr = VirtualAllocEx(process.Handle, IntPtr.Zero, 8, MEM_COMMIT, PAGE_READWRITE);
				IntPtr ipc_iconlabel = VirtualAllocEx(process.Handle, IntPtr.Zero, 100, MEM_COMMIT, PAGE_READWRITE);
				IntPtr ipc_buffer = VirtualAllocEx(process.Handle, IntPtr.Zero, 300, MEM_COMMIT, PAGE_READWRITE);

				// Get the number of icons that's currently on the desktop
				int iconCount = (int)SendMessage(desktopReference, LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);

				for (int i = 0; i < iconCount; i++)
				{
					// Send a window message to the desktop listview to get it to store the icon's position in our borrowed memory space
					IntPtr x2 = SendMessage(desktopReference, LVM_GETITEMPOSITION, (IntPtr)i, (IntPtr)ptr);

					// Copy the contents of the remote allocated array into our own memory space
					byte[] b = new byte[8];
					int f;
					unsafe
					{
						fixed (byte* bp = b)
						{
							ReadProcessMemory(process.Handle, ptr, (IntPtr)bp, 8, out f);
						}
					}

					int xpos = b[0] + (b[1] << 8) + (b[2] << 16) + (b[3] << 24);
					int ypos = b[4] + (b[5] << 8) + (b[6] << 16) + (b[7] << 24);


					LVITEM iconlabel = new LVITEM();
					iconlabel.iSubItem = 0;
					iconlabel.cchTextMax = 256;
					iconlabel.mask = LVIF_TEXT;
					iconlabel.pszText = ipc_buffer;

					// Test code
					/*string s1 = listView1.Items[0].Text;
					unsafe
					{
						HandleRef xReference = new HandleRef(null, listView1.Handle);
						LVITEM* bp = &iconlabel;
						byte[] bx = new byte[300];
						fixed (byte* bxp = bx)
						{
							iconlabel.pszText = (IntPtr)bxp;
							IntPtr x23 = SendMessage(xReference, LVM_GETITEMTEXT, (IntPtr)0, (IntPtr)bp);
						}
					}*/

					unsafe
					{
						LVITEM* bp = &iconlabel;
						WriteProcessMemory(process.Handle, ipc_iconlabel, (IntPtr)bp, 100, out f);
					}

					int iconNameLength = (int)SendMessage(desktopReference, LVM_GETITEMTEXT, (IntPtr)i, ipc_iconlabel);

					// Copy the contents of the remote allocated array into our own memory space
					byte[] b2 = new byte[300];
					unsafe
					{
						fixed (byte* bp = b2)
						{
							ReadProcessMemory(process.Handle, ipc_buffer, (IntPtr)bp, 300, out f);
						}
					}
					char[] c2 = new char[iconNameLength];
					for (int j = 0; j < iconNameLength; j++)
						c2[j] = (char)b2[j];
					String iconName = new String(c2);

					IconInfo ii = new IconInfo(iconName, xpos, ypos);
					iconInfos.AddLast(ii);
				}

				string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Arrange\\";
				if (!Directory.Exists(folder))
					Directory.CreateDirectory(folder);

				Stream stream = File.Open(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Arrange\\backup.dat", FileMode.Create);
				BinaryFormatter bformatter = new BinaryFormatter();

				bformatter.Serialize(stream, iconInfos);
				stream.Close();
			}
		}

		private void RestoreIconPositions(IntPtr listviewWindow)
		{
			Stream stream = File.Open(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Arrange\\backup.dat", FileMode.Open);
			BinaryFormatter bformatter = new BinaryFormatter();
			LinkedList<IconInfo> iconInfos = (LinkedList<IconInfo>)bformatter.Deserialize(stream);
			stream.Close();

			// Get the process handle to the currently running explorer
			Process[] processes = Process.GetProcessesByName("explorer");

			foreach (Process process in processes)
			{
				// Convert the desktop listview handle to a handle reference so we can later use it in unsafe code
				HandleRef desktopReference = new HandleRef(null, listviewWindow);

				// Allocate some memory in explorer's space so we can use that as temporary storage
				IntPtr ptr = VirtualAllocEx(process.Handle, IntPtr.Zero, 8, MEM_COMMIT, PAGE_READWRITE);
				IntPtr ipc_iconlabel = VirtualAllocEx(process.Handle, IntPtr.Zero, 100, MEM_COMMIT, PAGE_READWRITE);
				IntPtr ipc_buffer = VirtualAllocEx(process.Handle, IntPtr.Zero, 300, MEM_COMMIT, PAGE_READWRITE);

				// Get the number of icons that's currently on the desktop
				int iconCount = (int)SendMessage(desktopReference, LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);

				for (int i = 0; i < iconCount; i++)
				{
					int f;
					LVITEM iconlabel = new LVITEM();
					iconlabel.iSubItem = 0;
					iconlabel.cchTextMax = 256;
					iconlabel.mask = LVIF_TEXT;
					iconlabel.pszText = ipc_buffer;

					unsafe
					{
						LVITEM* bp = &iconlabel;
						WriteProcessMemory(process.Handle, ipc_iconlabel, (IntPtr)bp, 100, out f);
					}

					int iconNameLength = (int)SendMessage(desktopReference, LVM_GETITEMTEXT, (IntPtr)i, ipc_iconlabel);

					// Copy the contents of the remote allocated array into our own memory space
					byte[] b2 = new byte[300];
					unsafe
					{
						fixed (byte* bp = b2)
						{
							ReadProcessMemory(process.Handle, ipc_buffer, (IntPtr)bp, 300, out f);
						}
					}
					char[] c2 = new char[iconNameLength];
					for (int j = 0; j < iconNameLength; j++)
						c2[j] = (char)b2[j];
					String iconName = new String(c2);

					foreach (IconInfo ii in iconInfos)
					{
						if (ii.name == iconName)
						{
							SendMessage(desktopReference, LVM_SETITEMPOSITION, (IntPtr)i, CalculateCoordinates(ii.x, ii.y)); 
							break;
						}
					}
				}

			}
		}

		private void saveButton_Click(object sender, EventArgs e)
		{
			StoreIconPositions(listviewWindow);
		}

		private void arrangeButton_Click(object sender, EventArgs e)
		{
			ArrangeByPenis(listviewWindow);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			RestoreIconPositions(listviewWindow);
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}
	}
}

