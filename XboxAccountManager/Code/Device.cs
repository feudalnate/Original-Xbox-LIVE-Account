using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AccountManager
{

    public interface IDrive
    {
        //easier to do an interface to work in the device image support without redoing a bunch of stuff
        string Name { get; }
        string Path { get; }
        ulong Capacity { get; }
        bool IsOpen { get; }
        void Dispose();
        bool IsFATX { get; }
        bool IsMemoryCard { get; }
        int CurrentAccounts { get; }
        int MaxAccounts { get; }
        string FriendlyCapacity { get; }
        bool ReadAccount(int index, out API.XOnline.ONLINE_USER_ACCOUNT_STRUCT account);
        bool WriteAccount(int index, API.XOnline.ONLINE_USER_ACCOUNT_STRUCT account);
        bool DeleteAccount(int index);
    }

    public class Drive : IDisposable, IDrive
    {
        private bool disposed; //want to avoid calling CloseHandle multiple times on accident (interop calls are expensive)
        private SafeFileHandle handle;

        /// <summary>
        /// Device name
        /// </summary>
        public string Name { private set; get; }

        /// <summary>
        /// OS mounted path
        /// </summary>
        public string Path { private set; get; }

        /// <summary>
        /// Usable space on the device in bytes
        /// </summary>
        public ulong Capacity { private set; get; }

        /// <summary>
        /// Whether the device handle is open
        /// </summary>
        public bool IsOpen { private set; get; }

        internal Drive(string name, string path, ulong capacity)
        {
            Name = name;
            Path = path;
            Capacity = capacity;
            IsOpen = Open();
            disposed = false;
        }

        ~Drive() { Dispose(); }

        public void Dispose()
        {
            if (disposed) return;
            handle.Close();
            IsOpen = false;
            disposed = true;
        }

        private bool Open()
        {
            if (disposed) return false;
            try
            {
                handle = Win32.CreateFile(Path,
                    Win32.DesiredAccess.GENERIC_READ | Win32.DesiredAccess.GENERIC_WRITE,
                    Win32.ShareMode.FILE_SHARE_READ | Win32.ShareMode.FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    Win32.CreationDisposition.OPEN_EXISTING,
                    Win32.FlagsAndAttributes.FILE_FLAG_NO_BUFFERING | Win32.FlagsAndAttributes.FILE_FLAG_WRITE_THROUGH,
                    IntPtr.Zero);
                if (handle.IsInvalid) return false;
                return true;
            }
            catch
            {
                handle.Close();
                return false;
            }
        }

        /// <summary>
        /// Whether device is FATX formatted
        /// </summary>
        public bool IsFATX
        {
            get
            {
                if (disposed) return false;
                if (Win32.SetFilePointer(handle, 0, IntPtr.Zero, Win32.MoveMethod.FILE_BEGIN) != Win32.INVALID_SET_FILE_POINTER)
                {
                    //When you are directly accessing a disk device, you cannot seek to positions or read/write lengths in the middle of a sector. 
                    //The position and length must always be a multiple of the sector length.
                    var sector = new byte[0x200];
                    uint read = 0;
                    if (Win32.ReadFile(handle, sector, 0x200, ref read, IntPtr.Zero) && read == 0x200)
                    {
                        if (BitConverter.ToUInt32(sector, 0) != 0x58544146) return false;
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// True if the device is a MU else false for HDD
        /// </summary>
        public bool IsMemoryCard
        {
            get
            {
                //assume memory card if drive capacity is below 8GB
                if (Capacity >= 0x1BA443700) return false;
                return true;
            }
        }

        /// <summary>
        /// Number of accounts currently stored on the device
        /// </summary>
        public int CurrentAccounts
        {
            get
            {
                if (disposed) return 0;
                int slots = MaxAccounts;
                if (slots == 0) return 0;
                int count = 0;
                if (Win32.SetFilePointer(handle, 0, IntPtr.Zero, Win32.MoveMethod.FILE_BEGIN) != Win32.INVALID_SET_FILE_POINTER)
                {
                    byte[] sectors = new byte[0x400]; //2 sectors (1 sector = 512 bytes (0x200))
                    uint read = 0;
                    if (Win32.ReadFile(handle, sectors, 0x400, ref read, IntPtr.Zero) && read == 0x400)
                    {
                        byte[] buffer = new byte[0x6C];
                        API.XOnline.ONLINE_USER_ACCOUNT_STRUCT account;
                        for (int i = 0; i < slots; i++)
                        {
                            Array.Copy(sectors, (0x50 + (i * 0x6C)), buffer, 0, 0x6C);
                            account = buffer.Deserialize<API.XOnline.ONLINE_USER_ACCOUNT_STRUCT>();
                            if (API.XOnline.VerifyOnlineUserSignature(account)) count++;
                        }
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Maximum number of accounts that can be stored on the device
        /// </summary>
        public int MaxAccounts
        {
            get
            {
                if (disposed || !IsFATX) return 0;
                if (IsMemoryCard) return 1; //MU 1 account slot
                return 8; //HDD 8 account slots
            }
        }

        /// <summary>
        /// Friendly string containing the amount of usable space on the drive
        /// </summary>
        public string FriendlyCapacity
        {
            get
            {
                if (disposed) return "";
                var sizes = new string[] { "B", "KB", "MB", "GB", "TB" };
                var temp = (double)Capacity;
                int i = 0;
                while (temp > 1024.0)
                {
                    temp /= 1024.0;
                    i++;
                }
                return $"{Math.Round(temp, 2)} {sizes[i]}";
            }
        }

        public bool ReadAccount(int index, out API.XOnline.ONLINE_USER_ACCOUNT_STRUCT account)
        {
            account = default(API.XOnline.ONLINE_USER_ACCOUNT_STRUCT);
            int max = MaxAccounts;
            if (max == 0 || (index > (max - 1))) return false;
            if (Win32.SetFilePointer(handle, 0, IntPtr.Zero, Win32.MoveMethod.FILE_BEGIN) != Win32.INVALID_SET_FILE_POINTER)
            {
                byte[] sectors = new byte[0x400]; //2 sectors (1 sector = 512 bytes (0x200))
                uint read = 0;
                if (Win32.ReadFile(handle, sectors, 0x400, ref read, IntPtr.Zero) && read == 0x400)
                {
                    byte[] buffer = new byte[0x6C];
                    Array.Copy(sectors, (0x50 + (index * 0x6C)), buffer, 0, 0x6C);
                    account = buffer.Deserialize<API.XOnline.ONLINE_USER_ACCOUNT_STRUCT>();
                    return API.XOnline.VerifyOnlineUserSignature(account);
                }
            }
            return false;
        }

        public bool WriteAccount(int index, API.XOnline.ONLINE_USER_ACCOUNT_STRUCT account)
        {
            int max = MaxAccounts;
            if (max == 0 || (index > (max - 1))) return false;
            if (!API.XOnline.SignOnlineUserSignature(ref account)) return false;
            if (Win32.SetFilePointer(handle, 0, IntPtr.Zero, Win32.MoveMethod.FILE_BEGIN) != Win32.INVALID_SET_FILE_POINTER)
            {
                byte[] sectors = new byte[0x400]; //2 sectors (1 sector = 512 bytes (0x200))
                uint read = 0;
                if (Win32.ReadFile(handle, sectors, 0x400, ref read, IntPtr.Zero) && read == 0x400) //read the FATX header out
                {
                    if (Win32.SetFilePointer(handle, 0, IntPtr.Zero, Win32.MoveMethod.FILE_BEGIN) != Win32.INVALID_SET_FILE_POINTER)
                    {
                        byte[] buffer = account.Serialize();
                        Array.Copy(buffer, 0, sectors, (0x50 + (index * 0x6C)), 0x6C); //copy the account data into the header
                        uint written = 0;
                        if (Win32.WriteFile(handle, sectors, 0x400, ref written, IntPtr.Zero)) //write the FATX header back
                        {
                            return written == 0x400;
                        }
                    }
                }
            }
            return false;
        }

        public bool DeleteAccount(int index)
        {
            int max = MaxAccounts;
            if (max == 0 || (index > (max - 1))) return false;

            byte[] buffer = new byte[]
            {
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
            };

            if (Win32.SetFilePointer(handle, 0, IntPtr.Zero, Win32.MoveMethod.FILE_BEGIN) != Win32.INVALID_SET_FILE_POINTER)
            {
                byte[] sectors = new byte[0x400]; //2 sectors (1 sector = 512 bytes (0x200))
                uint read = 0;
                if (Win32.ReadFile(handle, sectors, 0x400, ref read, IntPtr.Zero) && read == 0x400) //read the FATX header out
                {
                    if (Win32.SetFilePointer(handle, 0, IntPtr.Zero, Win32.MoveMethod.FILE_BEGIN) != Win32.INVALID_SET_FILE_POINTER)
                    {
                        Array.Copy(buffer, 0, sectors, (0x50 + (index * 0x6C)), 0x6C); //copy the account data into the header
                        uint written = 0;
                        if (Win32.WriteFile(handle, sectors, 0x400, ref written, IntPtr.Zero)) //write the FATX header back
                        {
                            return written == 0x400;
                        }
                    }
                }
            }
            return false;
        }

        private static class Win32
        {

            [Flags]
            public enum DesiredAccess : uint
            {
                GENERIC_READ = 0x80000000,
                GENERIC_WRITE = 0x40000000
            }

            [Flags]
            public enum ShareMode : uint
            {
                FILE_SHARE_NONE = 0x0,
                FILE_SHARE_READ = 0x1,
                FILE_SHARE_WRITE = 0x2,
                FILE_SHARE_DELETE = 0x4,
            }

            public enum MoveMethod : uint
            {
                FILE_BEGIN = 0,
                FILE_CURRENT = 1,
                FILE_END = 2
            }

            public enum CreationDisposition : uint
            {
                CREATE_NEW = 1,
                CREATE_ALWAYS = 2,
                OPEN_EXISTING = 3,
                OPEN_ALWAYS = 4,
                TRUNCATE_EXSTING = 5
            }

            [Flags]
            public enum FlagsAndAttributes : uint
            {
                FILE_ATTRIBUTES_ARCHIVE = 0x20,
                FILE_ATTRIBUTE_HIDDEN = 0x2,
                FILE_ATTRIBUTE_NORMAL = 0x80,
                FILE_ATTRIBUTE_OFFLINE = 0x1000,
                FILE_ATTRIBUTE_READONLY = 0x1,
                FILE_ATTRIBUTE_SYSTEM = 0x4,
                FILE_ATTRIBUTE_TEMPORARY = 0x100,
                FILE_FLAG_WRITE_THROUGH = 0x80000000,
                FILE_FLAG_OVERLAPPED = 0x40000000,
                FILE_FLAG_NO_BUFFERING = 0x20000000,
                FILE_FLAG_RANDOM_ACCESS = 0x10000000,
                FILE_FLAG_SEQUENTIAL_SCAN = 0x8000000,
                FILE_FLAG_DELETE_ON = 0x4000000,
                FILE_FLAG_POSIX_SEMANTICS = 0x1000000,
                FILE_FLAG_OPEN_REPARSE_POINT = 0x200000,
                FILE_FLAG_OPEN_NO_CALL = 0x100000
            }

            public const uint INVALID_HANDLE_VALUE = 0xFFFFFFFF;
            public const uint INVALID_SET_FILE_POINTER = 0xFFFFFFFF;

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern SafeFileHandle CreateFile(string lpFileName, DesiredAccess dwDesiredAccess, ShareMode dwShareMode, IntPtr lpSecurityAttributes, CreationDisposition dwCreationDisposition, FlagsAndAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);

            [DllImport("kernel32", SetLastError = true)]
            public static extern bool ReadFile(SafeFileHandle hFile, byte[] aBuffer, uint cbToRead, ref uint cbThatWereRead, IntPtr pOverlapped);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool WriteFile(SafeFileHandle hFile, byte[] aBuffer, uint cbToWrite, ref uint cbThatWereWritten, IntPtr pOverlapped);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern uint SetFilePointer(SafeFileHandle hFile, int cbDistanceToMove, IntPtr pDistanceToMoveHigh, MoveMethod fMoveMethod);

        }

    }

    public class DriveImage : IDisposable, IDrive
    {
        private bool disposed;
        private FileStream stream;

        /// <summary>
        /// File name
        /// </summary>
        public string Name { private set; get; }

        /// <summary>
        /// Path to the file on disk
        /// </summary>
        public string Path { private set; get; }

        /// <summary>
        /// Size of the file on disk
        /// </summary>
        public ulong Capacity { private set; get; }

        /// <summary>
        /// Whether the file stream is open
        /// </summary>
        public bool IsOpen { private set; get; }

        internal DriveImage(string path)
        {
            //the check for if the file exists and that it is also a FATX image is done at the UI level
            stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            var info = new FileInfo(path);
            Name = info.Name;
            Path = path;
            Capacity = (ulong)info.Length;
            IsOpen = true;
            disposed = false;
        }

        ~DriveImage() { Dispose(); }

        public void Dispose()
        {
            if (disposed) return;
            stream.Close();
            stream.Dispose();
            IsOpen = false;
            disposed = true;
        }

        /// <summary>
        /// Whether device image is FATX formatted
        /// </summary>
        public bool IsFATX
        {
            get
            {
                //this is pre-checked at the UI level
                return true;
            }
        }

        /// <summary>
        /// True if the device image is a MU else false for HDD
        /// </summary>
        public bool IsMemoryCard
        {
            get
            {
                //assume memory card if drive image size is below 8GB
                if (Capacity >= 0x1BA443700) return false;
                return true;
            }
        }

        /// <summary>
        /// Number of accounts currently stored in the device image
        /// </summary>
        public int CurrentAccounts
        {
            get
            {
                if (disposed) return 0;
                int slots = MaxAccounts;
                if (slots == 0) return 0;
                int count = 0;
                byte[] buffer = new byte[0x6C];
                API.XOnline.ONLINE_USER_ACCOUNT_STRUCT account;
                try
                {
                    for (int i = 0; i < slots; i++)
                    {
                        stream.Seek((0x50 + (i * 0x6C)), SeekOrigin.Begin);
                        if (stream.Read(buffer, 0, 0x6C) == 0x6C)
                        {
                            account = buffer.Deserialize<API.XOnline.ONLINE_USER_ACCOUNT_STRUCT>();
                            if (API.XOnline.VerifyOnlineUserSignature(account)) count++;
                        }
                        else return 0;
                    }
                }
                catch { return 0; }
                return count;
            }
        }

        /// <summary>
        /// Maximum number of accounts that can be stored in the device image
        /// </summary>
        public int MaxAccounts
        {
            get
            {
                if (disposed) return 0;
                if (IsMemoryCard) return 1; //MU 1 account slot
                return 8; //HDD 8 account slots
            }
        }

        /// <summary>
        /// Friendly string containing the size of the device image
        /// </summary>
        public string FriendlyCapacity
        {
            get
            {
                if (disposed) return "";
                var sizes = new string[] { "B", "KB", "MB", "GB", "TB" };
                var temp = (double)Capacity;
                int i = 0;
                while (temp > 1024.0)
                {
                    temp /= 1024.0;
                    i++;
                }
                return $"{Math.Round(temp, 2)} {sizes[i]}";
            }
        }

        public bool ReadAccount(int index, out API.XOnline.ONLINE_USER_ACCOUNT_STRUCT account)
        {
            account = default(API.XOnline.ONLINE_USER_ACCOUNT_STRUCT);
            int max = MaxAccounts;
            if (max == 0 || (index > (max - 1))) return false;
            //no need to seek or read/write on sector bounds with a file, we can go directly to the offset we want and read/write exactly what's needed
            byte[] buffer = new byte[0x6C];
            try
            {
                stream.Seek((0x50 + (index * 0x6C)), SeekOrigin.Begin);
                if (stream.Read(buffer, 0, 0x6C) == 0x6C)
                {
                    account = buffer.Deserialize<API.XOnline.ONLINE_USER_ACCOUNT_STRUCT>();
                    return API.XOnline.VerifyOnlineUserSignature(account);
                }
            }
            catch { }
            return false;
        }

        public bool WriteAccount(int index, API.XOnline.ONLINE_USER_ACCOUNT_STRUCT account)
        {
            int max = MaxAccounts;
            if (max == 0 || (index > (max - 1))) return false;
            if (!API.XOnline.SignOnlineUserSignature(ref account)) return false;
            byte[] buffer = account.Serialize();
            try
            {
                stream.Seek((0x50 + (index * 0x6C)), SeekOrigin.Begin);
                stream.Write(buffer, 0, 0x6C);
                stream.Flush();
            }
            catch { return false; }
            return true;
        }

        public bool DeleteAccount(int index)
        {
            int max = MaxAccounts;
            if (max == 0 || (index > (max - 1))) return false;

            byte[] buffer = new byte[]
            {
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
            };

            try
            {
                stream.Seek((0x50 + (index * 0x6C)), SeekOrigin.Begin);
                stream.Write(buffer, 0, 0x6C);
                stream.Flush();
            }
            catch { return false; }
            return true;
        }

    }

}
