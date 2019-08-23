using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace API
{

    public static class XOnline
    {

        public static bool SignOnlineUserSignature(ref ONLINE_USER_ACCOUNT_STRUCT Account)
        {
            byte[] account = Account.Serialize(); //struct to byte array, prefer serialization over C#'s 'unsafe' functionality even if slightly slower

            byte[] seed_data = { 0xA7, 0x14, 0x21, 0x3D, 0x94, 0x46, 0x1E, 0x05, 0x97, 0x6D, 0xE8, 0x35, 0x21, 0x2A, 0xE5, 0x7C };
            byte[] seed_key_a = { 0x2B, 0xB8, 0xD9, 0xEF, 0xD2, 0x04, 0x6D, 0x9D, 0x1F, 0x39, 0xB1, 0x5B, 0x46, 0x58, 0x01, 0xD7 };
            byte[] seed_key_b = { 0x1E, 0x05, 0xD7, 0x3A, 0xA4, 0x20, 0x6A, 0x7B, 0xA0, 0x5B, 0xCD, 0xDF, 0xAD, 0x26, 0xD3, 0xDE };
            byte[] auth_key = { 0x62, 0xBD, 0x92, 0xB6, 0x4F, 0x45, 0x84, 0x70, 0xD3, 0xFF, 0x4F, 0x22, 0x3C, 0x6E, 0xE7, 0xEA };
            byte[] IV = { 0x7B, 0x35, 0xA8, 0xB7, 0x27, 0xED, 0x43, 0x7A };

            //generate tripledes key (3DES)
            //NOTE: for a "machine signed" account, replace seed_data with XboxHDKey (machine signed will make the account 'non-roamable')
            byte[] tempHash;
            byte[] Key = new byte[0x18];

            //store first 4 bytes of resulting hash
            Kernel.XCryptHMAC(seed_key_a, seed_data, 0, 0x10, out tempHash);
            Array.Copy(tempHash, 0, Key, 0, 4);

            //store entire hash result, beginning from 0x4 in the key
            Kernel.XCryptHMAC(seed_key_b, seed_data, 0, 0x10, out tempHash);
            Array.Copy(tempHash, 0, Key, 0x4, 0x14);

            /*
              possible to use pre-computed key if not 'machine signing' to save on cpu cycles
              0x2B, 0x84, 0x95, 0xE8, 0x82, 0xE2, 0xA3, 0x33, 0x30, 0x60, 0x6D, 0x8A, 0xDA, 0x8B, 0x26, 0x93, 0x4E, 0x3A, 0x9D, 0xF6, 0xF5, 0xB8, 0xFA


              below is the resulting 3DES key layout // Ax = seed_key_a hash result bytes // Bx = seed_key_b hash result bytes
              0  1  2  3  4  5  6  7  8  9  10  11  12  13  14  15  16  17  19  20  21  22  23  24  // INDEX
              A0  A1  A2  A3 B0 B1 B2 B3 B4 B5  B6  B7  B8  B9  B10 B11 B12 B13 B14 B15 B16 B17 B18 // 3DES KEY LAYOUT
            */

            //compute hash of first 0x64 (100) bytes of account data using auth_key, covering all account variables and the confounder
            //NOTE: for a "machine signed" account, replace auth_key with XboxHDKey
            byte[] auth_hash;
            Kernel.XCryptHMAC(auth_key, account, 0, 0x64, out auth_hash);

            //encrypt the first 0x10 bytes of confounder (confounder is 0x14 bytes total) in the account data
            if (Kernel.XCryptBlockCryptCBC(Key, IV, ref account, 0x50, 0x10, true))
            {

                //store first 8 bytes of the resulting hash computed over the account data and decrypted confounder, used for authenticating the account
                Array.Copy(auth_hash, 0, account, 0x64, 8);

                //move working buffer back into struct
                Account = account.Deserialize<ONLINE_USER_ACCOUNT_STRUCT>();

                return true;
            }

            return false;
        }

        public static bool VerifyOnlineUserSignature(ONLINE_USER_ACCOUNT_STRUCT Account)
        {
            byte[] account = Account.Serialize(); //struct to byte array, prefer serialization over C#'s 'unsafe' functionality even if slightly slower

            byte[] seed_data = { 0xA7, 0x14, 0x21, 0x3D, 0x94, 0x46, 0x1E, 0x05, 0x97, 0x6D, 0xE8, 0x35, 0x21, 0x2A, 0xE5, 0x7C };
            byte[] seed_key_a = { 0x2B, 0xB8, 0xD9, 0xEF, 0xD2, 0x04, 0x6D, 0x9D, 0x1F, 0x39, 0xB1, 0x5B, 0x46, 0x58, 0x01, 0xD7 };
            byte[] seed_key_b = { 0x1E, 0x05, 0xD7, 0x3A, 0xA4, 0x20, 0x6A, 0x7B, 0xA0, 0x5B, 0xCD, 0xDF, 0xAD, 0x26, 0xD3, 0xDE };
            byte[] auth_key = { 0x62, 0xBD, 0x92, 0xB6, 0x4F, 0x45, 0x84, 0x70, 0xD3, 0xFF, 0x4F, 0x22, 0x3C, 0x6E, 0xE7, 0xEA };
            byte[] IV = { 0x7B, 0x35, 0xA8, 0xB7, 0x27, 0xED, 0x43, 0x7A };

            //generate tripledes key (3DES)
            //NOTE: for a "machine signed" account, replace seed_data with XboxHDKey (machine signed will make the account 'non-roamable')
            byte[] tempHash;
            byte[] Key = new byte[0x18];

            //store first 4 bytes of resulting hash
            Kernel.XCryptHMAC(seed_key_a, seed_data, 0, 0x10, out tempHash);
            Array.Copy(tempHash, 0, Key, 0, 4);

            //store entire hash result, beginning from 0x4 in the key
            Kernel.XCryptHMAC(seed_key_b, seed_data, 0, 0x10, out tempHash);
            Array.Copy(tempHash, 0, Key, 0x4, 0x14);

            /*
              possible to use pre-computed key if not 'machine signing' to save on cpu cycles
              0x2B, 0x84, 0x95, 0xE8, 0x82, 0xE2, 0xA3, 0x33, 0x30, 0x60, 0x6D, 0x8A, 0xDA, 0x8B, 0x26, 0x93, 0x4E, 0x3A, 0x9D, 0xF6, 0xF5, 0xB8, 0xFA


              below is the resulting 3DES key layout // Ax = seed_key_a hash result bytes // Bx = seed_key_b hash result bytes
              0  1  2  3  4  5  6  7  8  9  10  11  12  13  14  15  16  17  19  20  21  22  23  24  // INDEX
              A0  A1  A2  A3 B0 B1 B2 B3 B4 B5  B6  B7  B8  B9  B10 B11 B12 B13 B14 B15 B16 B17 B18 // 3DES KEY LAYOUT
            */

            //decrypt the first 0x10 bytes of confounder (confounder is 0x14 bytes total) in the account data
            if (Kernel.XCryptBlockCryptCBC(Key, IV, ref account, 0x50, 0x10, false))
            {
                //compute hash of first 0x64 (100) bytes of account data using auth_key, covering all account variables and the confounder
                //NOTE: for a "machine signed" account, replace auth_key with XboxHDKey
                byte[] auth_hash;
                Kernel.XCryptHMAC(auth_key, account, 0, 0x64, out auth_hash);

                //compare first 8 bytes of the resulting auth_hash to the 8 stored bytes of the validation bytes (aka the 8 bytes of the last auth_hash)
                if (memcmp(auth_hash, 0, account, 0x64, 8))
                {
                    //other validation checks, these checks are done by the XOnline API and must comply to be considered valid account data

                    //check flags, last 4 bits are resevered and must be zero
                    if ((account[0x1F] & 0xF) == 0)
                    {
                        //check unknown int value stored at 0x8, must be zero (reserved for PARTNER.NET accounts?)
                        if (BitConverter.ToUInt32(account, 8) == 0)
                        {
                            //check gamertag for overflow and is null-terminated
                            if (account[0x1B] == 0)
                            {
                                //check domain for overflow and is null-terminated
                                if (account[0x37] == 0)
                                {
                                    //check realm for overflow and is null-terminated
                                    if (account[0x4F] == 0)
                                    {
                                        return true; //valid account
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        //because C#
        private static bool memcmp(byte[] a, int aIndex, byte[] b, int bIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (a[aIndex + i] != b[bIndex + i]) return false;
            }
            return true;
        }

        [StructLayout(LayoutKind.Sequential, Size = 0x6C, Pack = 1)]
        public struct ONLINE_USER_ACCOUNT_STRUCT
        {
            [MarshalAs(UnmanagedType.U8)]
            public ulong XUID;

            [MarshalAs(UnmanagedType.U4)]
            public uint unknown; //reserved (assume PARTNER.NET accounts), verification fails if this is non-zero

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10, ArraySubType = UnmanagedType.U1)]
            public char[] Gamertag; //must null term, verification fails if last byte not zero

            [MarshalAs(UnmanagedType.U4)]
            public uint Flags; //last 4 bits reserved, verification fails if they're set

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4, ArraySubType = UnmanagedType.U1)]
            public byte[] Passcode;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14, ArraySubType = UnmanagedType.ByValTStr)]
            public char[] Domain; //must null term, verification fails if last byte not zero

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x18, ArraySubType = UnmanagedType.ByValTStr)]
            public char[] Realm; //must null term, verification fails if last byte not zero

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14, ArraySubType = UnmanagedType.U1)]
            public byte[] Confounder;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8, ArraySubType = UnmanagedType.U1)]
            public byte[] Verification;
        }

    }

    public static class Kernel
    {

        public static void XCryptHMAC(byte[] Key, byte[] Data, int Index, int Count, out byte[] Hash)
        {
            using (var HMAC = new HMACSHA1(Key))
            {
                Hash = HMAC.ComputeHash(Data, Index, Count);
            }
        }

        public static bool XCryptBlockCryptCBC(byte[] Key, byte[] IV, ref byte[] Data, int Index, int Count, bool Encrypt)
        {
            try
            {
                using (var Crypto = new TripleDESCryptoServiceProvider())
                {
                    Crypto.BlockSize = 64;
                    Crypto.Padding = PaddingMode.None;
                    Crypto.Mode = CipherMode.CBC;
                    using (var Transform = (Encrypt ? Crypto.CreateEncryptor(Key, IV) : Crypto.CreateDecryptor(Key, IV)))
                    {
                        byte[] tempCrypt = Transform.TransformFinalBlock(Data, Index, Count);
                        Array.Copy(tempCrypt, 0, Data, Index, Count);
                    }
                }
                return true;
            }
            catch { return false; }
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

    }

}
