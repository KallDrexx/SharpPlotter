using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ImGuiHandler;
using ImGuiNET;
using SharpPlotter.Scripting;

namespace SharpPlotter.Ui.UiElements
{
    public class AppToolbar : ImGuiElement
    {
        private readonly ScriptManager _scriptManager;
        private readonly AppSettings _appSettings;
        private readonly List<string> _filesInScriptDirectory = new List<string>();
        private bool _scriptFileListLoaded;

        public Point2d? MousePointerGraphLocation { get; set; }
        
        public event EventHandler NewClicked;
        public event EventHandler<string> OpenClicked;
        public event EventHandler SettingsClicked;

        public AppToolbar(ScriptManager scriptManager, AppSettings appSettings)
        {
            _scriptManager = scriptManager;
            _appSettings = appSettings;
        }
        
        protected override void CustomRender()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("New")) NewClicked?.Invoke(this, EventArgs.Empty);
                    if (ImGui.BeginMenu("Open"))
                    {
                        var selectedFileToOpen = (string) null;
                        var openFile = false;
                        var recentCount = 0;
                        foreach (var recentlyOpenedFile in _appSettings.RecentlyOpenedFiles)
                        {
                            if (!recentlyOpenedFile.Equals(_scriptManager.CurrentFileName, StringComparison.Ordinal))
                            {
                                if (ImGui.MenuItem(recentlyOpenedFile))
                                {
                                    // We can't trigger the open event here, due to being in hte middle of iteration
                                    // of the recently opened file loop
                                    openFile = true;
                                    selectedFileToOpen = recentlyOpenedFile;
                                }
                                
                                recentCount++;
                            }
                        }

                        if (openFile)
                        {
                            OpenClicked?.Invoke(this, selectedFileToOpen);
                        }
                        
                        ImGui.Separator();

                        if (ImGui.BeginMenu("Open Other File"))
                        {
                            if (!_scriptFileListLoaded)
                            {
                                var files = Directory.GetFiles(_appSettings.ScriptFolderPath)
                                    .Select(x => x.Replace(_appSettings.ScriptFolderPath, "", StringComparison.OrdinalIgnoreCase))
                                    .Select(x => x.StartsWith("\\") || x.StartsWith("/") ? x.Substring(1) : x)
                                    .OrderBy(x => x);
                                
                                _filesInScriptDirectory.AddRange(files);
                                _scriptFileListLoaded = true;
                            }

                            foreach (var file in _filesInScriptDirectory)
                            {
                                if (ImGui.MenuItem(file))
                                {
                                    OpenClicked?.Invoke(this, file);
                                }
                            }

                            ImGui.EndMenu();
                        }
                        else
                        {
                            _scriptFileListLoaded = false;
                            _filesInScriptDirectory.Clear();
                        }
                        
                        ImGui.EndMenu();
                    }
                    
                    ImGui.Separator();
                    
                    if (ImGui.MenuItem("Settings")) SettingsClicked?.Invoke(this, EventArgs.Empty);

                    ImGui.EndMenu();
                }
                
                if (!string.IsNullOrWhiteSpace(_scriptManager.CurrentFileName) && _scriptManager.CurrentLanguage != null)
                {
                    var languageName = _scriptManager.CurrentLanguage.Value switch
                    {
                        ScriptLanguage.CSharp => "C#",
                        _ => ""
                    };
                    
                    ImGui.Text($"- {_scriptManager.CurrentFileName} ({languageName})");
                }
                
                if (MousePointerGraphLocation != null)
                {
                    ImGui.SameLine(ImGui.GetWindowWidth() - 150);
                    ImGui.Text($"({MousePointerGraphLocation.Value.X:F1},{MousePointerGraphLocation.Value.Y:F1})");
                }

                ImGui.EndMainMenuBar();
            }
        }
    }
}