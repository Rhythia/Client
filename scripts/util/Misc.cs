using System;
using System.Collections.Generic;
using System.IO;
using Godot;

namespace Util
{
    public class Misc
    {
        public static GodotObject OBJParser = (GodotObject)GD.Load<GDScript>("res://scripts/util/OBJParser.gd").New();

        public static ImageTexture GetModIcon(string mod)
        {
            ImageTexture tex;

            switch (mod)
            {
                case "NoFail":
                    tex = SkinManager.Instance.Skin.ModNoFailImage;
                    break;
                case "Ghost":
                    tex = SkinManager.Instance.Skin.ModGhostImage;
                    break;
                case "HardRock":
                    tex = SkinManager.Instance.Skin.ModHardRockImage;
                    break;
                default:
                    tex = new();
                    break;
            }

            return tex;
        }

        public static void CopyProperties(Node node, Node reference)
        {
            foreach (Godot.Collections.Dictionary property in reference.GetPropertyList())
            {
                string key = (string)property["name"];

                if (key == "size" || key == "script")
                {
                    continue;
                }

                node.Set(key, reference.Get(key));
            }
        }

        public static void CopyReference(Node node, Node reference)
        {
            Util.Misc.CopyProperties(node, reference);

            reference.ReplaceBy(node);
            reference.QueueFree();
        }

        public static Image LoadImageFromBuffer(byte[] buffer)
        {
            Image img = new Image();
            foreach (var load in new Func<byte[], Error>[] {
                img.LoadPngFromBuffer,
                img.LoadJpgFromBuffer,
                img.LoadWebpFromBuffer,
                img.LoadBmpFromBuffer,
            })
            {
                if (load(buffer) == Error.Ok)
                    return img;
            }
            return null;
        }

        public static string GetRank(double accuracy)
        {
            if (accuracy >= 100) return "SS";
            if (accuracy >= 98) return "S";
            if (accuracy >= 95) return "A";
            if (accuracy >= 90) return "B";
            if (accuracy >= 85) return "C";
            if (accuracy >= 80) return "D";
            return "F";
        }

        public static Color GetRankColor(string rank)
        {
            return rank switch
            {
                "SS" => Color.Color8(255, 105, 180), // Hot Pink
                "S" => Color.Color8(255, 215, 0),    // Gold
                "A" => Color.Color8(50, 205, 50),    // Lime Green
                "B" => Color.Color8(30, 144, 255),   // Dodger Blue
                "C" => Color.Color8(255, 255, 0),    // Yellow
                "D" => Color.Color8(255, 165, 0),    // Orange
                _ => Color.Color8(255, 69, 0),       // Red-Orange (F)
            };
        }
    }
}
