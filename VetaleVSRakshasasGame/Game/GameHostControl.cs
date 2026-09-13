using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VetaleVSRakshasasGame.Game;

/// <summary>
/// Нативний хост MonoGame — як WebView2 у браузері: створює дочірній HWND
/// всередині сірого вікна Avalonia і малює DirectX прямо в нього.
/// Окремого вікна, таскбара і Alt+Tab не існує взагалі.
/// </summary>
public sealed class GameHostControl : NativeControlHost
{
    private const uint WS_CHILD = 0x40000000;
    private const uint WS_VISIBLE = 0x10000000;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowEx(
        int exStyle, string className, string name, uint style,
        int x, int y, int w, int h, nint parent, nint menu, nint instance, nint param);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT cursor);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_LBUTTON = 0x01;
    private const int VK_ESCAPE = 0x1B;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public event Action<int, int>? Finished; // рахунок, фраги
    public bool Running { get; private set; }

    private nint _child;
    private GraphicsDevice? _device;
    private DefenseGame? _game;
    private DispatcherTimer? _timer;
    private readonly Stopwatch _sw = new();
    private bool _prevLeft;
    private double _hudTimer;
    private int _bw;
    private int _bh;

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        _child = CreateWindowEx(0, "STATIC", "", WS_CHILD | WS_VISIBLE,
            0, 0, 8, 8, parent.Handle, nint.Zero, nint.Zero, nint.Zero);
        return new PlatformHandle(_child, "HWND");
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        StopInternal();
        _game?.Dispose();
        _game = null;
        _device?.Dispose();
        _device = null;
        if (_child != nint.Zero)
        {
            DestroyWindow(_child);
            _child = nint.Zero;
        }
    }

    public void Start()
    {
        if (Running || _child == nint.Zero)
            return;
        double scaling = (VisualRoot as TopLevel)?.RenderScaling ?? 1.0;
        int w = Math.Max(320, (int)(Bounds.Width * scaling));
        int h = Math.Max(200, (int)(Bounds.Height * scaling));
        if (_device is null || _bw != w || _bh != h)
        {
            _game?.Dispose();
            _device?.Dispose();
            _bw = w;
            _bh = h;
            var pp = new PresentationParameters
            {
                BackBufferWidth = w,
                BackBufferHeight = h,
                DeviceWindowHandle = _child,
            };
            _device = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, pp);
            var dir = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Game");
            _game = new DefenseGame(dir, w, h);
            _game.Load(_device);
        }
        _game!.Reset();
        _prevLeft = false;
        _hudTimer = 0;
        Running = true;
        _sw.Restart();
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Normal, (_, _) => Tick());
        _timer.Start();
    }

    public void Stop()
    {
        StopInternal();
    }

    private void StopInternal()
    {
        Running = false;
        _timer?.Stop();
        _timer = null;
        _sw.Stop();
    }

    private void Tick()
    {
        if (!Running || _device is null || _game is null || _child == nint.Zero)
            return;
        double dt = Math.Min(_sw.Elapsed.TotalSeconds, 0.05);
        _sw.Restart();

        // Миша напряму через Win32 (екранні координати -> клієнтські -> дизайн-кадр).
        // MonoGame Mouse без Game-вікна дає невірні значення, тому його не використовуємо.
        GetCursorPos(out var cursor);
        GetWindowRect(_child, out var rc);
        float s = Math.Min(_bw / 1280f, _bh / 720f);
        float ox = (_bw - 1280f * s) / 2f;
        float oy = (_bh - 720f * s) / 2f;
        var aim = new Vector2((cursor.X - rc.Left - ox) / s, (cursor.Y - rc.Top - oy) / s);
        bool left = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
        bool clicked = left && !_prevLeft;
        _prevLeft = left;

        try
        {
            _game.Update((float)dt, aim, clicked);
            _game.Draw(aim);
        }
        catch
        {
            // втрата пристрою — наступний тік спробує знову
            return;
        }

        var ks = (GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0;
        if (ks || _game.IsOver)
        {
            int score = _game.Score, kills = _game.Kills;
            StopInternal();
            Finished?.Invoke(score, kills);
            return;
        }

        _hudTimer -= dt;
        if (_hudTimer <= 0)
        {
            _hudTimer = 0.15;
            HudChanged?.Invoke(_game.Score, _game.Hp, _game.Stone, _game.Wave);
        }
    }

    public event Action<int, float, float, int>? HudChanged; // рахунок, HP, камінь, хвиля
}
