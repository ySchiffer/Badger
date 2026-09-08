[Setup]
AppName=Badger
AppVersion=1.0
DefaultDirName={autopf}\Badger
DefaultGroupName=Default
OutputDir=.
OutputBaseFilename=BadgerSetup
Compression=lzma
SolidCompression=yes
SetupIconFile=app.ico
WizardStyle=modern dynamic
UsePreviousAppDir=no
DisableProgramGroupPage=yes
UninstallFilesDir={app}
UninstallDisplayName=Desinstalar
AppMutex=BadgerSetup

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Files]
; Copia todos os arquivos do build para a raiz da pasta de instalação
Source: "bin\Release\net10.0-windows\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "app.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Badger"; Filename: "{app}\Badger.exe"; IconFilename: "{app}\app.ico"
Name: "{group}\Documentação (README)"; Filename: "{app}\README.md"
Name: "{autodesktop}\Badger"; Filename: "{app}\Badger.exe"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Criar ícone na Área de Trabalho"; GroupDescription: "Atalhos:"
Name: "startupicon"; Description: "Iniciar junto com o Windows automaticamente"; GroupDescription: "Configurações:"

[Run]
Filename: "{app}\Badger.exe"; Description: "Executar Badger agora"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "BadgerGameActivity"; ValueData: """{app}\Badger.exe"""; Tasks: startupicon; Flags: uninsdeletevalue

[UninstallDelete]
; Remove os arquivos ocultos do worker gerados em tempo de execução na desinstalação
Type: files; Name: "{app}\Stardew Valley.exe"
Type: files; Name: "{app}\Stardew Valley.dll"
Type: files; Name: "{app}\Stardew Valley.deps.json"
Type: files; Name: "{app}\Stardew Valley.runtimeconfig.json"
Type: dirifempty; Name: "{app}"

[Code]
var
  InfoPage: TWizardPage;
  InfoLabel: TNewStaticText;
  DiscordButton: TNewButton;

procedure DiscordButtonOnClick(Sender: TObject);
var
  ErrorCode: Integer;
begin
  ShellExec('open', 'https://discord.gg/KQaEchPhcG', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if Assigned(WizardForm) and Assigned(WizardForm.CancelButton) then
  begin
    DiscordButton.Parent := WizardForm;
    DiscordButton.Width := ScaleX(155);
    DiscordButton.Height := WizardForm.CancelButton.Height;
    DiscordButton.Left := ScaleX(20);
    DiscordButton.Top := WizardForm.CancelButton.Top;
    
    if CurPageID = wpFinished then
      DiscordButton.Visible := False
    else
      DiscordButton.Visible := True;
  end;
end;

procedure InitializeWizard();
begin
  InfoPage := CreateCustomPage(wpWelcome, 'Sobre o Badger', 'Entenda o propósito do utilitário e seus créditos.');
  
  InfoLabel := TNewStaticText.Create(InfoPage);
  InfoLabel.Parent := InfoPage.Surface;
  InfoLabel.Caption := 'O Badger é um utilitário leve desenvolvido para gerenciar e farmar de forma precisa a badge de tempo de jogo no Discord.' + #13#10 + #13#10 +
                       'Ele foi criado para solucionar falhas onde o Discord lê incorretamente o tempo em jogos (muitas vezes registrando muito menos horas do que o real).' + #13#10 + #13#10 +
                       'Desenvolvido com foco em eficiência, leveza e total integração nativa.' + #13#10 + #13#10 +
                       'Clique em "Avançar" para prosseguir com a instalação.';
  InfoLabel.Left := ScaleX(10);
  InfoLabel.Top := ScaleY(15);
  InfoLabel.Width := ScaleX(390);
  InfoLabel.Height := ScaleY(160);
  InfoLabel.AutoSize := False;
  InfoLabel.WordWrap := True;

  DiscordButton := TNewButton.Create(WizardForm);
  DiscordButton.Parent := WizardForm;
  DiscordButton.Caption := 'Acesse nosso Discord';
  DiscordButton.OnClick := @DiscordButtonOnClick;
  DiscordButton.Visible := False;
end;