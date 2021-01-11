Unit FilesTools;

Interface

Uses SysUtils, windows, forms, ShellAPI,classes,archivemanager,filectrl,archive;

resourcestring
  TXTs_FILENOTFOUND = 'File %s Not Found.';
  TXT_FILEERROR = 'File Error during transfert';
  TXT_CANTMOVEFILE = 'Can''t move file. (readonly)';
  TXT_CANTCOPYFILE = 'Can''t copy file.';
  TXT_CANTDELETEORIGINAL = 'Can''t delete original.';
  TXT_DISKISFULL = 'Disk %s is full';

Type
  EFCantMove = Class(Exception);
  EDiskFull = Class(Exception);
  EFileError = Class(Exception);
  EReadOnly = Class(Exception);

  TDirInfo = Record
    files: Integer;
    dirs: Integer;
    size: Integer;
  End;

Function DirInfo(Dirname: String; Subs: Boolean): TDirInfo;
Function HasAttr(Const FileName: String; Attr: Word): Boolean;
Function isReadOnly(path: String): boolean;
Function ChooseDirectory(root: String): String;
Function ExtractPathDir(Path: String): String; //retourne la partie dir du path complet (sans drive, ni file)
Function ExtractLastFolderAndFileName(FullFileName:string):string; //c:/a/b/c/fn.ext -> c/fn.ext (used for log msg)
Function PathToShortPath(PathName:string):string; //c:\a\b\c\ -> c:..\b\c (used for rom paths node)
Function FileExecute(sExecutableFilePath: String; wait: boolean; minimize: boolean): integer;
Function FileExecuteShell(Const FileName, Params, StartDir: String; Wait: Boolean; WS_State: integer): Integer;
Procedure renommer(Handle: HWND; Source, Cible: String);
Procedure deplacer(Handle: HWND; Source, Cible: String; RenameOnCollision: Boolean);
Procedure effacer(Handle: HWND; Source: String);
Procedure copier(Handle: HWND; Source, Cible: String);
Procedure ToLowerCase(FileName: String);
Function SizeOfFile(FileName: String): int64;
Function NbOfFiles(Path: String): integer;
Procedure MoveFile(Const FileName, DestName: String);
Function WindowsTempDir: String;
Function UnprotectFile(filename: String): boolean;

//contrôle que l'espace disque est suffisant pour stocker la rom complete dézipé
//contrôle sur archive path et sur path
//retourne une exception EDiskFull
Procedure CheckDiskFreeSpace(octets: Int64; path: String);

//retourne la date et l'heure (UTC) de modif du fichier ou path
function DateOfFile(AFile: string): TDateTime;

//return the date (filetime) of the most recent file in the path
Function LastChangeInPath(Path: String): TDateTime;

//return age of directory (fileage does not work for paths)
function DirAge(const PathName: string): Integer;

function FileExistsEx(filename:string;CaseSensitive:boolean=false):boolean;

function DirectoryExistsEx(filename:string;CaseSensitive:boolean=false):boolean;

function CreateDummyFile(filename:string;Length:integer;Content:char = #00):boolean;

procedure DskRenameFile(Pathname,Name,NewName: string);

procedure DskRenameRomFile(Arch:TArchive;SrcFile,SrcRomName,DstRomName:string);overload;


procedure DskDeleteFile(AFile: string;ArchivePath:string);

procedure DskMoveFile(SrcFileName,DstFileName: string);
procedure DskCopyFile(SrcFileName,DstFileName: string);

function GetUniqueFileName(Path,filename:string):string;

//dtUnknown, dtNoDrive, dtFloppy, dtFixed, dtNetwork, dtCDROM, dtRAM
function DriveTypeToString(drivetype:TDriveType):string;

function ExcludeDrive(pathname:string):string;//return path without drive (beginning without \)

procedure GetLogicalDrives(Drives: TStringList);//return list of used drive letters

procedure TestReadOnly(PathOrFile:string); //raise a EReadOnly exception

procedure TestExist(PathOrFile:string); //raise a EFileNotFound or EPathNotFound exception

Implementation

Uses stringstools, diskinfo,JclFileUtils,JclDateTime,jclshell,JclStrings,
  Dialogs;

function DateOfFile(AFile: string): TDateTime;
Begin
  Result := -1;
  if not FileExists(AFile) and not DirectoryExists(AFile) then raise EFileError.create(format('File %s not found',[AFile]));

  AFile := ExcludeTrailingPathDelimiter(AFile);
  GetFileLastWrite(afile,result);
end;

Function WindowsTempDir: String;
Var
  TempPathName: Array[0..512] Of Char;
Begin
  GetTempPath(MAX_PATH, @TempPathName);
  Result := IncludeTrailingPathDelimiter(TempPathName);
End;

Function SizeOfFile(FileName: String): int64;
Begin
  if not FileExists(FileName) and not DirectoryExists(FileName) then raise EFileError.create(format('File %s not found',[FileName]));

  if isdirectory (FileName) then result := 0
  else result := GetSizeOfFile(filename); //int64
End;

Function NbOfFiles(Path: String): integer;
Var
  SearchRec: TSearchRec;
  SearchFiles:string;
Begin
  //charger tous les fichiers
  SearchFiles := IncludeTrailingPathDelimiter(Path) + '*';

  //compte le nombre de fichiers
  Result := 0;
  If FindFirst(SearchFiles, faAnyFile, SearchRec) = 0 Then Begin
    Repeat

      //filtre
      If (SearchRec.Name = '.') Or (SearchRec.Name = '..') Then continue;

      Result := Result + 1;

      Application.ProcessMessages;

    Until FindNext(SearchRec) <> 0 //plus de fichiers
  End;
  sysutils.FindClose(SearchRec);
End;

Function LastChangeInPath(Path: String): TDateTime;
Var
  SearchRec: TSearchRec;
  SearchAttr:integer;
  SearchFiles:string;
  filename:string;
Begin
  //charger tous les fichiers
  SearchAttr := faAnyFile;
  SearchFiles := IncludeTrailingPathDelimiter(Path) + '*';

  //last file change date not what expected ?
  Result := 0;
  If FindFirst(SearchFiles, SearchAttr, SearchRec) = 0 Then Begin
    Repeat

      //filtre
      If (SearchRec.Name = '.') Or (SearchRec.Name = '..') Then continue;
      filename := IncludeTrailingPathDelimiter(Path) + SearchRec.Name;

      if dateoffile(filename) >= Result then Result := dateoffile(filename);
    Until FindNext(SearchRec) <> 0 //plus de fichiers
  End;
  sysutils.FindClose(SearchRec);
End;

Procedure ToLowerCase(FileName: String);
Begin
  TestExist(Filename);
  If Not RenameFile(FileName, LowerCase(FileName)) Then Begin
    //Raise an exception
    Raise EFileError.create(SysErrorMessage(GetLastError));
  End;
End;

Procedure copier(Handle: HWND; Source, Cible: String);
Var
  fos: TSHFileOpStruct;
Begin
  FillChar(fos,sizeof(fos),0);
  fos.Wnd := handle;
  fos.wFunc := FO_COPY;
  fos.pFrom := Pchar(Source+#0);
  fos.pTo := Pchar(Cible+#0);
  fos.fFlags := FOF_ALLOWUNDO + FOF_SILENT + FOF_NOCONFIRMATION; // + FOF_RENAMEONCOLLISION; //copy (1) of..
  If SHFileOperation(fos) <> 0 Then Begin
    //Raise an exception
    Raise EFileError.Create(TXT_FILEERROR);
  End;
End;

Procedure effacer(Handle: HWND; Source: String);
Var
  fos: TSHFileOpStruct;
Begin
  FillChar(fos,sizeof(fos),0);
  fos.Wnd := handle;
  fos.wFunc := FO_DELETE;
  fos.pFrom := Pchar(Source+#0);
  fos.fFlags := FOF_ALLOWUNDO + FOF_SILENT + FOF_NOCONFIRMATION; // + FOF_RENAMEONCOLLISION; //copy (1) of..
  If SHFileOperation(fos) <> 0 Then Begin
    //Raise an exception
    Raise EFileError.Create(TXT_FILEERROR);
  End;
End;

Procedure deplacer(Handle: HWND; Source, Cible: String; RenameOnCollision: Boolean);
Var
  fos: TSHFileOpStruct;
Begin
  FillChar(fos,sizeof(fos),0);
  fos.Wnd := handle;
  fos.wFunc := FO_MOVE;
  fos.pFrom := Pchar(Source+#0);
  fos.pTo := Pchar(Cible+#0);
  fos.fFlags := FOF_ALLOWUNDO + FOF_SILENT + FOF_NOCONFIRMATION;
  If RenameOnCollision Then
    fos.fFlags := fos.fFlags + FOF_RENAMEONCOLLISION; //copy (1) of..
  If SHFileOperation(fos) <> 0 Then Begin
    //Raise an exception
    Raise EFileError.Create(TXT_FILEERROR);
  End;
End;


Procedure renommer(Handle: HWND; Source, Cible: String);
Var
  fos: TSHFileOpStruct;
Begin
  FillChar(fos,sizeof(fos),0);
  fos.Wnd := handle;
  fos.wFunc := FO_RENAME;
  fos.pFrom := Pchar(Source+#0);
  fos.pTo := Pchar(Cible+#0);
  fos.fFlags := FOF_ALLOWUNDO + FOF_SILENT + FOF_NOCONFIRMATION; // + FOF_RENAMEONCOLLISION; //copy (1) of..
  If SHFileOperation(fos) <> 0 Then Begin
    //Raise an exception
    Raise EFileError.Create(TXT_FILEERROR);
  End;
End;

Function HasAttr(Const FileName: String; Attr: Word): Boolean;
Var
  FileAttr: Integer;
Begin
  FileAttr := FileGetAttr(FileName);
  If FileAttr = -1 Then FileAttr := 0;
  Result := (FileAttr And Attr) = Attr;
End;

Procedure MoveFile(Const FileName, DestName: String);
Var
  Destination: String;
Begin
  Destination := ExpandFileName(DestName); { expand the destination path }
  If Not RenameFile(FileName, Destination) Then { try just renaming }  Begin
    If HasAttr(FileName, faReadOnly) Then { if it's read-only... }
      Raise EFCantMove.Create(TXT_CANTMOVEFILE); { we wouldn't be able to delete it }
    If Not CopyFile(pchar(FileName), pchar(Destination), false) Then
      Raise EFCantMove.Create(TXT_CANTCOPYFILE + #10 + SysErrorMessage(GetLastError)); { we wouldn't be able to delete it }
    If Not DeleteFile(pchar(FileName)) Then
      Raise EFCantMove.Create(TXT_CANTDELETEORIGINAL + #10 + SysErrorMessage(GetLastError)); { we wouldn't be able to delete it }
  End;
End;

Function isReadOnly(path: String): boolean;
var
  F: File;
  ErrorMode: Word;
  ds:int64;
  drive:Byte;
begin
  ErrorMode := SetErrorMode(SEM_FAILCRITICALERRORS);
  try

    //check if media is valid
    drive := ord(uppercase(path)[1]) - ord('A')+1;
    //do not use that with network share (begin with \\)
    //only with mapped drives
    if (drive >=1) and (drive <=26) then begin //mapped
      ds:= DiskSize(drive);
      //If the drive is invalid, or no media, -1 is returned
      //If the drive is read-only, 0 is returned
      //but doesn't work with usb write protected...
      if ds = -1 then
      begin
        Result := true;
        exit;
      end;
    end;

    //check if readonly (above method doesn't work with usb write protected)
    try
      //try to create a file, exception -> read only
      AssignFile(F,IncludeTrailingPathDelimiter(path)+'_$.$$$');
      Rewrite(F);
      CloseFile(F);
      Erase(F);
      Result:=False;
    except
      Result:=True;
    end;
  finally
    SetErrorMode(ErrorMode);
  end;
End;


Function FileExecute(sExecutableFilePath: String; wait: boolean; minimize: boolean): integer;
Var
  pi: TProcessInformation;
  si: TStartupInfo;
  CreateOk: boolean;
  ExitCode: cardinal;
Begin
  Result := -10000;
  FillMemory(@si, sizeof(si), 0);
  si.cb := sizeof(si);
  si.dwFlags := STARTF_USESHOWWINDOW;
  If minimize Then si.wShowWindow := SW_MINIMIZE
  Else si.wShowWindow := SW_NORMAL;

  CreateOK := CreateProcess(
    Nil,

    // path to the executable file:
    PChar(sExecutableFilePath),

    Nil, Nil, False,
    NORMAL_PRIORITY_CLASS, Nil, Nil,
    si, pi);

  If CreateOK Then Begin
    If wait Then Begin
      //Application.Minimize;
      //may or may not be needed. Usually wait for child processes
      WaitForSingleObject(pi.hProcess, INFINITE);
      GetExitCodeProcess(pi.hProcess, ExitCode);
      Result := ExitCode;
    End;
    Application.Restore;
  End;
  CloseHandle(pi.hProcess);
  CloseHandle(pi.hThread);
End;

Function ChooseDirectory(root: String): String;
Var
  orig: String;
Begin
  orig := root;
  If Not DirectoryExists(root) Then root := '.';
  If SelectDirectory(root, [], 0) Then result := root
  Else result := '';
End;

Function ExtractPathDir(Path: String): String; //retourne la partie dir du path complet (sans drive, ni file)
Var
  dir: String;
Begin

  dir := ExtractFileDir(ExpandFileName(Path));
  Result := IncludeTrailingPathDelimiter(Str_Extract(dir, 3, Length(Path)));
End;

Function FileExecuteShell(Const FileName, Params, StartDir: String; Wait: Boolean; WS_State: integer): Integer;
Var
  Info: TShellExecuteInfo;
  ExitCode: DWORD;
Begin

  FillChar(Info, SizeOf(Info), 0);
  Info.cbSize := SizeOf(TShellExecuteInfo);
  With Info Do Begin
    fMask := SEE_MASK_NOCLOSEPROCESS;
    Wnd := Application.Handle;
    lpFile := PChar(FileName);
    lpParameters := PChar(Params);
    lpDirectory := PChar(StartDir);
    nShow := WS_State;
  End;

  If ShellExecuteEx(@Info) Then Begin
    Result := 1;
    If wait Then Begin
      Repeat
        Application.ProcessMessages;
        GetExitCodeProcess(Info.hProcess, ExitCode);
      Until (ExitCode <> STILL_ACTIVE) Or Application.Terminated;
      Result := ExitCode;
    End;
  End
  Else Begin //erreur
    Result := GetLastError;

    Case result Of
      0: Raise Exception.Create('The operating system is out of memory or resources.');
      ERROR_FILE_NOT_FOUND: Raise Exception.Create('The specified file was not found.');
      ERROR_PATH_NOT_FOUND: Raise Exception.Create('The specified path was not found.');
      ERROR_BAD_FORMAT: Raise Exception.Create('The .EXE file is invalid (non-Win32 .EXE or error in .EXE image).');
      SE_ERR_ACCESSDENIED: Raise Exception.Create('The operating system denied access to the specified file.');
      SE_ERR_ASSOCINCOMPLETE: Raise Exception.Create('The filename association is incomplete or invalid.');
      SE_ERR_DDEBUSY: Raise Exception.Create('The DDE transaction could not be completed because other DDE transactions were being processed.');
      SE_ERR_DDEFAIL: Raise Exception.Create('The DDE transaction failed.');
      SE_ERR_DDETIMEOUT: Raise Exception.Create('The DDE transaction could not be completed because the request timed out.');
      SE_ERR_DLLNOTFOUND: Raise Exception.Create('The specified dynamic-link library was not found.');
      SE_ERR_NOASSOC: Raise Exception.Create('There is no application associated with the given filename extension.');
      SE_ERR_OOM: Raise Exception.Create('There was not enough memory to complete the operation.');
      SE_ERR_SHARE: Raise Exception.Create('A sharing violation occurred.');
    End;
  End;
End;

//______________________________________________________________________________
Procedure CheckDiskFreeSpace(octets: Int64; path: String);
Var
  di: TDiskInfo;
  ErrorMode:Word;
Begin

  //calcul la taille disponible sur le disque (attentions aux unc)
  di := TDiskInfo.Create(Nil);
  try
    ErrorMode := SetErrorMode(SEM_FAILCRITICALERRORS);
    try
      di.Disk := ExtractFileDrive(path);
    finally
      SetErrorMode(ErrorMode);
    end;

    //si taille < octets + 5 MB, exception
    If di.DiskFree < octets + 5 * 1024 * 1024 Then
      Raise EDiskFull.Create( format(TXT_DISKISFULL,[di.Disk]));
  finally
    di.free;
  end;

End;

//______________________________________________________________________________
Function UnprotectFile(filename: String): boolean;
Var
  Attributes, NewAttributes: Word;
Begin
  If Not FileExists(filename) Then Begin
    result := false;
    exit;
  End;

  Attributes := FileGetAttr(filename);
  NewAttributes := Attributes And Not faReadOnly;
  If FileSetAttr(filename, NewAttributes) <> 0 Then
    result := false
  Else result := true;
End;

Function DirInfo(Dirname: String; Subs: Boolean): TDirInfo;
Var
  rec: TSearchRec;
  code: Integer;
Begin
  With Result Do Begin
    files := 0;
    dirs := 0;
    size := 0;
  End;

  Dirname := ExcludeTrailingPathDelimiter(Dirname);
  code := FindFirst(Dirname + '\*.*', faAnyfile, rec);

  While code = 0 Do Begin
    Inc(Result.size, rec.size);

    If ((rec.attr And faDirectory) <> 0) Then Begin
      If (rec.name[1] <> '.') Then Begin
        Inc(Result.dirs);

        If Subs Then With DirInfo(IncludeTrailingPathDelimiter(Dirname) + rec.name, True) Do Begin
            Inc(Result.files, files);
            Inc(Result.dirs, dirs);
            Inc(Result.size, size);

          End;
      End;
    End Else Inc(Result.files);

    code := FindNext(rec);
  End;
  sysutils.FindClose(rec);

End;

function FileExistsEx(filename:string;CaseSensitive:boolean=false):boolean;
var
  f:TSearchRec;
begin
  if not CaseSensitive then result := FileExists(filename)
  else begin
      FindFirst(filename,faAnyFile - faDirectory,f);
      result := (f.Name = ExtractFileName(filename));
      sysutils.FindClose(f);
  end;
end;

function DirectoryExistsEx(filename:string;CaseSensitive:boolean=false):boolean;
var
  f:TSearchRec;
begin
  if not CaseSensitive then result := FileExists(filename)
  else begin
      FindFirst(ExcludeTrailingPathDelimiter(filename),faDirectory,f);
      result := (f.Name = ExtractFileName(ExcludeTrailingPathDelimiter(filename)));
      sysutils.FindClose(f);
  end;
end;

function CreateDummyFile(filename:string;Length:integer;Content:char = #00):boolean;
VAR
  F : File;
begin
  TestReadOnly(filename);

  AssignFile(f, filename);
  Rewrite(f, 1);
  if length >0 then begin
    Seek(f, Length-1);
    BlockWrite(F, Content, 1);
  end;
  CLoseFile(F);
  result := true;
end;

function DirAge(const PathName: string): Integer;
var
  Handle: THandle;
  FindData: TWin32FindData;
  LocalFileTime: TFileTime;
begin
  Handle := FindFirstFile(PChar(ExcludeTrailingPathDelimiter(PathName)), FindData);
  if Handle <> INVALID_HANDLE_VALUE then
  begin
    Windows.FindClose(Handle);
    windows.FileTimeToLocalFileTime(FindData.ftLastWriteTime, LocalFileTime);
    if windows.FileTimeToDosDateTime(LocalFileTime, LongRec(Result).Hi,LongRec(Result).Lo) then
      Exit;
  end;
  Result := -1;
end;

procedure DskRenameFile(Pathname,Name,NewName: string);
//renomme le fichier/dir/zip par 'newname'
//génère une exception en cas de problèmes
var
  ok: boolean;
begin
  if (Name = NewName) or (NewName = '') then exit;

  //test if filename >= 255 char
  if length(Pathname + newname) > 255 then
  begin
    raise EFileError.Create('Pathname\Filename exceeds 255 characters.');
  end;

  TestReadOnly(Pathname);

  Pathname := IncludeTrailingBackslash(Pathname);

  //dir
  if IsDirectory(Name) then begin
    //rename dir
    Name := IncludeTrailingBackslash(name);
    NewName := IncludeTrailingBackslash(NewName);
    //Infos(StringReplace(TXT_RENAMING, '%1', Name, []));
    //renommer
    ok := Sysutils.RenameFile(Pathname + Name, Pathname + NewName);

    //erreur ?
    if not ok then begin
      //Lever une exception
      raise EFileError.create(SysErrorMessage(GetLastError));
    end;
  end
  else begin
    //rename file

    //Infos(StringReplace(TXT_RENAMING, '%1', Name, []));
    //renommer
    ok := Sysutils.renameFile(Pathname + Name, Pathname + NewName);

    //erreur ?
    if not ok then begin
      //Lever une exception
      raise EFileError.create(SysErrorMessage(GetLastError));
    end;
  end;

end;


procedure DskDeleteFile(AFile: string;ArchivePath:string);
//delete file (-> bin)
var
  Ok: boolean;
  NewFile:string;
begin
  if not FileExists(Afile) and not DirectoryExists(AFile) then exit; //nothing to do

  TestReadOnly(AFile);

  //suppression physique de l'objet
  UnprotectFile(AFile);

  if (ArchivePath = '') or (not DirectoryExists(ArchivePath)) then begin
    //efface (envoie dans la poubelle)
    if IsDirectory(ExcludeTrailingPathDelimiter(AFile)) then
      ok := SHDeleteFolder(0, ExcludeTrailingPathDelimiter(AFile), [doSilent, doAllowUndo])
    else
      ok := SHDeleteFiles(0, ExcludeTrailingPathDelimiter(AFile), [doSilent, doAllowUndo]);
  end
  else begin
    //move to archive path
    NewFile := ArchivePath+GetUniqueFileName(ArchivePath,AFile);
    DskMoveFile(AFile,NewFile);
    Ok := FileExists(NewFile) or DirectoryExists(NewFile);
  end;
  //erreur ?
  if not ok then begin
    //Lever une exception
    raise EFileError.create('Can''t delete file:'+SysErrorMessage(GetLastError));
  end;
end;

//

procedure DskRenameRomFile(Arch:TArchive;SrcFile,SrcRomName,DstRomName:string);
//change the romfile name in the zip
var
  RenItem: pRenameItem;
  RenList: TList;
  Path:string;
begin
  //TestReadOnly(SrcFile);

  if isdirectory(srcfile) then begin
    //rename romfile in a folder
    RenameFile(SrcFile + '\' + SrcRomName,SrcFile + '\' + DstRomName);
  end
  else if lowercase(ExtractFileExt(srcfile)) = '.zip' then begin
    //rename romfile in a zip
    RenList := TList.Create;
    new(RenItem);
    RenItem^.DateTime := 0;
    RenItem^.Source := SrcRomName;
    RenItem^.Dest := DstRomName;
    RenList.Add(renitem);
    try
      Arch.RenameEntries(SrcFile, RenList, true);
    finally
      //efface la liste
      Dispose(RenList[0]);
      RenList.Free;
    end;
  end
  else begin
    //file
    path := IncludeTrailingPathDelimiter(ExtractFilePath(SrcFile));
    RenameFile(Path + SrcRomName,Path + DstRomName);
  end;

end;

function GetUniqueFileName(Path,filename:string):string;
var
  n:integer;
  fn,ext:string;
begin
  path := IncludeTrailingPathDelimiter(path);
  result := extractfilename(filename);
  fn := ExcludeTrailingExtension(extractfilename(filename));
  ext := ExtractFileExt(filename);
  n :=1;
  while FileExists(path + result) or DirectoryExists(path+result) do begin
    result := fn + '_' + IntToStr(n) + ext;
    n := n + 1;
  end; //while
end;


procedure DskMoveFile(SrcFileName,DstFileName: string);
//renomme le fichier/dir/zip par 'newname'
//génère une exception en cas de problèmes

var
  ok: boolean;
begin
  if (SrcFileName = DstFileName) or (DstFileName = '') or (SrcFileName = '') then exit;

  TestReadOnly(SrcFileName);
  TestReadOnly(DstFileName);

  //dir
  if IsDirectory(SrcFileName) then begin
    //rename dir
    //SrcFileName := IncludeTrailingBackslash(SrcFileName);
    //DstFileName := IncludeTrailingBackslash(DstFileName);
    ok := MoveDirectory(SrcFileName, DstFileName);

    //erreur ?
    if not ok then begin
      //Lever une exception
      raise EFileError.create(SysErrorMessage(GetLastError));
    end;
  end
  else begin
    //rename file
    ok := Sysutils.renameFile(SrcFileName, DstFileName);

    //erreur ?
    if not ok then begin
      //Lever une exception
      raise EFileError.create(SysErrorMessage(GetLastError));
    end;
  end;

end;

procedure DskCopyFile(SrcFileName,DstFileName: string);
//renomme le fichier/dir/zip par 'newname'
//génère une exception en cas de problèmes

var
  ok: boolean;
begin
  if (SrcFileName = DstFileName) or (DstFileName = '') or (SrcFileName = '') then exit;

  TestReadOnly(DstFileName);

  //dir
  if IsDirectory(SrcFileName) then begin
    //rename dir
    //SrcFileName := IncludeTrailingBackslash(SrcFileName);
    //DstFileName := IncludeTrailingBackslash(DstFileName);
    ok := CopyDirectory(SrcFileName, DstFileName);
    //erreur ?
    if not ok then begin
      //Lever une exception
      raise EFileError.create(SysErrorMessage(GetLastError));
    end;
  end
  else begin
    //rename file
    ok := CopyFile(PWideChar(SrcFileName), PWideChar(DstFileName),true);

    //erreur ?
    if not ok then begin
      //Lever une exception
      raise EFileError.create(SysErrorMessage(GetLastError));
    end;
  end;

end;

//dtUnknown, dtNoDrive, dtFloppy, dtFixed, dtNetwork, dtCDROM, dtRAM
function DriveTypeToString(drivetype:TDriveType):string;
begin
  case drivetype of
    dtNoDrive: result := 'NoDrive' ;
    dtFloppy: result := 'Floppy' ; //also for usb key !
    dtFixed: result := 'Fixed' ;
    dtNetwork: result := 'Network' ;
    dtCDROM: result := 'CDROM' ;
    dtRAM: result := 'RAM' ;
  else result := 'Unknown' ;
  end;

end;

function ExcludeDrive(pathname:string):string;//return path without drive (beginning without \)
var
  drv : string;
begin
  drv := ExtractFileDrive(pathname);
  result := StrAfter(drv,pathname);
  if (result <> '') and (result[1] = '\') then result := StrAfter('\',result);
end;

procedure GetLogicalDrives(Drives: TStringList);
var
  MyStr: PChar;
  i, Length: Integer;
const
  Size: Integer = 200;
begin
  GetMem(MyStr, Size);
  Length:=GetLogicalDriveStrings(Size, MyStr);
  for i:=0 to Length-1 do
  begin
    if (lowercase(MyStr[i])>='a')and(lowercase(MyStr[i])<='z') then
      Drives.Add(MyStr[i]+':\');
  end;
  FreeMem(MyStr);
end;

procedure TestExist(PathOrFile:string); //raise a EFileNotFound or EPathNotFound exception
begin
{  if isdirectory(PathOrFile) then begin
    if not DirectoryExists(PathOrFile) then raise EFileNotFound.create(format(TXTs_PATHNOTFOUND, [PathOrFile]),PathOrFile,'');
  end
  else begin
    if FileExists(PathOrFile) then raise EPathNotFound.create(format(TXTs_FILENOTFOUND, [PathOrFile]),PathOrFile,'');
  end;
}end;

procedure TestReadOnly(PathOrFile:string); //raise a EReadOnly exception
begin
  if isdirectory(PathOrFile) then begin
    //test if path is writable
    if isReadOnly(PathOrFile) then raise EReadOnly.create('Folder is Read Only');
  end
  else begin
    //test if file path is writable
    if isReadOnly(ExtractFilePath(PathOrFile)) then raise EReadOnly.create('Folder is Read Only');
  end;
end;

Function ExtractLastFolderAndFileName(FullFileName:string):string;
// c:/a/b/fn.ext -> b/fn.ext (used for log msg)
begin
  result := ExtractFileName(ExcludeTrailingPathDelimiter(ExtractFilePath(FullFileName))) + '\' + ExtractFileName(FullFileName);
end;

Function PathToShortPath(PathName:string):string;
// c:\a\b\c\ -> c:\...\b\c (used for rom paths node)
// c:\a\b -> c:\a\b
begin
  if StrCharCount(pathname,'\') > 2 then
    result := ExtractFileDrive(pathname) + '\...\' + ExtractFileName(ExcludeTrailingPathDelimiter(pathname))
  else result := pathname;
end;

End.


