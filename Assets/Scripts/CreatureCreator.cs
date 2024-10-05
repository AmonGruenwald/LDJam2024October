using System;
using UnityEngine;

public class CreatureCreator : MonoBehaviour
{
    [SerializeField]
    GameObject CreaturePrefab;
    [SerializeField]
    Texture2D DummyImage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private int idCounter = 0;
    void Start()
    {

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
            .SetStats(1, 3, 1, 1, 1) // TODO figure default stats
            .SetImage(DummyImage);
        return creatureComponent;
    }

    public Creature CreateCreature(Texture2D image)
    {
        var objectImage = SegmentImage(image);
        var description = DescribeImage(objectImage);
        GameObject instantiatedObject = Instantiate(CreaturePrefab, new Vector3(0, 0.5f, 0), Quaternion.identity);
        Creature creatureComponent = instantiatedObject.GetComponent<Creature>();
        creatureComponent
            .SetId(idCounter++)
            .SetStats(description)
            .SetImage(objectImage);
        return creatureComponent;
    }

    private CreatureDescription DescribeImage(Texture2D objectImage)
    {
        return new CreatureDescription();
    }

    private Texture2D SegmentImage(Texture2D image)
    {
        return image;
    }
}
