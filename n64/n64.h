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

#ifndef N64_H
#define N64_H

typedef enum { V64, Z64 } n64_file_t;

extern n64_file_t n64_get_file_type (void);
extern int n64_init (st_rominfo_t *rominfo, char **comment);

#endif // N64_H
