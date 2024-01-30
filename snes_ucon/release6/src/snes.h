/*
  SNES plug-in for RomCenter (http://www.romcenter.com)
  Copyright (c) 2003, 2005 dbjh

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/
#ifndef SNES_H
#define SNES_H

typedef enum { SWC = 1, GD3, UFO, FIG, MGD_SNES, SMC } snes_file_t;

extern snes_file_t snes_get_file_type (void);
extern int snes_init (st_rominfo_t *rominfo, char **comment);

#endif
