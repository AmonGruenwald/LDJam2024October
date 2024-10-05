using UnityEngine;
using System;

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
        if (a.Health <= 0)
        {
            Debug.Log("#"+a.id+" died");
            completeFight(b);
        }
        else if(b.Health <= 0)
        {
            Debug.Log("#" + b.id + " died");
            completeFight(a);
        }
    }
    public void StartFight(Creature a, Creature b)
    {
        a.gameObject.transform.position = FightSpot.position + FightSpot.right * Distance * 0.5f + new Vector3(0, a.gameObject.transform.localScale.y * 1.0f, 0);
        b.gameObject.transform.position = FightSpot.position + FightSpot.right * Distance * -0.5f + new Vector3(0, a.gameObject.transform.localScale.y * 1.0f, 0);
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
