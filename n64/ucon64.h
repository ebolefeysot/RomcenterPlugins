/*
  Nintendo 64 plug-in for RomCenter (http://www.romcenter.com)
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

#ifndef UCON64_H
#define UCON64_H

#define MBIT 131072
#define MAXROMSIZE (512 * MBIT)
#define UCON64_UNKNOWN -1
#define UCON64_N64 1
#define UCON64_ISSET(x) (x != UCON64_UNKNOWN)

typedef struct
{
  int interleaved;                              // ROM is interleaved (swapped)
  int buheader_len;                             // length of backup unit header 0 == no bu hdr
} st_rominfo_t;

typedef struct
{
  const char *rom;                              // ROM (cmdline) with path
  int file_size;                                // (uncompressed) ROM file size
  unsigned int crc32;                           // crc32 value of ROM (used for DAT files)
  int console;                                  // the detected console system
  int buheader_len;                             // length of backup unit header 0 == no bu hdr
  int interleaved;                              // ROM is interleaved (swapped)
} st_ucon64_t;

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

#endif
