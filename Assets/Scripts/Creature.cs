using DG.Tweening;
using System;
using System.Collections;
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
    public Transform Body;
    public ParticleSystem AttackParticles;
    public GameObject DodgedPrefab;
    public AudioSource AttackAudio;
    public AudioSource[] AttackAudioVariations;
    public AudioSource DamageAudio;
    public AudioSource DodgeAudio;
    public AudioSource DeathAudio;

    public Element Element;
    private Renderer _renderer;
    private Animator _animator;
    public ParticleSystem ParticleSystem;
    public Leg[] Legs;
    private float currentAttackDeadline = 1.0f;

    public bool AttackRunning = false;

    public float CurrentHealth;
    private float randomBodyOffset;

    public Vector3 BodyBasePosition;

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
        randomBodyOffset = UnityEngine.Random.Range(0.0f, 180.0f);
        BodyBasePosition = Body.localPosition;
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
        colorKeys[0] = new GradientColorKey(GetColorForElement(element), 0);
        colorKeys[1] = new GradientColorKey(new Color(1, 1, 1), 1);
        ParticleSystem.colorOverLifetime.color.gradient.colorKeys = colorKeys;

        return this;
    }

    private void Update()
    {
        Vector3 bodyOffset = new Vector3(0, Mathf.Sin(Time.time * 7 + randomBodyOffset) * 0.05f, Mathf.Sin(Time.time * 5 + randomBodyOffset) * 0.01f);
        Body.localPosition = BodyBasePosition + bodyOffset;
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
        AttackAudio.Play();
        AttackAudioVariations[UnityEngine.Random.Range(0, AttackAudioVariations.Length)].Play();
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
        if (!doesDodge) {
            other.AttackParticles.Play();
        }
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
            float elementMultiplier = GetElementDamageMultiplier(this.Element, other.Element);
            float elementalDamage = randomDamage * elementMultiplier;
            float finalDamage = Mathf.Max(1.0f, elementalDamage);
            yield return other.Damage(finalDamage);

            Debug.Log($"Creature #{id} dealt {finalDamage} damage to creature #{other.id}. Health afterwards: {other.Health}");
        }
        else
        {
            Debug.Log($"Fuck-a-duck, the boi #{other.id} dodged!");
        }


        AttackRunning = false;
    }

    private float GetElementDamageMultiplier(Element Attacker, Element Defender)
    {
        switch (Attacker)
        {
            case Element.Wood:
                if (Defender == Element.Fire)
                {
                    return 0.75f;
                }
                if (Defender == Element.Earth)
                {
                    return 1.25f;
                }
                break;
            case Element.Fire:
                if (Defender == Element.Earth)
                {
                    return 0.75f;
                }
                if (Defender == Element.Metal)
                {
                    return 1.25f;
                }
                break;
            case Element.Earth:
                if (Defender == Element.Metal)
                {
                    return 0.75f;
                }
                if (Defender == Element.Water)
                {
                    return 1.25f;
                }
                break;
            case Element.Metal:
                if (Defender == Element.Water)
                {
                    return 0.75f;
                }
                if (Defender == Element.Wood)
                {
                    return 1.25f;
                }
                break;
            case Element.Water:
                if (Defender == Element.Wood)
                {
                    return 0.75f;
                }
                if (Defender == Element.Fire)
                {
                    return 1.25f;
                }
                break;
        }
        return 1.0f;
    }

    private void Dodged(float duration)
    {
        DodgeAudio.Play();
        Vector3 originalPosition = this.transform.position;
        Vector3 targetPoint = (originalPosition + this.gameObject.transform.right * (UnityEngine.Random.Range(0.0f, 10.0f) < 5.0f ? 1 : -1) * 0.25f);
        Vector3[] path = new Vector3[] { originalPosition, targetPoint, originalPosition };
        this.gameObject.transform.DOPath(path, duration * 2);
        Quaternion startRotation = this.gameObject.transform.rotation;

        var go = Instantiate(DodgedPrefab, transform.position, Quaternion.identity);
        Destroy(go, 3f);

        this.gameObject.transform.DORotateQuaternion(startRotation * Quaternion.Euler(0, 90 * (UnityEngine.Random.Range(0.0f, 10.0f) < 5.0f ? 1 : -1), 0), duration);
        this.gameObject.transform.DORotateQuaternion(startRotation, duration)
            .SetDelay(duration * 0.5f);
    }

    private IEnumerator Damage(float finalDamage)
    {
        DamageAudio.Play();
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

    public void JumpAway() {
       StartCoroutine(DoJumpAway());
    }

    public void AttackAnimation() {
        _animator.SetTrigger("Attack"); 
    }

    public IEnumerator DoJumpAway() {
        _animator.SetTrigger("Attack");
        transform.DOMoveY(transform.position.y - 0.2f, 0.4f);
        yield return new WaitForSeconds(0.4f);
        transform.DOMoveY(transform.position.y + 5f, 1f);
        yield return new WaitForSeconds(0.95f);
        gameObject.SetActive(false);
    }

    public void Die()
    {
        DeathAudio.Play();
        this.Legs[0].Shrink();
        this.Legs[1].Shrink();
        _animator.SetTrigger("Died");
        StartCoroutine(DelayedDestroy());
    }

    private IEnumerator DelayedDestroy()
    {
        yield return new WaitForSeconds(0.95f);
        yield return transform.DOScale(0.0f, 1f);
        Destroy(this.gameObject);
    }
}