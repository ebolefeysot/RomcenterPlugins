.PHONY: all clean

ZLIB=1

CC=gcc
CFLAGS=-Wall -W -O3 -DDLL -DHAVE_UNISTD_H
#-DHAVE_CONFIG_H
LDFLAGS=-s

LIBNAME=n64
OBJECTS=main.o n64.o ucon64.o misc.o
LIBS=-ladvapi32
ifdef ZLIB
LIBS+=-lz
CFLAGS+=-DHAVE_ZLIB_H
OBJECTS+=unzip.o map.o miscz.o
endif

ifeq ($(TERM),cygwin)

GCCA_DIR=/lib/gcc-lib/i686-pc-cygwin/3.2/
LIBS+=-L$(GCCA_DIR) -lgcc -lcygwin -lkernel32

FULLLIBNAME=$(LIBNAME).dll
DLLFLAGS=$(LDFLAGS) --dll $(OBJECTS) $(LIBS) -e __cygwin_dll_entry@12 -o $(LIBNAME).dll
DLLTOOLFLAGS=-d tmp.def -b tmp.base -e tmp.exp -D $(LIBNAME).dll

else
ifeq ($(OSTYPE),msys)

GCCA_DIR=/mingw/lib/gcc-lib/mingw32/3.2.3/
LIBS+=-L/mingw/lib -lkernel32 -lmsvcrt -L$(GCCA_DIR) -lgcc
# Msys problem: Specifying the library directory is necessary when compiling on
# a different filesystem than the filesystem that MinGW is installed on.

FULLLIBNAME=$(LIBNAME).dll
DLLFLAGS=$(LDFLAGS) --dll $(OBJECTS) $(LIBS) -e _DllMain@12 -o $(LIBNAME).dll
DLLTOOLFLAGS=-d tmp.def -b tmp.base -e tmp.exp -D $(LIBNAME).dll

endif                                   # msys
endif                                   # cygwin


all: $(FULLLIBNAME)


clean:
	rm -f $(LIBNAME).dll $(LIBNAME).a $(OBJECTS) *.core *.stackdump *.o \
        tmp.def tmp.base tmp.exp


.c.o:
	$(CC) $(CFLAGS) -c $< -o $@


$(LIBNAME).dll: $(OBJECTS)
	cp $(LIBNAME).def tmp.def
#	echo GetVersion >> tmp.def

	ld --base-file tmp.base $(DLLFLAGS)
	dlltool $(DLLTOOLFLAGS)
	ld tmp.exp $(DLLFLAGS) --enable-stdcall-fixup


# Dependencies
main.o: misc.h miscz.h ucon64.h n64.h
n64.o: misc.h ucon64.h n64.h
ucon64.o: ucon64.h
misc.o: misc.h miscz.h
map.o: map.h
miscz.o: miscz.h misc.h map.h
