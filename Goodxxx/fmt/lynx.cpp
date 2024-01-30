// Lynx

char cPluginName[] = "Lynx";
char cDescription[] = "";
char cVersion[] = "1.0";
char *ext[] = { ".lnx" };


int convert_file()
{
// BEGIN

		if (iImageSize > 64) {
			pDataSize -= 64;
			pData = &pDynMem[64];
		}

// END
	pExt = ext[0];
	return 0;
}
