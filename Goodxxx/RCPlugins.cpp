// variable argument lists
#include <stdarg.h>
#include <stdio.h>
#include "types.h"
#include "crc32.h"


#define COMMENT_LEN 261


// char cPluginName[]
char cSignature[] =   "romcenter signature calculator";
char cAuthor[] =      "Codeine";
char cIntfVersion[] = "2.50";
char cEmail[] =       "";
char cWebPage[] =     "";

// internal data
char cCRC32[9];
char *pExt;
char *pComment;



// TUGID conversion helpers
#define TGD_CNV(fn) tmp = fn (pDynMem, &iImageSize)
#define CNVPROC(fn) UINT8* fn ( UINT8 *raw, tgd_size_t *len)

unsigned char *pDynMem, *pData;
unsigned iImageSize, pDataSize;
UINT8 *tmp;


// header files for conversion utils
#include <memory.h>
#include <string.h>


// specific functions are in fmt/ files
#include "fmt/snes.cpp"



// winnt.h
#define DLL_PROCESS_ATTACH 1    
#define DLL_THREAD_ATTACH  2    
#define DLL_THREAD_DETACH  3    
#define DLL_PROCESS_DETACH 0    

bool __stdcall DllMain( void *hModule, 
                       unsigned  ul_reason_for_call, 
                       void * lpReserved
					 )
{
	if ( ul_reason_for_call == DLL_PROCESS_ATTACH )
		crcgen();

    return true;
}


#define RCPLUGINS_API extern "C" __declspec(dllexport)

//	return address of embedded author string
RCPLUGINS_API char *GetAuthor(void *p) {
	return cAuthor;
}

//	return address of embedded text description
RCPLUGINS_API char *GetDescription(void *p) {
	return cDescription;
}

//	return address of embedded interface version ("2.50")
RCPLUGINS_API char *GetDllInterfaceVersion(void *p) {
	return cIntfVersion;
}

//	return address of embedded author's email address
RCPLUGINS_API char *GetEmail(void *p) {
	return cEmail;
}

//	return address of embedded plugin version
RCPLUGINS_API char *GetVersion(void *p) {
	return cVersion;
}

//	return address of embedded author's web page
RCPLUGINS_API char *GetWebPage(void *p) {
	return cWebPage;
}

//
RCPLUGINS_API char *GetDllType(void *p) {
	return cSignature;
}

//
RCPLUGINS_API char *GetPlugInName(void *p) {
	return cPluginName;
}

//function GetSignature(filename: PChar; ZipCrc: PChar; var format:pchar; var size:int64;
//                      var comment:pchar; var ErrorMsg:pchar): PChar; stdcall;

// return address of text crc32, lower case
RCPLUGINS_API char *GetSignature(char *name, char *zipcrc, char **format, __int64 *size, char **comment, char **errmsg) {


	// initialization
	*errmsg = NULL;
	*comment = NULL;

	// open file
	FILE *fp = fopen (name, "rb");
	if (!fp)
		return NULL;

	fseek (fp, 0, SEEK_END);
	iImageSize = ftell (fp);
	fseek (fp, 0, SEEK_SET);

	// allocate pDynMem
	pData = pDynMem = new UINT8[iImageSize];
	fread (pDynMem, 1, iImageSize, fp);



	// call function
	tmp = NULL;
	pDataSize = iImageSize;
	convert_file();

	*size = pDataSize;


	// calc CRC32 using pData and iImageSize
	UINT32 crc = MemGetCRC (pData, pDataSize, 0);


	// free pDynMem and close file
	delete [] pDynMem;
	fclose (fp);

	// set data
	*format = pExt;
	if (pComment)
		*comment = pComment;

#ifdef SNES_DLL
	encode_crc32 (cCRC32, crc);
#else
	sprintf (cCRC32, "%08x", crc);
#endif

	return cCRC32;
}
