using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class Creature : MonoBehaviour, IEquatable<Creature>
{
    public string Name;
    public float Speed;
    public float Health;
    public float Defense;
    public float Strength;
    public float Evasion;
    public int id;

    private Renderer _renderer;
    private float currentAttackDeadline = 1.0f;

    public bool AttackRunning = false;

    private void Awake() {
        _renderer = GetComponentInChildren<Renderer>();
    }

    public Creature SetId(int id)
    {
        this.id = id;
        return this;
    }
    public Creature SetStats(
        float speed,
        float health,
        float defense, 
        float attack,
        float evasion)
    {
        this.Speed = speed;
        this.Health = health;
        this.Defense = defense;
        this.Strength = attack;
        this.Evasion = evasion;

        return this;
    }
    public Creature SetStats(CreatureDescription description)
    {
        this.name = description.name;

        //TODO: elemental
        this.Speed = description.speed;
        this.Health = description.health;
        this.Defense = description.defense;
        this.Strength = description.strength;
        this.Evasion = description.evasion;
        return this;
    }

    public Creature SetImage(Texture2D image)
    {
        // TODO: destroy old texture
        _renderer.material.mainTexture = image;
        AdjustScale();
        return this;
    }

    public bool decreaseAndCheckAttackDeadline(float value)
    {
        currentAttackDeadline -= value;
        print("#"+id + " deadline value: " + currentAttackDeadline);
        if (currentAttackDeadline <= 0)
        {
            currentAttackDeadline = 1;
            return true;
        }
        return false;
    }

    public void Attack(Creature other)
    {
        if (!AttackRunning)
        {
            StartCoroutine(AttackCoroutine(other));
        }
    }

    private IEnumerator AttackCoroutine(Creature other)
    {
        AttackRunning = true;

        // Calculate the middle point between the attacker and the target
        Vector3 middlePoint = (transform.position + other.gameObject.transform.position) / 2f;
        middlePoint.y += 1; // Add height for the arc

        Vector3 originalPosition = this.transform.position;
        // Create a path for the jump animation
        Vector3[] path = new Vector3[] { originalPosition, middlePoint, other.transform.position };

        // Perform the jump animation
        yield return this.transform.DOPath(path, 0.5f, PathType.CatmullRom)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();
        this.transform.DOMove(originalPosition, 0.3f)
            .SetEase(Ease.InQuad)
            .WaitForCompletion();
        if (!other.TryDodge())
        {
            float baseDamageReduction = other.Defense * 5.0f / 100;
            // Add some randomness to the damage reduction (±10%)
            float randomFactor = (UnityEngine.Random.Range(0.0f, 1.0f) * 0.2f - 0.1f);
            float damageReduction = Mathf.Clamp(baseDamageReduction + randomFactor, 0f, 0.5f);
            // Add randomness to the base damage (±10%)
            float baseDamage = this.Strength * (1 - damageReduction);
            float randomDamage = baseDamage * (UnityEngine.Random.Range(0.0f, 1.0f) * 0.2f - 0.1f);
            float finalDamage = Mathf.Max(1.0f, randomDamage);
            yield return other.Damage(finalDamage);

            Debug.Log($"Creature #{id} dealt {finalDamage} damage to creature #{other.id}. Health afterwards: {other.Health}");
        }
        else
        {
            Debug.Log($"Fuck-a-duck, the boi #{other.id} dodged!");
        }


        AttackRunning = false;
    }

    public void PerformAttackAnimation(Transform target)
    {
    }


    private IEnumerator Damage(float finalDamage)
    {
        this.Health -= finalDamage;
        yield return this.gameObject.transform.DOShakePosition(0.5f, 0.3f, 20, 10.0f)
            .SetEase(Ease.OutElastic)
            .WaitForCompletion();
    }

    public bool TryDodge()
    {
        return this.Evasion >= UnityEngine.Random.Range(0, 30);
    }
    public bool Equals(Creature other)
    {
        return this.id.Equals(other.id);
    }

    public void AdjustScale()
    {
        var texture = _renderer.material.mainTexture;

        float textureAspectRatio = (float)texture.width / texture.height;

        Vector3 scale = transform.localScale;
        scale.x = scale.y * textureAspectRatio;
        transform.localScale = scale;
    }
}