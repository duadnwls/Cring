using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// FBX 안에 들어 있는 텍스처와 재질을 밖으로 꺼낸다.
/// Mixamo 모델은 텍스처가 파일 내부에 박혀 있어 그냥 임포트하면 흰색으로 보인다.
/// </summary>
public static class EmbeddedTextureExtractor
{
    [MenuItem("Tools/Extract Textures From Selected FBX")]
    public static void ExtractFromSelection()
    {
        var models = Selection.objects
            .Select(AssetDatabase.GetAssetPath)
            .Where(p => !string.IsNullOrEmpty(p) && p.ToLower().EndsWith(".fbx"))
            .ToArray();

        if (models.Length == 0)
        {
            Debug.LogError("Project 창에서 .fbx 파일을 선택한 뒤 다시 실행하세요.");
            return;
        }

        foreach (var path in models)
            Extract(path);
    }

    [MenuItem("Tools/Extract Player Character Textures")]
    public static void ExtractPlayerCharacter()
    {
        const string folder = "Assets/Animation/Player/Character";
        if (!Directory.Exists(folder))
        {
            Debug.LogError("폴더가 없습니다: " + folder);
            return;
        }

        var fbx = Directory.GetFiles(folder, "*.fbx").FirstOrDefault();
        if (fbx == null)
        {
            Debug.LogError($"{folder} 안에 .fbx가 없습니다.");
            return;
        }

        Extract(fbx.Replace('\\', '/'));
    }

    public static void Extract(string modelPath)
    {
        var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("모델 임포터를 가져올 수 없습니다: " + modelPath);
            return;
        }

        string folder = Path.GetDirectoryName(modelPath).Replace('\\', '/');
        string textureFolder = folder + "/Textures";
        Directory.CreateDirectory(textureFolder);

        if (importer.ExtractTextures(textureFolder))
        {
            AssetDatabase.Refresh();
            Debug.Log("텍스처 추출 → " + textureFolder);
        }
        else
        {
            Debug.LogWarning("추출할 내장 텍스처가 없거나 이미 추출되어 있습니다: " + modelPath);
        }

        // 노멀맵으로 임포트되도록 설정 (안 하면 요철이 이상하게 보인다)
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { textureFolder }))
        {
            string texPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!Path.GetFileName(texPath).ToLower().Contains("normal")) continue;

            var texImporter = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (texImporter != null && texImporter.textureType != TextureImporterType.NormalMap)
            {
                texImporter.textureType = TextureImporterType.NormalMap;
                texImporter.SaveAndReimport();
            }
        }

        // 재질을 FBX 밖으로 빼내 추출된 텍스처를 물게 한다
        importer.materialLocation = ModelImporterMaterialLocation.External;
        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        Debug.Log($"재질 추출 완료: {Path.GetFileName(modelPath)}\n" +
                  "씬의 캐릭터에 색이 입혀졌는지 확인하세요.");
    }
}
