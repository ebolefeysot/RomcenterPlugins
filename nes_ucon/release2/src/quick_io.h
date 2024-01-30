/*
  quick_io.h - simple wrapper for file io
  
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

#ifndef QUICK_IO_H
#define QUICK_IO_H
#ifdef  __cplusplus
extern "C" {
#endif
/*
  Quick IO

  mode
    "r", "rb", "w", "wb", "a", "ab"

  quick_io_c() returns byte read or fputc()'s status
  quick_io() returns number of bytes read or written
*/
extern int quick_io (void *buffer, size_t start, size_t len, const char *fname, const char *mode);
extern int quick_io_c (int value, size_t start, const char *fname, const char *mode);


/*
  Macros

  q_fread()  same as fread but takes start and src is a filename
  q_fwrite() same as fwrite but takes start and dest is a filename; mode
             is the same as fopen() modes
  q_fgetc()  same as fgetc but takes filename instead of FILE and a pos
  q_fputc()  same as fputc but takes filename instead of FILE and a pos

  b,s,l,f,m == buffer,start,len,filename,mode
*/
#define q_fread(b,s,l,f) (quick_io(b,s,l,f,"rb"))
#define q_fwrite(b,s,l,f,m) (quick_io((void *)b,s,l,f,m))
#define q_fgetc(f,s) (quick_io_c(0,s,f,"rb"))
#define q_fputc(f,s,b,m) (quick_io_c(b,s,f,m))

#ifdef  __cplusplus
}
#endif
#endif // QUICK_IO_H
