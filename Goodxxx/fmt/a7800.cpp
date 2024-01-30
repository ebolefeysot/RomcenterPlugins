// Atari 7800

char cPluginName[] = "Atari 7800";
char cDescription[] = "";
char cVersion[] = "1.1";
char *ext[] = { ".a78" };


int convert_file()
{
// BEGIN
#if 0
		char cA7800[] = "ACTUAL CART DATA STARTS HERE";

		if( iImageSize > (0x64 + sizeof(cA7800)) ) {
			if( !strncmp( (char *) &pDynMem[0x64], cA7800, sizeof(cA7800)-1 )) {
				pDataSize -= 128;
				pData = &pDynMem[128];
			}
		}
#else
		char cA7800[] = "ATARI7800";

		if( iImageSize > 128 ) {
			if( !strncmp( (char *) &pDynMem[1], cA7800, sizeof(cA7800)-1 ))
				memset (&pDynMem[11], 0, 32);
		}

#endif
// END		
	pExt = ext[0];
	return 0;
}
