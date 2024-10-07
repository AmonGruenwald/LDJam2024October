using DG.Tweening;
using System;
using System.Collections;
using Unity.Android.Gradle;
using UnityEngine;

public enum Element
{
    Water, Metal, Earth, Fire, Wood
}
public class Creature : MonoBehaviour, IEquatable<Creature>
{
    public string Name;
    public float Speed;
    public float Health;
    public float Defense;
    public float Strength;
    public float Evasion;
    public int id;

    public Element Element;
    private Renderer _renderer;
    private Animator _animator;
    public ParticleSystem ParticleSystem;
    public Leg[] Legs;
    private float currentAttackDeadline = 1.0f;

    public bool AttackRunning = false;

    public float CurrentHealth;


    private Color GetColorForElement(Element element)
    {
        switch (element)
        {
            case Element.Water:
                return new Color(0.0f / 255.0f, 255.0f / 255.0f, 246.0f / 255.0f);
                break;
            case Element.Earth:
                return new Color(203.0f / 255.0f, 111.0f / 255.0f, 0.0f / 255.0f);
                break;
            case Element.Fire:
                return new Color(255.0f / 255.0f, 9.0f / 255.0f, 0.0f / 255.0f);
                break;
            case Element.Wood:
                return new Color(34.0f / 255.0f, 255.0f / 255.0f, 0.0f / 255.0f);
                break;
            case Element.Metal:
                return new Color(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
                break;
        }
        return Color.white;
    }

    private void Awake() {
        _renderer = GetComponentInChildren<Renderer>();
        _animator = GetComponentInChildren<Animator>();
        ParticleSystem = GetComponentInChildren<ParticleSystem>();
        Legs = GetComponentsInChildren<Leg>();
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
        float strength,
        float evasion,
        Element element)
    {
        this.Speed = speed;
        this.Health = health;
        this.CurrentHealth = this.Health;
        this.Defense = defense;
        this.Strength = strength;
        this.Evasion = evasion;
        this.Element = element;


        var colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(GetColorForElement(Element.Wood), 0);
        colorKeys[1] = new GradientColorKey(new Color(1, 1, 1), 1);
        ParticleSystem.colorOverLifetime.color.gradient.colorKeys = colorKeys;

        return this;
    }
    public Creature SetStats(CreatureDescription description)
    {
        Name = description.name;
        return SetStats(description.speed, description.health, description.defense, description.strength, description.evasion, description.element);
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
        _animator.SetTrigger("Attacking");
        yield return new WaitForSeconds(0.2f);
        // Calculate the middle point between the attacker and the target
        Vector3 originalPosition = this.transform.position;
        Vector3 targetPoint = (originalPosition + (other.gameObject.transform.position - originalPosition) * 0.75f);
        Vector3 middlePoint = (originalPosition + targetPoint) / 2f;
        middlePoint.y += 0.8f; // Add height for the arc

        // Create a path for the jump animation
        Vector3[] path = new Vector3[] { originalPosition, middlePoint, targetPoint };

        bool doesDodge = other.TryDodge();
        float attackDuration = 0.5f;
        if (doesDodge)
        {
            other.Dodged(attackDuration);
        }
        // Perform the jump animation
        yield return this.transform.DOPath(path, attackDuration, PathType.CatmullRom)
            .SetEase(Ease.OutQuad)
            .SetDelay(0.05f)
            .WaitForCompletion();
        this.transform.DOMove(originalPosition, 0.3f)
            .SetEase(Ease.InQuad)
            .WaitForCompletion();
        if (!doesDodge)
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

    private void Dodged(float duration)
    {
        Vector3 originalPosition = this.transform.position;
        Vector3 targetPoint = (originalPosition + this.gameObject.transform.right * (UnityEngine.Random.Range(0.0f, 10.0f) < 5.0f ? 1 : -1) * 0.25f);
        Vector3[] path = new Vector3[] { originalPosition, targetPoint, originalPosition };
        this.gameObject.transform.DOPath(path, duration * 2);
        Quaternion startRotation = this.gameObject.transform.rotation;
        this.gameObject.transform.DORotateQuaternion(startRotation * Quaternion.Euler(0, 90 * (UnityEngine.Random.Range(0.0f, 10.0f) < 5.0f ? 1 : -1), 0), duration);
        this.gameObject.transform.DORotateQuaternion(startRotation, duration)
            .SetDelay(duration * 0.5f);
    }

    public void PerformAttackAnimation(Transform target)
    {
    }


    private IEnumerator Damage(float finalDamage)
    {
        this.CurrentHealth -= finalDamage;
        _animator.SetTrigger("Damaged");
        yield return this.gameObject.transform.DOShakePosition(0.5f, 0.15f, 10, 10.0f)
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

    public string HighestStat() {
        float highest = Speed;
        string stat = "Speed";
        if (Health > highest) {
            highest = Health;
            stat = "Health";
        }
        if (Defense > highest) {
            highest = Defense;
            stat = "Defense";
        }
        if (Strength > highest) {
            highest = Strength;
            stat = "Strength";
        }
        if (Evasion > highest) {
            highest = Evasion;
            stat = "Evasion";
        }

        return stat;
    }

    public string LowestStat() {
        float lowest = Speed;
        string stat = "Speed";
        if (Health < lowest) {
            lowest = Health;
            stat = "Health";
        }
        if (Defense < lowest) {
            lowest = Defense;
            stat = "Defense";
        }
        if (Strength < lowest) {
            lowest = Strength;
            stat = "Strength";
        }
        if (Evasion < lowest) {
            lowest = Evasion;
            stat = "Evasion";
        }

        return stat;
    }
}