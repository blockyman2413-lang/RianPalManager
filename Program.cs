
#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace RianPalworldManager
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class SettingInfo
    {
        public string Key { get; }
        public string Label { get; }
        public string Category { get; }
        public string DefaultValue { get; }

        public SettingInfo(string key, string label, string category, string defaultValue)
        {
            Key = key;
            Label = label;
            Category = category;
            DefaultValue = defaultValue;
        }
    }

    // Prevent accidental setting changes when the user scrolls through the form.
    // Dropdown selections remain available by opening the list and clicking an option.
    internal sealed class ClickOnlyComboBox : ComboBox
    {
        private const int WmMouseWheel = 0x020A;
        private const int WmMouseHWheel = 0x020E;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg is WmMouseWheel or WmMouseHWheel)
                return;

            base.WndProc(ref m);
        }
    }

    public sealed class MainForm : Form
    {
        private const int OriginalWidth = 650;
        private const int OriginalHeight = 800;

        private string selectedFile = "";

        private readonly Dictionary<string, Control> inputs =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string> settings =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> settingOrder = new();

        private readonly string appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RianPalworldManager");

        private readonly TextBox filePath = new();
        private readonly Label status = new();
        private readonly Panel settingsPanel = new();
        private readonly CheckBox customSizeCheck = new();

        private readonly Button browseButton = new();
        private readonly Button reloadButton = new();
        private readonly Button saveButton = new();
        private readonly Button resetButton = new();
        private readonly Button backupButton = new();
        private readonly Button aboutButton = new();
        private readonly ToolTip aboutToolTip = new();

        private const string BackupFolderName = "RianPalManager Backup File";
        private const string BackupReadme = """
# RianPalManager backups

Choose **Create Backup** in RianPalManager to open a save dialog for the currently selected `PalWorldSettings.ini`. The dialog starts in this folder with a timestamped filename; choose another folder or filename if you prefer. A copy of this README is placed beside the backup. Saving settings does not create a backup automatically.

## Restore a backup

1. Stop the Palworld dedicated server.
2. In RianPalManager, note the selected `PalWorldSettings.ini` path. Use **Browse** if needed.
3. Keep a copy of the current INI somewhere safe before replacing it.
4. Copy the backup you want from this folder and paste it into the selected file's `WindowsServer` folder.
5. Rename the copied backup to `PalWorldSettings.ini` and approve replacing the current file.
6. Start the dedicated server and check that the settings are restored.

Backups contain your server settings, including any passwords. Keep this folder private and do not commit its INI files to a public repository.
""";

        private readonly Panel header = new();
        private readonly Panel fileBar = new();
        private readonly Panel bottom = new();

        private readonly List<SettingInfo> definitions = new()
        {
            // SERVER
            new("ServerName", "Server Name", "Server", "\"Rian Server's\""),
            new("ServerDescription", "Description", "Server", "\"\""),
            new("ServerPassword", "Server Password", "Server", "\"\""),
            new("AdminPassword", "Admin Password", "Server", "\"\""),
            new("ServerPlayerMaxNum", "Maximum Players", "Server", "5"),
            new("PublicPort", "Public Port", "Server", "8211"),
            new("PublicIP", "Public IP", "Server", "\"\""),
            new("RCONEnabled", "Enable RCON", "Server", "False"),
            new("RCONPort", "RCON Port", "Server", "25575"),
            new("bUseAuth", "Use Authentication", "Server", "True"),
            new("RESTAPIEnabled", "Enable REST API", "Server", "False"),
            new("RESTAPIPort", "REST API Port", "Server", "8212"),
            new("Region", "Region", "Server", "\"\""),
            new("bIsMultiplay", "Multiplayer", "Server", "True"),
            new("bIsPvP", "Enable PvP", "Server", "False"),
            new("bEnableInvaderEnemy", "Enable Invaders", "Server", "True"),
            new("bEnableNonLoginPenalty", "Non-login Penalty", "Server", "True"),
            new("bEnableFastTravel", "Enable Fast Travel", "Server", "True"),
            new("bAllowGlobalPalboxExport", "Allow Palbox Export", "Server", "True"),
            new("bAllowGlobalPalboxImport", "Allow Palbox Import", "Server", "False"),
            new("AutoSaveSpan", "Auto-save Interval", "Server", "30"),
            new("bShowPlayerList", "Show Player List", "Server", "False"),
            new("bIsShowJoinLeftMessage", "Show Join/Leave Messages", "Server", "True"),

            // PLAYER
            new("ExpRate", "Experience Rate", "Player", "1.000000"),
            new("DeathPenalty", "Death Penalty", "Player", "All"),
            new("PlayerStomachDecreaceRate", "Hunger Decrease Rate", "Player", "1.000000"),
            new("PlayerStaminaDecreaceRate", "Stamina Decrease Rate", "Player", "1.000000"),
            new("PlayerAutoHPRegeneRate", "HP Regeneration", "Player", "1.000000"),
            new("PlayerAutoHpRegeneRateInSleep", "Sleep HP Regeneration", "Player", "1.000000"),
            new("PlayerDamageRateAttack", "Player Attack Damage", "Player", "1.000000"),
            new("PlayerDamageRateDefense", "Player Defense Rate", "Player", "1.000000"),
            new("ItemWeightRate", "Item Weight Rate", "Player", "1.000000"),
            new("bIsStartLocationSelectByMap", "Choose Starting Location", "Player", "True"),
            new("bExistPlayerAfterLogout", "Keep Player After Logout", "Player", "False"),
            new("bEnableFastTravelOnlyBaseCamp", "Fast Travel Only From Base", "Player", "False"),
            new("bAllowEnhanceStat_Health", "Enhance Health", "Player", "True"),
            new("bAllowEnhanceStat_Attack", "Enhance Attack", "Player", "True"),
            new("bAllowEnhanceStat_Stamina", "Enhance Stamina", "Player", "True"),
            new("bAllowEnhanceStat_Weight", "Enhance Weight", "Player", "True"),
            new("bAllowEnhanceStat_WorkSpeed", "Enhance Work Speed", "Player", "True"),

            // PALS
            new("PalCaptureRate", "Capture Rate", "Pals", "1.000000"),
            new("PalSpawnNumRate", "Pal Spawn Rate", "Pals", "1.000000"),
            new("PalDamageRateAttack", "Pal Attack Damage", "Pals", "1.000000"),
            new("PalDamageRateDefense", "Pal Defense Rate", "Pals", "1.000000"),
            new("PalStomachDecreaceRate", "Pal Hunger Decrease", "Pals", "1.000000"),
            new("PalStaminaDecreaceRate", "Pal Stamina Decrease", "Pals", "1.000000"),
            new("PalAutoHPRegeneRate", "Pal HP Regeneration", "Pals", "1.000000"),
            new("PalAutoHpRegeneRateInSleep", "Pal Sleep HP Regeneration", "Pals", "1.000000"),
            new("PalEggDefaultHatchingTime", "Egg Hatching Time", "Pals", "72.000000"),
            new("WorkSpeedRate", "Work Speed Rate", "Pals", "1.000000"),
            new("MonsterFarmActionSpeedRate", "Farm Action Speed", "Pals", "1.000000"),

            // WORLD
            new("DayTimeSpeedRate", "Day Speed", "World", "1.000000"),
            new("NightTimeSpeedRate", "Night Speed", "World", "1.000000"),
            new("CollectionDropRate", "Gathering Drop Rate", "World", "1.000000"),
            new("CollectionObjectHpRate", "Gatherable Object HP", "World", "1.000000"),
            new("CollectionObjectRespawnSpeedRate", "Object Respawn Speed", "World", "1.000000"),
            new("EnemyDropItemRate", "Enemy Drop Rate", "World", "1.000000"),
            new("SupplyDropSpan", "Supply Drop Interval", "World", "180"),
            new("bEnablePredatorBossPal", "Predator Boss Pals", "World", "True"),
            new("bEnableDefenseOtherGuildPlayer", "Other Guild Defense", "World", "False"),
            new("bIsRandomizerPalLevelRandom", "Randomize Pal Levels", "World", "False"),

            // BUILDING
            new("BuildObjectHpRate", "Building HP Rate", "Building", "1.000000"),
            new("BuildObjectDamageRate", "Building Damage Rate", "Building", "1.000000"),
            new("BuildObjectDeteriorationDamageRate", "Building Deterioration", "Building", "1.000000"),
            new("BaseCampMaxNum", "Maximum Bases", "Building", "128"),
            new("BaseCampWorkerMaxNum", "Workers Per Base", "Building", "15"),
            new("BaseCampMaxNumInGuild", "Bases Per Guild", "Building", "4"),
            new("MaxBuildingLimitNum", "Maximum Building Limit", "Building", "0"),
            new("MaxBuildingLimitNumPerPlayer", "Building Limit Per Player", "Building", "0"),
            new("BlockRespawnTime", "Block Respawn Time", "Building", "0.000000"),
            new("bBuildAreaLimit", "Building Area Limit", "Building", "True"),
            new("bAllowEnemyCampSpawnNearBaseCamp", "Enemy Camps Near Bases", "Building", "True"),
            new("bEnableBuildingPlayerUIdDisplay", "Show Builder ID", "Building", "False"),

            // MULTIPLAYER
            new("CoopPlayerMaxNum", "Co-op Maximum Players", "Multiplayer", "4"),
            new("GuildPlayerMaxNum", "Maximum Guild Members", "Multiplayer", "20"),
            new("bEnablePlayerToPlayerDamage", "Player-to-Player Damage", "Multiplayer", "False"),
            new("bEnableFriendlyFire", "Friendly Fire", "Multiplayer", "False"),
            new("bAutoResetGuildNoOnlinePlayers", "Reset Empty Guilds", "Multiplayer", "False"),
            new("AutoResetGuildTimeNoOnlinePlayers", "Guild Reset Time", "Multiplayer", "72.000000"),
            new("GuildRejoinCooldownMinutes", "Guild Rejoin Cooldown", "Multiplayer", "0"),
            new("bCanPickupOtherGuildDeathPenaltyDrop", "Pick Up Other Guild Drops", "Multiplayer", "False"),

            // ADVANCED
            new("bHardcore", "Hardcore Mode", "Advanced", "False"),
            new("bPalLost", "Pal Lost on Death", "Advanced", "False"),
            new("bCharacterRecreateInHardcore", "Recreate Character in Hardcore", "Advanced", "False"),
            new("bActiveUNKO", "Enable UNKO", "Advanced", "False"),
            new("EquipmentDurabilityDamageRate", "Equipment Durability Damage", "Advanced", "1.000000"),
            new("FishingDifficultyRate", "Fishing Difficulty", "Advanced", "1.000000"),
            new("bAllowClientMod", "Allow Client Mods", "Advanced", "False"),
            new("bEnableVoiceChat", "Enable Voice Chat", "Advanced", "False"),
            new("VoiceChatMaxVolumeDistance", "Voice Chat Max Distance", "Advanced", "0.000000"),
            new("VoiceChatZeroVolumeDistance", "Voice Chat Zero Distance", "Advanced", "0.000000"),
            new("ChatPostLimitPerMinute", "Chat Limit Per Minute", "Advanced", "10"),
            new("bIsUseBackupSaveData", "Use Backup Save Data", "Advanced", "True"),
            new("DropItemMaxNum", "Maximum Dropped Items", "Advanced", "3000"),
            new("DropItemAliveMaxHours", "Dropped Item Lifetime", "Advanced", "1.000000"),
            new("ServerReplicatePawnCullDistance", "Replication Cull Distance", "Advanced", "15000.000000")
        };

        public MainForm()
        {
            Text = "RianPalManager";
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
            AutoScaleMode = AutoScaleMode.Dpi;

            Size = new Size(OriginalWidth, OriginalHeight);
            MinimumSize = new Size(OriginalWidth, OriginalHeight);
            MaximumSize = new Size(OriginalWidth, OriginalHeight);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            BuildInterface();

            Shown += async (_, _) =>
            {
                status.Text = "Looking for dedicated-server installations...";
                string[] detected = await Task.Run(FindPalworldSettingsFiles);
                if (detected.Length == 1)
                {
                    selectedFile = detected[0];
                    filePath.Text = selectedFile;
                    LoadSettings();
                }
                else if (detected.Length > 1)
                {
                    string? chosen = ChooseInstallation(detected);
                    if (chosen != null)
                    {
                        selectedFile = chosen;
                        filePath.Text = chosen;
                        RememberServerPath(chosen);
                        LoadSettings();
                    }
                    else status.Text = "Choose a dedicated-server file with Browse.";
                }
                else
                {
                    status.Text = "Dedicated server not found. Use Browse to select it once.";
                    status.ForeColor = Color.DarkOrange;
                }
            };
        }

        private void BuildInterface()
        {
            header.Dock = DockStyle.Top;
            header.Height = 88;
            header.BackColor = Color.White;

            customSizeCheck.Text = "Custom Size";
            customSizeCheck.AutoSize = true;
            customSizeCheck.Location = new Point(12, 8);
            customSizeCheck.Checked = false;
            customSizeCheck.CheckedChanged += CustomSizeCheck_CheckedChanged;

            aboutButton.Text = string.Empty;
            aboutButton.Size = new Size(36, 36);
            aboutButton.Location = new Point(0, 4);
            aboutButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            aboutButton.FlatStyle = FlatStyle.Flat;
            aboutButton.FlatAppearance.BorderSize = 0;
            aboutButton.Image = CreateInfoIcon();
            aboutButton.ImageAlign = ContentAlignment.MiddleCenter;
            aboutButton.BackColor = Color.White;
            aboutButton.UseVisualStyleBackColor = false;
            aboutButton.AccessibleName = "About RianPalManager";
            aboutButton.AccessibleDescription = "Show copyright information";
            aboutToolTip.SetToolTip(aboutButton, "About RianPalManager");
            aboutButton.Click += (_, _) => ShowAboutDialog();
            header.Resize += (_, _) => PositionAboutButton();

            header.Controls.Add(customSizeCheck);
            header.Controls.Add(aboutButton);
            header.Controls.Add(new Label
            {
                Text = "RianPalManager",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(12, 30)
            });
            header.Controls.Add(new Label
            {
                Text = "Dedicated server configuration editor",
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(15, 62)
            });

            fileBar.Dock = DockStyle.Top;
            fileBar.Height = 50;
            fileBar.Padding = new Padding(8);
            fileBar.BackColor = Color.FromArgb(248, 248, 248);

            filePath.ReadOnly = true;
            filePath.Location = new Point(8, 10);
            filePath.Width = 280;
            filePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            browseButton.Text = "Browse";
            browseButton.Size = new Size(70, 28);
            browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            browseButton.Click += (_, _) => BrowseFile();

            reloadButton.Text = "Reload";
            reloadButton.Size = new Size(65, 28);
            reloadButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            reloadButton.Click += (_, _) => LoadSettings();

            fileBar.Controls.Add(filePath);
            fileBar.Controls.Add(browseButton);
            fileBar.Controls.Add(reloadButton);

            fileBar.Resize += (_, _) =>
            {
                reloadButton.Location = new Point(fileBar.ClientSize.Width - 73, 9);
                browseButton.Location = new Point(fileBar.ClientSize.Width - 150, 9);
                filePath.Width = Math.Max(80, browseButton.Left - filePath.Left - 7);
            };

            settingsPanel.Dock = DockStyle.Fill;
            settingsPanel.AutoScroll = true;
            settingsPanel.BackColor = Color.White;
            settingsPanel.Padding = new Padding(8);

            status.Dock = DockStyle.Bottom;
            status.Height = 27;
            status.Padding = new Padding(8, 5, 5, 3);
            status.ForeColor = Color.Gray;

            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 56;
            bottom.BackColor = Color.White;

            resetButton.Text = "Reset Fields";
            resetButton.Size = new Size(100, 34);
            resetButton.Location = new Point(8, 10);
            resetButton.Click += (_, _) => ResetFields();

            backupButton.Text = "Create Backup";
            backupButton.Size = new Size(110, 34);
            backupButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            backupButton.Click += (_, _) => CreateBackup();

            saveButton.Text = "Save Settings";
            saveButton.Size = new Size(120, 34);
            saveButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            saveButton.BackColor = Color.FromArgb(30, 30, 30);
            saveButton.ForeColor = Color.White;
            saveButton.FlatStyle = FlatStyle.Flat;
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.Click += (_, _) => SaveSettings();

            bottom.Controls.Add(resetButton);
            bottom.Controls.Add(backupButton);
            bottom.Controls.Add(saveButton);
            bottom.Resize += (_, _) =>
                LayoutBottomButtons();
            LayoutBottomButtons();

            Controls.Add(settingsPanel);
            Controls.Add(status);
            Controls.Add(bottom);
            Controls.Add(fileBar);
            Controls.Add(header);
            PositionAboutButton();

            BuildSettingFields();
        }

        private void CustomSizeCheck_CheckedChanged(object? sender, EventArgs e)
        {
            if (customSizeCheck.Checked)
            {
                // Unlock resizing and allow the window to expand.
                MaximumSize = Size.Empty;
                MinimumSize = new Size(650, 500);
                FormBorderStyle = FormBorderStyle.Sizable;
                MaximizeBox = true;
            }
            else
            {
                // Restore the original compact dimensions and lock the window.
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.FixedSingle;
                MaximizeBox = false;
                MaximumSize = Size.Empty;
                MinimumSize = new Size(OriginalWidth, OriginalHeight);
                Size = new Size(OriginalWidth, OriginalHeight);
                MaximumSize = new Size(OriginalWidth, OriginalHeight);
            }

            ResizeSettingControls();
        }

        private void LayoutBottomButtons()
        {
            saveButton.Location = new Point(bottom.ClientSize.Width - 128, 10);
            backupButton.Location = new Point(saveButton.Left - 118, 10);
        }

        private void PositionAboutButton()
        {
            aboutButton.Location = new Point(
                Math.Max(8, header.ClientSize.Width - aboutButton.Width - 12), 4);
        }

        private void ShowAboutDialog()
        {
            using var dialog = new Form
            {
                Text = "RianPalManager Copyright",
                Icon = SystemIcons.Information,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ClientSize = new Size(360, 220),
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.White,
                Font = Font
            };

            dialog.Controls.Add(new Label
            {
                Text = "RianPalManager",
                Font = new Font(Font.FontFamily, 20F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 25),
                Size = new Size(320, 42)
            });
            dialog.Controls.Add(new Label
            {
                Text = $"Copyright © {DateTime.Now.Year} RIAN",
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 96),
                Size = new Size(320, 26)
            });
            dialog.Controls.Add(new Label
            {
                Text = "All rights reserved.",
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 128),
                Size = new Size(320, 26)
            });

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(260, 174),
                Size = new Size(85, 30)
            };
            dialog.Controls.Add(okButton);
            dialog.AcceptButton = okButton;
            dialog.ShowDialog(this);
        }

        private static Bitmap CreateInfoIcon()
        {
            var image = new Bitmap(28, 28);
            using Graphics graphics = Graphics.FromImage(image);
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var outline = new Pen(Color.FromArgb(105, 105, 105), 1.3F);
            graphics.DrawEllipse(outline, 2, 2, 24, 24);
            using var font = new Font("Segoe UI", 16F, FontStyle.Regular);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            graphics.DrawString("i", font, SystemBrushes.ControlText,
                new RectangleF(2, 1, 24, 25), format);
            return image;
        }

        private static bool IsBooleanSetting(SettingInfo info) =>
            info.DefaultValue.Equals("True", StringComparison.OrdinalIgnoreCase) ||
            info.DefaultValue.Equals("False", StringComparison.OrdinalIgnoreCase);

        private static string[] GetChoiceValues(SettingInfo info)
        {
            if (IsBooleanSetting(info)) return new[] { "False", "True" };
            if (info.Key.Equals("DeathPenalty", StringComparison.OrdinalIgnoreCase))
                return new[] { "None", "Item", "ItemAndEquipment", "All" };
            return Array.Empty<string>();
        }

        private static Control CreateSettingInput(SettingInfo info)
        {
            string[] choices = GetChoiceValues(info);
            if (choices.Length > 0)
            {
                var combo = new ClickOnlyComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    IntegralHeight = true
                };
                combo.Items.AddRange(choices);
                return combo;
            }

            return new TextBox();
        }

        private static void SetSettingInputValue(Control input, SettingInfo info, string value)
        {
            string displayValue = IsQuotedStringSetting(info)
                ? DecodeIniString(value)
                : value;

            if (input is ComboBox combo)
            {
                int index = -1;
                for (int i = 0; i < combo.Items.Count; i++)
                {
                    if (string.Equals(combo.Items[i]?.ToString(), displayValue, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }

                // Keep unfamiliar values visible and unchanged until the user chooses another option.
                if (index < 0)
                {
                    combo.Items.Add(displayValue);
                    index = combo.Items.Count - 1;
                }
                combo.SelectedIndex = index;
            }
            else
            {
                input.Text = displayValue;
            }
        }

        private static bool IsQuotedStringSetting(SettingInfo info) =>
            info.DefaultValue.Length >= 2 &&
            info.DefaultValue.StartsWith("\"", StringComparison.Ordinal) &&
            info.DefaultValue.EndsWith("\"", StringComparison.Ordinal);

        private static string DecodeIniString(string value)
        {
            if (value.Length < 2 || value[0] != '"' || value[^1] != '"')
                return value;

            var decoded = new StringBuilder(value.Length - 2);
            for (int i = 1; i < value.Length - 1; i++)
            {
                char current = value[i];
                if (current == '\\' && i + 1 < value.Length - 1 &&
                    (value[i + 1] == '\\' || value[i + 1] == '"'))
                {
                    decoded.Append(value[++i]);
                }
                else
                {
                    decoded.Append(current);
                }
            }
            return decoded.ToString();
        }

        private void BuildSettingFields()
        {
            settingsPanel.SuspendLayout();
            settingsPanel.Controls.Clear();
            inputs.Clear();

            int y = 8;
            const int labelWidth = 190;

            string[] categories =
            {
                "Server", "Player", "Pals", "World",
                "Building", "Multiplayer", "Advanced"
            };

            foreach (string category in categories)
            {
                settingsPanel.Controls.Add(new Label
                {
                    Text = category,
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(8, y)
                });

                y += 29;

                foreach (SettingInfo info in definitions.Where(
                    x => x.Category == category))
                {
                    var label = new Label
                    {
                        Text = info.Label,
                        AutoEllipsis = true,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Location = new Point(10, y),
                        Size = new Size(labelWidth, 26)
                    };

                    Control input = CreateSettingInput(info);
                    input.Location = new Point(labelWidth + 15, y + 1);
                    input.Width = 300;
                    input.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    SetSettingInputValue(input, info, info.DefaultValue);

                    inputs[info.Key] = input;
                    settingsPanel.Controls.Add(label);
                    settingsPanel.Controls.Add(input);
                    y += 32;
                }

                y += 10;
            }

            settingsPanel.Resize += (_, _) => ResizeSettingControls();
            ResizeSettingControls();
            settingsPanel.ResumeLayout(true);
        }

        private void ResizeSettingControls()
        {
            foreach (Control control in settingsPanel.Controls)
            {
                if (control is TextBox or ComboBox)
                {
                    control.Width = Math.Max(
                        100,
                        settingsPanel.ClientSize.Width - control.Left - 25);
                }
            }
        }

        private string GetSavedServerPath()
        {
            string pathFile = Path.Combine(appFolder, "serverpath.txt");

            try
            {
                return File.Exists(pathFile)
                    ? File.ReadAllText(pathFile).Trim()
                    : "";
            }
            catch
            {
                return "";
            }
        }

        private void RememberServerPath(string path)
        {
            try
            {
                Directory.CreateDirectory(appFolder);
                File.WriteAllText(Path.Combine(appFolder, "serverpath.txt"), path);
            }
            catch
            {
                // Remembering the path is optional.
            }
        }

        private bool IsDedicatedServerSettingsFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            try
            {
                string fullPath = Path.GetFullPath(path);
                string suffix = Path.Combine(
                    "Pal", "Saved", "Config",
                    "WindowsServer", "PalWorldSettings.ini");

                if (!fullPath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return false;

                DirectoryInfo? serverRoot = new FileInfo(fullPath).Directory;
                for (int i = 0; i < 4 && serverRoot != null; i++) serverRoot = serverRoot.Parent;
                if (!string.Equals(serverRoot?.Name, "PalServer", StringComparison.OrdinalIgnoreCase))
                    return false;

                string content = File.ReadAllText(fullPath);
                return content.IndexOf("OptionSettings", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private string[] FindPalworldSettingsFiles()
        {
            string saved = GetSavedServerPath();
            if (IsDedicatedServerSettingsFile(saved))
                return new[] { Path.GetFullPath(saved) };

            var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string root in GetServerRoots())
            {
                string candidate = Path.Combine(
                    root, "Pal", "Saved", "Config",
                    "WindowsServer", "PalWorldSettings.ini");

                if (IsDedicatedServerSettingsFile(candidate))
                {
                    matches.Add(Path.GetFullPath(candidate));
                }
            }

            return matches.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private IEnumerable<string> GetServerRoots()
        {
            string[] knownRoots =
            {
                @"C:\steamcmd\steamapps\common\PalServer",
                @"C:\Program Files (x86)\Steam\steamapps\common\PalServer",
                @"C:\Program Files\Steam\steamapps\common\PalServer",
                @"C:\SteamLibrary\steamapps\common\PalServer",
                @"D:\steamcmd\steamapps\common\PalServer",
                @"D:\SteamLibrary\steamapps\common\PalServer",
                @"D:\PalServer",
                @"E:\SteamLibrary\steamapps\common\PalServer",
                @"E:\PalServer"
            };

            foreach (string root in knownRoots)
                yield return root;

            foreach (string steamRoot in GetSteamRoots())
                yield return Path.Combine(steamRoot, "steamapps", "common", "PalServer");

            DriveInfo[] drives;

            try
            {
                drives = DriveInfo.GetDrives();
            }
            catch
            {
                yield break;
            }

            foreach (DriveInfo drive in drives)
            {
                bool ready;

                try
                {
                    ready = drive.IsReady && drive.DriveType == DriveType.Fixed;
                }
                catch
                {
                    ready = false;
                }

                if (!ready)
                    continue;

                string root = drive.RootDirectory.FullName;

                yield return Path.Combine(root, "steamcmd", "steamapps", "common", "PalServer");
                yield return Path.Combine(root, "SteamLibrary", "steamapps", "common", "PalServer");
                yield return Path.Combine(root, "PalServer");
            }
        }

        private IEnumerable<string> GetSteamRoots()
        {
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                if (key?.GetValue("SteamPath") is string path && !string.IsNullOrWhiteSpace(path))
                    roots.Add(path.Replace('/', '\\'));
            }
            catch { }

            foreach (string steamRoot in roots.ToArray())
            {
                string libraries = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
                try
                {
                    if (!File.Exists(libraries)) continue;
                    foreach (Match match in Regex.Matches(File.ReadAllText(libraries), "\\\"path\\\"\\s+\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase))
                        roots.Add(match.Groups[1].Value.Replace("\\\\", "\\"));
                }
                catch { }
            }
            return roots;
        }

        private string? ChooseInstallation(string[] candidates)
        {
            using var dialog = new Form
            {
                Text = "Choose dedicated-server installation",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(620, 330),
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog
            };
            var list = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            list.Items.AddRange(candidates);
            list.SelectedIndex = 0;
            var choose = new Button { Text = "Use selected", DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = "Browse instead", DialogResult = DialogResult.Cancel, AutoSize = true };
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(6) };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(choose);
            dialog.Controls.Add(list);
            dialog.Controls.Add(buttons);
            dialog.AcceptButton = choose;
            dialog.CancelButton = cancel;
            return dialog.ShowDialog(this) == DialogResult.OK && list.SelectedItem is string path ? path : null;
        }

        private void BrowseFile()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Select dedicated-server PalWorldSettings.ini",
                Filter = "PalWorldSettings.ini|PalWorldSettings.ini|INI files (*.ini)|*.ini",
                FileName = "PalWorldSettings.ini",
                CheckFileExists = true
            };

            string saved = GetSavedServerPath();

            if (File.Exists(saved))
                dialog.InitialDirectory = Path.GetDirectoryName(saved) ?? "";

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            if (!IsDedicatedServerSettingsFile(dialog.FileName))
            {
                MessageBox.Show(
                    this,
                    "This file was rejected.\n\nSelect the dedicated-server configuration at:\n" +
                    "Pal\\Saved\\Config\\WindowsServer\\PalWorldSettings.ini",
                    "Not a dedicated-server configuration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            selectedFile = dialog.FileName;
            filePath.Text = selectedFile;
            RememberServerPath(selectedFile);
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (!IsDedicatedServerSettingsFile(selectedFile))
            {
                status.Text = "Dedicated-server file not selected. Use Browse.";
                status.ForeColor = Color.DarkOrange;
                return;
            }

            try
            {
                string content = File.ReadAllText(selectedFile);
                int start = content.IndexOf("OptionSettings", StringComparison.OrdinalIgnoreCase);
                int equals = content.IndexOf('=', start);
                int openParen = equals >= 0 ? content.IndexOf('(', equals) : -1;
                int closeParen = FindMatchingParenthesis(content, openParen);

                if (start < 0 || openParen < 0 || closeParen < 0)
                    throw new Exception("Could not find a valid OptionSettings section.");

                string body = content.Substring(openParen + 1, closeParen - openParen - 1);

                settings.Clear();
                settingOrder.Clear();

                foreach (string item in SplitTopLevel(body))
                {
                    int split = item.IndexOf('=');
                    if (split <= 0)
                        continue;

                    string key = item.Substring(0, split).Trim();
                    string value = item.Substring(split + 1).Trim();

                    if (!settings.ContainsKey(key))
                        settingOrder.Add(key);

                    settings[key] = value;
                }

                foreach (SettingInfo info in definitions)
                {
                    if (!inputs.TryGetValue(info.Key, out Control? input) || input == null)
                        continue;

                    string value = settings.TryGetValue(info.Key, out string? loadedValue)
                        ? loadedValue ?? info.DefaultValue
                        : info.DefaultValue;
                    SetSettingInputValue(input, info, value);
                }

                filePath.Text = selectedFile;
                RememberServerPath(selectedFile);
                status.Text = "Dedicated-server settings loaded.";
                status.ForeColor = Color.DarkGreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Unable to load settings",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveSettings()
        {
            if (!IsDedicatedServerSettingsFile(selectedFile))
            {
                MessageBox.Show(this, "Select a valid dedicated-server settings file first.",
                    "No dedicated-server file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var editedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (SettingInfo info in definitions)
                {
                    if (!inputs.TryGetValue(info.Key, out Control? input) || input == null)
                        continue;
                    string[]? allowedChoices = input is ComboBox combo
                        ? combo.Items.Cast<object>().Select(x => x.ToString() ?? string.Empty).ToArray()
                        : null;
                    editedValues[info.Key] = ValidateAndFormatValue(info, input.Text, allowedChoices);
                }

                if (MessageBox.Show(this,
                    "Save changes to the dedicated-server configuration?\n\nStop the server before saving.",
                    "Confirm save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

                foreach (SettingInfo info in definitions)
                    if (settings.ContainsKey(info.Key)) settings[info.Key] = editedValues[info.Key];

                string original = File.ReadAllText(selectedFile);
                int start = original.IndexOf("OptionSettings", StringComparison.OrdinalIgnoreCase);
                int equals = original.IndexOf('=', start);
                int openParen = equals >= 0 ? original.IndexOf('(', equals) : -1;
                int closeParen = FindMatchingParenthesis(original, openParen);

                if (start < 0 || openParen < 0 || closeParen < 0)
                    throw new Exception("Could not find a valid OptionSettings section.");

                string body = string.Join(",",
                    settingOrder
                        .Where(key => settings.ContainsKey(key))
                        .Select(key => key + "=" + settings[key]));

                string updated = original.Substring(0, openParen + 1) +
                    body + original.Substring(closeParen);

                string temp = selectedFile + ".tmp_" + Guid.NewGuid().ToString("N");
                try
                {
                    File.WriteAllText(temp, updated, new UTF8Encoding(false));
                    File.Replace(temp, selectedFile, null);
                }
                finally
                {
                    if (File.Exists(temp)) File.Delete(temp);
                }

                status.Text = "Settings saved successfully.";
                status.ForeColor = Color.DarkGreen;

                MessageBox.Show(
                    this,
                    "Settings saved.\n\nCreate a backup separately with the Create Backup button if needed.\n\nRestart the dedicated server for applicable changes.",
                    "Save complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save settings:\n" + ex.Message,
                    "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateBackup()
        {
            if (!IsDedicatedServerSettingsFile(selectedFile))
            {
                MessageBox.Show(this, "Select a valid dedicated-server settings file first.",
                    "No dedicated-server file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string backupDirectory = Path.Combine(AppContext.BaseDirectory, BackupFolderName);
                Directory.CreateDirectory(backupDirectory);
                EnsureBackupReadme(backupDirectory);

                using var dialog = new SaveFileDialog
                {
                    Title = "Choose where to save the backup",
                    Filter = "INI backup (*.ini)|*.ini|All files (*.*)|*.*",
                    DefaultExt = "ini",
                    AddExtension = true,
                    CheckPathExists = true,
                    OverwritePrompt = true,
                    InitialDirectory = backupDirectory,
                    FileName = Path.GetFileNameWithoutExtension(selectedFile) + "_" +
                        DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".ini"
                };

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                string destination = Path.GetFullPath(dialog.FileName);
                if (string.Equals(destination, Path.GetFullPath(selectedFile), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Choose a different file path so the original settings file is not replaced.");

                File.Copy(selectedFile, destination, true);
                EnsureBackupReadme(Path.GetDirectoryName(destination)!);

                status.Text = "Backup created at the selected location.";
                status.ForeColor = Color.DarkGreen;
                MessageBox.Show(this, "Backup created:\n" + destination +
                    "\n\nSee README.md beside the backup for restore instructions.",
                    "Backup complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not create backup:\n" + ex.Message,
                    "Backup error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void EnsureBackupReadme(string directory)
        {
            string readmePath = Path.Combine(directory, "README.md");
            if (!File.Exists(readmePath))
                File.WriteAllText(readmePath, BackupReadme, new UTF8Encoding(false));
        }

        private static string ValidateAndFormatValue(SettingInfo info, string raw, string[]? allowedChoices = null)
        {
            string value = raw.Trim();
            if (info.DefaultValue.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                info.DefaultValue.Equals("False", StringComparison.OrdinalIgnoreCase))
            {
                if (!bool.TryParse(value, out bool boolean))
                    throw new InvalidDataException($"{info.Label} must be True or False.");
                return boolean ? "True" : "False";
            }

            if (double.TryParse(info.DefaultValue, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                if (!double.TryParse(value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double number) ||
                    double.IsNaN(number) || double.IsInfinity(number) || number < 0)
                    throw new InvalidDataException($"{info.Label} must be a non-negative number.");
                if (!info.DefaultValue.Contains('.') && number != Math.Truncate(number))
                    throw new InvalidDataException($"{info.Label} must be a whole number.");
                return value;
            }

            string[] choices = allowedChoices ?? GetChoiceValues(info);
            if (choices.Length > 0)
            {
                string? choice = choices.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
                if (choice == null)
                    throw new InvalidDataException($"Choose a valid option for {info.Label}.");
                return choice;
            }

            if (!IsQuotedStringSetting(info)) return value;
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private void ResetFields()
        {
            if (MessageBox.Show(
                this,
                "Restore the loaded values in the fields?\nThis will not save anything to the server.",
                "Reset fields",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            foreach (SettingInfo info in definitions)
            {
                if (!inputs.TryGetValue(info.Key, out Control? input) || input == null)
                    continue;

                string value = settings.TryGetValue(info.Key, out string? existing)
                    ? existing ?? info.DefaultValue
                    : info.DefaultValue;
                SetSettingInputValue(input, info, value);
            }
        }

        private static int FindMatchingParenthesis(string text, int openIndex)
        {
            if (openIndex < 0 || openIndex >= text.Length)
                return -1;

            int depth = 0;
            bool inString = false;
            bool escaped = false;

            for (int i = openIndex; i < text.Length; i++)
            {
                char c = text[i];

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\' && inString)
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString)
                    continue;

                if (c == '(')
                    depth++;

                if (c == ')')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }

            return -1;
        }

        private static List<string> SplitTopLevel(string text)
        {
            var result = new List<string>();
            var current = new StringBuilder();

            int depth = 0;
            bool inString = false;
            bool escaped = false;

            foreach (char c in text)
            {
                if (escaped)
                {
                    current.Append(c);
                    escaped = false;
                    continue;
                }

                if (c == '\\' && inString)
                {
                    current.Append(c);
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    current.Append(c);
                    continue;
                }

                if (!inString)
                {
                    if (c == '(')
                        depth++;
                    else if (c == ')')
                        depth--;
                    else if (c == ',' && depth == 0)
                    {
                        result.Add(current.ToString().Trim());
                        current.Clear();
                        continue;
                    }
                }

                current.Append(c);
            }

            if (current.Length > 0)
                result.Add(current.ToString().Trim());

            return result;
        }
    }
}
