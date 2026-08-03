using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 하단 보스 체력바. 전투가 시작되면 나타나고, 피해를 입으면 흰 잔상이 뒤늦게 따라온다.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [SerializeField] BossAI boss;
    [SerializeField] Health bossHealth;
    [SerializeField] CanvasGroup group;
    [SerializeField] RectTransform fill;
    [SerializeField] RectTransform delayedFill; // 감소분을 천천히 따라오는 잔상
    [SerializeField] Text nameText;
    [SerializeField] string bossName = "MUTANT";

    [SerializeField] float delayedCatchUpSpeed = 0.35f;

    float _delayedRatio = 1f;

    void Start()
    {
        if (nameText != null) nameText.text = bossName;
        if (group != null) group.alpha = 0f;
    }

    void LateUpdate()
    {
        if (boss == null || bossHealth == null) return;

        bool visible = boss.IsEngaged || (bossHealth.IsDead && _delayedRatio > 0.01f);
        if (group != null)
            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, Time.deltaTime * 2.5f);

        float ratio = bossHealth.Current / bossHealth.Max;
        if (fill != null)
            fill.localScale = new Vector3(ratio, 1f, 1f);

        _delayedRatio = _delayedRatio < ratio
            ? ratio
            : Mathf.MoveTowards(_delayedRatio, ratio, delayedCatchUpSpeed * Time.deltaTime);
        if (delayedFill != null)
            delayedFill.localScale = new Vector3(_delayedRatio, 1f, 1f);
    }
}
