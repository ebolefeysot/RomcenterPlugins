// SMS, SFC, PCE

char cPluginName[] = "Super Nintendo";
char cDescription[] = "";
char cVersion[] = "1.0";
char *ext[] = { NULL, ".smc", ".sfc", ",swc", ".058", ".078"};


int convert_file()
{
	int ext = 0;
// BEGIN

		if ( (iImageSize > 0x200) && (iImageSize % 0x400 == 0x200) ) {
			pDataSize -= 0x200;
			pData = &pDynMem[0x200];
			goto cnv_finish;
		}

cnv_finish:
// END		
	pExt = ext[ext];
	return 0;
}
