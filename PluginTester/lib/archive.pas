unit archive;

interface
uses Classes,sysutils;

type
  EArchiveException = class(Exception)
  public
    ArchiveName:string;
    Entry:string;
    constructor create(Msg,ArchiveName,Entry:string);
  end;

  ECorrupted = class(EArchiveException);
  EError = class(EArchiveException);
  EMemoryError  = class(EArchiveException);
  EADiskFull  = class(EArchiveException);
  ESystemError = class(EArchiveException);

  EFileNotFound = class(EError);
  EFileExist = class(EError);
  ENoFileSpecified = class(EError);
  EPathExist = class(EError);
  EPathNotFound = class(EError);

  TArchiveItem = class
  private
    function GetFullName: string;
  public
    Name:string;
    Path:string;
    UncompSize:integer;
    crc:string;
    Date:TDateTime;
    property FullName:string read GetFullName;
  end;

  TArchiveItems = class(TList)
  private
    function GetItems(Index: Integer): TArchiveItem;
    { Déclarations privées }
  public
    { Déclarations publiques }
    destructor Destroy;override;
    property ItemsArchive[Index: Integer]: TArchiveItem read GetItems; default;
  end;

  TInfosEvent = Procedure(infos: String) Of Object;
  TCorruptedEvent = Procedure(ArchiveName,Entry,msg: String) Of Object;

  TArchive = class
  private
    FCompressionLevel: integer;
    { Déclarations privées }
  protected
    Items:TArchiveItems;
    FOnInfos: TInfosEvent;
    FOnCorrupted: TCorruptedEvent;
    procedure SetCompressionLevel(const Value: integer);
  public
    { Déclarations publiques }
    TempDir:string;
    Property OnCorrupted: TCorruptedEvent Read FOnCorrupted write FOnCorrupted;
    Property OnInfos: TInfosEvent Read FOnInfos Write FOnInfos;
    Property CompressionLevel:integer read FCompressionLevel write SetCompressionLevel;
    constructor Create;
    destructor Destroy;override;

    procedure GetContent(filename: string;it:TArchiveItems);virtual;
    procedure ExtractFile(ArchiveName, FileToExtract,DestDir: string;ExtractPath:boolean);virtual;
    procedure AddFile(FileName : string; FileToAdd: String);virtual;
    procedure TestFile(FileName : string);virtual;
    procedure RenameEntries(ArchiveName:string;RenList:TList;FastRename:boolean); virtual;
    procedure DeleteEntry(ArchiveName,FileToDelete:string;BackupFolder:string);virtual;
    procedure CreateEmptyArchive(ArchiveName:string);virtual;
    procedure ExtractAll(FileName: string;Path:string);virtual;
    //le path ne doit pas exister, il est créé dans la procédure
    procedure AddAll(Path, FileName: string);virtual;
    procedure SetComments(ArchiveName,Comment: string);virtual;
    function GetComments(ArchiveName:string):string;virtual;
  end;

implementation

{ TArchive }

procedure TArchive.AddAll(Path, FileName: string);
begin
end;

procedure TArchive.AddFile(FileName, FileToAdd: String);
begin
end;

constructor TArchive.Create;
begin
end;

procedure TArchive.CreateEmptyArchive(ArchiveName: string);
begin

end;

procedure TArchive.DeleteEntry(ArchiveName, FileToDelete: string;BackupFolder:string);
begin
end;

destructor TArchive.Destroy;
begin
//  inherited;
end;

procedure TArchive.ExtractAll(FileName, Path: string);
begin

end;

procedure TArchive.ExtractFile(ArchiveName, FileToExtract,DestDir: string;ExtractPath:boolean);
begin

end;

function TArchive.GetComments(ArchiveName:string):string;
begin

end;

procedure TArchive.GetContent(filename: string;it:TArchiveItems);
begin

end;

procedure TArchive.RenameEntries(ArchiveName: string; RenList: TList;
  FastRename: boolean);
begin

end;

//Remplace archive comment
procedure TArchive.SetComments(ArchiveName, Comment: string);
begin

end;

procedure TArchive.SetCompressionLevel(const Value: integer);
begin
  FCompressionLevel := Value;
end;

procedure TArchive.TestFile(FileName: string);
begin

end;

{ TArchiveItems }

destructor TArchiveItems.destroy;
var
  i:integer;
begin
  for i := 0 to Count - 1 do begin
    ItemsArchive[i].Free;
  end;

  inherited;

end;

function TArchiveItems.GetItems(Index: Integer): TArchiveItem;
begin
  result := TArchiveItem(Items[index]);
end;

{ TArchiveItem }

function TArchiveItem.GetFullName: string;
begin
  if path = '\' then result := Name
  else result := IncludeTrailingPathDelimiter(Path) + Name;
end;

{ EArchiveException }

constructor EArchiveException.create(Msg, ArchiveName, Entry: string);
begin
  inherited create(Msg);
  if archivename <> '' then self.ArchiveName := ArchiveName;
  if Entry <> '' then self.Entry := Entry;
end;

end.
