using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace AccountManager
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();

            checkBox1.CheckedChanged += (s, e) =>
            {
                comboBox1.Enabled = checkBox1.Checked;
                comboBox2.Enabled = checkBox1.Checked;
                comboBox3.Enabled = checkBox1.Checked;
                comboBox4.Enabled = checkBox1.Checked;
                comboBox1.Update();
                comboBox2.Update();
                comboBox3.Update();
                comboBox4.Update();
            };

            blankMenuItem.Click += (s, e) =>
            {
                Blank();
            };

            generateMenuItem.Click += (s, e) =>
            {
                Generate();
            };

            openFileMenuItem.Click += (s, e) =>
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Title = "Open XBL Account File";
                    dialog.Filter = "All Files (*.*)|*.*";
                    dialog.Multiselect = false;
                    dialog.CheckFileExists = true;
                    dialog.CheckPathExists = true;
                    dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    dialog.RestoreDirectory = true;
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        var file = new FileInfo(dialog.FileName);
                        if (file.Length != 0x6C)
                        {
                            MessageBox.Show(this, "Selected file is not a valid Xbox LIVE account file", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        var buffer = new byte[0x6C];
                        using (var stream = file.OpenRead())
                        {
                            stream.Read(buffer, 0, 0x6C);
                        }
                        var account = buffer.Deserialize<API.XOnline.ONLINE_USER_ACCOUNT_STRUCT>();
                        if (!API.XOnline.VerifyOnlineUserSignature(account))
                        {
                            MessageBox.Show(this, "Selected file is not a valid Xbox LIVE account file", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        LoadAccountToUI(account);
                    }
                }
            };

            saveFileMenuItem.Click += (s, e) =>
            {
                //check xuid
                if (string.IsNullOrEmpty(textBox1.Text) || textBox1.Text.Length != 16 || !textBox1.Text.IsHex())
                {
                    MessageBox.Show(this, "XUID must be 8 bytes in length (16 hexadecimal characters)\n\nIt's safe to set the XUID to all zeroes", "Invalid XUID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //check gamertag
                if (string.IsNullOrEmpty(textBox2.Text) || !textBox2.Text.IsASCII())
                {
                    MessageBox.Show(this, "Gamertag must be at least 1 character in length. Only ASCII characters are valid", "Invalid Gamertag", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var dialog = new SaveFileDialog())
                {
                    dialog.Title = "Save XBL Account File";
                    dialog.Filter = "All Files (*.*)|*.*";
                    dialog.CheckPathExists = true;
                    dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    dialog.RestoreDirectory = true;
                    dialog.OverwritePrompt = true;
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        var account = UnloadAccountFromUI();
                        if (!API.XOnline.SignOnlineUserSignature(ref account))
                        {
                            //this shouldnt fail but we'll handle it anyway
                            MessageBox.Show(this, "Failed to encrypt and sign account data", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        var buffer = account.Serialize();
                        using (var stream = File.Open(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                        {
                            stream.Write(buffer, 0, 0x6C);
                            stream.Flush();
                        }
                        MessageBox.Show(this, "Account data was written successfully", "Account saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };

            openDeviceMenuItem.Click += (s, e) =>
            {
                using (var deviceDialog = new DeviceDialog(false))
                {
                    if (deviceDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        LoadAccountToUI(deviceDialog.Account);
                    }
                }
            };

            saveDeviceMenuItem.Click += (s, e) =>
            {
                //check xuid
                if (string.IsNullOrEmpty(textBox1.Text) || textBox1.Text.Length != 16 || !textBox1.Text.IsHex())
                {
                    MessageBox.Show(this, "XUID must be 8 bytes in length (16 hexadecimal characters)\n\nIt's safe to set the XUID to all zeroes", "Invalid XUID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //check gamertag
                if (string.IsNullOrEmpty(textBox2.Text) || !textBox2.Text.IsASCII())
                {
                    MessageBox.Show(this, "Gamertag must be at least 1 character in length. Only ASCII characters are valid", "Invalid Gamertag", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var deviceDialog = new DeviceDialog(true))
                {
                    deviceDialog.Account = UnloadAccountFromUI();
                    deviceDialog.ShowDialog(this);
                }
            };

            Generate();

        }

        private void Blank()
        {
            textBox1.Text = "".PadRight(16, '0');
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
            checkBox1.Checked = false;
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            comboBox4.SelectedIndex = 0;
            textBox1.Update();
            textBox2.Update();
            textBox3.Update();
            textBox4.Update();
            checkBox1.Update();
            comboBox1.Update();
            comboBox2.Update();
            comboBox3.Update();
            comboBox4.Update();
        }

        private void Generate()
        {
            textBox1.Text = GenerateXUID();
            textBox2.Text = "";
            textBox3.Text = "xbox.com";
            textBox4.Text = "PASSPORT.NET";
            checkBox1.Checked = false;
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            comboBox4.SelectedIndex = 0;
            textBox1.Update();
            textBox2.Update();
            textBox3.Update();
            textBox4.Update();
            checkBox1.Update();
            comboBox1.Update();
            comboBox2.Update();
            comboBox3.Update();
            comboBox4.Update();
        }

        private void LoadAccountToUI(API.XOnline.ONLINE_USER_ACCOUNT_STRUCT account)
        {
            textBox1.Text = BitConverter.GetBytes(account.XUID).ToHex();
            textBox2.Text = new string(account.Gamertag).TrimEnd('\0');
            textBox3.Text = new string(account.Domain).TrimEnd('\0');
            textBox4.Text = new string(account.Realm).TrimEnd('\0');
            if (GetBit(account.Flags, 0))
            {
                checkBox1.Checked = true;
                comboBox1.SelectedIndex = PasscodeButtonValueToIndex(account.Passcode[0]);
                comboBox2.SelectedIndex = PasscodeButtonValueToIndex(account.Passcode[1]);
                comboBox3.SelectedIndex = PasscodeButtonValueToIndex(account.Passcode[2]);
                comboBox4.SelectedIndex = PasscodeButtonValueToIndex(account.Passcode[3]);
            }
            else
            {
                checkBox1.Checked = false;
                comboBox1.SelectedIndex = 0;
                comboBox2.SelectedIndex = 0;
                comboBox3.SelectedIndex = 0;
                comboBox4.SelectedIndex = 0;
            }
            textBox1.Update();
            textBox2.Update();
            textBox3.Update();
            textBox4.Update();
            checkBox1.Update();
            comboBox1.Update();
            comboBox2.Update();
            comboBox3.Update();
            comboBox4.Update();
        }

        private API.XOnline.ONLINE_USER_ACCOUNT_STRUCT UnloadAccountFromUI()
        {
            var account = new API.XOnline.ONLINE_USER_ACCOUNT_STRUCT();
            account.XUID = BitConverter.ToUInt64(textBox1.Text.ToBytes(), 0);
            account.unknown = 0; //must be zero to be considered a valid account
            account.Gamertag = new char[0x10];
            char[] gamertag = Encoding.ASCII.GetChars(Encoding.ASCII.GetBytes(textBox2.Text));
            Array.Copy(gamertag, account.Gamertag, gamertag.Length);
            uint flags = 0;
            byte[] passcode = new byte[4];
            if (checkBox1.Checked)
            {
                flags = SetBit(flags, 0);
                passcode[0] = IndexToPasscodeButtonValue(comboBox1.SelectedIndex);
                passcode[1] = IndexToPasscodeButtonValue(comboBox2.SelectedIndex);
                passcode[2] = IndexToPasscodeButtonValue(comboBox3.SelectedIndex);
                passcode[3] = IndexToPasscodeButtonValue(comboBox4.SelectedIndex);
            }
            account.Flags = flags;
            account.Passcode = passcode;
            account.Domain = new char[0x14];
            if (!string.IsNullOrEmpty(textBox3.Text))
            {
                char[] domain = Encoding.ASCII.GetChars(Encoding.ASCII.GetBytes(textBox3.Text));
                Array.Copy(domain, account.Domain, domain.Length);
            }
            account.Realm = new char[0x18];
            if (!string.IsNullOrEmpty(textBox4.Text))
            {
                char[] realm = Encoding.ASCII.GetChars(Encoding.ASCII.GetBytes(textBox4.Text));
                Array.Copy(realm, account.Realm, realm.Length);
            }
            var confounder = new byte[0x14];
            new Random().NextBytes(confounder);
            account.Confounder = confounder;
            account.Verification = new byte[8];
            return account;
        }

        private string GenerateXUID()
        {
            var random = new Random();
            var result = new List<byte>();
            result.AddRange(BitConverter.GetBytes((uint)random.Next()));
            result.AddRange(BitConverter.GetBytes((ushort)random.Next()));
            result.AddRange(BitConverter.GetBytes((ushort)9));
            return result.ToArray().ToHex();
        }

        private int PasscodeButtonValueToIndex(byte buttonValue)
        {
            if (buttonValue > 0 && buttonValue <= 8) return buttonValue - 1;
            if (buttonValue == 0xB) return 8;
            if (buttonValue == 0xC) return 9;
            return 0;
        }

        private byte IndexToPasscodeButtonValue(int index)
        {
            if (index >= 0 && index <= 7) return (byte)(index + 1);
            if (index == 8) return 0xB;
            if (index == 9) return 0xC;
            return 0;
        }

        private bool GetBit(uint value, int index)
        {
            return (value & (1 << index)) != 0;
        }

        private uint SetBit(uint value, int index)
        {
            value |= (uint)1UL << index;
            return value;
        }

    }

    public static class Extensions
    {

        public static T Deserialize<T>(this byte[] buffer) where T : struct
        {
            int size;
            IntPtr pointer;
            T structure;
            size = Marshal.SizeOf(typeof(T));
            pointer = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, pointer, size);
            structure = (T)Marshal.PtrToStructure(pointer, typeof(T));
            Marshal.FreeHGlobal(pointer);
            return structure;
        }

        public static byte[] Serialize<T>(this T structure) where T : struct
        {
            IntPtr pointer;
            int size;
            byte[] buffer;
            size = Marshal.SizeOf(typeof(T));
            buffer = new byte[size];
            pointer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structure, pointer, true);
            Marshal.Copy(pointer, buffer, 0, size);
            Marshal.FreeHGlobal(pointer);
            return buffer;
        }

        public static bool IsASCII(this string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (var c in text)
            {
                if (c > 127) return false;
            }
            return true;
        }

        public static bool IsHex(this string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            text = text.Replace("\0", "").Replace(":", "").Replace("-", "").Replace("\r\n", "").Replace("\n", "").Replace(" ", "");
            if (text.Length % 2 != 0) return false;
            char[] valid = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
            foreach (char c in text)
            {
                if (!valid.Contains(c)) return false;
            }
            return true;
        }

        public static string ToHex(this byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0) return "";
            var result = new StringBuilder();
            for (int i = 0; i < buffer.Length; i++)
            {
                result.Append(buffer[i].ToString("X2"));
            }
            return result.ToString();
        }

        public static byte[] ToBytes(this string a)
        {
            if (a.IsHex())
            {
                return Enumerable.Range(0, a.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(a.Substring(x, 2), 16)).ToArray();
            }
            return null;
        }

    }

}
