/*
  misc.h - miscellaneous functions
  
  written by 1999 - 2002 NoisyB (noisyb@gmx.net)
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

#ifndef MISC_H
#define MISC_H

#ifdef  HAVE_CONFIG_H
#include "config.h"                             // HAVE_ZLIB_H, ANSI_COLOR support
#endif

#ifdef  __cplusplus
extern "C" {
#endif

#include <string.h>
#include <stdio.h>
#ifdef  HAVE_ZLIB_H
#include "miscz.h"
#endif                                          // HAVE_ZLIB_H

#if     defined __CYGWIN__
#include <sys/types.h>
#ifndef OWN_INTTYPES
#define OWN_INTTYPES                            // signal that these are defined
#if     __GNUC__ < 3
typedef u_int8_t uint8_t;
typedef u_int16_t uint16_t;
typedef u_int32_t uint32_t;
typedef u_int64_t uint64_t;
#endif
#endif // OWN_INTTYPES
#else
#ifndef OWN_INTTYPES
#define OWN_INTTYPES                            // signal that these are defined
typedef unsigned char uint8_t;
typedef unsigned short int uint16_t;
typedef unsigned int uint32_t;
#ifndef _WIN32
typedef unsigned long long int uint64_t;
#else
typedef unsigned __int64 uint64_t;
#endif
typedef signed char int8_t;
typedef signed short int int16_t;
typedef signed int int32_t;
#ifndef _WIN32
typedef signed long long int int64_t;
#else
typedef signed __int64 int64_t;
#endif
#endif // OWN_INTTYPES
#endif

#if     (!defined TRUE || !defined FALSE)
#define FALSE 0
#define TRUE (!FALSE)
#endif

#if     defined __CYGWIN__
  #define CURRENT_OS_S "Win32 (Cygwin)"
#elif   defined _WIN32
  #ifdef  __MINGW32__
    #define CURRENT_OS_S "Win32 (MinGW)"
  #else
    #define CURRENT_OS_S "Win32 (Visual C++)"
  #endif
#endif

#if     ((defined __unix__ || defined __BEOS__) && !defined __MSDOS__)
// Cygwin, GNU/Linux, Solaris, FreeBSD, BeOS
#define FILE_SEPARATOR '/'
#define FILE_SEPARATOR_S "/"
#else // DJGPP, Win32
#define FILE_SEPARATOR '\\'
#define FILE_SEPARATOR_S "\\"
#endif

#ifndef MAXBUFSIZE
#define MAXBUFSIZE 32768
#endif // MAXBUFSIZE

#ifdef  __CYGWIN__
extern char *fix_character_set (char *value);
#endif

extern int is_func (char *s, int size, int (*func) (int));
extern void *mem_swap_b (void *buffer, uint32_t n);

#ifndef  HAVE_ZLIB_H
// use zlib's crc32() if HAVE_ZLIB_H is defined
extern unsigned int crc32 (unsigned int crc32, const void *buffer, unsigned int size);
#endif

extern int q_fcrc32 (const char *filename, int start);
#ifndef  HAVE_ZLIB_H
extern int q_fsize (const char *filename);
#endif

extern const char *get_property (const char *filename, const char *propname, char *value, const char *def);


#ifdef  _WIN32
// Note that _WIN32 is defined by cl.exe while the other constants (like WIN32)
//  are defined in header files. MinGW's gcc.exe defines all constants.

#include <sys/types.h>

int sync (void);
// For MinGW popen() and pclose() are unavailable for DLL's. For DLL's _popen()
//  and _pclose() should be used. Visual C++ only has the latter two.
#ifndef pclose                                  // miscz.h's definition gets higher "precedence"
#define pclose  _pclose
#endif
#ifndef popen                                   // idem
#define popen   _popen
#endif

#ifndef __MINGW32__
#include <io.h>
#include <direct.h>
#include <sys/stat.h>                           // According to MSDN <sys/stat.h> must
                                                //  come after <sys/types.h>. Yep, that's M$.
#define S_IWUSR _S_IWRITE
#define S_IRUSR _S_IREAD
#define S_ISDIR(mode) ((mode) & _S_IFDIR ? 1 : 0)
#define S_ISREG(mode) ((mode) & _S_IFREG ? 1 : 0)

#define F_OK 00
#define W_OK 02
#define R_OK 04
#define X_OK R_OK                               // this is correct for dirs, but not for exes

#define STDIN_FILENO (fileno (stdin))
#define STDOUT_FILENO (fileno (stdout))
#define STDERR_FILENO (fileno (stderr))

#else
#ifdef  DLL
#define access  _access
#define chmod   _chmod
#define fileno  _fileno
#define getcwd  _getcwd
#define isatty  _isatty
#define rmdir   _rmdir
#define stat    _stat
#define strdup  _strdup
#define stricmp _stricmp
#define strlwr  _strlwr
#define strnicmp _strnicmp
#define strupr  _strupr
#endif // DLL

#endif // !__MINGW32__
#endif // _WIN32

#ifdef  __cplusplus
}
#endif

#endif // #ifndef MISC_H
