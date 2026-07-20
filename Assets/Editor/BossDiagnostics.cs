using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class BossDiagnostics
{
    [MenuItem("Tools/Diagnose Boss")]
    public static void Run()
    {
        var sb = new StringBuilder();

        var boss = GameObject.Find("Boss");
        if (boss == null)
        {
            sb.AppendLine("Boss 오브젝트 없음!");
        }
        else
        {
            sb.AppendLine($"Boss 위치: {boss.transform.position}, 스케일: {boss.transform.localScale}");

            var animator = boss.GetComponent<Animator>();
            if (animator == null)
            {
                sb.AppendLine("Animator 컴포넌트 없음!");
            }
            else
            {
                sb.AppendLine($"Animator.enabled: {animator.enabled}");
                sb.AppendLine($"Controller: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "없음!!")}");
                sb.AppendLine($"Avatar: {(animator.avatar != null ? animator.avatar.name : "없음!!")}");
                if (animator.avatar != null)
                {
                    sb.AppendLine($"Avatar.isValid: {animator.avatar.isValid}");
                    sb.AppendLine($"Avatar.isHuman: {animator.avatar.isHuman}");
                }
                sb.AppendLine($"CullingMode: {animator.cullingMode}");

                if (animator.runtimeAnimatorController != null)
                {
                    sb.AppendLine("클립 목록:");
                    foreach (var clip in animator.runtimeAnimatorController.animationClips)
                        sb.AppendLine($"  - {clip.name}: 길이 {clip.length:F2}s, humanMotion={clip.humanMotion}, loop={clip.isLooping}");
                }
            }

            var smr = boss.GetComponentInChildren<SkinnedMeshRenderer>();
            sb.AppendLine($"SkinnedMeshRenderer: {(smr != null ? smr.name + " (bones: " + smr.bones.Length + ")" : "없음!!")}");

            sb.AppendLine("자식 오브젝트(1단계):");
            foreach (Transform child in boss.transform)
                sb.AppendLine($"  - {child.name}");
        }

        string path = "Temp/boss_diag.txt";
        File.WriteAllText(path, sb.ToString());
        Debug.Log("진단 완료 → " + path + "\n" + sb);
    }
}
