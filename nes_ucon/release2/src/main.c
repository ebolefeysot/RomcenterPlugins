/*
  NES plug-in for RomCenter (http://www.romcenter.com)
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
#include "nes.h"


//#define DEBUG
#define CONFIG_FILE_NAME "nes.cfg"
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

enum { INES_S = 0, UNIF_S, FFE_S, FDS_S, FAM_S,
       PASOFAMI_PRM_S, PASOFAMI_PRG_S, PASOFAMI_CHR_S };
static char returnstr[500], suffixes[8][300];
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

      get_property (config_file, "ines_suffix", suffixes[INES_S], ".nes");
      get_property (config_file, "unif_suffix", suffixes[UNIF_S], ".unf");
      get_property (config_file, "pasofami_prm_suffix", suffixes[PASOFAMI_PRM_S], ".prm");
      get_property (config_file, "pasofami_prg_suffix", suffixes[PASOFAMI_PRG_S], ".prg");
      get_property (config_file, "pasofami_chr_suffix", suffixes[PASOFAMI_CHR_S], ".chr");
      get_property (config_file, "ffe_suffix", suffixes[FFE_S], ".ffe");
      get_property (config_file, "fds_suffix", suffixes[FDS_S], ".fds");
      get_property (config_file, "fam_suffix", suffixes[FAM_S], ".fam");

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
  return "NES CRC-32 calculator";
}


#ifdef  RC261_COMPATIBILITY
#ifdef  _MSC_VER
__declspec (dllexport)
#endif
const char * __cdecl
/*
  Yes, this is not correct, but it works. When using __stdcall, link.exe finds
  a conflict with the GetVersion() in kernel32.dll.
  I tried to specify the decorated name in nes.def, but then the plugin tester
  can't find the function. Specifying the name on the command line gives the
  same results.
  I also tried to specify an alias in nes.def, but then the plugin tester
  can't load the DLL.
  Specifying the name of this function in nes.def will always cause an error,
  but the name has to be exported. That's why I use "__declspec (dllexport)" in
  the source code.
*/
#else
const char * __stdcall 
#endif
rc_GetVersion (void)
{
  return "1.1 " COMPILER;
}


const char * __stdcall 
rc_GetDescription (void)
{
  return "NES CRC-32 calculator. Based on NES code of uCON64"
         " (http://ucon64.sf.net). Detects files in"
         " iNES, UNIF, Pasofami, FFE, FDS and FAM format.";
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
  char *str;
  (void) crc32_in_zip;
  
#if 0 // I don't know what the minimum file size is
  // Check file size
  if (ucon64.file_size < 8192)
    {
      *comment = (char *) "File is too small for a NES ROM dump.";
      return 0;
    }
#endif

  ucon64.interleaved =
  ucon64.buheader_len =
  ucon64.console = UCON64_UNKNOWN;
  ucon64.crc32 = 0;
  memset (&rominfo, 0, sizeof (rominfo));
  rominfo.data_size = UCON64_UNKNOWN;

  if (nes_init (&rominfo, comment) != 0)
    if (*comment == NULL) // if some error occurred in nes_init() => comment != NULL
      *comment = "File was not detected as NES ROM dump.";
  
  if (ucon64.crc32 == 0)
    if (ucon64.file_size <= MAXROMSIZE)
      ucon64.crc32 = q_fcrc32 (ucon64.rom, rominfo.buheader_len);
      
  sprintf (crc32, "%08x", ucon64.crc32);

  switch (nes_get_file_type ())
    {
    case INES:
      *suffix = suffixes[INES_S];
      break;
    case UNIF:
      *suffix = suffixes[UNIF_S];
      break;
    default:                                    // falling through
    case PASOFAMI:
      str = (char *) get_suffix (ucon64.rom);
      if (!stricmp (str, suffixes[PASOFAMI_PRM_S]) ||
          !stricmp (str, suffixes[PASOFAMI_PRG_S]) ||
          !stricmp (str, suffixes[PASOFAMI_CHR_S]))
        *suffix = str;
      else
        *suffix = ".pas";
      break;
    case FFE:
      *suffix = suffixes[FFE_S];
      break;
    case FDS:
      *suffix = suffixes[FDS_S];
      break;
    case FAM:                                   
      *suffix = suffixes[FAM_S];
      break;
    }

  *file_size = UCON64_ISSET (rominfo.data_size) ?
    rominfo.data_size : ucon64.file_size - rominfo.buheader_len;

  return 1;
}
