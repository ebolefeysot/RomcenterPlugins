#define RCPLUGINS_API extern "C" __declspec(dllexport)

RCPLUGINS_API char *GetAuthor(void *p);
//	return address of embedded author string

RCPLUGINS_API char *GetDescription(void *p);
//	return address of embedded text description

RCPLUGINS_API char *GetDllInterfaceVersion(void *p);
//	return address of embedded interface version ("2.50")

RCPLUGINS_API char *GetEmail(void *p);
//	return address of embedded author's email address

RCPLUGINS_API char *GetVersion(void *p);
//	return address of embedded plugin version

RCPLUGINS_API char *GetWebPage(void *p);
//	return address of embedded author's web page

RCPLUGINS_API char *GetDllType(void *p);
//

RCPLUGINS_API char *GetPlugInName(void *p);
//

RCPLUGINS_API char *GetSignature(char *name, char *zipcrc, char **format, __int64 *size, char **comment, char **errmsg);
//
