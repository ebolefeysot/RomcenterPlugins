unit archivemanager;
//{$define debug}
// gestion des erreurs
// methode      niveau           retour des erreurs
//
// GetContent   Archive          exceptions
// AddFile      Archive          exceptions
// ExtractFile  Archive + entry  exceptions
// TestFile     Archive + entry  evenement + exceptions systemes
// DeleteEntry  Archive          exceptions
// ExtractAll   Archive          exceptions

interface
uses Classes, archive, sysutils;

type
  EFileError = class(Exception);

  TOperation = (toRename); //opération à faire dans le thread

  TRenameItem = record
    Source: string;
    Dest: string;
    DateTime: Integer;
  end;
  PRenameItem = ^TRenameITem;

  TArchiveManager = class(TComponent)
  private
    FTempDir: string;
    FOnInfos: TInfosEvent;
    FOnCorrupted: TCorruptedEvent;
    FCompressionLevel: integer;
    procedure SetTempDir(const Value: string);
    procedure SetCompressionLevel(const Value: integer);
    { Déclarations privées }
  public
    { Déclarations publiques }
    property CompressionLevel: integer read FCompressionLevel write SetCompressionLevel;

    constructor Create(AOwner: TComponent); override;
    property OnInfos: TInfosEvent read FOnInfos write FOnInfos;
    property OnCorrupted: TCorruptedEvent read FOnCorrupted write FOnCorrupted;
    property TempDir: string read FTempDir write SetTempDir;
    function IsAnArchive(FileName: string): boolean;
    function GetArchiveObject(FileName: string): TArchive;
    function GetNewArchiveObject(FileName: string): TArchive;

    procedure GetContent(FileName: string; it: TArchiveItems);
    function IsEmpty(ArchiveName: string): boolean;
    procedure AddFile(ZipFileName: string; FileToAdd,FileNewName: string);
    procedure ExtractFile(ArchiveName, FileToExtract, DestDir: string; ExtractPath: boolean);
    procedure TestFile(FileName: string);
    procedure DeleteEntry(ArchiveName, FileToDelete: string;BackupFolder:string);
    procedure ExtractAll(FileName: string; Path: string);
    procedure AddAll(Path: string; FileName: string);

    procedure CreateEmptyArchive(ArchiveName: string);
    procedure RenameEntry(ArchiveName, FileToRename, NewName: string);
    procedure RenameEntries(ArchiveName: string; RenList: TList; FastRename: boolean);
    procedure CopyEntry(SrcArchive, SrcEntry, DstArchive, DstEntry: string);
    procedure MoveEntry(SrcArchive, SrcEntry, DstArchive, DstEntry: string);
    procedure ReZip(ArchiveName: string);
    function FileExistsInZip(Archivename,filename:string):boolean;
    procedure RemoveComments(Archivename:string);
    function GetComment(Archivename:string):string;
  end;

implementation
uses rcZip, filestools, windows, stringstools, jclfileutils, Dialogs, jclshell;

{ TArchiveManager }

procedure TArchiveManager.AddFile(ZipFileName, FileToAdd,FileNewName: string);
var
  a: TArchive;
  usetmpfile:boolean;
  olddir:string;
begin
{$IFDEF DEBUG}
  OutputDebugString(pchar('Add file ' + FileToAdd + ' in ' + ZipFileName));
{$ENDIF}
  usetmpfile := false;
  try
    if (FileNewName <> '') and (FileToAdd <> FileNewName) then begin //rename rom
      //copie rom to temp
      olddir := GetCurrentDir;
      SetCurrentDir(TempDir);

      if fileexists(TempDir + FileNewName) then sysutils.deletefile(TempDir + FileNewName);
      //copy file in temp
      if not FileCopy(FileToAdd,TempDir + FileNewName,true) then
        raise EFileError.Create(TempDir + FileNewName + ': ' + SysErrorMessage(GetLastError));
      usetmpfile := true;
      FileToAdd :=  TempDir + FileNewName;
    end;

    a := GetArchiveObject(ZipFileName);
    UnprotectFile(ZipFileName);
    a.OnCorrupted := OnCorrupted;
    a.OnInfos := OnInfos;
    a.AddFile(ZipFileName, FileToAdd);
  finally
    if usetmpfile then begin
      SetCurrentDir(olddir);
      sysutils.DeleteFile(FileToAdd);
    end;
  end;
end;

procedure TArchiveManager.CreateEmptyArchive(ArchiveName: string);
var
  a: TArchive;
begin
  a := GetArchiveObject(ArchiveName);
  a.OnCorrupted := OnCorrupted;
  a.OnInfos := OnInfos;
  a.CreateEmptyArchive(ArchiveName);
end;

procedure TArchiveManager.DeleteEntry(ArchiveName, FileToDelete: string;BackupFolder:string);
var
  a: TArchive;
begin
{$IFDEF DEBUG}
  OutputDebugString(pchar('Delete entry ' + FileToDelete + ' from ' + ArchiveName));
{$ENDIF}
  UnprotectFile(ArchiveName);
  a := GetArchiveObject(ArchiveName);
  a.OnCorrupted := OnCorrupted;
  a.OnInfos := OnInfos;
  a.DeleteEntry(ArchiveName, FileToDelete,BackupFolder);
end;

procedure TArchiveManager.ExtractFile(ArchiveName, FileToExtract, DestDir: string; ExtractPath: boolean);
var
  a: TArchive;
begin
{$IFDEF DEBUG}
  OutputDebugString(pchar('Extract ' + FileToExtract + ' from ' + ArchiveName + ' in ' + DestDir));
{$ENDIF}

  DestDir := IncludeTrailingPathDelimiter(DestDir);

  //Si un fichier de même nom existe déjà, on l'efface
  if fileexists(DestDir + FileToExtract) then begin
    UnprotectFile(DestDir + FileToExtract);
    sysutils.deletefile(DestDir + FileToExtract);
  end;

  a := GetArchiveObject(ArchiveName);
  a.OnCorrupted := OnCorrupted;
  a.OnInfos := OnInfos;
  a.ExtractFile(ArchiveName, FileToExtract, DestDir, ExtractPath);
  UnprotectFile(DestDir + FileToExtract);

end;

function TArchiveManager.GetComment(Archivename: string): string;
var
  a: TArchive;
begin
  a := GetArchiveObject(ArchiveName);
  if a = nil then raise EFileNotFound('Archive ' +ArchiveName+' not found');
  a.OnCorrupted := OnCorrupted;
  a.OnInfos := OnInfos;
  result := a.GetComments(ArchiveName);
end;

procedure TArchiveManager.GetContent(FileName: string; it: TArchiveItems);
var
  a: TArchive;
begin
{$IFDEF DEBUG}
  OutputDebugString(pchar('Get content of ' + FileName));
{$ENDIF}
  a := GetArchiveObject(FileName);
  a.OnCorrupted := OnCorrupted;
  a.OnInfos := OnInfos;
  a.GetContent(FileName, it);
end;

function TArchiveManager.IsAnArchive(FileName: string): boolean;
var
  a: TArchive;
begin
  a := GetArchiveObject(FileName);
  result := a <> nil;
end;

procedure TArchiveManager.RenameEntry(ArchiveName, FileToRename, NewName: string);
var
  a1: TArchive;
  FileToRenameShort: string; //sans path
begin
{$IFDEF DEBUG}
  OutputDebugString(pchar('Rename entry ' + FileToRename + ' from ' + ArchiveName + ' as ' + NewName));
{$ENDIF}

  if FileToRename = NewName then exit;
  FileToRenameShort := ExtractFileName(FileToRename);
  a1 := GetArchiveObject(ArchiveName);

  UnprotectFile(ArchiveName);

  SetCurrentDir(TempDir);

  //efface le fichier destination si il existe (sauf si on change la casse)
  if not sametext(FileToRename,NewName) then begin
    UnprotectFile(PChar(TempDir + NewName));
    sysUtils.DeleteFile(TempDir + NewName);
  end;

  //extraction sans le path
  a1.ExtractFile(ArchiveName, FileToRename, TempDir, false);
  UnprotectFile(TempDir + FileToRename);

  try

    //renomme le fichier
    if not RenameFile(TempDir + FileToRenameShort, TempDir + NewName) then
      raise EFileError.Create(TempDir + FileToRenameShort + ': ' + SysErrorMessage(GetLastError));

    //recompression
    a1.AddFile(ArchiveName, TempDir + NewName);

    //suppression de l'ancienne dans l'archive
    a1.DeleteEntry(ArchiveName, FileToRename,'');

  finally
    //efface le fichier renommé si existe
    if FileExists(TempDir + NewName) then begin
      UnprotectFile(PChar(TempDir + NewName));
      SysUtils.DeleteFile(TempDir + NewName);
    end;

    //efface le fichier original (si existe)
    if FileExists(TempDir + FileToRenameShort) then begin
      UnprotectFile(PChar(TempDir + FileToRenameShort));
      //envoie dans la poubelle
      SHDeleteFiles(GetDesktopWindow, TempDir + FileToRenameShort, [doSilent, doAllowUndo]);
    end;

    //suppression du répertoire temporaires
    //removedir(ExtractFilePath(FileToRename));

  end;

end;

procedure TArchiveManager.SetTempDir(const Value: string);
begin
  FTempDir := IncludeTrailingPathDelimiter(Value);
  Zip.TempDir := FTempDir;
end;

procedure TArchiveManager.TestFile(FileName: string);
var
  a: TArchive;
begin
{$IFDEF DEBUG}
  OutputDebugString(pchar('Test file ' + FileName));
{$ENDIF}
  a := GetArchiveObject(FileName);
  if a = nil then raise EFileNotFound('Archive ' +FileName+' not found');
  a.OnCorrupted := OnCorrupted;
  a.OnInfos := OnInfos;
  a.TestFile(FileName);
end;

procedure TArchiveManager.CopyEntry(SrcArchive, SrcEntry, DstArchive, DstEntry: string);
//The DstEntry is copied without folder
var
  asrc, adst: TArchive;
  olddir: string;
begin
{$IFDEF DEBUG}
  OutputDebugString(pchar('Copy entry ' + srcentry + ' from ' + SrcArchive + ' in ' + DstArchive + ' as ' + DstEntry));
{$ENDIF}
  asrc := GetArchiveObject(SrcArchive);
  adst := GetArchiveObject(DstArchive);

  SrcArchive := ExpandFileName(SrcArchive);
  DstArchive := ExpandFileName(DstArchive);

  if (asrc = nil) then raise EFileNotFound('Archive ' +SrcArchive+' not found');
  if (adst = nil) then raise EFileNotFound('Archive ' +DstArchive+' not found');

  olddir := GetCurrentDir;
  SetCurrentDir(TempDir);
  try

    //extraction arc sans le path
    try
      asrc.ExtractFile(SrcArchive, SrcEntry, TempDir, false);

      //test if file is extracted
      if not FileExists( TempDir + ExtractFileName(SrcEntry)) then begin
        //error in zip !
        raise ECorrupted.Create('Unzip error',SrcArchive,SrcEntry);
      end;

      UnprotectFile(TempDir + ExtractFileName(SrcEntry));
    except
      on e:exception do begin
        raise;
        //MessageDlg('erreur sur ' + SrcArchive, mtWarning, [mbOK], 0);
      end;
    end;

    //renommage
    if SrcEntry <> DstEntry then begin
      //both files can be the same, but with different case.

      //Delete existing file (if both files have different name)
      if (not SameText(ExtractFileName(SrcEntry), DstEntry)) and (FileExists(DstEntry)) then sysutils.DeleteFile(DstEntry);

      //renomme le fichier
      if not RenameFile(ExtractFileName(SrcEntry), DstEntry) then begin
        //erreur ?
        //Lever une exception
        raise EFileError.create(SysErrorMessage(GetLastError));
      end;
    end;

    //recompression (sans le path)
    UnprotectFile(DstArchive);
    try
      adst.AddFile(DstArchive, TempDir + DstEntry);//ExtractFileName(DstEntry));
    except
      on e:exception do begin
        raise;
        //MessageDlg('erreur sur ' + DstArchive, mtWarning, [mbOK], 0);
      end;
    end;

    //suppression du fichier temporaire
    sysutils.DeleteFile(ExtractFileName(DstEntry));

  finally
    SetCurrentDir(olddir)
  end;
end;

function TArchiveManager.GetArchiveObject(FileName: string): TArchive;
var
  ext: string;
begin
  result := nil;

  ext := ExtractFileExt(FileName);
  if SameText(ext, '.zip') then begin
    result := zip;
  end;
  if result <> nil then begin
    //result.OnInfos := OnInfos;
    //result.OnCorrupted := OnCorrupted;
  end;
end;

//get a new created archive object to handle filename
function TArchiveManager.GetNewArchiveObject(FileName: string): TArchive;
var
  ext: string;
begin
  result := nil;

  ext := ExtractFileExt(FileName);
  if SameText(ext, '.zip') then begin
    result := TRCZip.Create;
    result.TempDir := FTempDir;
    result.CompressionLevel := FCompressionLevel;
  end;
end;

procedure TArchiveManager.MoveEntry(SrcArchive, SrcEntry, DstArchive,
  DstEntry: string);
var
  a: TArchive;
begin
{$IFDEF DEBUG}
  OutputDebugString(pchar('Move entry ' + srcentry + ' from ' + SrcArchive + ' in ' + DstArchive + ' as ' + DstEntry));
{$ENDIF}
  CopyEntry(SrcArchive, SrcEntry, DstArchive, DstEntry);

  //efface le source
  a := GetArchiveObject(SrcArchive);
  if a = nil then raise EFileNotFound('Archive ' +SrcArchive+' not found');
  UnprotectFile(SrcArchive);
  a.DeleteEntry(SrcArchive, SrcEntry,'');
end;

constructor TArchiveManager.Create(AOwner: TComponent);
begin
  TempDir := WindowsTempDir;
end;

procedure TArchiveManager.SetCompressionLevel(const Value: integer);
begin
  FCompressionLevel := Value;
  Zip.CompressionLevel := Value;
end;

procedure TArchiveManager.ExtractAll(FileName, Path: string);
var
  a: TArchive;
begin
{$IFDEF DEBUG}
  OutputDebugString(pchar('Extract file ' + FileName));
{$ENDIF}
  a := GetArchiveObject(FileName);
  if a = nil then raise EFileNotFound('Archive ' +FileName+' not found');
  a.OnCorrupted := OnCorrupted;
  a.OnInfos := OnInfos;
  a.ExtractAll(FileName, path);
end;

procedure TArchiveManager.AddAll(Path, FileName: string);
var
  a: TArchive;
begin
{$IFDEF DEBUG}
  OutputDebugString(pchar('zip file ' + FileName));
{$ENDIF}
  a := GetArchiveObject(FileName);
  if a = nil then raise EFileNotFound('Archive ' +FileName+' not found');
  a.OnCorrupted := OnCorrupted;
  a.OnInfos := OnInfos;
  a.AddAll(Path, FileName);

end;

procedure TArchiveManager.ReZip(ArchiveName: string);
var
  dir: string;
  i: integer;
begin
  //quitte si pas un zip
  if not IsAnArchive(ArchiveName) then exit;

  //crée le dir
  dir := IncludeTrailingPathDelimiter(tempdir) + ExcludeTrailingExtension(ExtractFilename(ArchiveName));
  i := 0;
  while DirectoryExists(dir) do begin
    dir := dir + inttostr(i);
  end;

  //unzip
  ExtractAll(ArchiveName, Dir);

  //efface le zip original (dans la poubelle)
  UnprotectFile(ArchiveName);
  SHDeleteFiles(GetDesktopWindow, ArchiveName, [doSilent, doAllowUndo]);

  //rezip
  AddAll(dir, ArchiveName);

  //efface le dir
  DelTree(dir);
  RemoveDir(dir);
end;

function TArchiveManager.IsEmpty(ArchiveName: string): boolean;
var
  a: TArchive;
  it: TArchiveItems;
begin
{$IFDEF DEBUG}
  OutputDebugString(pchar('Is Empty ' + ArchiveName));
{$ENDIF}
//  result := false;
  a := GetArchiveObject(ArchiveName);
  if a = nil then raise EFileNotFound('Archive ' +ArchiveName+' not found');
  it := TArchiveItems.Create;
  try
    a.OnCorrupted := OnCorrupted;
    a.OnInfos := OnInfos;
    a.GetContent(ArchiveName, it);
    result := (it.Count = 0);
  finally
    it.free;
  end;

end;

{ ArchiveThread }

//remove comments in the zip file
procedure TArchiveManager.RemoveComments(Archivename: string);
var
  a: TArchive;
begin

  a := GetArchiveObject(ArchiveName);
  if a = nil then raise EFileNotFound('Archive ' +ArchiveName+' not found');
  a.OnCorrupted := OnCorrupted;
  a.OnInfos := OnInfos;
  a.SetComments(ArchiveName,'');
end;

procedure TArchiveManager.RenameEntries(ArchiveName: string; RenList: TList; FastRename: boolean);
var
  a: TArchive;
begin
{$IFDEF DEBUG}
  OutputDebugString(pchar('Fast rename entries of ' + ArchiveName));
{$ENDIF}

  if RenList.Count = 0 then exit;

  a := GetArchiveObject(ArchiveName);
  if a = nil then raise EFileNotFound('Archive ' +ArchiveName+' not found');
  a.OnCorrupted := OnCorrupted;
  a.OnInfos := OnInfos;
  a.RenameEntries(ArchiveName, RenList, true);

end;

function TArchiveManager.FileExistsInZip(Archivename, filename: string): boolean;
var
  i: integer;
  it: TArchiveItems;
  a: TArchive;
begin
  if not jclfileutils.fileexists(Archivename) then begin
    raise EFileNotFound.Create(format(TXT_FILENOTFOUND,[Archivename]),'','');
  end;

  if not IsAnArchive(Archivename) then begin
    raise EFileError.Create('File ' + Archivename + ' is not a zip');
  end
  else begin
    //lecture du zip
    it := TArchiveItems.Create;
    result := false;

    a := GetArchiveObject(ArchiveName);
    if a = nil then raise EFileNotFound('Archive ' +ArchiveName+' not found');
    a.OnCorrupted := OnCorrupted;
    a.OnInfos := OnInfos;
    try //finally
      a.GetContent(Archivename, it);

      //for each file of the zip
      for i := 0 to it.Count - 1 do begin
        if sametext(filename,it[i].Name) then begin
          result := true;
          break;
        end;
      end;
    finally
      it.Free;
    end;
  end;
end;
end.

