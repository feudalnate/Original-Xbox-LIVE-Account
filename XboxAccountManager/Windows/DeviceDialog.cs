using System;
using System.Linq;
using System.Collections.Generic;
using System.Management;
using System.Windows.Forms;
using System.Threading;

namespace AccountManager
{
    public partial class DeviceDialog : Form
    {
        private List<IDrive> loadedDrives;

        public API.XOnline.ONLINE_USER_ACCOUNT_STRUCT Account { set; get; }

        public DeviceDialog(bool saving)
        {
            InitializeComponent();

            DialogResult = DialogResult.Cancel;

            Shown += (s, e) =>
            {
                ListDrives();
            };

            deviceList.SelectedIndexChanged += (s, e) =>
            {
                if (deviceList.SelectedIndices == null || deviceList.SelectedIndices.Count == 0)
                {
                    openButton.Enabled = false;
                    openButton.Update();
                }
                else
                {
                    openButton.Enabled = true;
                    openButton.Update();
                }
            };

            deviceList.DoubleClick += (s, e) =>
            {
                openButton.PerformClick();
            };

            FormClosing += (s, e) =>
            {
                ClearDrives();
            };

            refreshButton.Click += (s, e) =>
            {
                ListDrives();
            };

            openButton.Click += (s, e) =>
            {
                if (deviceList.SelectedIndices == null || deviceList.SelectedIndices.Count != 1) return;
                var drive = loadedDrives[deviceList.SelectedIndices[0]];
                using (var accountDialog = new AccountDialog(ref drive, saving))
                {
                    if (saving)
                    {
                        accountDialog.Account = Account;
                        if (accountDialog.ShowDialog(this) == DialogResult.OK)
                        {
                            Close();
                        }
                        else ListDrives(); //refresh
                    }
                    else
                    {
                        if (accountDialog.ShowDialog(this) == DialogResult.OK)
                        {
                            Account = accountDialog.Account;
                            DialogResult = DialogResult.OK;
                            Close();
                        }
                        else ListDrives(); //refresh
                    }
                }
            };

            cancelButton.Click += (s, e) =>
            {
                Close();
            };

        }

        private void ListDrives()
        {
            new Thread(() => //thread this, has the potential to be fairly slow
            {
                ClearDrives();
                Invoke((Action)delegate
                {
                    openButton.Enabled = false;
                    openButton.Update();
                    refreshButton.Enabled = false;
                    refreshButton.Update();
                    label1.Text = "Searching..";
                    label1.Update();
                    deviceList.BeginUpdate();
                    deviceList.Items.Clear();
                });
                loadedDrives = ScanDrives();
                ListViewItem item;
                foreach (var drive in loadedDrives)
                {
                    item = new ListViewItem();
                    item.Text = drive.Name;
                    item.SubItems.Add(drive.FriendlyCapacity);
                    item.SubItems.Add(drive.IsMemoryCard ? "MU" : "HDD");
                    item.SubItems.Add(drive.Path);
                    item.SubItems.Add($"{drive.CurrentAccounts}/{drive.MaxAccounts}");
                    Invoke((Action)delegate
                    {
                        deviceList.Items.Add(item);
                    });
                }
                Invoke((Action)delegate
                {
                    deviceList.EndUpdate();
                    label1.Text = $"{loadedDrives.Count} FATX devices found";
                    label1.Update();
                    refreshButton.Enabled = true;
                    refreshButton.Update();
                });
            }).Start();
        }

        private void ClearDrives()
        {
            if (loadedDrives != null && loadedDrives.Count > 0)
            {
                foreach (var drive in loadedDrives) drive.Dispose();
                loadedDrives.Clear();
            }
        }

        private List<IDrive> ScanDrives()
        {
            //get drives from WMI (slow...)
            ManagementObjectCollection WMIObjects;
            using (var WMI = new ManagementObjectSearcher("SELECT Caption, Name, Size FROM Win32_DiskDrive"))
            {
                WMIObjects = WMI.Get();
            }

            //sort the properties of the results
            var sorted = new List<Dictionary<string, object>>();
            foreach (var obj in WMIObjects)
            {
                var list = new Dictionary<string, object>();
                foreach (var property in obj.Properties)
                {
                    if (property.Name == "Caption" || property.Name == "Name" || property.Name == "Size")
                    {
                        if (property.Value != null) list.Add(property.Name, property.Value);
                    }
                }
                if (list.Count != 3) continue;
                sorted.Add(list);
            }

            //check if any potential drives are FATX 
            var results = new List<IDrive>();
            foreach (var drive in sorted)
            {
                var temp = new Drive((string)drive["Caption"], (string)drive["Name"], (ulong)drive["Size"]);
                if (temp.IsOpen && temp.IsFATX)
                    results.Add(temp);
                else
                    temp.Dispose();
            }

            return results;
        }

    }
}
