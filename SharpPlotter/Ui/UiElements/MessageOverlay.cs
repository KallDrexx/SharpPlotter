using System;
using System.Linq;
using System.Numerics;
using ImGuiHandler;
using ImGuiNET;

namespace SharpPlotter.Ui.UiElements
{
    public class MessageOverlay : ImGuiElement
    {
        private readonly OnScreenLogger _onScreenLogger;
        private Vector2? _center, _pivot;

        public event EventHandler DismissMostRecentMessageClicked;

        public MessageOverlay(OnScreenLogger onScreenLogger)
        {
            _onScreenLogger = onScreenLogger;
        }

        protected override void CustomRender()
        {
            var message = _onScreenLogger.GetLatestMessage();
            if (message == null)
            {
                return;
            }
            
            // Can't get center position before first frame, but we don't want to calculate it every frame.
            if (_center == null || _pivot == null)
            {
                _center = new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f);
                _pivot = new Vector2(0.5f, 0.5f);
            }

            ImGui.SetNextWindowPos(_center.Value, ImGuiCond.Always, _pivot.Value);
            const ImGuiWindowFlags flags = ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.AlwaysAutoResize |
                                           ImGuiWindowFlags.NoSavedSettings |
                                           ImGuiWindowFlags.NoFocusOnAppearing |
                                           ImGuiWindowFlags.NoMove;
            
            if (ImGui.Begin("Message", flags))
            {
                ImGui.Text(message);
                ImGui.NewLine();
                if (ImGui.Button("Dismiss"))
                {
                    DismissMostRecentMessageClicked?.Invoke(this, EventArgs.Empty);
                }
            }
            
            ImGui.End();
        }
    }
}