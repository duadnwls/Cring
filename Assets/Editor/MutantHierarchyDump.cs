using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MutantHierarchyDump
{
    [MenuItem("Tools/Dump Mutant Hierarchy")]
    public static void Dump()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animation/Boss/Mutant.fbx");
        if (model == null)
        {
            Debug.LogError("Mutant.fbx 로드 실패");
            return;
        }

        var sb = new StringBuilder();
        Walk(model.transform, 0, sb);

        string path = "Temp/mutant_hierarchy.txt";
        File.WriteAllText(path, sb.ToString());
        Debug.Log("계층 덤프 완료 → " + path);
    }

    static void Walk(Transform t, int depth, StringBuilder sb)
    {
        sb.AppendLine(new string(' ', depth * 2) + t.name);
        foreach (Transform child in t)
            Walk(child, depth + 1, sb);
    }
}
