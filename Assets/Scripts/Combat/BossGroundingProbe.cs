using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 플레이 중 보스의 실제 발 높이를 측정해 기록한다. 원인 파악용 임시 도구.
/// </summary>
public class BossGroundingProbe : MonoBehaviour
{
    [SerializeField] float sampleInterval = 0.35f;
    [SerializeField] int sampleCount = 24;

    Transform[] _feet;
    Transform _hips;
    NavMeshAgent _agent;
    readonly StringBuilder _log = new StringBuilder();

    void Start()
    {
        var all = GetComponentsInChildren<Transform>(true);
        _feet = all.Where(t => t.name.Contains("ToeBase")).ToArray();
        if (_feet.Length == 0)
            _feet = all.Where(t => t.name.Contains("Foot")).ToArray();
        _hips = all.FirstOrDefault(t => t.name.Contains("Hips"));
        _agent = GetComponent<NavMeshAgent>();

        _log.AppendLine("time\trootY\tfootY\thipsY\tnavMeshY\tagentOnMesh\t상태");
        StartCoroutine(Sample());
    }

    IEnumerator Sample()
    {
        for (int i = 0; i < sampleCount; i++)
        {
            yield return new WaitForSeconds(sampleInterval);

            float rootY = transform.position.y;
            float footY = _feet.Length > 0 ? _feet.Min(t => t.position.y) : float.NaN;
            float hipsY = _hips != null ? _hips.position.y : float.NaN;

            float navY = float.NaN;
            if (NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
                navY = hit.position.y;

            string state = "?";
            var animator = GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                var info = animator.GetCurrentAnimatorClipInfo(0);
                if (info.Length > 0) state = info[0].clip.name;
            }

            _log.AppendLine($"{Time.time:F1}\t{rootY:F3}\t{footY:F3}\t{hipsY:F3}\t{navY:F3}\t" +
                            $"{(_agent != null && _agent.isOnNavMesh)}\t{state}");
        }

        // 지면(y=0) 기준으로 발이 얼마나 떠 있는지 요약
        _log.AppendLine("\n=== 요약 ===");
        _log.AppendLine("footY가 0보다 크면 그만큼 공중에 떠 있다는 뜻입니다.");

        File.WriteAllText("Temp/boss_grounding.txt", _log.ToString());
        Debug.Log("보스 접지 측정 완료 → Temp/boss_grounding.txt\n" + _log);
    }
}
