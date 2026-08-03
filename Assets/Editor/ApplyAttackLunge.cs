using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 씬에 이미 붙어 있는 PlayerCombat의 attackLunge 값을 0으로 맞춘다.
/// 코드의 기본값을 바꿔도 이미 저장된 컴포넌트 값은 자동으로 갱신되지 않기 때문에 필요하다.
/// </summary>
public static class ApplyAttackLunge
{
    [MenuItem("Tools/Set Attack To Stationary")]
    public static void SetStationary()
    {
        var player = GameObject.Find("PlayerArmature");
        if (player == null)
        {
            Debug.LogError("PlayerArmature를 찾을 수 없습니다.");
            return;
        }

        var combat = player.GetComponent<PlayerCombat>();
        if (combat == null)
        {
            Debug.LogError("PlayerCombat 컴포넌트가 없습니다.");
            return;
        }

        var so = new SerializedObject(combat);
        var prop = so.FindProperty("attackLunge");
        float before = prop.floatValue;
        prop.floatValue = 0f;
        so.ApplyModifiedPropertiesWithoutUndo();

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"공격 전진 속도 {before} → 0 (제자리 공격).\n" +
                  "다시 앞으로 나가게 하려면 PlayerArmature > PlayerCombat > Attack Lunge 값을 올리세요.");
    }
}
