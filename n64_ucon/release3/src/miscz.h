/*
  miscz.h - miscellaneous zlib functions
  
  written by 2001 - 2003 dbjh

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

#ifndef MISCZ_H
#define MISCZ_H

#ifdef  HAVE_CONFIG_H
#include "config.h"                             // HAVE_ZLIB_H, ANSI_COLOR support
#endif

#ifdef  __cplusplus
extern "C" {
#endif

#ifdef  HAVE_ZLIB_H
// make sure ZLIB support is enabled everywhere
//#warning HAVE_ZLIB_H is defined

#include <stdio.h>
#include <zlib.h>
#include "unzip.h"

extern FILE *fopen2 (const char *filename, const char *mode);
extern int fclose2 (FILE *file);
extern int fseek2 (FILE *file, long offset, int mode);
extern size_t fread2 (void *buffer, size_t size, size_t number, FILE *file);
extern int fgetc2 (FILE *file);
extern char *fgets2 (char *buffer, int maxlength, FILE *file);
extern int feof2 (FILE *file);
extern size_t fwrite2 (const void *buffer, size_t size, size_t number, FILE *file);
extern int fputc2 (int character, FILE *file);
extern long ftell2 (FILE *file);
extern void rewind2 (FILE *file);
extern FILE *popen2 (const char *command, const char *type);
extern int pclose2 (FILE *stream);

extern int q_fsize2 (const char *filename);

#undef  feof                                    // necessary on (at least) Cygwin

#define fopen(FILE, MODE) fopen2(FILE, MODE)
#define fclose(FILE) fclose2(FILE)
#define fseek(FILE, OFFSET, MODE) fseek2(FILE, OFFSET, MODE)
#define fread(BUF, SIZE, NUM, FILE) fread2(BUF, SIZE, NUM, FILE)
#define fgetc(FILE) fgetc2(FILE)
#define fgets(BUF, MAXLEN, FILE) fgets2(BUF, MAXLEN, FILE)
#define feof(FILE) feof2(FILE)
#define fwrite(BUF, SIZE, NUM, FILE) fwrite2(BUF, SIZE, NUM, FILE)
#define fputc(CHAR, FILE) fputc2(CHAR, FILE)
#define ftell(FILE) ftell2(FILE)
#define rewind(FILE) rewind2(FILE)
#undef  popen
#define popen(COMMAND, TYPE) popen2(COMMAND, TYPE)
#undef  pclose
#define pclose(FILE) pclose2(FILE)

#define q_fsize(FILENAME) q_fsize2(FILENAME)

// Returns the number of files in the "central dir of this disk" or -1 if
//  filename is not a ZIP file or an error occured.
extern int unzip_get_number_entries (const char *filename);
extern int unzip_goto_file (unzFile file, int file_index);
extern int unzip_current_file_nr;
#endif

#ifdef  __cplusplus
}
#endif

#endif // #ifndef MISCZ_H
