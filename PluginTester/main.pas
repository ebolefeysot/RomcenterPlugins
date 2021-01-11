unit main;

interface

uses
  SysUtils, Windows, Messages, Classes, Graphics, Controls,
  Forms, Dialogs, StdCtrls, Spin, ComCtrls, SignatureCalculator, CrcPlugIn;

type
  TForm1 = class(TForm)
    BtnGetCrc: TButton;
    lblcrc: TLabel;
    BtnUnloaddll: TButton;
    Label3: TLabel;
    BtnLoadDll: TButton;
    Label4: TLabel;
    Dll: TGroupBox;
    GroupBox1: TGroupBox;
    ListView1: TListView;
    Label5: TLabel;
    Label6: TLabel;
    Label7: TLabel;
    Label8: TLabel;
    Label9: TLabel;
    lblname: TLabel;
    lblauthor: TLabel;
    lblversion: TLabel;
    lblwebpage: TLabel;
    lblemail: TLabel;
    lbldat: TLabel;
    lblext: TLabel;
    Memo1: TMemo;
    LblFormat: TLabel;
    Label2: TLabel;
    LblSize: TLabel;
    Label10: TLabel;
    Label1: TLabel;
    Lblcomment: TLabel;
    edtFileName: TEdit;
    btnOpenFile: TButton;
    OpenDialog: TOpenDialog;
    procedure FormCreate(Sender: TObject);
    procedure BtnGetCrcClick(Sender: TObject);
    procedure BtnUnloaddllClick(Sender: TObject);
    procedure BtnLoadDllClick(Sender: TObject);
    procedure ListView1SelectItem(Sender: TObject; Item: TListItem;
      Selected: Boolean);
    procedure btnOpenFileClick(Sender: TObject);
  private
    { Private declarations }
    dllLoaded:boolean;
    procedure ClearDisplay;
  public
    { Public declarations }
  end;

var
  Form1: TForm1;
  Pi: TCrcPlugIn;

  implementation
{$R *.DFM}

procedure TForm1.FormCreate(Sender: TObject);
begin
  Pi := TCrcPlugIn.Create(self);
end;

procedure TForm1.BtnGetCrcClick(Sender: TObject);
var
  comment,format,ErrorMsg,filename:pansichar;
  size:int64;
begin
  if not dllLoaded then begin
    MessageDlg('Select a plugin.', mtWarning, [mbOK], 0);
    exit;
  end;
  if trim(edtFileName.text) = '' then begin
    MessageDlg('Select a file.', mtWarning, [mbOK], 0);
    exit;
  end;

  comment := '';
  format := '';
  ErrorMsg := '';
  filename := PAnsichar(ansistring(edtFileName.text));
  lblcrc.Caption := string(Pi.GetSignature(filename,nil,format,size,comment,ErrorMsg));
  LblFormat.Caption := string(format);
  LblSize.Caption := string(IntToStr(size));
  Lblcomment.Caption := string(comment);
  if (ErrorMsg <> nil) and (trim(string(ErrorMsg)) <> '') then MessageDlg(string(ErrorMsg), mtError, [mbOK], 0);
end;

procedure TForm1.BtnUnloaddllClick(Sender: TObject);
begin
  pi.UnloadDll;
  DllLoaded := false;
  ListView1.Items.Clear;
end;

procedure TForm1.BtnLoadDllClick(Sender: TObject);
var
  sr: TSearchRec;
  item:TListItem;
  msg:string;
begin

  Pi.UnloadDll;
  DllLoaded := false;
  ListView1.Items.Clear;

  if FindFirst('*.dll', faAnyFile, sr) = 0 then begin
    repeat
      try
        Pi.FileName := sr.Name;
        pi.LoadDll;
        msg := 'Loaded';
      except
        on e:exception do begin
          msg := e.Message;
        end;
      end;
      item := ListView1.Items.Add;
      item.Caption := sr.name;
      item.SubItems.Add(msg)
    until FindNext(sr) <> 0;
    Sysutils.FindClose(sr);
  end;
end;

procedure TForm1.btnOpenFileClick(Sender: TObject);
begin
  OpenDialog.Execute(self.Handle);
  edtFileName.Text := OpenDialog.FileName;
end;

procedure TForm1.ListView1SelectItem(Sender: TObject; Item: TListItem;
  Selected: Boolean);
begin
  if not selected then begin
    ClearDisplay;
    pi.UnloadDll;
    DllLoaded := false;
  end
  else begin
    Pi.FileName := item.Caption;
    try
      pi.LoadDll;
      DllLoaded := true;
    except
      on e:exception do begin
        ClearDisplay;
        exit;
      end;
    end;

    lblname.Caption := pi.PlugInName;
    lblauthor.Caption := pi.Author;
    lblversion.Caption := pi.Version;
    lblwebpage.Caption := pi.WebPage;
    lblemail.Caption := pi.Email;
    Memo1.Lines.Add(pi.Description);
  end;
end;

procedure TForm1.ClearDisplay;
begin
  lblname.Caption := '';
  lblauthor.Caption := '';
  lblversion.Caption := '';
  lblwebpage.Caption := '';
  lblemail.Caption := '';
  lbldat.Caption := '';
  lblext.Caption := '';
  Memo1.Lines.Clear;

  lblcrc.Caption := '';
  LblFormat.Caption := '';
  LblSize.Caption := '';
end;

end.
