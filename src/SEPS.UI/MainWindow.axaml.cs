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

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OpenPlanet_Click(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select Space Engineers Planet Folder",
                AllowMultiple = false
            });

        var folder = folders.FirstOrDefault();
        if (folder == null) return;

        var path = folder.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            _planet = PlanetFolderLoader.Scan(path);
            PlanetNameText.Text = _planet.Name;
            StatusText.Text = BuildStatus(_planet);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Unable to open planet: {ex.Message}";
        }
    }

    private void Face_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag } && Enum.TryParse<CubeFace>(tag, out var face))
        {
            _selectedFace = face;
            FaceInfoText.Text = $"Face: {face}";
        }
    }

    private void Exit_Click(object? sender, RoutedEventArgs e) => Close();

    private async void About_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "About SEPS",
            Width = 450,
            Height = 220,
            Content = new TextBlock
            {
                Text = "Space Engineers Planet Studio\n\nv0.1 prototype — Face workspace foundation",
                Margin = new Avalonia.Thickness(24),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            }
        };
        await dialog.ShowDialog(this);
    }

    private static string BuildStatus(Planet planet)
    {
        var heightCount = planet.Faces.Values.Count(f => f.HeightMapPath != null);
        var materialCount = planet.Faces.Values.Count(f => f.MaterialMapPath != null);
        return $"Detected height maps: {heightCount}/6\nDetected material maps: {materialCount}/6";
    }
}
