I'll start off by saying that although you can use this information to create Xbox LIVE accounts for the *original* Xbox that are considered valid, you cannot sign-in to the Xbox LIVE service. The service was taken offline April 15th, 2010. However, there are some games that require you to have a Xbox LIVE account to play them, even if you cannot sign-in (such as Phantasy Star Online). You *cannot* use a valid Xbox LIVE account created for the *original* Xbox on the Xbox 360 or the Xbox One.

This project was something I had an interest in simply because it was one of the few things left on the *original* Xbox that had not been reverse engineered yet, content-wise at least.


#### Where the account data is stored
Account data is stored in the first 1-2 sectors on a harddrive or memory card, within the FATX header. 1 sector for memory cards and up to 2 sectors for harddrives. This is because the XOnline API has a hardcoded limit for how many accounts it will read/write to a specific device type. A memory card is capable of storing just as many as a harddrive but is intentionally limited. If you were to store 2 accounts on a memory card, the XOnline API would only recognize the first account. Below is how the account data is stored on devices

### FATX Header

| Name | Offset | Length | Type | Comment |
| - | - | - | - | - |
| Magic | 0x0 | 0x4 | uint32 | Magic value to identify the device volume type. 4 ASCII characters "FATX" or 0x58544146 uint32 |
| Volume ID | 0x4 | 0x4 | uint32 | Unique identifier for the volume. This is a randomly generated value and is used to differentiate between the potentially various mounted volumes on the Xbox |
| Cluster Size | 0x8 | 0x4 | uint32 | Cluster size in 0x200 byte sectors (Cluster Size = 0x200 x value). This value is unused, cluster size is hardcoded as 32 sectors on harddrives (0x200 x 32 = 0x4000) and 16 sectors on memory cards (0x200 x 16 = 0x2000). Value is always 4 |
| Number of active FAT's | 0xC | 0x4 | uint32 | Number of File Allocation Tables in use. Value is always 1 |
| Volume Name | 0x10 | 0x40 | w_char[] | Friendly name of the device shown in the dashboard. 31 unicode characters + 1 null terminator |
| Xbox LIVE Account 0 | 0x50 | 0x6C | byte[] | 1st account slot (only account slot on memory cards) |
| Xbox LIVE Account 1 | 0xBC | 0x6C | byte[] | 2nd account slot (harddrive only) |
| Xbox LIVE Account 2 | 0x128 | 0x6C | byte[] | 3rd account slot (harddrive only) |
| Xbox LIVE Account 3 | 0x194 | 0x6C | byte[] | 4th account slot (harddrive only) |
| Xbox LIVE Account 4 | 0x200 | 0x6C | byte[] | 5th account slot (harddrive only) |
| Xbox LIVE Account 5 | 0x26C | 0x6C | byte[] | 6th account slot (harddrive only) |
| Xbox LIVE Account 6 | 0x2D8 | 0x6C | byte[] | 7th account slot (harddrive only) |
| Xbox LIVE Account 7 | 0x344 | 0x6C | byte[] | 8th account slot (harddrive only) |
> *Memory cards can store a maximum of 1 account and harddrives can store a maximum of 8 accounts*


#### How account data is signed and validated

Account data is signed/validated with a combination of HMAC-SHA-1 hashing and TripleDES encryption. Both the hashing and encryption  algorithms use hardcoded keys, these keys are stored in the code itself (hense hardcoded) rather than the EEPROM where most people would assume them to be (the XOnline API supports signing using the XboxHDKey, which *is* stored in the EEPROM and would essentially create a "console locked" account but this is seemly unused - for retail consoles at least - perhaps this was for testing purposes or PARTNER.NET developer accounts, I am unsure)

Signing the account begins with running HMAC-SHA-1 over the first 0x64 bytes of the account data, the first 0x10 bytes of the confounder are then encrypted with TripleDES, and finally the first 0x8 bytes of the resulting hash from the HMAC-SHA-1 are appended to the end of the data.

Validation is just the method of signing in reverse. The confounder is decrypted, a HMAC-SHA-1 is ran over the first 0x64 bytes of data, and then the first 0x8 bytes of the resulting hash are compared against the last 0x8 bytes of the account data.

There are also a few more validation checks that are done that also must be valid for the account data to be considered an authentic account: The reserved value is checked, it must be zero. The gamertag string must be null terminated. The flags value is checked, the last 4 bits must be unset. The kerberos domain and realm strings must be null terminated.

You could potentially create an account that is completely null (all zeroes) and as long as the signature is valid then the account data is considered valid as well.


### Xbox LIVE Account

| Name | Offset | Length | Type | Comment |
| - | - | - | - | - |
| XUID | 0x0 | 0x8 | uint64 | Unique identifier for the account |
| Reserved | 0x8 | 0x4 | uint32 | Unknown. XOnline expects this to be zero, verification fails if it's non-zero |
| Gamertag | 0xC | 0x10 | char[] | Account gamertag. 15 ASCII characters + 1 null terminator. XOnline expects null-term, validation fails if it's not |
| Flags | 0x1C | 0x4 | BitFlags | If bit 0 is set then passcode is enabled. The rest are unknown/unused. Bits 28/29/30/31 are reserved and validation fails if they're set |
| Passcode | 0x20 | 0x4 | byte[] | 4 bytes representing a 4 button combination required to access the account. 1 button stored in each byte (see the table below for the button values) |
| Kerberos Domain | 0x24 | 0x14 | char[] | Server for account and content authentication. Used to login and everything else to do with the Xbox LIVE service. 19 ASCII characters + 1 null terminator. XOnline expects null-term, validation fails if it's not |
| Kerberos Realm | 0x38 | 0x18 | char[] | Same as above. 23 ASCII characters + 1 null terminator. XOnline expects null-term, validation fails if it's not |
| Confounder | 0x50 | 0x14 | byte[] | Random data, first 0x10 bytes are encrypted with TripleDES (3DES) CBC encryption |
| Signature | 0x64 | 0x8 | byte[] | The first 0x8 bytes of a HMAC SHA-1 hash that was computed over the account data before the confounder was encrypted. This is the account signature and is the main validation check |

#### Passcode Button Values

| Button | Value |
| - | - |
| D-Pad or Analog UP | 0x1 |
| D-Pad or Analog DOWN | 0x2 |
| D-Pad or Analog LEFT | 0x3 |
| D-Pad or Analog RIGHT | 0x4 |
| X Button | 0x5 |
| Y Button | 0x6 |
| Left Trigger | 0x9 |
| Right Trigger | 0xA |
> *A button, B button, and left analog stick are reserved during passcode prompts (right analog stick is still applicable)*

#### Creating a Xbox LIVE account for the *original* Xbox

Begin by downloading the [Xbox Account Manager](https://github.com/feudalnate/Original-Xbox-LIVE-Account/blob/master/XboxAccountManager/bin/Release/XboxAccountManager.exe) tool (in this repository) and connecting a FATX formatted harddrive or memory card to your computer. Open the Xbox Account Manager and you will be presented with this

![](https://i.imgur.com/d1yh0UP.png)

You may change any value you would like, however the **XUID** must be valid hexadecimal and 16 characters in length and the **Gamertag** must be at least 1 character in length. After making your changes, go into the **File** menu, select **Save**, and then select **Device..**

![](https://i.imgur.com/9ShzIAO.png)

A device selection window will open and you can choose which device you would like to write the account to. **Double click** on a device to select it or click on a device and press the **Open Device** button

![](https://i.imgur.com/ifDoF76.png)

Once you have selected a device, a final window will open and you can choose which account slot you would like to write the account data to. **Double click** on a slot to select it or click on a slot and press the **Save** button

![](https://i.imgur.com/0qjktLG.png)

After you have selected the slot, a dialog will prompt you for confirmation to commit the write to your device. Confirm everything is correct and you can continue with saving the account to your device

![](https://i.imgur.com/OoCGFJR.png)

When you're done you can disconnect your harddrive or memory card from your computer and plug it into your *original* Xbox

![](https://i.imgur.com/EVHOk3J.jpg)

![](https://i.imgur.com/lV7fxW6.jpg)

### Acknowledgements

Thank you to the users on AssemblerGames that had began researching this back in 2016, the information they posted was an excellent starting point. https://assemblergames.com/threads/xbox-live-accounts-xqemu-and-mu.60352/

Thank you to the user CodeAsm for providing not one but two valid Xbox LIVE accounts for reference, it was extremely helpful!
