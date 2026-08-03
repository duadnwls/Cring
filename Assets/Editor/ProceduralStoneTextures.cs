using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 다운로드 없이 석재 텍스처(알베도 + 노멀)를 생성한다.
/// 이음매 없이 반복되도록 4방향 블렌딩으로 타일링 가능한 노이즈를 만든다.
/// </summary>
public static class ProceduralStoneTextures
{
    const int Size = 512;
    const string Folder = "Assets/Materials/Generated";

    public static Material CreateStoneMaterial(string name, Color baseColor, float blockScale,
                                               float roughness, float normalStrength)
    {
        Directory.CreateDirectory(Folder);

        var height = new float[Size, Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                // 큰 벽돌 무늬 + 잔주름을 섞는다
                float blocks = BlockPattern(x, y, blockScale);
                float grain = Fbm(x, y, 6f, 4) * 0.35f + Fbm(x, y, 24f, 3) * 0.15f;
                height[x, y] = Mathf.Clamp01(blocks * 0.55f + grain);
            }
        }

        var albedo = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
        var pixels = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float h = height[x, y];
                // 높이에 따라 밝기 변화 + 미세한 색조 얼룩
                float tint = 0.75f + h * 0.5f;
                float blotch = Fbm(x, y, 3f, 3) * 0.18f;
                var c = new Color(
                    Mathf.Clamp01(baseColor.r * tint + blotch * 0.6f),
                    Mathf.Clamp01(baseColor.g * tint + blotch * 0.55f),
                    Mathf.Clamp01(baseColor.b * tint + blotch * 0.5f),
                    1f);
                pixels[y * Size + x] = c;
            }
        }
        albedo.SetPixels32(pixels);
        albedo.Apply();

        var normal = BuildNormalMap(height, normalStrength);

        string albedoPath = $"{Folder}/{name}_Albedo.png";
        string normalPath = $"{Folder}/{name}_Normal.png";
        File.WriteAllBytes(albedoPath, albedo.EncodeToPNG());
        File.WriteAllBytes(normalPath, normal.EncodeToPNG());
        Object.DestroyImmediate(albedo);
        Object.DestroyImmediate(normal);
        AssetDatabase.Refresh();

        var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }

        string matPath = $"{Folder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath));
        mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
        mat.EnableKeyword("_NORMALMAP");
        mat.SetFloat("_Smoothness", 1f - roughness);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    /// <summary>어긋나게 쌓은 벽돌 무늬. 줄눈은 어둡게.</summary>
    static float BlockPattern(int x, int y, float scale)
    {
        float bw = Size / scale;        // 벽돌 너비
        float bh = bw * 0.45f;          // 벽돌 높이
        int row = Mathf.FloorToInt(y / bh);
        float offset = (row % 2 == 0) ? 0f : bw * 0.5f;

        float fx = Mathf.Repeat(x + offset, bw) / bw;
        float fy = Mathf.Repeat(y, bh) / bh;

        // 줄눈(가장자리)에서 0, 벽돌 가운데에서 1
        float edge = Mathf.Min(
            Mathf.SmoothStep(0f, 0.09f, fx) * Mathf.SmoothStep(0f, 0.09f, 1f - fx),
            Mathf.SmoothStep(0f, 0.16f, fy) * Mathf.SmoothStep(0f, 0.16f, 1f - fy));

        // 벽돌마다 조금씩 다른 높이
        float variation = Hash(Mathf.FloorToInt((x + offset) / bw), row) * 0.25f;
        return Mathf.Clamp01(edge * (0.8f + variation));
    }

    static float Hash(int x, int y)
    {
        int n = x * 73856093 ^ y * 19349663;
        return Mathf.Abs((n % 1000) / 1000f);
    }

    /// <summary>이음매 없이 반복되는 프랙탈 노이즈.</summary>
    static float Fbm(int px, int py, float frequency, int octaves)
    {
        float total = 0f, amplitude = 1f, maxValue = 0f;
        for (int i = 0; i < octaves; i++)
        {
            total += TileableNoise(px, py, frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }
        return total / maxValue;
    }

    /// <summary>네 귀퉁이 샘플을 가중 평균해 경계에서 이어지게 만든다.</summary>
    static float TileableNoise(int px, int py, float frequency)
    {
        float x = px / (float)Size * frequency;
        float y = py / (float)Size * frequency;
        float fx = px / (float)Size;
        float fy = py / (float)Size;

        float v00 = Mathf.PerlinNoise(x, y);
        float v10 = Mathf.PerlinNoise(x - frequency, y);
        float v01 = Mathf.PerlinNoise(x, y - frequency);
        float v11 = Mathf.PerlinNoise(x - frequency, y - frequency);

        return v00 * (1 - fx) * (1 - fy) + v10 * fx * (1 - fy) +
               v01 * (1 - fx) * fy + v11 * fx * fy;
    }

    /// <summary>높이맵에서 노멀맵 생성 (소벨 필터).</summary>
    static Texture2D BuildNormalMap(float[,] height, float strength)
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
        var pixels = new Color32[Size * Size];

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float hl = height[(x - 1 + Size) % Size, y];
                float hr = height[(x + 1) % Size, y];
                float hd = height[x, (y - 1 + Size) % Size];
                float hu = height[x, (y + 1) % Size];

                var n = new Vector3((hl - hr) * strength, (hd - hu) * strength, 1f).normalized;
                pixels[y * Size + x] = new Color32(
                    (byte)((n.x * 0.5f + 0.5f) * 255),
                    (byte)((n.y * 0.5f + 0.5f) * 255),
                    (byte)((n.z * 0.5f + 0.5f) * 255),
                    255);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }
}
