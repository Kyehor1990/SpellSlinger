using UnityEngine;

public class VFXAutoDestroy : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float fallbackLifetime = 0.8f;

    private float elapsed;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (HasFinishedAnimatorState() || elapsed >= fallbackLifetime)
            Destroy(gameObject);
    }

    private bool HasFinishedAnimatorState()
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            return false;

        if (animator.IsInTransition(0))
            return false;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.length > 0f && stateInfo.normalizedTime >= 1f;
    }
}
