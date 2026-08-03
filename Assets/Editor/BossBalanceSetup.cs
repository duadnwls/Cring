using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 보스 난이도 상향. 씬에 이미 저장된 컴포넌트 값은 코드 기본값을 바꿔도 갱신되지 않으므로
/// 여기서 직접 덮어쓴다.
/// </summary>
public static class BossBalanceSetup
{
    [MenuItem("Tools/Apply Hard Boss Balance")]
    public static void Apply()
    {
        var boss = GameObject.Find("Boss");
        if (boss == null)
        {
            Debug.LogError("Boss를 찾을 수 없습니다.");
            return;
        }

        // 체력
        var health = boss.GetComponent<Health>();
        var healthSO = new SerializedObject(health);
        float oldHp = healthSO.FindProperty("maxHealth").floatValue;
        healthSO.FindProperty("maxHealth").floatValue = 320f;
        healthSO.ApplyModifiedPropertiesWithoutUndo();

        // 이동 속도 (추적이 굼뜨면 그냥 도망다니면 되는 게임이 된다)
        var agent = boss.GetComponent<NavMeshAgent>();
        float oldSpeed = agent.speed;
        agent.speed = 3.4f;
        agent.acceleration = 24f;
        agent.angularSpeed = 720f;

        // 전투 수치
        var ai = boss.GetComponent<BossAI>();
        var aiSO = new SerializedObject(ai);
        aiSO.FindProperty("swipeDamage").floatValue = 22f;
        aiSO.FindProperty("punchDamage").floatValue = 34f;
        aiSO.FindProperty("attackLunge").floatValue = 2.2f;
        // 밀착한 플레이어가 사각지대에 빠지지 않도록 판정 구체를 보스 쪽으로 당기고 키운다
        aiSO.FindProperty("hitReach").floatValue = 1.1f;
        aiSO.FindProperty("hitRadius").floatValue = 1.9f;
        aiSO.FindProperty("aggroRange").floatValue = 14f;
        aiSO.FindProperty("turnSpeed").floatValue = 9f;
        aiSO.FindProperty("cooldownRange").vector2Value = new Vector2(0.55f, 1.15f);
        aiSO.FindProperty("comboRange").vector2IntValue = new Vector2Int(1, 2);
        aiSO.FindProperty("comboGap").floatValue = 0.18f;
        aiSO.FindProperty("phase2Threshold").floatValue = 0.5f;
        aiSO.FindProperty("phase2ComboRange").vector2IntValue = new Vector2Int(2, 4);
        aiSO.FindProperty("phase2CooldownRange").vector2Value = new Vector2(0.3f, 0.7f);
        aiSO.FindProperty("phase2SpeedMultiplier").floatValue = 1.35f;
        aiSO.FindProperty("phase2DamageMultiplier").floatValue = 1.2f;
        aiSO.ApplyModifiedPropertiesWithoutUndo();

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"보스 강화 적용:\n" +
                  $"  체력 {oldHp} → 320 (플레이어 공격 20 기준 16대)\n" +
                  $"  이동 속도 {oldSpeed} → 3.4 (2페이즈 4.6)\n" +
                  $"  데미지 휘두르기 22 / 펀치 34 (2페이즈 ×1.2 → 26/41)\n" +
                  $"  연속 공격 1~2타, 2페이즈 2~4타\n" +
                  $"  체력 50% 이하에서 광폭화");
    }
}
