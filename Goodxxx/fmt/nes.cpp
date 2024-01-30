// NES

char cPluginName[] = "NES";
char cDescription[] = "";
char cVersion[] = "1.1";
char *ext[] = { ".nes", ".unif" };


int convert_file()
{
// BEGIN

		if (pDynMem[0] == 'N' && pDynMem[1] == 'E' && pDynMem[2] == 'S') {
			pDataSize -= 0x10;
			pData = &pDynMem[0x10];
		}

// END		
	pExt = ext[0];
	return 0;
}
