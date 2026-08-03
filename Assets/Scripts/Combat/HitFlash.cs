using System.Collections;
using UnityEngine;

/// <summary>피해를 입으면 모델 전체가 잠깐 밝게 번쩍인다. MaterialPropertyBlock을 써서 재질을 복제하지 않는다.</summary>
[RequireComponent(typeof(Health))]
public class HitFlash : MonoBehaviour
{
    [SerializeField] Color flashColor = new Color(1f, 0.35f, 0.3f);
    [SerializeField] float flashDuration = 0.12f;

    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    Renderer[] _renderers;
    MaterialPropertyBlock _block;
    Color[] _originalColors;
    Coroutine _routine;

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _block = new MaterialPropertyBlock();
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            var mat = _renderers[i].sharedMaterial;
            _originalColors[i] = mat != null && mat.HasProperty(BaseColorID)
                ? mat.GetColor(BaseColorID)
                : Color.white;
        }

        GetComponent<Health>().OnDamaged += (amount, from) => Flash();
    }

    public void Flash()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        SetColor(flashColor, true);
        // 히트스톱 중에도 정상 속도로 번쩍이도록 unscaled 사용
        yield return new WaitForSecondsRealtime(flashDuration);
        SetColor(Color.white, false);
        _routine = null;
    }

    void SetColor(Color color, bool flashing)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            _renderers[i].GetPropertyBlock(_block);
            _block.SetColor(BaseColorID, flashing ? color : _originalColors[i]);
            _renderers[i].SetPropertyBlock(_block);
        }
    }
}
