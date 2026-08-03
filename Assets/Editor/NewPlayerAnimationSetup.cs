using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 새로 받은 Sword And Shield 계열 클립으로 플레이어 애니메이션을 교체한다.
/// 기존 PlayerAnimator.controller의 이동 블렌드 트리는 유지하고 전투 상태만 갈아끼운다.
/// </summary>
public static class NewPlayerAnimationSetup
{
    const string NewFolder = "Assets/Animation/Player/NewAnimation";
    const string ControllerPath = "Assets/Animation/PlayerAnimator.controller";

    /// <summary>파일명 뒷부분 → 코드에서 쓰는 짧은 클립 이름</summary>
    static readonly (string match, string shortName, bool loop)[] ClipMap =
    {
        ("Standing Dive Forward",      "Dive",       false),
        ("Sword And Shield Slash (1)", "Slash1",     false), // 1타
        ("Sword And Shield Attack",    "Slash2",     false), // 2타
        ("Sword And Shield Kick",      "Kick",       false), // 예비 (현재 미사용)
        ("Sword And Shield Impact",    "Impact",     false),
        ("Sword And Shield Death",     "SwordDeath", false),
        ("Sword And Shield Idle",      "SwordIdle",  true),
        ("Sword And Shield Jump",      "SwordJump",  false),
    };

    [MenuItem("Tools/Apply New Player Animations")]
    public static void Apply()
    {
        if (!Directory.Exists(NewFolder))
        {
            Debug.LogError("폴더가 없습니다: " + NewFolder);
            return;
        }

        // 1) 임포트 설정 — Humanoid, 짧은 이름, 발 기준 높이
        var clipsByName = new Dictionary<string, AnimationClip>();

        foreach (var file in Directory.GetFiles(NewFolder, "*.fbx"))
        {
            string path = file.Replace('\\', '/');
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            string fileName = Path.GetFileNameWithoutExtension(path);
            var entry = ClipMap.FirstOrDefault(m => fileName.EndsWith(m.match));
            if (entry.shortName == null)
            {
                Debug.LogWarning($"용도를 알 수 없는 파일이라 건너뜁니다: {fileName}");
                continue;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = entry.shortName;
                clip.loopTime = entry.loop;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = false;
                clip.heightFromFeet = true;   // 발이 바닥에 닿도록
                clip.keepOriginalPositionXZ = false;

                // XZ는 Bake Into Pose를 꺼야 제자리 동작이 된다.
                // 켜면 수평 이동이 '포즈 안에 남아' 오브젝트는 가만히 있는데 몸만 앞으로 나간다.
                // 끄면 루트 모션으로 분리되고, Apply Root Motion이 꺼져 있으므로 그대로 버려진다.
                clip.lockRootPositionXZ = false;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            var imported = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.Contains("__preview__"));
            if (imported != null)
                clipsByName[entry.shortName] = imported;
        }

        // 2) 컨트롤러를 건드리기 전에 필요한 클립이 전부 준비됐는지 먼저 확인한다.
        //    중간에 실패하면 상태가 빈 채로 남아 캐릭터가 바닥에 주저앉는다.
        string[] required = { "Slash1", "Slash2", "Dive", "Impact", "SwordDeath" };
        var missing = required.Where(r => !clipsByName.ContainsKey(r)).ToArray();
        if (missing.Length > 0)
        {
            Debug.LogError($"필요한 클립이 없어 중단합니다: {string.Join(", ", missing)}\n" +
                           $"{NewFolder} 안의 파일 이름을 확인하세요. 컨트롤러는 건드리지 않았습니다.");
            return;
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("컨트롤러를 찾을 수 없습니다: " + ControllerPath);
            return;
        }

        var sm = controller.layers[0].stateMachine;

        SetStateMotion(sm, "Attack1", Get(clipsByName, "Slash1"));
        SetStateMotion(sm, "Attack2", Get(clipsByName, "Slash2"));
        SetStateMotion(sm, "Roll", Get(clipsByName, "Dive"));
        SetStateMotion(sm, "Hit", Get(clipsByName, "Impact"));
        SetStateMotion(sm, "Death", Get(clipsByName, "SwordDeath"));

        // 3) 이동 블렌드 트리의 정지 동작을 '검 든 대기'로 교체
        var swordIdle = Get(clipsByName, "SwordIdle");
        if (swordIdle != null)
            ReplaceIdleInBlendTree(sm, swordIdle);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        // 4) 빈 상태(모션 없음)가 남지 않았는지 검증 — 있으면 캐릭터가 바닥에 주저앉는다
        var empty = sm.states
            .Where(s => s.state != null && s.state.motion == null)
            .Select(s => s.state.name)
            .ToArray();
        if (empty.Length > 0)
        {
            Debug.LogError($"모션이 비어 있는 상태가 남았습니다: {string.Join(", ", empty)}\n" +
                           "해당 상태에 들어가면 캐릭터 자세가 무너집니다. 클립 파일을 확인하세요.");
            return;
        }

        Debug.Log($"플레이어 애니메이션 교체 완료 ({clipsByName.Count}개 클립, 빈 상태 없음).\n" +
                  "Play로 공격/구르기/피격을 확인하세요.");
    }

    static AnimationClip Get(Dictionary<string, AnimationClip> map, string name)
    {
        if (map.TryGetValue(name, out var clip)) return clip;
        Debug.LogWarning($"클립을 찾지 못했습니다: {name}");
        return null;
    }

    static void SetStateMotion(AnimatorStateMachine sm, string stateName, Motion motion, bool mirror = false)
    {
        if (motion == null) return;

        var state = sm.states.FirstOrDefault(s => s.state != null && s.state.name == stateName).state;
        if (state == null)
        {
            Debug.LogWarning($"상태를 찾지 못했습니다: {stateName}");
            return;
        }

        state.motion = motion;
        state.mirror = mirror;
        state.speed = 1f;
    }

    /// <summary>속도 0(정지) 위치의 모션만 교체해 걷기/뛰기 블렌딩은 그대로 둔다.</summary>
    static void ReplaceIdleInBlendTree(AnimatorStateMachine sm, AnimationClip idle)
    {
        foreach (var s in sm.states)
        {
            if (s.state?.motion is not BlendTree tree) continue;

            var children = tree.children;
            bool changed = false;
            for (int i = 0; i < children.Length; i++)
            {
                if (Mathf.Approximately(children[i].threshold, 0f))
                {
                    children[i].motion = idle;
                    changed = true;
                }
            }
            if (changed)
            {
                tree.children = children;
                Debug.Log($"'{s.state.name}' 블렌드 트리의 정지 동작을 검 든 대기로 교체했습니다.");
            }
        }
    }
}
