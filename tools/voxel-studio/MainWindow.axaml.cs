using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace VoxelStudio;

public partial class MainWindow : Window
{
    // Fallback embedded model used only if no .vox files are found anywhere
    const string DemoVox = """
# Acropolis demo model
material sandstone  top=#FFF5D1  front=#9F8552  side=#7E6740
material marble     top=#F0EFF5  front=#8C8C9E  side=#6B6B7B
material gold       top=#F2C738  front=#9F731A  side=#7A5612

box 0 0 0   4 0 4   sandstone
box 1 1 1   3 1 3   sandstone
box 0 2 0   0 4 0   marble
box 4 2 0   4 4 0   marble
box 0 2 4   0 4 4   marble
box 4 2 4   4 4 4   marble
box 2 2 0   2 4 0   marble
box 2 2 4   2 4 4   marble
box 0 2 2   0 4 2   marble
box 4 2 2   4 4 2   marble
box 0 5 0   4 5 4   sandstone
box 1 6 1   3 6 3   sandstone
v 2 7 2   gold
""";

    const int StudioW = 600;
    const int StudioH = 600;
    const int PxlW = 24;
    const int PxlH = 24;
    const int PxlPxPerVoxel = 2;

    // LED-style preview: every 24×24 pixel becomes one LED cell with a gap +
    // 3D shading (top-left highlight, bottom-right shadow). Mimics the look
    // of a real RGB LED matrix.
    const int LedCell = 14;                  // px per cell (incl. gap)
    const int LedGap = 1;                    // px gap on each side of the cell
    const int LedW = PxlW * LedCell;         // 336
    const int LedH = PxlH * LedCell;         // 336

    // Sky + water gradient palette (matches PalmIsland15_OrganicCove)
    const int HorizonY = 8;
    static readonly uint cSkyTop      = MakeArgb(0.62, 0.88, 1.00);
    static readonly uint cSkyHorizon  = MakeArgb(0.85, 0.95, 1.00);
    static readonly uint cWaterBright = MakeArgb(0.30, 0.78, 0.74);
    static readonly uint cWaterDeep   = MakeArgb(0.10, 0.50, 0.55);

    static uint MakeArgb(double r, double g, double b)
        => 0xFF000000u
         | ((uint)Math.Clamp(r * 255.0, 0, 255) << 16)
         | ((uint)Math.Clamp(g * 255.0, 0, 255) << 8)
         |  (uint)Math.Clamp(b * 255.0, 0, 255);

    readonly WriteableBitmap _studioBitmap;
    readonly WriteableBitmap _pxlBitmap;
    readonly WriteableBitmap _ledBitmap;
    readonly uint[] _studioPixels = new uint[StudioW * StudioH];
    readonly uint[] _pxlPixels = new uint[PxlW * PxlH];
    readonly uint[] _ledPixels = new uint[LedW * LedH];

    VoxModel? _model;
    DateTime _startTime = DateTime.UtcNow;
    DispatcherTimer? _ticker;

    string? _modelsDir;
    string? _currentFilePath;
    bool _suppressTextChanged;

    // Hot-reload: watch the currently loaded file for external edits
    FileSystemWatcher? _fileWatcher;
    DateTime _lastReloadAt = DateTime.MinValue;

    // Mouse-drag orbit state
    bool _isDragging;
    double _dragStartX;
    double _dragStartY;
    double _dragStartYaw;
    double _dragStartPitch;
    double _dragStartSunYaw;
    double _dragStartSunPitch;
    const double YawSensitivity = 0.6;    // degrees per pixel horizontal
    const double PitchSensitivity = 0.4;  // degrees per pixel vertical

    // Middle-mouse 2D screen-space pan state (in studio pixels)
    bool _isPanning;
    double _panDragStartX;
    double _panDragStartY;
    double _panOriginX;
    double _panOriginY;
    double _panX;          // accumulated pan offset (studio-pixel units)
    double _panY;

    // Sun position in WORLD space (azimuth + elevation in degrees).
    // Updated by drag with Alt-modifier or by switching presets.
    // Defaults match the user's preferred initial view.
    double _sunYawDeg = 250;
    double _sunPitchDeg = 32;

    // Currently selected lighting preset + its toggle button (radio-style).
    // Default: "Studio R" (index 1).
    LightingPreset? _currentLightPreset = LightingPresets.All[1];
    readonly List<ToggleButton> _lightButtons = new();

    // Per-material set of disabled slot names ("top" / "side" / "front").
    // Disabled slots inherit their color from the first enabled slot in the
    // priority order [top, side, front]. At least one slot must stay enabled.
    readonly Dictionary<string, HashSet<string>> _disabledSlots = new();

    // Pxl-preview background mode (clock-style layout). Default = Water only.
    enum BgMode { Both, SkyOnly, WaterOnly }
    BgMode _bgMode = BgMode.WaterOnly;

    // Currently open color-picker popup (so we can close it before opening a new one)
    Window? _activeColorWindow;

    public MainWindow()
    {
        InitializeComponent();

        _studioBitmap = new WriteableBitmap(
            new PixelSize(StudioW, StudioH),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        _pxlBitmap = new WriteableBitmap(
            new PixelSize(PxlW, PxlH),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        _ledBitmap = new WriteableBitmap(
            new PixelSize(LedW, LedH),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        StudioImage.Source = _studioBitmap;
        PxlImage1x.Source = _pxlBitmap;
        PxlImage10x.Source = _ledBitmap;  // big preview is now LED-style

        // Wire up UI events
        AngleSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty) RenderFrame();
        };
        PitchSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty) RenderFrame();
        };
        ZoomSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty) RenderFrame();
        };
        BuildLightPresetButtons();
        // Background-mode toggles (radio behaviour: exactly one checked)
        BgBothToggle.Click  += (_, _) => SetBgMode(BgMode.Both);
        BgSkyToggle.Click   += (_, _) => SetBgMode(BgMode.SkyOnly);
        BgWaterToggle.Click += (_, _) => SetBgMode(BgMode.WaterOnly);
        SetBgMode(_bgMode);
        // Note: do NOT DeriveSunFromPreset on initial load — keep the
        // user-preferred default sun position (250°/32°).
        AmbientSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty) RenderFrame();
        };
        SunSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty) RenderFrame();
        };
        PanSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty) RenderFrame();
        };
        AutoRotateToggle.IsCheckedChanged += (_, _) => RenderFrame();

        // Mouse-drag orbit on the studio panel — left button only.
        // Horizontal drag = yaw (rotation around world Y axis).
        // Vertical drag   = pitch (camera tilts up/down) but clamped so the
        // up vector never flips → model never goes "schief".
        StudioPanel.PointerPressed += (_, e) =>
        {
            var pt = e.GetCurrentPoint(StudioPanel);
            // Middle button = free 2D pan (screen-space, both axes)
            if (pt.Properties.IsMiddleButtonPressed)
            {
                _isPanning = true;
                _panDragStartX = pt.Position.X;
                _panDragStartY = pt.Position.Y;
                _panOriginX = _panX;
                _panOriginY = _panY;
                e.Pointer.Capture(StudioPanel);
                return;
            }
            if (!pt.Properties.IsLeftButtonPressed) return;
            if (AutoRotateToggle.IsChecked == true) AutoRotateToggle.IsChecked = false;
            _isDragging = true;
            _dragStartX = pt.Position.X;
            _dragStartY = pt.Position.Y;
            _dragStartYaw = AngleSlider.Value;
            _dragStartPitch = PitchSlider.Value;
            _dragStartSunYaw = _sunYawDeg;
            _dragStartSunPitch = _sunPitchDeg;
            e.Pointer.Capture(StudioPanel);
        };
        StudioPanel.PointerMoved += (_, e) =>
        {
            var pt = e.GetCurrentPoint(StudioPanel);

            if (_isPanning)
            {
                // Pan delta in panel-pixel space. Scale by the studio
                // bitmap-to-panel ratio so the model tracks the cursor 1:1
                // even when the panel is bigger than 600px.
                var panelW = Math.Max(1.0, StudioPanel.Bounds.Width);
                var panelH = Math.Max(1.0, StudioPanel.Bounds.Height);
                var ratio = Math.Min(StudioW / panelW, StudioH / panelH);
                var dxp = (pt.Position.X - _panDragStartX) * ratio;
                var dyp = (pt.Position.Y - _panDragStartY) * ratio;
                _panX = _panOriginX + dxp;
                _panY = _panOriginY + dyp;
                RenderFrame();
                return;
            }

            if (!_isDragging) return;
            var dx = pt.Position.X - _dragStartX;
            var dy = pt.Position.Y - _dragStartY;

            var mods = e.KeyModifiers;
            bool altDown  = (mods & Avalonia.Input.KeyModifiers.Alt)  != 0;
            bool metaDown = (mods & Avalonia.Input.KeyModifiers.Meta) != 0;

            bool moveCamera = !altDown || metaDown;       // none → both; cmd → only camera
            bool moveSun    = !metaDown || altDown;       // none → both; alt → only sun
            // (alt && !meta) → moveCamera = false, moveSun = true
            // (meta && !alt) → moveCamera = true,  moveSun = false
            // (none)         → moveCamera = true,  moveSun = true
            // (alt && meta)  → moveCamera = true,  moveSun = true (both, same as none)

            if (moveCamera)
            {
                var newYaw   = ((_dragStartYaw   + dx * YawSensitivity)   % 360.0 + 360.0) % 360.0;
                var newPitch = ((_dragStartPitch - dy * PitchSensitivity) % 360.0 + 360.0) % 360.0;
                AngleSlider.Value = newYaw;
                PitchSlider.Value = newPitch;
            }
            if (moveSun)
            {
                _sunYawDeg   = ((_dragStartSunYaw   + dx * YawSensitivity)   % 360.0 + 360.0) % 360.0;
                _sunPitchDeg = ((_dragStartSunPitch - dy * PitchSensitivity) % 360.0 + 360.0) % 360.0;
                RenderFrame();
            }
        };
        StudioPanel.PointerReleased += (_, e) =>
        {
            if (_isPanning)
            {
                _isPanning = false;
                e.Pointer.Capture(null);
                return;
            }
            if (!_isDragging) return;
            _isDragging = false;
            e.Pointer.Capture(null);
        };
        StudioPanel.PointerCaptureLost += (_, _) =>
        {
            _isDragging = false;
            _isPanning = false;
        };

        // Mouse wheel on the studio panel = zoom (px/voxel)
        StudioPanel.PointerWheelChanged += (_, e) =>
        {
            var step = e.Delta.Y > 0 ? 2 : -2;
            var newZoom = ZoomSlider.Value + step;
            if (newZoom < ZoomSlider.Minimum) newZoom = ZoomSlider.Minimum;
            if (newZoom > ZoomSlider.Maximum) newZoom = ZoomSlider.Maximum;
            ZoomSlider.Value = newZoom;
            e.Handled = true;
        };

        OpenButton.Click += async (_, _) => await OpenFileDialog();
        SaveButton.Click += async (_, _) => await SaveFileDialog();
        ReloadButton.Click += (_, _) => RescanModelsFolder();
        FileSelector.SelectionChanged += async (_, _) => await OnFileSelected();
        VoxEditor.TextChanged += (_, _) =>
        {
            if (_suppressTextChanged) return;
            ReparseModel();
        };

        // Locate models folder + populate file list. Auto-loads first file.
        _modelsDir = FindModelsDirectory();
        RescanModelsFolder();

        // If no files found, fall back to embedded demo
        if (FileSelector.ItemCount == 0)
        {
            SetEditorText(DemoVox);
            ReparseModel();
        }

        _ticker = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _ticker.Tick += (_, _) => RenderFrame();
        _ticker.Start();

        Closing += (_, _) => StopWatchingFile();
    }

    // ========================================================================
    // Models folder discovery
    // ========================================================================

    // Looks for an explicit `voxstudio.config` (one path per non-blank, non-#
    // line) in the working dir or next to the executable. Path can be absolute
    // or relative to the config file. If no config, walks up from cwd/exe to
    // find the first folder named "Models".
    static string? FindModelsDirectory()
    {
        var dirs = CandidateConfigDirs().ToList();

        foreach (var dir in dirs)
        {
            var configPath = Path.Combine(dir, "voxstudio.config");
            if (!File.Exists(configPath)) continue;
            try
            {
                foreach (var raw in File.ReadAllLines(configPath))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    var path = Path.IsPathRooted(line) ? line : Path.GetFullPath(Path.Combine(dir, line));
                    if (Directory.Exists(path)) return path;
                }
            }
            catch { /* ignore broken config */ }
        }

        // Auto-detect: walk up looking for "Models"
        foreach (var startDir in dirs)
        {
            var d = startDir;
            for (var i = 0; i < 6 && d != null; i++)
            {
                var candidate = Path.Combine(d, "Models");
                if (Directory.Exists(candidate)) return candidate;
                d = Path.GetDirectoryName(d);
            }
        }
        return null;
    }

    static IEnumerable<string> CandidateConfigDirs()
    {
        var cwd = Directory.GetCurrentDirectory();
        yield return cwd;
        var exe = Assembly.GetExecutingAssembly().Location;
        var exeDir = Path.GetDirectoryName(exe);
        if (!string.IsNullOrEmpty(exeDir) && !string.Equals(exeDir, cwd, StringComparison.Ordinal))
            yield return exeDir;
    }

    void BuildLightPresetButtons()
    {
        for (var i = 0; i < LightingPresets.All.Length; i++)
        {
            var preset = LightingPresets.All[i];
            var btn = new ToggleButton
            {
                Content = preset.Name,
                Margin = new Thickness(0, 0, 4, 4),
                Padding = new Thickness(8, 3),
                IsChecked = preset == _currentLightPreset,
                Tag = preset,
            };
            btn.Click += (_, _) =>
            {
                // Radio-button behaviour: enforce exactly one checked
                foreach (var other in _lightButtons)
                    other.IsChecked = other == btn;
                _currentLightPreset = preset;
                AmbientSlider.Value = preset.Ambient * 100.0;
                DeriveSunFromPreset(preset);
                RenderFrame();
            };
            _lightButtons.Add(btn);
            LightButtonPanel.Children.Add(btn);
        }
    }

    void RescanModelsFolder()
    {
        var previousPath = _currentFilePath;
        var entries = new List<VoxFileEntry>();
        if (_modelsDir != null && Directory.Exists(_modelsDir))
        {
            try
            {
                var files = Directory.EnumerateFiles(_modelsDir, "*.vox", SearchOption.AllDirectories)
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
                foreach (var f in files)
                {
                    entries.Add(new VoxFileEntry
                    {
                        FullPath = f,
                        DisplayName = Path.GetRelativePath(_modelsDir, f),
                    });
                }
            }
            catch (Exception ex)
            {
                ParseStatus.Text = $"Folder scan failed: {ex.Message}";
                ParseStatus.Foreground = Avalonia.Media.Brushes.OrangeRed;
            }
        }
        FileSelector.ItemsSource = entries;

        if (entries.Count == 0)
        {
            // No files — let caller decide what to do (fallback demo etc.)
            return;
        }

        // Re-select the previously loaded file if it survived the rescan;
        // otherwise prefer a "default" file (palm-island-15.vox) on first
        // load, falling back to the first entry alphabetically.
        int idx;
        if (previousPath != null)
        {
            idx = entries.FindIndex(e => string.Equals(e.FullPath, previousPath, StringComparison.Ordinal));
            if (idx < 0) idx = 0;
        }
        else
        {
            idx = entries.FindIndex(e => e.DisplayName.EndsWith("palm-island-15.vox", StringComparison.OrdinalIgnoreCase));
            if (idx < 0) idx = 0;
        }
        FileSelector.SelectedIndex = idx;
    }

    async Task OnFileSelected()
    {
        if (FileSelector.SelectedItem is not VoxFileEntry entry) return;
        try
        {
            var text = await File.ReadAllTextAsync(entry.FullPath);
            SetEditorText(text);
            _currentFilePath = entry.FullPath;
            Title = $"Voxel Studio — {entry.DisplayName}";
            StartWatchingFile(entry.FullPath);
            ReparseModel();
        }
        catch (Exception ex)
        {
            ParseStatus.Text = $"Failed to load {entry.DisplayName}: {ex.Message}";
            ParseStatus.Foreground = Avalonia.Media.Brushes.OrangeRed;
        }
    }

    // Hot-reload: watch the currently loaded file for external writes (e.g. you
    // edit it in another editor) and reload the editor + re-parse on change.
    void StartWatchingFile(string path)
    {
        StopWatchingFile();
        var dir = Path.GetDirectoryName(path);
        var name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(name)) return;
        try
        {
            _fileWatcher = new FileSystemWatcher(dir, name)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                EnableRaisingEvents = true,
            };
            _fileWatcher.Changed += OnWatchedFileChanged;
            _fileWatcher.Created += OnWatchedFileChanged;
            _fileWatcher.Renamed += OnWatchedFileChanged;
        }
        catch { /* watcher setup failed (e.g. invalid path) — silently skip */ }
    }

    void StopWatchingFile()
    {
        if (_fileWatcher == null) return;
        try
        {
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Dispose();
        }
        catch { }
        _fileWatcher = null;
    }

    void OnWatchedFileChanged(object? sender, FileSystemEventArgs e)
    {
        // Debounce — editors often emit several Changed events for one save
        var now = DateTime.UtcNow;
        if ((now - _lastReloadAt).TotalMilliseconds < 250) return;
        _lastReloadAt = now;

        // Hop back to UI thread; small delay so the writer has time to flush+release the file
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await Task.Delay(80);
            if (_currentFilePath == null) return;
            try
            {
                var text = await File.ReadAllTextAsync(_currentFilePath);
                if (text == VoxEditor.Text) return;
                SetEditorText(text);
                ReparseModel();
            }
            catch
            {
                // File may still be locked by the writer — next event will retry
            }
        });
    }

    void SetEditorText(string text)
    {
        _suppressTextChanged = true;
        try { VoxEditor.Text = text; }
        finally { _suppressTextChanged = false; }
    }

    // ========================================================================
    // Parsing + rendering
    // ========================================================================

    void ReparseModel()
    {
        try
        {
            _model = VoxFormat.Parse(VoxEditor.Text ?? "");
            ParseStatus.Text = $"Parsed {_model.Voxels.Count} voxels, {_model.Materials.Count} materials";
            ParseStatus.Foreground = Avalonia.Media.Brushes.LightGreen;
        }
        catch (VoxParseException ex)
        {
            ParseStatus.Text = $"Parse error: {ex.Message}";
            ParseStatus.Foreground = Avalonia.Media.Brushes.OrangeRed;
        }
        catch (Exception ex)
        {
            ParseStatus.Text = $"Error: {ex.Message}";
            ParseStatus.Foreground = Avalonia.Media.Brushes.OrangeRed;
        }
        RebuildMaterialsPanel();
        RenderFrame();
    }

    bool _suppressMaterialsPanelRebuild;

    void RebuildMaterialsPanel()
    {
        if (_suppressMaterialsPanelRebuild) return;
        MaterialsPanel.Children.Clear();
        if (_model == null) return;
        foreach (var mat in _model.Materials)
        {
            var name = mat.Name;
            var row = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 4,
            };
            row.Children.Add(new TextBlock
            {
                Text = name,
                Foreground = Avalonia.Media.Brushes.LightGray,
                Width = 78,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontFamily = new Avalonia.Media.FontFamily("Menlo,Consolas,monospace"),
                FontSize = 11,
            });
            // Each slot is independently toggleable via right-click.
            // At least one must remain enabled (enforced in ToggleSlotEnabled).
            row.Children.Add(MakeColorSwatch(name, "top",   mat.Top,   IsSlotEnabled(name, "top")));
            row.Children.Add(MakeColorSwatch(name, "side",  mat.Side,  IsSlotEnabled(name, "side")));
            row.Children.Add(MakeColorSwatch(name, "front", mat.Front, IsSlotEnabled(name, "front")));
            MaterialsPanel.Children.Add(row);
        }
    }

    bool IsSlotEnabled(string mat, string slot) =>
        !_disabledSlots.TryGetValue(mat, out var set) || !set.Contains(slot);

    int CountEnabledSlots(string mat) =>
        3 - (_disabledSlots.TryGetValue(mat, out var set) ? set.Count : 0);

    void ToggleSlotEnabled(string mat, string slot)
    {
        if (!_disabledSlots.TryGetValue(mat, out var set))
            _disabledSlots[mat] = set = new HashSet<string>();
        if (set.Contains(slot))
        {
            set.Remove(slot);
        }
        else
        {
            // Refuse to disable the last enabled slot
            if (CountEnabledSlots(mat) <= 1) return;
            set.Add(slot);
        }
        RebuildMaterialsPanel();
        RenderFrame();
    }

    static Avalonia.Media.Color ToAvColor(uint argb) => Avalonia.Media.Color.FromArgb(
        (byte)((argb >> 24) & 0xFF), (byte)((argb >> 16) & 0xFF),
        (byte)((argb >>  8) & 0xFF), (byte)( argb        & 0xFF));

    // Color swatch as a Border so we can intercept right-click directly
    // (Button's Click only fires for left). Left = open color picker (when
    // enabled), right = toggle enabled/disabled.
    Border MakeColorSwatch(string materialName, string slot, uint argb, bool isActive)
    {
        var initial = ToAvColor(argb);
        var swatch = new Border
        {
            Width = 32,
            Height = 24,
            Background = new Avalonia.Media.SolidColorBrush(initial),
            BorderBrush = isActive ? Avalonia.Media.Brushes.Black : Avalonia.Media.Brushes.Crimson,
            BorderThickness = new Thickness(isActive ? 1 : 2),
            CornerRadius = new Avalonia.CornerRadius(2),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            Opacity = isActive ? 1.0 : 0.55,
        };
        // When disabled, overlay a red ✕ so the state is visually unambiguous
        if (!isActive)
        {
            swatch.Child = new TextBlock
            {
                Text = "✕",
                FontSize = 18,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brushes.Red,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                IsHitTestVisible = false,
            };
        }
        ToolTip.SetTip(swatch, isActive
            ? $"{materialName}.{slot} — left-click: edit color · right-click: disable"
            : $"{materialName}.{slot} — DISABLED (mirrors first enabled slot) · right-click: re-enable");
        swatch.PointerPressed += (_, e) =>
        {
            var props = e.GetCurrentPoint(swatch).Properties;
            if (props.IsRightButtonPressed)
            {
                ToggleSlotEnabled(materialName, slot);
                e.Handled = true;
            }
            else if (props.IsLeftButtonPressed && isActive)
            {
                OpenColorPicker(materialName, slot, swatch);
                e.Handled = true;
            }
        };
        return swatch;
    }

    // Color picker as a separate WINDOW. Avoids the macOS popup pointer-
    // capture mess entirely — slider thumbs, spectrum, palette all work
    // normally inside a real window.
    void OpenColorPicker(string materialName, string slot, Border swatch)
    {
        if (_model == null) return;
        var matIdx = _model.IndexOfMaterial(materialName);
        if (matIdx < 0) return;

        // Close any previously-open color window
        if (_activeColorWindow != null)
        {
            try { _activeColorWindow.Close(); } catch { }
            _activeColorWindow = null;
        }

        var mat = _model.Materials[matIdx];
        var argb = slot switch
        {
            "top"   => mat.Top,
            "front" => mat.Front,
            "side"  => mat.Side,
            _       => mat.Top,
        };

        var view = new Avalonia.Controls.ColorView
        {
            Color = ToAvColor(argb),
            IsAlphaEnabled = false,
            IsAlphaVisible = false,
        };
        view.ColorChanged += (_, e) =>
        {
            var c = Avalonia.Media.Color.FromArgb(255, e.NewColor.R, e.NewColor.G, e.NewColor.B);
            swatch.Background = new Avalonia.Media.SolidColorBrush(c);
            UpdateMaterialColorInVoxText(materialName, slot, c);
        };

        var win = new Window
        {
            Title = $"{materialName}.{slot}",
            Width = 360,
            Height = 460,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            SystemDecorations = SystemDecorations.Full,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0x22, 0x26, 0x2C)),
            Content = new Border
            {
                Padding = new Thickness(10),
                Child = view,
            },
        };

        // Position to the right of the main window
        win.Position = new PixelPoint(Position.X + (int)Width - 380, Position.Y + 80);

        win.Closed += (_, _) =>
        {
            if (_activeColorWindow == win) _activeColorWindow = null;
        };

        _activeColorWindow = win;
        win.Show(this);   // owned by the main window
    }

    // Walk the visual tree from `node` upward; return true if `ancestor` is
    // on the path. Used to decide if a pointer event originated inside a
    // particular subtree (popup content, swatch, etc.).
    static bool IsVisualDescendant(Visual? node, Visual ancestor)
    {
        while (node != null)
        {
            if (node == ancestor) return true;
            node = node.GetVisualParent() as Visual;
        }
        return false;
    }

    // True if `node` lives inside any popup root, overlay popup host, or
    // overlay layer — i.e. the click is happening inside SOME popup/overlay
    // (ours or an internal one opened by a child control like ColorView's
    // ColorSlider thumb, palette swatches, etc.).
    static bool IsInsidePopupOrOverlay(Visual? node)
    {
        while (node != null)
        {
            var name = node.GetType().Name;
            if (name == "PopupRoot" ||
                name == "OverlayPopupHost" ||
                name == "OverlayLayer" ||
                name == "Popup")
                return true;
            node = node.GetVisualParent() as Visual;
        }
        return false;
    }

    // Patch a single slot=#RRGGBB value in the editor. Slot count is applied
    // as a render-time override (see ApplySlotCountOverrides), so we never
    // mirror or destroy other slots' colors here.
    void UpdateMaterialColorInVoxText(string materialName, string slot,
                                      Avalonia.Media.Color newColor)
    {
        var hex = $"#{newColor.R:X2}{newColor.G:X2}{newColor.B:X2}";
        var src = VoxEditor.Text ?? "";
        var lines = src.Split('\n');
        var changed = false;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimStart();
            if (!trimmed.StartsWith("material ", StringComparison.OrdinalIgnoreCase)) continue;
            var parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;
            if (!parts[1].Equals(materialName, StringComparison.OrdinalIgnoreCase)) continue;

            var key = slot + "=";
            var idx = line.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) break;
            var hashIdx = idx + key.Length;
            if (hashIdx >= line.Length || line[hashIdx] != '#') break;
            if (hashIdx + 7 > line.Length) break;
            lines[i] = line.Substring(0, hashIdx) + hex + line.Substring(hashIdx + 7);
            changed = true;
            break;
        }
        if (!changed) return;
        _suppressMaterialsPanelRebuild = true;
        try
        {
            SetEditorText(string.Join("\n", lines));
            ReparseModel();
        }
        finally { _suppressMaterialsPanelRebuild = false; }
    }

    // Render-time copy of the model with disabled slots replaced by the
    // first enabled slot's color (priority order: top → side → front).
    // Vox source is never touched.
    VoxModel ApplyEnabledOverrides(VoxModel original)
    {
        var anyDisabled = false;
        foreach (var kv in _disabledSlots)
            if (kv.Value.Count > 0) { anyDisabled = true; break; }
        if (!anyDisabled) return original;

        var modified = new VoxModel();
        foreach (var m in original.Materials)
        {
            var topOn   = IsSlotEnabled(m.Name, "top");
            var sideOn  = IsSlotEnabled(m.Name, "side");
            var frontOn = IsSlotEnabled(m.Name, "front");
            // Fallback colour = first enabled slot in priority order
            var fallback = topOn ? m.Top : sideOn ? m.Side : m.Front;
            var top   = topOn   ? m.Top   : fallback;
            var side  = sideOn  ? m.Side  : fallback;
            var front = frontOn ? m.Front : fallback;
            modified.Materials.Add(new Material(m.Name, top, front, side));
        }
        foreach (var v in original.Voxels) modified.Voxels.Add(v);
        return modified;
    }

    void RenderFrame()
    {
        if (_model is null) return;

        double yawDeg;
        if (AutoRotateToggle.IsChecked == true)
        {
            var elapsed = (DateTime.UtcNow - _startTime).TotalSeconds;
            yawDeg = (elapsed * 360.0 / 14.0) % 360.0;
            AngleSlider.Value = yawDeg;
        }
        else
        {
            yawDeg = AngleSlider.Value;
        }
        var pitchDeg = PitchSlider.Value;
        AngleLabel.Text = $"{(int)yawDeg}°";
        PitchLabel.Text = $"{(int)pitchDeg}°";
        var yawRad = yawDeg * Math.PI / 180.0;
        var pitchRad = pitchDeg * Math.PI / 180.0;

        var basePreset = _currentLightPreset ?? LightingPresets.All[0];
        var ambientValue = AmbientSlider.Value / 100.0;
        AmbientLabel.Text = ambientValue.ToString("0.00");
        SunPositionLabel.Text = $"sun: {(int)_sunYawDeg}°/{(int)_sunPitchDeg}°";

        // Build effective light direction from the manipulable sun yaw/pitch
        var (lx, ly, lz) = ComputeSunDirection(_sunYawDeg, _sunPitchDeg);
        var light = new LightingPreset
        {
            Name = basePreset.Name,
            Ambient = ambientValue,
            LightX = lx,
            LightY = ly,
            LightZ = lz,
        };

        var studioScale  = ZoomSlider.Value;
        var pxlScale     = ZoomSlider.Value / 32.0;
        var sunIntensity = SunSlider.Value / 100.0;
        var panOffset    = PanSlider.Value / 100.0;
        SunLabel.Text = $"{(int)SunSlider.Value}%";
        PanLabel.Text = $"{(int)PanSlider.Value}";

        var modelForRender = ApplyEnabledOverrides(_model);

        // Pxl preview's pan is the studio pan scaled down to 24×24 space
        // (so middle-mouse drag in the studio also pans the LED preview the
        // same proportional amount).
        var pxlPanRatio = pxlScale / studioScale;

        // Studio: with sun marker so the light position is visually anchored
        IsoRenderer.Render(modelForRender, yawRad, pitchRad, light, _studioPixels, StudioW, StudioH, studioScale,
                            showSunMarker: true,
                            lightIntensity: sunIntensity,
                            panOffset: panOffset,
                            panScreenX: _panX,
                            panScreenY: _panY);
        BlitToBitmap(_studioBitmap, _studioPixels, StudioW, StudioH);

        // Pxl preview: sky + water gradient first, then voxels on top
        FillPxlSkyAndWater();
        IsoRenderer.Render(modelForRender, yawRad, pitchRad, light, _pxlPixels, PxlW, PxlH, pxlScale,
                            clearBackground: false,
                            lightIntensity: sunIntensity,
                            panOffset: panOffset,
                            panScreenX: _panX * pxlPanRatio,
                            panScreenY: _panY * pxlPanRatio);
        BlitToBitmap(_pxlBitmap, _pxlPixels, PxlW, PxlH);

        // LED-style preview: derive from _pxlPixels
        RenderLedPreview();
        BlitToBitmap(_ledBitmap, _ledPixels, LedW, LedH);

        StudioImage.InvalidateVisual();
        PxlImage1x.InvalidateVisual();
        PxlImage10x.InvalidateVisual();
    }

    // Render the 24×24 Pxl pixels as an LED-matrix-style preview: each pixel
    // becomes one cell with a 1px black gap on each side and 3D shading
    // (top-left highlight, bottom-right shadow) so the cells read like
    // physical LEDs sitting on a black PCB.
    // Fill the Pxl pixel buffer with the sky + water gradient (matches the
    // PalmIsland15_OrganicCove pixogram). Drawn before voxels so the model
    // sits in front of the gradient.
    void FillPxlSkyAndWater()
    {
        for (var y = 0; y < PxlH; y++)
        {
            uint c;
            switch (_bgMode)
            {
                case BgMode.SkyOnly:
                {
                    var f = y / (double)(PxlH - 1);
                    c = LerpArgb(cSkyTop, cSkyHorizon, f);
                    break;
                }
                case BgMode.WaterOnly:
                {
                    var f = y / (double)(PxlH - 1);
                    c = LerpArgb(cWaterBright, cWaterDeep, f);
                    break;
                }
                default: // Both — sky on top, water on bottom (horizon at HorizonY)
                {
                    if (y < HorizonY)
                    {
                        var f = y / (double)(HorizonY - 1);
                        c = LerpArgb(cSkyTop, cSkyHorizon, f);
                    }
                    else
                    {
                        var f = (y - HorizonY) / (double)(PxlH - 1 - HorizonY);
                        c = LerpArgb(cWaterBright, cWaterDeep, f);
                    }
                    break;
                }
            }
            for (var x = 0; x < PxlW; x++)
                _pxlPixels[y * PxlW + x] = c;
        }
    }

    void SetBgMode(BgMode mode)
    {
        _bgMode = mode;
        BgBothToggle.IsChecked  = mode == BgMode.Both;
        BgSkyToggle.IsChecked   = mode == BgMode.SkyOnly;
        BgWaterToggle.IsChecked = mode == BgMode.WaterOnly;
        RenderFrame();
    }

    // World-space sun direction from yaw (azimuth) + pitch (elevation) in degrees.
    //   yaw = 0  → light comes from -Z (front)
    //   yaw = 90 → light comes from -X (left)
    //   pitch = 0  → horizon level   pitch = 90 → straight overhead
    static (double lx, double ly, double lz) ComputeSunDirection(double yawDeg, double pitchDeg)
    {
        var yawR = yawDeg * Math.PI / 180.0;
        var pitR = pitchDeg * Math.PI / 180.0;
        var cp = Math.Cos(pitR);
        return (
            -Math.Sin(yawR) * cp,
             Math.Sin(pitR),
            -Math.Cos(yawR) * cp);
    }

    // Inverse of ComputeSunDirection — extract yaw/pitch from a preset's
    // (LightX, LightY, LightZ) so the sun state matches the chosen preset.
    void DeriveSunFromPreset(LightingPreset p)
    {
        var len = Math.Sqrt(p.LightX * p.LightX + p.LightY * p.LightY + p.LightZ * p.LightZ);
        if (len < 1e-9) return;
        var lx = p.LightX / len;
        var ly = p.LightY / len;
        var lz = p.LightZ / len;
        _sunPitchDeg = Math.Asin(Math.Clamp(ly, -1.0, 1.0)) * (180.0 / Math.PI);
        var yawRad = Math.Atan2(-lx, -lz);
        var deg = yawRad * (180.0 / Math.PI);
        if (deg < 0) deg += 360;
        _sunYawDeg = deg;
    }

    static uint LerpArgb(uint a, uint b, double t)
    {
        if (t < 0) t = 0; else if (t > 1) t = 1;
        var ar = (a >> 16) & 0xFF;
        var ag = (a >> 8) & 0xFF;
        var ab = a & 0xFF;
        var br = (b >> 16) & 0xFF;
        var bg = (b >> 8) & 0xFF;
        var bb = b & 0xFF;
        var nr = (uint)(ar + (br - (double)ar) * t);
        var ng = (uint)(ag + (bg - (double)ag) * t);
        var nb = (uint)(ab + (bb - (double)ab) * t);
        return 0xFF000000u | (nr << 16) | (ng << 8) | nb;
    }

    void RenderLedPreview()
    {
        // Every pixel becomes an LED cell (no skip — sky + water render too).
        const uint bg = 0xFF050608;  // PCB black between the cells
        for (var i = 0; i < _ledPixels.Length; i++) _ledPixels[i] = bg;

        var inner = LedCell - 2 * LedGap;
        for (var cy = 0; cy < PxlH; cy++)
        for (var cx = 0; cx < PxlW; cx++)
        {
            var color = _pxlPixels[cy * PxlW + cx];
            var x0 = cx * LedCell + LedGap;
            var y0 = cy * LedCell + LedGap;
            var brightHi   = ScaleColor(color, 1.35);
            var brightMid  = ScaleColor(color, 1.10);
            var brightLow  = ScaleColor(color, 0.65);
            var brightDark = ScaleColor(color, 0.40);

            for (var dy = 0; dy < inner; dy++)
            for (var dx = 0; dx < inner; dx++)
            {
                uint c;
                if (dx == 0 && dy == 0) c = brightHi;
                else if (dx == 0 || dy == 0) c = brightMid;
                else if (dx == inner - 1 && dy == inner - 1) c = brightDark;
                else if (dx == inner - 1 || dy == inner - 1) c = brightLow;
                else c = color;
                _ledPixels[(y0 + dy) * LedW + (x0 + dx)] = c;
            }
        }
    }

    static uint ScaleColor(uint c, double factor)
    {
        var a = (c >> 24) & 0xFFu;
        var r = (uint)Math.Min(255, Math.Max(0, ((c >> 16) & 0xFF) * factor));
        var g = (uint)Math.Min(255, Math.Max(0, ((c >> 8)  & 0xFF) * factor));
        var b = (uint)Math.Min(255, Math.Max(0, ( c        & 0xFF) * factor));
        return (a << 24) | (r << 16) | (g << 8) | b;
    }

    static void BlitToBitmap(WriteableBitmap bitmap, uint[] pixels, int width, int height)
    {
        using var fb = bitmap.Lock();
        unsafe
        {
            var dst = (uint*)fb.Address;
            for (var i = 0; i < pixels.Length; i++) dst[i] = pixels[i];
        }
    }

    // ========================================================================
    // File dialogs (default to the models folder)
    // ========================================================================

    async Task<IStorageFolder?> GetModelsFolder()
    {
        var sp = StorageProvider;
        if (sp is null || _modelsDir is null) return null;
        try { return await sp.TryGetFolderFromPathAsync(_modelsDir); }
        catch { return null; }
    }

    async Task OpenFileDialog()
    {
        var sp = StorageProvider;
        if (sp is null) return;
        var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open .vox model",
            AllowMultiple = false,
            SuggestedStartLocation = await GetModelsFolder(),
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Voxel models") { Patterns = new[] { "*.vox", "*.txt" } },
                FilePickerFileTypes.All,
            },
        });
        if (files.Count == 0) return;
        var picked = files[0];
        await using var stream = await picked.OpenReadAsync();
        using var reader = new StreamReader(stream);
        SetEditorText(await reader.ReadToEndAsync());
        _currentFilePath = picked.TryGetLocalPath();
        Title = $"Voxel Studio — {picked.Name}";
        if (_currentFilePath != null) StartWatchingFile(_currentFilePath);
        ReparseModel();
    }

    async Task SaveFileDialog()
    {
        var sp = StorageProvider;
        if (sp is null) return;
        var suggestedName = _currentFilePath != null ? Path.GetFileName(_currentFilePath) : "model.vox";
        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save .vox model",
            DefaultExtension = "vox",
            SuggestedFileName = suggestedName,
            SuggestedStartLocation = await GetModelsFolder(),
        });
        if (file is null) return;
        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(VoxEditor.Text);
        _currentFilePath = file.TryGetLocalPath();
        Title = $"Voxel Studio — {file.Name}";
        RescanModelsFolder();  // pick up newly-created file in dropdown
    }
}

// Simple wrapper so ComboBox displays the relative path while we keep the
// full path for loading. ToString is used by Avalonia's default item template.
public sealed class VoxFileEntry
{
    public string FullPath { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public override string ToString() => DisplayName;
}
