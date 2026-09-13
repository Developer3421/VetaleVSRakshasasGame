using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace VetaleVSRakshasasGame.Scenes;

/// <summary>Сторінка рівня 2: MonoGame вбудований через NativeControlHost. Жодних окремих вікон.</summary>
public partial class Level2 : UserControl
{
    public event EventHandler<(int Score, int Kills)>? LevelFinished;
    public event EventHandler? BackRequested;
    public bool IsRunning => GameHost.Running;

    public Level2()
    {
        InitializeComponent();
        GameHost.Finished += (score, kills) => LevelFinished?.Invoke(this, (score, kills));
    }

    public void SetStatus(string text) => StatusText.Text = text;

    public void StartLevel()
    {
        SetStatus("Рівень 2: ціль мишею, клік — постріл");
        GameHost.Start();
    }

    public void StopLevel() => GameHost.Stop();

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        StopLevel();
        BackRequested?.Invoke(this, EventArgs.Empty);
    }
}
