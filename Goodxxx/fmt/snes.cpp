// SNES

char cPluginName[] = "Super Nintendo";
char cDescription[] = "";
char cVersion[] = "1.1";
char *ext[] = {".bin", ".fig", ".swc", ".078", ".smc"};


// swteche.txt
enum ffe_type {
	ffe_null = 0,
	fe_unused, 
	ffe_pce, // '02': MAGIC GRIFFIN GAME FILE. (PC ENGINE)
	ffe_pces,// '03': MAGIC GRIFFIN SRAM DATA FAILE.
	ffe_sXc, // '04': SWC&SMC GAME FILE. (SUPER MAGICOM)
	ffe_sXcs,// '05': SWC&SMC PASSWORD, SRAM DATA, SAVER DATA FILE.
	ffe_smd, // '06': SMD GAME FILE. (MEGA DRIVE)
	ffe_smds,// '07': SMD SRAM DATA FILE.
};


int convert_file()
{
// BEGIN
/*
		if ( (iImageSize > 0x200) && (iImageSize % 0x400 == 0x200) ) {
			pDataSize -= 0x200;
			pData = &pDynMem[0x200];
			goto cnv_finish;
		}
*/

		if ( iImageSize > 0x200) {

			// FFE
			if ( pDynMem[8] == 0xAA && pDynMem[9] == 0xBB ) {
				if (pDynMem[10] == ffe_sXc)
					pExt = ext[2];
				else
					pExt = ext[0];
				
				pDataSize -= 0x200;
				pData = &pDynMem[0x200];
			} else if (( pDynMem[5] == 0x82 || pDynMem[5] == 0x83) &&
				(pDynMem[4] == 0xFD || pDynMem[4] == 0x47 || pDynMem[4] == 0x77 )) {
					pExt = ext[1];
			} else {
				// check for 254 NULL bytes in a row, if so:
//				pDataSize -= 0x200;
//				pData = &pDynMem[0x200];
			}
		}

	pExt = ext[0];

cnv_finish:


// END		
	return 0;
}

