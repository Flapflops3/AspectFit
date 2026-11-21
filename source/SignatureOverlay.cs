using Godot;

namespace AspectWindow;

internal static class SignatureOverlay
{
    public static void Attach(SceneTree tree)
    {
        // Branding must never prevent the independent native sizing hook from working.
        try
        {
            using var stream = typeof(SignatureOverlay).Assembly
                .GetManifestResourceStream("AspectWindow.signature.png")
                ?? throw new InvalidOperationException("Missing signature resource.");
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            using var image = new Image();
            if (image.LoadPngFromBuffer(buffer.ToArray()) != Error.Ok)
                throw new InvalidOperationException("Invalid signature image.");

            var layer = new CanvasLayer { Name = "AspectFitSignature", Layer = 100,
                ProcessMode = Node.ProcessModeEnum.Always };
            var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
            var mark = new TextureRect
            {
                Name = "Signature", Texture = ImageTexture.CreateFromImage(image),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Stop,
                MouseDefaultCursorShape = Control.CursorShape.PointingHand,
                TooltipText = "随窗而变 · AspectFit",
                Modulate = new Color(1, 1, 1, 0.55f)
            };
            tree.Root.AddChild(layer);
            layer.AddChild(root);
            root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            root.AddChild(mark);
            mark.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
            mark.OffsetLeft = -136;
            mark.OffsetRight = -16;
            mark.OffsetTop = -96;
            mark.OffsetBottom = -16;
            mark.MouseEntered += () => mark.Modulate = Colors.White;
            mark.MouseExited += () => mark.Modulate = new Color(1, 1, 1, 0.55f);
            mark.GuiInput += input =>
            {
                if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
                {
                    mark.AcceptEvent();
                    mark.Hide();
                }
            };
            GD.Print("[AspectFit] Signature ready; hover for mod name, click to dismiss this session.");
        }
        catch (Exception error)
        {
            GD.PushWarning($"[AspectFit] Signature unavailable: {error.Message}");
        }
    }
}
