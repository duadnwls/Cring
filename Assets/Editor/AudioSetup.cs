using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Assets/Audio 폴더의 파일을 이름으로 알아보고 GameAudio에 자동 연결한다.
/// 파일이 없는 항목은 비워두며, 비어 있어도 게임은 정상 동작한다.
/// </summary>
public static class AudioSetup
{
    const string AudioFolder = "Assets/Audio";
    const string ArenaScenePath = "Assets/Scenes/Arena.unity";

    /// <summary>GameAudio의 필드 이름 → 파일명에 들어 있어야 할 키워드</summary>
    static readonly (string field, string[] keywords)[] Mapping =
    {
        ("playerSwing", new[] { "swing", "swoosh", "whoosh" }),
        ("playerHit",   new[] { "hit", "impact", "slash" }),
        ("playerHurt",  new[] { "hurt", "pain", "grunt" }),
        ("playerRoll",  new[] { "roll", "dodge" }),
        ("playerDeath", new[] { "player_death", "death_player" }),
        ("bossRoar",    new[] { "roar", "growl" }),
        ("bossSwing",   new[] { "boss_swing", "monster_attack" }),
        ("bossHurt",    new[] { "boss_hurt", "monster_hurt" }),
        ("bossDeath",   new[] { "boss_death", "monster_death" }),
        ("bgm",         new[] { "bgm", "music", "theme", "loop", "ambient", "ost" }),
    };

    /// <summary>파일명의 공백·하이픈을 밑줄로 통일해서 키워드가 걸리게 한다.</summary>
    static string Normalize(string name) => name.ToLower().Replace(' ', '_').Replace('-', '_');

    [MenuItem("Tools/Setup Audio")]
    public static void Setup()
    {
        Directory.CreateDirectory(AudioFolder);
        AssetDatabase.Refresh();

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.GetActiveScene().path == ArenaScenePath
            ? EditorSceneManager.GetActiveScene()
            : EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

        var go = GameObject.Find("GameAudio");
        if (go == null) go = new GameObject("GameAudio");
        var audio = go.GetComponent<GameAudio>();
        if (audio == null) audio = go.AddComponent<GameAudio>();

        // 폴더 안의 오디오 클립을 전부 모은다
        var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(p => (path: p, clip: AssetDatabase.LoadAssetAtPath<AudioClip>(p)))
            .Where(x => x.clip != null)
            .ToList();

        var so = new SerializedObject(audio);
        var assigned = new List<string>();
        var empty = new List<string>();
        var used = new HashSet<string>();

        foreach (var (field, keywords) in Mapping)
        {
            var prop = so.FindProperty(field);
            if (prop == null) continue;

            // 아직 안 쓴 클립 중에서 키워드가 파일명에 들어 있는 것을 찾는다
            var match = clips.FirstOrDefault(c =>
                !used.Contains(c.path) &&
                keywords.Any(k => Normalize(Path.GetFileNameWithoutExtension(c.path)).Contains(k)));

            if (match.clip != null)
            {
                prop.objectReferenceValue = match.clip;
                used.Add(match.path);
                assigned.Add($"{field} ← {Path.GetFileName(match.path)}");
            }
            else if (prop.objectReferenceValue == null)
            {
                empty.Add(field);
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        string report = $"오디오 연결 완료 ({assigned.Count}개)\n";
        if (assigned.Count > 0) report += "  " + string.Join("\n  ", assigned) + "\n";
        if (empty.Count > 0) report += $"비어 있음(소리 없이 동작): {string.Join(", ", empty)}";
        Debug.Log(report);
    }
}
