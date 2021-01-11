unit rcZip;
//{$DEFINE debug}

interface

uses archive, UnZip32, Windows, Zip32, Classes,zipmstr;

resourcestring

  TXT_ZIPINVALID = 'Zip file structure invalid';
  TXT_NOTHINGTODO = 'Nothing to do';
  TXT_MISSINGOREMPTYZIP = 'Missing or empty zip file';
  TXT_INVALIDEARGUMENTS = 'Invalid command arguments';
  TXT_OUTOFMEMORY = 'Out of memory';
  TXT_FILENOTFOUND = 'File Not Found.';
  TXT_DISKFULL = 'Disk full';
  TXT_ERRORINZIP = 'Errors in zipfile';
  TXT_SEVEREERRORINZIP = 'Severes errors in zipfile';
  TXT_NOTAZIP = 'Probably not a zip';
  TXT_UNEXPECTEDEOF = 'Unexpected EOF';

  //return code for zip32.dll
const
  //ZE_MISS = -1      // used by procname(), zipbare()
  ZE_OK = 0; // Success
  ZE_EOF = 2; // Unexpected end of zip file
  ZE_FORM = 3; // Zip file structure invalid
  ZE_MEM = 4; // Out of memory
  ZE_LOGIC = 5; // Internal logic error
  ZE_BIG = 6; // Entry too large to split
  ZE_NOTE = 7; // Invalid comment format
  ZE_TEST = 8; // Zip file invalid or could not spawn unzip
  ZE_ABORT = 9; // Interrupted
  ZE_TEMP = 10; // Temporary file failure
  ZE_READ = 11; // Input file read failur
  ZE_NONE = 12; // Nothing to do
  ZE_NAME = 13; // Missing or empty zip file
  ZE_WRITE = 14; // Output file write failure
  ZE_CREAT = 15; // Could not create output file
  ZE_PARMS = 16; // Invalid command arguments
  ZE_OPEN = 18; // File not found or no read permission

  LocalFileHeaderSig = $04034B50; { 'PK'34  (in file: 504b0304) }
  CentralFileHeaderSig = $02014B50; { 'PK'12 }
  EndCentralDirSig = $06054B50; { 'PK'56 }
  SpannedSig = $08074B50; { 'PK'78 }
  BufSize = 8192; // Keep under 12K to avoid Winsock problems on Win95.
  // If chunks are too large, the Winsock stack can
  // lose bytes being sent or received.
type

  //erreurs unzip32.dll
  EPK_WARN = class(Eerror); // Warning
  EPK_ERR = class(ECorrupted); // error in zipfile
  EPK_BADERR = class(ECorrupted); // severe error in zipfile
  EPK_MEM = class(EMemoryError); // insufficient memory (during initialization)
  EPK_MEM2 = class(EMemoryError); // insufficient memory (password failure)
  EPK_MEM3 = class(EMemoryError); // insufficient memory (file decompression)
  EPK_MEM4 = class(EMemoryError); // insufficient memory (memory decompression)
  EPK_NOZIP = class(Eerror); // zipfile not found
  EPK_PARAM = class(ESystemError); // bad or illegal parameters specified
  EPK_FIND = class(Eerror); // no files found
  EPK_DISK = class(EADiskFull); // disk full
  EPK_EOF = class(ECorrupted); // unexpected EOF

  //erreurs zip32.dll
  EZE_CantSetZipOption = class(ESystemError);
  EZE_EOF = class(ECorrupted); //2 Unexpected end of zip file
  EZE_FORM = class(ECorrupted); //3 Zip file structure invalid
  EZE_MEM = class(EMemoryError); //4 Out of memory
  EZE_LOGIC = class(ESystemError); //5 Internal logic error
  EZE_BIG = class(EMemoryError); //6 Entry too large to split
  EZE_NOTE = class(ESystemError); //7 Invalid comment format
  EZE_TEST = class(ECorrupted); //8 Zip file invalid or could not spawn unzip
  EZE_ABORT = class(Eerror); //9 Interrupted
  EZE_TEMP = class(ESystemError); //10 Temporary file failure
  EZE_READ = class(ESystemError); //11 Input file read failure
  EZE_NONE = class(Eerror); //12 Nothing to do
  EZE_NAME = class(Eerror); //13 Missing or empty zip file
  EZE_WRITE = class(ESystemError); //14 Output file write failure
  EZE_CREAT = class(ESystemError); //15 Could not create output file
  EZE_PARMS = class(ESystemError); //16 Invalid command arguments
  EZE_OPEN = class(Eerror); //18 File not found or no read permission
  EZE = class(ECorrupted);

  ZipLocalHeader = packed record
    HeaderSig: LongWord;
    VersionNeed: Word;
    Flag: Word;
    ComprMethod: Word;
    ModifTime: Word;
    ModifDate: Word;
    CRC32: LongWord;
    ComprSize: LongWord;
    UnComprSize: LongWord;
    FileNameLen: Word;
    ExtraLen: Word;
  end;

  ZipCentralHeader = packed record //fixed part size : 42 bytes
    HeaderSig: LongWord; // hex: 02014B50(4)
    VersionMadeBy0: Byte; //version made by(1)
    VersionMadeBy1: Byte; //host number(1)
    VersionNeed: Word; // version needed to extract(2)
    Flag: Word; //generalPurpose bitflag(2)
    ComprMethod: Word; //compression method(2)
    ModifTime: Word; // modification time(2)
    ModifDate: Word; // modification date(2)
    CRC32: LongWord; //Cycling redundancy check (4)
    ComprSize: LongWord; //compressed file size  (4)
    UnComprSize: LongWord; //uncompressed file size (4)
    FileNameLen: Word; //(2)
    ExtraLen: Word; //(2)
    FileComLen: Word; //(2)
    DiskStart: Word; //starts on disk number xx(2)
    IntFileAtt: Word; //internal file attributes(2)
    ExtFileAtt: LongWord; //external file attributes(4)
    RelOffLocal: LongWord; //relative offset of local file header(4)
    // not used as part of this record structure:
    // filename, extra data, file comment
  end;

  ZipEndOfCentral = packed record //Fixed part size : 22 bytes
    HeaderSig: LongWord; //(4)  hex=06054B50
    ThisDiskNo: Word; //(2)This disk's number
    CentralDiskNo: Word; //(2)Disk number central dir start
    CentralEntries: Word; //(2)Number of central dir entries on this disk
    TotalEntries: Word; //(2)Number of entries in central dir
    CentralSize: LongWord; //(4)Size of central directory
    CentralOffSet: LongWord; //(4)offsett of central dir on 1st disk
    ZipCommentLen: Word; //(2)
    // not used as part of this record structure:
    // ZipComment
  end;

  ZipRenameRec = record
    Source: string;
    Dest: string;
    DateTime: Integer;
  end;
  pZipRenameRec = ^ZipRenameRec;

  MDZipData = record // MyDirZipData
    RelOffLocal: LongWord; // offset from the start of the first disk
    FileNameLen: Word; // length of current filename
    FileName: array[0..254] of AnsiChar; // Array of current filename
    ComprSize: Longword;
    DateTime: Integer;
  end;

  TOperation = (toExtractAll, toTest, toGetContent, toZip, toUnzip); //opération à faire dans le thread

  TArchiveThread = class(TThread)
  private
  protected
    FOperation: TOperation;
    procedure Execute; override;
  public
    Dir: string;
    ArchiveName: string;
    EntryName: string;
    constructor Create(Operation: TOperation);
  end;

  TRCZip = class(TArchive)
  private
    { Private declarations }
    ArchiveThread: TArchiveThread;
    UseTestingCallBack: boolean;

    FZipEOC: Integer; // End-Of-Central-Dir location
    FZipComment: string;
    FInFileName: string;
    FInFileHandle: Integer;
    MDZD: TList;
    MDZDp: ^MDZipData;

    ZipMaster : TZipMaster;

    procedure GetMessage(UnCompSize: ULONG; CompSize: ULONG; Factor: UINT; Month: UINT; Day: UINT; Year: UINT; Hour: UINT; Minute: UINT; C: Char; FileName: PAnsiChar; MethBuf: PAnsiChar; CRC: ULONG; Crypt: Char);
    procedure DllPrnt(msg: string);
    procedure DllService(msg: string);
    procedure HandleUnzipError(ErrorNo: integer; HandleCorrupted: boolean = true);
    procedure HandleZipError(ErrorNo: integer);
    procedure HandleZipMasterError(zipmaster: TZipmaster);
    function ConvertEntry(EntryName: string): string;

    procedure Rename(ArchiveFileName: string; RenameList: TList; DateTime: Integer);
    function ReplaceForwardSlash(aStr: string): string;
    function AppendSlash(sDir: string): string;
    function CheckIfLastDisk(var EOC: ZipEndOfCentral): boolean;
    procedure AllocSpanMem(TotalEntries: Integer);
    procedure DeleteSpanMem;

  public
    { Public declarations }
    UseThread: boolean;
    constructor Create;
    destructor Destroy;override;
    procedure ExtractFile(ArchiveName, FileToExtract, DestDir: string; ExtractPath: boolean); override;
    procedure GetContent(filename: string; it: TArchiveItems); override;
    procedure AddFile(ZipFileName: string; FileToAdd: string); override;
    procedure TestFile(FileName: string); override;
    procedure RenameEntries(ArchiveName: string; RenList: TList; FastRename: boolean); override;
    procedure DeleteEntry(ArchiveName, FileToDelete: string; BackupFolder: string); override;
    procedure CreateEmptyArchive(ArchiveName: string); override;
    procedure ExtractAll(FileName: string; Path: string); override;
    procedure AddAll(Path, FileName: string); override;
    procedure SetComments(ArchiveName,Comment: string);override;
    function GetComments(ArchiveName:string):string;override;
  end;

var
  Dummy: PAnsiChar;
  ZipRec: TZCL;
  Zip: TRCZip;
  FNV: array[0..999] of PAnsiChar;
  UF: TUserFunctions;
  Opt: TDCL;
  LastMessage: string;
  FirstMsg: boolean;
  CurrentEntry, CurrentArchive, CurrentError: string;
  ZUF: TZipUserFunctions;

  // global routines
procedure SetUserFunctions;

// user functions for use with the TUserFunctions structure
function DllPrnt(Buffer: PAnsiChar; Size: ULONG): integer; stdcall;
function DllPassword(P: PAnsiChar; N: Integer; M, Name: PAnsiChar): integer; stdcall;
function DllService(CurFile: PAnsiChar; Size: ULONG): integer; stdcall;
function DllReplace(FileName: PAnsiChar): integer; stdcall;
procedure DllMessage(UnCompSize: ULONG; CompSize: ULONG; Factor: UINT; Month: UINT; Day: UINT; Year: UINT; Hour: UINT; Minute: UINT; C: Char; FileName: PAnsiChar; MethBuf: PAnsiChar; CRC: ULONG; Crypt: Char); stdcall;

implementation

uses sysutils, Dialogs, jclunicode, forms, jclfileutils, filestools,
  DateUtils;
//  ShellApi;

const
  RC_FILE_NOT_FOUND = 100;
  RC_PATH_EXIST = 101;

function GetWinTempDir: string;
var
  Buf: array[0..1023] of char;
begin
  SetString(Result, Buf, GetTempPath(SizeOf(Buf), Buf));
end;

// global routines

// user functions for use with the TUserFunctions structure
//----------------------------------------------------------------------------------

function DllPrnt(Buffer: PAnsiChar; Size: ULONG): integer;
begin
  zip.DllPrnt(string(Buffer));
  Result := Size;
end;
//----------------------------------------------------------------------------------

function DllPassword(P: PAnsiChar; N: Integer; M, Name: PAnsiChar): integer;
begin
  Result := 1;
end;
//----------------------------------------------------------------------------------

function DllService(CurFile: PAnsiChar; Size: ULONG): integer;
begin
  Zip.DllService(string(CurFile));
  Result := 0;
end;
//----------------------------------------------------------------------------------

function DllReplace(FileName: PAnsiChar): integer;
begin
  Result := 1;
end;
//----------------------------------------------------------------------------------

procedure DllMessage(UnCompSize: ULONG; CompSize: ULONG; Factor: UINT; Month: UINT; Day: UINT; Year: UINT; Hour: UINT; Minute: UINT; C: Char; FileName: PAnsiChar; MethBuf: PAnsiChar; CRC: ULONG; Crypt: Char);
begin
  Zip.GetMessage(UnCompSize, CompSize, Factor, Month, Day, Year, Hour, Minute, C, FileName, MethBuf, CRC, Crypt);
end;

//----------------------------------------------------------------------------------

procedure SetZipInitFunctions;
begin
  { prepare ZipUserFunctions structure }
  with ZUF do begin
    @Print := @DllPrnt;
    @Comment := @DllMessage;
    @Password := @DllPassword;
    @Service := @DllService;
  end;
  { send it to dll }
  ZpInit(ZUF);
end;

procedure SetUserFunctions;
begin
  // prepare TUserFunctions structure
  with UF do begin
    @Print := @DllPrnt;
    @Sound := nil;
    @Replace := @DllReplace;
    @Password := @DllPassword;
    @SendApplicationMessage := @DllMessage;
    @ServCallBk := @DllService;
  end;

end;

function TRCZip.GetComments(ArchiveName:string):string;
begin
  inherited;
  ZipMaster.ZipFileName := ArchiveName;
  result := ZipMaster.ZipComment;

  HandlezipMasterError(zipmaster);

end;

procedure TRCZip.GetContent(filename: string; it: TArchiveItems);
var
  res: integer;
begin
  if not FileExists(filename) then    begin
    res := RC_FILE_NOT_FOUND
  end
  else begin
    CurrentArchive := filename;
    CurrentEntry := '';
    items := it;
    with Opt do begin
      ExtractOnlyNewer := Integer(False); // true if you are to extract only newer
      SpaceToUnderscore := Integer(False); // true if convert space to underscore
      PromptToOverwrite := Integer(False); // true if prompt to overwrite is wanted
      fQuiet := 2; // quiet flag. 1 = few messages, 2 = no messages, 0 = all messages
      nCFlag := Integer(False); // write to stdout if true
      nTFlag := Integer(False); // test zip file
      nVFlag := Integer(True); // verbose listing
      nUFlag := Integer(True); // "update" (extract only newer/new files)
      nZFlag := Integer(False); // display zip file comment
      nDFlag := Integer(False); // all args are files/dir to be extracted
      nOFlag := Integer(False); // true if you are to always over-write files, false if not
      nAFlag := Integer(False); // do end-of-line translation
      nZIFlag := Integer(False); // get zip info if true
      C_flag := Integer(False); // be case insensitive if TRUE
      fPrivilege := 1; // 1 => restore Acl's, 2 => Use privileges

      lpszExtractDir := PAnsiChar(AnsiString(TempDir)); // zip file name
      lpszZipFN := PAnsiChar(AnsiString(filename)); // Directory to extract to. NULL for the current directory
    end;

    if UseThread then begin
      //create the thread
      ArchiveThread := TArchiveThread.Create(toGetContent);
      //wait for the thread to finish
      try
        try
          repeat
            Application.ProcessMessages;
          until ArchiveThread.Terminated;
          res := ArchiveThread.ReturnValue;
        except
          res := PK_BADERR;
        end;
      finally
        ArchiveThread.Free;
      end;
    end
    else begin
      res := Wiz_SingleEntryUnzip(0, // number of file names being passed
        dummy, // file names to be unarchived
        0, // number of "file names to be excluded from processing" being  passed
        dummy, // file names to be excluded from the unarchiving process
        Opt, // pointer to a structure with the flags for setting the  various options
        UF); // pointer to a structure that contains pointers to user functions
    end;
  end;

  HandleUnZipError(res);
end;

//______________________________________________________________________________
procedure TRCZip.ExtractFile(ArchiveName, FileToExtract, DestDir: string; ExtractPath: boolean);

begin
  ZipMaster.ZipFileName := ArchiveName;
  if ExtractPath then ZipMaster.ExtrOptions := [ExtrDirNames]
  else ZipMaster.ExtrOptions := [];
  ZipMaster.ExtrBaseDir :=  DestDir;
  ZipMaster.FSpecArgs.Add(FileToExtract);
  ZipMaster.Extract;
end;

{procedure TRCZip.ExtractFile(ArchiveName, FileToExtract, DestDir: string; ExtractPath: boolean);
var
  res: integer;
  argc: integer;
begin
  // precautions
  if not FileExists(ArchiveName) then    begin
    res := RC_FILE_NOT_FOUND
  end
  else begin
    TestReadOnly(DestDir);

    if ExtractPath then begin
      //crée le répartoire d'extraction (si FileToExtract en posséde un)
      DestDir := IncludeTrailingPathDelimiter(DestDir) + ExtractFilePath(FileToExtract);
    end;
    DestDir := ExcludeTrailingPathDelimiter(DestDir);
    ForceDirectories(DestDir);

    //remplace les séparateurs de dir par des /
    FileToExtract := StringReplace(FileToExtract, '\', '/', [rfReplaceAll]);
    //remplace les '[' par '[[]', sinon, ils sont mal interpretés
    FileToExtract := StringReplace(FileToExtract, '[', '[[]', [rfReplaceAll]);

    CurrentArchive := ArchiveName;
    CurrentEntry := FileToExtract;

    with Opt do begin
      ExtractOnlyNewer := Integer(False); // true if you are to extract only newer
      SpaceToUnderscore := Integer(False); // true if convert space to underscore
      PromptToOverwrite := Integer(False); // true if prompt to overwrite is wanted
      fQuiet := 1; // quiet flag. 1 = few messages, 2 = no messages, 0 = all messages
      nCFlag := Integer(False); // write to stdout if true
      nTFlag := Integer(False); // test zip file
      nVFlag := Integer(False); // verbose listing
      nUFlag := Integer(False); // "update" (extract only newer/new files)
      nZFlag := Integer(False); // display zip file comment
      nDFlag := Integer(ExtractPath); // all args are files/dir to be extracted
      nOFlag := Integer(True); // true if you are to always over-write files, false if not
      nAFlag := Integer(False); // do end-of-line translation
      nZIFlag := Integer(False); // get zip info if true
      C_flag := Integer(True); // be case insensitive if TRUE
      fPrivilege := 1; // 1 => restore Acl's, 2 => Use privileges

      lpszExtractDir := PansiChar(ansistring(DestDir));
      lpszZipFN := PansiChar(ansistring(ArchiveName));
    end;

    {     //décompression de plusieurs fichiers en même temps:

          // copy the file names from SelectedList to FNV dynamic array
          for i := 0 to SelectedList.Count - 1 do
          begin
            GetMem(FNV[i], Length(SelectedList[i]) + 1 );
            StrPCopy(FNV[i], SelectedList[i]);
          end;

          argc := SelectedList.Count;
    }
{
    // copy the file name to FNV dynamic array
    GetMem(FNV[0], Length(FileToExtract) + 1);
    StrPCopy(FNV[0], Ansistring(FileToExtract));

    argc := 1;

    if UseThread then begin
      //create the thread
      ArchiveThread := TArchiveThread.Create(toUnzip);
      //wait for the thread to finish
      try
        try
          repeat
            Application.ProcessMessages;
          until ArchiveThread.Terminated;
          res := ArchiveThread.ReturnValue;
        except
          res := PK_BADERR;
        end;
      finally
        ArchiveThread.Free;
      end;
    end
    else begin
      res := Wiz_SingleEntryUnzip(argc, // number of file names being passed
        FNV[0], // file names to be unarchived
        0, // number of "file names to be excluded from processing" being  passed
        dummy, // file names to be excluded from the unarchiving process
        Opt, // pointer to a structure with the flags for setting the  various options
        UF); // pointer to a structure that contains pointers to user functions
    end;
    // release the memory
    FreeMem(FNV[0], Length(FileToExtract) + 1);
    //for i := (SelectedList.Count - 1) downto 0 do
    //  FreeMem(FNV[i], Length(SelectedList[i]) + 1 );

  end;

  HandleUnZipError(res);
end;
}
//----------------------------------------------------------------------------------

constructor TRCZip.Create;
begin
  inherited;

  ZipMaster := TZipMaster.Create(nil);
  zipmaster.Unattended := true; //don't show message box

  // set unzip user functions
  SetUserFunctions;

  // set zip user functions
  SetZipInitFunctions;

  FirstMsg := true;

  UseThread := true;

end;

procedure TRCZip.GetMessage(UnCompSize, CompSize: ULONG; Factor, Month, Day, Year, Hour, Minute: UINT; C: Char; FileName, MethBuf: PAnsiChar; CRC: ULONG; Crypt: Char);
var
  Item: TArchiveItem;
  s: string;
begin
  s := StringReplace(string(FileName), '/', '\', [rfReplaceAll]);
  Item := TArchiveItem.create;
  Item.Name := ExtractFileName(s);
  Item.Path := IncludeTrailingPathDelimiter(ExtractFilePath(s));
  Item.UncompSize := uncompsize;
  Item.crc := lowercase(IntToHex(crc, 8));

  //I must set the year century : 19 or 20

  if (year < 100) and (year > 39) then begin
    year := year + 1900
  end else begin
    year := year + 2000
  end;
  Item.Date := EncodeDateTime(year, month, day, hour, minute, 0, 0);
  Items.Add(Item);
end;

procedure TRCZip.DllPrnt(msg: string);
begin
  //correction du message
  msg := trim(msg);
  msg := StringReplace(msg, #10, '', [rfReplaceAll]);
  msg := StringReplace(msg, '/', '\', [rfReplaceAll]);

  if UseTestingCallBack then begin
    //génère un evenement pour les messages
    if Assigned(FOnCorrupted) then begin
      FOnCorrupted(CurrentArchive, CurrentEntry, msg);
    end;
  end
  else begin
    if Assigned(FOnInfos) then begin
      FOnInfos(msg);
    end;
  end;

  exit;
  //le message d'erreur arrive ici
  //puis le nom du fichier concerné arrive dans dllservice
  // 1 - nom du fichier
  // 2 - erreur

{  //premier message
  if FirstMsg then begin
    if pos(CurrentArchive,msg) = 0 then CurrentEntry := msg
    else CurrentEntry := '';
  end;

  If not FirstMsg Then CurrentError := msg;
  else FirstMsg := false;
}
  LastMessage := msg;
  if Assigned(FOnInfos) then begin
    FOnInfos(msg);
  end;

  if pos('No errors', msg) = 0 then begin
    CurrentError := msg
  end;
end;

procedure TRCZip.HandleZipError(ErrorNo: integer);
var
  e: EArchiveException;
begin
  if ErrorNo = PK_OK then begin
    exit
  end;

  e := nil;
  case ErrorNo of
    ZE_EOF:
    begin
      e := EZE_EOF.create(TXT_UNEXPECTEDEOF, CurrentArchive, extractfilename(CurrentEntry))
    end;
    ZE_FORM:
    begin
      e := EZE_FORM.create(TXT_ZIPINVALID, CurrentArchive, extractfilename(CurrentEntry))
    end;
    ZE_MEM:
    begin
      e := EZE_MEM.create(TXT_OUTOFMEMORY, CurrentArchive, extractfilename(CurrentEntry))
    end;
    ZE_LOGIC:
    begin
      e := EZE_LOGIC.create('Internal logic error', CurrentArchive, extractfilename(CurrentEntry))
    end;
    ZE_BIG:
    begin
      e := EZE_BIG.create('Entry too large to split', CurrentArchive, extractfilename(CurrentEntry))
    end;
    ZE_NOTE:
    begin
      e := EZE_NOTE.create('Invalid comment format', CurrentArchive, extractfilename(CurrentEntry))
    end;
    ZE_TEST:
    begin
      e := EZE_TEST.create('Zip file invalid or could not spawn unzip', CurrentArchive, extractfilename(CurrentEntry))
    end;
    ZE_ABORT:
    begin
      e := EZE_ABORT.create('Interrupted', CurrentArchive, extractfilename(CurrentEntry))
    end;
    ZE_TEMP:
    begin
      e := EZE_TEMP.create('Temporary file failure', CurrentArchive, extractfilename(CurrentEntry))
    end;
    ZE_READ:
    begin
      e := EZE_READ.create('Input file read failure', CurrentArchive, extractfilename(CurrentEntry))
    end;
    ZE_NONE:
    begin
      e := EZE_NONE.create(TXT_NOTHINGTODO, CurrentArchive, extractfilename(CurrentEntry))
    end;
    ZE_NAME:
    begin
      e := EZE_NAME.create(TXT_MISSINGOREMPTYZIP, CurrentArchive, extractfilename(CurrentEntry))
    end;
    ZE_WRITE:
    begin
      e := EZE_WRITE.create('Output file write failure', CurrentArchive, extractfilename(CurrentEntry))
    end;
    ZE_CREAT:
    begin
      e := EZE_CREAT.create('Could not create output file', CurrentArchive, extractfilename(CurrentEntry))
    end;
    ZE_PARMS:
    begin
      e := EZE_PARMS.create(TXT_INVALIDEARGUMENTS, CurrentArchive, extractfilename(CurrentEntry))
    end;
    ZE_OPEN:
    begin
      e := EZE_OPEN.create('File not found or no read permission', CurrentArchive, extractfilename(CurrentEntry))
    end;
    PK_EOF:
    begin
      e := EPK_EOF.Create(TXT_UNEXPECTEDEOF, CurrentArchive, CurrentEntry)
    end;
    PK_DISK:
    begin
      e := EPK_DISK.Create(TXT_DISKFULL, CurrentArchive, CurrentEntry)
    end;
    RC_FILE_NOT_FOUND:
    begin
      e := EFileNotFound.Create(TXT_FILENOTFOUND, CurrentArchive, CurrentEntry)
    end;
  end;

  //generation d'exception systemes
{  case ErrorNo of
    //PK_WARN: ; //raise EPK_WARN.Create( msg + 'Warning error' + #13 + LastMessage);
    PK_MEM: e := EPK_MEM.Create(TXT_OUTOFMEMORY,CurrentArchive,CurrentEntry);
    PK_MEM2: e := EPK_MEM2.Create(TXT_OUTOFMEMORY,CurrentArchive,CurrentEntry);
    PK_MEM3: e := EPK_MEM3.Create(TXT_OUTOFMEMORY,CurrentArchive,CurrentEntry);
    PK_MEM4: e := EPK_MEM4.Create(TXT_OUTOFMEMORY,CurrentArchive,CurrentEntry);
    PK_PARAM: e := EPK_PARAM.Create(TXT_INVALIDEARGUMENTS,CurrentArchive,CurrentEntry);
    PK_FIND: e := EPK_FIND.Create(TXT_FILENOTFOUND,CurrentArchive,CurrentEntry);
  end;

  //on ne genere pas d'exception 'corrupted' lorsqu'on teste une archive
  case ErrorNo of
    //PK_ERR: e := EPK_ERR.Create(TXT_ERRORINZIP,CurrentArchive,CurrentEntry);
    //PK_BADERR: e := EPK_BADERR.Create(TXT_SEVEREERRORINZIP,CurrentArchive,CurrentEntry);
    //PK_NOZIP: e := EPK_NOZIP.Create(TXT_NOTAZIP,CurrentArchive,CurrentEntry);
  end;
}
  if e = nil then begin
    e := ESystemError.Create('Unknown error n° ' + inttostr(ErrorNo), CurrentArchive, CurrentEntry)
  end;
  raise e;

end;

procedure TRCZip.HandleZipMasterError(zipmaster: TZipmaster);
var
  errcode:integer;
begin
  errcode := zipmaster.ErrCode;
  if errcode <> 0 then
  begin
    raise EZE.create(zipmaster.Message,zipmaster.ZipFileName,'');
  end;
end;

procedure TRCZip.HandleUnZipError(ErrorNo: integer; HandleCorrupted: boolean = true);
var
  e: EArchiveException;
begin
  e := nil;
  if ErrorNo = PK_OK then begin
    exit
  end;
  if e <> nil then begin
    raise e
  end;

end;

procedure TRCZip.AddFile(ZipFileName, FileToAdd: string);
var
  res: integer;
begin

  CurrentArchive := ZipFileName;
  CurrentEntry := FileToAdd;

  // precaution
  if Trim(ZipFileName) = '' then begin
    raise ENoFileSpecified.Create('[1] No filename specified', '', '')
  end;
  if Trim(FileToAdd) = '' then begin
    raise ENoFileSpecified.Create('[2] No filename specified', '', '')
  end;
  //if not FileExists(FileName) then Raise EFileNotFound.Create('[1] Archive File [' + FileName + '] not found');
  //if not FileExists(FileToAdd) then Raise EFileNotFound.Create('[2] File [' + FileToAdd + '] not found');

  TestReadOnly(ZipFileName);

  //remplace les '[' par '[[]', sinon, ils sont mal interpretés
  //FileToAdd := StringReplace(FileToAdd, '[', '[[]', [rfReplaceAll]);
  //FileToAdd := StringReplace(FileToAdd, '-', '[-]', [rfReplaceAll]);

  ZipMaster.ZipFileName := ZipFileName;
  ZipMaster.FSpecArgs.Clear;
  ZipMaster.FSpecArgs.Add(FileToAdd);
  res := ZipMaster.Add;

  HandleZipError(res);

end;

{
*** old procedure, test file and every roms in file
procedure TRCZip.TestFile(FileName: string);
var
  i, res: integer;
  it: TArchiveItems;
begin
  //on fait deux passes, une pour tester le fichier zip lui même
  //et si des erreurs sont détectées dedans, on fait une passe
  //pour chaque entrée.

  UseTestingCallBack := false;

  CurrentArchive := FileName;
  CurrentEntry := '';

  if Assigned(FOnInfos) then begin
    FOnInfos('File ' + FileName);
  end;

  //passe 1
  if not FileExists(filename) then begin
    if Assigned(FOnInfos) then FOnInfos('File not found');
    res := RC_FILE_NOT_FOUND;
  end
  else begin
    with Opt do begin
      ExtractOnlyNewer := Integer(False); // true if you are to extract only newer
      SpaceToUnderscore := Integer(False); // true if convert space to underscore
      PromptToOverwrite := Integer(False); // true if prompt to overwrite is wanted
      fQuiet := 1; // quiet flag. 1 = few messages, 2 = no messages, 0 = all messages
      nCFlag := Integer(False); // write to stdout if true
      nTFlag := Integer(true); // test zip file
      nVFlag := Integer(false); // verbose listing
      nUFlag := Integer(false); // "update" (extract only newer/new files)
      nZFlag := Integer(False); // display zip file comment
      nDFlag := Integer(False); // all args are files/dir to be extracted
      nOFlag := Integer(False); // true if you are to always over-write files, false if not
      nAFlag := Integer(False); // do end-of-line translation
      nZIFlag := Integer(False); // get zip info if true
      C_flag := Integer(True); // be case insensitive if TRUE
      fPrivilege := 1; // 1 => restore Acl's, 2 => Use privileges

      lpszExtractDir := PAnsiChar(TempDir); // zip file name
      lpszZipFN := PAnsiChar(filename); // Directory to extract to. NULL for the current directory
    end;

    if UseThread then begin
      //create the thread
      ArchiveThread := TArchiveThread.Create(toTest);
      //wait for the thread to finish
      try
        try
          repeat
            Application.ProcessMessages;
          until ArchiveThread.Terminated;
          res := ArchiveThread.ReturnValue;
        except
          res := PK_BADERR;
        end;
      finally
        ArchiveThread.Free;
      end;

    end
    else begin
      // test archive
      Res := Wiz_SingleEntryUnzip(0, // number of file names being passed
        nil, // file names to be unarchived
        0, // number of "file names to be excluded from processing" being  passed
        nil, // file names to be excluded from the unarchiving process
        Opt, // pointer to a structure with the flags for setting the  various options
        UF); // pointer to a structure that contains pointers to user functions
    end;
  end;

  //genere les exceptions systemes seulement (memoire, disk...)
  HandleUnZipError(res, false);

  //genere les evenements pour les fichiers corrompus
  if Assigned(FOnCorrupted) then begin
    case res of
      PK_ERR: begin //des erreus sont présentes dans le zip
          //reteste précisement le contenu du zip
          it := TArchiveItems.Create;
          try
            GetContent(FileName, it);

            //positionne les callback pour receptionner les messages d'erreurs
            //et generer les evenements
            UseTestingCallback := true;

            for i := 0 to it.Count - 1 do begin
              CurrentEntry := it[i].FullName;

              GetMem(FNV[0], Length(CurrentEntry) + 1);
              StrPCopy(FNV[0], CurrentEntry);

              // unzip
              with Opt do begin
                ExtractOnlyNewer := Integer(False); // true if you are to extract only newer
                SpaceToUnderscore := Integer(False); // true if convert space to underscore
                PromptToOverwrite := Integer(False); // true if prompt to overwrite is wanted
                fQuiet := 2; // quiet flag. 1 = few messages, 2 = no messages, 0 = all messages
                nCFlag := Integer(False); // write to stdout if true
                nTFlag := Integer(true); // test zip file
                nVFlag := Integer(false); // verbose listing
                nUFlag := Integer(false); // "update" (extract only newer/new files)
                nZFlag := Integer(False); // display zip file comment
                nDFlag := Integer(False); // all args are files/dir to be extracted
                nOFlag := Integer(False); // true if you are to always over-write files, false if not
                nAFlag := Integer(False); // do end-of-line translation
                nZIFlag := Integer(False); // get zip info if true
                C_flag := Integer(True); // be case insensitive if TRUE
                fPrivilege := 1; // 1 => restore Acl's, 2 => Use privileges

                lpszExtractDir := PAnsiChar(TempDir); // zip file name
                lpszZipFN := PAnsiChar(filename); // Directory to extract to. NULL for the current directory
              end;

              if UseThread then begin
                //create the thread
                ArchiveThread := TArchiveThread.Create(toUnzip);
                //wait for the thread to finish
                try
                  repeat
                    Application.ProcessMessages;
                  until ArchiveThread.Terminated;
                finally
                  ArchiveThread.Free;
                end;
              end
              else begin
                Wiz_SingleEntryUnzip(1, // number of file names being passed
                  @FNV, // file names to be unarchived
                  0, // number of "file names to be excluded from processing" being  passed
                  nil, // file names to be excluded from the unarchiving process
                  Opt, // pointer to a structure with the flags for setting the  various options
                  UF); // pointer to a structure that contains pointers to user functions

              end;
              // release the memory
              FreeMem(FNV[0], length(CurrentEntry) + 1);
            end;
          finally
            UseTestingCallback := false;
            it.free;
          end;

        end;
      PK_BADERR: begin
          FOnCorrupted(CurrentArchive, '', TXT_SEVEREERRORINZIP);
        end;
      PK_NOZIP: FOnCorrupted(CurrentArchive, '', TXT_NOTAZIP);
      PK_EOF: FOnCorrupted(CurrentArchive, '', TXT_UNEXPECTEDEOF);
    end;
  end;

  //saute une ligne
  if Assigned(FOnInfos) then begin
    FOnInfos(#13 + #10);
  end;

end;
}

procedure TRCZip.TestFile(FileName: string);
//test file. stop on first error
var
  res: integer;
begin
  //on fait deux passes, une pour tester le fichier zip lui même
  //et si des erreurs sont détectées dedans, on fait une passe
  //pour chaque entrée.

  TestReadOnly(filename);

  UseTestingCallBack := false;

  CurrentArchive := FileName;
  CurrentEntry := '';

  if Assigned(FOnInfos) then begin
    FOnInfos('File ' + FileName);
  end;

  //passe 1
  if not FileExists(filename) then begin
    if Assigned(FOnInfos) then begin
      FOnInfos('File not found')
    end;
    res := RC_FILE_NOT_FOUND;
  end
  else begin
    with Opt do begin
      ExtractOnlyNewer := Integer(False); // true if you are to extract only newer
      SpaceToUnderscore := Integer(False); // true if convert space to underscore
      PromptToOverwrite := Integer(False); // true if prompt to overwrite is wanted
      fQuiet := 1; // quiet flag. 1 = few messages, 2 = no messages, 0 = all messages
      nCFlag := Integer(False); // write to stdout if true
      nTFlag := Integer(true); // test zip file
      nVFlag := Integer(false); // verbose listing
      nUFlag := Integer(false); // "update" (extract only newer/new files)
      nZFlag := Integer(False); // display zip file comment
      nDFlag := Integer(False); // all args are files/dir to be extracted
      nOFlag := Integer(False); // true if you are to always over-write files, false if not
      nAFlag := Integer(False); // do end-of-line translation
      nZIFlag := Integer(False); // get zip info if true
      C_flag := Integer(True); // be case insensitive if TRUE
      fPrivilege := 1; // 1 => restore Acl's, 2 => Use privileges

      lpszExtractDir := PansiChar(ansistring(TempDir)); // zip file name
      lpszZipFN := PansiChar(ansistring(filename)); // Directory to extract to. NULL for the current directory
    end;

    if UseThread then begin
      //create the thread
      ArchiveThread := TArchiveThread.Create(toTest);
      //wait for the thread to finish
      try
        try
          repeat
            Application.ProcessMessages;
          until ArchiveThread.Terminated;
          res := ArchiveThread.ReturnValue;
        except
          res := PK_BADERR;
        end;
      finally
        ArchiveThread.Free;
      end;

    end
    else begin
      // test archive
      Res := Wiz_SingleEntryUnzip(0, // number of file names being passed
        dummy, // file names to be unarchived
        0, // number of "file names to be excluded from processing" being  passed
        dummy, // file names to be excluded from the unarchiving process
        Opt, // pointer to a structure with the flags for setting the  various options
        UF); // pointer to a structure that contains pointers to user functions
    end;
  end;

  //genere les exceptions
  //HandleUnZipError(res, true);

  //genere les evenements pour les fichiers corrompus
  if Assigned(FOnCorrupted) then begin
    case res of
      PK_ERR:
      begin
        FOnCorrupted(CurrentArchive, '', TXT_ERRORINZIP)
      end;
      PK_BADERR:
      begin
        FOnCorrupted(CurrentArchive, '', TXT_SEVEREERRORINZIP)
      end;
      PK_NOZIP:
      begin
        FOnCorrupted(CurrentArchive, '', TXT_NOTAZIP)
      end;
      PK_EOF:
      begin
        FOnCorrupted(CurrentArchive, '', TXT_UNEXPECTEDEOF)
      end;
    end;
  end;

  //saute une ligne
  if Assigned(FOnInfos) then begin
    FOnInfos(#13 + #10);
  end;

end;

//______________________________________________________________________________

procedure TRCZip.DeleteEntry(ArchiveName, FileToDelete: string; BackupFolder: string);
var
  res: integer;
begin
  TestReadOnly(ArchiveName);

  // precaution
  if Trim(ArchiveName) = '' then begin
    raise ENoFileSpecified.Create('[DeleteEntry] No filename specified', '', '')
  end;

  CurrentArchive := ArchiveName;
  CurrentEntry := FileToDelete;

  //save to archive folder if asked
  if (BackupFolder <> '') and (DirectoryExists(BackupFolder)) then begin
    //get a unique name for rom in backupfolder
    if FileExists(BackupFolder + FileToDelete) then begin
      //file exists in backup folder: rename
      DskRenameFile(BackupFolder, FileToDelete,GetUniqueFileName(BackupFolder, FileToDelete));
    end;
    //save file to backupfolder
    ExtractFile(ArchiveName, FileToDelete, BackupFolder, false);
  end;

  ZipMaster.ZipFileName := ArchiveName;
  ZipMaster.FSpecArgs.Clear;
  ZipMaster.FSpecArgs.Add(FileToDelete);
  res := ZipMaster.Delete;
  HandleZipError(res);
end;

procedure TRCZip.CreateEmptyArchive(ArchiveName: string);
var
  FileHandle: integer;
  void: string;
begin
  TestReadOnly(archivename);

  ArchiveName := ExpandFileName(ArchiveName);

  //crée un fichier vide
  void := TempDir + '\void.tmp';
  FileHandle := FileCreate(void);
  FileClose(FileHandle);

  //compression du fichier vide
  AddFile(ArchiveName, void);

  //supprime le fichier vide
  DeleteFile(void);
  DeleteEntry(ArchiveName, 'void.tmp', '');

end;

procedure TRCZip.DllService(msg: string);
begin
  {  If Assigned(FOnInfos) Then Begin
      FOnInfos(msg);
    End;
  }
  //on reçoit ici le nom du fichier en cours de traitement dans le zip
  //si un fichier est corrompu, le message d'erreur dllprnt arrive avant
  //celui ci. On le memorise et on envoie l'evenement ici
  {  If (CurrentError <> '') and Assigned(FOnCorrupted) Then Begin
      FOnCorrupted(CurrentArchive,msg,CurrentError);
      FirstMsg := true;
      CurrentError := '';
    End
  }
end;

procedure TRCZip.ExtractAll(FileName, Path: string);
var
  res: integer;
begin
  inherited;
  res := 0;
  // precautions
  if DirectoryExists(path) then begin
    res := RC_PATH_EXIST
  end;
  if not FileExists(FileName) then begin
    res := RC_FILE_NOT_FOUND
  end;

  TestReadOnly(path);

  if res = 0 then {//pas d'erreurs} begin
    Path := ExcludeTrailingPathDelimiter(Path);
    ForceDirectories(Path);

    CurrentArchive := FileName;
    CurrentEntry := '';

    with Opt do begin
      ExtractOnlyNewer := Integer(False); // true if you are to extract only newer
      SpaceToUnderscore := Integer(False); // true if convert space to underscore
      PromptToOverwrite := Integer(False); // true if prompt to overwrite is wanted
      fQuiet := 1; // quiet flag. 1 = few messages, 2 = no messages, 0 = all messages
      nCFlag := Integer(False); // write to stdout if true
      nTFlag := Integer(False); // test zip file
      nVFlag := Integer(False); // verbose listing
      nUFlag := Integer(False); // "update" (extract only newer/new files)
      nZFlag := Integer(False); // display zip file comment
      nDFlag := Integer(True); // all args are files/dir to be extracted
      nOFlag := Integer(True); // true if you are to always over-write files, false if not
      nAFlag := Integer(False); // do end-of-line translation
      nZIFlag := Integer(False); // get zip info if true
      C_flag := Integer(True); // be case insensitive if TRUE
      fPrivilege := 1; // 1 => restore Acl's, 2 => Use privileges

      lpszExtractDir := PansiChar(ansistring(Path));
      lpszZipFN := PansiChar(ansistring(FileName));
    end;

    if UseThread then begin
      //create the thread
      ArchiveThread := TArchiveThread.Create(toExtractAll);
      //wait for the thread to finish
      try
        try
          repeat
            Application.ProcessMessages;
          until ArchiveThread.Terminated;
          res := ArchiveThread.ReturnValue;
        except
          res := PK_BADERR;
        end;
      finally
        ArchiveThread.Free;
      end;
    end
    else begin
      Res := Wiz_SingleEntryUnzip(0, // number of file names being passed
        dummy, // file names to be unarchived
        0, // number of "file names to be excluded from processing" being  passed
        dummy, // file names to be excluded from the unarchiving process
        Opt, // pointer to a structure with the flags for setting the  various options
        UF); // pointer to a structure that contains pointers to user functions

    end;
  end;

  HandleUnZipError(res);

end;

procedure TRCZip.AddAll(Path, FileName: string);
var
  res: integer;
  ZipOptions: TZPOpt;
  OldDir: string;
begin
  inherited;

  TestReadOnly(path);

  res := ZE_OK;
  path := ExcludeTrailingPathDelimiter(Path);
  CurrentArchive := FileName;
  CurrentEntry := '';

  // precaution
  if Trim(FileName) = '' then begin
    raise ENoFileSpecified.Create('[1] No filename specified', '', '')
  end;
  if Trim(path) = '' then begin
    raise ENoFileSpecified.Create('[2] No filename specified', '', '')
  end;
  if not DirectoryExists(Path) then begin
    raise EPathExist.Create('[3] Path ' + path + ' doesn''t exist', CurrentArchive, CurrentEntry)
  end;
  if FileExists(filename) then begin
    raise EFileExist.Create('[4] File ' + filename + ' already exist', CurrentArchive, CurrentEntry)
  end;

  SetZipInitFunctions;

  // number of files to zip
  ZipRec.argc := 1;

  // number of files to zip
  ZipOptions.Date := nil; { Date to include after (US format e.g. "12/31/98") }
  ZipOptions.szRootDir := nil; { Directory to use as base for zipping }
  ZipOptions.szTempDir := nil; //PAnsiChar(ExcludeTrailingPathDelimiter(TempDir)); { Temporary directory used during zipping }
  ZipOptions.fSuffix := false; { Include suffixes (not implemented) }
  ZipOptions.fEncrypt := False; { Encrypt files }
  ZipOptions.fSystem := false; { Include system and hidden files }
  ZipOptions.fVolume := False; { Include volume label }
  ZipOptions.fExtra := true; { Exclude extra attributes }
  ZipOptions.fNoDirEntries := false; { Do not add directory entries }
  ZipOptions.fExcludeDate := false; { Exclude files earlier than specified date }
  ZipOptions.fIncludeDate := false; { Include only files earlier than specified date }
  ZipOptions.fVerbose := false; { Mention oddities in zip file structure }
  ZipOptions.fQuiet := false; { Quiet operation }
  ZipOptions.fCRLF_LF := false; { Translate CR/LF to LF }
  ZipOptions.fLF_CRLF := false; { Translate LF to CR/LF }
  ZipOptions.fJunkDir := false; { don't add directory names }
  ZipOptions.fGrow := false; { Allow appending to a zip file }
  ZipOptions.fForce := false; { Make entries using DOS names (k for Katz) }
  ZipOptions.fMove := false; { Delete files added or updated in zip file }
  ZipOptions.fDeleteEntries := false; { Delete files from zip file }
  ZipOptions.fUpdate := false; { Update zip file--overwrite only if newer }
  ZipOptions.fFreshen := false; { Freshen zip file--overwrite only }
  ZipOptions.fJunkSFX := false; { Junk SFX prefix }
  ZipOptions.fLatestTime := false; { Set zip file time to time of latest file in it }
  ZipOptions.fComment := false; { Put comment in zip file }
  ZipOptions.fOffsets := false; { Update archive offsets for SFX files }
  ZipOptions.fPrivilege := false; { Use privileges (WIN32 only) }
  ZipOptions.fEncryption := false; { TRUE if encryption supported, else FALSE. This is a read only flag }
  ZipOptions.fRecurse := 1; { Recurse into subdirectories.  1 => -r, 2 => -R }
  ZipOptions.fRepair := 0; { Repair archive. 1 => -F, 2 => -FF }
  ZipOptions.fLevel := inttostr(CompressionLevel)[1]; { Compression level (0-9) 6 = Default}

  { send the options to the dll }
  if not ZpSetOptions(ZipOptions) then begin
    raise EZE_CantSetZipOption.Create('Error setting Zip Options', CurrentArchive, CurrentEntry)
  end;

  // name of zip file - allocate room for null terminated string
  GetMem(ZipRec.lpszZipFN, Length(FileName) + 1);
  ZipRec.lpszZipFN := StrPCopy(ZipRec.lpszZipFN, ansistring(FileName));
  try
    // dynamic array allocation
    GetMem(ZipRec.FNV, SizeOf(PAnsiChar));
    try
      // copy the file names to ZipRec.FNV dynamic array
      GetMem(ZipRec.FNV^[0], length('.') + 1);
      StrPCopy(ZipRec.FNV^[0], '.');
      OldDir := GetCurrentDir;
      try
        // send the data to the dll
        SetCurrentDir(path);

        if UseThread then begin
          //create the thread
          ArchiveThread := TArchiveThread.Create(toZip);
          //wait for the thread to finish
          try
            try
              repeat
                Application.ProcessMessages;
              until ArchiveThread.Terminated;
              res := ArchiveThread.ReturnValue;
            except
              res := PK_BADERR;
            end;
          finally
            ArchiveThread.Free;
          end;
        end
        else begin
          Res := ZpArchive(ZipRec);
        end;
      finally
        // release the memory for the file list
        FreeMem(ZipRec.FNV^[0], length('.') + 1);
        SetCurrentDir(OldDir);
      end;

    finally
      // release the memory for the ZipRec.FNV dynamic array
      FreeMem(ZipRec.FNV, ZipRec.argc * SizeOf(PAnsiChar));
    end;
  finally
    // release the memory for the FileName
    FreeMem(ZipRec.lpszZipFN, Length(FileName) + 1);
    HandleZipError(res);
  end;

end;

function TRCZip.ConvertEntry(EntryName: string): string;
var
  s: string;
  j: integer;
begin
  //    changetext := pos(char(159),EntryName) > 0;
  //result := StringReplace(EntryName, '[', '[[]', [rfReplaceAll]);
  //result := StringReplace(result, '-', '[-]', [rfReplaceAll]);

  //    result := translatestring(result,cp_oemcp,cp_acp);
  for j := 1 to length(result) do begin
    result[j] := char(integer(result[j]));
    //      if changetext then begin
    if integer(result[j]) = 146 then begin
      result[j] := 'Æ'
    end;
    if integer(result[j]) = 128 then begin
      result[j] := 'Ç'
    end;
    if integer(result[j]) = 145 then begin
      result[j] := 'æ'
    end;
    if integer(result[j]) = 161 then begin
      result[j] := 'í'
    end;
    {
            if integer(result[j]) = 152 then result[j] := 'ÿ';
            if integer(result[j]) = 153 then result[j] := 'Ö';
            if integer(result[j]) = 154 then result[j] := 'Ü';
            if integer(result[j]) = 155 then result[j] := 'ø';
            if integer(result[j]) = 156 then result[j] := '£';
            if integer(result[j]) = 157 then result[j] := 'Ø';
            if integer(result[j]) = 158 then result[j] := '×';
            if integer(result[j]) = 159 then result[j] := 'ƒ';
            if integer(result[j]) = 162 then result[j] := 'ó';
            if integer(result[j]) = 163 then result[j] := 'ú';
            if integer(result[j]) = 165 then result[j] := 'Ñ';
    }
    //        if integer(result[j]) = 160 then result[j] := 'á';
    //        if integer(result[j]) = 170 then result[j] := '¬';
    //      end;

    {      if (changenext) and (length(entryname) > 1) then begin
            //if integer(result[j]) = 131 then result[j] := 'â';
            if integer(result[j]) = 160 then result[j] := 'á';
            if integer(result[j]) = 170 then result[j] := '¬';
            changenext := false;
          end;

          if integer(result[j]) = 131 then changenext := true;
    }
    if integer(result[j]) > 127 then      begin
      s := s + '[' + result[j] + ']'
    end
    else begin
      s := s + result[j]
    end;
  end;

  result := string(translatestring(ansistring(s), cp_acp, cp_oemcp));
end;

{ TArchiveThread }

constructor TArchiveThread.Create(Operation: TOperation);
begin
  FOperation := Operation;
  inherited Create(false);

end;

procedure TArchiveThread.Execute;
begin
  try
    try
      case FOperation of
        toUnzip:
        begin
{$IFDEF DEBUG}
          OutputDebugString('Thread unzip');
{$ENDIF}
          ReturnValue := Wiz_SingleEntryUnzip(1, // number of file names being passed
            FNV[0], // file names to be unarchived
            0, // number of "file names to be excluded from processing" being  passed
            dummy, // file names to be excluded from the unarchiving process
            Opt, // pointer to a structure with the flags for setting the  various options
            UF); // pointer to a structure that contains pointers to user functions
        end;
        tozip:
        begin
{$IFDEF DEBUG}
          OutputDebugString('Thread zip');
{$ENDIF}
          // send the data to the dll
          ReturnValue := ZpArchive(ZipRec);
          //ReturnValue := 10;
        end;
        toGetContent:
        begin
{$IFDEF DEBUG}
          OutputDebugString('Thread get content');
{$ENDIF}
          //get content
          ReturnValue := Wiz_SingleEntryUnzip(0, // number of file names being passed
            dummy, // file names to be unarchived
            0, // number of "file names to be excluded from processing" being  passed
            dummy, // file names to be excluded from the unarchiving process
            Opt, // pointer to a structure with the flags for setting the  various options
            UF); // pointer to a structure that contains pointers to user functions
        end;
        toTest:
        begin
{$IFDEF DEBUG}
          OutputDebugString('Thread test');
{$ENDIF}
          // test archive
          ReturnValue := Wiz_SingleEntryUnzip(0, // number of file names being passed
            dummy, // file names to be unarchived
            0, // number of "file names to be excluded from processing" being  passed
            dummy, // file names to be excluded from the unarchiving process
            Opt, // pointer to a structure with the flags for setting the  various options
            UF); // pointer to a structure that contains pointers to user functions
        end;
        toExtractAll:
        begin
{$IFDEF DEBUG}
          OutputDebugString('Thread extract all');
{$ENDIF}
          // unzip all
          ReturnValue := Wiz_SingleEntryUnzip(0, // number of file names being passed
            dummy, // file names to be unarchived
            0, // number of "file names to be excluded from processing" being  passed
            dummy, // file names to be excluded from the unarchiving process
            Opt, // pointer to a structure with the flags for setting the  various options
            UF); // pointer to a structure that contains pointers to user functions
        end;
      end;
    except
      on e: exception do begin
        MessageDlg(e.message, mtError, [mbOK], 0);
      end;
    end;
  finally
    Terminate;
  end;
end;

procedure TRCZip.RenameEntries(ArchiveName: string; RenList: TList; FastRename: boolean);
begin
  //renommage rapide et groupé des entrées spécifiées dans RenList
  Rename(ArchiveName, RenList, 0);
end;


// Function to read a Zip archive and change one or more file specifications.
// Source and Destination should be of the same type. (path or file)
// If NewDateTime is 0 then no change is made in the date/time fields.
procedure TRcZip.Rename(ArchiveFileName: string; RenameList: TList; DateTime: Integer);
var
  EOC: ZipEndOfCentral;
  CEH: ZipCentralHeader;
  LOH: ZipLocalHeader;
  Buffer: array[0..BufSize - 1] of AnsiChar;
  i, j: Integer;
  RenRec: pZipRenameRec;
  FastRenamePossible: boolean; //new files of the same length as old
  found: boolean;
begin
  FastRenamePossible := true; //default
  FInFileName := ArchiveFileName;
  FInFileHandle := -1;
  RenRec := nil;

  // Check the input file.
  if not FileExists(FInFileName) then begin
    raise EFileNotFound.Create('File not found', ArchiveFileName, '')
  end;

  //If we only have a source path make sure the destination is also a path.
  for i := 0 to RenameList.Count - 1 do begin
    RenRec := RenameList.Items[i];
    RenRec^.Source := ReplaceForwardSlash(RenRec^.Source);
    RenRec^.Dest := ReplaceForwardSlash(RenRec^.Dest);
    FastRenamePossible := FastRenamePossible and (length(RenRec^.Source) = length(RenRec^.Dest));
    if Length(ExtractFileName(RenRec^.Source)) = 0 then {// Assume it's a path.} begin // Make sure destination is a path also.
      RenRec^.Dest := AppendSlash(ExtractFilePath(RenRec^.Dest));
      RenRec^.Source := AppendSlash(RenRec^.Source);
    end
    else if Length(ExtractFileName(RenRec^.Dest)) = 0 then        begin
      raise ESystemError.Create('Can''t rename file to path or path to file.', '', '')
    end;
  end;


  if FastRenamePossible then begin
    //direct access to original file

    //déprotège le fichier source
    UnprotectFile(FInFileName);

    //update date of input file (for future cache synchronisation)
    SetFileLastWrite(FInFileName, Now);

    // Open the input archive
    FInFileHandle := FileOpen(FInFileName, fmShareExclusive or fmOpenReadWrite);
    if FInFileHandle = -1 then begin
      raise ESystemError.Create('Can''t open file.', ArchiveFileName, '')
    end;

    // The following function will read the EOC
    CheckIfLastDisk(EOC);
    AllocSpanMem(EOC.TotalEntries); // Allocate memory for MDZD
    try
      // Go to the start of the Central directory.
      if FileSeek(FInFileHandle, EOC.CentralOffset, 0) = -1 then begin
        raise ESystemError.Create('Can''t read file.', ArchiveFileName, '')
      end;

      // Rename entries in the central header
      for i := 0 to (EOC.TotalEntries - 1) do begin

        Application.ProcessMessages;

        // Read a central header.
        if FileRead(FInFileHandle, CEH, SizeOf(CEH)) <> SizeOf(CEH) then begin
          raise ESystemError.Create('Can''t read file.', ArchiveFileName, '')
        end;

        if CEH.HeaderSig <> CentralFileHeaderSig then begin
          raise ECorrupted.create('Wrong Central Header signature.', ArchiveFileName, '')
        end;

        // Now the filename.

        if FileRead(FInFileHandle, Buffer, CEH.FileNameLen) <> CEH.FileNameLen then begin
          raise ESystemError.Create('Can''t read file.', ArchiveFileName, '')
        end;

        // Save the file name info in the MDZD structure.
        MDZDp := MDZD[i];
        MDZDp^.FileNameLen := CEH.FileNameLen;
        StrLCopy(MDZDp^.FileName, Buffer, CEH.FileNameLen);
        MDZDp^.RelOffLocal := CEH.RelOffLocal;
        MDZDp^.DateTime := DateTime;

        //search for the new filename in the list
        found := false;
        for j := 0 to RenameList.Count - 1 do begin
          RenRec := RenameList.Items[j];
          if ReplaceForwardSlash(RenRec^.Source) = string(Buffer) then begin
            found := true;
            break;
          end;
        end;

        if found then begin

          //go back to the begining of the filename
          if FileSeek(FInFileHandle, -CEH.FileNameLen, 1) = -1 then begin
            raise ESystemError.Create('Can''t read file.', ArchiveFileName, '')
          end;

          //change filename
          StrLCopy(Buffer, PAnsiChar(ansistring(ReplaceForwardSlash(RenRec^.Dest))), CEH.FileNameLen);

          //write new filename
          if FileWrite(FInFileHandle, buffer, CEH.FileNameLen) <> CEH.FileNameLen then begin
            raise ESystemError.Create('Can''t write file ' + FInFileName, '', '')
          end;
        end;

        // Seek past the extra field and the file comment.
        if FileSeek(FInFileHandle, CEH.ExtraLen + CEH.FileComLen, 1) = -1 then begin
          raise ESystemError.Create('Can''t read file.', ArchiveFileName, '')
        end;

        //erase buffer
        FillChar(Buffer, CEH.FileNameLen, #0);

      end; //next file

      // Rename entries in the local header
      for i := 0 to (EOC.TotalEntries - 1) do begin

        Application.ProcessMessages;

        // Seek to the first entry.
        MDZDp := MDZD[i];

        //go to the start of local header
        FileSeek(FInFileHandle, MDZDp^.RelOffLocal, 0);

        // Read the local header.
        if FileRead(FInFileHandle, LOH, SizeOf(LOH)) <> SizeOf(LOH) then begin
          raise ESystemError.Create('Can''t read file.', ArchiveFileName, '')
        end;

        //check we are correct
        if LOH.HeaderSig <> LocalFileHeaderSig then begin
          raise ECorrupted.create('Wrong Local Header signature.', ArchiveFileName, '')
        end;

        // Read the filename. (after the loh)
        if FileRead(FInFileHandle, Buffer, LOH.FileNameLen) <> LOH.FileNameLen then begin
          raise ESystemError.Create('Can''t read file.', ArchiveFileName, '')
        end;

        //search for the new filename in the list
        found := false;
        for j := 0 to RenameList.Count - 1 do begin
          RenRec := RenameList.Items[j];
          if ReplaceForwardSlash(RenRec^.Source) = string(Buffer) then begin
            found := true;
            break;
          end;
        end;

        if found then begin

          //go back to the begining of the filename
          if FileSeek(FInFileHandle, -LOH.FileNameLen, 1) = -1 then begin
            raise ESystemError.Create('Can''t read file.', ArchiveFileName, '')
          end;

          //Change and write the filename.
          StrLCopy(Buffer, PAnsiChar(ansistring(ReplaceForwardSlash(RenRec^.Dest))), LOH.FileNameLen);

          //write new filename
          if FileWrite(FInFileHandle, buffer, LOH.FileNameLen) <> LOH.FileNameLen then begin
            raise ESystemError.Create('Can''t write file ' + FInFileName, ArchiveFileName, '')
          end;

          // Change Date and Time if needed.
          if RenRec^.DateTime <> 0 then begin
            LOH.ModifDate := HIWORD(MDZDp^.DateTime);
            LOH.ModifTime := LOWORD(MDZDp^.DateTime);
          end;
        end;

        //erase buffer
        FillChar(Buffer, LOH.FileNameLen, #0);

      end; // Next entry

    finally
      DeleteSpanMem;
      if FInFileHandle <> -1 then begin
        FileClose(FInFileHandle)
      end;
    end;

  end
  else begin

    //******************************************************************************
    //******************************************************************************

    //slow rename (create a new file)
    ZipMaster.ZipFileName := ArchiveFileName;
    if ZipMaster.Rename(RenameList, datetime,htrFull) <> 0 then begin
      raise EArchiveException.Create(TXT_ERRORINZIP, ArchiveFileName, '');
    end;

    //******************************************************************************
    //******************************************************************************
  end;
end;

function TRcZip.ReplaceForwardSlash(aStr: string): string;
begin
  Result := StringReplace(aStr,'/','\',[rfReplaceAll]);
end;

procedure TRCZip.SetComments(ArchiveName,Comment: string);
begin
    ZipMaster.ZipFileName := ArchiveName;
    ZipMaster.ZipComment := Comment;
end;

function TRcZip.AppendSlash(sDir: string): string;
begin
  if (sDir <> '') and (sDir[Length(sDir)] <> '\') then    begin
    Result := sDir + '\'
  end
  else begin
    Result := sDir
  end;
end;

//---------------------------------------------------------------------------
// Function to find the EOC record at the end of the archive (on the last disk.)
// We can get a return value( true::Found, false::Not Found ) or an exception if not found.
function TRcZip.CheckIfLastDisk(var EOC: ZipEndOfCentral): boolean;
var
  Sig: Cardinal;
  Size, FFileSize, i: Integer;
  ZipBuf: PAnsiChar;
begin
  //init
  FZipComment := '';
  ZipBuf := nil;
  FZipEOC := 0;

  //read first signature
  if FileRead(FInFileHandle, Sig, 4) <> 4 then begin
    raise ESystemError.Create('Can''t read file.', '', '')
  end;

  // check if archive is spanned
  if Sig = SpannedSig then begin
    raise EError.Create('Spanned archive. (Skipped)', '', '')
  end;

  //   if (Sig <> LocalFileHeaderSig) and (Sig <> EndCentralDirSig) then raise ECorrupted.Create('Bad header.');

  //essaye de localiser l'eoc
  //passe 1: sans zipcomment
  //se positionne à la fin - EOC
  FFileSize := FileSeek(FInFileHandle, -SizeOf(EOC), 2);
  if FFileSize <> -1 then begin
    Inc(FFileSize, SizeOf(EOC)); // Save the archive size.
    if (FileRead(FInFileHandle, EOC, SizeOf(EOC)) = SizeOf(EOC)) and
      (EOC.HeaderSig = EndCentralDirSig) then begin
      //trouvé !
      FZipEOC := FFileSize - SizeOf(EOC);
      Result := True;
      Exit;
    end;
  end;

  //if not found, we try to find the EOC record within the last 65535 + sizeof( EOC ) bytes
  // of this file because Zip archive comment length is max 65535
  try
    Size := 65535 + SizeOf(EOC);
    if FFileSize < Size then begin
      Size := FFileSize
    end;

    GetMem(ZipBuf, Size + 1);

    if FileSeek(FInFileHandle, -Size, 2) = -1 then begin
      raise ESystemError.Create('Can''t read file.', '', '')
    end;
    if (FileRead(FInFileHandle, ZipBuf^, Size) <> Size) then begin
      raise ESystemError.Create('Can''t read file.', '', '')
    end;

    //scan for EOC signature
    for i := Size - SizeOf(EOC) - 1 downto 0 do begin
      if (ZipBuf[i] = 'P') and (ZipBuf[i + 1] = 'K') and (ZipBuf[i + 2] = #$05) and (ZipBuf[i + 3] = #$06) then begin
        //signature found
        FZipEOC := FFileSize - Size + i;
        Move(ZipBuf[i], EOC, SizeOf(EOC)); // Copy from our buffer to the EOC record.

        // If we have ZipComment: Save it, must be after Garbage check because a #0 is set!
        if EOC.ZipCommentLen <> 0 then begin
          ZipBuf[i + SizeOf(EOC) + EOC.ZipCommentLen] := #0;
          FZipComment := string(ZipBuf + i + SizeOf(EOC)); // No codepage translation yet, wait for CEH read.
        end;
        FreeMem(ZipBuf);
        Result := True;
        Exit;
      end
    end;
    FreeMem(ZipBuf);
  except
    FreeMem(ZipBuf);
  end;

  Result := False;
end;

//---------------------------------------------------------------------------
procedure TRcZip.AllocSpanMem(TotalEntries: Integer);
var
  i: Integer;
begin
  MDZD := TList.Create;

  MDZD.Capacity := TotalEntries;
  for i := 1 to TotalEntries do begin
    New(MDZDp);
    MDZDp^.FileName := '';
    MDZD.Add(MDZDp);
  end;
end;

//---------------------------------------------------------------------------
procedure TRcZip.DeleteSpanMem;
var
  i: Integer;
begin
  if not Assigned(MDZD) or (MDZD.Count = 0) then    begin
    Exit
  end;
  for i := (MDZD.Count - 1) downto 0 do begin
    if Assigned(MDZD[i]) then begin
      // dispose of the memory pointed-to by this entry
      MDZDp := MDZD[i];
      Dispose(MDZDp);
    end;
    MDZD.Delete(i); // delete the TList pointer itself
  end;
  MDZD.Free;
  MDZD := nil;
end;


destructor TRCZip.Destroy;
begin
  ZipMaster.Free;
  inherited;
end;

initialization
  zip := TRCZip.Create;

finalization
  zip.Free;
end.

