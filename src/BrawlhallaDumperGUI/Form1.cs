using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BrawlhallaSWZTool;
using BrawlKeyFinder;

namespace BrawlhallaDumperGUI;

public partial class Form1 : Form
{
    private bool _isDecrypting;
    private bool _isEncrypting;

    // Memory Reader
    private Process? _brawlProcess;
    private IntPtr _hProcess = IntPtr.Zero;
    private IntPtr _airDllBase = IntPtr.Zero;
    private long _airDllSize;
    private IntPtr _gameExeBase = IntPtr.Zero;
    private long _gameExeSize;

    // Dialogs
    private readonly FolderBrowserDialog folderBrowserDialog = new();
    private readonly OpenFileDialog openFileDialogSwz = new();
    private readonly OpenFileDialog openFileDialogXml = new();
    private readonly SaveFileDialog saveFileDialogSwz = new();

    public Form1()
    {
        InitializeComponent();
    }

    // =========================================================================
    // Key Finder
    // =========================================================================

    private async void btnFindKey_Click(object sender, EventArgs e)
    {
        btnFindKey.Enabled = false;
        txtKey.Text = "";
        lblKeyStatus.Text = "Locating BrawlhallaAir.swf...";

        try
        {
            string? swfPath = await Task.Run(() => SwfKeyExtractor.LocateSwf());

            if (swfPath is null)
            {
                lblKeyStatus.Text = "BrawlhallaAir.swf not found. Is Brawlhalla installed via Steam?";
                lblKeyStatus.ForeColor = System.Drawing.Color.OrangeRed;
                return;
            }

            lblKeyStatus.Text = $"Found: {swfPath}";
            lblKeyStatus.ForeColor = System.Drawing.Color.Gray;

            uint? key = await Task.Run(() => SwfKeyExtractor.ExtractKey(swfPath));

            if (key is null)
            {
                lblKeyStatus.Text = "Key pattern not found inside ABC bytecode.";
                lblKeyStatus.ForeColor = System.Drawing.Color.OrangeRed;
                return;
            }

            txtKey.Text = key.Value.ToString();
            lblKeyStatus.ForeColor = System.Drawing.Color.Green;
            lblKeyStatus.Text = $"Key extracted: {key.Value} (also copied to clipboard)";

            ClipboardHelper.SetText(key.Value.ToString());
        }
        catch (Exception ex)
        {
            lblKeyStatus.ForeColor = System.Drawing.Color.Red;
            lblKeyStatus.Text = $"Error: {ex.Message}";
        }
        finally
        {
            btnFindKey.Enabled = true;
        }
    }

    private void btnCopyKey_Click(object sender, EventArgs e)
    {
        string key = txtKey.Text;
        if (string.IsNullOrEmpty(key))
        {
            MessageBox.Show("No key to copy. Find a key first.", "Copy Key", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (ClipboardHelper.SetText(key))
            MessageBox.Show("Key copied to clipboard.", "Copy Key", MessageBoxButtons.OK, MessageBoxIcon.Information);
        else
            MessageBox.Show("Failed to copy to clipboard.", "Copy Key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    // =========================================================================
    // Decrypt
    // =========================================================================

    private void btnBrowseDecryptOutput_Click(object sender, EventArgs e)
    {
        folderBrowserDialog.Description = "Select output directory for decrypted XML";
        if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
        {
            txtDecryptOutput.Text = folderBrowserDialog.SelectedPath;
        }
    }

    private void btnBrowseDecryptFiles_Click(object sender, EventArgs e)
    {
        openFileDialogSwz.Multiselect = true;
        if (openFileDialogSwz.ShowDialog() == DialogResult.OK)
        {
            txtDecryptFiles.Lines = openFileDialogSwz.FileNames;
        }
    }

    private void btnDumpSeed_Click(object sender, EventArgs e)
    {
        string[] files = txtDecryptFiles.Lines
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        if (files.Length == 0)
        {
            openFileDialogSwz.Multiselect = false;
            if (openFileDialogSwz.ShowDialog() != DialogResult.OK) return;
            files = new[] { openFileDialogSwz.FileName };
        }

        try
        {
            uint seed = ReadSwzSeed(files[0]);
            txtDumpSeed.Text = $"0x{seed:X8}  ({seed})";
            txtEncryptSeed.Text = seed.ToString();
        }
        catch (Exception ex)
        {
            txtDumpSeed.Text = $"Error: {ex.Message}";
        }
    }

    private static uint ReadSwzSeed(string path)
    {
        using var fs = File.OpenRead(path);
        if (fs.Length < 8)
            throw new InvalidDataException("File is too small to be a valid SWZ file.");

        fs.Position = 4;
        Span<byte> buf = stackalloc byte[4];
        fs.ReadExactly(buf);
        return (uint)(buf[3] | (buf[2] << 8) | (buf[1] << 16) | (buf[0] << 24));
    }

    private async void btnDecrypt_Click(object sender, EventArgs e)
    {
        if (_isDecrypting) return;

        string keyStr = txtDecryptKey.Text.Trim();
        string outputDir = txtDecryptOutput.Text.Trim();
        string[] swzFiles = txtDecryptFiles.Lines;

        if (string.IsNullOrEmpty(keyStr))
        {
            MessageBox.Show("Please enter a global key.", "Decrypt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtDecryptKey.Focus();
            return;
        }

        if (string.IsNullOrEmpty(outputDir))
        {
            MessageBox.Show("Please select an output directory.", "Decrypt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtDecryptOutput.Focus();
            return;
        }

        if (swzFiles.Length == 0 || string.IsNullOrWhiteSpace(swzFiles[0]))
        {
            MessageBox.Show("Please select at least one SWZ file.", "Decrypt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtDecryptFiles.Focus();
            return;
        }

        uint globalKey = CliHelpers.ParseUInt32Arg(keyStr);

        _isDecrypting = true;
        btnDecrypt.Enabled = false;
        progressBarDecrypt.Visible = true;
        progressBarDecrypt.Style = ProgressBarStyle.Marquee;
        txtDecryptLog.Clear();

        var originalOut = Console.Out;
        var originalErr = Console.Error;
        var writer = new TextBoxWriter(txtDecryptLog);
        Console.SetOut(writer);
        Console.SetError(writer);

        try
        {
            await Task.Run(() =>
            {
                int total = 0;
                foreach (var swzPath in swzFiles)
                {
                    if (string.IsNullOrWhiteSpace(swzPath)) continue;
                    total++;

                    if (!File.Exists(swzPath))
                    {
                        Console.WriteLine($"[SKIP] File not found: {swzPath}");
                        continue;
                    }

                    string swzName = Path.GetFileNameWithoutExtension(swzPath);
                    Console.WriteLine($"\n==> Decrypting: {swzPath}");

                    using var fs = File.OpenRead(swzPath);
                    var entries = BrawlhallaSWZ.Decrypt(fs, globalKey);

                    Console.WriteLine($"    {entries.Length} entries found.");
                    SwzExporter.Export(entries, outputDir, swzName);
                }

                Console.WriteLine($"\n[*] Done — {total} file(s) processed.");
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\n[ERROR] {ex.Message}");
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
            progressBarDecrypt.Visible = false;
            progressBarDecrypt.Style = ProgressBarStyle.Blocks;
            _isDecrypting = false;
            btnDecrypt.Enabled = true;
        }
    }

    // =========================================================================
    // Encrypt
    // =========================================================================

    private void btnBrowseEncryptInput_Click(object sender, EventArgs e)
    {
        openFileDialogXml.CheckFileExists = true;
        if (openFileDialogXml.ShowDialog() == DialogResult.OK)
        {
            txtEncryptInput.Text = openFileDialogXml.FileName;
        }
    }

    private void btnBrowseEncryptOutput_Click(object sender, EventArgs e)
    {
        saveFileDialogSwz.Filter = "SWZ files (*.swz)|*.swz|All files (*.*)|*.*";
        if (saveFileDialogSwz.ShowDialog() == DialogResult.OK)
        {
            txtEncryptOutput.Text = saveFileDialogSwz.FileName;
        }
    }

    private async void btnEncrypt_Click(object sender, EventArgs e)
    {
        if (_isEncrypting) return;

        string keyStr = txtEncryptKey.Text.Trim();
        string seedStr = txtEncryptSeed.Text.Trim();
        string inputXml = txtEncryptInput.Text.Trim();
        string outputSwz = txtEncryptOutput.Text.Trim();

        if (string.IsNullOrEmpty(keyStr))
        {
            MessageBox.Show("Please enter a global key.", "Encrypt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtEncryptKey.Focus();
            return;
        }

        if (string.IsNullOrEmpty(seedStr))
        {
            MessageBox.Show("Please enter a seed.", "Encrypt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtEncryptSeed.Focus();
            return;
        }

        if (string.IsNullOrEmpty(inputXml))
        {
            MessageBox.Show("Please select an input XML file.", "Encrypt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtEncryptInput.Focus();
            return;
        }

        if (string.IsNullOrEmpty(outputSwz))
        {
            MessageBox.Show("Please specify an output SWZ file path.", "Encrypt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtEncryptOutput.Focus();
            return;
        }

        if (!File.Exists(inputXml))
        {
            MessageBox.Show($"Input XML not found: {inputXml}", "Encrypt", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        uint globalKey = CliHelpers.ParseUInt32Arg(keyStr);
        uint seed = CliHelpers.ParseUInt32Arg(seedStr);

        _isEncrypting = true;
        btnEncrypt.Enabled = false;
        progressBarEncrypt.Visible = true;
        progressBarEncrypt.Style = ProgressBarStyle.Marquee;
        txtEncryptLog.Clear();

        var originalOut = Console.Out;
        var originalErr = Console.Error;
        var writer = new TextBoxWriter(txtEncryptLog);
        Console.SetOut(writer);
        Console.SetError(writer);

        try
        {
            await Task.Run(() =>
            {
                Console.WriteLine("==> Loading entries from XML...");
                var entries = SwzExporter.Import(inputXml);
                Console.WriteLine($"    {entries.Length} entries loaded.");

                Console.WriteLine("==> Re-encrypting...");
                var encrypted = BrawlhallaSWZ.Encrypt(seed, globalKey, entries);

                File.WriteAllBytes(outputSwz, encrypted);
                Console.WriteLine($"[*] Written {encrypted.Length} bytes → {outputSwz}");
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\n[ERROR] {ex.Message}");
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
            progressBarEncrypt.Visible = false;
            progressBarEncrypt.Style = ProgressBarStyle.Blocks;
            _isEncrypting = false;
            btnEncrypt.Enabled = true;
        }
    }

    // =========================================================================
    // Memory Reader
    // =========================================================================

    private void btnRefreshMemory_Click(object? sender, EventArgs e)
    {
        AttachOrDetach();
    }

    private void chkAutoRefresh_CheckedChanged(object? sender, EventArgs e)
    {
        if (chkAutoRefresh.Checked)
        {
            if (_hProcess == IntPtr.Zero)
                AttachOrDetach();
            if (_hProcess != IntPtr.Zero)
                timerAutoRefresh.Start();
        }
        else
        {
            timerAutoRefresh.Stop();
        }
    }

    private void timerAutoRefresh_Tick(object? sender, EventArgs e)
    {
        if (_hProcess == IntPtr.Zero)
        {
            timerAutoRefresh.Stop();
            chkAutoRefresh.Checked = false;
            return;
        }

        // Check if process is still alive
        try
        {
            _brawlProcess!.Refresh();
            if (_brawlProcess.HasExited)
            {
                Detach();
                return;
            }
        }
        catch
        {
            Detach();
            return;
        }

        ReadAllOffsets(null);
    }

    private bool _isAttaching;

    private async void AttachOrDetach()
    {
        if (_hProcess != IntPtr.Zero)
        {
            Detach();
            return;
        }

        if (_isAttaching) return;
        _isAttaching = true;
        btnRefreshMemory.Enabled = false;

        var proc = ProcessMemoryReader.FindBrawlhallaProcess();
        if (proc is null)
        {
            lblMemStatus.Text = "Brawlhalla not found. Start the game first.";
            lblMemStatus.ForeColor = System.Drawing.Color.OrangeRed;
            _isAttaching = false;
            btnRefreshMemory.Enabled = true;
            return;
        }

        _brawlProcess = proc;
        _hProcess = ProcessMemoryReader.OpenProcess(proc);

        if (_hProcess == IntPtr.Zero)
        {
            lblMemStatus.Text = $"Failed to open process (run as admin). PID: {proc.Id}";
            lblMemStatus.ForeColor = System.Drawing.Color.Red;
            _isAttaching = false;
            btnRefreshMemory.Enabled = true;
            return;
        }

        // Find Adobe AIR.dll
        _airDllBase = ProcessMemoryReader.GetModuleBase(proc, "Adobe AIR.dll");
        if (_airDllBase == IntPtr.Zero)
        {
            foreach (ProcessModule m in proc.Modules)
            {
                if (m.ModuleName.Contains("Adobe AIR", StringComparison.OrdinalIgnoreCase))
                {
                    _airDllBase = m.BaseAddress;
                    _airDllSize = m.ModuleMemorySize;
                    break;
                }
            }
        }
        else
        {
            foreach (ProcessModule m in proc.Modules)
            {
                if (m.BaseAddress == _airDllBase)
                {
                    _airDllSize = m.ModuleMemorySize;
                    break;
                }
            }
        }

        // Find Brawlhalla.exe
        _gameExeBase = ProcessMemoryReader.GetModuleBase(proc, "Brawlhalla.exe");
        if (_gameExeBase != IntPtr.Zero)
        {
            foreach (ProcessModule m in proc.Modules)
            {
                if (m.BaseAddress == _gameExeBase)
                {
                    _gameExeSize = m.ModuleMemorySize;
                    break;
                }
            }
        }

        lblMemStatus.Text = "Attached — scanning AOB patterns...";
        lblMemStatus.ForeColor = System.Drawing.Color.Orange;
        lblMemPid.Text = $"PID: {proc.Id}  |  Scanning...";

        // Run AOB discovery on background thread to avoid UI freeze
        string discoveryLog = await Task.Run(() =>
            BrawlhallaOffsets.DiscoverOffsets(
                _hProcess, _airDllBase, _airDllSize, _gameExeBase, _gameExeSize));

        if (_hProcess == IntPtr.Zero)
        {
            // Process died during scan
            _isAttaching = false;
            btnRefreshMemory.Enabled = true;
            return;
        }

        lblMemStatus.Text = BrawlhallaOffsets.OffsetsDiscovered
            ? "Attached (offsets discovered)"
            : "Attached (using defaults — patterns not found)";
        lblMemStatus.ForeColor = BrawlhallaOffsets.OffsetsDiscovered ? System.Drawing.Color.Green : System.Drawing.Color.Orange;
        lblMemPid.Text = $"PID: {proc.Id}  |  AIR.dll @ 0x{_airDllBase.ToInt64():X}";

        ReadAllOffsets(discoveryLog);

        _isAttaching = false;
        btnRefreshMemory.Enabled = true;
    }

    private void Detach()
    {
        timerAutoRefresh.Stop();
        chkAutoRefresh.Checked = false;

        if (_hProcess != IntPtr.Zero)
        {
            ProcessMemoryReader.CloseProcessHandle(_hProcess);
            _hProcess = IntPtr.Zero;
        }

        _brawlProcess = null;
        _airDllBase = IntPtr.Zero;
        _airDllSize = 0;
        _gameExeBase = IntPtr.Zero;
        _gameExeSize = 0;

        lblMemStatus.Text = "Detached";
        lblMemStatus.ForeColor = System.Drawing.Color.Gray;
        lblMemPid.Text = "";
        lblMemAddr.Text = "";

        lvOffsets.Items.Clear();
    }

    private void ReadAllOffsets(string? discoveryLog = null)
    {
        if (_hProcess == IntPtr.Zero) return;

        lvOffsets.BeginUpdate();
        lvOffsets.Items.Clear();

        void AddRow(string category, string offset, long value)
        {
            var item = new ListViewItem(category);
            item.SubItems.Add(offset);
            item.SubItems.Add(value.ToString());
            item.SubItems.Add($"0x{value:X}");
            lvOffsets.Items.Add(item);
        }

        void AddRowDouble(string category, string offset, double value)
        {
            var item = new ListViewItem(category);
            item.SubItems.Add(offset);
            item.SubItems.Add(value.ToString("F4"));
            item.SubItems.Add($"0x{BitConverter.DoubleToInt64Bits(value):X}");
            lvOffsets.Items.Add(item);
        }

        void AddPtr(string category, string offset, IntPtr addr)
        {
            long val = addr.ToInt64();
            AddRow(category, offset, val);
        }

        void AddBad(string category, string offset, string reason = "chain failed")
        {
            var item = new ListViewItem(category);
            item.SubItems.Add(offset);
            item.SubItems.Add($"--- ({reason})");
            item.SubItems.Add("");
            item.ForeColor = System.Drawing.Color.Gray;
            lvOffsets.Items.Add(item);
        }

        void AddStatus(string label, string value, Color color)
        {
            var item = new ListViewItem(label);
            item.SubItems.Add("");
            item.SubItems.Add(value);
            item.SubItems.Add("");
            item.ForeColor = color;
            lvOffsets.Items.Add(item);
        }

        try
        {
            // Show offset source status
            string discovered = BrawlhallaOffsets.OffsetsDiscovered ? "AOB Scanned" : "Defaults (AOB patterns not found)";
            AddStatus("Status", discovered, BrawlhallaOffsets.OffsetsDiscovered ? System.Drawing.Color.Green : System.Drawing.Color.Orange);

            // Show discovered offset values
            AddRow("Offset", "Entity_X", BrawlhallaOffsets.Entity_X);
            AddRow("Offset", "Entity_Y", BrawlhallaOffsets.Entity_Y);
            AddRow("Offset", "Camera_Zoom", BrawlhallaOffsets.Camera_Zoom);
            AddRow("Offset", "Camera_X", BrawlhallaOffsets.Camera_X);
            AddRow("Offset", "Camera_Y", BrawlhallaOffsets.Camera_Y);
            AddRow("Offset", "Camera_Center", BrawlhallaOffsets.Camera_Center);
            AddRow("Offset", "GInput_Value", BrawlhallaOffsets.GInput_Value);
            AddRow("Offset", "DamageTaken", BrawlhallaOffsets.Entity_DamageTaken);
            AddRow("Offset", "Team", BrawlhallaOffsets.Entity_Team);
            AddRow("Offset", "JumpsUsed", BrawlhallaOffsets.Entity_JumpsUsed);
            AddRow("Offset", "Attacking", BrawlhallaOffsets.Entity_AttackingFlag);
            AddRow("Offset", "AttackId", BrawlhallaOffsets.Entity_AttackId);

            if (_airDllBase != IntPtr.Zero)
            {
                // Camera chain
                IntPtr camPtr = ProcessMemoryReader.ResolvePointer(_hProcess, _airDllBase, BrawlhallaOffsets.CameraChain);
                if (camPtr != IntPtr.Zero)
                {
                    AddPtr("Camera", "base", camPtr);

                    if (ProcessMemoryReader.ReadDouble(_hProcess, IntPtr.Add(camPtr, BrawlhallaOffsets.Camera_X), out double camX))
                        AddRowDouble("Camera", "X", camX);
                    else
                        AddBad("Camera", "X");

                    if (ProcessMemoryReader.ReadDouble(_hProcess, IntPtr.Add(camPtr, BrawlhallaOffsets.Camera_Y), out double camY))
                        AddRowDouble("Camera", "Y", camY);
                    else
                        AddBad("Camera", "Y");

                    if (ProcessMemoryReader.ReadDouble(_hProcess, IntPtr.Add(camPtr, BrawlhallaOffsets.Camera_Zoom), out double zoom))
                        AddRowDouble("Camera", "Zoom", zoom);
                    else
                        AddBad("Camera", "Zoom");

                    if (ProcessMemoryReader.ReadDouble(_hProcess, IntPtr.Add(camPtr, BrawlhallaOffsets.Camera_Center), out double center))
                        AddRowDouble("Camera", "Center", center);
                    else
                        AddBad("Camera", "Center");
                }
                else
                {
                    AddBad("Camera", "X", "chain failed");
                    AddBad("Camera", "Y", "chain failed");
                    AddBad("Camera", "Zoom", "chain failed");
                    AddBad("Camera", "Center", "chain failed");
                }

                // Local player chain
                IntPtr playerPtr = ProcessMemoryReader.ResolvePointer(_hProcess, _airDllBase, BrawlhallaOffsets.LocalPlayerChain);
                if (playerPtr != IntPtr.Zero)
                {
                    AddPtr("Player", "base", playerPtr);

                    if (ProcessMemoryReader.ReadDouble(_hProcess, IntPtr.Add(playerPtr, BrawlhallaOffsets.Entity_X), out double px))
                        AddRowDouble("Player", "X", px);
                    else
                        AddBad("Player", "X");

                    if (ProcessMemoryReader.ReadDouble(_hProcess, IntPtr.Add(playerPtr, BrawlhallaOffsets.Entity_Y), out double py))
                        AddRowDouble("Player", "Y", py);
                    else
                        AddBad("Player", "Y");

                    if (ProcessMemoryReader.ReadDouble(_hProcess, IntPtr.Add(playerPtr, BrawlhallaOffsets.Entity_DamageTaken), out double dmg))
                        AddRowDouble("Player", "Damage", dmg);
                    else
                        AddBad("Player", "Damage");

                    if (ProcessMemoryReader.ReadInt32(_hProcess, IntPtr.Add(playerPtr, BrawlhallaOffsets.Entity_Team), out int team))
                        AddRow("Player", "Team", team);
                    else
                        AddBad("Player", "Team");

                    if (ProcessMemoryReader.ReadInt32(_hProcess, IntPtr.Add(playerPtr, BrawlhallaOffsets.Entity_JumpsUsed), out int jumps))
                        AddRow("Player", "JumpsUsed", jumps);
                    else
                        AddBad("Player", "JumpsUsed");

                    if (ProcessMemoryReader.ReadByte(_hProcess, IntPtr.Add(playerPtr, BrawlhallaOffsets.Entity_AttackingFlag), out byte atk))
                        AddRow("Player", "Attacking", atk);
                    else
                        AddBad("Player", "Attacking");

                    if (ProcessMemoryReader.ReadInt32(_hProcess, IntPtr.Add(playerPtr, BrawlhallaOffsets.Entity_AttackId), out int atkId))
                        AddRow("Player", "AttackId", atkId);
                    else
                        AddBad("Player", "AttackId");

                    if (ProcessMemoryReader.ReadInt32(_hProcess, IntPtr.Add(playerPtr, BrawlhallaOffsets.Entity_LocalPlayerMarker), out int marker))
                        AddRow("Player", "IsLocal", marker);
                    else
                        AddBad("Player", "IsLocal");
                }
                else
                {
                    AddBad("Player", "X", "chain failed");
                    AddBad("Player", "Y", "chain failed");
                    AddBad("Player", "Damage", "chain failed");
                }

                // GInput chain
                IntPtr inputPtr = ProcessMemoryReader.ResolvePointer(_hProcess, _airDllBase, BrawlhallaOffsets.GInputChain);
                if (inputPtr != IntPtr.Zero)
                {
                    AddPtr("Input", "base", inputPtr);

                    if (ProcessMemoryReader.ReadInt32(_hProcess, IntPtr.Add(inputPtr, BrawlhallaOffsets.GInput_Value), out int inputVal))
                        AddRow("Input", "Value", inputVal);
                    else
                        AddBad("Input", "Value");
                }
                else
                {
                    AddBad("Input", "Value", "chain failed");
                }
            }
            else
            {
                AddBad("System", "", "Adobe AIR.dll not found");
            }
        }
        catch (Exception ex)
        {
            var item = new ListViewItem("ERROR");
            item.SubItems.Add("");
            item.SubItems.Add(ex.Message);
            item.SubItems.Add("");
            item.ForeColor = System.Drawing.Color.Red;
            lvOffsets.Items.Add(item);
        }

        lvOffsets.EndUpdate();
    }
}
