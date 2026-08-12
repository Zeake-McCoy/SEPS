using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SEPS.Core;
using SEPS.IO;

namespace SEPS.UI;

public partial class MainWindow : Window
{
    private Planet? _planet;
    private CubeFace _selectedFace = CubeFace.Front;

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

        // Once we have real editing, this will only be reset
        // after saving/discarding the previous planet.
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

        FaceInfoText.Text =
            $"Face: {face}";

        await LoadSelectedFaceAsync();
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

        e.Handled = true;
    }

    private void MapWorkspace_PointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        MapWorkspace.Focus(
            NavigationMethod.Pointer);

        e.Handled = true;
    }

    private void MapWorkspace_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        const double movement =
            0.08;

        switch (e.Key)
        {
            case Key.W:
                GlViewport.ViewportState.Pan(
                    0.0,
                    movement);
                break;

            case Key.S:
                GlViewport.ViewportState.Pan(
                    0.0,
                    -movement);
                break;

            case Key.A:
                GlViewport.ViewportState.Pan(
                    movement,
                    0.0);
                break;

            case Key.D:
                GlViewport.ViewportState.Pan(
                    -movement,
                    0.0);
                break;

            default:
                return;
        }

        GlViewport.RequestViewportRedraw();

        e.Handled = true;
    }

    private async Task LoadSelectedFaceAsync()
    {
        if (_planet == null)
        {
            GlViewport.SetHeightMap(
                null);

            StatusText.Text =
                "No planet selected.";

            return;
        }

        var planetFace =
            _planet.Faces[_selectedFace];

        if (string.IsNullOrWhiteSpace(
            planetFace.HeightMapPath))
        {
            GlViewport.SetHeightMap(
                null);

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

            ushort min =
                ushort.MaxValue;

            ushort max =
                ushort.MinValue;

            foreach (var sample in heightMap.Samples)
            {
                if (sample < min)
                    min = sample;

                if (sample > max)
                    max = sample;
            }

            Console.WriteLine(
                $"HEIGHTMAP LOADED: " +
                $"{heightMap.Width}x{heightMap.Height}");

            Console.WriteLine(
                $"Samples: {heightMap.Samples.Length}");

            Console.WriteLine(
                $"Range: {min} → {max}");

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
                    "V0.2.1 — Raw Map Renderer",
                Margin =
                    new Avalonia.Thickness(24),
                TextWrapping =
                    Avalonia.Media.TextWrapping.Wrap
            }
        };

        await dialog.ShowDialog(this);
    }

    private static string DescribeInputSource(
    object? source)
    {
        if (source is Avalonia.Controls.Control control)
        {
            return
                $"{control.GetType().Name}" +
                $" Name={control.Name ?? "<unnamed>"}";
        }

        return source?.GetType().Name
            ?? "<null>";
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