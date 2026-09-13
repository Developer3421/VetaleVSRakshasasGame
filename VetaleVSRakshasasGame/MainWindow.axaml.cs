using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Media.Imaging;
using System;

namespace VetaleVSRakshasasGame;

public partial class MainWindow : Window
{
    private bool _isEnglish = true;

    public MainWindow()
    {
        InitializeComponent();
        LevelView.BackRequested += (_, _) => CloseLevel();
        LevelView.LevelFinished += (_, r) =>
        {
            LevelView.IsVisible = false;
            MenuView.IsVisible = true;
            StatusText.Text = $"Гру завершено. Рахунок: {r.Score}, фраги: {r.Kills}";
        };
        Closing += (_, _) => LevelView.StopLevel();
    }

    private void OnMinClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        // Кліки по кнопках Згорнути/Закрити не починають перетягування
        var visual = e.Source as Visual;
        while (visual is not null)
        {
            if (visual is Button)
                return;
            visual = visual.GetVisualParent();
        }
        BeginMoveDrag(e);
    }

    private void OnNewGameClick(object? sender, RoutedEventArgs e)
    {
        if (LevelView.IsRunning)
            return; // рівень уже йде
        // Рівень-перезапис: меню ховається цілком, видно тільки сторінку рівня
        MenuView.IsVisible = false;
        LevelView.IsVisible = true;
        Avalonia.Threading.Dispatcher.UIThread.Post(
            LevelView.StartLevel,
            Avalonia.Threading.DispatcherPriority.Render);
    }

    private void CloseLevel()
    {
        LevelView.StopLevel();
        LevelView.IsVisible = false;
        MenuView.IsVisible = true;
        StatusText.Text = "Головне меню";
    }

    private void OnContinueClick(object? sender, RoutedEventArgs e)
    {
        StatusText.Text = "Продовжити: завантаження збереження…";
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        StatusText.Text = "Гру збережено.";
    }

    private void OnWhatsNewClick(object? sender, RoutedEventArgs e)
    {
        StatusText.Text = "Що нового: головне меню готове.";
    }

    private void OnStubClick(object? sender, RoutedEventArgs e)
    {
        StatusText.Text = "Налаштування: музика / звук / Facebook.";
    }

    private void OnLangClick(object? sender, RoutedEventArgs e)
    {
        // Перемикач іконки EN <-> RU з папки А2
        _isEnglish = !_isEnglish;
        var uri = _isEnglish
            ? new Uri("avares://VetaleVSRakshasasGame/Assets/Menu/btn_lang_en.png")
            : new Uri("avares://VetaleVSRakshasasGame/Assets/Menu/btn_lang_ru.png");
        if (sender is Button b && b.Content is Image img)
            img.Source = new Bitmap(Avalonia.Platform.AssetLoader.Open(uri));
        StatusText.Text = _isEnglish ? "Language: English" : "Язык: Русский";
    }
}
