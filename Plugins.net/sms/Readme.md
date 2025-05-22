### 📄 Summary: SMS ROM File Structure & Format Detection (IA generated)

**Sega Master System(SMS) ROMs** have a relatively simple structure and few formal headers. Here's a quick overview of how SMS ROMs are structured and how their format can be detected:

---

### 🧱 **SMS ROM File Structure**

* **No standard file header** like NES or SNES.
* ROM is a raw memory dump of the cartridge.
* Some ROMs (especially official games) include a signature string:

  * `"TMR SEGA"` typically found **16 bytes before the end of the ROM**.
  * Example offsets:

    *32 KB ROM → `0x7FF0`
    * 64 KB ROM → `0xBFF0`
    * 128 KB ROM → `0x1FFF0`
    * 256 KB ROM → `0x3FFF0`
* BIOS ROMs often **do not include this marker**.

---

### 🕵️ **How Format Detection Works**

To detect whether a ROM is an SMS file:

1. * *Check the file size**:

   *Acceptable range: **8 KB to 512 KB**
   * Smaller → `TooSmall`, Larger → `TooBig`

2. **Look for `"TMR SEGA"`** string at the typical offsets.

   * If found, the ROM is positively identified as SMS.

3. **Handle special cases like BIOS ROMs**:

   *BIOS dumps are often **8 KB or 16 KB**.
   * These files **may not contain** the `"TMR SEGA"` string.

4. **Fallback: CRC Matching**:

   *For BIOS files with no markers, detection relies on **known CRC checksums**.
   * Matching a ROM's CRC against a list of verified BIOS CRCs confirms it's an SMS BIOS.

---