using System.Collections.Generic;
using UnityEngine;

namespace PeakCooking;

// TODO:
// Save items added to pot on pickup/place down
// Implement game logic for what adding an item does
// Sync fields/methods across multiplayer
public class CookingPot : MonoBehaviour
{
    // this version of C# has non-nullability enabled, so that's why these getters and setters exist
    Item? _item;
    Item item { get => Utils.NonNullGet(_item); set => Utils.NonNullSet(ref _item, value); }

    GameObject? _soup;
    GameObject soup { get => Utils.NonNullGet(_soup); set => Utils.NonNullSet(ref _soup, value); }

    Vector3 soupScale;

    List<GameObject> dummyItems = new List<GameObject>();
    float dummyItemScale = 0.35f;
    float dummyItemHeight = 0.05f;

    public void Start()
    {
        item = GetComponent<Item>();
        soup = transform.Find("Model").Find("Soup").gameObject;
        soupScale = soup.transform.Find("Cylinder").localScale * 0.95f;
        Plugin.Log.LogInfo($"Cooking Pot was spawned!");
    }

    public void Update()
    {

    }

    private void AddToPotGameLogic()
    {
        // TODO: Handle actual logic here
        Plugin.Log.LogInfo($"Item {item.GetItemName()} placed in Cooking Pot!");
        // for now, add example hunger restoration
        var status = item.gameObject.AddComponent<Action_ModifyStatus>();
        status.statusType = CharacterAfflictions.STATUSTYPE.Hunger;
        status.changeAmount = -0.35f;
        status.OnCastFinished = true;
    }

    public void AddDummyItemToPot(Item item)
    {
        // add dummy item to the soup
        GameObject dummyItem = Utils.CloneItemMeshesOnly(item.gameObject, out Bounds bounds);
        float itemSize = Mathf.Max(bounds.extents.x, bounds.extents.z) * dummyItemScale;
        Vector2 randomCircle = new Vector2(Mathf.Max(0f, soupScale.x * 0.5f - itemSize), Mathf.Max(soupScale.z * 0.5f - itemSize));
        Vector2 randomPos = Random.insideUnitCircle * randomCircle;
        dummyItem.transform.parent = soup.transform;
        dummyItem.transform.localPosition = new Vector3(randomPos.x, dummyItemHeight, randomPos.y);
        dummyItem.transform.localRotation = Quaternion.identity;
        dummyItem.transform.localScale = Vector3.one * dummyItemScale;
        dummyItems.Add(dummyItem);
    }

    public void AddToPot(Item item)
    {
        AddToPotGameLogic();
        AddDummyItemToPot(item);
    }
}