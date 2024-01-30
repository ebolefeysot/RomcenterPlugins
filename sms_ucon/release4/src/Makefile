.PHONY: all clean

ZLIB=1
#RC261_COMPATIBILITY=1

CC=gcc
CFLAGS=-Wall -W -O3 -DDLL -DHAVE_UNISTD_H
ifdef RC261_COMPATIBILITY
CFLAGS+=-DRC261_COMPATIBILITY
endif
#-DHAVE_CONFIG_H
LDFLAGS=-s

LIBNAME=sms
OBJECTS=main.o sms.o ucon64.o misc.o
LIBS=-ladvapi32
ifdef ZLIB
LIBS+=-lz
CFLAGS+=-DHAVE_ZLIB_H
OBJECTS+=unzip.o map.o miscz.o
endif

ifeq ($(TERM),cygwin)

GCCA_DIR=/lib/gcc-lib/i686-pc-cygwin/3.3.3/
LIBS+=-L$(GCCA_DIR) -lgcc -lcygwin -lkernel32
ENTRY=__cygwin_dll_entry@12

else
ifeq ($(OSTYPE),msys)

GCCA_DIR=/mingw/lib/gcc-lib/mingw32/3.2.3/
LIBS+=-L/mingw/lib -lkernel32 -lmsvcrt -L$(GCCA_DIR) -lgcc
# MSYS problem: Specifying the library directory is necessary when compiling on
# a different filesystem than the filesystem that MinGW is installed on.
ENTRY=_DllMain@12

endif                                   # msys
endif                                   # cygwin

FULLLIBNAME=$(LIBNAME).dll
DLLFLAGS=$(LDFLAGS) --dll $(OBJECTS) $(LIBS) -e $(ENTRY) -o $(FULLLIBNAME)
DLLTOOLFLAGS=-d tmp.def -b tmp.base -e tmp.exp -D $(FULLLIBNAME)


all: $(FULLLIBNAME)


clean:
	rm -f $(LIBNAME).dll $(LIBNAME).a $(OBJECTS) *.core *.stackdump *.o \
        tmp.def tmp.base tmp.exp


.c.o:
	$(CC) $(CFLAGS) -c $< -o $@


$(FULLLIBNAME): $(OBJECTS)
ifdef RC261_COMPATIBILITY
	cp $(LIBNAME).old.def tmp.def
	echo GetVersion >> tmp.def
else
	cp $(LIBNAME).def tmp.def
endif
	ld --base-file tmp.base $(DLLFLAGS)
	dlltool $(DLLTOOLFLAGS)
	ld tmp.exp $(DLLFLAGS) --enable-stdcall-fixup


# Dependencies
main.o: misc.h miscz.h ucon64.h sms.h
sms.o: misc.h ucon64.h sms.h
ucon64.o: ucon64.h
misc.o: misc.h miscz.h
map.o: map.h
miscz.o: miscz.h misc.h map.h
