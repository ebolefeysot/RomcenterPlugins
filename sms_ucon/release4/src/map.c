/*
  SMS plug-in for RomCenter (http://www.romcenter.com)
  Copyright (c) 2002, 2003, 2005 dbjh

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
#include "map.h"
#if     defined DJGPP && defined DLL
#include "dxedll_priv.h"
#endif


st_map_t *
map_create (int n_elements)
{
  st_map_t *map;
  int size = sizeof (st_map_t) + n_elements * sizeof (st_map_element_t);

  if ((map = (st_map_t *) malloc (size)) == NULL)
    {
      fprintf (stderr, "ERROR: Not enough memory for buffer (%d bytes)\n", size);
      exit (1);
    }
  map->data = (st_map_element_t *) (((unsigned char *) map) + sizeof (st_map_t));
  memset (map->data, MAP_FREE_KEY, n_elements * sizeof (st_map_element_t));
  map->size = n_elements;
  map->cmp_key = map_cmp_key_def;
  return map;
}


st_map_t *
map_resize (st_map_t *map, int n_elements)
{
  int size = sizeof (st_map_t) + n_elements * sizeof (st_map_element_t);

  if ((map = (st_map_t *) realloc (map, size)) == NULL)
    {
      fprintf (stderr, "ERROR: Not enough memory for buffer (%d bytes)\n", size);
      exit (1);
    }
  map->data = (st_map_element_t *) (((unsigned char *) map) + sizeof (st_map_t));
  if (n_elements > map->size)
    memset (((unsigned char *) map->data) + map->size * sizeof (st_map_element_t),
            MAP_FREE_KEY, (n_elements - map->size) * sizeof (st_map_element_t));
  map->size = n_elements;
  return map;
}


void
map_copy (st_map_t *dest, st_map_t *src)
{
  memcpy (dest->data, src->data, src->size * sizeof (st_map_element_t));
  dest->cmp_key = src->cmp_key;
}


int
map_cmp_key_def (void *key1, void *key2)
{
  return key1 != key2;
}


st_map_t *
map_put (st_map_t *map, void *key, void *object)
{
  int n = 0;

  while (n < map->size && map->data[n].key != MAP_FREE_KEY &&
         map->cmp_key (map->data[n].key, key))
    n++;

  if (n == map->size)                           // current map is full
    map = map_resize (map, map->size + 20);

  map->data[n].key = key;
  map->data[n].object = object;

  return map;
}


void *
map_get (st_map_t *map, void *key)
{
  int n = 0;

  while (n < map->size && (map->data[n].key == MAP_FREE_KEY ||
         map->cmp_key (map->data[n].key, key)))
    n++;

  if (n == map->size)
    return NULL;

  return map->data[n].object;
}


void
map_del (st_map_t *map, void *key)
{
  int n = 0;

  while (n < map->size && (map->data[n].key == MAP_FREE_KEY ||
         map->cmp_key (map->data[n].key, key)))
    n++;

  if (n < map->size)
    map->data[n].key = MAP_FREE_KEY;
}


void
map_dump (st_map_t *map)
{
  int n = 0;

  while (n < map->size)
    {
      printf ("%p -> %p\n", map->data[n].key, map->data[n].object);
      n++;
    }
}
