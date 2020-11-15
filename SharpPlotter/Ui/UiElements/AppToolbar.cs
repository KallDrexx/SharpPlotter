using System;
using System.Collections.Generic;
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
        private readonly List<string> _recentlyOpenedFiles = new List<string>();
        private bool _scriptFileListLoaded, _recentFilesListLoaded;

        public Point2d? MousePointerGraphLocation { get; set; }
        
        public event EventHandler NewClicked;
        public event EventHandler<string> OpenClicked;
        public event EventHandler SettingsClicked;
        public event EventHandler UpdateCameraOriginRequested;
        public event EventHandler UpdateCameraBoundsRequested;
        public event EventHandler ResetCameraRequested;

        public Point2d CameraOrigin
        {
            get => Get<Point2d>();
            set => Set(value);
        }

        public Point2d CameraMinBounds
        {
            get => Get<Point2d>();
            set => Set(value);
        }

        public Point2d CameraMaxBounds
        {
            get => Get<Point2d>();
            set => Set(value);
        }

        public bool HideGridLines
        {
            get => Get<bool>();
            set => Set(value);
        }

        public AppToolbar(ScriptManager scriptManager, AppSettings appSettings)
        {
            _scriptManager = scriptManager;
            _appSettings = appSettings;
        }
        
        protected override void CustomRender()
        {
            if (ImGui.BeginMainMenuBar())
            {
                CreateFileMenu();
                CreateViewMenu();
                
                if (!string.IsNullOrWhiteSpace(_scriptManager.CurrentFileName) && _scriptManager.CurrentLanguage != null)
                {
                    var languageName = _scriptManager.CurrentLanguage.Value switch
                    {
                        ScriptLanguage.CSharp => "C#",
                        ScriptLanguage.Javascript => "Javascript",
                        ScriptLanguage.Python => "Python",
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

        private void CreateFileMenu()
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("New")) NewClicked?.Invoke(this, EventArgs.Empty);
                if (ImGui.BeginMenu("Open"))
                {
                    var selectedFileToOpen = (string) null;
                    var openFile = false;

                    if (!_recentFilesListLoaded)
                    {
                        var files = _appSettings.RecentlyOpenedFiles
                                        ?.Where(x => !x.Equals(_scriptManager.CurrentFileName,
                                            StringComparison.OrdinalIgnoreCase))
                                    ?? Array.Empty<string>();

                        _recentlyOpenedFiles.Clear();
                        _recentlyOpenedFiles.AddRange(files);
                        _recentFilesListLoaded = true;
                    }

                    var visibleRecentlyOpenedFiles = _appSettings.RecentlyOpenedFiles
                                                         ?.Where(x => !x.Equals(_scriptManager.CurrentFileName,
                                                             StringComparison.OrdinalIgnoreCase))
                                                     ?? Array.Empty<string>();

                    if (_recentlyOpenedFiles.Any())
                    {
                        foreach (var recentFile in visibleRecentlyOpenedFiles)
                        {
                            if (ImGui.MenuItem(recentFile))
                            {
                                // We can't trigger the open event here, due to being in hte middle of iteration
                                // of the recently opened file loop
                                openFile = true;
                                selectedFileToOpen = recentFile;
                            }
                        }
                    }
                    else
                    {
                        ImGui.MenuItem("<No Files Recently Opened>");
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
                            if (!Directory.Exists(_appSettings.ScriptFolderPath))
                            {
                                Directory.CreateDirectory(_appSettings.ScriptFolderPath);
                            }
                            
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
                else
                {
                    _recentFilesListLoaded = false;
                }

                ImGui.Separator();

                if (ImGui.MenuItem("Settings")) SettingsClicked?.Invoke(this, EventArgs.Empty);

                ImGui.EndMenu();
            }
        }

        private void CreateViewMenu()
        {
            if (ImGui.BeginMenu("View"))
            {
                if (ImGui.Button("Reset Camera"))
                {
                    ResetCameraRequested?.Invoke(this, EventArgs.Empty);
                }
                
                ImGui.NewLine();
                
                Checkbox(nameof(HideGridLines), "Hide Grid Lines");
                
                ImGui.NewLine();
                
                var originX = (int) CameraOrigin.X;
                var originY = (int) CameraOrigin.Y;
                var minX = (int) CameraMinBounds.X;
                var maxX = (int) CameraMaxBounds.X;
                var minY = (int) CameraMinBounds.Y;
                var maxY = (int) CameraMaxBounds.Y;

                ImGui.InputInt("Camera Origin X", ref originX); 
                ImGui.InputInt("Camera Origin Y", ref originY);
                
                if (ImGui.Button("Update Camera Origin"))
                {
                    UpdateCameraOriginRequested?.Invoke(this, EventArgs.Empty);
                }
                
                ImGui.NewLine();

                ImGui.InputInt("Camera Min X", ref minX);
                ImGui.InputInt("Camera Max X", ref maxX);
                ImGui.InputInt("Camera Min Y", ref minY);
                ImGui.InputInt("Camera Max Y", ref maxY);

                CameraOrigin = new Point2d(originX, originY);
                CameraMinBounds = new Point2d(minX, minY);
                CameraMaxBounds = new Point2d(maxX, maxY);
                
                ImGui.NewLine();
                if (ImGui.Button("Update Camera Bounds"))
                {
                    UpdateCameraBoundsRequested?.Invoke(this, EventArgs.Empty);
                }

                ImGui.Separator();

                if (ImGui.BeginMenu("Camera Controls"))
                {
                    ImGui.Text("Move Around: up, down, left, right, or click + drag with the mouse");
                    ImGui.Text("Zoom: page up, page down, or the mouse scroll wheel");
                    ImGui.Text("Horizontal field of view: insert or delete");
                    ImGui.Text("Vertical field of view: home or end");
                    ImGui.Text("Reset camera and field of view: backspace");
                    
                    ImGui.EndMenu();
                }

                ImGui.EndMenu();
            }
        }
    }
}