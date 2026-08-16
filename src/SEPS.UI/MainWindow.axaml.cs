using Avalonia;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SEPS.Core;
using SEPS.Editor;
using SEPS.IO;

namespace SEPS.UI;

public partial class MainWindow : Window
{
    private readonly EditorState _editorState =
    new();

    private Planet? _planet;
    private CubeFace _selectedFace = CubeFace.Front;

    private bool _heightMapLoaded;
    private int _heightMapWidth;
    private int _heightMapHeight;
    private ushort _heightMapMin;
    private ushort _heightMapMax;

    private bool _middleMouseDragging;
    private Point _lastMousePosition;

    private readonly HashSet<Key>
        _navigationKeys =
            new();
    private readonly DispatcherTimer
        _navigationTimer;
    private readonly Stopwatch
        _navigationClock =
            Stopwatch.StartNew();

    private double _lastNavigationTime;
    private double _panVelocityX;
    private double _panVelocityY;
    private double _yawVelocity;

    // V0.1.1: currently loaded mod/root directory.
    private string? _planetRoot;

    // V0.1.1: planets discovered underneath the selected root.
    private IReadOnlyList<Planet> _discoveredPlanets =
        Array.Empty<Planet>();

    // Editing infrastructure.
    // This will remain false until actual editing is implemented.
    private bool _hasUnsavedChanges;

    // Prevents programmatic ComboBox updates from triggering
    // planet switching recursively.
    private bool _updatingPlanetSelector;

    public MainWindow()
    {
        InitializeComponent();

        _navigationTimer =
            new DispatcherTimer
            {
                Interval =
                    TimeSpan.FromMilliseconds(16)
            };

        _navigationTimer.Tick +=
            NavigationTimer_Tick;

        _lastNavigationTime =
            _navigationClock.Elapsed.TotalSeconds;
    }

    private async void OpenPlanet_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_hasUnsavedChanges)
        {
            var discard =
                await ConfirmDiscardChangesAsync();

            if (!discard)
                return;
        }

        var folders =
            await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title =
                        "Select Space Engineers Planet Mod / Root Folder",
                    AllowMultiple = false
                });

        var folder = folders.FirstOrDefault();

        if (folder == null)
            return;

        var path = folder.TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(path))
        {
            StatusText.Text =
                "The selected folder does not have a usable local path.";

            return;
        }

        try
        {
            StatusText.Text =
                "Searching for planets...";

            _planetRoot = path;

            _discoveredPlanets =
                PlanetFolderLoader.Discover(path);

            if (_discoveredPlanets.Count == 0)
            {
                _planet = null;
                _hasUnsavedChanges = false;

                ClearPlanetSelection();

                StatusText.Text =
                    "No Space Engineers planet directories were found.";

                return;
            }

            // Select the first discovered planet by default.
            _planet =
                _discoveredPlanets[0];

            _editorState.SetPlanet(
                _planet);

            _hasUnsavedChanges = false;

            UpdatePlanetSelectionUi();

            StatusText.Text =
                BuildStatus(_planet);
        }
        catch (Exception ex)
        {
            _planet = null;
            _discoveredPlanets =
                Array.Empty<Planet>();

            _hasUnsavedChanges = false;

            ClearPlanetSelection();

            StatusText.Text =
                $"Unable to open planet mod: {ex.Message}";
        }
    }

    private async void PlanetSelector_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_updatingPlanetSelector)
            return;

        if (PlanetSelector.SelectedItem is Planet planet)
        {
            await SwitchPlanetAsync(planet);
        }
    }

    private async void SwitchPlanetMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: Planet planet })
        {
            await SwitchPlanetAsync(planet);
        }
    }

    private async Task SwitchPlanetAsync(
        Planet planet)
    {
        if (ReferenceEquals(_planet, planet))
            return;

        if (_hasUnsavedChanges)
        {
            var discard =
                await ConfirmDiscardChangesAsync();

            if (!discard)
            {
                RestorePlanetSelector();
                return;
            }
        }

        _planet = planet;

        _editorState.SetPlanet(
            planet);

        _hasUnsavedChanges = false;

        UpdatePlanetSelectionUi();

        StatusText.Text =
            BuildStatus(_planet);
    }

    private void UpdatePlanetSelectionUi()
    {
        _updatingPlanetSelector = true;

        try
        {
            PlanetSelector.ItemsSource =
                _discoveredPlanets;

            PlanetSelector.SelectedItem =
                _planet;
        }
        finally
        {
            _updatingPlanetSelector = false;
        }

        if (_planet == null)
        {
            ClearPlanetSelection();
            return;
        }

        PlanetSubtypeText.Text =
            _planet.DataSubtypeId;

        GeneratorDefinitionText.Text =
            _planet.GeneratorDefinition != null
                ? $"✓ {Path.GetFileName(
                    _planet.GeneratorDefinition.SourceFilePath)}"
                : "⚠ Not found";

        RebuildSwitchPlanetMenu();

        _ = LoadSelectedFaceAsync();
    }

    private void ClearPlanetSelection()
    {
        _updatingPlanetSelector = true;

        try
        {
            PlanetSelector.ItemsSource =
                Array.Empty<Planet>();

            PlanetSelector.SelectedItem = null;
        }
        finally
        {
            _updatingPlanetSelector = false;
        }

        PlanetSubtypeText.Text =
            string.Empty;

        GeneratorDefinitionText.Text =
            string.Empty;

        RebuildSwitchPlanetMenu();
    }

    private void RestorePlanetSelector()
    {
        _updatingPlanetSelector = true;

        try
        {
            PlanetSelector.SelectedItem =
                _planet;
        }
        finally
        {
            _updatingPlanetSelector = false;
        }
    }

    private void RebuildSwitchPlanetMenu()
    {
        SwitchPlanetMenu.Items.Clear();

        foreach (var planet in _discoveredPlanets)
        {
            var item = new MenuItem
            {
                Header =
                    ReferenceEquals(planet, _planet)
                        ? $"✓ {planet.Name}"
                        : planet.Name,

                Tag = planet
            };

            item.Click +=
                SwitchPlanetMenuItem_Click;

            SwitchPlanetMenu.Items.Add(item);
        }

        SwitchPlanetMenu.IsEnabled =
            _discoveredPlanets.Count > 1;
    }

    private async Task<bool> ConfirmDiscardChangesAsync()
    {
        var dialog = new Window
        {
            Title = "Unsaved Changes",
            Width = 460,
            Height = 190,
            CanResize = false,
            WindowStartupLocation =
                WindowStartupLocation.CenterOwner
        };

        var message = new TextBlock
        {
            Text =
                "The current planet has unsaved changes.\n\n" +
                "Switching planets without saving will discard them.",
            TextWrapping =
                Avalonia.Media.TextWrapping.Wrap
        };

        var discardButton = new Button
        {
            Content = "Discard Changes",
            Width = 130
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 100
        };

        discardButton.Click +=
            (_, _) => dialog.Close(true);

        cancelButton.Click +=
            (_, _) => dialog.Close(false);

        var buttons = new StackPanel
        {
            Orientation =
                Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment =
                Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8,
            Children =
            {
                cancelButton,
                discardButton
            }
        };

        dialog.Content = new StackPanel
        {
            Margin =
                new Avalonia.Thickness(20),
            Spacing = 18,
            Children =
            {
                message,
                buttons
            }
        };

        var result =
            await dialog.ShowDialog<bool>(this);

        return result;
    }

    private async void Face_Click(
    object? sender,
    RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag } ||
            !Enum.TryParse<CubeFace>(
                tag,
                out var face))
        {
            return;
        }

        _selectedFace =
            face;

        _heightMapLoaded =
            false;

        FaceInfoText.Text =
            $"Face: {face}";

        await LoadSelectedFaceAsync();
    }

    private void ResetView_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _navigationKeys.Clear();

        _panVelocityX = 0.0;
        _panVelocityY = 0.0;
        _yawVelocity = 0.0;

        _navigationTimer.Stop();

        GlViewport.ViewportState.Reset();

        GlViewport.RequestViewportRedraw();

        UpdateViewportInfo();

        MapWorkspace.Focus(
            NavigationMethod.Pointer);
    }

    private async void Diagnostics_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var dialog =
            new Window
            {
                Title =
                    "SEPS Diagnostics",
                Width =
                    560,
                Height =
                    620,
                MinWidth =
                    500,
                MinHeight =
                    500,
                CanResize =
                    true,
                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner
            };

        var diagnostics =
            BuildDiagnosticsText();

        var textBlock =
            new TextBlock
            {
                Text =
                    diagnostics,
                FontFamily =
                    new Avalonia.Media.FontFamily(
                        "Consolas"),
                FontSize =
                    13,
                TextWrapping =
                    Avalonia.Media.TextWrapping.NoWrap
            };

        var closeButton =
            new Button
            {
                Content =
                    "Close",
                HorizontalAlignment =
                    Avalonia.Layout.HorizontalAlignment.Right,
                MinWidth =
                    90
            };

        closeButton.Click +=
            (_, _) =>
                dialog.Close();

        dialog.Content =
            new StackPanel
            {
                Margin =
                    new Avalonia.Thickness(18),
                Spacing =
                    12,
                Children =
                {
                    textBlock,
                    closeButton
                }
            };

        await dialog.ShowDialog(this);
    }

    private string BuildDiagnosticsText()
    {
        var lines =
            new List<string>();

        lines.Add(
            "SEPS Diagnostics");

        lines.Add(
            "==============================");

        lines.Add(
            "Application");

        lines.Add(
            "  Version: V0.2.8");

        lines.Add(
            "  Runtime: " +
            Environment.Version);

        lines.Add(string.Empty);

        lines.Add(
            "Planet");

        lines.Add(
            "  Name: " +
            (_planet?.Name ??
            "<none>"));

        lines.Add(
            "  SubtypeId: " +
            (_planet?.DataSubtypeId ??
            "<none>"));

        lines.Add(
            "  Selected face: " +
            _selectedFace);

        var heightCount =
            _planet?.Faces.Values.Count(
                f => f.HeightMapPath != null)
            ?? 0;

        var materialCount =
            _planet?.Faces.Values.Count(
                f => f.MaterialMapPath != null)
            ?? 0;

        lines.Add(
            $"  Heightmaps: {heightCount}/6");

        lines.Add(
            $"  Material maps: {materialCount}/6");

        lines.Add(string.Empty);

        lines.Add(
            "Heightmap");

        if (_heightMapLoaded)
        {
            lines.Add(
                $"  Dimensions: " +
                $"{_heightMapWidth} × " +
                $"{_heightMapHeight}");

            lines.Add(
                "  Bit depth: 16-bit");

            lines.Add(
                $"  Range: " +
                $"{_heightMapMin} → " +
                $"{_heightMapMax}");
        }
        else
        {
            lines.Add(
                "  No heightmap loaded.");
        }

        lines.Add(string.Empty);

        lines.Add(
            "Viewport");

        lines.Add(
            $"  Zoom: " +
            $"{GlViewport.ViewportState.Zoom:0.0000}×");

        lines.Add(
            $"  Pan X: " +
            $"{GlViewport.ViewportState.PanX:0.0000}");

        lines.Add(
            $"  Pan Y: " +
            $"{GlViewport.ViewportState.PanY:0.0000}");

        lines.Add(
            $"  Yaw: " +
            $"{GlViewport.ViewportState.Yaw:0.00}°");

        lines.Add(string.Empty);

        lines.Add(
            "Renderer");

        lines.Add(
            "  OpenGL initialized: " +
            (GlViewport.IsRendererInitialized
                ? "Yes"
                : "No"));

        lines.Add(
            $"  Texture: " +
            $"{GlViewport.RendererTextureWidth} × " +
            $"{GlViewport.RendererTextureHeight}");

        return string.Join(
            Environment.NewLine,
            lines);
    }

    private void UpdateViewportInfo()
    {
        ViewportInfoText.Text =
            $"Zoom: " +
            $"{GlViewport.ViewportState.Zoom:0.00}×";
    }

    private void MapWorkspace_PointerWheelChanged(
        object? sender,
        PointerWheelEventArgs e)
    {
        var delta = e.Delta.Y;

        if (Math.Abs(delta) < double.Epsilon)
            return;

        var factor =
            delta > 0
                ? 1.15
                : 1.0 / 1.15;

        GlViewport.ViewportState.AdjustZoom(
            factor);

        GlViewport.RequestViewportRedraw();

        UpdateViewportInfo();

        e.Handled = true;
    }

    private void MapWorkspace_PointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        MapWorkspace.Focus(
            NavigationMethod.Pointer);

        var properties =
            e.GetCurrentPoint(
                MapWorkspace).Properties;

        if (properties.IsMiddleButtonPressed)
        {
            _middleMouseDragging = true;

            _lastMousePosition =
                e.GetPosition(
                    MapWorkspace);

            e.Pointer.Capture(
                MapWorkspace);
        }

        e.Handled = true;
    }

    private void MapWorkspace_PointerMoved(
        object? sender,
        PointerEventArgs e)
    {
        if (!_middleMouseDragging)
            return;

        var currentPosition =
            e.GetPosition(
                MapWorkspace);

        var deltaX =
            currentPosition.X -
            _lastMousePosition.X;

        _lastMousePosition =
            currentPosition;

        if (Math.Abs(deltaX) <
            double.Epsilon)
        {
            return;
        }

        const double yawSensitivity =
            0.35;

        GlViewport.ViewportState.AdjustYaw(
            deltaX *
            yawSensitivity);

        GlViewport.RequestViewportRedraw();

        e.Handled = true;
    }

    private void MapWorkspace_PointerReleased(
        object? sender,
        PointerReleasedEventArgs e)
    {
        if (!_middleMouseDragging)
            return;

        _middleMouseDragging = false;

        e.Pointer.Capture(
            null);

        e.Handled = true;
    }

    private void MapWorkspace_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.W:
            case Key.S:
            case Key.A:
            case Key.D:
            case Key.Q:
            case Key.E:
                _navigationKeys.Add(
                    e.Key);

                _navigationTimer.Start();

                e.Handled = true;
                break;
        }
    }

    private void MapWorkspace_KeyUp(
        object? sender,
        KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.W:
            case Key.S:
            case Key.A:
            case Key.D:
            case Key.Q:
            case Key.E:
                _navigationKeys.Remove(
                    e.Key);

                e.Handled = true;
                break;
        }
    }

    private void MapWorkspace_LostFocus(
        object? sender,
        RoutedEventArgs e)
    {
        _navigationKeys.Clear();
    }

    private void NavigationTimer_Tick(
        object? sender,
        EventArgs e)
    {
        var now =
            _navigationClock.Elapsed.TotalSeconds;

        var deltaTime =
            now -
            _lastNavigationTime;

        _lastNavigationTime =
            now;

        // Protect against a large jump if the UI thread was paused.
        deltaTime =
            Math.Clamp(
                deltaTime,
                0.0,
                0.05);

        const double panAcceleration =
            4.0;

        const double yawAcceleration =
            360.0;

        const double basePanSpeed =
            0.65;

        const double yawSpeed =
            100.0;

        var targetPanX =
            0.0;

        var targetPanY =
            0.0;

        var targetYaw =
            0.0;

        if (_navigationKeys.Contains(
            Key.W))
        {
            targetPanY += 1.0;
        }

        if (_navigationKeys.Contains(
            Key.S))
        {
            targetPanY -= 1.0;
        }

        if (_navigationKeys.Contains(
            Key.A))
        {
            targetPanX -= 1.0;
        }

        if (_navigationKeys.Contains(
            Key.D))
        {
            targetPanX += 1.0;
        }

        if (_navigationKeys.Contains(
            Key.Q))
        {
            targetYaw -= 1.0;
        }

        if (_navigationKeys.Contains(
            Key.E))
        {
            targetYaw += 1.0;
        }

        var zoom =
            GlViewport.ViewportState.Zoom;

        var panSpeed =
            basePanSpeed /
            Math.Max(
                zoom,
                1.0);

        targetPanX *= panSpeed;
        targetPanY *= panSpeed;
        targetYaw *= yawSpeed;

        _panVelocityX =
            MoveTowards(
                _panVelocityX,
                targetPanX,
                panAcceleration *
                deltaTime);

        _panVelocityY =
            MoveTowards(
                _panVelocityY,
                targetPanY,
                panAcceleration *
                deltaTime);

        _yawVelocity =
            MoveTowards(
                _yawVelocity,
                targetYaw,
                yawAcceleration *
                deltaTime);

        if (Math.Abs(_panVelocityX) > 0.000001 ||
            Math.Abs(_panVelocityY) > 0.000001 ||
            Math.Abs(_yawVelocity) > 0.001)
        {
            GlViewport.ViewportState.Pan(
                _panVelocityX *
                    deltaTime,
                _panVelocityY *
                    deltaTime);

            GlViewport.ViewportState.AdjustYaw(
                _yawVelocity *
                    deltaTime);

            GlViewport.RequestViewportRedraw();

            UpdateViewportInfo();
        }

        var motionActive =
            _navigationKeys.Count > 0 ||
            Math.Abs(_panVelocityX) > 0.000001 ||
            Math.Abs(_panVelocityY) > 0.000001 ||
            Math.Abs(_yawVelocity) > 0.001;

        if (!motionActive)
        {
            _navigationTimer.Stop();
        }
    }

    private static double MoveTowards(
        double current,
        double target,
        double maxDelta)
    {
        if (Math.Abs(
                target -
                current) <= maxDelta)
        {
            return target;
        }

        return current +
            Math.Sign(
                target -
                current) *
            maxDelta;
    }

    private async Task LoadSelectedFaceAsync()
    {
        if (_planet == null)
        {
            GlViewport.SetHeightMap(
                null);

            _heightMapLoaded = false;
            _heightMapWidth = 0;
            _heightMapHeight = 0;
            _heightMapMin = 0;
            _heightMapMax = 0;

            StatusText.Text =
                "No planet selected.";

            return;
        }

        var session =
            _editorState.Session;

        if (session == null)
        {
            StatusText.Text =
                "No active planet editing session.";

            GlViewport.SetHeightMap(
                null);

            return;
        }

        var planetFace =
            _planet.Faces[_selectedFace];

        var heightAsset =
            session.GetAsset(
                _selectedFace,
                MapKind.Height);

        if (heightAsset.HeightMap != null)
        {
            var heightMap =
                heightAsset.HeightMap;

            UpdateHeightMapDiagnostics(
                heightMap);

            GlViewport.SetHeightMap(
                heightMap);

            StatusText.Text =
                $"{_selectedFace} heightmap reused " +
                $"({heightMap.Width}×{heightMap.Height}).";

            return;
        }

        if (string.IsNullOrWhiteSpace(
            planetFace.HeightMapPath))
        {   
            GlViewport.SetHeightMap(
                null);

            _heightMapLoaded = false;
            _heightMapWidth = 0;
            _heightMapHeight = 0;
            _heightMapMin = 0;
            _heightMapMax = 0;

            StatusText.Text =
                $"No heightmap found for {_selectedFace}.";

            return;
        }

        var path =
            planetFace.HeightMapPath;

        try
        {
            StatusText.Text =
                $"Loading {_selectedFace} heightmap...";

            Console.WriteLine(
                $"HEIGHTMAP LOAD: {_selectedFace}");

            Console.WriteLine(
                $"Path: {path}");

            var heightMap =
                await Task.Run(
                    () =>
                        PlanetImageLoader.LoadHeightMap(
                            path));

            UpdateHeightMapDiagnostics(
                heightMap);

            heightAsset.SetHeightMap(
                heightMap);

            Console.WriteLine(
                $"HEIGHTMAP LOADED: " +
                $"{heightMap.Width}x{heightMap.Height}");

            Console.WriteLine(
                $"Samples: {heightMap.Samples.Length}");

            Console.WriteLine(
                $"Range: {_heightMapMin} → {_heightMapMax}");

            GlViewport.SetHeightMap(
                heightMap);

            StatusText.Text =
                $"{_selectedFace} heightmap loaded " +
                $"({heightMap.Width}×{heightMap.Height}).";
        }
        catch (Exception ex)
        {
            GlViewport.SetHeightMap(
                null);

            StatusText.Text =
                $"Unable to load {_selectedFace} heightmap: " +
                ex.Message;

            Console.WriteLine(
                $"HEIGHTMAP LOAD FAILED: {ex}");
        }
    }

    private void UpdateHeightMapDiagnostics(
        HeightMap heightMap)
    {
        ushort min =
            ushort.MaxValue;

        ushort max =
            ushort.MinValue;

        foreach (var sample in
                heightMap.Samples)
        {
            if (sample < min)
                min = sample;

            if (sample > max)
                max = sample;
        }

        _heightMapLoaded =
            true;

        _heightMapWidth =
            heightMap.Width;

        _heightMapHeight =
            heightMap.Height;

        _heightMapMin =
            min;

        _heightMapMax =
            max;
    }

    private void MainWindow_Opened(
        object? sender,
        EventArgs e)
    {
        UpdateViewportInfo();
        
        MapWorkspace.Focus(
            NavigationMethod.Pointer);
    }

    private void Exit_Click(
        object? sender,
        RoutedEventArgs e) =>
        Close();

    private async void About_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "About SEPS",
            Width = 450,
            Height = 220,
            Content = new TextBlock
            {
                Text =
                    "Space Engineers Planet Studio\n\n" +
                    "V0.2.8 — Diagnostics Implementation",
                Margin =
                    new Avalonia.Thickness(24),
                TextWrapping =
                    Avalonia.Media.TextWrapping.Wrap
            }
        };

        await dialog.ShowDialog(this);
    }

    private static string BuildStatus(
        Planet planet)
    {
        var heightCount =
            planet.Faces.Values.Count(
                f => f.HeightMapPath != null);

        var materialCount =
            planet.Faces.Values.Count(
                f => f.MaterialMapPath != null);

        var lines = new List<string>
        {
            $"SubtypeId: {planet.DataSubtypeId}",
            $"Detected height maps: {heightCount}/6",
            $"Detected material maps: {materialCount}/6"
        };

        if (planet.GeneratorDefinition is { } definition)
        {
            lines.Add(
                $"Generator definition: " +
                $"{Path.GetFileName(
                    definition.SourceFilePath)}");

            if (definition.HillParams is { } hills)
            {
                lines.Add(
                    $"HillParams: " +
                    $"{hills.Min} → {hills.Max}");
            }

            if (definition.SurfaceDetail is { } detail)
            {
                lines.Add(
                    $"Surface detail: " +
                    $"{detail.Texture ?? "none"}");
            }

            if (definition.SurfaceGravity is { } gravity)
            {
                lines.Add(
                    $"Surface gravity: {gravity}");
            }
        }
        else
        {
            lines.Add(
                "Generator definition: NOT FOUND");
        }

        return string.Join(
            Environment.NewLine,
            lines);
    }
}