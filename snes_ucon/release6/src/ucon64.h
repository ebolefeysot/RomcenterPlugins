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
#ifndef UCON64_H
#define UCON64_H

#include <stdio.h>


#define MBIT 131072
#define MAXROMSIZE (128 * MBIT)
#define UCON64_UNKNOWN -1
#define UCON64_SNES 1
#define UNKNOWN_HEADER_START 0
#define UNKNOWN_HEADER_LEN (sizeof (st_unknown_header_t))
#define UCON64_ISSET(x) (x != UCON64_UNKNOWN)

typedef struct
{
  int interleaved;                              // ROM is interleaved (swapped)
  int buheader_len;                             // length of backup unit header 0 == no bu hdr
  int header_start;                             // start of internal ROM header
  int header_len;                               // length of internal ROM header 0 == no hdr
} st_rominfo_t;

typedef struct
{
  const char *rom;                              // ROM (cmdline) with path
  int file_size;                                // (uncompressed) ROM file size
  unsigned int crc32;                           // crc32 value of ROM (used for DAT files)
  int console;                                  // the detected console system
  int buheader_len;                             // length of backup unit header 0 == no bu hdr
  int interleaved;                              // ROM is interleaved (swapped)
  int snes_header_base;                         // SNES ROM is "Extended" (or Sufami Turbo)
  int snes_hirom;                               // SNES ROM is HiROM
  int bs_dump;                                  // SNES "ROM" is a Broadcast Satellaview dump
} st_ucon64_t;

typedef struct // st_unknown_header
{
  unsigned char size_low;
  unsigned char size_high;
  unsigned char emulation;
  unsigned char hirom;
  unsigned char emulation1;
  unsigned char emulation2;
  unsigned char pad[2];
  unsigned char id1;
  unsigned char id2;
  unsigned char type;
  unsigned char pad2[501];
} st_unknown_header_t;

enum
{
  OPEN_READ_ERROR,
  READ_ERROR,
  BUFFER_ERROR,                                 // not enough memory
  ROM_BUFFER_ERROR,
  FILE_BUFFER_ERROR
};

extern st_ucon64_t ucon64;
extern const char *ucon64_msg[];

extern int ucon64_fread (void *buffer, size_t start, size_t len,
                         const char *filename);

#endif
