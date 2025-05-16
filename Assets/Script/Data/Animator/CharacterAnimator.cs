using UnityEngine;
using System.Collections;

public class CharacterAnimator : MonoBehaviour
{
    [Header("Animation References")]
    public Animator animator;
    public GameObject attackEffect;
    
    [Header("Animation Parameters")]
    public float attackAnimDuration = 0.5f;
    public float hitAnimDuration = 0.3f;
    
    private static readonly int IdleTrigger = Animator.StringToHash("Idle");
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int DamageTrigger = Animator.StringToHash("Damaged");
    private static readonly int DeathTrigger = Animator.StringToHash("Death");
    
    private void Start()
    {
        // Start in idle animation
        if (animator != null)
        {
            animator.SetTrigger(IdleTrigger);
        }
    }
    
    public void PlayIdleAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(IdleTrigger);
        }
    }
    
    public IEnumerator PlayAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(AttackTrigger);
            
            // Show attack effect if available
            if (attackEffect != null)
            {
                attackEffect.SetActive(true);
                yield return new WaitForSeconds(0.2f); // Show effect briefly
                attackEffect.SetActive(false);
            }
            
            yield return new WaitForSeconds(attackAnimDuration);
            animator.SetTrigger(IdleTrigger); // Return to idle
        }
        else
        {
            yield return null;
        }
    }
    
    public IEnumerator PlayDamageAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(DamageTrigger);
            yield return new WaitForSeconds(hitAnimDuration);
            animator.SetTrigger(IdleTrigger); // Return to idle
        }
        else
        {
            yield return null;
        }
    }
    
    public void PlayDeathAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(DeathTrigger);
            Debug.Log($"Playing death animation on {gameObject.name}");
        }
    }
}