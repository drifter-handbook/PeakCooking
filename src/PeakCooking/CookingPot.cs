using Newtonsoft.Json;
using PEAKLib.Items;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PeakCooking;

public class CookingPot : ModItemComponent
{
    GameObject? _soup;
    GameObject soup { get => Utils.NonNullGet(_soup); set => Utils.NonNullSet(ref _soup, value); }

    Vector3 soupScale;

    // "dummy items" are the purely cosmetic items floating in the soup
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
    float dummyItemRadius = 0.85f;
    float dummyItemScale = 0.35f;
    float dummyItemHeight = 0.03f;

    // Synced data format
    [Serializable]
    public class PotItem
    {
        public ushort ID;
        public int CookedAmount;
        public int Uses;
    }

    CookingPotEffects CurrentEffects = new CookingPotEffects();

    public override void Awake()
    {
        base.Awake();
        soup = transform.Find("Model").Find("Soup").gameObject;
        soupScale = soup.transform.Find("Cylinder").localScale * dummyItemRadius;
        // init item before Item.Start()
        if (!HasData(DataEntryKey.ItemUses))
        {
            OptionableIntItemData optionableIntItemData = GetData<OptionableIntItemData>(DataEntryKey.ItemUses);
            optionableIntItemData.HasData = true;
            optionableIntItemData.Value = 0;
            item.SetUseRemainingPercentage(0f);
        }

    }

    void Start()
    {
        OnInstanceDataSet();
        List<PotItem> data = GetData();
        Plugin.Log.LogInfo($"Cooking Pot State: {CurrentEffects}");
        Plugin.Log.LogInfo($"Cooking Pot Items: {JsonConvert.SerializeObject(data)}");
    }

    public void Update()
    {

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
        int uses = Mathf.Max(1, item.GetData<OptionableIntItemData>(DataEntryKey.ItemUses).Value);
        int cookedAmount = item.GetData<IntItemData>(DataEntryKey.CookedAmount).Value;
        Plugin.Log.LogInfo($"Item {item.GetItemName()} placed in Cooking Pot!");
        photonView.RPC("AddToPotRPC", RpcTarget.All, (int)item.itemID, cookedAmount, uses);
    }
    [PunRPC]
    public void AddToPotRPC(int ID, int cookedAmount, int uses)
    {
        AddItemToData((ushort)ID, cookedAmount, uses);
        OnInstanceDataSet();
        List<PotItem> data = GetData();
        Plugin.Log.LogInfo($"Cooking Pot State: {CurrentEffects}");
        Plugin.Log.LogInfo($"Cooking Pot Items: {JsonConvert.SerializeObject(data)}");
    }

    public void ClearPot()
    {
        photonView.RPC("ClearPotRPC", RpcTarget.All);
    }
    [PunRPC]
    public void ClearPotRPC()
    {
        ClearItemsFromData();
        OnInstanceDataSet();
        List<PotItem> data = GetData();
        Plugin.Log.LogInfo($"Cooking Pot State: {CurrentEffects}");
        Plugin.Log.LogInfo($"Cooking Pot Items: {JsonConvert.SerializeObject(data)}");
    }

    public void RemoveRandomItem()
    {
        List<PotItem> data = GetData();
        int useIndex = UnityEngine.Random.Range(0, data.Sum(x => x.Uses));
        photonView.RPC("RemoveItemRPC", RpcTarget.All, useIndex);
    }
    [PunRPC]
    public void RemoveItemRPC(int useIndex)
    {
        List<PotItem> data = GetData();
        List<PotItem> itemIndexes = new List<PotItem>();
        foreach (PotItem item in data)
        {
            for (int i = 0; i < item.Uses; i++)
            {
                itemIndexes.Add(item);
            }
        }
        PotItem choice = itemIndexes[Mathf.Max(0, Mathf.Min(useIndex, itemIndexes.Count - 1))];
        choice.Uses--;
        // remove any items with uses equal to zero
        for (int i = 0; i < data.Count; i++)
        {
            if (data[i].Uses <= 0)
            {
                Plugin.Log.LogInfo($"Item {data[i].Item().GetItemName()} removed from Cooking Pot!");
                data.RemoveAt(i);
                i--;
            }
        }
        OptionableIntItemData usesData = item.GetData<OptionableIntItemData>(DataEntryKey.ItemUses);
        if (usesData.HasData && usesData.Value == 0)
        {
            data.Clear();
        }
        SetModItemDataFromJson(data);
        OnInstanceDataSet();
        Plugin.Log.LogInfo($"Cooking Pot State: {CurrentEffects}");
        Plugin.Log.LogInfo($"Cooking Pot Items: {JsonConvert.SerializeObject(data)}");
    }

    // this can be called before Start(), so be careful
    public override void OnInstanceDataSet()
    {
        List<PotItem> data = GetData();
        // diff with current dummy items
        Dictionary<ushort, int> diff = new Dictionary<ushort, int>();
        foreach (var item in data)
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
        foreach (var k in new List<ushort>(diff.Keys))
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
                        Destroy(dummyItems[i].Object);
                        dummyItems.RemoveAt(i);
                        i--;
                        diff[k]++;
                    }
                }
            }
        }
        // recalculate stats
        CurrentEffects.FromCookingPotItems(data, data.Sum(x => x.Uses));
        CurrentEffects.UpdateGenerated(gameObject);
    }

    private void ClearItemsFromData()
    {
        SetModItemDataFromJson(new List<PotItem>());
    }

    private void AddItemToData(ushort ID, int cookedAmount, int uses)
    {
        var data = GetData();
        IncreaseUses(uses);
        data.Add(new PotItem() {
            ID = ID,
            CookedAmount = cookedAmount,
            Uses = uses,
        });
        SetModItemDataFromJson(data);
    }

    private void IncreaseUses(int amount)
    {
        OptionableIntItemData data = item.GetData<OptionableIntItemData>(DataEntryKey.ItemUses);
        if (data.HasData)
        {
            int newAmount = Mathf.Min(data.Value + amount, item.totalUses);
            data.Value = newAmount;
            if (item.totalUses > 0)
            {
                item.SetUseRemainingPercentage(data.Value / (float)item.totalUses);
            }
        }
    }

    private List<PotItem> GetData()
    {
        bool success = TryGetModItemDataFromJson(out List<PotItem>? data);
        if (!success)
        {
            SetModItemDataFromJson(new List<PotItem>());
        }
        success = TryGetModItemDataFromJson(out data);
        if (data == null || !success)
        {
            throw new NullReferenceException("Failed to read item data.");
        }
        return data;
    }

    [PunRPC]
    private void UpdateInstanceDataRPC()
    {
        OnInstanceDataSet();
    }
}