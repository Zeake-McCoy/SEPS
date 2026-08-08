using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Linq;
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

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OpenPlanet_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select Space Engineers Planet Mod / Root Folder",
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
            StatusText.Text = "Searching for planets...";

            _planetRoot = path;

            _discoveredPlanets =
                PlanetFolderLoader.Discover(path);

            if (_discoveredPlanets.Count == 0)
            {
                _planet = null;
                PlanetNameText.Text = "No planet found";

                StatusText.Text =
                    "No Space Engineers planet directories were found.";
                
                return;
            }

            // V0.1.1:
            // If exactly one planet exists, select it automatically.
            //
            // Multiple-planet selection UI is V0.1.2.
            _planet = _discoveredPlanets[0];

            PlanetNameText.Text = _planet.Name;
            StatusText.Text = BuildStatus(_planet);
        }
        catch (Exception ex)
        {
            _planet = null;
            _discoveredPlanets = Array.Empty<Planet>();

            StatusText.Text =
                $"Unable to open planet mod: {ex.Message}";
        }
    }

    private void Face_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag } &&
            Enum.TryParse<CubeFace>(tag, out var face))
        {
            _selectedFace = face;
            FaceInfoText.Text = $"Face: {face}";
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
                    "v0.1.1 — Planet root discovery",
                Margin = new Avalonia.Thickness(24),
                TextWrapping =
                    Avalonia.Media.TextWrapping.Wrap
            }
        };

        await dialog.ShowDialog(this);
    }

    private static string BuildStatus(Planet planet)
    {
        var heightCount =
            planet.Faces.Values.Count(
                f => f.HeightMapPath != null);

        var materialCount =
            planet.Faces.Values.Count(
                f => f.MaterialMapPath != null);

        return
            $"Detected height maps: {heightCount}/6\n" +
            $"Detected material maps: {materialCount}/6";
    }
}