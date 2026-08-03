using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Animator 그래프 창이 삭제된 상태(state)를 가리키는 낡은 그래프를 붙들고 있으면
/// 스크립트 재컴파일마다 UnityEditor.Graphs.Edge.WakeUp에서 NullReferenceException이 난다.
/// 게임에는 영향이 없지만 콘솔이 지저분해지므로 창과 남은 그래프 객체를 정리한다.
/// </summary>
public static class AnimatorWindowCleaner
{
    [MenuItem("Tools/Inspect Editor Windows")]
    public static void Inspect()
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== 열려 있는 에디터 창 ===");
        foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            sb.AppendLine($"  {w.GetType().FullName}  (제목: {w.titleContent.text})");

        var graphType = FindType("UnityEditor.Graphs.Graph");
        if (graphType == null)
        {
            sb.AppendLine("\nUnityEditor.Graphs.Graph 타입을 찾지 못했습니다.");
        }
        else
        {
            var graphs = Resources.FindObjectsOfTypeAll(graphType);
            sb.AppendLine($"\n=== 메모리에 남아 있는 Graph 객체: {graphs.Length}개 ===");
            foreach (var g in graphs)
                sb.AppendLine($"  {g.GetType().FullName} (name='{g.name}', hideFlags={g.hideFlags})");
        }

        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Clean Animator Graph Windows")]
    public static void Clean()
    {
        int closedWindows = 0;
        foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
        {
            // AnimatorControllerTool(Animator 창), AnimatorControllerLayerTool 등
            if (w.GetType().FullName.StartsWith("UnityEditor.Graphs."))
            {
                w.Close();
                closedWindows++;
            }
        }

        int destroyedGraphs = 0;
        var graphType = FindType("UnityEditor.Graphs.Graph");
        if (graphType != null)
        {
            foreach (var g in Resources.FindObjectsOfTypeAll(graphType))
            {
                UnityEngine.Object.DestroyImmediate(g);
                destroyedGraphs++;
            }
        }

        Debug.Log($"정리 완료: Animator 계열 창 {closedWindows}개 닫음, 남은 Graph 객체 {destroyedGraphs}개 제거.\n" +
                  "이제 스크립트를 다시 컴파일해도 NullReferenceException이 나지 않아야 합니다.");
    }

    static Type FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(a =>
            {
                try { return a.GetType(fullName); }
                catch { return null; }
            })
            .FirstOrDefault(t => t != null);
    }
}
