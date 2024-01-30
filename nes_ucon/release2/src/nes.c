/*
  NES plug-in for RomCenter (http://www.romcenter.com)
  Written by dbjh in 2003

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
#include <string.h>
#ifdef  HAVE_UNISTD_H
#include <unistd.h>                             // access()
#else // Visual C++
#include <io.h>
#endif
#include "misc.h"
#include "quick_io.h"
#include "ucon64.h"
#include "nes.h"


static nes_file_t type;
static st_ines_header_t ines_header;

static const int unif_prg_ids[] = {PRG0_ID, PRG1_ID, PRG2_ID, PRG3_ID,
                                   PRG4_ID, PRG5_ID, PRG6_ID, PRG7_ID,
                                   PRG8_ID, PRG9_ID, PRGA_ID, PRGB_ID,
                                   PRGC_ID, PRGD_ID, PRGE_ID, PRGF_ID};
static const int unif_chr_ids[] = {CHR0_ID, CHR1_ID, CHR2_ID, CHR3_ID,
                                   CHR4_ID, CHR5_ID, CHR6_ID, CHR7_ID,
                                   CHR8_ID, CHR9_ID, CHRA_ID, CHRB_ID,
                                   CHRC_ID, CHRD_ID, CHRE_ID, CHRF_ID};

static int rom_size;


nes_file_t
nes_get_file_type (void)
{
  return type;
}


static st_unif_chunk_t *
read_chunk (unsigned long id, unsigned char *rom_buffer, int cont)
/*
  The caller is responsible for freeing the memory for the allocated
  st_unif_chunk_t. It should do that by calling free() with the pointer to
  the st_unif_chunk_t. It should do NOTHING for the struct member `data'.
*/
{
  struct
  {
     unsigned int id;                           // chunk identification string
     unsigned int length;                       // data length, in little endian format
  } chunk_header;
  st_unif_chunk_t *unif_chunk;
  static int pos = 0;

  if (!cont)
    pos = 0;

  do
    {
      memcpy (&chunk_header, rom_buffer + pos, sizeof (chunk_header));
      pos += sizeof (chunk_header);
      if (chunk_header.id != id)
        {
          if ((int) (pos + chunk_header.length) >= rom_size)
            break;
          else
            pos += chunk_header.length;
        }
    }
  while (chunk_header.id != id);

  if (chunk_header.id != id || pos >= rom_size)
    return (st_unif_chunk_t *) NULL;

  if ((unif_chunk = (st_unif_chunk_t *)
         malloc (sizeof (st_unif_chunk_t) + chunk_header.length)) == NULL)
    {
      static char msg[100];

      sprintf (msg, "ERROR: Not enough memory for chunk (%d bytes)\n",
        (int) sizeof (st_unif_chunk_t) + chunk_header.length);
//      *comment = msg;
      return (st_unif_chunk_t *) NULL;
    }
  unif_chunk->id = chunk_header.id;
  unif_chunk->length = chunk_header.length;
  unif_chunk->data = &((unsigned char *) unif_chunk)[sizeof (st_unif_chunk_t)];

  memcpy (unif_chunk->data, rom_buffer + pos, chunk_header.length);
  return unif_chunk;
}


int
nes_j (unsigned char **mem_image, char **comment)
/*
  The Pasofami format consists of several files:
  - .PRM: header (uCON64 treats it as optional in order to support RAW images)
  - .700: trainer data (optional)
  - .PRG: ROM data
  - .CHR: VROM data (optional)
*/
{
  char src_name[FILENAME_MAX];
  unsigned char *buffer;
  int prg_size = 0, chr_size = 0, size, bytes_read = 0;

  // build iNES header
  memset (&ines_header, 0, INES_HEADER_LEN);
  memcpy (&ines_header.signature, INES_SIG_S, 4);

  strcpy (src_name, ucon64.rom);
  set_suffix (src_name, ".700");
  if (access (src_name, F_OK) == 0 && q_fsize (src_name) >= 512)
    ines_header.ctrl1 |= INES_TRAINER;

  set_suffix (src_name, ".PRG");
  if (access (src_name, F_OK) == 0)
    prg_size = q_fsize (src_name);
  ines_header.prg_size = prg_size >> 14;

  set_suffix (src_name, ".CHR");
  if (access (src_name, F_OK) == 0)
    chr_size = q_fsize (src_name);
  ines_header.chr_size = chr_size >> 13;

  size = prg_size + chr_size + ((ines_header.ctrl1 & INES_TRAINER) ? 512 : 0);
  if ((buffer = (unsigned char *) malloc (size)) == NULL)
    {
      static char msg[100];

      sprintf (msg, ucon64_msg[BUFFER_ERROR], size);
      *comment = msg;
      return -1;
    }

  if (ines_header.ctrl1 & INES_TRAINER)
    {
      set_suffix (src_name, ".700");
      q_fread (buffer, 0, 512, src_name);       // use 512 bytes at max
      bytes_read = 512;
    }

  if (prg_size > 0)
    {
      set_suffix (src_name, ".PRG");
      q_fread (buffer + bytes_read, 0, prg_size, src_name);
      bytes_read += prg_size;
    }

  if (chr_size > 0)
    {
      set_suffix (src_name, ".CHR");
      q_fread (buffer + bytes_read, 0, chr_size, src_name);
    }

  *mem_image = buffer;

  return 0;
}


int
nes_init (st_rominfo_t *rominfo, char **comment)
{
  unsigned char magic[15], *rom_buffer;
  int result = -1, size, x, n, crc = 0;
  char *str;
  st_unif_chunk_t *unif_chunk;

  type = PASOFAMI;                              // reset type, see below

  q_fread (magic, 0, 15, ucon64.rom);
  if (memcmp (magic, "NES", 3) == 0)
    /*
      Check for "NES" and not for INES_SIG_S ("NES\x1a"), because there are two
      NES files floating around on the internet with a pseudo iNES header:
      "Home Alone 2 - Lost in New York (U) [b3]" (magic: "NES\x19") and
      "Linus Music Demo (PD)" (magic: "NES\x1b")
    */
    {
      type = INES;
      result = 0;
    }
  else if (memcmp (magic, UNIF_SIG_S, 4) == 0)
    {
      type = UNIF;
      result = 0;
    }
  else if (memcmp (magic, FDS_SIG_S, 4) == 0)
    {
      type = FDS;
      result = 0;

      rominfo->buheader_len = FDS_HEADER_LEN;
    }
  else if (memcmp (magic, "\x01*NINTENDO-HVC*", 15) == 0) // "headerless" FDS/FAM file
    {
      if (ucon64.file_size % 65500 == 192)
        type = FAM;
      else
        type = FDS;
      result = 0;
    }

  if (type == PASOFAMI)                         // INES, UNIF, FDS and FAM are much
    {                                           //  more reliable than stricmp()s
      str = (char *) get_suffix (ucon64.rom);
      if (!stricmp (str, ".prm") ||
          !stricmp (str, ".700") ||
          !stricmp (str, ".prg") ||
          !stricmp (str, ".chr"))
        {
          type = PASOFAMI;
          result = 0;
        }
      else if (magic[8] == 0xaa && magic[9] == 0xbb)
        {                                       // TODO: finding a reliable means
          type = FFE;                           //  for detecting FFE images
          result = 0;
        }
    }
  if (ucon64.console == UCON64_NES)
    result = 0;

  switch (type)
    {
    case INES:
      rominfo->buheader_len = INES_HEADER_LEN;
      break;
    case UNIF:
      rominfo->buheader_len = UNIF_HEADER_LEN;

      rom_size = ucon64.file_size - UNIF_HEADER_LEN;
      if ((rom_buffer = (unsigned char *) malloc (rom_size)) == NULL)
        {
          static char msg[100];
    
          sprintf (msg, ucon64_msg[ROM_BUFFER_ERROR], rom_size);
          *comment = msg;
          return -1;
        }
      q_fread (rom_buffer, UNIF_HEADER_LEN, rom_size, ucon64.rom);

      size = 0;
      // PRG chunks
      for (n = 0; n < 16; n++)
        {
          if ((unif_chunk = read_chunk (unif_prg_ids[n], rom_buffer, 0)) != NULL)
            {
              crc = crc32 (crc, (unsigned char *) unif_chunk->data, unif_chunk->length);
              size += unif_chunk->length;
            }
          free (unif_chunk);
        }

      // CHR chunks
      for (n = 0; n < 16; n++)
        {
          if ((unif_chunk = read_chunk (unif_chr_ids[n], rom_buffer, 0)) != NULL)
            {
              crc = crc32 (crc, (unsigned char *) unif_chunk->data, unif_chunk->length);
              size += unif_chunk->length;
            }
          free (unif_chunk);
        }
      ucon64.crc32 = crc;
      rominfo->data_size = size;

      free (rom_buffer);
      break;
    case PASOFAMI:
      /*
        Either a *.PRM header file, a 512-byte *.700 trainer file, a *.PRG
        ROM data file or a *.CHR VROM data file.
      */

      /*
        Build a temporary iNES image in memory from the Pasofami files.
        In memory, because we want to be able to display info for Pasofami
        files on read-only filesystems WITHOUT messing with/finding temporary
        storage somewhere. We also want to calculate the CRC and it's handy to
        have the data in memory for that.
        Note that nes_j() wouldn't be much different if q_fcrc32() would be
        used. This function wouldn't be much different either.
      */
      x = nes_j (&rom_buffer, comment);
      rominfo->data_size = (ines_header.prg_size << 14) + (ines_header.chr_size << 13) +
                             ((ines_header.ctrl1 & INES_TRAINER) ? 512 : 0);
      if (x == 0)
        {                                       // use buf only if it could be allocated
          ucon64.crc32 = crc32 (0, rom_buffer, rominfo->data_size);
          free (rom_buffer);
        }

      break;
    case FFE:
      /*
        512-byte header
        512-byte trainer (optional)
        ROM data
        VROM data (optional)

        It makes no sense to make a temporary iNES image here. It makes sense
        for Pasofami, because there might be a .PRM file and because there is
        still other information about the image structure.
      */
      rominfo->buheader_len = 512;
      break;
    case FDS:
      break;
    case FAM:
      // FAM files don't have a header. Instead they seem to have a 192 byte trailer.
      rom_size = ucon64.file_size - FAM_HEADER_LEN;
      if ((rom_buffer = (unsigned char *) malloc (rom_size)) == NULL)
        {
          static char msg[100];
    
          sprintf (msg, ucon64_msg[ROM_BUFFER_ERROR], rom_size);
          *comment = msg;
          return -1;
        }
      q_fread (rom_buffer, 0, rom_size, ucon64.rom);
      ucon64.crc32 = crc32 (0, rom_buffer, rom_size);
      free (rom_buffer);
      break;
    }

  if (UCON64_ISSET (ucon64.buheader_len))       // -hd, -nhd or -hdn switch was specified
    rominfo->buheader_len = ucon64.buheader_len;

  if (ucon64.crc32 == 0)
    ucon64.crc32 = q_fcrc32 (ucon64.rom, rominfo->buheader_len);

  return result;
}
