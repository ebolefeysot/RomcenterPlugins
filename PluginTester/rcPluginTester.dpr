program rcPluginTester;

uses
  Forms,
  main in 'main.pas' {Form1},
  archive in 'lib\archive.pas',
  archivemanager in 'lib\archivemanager.pas',
  crcplugin in 'lib\crcplugin.pas',
  rcZip in 'lib\rcZip.pas',
  SignatureCalculator in 'lib\SignatureCalculator.pas';

{$R *.RES}

begin
  Application.Title := 'RomCenter plug in tester';
  Application.CreateForm(TForm1, Form1);
  Application.Run;
end.
