using System;
using System.Collections.Generic;
using UnityEngine;

public class CreatureCreator : MonoBehaviour
{
    [SerializeField]
    GameObject CreaturePrefab;
    [SerializeField]
    Texture2D DummyImage;

    [SerializeField] TextAsset jsonData;

    public List<SerializedCreatureData> statTemplates;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private int idCounter = 0;
    void Start()
    {
        statTemplates = JsonUtility.FromJson<SerializedCreatureDataListWrapper>(jsonData.text).data;
    }

    // Update is called once per frame
    void Update()
    {
    }

    public Creature CreateDummyCreature()
    {
        GameObject instantiatedObject = Instantiate(CreaturePrefab, new Vector3(0, 0.5f, 0), Quaternion.identity);
        Creature creatureComponent = instantiatedObject.GetComponent<Creature>();
        creatureComponent
            .SetId(idCounter++)
            .SetStats(1, 3, 1, 1, 1, Element.Wood)
            .SetImage(DummyImage);
        return creatureComponent;
    }

    public Creature CreateCreature(Texture2D image, ClassificationPrediction prediction, Vector3 spawnPosition)
    {
        var description = DescribeImage(prediction);
        GameObject instantiatedObject = Instantiate(CreaturePrefab, spawnPosition, Quaternion.identity);
        Creature creatureComponent = instantiatedObject.GetComponent<Creature>();

        creatureComponent
            .SetId(idCounter++)
            .SetStats(description)
            .SetImage(image);
        return creatureComponent;
    }

    
    private CreatureDescription DescribeImage(ClassificationPrediction prediction)
    {
        var template = statTemplates[prediction.categoryIndex];
        var nameIndex = UnityEngine.Random.Range(0, template.name.Length);

        return new CreatureDescription
        {
            name = template.name[nameIndex],
            speed = template.stats.RandomSpeed(),
            health = template.stats.RandomHealth(),
            strength = template.stats.RandomStrength(),
            evasion = template.stats.RandomEvasion(),
            defense = template.stats.RandomDefense(),
            element = StringToElement(template.element)
        };
    }

    private Element StringToElement(string element)
    {
        switch (element)
        {
            case "Water":
                return Element.Water;
                break;
            case "Metal":
                return Element.Metal;
                break;
            case "Earth":
                return Element.Earth;
                break;
            case "Fire":
                return Element.Fire;
                break;
            case "Wood":
                return Element.Wood;
                break;
        }
        return Element.Wood;
    }
}

[Serializable]
public class SerializedCreatureDataListWrapper {
    public List<SerializedCreatureData> data;
}

[Serializable]
public class SerializedCreatureData {
    public string[] name;
    public string element;
    public SerializedCreatureDataStats stats;
}

[Serializable]
public class SerializedCreatureDataStats {
    public float[] speed;
    public float[] health;
    public float[] strength;
    public float[] evasion;
    public float[] defense;


    public float RandomSpeed() => UnityEngine.Random.Range(speed[0], speed[1]);
    public float RandomHealth() => UnityEngine.Random.Range(health[0], health[1]);
    public float RandomStrength() => UnityEngine.Random.Range(strength[0], strength[1]);
    public float RandomEvasion() => UnityEngine.Random.Range(evasion[0], evasion[1]);
    public float RandomDefense() => UnityEngine.Random.Range(defense[0], defense[1]);
}
