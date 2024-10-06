using UnityEngine;
using System;
using System.Collections;
using DG.Tweening;

public class FightManager : MonoBehaviour
{
    [SerializeField]
    public Transform FightSpot;
    [SerializeField]
    public float Distance = 4.0f;
    public bool FightRunning = false;
    public event Action<Creature> OnFightComplete;

    private float attackSpeedFactor = 0.3f;
    private Creature a;
    private Creature b;
    private void FixedUpdate()
    {
        if (!FightRunning)
        {
            return;
        }
        if (a.AttackRunning || b.AttackRunning)
        {
            return;
        }
        if (a.decreaseAndCheckAttackDeadline(Time.fixedDeltaTime * attackSpeedFactor * a.Speed))
        {
            float prevHealth = b.Health;
            a.Attack(b);
            Debug.Log("#"+a.id+" attacks #"+b.id+": Health: " + prevHealth + "->"+b.Health);
        }
        if (b.decreaseAndCheckAttackDeadline(Time.fixedDeltaTime * attackSpeedFactor * b.Speed))
        {
            float prevHealth = a.Health;
            b.Attack(a);
            Debug.Log("#" + b.id + " attacks #" + a.id + ": Health: " + prevHealth + "->" + a.Health);
        }
        if (a.CurrentHealth <= 0)
        {
            Debug.Log("#"+a.id+" died");
            completeFight(b);
        }
        else if(b.CurrentHealth <= 0)
        {
            Debug.Log("#" + b.id + " died");
            completeFight(a);
        }
    }
    public IEnumerator StartFight(Creature a, Creature b, Transform fightSpot)
    {
        if (fightSpot != null) {
            FightSpot = fightSpot;
        }

        var targetPosA = FightSpot.position + FightSpot.right * Distance * 0.5f + new Vector3(0, a.gameObject.transform.localScale.y * 1.0f, 0);
        a.transform.position = targetPosA;

        var targetPosB = FightSpot.position + FightSpot.right * Distance * -0.5f + new Vector3(0, a.gameObject.transform.localScale.y * 1.0f, 0);
        yield return b.transform.DOMove(targetPosB, 1f).WaitForCompletion();
        this.a = a;
        this.b = b;
        FightRunning = true;
    }
    private void completeFight(Creature winner)
    {
        FightRunning = false;
        OnFightComplete(winner);
    }
}
