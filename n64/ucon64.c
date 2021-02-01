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

#include "ucon64.h"

st_ucon64_t ucon64;

const char *ucon64_msg[] = {
  "ERROR: Can't open \"%s\" for reading\n",
  "ERROR: Can't read from \"%s\"\n",
  "ERROR: Not enough memory for buffer (%d bytes)\n",
  "ERROR: Not enough memory for ROM buffer (%d bytes)\n",
  "ERROR: Not enough memory for file buffer (%d bytes)\n",
  0
};
