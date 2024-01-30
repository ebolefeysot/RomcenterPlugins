// SMS, SFC, PCE, GBX shared

char cPluginName[] = "GameBoy (GBX)";
char cDescription[] = "";
char cVersion[] = "1.0";
char *ext[] = { ".gb", ".gbc", ".sgb"};


int convert_file()
{
	enum { gb = 0, gbc, sgb };
	int type = gb;
// BEGIN

		if ( (iImageSize > 0x200) && (iImageSize % 0x400 == 0x200) ) {
			pDataSize -= 0x200;
			pData = &pDynMem[0x200];
			goto cnv_finish;
		}

cnv_finish:
	if( pData[0x146] == 0x03) {		// 3=sgb 0=gb
		if (pData[0x14B] == 0x33)	// licensee code, 33 = check 0x144/0x145
			type = sgb;
	}

	if( pData[0x143] == 0x80 )	// gbc flag
		type = gbc;

	if( pData[0x143] == 0xC0 )	// ????
		type = gbc;

// END		
	pExt = ext[type];
	return 0;
}
