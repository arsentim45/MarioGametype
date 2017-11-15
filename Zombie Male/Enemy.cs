using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour {

    private enum EnemyState { Patroll, Alert, Attack }
    private EnemyState currentEnemyState;
    private bool isAttacking = false;


    private Rigidbody2D enemyRigidbody;

    public float attackSpeedTime = 3f;
    private float previousAttackTime;

    public Transform checkDiretctionPivot;
    public Transform groundPivot;
    public float radiusCast = 0.3f;
    public LayerMask groundLayer;
    public LayerMask interactiveObjectLayer;
    private Vector2 movementDirection;

    public float movementSpeed = 2f;

    public float maxHealth = 30f;
    private float health;

    public float attackDamage = 15f;

    public float checkRadius = 4f;

    [HideInInspector] public bool dead;
    private Animator anim;

    public LayerMask playerLayer;

    private Player target;

	// Use this for initialization
	void Start () {

        dead = false;

        enemyRigidbody = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        health = maxHealth;

        movementDirection = Vector3.right;

        previousAttackTime = -attackSpeedTime - 0.1f;

        TriggerPartrollState();
        TriggerAlertState();
        TriggerAttackState();

    }
	
	// Update is called once per frame
	void Update () {

        Act();

	}

    public void TriggerPartrollState()
    {
        if (!Physics2D.OverlapCircle(transform.position, checkRadius, playerLayer))
        {
            currentEnemyState = EnemyState.Patroll;
        }
    }

    public void TriggerAlertState()
    {
        if (Physics2D.OverlapCircle(transform.position, checkRadius, playerLayer))
        {
            currentEnemyState = EnemyState.Alert;
        }
    }

    public void TriggerAttackState()
    {
        RaycastHit2D hit;
        for (int i = -4; i < 5; i++)
        {
            hit = Physics2D.Raycast(transform.position, DirectionFromAngle(22.5f * i), checkRadius * 0.75f, playerLayer);
            if (hit)
            {
                target = hit.collider.GetComponent<Player>();
                if (!target.dead)
                {
                    currentEnemyState = EnemyState.Attack;
                    break;
                }
            }
        }
    }

    public Vector3 DirectionFromAngle(float angleInDegrees)
    {
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    private void Act()
    {
        if (dead)
        {
            return;
        }

        switch (currentEnemyState)
        {
            case EnemyState.Patroll:
                TriggerAlertState();

                // To make normalized on x == 1 or -1
                movementDirection.y = 0;
                int normalizedXMovement = (int)movementDirection.normalized.x;
                if (Physics2D.OverlapCircle(checkDiretctionPivot.transform.position, radiusCast, groundLayer) && !Physics2D.OverlapCircle(checkDiretctionPivot.transform.position, radiusCast, interactiveObjectLayer))
                {
                    transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * normalizedXMovement, transform.localScale.y, transform.localScale.z);
                    enemyRigidbody.velocity = new Vector2(normalizedXMovement * movementSpeed, enemyRigidbody.velocity.y);
                }
                else if (Physics2D.OverlapCircle(groundPivot.transform.position, radiusCast, groundLayer))
                {
                    movementDirection *= -1;
                    movementDirection.y = 0;
                    normalizedXMovement = (int)movementDirection.normalized.x;
                    transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * normalizedXMovement, transform.localScale.y, transform.localScale.z);
                }
                

                break;
            case EnemyState.Alert:
                TriggerAttackState();
                TriggerPartrollState();

                break;
            case EnemyState.Attack:
                TriggerPartrollState();

                movementDirection = target.transform.position - transform.position;

                if (movementDirection.sqrMagnitude < 3f)
                {
                    if (Time.time - previousAttackTime > attackSpeedTime)
                    {
                        Attack(target, 3, attackDamage);
                        previousAttackTime = Time.time;
                    }
                }
                else
                {
                    // To make normalized on x == 1 or -1
                    movementDirection.y = 0;
                    normalizedXMovement = (int)movementDirection.normalized.x;
                    transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * normalizedXMovement, transform.localScale.y, transform.localScale.z);
                    enemyRigidbody.velocity =  new Vector2(movementDirection.normalized.x * movementSpeed, enemyRigidbody.velocity.y);
                }

                break;
            default:
                break;
        }

        anim.SetFloat("Walk", Mathf.Abs(enemyRigidbody.velocity.x));
    }

    private void Distraction()
    {
        
    }

    private IEnumerator DistractionBehavior()
    {
        yield return null;
    }

    private void Attack(Player attackTarget, float attackSmoothingValue, float attackDamage)
    {
        if (!isAttacking && !attackTarget.dead)
        {
            StartCoroutine(AttackAnimation(attackTarget, attackSmoothingValue, attackDamage));
        }
        else
        {
            currentEnemyState = EnemyState.Patroll;
        }
    }

    private IEnumerator AttackAnimation(Player attackTarget, float attackSmoothingValue, float attackDamage)
    {
        isAttacking = true;
        anim.SetTrigger("Attack");
        float timePercent = 0;
        bool attacked = false;

        Vector3 cashedPosition = transform.position;

        while (timePercent <= 1)
        {
            timePercent += attackSmoothingValue * Time.deltaTime;
            float transformedPercent = (timePercent - timePercent * timePercent) * 4;

            transform.position = Vector3.Lerp(cashedPosition, attackTarget.transform.position, transformedPercent);

            if (!attacked && timePercent >= 0.5f)
            {
                attackTarget.TakeDamage(attackDamage);
                attacked = true;
            }

            yield return null;
        }

        isAttacking = false;

    }

    public void TakeDamage(float amount)
    {
        if (!dead)
        {
            health -= amount;

            if (health <= 0)
            {
                health = 0;
                Die();
            }

            // Update UI

        }
    }

    private void Die()
    {
        dead = true;
        StartCoroutine(DeathAnimation());
		Player.Instance.AddToScore (10);
    }

    private IEnumerator DeathAnimation()
    {
        // Trigger death animation
        anim.SetTrigger("Death");

        yield return new WaitForSeconds(2);

        Destroy(this.gameObject);
    }

#if UNITY_EDITOR

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, checkRadius);
        Gizmos.DrawWireSphere(checkDiretctionPivot.transform.position, radiusCast);
        Gizmos.DrawWireSphere(groundPivot.transform.position, radiusCast);
    }

#endif

}
