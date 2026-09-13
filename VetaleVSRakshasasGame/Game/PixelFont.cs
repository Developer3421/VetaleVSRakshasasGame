using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace VetaleVSRakshasasGame.Game;

/// <summary>Мінімальний піксельний шрифт 3x5 для HUD всередині рівня.</summary>
internal static class PixelFont
{
    private static readonly Dictionary<char, string[]> G = new()
    {
        ['A'] = new[] { "010", "101", "111", "101", "101" },
        ['B'] = new[] { "110", "101", "110", "101", "110" },
        ['C'] = new[] { "011", "100", "100", "100", "011" },
        ['D'] = new[] { "110", "101", "101", "101", "110" },
        ['E'] = new[] { "111", "100", "110", "100", "111" },
        ['F'] = new[] { "111", "100", "110", "100", "100" },
        ['G'] = new[] { "011", "100", "101", "101", "011" },
        ['H'] = new[] { "101", "101", "111", "101", "101" },
        ['I'] = new[] { "111", "010", "010", "010", "111" },
        ['J'] = new[] { "001", "001", "001", "101", "010" },
        ['K'] = new[] { "101", "101", "110", "101", "101" },
        ['L'] = new[] { "100", "100", "100", "100", "111" },
        ['M'] = new[] { "101", "111", "111", "101", "101" },
        ['N'] = new[] { "110", "101", "101", "101", "101" },
        ['O'] = new[] { "010", "101", "101", "101", "010" },
        ['P'] = new[] { "110", "101", "110", "100", "100" },
        ['Q'] = new[] { "010", "101", "101", "110", "011" },
        ['R'] = new[] { "110", "101", "110", "101", "101" },
        ['S'] = new[] { "011", "100", "010", "001", "110" },
        ['T'] = new[] { "111", "010", "010", "010", "010" },
        ['U'] = new[] { "101", "101", "101", "101", "111" },
        ['V'] = new[] { "101", "101", "101", "101", "010" },
        ['W'] = new[] { "101", "101", "101", "111", "101" },
        ['X'] = new[] { "101", "101", "010", "101", "101" },
        ['Y'] = new[] { "101", "101", "010", "010", "010" },
        ['Z'] = new[] { "111", "001", "010", "100", "111" },
        ['0'] = new[] { "111", "101", "101", "101", "111" },
        ['1'] = new[] { "010", "110", "010", "010", "111" },
        ['2'] = new[] { "111", "001", "111", "100", "111" },
        ['3'] = new[] { "111", "001", "111", "001", "111" },
        ['4'] = new[] { "101", "101", "111", "001", "001" },
        ['5'] = new[] { "111", "100", "111", "001", "111" },
        ['6'] = new[] { "111", "100", "111", "101", "111" },
        ['7'] = new[] { "111", "001", "001", "010", "010" },
        ['8'] = new[] { "111", "101", "111", "101", "111" },
        ['9'] = new[] { "111", "101", "111", "001", "111" },
        [':'] = new[] { "000", "010", "000", "010", "000" },
        ['-'] = new[] { "000", "000", "111", "000", "000" },
        [' '] = new[] { "000", "000", "000", "000", "000" },
    };

    public static int Measure(string text, int scale) => text.Length * 4 * scale - scale;

    public static void Draw(SpriteBatch batch, Texture2D pixel, string text, Microsoft.Xna.Framework.Vector2 pos, int scale, Microsoft.Xna.Framework.Color color)
    {
        text = text.ToUpperInvariant();
        DrawInternal(batch, pixel, text, pos + new Microsoft.Xna.Framework.Vector2(scale, scale), scale, Microsoft.Xna.Framework.Color.Black * 0.7f);
        DrawInternal(batch, pixel, text, pos, scale, color);
    }

    private static void DrawInternal(SpriteBatch batch, Texture2D pixel, string text, Microsoft.Xna.Framework.Vector2 pos, int scale, Microsoft.Xna.Framework.Color color)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (!G.TryGetValue(text[i], out var glyph))
                continue;
            for (int r = 0; r < 5; r++)
            for (int c = 0; c < 3; c++)
            {
                if (glyph[r][c] == '1')
                    batch.Draw(pixel,
                        new Microsoft.Xna.Framework.Rectangle((int)pos.X + i * 4 * scale + c * scale, (int)pos.Y + r * scale, scale, scale),
                        color);
            }
        }
    }
}
