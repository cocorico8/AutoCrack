using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using System.Text;
using DiscordRPC;
using DiscordRPC.Logging;
using Button = System.Windows.Forms.Button;
using Timer = System.Windows.Forms.Timer;

namespace AutoCrackGUI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        // --- CONFIGURATION ---
        private const string DISCORD_CLIENT_ID = "1383473060350267432";
        private const string API_URL = "https://autocrack.fluxyrepacks.xyz/"; 
        private const string GITHUB_RAW = "https://raw.githubusercontent.com/FluxyRepacks/AutoCrack/main/steam";

        // --- CLIENTS ---
        public DiscordRpcClient discordClient;
        private static readonly HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        // --- UI VARIABLES ---
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;
        private User currentUser = null; 

        // --- CONTROLES UI ---
        private ModernTextBox txtTargetPath;
        private ModernButton btnBrowse;
        private ModernButton btnStart;
        private RichTextBox txtLog;
        private Label lblStatus;
        private ModernProgressBar progressBar;
        private PictureBox picGameCover;
        private Label lblGameNamePreview;
        private Label lblClock;
        private Timer clockTimer;
        
        // Header User Info
        private PictureBox picUserAvatar;
        private Label lblUserName;

        public static class Colors
        {
            public static readonly Color Background = Color.FromArgb(18, 18, 18);
            public static readonly Color Surface = Color.FromArgb(30, 30, 35);
            public static readonly Color SurfaceLight = Color.FromArgb(45, 45, 50);
            public static readonly Color Primary = Color.FromArgb(252, 175, 23); // Jaune Rockstar
            public static readonly Color TextPrimary = Color.FromArgb(240, 240, 240);
            public static readonly Color TextSecondary = Color.FromArgb(150, 150, 150);
            public static readonly Color Success = Color.FromArgb(74, 222, 128);
            public static readonly Color Error = Color.FromArgb(248, 113, 113);
        }

        public MainForm()
        {
            if (!httpClient.DefaultRequestHeaders.Contains("User-Agent"))
                httpClient.DefaultRequestHeaders.Add("User-Agent", "AutoCrack-App/1.0");

            InitializeUI();

            try {
        this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
    } catch { }
            InitializeDiscord(); 
            StartClock();
        }


        private void InitializeUI()
        {
            this.Text = "Fluxy AutoCrack";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Colors.Background;
            this.Icon = SystemIcons.Application;

            // --- Header ---
            var panelHeader = new GradientPanel
            {
                Location = new Point(0, 0),
                Size = new Size(1000, 80),
                StartColor = Color.FromArgb(25, 25, 25),
                EndColor = Color.FromArgb(20, 20, 20)
            };
            panelHeader.MouseDown += Header_MouseDown;
            panelHeader.MouseMove += Header_MouseMove;
            panelHeader.MouseUp += Header_MouseUp;
            this.Controls.Add(panelHeader);

            var btnClose = new Label
            {
                Text = "✕", Font = new Font("Segoe UI", 12), ForeColor = Colors.TextSecondary,
                Location = new Point(960, 10), Size = new Size(30, 30), Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => {
                discordClient?.Dispose();
                Application.Exit();
            };
            panelHeader.Controls.Add(btnClose);

            var lblTitle = new Label { Text = "FLUXY", Font = new Font("Segoe UI Black", 20, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 20), AutoSize = true, BackColor = Color.Transparent };
            var lblSub = new Label { Text = "REPACKS", Font = new Font("Segoe UI", 20, FontStyle.Regular), ForeColor = Colors.Primary, Location = new Point(120, 20), AutoSize = true, BackColor = Color.Transparent };
            panelHeader.Controls.Add(lblTitle);
            panelHeader.Controls.Add(lblSub);

            picUserAvatar = new PictureBox 
            { 
                Location = new Point(830, 15), 
                Size = new Size(50, 50), 
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Transparent 
            };
            GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse(0, 0, 50, 50);
            picUserAvatar.Region = new Region(gp);
            panelHeader.Controls.Add(picUserAvatar);

            lblUserName = new Label { Text = "Connecting...", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Colors.TextPrimary, Location = new Point(700, 20), Size = new Size(120, 40), TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent };
            panelHeader.Controls.Add(lblUserName);

            lblClock = new Label { Text = "00:00", Font = new Font("Consolas", 14), ForeColor = Colors.TextSecondary, Location = new Point(890, 45), AutoSize = true, BackColor = Color.Transparent };
            panelHeader.Controls.Add(lblClock);

            // --- Left Panel ---
            var leftPanel = new RoundedPanel { Location = new Point(30, 110), Size = new Size(300, 400), BackColor = Colors.Surface, CornerRadius = 10 };
            this.Controls.Add(leftPanel);

            picGameCover = new PictureBox
            {
                Location = new Point(10, 10), Size = new Size(280, 160),
                SizeMode = PictureBoxSizeMode.CenterImage, BackColor = Colors.SurfaceLight, Image = null
            };
            picGameCover.Paint += (s, e) => {
                if(picGameCover.Image == null) TextRenderer.DrawText(e.Graphics, "NO GAME SELECTED", new Font("Segoe UI", 10), picGameCover.ClientRectangle, Colors.TextSecondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            leftPanel.Controls.Add(picGameCover);

            lblGameNamePreview = new Label { Text = "Waiting for selection...", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Colors.TextPrimary, Location = new Point(10, 180), Size = new Size(280, 100), TextAlign = ContentAlignment.TopCenter };
            leftPanel.Controls.Add(lblGameNamePreview);

            // --- Right Panel ---
            var rightPanel = new Panel { Location = new Point(350, 110), Size = new Size(620, 500) };
            this.Controls.Add(rightPanel);

            rightPanel.Controls.Add(new Label { Text = "GAME INSTALLATION FOLDER", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Colors.TextSecondary, Location = new Point(0, 0), AutoSize = true });

            txtTargetPath = new ModernTextBox { Location = new Point(0, 25), Size = new Size(480, 40), PlaceholderText = "C:\\Games\\Cyberpunk 2077..." };
            rightPanel.Controls.Add(txtTargetPath);

            btnBrowse = new ModernButton { Text = "BROWSE", Location = new Point(490, 25), Size = new Size(130, 40) };
            btnBrowse.Click += BtnBrowse_Click;
            rightPanel.Controls.Add(btnBrowse);

            rightPanel.Controls.Add(new Label { Text = "ACTIVITY LOG", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Colors.TextSecondary, Location = new Point(0, 85), AutoSize = true });

            txtLog = new RichTextBox
            {
                Location = new Point(0, 110), Size = new Size(620, 220),
                BackColor = Colors.Surface, ForeColor = Colors.TextSecondary,
                BorderStyle = BorderStyle.None, Font = new Font("Consolas", 9), ReadOnly = true
            };
            rightPanel.Controls.Add(txtLog);

            btnStart = new ModernButton
            {
                Text = "APPLY CRACK", Location = new Point(0, 350), Size = new Size(620, 50),
                IsPrimary = true, Font = new Font("Segoe UI", 14, FontStyle.Bold), Enabled = false
            };
            btnStart.Click += BtnStart_Click;
            rightPanel.Controls.Add(btnStart);

            progressBar = new ModernProgressBar { Location = new Point(0, 640), Size = new Size(1000, 10), Dock = DockStyle.Bottom, Visible = false };
            this.Controls.Add(progressBar);
            lblStatus = new Label { Text = "Ready", Font = new Font("Segoe UI", 9), ForeColor = Colors.TextSecondary, Location = new Point(10, 620), AutoSize = true };
            this.Controls.Add(lblStatus);
        }

        private void InitializeDiscord()
        {
            try 
            { 
                discordClient = new DiscordRpcClient(DISCORD_CLIENT_ID);
                
                discordClient.OnReady += (sender, e) =>
                {
                    currentUser = e.User;
                    LogMessage($"Connected to Discord as {e.User.Username}", Colors.Success);
                    
                    this.Invoke(new Action(() => {
                        lblUserName.Text = e.User.Username;
                        string avatarUrl = e.User.GetAvatarURL(User.AvatarFormat.PNG, User.AvatarSize.x128);
                        if(!string.IsNullOrEmpty(avatarUrl)) LoadAvatar(avatarUrl);
                    }));
                };

                discordClient.OnError += (sender, e) =>
                {
                    LogMessage($"Discord Error: {e.Message}", Colors.Error);
                };

                discordClient.OnConnectionFailed += (sender, e) =>
                {
                    LogMessage("Discord connection failed. Make sure Discord is running.", Colors.Error);
                    this.Invoke(new Action(() => lblUserName.Text = "No Discord"));
                };

                discordClient.Initialize(); 
                UpdateDiscordPresence("Idle", "Waiting for game..."); 
            } 
            catch (Exception ex)
            { 
                LogMessage($"Discord Init Error: {ex.Message}", Colors.Error);
                lblUserName.Text = "No Discord";
            }
        }

        private async void LoadAvatar(string url)
        {
            try {
                var bytes = await httpClient.GetByteArrayAsync(url);
                using (var ms = new MemoryStream(bytes)) {
                    var img = Image.FromStream(ms);
                    if (picUserAvatar.InvokeRequired) {
                        picUserAvatar.Invoke(new Action(() => picUserAvatar.Image = img));
                    } else {
                        picUserAvatar.Image = img;
                    }
                }
            } catch (Exception ex) {
                LogMessage($"Avatar load error: {ex.Message}", Colors.Error);
            }
        }

        private void UpdateDiscordPresence(string state, string details)
        {
            try {
                if (discordClient == null || !discordClient.IsInitialized) return;
                discordClient.SetPresence(new RichPresence() 
                { 
                    Details = details, 
                    State = state, 
                    Assets = new Assets() 
                    { 
                        LargeImageKey = "app_logo",
                        LargeImageText = "AutoCrack by Fluxy"
                    },
                    Timestamps = Timestamps.Now
                });
            } catch (Exception ex) {
                LogMessage($"Discord presence error: {ex.Message}", Colors.Error);
            }
        }
        private async Task AnalyzeGameFolder(string path)
        {
            lblGameNamePreview.Text = "Analyzing...";
            picGameCover.Image = null;

            string bestName = new DirectoryInfo(path).Name; 
            string exePath = null;

            var exes = Directory.GetFiles(path, "*.exe", SearchOption.TopDirectoryOnly)
                .Where(f => !f.ToLower().Contains("unins") && 
                           !f.ToLower().Contains("setup") && 
                           !f.ToLower().Contains("crash") &&
                           !f.ToLower().Contains("launcher"))
                .OrderByDescending(f => new FileInfo(f).Length)
                .ToList();

            if (exes.Count > 0)
            {
                exePath = exes[0];
                try {
                    var info = FileVersionInfo.GetVersionInfo(exePath);
                    if (!string.IsNullOrWhiteSpace(info.ProductName)) bestName = info.ProductName;
                } catch { }
            }

            LogMessage($"Detected Game: {bestName}", Colors.Primary);
            
            bool imageFound = await FetchGameInfo(bestName);

            if (!imageFound && exePath != null)
            {
                try {
                    Icon icon = Icon.ExtractAssociatedIcon(exePath);
                    if (icon != null) {
                        picGameCover.Image = icon.ToBitmap();
                        picGameCover.SizeMode = PictureBoxSizeMode.CenterImage;
                        lblGameNamePreview.Text = bestName + "\n(Local Icon)";
                    }
                } catch { }
            }
            if(lblGameNamePreview.Text == "Analyzing...") lblGameNamePreview.Text = bestName;
        }

        private async Task<bool> FetchGameInfo(string gameName)
        {
            try {
                LogMessage($"Searching game info for: {gameName}", Colors.TextSecondary);
                var response = await httpClient.GetStringAsync($"{API_URL}/api/game-info?name={Uri.EscapeDataString(gameName)}");
                using (JsonDocument doc = JsonDocument.Parse(response)) {
                    if (doc.RootElement.GetProperty("found").GetBoolean()) {
                        string imgUrl = doc.RootElement.GetProperty("imageUrl").GetString();
                        string realName = doc.RootElement.GetProperty("title").GetString();
                        
                        lblGameNamePreview.Text = realName;
                        LogMessage($"Found: {realName}", Colors.Success);
                        
                        var imgBytes = await httpClient.GetByteArrayAsync(imgUrl);
                        using (var ms = new MemoryStream(imgBytes)) {
                            picGameCover.Image = Image.FromStream(ms);
                            picGameCover.SizeMode = PictureBoxSizeMode.Zoom;
                        }
                        return true;
                    }
                }
            } catch (Exception ex) {
                LogMessage($"Game info fetch error: {ex.Message}", Colors.Error);
            }
            return false;
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            string targetDir = txtTargetPath.Text;
            
            if (string.IsNullOrWhiteSpace(targetDir) || !Directory.Exists(targetDir))
            {
                MessageBox.Show("Please select a valid game folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string gameName = lblGameNamePreview.Text.Split('\n')[0];

            btnStart.Enabled = false; 
            btnBrowse.Enabled = false; 
            progressBar.Visible = true;
            UpdateDiscordPresence("Cracking...", gameName);
            LogMessage("--- STARTING CRACK PROCESS ---", Colors.Primary);

            try {
                await Task.Run(() => ProcessCrack(targetDir));
                LogMessage("--- CRACK APPLIED SUCCESSFULLY ---", Colors.Success);
                
                string userId = currentUser != null ? currentUser.ID.ToString() : "Anonymous";
                string userName = currentUser != null ? currentUser.Username : "LocalUser";
                await LogCrackAttemptToApi(true, gameName, userId, userName);
                
                MessageBox.Show("Crack Applied Successfully!\n\nYou can now launch the game.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateDiscordPresence("Idle", "Crack applied successfully");
            }
            catch (Exception ex) {
                LogMessage($"ERROR: {ex.Message}", Colors.Error);
                string userId = currentUser != null ? currentUser.ID.ToString() : "Anonymous";
                string userName = currentUser != null ? currentUser.Username : "LocalUser";
                await LogCrackAttemptToApi(false, gameName, userId, userName);
                MessageBox.Show($"An error occurred:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateDiscordPresence("Idle", "Error occurred");
            }
            finally {
                btnStart.Enabled = true; 
                btnBrowse.Enabled = true; 
                progressBar.Visible = false;
            }
        }

        private void ProcessCrack(string targetDir)
        {
            string steamDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steam");
            if (!Directory.Exists(steamDir)) Directory.CreateDirectory(steamDir);
            
            string[] dlls = { "steam_api.dll", "steam_api64.dll" };

            foreach (var dll in dlls) {
                string localPath = Path.Combine(steamDir, dll);
                if (!File.Exists(localPath)) {
                    LogMessage($"Downloading {dll} from GitHub...", Colors.TextSecondary);
                    try {
                        string url = $"{GITHUB_RAW}/{dll}";
                        LogMessage($"URL: {url}", Colors.TextSecondary);
                        var data = httpClient.GetByteArrayAsync(url).Result;
                        File.WriteAllBytes(localPath, data);
                        LogMessage($"✓ Downloaded {dll} ({data.Length} bytes)", Colors.Success);
                    } catch (Exception ex) { 
                        throw new Exception($"Failed to download {dll}: {ex.Message}. Check internet connection and GitHub access."); 
                    }
                }
            }

            LogMessage("Scanning game folder for Steam API files...", Colors.TextPrimary);
            int count = 0;
            void ScanAndReplace(string dir) {
                try {
                    foreach (string file in Directory.GetFiles(dir)) {
                        string fileName = Path.GetFileName(file).ToLower();
                        if (fileName == "steam_api.dll" || fileName == "steam_api64.dll") {
                            string sourcePath = Path.Combine(steamDir, fileName);
                            File.Copy(sourcePath, file, true);
                            LogMessage($"✓ Replaced: {fileName} in {Path.GetFileName(dir)}", Colors.Success);
                            count++;
                        }
                    }
                    foreach (string sub in Directory.GetDirectories(dir)) ScanAndReplace(sub);
                } catch (Exception ex) {
                    LogMessage($"⚠ Access denied to {Path.GetFileName(dir)}: {ex.Message}", Colors.Error);
                }
            }
            ScanAndReplace(targetDir);
            
            if (count == 0) {
                LogMessage("⚠ No Steam API files found in the game folder.", Colors.Error);
                throw new Exception("No Steam API files found. This might not be a Steam game.");
            }
            else {
                LogMessage($"✓ Total files replaced: {count}", Colors.Primary);
            }
        }

        private async Task LogCrackAttemptToApi(bool success, string gameName, string id, string name)
        {
            try {
                var payload = new { 
                    discordId = id, 
                    discordName = name, 
                    gameName = gameName, 
                    status = success ? "SUCCESS" : "FAILED" 
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync($"{API_URL}/api/log", content);
                if (response.IsSuccessStatusCode) {
                    LogMessage("Activity logged to server", Colors.Success);
                } else {
                    LogMessage($"Failed to log activity: {response.StatusCode}", Colors.Error);
                }
            } catch (Exception ex) {
                LogMessage($"API log error: {ex.Message}", Colors.Error);
            }
        }

        private async void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog()) {
                dialog.Description = "Select the game installation folder";
                if (dialog.ShowDialog() == DialogResult.OK) {
                    txtTargetPath.Text = dialog.SelectedPath;
                    btnStart.Enabled = true;
                    await AnalyzeGameFolder(dialog.SelectedPath);
                }
            }
        }

        // --- UTILS ---
        private void StartClock() { 
            clockTimer = new Timer { Interval = 1000 }; 
            clockTimer.Tick += (s, e) => {
                if (lblClock.InvokeRequired) {
                    lblClock.Invoke(new Action(() => lblClock.Text = DateTime.Now.ToString("HH:mm")));
                } else {
                    lblClock.Text = DateTime.Now.ToString("HH:mm");
                }
            }; 
            clockTimer.Start(); 
        }

        private void LogMessage(string msg, Color color) {
            if (txtLog.InvokeRequired) { 
                txtLog.Invoke(new Action<string, Color>(LogMessage), msg, color); 
                return; 
            }
            txtLog.SelectionStart = txtLog.TextLength; 
            txtLog.SelectionLength = 0; 
            txtLog.SelectionColor = color; 
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n"); 
            txtLog.ScrollToCaret();
        }

        private void Header_MouseDown(object sender, MouseEventArgs e) { 
            dragging = true; 
            dragCursorPoint = Cursor.Position; 
            dragFormPoint = this.Location; 
        }

        private void Header_MouseMove(object sender, MouseEventArgs e) { 
            if (dragging) { 
                Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint)); 
                this.Location = Point.Add(dragFormPoint, new Size(dif)); 
            } 
        }

        private void Header_MouseUp(object sender, MouseEventArgs e) { 
            dragging = false; 
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            discordClient?.Dispose();
            clockTimer?.Stop();
            clockTimer?.Dispose();
        }
    }

    // --- CUSTOM CONTROLS ---
    public static class UIHelper
    {
        public static GraphicsPath GetRoundedRect(Rectangle rect, int radius) {
            GraphicsPath path = new GraphicsPath(); 
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure(); 
            return path;
        }
    }

    public class ModernButton : Button {
        public bool IsPrimary { get; set; } = false;
        public ModernButton() { 
            FlatStyle = FlatStyle.Flat; 
            FlatAppearance.BorderSize = 0; 
            Cursor = Cursors.Hand; 
            UpdateColors(); 
        }
        protected override void OnEnabledChanged(EventArgs e) { 
            base.OnEnabledChanged(e); 
            UpdateColors(); 
        }
        private void UpdateColors() { 
            if (Enabled) { 
                BackColor = IsPrimary ? MainForm.Colors.Primary : MainForm.Colors.SurfaceLight; 
                ForeColor = IsPrimary ? Color.Black : MainForm.Colors.TextPrimary; 
            } else { 
                BackColor = MainForm.Colors.Surface; 
                ForeColor = MainForm.Colors.TextSecondary; 
            } 
        }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
            e.Graphics.Clear(Parent.BackColor);
            using (var path = UIHelper.GetRoundedRect(ClientRectangle, 8)) 
            using (var brush = new SolidBrush(BackColor)) { 
                e.Graphics.FillPath(brush, path); 
                TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); 
            }
        }
    }

    public class ModernTextBox : Panel {
        public TextBox box; 
        public string PlaceholderText { get => box.Text; set => box.Text = value; } 
        public override string Text { get => box.Text; set => box.Text = value; }
        public ModernTextBox() {
            BackColor = MainForm.Colors.Surface; 
            Padding = new Padding(10, 10, 10, 10);
            box = new TextBox { 
                BorderStyle = BorderStyle.None, 
                BackColor = MainForm.Colors.Surface, 
                ForeColor = MainForm.Colors.TextPrimary, 
                Font = new Font("Segoe UI", 11), 
                Dock = DockStyle.Fill 
            }; 
            Controls.Add(box);
        }
        protected override void OnPaint(PaintEventArgs e) { 
            using (var pen = new Pen(MainForm.Colors.SurfaceLight, 2)) 
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1); 
        }
    }

    public class RoundedPanel : Panel {
        public int CornerRadius { get; set; } = 15;
        protected override void OnPaint(PaintEventArgs e) { 
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
            using (var path = UIHelper.GetRoundedRect(ClientRectangle, CornerRadius)) 
            using (var brush = new SolidBrush(BackColor)) 
                e.Graphics.FillPath(brush, path); 
        }
    }

    public class GradientPanel : Panel {
        public Color StartColor { get; set; } 
        public Color EndColor { get; set; }
        protected override void OnPaint(PaintEventArgs e) { 
            using (var brush = new LinearGradientBrush(ClientRectangle, StartColor, EndColor, LinearGradientMode.Horizontal)) 
                e.Graphics.FillRectangle(brush, ClientRectangle); 
        }
    }

    public class ModernProgressBar : Control {
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.FillRectangle(new SolidBrush(MainForm.Colors.Surface), ClientRectangle);
            int width = (int)(Width * 0.4); 
            int x = (int)((DateTime.Now.Ticks / 50000) % (Width + width)) - width;
            e.Graphics.FillRectangle(new SolidBrush(MainForm.Colors.Primary), x, 0, width, Height); 
            Invalidate();
        }
    }
}