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
#include <string.h>
#include <ctype.h>
#ifdef  HAVE_UNISTD_H
#include <unistd.h>                             // getcwd()
#else // Visual C++
#include <direct.h>
#endif
#include "misc.h"
#ifdef  HAVE_ZLIB_H
#include "miscz.h"
#endif
#include "ucon64.h"
#include "n64.h"


//#define DEBUG
#define CONFIG_FILE_NAME "n64.cfg"
//#define RC261_COMPATIBILITY
#ifdef  RC261_COMPATIBILITY
#define rc_GetSignature GetSignature
#define rc_GetDllType GetDllType
#define rc_GetDllInterfaceVersion GetDllInterfaceVersion
#define rc_GetPlugInName GetPlugInName
#define rc_GetVersion GetVersion
#define rc_GetDescription GetDescription
#define rc_GetAuthor GetAuthor
#define rc_GetEmail GetEmail
#define rc_GetWebPage GetWebPage
#else
#include <windows.h>                            // registry functions / MinGW
#endif

#if     defined __MINGW32__
#define COMPILER "(MinGW)"
#elif   defined __CYGWIN__
#define COMPILER "(Cygwin)"
#elif   defined _MSC_VER
#define COMPILER "(Visual C++)"
#else
#define COMPILER "(Unknown)"
#error Unsupported compiler. Use MinGW, Cygwin or Visual C++.
#endif

static int process_file (char *crc32, char *crc32_in_zip, char **suffix,
                         int64_t *file_size, char **comment);

enum { V64_S, Z64_S };
static char returnstr[500], suffixes[2][300];
static int suffixes_set = 0;
static FILE *stdout2 = NULL;


#ifdef  __MINGW32__
#ifndef RC261_COMPATIBILITY
BOOL WINAPI
DllMain (HINSTANCE h, DWORD reason, LPVOID ptr)
{
  (void) ptr;                                   // warning remover
  switch (reason)
    {
    case DLL_PROCESS_ATTACH:
      DisableThreadLibraryCalls ((HMODULE) h);
      break;
    case DLL_PROCESS_DETACH:
      break;
    case DLL_THREAD_ATTACH:
      break;
    case DLL_THREAD_DETACH:
      break;
    }
  return TRUE;
}
#else
int __stdcall
DllMain (int a, int b, int c)
{
  (void) a;
  (void) b;
  (void) c;
  return 1;
}
#endif // RC261_COMPATIBILITY
#endif // __MINGW32__


static void
close_stdout2 (void)
{
  if (stdout2)
    {
      fclose (stdout2);
      stdout2 = NULL;
    }
}


char * __stdcall
rc_GetSignature (char *filename, char *crc32_in_zip, char **suffix,
                 int64_t *file_size, char **comment, char **error_msg)
{
  FILE *file;

  *suffix = NULL;
  *file_size = 0;
  *comment = NULL;
  *error_msg = NULL;
  
#ifdef  DEBUG
  if (!stdout2)
    {
      stdout2 = fopen ("stdout2.txt", "ab");
//      atexit (close_stdout2);
    }
#endif

  /*
    When the plug-in is compiled with MinGW or Visual C++ the current directory
    is <RomCenter base directory>\Cache at this point. When the plug-in is
    compiled with Cygwin it's <RomCenter base directory>.
    First we try to find RomCenter's base directory in the registry. If that
    fails we fall back to using a relative path.
  */
  if (!suffixes_set)
    {
      char config_file[FILENAME_MAX];
      int config_file_set = 0;
#ifndef RC261_COMPATIBILITY
      HKEY key;
      DWORD size = FILENAME_MAX;
      
      if (RegOpenKeyEx (HKEY_CURRENT_USER, "Software\\RomCenter2", 0,
                        KEY_QUERY_VALUE, &key) == ERROR_SUCCESS)
        {
          if (RegQueryValueEx (key, "HomePath", NULL, NULL, config_file, &size)
                == ERROR_SUCCESS)
            {
              if (config_file[size - 2] != '\\') // RegQueryValueEx() also counts the ASCII-z
                strcat (config_file, "\\");
              strcat (config_file, CONFIG_FILE_NAME);
              config_file_set = 1;
            }
          RegCloseKey (key);
        }
#endif

      if (!config_file_set)
        {
#ifndef __CYGWIN__
          strcpy (config_file, "..\\" CONFIG_FILE_NAME);
#else
          strcpy (config_file, CONFIG_FILE_NAME);
#endif
        }

#ifdef  DEBUG
      {
        char buffer[FILENAME_MAX];
        fprintf (stdout2, "current directory: \"%s\"\n", getcwd (buffer, FILENAME_MAX));
        fprintf (stdout2, "configuration file: \"%s\"\n", config_file);
      }
#endif

      get_property (config_file, "v64_suffix", suffixes[V64_S], ".v64");
      get_property (config_file, "z64_suffix", suffixes[Z64_S], ".z64");

      suffixes_set = 1;
    }
#ifdef  DEBUG
  close_stdout2 ();
#endif

  // the old plugin tester doesn't do the following check
  if (filename[0] == 0)
    {
      strcpy (returnstr, "No file name specified");
      *error_msg = returnstr;
      *comment = returnstr; // the old plugin tester interprets comment as error message
      return "00000000";
    }
      
  returnstr[0] = 0;
  if ((file = fopen (filename, "rb")) != NULL)
    {
      ucon64.rom = filename;

      // I use fseek()/ftell() (and not access()), because the file could be
      //  compressed with gzip
      fseek (file, 0, SEEK_END);
      ucon64.file_size = ftell (file);
      fclose (file);

      process_file (returnstr, crc32_in_zip, suffix, file_size, comment);
      return returnstr;
    }
  else
    {
      sprintf (returnstr, "Could not open file \"%s\"\n", filename);
      *error_msg = returnstr;
      *comment = returnstr;
      return "00000000";
    }
}


const char * __stdcall 
rc_GetDllType (void)
{
  return "romcenter signature calculator";
}


const char * __stdcall 
rc_GetDllInterfaceVersion (void)
{
  return "2.50";
}


const char * __stdcall 
rc_GetPlugInName (void)
{
  return "Nintendo 64 CRC-32 calculator";
}


#ifdef  RC261_COMPATIBILITY
#ifdef  _MSC_VER
__declspec (dllexport)
#endif
const char * __cdecl
/*
  Yes, this is not correct, but it works. When using __stdcall, link.exe finds
  a conflict with the GetVersion() in kernel32.dll.
  I tried to specify the decorated name in n64.def, but then the plugin tester
  can't find the function. Specifying the name on the command line gives the
  same results.
  I also tried to specify an alias in n64.def, but then the plugin tester
  can't load the DLL.
  Specifying the name of this function in n64.def will always cause an error,
  but the name has to be exported. That's why I use "__declspec (dllexport)" in
  the source code.
*/
#else
const char * __stdcall 
#endif
rc_GetVersion (void)
{
  return "1.2 " COMPILER;
}


const char * __stdcall 
rc_GetDescription (void)
{
  return "Nintendo 64 CRC-32 calculator. Based on Nintendo 64 code of uCON64"
         " (http://ucon64.sf.net). Detects files in Doctor V64 (Junior) and"
         " Mr. Backup Z64 format.";
}


const char * __stdcall 
rc_GetAuthor (void)
{
  return "dbjh";
}


const char * __stdcall 
rc_GetEmail (void)
{
  return "See the plug-in documentation";
}


const char * __stdcall 
rc_GetWebPage (void)
{
  return "www.romcenter.com";
}


int
process_file (char *crc32, char *crc32_in_zip, char **suffix,
              int64_t *file_size, char **comment)
{
  st_rominfo_t rominfo;
  (void) crc32_in_zip;
  
#if 0 // I don't know what the minimum file size is (1 MB?)
  // Check file size
  if (ucon64.file_size < 1024 * 1024)
    {
      *comment = (char *) "File is too small for a Nintendo 64 ROM dump.";
      return 0;
    }
#endif

  ucon64.interleaved =
  ucon64.buheader_len =
  ucon64.console = UCON64_UNKNOWN;
  ucon64.crc32 = 0;
  memset (&rominfo, 0, sizeof (rominfo));

  if (n64_init (&rominfo, comment) != 0)
    if (*comment == NULL) // if some error occurred in n64_init() => comment != NULL
      *comment = "File was not detected as Nintendo 64 ROM dump.";
  
  if (ucon64.crc32 == 0)
    if (ucon64.file_size <= MAXROMSIZE)
      ucon64.crc32 = q_fcrc32 (ucon64.rom, rominfo.buheader_len);
      
  sprintf (crc32, "%08x", ucon64.crc32);

  switch (n64_get_file_type ())
    {
    case V64:
      *suffix = suffixes[V64_S];
      break;
    case Z64:                                   // falling through
    default:
      *suffix = suffixes[Z64_S];
      break;
    }

  *file_size = ucon64.file_size;
  
  return 1;
}
