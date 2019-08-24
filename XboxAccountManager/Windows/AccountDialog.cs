using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace AccountManager
{
    public partial class AccountDialog : Form
    {
        private IDrive currentDrive;
        private List<API.XOnline.ONLINE_USER_ACCOUNT_STRUCT> loadedAccounts;

        public API.XOnline.ONLINE_USER_ACCOUNT_STRUCT Account { set; get; }

        public AccountDialog(ref IDrive drive, bool saving)
        {
            InitializeComponent();
            DialogResult = DialogResult.Cancel;

            Shown += (s, e) =>
            {
                if (saving) selectButton.Text = "Save";
                else selectButton.Text = "Load";
                selectButton.Update();
                ListAccounts();
            };

            slotsList.SelectedIndexChanged += (s, e) =>
            {
                if (slotsList.SelectedIndices != null && slotsList.SelectedIndices.Count > 0)
                {
                    API.XOnline.ONLINE_USER_ACCOUNT_STRUCT temp;
                    int index = slotsList.SelectedIndices[0];
                    if (currentDrive.ReadAccount(index, out temp))
                    {
                        selectButton.Enabled = true;
                        deleteButton.Enabled = true;
                        selectButton.Update();
                        deleteButton.Update();
                    }
                    else
                    {
                        if (!saving)
                        {
                            selectButton.Enabled = false;
                            selectButton.Update();
                        }
                        else
                        {
                            selectButton.Enabled = true;
                            selectButton.Update();
                        }
                        deleteButton.Enabled = false;
                        deleteButton.Update();
                    }
                    
                }
                else
                {
                    selectButton.Enabled = false;
                    deleteButton.Enabled = false;
                    selectButton.Update();
                    deleteButton.Update();
                }
            };

            slotsList.DoubleClick += (s, e) =>
            {
                selectButton.PerformClick();
            };

            deleteButton.Click += (s, e) =>
            {
                if (slotsList.SelectedIndices != null && slotsList.SelectedIndices.Count == 1)
                {
                    int index = slotsList.SelectedIndices[0];
                    API.XOnline.ONLINE_USER_ACCOUNT_STRUCT account;
                    if (currentDrive.ReadAccount(index, out account))
                    {
                        var message = $"Account \'{new string(account.Gamertag).TrimEnd('\0')}\' will be deleted from the following device:\n\n" +
                        $"Device: {currentDrive.Name} ({(currentDrive.IsMemoryCard ? "MU" : "HDD")})\n" +
                        $"Capacity: {currentDrive.FriendlyCapacity} ({currentDrive.Capacity.ToString("N0")} bytes)\n" +
                        $"Slot: {index + 1} (index: {index})\n\n" +
                        "Continue?";
                        if (MessageBox.Show(this, message, "Confirm device write", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            if (currentDrive.DeleteAccount(index))
                            {
                                MessageBox.Show(this, "Account data was deleted successfully", "Account saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show(this,
                                    "Failed to write data to device\n\nThis can fail on occassion if the Operating System is busy with the device.\nWait 5-10 seconds and try again\n\n" +
                                    "If writes continue to fail, close the program, unplug your device for a few seconds, plug it back in, and try again",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            ListAccounts(); //refresh
                        }
                    }
                }
            };

            selectButton.Click += (s, e) =>
            {
                if (slotsList.SelectedIndices != null && slotsList.SelectedIndices.Count == 1)
                {
                    int index = slotsList.SelectedIndices[0];
                    if (saving)
                    {
                        if (!default(API.XOnline.ONLINE_USER_ACCOUNT_STRUCT).Equals(Account))
                        {
                            var message = $"Account \'{new string(Account.Gamertag).TrimEnd('\0')}\' will be written to the following device:\n\n" +
                            $"Device: {currentDrive.Name} ({(currentDrive.IsMemoryCard ? "MU" : "HDD")})\n" +
                            $"Capacity: {currentDrive.FriendlyCapacity} ({currentDrive.Capacity.ToString("N0")} bytes)\n" +
                            $"Slot: {index + 1} (index: {index})\n\n" +
                            "If an account already exists in the selected slot, it will be overwritten.\n\nContinue?";
                            if (MessageBox.Show(this, message, "Confirm device write", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                if (currentDrive.WriteAccount(index, Account))
                                {
                                    MessageBox.Show(this, "Account data was written successfully", "Account saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                else
                                {
                                    MessageBox.Show(this,
                                        "Failed to write data to device\n\nThis can fail on occassion if the Operating System is busy with the device.\nWait 5-10 seconds and try again\n\n" +
                                        "If writes continue to fail, close the program, unplug your device for a few seconds, plug it back in, and try again",
                                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                    }
                    else
                    {
                        Account = loadedAccounts[index];
                    }
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };

            cancelButton.Click += (s, e) =>
            {
                Close();
            };

            if (drive == null) //this shouldnt happen
            {
                Close();
            }

            currentDrive = drive;
            Text = currentDrive.Name;
        }

        private void ListAccounts()
        {
            new Thread(() => //don't really need to thread this but we wait on IO so why not
            {
                Invoke((Action)delegate
                {
                    selectButton.Enabled = false;
                    selectButton.Update();
                    deleteButton.Enabled = false;
                    deleteButton.Update();
                    label1.Text = "Searching..";
                    label1.Update();
                    slotsList.BeginUpdate();
                    slotsList.Items.Clear();
                });
                loadedAccounts = ScanAccounts();
                int count = 0;
                ListViewItem item;
                foreach (var account in loadedAccounts)
                {
                    item = new ListViewItem();
                    if (account.Equals(default(API.XOnline.ONLINE_USER_ACCOUNT_STRUCT)))
                    {
                        item.Text = "(EMPTY)";
                        item.ImageIndex = 0;
                    }
                    else
                    {
                        item.Text = new string(account.Gamertag).TrimEnd('\0');
                        item.ImageIndex = 1;
                        count++;
                    }
                    Invoke((Action)delegate
                    {
                        slotsList.Items.Add(item);
                    });
                }
                Invoke((Action)delegate
                {
                    slotsList.EndUpdate();
                    label1.Text = $"{count} / {currentDrive.MaxAccounts} slot{(count > 1 ? "s" : "")} in use";
                    label1.Update();
                });
            }).Start();
        }

        private List<API.XOnline.ONLINE_USER_ACCOUNT_STRUCT> ScanAccounts()
        {
            var result = new List<API.XOnline.ONLINE_USER_ACCOUNT_STRUCT>();
            if (currentDrive != null && currentDrive.MaxAccounts > 0)
            {
                API.XOnline.ONLINE_USER_ACCOUNT_STRUCT account;
                for (int i = 0; i < currentDrive.MaxAccounts; i++)
                {
                    result.Add(currentDrive.ReadAccount(i, out account) ? account : default(API.XOnline.ONLINE_USER_ACCOUNT_STRUCT));
                }
            }
            return result;
        }

    }
}
