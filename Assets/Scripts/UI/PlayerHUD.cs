using UnityEngine;

/// <summary>체력/스태미나 바 표시. Fill의 X 스케일을 조절하는 단순한 방식.</summary>
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] Health playerHealth;
    [SerializeField] Stamina playerStamina;
    [SerializeField] RectTransform healthFill;
    [SerializeField] RectTransform staminaFill;

    void LateUpdate()
    {
        if (playerHealth != null && healthFill != null)
            healthFill.localScale = new Vector3(playerHealth.Current / playerHealth.Max, 1f, 1f);

        if (playerStamina != null && staminaFill != null)
            staminaFill.localScale = new Vector3(playerStamina.Current / playerStamina.Max, 1f, 1f);
    }
}
