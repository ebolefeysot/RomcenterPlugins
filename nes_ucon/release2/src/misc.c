/*
  misc.c - miscellaneous functions
  
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

#ifdef  HAVE_CONFIG_H
#include "config.h"                             // HAVE_ZLIB_H
#endif
#include <stddef.h>
#include <stdlib.h>
#include <ctype.h>
#ifdef  HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <stdio.h>
#include <string.h>
#include <errno.h>
#include <time.h>

#ifdef  HAVE_ZLIB_H
#include "miscz.h"
#endif
#include "misc.h"


extern int errno;


#ifndef HAVE_ZLIB_H
/*
  crc32 routines
*/
#define CRC32_POLYNOMIAL     0xedb88320

static unsigned int crc32_table[256];
static int crc32_table_built = 0;

void
build_crc32_table (void)
{
  unsigned int crc32, i, j;

  for (i = 0; i <= 255; i++)
    {
      crc32 = i;
      for (j = 8; j > 0; j--)
        {
          if (crc32 & 1)
            crc32 = (crc32 >> 1) ^ CRC32_POLYNOMIAL;
          else
            crc32 >>= 1;
        }
      crc32_table[i] = crc32;
    }
  crc32_table_built = 1;
}


unsigned int
crc32 (unsigned int crc32, const void *buffer, unsigned int size)
{
  unsigned char *p;

  if (!crc32_table_built)
    build_crc32_table ();

  crc32 ^= 0xffffffff;
  p = (unsigned char *) buffer;
  while (size-- != 0)
    crc32 = (crc32 >> 8) ^ crc32_table[(crc32 ^ *p++) & 0xff];
  return crc32 ^ 0xffffffff;
}


int
q_fsize (const char *filename)
{
  struct stat fstate;

  if (!stat (filename, &fstate))
    return fstate.st_size;

  errno = ENOENT;
  return -1;
}
#endif


int
is_func (char *s, int size, int (*func) (int))
{
  char *p = s;

  /*
    Casting to unsigned char * is necessary to avoid differences between the
    different compilers' run-time environments. At least for isprint(). Without
    the cast the isprint() of (older versions of) DJGPP, MinGW, Cygwin and
    Visual C++ returns nonzero values for ASCII characters > 126.
  */
  for (; size >= 0; p++, size--)
    if (!func (*(unsigned char *) p))
      return FALSE;

  return TRUE;
}


char *
set_suffix (char *filename, const char *suffix)
{
  char suffix2[FILENAME_MAX], *p, *p2;

  if (!(p = basename (filename)))
    p = filename;
  if ((p2 = strrchr (p, '.')))
    if (p2 != p)                                // files can start with '.'
      *p2 = 0;

  strcpy (suffix2, suffix);
  strcat (filename, is_func (p, strlen (p), isupper) ? strupr (suffix2) : strlwr (suffix2));

  return filename;
}


const char *
get_suffix (const char *filename)
// Note that get_suffix() never returns NULL. Other code relies on that!
{
  char *p, *p2;

  if (!(p = basename (filename)))
    p = (char *) filename;
  if (!(p2 = strrchr (p, '.')))
    p2 = "";
  if (p2 == p)
    p2 = "";                                    // files can start with '.'; be
                                                //  consistent with set_suffix[_i]()
  return p2;
}


char *
basename2 (const char *path)
// basename() clone (differs from Linux's basename())
{
  char *p1;
#if     defined DJGPP || defined __CYGWIN__
  char *p2;
#endif

  if (path == NULL)
    return NULL;

#if     defined DJGPP || defined __CYGWIN__
  // Yes, DJGPP, not __MSDOS__, because DJGPP's basename() behaves the same
  // Cygwin has no basename()
  p1 = strrchr (path, '/');
  p2 = strrchr (path, '\\');
  if (p2 > p1)                                  // use the last separator in path
    p1 = p2;
#else
  p1 = strrchr (path, FILE_SEPARATOR);
#endif
#if     defined DJGPP || defined __CYGWIN__ || defined _WIN32
  if (p1 == NULL)                               // no slash, perhaps a drive?
    p1 = strrchr (path, ':');
#endif

  return p1 ? p1 + 1 : (char *) path;
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


/*
  getenv() suitable for enviroments w/o HOME, TMP or TEMP variables.
  The caller should copy the returned string to it's own memory, because this
  function will overwrite that memory on the next call.
  Note that this function never returns NULL.
*/
char *
getenv2 (const char *variable)
{
  char *tmp;
  static char value[MAXBUFSIZE];
#if     defined __CYGWIN__ || defined __MSDOS__
/*
  Under DOS and Windows the environment variables are not stored in a case
  sensitive manner. The runtime systems of DJGPP and Cygwin act as if they are
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
            DJGPP's runtime system is able to handle forward slashes in paths,
            but access() won't differentiate between files and dirs if a
            forward slash is used. Cygwin's runtime system seems to handle
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
    Under certain circumstances Cygwin's runtime system returns "/" as value of
    HOME while that var has not been set. To specify a root dir a path like
    /cygdrive/<drive letter> or simply a drive letter should be used.
  */
  if (!strcmp (variable, "HOME") && !strcmp (value, "/"))
    getcwd (value, FILENAME_MAX);

  return fix_character_set (value);
#else
  return value;
#endif
}


static const char *
get_property2 (const char *filename, const char *propname, char divider, char *buffer, const char *def)
// divider is the 1st char after propname ('=', ':', etc..)
{
  char line[MAXBUFSIZE], *p = NULL;
  FILE *fh;
  int prop_found = 0, i;

  if ((fh = fopen (filename, "r")) != 0)        // opening the file in text mode
    {                                           //  avoids trouble under DOS
      while (fgets (line, sizeof line, fh) != NULL)
        {
          p = line + strspn (line, "\t ");
          if (*p == '#' || *p == '\n' || *p == '\r')
            continue;                           // text after # is comment
          if ((p = strpbrk (line, "\n\r#")))    // strip *any* returns
            *p = 0;

          if (!strnicmp (line, propname, strlen (propname)))
            {
              p = strchr (line, divider);
              if (p)                    // if no divider was found the propname must be
                {                       //  a bool config entry (present or not present)
                  p++;
                  strcpy (buffer, p + strspn (p, "\t "));
                  // strip trailing whitespace
                  for (i = strlen (buffer) - 1;
                       i >= 0 && (buffer[i] == '\t' || buffer[i] == ' ');
                       i--)
                    buffer[i] = 0;

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


const char *
get_property (const char *filename, const char *propname, char *buffer, const char *def)
{
  return get_property2 (filename, propname, '=', buffer, def);
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
