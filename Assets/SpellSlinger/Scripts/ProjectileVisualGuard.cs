using UnityEngine;

[DisallowMultipleComponent]
public class ProjectileVisualGuard : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Sprite fallbackSprite;
    [SerializeField] private string sortingLayerName = "Projectiles";
    [SerializeField] private int sortingOrder = 1;

    private bool hasWarnedMissingRenderer;

    private void Awake()
    {
        CacheReferences();
        EnsureVisibleFallback();
    }

    private void OnEnable()
    {
        CacheReferences();
        EnsureVisibleFallback();
    }

    private void LateUpdate()
    {
        EnsureVisibleFallback();
    }

    private void CacheReferences()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
    }

    private void EnsureVisibleFallback()
    {
        if (spriteRenderer == null)
        {
            if (!hasWarnedMissingRenderer)
            {
                Debug.LogWarning($"{name} has no SpriteRenderer for its projectile visual.", this);
                hasWarnedMissingRenderer = true;
            }

            return;
        }

        if (!spriteRenderer.gameObject.activeSelf)
        {
            spriteRenderer.gameObject.SetActive(true);
        }

        spriteRenderer.enabled = true;

        Color color = spriteRenderer.color;
        if (color.a <= 0f)
        {
            color.a = 1f;
            spriteRenderer.color = color;
        }

        if (spriteRenderer.transform.localScale == Vector3.zero)
        {
            spriteRenderer.transform.localScale = Vector3.one;
        }

        if (!string.IsNullOrEmpty(sortingLayerName))
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
        }

        spriteRenderer.sortingOrder = sortingOrder;

        if (spriteRenderer.sprite == null && fallbackSprite != null)
        {
            spriteRenderer.sprite = fallbackSprite;
        }

        if (animator != null && animator.runtimeAnimatorController == null)
        {
            animator.enabled = false;
        }
    }
}
