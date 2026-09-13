using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace VetaleVSRakshasasGame.Game;

internal sealed class Enemy
{
    public Vector2 Pos;
    public bool IsGreen;
    public float Speed;
    public int Hp;
    public int MaxHp;
    public float HitFlash;
    public float FireTimer;
    public float StopX;
}

internal sealed class Bullet
{
    public Vector2 Pos;
    public Vector2 Vel;
}

internal sealed class EnemyBullet
{
    public Vector2 Pos;
    public Vector2 Vel;
}

internal sealed class Fx
{
    public Texture2D Tex = null!;
    public Vector2 Pos;
    public float Ttl;
    public float Max;
    public float Size;
}

/// <summary>
/// Оборона рівня 2. Малює в GraphicsDevice, прив'язаний до дочірнього HWND
/// всередині NativeControlHost — окремого вікна не існує.
/// </summary>
internal sealed class DefenseGame : IDisposable
{
    private const int DW = 1280;
    private const int DH = 720;
    private const float GroundY = 604f;

    private static readonly Vector2 VetaleFeet = new(200, GroundY);
    private const float VetaleHeight = 260f;
    private const float HandX = 0.55f;
    private const float HandY = 0.45f;
    private const float GunLen = 150f;

    private static readonly Vector2 StoneFeet = new(370, GroundY + 4);
    private const float StoneHeight = 200f;
    private const float StoneMaxHp = 120f;

    public int Score => _score;
    public int Kills => _kills;
    public float Hp => _hp;
    public float Stone => Math.Max(0, _stoneHp);
    public int Wave => 1 + (int)(_elapsed / 25f);
    public bool IsOver { get; private set; }

    private readonly string _assetsDir;
    private readonly float _s;
    private readonly float _ox;
    private readonly float _oy;
    private GraphicsDevice? _device;
    private SpriteBatch? _batch;
    private bool _disposed;

    private Texture2D? _bg;
    private Texture2D? _body;
    private Texture2D? _vetaleGun;
    private Texture2D? _green;
    private Texture2D? _yellow;
    private Texture2D? _stone;
    private Texture2D? _boomHit;
    private Texture2D? _boomDie;
    private Texture2D? _coin;
    private Texture2D? _hpbar;
    private Texture2D? _pixel;
    private Texture2D? _circle;

    private readonly List<Enemy> _enemies = new();
    private readonly List<Bullet> _bullets = new();
    private readonly List<EnemyBullet> _enemyBullets = new();
    private readonly List<Fx> _fx = new();
    private readonly Random _rnd = new();
    private float _spawnTimer = 1f;
    private float _fireCooldown;
    private float _muzzleFlash;
    private Vector2 _muzzlePos;
    private float _elapsed;
    private int _score;
    private int _kills;
    private float _hp = 100f;
    private float _stoneHp = StoneMaxHp;

    public DefenseGame(string assetsDir, int backWidth, int backHeight)
    {
        _assetsDir = assetsDir;
        _s = Math.Min(backWidth / (float)DW, backHeight / (float)DH);
        _ox = (backWidth - DW * _s) / 2f;
        _oy = (backHeight - DH * _s) / 2f;
    }

    public void Load(GraphicsDevice device)
    {
        _device = device;
        _batch = new SpriteBatch(device);
        _bg = Load("bg_scene2.jpg");
        _body = Load("vetale_body.png");
        _vetaleGun = Load("gun_vetale.png");
        _green = Load("rakshasa_green.png");
        _yellow = Load("enemy_yellow.png");
        _stone = Load("stone.png");
        _boomHit = Load("boom_hit.png");
        _boomDie = Load("boom_die.png");
        _coin = Load("coin.png");
        _hpbar = Load("hpbar.png");

        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _circle = new Texture2D(device, 64, 64);
        var data = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            float dx = (x - 32) / 32f, dy = (y - 32) / 32f;
            data[y * 64 + x] = dx * dx + dy * dy <= 1f ? Color.White : Color.Transparent;
        }
        _circle.SetData(data);
    }

    private Texture2D Load(string name)
    {
        using var fs = File.OpenRead(Path.Combine(_assetsDir, name));
        return Texture2D.FromStream(_device!, fs);
    }

    private Vector2 P(float x, float y) => new(_ox + x * _s, _oy + y * _s);
    private Rectangle R(float x, float y, float w, float h) =>
        new((int)(_ox + x * _s), (int)(_oy + y * _s), Math.Max(1, (int)(w * _s)), Math.Max(1, (int)(h * _s)));

    public void Reset()
    {
        _enemies.Clear();
        _bullets.Clear();
        _enemyBullets.Clear();
        _fx.Clear();
        _elapsed = 0;
        _score = 0;
        _kills = 0;
        _hp = 100;
        _stoneHp = StoneMaxHp;
        _spawnTimer = 1;
        _fireCooldown = 0;
        IsOver = false;
    }

    public void Update(float dt, Vector2 aim, bool clicked)
    {
        if (IsOver) return;
        _elapsed += dt;
        int wave = Wave;
        float speedMul = 1f + wave * 0.06f;

        _spawnTimer -= dt;
        int maxEnemies = Math.Min(2 + wave / 2, 6);
        if (_spawnTimer <= 0 && _enemies.Count < maxEnemies)
        {
            _spawnTimer = Math.Max(0.9f, 2.2f - wave * 0.15f);
            bool green = _rnd.Next(2) == 0;
            int hp = green ? 3 : 2;
            _enemies.Add(new Enemy
            {
                Pos = new Vector2(DW + 60, GroundY + _rnd.Next(-8, 9)),
                IsGreen = green,
                Speed = (green ? _rnd.Next(50, 70) : _rnd.Next(80, 105)) * speedMul,
                Hp = hp,
                MaxHp = hp,
                FireTimer = _rnd.NextSingle() * 2f + 1f,
                StopX = 700f + _rnd.NextSingle() * 200f,
            });
        }

        _fireCooldown -= dt;
        _muzzleFlash -= dt;
        if (clicked && _fireCooldown <= 0)
        {
            _fireCooldown = 0.18f;
            _muzzleFlash = 0.06f;
            var pivot = GunPivot();
            var dir = aim - pivot;
            if (dir == Vector2.Zero) dir = Vector2.UnitX;
            dir.Normalize();
            _muzzlePos = pivot + dir * GunLen;
            _bullets.Add(new Bullet { Pos = _muzzlePos, Vel = dir * 950f });
        }

        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            var b = _bullets[i];
            b.Pos += b.Vel * dt;
            bool dead = b.Pos.X < -20 || b.Pos.X > DW + 20 || b.Pos.Y < -20 || b.Pos.Y > DH + 20;
            if (!dead)
            {
                for (int j = _enemies.Count - 1; j >= 0; j--)
                {
                    var e = _enemies[j];
                    if (Vector2.Distance(b.Pos, e.Pos - new Vector2(0, 62)) < 54f)
                    {
                        dead = true;
                        e.Hp--;
                        e.HitFlash = 0.09f;
                        e.Pos.X += 14f;
                        if (_boomHit is not null)
                            _fx.Add(new Fx { Tex = _boomHit, Pos = b.Pos, Ttl = 0.09f, Max = 0.09f, Size = 70f });
                        if (e.Hp <= 0)
                        {
                            _enemies.RemoveAt(j);
                            _kills++;
                            _score += e.IsGreen ? 20 : 30;
                            if (_boomDie is not null)
                                _fx.Add(new Fx { Tex = _boomDie, Pos = e.Pos - new Vector2(0, 62), Ttl = 0.3f, Max = 0.3f, Size = 140f });
                        }
                        break;
                    }
                }
            }
            if (dead) _bullets.RemoveAt(i);
        }

        bool stoneAlive = _stoneHp > 0;
        var stoneRect = new Rectangle(257, 408, 226, 200);
        var vetaleCenter = VetaleFeet - new Vector2(0, 130);
        foreach (var e in _enemies)
        {
            e.HitFlash -= dt;
            if (e.IsGreen)
            {
                float stopX = stoneAlive ? 492f : 252f;
                if (e.Pos.X > stopX)
                    e.Pos.X -= e.Speed * dt;
                else if (stoneAlive)
                    _stoneHp -= 10f * dt;
                else
                {
                    _hp -= 15f * dt;
                    if (_hp <= 0)
                    {
                        IsOver = true;
                        return;
                    }
                }
            }
            else if (e.Pos.X > e.StopX)
            {
                e.Pos.X -= e.Speed * dt;
            }
            else
            {
                e.FireTimer -= dt;
                if (e.FireTimer <= 0)
                {
                    e.FireTimer = 1.6f + _rnd.NextSingle() * 1.4f;
                    var from = e.Pos - new Vector2(50, 72);
                    var d = vetaleCenter - from;
                    if (d == Vector2.Zero) d = -Vector2.UnitX;
                    d.Normalize();
                    float spread = (_rnd.NextSingle() - 0.5f) * 0.12f;
                    float cs = (float)Math.Cos(spread), sn = (float)Math.Sin(spread);
                    d = new Vector2(d.X * cs - d.Y * sn, d.X * sn + d.Y * cs);
                    _enemyBullets.Add(new EnemyBullet { Pos = from, Vel = d * 320f });
                }
            }
        }

        for (int i = _enemyBullets.Count - 1; i >= 0; i--)
        {
            var b = _enemyBullets[i];
            b.Pos += b.Vel * dt;
            bool dead = b.Pos.X < -20 || b.Pos.X > DW + 20 || b.Pos.Y < -20 || b.Pos.Y > DH + 20;
            if (!dead && stoneAlive && stoneRect.Contains((int)b.Pos.X, (int)b.Pos.Y))
            {
                dead = true;
                _stoneHp -= 6f;
                if (_boomHit is not null)
                    _fx.Add(new Fx { Tex = _boomHit, Pos = b.Pos, Ttl = 0.09f, Max = 0.09f, Size = 60f });
            }
            else if (!dead && Vector2.Distance(b.Pos, vetaleCenter) < 62f)
            {
                dead = true;
                _hp -= 8f;
                if (_boomHit is not null)
                    _fx.Add(new Fx { Tex = _boomHit, Pos = b.Pos, Ttl = 0.09f, Max = 0.09f, Size = 60f });
                if (_hp <= 0)
                {
                    IsOver = true;
                    return;
                }
            }
            if (dead) _enemyBullets.RemoveAt(i);
        }

        for (int i = _enemies.Count - 1; i >= 0; i--)
            if (_enemies[i].Pos.X < -60)
                _enemies.RemoveAt(i);

        for (int i = 0; i < _enemies.Count; i++)
        for (int j = i + 1; j < _enemies.Count; j++)
        {
            var a = _enemies[i].Pos;
            var b = _enemies[j].Pos;
            float d = Vector2.Distance(a, b);
            if (d > 0.01f && d < 56f)
            {
                var push = (a - b) / d * 34f * dt;
                _enemies[i].Pos += push;
                _enemies[j].Pos -= push;
            }
        }

        for (int i = _fx.Count - 1; i >= 0; i--)
        {
            _fx[i].Ttl -= dt;
            if (_fx[i].Ttl <= 0) _fx.RemoveAt(i);
        }
    }

    private Vector2 GunPivot()
    {
        if (_body is null) return VetaleFeet - new Vector2(0, 120);
        float s = VetaleHeight / _body.Height;
        float bw = _body.Width * s;
        float left = VetaleFeet.X - bw / 2f;
        float top = VetaleFeet.Y - VetaleHeight;
        return new Vector2(left + bw * HandX, top + VetaleHeight * HandY);
    }

    public void Draw(Vector2 aim)
    {
        if (_device is null || _batch is null || _bg is null || _body is null || _vetaleGun is null ||
            _green is null || _yellow is null || _stone is null || _pixel is null || _circle is null ||
            _coin is null || _hpbar is null)
            return;

        var pivot = GunPivot();
        var dir = aim - pivot;
        if (dir == Vector2.Zero) dir = Vector2.UnitX;
        dir.Normalize();
        float angle = (float)Math.Atan2(dir.Y, dir.X);
        bool flip = dir.X < 0;

        _device.Clear(Color.Black);
        _batch.Begin();

        float bs = Math.Max(DW / (float)_bg.Width, DH / (float)_bg.Height) * _s;
        _batch.Draw(_bg, P(DW / 2f, DH / 2f), null, Color.White, 0f,
            new Vector2(_bg.Width / 2f, _bg.Height / 2f), bs, SpriteEffects.None, 0f);

        float s = VetaleHeight / _body.Height * _s;
        _batch.Draw(_body, P(VetaleFeet.X, VetaleFeet.Y), null, Color.White, 0f,
            new Vector2(_body.Width / 2f, _body.Height), s, SpriteEffects.None, 0f);

        float gs = 58f / _vetaleGun.Height * _s;
        _batch.Draw(_vetaleGun, P(pivot.X, pivot.Y), null, Color.White, angle,
            new Vector2(_vetaleGun.Width * 0.28f, _vetaleGun.Height / 2f), gs,
            flip ? SpriteEffects.FlipVertically : SpriteEffects.None, 0f);

        if (_muzzleFlash > 0)
            _batch.Draw(_circle, P(_muzzlePos.X, _muzzlePos.Y), null, Color.Yellow, 0f,
                new Vector2(32, 32), 0.55f * _s, SpriteEffects.None, 0f);

        foreach (var e in _enemies)
        {
            var tex = e.IsGreen ? _green : _yellow;
            float es = (e.IsGreen ? 150f : 132f) / tex.Height * _s;
            var tint = e.HitFlash > 0 ? Color.Red : Color.White;
            var center = e.Pos - new Vector2(0, 62);
            _batch.Draw(tex, P(e.Pos.X, e.Pos.Y), null, tint, 0f,
                new Vector2(tex.Width / 2f, tex.Height), es, SpriteEffects.None, 0f);
            float frac = Math.Clamp(e.Hp / (float)e.MaxHp, 0f, 1f);
            _batch.Draw(_pixel, R(center.X - 26, center.Y - 88, 52, 6), Color.Black * 0.7f);
            _batch.Draw(_pixel, R(center.X - 25, center.Y - 87, 50 * frac, 4),
                frac > 0.5f ? Color.LimeGreen : Color.OrangeRed);
        }

        foreach (var b in _bullets)
        {
            var d = b.Vel;
            d.Normalize();
            float a = (float)Math.Atan2(d.Y, d.X);
            _batch.Draw(_pixel, P(b.Pos.X, b.Pos.Y), null, Color.Yellow, a,
                new Vector2(1, 0.5f), new Vector2(16 * _s, 3 * _s), SpriteEffects.None, 0f);
        }
        foreach (var b in _enemyBullets)
        {
            var d = b.Vel;
            d.Normalize();
            float a = (float)Math.Atan2(d.Y, d.X);
            _batch.Draw(_pixel, P(b.Pos.X, b.Pos.Y), null, Color.Red, a,
                new Vector2(1, 0.5f), new Vector2(13 * _s, 3 * _s), SpriteEffects.None, 0f);
        }

        foreach (var f in _fx)
        {
            float grow = 1f + (1f - f.Ttl / f.Max) * 0.6f;
            _batch.Draw(f.Tex, P(f.Pos.X, f.Pos.Y), null, Color.White, 0f,
                new Vector2(f.Tex.Width / 2f, f.Tex.Height / 2f),
                f.Size / f.Tex.Height * grow * _s, SpriteEffects.None, 0f);
        }

        if (_stoneHp > 0)
        {
            float ss = StoneHeight / _stone.Height * _s;
            _batch.Draw(_stone, P(StoneFeet.X, StoneFeet.Y), null, Color.White, 0f,
                new Vector2(_stone.Width / 2f, _stone.Height), ss, SpriteEffects.None, 0f);
            float f = Math.Clamp(_stoneHp / StoneMaxHp, 0f, 1f);
            _batch.Draw(_pixel, R(StoneFeet.X - 55, StoneFeet.Y - StoneHeight - 18, 110, 8), Color.Black * 0.7f);
            _batch.Draw(_pixel, R(StoneFeet.X - 54, StoneFeet.Y - StoneHeight - 17, 108 * f, 6), Color.SlateGray);
        }

        int waveHud = Wave;
        float cs2 = 44f / _coin.Height * _s;
        _batch.Draw(_coin, P(16, 10), null, Color.White, 0f, Vector2.Zero, cs2, SpriteEffects.None, 0f);
        PixelFont.Draw(_batch, _pixel, $"SCORE {_score}", P(70, 14), 4, Color.Gold);
        _batch.Draw(_hpbar, R(16, 62, 220, 13), Color.White);
        float hpFrac = Math.Clamp(_hp / 100f, 0f, 1f);
        _batch.Draw(_pixel, R(24, 65, 204 * hpFrac, 7),
            hpFrac > 0.5f ? Color.LimeGreen : hpFrac > 0.25f ? Color.Orange : Color.Red);
        PixelFont.Draw(_batch, _pixel, $"HP {(int)Math.Max(0, _hp)}", P(246, 60), 3, Color.White);
        string waveTxt = $"WAVE {waveHud}";
        PixelFont.Draw(_batch, _pixel, waveTxt, P(DW - PixelFont.Measure(waveTxt, 4) - 16, 14), 4, Color.White);

        _batch.End();
        _device.Present();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _batch?.Dispose();
        _bg?.Dispose();
        _body?.Dispose();
        _vetaleGun?.Dispose();
        _green?.Dispose();
        _yellow?.Dispose();
        _stone?.Dispose();
        _boomHit?.Dispose();
        _boomDie?.Dispose();
        _coin?.Dispose();
        _hpbar?.Dispose();
        _pixel?.Dispose();
        _circle?.Dispose();
        _batch = null;
    }
}
