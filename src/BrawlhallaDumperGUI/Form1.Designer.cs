namespace BrawlhallaDumperGUI;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    private TabControl tabControl;
    private TabPage tabKeyFinder;
    private TabPage tabDecrypt;
    private TabPage tabEncrypt;
    private TabPage tabMemory;

    private Button btnFindKey;
    private TextBox txtKey;
    private Button btnCopyKey;
    private Label lblKeyStatus;

    private TextBox txtDecryptKey;
    private TextBox txtDecryptOutput;
    private TextBox txtDecryptFiles;
    private TextBox txtDumpSeed;
    private Button btnDumpSeed;
    private Button btnBrowseDecryptOutput;
    private Button btnBrowseDecryptFiles;
    private Button btnDecrypt;
    private ProgressBar progressBarDecrypt;
    private TextBox txtDecryptLog;

    private TextBox txtEncryptKey;
    private TextBox txtEncryptSeed;
    private TextBox txtEncryptInput;
    private TextBox txtEncryptOutput;
    private Button btnBrowseEncryptInput;
    private Button btnBrowseEncryptOutput;
    private Button btnEncrypt;
    private ProgressBar progressBarEncrypt;
    private TextBox txtEncryptLog;

    private Button btnRefreshMemory;
    private CheckBox chkAutoRefresh;
    private Label lblMemStatus;
    private Label lblMemPid;
    private Label lblMemAddr;
    private ListView lvOffsets;
    private System.Windows.Forms.Timer timerAutoRefresh;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();

        const int pad = 26;
        const int lblW = 110;
        const int txtX = pad + lblW;
        const int txtW = 600;
        const int btnX = txtX + txtW + 14;
        const int btnW = 100;
        const int rowH = 52;
        const int fieldH = 28;
        const int logTop = 310;

        this.tabControl = new TabControl();
        this.tabKeyFinder = new TabPage();
        this.tabDecrypt = new TabPage();
        this.tabEncrypt = new TabPage();
        this.tabMemory = new TabPage();

        this.btnFindKey = new Button();
        this.txtKey = new TextBox();
        this.btnCopyKey = new Button();
        this.lblKeyStatus = new Label();

        this.txtDecryptKey = new TextBox();
        this.txtDecryptOutput = new TextBox();
        this.txtDecryptFiles = new TextBox();
        this.txtDumpSeed = new TextBox();
        this.btnDumpSeed = new Button();
        this.btnBrowseDecryptOutput = new Button();
        this.btnBrowseDecryptFiles = new Button();
        this.btnDecrypt = new Button();
        this.progressBarDecrypt = new ProgressBar();
        this.txtDecryptLog = new TextBox();

        this.txtEncryptKey = new TextBox();
        this.txtEncryptSeed = new TextBox();
        this.txtEncryptInput = new TextBox();
        this.txtEncryptOutput = new TextBox();
        this.btnBrowseEncryptInput = new Button();
        this.btnBrowseEncryptOutput = new Button();
        this.btnEncrypt = new Button();
        this.progressBarEncrypt = new ProgressBar();
        this.txtEncryptLog = new TextBox();

        this.btnRefreshMemory = new Button();
        this.chkAutoRefresh = new CheckBox();
        this.lblMemStatus = new Label();
        this.lblMemPid = new Label();
        this.lblMemAddr = new Label();
        this.lvOffsets = new ListView();
        this.timerAutoRefresh = new System.Windows.Forms.Timer(this.components);

        this.tabControl.SuspendLayout();
        this.tabKeyFinder.SuspendLayout();
        this.tabDecrypt.SuspendLayout();
        this.tabEncrypt.SuspendLayout();
        this.tabMemory.SuspendLayout();
        this.SuspendLayout();

        // 
        // tabControl
        // 
        this.tabControl.Controls.Add(this.tabKeyFinder);
        this.tabControl.Controls.Add(this.tabDecrypt);
        this.tabControl.Controls.Add(this.tabEncrypt);
        this.tabControl.Controls.Add(this.tabMemory);
        this.tabControl.Dock = DockStyle.Fill;
        this.tabControl.Name = "tabControl";
        this.tabControl.TabIndex = 0;

        // 
        // tabKeyFinder
        // 
        this.tabKeyFinder.Name = "tabKeyFinder";
        this.tabKeyFinder.Padding = new Padding(4);
        this.tabKeyFinder.TabIndex = 0;
        this.tabKeyFinder.Text = "Key Finder";

        var lblKeyTitle = new Label();
        lblKeyTitle.Text = "Find the SWZ encryption key";
        lblKeyTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        lblKeyTitle.Location = new System.Drawing.Point(pad, 22);
        lblKeyTitle.Size = new System.Drawing.Size(400, 24);
        this.tabKeyFinder.Controls.Add(lblKeyTitle);

        this.lblKeyStatus.AutoSize = true;
        this.lblKeyStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
        this.lblKeyStatus.ForeColor = System.Drawing.Color.Gray;
        this.lblKeyStatus.Location = new System.Drawing.Point(pad, 54);
        this.lblKeyStatus.Name = "lblKeyStatus";
        this.lblKeyStatus.TabIndex = 3;
        this.lblKeyStatus.Text = "The key is extracted from BrawlhallaAir.swf inside your Steam install.";
        this.tabKeyFinder.Controls.Add(this.lblKeyStatus);

        this.btnFindKey.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.btnFindKey.Location = new System.Drawing.Point(pad, 90);
        this.btnFindKey.Size = new System.Drawing.Size(260, 46);
        this.btnFindKey.TabIndex = 0;
        this.btnFindKey.Text = "Find Key";
        this.btnFindKey.Click += new EventHandler(this.btnFindKey_Click);
        this.tabKeyFinder.Controls.Add(this.btnFindKey);

        this.txtKey.Location = new System.Drawing.Point(pad, 154);
        this.txtKey.Size = new System.Drawing.Size(540, 27);
        this.txtKey.TabIndex = 1;
        this.txtKey.ReadOnly = true;
        this.txtKey.Text = "";
        this.tabKeyFinder.Controls.Add(this.txtKey);

        this.btnCopyKey.Location = new System.Drawing.Point(588, 154);
        this.btnCopyKey.Size = new System.Drawing.Size(104, 27);
        this.btnCopyKey.TabIndex = 2;
        this.btnCopyKey.Text = "Copy";
        this.btnCopyKey.Click += new EventHandler(this.btnCopyKey_Click);
        this.tabKeyFinder.Controls.Add(this.btnCopyKey);

        var lblInfoTitle = new Label();
        lblInfoTitle.Text = "Workflow";
        lblInfoTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        lblInfoTitle.Location = new System.Drawing.Point(pad, 220);
        lblInfoTitle.Size = new System.Drawing.Size(200, 22);
        this.tabKeyFinder.Controls.Add(lblInfoTitle);

        var infoLines = new[]
        {
            "1. Click \"Find Key\" - the tool locates BrawlhallaAir.swf inside your Steam install.",
            "2. The key is extracted from the game's bytecode and copied to the clipboard automatically.",
            "3. Paste it into the Decrypt or Encrypt tabs to process SWZ archives.",
            "4. Decrypted archives are exported as XML into the folder you choose."
        };
        int infoY = 252;
        foreach (var line in infoLines)
        {
            var lbl = new Label();
            lbl.Text = line;
            lbl.Font = new System.Drawing.Font("Segoe UI", 9F);
            lbl.ForeColor = System.Drawing.Color.DimGray;
            lbl.AutoSize = true;
            lbl.Location = new System.Drawing.Point(pad, infoY);
            this.tabKeyFinder.Controls.Add(lbl);
            infoY += 28;
        }

        // 
        // tabDecrypt
        // 
        this.tabDecrypt.Name = "tabDecrypt";
        this.tabDecrypt.Padding = new Padding(4);
        this.tabDecrypt.TabIndex = 1;
        this.tabDecrypt.Text = "Decrypt";

        // Row 1: Global Key
        var lblDecryptKey = new Label();
        lblDecryptKey.Text = "Global Key";
        lblDecryptKey.Font = new System.Drawing.Font("Segoe UI", 9F);
        lblDecryptKey.Location = new System.Drawing.Point(pad, 28);
        lblDecryptKey.Size = new System.Drawing.Size(lblW, 20);
        lblDecryptKey.TextAlign = ContentAlignment.MiddleLeft;
        this.tabDecrypt.Controls.Add(lblDecryptKey);

        this.txtDecryptKey.Location = new System.Drawing.Point(txtX, 24);
        this.txtDecryptKey.Size = new System.Drawing.Size(txtW, fieldH);
        this.txtDecryptKey.TabIndex = 0;
        this.tabDecrypt.Controls.Add(this.txtDecryptKey);

        // Row 2: Output Dir + Browse
        var lblDecryptOutput = new Label();
        lblDecryptOutput.Text = "Output Dir";
        lblDecryptOutput.Font = new System.Drawing.Font("Segoe UI", 9F);
        lblDecryptOutput.Location = new System.Drawing.Point(pad, 28 + rowH);
        lblDecryptOutput.Size = new System.Drawing.Size(lblW, 20);
        lblDecryptOutput.TextAlign = ContentAlignment.MiddleLeft;
        this.tabDecrypt.Controls.Add(lblDecryptOutput);

        this.txtDecryptOutput.Location = new System.Drawing.Point(txtX, 24 + rowH);
        this.txtDecryptOutput.Size = new System.Drawing.Size(txtW, fieldH);
        this.txtDecryptOutput.TabIndex = 1;
        this.tabDecrypt.Controls.Add(this.txtDecryptOutput);

        this.btnBrowseDecryptOutput.Location = new System.Drawing.Point(btnX, 24 + rowH);
        this.btnBrowseDecryptOutput.Size = new System.Drawing.Size(btnW, fieldH);
        this.btnBrowseDecryptOutput.TabIndex = 2;
        this.btnBrowseDecryptOutput.Text = "Browse";
        this.btnBrowseDecryptOutput.Click += new EventHandler(this.btnBrowseDecryptOutput_Click);
        this.tabDecrypt.Controls.Add(this.btnBrowseDecryptOutput);

        // Row 3: SWZ Files + Browse
        var lblDecryptFiles = new Label();
        lblDecryptFiles.Text = "SWZ Files";
        lblDecryptFiles.Font = new System.Drawing.Font("Segoe UI", 9F);
        lblDecryptFiles.Location = new System.Drawing.Point(pad, 28 + rowH * 2);
        lblDecryptFiles.Size = new System.Drawing.Size(lblW, 20);
        lblDecryptFiles.TextAlign = ContentAlignment.MiddleLeft;
        this.tabDecrypt.Controls.Add(lblDecryptFiles);

        this.txtDecryptFiles.Location = new System.Drawing.Point(txtX, 24 + rowH * 2);
        this.txtDecryptFiles.Multiline = true;
        this.txtDecryptFiles.Size = new System.Drawing.Size(txtW, 84);
        this.txtDecryptFiles.ScrollBars = ScrollBars.Vertical;
        this.txtDecryptFiles.TabIndex = 3;
        this.tabDecrypt.Controls.Add(this.txtDecryptFiles);

        this.btnBrowseDecryptFiles.Location = new System.Drawing.Point(btnX, 24 + rowH * 2);
        this.btnBrowseDecryptFiles.Size = new System.Drawing.Size(btnW, 34);
        this.btnBrowseDecryptFiles.TabIndex = 4;
        this.btnBrowseDecryptFiles.Text = "Browse";
        this.btnBrowseDecryptFiles.Click += new EventHandler(this.btnBrowseDecryptFiles_Click);
        this.tabDecrypt.Controls.Add(this.btnBrowseDecryptFiles);

        // Row 4: Seed + Dump Seed
        var lblDumpSeed = new Label();
        lblDumpSeed.Text = "Seed";
        lblDumpSeed.Font = new System.Drawing.Font("Segoe UI", 9F);
        lblDumpSeed.Location = new System.Drawing.Point(pad, 28 + rowH * 2 + 96);
        lblDumpSeed.Size = new System.Drawing.Size(lblW, 20);
        lblDumpSeed.TextAlign = ContentAlignment.MiddleLeft;
        this.tabDecrypt.Controls.Add(lblDumpSeed);

        this.txtDumpSeed.Location = new System.Drawing.Point(txtX, 24 + rowH * 2 + 96);
        this.txtDumpSeed.Size = new System.Drawing.Size(txtW, fieldH);
        this.txtDumpSeed.ReadOnly = true;
        this.txtDumpSeed.TabIndex = 8;
        this.txtDumpSeed.Text = "";
        this.tabDecrypt.Controls.Add(this.txtDumpSeed);

        this.btnDumpSeed.Location = new System.Drawing.Point(btnX, 24 + rowH * 2 + 96);
        this.btnDumpSeed.Size = new System.Drawing.Size(btnW, fieldH);
        this.btnDumpSeed.TabIndex = 9;
        this.btnDumpSeed.Text = "Dump Seed";
        this.btnDumpSeed.Click += new EventHandler(this.btnDumpSeed_Click);
        this.tabDecrypt.Controls.Add(this.btnDumpSeed);

        // Row 5: Decrypt button + progress
        this.progressBarDecrypt.Location = new System.Drawing.Point(txtX, logTop - 26);
        this.progressBarDecrypt.Size = new System.Drawing.Size(txtW, 10);
        this.progressBarDecrypt.TabIndex = 6;
        this.progressBarDecrypt.Style = ProgressBarStyle.Blocks;
        this.progressBarDecrypt.Visible = false;
        this.tabDecrypt.Controls.Add(this.progressBarDecrypt);

        this.btnDecrypt.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.btnDecrypt.Location = new System.Drawing.Point(btnX, logTop - 46);
        this.btnDecrypt.Size = new System.Drawing.Size(btnW, 38);
        this.btnDecrypt.TabIndex = 5;
        this.btnDecrypt.Text = "Decrypt";
        this.btnDecrypt.Click += new EventHandler(this.btnDecrypt_Click);
        this.tabDecrypt.Controls.Add(this.btnDecrypt);

        // Log
        this.txtDecryptLog.Location = new System.Drawing.Point(pad, logTop);
        this.txtDecryptLog.Multiline = true;
        this.txtDecryptLog.ReadOnly = true;
        this.txtDecryptLog.ScrollBars = ScrollBars.Vertical;
        this.txtDecryptLog.Size = new System.Drawing.Size(btnX + btnW - pad, 260);
        this.txtDecryptLog.TabIndex = 7;
        this.txtDecryptLog.Text = "";
        this.txtDecryptLog.WordWrap = false;
        this.tabDecrypt.Controls.Add(this.txtDecryptLog);

        // 
        // tabEncrypt
        // 
        this.tabEncrypt.Name = "tabEncrypt";
        this.tabEncrypt.Padding = new Padding(4);
        this.tabEncrypt.TabIndex = 2;
        this.tabEncrypt.Text = "Encrypt";

        // Row 1: Global Key
        var lblEncryptKey = new Label();
        lblEncryptKey.Text = "Global Key";
        lblEncryptKey.Font = new System.Drawing.Font("Segoe UI", 9F);
        lblEncryptKey.Location = new System.Drawing.Point(pad, 28);
        lblEncryptKey.Size = new System.Drawing.Size(lblW, 20);
        lblEncryptKey.TextAlign = ContentAlignment.MiddleLeft;
        this.tabEncrypt.Controls.Add(lblEncryptKey);

        this.txtEncryptKey.Location = new System.Drawing.Point(txtX, 24);
        this.txtEncryptKey.Size = new System.Drawing.Size(txtW, fieldH);
        this.txtEncryptKey.TabIndex = 0;
        this.tabEncrypt.Controls.Add(this.txtEncryptKey);

        // Row 2: Seed
        var lblEncryptSeed = new Label();
        lblEncryptSeed.Text = "Seed";
        lblEncryptSeed.Font = new System.Drawing.Font("Segoe UI", 9F);
        lblEncryptSeed.Location = new System.Drawing.Point(pad, 28 + rowH);
        lblEncryptSeed.Size = new System.Drawing.Size(lblW, 20);
        lblEncryptSeed.TextAlign = ContentAlignment.MiddleLeft;
        this.tabEncrypt.Controls.Add(lblEncryptSeed);

        this.txtEncryptSeed.Location = new System.Drawing.Point(txtX, 24 + rowH);
        this.txtEncryptSeed.Size = new System.Drawing.Size(txtW, fieldH);
        this.txtEncryptSeed.TabIndex = 1;
        this.tabEncrypt.Controls.Add(this.txtEncryptSeed);

        // Row 3: Input XML + Browse
        var lblEncryptInput = new Label();
        lblEncryptInput.Text = "Input XML";
        lblEncryptInput.Font = new System.Drawing.Font("Segoe UI", 9F);
        lblEncryptInput.Location = new System.Drawing.Point(pad, 28 + rowH * 2);
        lblEncryptInput.Size = new System.Drawing.Size(lblW, 20);
        lblEncryptInput.TextAlign = ContentAlignment.MiddleLeft;
        this.tabEncrypt.Controls.Add(lblEncryptInput);

        this.txtEncryptInput.Location = new System.Drawing.Point(txtX, 24 + rowH * 2);
        this.txtEncryptInput.Size = new System.Drawing.Size(txtW, fieldH);
        this.txtEncryptInput.TabIndex = 2;
        this.tabEncrypt.Controls.Add(this.txtEncryptInput);

        this.btnBrowseEncryptInput.Location = new System.Drawing.Point(btnX, 24 + rowH * 2);
        this.btnBrowseEncryptInput.Size = new System.Drawing.Size(btnW, fieldH);
        this.btnBrowseEncryptInput.TabIndex = 3;
        this.btnBrowseEncryptInput.Text = "Browse";
        this.btnBrowseEncryptInput.Click += new EventHandler(this.btnBrowseEncryptInput_Click);
        this.tabEncrypt.Controls.Add(this.btnBrowseEncryptInput);

        // Row 4: Output SWZ + Browse
        var lblEncryptOutput = new Label();
        lblEncryptOutput.Text = "Output SWZ";
        lblEncryptOutput.Font = new System.Drawing.Font("Segoe UI", 9F);
        lblEncryptOutput.Location = new System.Drawing.Point(pad, 28 + rowH * 3);
        lblEncryptOutput.Size = new System.Drawing.Size(lblW, 20);
        lblEncryptOutput.TextAlign = ContentAlignment.MiddleLeft;
        this.tabEncrypt.Controls.Add(lblEncryptOutput);

        this.txtEncryptOutput.Location = new System.Drawing.Point(txtX, 24 + rowH * 3);
        this.txtEncryptOutput.Size = new System.Drawing.Size(txtW, fieldH);
        this.txtEncryptOutput.TabIndex = 4;
        this.tabEncrypt.Controls.Add(this.txtEncryptOutput);

        this.btnBrowseEncryptOutput.Location = new System.Drawing.Point(btnX, 24 + rowH * 3);
        this.btnBrowseEncryptOutput.Size = new System.Drawing.Size(btnW, fieldH);
        this.btnBrowseEncryptOutput.TabIndex = 5;
        this.btnBrowseEncryptOutput.Text = "Browse";
        this.btnBrowseEncryptOutput.Click += new EventHandler(this.btnBrowseEncryptOutput_Click);
        this.tabEncrypt.Controls.Add(this.btnBrowseEncryptOutput);

        // Row 5: Encrypt button + progress
        this.progressBarEncrypt.Location = new System.Drawing.Point(txtX, logTop - 26);
        this.progressBarEncrypt.Size = new System.Drawing.Size(txtW, 10);
        this.progressBarEncrypt.TabIndex = 7;
        this.progressBarEncrypt.Style = ProgressBarStyle.Blocks;
        this.progressBarEncrypt.Visible = false;
        this.tabEncrypt.Controls.Add(this.progressBarEncrypt);

        this.btnEncrypt.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.btnEncrypt.Location = new System.Drawing.Point(btnX, logTop - 46);
        this.btnEncrypt.Size = new System.Drawing.Size(btnW, 38);
        this.btnEncrypt.TabIndex = 6;
        this.btnEncrypt.Text = "Encrypt";
        this.btnEncrypt.Click += new EventHandler(this.btnEncrypt_Click);
        this.tabEncrypt.Controls.Add(this.btnEncrypt);

        // Log
        this.txtEncryptLog.Location = new System.Drawing.Point(pad, logTop);
        this.txtEncryptLog.Multiline = true;
        this.txtEncryptLog.ReadOnly = true;
        this.txtEncryptLog.ScrollBars = ScrollBars.Vertical;
        this.txtEncryptLog.Size = new System.Drawing.Size(btnX + btnW - pad, 260);
        this.txtEncryptLog.TabIndex = 8;
        this.txtEncryptLog.Text = "";
        this.txtEncryptLog.WordWrap = false;
        this.tabEncrypt.Controls.Add(this.txtEncryptLog);

        // 
        // tabMemory
        // 
        this.tabMemory.Name = "tabMemory";
        this.tabMemory.Padding = new Padding(4);
        this.tabMemory.TabIndex = 3;
        this.tabMemory.Text = "Memory Reader";

        this.lblMemStatus.AutoSize = true;
        this.lblMemStatus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.lblMemStatus.Location = new System.Drawing.Point(pad, 28);
        this.lblMemStatus.Name = "lblMemStatus";
        this.lblMemStatus.Size = new System.Drawing.Size(200, 18);
        this.lblMemStatus.Text = "No process attached";
        this.tabMemory.Controls.Add(this.lblMemStatus);

        this.btnRefreshMemory.Location = new System.Drawing.Point(pad, 60);
        this.btnRefreshMemory.Size = new System.Drawing.Size(120, 36);
        this.btnRefreshMemory.TabIndex = 0;
        this.btnRefreshMemory.Text = "Refresh";
        this.btnRefreshMemory.Click += new EventHandler(this.btnRefreshMemory_Click);
        this.tabMemory.Controls.Add(this.btnRefreshMemory);

        this.chkAutoRefresh.AutoSize = true;
        this.chkAutoRefresh.Location = new System.Drawing.Point(156, 68);
        this.chkAutoRefresh.Name = "chkAutoRefresh";
        this.chkAutoRefresh.Size = new System.Drawing.Size(160, 21);
        this.chkAutoRefresh.Text = "Auto-refresh (200 ms)";
        this.chkAutoRefresh.TabIndex = 1;
        this.chkAutoRefresh.CheckedChanged += new EventHandler(this.chkAutoRefresh_CheckedChanged);
        this.tabMemory.Controls.Add(this.chkAutoRefresh);

        this.lblMemPid.AutoSize = true;
        this.lblMemPid.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblMemPid.ForeColor = System.Drawing.Color.DimGray;
        this.lblMemPid.Location = new System.Drawing.Point(pad, 106);
        this.lblMemPid.Name = "lblMemPid";
        this.lblMemPid.Size = new System.Drawing.Size(400, 17);
        this.lblMemPid.Text = "";
        this.tabMemory.Controls.Add(this.lblMemPid);

        this.lblMemAddr.AutoSize = true;
        this.lblMemAddr.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblMemAddr.ForeColor = System.Drawing.Color.DimGray;
        this.lblMemAddr.Location = new System.Drawing.Point(pad, 128);
        this.lblMemAddr.Name = "lblMemAddr";
        this.lblMemAddr.Size = new System.Drawing.Size(400, 17);
        this.lblMemAddr.Text = "";
        this.tabMemory.Controls.Add(this.lblMemAddr);

        // 
        // lvOffsets
        // 
        this.lvOffsets.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.lvOffsets.Location = new System.Drawing.Point(pad, 155);
        this.lvOffsets.Size = new System.Drawing.Size(830, 370);
        this.lvOffsets.View = View.Details;
        this.lvOffsets.FullRowSelect = true;
        this.lvOffsets.GridLines = true;
        this.lvOffsets.Font = new System.Drawing.Font("Consolas", 9F);
        this.lvOffsets.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        this.lvOffsets.Scrollable = true;
        this.lvOffsets.Columns.Add("Category", 90);
        this.lvOffsets.Columns.Add("Field", 110);
        this.lvOffsets.Columns.Add("Value", 180);
        this.lvOffsets.Columns.Add("Hex", 140);
        this.tabMemory.Controls.Add(this.lvOffsets);

        // 
        // timerAutoRefresh
        // 
        this.timerAutoRefresh.Interval = 200;
        this.timerAutoRefresh.Tick += new EventHandler(this.timerAutoRefresh_Tick);

        // 
        // folderBrowserDialog
        // 
        this.folderBrowserDialog.Description = "Select output directory";
        this.folderBrowserDialog.ShowNewFolderButton = true;

        // 
        // openFileDialogSwz
        // 
        this.openFileDialogSwz.Filter = "SWZ files (*.swz)|*.swz|All files (*.*)|*.*";
        this.openFileDialogSwz.Multiselect = true;
        this.openFileDialogSwz.Title = "Select SWZ file(s)";

        // 
        // openFileDialogXml
        // 
        this.openFileDialogXml.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
        this.openFileDialogXml.Title = "Select exported XML file";

        // 
        // saveFileDialogSwz
        // 
        this.saveFileDialogSwz.Filter = "SWZ files (*.swz)|*.swz|All files (*.*)|*.*";
        this.saveFileDialogSwz.Title = "Save encrypted SWZ file";

        // 
        // Form1
        // 
        this.AutoScaleDimensions = new SizeF(8F, 17F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(870, 560);
        this.Controls.Add(this.tabControl);
        this.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.MinimumSize = new Size(700, 500);
        this.Name = "Form1";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Icon = new System.Drawing.Icon(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("BrawlhallaDumperGUI.icon.ico") ?? throw new InvalidOperationException("icon.ico not found as embedded resource"));
        this.Text = "Brawlhalla SWZ Dumper";

        this.tabControl.ResumeLayout();
        this.tabKeyFinder.ResumeLayout();
        this.tabDecrypt.ResumeLayout();
        this.tabEncrypt.ResumeLayout();
        this.tabMemory.ResumeLayout();
        this.ResumeLayout(false);
    }
}
