﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using JR.Utils.GUI.Forms;
using NO_INSTALL.Forms;

namespace NO_INSTALL
{
    public partial class mainWindow : Form
    {
        string appVersion = Regex.Replace(Application.ProductVersion, @"(\d+\.\d+\.\d+)\.\d+", @"$1");

        public mainWindow()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = $"NO INSTALL v{appVersion} (by wvzxn)";
            ControlCenter(textBoxDirectory);
            ControlCenter(textBoxRegex);
            comboBoxFile.Text = comboBoxFile.Items[0].ToString();
            comboBoxDir.Text = comboBoxDir.Items[0].ToString();
        }
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            ControlCenter(textBoxDirectory);
            ControlCenter(textBoxRegex);
        }
        private void panelRList_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 0)
            {
                if (panelRList.Sorting != SortOrder.Ascending) { panelRList.Sorting = SortOrder.Ascending; }
                else { panelRList.Sorting = SortOrder.Descending; }
                panelRList.Sort();
            }
        }
        private void buttonSelectDir_Click(object sender, EventArgs e)
        {
            using (var dir = new FolderBrowserDialog())
            {
                DialogResult result = dir.ShowDialog();
                if (result == DialogResult.OK) { textBoxDirectory.Text = dir.SelectedPath.ToString(); }
            }
        }
        private void buttonRegexSearch_Click(object sender, EventArgs e)
        {
            string dir = textBoxDirectory.Text;
            if (dir == "") { return; }

            panelRList.Items.Clear();
            var listDuplicates = new List<string>();
            string regexDefault = @"vst3|vstplugins|program(?: files(?:.?\(x86\))?|data)|common files|roaming|documents";
            string regex = textBoxRegex.Text;

            foreach (string dirPathFull in Directory.EnumerateDirectories(dir + @"\C", "*", SearchOption.AllDirectories))
            {
                string dirPath = dirPathFull.Substring(dir.Length + 1);
                string dirName = Regex.Replace(dirPath, @".+\\([^\\]+?)$", @"$1");

                if (regex == "")
                {
                    //  Auto-Search

                    //  Deny if not match default regex
                    if (!Regex.IsMatch(dirPath, $"(?:{regexDefault})" + @"\\([^\\]+?)$", RegexOptions.IgnoreCase)) { continue; }
                }
                else
                {
                    //  Manual regex Search

                    //  Add VST3\* or VstPlugins\* child files into ListView
                    if (Regex.IsMatch(dirName, @"vst3?", RegexOptions.IgnoreCase))
                    {
                        foreach (string i in Directory.EnumerateFiles(dirPathFull, "*"))
                        {
                            string iPath = i.Substring(dir.Length + 1);
                            string iName = Regex.Replace(iPath, @".+\\([^\\]+?)$", @"$1");

                            //  Deny if not match regex
                            if (!Regex.IsMatch(iName, regex, RegexOptions.IgnoreCase)) { continue; }

                            //  Add into ListView
                            if (listDuplicates.Contains(iPath)) { continue; }
                            listDuplicates.Add(iPath);
                            var ilvi = new ListViewItem(iPath);
                            ilvi.SubItems.Add(File.Exists(i) ? comboBoxFile.Text : comboBoxDir.Text);
                            panelRList.Items.Add(ilvi);
                        }
                    }

                    //  Deny if child item with same regex name
                    if (Regex.IsMatch(dirPath, $"({regex})" + @".*\\[^\\]*?" + $"({regex})", RegexOptions.IgnoreCase)) { continue; }

                    //  If folder name not match regex, check for child files
                    if (!Regex.IsMatch(dirName, regex, RegexOptions.IgnoreCase))
                    {
                        foreach (string i in Directory.EnumerateFiles(dirPathFull, "*"))
                        {
                            string iPath = i.Substring(dir.Length + 1);
                            string iName = Regex.Replace(iPath, @".+\\([^\\]+?)$", @"$1");

                            //  Deny if not match regex
                            if (!Regex.IsMatch(iName, regex, RegexOptions.IgnoreCase)) { continue; }

                            //  Add into ListView if not duplicate
                            if (listDuplicates.Contains(iPath)) { continue; }
                            listDuplicates.Add(iPath);
                            var ilvi = new ListViewItem(iPath);
                            ilvi.SubItems.Add(File.Exists(i) ? comboBoxFile.Text : comboBoxDir.Text);
                            panelRList.Items.Add(ilvi);
                        }

                        continue;
                    }
                }

                //  Add folder into ListView if not duplicate and default (env) folder
                if (listDuplicates.Contains(dirPath) || Regex.IsMatch(dirName, regexDefault, RegexOptions.IgnoreCase)) { continue; }
                listDuplicates.Add(dirPath);
                var lvi = new ListViewItem(dirPath);
                lvi.SubItems.Add(File.Exists(dirPathFull) ? comboBoxFile.Text : comboBoxDir.Text);
                lvi.Checked = !Regex.IsMatch(dirName, regexDefault, RegexOptions.IgnoreCase);
                panelRList.Items.Add(lvi);

                //  Directory check
                if (!Directory.Exists(dirPathFull)) { continue; }

                //  Child items check
                foreach (string i in Directory.EnumerateFileSystemEntries(dirPathFull, "*"))
                {
                    string iPath = i.Substring(dir.Length + 1);
                    string iName = Regex.Replace(iPath, @".+\\([^\\]+?)$", @"$1");

                    //  Deny if not match regex
                    if (!Regex.IsMatch(iName, regex, RegexOptions.IgnoreCase)) { continue; }

                    //  Add into ListView if not duplicate
                    if (listDuplicates.Contains(iPath)) { continue; }
                    listDuplicates.Add(iPath);
                    var ilvi = new ListViewItem(iPath);
                    ilvi.SubItems.Add(File.Exists(i) ? comboBoxFile.Text : comboBoxDir.Text);
                    panelRList.Items.Add(ilvi);
                }
            }
        }
        private void buttonRemove_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem i in panelRList.SelectedItems) { panelRList.Items.Remove(i); }
        }
        private void buttonAddFiles_Click(object sender, EventArgs e)
        {
            var dir = new OpenFileDialog
            {
                InitialDirectory = textBoxDirectory.Text,
                Multiselect = true
            };
            DialogResult result = dir.ShowDialog();
            if (result == DialogResult.OK)
            {
                foreach (string i in dir.FileNames)
                {
                    if (!i.Contains(textBoxDirectory.Text)) { MessageBox.Show($"The item must be inside the folder\n'{textBoxDirectory.Text}'", i); break; }
                    string j = i.Substring(textBoxDirectory.Text.Length + 1);
                    var lvi = new ListViewItem(j);
                    lvi.SubItems.Add(comboBoxFile.Text);
                    panelRList.Items.Add(lvi);
                }
            }
        }
        private void buttonAddFolder_Click(object sender, EventArgs e)
        {
            using (var dir = new FolderBrowserDialog())
            {
                DialogResult result = dir.ShowDialog();
                if (result == DialogResult.OK)
                {
                    string i = dir.SelectedPath.ToString();
                    if (!i.Contains(textBoxDirectory.Text)) { MessageBox.Show($"The item must be inside the folder\n'{textBoxDirectory.Text}'", i); return; }
                    i = i.Substring(textBoxDirectory.Text.Length + 1);
                    panelRList.Items.Add(i).SubItems.Add(comboBoxDir.Text);
                }
            }
        }
        private void buttonCreateInstaller_Click(object sender, EventArgs e)
        {
            string directory = textBoxDirectory.Text;
            if (directory == "") { return; }

            bool customEcho = menuAddonsCustomEcho.Checked;
            bool pwshReg = menuAddonsPwshReg.Checked;
            string reg = "1.reg";
            if (menuAddonsPlusDir.Checked) { CreatePlusFolder(directory); reg = @"+\" + reg; }
            if (!File.Exists(Path.Combine(directory, reg))) { reg = @""; }

            CreateCmd(directory + @"\SymLink Installer.cmd", GetCmdList(true, directory, reg, customEcho, pwshReg));
        }
        private void buttonCreateUninstaller_Click(object sender, EventArgs e)
        {
            string directory = textBoxDirectory.Text;
            if (directory == "") { return; }

            bool customEcho = menuAddonsCustomEcho.Checked;
            bool pwshReg = menuAddonsPwshReg.Checked;
            string reg = "2.reg";
            if (menuAddonsPlusDir.Checked) { CreatePlusFolder(directory); reg = @"+\" + reg; }
            if (!File.Exists(Path.Combine(directory, reg))) { reg = @""; }

            CreateCmd(textBoxDirectory.Text + @"\SymLink Uninstaller.cmd", GetCmdList(false, directory, reg, customEcho, pwshReg));
        }
        private void buttonCreate2Reg_Click(object sender, EventArgs e)
        {
            string dir = textBoxDirectory.Text;
            if (dir == "") { return; }

            if (!File.Exists(Path.Combine(dir, @"1.reg"))) { return; }
            CreateRegUninstall(Path.Combine(dir, @"1.reg"), Path.Combine(dir, @"2.reg"));
        }
        private void buttonSandboxie_Click(object sender, EventArgs e)
        {
            string currentDir = textBoxDirectory.Text;
            if (currentDir == "") { return; }
            if (Directory.Exists(Path.Combine(currentDir, @"C"))) { return; }

            string envSystemDrive = Environment.GetEnvironmentVariable("SYSTEMDRIVE") + @"\";
            string envUserName = Environment.GetEnvironmentVariable("USERNAME");
            string Sandboxie = Path.Combine(envSystemDrive, "Sandbox", envUserName, "DefaultBox");
            string SandboxieC = Path.Combine(Sandboxie, @"drive\C");
            string SandboxieData = Path.Combine(Sandboxie, @"user\all");
            string SandboxieUser = Path.Combine(Sandboxie, @"user\current");

            CopyDirectory(SandboxieC, Path.Combine(currentDir, @"C"), true);
            CopyDirectory(SandboxieData, Path.Combine(currentDir, @"C\ProgramData"), true);
            CopyDirectory(SandboxieUser, Path.Combine(currentDir, @"C\Users\(Name)"), true);
        }
        private void buttonRemoveJunk_Click(object sender, EventArgs e)
        {
            string currentDir = textBoxDirectory.Text;
            string regex = textBoxRegex.Text;
            if (currentDir == "") { return; }
            //if (regex == "") { return; }

            string regexJunk1 = @"(?:microsoft\\windows\\start menu\\(.+?$))";
            string regexJunk2 = @"(?:users\\.+?\\desktop\\(.+?$))";
            string regexJunk3 = @"(?:program(?: files(?: \(x86\))?|data))\\.+?\\(unins.*?\.|.+?\.ico)";
            string regexJunk = regexJunk1 + @"|" + regexJunk2 + @"|" + regexJunk3;

            Regex regexMatch = new Regex(regex, RegexOptions.IgnoreCase);
            Regex regexJunkMatch = new Regex(regexJunk, RegexOptions.IgnoreCase);

            var files = Directory.EnumerateFiles(Path.Combine(currentDir, @"C"), @"*", SearchOption.AllDirectories);
            //string str = "";
            foreach (string file in files)
            {
                string i = file.Substring(currentDir.Length + 1);
                if (regexJunkMatch.IsMatch(i) || !regexMatch.IsMatch(i)) { File.Delete(file); }
            }
            string startMenuLnkJunk = Path.Combine(currentDir, @"C\ProgramData\Microsoft\Windows\Start Menu");
            if (Directory.Exists(startMenuLnkJunk)) { Directory.Delete(startMenuLnkJunk, true); }
            DeleteEmptyDirs(currentDir, regex);
            //FlexibleMessageBox.Show(str);
        }

        
        // Addons menu
        private void menuAddonsCustomEcho_Click(object sender, EventArgs e)
        {
            menuAddonsCustomEcho.Checked = menuAddonsCustomEcho.Checked ? false : true;
        }
        private void menuAddonsPwshReg_Click(object sender, EventArgs e)
        {
            menuAddonsPwshReg.Checked = menuAddonsPwshReg.Checked ? false : true;
        }
        private void menuAddonsPlusDir_Click(object sender, EventArgs e)
        {
            menuAddonsPlusDir.Checked = menuAddonsPlusDir.Checked ? false : true;
        }
        private void menuAddonsHostsEdit_Click(object sender, EventArgs e)
        {
            string directory = textBoxDirectory.Text;
            if (directory == "") { return; }

            var text = new List<string>
            {
                $":::       Generated via NO INSTALL v{appVersion} | https://github.com/wvzxn/no_install",
                $":::       {DateTime.Now:yyyy/MM/dd HH:mm:ss}",
                @"::  HostsEdit (CMD)",
                @"::  ",
                @"::  [Author]",
                @"::    wvzxn | https://github.com/wvzxn/",
                @"::  ",
                @"::  [Description]",
                @"::    Edit strings in the Windows hosts file.",
                @"::    /add - Add ",
                @"::    /del - Remove",
                @"::    /q   - Disable output (optionally)",
                @"::  ",
                @"::  [Example]",
                "::    hostsEdit.cmd /add \"1.2.3.4 www.example.com\"",
                "::    hostsEdit.cmd \"/del\" /q \"1.2.3.4 www.example.com\" \"4.3.2.1 api.example.com\"",
                @"",
                @"@echo off",
                @"fsutil dirty query %SYSTEMDRIVE% >nul&if ERRORLEVEL 1 (echo Run as Administrator required&pause&exit)",
                @"setlocal EnableDelayedExpansion",
                @"",
                "set \"hosts=%WINDIR%\\System32\\drivers\\etc\\hosts\"",
                "set \"cmnd=$h;$b\"",
                "if \"%~1\"==\"/add\" (call :add) else (if \"%~1\"==\"/del\" (call :del) else (goto :info))",
                @"",
                ":: Edit blockLines [\"1a\" \"2b\" -> '1a','2b'] + Output current action",
                "for %%Q in (%*) do set \"q=%%~Q\"& if not \"!q:~0,1!\"==\"/\" (set \"lines=!lines!'!q!',\"& if not \"%~2\"==\"/q\" (echo !q! - %echo%))",
                "set \"blockLines=%lines:~0,-1%\"",
                @"",
                "powershell \"$h='%hosts%';$b=%blockLines%;%cmnd%\"",
                "if not \"%~2\"==\"/q\" (pause)",
                @"exit",
                @"",
                @":add",
                "set \"echo=Adding to the hosts file...\"",
                "set \"cmnd=$c=gc $h;$b|%%{if($c -notcontains $_){$c+=$_}};sc $h $c -for\"",
                @"exit /b",
                @"",
                @":del",
                "set \"echo=Removing from the hosts file...\"",
                "set \"cmnd=sc $h (gc $h|select-string -patt $b -notm) -for\"",
                @"exit /b",
                @"",
                @":info",
                "for /f \"usebackq delims=\" %%Q in (`findstr /b /c:\"::  \" \"%~f0\"`) do echo %%Q",
                @"pause",
                @"exit"
            };

            string hostsEditPath = Path.Combine(directory, "HostsEdit.cmd");
            if (File.Exists(hostsEditPath)) { File.Delete(hostsEditPath); }
            using (var file = new StreamWriter(hostsEditPath, false))
            {
                foreach (var line in text) { file.WriteLine(line); }
            }
        }
        private void menuAddonsLeftoversCmd_Click(object sender, EventArgs e)
        {
            string directory = textBoxDirectory.Text;
            if (directory == "") { return; }

            string parentPath;
            string mdPattern = @"^\%[^\%]+?\%(?:(?:\\.*)?\\(VST3|VstPlugins|Documents|Windows|system32))?$";
            var mdDuplicateList = new List<string>();
            var text = new List<string>
            {
                $"::        Generated via NO INSTALL v{appVersion} | https://github.com/wvzxn/no_install",
                $"::        {DateTime.Now:yyyy/MM/dd HH:mm:ss}",
                @"@echo off",
                @"fsutil dirty query %SYSTEMDRIVE% >nul&if ERRORLEVEL 1 (echo Run as Administrator required&pause&exit)",
                "cd /d \"%~dp0\""
            };

            foreach (ListViewItem listItem in panelRList.Items)
            {
                if (!listItem.Checked) { continue; }
                string itemPath = EditEnv(listItem.Text);
                bool isFile = File.Exists(Path.Combine(directory, listItem.Text));
                string line = isFile ? $"del /f /q \"{itemPath}\"" : $"rd \"{itemPath}\" 2>nul || rd /s /q \"{itemPath}\"";
                text.Add(line);

                parentPath = Path.GetDirectoryName(itemPath);

                if ((!Regex.IsMatch(parentPath, mdPattern, RegexOptions.IgnoreCase))
                && (!mdDuplicateList.Contains(parentPath)))
                {
                    mdDuplicateList.Add(parentPath);
                }
            }

            foreach (string i in mdDuplicateList) { text.Add($"rd \"{i}\""); }
            text.Add("if exist \"2.reg\" (regedit /s 2.reg)");
            text.Add("pause");
            string rlPath = Path.Combine(directory, "Remove Leftovers.cmd");
            if (File.Exists(rlPath)) { File.Delete(rlPath); }
            using (var file = new StreamWriter(rlPath, false))
            {
                foreach (var line in text) { file.WriteLine(line); }
            }
        }
        private void menuAddonsCollectVSTs_Click(object sender, EventArgs e)
        {
            string currentDir = textBoxDirectory.Text;
            if (currentDir == "") { return; }

            string folderName = "";
            using (var prompt = new userEnter()) { if (prompt.ShowDialog() == DialogResult.OK) { folderName = prompt.returnValue; } }
            if (folderName == "") { return; }

            var vstDirList = new List<string>
            {
                @"C\Program Files\Common Files\VST3",
                @"C\Program Files\VSTPlugins",
                @"C\Program Files (x86)\Common Files\VST3",
                @"C\Program Files (x86)\VSTPlugins"
            };

            foreach (string vstDir in vstDirList)
            {
                string vstPath = Path.Combine(currentDir, vstDir);
                string folderPath = Path.Combine(vstPath, folderName);
                if (!Directory.Exists(vstPath)) { continue; }
                Directory.CreateDirectory(folderPath);

                foreach (string itemPath in Directory.EnumerateFileSystemEntries(vstPath, @"*"))
                {
                    string itemName = Regex.Replace(itemPath, @"^.+\\([^\\]+?)$", @"$1");
                    string destination = Path.Combine(folderPath, itemName);

                    if (File.Exists(itemPath))
                    {
                        if (File.Exists(destination)) { continue; }
                        File.Copy(itemPath, destination);
                        File.Delete(itemPath);
                    }
                    else
                    {
                        if (Directory.Exists(destination) || itemName == folderName) { continue; }
                        CopyDirectory(itemPath, destination, true);
                        Directory.Delete(itemPath, true);
                    }
                }
            }
        }
        private void menuAbout_Click(object sender, EventArgs e)
        {
            var aboutForm = new about();
            aboutForm.ShowDialog();
        }


        // File creation, editing and deleting
        private void CreatePlusFolder(string Dir)
        {
            string plusDir = Path.Combine(Dir, @"+");
            if (!Directory.Exists(plusDir)) { Directory.CreateDirectory(plusDir); }
            var files = Directory.EnumerateFiles(Dir, "*", SearchOption.TopDirectoryOnly);
            foreach (string filePath in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string fileExt = Path.GetExtension(filePath);
                string fileNewPath = Path.Combine(plusDir, fileName + fileExt);
                if (File.Exists(fileNewPath)) { fileNewPath = Path.Combine(plusDir, fileName + @" (2)" + fileExt); }
                if (!Regex.IsMatch(fileName + fileExt, @"symlink", RegexOptions.IgnoreCase)) { File.Move(filePath, fileNewPath); }
            }
        }
        private void CreateBackupFile(string filePath)
        {
            string backupPath = filePath + @".BAK";

            if (File.Exists(filePath))
            {
                if (File.Exists(backupPath)) { File.Delete(backupPath); }
                File.Move(filePath, backupPath);
            }
        }
        private void CreateCmd(string filePath, List<string> commands)
        {
            string dirPath = Path.GetDirectoryName(filePath);
            string dirName = Path.GetFileName(dirPath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string dirNamePad = "", fileNamePad = "";
            for (int i = 0; i < ((60 - dirName.Length) / 2); i++) { dirNamePad += " "; }
            for (int i = 0; i < ((60 - fileName.Length) / 2); i++) { fileNamePad += " "; }

            var text = new List<string>
            {
                $"::        Generated via NO INSTALL v{appVersion} | https://github.com/wvzxn/no_install",
                $"::        {DateTime.Now:yyyy/MM/dd HH:mm:ss}",
                @"",
                @"@echo off",
                "cd /d \"%~dp0\"",
                @"fsutil dirty query %SYSTEMDRIVE% >nul&if ERRORLEVEL 1 (echo Run as Administrator required&pause&exit)",
                $"title {fileName}",
                @"echo ############################################################",
                $"echo {fileNamePad}{fileName}",
                @"echo ############################################################",
                $"echo {dirNamePad}{dirName}",
                @"echo ############################################################",
                @"pause & echo:",
                @""
            };
            text.AddRange(commands);
            text.Add(@"");
            text.Add(@"echo: & pause");

            CreateBackupFile(filePath);

            using (var file = new StreamWriter(filePath, false))
            {
                foreach (var line in text) { file.WriteLine(line); }
            }
        }
        private void CreateRegUninstall(string sourcePath, string destinationPath)
        {
            string[] lines = File.ReadAllLines(sourcePath);
            List<string> linesNew = new List<string>();
            bool multiLine = false;

            foreach (string line in lines)
            {
                //  If starts with "*"= or @=
                if (Regex.IsMatch(line, @"^(\'.*?(?<!\\)\'|@)=".Replace("'","\"")))
                {
                    //  When not end with " - multiLine starts
                    if (!Regex.IsMatch(line, @"(?<!\\)\'$".Replace("'", "\""))) { multiLine = true; }
                    continue;
                }

                if (multiLine)
                {
                    //  When ends with " - multiLine ends
                    if (Regex.IsMatch(line, @"(?<!\\)\'$".Replace("'", "\""))) { multiLine = false; }
                    continue;
                }

                if (line.StartsWith("[")) { linesNew.Add(line.Replace("[", "[-")); }
                if (line.StartsWith("windows registry editor", true, null) ||
                    line.StartsWith("regedit", true, null)) { linesNew.Add(line); linesNew.Add(""); }
                if (line.StartsWith(";")) { linesNew.Add(line); }
            }

            CreateBackupFile(destinationPath);

            using (var file = new StreamWriter(destinationPath, false))
            {
                foreach (var line in linesNew) { file.WriteLine(line); }
            }
        }
        private void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                //throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
                return;

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
        private void DeleteEmptyDirs(string dir, string regexExclude)
        {
            if (String.IsNullOrEmpty(dir))
            {
                throw new ArgumentException(
                    "Starting directory is a null reference or an empty string",
                    "dir");
            }

            try
            {
                foreach (var d in Directory.EnumerateDirectories(dir))
                {
                    string r = regexExclude;
                    DeleteEmptyDirs(d, r);
                }

                var entries = Directory.EnumerateFileSystemEntries(dir);


                if (!entries.Any())
                {
                    try
                    {
                        bool ex = Regex.IsMatch(new DirectoryInfo(dir).Name, regexExclude, RegexOptions.IgnoreCase);
                        //FlexibleMessageBox.Show($"Path: '{dir}'\nName: '{new DirectoryInfo(dir).Name}'\nMatches: " + ex.ToString());
                        if (ex) { return; }
                        Directory.Delete(dir);
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (DirectoryNotFoundException) { }
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        
        // String editing
        private string EditEnv(string var2edit) // Ex. '..\appdata\roaming' --> '%APPDATA%'
        {
            var replace1list = new List<string>
            {
                @"^.+?\\program files \(x86\)\\common files\\",
                @"^.+?\\program files \(x86\)\\",
                @"^.+?\\program files\\common files\\",
                @"^.+?\\program files\\",
                @"^.+?\\programdata\\",
                @"^.+?\\appdata\\local\\",
                @"^.+?\\appdata\\roaming\\",
                @"^.+?\\users\\\(name\)\\",
                @"^.+?\\users\\public\\",
                @"^c\\"
            };
            var replace2list = new List<string>
            {
                @"%COMMONPROGRAMFILES(x86)%",
                @"%PROGRAMFILES(x86)%",
                @"%COMMONPROGRAMFILES%",
                @"%PROGRAMFILES%",
                @"%PROGRAMDATA%",
                @"%LOCALAPPDATA%",
                @"%APPDATA%",
                @"%USERPROFILE%",
                @"%PUBLIC%",
                @"%SYSTEMDRIVE%"
            };
            for (int i = 0; i < replace1list.Count; i++)
            {
                var2edit = Regex.Replace(var2edit, replace1list[i], replace2list[i] + @"\", RegexOptions.IgnoreCase);
            }
            return var2edit;
        }
        private List<string> GetCmdList(bool installer, string dir, string reg, bool customEcho, bool pwshReg) // mklink, md, regedit
        {
            var mklinkList = new List<string>();
            var mdListPaths = new List<string>();
            var mdList = new List<string>();
            var cmdList = new List<string>();
            var pwshRegFix = new List<string>
            {
                ":: Replace old user Drive letter + Name in 1.reg",
                "powershell \"(gc -LiteralPath '%~dp0+\\1.reg') -replace '(.:)(\\\\\\\\Users\\\\\\\\)[^\\\\]+?\\\\\\\\','%SYSTEMDRIVE%${2}%USERNAME%\\\\'|sc -LiteralPath '%~dp0+\\1.reg'\""
            };

            foreach (ListViewItem listItem in panelRList.Items)
            {
                if (!listItem.Checked) { continue; }

                string mdPattern = @"^\%[^\%]+?\%(?:(?:\\.*)?\\(Documents|Windows|system32))?$";
                string itemPath = EditEnv(listItem.Text);
                string parentPath = Path.GetDirectoryName(itemPath);
                string par = listItem.SubItems[1].Text == "(default)" ? "" : $"{listItem.SubItems[1].Text} ";
                if (!installer) { par = File.Exists(dir + "\\" + listItem.Text) ? "del /f /q" : "rd"; }

                //  md / rd
                if ((!Regex.IsMatch(parentPath, mdPattern, RegexOptions.IgnoreCase))
                && (!mdListPaths.Contains(parentPath)))
                {
                    mdListPaths.Add(parentPath);
                    string md = installer ?
                        $"md \"{parentPath}\"" :
                        $"rd \"{parentPath}\"";
                    if (customEcho) { md += installer ? $" && echo Folder created: {parentPath}" : $" && echo Deleted: {parentPath}"; }
                    mdList.Add(md);
                }

                //  mklink / del(rd)
                string mklinkLine = installer ?
                    $"mklink {par}\"{itemPath}\" \"%~dp0{listItem.Text}\"" : 
                    $"{par} \"{itemPath}\"";
                if (customEcho) { mklinkLine += installer ? $" >nul && echo SymLinked: {itemPath}" : $" && echo Deleted: {itemPath}"; }
                mklinkList.Add(mklinkLine);
            }

            //  md + mklink
            cmdList.AddRange(installer ? mdList : mklinkList);
            if (mdList.Count != 0) { cmdList.Add(""); }
            cmdList.AddRange(installer ? mklinkList : mdList);

            //  +reg
            if (reg != "")
            {
                cmdList.Add(@"");
                if (installer && pwshReg) { cmdList.AddRange(pwshRegFix); }
                string regLine = $"regedit /s {reg}";
                if (customEcho) { regLine += $" && echo Registry Edit: .\\{reg}"; }
                cmdList.Add(regLine);
            }

            return cmdList;
        }

        
        // Misc
        private void ControlCenter(Control item) // Snap a control to the center of parent item
        {
            int pad = ((item.Parent.Height / 2) - item.Height) / 2;
            item.Margin = new Padding(item.Margin.Left, pad, item.Margin.Right, pad);
        }

    }

    class Background
    {
        public static void Test(string[] arguments)
        {
            FlexibleMessageBox.Show(arguments[0]);
        }
    }
}
