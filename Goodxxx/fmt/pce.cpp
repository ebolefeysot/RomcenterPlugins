// SMS, PCE, GBX shared.  SNES could use this but does not

char cPluginName[] = "PC Engine";
char cDescription[] = "";
char cVersion[] = "1.0";
char *ext[] = {".pce"}


int convert_file()
{
// BEGIN

		if ( (iImageSize > 0x200) && (iImageSize % 0x400 == 0x200) ) {
			pDataSize -= 0x200;
			pData = &pDynMem[0x200];
			goto cnv_finish;
		}

cnv_finish:
// END		
	pExt = ext[0];
	return 0;
}
