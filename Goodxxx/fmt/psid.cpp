// Commodore SID (PlaySID format)

char cPluginName[] = "PSID";
char cDescription[] = "";
char cVersion[] = "1.0";
char *ext[] = { ".sid" };


int convert_file()
{
// BEGIN

		tgd_size_t tmp;
		tmp = 0;

		if (!memcmp(pDynMem, "PSID", 4)) {
			tmp = (pDynMem[6] << 8) | pDynMem[7];
//			ver = (pDynMem[4] << 8) | DynMem[5];
			// valid values for tmp are 0x76 ver 1, 0x7C ver 2
		}
		if (tmp < iImageSize) {
			pDataSize = iImageSize - tmp;
			pData = &pDynMem[tmp];
		}

// END
	pExt = ext[0];
	return 0;
}
