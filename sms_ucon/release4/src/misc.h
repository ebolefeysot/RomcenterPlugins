/*
  SMS plug-in for RomCenter (http://www.romcenter.com)
  Copyright (c) 1999 - 2002 NoisyB <noisyb@gmx.net>
  Copyright (c) 2001, 2003, 2005 dbjh

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
#ifndef MISC_H
#define MISC_H

#include <string.h>
#include <stdio.h>
#ifdef  HAVE_ZLIB_H
#include "miscz.h"
#endif                                          // HAVE_ZLIB_H

#if     defined __MINGW32__ || defined __CYGWIN__ //#ifdef  HAVE_INTTYPES_H
#include <inttypes.h>
#else                                           // __MSDOS__, _WIN32 (VC++)
#ifndef OWN_INTTYPES
#define OWN_INTTYPES                            // signal that these are defined
typedef unsigned char uint8_t;
typedef signed char int8_t;
typedef unsigned short int uint16_t;
typedef signed short int int16_t;
typedef unsigned int uint32_t;
typedef signed int int32_t;
#ifndef _WIN32
typedef unsigned long long int uint64_t;
typedef signed long long int int64_t;
#else
typedef unsigned __int64 uint64_t;
typedef signed __int64 int64_t;
#endif
#endif                                          // OWN_INTTYPES
#endif

#if     (defined __unix__ && !defined __MSDOS__) || defined __BEOS__ || \
        defined AMIGA || defined __APPLE__      // Mac OS X actually
// GNU/Linux, Solaris, FreeBSD, OpenBSD, Cygwin, BeOS, Amiga, Mac (OS X)
#define FILE_SEPARATOR '/'
#define FILE_SEPARATOR_S "/"
#else // DJGPP, Win32
#define FILE_SEPARATOR '\\'
#define FILE_SEPARATOR_S "\\"
#endif

#ifndef MAXBUFSIZE
#define MAXBUFSIZE 32768
#endif

extern const void *memmem2 (const void *buffer, size_t bufferlen,
                            const void *search, size_t searchlen);

#ifdef  __CYGWIN__
extern char *fix_character_set (char *str);
#endif

#ifndef  HAVE_ZLIB_H
// use zlib's crc32() if HAVE_ZLIB_H is defined
extern unsigned int crc32 (unsigned int crc32, const void *buffer, unsigned int size);
#endif

extern int q_fcrc32 (const char *filename, int start);
extern char *get_property (const char *filename, const char *propname, char *value,
                           const char *def);

#ifdef _MSC_VER

#include <io.h>
#include <direct.h>
#include <sys/types.h>
#include <sys/stat.h>                           // According to MSDN <sys/stat.h> must
                                                //  come after <sys/types.h>. Yep, that's M$.
#define F_OK 00
#define W_OK 02
#define R_OK 04
#define X_OK R_OK                               // this is correct for dirs, but not for exes

#elif   defined __MINGW32__ && defined DLL

#define access  _access
#define getcwd  _getcwd
#define stat    _stat
#define stricmp _stricmp

#endif

#endif // #ifndef MISC_H
