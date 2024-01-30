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
#ifdef  HAVE_ZLIB_H
#include "miscz.h"
#endif
#include "ucon64.h"


st_ucon64_t ucon64;

const char *ucon64_msg[] = {
  "ERROR: Can't open \"%s\" for reading\n",                 // OPEN_READ_ERROR
  "ERROR: Can't read from \"%s\"\n",                        // READ_ERROR
  "ERROR: Not enough memory for buffer (%d bytes)\n",       // BUFFER_ERROR
  "ERROR: Not enough memory for ROM buffer (%d bytes)\n",   // ROM_BUFFER_ERROR
  "ERROR: Not enough memory for file buffer (%d bytes)\n",  // FILE_BUFFER_ERROR
  0
};


int
ucon64_fread (void *buffer, size_t start, size_t len, const char *filename)
{
  int result;
  FILE *fh;

  if ((fh = fopen (filename, "rb")) == NULL)
    return -1;

  fseek (fh, start, SEEK_SET);
  result = (int) fread (buffer, 1, len, fh);

  fclose (fh);
  return result;
}
