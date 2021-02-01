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

#include <stdio.h>
#include <stdlib.h>
#include "misc.h"
#include "ucon64.h"
#include "n64.h"

#define BUFSIZE (1024 * 1024)


static n64_file_t type;


n64_file_t
n64_get_file_type (void)
{
  return type;
}


int
n64_init (st_rominfo_t *rominfo, char **comment)
{
  int result = -1, x;
  FILE *file;
  unsigned char *buffer;

  if (!(file = fopen (ucon64.rom, "rb")))
    return -1;
    
  fread (&x, 1, 4, file);
  /*
    0x41123780 and 0x12418037 can be found in te following files:
    2 Blokes & An Armchair - Nintendo 64 Remix Remix (PD)
    Zelda Boot Emu V1 (PD)
    Zelda Boot Emu V2 (PD)
  */
  if (x == 0x40123780 || x == 0x41123780) // 0x80371240, 0x80371241
    {
      type = Z64;
      rominfo->interleaved = 0;
      result = 0;
    }
  else if (x == 0x12408037 || x == 0x12418037) // 0x37804012, 0x37804112
    {
      type = V64;
      rominfo->interleaved = 1;
      result = 0;
    }
  else
    result = -1;

  if (UCON64_ISSET (ucon64.interleaved))
    rominfo->interleaved = ucon64.interleaved;
  if (ucon64.console == UCON64_N64)
    result = 0;

  if (result == 0)
    {
      if ((buffer = (unsigned char *) malloc (BUFSIZE)) == NULL)
        {
          static char msg[100];
    
          sprintf (msg, ucon64_msg[ROM_BUFFER_ERROR], BUFSIZE);
          *comment = msg;
          fclose (file);
          return -1;
        }
    
      fseek (file, 0, SEEK_SET);
      while ((x = fread (buffer, 1, BUFSIZE, file)))
        {
          /*
            For historical reasons I calculate the CRC-32 value of the data
            in V64 format. Otherwise the data would have to be byte-swapped for
            V64 files.
          */
          if (!rominfo->interleaved)
            mem_swap_b (buffer, x);
          ucon64.crc32 = crc32 (ucon64.crc32, buffer, x);
        }
      free (buffer);
    }

  fclose (file);
  return result;
}
