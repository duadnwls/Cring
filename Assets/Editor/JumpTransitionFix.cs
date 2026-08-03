using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 점프를 연타하면 애니메이션이 안 나오는 문제 수정.
/// 원인: JumpLand(착지) 상태에서 나가는 전환이 '대기 상태로' 하나뿐이라,
/// 착지 모션 재생 중에 다시 점프하면 JumpStart로 갈 경로가 없어 그냥 씹힌다.
/// </summary>
public static class JumpTransitionFix
{
    const string ControllerPath = "Assets/Animation/PlayerAnimator.controller";

    [MenuItem("Tools/Fix Jump Spam Animation")]
    public static void Fix()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("컨트롤러를 찾을 수 없습니다: " + ControllerPath);
            return;
        }

        var sm = controller.layers[0].stateMachine;
        var jumpLand = FindState(sm, "JumpLand");
        var jumpStart = FindState(sm, "JumpStart");

        if (jumpLand == null || jumpStart == null)
        {
            Debug.LogError("JumpLand 또는 JumpStart 상태를 찾을 수 없습니다.");
            return;
        }

        // 이미 만들어둔 전환이 있으면 지우고 다시 만든다 (재실행 안전)
        foreach (var t in jumpLand.transitions.Where(t => t.destinationState == jumpStart).ToArray())
            jumpLand.RemoveTransition(t);

        var transition = jumpLand.AddTransition(jumpStart);
        transition.hasExitTime = false;          // 착지 모션이 끝나길 기다리지 않는다
        transition.duration = 0.04f;
        transition.hasFixedDuration = true;
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Jump");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("점프 전환 추가 완료: JumpLand → JumpStart (Jump=true, 대기 없음).\n" +
                  "이제 점프를 연타해도 매번 점프 모션이 나옵니다.");
    }

    static AnimatorState FindState(AnimatorStateMachine sm, string name)
    {
        return sm.states.FirstOrDefault(s => s.state != null && s.state.name == name).state;
    }
}
