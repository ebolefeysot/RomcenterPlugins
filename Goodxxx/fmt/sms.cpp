// SMS, SNES, PCE, GBX shared

char cPluginName[] = "Sega Master System";
char cDescription[] = "";
char cVersion[] = "1.1";
char *ext[] = { ".sms", ".sf7"};	// .sc .sg


int convert_file()
{
// BEGIN

		if (pDataSize > 0x200 && pDynMem[1] == 0x01) {
			switch (pDynMem[0]) {
			case 0x01:
			case 0x02:
			case 0x10:
			case 0x20:
				pDataSize -= 0x200;
				pData = &pDynMem[0x200];
			}
		}

		if (!strncmp((const char *) pData, "SYS: ", 5))
			pExt = ext[1];
		else if (!strncmp((const char *) &pData[0xCA], "POSEIDON.HEX", 12))
			pExt = ext[1];
		else
			pExt = ext[0];

// END		
	return 0;
}
