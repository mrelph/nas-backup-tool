using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NASBackup
{
    public partial class MainForm : Form
    {
        private BackupEngine backupEngine;
        private BackupConfig config;
        private System.Windows.Forms.Timer scheduleTimer;
        private DuplicateAnalyzer duplicateAnalyzer;

        // Modern color scheme
        private readonly Color DarkBackground = Color.FromArgb(24, 26, 31);
        private readonly Color MediumBackground = Color.FromArgb(32, 35, 42);
        private readonly Color LightBackground = Color.FromArgb(45, 48, 58);
        private readonly Color AccentBlue = Color.FromArgb(88, 166, 255);
        private readonly Color AccentTeal = Color.FromArgb(64, 224, 208);
        private readonly Color TextPrimary = Color.FromArgb(255, 255, 255);
        private readonly Color TextSecondary = Color.FromArgb(160, 170, 180);
        private readonly Color SuccessGreen = Color.FromArgb(72, 187, 120);
        private readonly Color WarningOrange = Color.FromArgb(255, 183, 77);
        private readonly Color ErrorRed = Color.FromArgb(255, 99, 99);

        public MainForm()
        {
            InitializeComponent();
            ApplyModernTheme();
            backupEngine = new BackupEngine();
            config = BackupConfig.Load();
            scheduleTimer = new System.Windows.Forms.Timer();
            duplicateAnalyzer = new DuplicateAnalyzer(config.AwsAccessKey, config.AwsSecretKey, config.AwsRegion, config.BedrockModel);
            
            LoadConfiguration();
            SetupEventHandlers();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1000, 700);
            this.Text = "NAS Backup Tool";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.BackColor = DarkBackground;
            this.ForeColor = TextPrimary;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            
            // Main Tab Control
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.BackColor = MediumBackground;
            tabControl.ForeColor = TextPrimary;
            tabControl.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            tabControl.ItemSize = new Size(120, 40);
            tabControl.SizeMode = TabSizeMode.Fixed;
            tabControl.Appearance = TabAppearance.FlatButtons;
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += TabControl_DrawItem;
            
            // Backup Tab
            backupTab = new TabPage("🗂️ Backup");
            backupTab.BackColor = DarkBackground;
            backupTab.ForeColor = TextPrimary;
            SetupBackupTab();
            tabControl.TabPages.Add(backupTab);
            
            // Settings Tab
            settingsTab = new TabPage("⚙️ Settings");
            settingsTab.BackColor = DarkBackground;
            settingsTab.ForeColor = TextPrimary;
            SetupSettingsTab();
            tabControl.TabPages.Add(settingsTab);
            
            // Schedule Tab
            scheduleTab = new TabPage("⏰ Schedule");
            scheduleTab.BackColor = DarkBackground;
            scheduleTab.ForeColor = TextPrimary;
            SetupScheduleTab();
            tabControl.TabPages.Add(scheduleTab);
            
            // Duplicates Tab
            duplicatesTab = new TabPage("🔍 Duplicates");
            duplicatesTab.BackColor = DarkBackground;
            duplicatesTab.ForeColor = TextPrimary;
            SetupDuplicatesTab();
            tabControl.TabPages.Add(duplicatesTab);
            
            this.Controls.Add(tabControl);
            this.ResumeLayout(false);
        }
        
        private void ApplyModernTheme()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        }
        
        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            Rectangle tabRect = tabControl.GetTabRect(e.Index);
            
            // Background
            using (Brush backgroundBrush = new SolidBrush(e.Index == tabControl.SelectedIndex ? AccentBlue : MediumBackground))
            {
                e.Graphics.FillRectangle(backgroundBrush, tabRect);
            }
            
            // Text
            Color textColor = e.Index == tabControl.SelectedIndex ? TextPrimary : TextSecondary;
            using (Brush textBrush = new SolidBrush(textColor))
            {
                StringFormat stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(tabControl.TabPages[e.Index].Text, tabControl.Font, textBrush, tabRect, stringFormat);
            }
        }

        private TabControl tabControl;
        private TabPage backupTab, settingsTab, scheduleTab, duplicatesTab;
        
        // Backup Tab Controls
        private ListBox sourcePathsListBox;
        private Button addSourceButton, removeSourceButton;
        private TextBox destinationPathTextBox;
        private Button browseDestinationButton;
        private Button startBackupButton;
        private Button simulateBackupButton;
        private ProgressBar backupProgressBar;
        private Label statusLabel;
        private RichTextBox logTextBox;
        
        // Settings Tab Controls
        private TextBox nasServerTextBox;
        private TextBox usernameTextBox;
        private TextBox passwordTextBox;
        private Button testConnectionButton;
        private CheckBox useCredentialsCheckBox;
        private CheckBox enableDuplicateDetectionCheckBox;
        private CheckBox autoRemoveDuplicatesCheckBox;
        private TextBox awsAccessKeyTextBox;
        private TextBox awsSecretKeyTextBox;
        private ComboBox awsRegionComboBox;
        private ComboBox bedrockModelComboBox;
        
        // Schedule Tab Controls
        private CheckBox enableScheduleCheckBox;
        private DateTimePicker scheduleTimePicker;
        private CheckBox[] dayCheckBoxes;
        
        // Duplicates Tab Controls
        private Button analyzeDuplicatesButton;
        private ListView duplicatesListView;
        private Button removeDuplicatesButton;
        private Label duplicateStatsLabel;

        private Button CreateStyledButton(string text, Point location, Size size, Color? backgroundColor = null)
        {
            var button = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                BackColor = backgroundColor ?? AccentBlue,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(Math.Min(255, button.BackColor.R + 20), 
                                                                       Math.Min(255, button.BackColor.G + 20), 
                                                                       Math.Min(255, button.BackColor.B + 20));
            return button;
        }
        
        private Label CreateStyledLabel(string text, Point location, Size size, bool isSecondary = false)
        {
            return new Label
            {
                Text = text,
                Location = location,
                Size = size,
                ForeColor = isSecondary ? TextSecondary : TextPrimary,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                BackColor = Color.Transparent
            };
        }
        
        private TextBox CreateStyledTextBox(Point location, Size size, bool isPassword = false)
        {
            var textBox = new TextBox
            {
                Location = location,
                Size = size,
                BackColor = LightBackground,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                UseSystemPasswordChar = isPassword
            };
            return textBox;
        }
        
        private ListBox CreateStyledListBox(Point location, Size size)
        {
            return new ListBox
            {
                Location = location,
                Size = size,
                BackColor = LightBackground,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
        }

        private void SetupBackupTab()
        {
            backupTab.Padding = new Padding(30);
            
            // Source Paths Section
            var sourceLabel = CreateStyledLabel("📁 Source Directories", new Point(30, 30), new Size(200, 25));
            sourceLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            
            sourcePathsListBox = CreateStyledListBox(new Point(30, 65), new Size(650, 130));
            addSourceButton = CreateStyledButton("+ Add Folder", new Point(700, 65), new Size(120, 35), AccentTeal);
            removeSourceButton = CreateStyledButton("− Remove", new Point(700, 110), new Size(120, 35), ErrorRed);
            
            // Destination Path Section
            var destLabel = CreateStyledLabel("🗄️ NAS Destination", new Point(30, 220), new Size(200, 25));
            destLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            
            destinationPathTextBox = CreateStyledTextBox(new Point(30, 255), new Size(650, 30));
            browseDestinationButton = CreateStyledButton("Browse", new Point(700, 255), new Size(120, 35), AccentBlue);
            
            // Action Section
            simulateBackupButton = CreateStyledButton("🔍 Simulate Backup", new Point(30, 320), new Size(150, 45), AccentBlue);
            simulateBackupButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            
            startBackupButton = CreateStyledButton("🚀 Start Backup", new Point(190, 320), new Size(150, 45), SuccessGreen);
            startBackupButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            
            // Progress Section
            var progressLabel = CreateStyledLabel("📊 Progress", new Point(30, 390), new Size(200, 25));
            progressLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            
            backupProgressBar = new ProgressBar { 
                Location = new Point(30, 425), 
                Size = new Size(790, 25),
                Style = ProgressBarStyle.Continuous,
                BackColor = LightBackground,
                ForeColor = AccentTeal
            };
            statusLabel = CreateStyledLabel("Ready to backup", new Point(30, 460), new Size(790, 25), true);
            
            // Log Section
            var logLabel = CreateStyledLabel("📋 Activity Log", new Point(30, 500), new Size(200, 25));
            logLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            
            logTextBox = new RichTextBox { 
                Location = new Point(30, 535), 
                Size = new Size(790, 120), 
                ReadOnly = true,
                BackColor = LightBackground,
                ForeColor = TextSecondary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 8F, FontStyle.Regular)
            };
            
            backupTab.Controls.AddRange(new Control[] {
                sourceLabel, sourcePathsListBox, addSourceButton, removeSourceButton,
                destLabel, destinationPathTextBox, browseDestinationButton,
                simulateBackupButton, startBackupButton, backupProgressBar, statusLabel,
                logLabel, logTextBox
            });
        }

        private CheckBox CreateStyledCheckBox(string text, Point location, Size size)
        {
            return new CheckBox
            {
                Text = text,
                Location = location,
                Size = size,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat
            };
        }
        
        private ComboBox CreateStyledComboBox(Point location, Size size)
        {
            return new ComboBox
            {
                Location = location,
                Size = size,
                BackColor = LightBackground,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
        }

        private void SetupSettingsTab()
        {
            settingsTab.Padding = new Padding(30);
            
            // NAS Connection Section
            var nasLabel = CreateStyledLabel("🌐 NAS Connection", new Point(30, 30), new Size(200, 25));
            nasLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            
            var serverLabel = CreateStyledLabel("Server Address:", new Point(30, 70), new Size(120, 25));
            nasServerTextBox = CreateStyledTextBox(new Point(160, 65), new Size(400, 30));
            
            // Credentials Section
            useCredentialsCheckBox = CreateStyledCheckBox("🔐 Use Authentication", new Point(30, 110), new Size(200, 25));
            
            var userLabel = CreateStyledLabel("Username:", new Point(30, 145), new Size(120, 25));
            usernameTextBox = CreateStyledTextBox(new Point(160, 140), new Size(250, 30));
            
            var passLabel = CreateStyledLabel("Password:", new Point(30, 180), new Size(120, 25));
            passwordTextBox = CreateStyledTextBox(new Point(160, 175), new Size(250, 30), true);
            
            testConnectionButton = CreateStyledButton("🔗 Test Connection", new Point(30, 220), new Size(150, 40), AccentTeal);
            
            // Duplicate Detection Section
            var duplicateLabel = CreateStyledLabel("🔍 Duplicate Detection", new Point(30, 290), new Size(200, 25));
            duplicateLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            
            enableDuplicateDetectionCheckBox = CreateStyledCheckBox("Enable duplicate detection", new Point(30, 325), new Size(250, 25));
            autoRemoveDuplicatesCheckBox = CreateStyledCheckBox("Auto-remove duplicates during backup", new Point(30, 355), new Size(300, 25));
            
            // AWS Bedrock Section
            var aiLabel = CreateStyledLabel("🤖 AWS Bedrock AI", new Point(30, 400), new Size(200, 25));
            aiLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            
            var accessKeyLabel = CreateStyledLabel("Access Key:", new Point(30, 440), new Size(120, 25));
            awsAccessKeyTextBox = CreateStyledTextBox(new Point(160, 435), new Size(300, 30));
            
            var secretKeyLabel = CreateStyledLabel("Secret Key:", new Point(30, 475), new Size(120, 25));
            awsSecretKeyTextBox = CreateStyledTextBox(new Point(160, 470), new Size(300, 30), true);
            
            var regionLabel = CreateStyledLabel("Region:", new Point(30, 510), new Size(120, 25));
            awsRegionComboBox = CreateStyledComboBox(new Point(160, 505), new Size(200, 30));
            awsRegionComboBox.Items.AddRange(new[] { "us-east-1", "us-west-2", "eu-west-1", "ap-southeast-1" });
            
            var modelLabel = CreateStyledLabel("Model:", new Point(30, 545), new Size(120, 25));
            bedrockModelComboBox = CreateStyledComboBox(new Point(160, 540), new Size(400, 30));
            bedrockModelComboBox.Items.AddRange(new[] { 
                "anthropic.claude-3-haiku-20240307-v1:0",
                "anthropic.claude-3-sonnet-20240229-v1:0",
                "anthropic.claude-3-opus-20240229-v1:0",
                "anthropic.claude-v2:1",
                "anthropic.claude-instant-v1"
            });
            
            settingsTab.Controls.AddRange(new Control[] {
                nasLabel, serverLabel, nasServerTextBox, useCredentialsCheckBox,
                userLabel, usernameTextBox, passLabel, passwordTextBox, testConnectionButton,
                duplicateLabel, enableDuplicateDetectionCheckBox, autoRemoveDuplicatesCheckBox,
                aiLabel, accessKeyLabel, awsAccessKeyTextBox, secretKeyLabel, awsSecretKeyTextBox,
                regionLabel, awsRegionComboBox, modelLabel, bedrockModelComboBox
            });
        }

        private void SetupScheduleTab()
        {
            scheduleTab.Padding = new Padding(10);
            
            enableScheduleCheckBox = new CheckBox { Text = "Enable Scheduled Backup", Location = new Point(10, 20), Size = new Size(200, 23) };
            
            var timeLabel = new Label { Text = "Backup Time:", Location = new Point(10, 60), Size = new Size(100, 23) };
            scheduleTimePicker = new DateTimePicker { 
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Location = new Point(120, 60),
                Size = new Size(100, 23)
            };
            
            var daysLabel = new Label { Text = "Days:", Location = new Point(10, 100), Size = new Size(100, 23) };
            
            string[] dayNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            dayCheckBoxes = new CheckBox[7];
            
            for (int i = 0; i < 7; i++)
            {
                dayCheckBoxes[i] = new CheckBox
                {
                    Text = dayNames[i],
                    Location = new Point(10 + (i % 4) * 120, 130 + (i / 4) * 30),
                    Size = new Size(100, 23)
                };
            }
            
            scheduleTab.Controls.Add(enableScheduleCheckBox);
            scheduleTab.Controls.Add(timeLabel);
            scheduleTab.Controls.Add(scheduleTimePicker);
            scheduleTab.Controls.Add(daysLabel);
            scheduleTab.Controls.AddRange(dayCheckBoxes);
        }
        
        private void SetupDuplicatesTab()
        {
            duplicatesTab.Padding = new Padding(10);
            
            analyzeDuplicatesButton = new Button { Text = "Analyze Duplicates", Location = new Point(10, 20), Size = new Size(150, 30) };
            
            duplicateStatsLabel = new Label { Text = "No analysis performed yet", Location = new Point(170, 25), Size = new Size(400, 23) };
            
            duplicatesListView = new ListView { 
                Location = new Point(10, 60), 
                Size = new Size(730, 400),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            
            duplicatesListView.Columns.Add("File Name", 200);
            duplicatesListView.Columns.Add("Size", 80);
            duplicatesListView.Columns.Add("Path", 300);
            duplicatesListView.Columns.Add("Reason", 150);
            
            removeDuplicatesButton = new Button { Text = "Remove Selected", Location = new Point(10, 470), Size = new Size(120, 30) };
            
            duplicatesTab.Controls.AddRange(new Control[] {
                analyzeDuplicatesButton, duplicateStatsLabel,
                duplicatesListView, removeDuplicatesButton
            });
        }

        private void SetupEventHandlers()
        {
            addSourceButton.Click += AddSourceButton_Click;
            removeSourceButton.Click += RemoveSourceButton_Click;
            browseDestinationButton.Click += BrowseDestinationButton_Click;
            simulateBackupButton.Click += SimulateBackupButton_Click;
            startBackupButton.Click += StartBackupButton_Click;
            testConnectionButton.Click += TestConnectionButton_Click;
            useCredentialsCheckBox.CheckedChanged += UseCredentialsCheckBox_CheckedChanged;
            enableScheduleCheckBox.CheckedChanged += EnableScheduleCheckBox_CheckedChanged;
            analyzeDuplicatesButton.Click += AnalyzeDuplicatesButton_Click;
            removeDuplicatesButton.Click += RemoveDuplicatesButton_Click;
            
            backupEngine.ProgressChanged += BackupEngine_ProgressChanged;
            backupEngine.StatusChanged += BackupEngine_StatusChanged;
            backupEngine.LogMessage += BackupEngine_LogMessage;
            
            duplicateAnalyzer.ProgressChanged += DuplicateAnalyzer_ProgressChanged;
            duplicateAnalyzer.LogMessage += DuplicateAnalyzer_LogMessage;
            
            scheduleTimer.Tick += ScheduleTimer_Tick;
            scheduleTimer.Interval = 60000; // Check every minute
        }

        private void LoadConfiguration()
        {
            // Load source paths into listbox
            sourcePathsListBox.Items.Clear();
            foreach (var path in config.SourcePaths)
            {
                sourcePathsListBox.Items.Add(path);
            }
            
            destinationPathTextBox.Text = config.DestinationPath;
            nasServerTextBox.Text = config.NasServer;
            usernameTextBox.Text = config.Username;
            passwordTextBox.Text = config.Password;
            useCredentialsCheckBox.Checked = config.UseCredentials;
            enableDuplicateDetectionCheckBox.Checked = config.EnableDuplicateDetection;
            autoRemoveDuplicatesCheckBox.Checked = config.AutoRemoveDuplicates;
            awsAccessKeyTextBox.Text = config.AwsAccessKey;
            awsSecretKeyTextBox.Text = config.AwsSecretKey;
            awsRegionComboBox.Text = config.AwsRegion;
            bedrockModelComboBox.Text = config.BedrockModel;
            enableScheduleCheckBox.Checked = config.ScheduleEnabled;
            scheduleTimePicker.Value = DateTime.Today.Add(config.ScheduleTime);
            
            for (int i = 0; i < 7; i++)
            {
                dayCheckBoxes[i].Checked = config.ScheduleDays[i];
            }
            
            UpdateCredentialsUI();
        }

        private void SaveConfiguration()
        {
            // Save source paths from listbox
            config.SourcePaths.Clear();
            foreach (string path in sourcePathsListBox.Items)
            {
                config.SourcePaths.Add(path);
            }
            
            config.DestinationPath = destinationPathTextBox.Text;
            config.NasServer = nasServerTextBox.Text;
            config.Username = usernameTextBox.Text;
            config.Password = passwordTextBox.Text;
            config.UseCredentials = useCredentialsCheckBox.Checked;
            config.EnableDuplicateDetection = enableDuplicateDetectionCheckBox.Checked;
            config.AutoRemoveDuplicates = autoRemoveDuplicatesCheckBox.Checked;
            config.AwsAccessKey = awsAccessKeyTextBox.Text;
            config.AwsSecretKey = awsSecretKeyTextBox.Text;
            config.AwsRegion = awsRegionComboBox.Text;
            config.BedrockModel = bedrockModelComboBox.Text;
            config.ScheduleEnabled = enableScheduleCheckBox.Checked;
            config.ScheduleTime = scheduleTimePicker.Value.TimeOfDay;
            
            for (int i = 0; i < 7; i++)
            {
                config.ScheduleDays[i] = dayCheckBoxes[i].Checked;
            }
            
            config.Save();
        }

        private void AddSourceButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (!sourcePathsListBox.Items.Contains(dialog.SelectedPath))
                    {
                        sourcePathsListBox.Items.Add(dialog.SelectedPath);
                        SaveConfiguration();
                    }
                }
            }
        }
        
        private void RemoveSourceButton_Click(object sender, EventArgs e)
        {
            if (sourcePathsListBox.SelectedIndex >= 0)
            {
                sourcePathsListBox.Items.RemoveAt(sourcePathsListBox.SelectedIndex);
                SaveConfiguration();
            }
        }

        private void BrowseDestinationButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    destinationPathTextBox.Text = dialog.SelectedPath;
                    SaveConfiguration();
                }
            }
        }

        private async void SimulateBackupButton_Click(object sender, EventArgs e)
        {
            if (sourcePathsListBox.Items.Count == 0 || string.IsNullOrEmpty(destinationPathTextBox.Text))
            {
                MessageBox.Show("Please select source and destination paths.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            simulateBackupButton.Enabled = false;
            backupProgressBar.Value = 0;
            logTextBox.Clear();

            try
            {
                var sourcePaths = sourcePathsListBox.Items.Cast<string>().ToList();
                var result = await backupEngine.SimulateBackupAsync(sourcePaths, destinationPathTextBox.Text, config);
                
                ShowSimulationResults(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Simulation failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                simulateBackupButton.Enabled = true;
            }
        }

        private void ShowSimulationResults(BackupSimulationResult result)
        {
            var message = new System.Text.StringBuilder();
            message.AppendLine("🔍 BACKUP SIMULATION RESULTS");
            message.AppendLine(new string('=', 40));
            message.AppendLine();
            message.AppendLine($"📊 SUMMARY:");
            message.AppendLine($"   • Total files: {result.TotalFiles:N0}");
            message.AppendLine($"   • Files to copy: {result.FilesToCopy:N0}");
            message.AppendLine($"   • Files to skip: {result.FilesToSkip:N0}");
            message.AppendLine($"   • Total size: {FormatBytes(result.TotalSize)}");
            message.AppendLine($"   • Size to transfer: {FormatBytes(result.SizeToTransfer)}");
            message.AppendLine($"   • Estimated time: {result.EstimatedTime:mm\\:ss}");
            message.AppendLine();
            
            if (result.SampleFilesToCopy.Any())
            {
                message.AppendLine($"📁 SAMPLE FILES TO COPY:");
                foreach (var file in result.SampleFilesToCopy)
                {
                    message.AppendLine($"   • {file}");
                }
                if (result.FilesToCopy > result.SampleFilesToCopy.Count)
                {
                    message.AppendLine($"   ... and {result.FilesToCopy - result.SampleFilesToCopy.Count} more");
                }
                message.AppendLine();
            }
            
            if (result.SampleFilesToSkip.Any())
            {
                message.AppendLine($"⏭️ SAMPLE FILES TO SKIP (already up-to-date):");
                foreach (var file in result.SampleFilesToSkip)
                {
                    message.AppendLine($"   • {file}");
                }
                if (result.FilesToSkip > result.SampleFilesToSkip.Count)
                {
                    message.AppendLine($"   ... and {result.FilesToSkip - result.SampleFilesToSkip.Count} more");
                }
            }

            var form = new Form
            {
                Text = "Backup Simulation Results",
                Size = new Size(600, 500),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = DarkBackground,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9F)
            };

            var textBox = new RichTextBox
            {
                Text = message.ToString(),
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = LightBackground,
                ForeColor = TextPrimary,
                Font = new Font("Consolas", 9F),
                Margin = new Padding(10)
            };

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = DarkBackground
            };

            var okButton = CreateStyledButton("✅ Ready to Backup", new Point(10, 10), new Size(150, 35), SuccessGreen);
            okButton.Dock = DockStyle.Bottom;
            okButton.Click += (s, e) => form.Close();

            panel.Controls.Add(textBox);
            panel.Controls.Add(okButton);
            form.Controls.Add(panel);
            form.ShowDialog(this);
        }

        private async void StartBackupButton_Click(object sender, EventArgs e)
        {
            if (sourcePathsListBox.Items.Count == 0 || string.IsNullOrEmpty(destinationPathTextBox.Text))
            {
                MessageBox.Show("Please select source and destination paths.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            startBackupButton.Enabled = false;
            backupProgressBar.Value = 0;
            logTextBox.Clear();

            try
            {
                var sourcePaths = sourcePathsListBox.Items.Cast<string>().ToList();
                
                // Run duplicate analysis if enabled
                if (config.EnableDuplicateDetection)
                {
                    OnLogMessage("Running duplicate analysis...");
                    var duplicates = await duplicateAnalyzer.AnalyzeDuplicatesAsync(sourcePaths);
                    
                    if (config.AutoRemoveDuplicates && duplicates.Any())
                    {
                        await RemoveDuplicatesAsync(duplicates);
                    }
                }
                
                await backupEngine.StartBackupAsync(sourcePaths, destinationPathTextBox.Text, config);
                MessageBox.Show("Backup completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                startBackupButton.Enabled = true;
            }
        }

        private async void TestConnectionButton_Click(object sender, EventArgs e)
        {
            SaveConfiguration();
            
            try
            {
                testConnectionButton.Enabled = false;
                testConnectionButton.Text = "Testing...";
                
                bool connected = await backupEngine.TestConnectionAsync(config);
                
                MessageBox.Show(
                    connected ? "Connection successful!" : "Connection failed!",
                    "Connection Test",
                    MessageBoxButtons.OK,
                    connected ? MessageBoxIcon.Information : MessageBoxIcon.Error
                );
            }
            finally
            {
                testConnectionButton.Enabled = true;
                testConnectionButton.Text = "Test Connection";
            }
        }

        private void UseCredentialsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCredentialsUI();
            SaveConfiguration();
        }

        private void UpdateCredentialsUI()
        {
            usernameTextBox.Enabled = useCredentialsCheckBox.Checked;
            passwordTextBox.Enabled = useCredentialsCheckBox.Checked;
        }

        private void EnableScheduleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            scheduleTimePicker.Enabled = enableScheduleCheckBox.Checked;
            foreach (var cb in dayCheckBoxes)
                cb.Enabled = enableScheduleCheckBox.Checked;
            
            scheduleTimer.Enabled = enableScheduleCheckBox.Checked;
            SaveConfiguration();
        }

        private async void ScheduleTimer_Tick(object sender, EventArgs e)
        {
            if (!config.ScheduleEnabled) return;
            
            var now = DateTime.Now;
            var today = (int)now.DayOfWeek;
            
            if (config.ScheduleDays[today] && 
                now.TimeOfDay.Hours == config.ScheduleTime.Hours && 
                now.TimeOfDay.Minutes == config.ScheduleTime.Minutes)
            {
                if (!string.IsNullOrEmpty(config.SourcePath) && !string.IsNullOrEmpty(config.DestinationPath))
                {
                    await backupEngine.StartBackupAsync(config.SourcePath, config.DestinationPath, config);
                }
            }
        }

        private void BackupEngine_ProgressChanged(object sender, int progress)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => backupProgressBar.Value = progress));
            }
            else
            {
                backupProgressBar.Value = progress;
            }
        }

        private void BackupEngine_StatusChanged(object sender, string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => statusLabel.Text = status));
            }
            else
            {
                statusLabel.Text = status;
            }
        }

        private void BackupEngine_LogMessage(object sender, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => {
                    logTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
                    logTextBox.ScrollToCaret();
                }));
            }
            else
            {
                logTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
                logTextBox.ScrollToCaret();
            }
        }

        private async void AnalyzeDuplicatesButton_Click(object sender, EventArgs e)
        {
            if (sourcePathsListBox.Items.Count == 0)
            {
                MessageBox.Show("Please select source paths first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            analyzeDuplicatesButton.Enabled = false;
            duplicatesListView.Items.Clear();
            duplicateStatsLabel.Text = "Analyzing...";

            try
            {
                var sourcePaths = sourcePathsListBox.Items.Cast<string>().ToList();
                var duplicates = await duplicateAnalyzer.AnalyzeDuplicatesAsync(sourcePaths);

                PopulateDuplicatesListView(duplicates);
                
                var totalFiles = duplicates.Sum(g => g.Files.Count);
                var wastedSpace = duplicates.Sum(g => g.WastedSpace);
                duplicateStatsLabel.Text = $"Found {duplicates.Count} duplicate groups, {totalFiles} files, {FormatBytes(wastedSpace)} wasted space";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Analysis failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                duplicateStatsLabel.Text = "Analysis failed";
            }
            finally
            {
                analyzeDuplicatesButton.Enabled = true;
            }
        }

        private void PopulateDuplicatesListView(List<DuplicateGroup> duplicates)
        {
            duplicatesListView.Items.Clear();
            
            foreach (var group in duplicates)
            {
                foreach (var file in group.Files)
                {
                    var item = new ListViewItem(file.Name);
                    item.SubItems.Add(FormatBytes(file.Size));
                    item.SubItems.Add(file.Path);
                    item.SubItems.Add(group.Reason);
                    item.Tag = file;
                    duplicatesListView.Items.Add(item);
                }
            }
        }

        private async void RemoveDuplicatesButton_Click(object sender, EventArgs e)
        {
            if (duplicatesListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select files to remove.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete {duplicatesListView.SelectedItems.Count} selected files?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                var filesToDelete = duplicatesListView.SelectedItems.Cast<ListViewItem>()
                    .Select(item => (BackupFileInfo)item.Tag)
                    .ToList();

                await RemoveFilesAsync(filesToDelete);
            }
        }

        private async Task RemoveFilesAsync(List<BackupFileInfo> files)
        {
            int deleted = 0;
            int failed = 0;

            foreach (var file in files)
            {
                try
                {
                    if (File.Exists(file.Path))
                    {
                        await Task.Run(() => File.Delete(file.Path));
                        deleted++;
                        OnLogMessage($"Deleted: {file.Path}");
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    OnLogMessage($"Failed to delete {file.Path}: {ex.Message}");
                }
            }

            MessageBox.Show($"Deletion complete. {deleted} files deleted, {failed} failed.", "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            // Refresh the duplicates view
            if (analyzeDuplicatesButton.Enabled)
            {
                AnalyzeDuplicatesButton_Click(analyzeDuplicatesButton, EventArgs.Empty);
            }
        }

        private async Task RemoveDuplicatesAsync(List<DuplicateGroup> duplicateGroups)
        {
            var filesToRemove = new List<BackupFileInfo>();
            
            foreach (var group in duplicateGroups)
            {
                if (group.Files.Count > 1)
                {
                    // Keep the newest file, remove others
                    var newest = group.Files.OrderByDescending(f => f.ModifiedDate).First();
                    filesToRemove.AddRange(group.Files.Where(f => f != newest));
                }
            }
            
            if (filesToRemove.Any())
            {
                OnLogMessage($"Auto-removing {filesToRemove.Count} duplicate files...");
                await RemoveFilesAsync(filesToRemove);
            }
        }

        private void DuplicateAnalyzer_ProgressChanged(object sender, string progress)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => duplicateStatsLabel.Text = progress));
            }
            else
            {
                duplicateStatsLabel.Text = progress;
            }
        }

        private void DuplicateAnalyzer_LogMessage(object sender, string message)
        {
            OnLogMessage(message);
        }

        private void OnLogMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => {
                    logTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
                    logTextBox.ScrollToCaret();
                }));
            }
            else
            {
                logTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
                logTextBox.ScrollToCaret();
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            
            return $"{number:n1} {suffixes[counter]}";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveConfiguration();
            duplicateAnalyzer?.Dispose();
            base.OnFormClosing(e);
        }
    }
}