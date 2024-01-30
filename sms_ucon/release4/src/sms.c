/*
  SMS plug-in for RomCenter (http://www.romcenter.com)
  Copyright (c) 2005 dbjh

  This library is free software; you can redistribute it and/or
  modify it under the terms of the GNU Lesser General Public
  License as published by the Free Software Foundation; either
  version 2.1 of the License, or (at your option) any later version.

  This library is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
  Lesser General Public License for more details.

  You should have received a copy of the GNU Lesser General Public
  License along with this library; if not, write to the Free Software
  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/
#include <stdio.h>
#include <stdlib.h>
#include <ctype.h>
#include "misc.h"
#include "ucon64.h"
#include "sms.h"


#define SMD_HEADER_LEN 512
#define SMS_HEADER_START 0x7ff0
#define SMS_HEADER_LEN (sizeof (st_sms_header_t))

typedef struct st_sms_header
{
  char signature[8];                            // "TMR "{"SEGA", "ALVS", "SMSC"}/"TMG SEGA"
  unsigned char pad[2];                         // 8
  unsigned char checksum_low;                   // 10
  unsigned char checksum_high;                  // 11
  unsigned char partno_low;                     // 12
  unsigned char partno_high;                    // 13
  unsigned char version;                        // 14
  unsigned char checksum_range;                 // 15, and country info
} st_sms_header_t;

static st_sms_header_t sms_header;
static sms_file_t type;


sms_file_t
sms_get_file_type (void)
{
  return type;
}


static void
smd_deinterleave (unsigned char *buffer, int size)
{
  int count, offset;
  unsigned char block[16384];

  for (count = 0; count < size / 16384; count++)
    {
      memcpy (block, &buffer[count * 16384], 16384);
      for (offset = 0; offset < 8192; offset++)
        {
          buffer[(count * 16384) + (offset << 1)] = block[offset + 8192];
          buffer[(count * 16384) + (offset << 1) + 1] = block[offset];
        }
    }
}


static int
sms_testinterleaved (st_rominfo_t *rominfo)
{
  unsigned char buf[0x4000] = { 0 };

  ucon64_fread (buf, rominfo->buheader_len + 0x4000, // header in 2nd 16 kB block
    0x2000 + (SMS_HEADER_START - 0x4000 + 8) / 2, ucon64.rom);
  if (!(memcmp (buf + SMS_HEADER_START - 0x4000, "TMR SEGA", 8) &&
        memcmp (buf + SMS_HEADER_START - 0x4000, "TMR ALVS", 8) && // SMS
        memcmp (buf + SMS_HEADER_START - 0x4000, "TMR SMSC", 8) && // SMS (unofficial)
        memcmp (buf + SMS_HEADER_START - 0x4000, "TMG SEGA", 8)))  // GG
    return 0;

  smd_deinterleave (buf, 0x4000);
  if (!(memcmp (buf + SMS_HEADER_START - 0x4000, "TMR SEGA", 8) &&
        memcmp (buf + SMS_HEADER_START - 0x4000, "TMR ALVS", 8) &&
        memcmp (buf + SMS_HEADER_START - 0x4000, "TMR SMSC", 8) &&
        memcmp (buf + SMS_HEADER_START - 0x4000, "TMG SEGA", 8)))
    return 1;

  return 0;                                     // unknown, act as if it's not interleaved
}


#define SEARCHBUFSIZE (SMS_HEADER_START + 8 + 16 * 1024)
#define N_SEARCH_STR 4
static int
sms_header_len (void)
/*
  At first sight it seems reasonable to also determine whether the file is
  interleaved in this function. However, we run into a chicken-and-egg problem:
  in order to deinterleave the data we have to know the header length. And in
  order to determine the header length we have to know whether the file is
  interleaved :-) Of course we could assume the header has an even size, but it
  turns out that that is not always the case. For example, there is a copy of
  GG Shinobi (E) [b1] floating around with a "header" of 5 bytes.
  In short: this function works only for files that are not interleaved.
*/
{
  // first two hacks for Majesco Game Gear BIOS (U) [!]
  if (ucon64.file_size == 1024)
    return 0;
  else if (ucon64.file_size == 1024 + SMD_HEADER_LEN)
    return SMD_HEADER_LEN;
  else
    {
      char buffer[SEARCHBUFSIZE] = { 0 }, *ptr, *ptr2 = NULL,
           search_str[N_SEARCH_STR][9] = { "TMR SEGA", "TMR ALVS", "TMR SMSC",
             "TMG SEGA" };
      int n;

      ucon64_fread (buffer, 0, SEARCHBUFSIZE, ucon64.rom);

      for (n = 0; n < N_SEARCH_STR; n++)
        {
          ptr = buffer;
          /*
            A few games contain several copies of the identification string
            (Alien 3 (UE) [!] (2 copies), Back to the Future 3 (UE) [!] (2
            copies), Sonic Spinball (UE) [!] (7 copies)). The "correct" one is
            the last where the corresponding check sum bytes are non-zero...
            However, finding *a* occurence is more important than the check sum
            bytes being non-zero.
          */
          while ((ptr = (char *) memmem2 (ptr, SEARCHBUFSIZE - (ptr - buffer),
                   search_str[n], 8)) != NULL)
            {
              if (!ptr2 ||
                  (ptr - buffer >= 12 && ptr[10] != 0 && ptr[11] != 0))
                ptr2 = ptr;
              ptr++;
            }
          if (ptr2)
            {
              n = ptr2 - buffer - SMS_HEADER_START;
              return n < 0 ? 0 : n;
            }
        }

      n = ucon64.file_size % (16 * 1024);       // SMD_HEADER_LEN
      if (ucon64.file_size > n)
        return n;
      else
        return 0;
    }
}
#undef SEARCHBUFSIZE
#undef N_SEARCH_STR


int
sms_init (st_rominfo_t *rominfo, char **comment)
{
  int result = -1, x;
  unsigned char buf[16384] = { 0 }, *rom_buffer;

  memset (&sms_header, 0, SMS_HEADER_LEN);

  if (UCON64_ISSET (ucon64.buheader_len))       // -hd, -nhd or -hdn option was specified
    rominfo->buheader_len = ucon64.buheader_len;
  else
    rominfo->buheader_len = sms_header_len ();

  rominfo->interleaved = UCON64_ISSET (ucon64.interleaved) ?
    ucon64.interleaved : sms_testinterleaved (rominfo);

  if (rominfo->interleaved)
    {
      type = SMD_SMS;                           // default to SMS
      ucon64_fread (buf, rominfo->buheader_len + 0x4000, // header in 2nd 16 kB block
        0x2000 + (SMS_HEADER_START - 0x4000 + SMS_HEADER_LEN) / 2, ucon64.rom);
      smd_deinterleave (buf, 0x4000);
      memcpy (&sms_header, buf + SMS_HEADER_START - 0x4000, SMS_HEADER_LEN);
    }
  else
    {
      type = MGD_SMS;                           // default to SMS
      ucon64_fread (&sms_header, rominfo->buheader_len + SMS_HEADER_START,
        SMS_HEADER_LEN, ucon64.rom);
    }

  rominfo->header_len = SMS_HEADER_LEN;

  ucon64_fread (buf, 0, 11, ucon64.rom);
  // Note that the identification bytes are the same as for Genesis SMD files
  if ((buf[8] == 0xaa && buf[9] == 0xbb && buf[10] == 6) ||
      !(memcmp (sms_header.signature, "TMR SEGA", 8) &&  // SMS or GG
        memcmp (sms_header.signature, "TMR ALVS", 8) &&  // SMS
        memcmp (sms_header.signature, "TMR SMSC", 8) &&  // SMS (unofficial)
        memcmp (sms_header.signature, "TMG SEGA", 8)) || // GG
      ucon64.console == UCON64_SMS)
    result = 0;
  else
    result = -1;

  x = sms_header.checksum_range & 0xf0;
  if (x == 0x50 || x == 0x60 || x == 0x70)      // GG file?
    type++;                                     // this works only because of how
                                                //  sms_file_t is initialised
  if (result == 0)
    {
      int size = ucon64.file_size - rominfo->buheader_len;
      if (!(rom_buffer = (unsigned char *) malloc (size)))
        {
          static char msg[100];
    
          sprintf (msg, "ERROR: Not enough memory for ROM buffer (%d bytes)\n", size);
          *comment = msg;
          return -1;
        }
      ucon64_fread (rom_buffer, rominfo->buheader_len, size, ucon64.rom);

      if (rominfo->interleaved)
        smd_deinterleave (rom_buffer, size);
      ucon64.crc32 = crc32 (0, rom_buffer, size);

      free (rom_buffer);
    }

  return result;
}
