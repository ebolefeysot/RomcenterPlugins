// Genesis

char cPluginName[] = "Genesis";
char cDescription[] = "";
char cVersion[] = "1.1";
char *ext[] = { ".bin", ".smd" };

/*

The following are not recognized based on goodgen,
the last two have bad file sizes and probably require hacks to recognize.

UNK: Game Genie (JUE) [c][!].zip
UNK: Genesis O.S. ROM (U).zip
UNK: Sample Program - Indian Picture (PD).zip
UNK: Sonic the Hedgehog (beta).zip

*/

#define BLOCKSIZE 16*1024

CNVPROC(unsmd) {

	unsigned char *block = raw;
	unsigned char *raw1 = raw + 512;
	unsigned char *raw2 = (unsigned char *) &raw[BLOCKSIZE/2] + 512;		// skip initial header
	tgd_size_t imglen = *len;

//	if( raw[1] != 0x3 || raw[8] != 0xAA || raw[9] != 0xBB ) {
//		return 0;
//	} else
	{
		unsigned char *BINblock = new unsigned char[BLOCKSIZE];
		imglen -= 512;					// ignore header
		imglen /= BLOCKSIZE;			// number of blocks
		for( unsigned iBlockNum = 0; iBlockNum < imglen; iBlockNum++) {
			for( int iOffset = 0; iOffset < BLOCKSIZE; ) {			// iOffset is increased in assignments
				BINblock[iOffset++] = *raw2++;
				BINblock[iOffset++] = *raw1++;
			}
			memcpy( block, BINblock, BLOCKSIZE );
			block += BLOCKSIZE;			// block = BLOCKSIZE * iBlockNum;
			raw1 += BLOCKSIZE/2;		// raw1 = block;
			raw2 += BLOCKSIZE/2;		// raw2 = block + BLOCKSIZE / 2;
		}
		delete [] BINblock;
		*len -= 512;
	}

	// return NULL for in-place conversion
	return NULL;
}


int convert_file()
{
	pExt = ext[1];
// BEGIN

		bool convert;

		convert = false;

		if ( iImageSize > 0x2281) {
			if ((pDynMem[0x100] == 'S') && (pDynMem[0x101] == 'E') && (pDynMem[0x102] == 'G') && (pDynMem[0x103] == 'A'))
				convert = false;
			else if ((pDynMem[0x2280] == 'S') && (pDynMem[0x280] == 'E') && (pDynMem[0x2281] == 'G') && (pDynMem[0x281] == 'A'))
				convert = true;
			else if ((pDynMem[0x08] == 0xAA) && (pDynMem[0x09] == 0xBB))
					convert = true;
			else if (iImageSize % 0x400 == 0x200) // catch Omega Race
					convert = true;
		}

		if (convert) {
			TGD_CNV(unsmd);
			pDataSize = iImageSize;
			goto cnv_finish;
		}

		// goodgen ignores extra data
		if (iImageSize > 0x4000) {
			iImageSize /= 0x4000;
			iImageSize *= 0x4000;
			pDataSize = iImageSize;
		}

	pExt = ext[0];

cnv_finish:
//	pData = pDynMem;

// END		
	return 0;
}

/*

goodgen makes the following checks:

ROM[100] = S
ROM[101] = E
ROM[102] = G
ROM[103] = A

interleaved:
ROM[2080] = S
ROM[80]   = E
ROM[2081] = G
ROM[81]   = A

??
ROM[2080] = ' '
ROM[80]   = S
ROM[2081] = E
ROM[81]   = G


special checks:
ROM[90] = "OEARC   " - Omega Race (interleaved w/NULL header)
ROM[6708] = " NTEBDKN"
ROM[2C0] = "so fCXP"
? = "sio-Wyo "
ROM[88] = "SS  CAL "
ROM[3648] = "SG NEPIE"
ROM[1ceb] = "@TTI>"
ROM[16D4] = "PPLTO"
ROM[50] = "DaeDsge"
ROM[BB8] = "ISOE "
ROM[A3] = "IHSRSRE FGMS"
ROM[80] = "EIETAUOSC"
*/
