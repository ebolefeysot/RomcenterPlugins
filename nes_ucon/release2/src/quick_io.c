/*
  quick_io.c - simple wrapper for file io
  
  written by 1999 - 2003 NoisyB (noisyb@gmx.net)
             2001 - 2003 dbjh

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

#ifdef  HAVE_CONFIG_H
#include "config.h"
#endif
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#ifdef  HAVE_UNISTD_H
#include <unistd.h>
#endif
#include "misc.h"
#include "quick_io.h"


int
quick_io (void *buffer, size_t start, size_t len, const char *filename,
          const char *mode)
{
  int result;
  FILE *fh;

  if ((fh = fopen (filename, (const char *) mode)) == NULL)
    {
#ifdef DEBUG
      extern int errno;
      fprintf (stderr, "ERROR: Could not open \"%s\" in mode \"%s\"\n"
                       "CAUSE: %s\n", filename, mode, strerror (errno));
#endif
      return -1; // TODO: 0?
    }

#ifdef DEBUG
  fprintf (stderr, "\"%s\": \"%s\"\n", filename, (char *) mode);
#endif

  fseek (fh, start, SEEK_SET);                  // TODO: what if fseek fails?

  // Note the order of arguments of fread() and fwrite(). Now quick_io()
  //  returns the number of characters read or written. Some code relies on
  //  this behaviour!
  if (*mode == 'r' && mode[1] != '+')           // "r+b" always writes
    result = (int) fread (buffer, 1, len, fh);
  else
    result = (int) fwrite (buffer, 1, len, fh);

  fclose (fh);
  return result;
}


int
quick_io_c (int value, size_t start, const char *filename, const char *mode)
{
  int result;
  FILE *fh;

  if ((fh = fopen (filename, (const char *) mode)) == NULL)
    {
#ifdef DEBUG
      extern int errno;
      fprintf (stderr, "ERROR: Could not open \"%s\" in mode \"%s\"\n"
                       "CAUSE: %s\n", filename, mode, strerror (errno));
#endif
      return -1; // TODO: 0?
    }

#ifdef DEBUG
  fprintf (stderr, "\"%s\": \"%s\"\n", filename, (char *) mode);
#endif

  fseek (fh, start, SEEK_SET);                  // TODO: what if fseek fails?

  if (*mode == 'r' && mode[1] != '+')           // "r+b" always writes
    result = fgetc (fh);
  else
    result = fputc (value, fh);

  fclose (fh);
  return result;
}
