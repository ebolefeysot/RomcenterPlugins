/* CCITT CRC-32 */


#include "types.h"

UINT32 crcTable[256];


UINT32 MemGetCRC( UINT8 *cImage, tgd_size_t size, UINT32 crcStart ) {
	register UINT32 crc;		// could use crcStart ^= 0xFFFFFFFF instead
	tgd_size_t		x;

	crc = crcStart^0xFFFFFFFF;	// precondition

	for(x=0; x<size ; x++)		//need to test for valid memory pointer?
		crc = ((crc>>8) & 0x00FFFFFF) ^ crcTable[ (crc^cImage[x]) & 0xFF ];
	return( crc^0xFFFFFFFF );	// postcondition
}


void crcgen( void ) {
	UINT32 crc, poly;
	INT32     i, j;

	poly = 0xEDB88320L;
	for (i=0; i<256; i++) {
		crc = i;
		for (j=8; j>0; j--) {
			if (crc&1) {
				crc = (crc >> 1) ^ poly;
			} else {
				crc >>= 1;
			}
		}
		crcTable[i] = crc;
	}
}
