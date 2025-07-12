using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeakCooking;

// TODO:
// Save items added to pot on pickup/place down
// Implement game logic for what adding an item does
// Sync fields/methods across multiplayer
public class CookingPot : ItemComponent
{
    // this version of C# has non-nullability enabled, so that's why these getters and setters exist
    Item? _item;
    Item potItem { get => Utils.NonNullGet(_item); set => Utils.NonNullSet(ref _item, value); }

    GameObject? _soup;
    GameObject soup { get => Utils.NonNullGet(_soup); set => Utils.NonNullSet(ref _soup, value); }

    Vector3 soupScale;

    public class DummyItem
    {
        public ushort ID;
        public GameObject Object;
        public DummyItem(ushort ID, GameObject Object)
        {
            this.ID = ID;
            this.Object = Object;
        }
    }
    List<DummyItem> dummyItems = new List<DummyItem>();
    float dummyItemRadius = 0.875f;
    float dummyItemScale = 0.35f;
    float dummyItemHeight = 0.05f;

    public const DataEntryKey CookingPotDataKey = (DataEntryKey)51;

    // Synced data format
    [Serializable]
    public class PotItem
    {
        public ushort ID;
        public int CookedAmount;
    }
    // controlled by multiplayer sync, do not modify
    List<PotItem> Items = new List<PotItem>();


    public void Start()
    {
        potItem = GetComponent<Item>();
        soup = transform.Find("Model").Find("Soup").gameObject;
        soupScale = soup.transform.Find("Cylinder").localScale * dummyItemRadius;
        if (string.IsNullOrEmpty(GetData<StringItemData>(CookingPotDataKey).Value))
        {
            ClearItemsFromData();
        }
        OnInstanceDataSet();
    }

    public void Update()
    {

    }

    private void AddToPotGameLogic(Item item)
    {
        // TODO: Handle actual logic here
        Plugin.Log.LogInfo($"Item {item.GetItemName()} placed in Cooking Pot!");
        // for now, add example hunger restoration
        var status = potItem.gameObject.AddComponent<Action_ModifyStatus>();
        status.statusType = CharacterAfflictions.STATUSTYPE.Hunger;
        status.changeAmount = -0.35f;
        status.OnCastFinished = true;
    }

    public void AddDummyItemToPot(Item item)
    {
        if (_soup == null)
        {
            // happens while item is being picked up, ignore these calls
            return;
        }
        // add dummy item to the soup
        GameObject dummyItem = Utils.CloneItemMeshesOnly(item.gameObject, out Bounds bounds);
        float itemSize = Mathf.Max(bounds.extents.x, bounds.extents.z) * dummyItemScale;
        Vector2 randomCircle = new Vector2(Mathf.Max(0f, soupScale.x * 0.5f - itemSize), Mathf.Max(soupScale.z * 0.5f - itemSize));
        Vector2 randomPos = UnityEngine.Random.insideUnitCircle * randomCircle;
        dummyItem.transform.parent = soup.transform;
        dummyItem.transform.localPosition = new Vector3(randomPos.x, dummyItemHeight, randomPos.y);
        dummyItem.transform.localRotation = Quaternion.identity;
        dummyItem.transform.localScale = Vector3.one * dummyItemScale;
        dummyItems.Add(new DummyItem(ID: item.itemID, Object: dummyItem));
    }

    public void AddToPot(Item item)
    {
        AddItemToData(item);
        OnInstanceDataSet();
        AddToPotGameLogic(item);
    }

    public override void OnInstanceDataSet()
    {
        StringItemData data = GetData<StringItemData>(CookingPotDataKey);
        if (string.IsNullOrEmpty(data.Value))
        {
            ClearItemsFromData();
        }
        var items = Deserialize(data.Value);
        if (items == null)
        {
            Plugin.Log.LogInfo($"Failed to deserialize: {data.Value}");
            return;
        }
        Items = items;
        // diff with current dummy items
        Dictionary<ushort, int> diff = new Dictionary<ushort, int>();
        foreach (var item in Items)
        {
            if (item != null)
            {
                if (!diff.ContainsKey(item.ID))
                {
                    diff[item.ID] = 0;
                }
                diff[item.ID]++;
            }
        }
        foreach (DummyItem item in dummyItems)
        {
            if (!diff.ContainsKey(item.ID))
            {
                diff[item.ID] = 0;
            }
            diff[item.ID]--;
        }
        foreach (var k in diff.Keys)
        {
            // add items until equal
            if (diff[k] > 0)
            {
                for (int i = 0; i < diff[k]; i++)
                {
                    ItemDatabase.TryGetItem(k, out Item item);
                    if (item != null)
                    {
                        AddDummyItemToPot(item);
                    }
                }
            }
            // remove items until equal
            else if (diff[k] < 0)
            {
                for (int i = 0; i < dummyItems.Count && diff[k] < 0; i++)
                {
                    if (dummyItems[i].ID == k)
                    {
                        dummyItems.RemoveAt(i);
                        i--;
                        diff[k]++;
                    }
                }
            }
        }
        // TODO: update stats
    }

    private void ClearItemsFromData()
    {
        GetData<StringItemData>(CookingPotDataKey).Value = Serialize(new List<PotItem>());
    }

    private void AddItemToData(Item item)
    {
        var data = GetData<StringItemData>(CookingPotDataKey);
        List<PotItem>? items = Deserialize(data.Value);
        if (items == null)
        {
            Plugin.Log.LogInfo($"Failed to deserialize: {data.Value}");
            return;
        }
        items.Add(new PotItem() {
            ID = item.itemID,
            CookedAmount = item.GetData<IntItemData>(DataEntryKey.CookedAmount).Value,
        });
        GetData<StringItemData>(CookingPotDataKey).Value = Serialize(items);
    }

    string Serialize(List<PotItem> items)
    {
        string result = JsonConvert.SerializeObject(items);
        Plugin.Log.LogInfo($"CookingPot Serialize: {result}");
        return result;
    }

    List<PotItem>? Deserialize(string data)
    {
        return JsonConvert.DeserializeObject<List<PotItem>>(data);
    }
}