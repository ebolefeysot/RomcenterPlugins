/*
  SNES plug-in for RomCenter (http://www.romcenter.com)
  Copyright (c) 2003, 2005 dbjh

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/
#include <stdio.h>
#include <stdlib.h>
#include <ctype.h>
#include "misc.h"
#include "ucon64.h"
#include "snes.h"


#define SNES_HEADER_START 0x7fb0
#define SNES_HIROM 0x8000
#define SNES_EROM 0x400000                      // "Extended" ROM, Hi or Lo
#define SWC_HEADER_LEN (sizeof (st_swc_header_t))
#define SNES_HEADER_LEN (sizeof (st_snes_header_t))
#define SNES_NAME_LEN 21
#define GD3_HEADER_MAPSIZE 0x18
#define NSRT_HEADER_VERSION 22                  // version 2.2 header
#define DETECT_NOTGOOD_DUMPS                    // makes _a_ complete GoodSNES 0.999.5 set detected
#define DETECT_SMC_COM_FUCKED_UP_LOROM          // adds support for interleaved LoROMs
#define DETECT_INSNEST_FUCKED_UP_LOROM          // only adds support for its 24 Mbit
                                                //  interleaved LoROM "format"

static int snes_deinterleave (st_rominfo_t *rominfo, unsigned char **rom_buffer,
                              int rom_size, char **comment);
static unsigned short int get_internal_sums (st_rominfo_t *rominfo);
static int snes_check_bs (void);
static int snes_isprint (char *s, int len);
static int check_banktype (unsigned char *rom_buffer, int header_offset);
static void get_nsrt_info (unsigned char *rom_buffer, int header_start,
                           unsigned char *buheader);

typedef struct st_swc_header
{
/*
  Don't create fields that are larger than one byte! For example size_low and size_high
  could be combined in one unsigned short int. However, this gives problems with little
  endian vs. big endian machines (e.g. writing the header to disk).
*/
  unsigned char size_low;
  unsigned char size_high;
  unsigned char emulation;
  unsigned char pad[5];
  unsigned char id1;
  unsigned char id2;
  unsigned char type;
  unsigned char pad2[501];
} st_swc_header_t;

typedef struct st_snes_header
{
  unsigned char maker_high;                     // 0
  unsigned char maker_low;                      // 1
  unsigned char game_id_prefix;                 // 2
  unsigned char game_id_low;                    // 3
  unsigned char game_id_high;                   // 4
  unsigned char game_id_country;                // 5
  // 'E' = USA, 'F' = France, 'G' = Germany, 'J' = Japan, 'P' = Europe, 'S' = Spain
  unsigned char pad1[7];                        // 6
  unsigned char sfx_sram_size;                  // 13
  unsigned char pad2[2];                        // 14
  unsigned char name[SNES_NAME_LEN];            // 16
  unsigned char map_type;                       // 37, a.k.a. ROM makeup
  unsigned char rom_type;                       // 38
#define bs_month rom_type                       // release date, month
  unsigned char rom_size;                       // 39
#define bs_day rom_size                         // release date, day
  unsigned char sram_size;                      // 40
#define bs_map_type sram_size
  unsigned char country;                        // 41
#define bs_type country
  unsigned char maker;                          // 42
  unsigned char version;                        // 43
  /*
    If we combine the following 4 bytes in 2 short int variables,
    inverse_checksum and checksum, they will have an incorrect value on big
    endian machines.
  */
  unsigned char inverse_checksum_low;           // 44
  unsigned char inverse_checksum_high;          // 45
  unsigned char checksum_low;                   // 46
  unsigned char checksum_high;                  // 47
} st_snes_header_t;

static st_snes_header_t snes_header;
static int snes_sramsize, snes_header_base, snes_hirom, snes_hirom_ok,
           nsrt_header, bs_dump, st_dump;
static snes_file_t type;


snes_file_t
snes_get_file_type (void)
{
  return type;
}


static int
snes_testinterleaved (unsigned char *rom_buffer, int size, int banktype_score)
/*
  The only way to determine whether a HiROM dump is interleaved or not seems to
  be to check the value of the map type byte. Valid HiROM values (hexadecimal):
  21, 31, 35, 3a
  Valid LoROM values:
  20, 23, 30, 32, 44 [, 41, 53]
  41 is the hexadecimal value of 'A' (WWF Super Wrestlemania (E)). 53 is the
  hexadecimal value value of 'S' (Contra III - The Alien Wars (U)).
  So, if a ROM dump seems LoROM, but the map type byte is that of a HiROM dump
  we assume it is interleaved. Interleaved LoROM dumps are not produced by any
  copier, but by incorrect ROM tools...
*/
{
  int interleaved = 0, check_map_type = 1;
  unsigned int crc;

  if (size < 64 * 1024)                         // snes_deinterleave() reads blocks of 32 kB
    return 0;                                   // file cannot be interleaved
    
  crc = crc32 (0, rom_buffer, 512);
  /*
    Special case hell

    0x4a70ad38: Double Dragon, Return of (J), Super Double Dragon (E/U) {[!], [a1]}
    0x0b34ddad: Kakinoki Shogi (J)
    0x348b5357: King of Rally, The (J)
    0xc39b8d3a: Pro Kishi Simulation Kishi no Hanamichi (J)
    0xbd7bc39f: Shin Syogi Club (J)
    0x9b4638d0: Street Fighter Alpha 2 (E/U) {[b1]}, Street Fighter Zero 2 (J)
    Only really necessary for (U). The other versions can be detected because
    one of the two internal headers has checksum bytes ff ff 00 00.
    0x0085b742: Super Bowling (U)
    0x30cbf83c: Super Bowling (J)
    These games have two headers.

    BUG ALERT: We don't check for 0xbd7bc39f. The first 512 bytes of what
    uCON64 detects as the interleaved dump of Shin Syogi Club (J) are identical
    to the first 512 bytes of what we detect as the uninterleaved dump of
    Kakinoki Shogi (J). We prefer uninterleaved dumps. Besides, concluding a
    dump is interleaved if the first 512 bytes have CRC32 0xbd7bc39f would mess
    up the detection of some BS dumps. See below.

    0x7039388a: Ys 3 - Wanderers from Ys (J)
    This game has 31 internal headers...

    0xd7470b37/0x9f1d6284: Dai Kaiju Monogatari 2 (J) (GD3/UFO)
    0xa2c5fd29/0xfe536fc9: Tales of Phantasia (J) (GD3/UFO)
    These are Extended HiROM games. By "coincidence" ToP can be detected in
    another way, but DKM2 (40 Mbit) can't. The CRC32's are checked for below.

    0xdbc88ebf: BS Satella2 1 (J)
    This game has a LoROM map type byte while it is a HiROM game.

    0x29226b62: BS Busters - Digital Magazine 5-24-98 (J),
                BS Do-Re-Mi No.2 5-10 (J),
                BS Do-Re-Mi No.2 5-25 (J),
                BS Furoito No Chousenjou {2, 3, 4, 5, 6} (J),
                BS Nintendo HP 5-17 (J),
                BS Nintendo HP 5-31 (J)
    0xbd7bc39f: BS Goods Press 6 Gatsu Gou (J),
                BS NP Magazine 107 (J),
                BS Tora no Maki 5-17 (J),
                BS Tora no Maki 5-31 (J)
    0x4ef3d27b: BS Lord Monarke (J)
    These games are *not* special cases. uCON64 detects them correctly, but the
    tool that was used to create GoodSNES - 0.999.5 for RC 2.5.dat, does not.
    This has been verified on a real SNES for the games with CRC 0x29226b62 and
    0x4ef3d27b. The games with CRC 0xbd7bc39f don't seem to run on a copier.

    0xc3194ad7: Yu Yu No Quiz De Go! Go! (J)
    0x89d09a77: Infernal's Evil Demo! (PD)
    0xd3095af3: Legend - SNDS Info, Incredible Hulk Walkthru (PD)
    0x9b161d4d: Pop 'N Twinbee Sample (J)
    0x6910700a: Rock Fall (PD)
    0x447df9d5: SM Choukyousi Hitomi (PD)
    0x02f401df: SM Choukyousi Hitomi Vol 2 (PD)
    0xf423997a: World of Manga 2 (PD)
    These games/dumps have a HiROM map type byte while they are LoROM.

    0x0f802e41: Mortal Kombat 3 Final (Anthrox Beta Hack)
    0xbd8f1b20: Rise of the Robots (Beta)
    0x05926d17: Shaq Fu (E)/(J)(NG-Dump Known)
    0x3e2e5619: Super Adventure Island II (Beta)
    0x023e1298: Super Air Driver (E) [b]
    These are also not special cases (not: HiROM map type byte + LoROM game).
    GoodSNES - 0.999.5 for RC 2.5.dat simply contains errors.

    0x2a4c6a9b: Super Noah's Ark 3D (U)
    0xfa83b519: Mortal Kombat (Beta)
    0xf3aa1eca: Power Piggs of the Dark Age (Pre-Release) {[h1]}
    0x65485afb: Super Aleste (J) {[t1]} <= header == trainer
    0xaad23842/0x5ee74558: Super Wild Card DX DOS ROM V1.122/interleaved
    0x422c95c4: Time Slip (Beta)
    0x7a44bd18: Total Football (E)(NG-Dump Known)
    0xf0bf8d7c/0x92180571: Utyu no Kishi Tekkaman Blade (Beta) {[h1]}/interleaved
    0x8e1933d0: Wesley Orangee Hotel (PD)
    0xe2b95725/0x9ca5ed58: Zool (Sample Cart)/interleaved
    These games/dumps have garbage in their header.
  */
  if (crc == 0xc3194ad7
#ifdef  DETECT_NOTGOOD_DUMPS
      ||
      crc == 0x89d09a77 || crc == 0xd3095af3 || crc == 0x9b161d4d ||
      crc == 0x6910700a || crc == 0x447df9d5 || crc == 0x02f401df ||
      crc == 0xf423997a || crc == 0xfa83b519 || crc == 0xf3aa1eca ||
      crc == 0xaad23842 || crc == 0x422c95c4 || crc == 0x7a44bd18 ||
      crc == 0xf0bf8d7c || crc == 0x8e1933d0 || crc == 0xe2b95725
#endif
     )
    check_map_type = 0;                         // not interleaved
  else if (crc == 0x4a70ad38 || crc == 0x0b34ddad || crc == 0x348b5357 ||
           crc == 0xc39b8d3a || crc == 0x9b4638d0 || crc == 0x0085b742 ||
           crc == 0x30cbf83c || crc == 0x7039388a || crc == 0xdbc88ebf ||
           crc == 0x2a4c6a9b
#ifdef  DETECT_NOTGOOD_DUMPS
           ||
           crc == 0x65485afb || crc == 0x5ee74558 || crc == 0x92180571 ||
           crc == 0x9ca5ed58
#endif
          )
    {
      interleaved = 1;
      snes_hirom = 0;
      snes_hirom_ok = 1;
      check_map_type = 0;                       // interleaved
    }
  // WARNING: st_dump won't be set if it's an interleaved dump
  else if (st_dump)
    check_map_type = 0;
  else
    {
#ifdef  DETECT_SMC_COM_FUCKED_UP_LOROM
      if (size > SNES_HEADER_START + SNES_HIROM + 0x4d)
        if (check_banktype (rom_buffer, size / 2) > banktype_score)
          {
            interleaved = 1;
            snes_hirom = 0;
            snes_hirom_ok = 1;                  // keep snes_deinterleave()
            check_map_type = 0;                 //  from changing snes_hirom
          }
#endif
#ifdef  DETECT_INSNEST_FUCKED_UP_LOROM
      /*
        "the most advanced and researched Super Nintendo ROM utility available"
        What a joke. They don't support their own "format"...
        For some games we never reach this code, because the previous code
        detects them (incorrectly). I (dbjh) don't think there are games in
        this format available on the internet, so I won't add special-case code
        (like CRC32 checks) to fix that -- it's a bug in inSNESt. Examples are:
        Lufia II - Rise of the Sinistrals (H)
        Super Mario All-Stars & World (E) [!]
      */
      if (!interleaved && size == 24 * MBIT)
        if (check_banktype (rom_buffer, 16 * MBIT) > banktype_score)
          {
            interleaved = 1;
            snes_hirom = 0;
            snes_hirom_ok = 2;                  // fix for snes_deinterleave()
            check_map_type = 0;
          }
#endif
    }
  if (check_map_type && !snes_hirom)
    {
      // first check if it's an interleaved Extended HiROM dump
      if (ucon64.file_size >= (int) (SNES_HEADER_START + SNES_EROM + SNES_HEADER_LEN))
        {
          // don't set snes_header_base to SNES_EROM for too small files (split files)
          if (crc == 0xd7470b37 || crc == 0xa2c5fd29) // GD3
            snes_header_base = SNES_EROM;
          else if (crc == 0x9f1d6284 || crc == 0xfe536fc9) // UFO
            {
              snes_header_base = SNES_EROM;
              interleaved = 1;
            }
        }
      if (snes_header.map_type == 0x21 || snes_header.map_type == 0x31 ||
          snes_header.map_type == 0x35 || snes_header.map_type == 0x3a ||
          snes_header.bs_map_type == 0x21 || snes_header.bs_map_type == 0x31)
        interleaved = 1;
    }

  return interleaved;
}


int
snes_deinterleave (st_rominfo_t *rominfo, unsigned char **rom_buffer, int rom_size, char **comment)
{
  unsigned char blocks[256], *rom_buffer2;
  int nblocks, i, j, org_hirom;

  org_hirom = snes_hirom;
  nblocks = rom_size >> 16;                     // # 32 kB blocks / 2
  if (nblocks * 2 > 256)
    return -1;                                  // file > 8 MB

  if (rominfo->interleaved == 2)                // SFX(2) games (Doom, Yoshi's Island)
    {
      for (i = 0; i < nblocks * 2; i++)
        {
          blocks[i] = (i & ~0x1e) | ((i & 2) << 2) | ((i & 4) << 2) |
                      ((i & 8) >> 2) | ((i & 16) >> 2);
          if (blocks[i] * 0x8000 + 0x8000 > rom_size)
            {
              static char msg[100];
        
              sprintf (msg, "WARNING: This ROM cannot be handled as if it is in interleaved format 2\n");
              *comment = msg;
              rominfo->interleaved = 0;
              return -1;
            }
        }
    }
  else // rominfo->interleaved == 1
    {
      int blocksset = 0;

      if (!snes_hirom_ok)
        {
          snes_hirom = SNES_HIROM;
          snes_hirom_ok = 1;
        }

      if (type == GD3)
        {
          // deinterleaving schemes specific for the Game Doctor
          if ((snes_hirom || snes_hirom_ok == 2) && rom_size == 24 * MBIT)
            {
              for (i = 0; i < nblocks; i++)
                {
                  blocks[i * 2] = i + ((i < (16 * MBIT >> 16) ? 16 : 4) * MBIT >> 15);
                  blocks[i * 2 + 1] = i;
                }
              blocksset = 1;
            }
          else if (snes_header_base == SNES_EROM)
            {
              int size2 = rom_size - 32 * MBIT; // size of second ROM
              j = 32 * MBIT >> 16;
              for (i = 0; i < j; i++)
                {
                  blocks[i * 2] = i + j + (size2 >> 15);
                  blocks[i * 2 + 1] = i + (size2 >> 15);
                }
              j = size2 >> 16;
              for (; i < j + (32 * MBIT >> 16); i++)
                {
                  blocks[i * 2] = (unsigned char) (i + j - (32 * MBIT >> 16));
                  blocks[i * 2 + 1] = (unsigned char) (i - (32 * MBIT >> 16));
                }
              blocksset = 1;
            }
        }
      if (!blocksset)
        for (i = 0; i < nblocks; i++)
          {
            blocks[i * 2] = i + nblocks;
            blocks[i * 2 + 1] = i;
          }
    }

  if (!(rom_buffer2 = (unsigned char *) malloc (rom_size)))
    {
      static char msg[100];

      sprintf (msg, ucon64_msg[ROM_BUFFER_ERROR], rom_size);
      *comment = msg;
      return -1; // uCON64 does "exit (1);"
    }
  for (i = 0; i < nblocks * 2; i++)
    memcpy (rom_buffer2 + i * 0x8000, (*rom_buffer) + blocks[i] * 0x8000, 0x8000);

  free (*rom_buffer);
  *rom_buffer = rom_buffer2;
  return 0;
}


unsigned short int
get_internal_sums (st_rominfo_t *rominfo)
/*
  Returns the sum of the internal checksum and the internal inverse checksum
  if the values for snes_hirom and rominfo->buheader_len are correct. If the
  values are correct the sum will be 0xffff. Note that the sum for bad ROM
  dumps can also be 0xffff, because this function adds the internal checksum
  bytes and doesn't do anything with the real, i.e. calculated, checksum.
*/
{
  int image = SNES_HEADER_START + snes_header_base + snes_hirom +
              rominfo->buheader_len;
  // don't use rominfo->header_start here!
  unsigned char buf[4];

  ucon64_fread (buf, image + 44, 4, ucon64.rom);
  return buf[0] + (buf[1] << 8) + buf[2] + (buf[3] << 8);
}


static void
snes_handle_buheader (st_rominfo_t *rominfo, st_unknown_header_t *header)
/*
  Determine the size of a possible backup unit header. This function also tries
  to determine the bank type in the process. However, snes_set_hirom() has the
  final word about that.
*/
{
  int x = 0, y;
  /*
    Check for "Extended" ROM dumps first, because at least one of them
    (Tales of Phantasia (J)) has two headers; an incorrect one at the normal
    location and a correct one at the Extended HiROM location.
  */
  if (ucon64.file_size >= (int) (SNES_HEADER_START + SNES_EROM + SNES_HEADER_LEN))
    {
      snes_header_base = SNES_EROM;
      snes_hirom = SNES_HIROM;
      rominfo->buheader_len = 0;
      if ((x = get_internal_sums (rominfo)) != 0xffff)
        {
          rominfo->buheader_len = SWC_HEADER_LEN;
          if ((x = get_internal_sums (rominfo)) != 0xffff)
            {
              snes_hirom = 0;
              if ((x = get_internal_sums (rominfo)) != 0xffff)
                {
                  rominfo->buheader_len = 0;
                  x = get_internal_sums (rominfo);
                }
            }
        }
    }
  if (x != 0xffff)
    {
      snes_header_base = 0;
      snes_hirom = 0;
      rominfo->buheader_len = 0;
      if ((x = get_internal_sums (rominfo)) != 0xffff)
        {
          rominfo->buheader_len = SWC_HEADER_LEN;
          if ((x = get_internal_sums (rominfo)) != 0xffff)
            {
              snes_hirom = SNES_HIROM;
              if ((x = get_internal_sums (rominfo)) != 0xffff)
                {
                  rominfo->buheader_len = 0;
                  x = get_internal_sums (rominfo);
                }
            }
        }
      }

  if (header->id1 == 0xaa && header->id2 == 0xbb && header->type == 4)
    type = SWC;
  else if (!strncmp ((char *) header, "GAME DOCTOR SF 3", 16))
    type = GD3;
  else if (!strncmp ((char *) header + 8, "SUPERUFO", 8))
    type = UFO;
  else if ((header->hirom == 0x80 &&            // HiROM
             ((header->emulation1 == 0x77 && header->emulation2 == 0x83) ||
              (header->emulation1 == 0xdd && header->emulation2 == 0x82) ||
              (header->emulation1 == 0xdd && header->emulation2 == 0x02) ||
              (header->emulation1 == 0xf7 && header->emulation2 == 0x83) ||
              (header->emulation1 == 0xfd && header->emulation2 == 0x82)))
            ||
           (header->hirom == 0x00 &&            // LoROM
             ((header->emulation1 == 0x77 && header->emulation2 == 0x83) ||
              (header->emulation1 == 0x00 && header->emulation2 == 0x80) ||
#if 1
              // This makes NES FFE ROMs & Game Boy ROMs be detected as SNES
              //  ROMs, see src/console/nes.c & src/console/gb.c
              (header->emulation1 == 0x00 && header->emulation2 == 0x00) ||
#endif
              (header->emulation1 == 0x47 && header->emulation2 == 0x83) ||
              (header->emulation1 == 0x11 && header->emulation2 == 0x02)))
          )
    type = FIG;
  else if (rominfo->buheader_len == 0 && x == 0xffff)
    type = MGD_SNES;

  /*
    x can be better trusted than type == FIG, but x being 0xffff is definitely
    not a guarantee that rominfo->buheader_len already has the right value
    (e.g. Earthworm Jim (U), Alfred Chicken (U|E), Soldiers of Fortune (U)).
  */
#if 0
  if (type != MGD_SNES) // don't do "&& type != SMC" or we'll miss a lot of PD ROMs
#endif
    {
      y = ((header->size_high << 8) + header->size_low) * 8 * 1024;
      y += SWC_HEADER_LEN;                      // if SWC-like header -> hdr[1] high byte,
      if (y == ucon64.file_size)                //  hdr[0] low byte of # 8 kB blocks in ROM
        rominfo->buheader_len = SWC_HEADER_LEN;
      else
        {
          int surplus = ucon64.file_size % 32768;
          if (surplus == 0)
            // most likely we guessed the copier type wrong
            {
              rominfo->buheader_len = 0;
              type = MGD_SNES;
            }
          /*
            Check for surplus being smaller than 31232 instead of MAXBUFSIZE
            (32768) to detect "Joystick Sampler with Still Picture (PD)" (64000
            bytes, including SWC header).
            "Super Wild Card V2.255 DOS ROM (BIOS)" is 16384 bytes (without
            header), so check for surplus being smaller than 16384.
            Shadow, The (Beta) [b3] has a surplus of 7680 bytes (15 * 512). So,
            accept a surplus of up to 7680 bytes as a header...
          */
          else if (surplus % SWC_HEADER_LEN == 0 &&
                   surplus < (int) (15 * SWC_HEADER_LEN) &&
                   ucon64.file_size > surplus)
            rominfo->buheader_len = surplus;
          // special case for Infinity Demo (PD)... (has odd size, but SWC
          //  header). Don't add "|| type == FIG" as it is too unreliable
          else if (type == SWC || type == GD3 || type == UFO)
            rominfo->buheader_len = SWC_HEADER_LEN;
        }
    }
  if (UCON64_ISSET (ucon64.buheader_len))       // -hd, -nhd or -hdn switch was specified
    {
      rominfo->buheader_len = ucon64.buheader_len;
      if (type == MGD_SNES && rominfo->buheader_len)
        type = SMC;
    }

  if (rominfo->buheader_len && !memcmp ((unsigned char *) header + 0x1e8, "NSRT", 4))
    nsrt_header = 1;
  else
    nsrt_header = 0;
}


static int
snes_set_hirom (unsigned char *rom_buffer, int size)
/*
  This function tries to determine if the ROM dump is LoROM or HiROM. It returns
  the highest value that check_banktype() returns. A higher value means a higher
  chance the bank type is correct.
*/
{
  int x, score_hi = 0, score_lo = 0;

  if (size >= (int) (8 * MBIT + SNES_HEADER_START + SNES_HIROM + SNES_HEADER_LEN) &&
      !strncmp ((char *) rom_buffer + SNES_HEADER_START + 16, "ADD-ON BASE CASSETE", 19))
    { // A Sufami Turbo dump contains 4 copies of the ST BIOS, which is 2 Mbit.
      //  After the BIOS comes the game data.
      st_dump = 1;
      snes_header_base = 8 * MBIT;
      x = 8 * MBIT + SNES_HIROM;
    }
  else if (snes_header_base == SNES_EROM)
    x = SNES_EROM + SNES_HIROM;
  else
    {
      snes_header_base = 0;
      x = SNES_HIROM;
    }

  if (size > SNES_HEADER_START + SNES_HIROM + 0x4d)
    {
      score_hi = check_banktype (rom_buffer, x);
      score_lo = check_banktype (rom_buffer, snes_header_base);
    }
  if (score_hi > score_lo)                      // yes, a preference for LoROM
    {                                           //  (">" vs. ">=")
      snes_hirom = SNES_HIROM;
      x = score_hi;
    }
  else
    {
      snes_hirom = 0;
      x = score_lo;
    }
  /*
    It would be nice if snes_header.map_type & 1 could be used to verify that
    snes_hirom has the correct value, but it doesn't help much. For games like
    Batman Revenge of the Joker (U) it matches what check_banktype() finds.
    snes_hirom must be 0x8000 for that game in order to display correct
    information. However it should be 0 when writing a copier header.
    So, snes_header.map_type can't be used to recognize such cases.
  */

  // step 3.
  if (UCON64_ISSET (ucon64.snes_hirom))         // -hi or -nhi switch was specified
    {
      snes_hirom = ucon64.snes_hirom;
      // keep snes_deinterleave() from changing snes_hirom
      snes_hirom_ok = 1;
      if (size < (int) (SNES_HEADER_START + SNES_HIROM + SNES_HEADER_LEN))
        snes_hirom = 0;
    }                                           

  if (UCON64_ISSET (ucon64.snes_header_base))   // -erom switch was specified
    {
      snes_header_base = ucon64.snes_header_base;
      if (snes_header_base &&
          size < (int) (snes_header_base + SNES_HEADER_START + snes_hirom + SNES_HEADER_LEN))
        snes_header_base = 0;                   // Don't let -erom crash on a too small ROM
    }

  return x;
}


static void
snes_set_bs_dump (st_rominfo_t *rominfo, unsigned char *rom_buffer, int size)
{
  bs_dump = snes_check_bs ();
  /*
    Do the following check before checking for ucon64.bs_dump. Then it's
    possible to specify both -erom and -bs with effect, for what it's worth ;-)
    The main reason to test this case is to display correct info for "SD Gundam
    G-NEXT + Rom Pack Collection (J) [!]". Note that testing for SNES_EROM
    causes the code to be skipped for Sufami Turbo dumps.
  */
  if (bs_dump &&
      snes_header_base == SNES_EROM && !UCON64_ISSET (ucon64.snes_header_base))
    {
      bs_dump = 0;
      snes_header_base = 0;
      snes_set_hirom (rom_buffer, size);
      rominfo->header_start = snes_header_base + SNES_HEADER_START + snes_hirom;
      memcpy (&snes_header, rom_buffer + rominfo->header_start, rominfo->header_len);
    }
  if (UCON64_ISSET (ucon64.bs_dump))            // -bs or -nbs switch was specified
    {
      bs_dump = ucon64.bs_dump;
      if (bs_dump && snes_header_base == SNES_EROM)
        bs_dump = 2;                            // Extended ROM => must be add-on cart
    }
}


int
snes_init (st_rominfo_t *rominfo, char **comment)
{
  int x, size, result = -1;                     // it's no SNES ROM dump until detected otherwise
  unsigned char *rom_buffer;
  st_unknown_header_t header = { 0, 0, 0, 0, 0, 0, { 0 }, 0, 0, 0, { 0 } };

  snes_hirom_ok = 0;                            // init these vars here, for -lsv
  snes_sramsize = 0;                            // idem
  type = SMC;                                   // idem, SMC indicates unknown copier type
  bs_dump = 0;                                  // for -lsv, but also just to init it
  st_dump = 0;                                  // idem

  x = 0;
  ucon64_fread (&header, UNKNOWN_HEADER_START, UNKNOWN_HEADER_LEN, ucon64.rom);

  /*
    snes_testinterleaved() needs the correct value for snes_hirom and
    rominfo->header_start. snes_hirom may be used only after the check for
    -hi/-nhi has been done. However, rominfo->buheader_len must have the
    correct value in order to determine the value for snes_hirom. This can only
    be known after the backup unit header length detection (including the check
    for -hd/-nhd/-hdn). So, the order must be
    1. - rominfo->buheader_len
    2. - snes_hirom
    3. - check for -hi/-nhi
    4. - snes_testinterleaved()
  */

  snes_handle_buheader (rominfo, &header);      // step 1. & first part of step 2.

  size = ucon64.file_size - rominfo->buheader_len;
  if (size < (int) (SNES_HEADER_START + SNES_HEADER_LEN))
    {
      snes_hirom = 0;
      if (UCON64_ISSET (ucon64.snes_hirom))     // see snes_set_hirom()
        snes_hirom = ucon64.snes_hirom;
      snes_hirom_ok = 1;

      rominfo->interleaved = 0;
      if (UCON64_ISSET (ucon64.interleaved))
        rominfo->interleaved = ucon64.interleaved;
      return -1;                                // don't continue (seg faults!)
    }
  if (ucon64.console == UCON64_SNES || (type != SMC && size <= 16 * 1024 * 1024))
    result = 0;                                 // it seems to be a SNES ROM dump

  if (!(rom_buffer = (unsigned char *) malloc (size)))
    {
      static char msg[100];

      sprintf (msg, ucon64_msg[ROM_BUFFER_ERROR], size);
      *comment = msg;
      return -1;                                // don't exit(), we might've been
    }                                           //  called with -lsv
  ucon64_fread (rom_buffer, rominfo->buheader_len, size, ucon64.rom);

  x = snes_set_hirom (rom_buffer, size);        // second part of step 2. & step 3.

  rominfo->header_start = snes_header_base + SNES_HEADER_START + snes_hirom;
  rominfo->header_len = SNES_HEADER_LEN;
  // set snes_header before calling snes_testinterleaved()
  memcpy (&snes_header, rom_buffer + rominfo->header_start, rominfo->header_len);

  // step 4.
  rominfo->interleaved = UCON64_ISSET (ucon64.interleaved) ?
    ucon64.interleaved : snes_testinterleaved (rom_buffer, size, x);

  // bs_dump has to be set before calling snes_chksum(), but snes_check_bs()
  //  needs snes_header to be filled with the correct data
  if (rominfo->interleaved)
    {
      snes_deinterleave (rominfo, &rom_buffer, size, comment);
      snes_set_hirom (rom_buffer, size);
      rominfo->header_start = snes_header_base + SNES_HEADER_START + snes_hirom;
      memcpy (&snes_header, rom_buffer + rominfo->header_start, rominfo->header_len);
    }

  snes_set_bs_dump (rominfo, rom_buffer, size);

  if (result == 0)
    {
      if (bs_dump == 1)                         // bs_dump == 2 for BS add-on dumps
        {
          unsigned short int *bs_date_ptr = (unsigned short int *)
            (rom_buffer + snes_header_base + SNES_HEADER_START + snes_hirom + 38);
          /*
            We follow the "uCONSRT standard" for calculating the CRC32 of BS
            dumps. At the time of this writing (20 June 2003) the uCONSRT
            standard defines that the date of BS dumps has to be "skipped"
            (overwritten with a constant number), because the date is variable.
            When a BS dump is made the BSX fills in the date. Otherwise two
            dumps of the same memory card would have a different CRC32.
            For BS add-on cartridge dumps we don't do anything special as they
            come from cartridges (with a constant date).
            Why 42? It's the answer to life, the universe and everything :-)
          */
#ifdef  WORDS_BIGENDIAN
          *bs_date_ptr = 0x4200;
#else
          *bs_date_ptr = 0x0042;
#endif
          get_nsrt_info (rom_buffer, rominfo->header_start, (unsigned char *) &header);
          ucon64.crc32 = crc32 (0, rom_buffer, size);
        }
      else
        {
          get_nsrt_info (rom_buffer, rominfo->header_start, (unsigned char *) &header);
          ucon64.crc32 = crc32 (0, rom_buffer, size);
        }
    }

  free (rom_buffer);
  return result;
}


int
snes_check_bs (void)
{
  if ((snes_header.maker == 0x33 || snes_header.maker == 0xff) &&
      (snes_header.map_type == 0 || (snes_header.map_type & 0x83) == 0x80))
    {
      int date = (snes_header.bs_day << 8) | snes_header.bs_month;
      if (date == 0)
        return 2;                               // BS add-on cartridge dump
      else if (date == 0xffff ||
               ((snes_header.bs_month & 0xf) == 0 &&
                ((unsigned int) ((snes_header.bs_month >> 4) - 1)) < 12))
        return 1;                               // BS dump (via BSX)
    }
  return 0;
}


int
snes_isprint (char *s, int len)
{
  unsigned char *p = (unsigned char *) s;

  for (; len >= 0; p++, len--)
    // we don't use isprint(), because we don't want to get different results
    //  of check_banktype() for different locale settings
    if (*p < 0x20 || *p > 0x7e)
      return 0;

  return 1;
}


int
check_banktype (unsigned char *rom_buffer, int header_offset)
/*
  This function is used to check if the value of header_offset is a good guess
  for the location of the internal SNES header (and thus of the bank type
  (LoROM, HiROM or Extended HiROM)). The higher the returned value, the higher
  the chance the guess was correct.
*/
{
  int score = 0, x, y;

//  dumper (stdout, (char *) rom_buffer + SNES_HEADER_START + header_offset,
//           SNES_HEADER_LEN, SNES_HEADER_START + header_offset, DUMPER_HEX);

  // game ID info (many games don't have useful info here)
  if (snes_isprint ((char *) rom_buffer + SNES_HEADER_START + header_offset + 2, 4))
    score += 1;

  if (!bs_dump)
    {
      if (snes_isprint ((char *) rom_buffer + SNES_HEADER_START + header_offset + 16,
                        SNES_NAME_LEN))
        score += 1;

      // map type
      x = rom_buffer[SNES_HEADER_START + header_offset + 37];
      if ((x & 0xf) < 4)
        score += 2;
      y = rom_buffer[SNES_HEADER_START + header_offset + 38];
      if (snes_hirom_ok && !(y == 0x34 || y == 0x35)) // ROM type for SA-1
        // map type, HiROM flag (only if we're sure about value of snes_hirom)
        if ((x & 1) == ((header_offset >= snes_header_base + SNES_HIROM) ? 1 : 0))
          score += 1;

      // ROM size
      if (1 << (rom_buffer[SNES_HEADER_START + header_offset + 39] - 7) <= 64)
        score += 1;

      // SRAM size
      if (1 << rom_buffer[SNES_HEADER_START + header_offset + 40] <= 256)
        score += 1;

      // country
      if (rom_buffer[SNES_HEADER_START + header_offset + 41] <= 13)
        score += 1;
    }
  else
    {
      if (snes_hirom_ok)
        // map type, HiROM flag
        if ((rom_buffer[SNES_HEADER_START + header_offset + 40] & 1) ==
            ((header_offset >= snes_header_base + SNES_HIROM) ? 1 : 0))
          score += 1;
    }

  // publisher "escape code"
  if (rom_buffer[SNES_HEADER_START + header_offset + 42] == 0x33)
    score += 2;
  else // publisher code
    if (snes_isprint ((char *) rom_buffer + SNES_HEADER_START + header_offset, 2))
      score += 2;

  // version
  if (rom_buffer[SNES_HEADER_START + header_offset + 43] <= 2)
    score += 2;

  // checksum bytes
  x = rom_buffer[SNES_HEADER_START + header_offset + 44] +
      (rom_buffer[SNES_HEADER_START + header_offset + 45] << 8);
  y = rom_buffer[SNES_HEADER_START + header_offset + 46] +
      (rom_buffer[SNES_HEADER_START + header_offset + 47] << 8);
  if (x + y == 0xffff)
    {
      if (x == 0xffff || y == 0xffff)
        score += 3;
      else
        score += 4;
    }

  // reset vector
  if (rom_buffer[SNES_HEADER_START + header_offset + 0x4d] & 0x80)
    score += 3;

  return score;
}


static void
get_nsrt_info (unsigned char *rom_buffer, int header_start, unsigned char *buheader)
{
  if (nsrt_header)
    {
      memcpy (rom_buffer + header_start + 16 - (st_dump ? SNES_HEADER_START : 0),
              buheader + 0x1d1, (bs_dump || st_dump) ? 16 : SNES_NAME_LEN); // name
      // we ignore interleaved ST dumps
      if (!bs_dump)
        {
          // According to the NSRT specification, the region byte should be set
          //  to 0 for BS dumps.
          rom_buffer[header_start + 41] = buheader[0x1d0] & 0x0f; // region
          // NSRT only modifies the internal header. For BS dumps the internal
          //  checksum does not include the header. So, we don't have to
          //  overwrite the checksum.
          rom_buffer[header_start + 44] = ~buheader[0x1e6]; // inverse checksum low
          rom_buffer[header_start + 45] = ~buheader[0x1e7]; // inverse checksum high
          rom_buffer[header_start + 46] = buheader[0x1e6]; // checksum low
          rom_buffer[header_start + 47] = buheader[0x1e7]; // checksum high
        }
    }
}
