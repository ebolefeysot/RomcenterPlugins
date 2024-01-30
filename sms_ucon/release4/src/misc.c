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
#ifdef  HAVE_CONFIG_H
#include "config.h"                             // HAVE_ZLIB_H
#endif
#include <stdlib.h>
#ifdef  HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <stdio.h>
#include <string.h>
#include <ctype.h>

#ifdef  HAVE_ZLIB_H
#include "miscz.h"
#endif
#include "misc.h"


#define PROPERTY_SEPARATOR '='


#ifndef HAVE_ZLIB_H
/*
  crc32 routines
*/
#define CRC32_POLYNOMIAL 0xedb88320

static unsigned int crc32_table[256];
static int crc32_table_set = 0;

void
init_crc_table (void *table, unsigned int polynomial)
{
  unsigned int crc, i, j;

  for (i = 0; i < 256; i++)
    {
      crc = i;
      for (j = 8; j > 0; j--)
        if (crc & 1)
          crc = (crc >> 1) ^ polynomial;
        else
          crc >>= 1;

      ((unsigned int *) table)[i] = crc;
    }
}


unsigned int
crc32 (unsigned int crc, const void *buffer, unsigned int size)
{
  unsigned char *p = (unsigned char *) buffer;

  if (!crc32_table_set)
    {
      init_crc_table (crc32_table, CRC32_POLYNOMIAL);
      crc32_table_set = 1;
    }

  crc = ~crc;
  while (size--)
    crc = (crc >> 8) ^ crc32_table[(crc ^ *p++) & 0xff];
  return ~crc;
}
#endif


const void *
memmem2 (const void *buffer, size_t bufferlen,
         const void *search, size_t searchlen)
{
  size_t i;

  if (bufferlen >= searchlen)
    for (i = 0; i <= bufferlen - searchlen; i++)
      if (!memcmp ((const unsigned char *) buffer + i, search, searchlen))
        return (const unsigned char *) buffer + i;

  return NULL;
}


#ifdef  __CYGWIN__
/*
  Weird problem with combination Cygwin uCON64 exe and cmd.exe (Bash is ok):
  When a string with "e (e with diaeresis, one character) is read from an
  environment variable, the character isn't the right character for accessing
  the file system. We fix this.
  TODO: fix the same problem for other non-ASCII characters (> 127).
*/
char *
fix_character_set (char *str)
{
  int n, l = strlen (str);
  unsigned char *ptr = (unsigned char *) str;

  for (n = 0; n < l; n++)
    {
      if (ptr[n] == 0x89)                       // e diaeresis
        ptr[n] = 0xeb;
      else if (ptr[n] == 0x84)                  // a diaeresis
        ptr[n] = 0xe4;
      else if (ptr[n] == 0x8b)                  // i diaeresis
        ptr[n] = 0xef;
      else if (ptr[n] == 0x94)                  // o diaeresis
        ptr[n] = 0xf6;
      else if (ptr[n] == 0x81)                  // u diaeresis
        ptr[n] = 0xfc;
    }

  return str;
}
#endif


char *
getenv2 (const char *variable)
/*
  getenv() suitable for enviroments w/o HOME, TMP or TEMP variables.
  The caller should copy the returned string to it's own memory, because this
  function will overwrite that memory on the next call.
  Note that this function never returns NULL.
*/
{
  char *tmp;
  static char value[MAXBUFSIZE];
#if     defined __CYGWIN__ || defined __MSDOS__
/*
  Under DOS and Windows the environment variables are not stored in a case
  sensitive manner. The run-time systems of DJGPP and Cygwin act as if they are
  stored in upper case. Their getenv() however *is* case sensitive. We fix this
  by changing all characters of the search string (variable) to upper case.

  Note that under Cygwin's Bash environment variables *are* stored in a case
  sensitive manner.
*/
  char tmp2[MAXBUFSIZE];

  strcpy (tmp2, variable);
  variable = strupr (tmp2);                     // DON'T copy the string into variable
#endif                                          //  (variable itself is local)

  *value = 0;

  if ((tmp = getenv (variable)) != NULL)
    strcpy (value, tmp);
  else
    {
      if (!strcmp (variable, "HOME"))
        {
          if ((tmp = getenv ("USERPROFILE")) != NULL)
            strcpy (value, tmp);
          else if ((tmp = getenv ("HOMEDRIVE")) != NULL)
            {
              strcpy (value, tmp);
              tmp = getenv ("HOMEPATH");
              strcat (value, tmp ? tmp : FILE_SEPARATOR_S);
            }
          else
            /*
              Don't just use C:\\ under DOS, the user might not have write access
              there (Windows NT DOS-box). Besides, it would make uCON64 behave
              differently on DOS than on the other platforms.
              Returning the current directory when none of the above environment
              variables are set can be seen as a feature. A frontend could execute
              uCON64 with an environment without any of the environment variables
              set, so that the directory from where uCON64 starts will be used.
            */
            {
              char c;
              getcwd (value, FILENAME_MAX);
              c = toupper (*value);
              // if current dir is root dir strip problematic ending slash (DJGPP)
              if (c >= 'A' && c <= 'Z' &&
                  value[1] == ':' && value[2] == '/' && value[3] == 0)
                value[2] = 0;
            }
         }

      if (!strcmp (variable, "TEMP") || !strcmp (variable, "TMP"))
        {
#if     defined __MSDOS__ || defined __CYGWIN__
          /*
            DJGPP and (yet another) Cygwin quirck
            A trailing backslash is used to check for a directory. Normally
            DJGPP's run-time system is able to handle forward slashes in paths,
            but access() won't differentiate between files and dirs if a
            forward slash is used. Cygwin's run-time system seems to handle
            paths with forward slashes quite different from paths with
            backslashes. This trick seems to work only if a backslash is used.
          */
          if (access ("\\tmp\\", R_OK | W_OK) == 0)
#else
          // trailing file separator to force it to be a directory
          if (access (FILE_SEPARATOR_S"tmp"FILE_SEPARATOR_S, R_OK | W_OK) == 0)
#endif
            strcpy (value, FILE_SEPARATOR_S"tmp");
          else
            getcwd (value, FILENAME_MAX);
        }
    }

#ifdef  __CYGWIN__
  /*
    Under certain circumstances Cygwin's run-time system returns "/" as value
    of HOME while that var has not been set. To specify a root dir a path like
    /cygdrive/<drive letter> or simply a drive letter should be used.
  */
  if (!strcmp (variable, "HOME") && !strcmp (value, "/"))
    getcwd (value, FILENAME_MAX);

  return fix_character_set (value);
#else
  return value;
#endif
}


char *
get_property (const char *filename, const char *propname, char *buffer,
              const char *def)
{
  char line[MAXBUFSIZE], *p = NULL;
  FILE *fh;
  int prop_found = 0, i, whitespace_len;

  if ((fh = fopen (filename, "r")) != 0)        // opening the file in text mode
    {                                           //  avoids trouble under DOS
      while (fgets (line, sizeof line, fh) != NULL)
        {
          whitespace_len = strspn (line, "\t ");
          p = line + whitespace_len;            // ignore leading whitespace
          if (*p == '#' || *p == '\n' || *p == '\r')
            continue;                           // text after # is comment
          if ((p = strpbrk (line, "#\r\n")))    // strip *any* returns
            *p = 0;

          p = strchr (line, PROPERTY_SEPARATOR);
          // if no divider was found the propname must be a bool config entry
          //  (present or not present)
          if (p)
            *p = 0;                             // note that this "cuts" _line_
          // strip trailing whitespace from property name part of line
          for (i = strlen (line) - 1;
               i >= 0 && (line[i] == '\t' || line[i] == ' ');
               i--)
            ;
          line[i + 1] = 0;

          if (!stricmp (line + whitespace_len, propname))
            {
              if (p)
                {
                  p++;
                  // strip leading whitespace from value
                  strcpy (buffer, p + strspn (p, "\t "));
                  // strip trailing whitespace from value
                  for (i = strlen (buffer) - 1;
                       i >= 0 && (buffer[i] == '\t' || buffer[i] == ' ');
                       i--)
                    ;
                  buffer[i + 1] = 0;
                }
              prop_found = 1;
              break;                            // an environment variable
            }                                   //  might override this
        }
      fclose (fh);
    }

  p = getenv2 (propname);
  if (*p == 0)                                  // getenv2() never returns NULL
    {
      if (!prop_found)
        {
          if (def)
            strcpy (buffer, def);
          else
            buffer = NULL;                      // buffer won't be changed
        }                                       //  after this func (=ok)
    }
  else
    strcpy (buffer, p);
  return buffer;
}


int
q_fcrc32 (const char *filename, int start)
{
  unsigned int n, crc = 0;                      // don't name it crc32 to avoid
  unsigned char buffer[MAXBUFSIZE];             //  name clash with zlib's func
  FILE *fh = fopen (filename, "rb");

  if (!fh)
    return -1;

  fseek (fh, start, SEEK_SET);

  while ((n = fread (buffer, 1, MAXBUFSIZE, fh)))
    crc = crc32 (crc, buffer, n);

  fclose (fh);

  return crc;
}


#if     defined __MINGW32__ && defined DLL
// Ugly hack in order to fix something in zlib (yep, it's that bad)
FILE *
fdopen (int fd, const char *mode)
{
  return _fdopen (fd, mode);
}
#endif
